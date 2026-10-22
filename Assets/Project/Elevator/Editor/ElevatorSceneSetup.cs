using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Elevator;
public static class ElevatorSceneSetup
{
    public const string ArrivalPath=OpeningProjectSetup.ScenePath;
    [MenuItem("Tools/Elevator/Configure Descent and Lower Floor")]
    public static void Configure(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var scene=EditorSceneManager.OpenScene(OpeningProjectSetup.ScenePath);
        var ride=UnityEngine.Object.FindFirstObjectByType<ElevatorRide>();
        var lower=System.Array.Find(scene.GetRootGameObjects(),g=>g.name=="02_LowerFloor");
        if(!ride || !lower)throw new Exception("Open the combined main scene; lower floor is missing.");
        ride.destinationRoot=lower;lower.SetActive(false);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(OpeningProjectSetup.ScenePath,true)};
        EditorSceneManager.SaveScene(scene);
    }
}
