using System.Collections.Generic;
using GroupProject.Preparation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace GroupProject.Opening
{
    /// <summary>
    /// A visual body inferred from the tracked head and controllers, not lower-body tracking.
    /// Head movement never writes to the camera or changes the locomotion rig.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class PlayerBodyAvatar : MonoBehaviour
    {
        public static PlayerBodyAvatar Active { get; private set; }
        public Transform playerRoot, head, leftController, rightController;
        [Tooltip("Optional wrist overrides. Normally the animated controller hands provide these.")]
        public Transform leftWrist, rightWrist;
        public Material everydayTop, everydayTrousers, cleanroomFabric, everydayShoes;
        [Range(15, 70)] public float torsoTurnDeadZone = 35;
        [Min(.2f)] public float calibrationSeconds = 2;

        public float CalibratedEyeHeight { get; private set; } = 1.65f;
        public float FloorHeight { get; private set; }
        public bool IsCalibrated { get; private set; }
        public Transform VisualRoot => visualRoot;
        public Transform Chest { get; private set; }
        public Transform LeftFoot { get; private set; }
        public Transform RightFoot { get; private set; }
        public SkinnedMeshRenderer BodyRenderer { get; private set; }
        public bool CoverallWorn { get; private set; }
        public bool PersonalJacketStored { get; private set; }
        public float BodyYaw => bodyYaw;
        public float BodyScale => CalibratedEyeHeight / 1.65f;
        public Quaternion BodyRotation => Quaternion.Euler(0, bodyYaw, 0);

        Transform visualRoot;
        readonly Transform[] bones = new Transform[10];
        readonly RaycastHit[] floorHits = new RaycastHit[32];
        readonly List<Material> ownedMaterials = new List<Material>();
        readonly Vector3[] elbowDirections={new Vector3(-.2f,-1,-.12f),new Vector3(.2f,-1,-.12f)};
        readonly bool[] footwearWorn = new bool[2];
        readonly GameObject[] shoes = new GameObject[2];
        Mesh bodyMesh;
        Material topMaterial, trousersMaterial, suitMaterial, shoeMaterial;
        float bodyYaw, previousRootYaw, validCalibrationTime, tallestSample;
        bool wasDesktop;
        bool initialized;

        // Neutral standing body coordinates, in metres at a 1.65 m eye height.
        static readonly Vector3[] Rest = {
            new Vector3(0,.88f,0), new Vector3(0,1.345f,0),
            new Vector3(-.205f,1.39f,0), new Vector3(-.32f,1.13f,.07f),
            new Vector3(.205f,1.39f,0), new Vector3(.32f,1.13f,.07f),
            new Vector3(-.10f,.88f,0), new Vector3(-.11f,.47f,.025f),
            new Vector3(.10f,.88f,0), new Vector3(.11f,.47f,.025f)
        };
        static Vector3 RestWrist(bool left) => new Vector3(left ? -.32f : .32f, .98f, .31f);
        static Vector3 RestAnkle(bool left) => new Vector3(left ? -.11f : .11f, .115f, .045f);

        void Awake() { Active = this; }
        void OnEnable() { Active = this; if (visualRoot) visualRoot.gameObject.SetActive(true); }
        void OnDisable() { if (Active == this) Active = null; if (visualRoot) visualRoot.gameObject.SetActive(false); }
        void Start() { Initialize(); }

        public void Configure(Transform root, Transform camera, Transform left, Transform right)
        {
            playerRoot = root; head = camera; leftController = left; rightController = right;
            if (Application.isPlaying) Initialize();
        }

        public void Initialize()
        {
            if (initialized) return;
            if (!playerRoot) playerRoot = transform;
            if (!head && Camera.main) head = Camera.main.transform;
            if (!head) return;
            Active = this;
            FloorHeight = FindFloor();
            bodyYaw = head.eulerAngles.y;
            previousRootYaw = playerRoot.eulerAngles.y;
            wasDesktop = DesktopPreview.Active;
            validCalibrationTime = 0;
            visualRoot = MakeTransform("Visible player body", transform);
            Chest = MakeTransform("Chest fitting anchor", visualRoot);
            LeftFoot = MakeTransform("Left foot floor anchor", visualRoot);
            RightFoot = MakeTransform("Right foot floor anchor", visualRoot);
            string[] names = { "Pelvis", "Chest", "Left upper arm", "Left forearm", "Right upper arm", "Right forearm", "Left thigh", "Left shin", "Right thigh", "Right shin" };
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i] = MakeTransform(names[i], visualRoot);
                bones[i].localPosition = Rest[i];
            }
            topMaterial = MakeMaterial(everydayTop, "Everyday shirt", new Color(.16f,.22f,.28f));
            trousersMaterial = MakeMaterial(everydayTrousers, "Everyday trousers", new Color(.10f,.12f,.15f));
            suitMaterial = MakeMaterial(cleanroomFabric ? cleanroomFabric : Resources.Load<Material>("DressingMaterials/Coverall_0"),
                "Worn cleanroom fabric", new Color(.84f,.89f,.92f));
            shoeMaterial = MakeMaterial(everydayShoes, "Everyday shoes", new Color(.075f,.085f,.10f));

            bodyMesh = BodySurface.Build(Rest, RestWrist(true), RestWrist(false), RestAnkle(true), RestAnkle(false));
            var bind = new Matrix4x4[bones.Length];
            for (int i = 0; i < bind.Length; i++) bind[i] = bones[i].worldToLocalMatrix * visualRoot.localToWorldMatrix;
            bodyMesh.bindposes = bind;
            BodyRenderer = MakeTransform("Continuous body clothing", visualRoot).gameObject.AddComponent<SkinnedMeshRenderer>();
            BodyRenderer.sharedMesh = bodyMesh;
            BodyRenderer.bones = bones;
            BodyRenderer.rootBone = visualRoot;
            BodyRenderer.quality = SkinQuality.Bone2;
            BodyRenderer.localBounds = new Bounds(new Vector3(0,.9f,0), new Vector3(3,3,3));
            BodyRenderer.updateWhenOffscreen = true;
            BodyRenderer.shadowCastingMode = ShadowCastingMode.On;
            BodyRenderer.receiveShadows = true;
            CreateShoe(true); CreateShoe(false);
            initialized = true;
            RefreshClothing();
            if (DesktopPreview.Active) CalibrateStanding();
            UpdatePose();
        }

        static Transform MakeTransform(string label, Transform parent)
        {
            var node = new GameObject(label).transform;
            node.SetParent(parent, false);
            return node;
        }

        Material MakeMaterial(Material source, string label, Color fallback)
        {
            var material = source ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = label;
            if (!source)
            {
                material.SetColor("_BaseColor", fallback);
                material.SetFloat("_Smoothness", .23f);
                material.SetFloat("_Metallic", 0);
            }
            ownedMaterials.Add(material);
            return material;
        }

        void CreateShoe(bool left)
        {
            var mesh = Resources.Load<Mesh>("DressingMeshes/Boot");
            if (!mesh) return;
            var shoe = MakeTransform(left ? "Left everyday shoe" : "Right everyday shoe", left ? LeftFoot : RightFoot);
            shoe.localRotation = Quaternion.Euler(0,180,0);
            shoe.localPosition = Vector3.up * -mesh.bounds.min.y;
            shoe.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = shoe.gameObject.AddComponent<MeshRenderer>();
            var materials = new Material[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = shoeMaterial;
            renderer.sharedMaterials = materials;
            shoes[left ? 0 : 1] = shoe.gameObject;
            shoe.gameObject.SetActive(!footwearWorn[left ? 0 : 1]);
        }

        public void SetCoverall(bool worn) { CoverallWorn = worn; RefreshClothing(); }
        public void SetPersonalJacketStored(bool stored) { PersonalJacketStored = stored; RefreshClothing(); }
        public bool IsFootwearWorn(bool left) => footwearWorn[left ? 0 : 1];
        public void SetFootwear(bool left, bool worn)
        {
            int index = left ? 0 : 1;
            footwearWorn[index] = worn;
            if (shoes[index]) shoes[index].SetActive(!worn);
        }
        void RefreshClothing()
        {
            if (!BodyRenderer) return;
            if (topMaterial && !everydayTop)
                topMaterial.SetColor("_BaseColor", PersonalJacketStored ? new Color(.45f,.50f,.53f) : new Color(.16f,.22f,.28f));
            BodyRenderer.sharedMaterials = CoverallWorn
                ? new[] { suitMaterial, suitMaterial } : new[] { topMaterial, trousersMaterial };
        }

        /// <summary>Call while standing comfortably after recentering. It never moves the tracked camera.</summary>
        [ContextMenu("Recalibrate Standing Height")]
        public void RecalibrateStandingHeight() { CalibrateStanding(); }
        public void CalibrateStanding()
        {
            if (!head || !playerRoot) return;
            FloorHeight = FindFloor();
            float height = head.position.y - FloorHeight;
            if (!ValidEyeHeight(height)) return;
            CalibratedEyeHeight = height;
            IsCalibrated = true;
            tallestSample = height;
            if (initialized) UpdatePose();
        }
        static bool ValidEyeHeight(float value) => value >= .85f && value <= 2.25f;

        void LateUpdate()
        {
            if (!initialized) { Initialize(); if (!initialized) return; }
            FloorHeight = FindFloor();
            bool preview = DesktopPreview.Active;
            if (preview != wasDesktop)
            {
                wasDesktop = preview;
                if (preview) CalibrateStanding();
                else { IsCalibrated = false; tallestSample = 0; validCalibrationTime = 0; }
            }
            if (!IsCalibrated)
            {
                var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                bool tracking = preview || (device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked);
                float sample = head.position.y - FloorHeight;
                if (tracking && ValidEyeHeight(sample))
                {
                    tallestSample = Mathf.Max(tallestSample, sample);
                    validCalibrationTime += Time.unscaledDeltaTime;
                    if (validCalibrationTime >= calibrationSeconds)
                    { CalibratedEyeHeight = tallestSample; IsCalibrated = true; }
                }
                else { validCalibrationTime = 0; tallestSample = 0; }
            }
            float rootYaw = playerRoot.eulerAngles.y;
            bodyYaw += Mathf.DeltaAngle(previousRootYaw, rootYaw);
            previousRootYaw = rootYaw;
            // A steep downward gaze has no useful heading. Retain the previous torso heading.
            var horizontalLook = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (horizontalLook.sqrMagnitude > .08f)
            {
                float gazeYaw = Quaternion.LookRotation(horizontalLook).eulerAngles.y;
                float delta = Mathf.DeltaAngle(bodyYaw, gazeYaw);
                if (Mathf.Abs(delta) > torsoTurnDeadZone)
                    bodyYaw = Mathf.MoveTowardsAngle(bodyYaw, gazeYaw - Mathf.Sign(delta) * torsoTurnDeadZone * .65f, 120 * Time.unscaledDeltaTime);
            }
            UpdatePose();
        }

        float FindFloor()
        {
            float rootY = playerRoot ? playerRoot.position.y : transform.position.y;
            Vector3 point = head ? head.position : transform.position;
            int count = Physics.RaycastNonAlloc(new Vector3(point.x, rootY + .55f, point.z), Vector3.down,
                floorHits, 2.2f, ~0, QueryTriggerInteraction.Ignore);
            float floor = rootY, nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = floorHits[i]; var collider = hit.collider;
                if (!collider || hit.normal.y < .65f || collider.transform.IsChildOf(playerRoot)
                    || collider.GetComponentInParent<WearableGarment>()) continue;
                if (hit.distance < nearest) { nearest = hit.distance; floor = hit.point.y; }
            }
            return floor;
        }

        void UpdatePose()
        {
            float scale = BodyScale;
            visualRoot.SetPositionAndRotation(new Vector3(head.position.x, FloorHeight, head.position.z)
                - BodyRotation * Vector3.forward * (.115f * scale), BodyRotation);
            visualRoot.localScale = Vector3.one * scale;
            float eye = (head.position.y - FloorHeight) / scale;
            float crouch = Mathf.Max(0, 1.65f - eye);
            float shoulderY = Mathf.Max(.64f, 1.39f - crouch);
            float hipY = Mathf.Clamp(.88f - crouch * .64f, .34f, .94f);
            bones[0].localPosition = new Vector3(0,hipY,0);
            bones[1].localPosition = new Vector3(0,shoulderY-.045f,0);
            Chest.localPosition = new Vector3(0,shoulderY-.20f,.04f);
            LeftFoot.localPosition = new Vector3(-.11f,0,.09f);
            RightFoot.localPosition = new Vector3(.11f,0,.09f);
            PoseArm(true, shoulderY); PoseArm(false, shoulderY);
            PoseLeg(true, hipY); PoseLeg(false, hipY);
        }

        Vector3 WristPosition(bool left)
        {
            Transform wrist = left ? leftWrist : rightWrist;
            var hands = AnimatedControllerHands.Active;
            if (!wrist && hands && hands.transform.IsChildOf(playerRoot)) wrist = left ? hands.LeftWrist : hands.RightWrist;
            if (wrist) return visualRoot.InverseTransformPoint(wrist.position);
            Transform controller = left ? leftController : rightController;
            if (controller && controller.gameObject.activeInHierarchy)
                return visualRoot.InverseTransformPoint(controller.TransformPoint(new Vector3(0,-.025f,-.075f)));
            return new Vector3(left ? -.24f : .24f, (head.position.y - FloorHeight) / BodyScale - .43f, .38f);
        }
        void PoseArm(bool left, float shoulderY)
        {
            int index = left ? 2 : 4;
            Vector3 start = new Vector3(left ? -.205f : .205f, shoulderY, 0);
            Vector3 end = WristPosition(left);
            // Reject invalid tracking jumps; do not stretch a sleeve across the room.
            if ((end-start).sqrMagnitude > 2.25f) end = start + new Vector3(left ? -.12f : .12f,-.35f,.28f);
            int side=left?0:1;
            var hint=new Vector3(left?-.2f:.2f,-1,Mathf.Lerp(-.12f,.12f,Mathf.InverseLerp(shoulderY-.4f,shoulderY+.4f,end.y)));
            elbowDirections[side]=Vector3.Slerp(elbowDirections[side],hint,1-Mathf.Exp(-12*Time.unscaledDeltaTime));
            Vector3 elbow = SolveJoint(start, end, Vector3.Distance(Rest[index],Rest[index+1]),
                Vector3.Distance(Rest[index+1],RestWrist(left)), elbowDirections[side]);
            PoseBone(index, start, elbow, Rest[index+1]-Rest[index]);
            PoseBone(index+1, elbow, end, RestWrist(left)-Rest[index+1]);
        }
        void PoseLeg(bool left, float hipY)
        {
            int index = left ? 6 : 8;
            Vector3 hip = new Vector3(left ? -.10f : .10f, hipY, 0);
            Vector3 ankle = RestAnkle(left) + Vector3.forward * .045f;
            Vector3 knee = SolveJoint(hip, ankle, Vector3.Distance(Rest[index],Rest[index+1]),
                Vector3.Distance(Rest[index+1],RestAnkle(left)), Vector3.forward);
            PoseBone(index, hip, knee, Rest[index+1]-Rest[index]);
            PoseBone(index+1, knee, ankle, RestAnkle(left)-Rest[index+1]);
        }
        static Vector3 SolveJoint(Vector3 start, Vector3 end, float upper, float lower, Vector3 bendHint)
        {
            Vector3 direction = end - start;
            float distance = Mathf.Max(.001f,direction.magnitude);
            direction /= distance;
            float stretch = Mathf.Max(1, distance / (upper+lower) * 1.001f);
            upper *= stretch; lower *= stretch;
            float along = Mathf.Clamp((distance*distance + upper*upper - lower*lower) / (2*distance), -upper, upper);
            Vector3 bend = Vector3.ProjectOnPlane(bendHint,direction).normalized;
            if (bend.sqrMagnitude < .01f) bend = Vector3.Cross(direction,Vector3.right).normalized;
            return start + direction*along + bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        }
        void PoseBone(int index, Vector3 start, Vector3 end, Vector3 restDirection)
        {
            bones[index].localPosition = start;
            bones[index].localRotation = Quaternion.FromToRotation(restDirection,end-start);
            bones[index].localScale = Vector3.one * ((end-start).magnitude / restDirection.magnitude);
        }

        /// <summary>Foot positions are floor anchors; the caller adds the garment's sole offset.</summary>
        public bool GetGarmentPose(WearableGarment.Kind kind, out Vector3 position, out Quaternion rotation, out float scale)
        {
            position = head ? head.position : transform.position;
            rotation = BodyRotation * Quaternion.Euler(0,180,0);
            scale = BodyScale;
            if (!initialized) return false;
            switch (kind)
            {
                case WearableGarment.Kind.Coverall: position = Chest.position; return true;
                case WearableGarment.Kind.LeftBoot: case WearableGarment.Kind.LeftOvershoe: position = LeftFoot.position; return true;
                case WearableGarment.Kind.RightBoot: case WearableGarment.Kind.RightOvershoe: position = RightFoot.position; return true;
                case WearableGarment.Kind.Hood: case WearableGarment.Kind.HairCap:
                    position = head.position + Vector3.up*.07f*scale; return true;
                case WearableGarment.Kind.Mask: case WearableGarment.Kind.SafetyGlasses:
                    position = head.position + head.forward*.13f*scale;
                    rotation = head.rotation * Quaternion.Euler(0,180,0); return true;
                default: return false;
            }
        }
        void OnDestroy()
        {
            if (Active == this) Active = null;
            if (bodyMesh) Destroy(bodyMesh);
            foreach (var material in ownedMaterials) if (material) Destroy(material);
            if (visualRoot) Destroy(visualRoot.gameObject);
        }

        // A single low-cost skinned surface, built once. There are no primitive joints or physics colliders.
        static class BodySurface
        {
            const int Sides = 24;
            sealed class Builder
            {
                public readonly List<Vector3> vertices = new List<Vector3>();
                public readonly List<Vector2> uv = new List<Vector2>();
                public readonly List<BoneWeight> weights = new List<BoneWeight>();
                public readonly List<int>[] triangles = { new List<int>(), new List<int>() };
                public void Ring(Vector3 center, Vector3 right, Vector3 back, float width, float depth, float v, int boneA, int boneB, float blend, float folds)
                {
                    for (int j=0;j<=Sides;j++)
                    {
                        float angle = j * Mathf.PI*2/Sides;
                        float crease = 1 + folds*Mathf.Cos(angle*6+v*9);
                        vertices.Add(center + right*(Mathf.Cos(angle)*width*crease) + back*(Mathf.Sin(angle)*depth*crease));
                        uv.Add(new Vector2((float)j/Sides,v));
                        weights.Add(new BoneWeight { boneIndex0=boneA, boneIndex1=boneB, weight0=1-blend, weight1=blend });
                    }
                }
                public void Join(int first, int rings, int material)
                {
                    for (int i=0;i<rings-1;i++) for(int j=0;j<Sides;j++)
                    {
                        int a=first+i*(Sides+1)+j, b=a+Sides+1;
                        triangles[material].Add(a); triangles[material].Add(b); triangles[material].Add(b+1);
                        triangles[material].Add(a); triangles[material].Add(b+1); triangles[material].Add(a+1);
                    }
                }
            }
            public static Mesh Build(Vector3[] rest, Vector3 leftWrist, Vector3 rightWrist, Vector3 leftAnkle, Vector3 rightAnkle)
            {
                var b = new Builder();
                float[] ys={.77f,.86f,1.0f,1.16f,1.32f,1.40f,1.46f};
                float[] widths={.176f,.205f,.155f,.18f,.203f,.205f,.060f};
                float[] depths={.105f,.118f,.105f,.12f,.127f,.105f,.052f};
                for(int i=0;i<ys.Length;i++) b.Ring(new Vector3(0,ys[i],0),Vector3.right,Vector3.forward,
                    widths[i],depths[i],ys[i],0,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.86f,1.35f,ys[i])),.014f);
                b.Join(0,ys.Length,0);
                // Close the neck and pelvis so looking down never exposes the hollow inside of the body.
                Cap(b,new Vector3(0,ys[ys.Length-1],0),(ys.Length-1)*(Sides+1),1,true,0);
                Cap(b,new Vector3(0,ys[0],0),0,0,false,0);
                Limb(b,rest[2],rest[3],leftWrist,2,3,.080f,.057f,.034f,0);
                Limb(b,rest[4],rest[5],rightWrist,4,5,.080f,.057f,.034f,0);
                Limb(b,rest[6],rest[7],leftAnkle,6,7,.098f,.078f,.052f,1);
                Limb(b,rest[8],rest[9],rightAnkle,8,9,.098f,.078f,.052f,1);
                var mesh = new Mesh { name="Player continuous clothing" };
                mesh.SetVertices(b.vertices); mesh.SetUVs(0,b.uv); mesh.boneWeights=b.weights.ToArray();
                mesh.subMeshCount=2;
                mesh.SetTriangles(b.triangles[0],0); mesh.SetTriangles(b.triangles[1],1);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
                return mesh;
            }
            static void Cap(Builder b,Vector3 center,int ring,int bone,bool up,int material)
            {
                int index=b.vertices.Count;b.vertices.Add(center);b.uv.Add(new Vector2(.5f,.5f));
                b.weights.Add(new BoneWeight{boneIndex0=bone,weight0=1});
                for(int j=0;j<Sides;j++){b.triangles[material].Add(index);b.triangles[material].Add(ring+(up?j+1:j));b.triangles[material].Add(ring+(up?j:j+1));}
            }
            static void Limb(Builder b,Vector3 start,Vector3 joint,Vector3 end,int upper,int lower,float startRadius,float jointRadius,float endRadius,int material)
            {
                const int rings=20;
                int first=b.vertices.Count;
                Vector3 control=joint*2-(start+end)*.5f;
                for(int i=0;i<rings;i++)
                {
                    float t=(float)i/(rings-1), s=1-t;
                    Vector3 center=start*s*s+control*2*s*t+end*t*t;
                    Vector3 tangent=((control-start)*s+(end-control)*t).normalized;
                    Vector3 right=Vector3.Cross(Vector3.forward,tangent).normalized;
                    if(right.sqrMagnitude<.01f)right=Vector3.right;
                    Vector3 back=Vector3.Cross(right,tangent).normalized;
                    float radius=t<.5f?Mathf.Lerp(startRadius,jointRadius,t*2):Mathf.Lerp(jointRadius,endRadius,(t-.5f)*2);
                    if(material==0 && i<2)
                    {
                        // Blend the sleeve's inset shoulder opening into the chest instead of exposing a cut tube.
                        if(i==0)center=new Vector3(start.x*.65f,start.y-.025f,start.z);
                        b.Ring(center,right,back,radius,radius*.90f,t,1,upper,i==0?0:.75f,.025f);
                    }
                    else b.Ring(center,right,back,radius,radius*.90f,t,upper,lower,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.34f,.66f,t)),.025f);
                }
                b.Join(first,rings,material);
            }
        }
    }
}
