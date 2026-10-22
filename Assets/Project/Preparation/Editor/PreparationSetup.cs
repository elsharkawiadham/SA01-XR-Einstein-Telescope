using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Preparation;
using GroupProject.Elevator;
using GroupProject.Opening;
public static class PreparationSetup
{
    public static void Configure()
    {
        var scene=EditorSceneManager.OpenScene(ProjectAssembly.Main);
        var ride=Object.FindFirstObjectByType<ElevatorRide>();
        var journey=ride.playerRoot.GetComponent<PreparationJourney>();if(!journey)journey=ride.playerRoot.gameObject.AddComponent<PreparationJourney>();
        journey.ride=ride;journey.head=ride.playerHead;journey.guide=Object.FindFirstObjectByType<StandaloneAIGuide>();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
