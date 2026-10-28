using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Creates visual-only hand prefabs using the rigged Unity XR Hands sample meshes already in this project.</summary>
public static class AnimatedHandSetup
{
    const string Folder = "Assets/Project/Opening/Resources/PlayerHands";
    const string Models = "Assets/Samples/XR Hands/1.8.1/HandVisualizer/Models/";
    [MenuItem("Tools/Player/Prepare Animated Hands")]
    public static void Run()
    {
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var skin = Material("Bare Skin", new Color(.62f, .40f, .29f), .27f);
        Material("Cleanroom Glove", new Color(.88f, .93f, .94f), .33f);
        Make(true, skin); Make(false, skin);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        ValidateGeneratedAssets();
    }

    static Material Material(string name, Color color, float smoothness)
    {
        string path = Folder + "/" + name + ".mat";
        var result = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!result)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader is unavailable for the human hand materials.");
            result = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(result, path);
        }
        result.SetColor("_BaseColor", color); result.SetFloat("_Metallic", 0); result.SetFloat("_Smoothness", smoothness);
        // Hands are solid skin/fabric. Do not inherit the translucent tracking-sample shader.
        result.SetFloat("_Surface", 0); result.SetFloat("_AlphaClip", 0);
        result.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        result.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
        result.SetFloat("_ZWrite", 1);
        result.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); result.DisableKeyword("_ALPHATEST_ON");
        result.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        result.enableInstancing = true; EditorUtility.SetDirty(result); return result;
    }

    static void Make(bool left, Material skin)
    {
        string side = left ? "Left" : "Right", prefix = left ? "L_" : "R_";
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + side + "Hand.fbx");
        if (!source) throw new InvalidOperationException("Missing existing XR Hands model: " + side);
        var root = new GameObject(side + " Human Hand");
        try
        {
            var model = UnityEngine.Object.Instantiate(source, root.transform);
            model.name = "Rigged skin";
            // Raw model contains only bones/skin. Remove any optional importer-added animator; poses come from controller input.
            foreach (var animator in model.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
            var wrist = Find(model.transform, prefix + "Wrist");
            var middle = Find(model.transform, prefix + "MiddleProximal");
            var index = Find(model.transform, prefix + "IndexProximal");
            var little = Find(model.transform, prefix + "LittleProximal");
            if (!wrist || !middle || !index || !little) throw new InvalidOperationException("XR Hands skeleton is missing expected joints.");
            // Normalize the imported bind pose: wrist at origin, fingers +Z, back of hand +Y.
            Vector3 forward = (middle.position - wrist.position).normalized;
            Vector3 across = (index.position - little.position).normalized;
            Vector3 up = Vector3.Cross(forward, across).normalized * (left ? 1 : -1);
            model.transform.rotation = Quaternion.Inverse(Quaternion.LookRotation(forward, up)) * model.transform.rotation;
            model.transform.position += root.transform.position - wrist.position;
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.sharedMaterials = new[] { skin };
                renderer.enabled = true;
                // Finger curls remain inside a generous local bound, without forcing off-screen skinning each frame.
                var bounds = renderer.localBounds; bounds.Expand(.18f); renderer.localBounds = bounds;
                renderer.updateWhenOffscreen = false;
            }
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + side + " Human Hand.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    /// <summary>Checks the actual imported mesh and controller-driven poses; no scene is opened or saved.</summary>
    public static void ValidateGeneratedAssets()
    {
        var rig = new GameObject("Hand validation (temporary)");
        try
        {
            var hands = rig.AddComponent<GroupProject.Opening.AnimatedControllerHands>();
            hands.head = new GameObject("Head").transform; hands.head.SetParent(rig.transform);
            hands.leftController = new GameObject("Left Controller").transform; hands.leftController.SetParent(rig.transform);
            hands.rightController = new GameObject("Right Controller").transform; hands.rightController.SetParent(rig.transform);
            hands.leftController.localPosition = new Vector3(-.25f, 1.1f, .4f);
            hands.rightController.localPosition = new Vector3(.25f, 1.1f, .4f);
            hands.leftHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Left Human Hand.prefab");
            hands.rightHandPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Right Human Hand.prefab");
            hands.bareSkinMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Bare Skin.mat");
            hands.gloveMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Cleanroom Glove.mat");
            hands.Initialize();
            if (!hands.Initialized) throw new InvalidOperationException("Human hands failed to initialize.");
            ValidateHand(hands, true); ValidateHand(hands, false);
        }
        finally { UnityEngine.Object.DestroyImmediate(rig); }
        Debug.Log("Animated hand assets PASS: wrists, skinning, inward finger curl, independent grip/trigger, glove materials.");
    }

    static void ValidateHand(GroupProject.Opening.AnimatedControllerHands hands, bool left)
    {
        var root = left ? hands.LeftVisualRoot : hands.RightVisualRoot;
        var wrist = left ? hands.LeftWrist : hands.RightWrist;
        string prefix = left ? "L_" : "R_";
        if (root.InverseTransformPoint(wrist.position).magnitude > .001f)
            throw new InvalidOperationException(prefix + "wrist is not at the normalized hand origin.");
        var middle = Find(root, prefix + "MiddleProximal");
        var index = Find(root, prefix + "IndexProximal");
        var tip = Find(root, prefix + "MiddleTip");
        var thumb = Find(root, prefix + "ThumbProximal");
        if (!middle || !index || !tip || !thumb) throw new InvalidOperationException(prefix + "finger bones missing.");
        if (root.InverseTransformPoint(middle.position).z <= 0)
            throw new InvalidOperationException(prefix + "hand is not facing along positive Z.");
        if (root.GetComponentInChildren<Collider>(true) || root.GetComponentInChildren<Animator>(true))
            throw new InvalidOperationException("Hand visual prefab must not change controller interaction or run a second animator.");
        var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers.Length == 0) throw new InvalidOperationException(prefix + "skinned mesh missing.");
        foreach (var renderer in renderers)
        {
            if (!renderer.sharedMesh || renderer.bones.Length < 20 || renderer.sharedMesh.bindposes.Length < 20)
                throw new InvalidOperationException(prefix + "mesh is not correctly rigged.");
            if (renderer.bounds.size.magnitude < .05f || renderer.bounds.size.magnitude > 1)
                throw new InvalidOperationException(prefix + "hand size is not human scale in metres.");
        }
        hands.ApplyPose(left, 0, 0, 0, 1);
        var middleOpen = middle.localRotation; var indexOpen = index.localRotation; var thumbOpen = thumb.localRotation;
        var openTip = root.InverseTransformPoint(tip.position);
        hands.ApplyPose(left, .25f, 0, 0, 1);
        if (root.InverseTransformPoint(tip.position).y >= openTip.y - .005f)
            throw new InvalidOperationException(prefix + "initial finger flexion points away from the palm.");
        hands.ApplyPose(left, 1, 0, 0, 1);
        if (Quaternion.Angle(middleOpen, middle.localRotation) < 30 || Quaternion.Angle(indexOpen, index.localRotation) > .1f)
            throw new InvalidOperationException(prefix + "grip must curl the lower fingers independently of the trigger.");
        var closedTip = root.InverseTransformPoint(tip.position);
        if (closedTip.z >= openTip.z - .035f || closedTip.y > .01f)
            throw new InvalidOperationException(prefix + "closed finger does not curl toward the palm.");
        hands.ApplyPose(left, 1, 1, 1, 1);
        if (Quaternion.Angle(indexOpen, index.localRotation) < 30 || Quaternion.Angle(thumbOpen, thumb.localRotation) < 10)
            throw new InvalidOperationException(prefix + "trigger/thumb input does not move its finger joints.");
        hands.SetGloved(left, true);
        foreach (var renderer in renderers) if (renderer.sharedMaterial != hands.gloveMaterial)
            throw new InvalidOperationException(prefix + "glove material failed to apply.");
        hands.SetGloved(left, false);
        foreach (var renderer in renderers) if (renderer.sharedMaterial != hands.bareSkinMaterial)
            throw new InvalidOperationException(prefix + "bare skin failed to restore.");
    }

    static Transform Find(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
        return null;
    }
}
