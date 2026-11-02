using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using GroupProject.Opening;
using GroupProject.Preparation;

[InitializeOnLoad]
public static class TrainingRevisionCheck
{
    static bool running;
    static TrainingRevisionCheck(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        EditorSceneManager.OpenScene(ProjectAssembly.Main);
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        foreach(var a in UnityEngine.Object.FindObjectsByType<StandaloneAIGuide>(FindObjectsInactive.Include,FindObjectsSortMode.None))a.gameObject.SetActive(false);
        var opening=UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();opening.enabled=false;opening.locomotionGuide.enabled=false;
        SessionState.SetBool("ET.RevisionCheck",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        int exit=SessionState.GetInt("ET.RevisionExit",-1);
        if(exit>=0&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt("ET.RevisionExit",-1);EditorApplication.Exit(exit);return;}
        if(running||!SessionState.GetBool("ET.RevisionCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        running=true;Check();
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static async Task Until(Func<bool> condition,string message)
    {double start=EditorApplication.timeSinceStartup;while(!condition()){if(EditorApplication.timeSinceStartup-start>12)throw new Exception(message);await Task.Delay(50);}}
    static void Move(PreparationJourney j,Vector3 headPosition,float yaw=0)
    {var c=j.ride.playerRoot.GetComponent<CharacterController>();if(c)c.enabled=false;j.ride.playerRoot.rotation=Quaternion.Euler(0,yaw,0);j.ride.playerRoot.position+=headPosition-j.head.position;Physics.SyncTransforms();}
    static void CheckCard(Transform card,Camera camera,float width,float height,float tab)
    {
        ChecklistPlacement.Place(card,camera.transform,width,height,tab);
        foreach(var local in new[]{new Vector3(0,tab),new Vector3(width,tab),new Vector3(0,-height),new Vector3(width,-height)})
        {var p=camera.WorldToViewportPoint(card.TransformPoint(local));Require(p.z>0&&p.x>.01f&&p.x<.99f&&p.y>.01f&&p.y<.99f,"Checklist outside view: "+p);}
    }
    static async void Check()
    {
        int exit=1;
        try
        {
            await Task.Delay(600);
            var j=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>();j.guide=null;j.ride.destinationRoot.SetActive(true);j.ride.enabled=false;
            typeof(PreparationJourney).GetField("briefed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(j,true);
            foreach(var c in j.ride.departureRoot.GetComponentsInChildren<Collider>(true))if(!c.transform.IsChildOf(j.ride.transform))c.enabled=false;
            Require(UnityEngine.Object.FindObjectsByType<GroupProject.Cleaning.CleaningStation>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"Cleaning demo still in playable scene");
            var smooth=j.ride.playerRoot.GetComponentInChildren<SmoothStickTurnProvider>(true);Require(smooth&&smooth.turnSpeed==45&&!smooth.enableTurnAround,"Smooth turn setup");
            Require(j.ride.playerRoot.GetComponentsInChildren<SnapTurnProvider>(true).All(p=>!p.enabled),"Snap turn still enabled");
            foreach(int hz in new[]{30,60,90,120}){float angle=0;for(int i=0;i<hz;i++)angle+=SmoothStickTurnProvider.TurnDelta(1,1f/hz);Require(Mathf.Abs(angle-45)<.002f,"Frame-dependent turn at "+hz);}
            Require(SmoothStickTurnProvider.TurnDelta(.1f,1)==0&&SmoothStickTurnProvider.TurnDelta(0,1)==0,"Turn deadzone / stop");
            Require(SmoothStickTurnProvider.TurnDelta(.4f,1)>0&&SmoothStickTurnProvider.TurnDelta(.4f,1)<SmoothStickTurnProvider.TurnDelta(.8f,1),"Turn not proportional");
            Move(j,new Vector3(23,1.85f,19.3f));await Task.Delay(200);
            Require(!j.Select(4)&&!j.Select(5),"Undressed shower access accepted");
            j.StorePersonalClothes();for(int i=1;i<=11;i++)j.Equip(i);
            await Until(()=>j.entryDoor.IsOpen,"Dressing did not open entry");
            Require(!j.Select(4),"Shower started with open entry");
            Move(j,new Vector3(22.1f,1.85f,19.3f));Require(!j.Select(5),"Door closed onto doorway occupant");
            Move(j,new Vector3(23,1.85f,19.3f));Require(j.Select(5),"Close door action rejected");Require(!j.Select(4),"Shower started while door moving");
            await Until(()=>j.entryDoor.IsClosed,"Entry failed to close");j.showerSeconds=2;Require(j.Select(4),"Closed-door start failed");Require(!j.Select(4),"Duplicate start accepted");
            await Task.Delay(500);var fx=UnityEngine.Object.FindAnyObjectByType<AirShowerEffects>();Require(fx.fan.isPlaying&&fx.jets.All(p=>p.isPlaying),"Shower effects absent");
            GuidePresentationCheck.Capture("Logs/Revision-Shower.png");
            Require(j.Select(5)&&!j.ShowerRunning&&!j.ShowerComplete,"Open-door cancel failed");await Until(()=>j.entryDoor.IsOpen,"Cancel did not reopen entry");
            j.Select(5);await Until(()=>j.entryDoor.IsClosed,"Restart door did not close");Require(j.Select(4),"Restart failed");
            Move(j,new Vector3(21,1.85f,19.3f));await Task.Delay(200);Require(!j.ShowerRunning&&!j.ShowerComplete,"Leaving did not cancel");await Until(()=>j.entryDoor.IsOpen,"Exit cancellation left entry closed");
            Move(j,new Vector3(23,1.85f,19.3f));j.Select(5);await Until(()=>j.entryDoor.IsClosed,"Final close failed");j.showerSeconds=.5f;Require(j.Select(4),"Final start failed");
            await Until(()=>j.ShowerComplete&&j.exitDoor.IsOpen,"Completion did not open second door");Require(j.entryDoor.IsClosed,"Entry opened during clean exit");
            j.entryDoor.SetOpen(true);await Until(()=>j.entryDoor.IsOpen,"Entry test setup");ProjectAssembly.ValidateRoute();
            Move(j,new Vector3(34,1.85f,27.3f));await Task.Delay(300);Require(j.FacilityReached,"Facility endpoint not marked");
            Require(Physics.Raycast(new Vector3(34,1.4f,28),Vector3.forward,out var hit,3)&&hit.transform==j.facilityDoor,"Facility door lacks collision");
            Require(!j.facilityDoor.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>(),"Facility door interactable");
            GuidePresentationCheck.Capture("Logs/Revision-Facility.png");
            Move(j,new Vector3(27,1.85f,19.3f),90);await Task.Delay(200);GuidePresentationCheck.Capture("Logs/Revision-Hallway.png");
            Move(j,new Vector3(16,1.85f,19.3f));await Task.Delay(200);
            var outfit=j.GetComponent<DressingRoomExperience>().GetComponentInChildren<DressingChecklist>(true);
            var card=j.head.Find("Training checklist");var camera=j.head.GetComponent<Camera>();float aspect=camera.aspect,fov=camera.fieldOfView;
            foreach(float ratio in new[]{.65f,1f,4f/3f,16f/9f,2.4f})foreach(float degrees in new[]{45f,60f,90f})
            {camera.aspect=ratio;camera.fieldOfView=degrees;camera.ResetProjectionMatrix();CheckCard(card,camera,460,420,40);CheckCard(outfit.transform,camera,400,600,34);}
            camera.aspect=aspect;camera.fieldOfView=fov;camera.ResetProjectionMatrix();await Task.Delay(150);
            GuidePresentationCheck.Capture("Logs/Revision-Checklist.png");
            string report="PASS: proportional turning at 30/60/90/120 Hz, deadzone and immediate stop; full checklist bounds at five aspect ratios and three fields of view; gowning prerequisite; close-door control; start blocked for open/moving doors; occupant clearance; duplicate-start rejection; cancel/restart and leave cancellation; fan/jets; exit animation after completion; entry stays closed; complete route floor/capsule clearance; locked terminal door collision; endpoint progress; no cleaning station in scene.\nEditor simulation. Physical Quest comfort, stereo readability and microphone remain hardware checks.\n";
            File.WriteAllText("Logs/TrainingRevisionCheck.txt",report);Debug.Log(report);exit=0;
        }
        catch(Exception e){File.WriteAllText("Logs/TrainingRevisionCheck.txt","FAIL: "+e);Debug.LogException(e);}
        SessionState.SetBool("ET.RevisionCheck",false);SessionState.SetInt("ET.RevisionExit",exit);EditorApplication.isPlaying=false;
    }
}
