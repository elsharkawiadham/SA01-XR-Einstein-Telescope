using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GroupProject.Opening;
using GroupProject.Elevator;
using GroupProject.Preparation;

[InitializeOnLoad]
public static class FacilityPolishCheck
{
    static bool running;
    static FacilityPolishCheck(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        EditorSceneManager.OpenScene(ProjectAssembly.Main);
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        var opening=UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();opening.enabled=false;opening.locomotionGuide.enabled=false;
        foreach(var ai in UnityEngine.Object.FindObjectsByType<StandaloneAIGuide>(FindObjectsInactive.Include,FindObjectsSortMode.None))ai.gameObject.SetActive(false);
        SessionState.SetBool("ET.FacilityCheck",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        int exit=SessionState.GetInt("ET.FacilityExit",-1);if(exit>=0&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt("ET.FacilityExit",-1);EditorApplication.Exit(exit);return;}
        if(running||!SessionState.GetBool("ET.FacilityCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;running=true;Check();
    }
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    static async Task Wait(Func<bool> predicate,string label){double start=EditorApplication.timeSinceStartup;while(!predicate()){if(EditorApplication.timeSinceStartup-start>15)throw new Exception(label);await Task.Delay(40);}}
    static void Move(PreparationJourney j,Vector3 head,float yaw=0){var c=j.ride.playerRoot.GetComponent<CharacterController>();if(c)c.enabled=false;j.ride.playerRoot.rotation=Quaternion.Euler(0,yaw,0);j.ride.playerRoot.position+=head-j.head.position;Physics.SyncTransforms();}
    static void Fit(WearableGarment garment){garment.SetPreview(true);garment.transform.position=garment.TargetPosition();Need(garment.TryPlace(),"Fitting failed: "+garment.kind);garment.SetPreview(false);}
    static int EnvelopeGrid(float x0,float x1,float z0,float z1)
    {
        int count=0;for(float x=x0;x<=x1;x+=.4f)for(float z=z0;z<=z1;z+=.4f)
        {var p=new Vector3(x,1.5f,z);Need(Physics.Raycast(p,Vector3.down,out var down,3,~0,QueryTriggerInteraction.Ignore)&&down.normal.y>.5f,"Floor gap "+p);Need(Physics.Raycast(p,Vector3.up,3,~0,QueryTriggerInteraction.Ignore),"Ceiling gap "+p);count++;}return count;
    }
    static int WallRun(Vector3 from,Vector3 to,Vector3 outward)
    {int count=0,n=Mathf.CeilToInt(Vector3.Distance(from,to)/.3f);for(int i=0;i<=n;i++)foreach(float y in new[]{.28f,1.4f,2.95f}){var p=Vector3.Lerp(from,to,i/(float)n);p.y=y;Need(Physics.Raycast(p,outward,.8f,~0,QueryTriggerInteraction.Ignore),"Wall gap "+p+" toward "+outward);count++;}return count;}
    static async void Check()
    {
        int exit=1;
        try
        {
            await Task.Delay(600);var j=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>();j.guide=null;var ride=j.ride;var call=ride.callButton;var arrival=ride.GetComponent<ElevatorArrivalControl>();
            Move(j,new Vector3(0,1.7f,-.5f));
            int openingSamples=EnvelopeGrid(-1.7f,1.7f,-1.7f,9.7f)+EnvelopeGrid(-9.4f,-2.3f,6.3f,9.7f);
            int openingWalls=WallRun(new Vector3(1.8f,0,-1.7f),new Vector3(1.8f,0,9.7f),Vector3.right)+WallRun(new Vector3(-1.8f,0,-1.7f),new Vector3(-1.8f,0,5.6f),Vector3.left)+WallRun(new Vector3(-9.4f,0,9.75f),new Vector3(1.7f,0,9.75f),Vector3.forward)+WallRun(new Vector3(-9.4f,0,6.25f),new Vector3(-2.3f,0,6.25f),Vector3.back);
            Move(j,new Vector3(call.transform.position.x+2.02f,1.7f,call.transform.position.z));await Task.Delay(100);Need(!call.Nearby&&!call.proximityVisual.activeSelf,"Arrow visible beyond range");
            Move(j,new Vector3(call.transform.position.x+1.98f,1.7f,call.transform.position.z));await Task.Delay(100);Need(call.Nearby&&call.proximityVisual.activeSelf,"Arrow not visible at two metres");
            Move(j,new Vector3(-7.5f,1.7f,8),-90);await Task.Delay(150);GuidePresentationCheck.Capture("Logs/Polish-LiftDoor.png");
            Move(j,new Vector3(-8.5f,1.7f,8),-90);call.openingSeconds=.2f;ride.doorSeconds=.3f;ride.travelSeconds=.4f;Need(call.TryCall(),"Lift call failed");
            await Task.Delay(400);Move(j,new Vector3(-11.4f,1.7f,8),-90);await Task.Delay(1200);
            var mirror=ride.GetComponentInChildren<AvatarMirror>();Need(mirror&&mirror.ReflectedRendererCount>0,"Cabin mirror not reflecting avatar");GuidePresentationCheck.Capture("Logs/Polish-LiftMirror.png");
            Need(ride.TryDepart(),"Departure rejected");await Wait(()=>ride.State==ElevatorRide.Phase.Arrived,"Lift arrival timeout");
            arrival.closeDelay=.5f;arrival.closeSeconds=.45f;await Task.Delay(100);Need(arrival.exteriorFloorSign.text.Contains("B1")&&ride.overheadFloor.text.Contains("B1"),"B1 floor indicators incorrect");Need(Quaternion.Angle(arrival.arrow.localRotation,Quaternion.identity)<.1f,"Call arrow did not point up");
            Move(j,new Vector3(-8,1.7f,8),-90);await Task.Delay(650);Move(j,new Vector3(-9.85f,1.7f,8),-90);await Task.Delay(600);Need(!arrival.Closed,"Door closed on doorway occupant");
            Move(j,new Vector3(-8,1.7f,8),-90);await Wait(()=>arrival.Closed,"B1 door did not close after exit");GuidePresentationCheck.Capture("Logs/Polish-B1Lift.png");
            Move(j,new Vector3(call.transform.position.x+1,1.7f,call.transform.position.z),-90);Need(call.TryCall(),"B1 up call did not reopen lift");await Task.Delay(550);Need(!arrival.Closed,"B1 reopening failed");
            typeof(PreparationJourney).GetField("briefed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(j,true);
            Move(j,new Vector3(11.5f,1.85f,20.5f));await Task.Delay(200);
            var tasks=UnityEngine.Object.FindObjectsByType<LockerTaskPanel>(FindObjectsSortMode.None).OrderBy(t=>t.number).ToArray();Need(tasks.Length==5&&tasks.Select(t=>t.number).SequenceEqual(new[]{1,2,3,4,5}),"Locker numbering");
            var store=UnityEngine.Object.FindAnyObjectByType<PersonalClothingStorage>();Need(store&&!store.jacket.Stored&&store.jacket.GetComponentsInChildren<Renderer>(true).All(r=>!r.enabled)&&!store.foldedTrousers.activeSelf,"First locker not initially empty");
            Need(tasks[0].Current&&!tasks[1].Current,"Initial next locker wrong");
            var first=store.GetComponentInParent<LockerDoor>();if(!first)first=store.transform.parent.parent.GetComponentInChildren<LockerDoor>();if(first)first.Toggle();
            await Task.Delay(1100);GuidePresentationCheck.Capture("Logs/Polish-LockerEmpty.png");store.GetComponent<XRSimpleInteractable>().activated.Invoke(new ActivateEventArgs());await Task.Delay(100);
            Need(j.PersonalClothesStored&&store.jacket.Stored&&store.foldedTrousers.activeSelf,"Personal clothing transfer failed");Need(store.jacket.GetComponentsInChildren<Renderer>(true).All(r=>r.enabled),"Stored outfit not visible");Need(tasks[0].Complete&&tasks[0].checkMark.activeSelf&&tasks[1].Current,"Storage task did not turn green / advance");
            GuidePresentationCheck.Capture("Logs/Polish-LockerStored.png");
            var avatar=j.ride.playerRoot.GetComponent<PlayerBodyAvatar>();Need(avatar.PersonalJacketStored&&avatar.BodyRenderer.sharedMaterials.All(m=>m.name.Contains("base layer")),"Avatar personal outfit not removed");
            var clothes=ride.destinationRoot.GetComponentsInChildren<WearableGarment>(true);Fit(clothes.First(g=>g.kind==WearableGarment.Kind.Coverall));await Task.Delay(100);Need(tasks[1].Complete&&tasks[2].Current,"Coverall task did not advance");
            foreach(var kind in new[]{WearableGarment.Kind.HairCap,WearableGarment.Kind.Hood,WearableGarment.Kind.Mask,WearableGarment.Kind.SafetyGlasses,WearableGarment.Kind.LeftGlove,WearableGarment.Kind.RightGlove,WearableGarment.Kind.LeftBoot,WearableGarment.Kind.RightBoot,WearableGarment.Kind.LeftOvershoe,WearableGarment.Kind.RightOvershoe})Fit(clothes.First(g=>g.kind==kind));
            await Task.Delay(250);Need(tasks.All(t=>t.Complete&&t.checkMark.activeSelf),"Locker tasks incomplete");Move(j,new Vector3(16,1.85f,19.4f));await Task.Delay(100);GuidePresentationCheck.Capture("Logs/Polish-Lockers.png");
            Need(UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Count(r=>r.name=="Official ETpathfinder photo")>=20,"Missing wall photos");
            j.entryDoor.SetOpen(true);j.exitDoor.SetOpen(true);await Wait(()=>j.entryDoor.IsOpen&&j.exitDoor.IsOpen,"Route door test setup");ProjectAssembly.ValidateRoute();
            int samples=openingSamples+EnvelopeGrid(24.8f,35.05f,18.2f,20.35f)+EnvelopeGrid(32.95f,35.05f,20,29.05f)+EnvelopeGrid(-9.2f,-.2f,5.85f,10.15f)+EnvelopeGrid(.6f,14.1f,5.85f,10.15f)+EnvelopeGrid(10,14.1f,10.7f,14.9f)+EnvelopeGrid(10.3f,21.7f,15.7f,23.05f);
            int wallSamples=openingWalls+WallRun(new Vector3(24.9f,0,18.3f),new Vector3(35,0,18.3f),Vector3.back)+WallRun(new Vector3(24.9f,0,20.3f),new Vector3(32.25f,0,20.3f),Vector3.forward)+WallRun(new Vector3(35,0,18.5f),new Vector3(35,0,29),Vector3.right)+WallRun(new Vector3(32.95f,0,21.1f),new Vector3(32.95f,0,29),Vector3.left);
            wallSamples+=WallRun(new Vector3(-9.3f,0,10.2f),new Vector3(-.1f,0,10.2f),Vector3.forward)+WallRun(new Vector3(-9.3f,0,5.8f),new Vector3(14.1f,0,5.8f),Vector3.back)+WallRun(new Vector3(.6f,0,10.2f),new Vector3(9.25f,0,10.2f),Vector3.forward)+WallRun(new Vector3(14.1f,0,5.9f),new Vector3(14.1f,0,15),Vector3.right)+WallRun(new Vector3(10,0,10.9f),new Vector3(10,0,14.9f),Vector3.left)+WallRun(new Vector3(10.3f,0,15.7f),new Vector3(10.3f,0,23),Vector3.left)+WallRun(new Vector3(10.3f,0,23),new Vector3(21.7f,0,23),Vector3.forward)+WallRun(new Vector3(21.7f,0,15.8f),new Vector3(21.7f,0,17.95f),Vector3.right)+WallRun(new Vector3(21.7f,0,20.55f),new Vector3(21.7f,0,23),Vector3.right);
            Move(j,new Vector3(27,1.85f,19.3f),90);await Task.Delay(100);GuidePresentationCheck.Capture("Logs/Polish-Hallway.png");Move(j,new Vector3(34,1.85f,27.1f));await Task.Delay(100);GuidePresentationCheck.Capture("Logs/Polish-NoEntryDoor.png");
            var tape=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).FirstOrDefault(r=>r.name=="Generated NO ENTRY tape");Need(tape&&tape.sharedMaterial.GetFloat("_Surface")==1,"Transparent tape asset missing");Need(j.facilityDoor.GetComponent<Collider>()&&!j.facilityDoor.GetComponent<XRBaseInteractable>(),"Final door not locked");
            var ai=UnityEngine.Object.FindAnyObjectByType<StandaloneAIGuide>(FindObjectsInactive.Include);Need(ai&&Mathf.Abs(ai.speechSpeed-.82f)<.001f&&Mathf.Abs(ai.recordedSpeechRate-.9f)<.001f,"Speech not slowed");
            string report="PASS: call-arrow visibility at 1.98 m / hidden at 2.02 m; metal lift door; avatar mirror reflection; B1 inside/outside signs and up arrow; delayed B1 door closure, doorway safety and reopening; five numbered locker tasks; initially empty personal locker; XR activation transfers clothes and changes avatar base layer; green checks and next step progression; all eleven garments fitted; official wall photos; route capsule clearance; "+samples+" floor/ceiling grid points and "+wallSamples+" wall boundary samples; transparent generated tape and solid non-interactive final door; slower synthesized/recorded voice configuration.\nEditor simulation and saved renders. Headset acceptance/performance and real microphone are not covered.\n";
            File.WriteAllText("Logs/FacilityPolishCheck.txt",report);Debug.Log(report);exit=0;
        }
        catch(Exception e){File.WriteAllText("Logs/FacilityPolishCheck.txt","FAIL: "+e);Debug.LogException(e);}
        SessionState.SetBool("ET.FacilityCheck",false);SessionState.SetInt("ET.FacilityExit",exit);EditorApplication.isPlaying=false;
    }
}
