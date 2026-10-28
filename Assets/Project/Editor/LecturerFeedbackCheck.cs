using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GroupProject.Opening;
using GroupProject.Elevator;
using GroupProject.Preparation;
using GroupProject.Guide;
[InitializeOnLoad] public static class LecturerFeedbackCheck
{
    const string Key="LecturerFeedbackCheck";static bool running;
    static LecturerFeedbackCheck(){EditorApplication.update+=Tick;}
    public static void Run(){EditorSceneManager.OpenScene(ProjectAssembly.Main);foreach(var a in UnityEngine.Object.FindObjectsByType<StandaloneAIGuide>(FindObjectsInactive.Include))a.gameObject.SetActive(false);foreach(var a in UnityEngine.Object.FindObjectsByType<OpeningSequence>())a.enabled=false;foreach(var a in UnityEngine.Object.FindObjectsByType<LocomotionGuide>())a.enabled=false;SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
    static async void Tick(){if(running||!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.5f)return;running=true;try{await Test();File.WriteAllText("Logs/LecturerFeedbackCheck.txt","PASS: saved thresholds/boundaries; 5cm parrot shift; 12 clothing hover cues; five handle cues; valid B1 press animation/latch; duplicate rejection; overhead descent/arrival; two avatar mirrors with reflected pose; larger shower control; future barrier; retained route and one camera/listener. Images captured. Scripted editor evidence, not headset comfort/performance acceptance.");SessionState.SetBool(Key,false);EditorApplication.Exit(0);}catch(Exception e){File.WriteAllText("Logs/LecturerFeedbackCheck.txt","FAIL: "+e);Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.Exit(1);}}
    static void Require(bool c,string m){if(!c)throw new Exception(m);}
    static async Task Wait(float sec){float end=Time.unscaledTime+sec;while(Time.unscaledTime<end)await Task.Yield();}
    static PreparationJourney j;
    static void Place(Vector3 feet,float yaw){var p=j.ride.playerRoot;p.SetPositionAndRotation(feet,Quaternion.identity);j.head.SetPositionAndRotation(feet+Vector3.up*1.65f,Quaternion.Euler(0,yaw,0));var h=p.GetComponent<AnimatedControllerHands>();h.leftController.SetPositionAndRotation(j.head.TransformPoint(new Vector3(-.25f,-.5f,.3f)),j.head.rotation);h.rightController.SetPositionAndRotation(j.head.TransformPoint(new Vector3(.25f,-.5f,.3f)),j.head.rotation);Physics.SyncTransforms();}
    static async Task Test(){
        Directory.CreateDirectory("Logs");j=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>();j.guide=null;var ride=j.ride;ride.callButton.aiGuide=null;
        foreach(var b in ride.playerRoot.GetComponentsInChildren<Behaviour>(true)){var n=b.GetType().Name;if(n=="TrackedPoseDriver"||n=="XRInputModalityManager"||n=="CharacterControllerDriver"||n=="XROrigin"||n=="XRBodyTransformer"||n.Contains("MoveProvider")||n.Contains("TurnProvider")||n=="DesktopPreview")b.enabled=false;}
        ride.playerRoot.GetComponent<CharacterController>().enabled=false;
        Require(j.cautionSpeed==1.3f&&j.excessiveSpeed==2,"Thresholds not doubled in saved scene");Require(j.PaceBand(1.3f)==0&&j.PaceBand(1.31f)==1&&!j.AboveWarningThreshold(2)&&j.AboveWarningThreshold(2.01f),"Boundary warning regression");
        var parrot=UnityEngine.Object.FindAnyObjectByType<ParrotCompanion>();Require(Mathf.Abs(parrot.offset.x+.65f)<.001f,"Parrot shift missing");
        Require(TrainingGuidance.Elevator=="Find the UI button and click on it to go down.","Required elevator instruction changed");
        ride.callButton.openingSeconds=.1f;ride.doorSeconds=.2f;ride.travelSeconds=2;
        Place(new Vector3(-8.5f,.03f,8),270);Require(ride.callButton.TryCall(),"Call failed");await Wait(.4f);Place(new Vector3(-11.4f,.03f,8),270);await Wait(.3f);
        var floor=UnityEngine.Object.FindAnyObjectByType<ElevatorFloorButton>();var cue=floor.GetComponent<InteractionFeedback>();var rest=cue.pressVisual.localPosition;
        Require(floor.TryPress()&&!floor.TryPress(),"B1/duplicate guard failed");await Wait(.12f);Require(cue.AcceptedCount==1&&Vector3.Distance(rest,cue.pressVisual.localPosition)>.001f,"No visible B1 press feedback");
        await Wait(.4f);Require(ride.State==ElevatorRide.Phase.Descending&&ride.overheadFloor.text.Contains("B1")&&ride.DescentProgress>0,"Overhead descent missing");
        GuidePresentationCheck.Capture("Logs/ElevatorFeedback.png");float deadline=Time.realtimeSinceStartup+15;while(ride.State!=ElevatorRide.Phase.Arrived&&Time.realtimeSinceStartup<deadline)await Task.Yield();await Wait(.1f);Require(ride.State==ElevatorRide.Phase.Arrived&&ride.overheadFloor.text.Contains("-300"),"Arrival indicator missing");
        Require(cue.Latched&&!floor.TryPress(),"Arrival latch failed");
        var mirrors=UnityEngine.Object.FindObjectsByType<AvatarMirror>();Require(mirrors.Length==2,"Expected two saved mirrors");
        var elevator=mirrors.Single(m=>m.name.Contains("Elevator"));Require(elevator.ReflectedRendererCount>=3,"Elevator mirror has no avatar");
        var garments=ride.destinationRoot.GetComponentsInChildren<WearableGarment>();Require(garments.Length==12&&garments.All(g=>g.GetComponent<InteractionFeedback>()),"Clothing cues missing");
        var doors=ride.destinationRoot.GetComponentsInChildren<LockerDoor>();Require(doors.Length==5&&doors.All(d=>d.GetComponent<InteractionFeedback>().surfaces.Length>0),"Handle cues missing");
        Place(new Vector3(16,.22f,20.3f),0);await Wait(.4f);GuidePresentationCheck.Capture("Logs/ChangingFeedback.png");
        foreach(var door in doors)door.Toggle();await Wait(1.2f);
        foreach(var garment in garments.OrderBy(g=>(int)g.kind)){garment.transform.position=garment.TargetPosition();Require(garment.TryPlace(),"Cannot equip "+garment.kind);await Wait(.05f);}
        Place(new Vector3(20.7f,.22f,16.6f),90);j.head.rotation=Quaternion.Euler(22,90,0);await Wait(1.2f);var changing=mirrors.Single(m=>m.name.Contains("Changing"));Require(changing.ReflectedRendererCount>=3,"Changing mirror empty");GuidePresentationCheck.Capture("Logs/ChangingMirror.png");
        File.WriteAllText("Logs/MirrorTiming.txt","Editor last bake ms: elevator="+elevator.LastBakeMilliseconds+", changing="+changing.LastBakeMilliseconds+". Not a Quest GPU/frame-time measurement.");
        Place(new Vector3(23.2f,.22f,19.25f),0);await Wait(.5f);var start=ride.destinationRoot.GetComponentsInChildren<PreparationButton>().Single(b=>b.id==4);j.showerSeconds=1;
        Require(start.transform.localScale.x>=.28f&&start.Press(),"Shower control failed");await Wait(.4f);Require(start.GetComponent<InteractionFeedback>().Latched,"Shower running cue missing");GuidePresentationCheck.Capture("Logs/ShowerFeedback.png");await Wait(1);Require(j.ShowerComplete,"Shower completion failed");
        var barrier=GameObject.Find("Pathfinder future barrier");Require(barrier&&barrier.GetComponent<BoxCollider>()&&!barrier.GetComponent<BoxCollider>().isTrigger,"Future area not blocked");ProjectAssembly.ValidateRoute();
        Require(UnityEngine.Object.FindObjectsByType<Camera>().Length==1&&UnityEngine.Object.FindObjectsByType<AudioListener>().Length==1,"Extra player camera/listener introduced");
    }
    public static void BuildMac()
    {
        var target=UnityEditor.Build.NamedBuildTarget.Standalone;
        var original=PlayerSettings.GetScriptingBackend(target);
        var playerSettings=File.ReadAllBytes("ProjectSettings/ProjectSettings.asset");
        var graphicsSettings=File.ReadAllBytes("ProjectSettings/GraphicsSettings.asset");
        try
        {
            // Only the Mono Mac module is installed here. Restore the project's backend after this smoke build.
            PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.Mono2x);
            Directory.CreateDirectory("Builds");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ProjectAssembly.Main},locationPathName="Builds/ET-Training.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/MacBuild.txt",report.summary.result+" (Mac Mono development smoke build); errors="+report.summary.totalErrors+"; bytes="+report.summary.totalSize+"; time="+report.summary.totalTime);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Mac build failed: "+report.summary.result);
        }
        finally {PlayerSettings.SetScriptingBackend(target,original);AssetDatabase.SaveAssets();File.WriteAllBytes("ProjectSettings/ProjectSettings.asset",playerSettings);File.WriteAllBytes("ProjectSettings/GraphicsSettings.asset",graphicsSettings);}
    }
}
