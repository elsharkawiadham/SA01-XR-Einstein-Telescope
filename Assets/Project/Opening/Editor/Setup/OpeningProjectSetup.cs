using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Opening;

public static class OpeningProjectSetup
{
    public const string ScenePath = "Assets/Project/Scenes/01_Opening.unity";
    [MenuItem("Tools/Opening Scene/Open Scene")]
    public static void Open()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }
    [MenuItem("Tools/Opening Scene/Configure Desktop Preview")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var opening = UnityEngine.Object.FindFirstObjectByType<OpeningSequence>();
        var rig = GameObject.Find("Shared_XR_Player");
        var desktop = rig.GetComponent<DesktopPreview>();
        if (!desktop) desktop = rig.AddComponent<DesktopPreview>();
        desktop.view = opening.headsetCamera;
        desktop.tutorial = opening.locomotionGuide; desktop.guide = opening.aiGuide;
        desktop.tutorial.desktopSteps=System.Array.Empty<AudioClip>();
        var elevator=UnityEngine.Object.FindFirstObjectByType<ElevatorCallButton>();
        elevator.desktopHintClip=null;
        // The saved scene is the source of truth; delete superseded inactive corridor modules.
        var root = GameObject.Find("Scene01_Opening_Adham");
        var unused = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t != root.transform && !t.gameObject.activeSelf && (t.name.EndsWith(" module") || t.name.StartsWith("Path segment ") || t.name.StartsWith("Central pool ") || t.name == "Movement instructions - left wall" || t.name == "Welcome transcript" || t.name == "Arrow right"))
            .Select(t => t.gameObject).ToArray();
        foreach (var item in unused) if(item)UnityEngine.Object.DestroyImmediate(item);
        foreach (var obj in scene.GetRootGameObjects())
            foreach (var t in obj.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) throw new Exception("Missing script: " + t.name);
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials)
                if (!material || !material.shader || !material.shader.isSupported) throw new Exception("Missing/unsupported material: " + renderer.name);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/OpeningLayoutCheck.txt", "PASS: moved scene loads, no missing scripts/materials, desktop camera assigned. Removed inactive legacy modules: " + unused.Length);
        Debug.Log("OPENING LAYOUT CHECK PASS; legacy modules removed: " + unused.Length);
    }
}
