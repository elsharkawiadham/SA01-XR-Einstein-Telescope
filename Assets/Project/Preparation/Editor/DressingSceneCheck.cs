using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Preparation;
using GroupProject.Opening;
[InitializeOnLoad]
public static class DressingSceneCheck
{
    static int step;static double since;
    static DressingSceneCheck(){EditorApplication.update+=Tick;}
    public static void Run(){DressingAssetsUpgrade.Run();EditorSceneManager.OpenScene(ProjectAssembly.Main);foreach(var g in UnityEngine.Object.FindObjectsByType<StandaloneAIGuide>(FindObjectsInactive.Include,FindObjectsSortMode.None))g.gameObject.SetActive(false);var opening=UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();if(opening){opening.enabled=false;opening.locomotionGuide.enabled=false;}SessionState.SetBool("DressingSceneCheck",true);EditorApplication.isPlaying=true;}
    static void Tick(){if(!SessionState.GetBool("DressingSceneCheck",false)||!EditorApplication.isPlaying)return;
        try{var j=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>();if(!j||!j.GetComponent<DressingRoomExperience>())return;
            if(step==0){j.guide=null;j.ride.enabled=false;j.ride.destinationRoot.SetActive(true);foreach(var c in j.ride.departureRoot.GetComponentsInChildren<Collider>(true))if(!c.transform.IsChildOf(j.ride.transform))c.enabled=false;
                var player=j.ride.playerRoot;var controller=player.GetComponent<CharacterController>();if(controller)controller.enabled=false;player.position+=new Vector3(16,1.65f,20.35f)-j.head.position;player.rotation=Quaternion.identity;
                var items=j.ride.destinationRoot.GetComponentsInChildren<WearableGarment>();if(items.Length!=12)throw new Exception("Expected 12 pieces including personal jacket; got "+items.Length);
                foreach(var item in items){var mesh=item.GetComponent<MeshFilter>().sharedMesh;if(!mesh||mesh.vertexCount<100)throw new Exception("Missing garment mesh: "+item.kind);if(item.GetComponent<MeshRenderer>().sharedMaterials.Length!=4)throw new Exception("Missing garment materials: "+item.kind);}
                foreach(var door in UnityEngine.Object.FindObjectsByType<LockerDoor>(FindObjectsSortMode.None))door.Toggle();since=EditorApplication.timeSinceStartup;step=1;
            }else if(step==1&&EditorApplication.timeSinceStartup-since>2){GuidePresentationCheck.Capture("Logs/DressingRoomPreview.png");
                var list=j.GetComponent<DressingRoomExperience>();
                if(!list.ChecklistVisible)throw new Exception("Checklist did not start visible");
                if(j.Answer("hide my outfit list")==null)throw new Exception("Hide command unhandled");since=EditorApplication.timeSinceStartup;step=2;
            }else if(step==2&&EditorApplication.timeSinceStartup-since>.4){
                var list=j.GetComponent<DressingRoomExperience>();if(list.ChecklistVisible)throw new Exception("Hidden checklist reopened on next frame");
                j.Answer("show my clothing list");if(!list.ChecklistVisible)throw new Exception("Checklist show failed");
                File.WriteAllText("Logs/DressingSceneCheck.txt","PASS: saved scene starts; twelve dressing items present; detailed meshes and four materials assigned; locker access and dressing checklist rendered; spoken-command routing hides across frames and shows again. No scene edits saved.");SessionState.SetBool("DressingSceneCheck",false);EditorApplication.Exit(0);}
        }catch(Exception e){Debug.LogException(e);SessionState.SetBool("DressingSceneCheck",false);EditorApplication.Exit(1);}
    }
}
