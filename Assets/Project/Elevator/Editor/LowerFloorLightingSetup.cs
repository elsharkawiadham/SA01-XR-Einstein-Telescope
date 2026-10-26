using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using GroupProject.Elevator;
public static class LowerFloorLightingSetup
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/01_Opening.unity");
        var ride = UnityEngine.Object.FindFirstObjectByType<ElevatorRide>(FindObjectsInactive.Include);
        if (!ride || !ride.destinationRoot) throw new Exception("B1 destination missing");
        var root = ride.destinationRoot;
        root.SetActive(false);
        if (!root.GetComponent<LowerFloorLighting>()) root.AddComponent<LowerFloorLighting>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        var before = RenderSettings.ambientLight;
        root.SetActive(true);
        // Explicitly invoke the lifecycle in edit mode; the same callbacks run on arrival in Play mode.
        root.GetComponent<LowerFloorLighting>().SendMessage("OnEnable");
        if (RenderSettings.ambientMode != AmbientMode.Flat || RenderSettings.ambientLight.r < .4f)
            throw new Exception("B1 ambient did not activate");
        root.GetComponent<LowerFloorLighting>().SendMessage("OnDisable");
        root.SetActive(false);
        if (RenderSettings.ambientLight != before) throw new Exception("Opening ambient was not restored");
        foreach (string name in new[]{"Performance", "Quality"})
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/Project Configuration/" + name + " URP Config.asset");
            if (asset.additionalLightsRenderingMode == UnityEngine.Rendering.Universal.LightRenderingMode.Disabled)
                throw new Exception(name + " disables room lighting");
        }
        File.WriteAllText("Logs/LowerFloorLightingCheck.txt", "PASS: B1 ambient activation and restoration; point lights enabled in both URP profiles. Editor checks; visual headset validation outstanding.");
    }
}
