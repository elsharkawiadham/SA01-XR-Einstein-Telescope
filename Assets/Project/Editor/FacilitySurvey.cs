using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Preparation;
public static class FacilitySurvey
{
    public static void Run(){EditorSceneManager.OpenScene(ProjectAssembly.Main);var j=Object.FindAnyObjectByType<PreparationJourney>();j.ride.destinationRoot.SetActive(true);
        File.WriteAllLines("Logs/FacilitySurvey.txt",Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r is MeshRenderer&&!r.GetComponentInParent<WearableGarment>()).Select(r=>Path(r.transform)+" | "+r.bounds+" | material="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null"))));
        File.WriteAllText("Logs/ElevatorSurvey.txt","Door "+j.ride.callButton.door.position+" scale="+j.ride.callButton.door.lossyScale+" bounds="+j.ride.callButton.door.GetComponentInChildren<Renderer>().bounds+"\nCabin "+j.ride.safeCabin.bounds+"\nButton "+j.ride.callButton.transform.position+"\nProximity "+j.ride.callButton.activationDistance+"\n"+string.Join("\n",j.ride.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t=>Path(t.transform)+" | "+t.transform.position+" | "+t.text)));
    }
    static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
}
