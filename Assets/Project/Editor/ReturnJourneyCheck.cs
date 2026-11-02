using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GroupProject.Opening;
using GroupProject.Elevator;
using GroupProject.Preparation;
using GroupProject.Guide;

[InitializeOnLoad]
public static class ReturnJourneyCheck
{
    static bool running;
    static ReturnJourneyCheck(){EditorApplication.update+=Tick;}
    public static void ConfigureAndRun(){FacilityPolishSetup.Run();Run();}
    public static void Run()
    {
        EditorSceneManager.OpenScene(ProjectAssembly.Main);
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        var opening=UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();opening.enabled=false;opening.locomotionGuide.enabled=false;
        foreach(var ai in UnityEngine.Object.FindObjectsByType<StandaloneAIGuide>(FindObjectsInactive.Include,FindObjectsSortMode.None))ai.gameObject.SetActive(false);
        SessionState.SetBool("ET.ReturnJourneyCheck",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        int exit=SessionState.GetInt("ET.ReturnJourneyExit",-1);if(exit>=0&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt("ET.ReturnJourneyExit",-1);EditorApplication.Exit(exit);return;}
        if(running||!SessionState.GetBool("ET.ReturnJourneyCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;running=true;Check();
    }
    static void Need(bool value,string why){if(!value)throw new Exception(why);}
    static async Task Wait(Func<bool> predicate,string why){double start=EditorApplication.timeSinceStartup;while(!predicate()){if(EditorApplication.timeSinceStartup-start>15)throw new Exception(why);await Task.Delay(40);}}
    static void Move(PreparationJourney j,Vector3 head,float yaw=0){var c=j.ride.playerRoot.GetComponent<CharacterController>();if(c)c.enabled=false;j.ride.playerRoot.rotation=Quaternion.Euler(0,yaw,0);j.ride.playerRoot.position+=head-j.head.position;Physics.SyncTransforms();}
    static void CardBounds(Transform card,Camera camera,float width,float height,float tab)
    {
        ChecklistPlacement.Place(card,camera.transform,width,height,tab);
        foreach(var p in new[]{new Vector3(0,tab),new Vector3(width,tab),new Vector3(0,-height),new Vector3(width,-height)}){var v=camera.WorldToViewportPoint(card.TransformPoint(p));Need(v.x>0&&v.x<1&&v.y>0&&v.y<1,"Card outside view "+v);}
    }
    static async void Check()
    {
        int exit=1;
        try
        {
            await Task.Delay(600);var j=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>();var ride=j.ride;j.guide=null;var call=ride.callButton;
            var ai=UnityEngine.Object.FindAnyObjectByType<StandaloneAIGuide>(FindObjectsInactive.Include);var parrot=UnityEngine.Object.FindAnyObjectByType<ParrotCompanion>();
            Need(j.Pace&&j.Pace.Canvas.enabled&&j.Pace.gameObject.activeInHierarchy,"Pace not visible from start");
            var station=parrot.transform.position;var rotation=parrot.transform.rotation;Move(j,new Vector3(0,1.7f,1),65);await Task.Delay(100);Need(Vector3.Distance(parrot.transform.position,station)<.001f&&Quaternion.Angle(rotation,parrot.transform.rotation)<.01f&&!parrot.transform.IsChildOf(ride.playerRoot),"Guide follows player/view");
            Move(j,new Vector3(.6f,1.7f,.3f));await Task.Delay(100);GuidePresentationCheck.Capture("Logs/Return-FixedGuide.png");
            var arrows=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.name=="Luminous direction arrow").ToArray();Need(arrows.Length<=20&&arrows.All(r=>r.transform.localScale.x<=.4f),"Arrows not sparse/narrow");
            var groundArrow=arrows.First(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.transform.IsChildOf(ride.departureRoot));
            var hiddenGround=ride.departureRoot.GetComponentsInChildren<Renderer>(true).First(r=>!r.enabled&&!r.transform.IsChildOf(ride.transform));
            Move(j,new Vector3(-8.5f,1.7f,8));call.openingSeconds=.12f;ride.doorSeconds=.15f;ride.travelSeconds=1;
            var arrival=ride.GetComponent<ElevatorArrivalControl>();arrival.closeDelay=.5f;arrival.closeSeconds=.15f;
            Need(call.TryCall(),"Ground lift call failed");await Task.Delay(250);Move(j,new Vector3(-11.4f,1.7f,8),90);Need(ride.TryDepart(),"B1 departure failed");await Wait(()=>ride.State==ElevatorRide.Phase.Arrived,"B1 arrival failed");
            Need(ride.AtB1&&ride.destinationRoot.activeSelf&&!groundArrow.enabled,"B1 geometry state");
            Need(Mathf.Abs(ride.display.transform.position.x-call.door.position.x)<.4f&&Mathf.Abs(ride.display.transform.position.z-call.door.position.z)<.1f&&ride.display.transform.position.y>2.5f,"Countdown not over doorway");
            Move(j,new Vector3(-7.4f,1.7f,8));await Task.Delay(100);var clipboard=UnityEngine.Object.FindAnyObjectByType<ChecklistClipboard>();
            Need(clipboard.transform.localPosition.x>-.65f,"Checklist still tucked against edge");
            var toggle=clipboard.GetComponentInChildren<ChecklistToggleButton>(true);toggle.GetComponent<XRSimpleInteractable>().activated.Invoke(new ActivateEventArgs());Need(!clipboard.Visible,"Manual checklist hide failed");Need(j.Pace.gameObject.activeInHierarchy&&j.Pace.Canvas.enabled,"Hiding checklist hid pace");
            Need(!clipboard.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t=>t.text.Contains("PACE")),"Pace still inside checklist");
            Move(j,new Vector3(11.5f,1.85f,20.5f));await Task.Delay(100);Need(j.InDressingRoom&&ai.SpeechSuppressed,"Dressing quiet zone absent");Need(!await ai.SpeakInstruction("This must remain silent."),"Dressing instruction accepted");
            var silence=AudioClip.Create("Quiet zone check",16000,1,16000,false);ride.announcements.clip=silence;ride.announcements.Play();parrot.voice.clip=silence;parrot.voice.Play();
            j.Tell("This clothing prompt must not queue.");await Task.Delay(150);Need(!parrot.voice.isPlaying&&!ride.announcements.isPlaying,"Existing speech continued in dressing room");
            var queue=(Queue<string>)typeof(PreparationJourney).GetField("speech",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(j);Need(queue.Count==0,"Dressing speech queued");UnityEngine.Object.Destroy(silence);
            var outfit=j.GetComponentInChildren<DressingChecklist>(true);var outfitToggle=outfit.GetComponentInChildren<ChecklistToggleButton>(true);outfitToggle.GetComponent<XRSimpleInteractable>().activated.Invoke(new ActivateEventArgs());Need(!outfit.Visible,"Manual outfit hide failed");await Task.Delay(100);Need(!outfit.Visible&&j.Pace.Canvas.enabled,"Hidden outfit reopened / pace vanished");
            var store=UnityEngine.Object.FindAnyObjectByType<PersonalClothingStorage>();Need(store.Store(),"Storage setup failed");
            var coverall=ride.destinationRoot.GetComponentsInChildren<WearableGarment>(true).First(g=>g.kind==WearableGarment.Kind.Coverall);coverall.SetPreview(true);coverall.transform.position=coverall.TargetPosition();Need(coverall.TryPlace(),"Coverall fit failed");coverall.SetPreview(false);var originalParent=coverall.transform.parent;
            for(int kind=1;kind<=11;kind++)j.Equip(kind);await Wait(()=>j.entryDoor.IsOpen,"Dressed entry did not open");
            Move(j,new Vector3(23,1.85f,19.3f));Need(j.Select(5),"Shower close failed");await Wait(()=>j.entryDoor.IsClosed,"Shower entry did not close");j.showerSeconds=.3f;Need(j.Select(4),"Shower start failed");await Wait(()=>j.ShowerComplete&&j.exitDoor.IsOpen,"Shower completion failed");
            Need(j.Select(5),"Completed shower entry cannot reopen");await Wait(()=>j.entryDoor.IsOpen,"Completed shower entry stayed closed");Need(j.exitDoor.Open&&j.ShowerComplete,"Returning reset completion or closed exit");
            Move(j,new Vector3(21,1.85f,19.3f),-90);await Task.Delay(120);Need(j.InDressingRoom&&ai.SpeechSuppressed,"Cannot return to quiet dressing room");Need(!outfit.Visible&&j.Pace.Canvas.enabled,"Return lost UI state");
            Move(j,new Vector3(-8.5f,1.7f,8),-90);Need(call.TryCall(),"B1 recall failed");await Task.Delay(250);Move(j,new Vector3(-11.4f,1.7f,8),90);
            Need(ride.TryDepart()&&ride.GoingUp,"G departure rejected");Move(j,new Vector3(-9.85f,1.7f,8),90);await Task.Delay(450);Need(ride.State==ElevatorRide.Phase.Arrived,"Upward doorway safety failed");
            Move(j,new Vector3(-11.4f,1.7f,8),90);Need(ride.TryDepart(),"G retry failed");Need(!ride.TryDepart(),"Duplicate upward trip accepted");await Wait(()=>ride.State==ElevatorRide.Phase.Ascending,"Ascent not started");await Task.Delay(160);
            Need(ride.DescentProgress<1&&ride.DescentProgress>0&&ride.display.text.Contains("GOING UP")&&ride.display.text.Contains("s"),"Up countdown/depth incorrect");
            j.head.LookAt(ride.display.transform.position);GuidePresentationCheck.Capture("Logs/Return-OverDoorCountdown.png");
            await Wait(()=>ride.State==ElevatorRide.Phase.GroundArrived,"Ground arrival failed");Need(!ride.AtB1&&!ride.destinationRoot.activeSelf&&groundArrow.enabled&&!hiddenGround.enabled,"Ground geometry not restored exactly");
            Need(j.Dressed&&j.ShowerComplete&&j.PersonalClothesStored&&coverall.Equipped&&coverall.gameObject.activeInHierarchy&&coverall.transform.IsChildOf(ride.playerRoot),"Return lost outfit/progress");Need(j.Pace.Canvas.enabled&&j.Pace.gameObject.activeInHierarchy,"Ground return lost pace HUD");
            await Task.Delay(100);Need(Vector3.Distance(parrot.transform.position,parrot.GetComponent<ParrotStations>().Choose().position)<.001f,"Guide not at ground station");
            Need(ride.TryDepart()&&!ride.GoingUp,"Repeat B1 departure failed");await Wait(()=>ride.State==ElevatorRide.Phase.Arrived,"Repeat B1 arrival failed");Need(coverall.transform.parent==originalParent&&j.ShowerComplete&&j.Dressed,"Repeat descent lost garment parent/progress");
            Move(j,new Vector3(26,1.85f,19.3f),90);await Task.Delay(350);clipboard.SetVisible(true);await Task.Delay(50);GuidePresentationCheck.Capture("Logs/Return-SparseArrowsAndPace.png");
            var camera=j.head.GetComponent<Camera>();float aspect=camera.aspect,fov=camera.fieldOfView;foreach(float ratio in new[]{.65f,1f,4f/3f,16f/9f})foreach(float angle in new[]{45f,60f,90f}){camera.aspect=ratio;camera.fieldOfView=angle;camera.ResetProjectionMatrix();CardBounds(clipboard.transform,camera,460,420,40);CardBounds(outfit.transform,camera,400,600,34);}camera.aspect=aspect;camera.fieldOfView=fov;camera.ResetProjectionMatrix();
            File.WriteAllText("Logs/ReturnJourneyCheck.txt","PASS: G→B1→G→B1 round trip, upward countdown/depth above door, duplicate input and upward doorway recovery, exact ground render/collider restoration, worn outfit and completed shower retained, completed shower entry reopening/backtracking, fixed guide stations independent of rig/head motion, narrow sparse arrows, inward checklist bounds across aspect/FOV combinations, VR manual hide/show, hidden-state persistence, separate always-visible numeric pace HUD, suppressed/cancelled speech and cleared prompts in the dressing room.\nEditor simulation and renders; physical Quest comfort/performance and microphone not covered.\n");exit=0;
        }
        catch(Exception e){File.WriteAllText("Logs/ReturnJourneyCheck.txt","FAIL: "+e);Debug.LogException(e);}
        SessionState.SetBool("ET.ReturnJourneyCheck",false);SessionState.SetInt("ET.ReturnJourneyExit",exit);EditorApplication.isPlaying=false;
    }
}
