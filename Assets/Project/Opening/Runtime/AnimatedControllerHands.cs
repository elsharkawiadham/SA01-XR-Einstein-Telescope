using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace GroupProject.Opening
{
    /// <summary>
    /// Visual hands driven by Quest controller input. The XR controller/interactor remains the input source.
    /// These animated poses are not optical finger tracking or Meta Capsense.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class AnimatedControllerHands : MonoBehaviour
    {
        public static AnimatedControllerHands Active { get; private set; }
        public Transform head, leftController, rightController;
        public GameObject leftHandPrefab, rightHandPrefab;
        public Material bareSkinMaterial, gloveMaterial;
        [Tooltip("Wrist offset relative to the controller grip pose, in metres.")]
        public Vector3 wristOffset = new Vector3(0, -.025f, -.075f);
        public Vector3 wristEuler = new Vector3(-12, 0, 0);
        [Min(1)] public float animationResponse = 18;

        sealed class Joint
        {
            public Transform bone;
            public Quaternion rest;
            public Vector3 axis;
            public int finger;
            public float maximum;
        }
        sealed class Hand
        {
            public bool left, gloved, materialAssigned;
            public Transform mount, wrist;
            public SkinnedMeshRenderer[] renderers;
            public readonly List<Joint> joints = new List<Joint>();
            public readonly Dictionary<Renderer, bool> controllerRenderers = new Dictionary<Renderer, bool>();
            public XRBaseInteractor[] interactors;
            public float grip, trigger, thumb;
        }
        Hand left, right;
        bool initialized, leftGloved, rightGloved, previewGrip, warnedMissingAssets;
        float nextVisualScan, nextInitialization;
        public bool Initialized => initialized;
        public Transform LeftWrist => left?.wrist;
        public Transform RightWrist => right?.wrist;
        public Transform LeftHandAnchor => LeftWrist;
        public Transform RightHandAnchor => RightWrist;
        public Transform LeftVisualRoot => left?.mount;
        public Transform RightVisualRoot => right?.mount;
        public bool IsGloved(bool isLeft) => isLeft ? leftGloved : rightGloved;

        void Awake() { Active = this; }
        void OnEnable()
        {
            Active = this;
            if (left?.mount) left.mount.gameObject.SetActive(true);
            if (right?.mount) right.mount.gameObject.SetActive(true);
        }
        void Start() { Initialize(); }

        public void Initialize()
        {
            if (initialized) return;
            Active = this;
            if (!head && Camera.main) head = Camera.main.transform;
            if (!leftController) leftController = FindNamed(transform, "Left Controller");
            if (!rightController) rightController = FindNamed(transform, "Right Controller");
            // Scene builders may attach this component before the rig's children exist.
            // Wait for both tracked controller anchors instead of leaving duplicate orphan visuals.
            if (!leftController || !rightController) return;
            if (!leftHandPrefab) leftHandPrefab = Resources.Load<GameObject>("PlayerHands/Left Human Hand");
            if (!rightHandPrefab) rightHandPrefab = Resources.Load<GameObject>("PlayerHands/Right Human Hand");
            if (!bareSkinMaterial) bareSkinMaterial = Resources.Load<Material>("PlayerHands/Bare Skin");
            if (!gloveMaterial) gloveMaterial = Resources.Load<Material>("PlayerHands/Cleanroom Glove");
            if (!leftHandPrefab || !rightHandPrefab || !bareSkinMaterial || !gloveMaterial)
            {
                if (!warnedMissingAssets)
                    Debug.LogError("Human hand assets are missing. Run Tools > Player > Prepare Animated Hands before building.", this);
                warnedMissingAssets = true;
                return;
            }
            if (left == null) left = CreateHand(true, leftHandPrefab, leftController);
            if (right == null) right = CreateHand(false, rightHandPrefab, rightController);
            initialized = left != null && right != null;
            SetGloved(true, leftGloved); SetGloved(false, rightGloved);
            if (initialized)
            {
                // Place models immediately, including when editor validation calls Initialize directly.
                UpdateHand(left, leftController); UpdateHand(right, rightController);
            }
        }

        static Transform FindNamed(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        Hand CreateHand(bool isLeft, GameObject prefab, Transform controller)
        {
            var visual = Instantiate(prefab, transform);
            visual.name = isLeft ? "Human Left Hand" : "Human Right Hand";
            var hand = new Hand { left = isLeft, mount = visual.transform,
                renderers = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                interactors = controller ? controller.GetComponentsInChildren<XRBaseInteractor>(true) : new XRBaseInteractor[0] };
            hand.wrist = FindNamed(visual.transform, isLeft ? "L_Wrist" : "R_Wrist");
            if (!hand.wrist)
            {
                Debug.LogError("Human hand prefab has no wrist joint.", visual); DestroyGenerated(visual); return null;
            }
            // Build the flexion axis in the open-hand pose, so left/right bone conventions cannot flip the animation.
            var down = -visual.transform.up;
            foreach (var bone in visual.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name;
                bool thumb = name.Contains("Thumb");
                if (!(name.Contains("Proximal") || name.Contains("Intermediate") || name.Contains("Distal") || thumb && name.Contains("Metacarpal"))) continue;
                if (bone.childCount == 0) continue;
                int finger = thumb ? 2 : name.Contains("Index") ? 1 : 0;
                Vector3 towardTip = (bone.GetChild(0).position - bone.position).normalized;
                Vector3 axis = Vector3.Cross(towardTip, down).normalized;
                if (axis.sqrMagnitude < .1f) axis = visual.transform.right;
                // The imported bind pose is already relaxed; these are additional, not absolute, joint angles.
                float angle = name.Contains("Intermediate") ? 60 : name.Contains("Distal") ? 35 : 55;
                if (thumb) angle = name.Contains("Metacarpal") ? 25 : name.Contains("Proximal") ? 35 : 40;
                hand.joints.Add(new Joint { bone = bone, rest = bone.localRotation,
                    axis = bone.InverseTransformDirection(axis), finger = finger, maximum = angle });
            }
            FindControllerRenderers(hand, controller);
            return hand;
        }

        static void FindControllerRenderers(Hand hand, Transform controller)
        {
            if (!controller) return;
            foreach (var renderer in controller.GetComponentsInChildren<Renderer>(true))
            {
                // Leave ray lines, reticles and interaction components active. Only hardware model meshes are hidden.
                if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;
                bool hardwareVisual = false;
                for (var t = renderer.transform; t && t != controller; t = t.parent)
                {
                    string n = t.name.ToLowerInvariant();
                    if (n.Contains("controller visual") || n.Contains("universalcontroller") || n.Contains("xr controller") || n.Contains("controller model"))
                    { hardwareVisual = true; break; }
                }
                if (hardwareVisual && !hand.controllerRenderers.ContainsKey(renderer))
                    hand.controllerRenderers.Add(renderer, renderer.forceRenderingOff);
            }
        }

        public void SetGloved(bool isLeft, bool value)
        {
            if (isLeft) leftGloved = value; else rightGloved = value;
            var hand = isLeft ? left : right;
            if (hand == null || hand.materialAssigned && hand.gloved == value) return;
            hand.gloved = value;
            hand.materialAssigned = true;
            foreach (var renderer in hand.renderers)
            {
                if (!renderer) continue;
                var materials = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
                for (int i = 0; i < materials.Length; i++) materials[i] = value ? gloveMaterial : bareSkinMaterial;
                renderer.sharedMaterials = materials;
            }
        }

        void LateUpdate()
        {
            if (!initialized)
            {
                if (Time.unscaledTime >= nextInitialization)
                {
                    nextInitialization = Time.unscaledTime + 1;
                    Initialize();
                }
                return;
            }
            if (DesktopPreview.Active && QuestControllerPreview.GripPressed) previewGrip = !previewGrip;
            if (Time.unscaledTime >= nextVisualScan)
            {
                FindControllerRenderers(left, leftController); FindControllerRenderers(right, rightController);
                nextVisualScan = Time.unscaledTime + 1;
            }
            UpdateHand(left, leftController); UpdateHand(right, rightController);
        }

        void UpdateHand(Hand hand, Transform controller)
        {
            // forceRenderingOff survives the controller model animator changing Renderer.enabled.
            // The original flag is restored when this visual system is disabled or destroyed.
            foreach (var entry in hand.controllerRenderers) if (entry.Key) entry.Key.forceRenderingOff = true;
            bool preview = DesktopPreview.Active;
            if (preview && head)
            {
                // This is a visual companion to the virtual controls, not simulated headset tracking.
                Vector3 offset = new Vector3(hand.left ? -.22f : .22f, -.40f, .38f);
                if (!hand.left && QuestControllerPreview.Lift) offset += new Vector3(0, .18f, .06f);
                var yaw = Quaternion.Euler(0, head.eulerAngles.y, 0);
                hand.mount.SetPositionAndRotation(head.position + yaw * offset,
                    Quaternion.Euler(Mathf.Clamp(Mathf.DeltaAngle(0, head.eulerAngles.x), -35, 55) - 12, head.eulerAngles.y, 0));
            }
            else if (controller)
                hand.mount.SetPositionAndRotation(controller.TransformPoint(wristOffset), controller.rotation * Quaternion.Euler(wristEuler));
            else return;

            float grip = 0, trigger = 0, thumb = 0;
            if (preview)
            {
                if (!hand.left) { grip = previewGrip ? 1 : 0; trigger = QuestControllerPreview.Trigger ? 1 : 0; thumb = QuestControllerPreview.Speak ? 1 : 0; }
            }
            else
            {
                var device = InputDevices.GetDeviceAtXRNode(hand.left ? XRNode.LeftHand : XRNode.RightHand);
                device.TryGetFeatureValue(CommonUsages.grip, out grip);
                device.TryGetFeatureValue(CommonUsages.trigger, out trigger);
                bool touched = Feature(device, CommonUsages.primaryTouch) || Feature(device, CommonUsages.secondaryTouch)
                    || Feature(device, CommonUsages.primary2DAxisTouch) || Feature(device, CommonUsages.primaryButton) || Feature(device, CommonUsages.secondaryButton);
                thumb = touched ? 1 : 0;
                foreach (var interactor in hand.interactors) if (interactor && interactor.hasSelection) grip = Mathf.Max(grip, .8f);
            }
            ApplyPose(hand.left, grip, trigger, thumb, Time.unscaledDeltaTime);
        }

        static bool Feature(InputDevice device, InputFeatureUsage<bool> feature) => device.TryGetFeatureValue(feature, out bool value) && value;

        /// <summary>Deterministic animation entry point, also used by the editor regression checks.</summary>
        public void ApplyPose(bool isLeft, float grip, float trigger, float thumb, float deltaTime)
        {
            var hand = isLeft ? left : right;
            if (hand == null) return;
            float blend = 1 - Mathf.Exp(-Mathf.Max(0, deltaTime) * animationResponse);
            hand.grip = Mathf.Lerp(hand.grip, Mathf.Clamp01(grip), blend);
            hand.trigger = Mathf.Lerp(hand.trigger, Mathf.Clamp01(trigger), blend);
            hand.thumb = Mathf.Lerp(hand.thumb, Mathf.Clamp01(thumb), blend);
            foreach (var joint in hand.joints)
            {
                float curl = joint.finger == 0 ? hand.grip : joint.finger == 1 ? hand.trigger : hand.thumb;
                // A slight relaxed bend at rest avoids a rigid, flat-palm silhouette.
                joint.bone.localRotation = joint.rest * Quaternion.AngleAxis(joint.maximum * Mathf.Lerp(.06f, 1, curl), joint.axis);
            }
        }

        static void RestoreControllerVisuals(Hand hand)
        {
            if (hand == null) return;
            foreach (var entry in hand.controllerRenderers) if (entry.Key) entry.Key.forceRenderingOff = entry.Value;
        }

        void OnDisable()
        {
            if (Active == this) Active = null;
            RestoreControllerVisuals(left); RestoreControllerVisuals(right);
            if (left?.mount) left.mount.gameObject.SetActive(false);
            if (right?.mount) right.mount.gameObject.SetActive(false);
            previewGrip = false;
        }

        static void DestroyGenerated(Object item)
        {
            if (!item) return;
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
            RestoreControllerVisuals(left); RestoreControllerVisuals(right);
            if (left?.mount) DestroyGenerated(left.mount.gameObject);
            if (right?.mount) DestroyGenerated(right.mount.gameObject);
        }
    }
}
