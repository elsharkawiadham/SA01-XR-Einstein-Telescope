using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Cleaning;
public static class CleaningSceneSetup
{
    public const string RootPath="Assets/Project/Cleaning";
    public const string Destination=OpeningProjectSetup.ScenePath;
    [MenuItem("Tools/Cleaning/Select Training Station")]
    public static void Build(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        var scene=EditorSceneManager.OpenScene(Destination);
        foreach(var root in scene.GetRootGameObjects()){
            var station=root.GetComponentInChildren<CleaningStation>(true);
            if(station){Selection.activeGameObject=station.gameObject;EditorGUIUtility.PingObject(station);return;}
        }
        throw new Exception("The main scene has no cleaning station.");
    }
    public static void BuildAndCheck(){CleaningExerciseCheck.Run();}
}
