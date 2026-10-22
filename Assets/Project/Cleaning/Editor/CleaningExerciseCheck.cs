using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GroupProject.Cleaning;
using GroupProject.Guide;
using GroupProject.Opening;

[InitializeOnLoad]
public static class CleaningExerciseCheck
{
    static int stage, strokeSample;
    static bool enableOfflineQA;
    [MenuItem("Tools/Cleaning/Preview With Offline Q&A")]
    public static void PreviewOfflineQA(){enableOfflineQA=true;try{Preview();}finally{enableOfflineQA=false;}}static double since;
    static CleaningStation station;
    static CleaningExerciseCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=RestorePreview;}
    static void RestorePreview(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("CleaningRestore",false))return;
        SessionState.SetBool("CleaningRestore",false);
        EditorSceneManager.OpenScene(OpeningProjectSetup.ScenePath);
    }
    static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Stroke(CleaningExercise exercise,bool lift=true)
    {float lane=exercise.Lane;exercise.Sample(true,new Vector2(-.21f,lane));exercise.Sample(true,new Vector2(-.08f,lane));exercise.Sample(true,new Vector2(.08f,lane));exercise.Sample(true,new Vector2(.21f,lane));if(lift)exercise.Sample(false,new Vector2(.21f,lane));}
    static void Rules()
    {
        var e=new CleaningExercise();Assert(!e.Select(CleaningExercise.WipeKind.AnticonGold),"Inspection can be bypassed");
        e.Inspected();Assert(!e.Select(CleaningExercise.WipeKind.KimtechW4),"Class A accepted KIMTECH");Assert(e.Select(CleaningExercise.WipeKind.AnticonGold),"Correct wipe rejected");
        e.Sample(true,new Vector2(-.21f,e.Lane));e.Sample(false,new Vector2(-.21f,e.Lane));Assert(e.Passes==0,"Stationary wipe counted");
        e.Sample(true,new Vector2(-.21f,e.Lane));e.Sample(true,new Vector2(.1f,e.Lane));e.Sample(true,new Vector2(-.1f,e.Lane));e.Sample(false,new Vector2(.21f,e.Lane));Assert(e.Passes==0,"Scrubbing counted");
        Stroke(e,false);Assert(e.Passes==0,"Pass counted without lifting");e.Sample(false,new Vector2(.21f,e.Lane));Assert(e.Passes==1,"Valid pass rejected");
        Stroke(e);Assert(e.Passes==1,"Used face accepted");e.FreshFace();
        for(int i=1;i<4;i++){Stroke(e);if(i<3)e.FreshFace();}
        Assert(e.State==CleaningExercise.Stage.Review,"Review not reached");e.Continue();Assert(e.Trial==1 && e.State==CleaningExercise.Stage.Inspect,"Second trial missing");
        e.Inspected();Assert(!e.Select(CleaningExercise.WipeKind.AnticonGold),"Class B recommendation not enforced");e.Select(CleaningExercise.WipeKind.KimtechW4);
        for(int i=0;i<4;i++){Stroke(e);if(i<3)e.FreshFace();}e.Continue();Assert(e.State==CleaningExercise.Stage.Complete,"Exercise cannot complete");
    }
    [MenuItem("Tools/Cleaning/Preview With Shared Player")]
    public static void Preview()
    {
        if(EditorApplication.isPlaying)return;
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        EditorSceneManager.OpenScene(OpeningProjectSetup.ScenePath);
        // This preview isolates the cleaning exercise; preparation gates have their own PreparationCheck.
        var preparation=UnityEngine.Object.FindAnyObjectByType<GroupProject.Preparation.PreparationJourney>();if(preparation){preparation.enabled=false;if(preparation.showerGate)preparation.showerGate.SetActive(false);if(preparation.cleanGate)preparation.cleanGate.SetActive(false);}
        var lower=Array.Find(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(),g=>g.name=="02_LowerFloor");lower.SetActive(true);
        var ai=UnityEngine.Object.FindAnyObjectByType<StandaloneAIGuide>();ai.gameObject.SetActive(enableOfflineQA);
        var opening=UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();opening.enabled=false;opening.locomotionGuide.enabled=false;opening.locomotionGuide.panel.SetActive(false);
        var ride=UnityEngine.Object.FindAnyObjectByType<GroupProject.Elevator.ElevatorRide>();ride.enabled=false;
        foreach(var r in ride.departureRoot.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(ride.transform))r.enabled=false;
        foreach(var c in ride.departureRoot.GetComponentsInChildren<Collider>(true))if(!c.transform.IsChildOf(ride.transform))c.enabled=false;
        foreach(var l in ride.departureRoot.GetComponentsInChildren<Light>(true))if(!l.transform.IsChildOf(ride.transform))l.enabled=false;
        var rig=GameObject.Find("Shared_XR_Player");rig.transform.SetPositionAndRotation(new Vector3(28,.25f,17.9f),Quaternion.identity);
        // Preview changes exist only in this editor session; don't save over the opening scene.
        SessionState.SetBool("CleaningRestore",true);SessionState.SetBool("CleaningPreview",true);EditorApplication.isPlaying=true;
    }
    [MenuItem("Tools/Cleaning/Run Exercise Checks")]
    public static void Run(){Rules();SessionState.SetBool("CleaningCheck",true);Preview();}
    static void Finish(string result){Directory.CreateDirectory("Logs");File.WriteAllText("Logs/CleaningExerciseCheck.txt",result);SessionState.SetBool("CleaningCheck",false);EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(result.StartsWith("PASS")?0:1);}
    static void WalkRoute()
    {
        var rig=GameObject.Find("Shared_XR_Player");var body=rig.GetComponent<CharacterController>();var saved=rig.transform.position;
        var points=ProjectAssembly.RoutePoints;
        body.enabled=false;rig.transform.position=points[0];body.enabled=true;
        foreach(var target in points.Skip(1).Concat(points.Reverse().Skip(1))) {
            for(int step=0;step<300;step++){
                var delta=target-rig.transform.position;delta.y=0;if(delta.magnitude<.04f)break;
                body.Move(Vector3.ClampMagnitude(delta,.04f)+Vector3.down*.015f);
            }
            var error=target-rig.transform.position;error.y=0;Assert(error.magnitude<.08f,"Character controller blocked on walking route at "+rig.transform.position+" toward "+target);
        }
        body.enabled=false;rig.transform.position=saved;body.enabled=true;
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        if(SessionState.GetBool("CleaningPreview",false))
        {
            var camera=Camera.main;if(!camera)return;camera.fieldOfView=75;camera.transform.localRotation=Quaternion.Euler(15,0,0);
            SessionState.SetBool("CleaningPreview",false);
        }
        if(!SessionState.GetBool("CleaningCheck",false))return;
        try
        {
            if(stage==0)
            {
                station=UnityEngine.Object.FindAnyObjectByType<CleaningStation>();Assert(station,"Station missing");
                Assert(station.speech.Length>=16 && station.speech.All(s=>s.clip && s.clip.length>1),"Narration missing");
                Assert(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled)==1,"Duplicate player camera");
                Assert(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c=>c.isActiveAndEnabled)==1,"Duplicate audio listener");
                Assert(station.tools.All(t=>t.GetComponent<XRGrabInteractable>() && t.tip),"Grab setup missing");
                foreach(var sceneRoot in station.gameObject.scene.GetRootGameObjects())foreach(var t in sceneRoot.GetComponentsInChildren<Transform>())Assert(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
                since=EditorApplication.timeSinceStartup;stage=1;
            }
            else if(stage==1 && EditorApplication.timeSinceStartup-since>1)
            {
                Assert(station.InReach,"Preview spawn not near bench");
                var lamp=station.tools[0];lamp.SetDesktop(true);lamp.transform.position=station.plate.position+Vector3.up*.4f;lamp.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);lamp.GetComponent<XRGrabInteractable>().activated.Invoke(new ActivateEventArgs());
                since=EditorApplication.timeSinceStartup;stage=2;
            }
            else if(stage==2 && station.Exercise.State==CleaningExercise.Stage.ChooseWipe && EditorApplication.timeSinceStartup-since>1.6)
            {
                Assert(station.Exercise.State==CleaningExercise.Stage.ChooseWipe,"Lamp activation/inspection failed");station.tools[0].SetDesktop(false);station.tools[0].ReturnHome();
                var wipe=station.tools[1];wipe.SetDesktop(true);Assert(station.Exercise.State==CleaningExercise.Stage.Wipe,"Grab did not select wipe");
                var p=UnityEngine.Object.FindAnyObjectByType<ParrotCompanion>();Assert(p.narrators.Contains(station.narrator),"Chapter narration not registered");
                File.WriteAllText("Logs/CleaningPose.txt","Camera="+Camera.main.transform.position+" rig="+GameObject.Find("Shared_XR_Player").transform.position+" plate="+station.plate.position);

                since=EditorApplication.timeSinceStartup;stage=3;
            }
            else if(stage==3 && EditorApplication.timeSinceStartup-since>.12)
            {
                if(strokeSample==0)GuidePresentationCheck.Capture("Logs/CleaningStation.png");
                int pass=strokeSample/6, phase=strokeSample%6;var wipe=station.tools[1];
                if(phase<5){float x=phase==0?-.21f:phase==1?-.07f:phase==2?.08f:.21f;var target=station.plate.TransformPoint(new Vector3(x,phase==4?.1f:0,-.12f+pass*.08f));wipe.transform.rotation=station.plate.rotation;wipe.transform.position+=target-wipe.tip.position;}
                else{Assert(station.Exercise.Passes==pass+1,"Physical wiping did not advance pass "+pass);if(pass<3)wipe.GetComponent<XRGrabInteractable>().activated.Invoke(new ActivateEventArgs());}
                strokeSample++;since=EditorApplication.timeSinceStartup;
                if(strokeSample==24){wipe.SetDesktop(false);wipe.ReturnHome();stage=4;}
            }
            else if(stage==4)
            {
                Assert(station.Exercise.State==CleaningExercise.Stage.Review,"Physical trial did not reach review");
                var button=UnityEngine.Object.FindAnyObjectByType<CleaningContinueButton>();button.GetComponent<XRSimpleInteractable>().activated.Invoke(new ActivateEventArgs());
                Assert(station.Exercise.Trial==1,"Quest continue event did not start Class B");
                CleaningRouteSetup.Validate();
                var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
                camera.transform.SetPositionAndRotation(new Vector3(10,1.7f,8),Quaternion.LookRotation(Vector3.right));
                GuidePresentationCheck.Capture("Logs/CleaningRoute.png");
                camera.transform.SetPositionAndRotation(new Vector3(12,1.9f,18),Quaternion.LookRotation(Vector3.right));
                GuidePresentationCheck.Capture("Logs/ChangingRoom.png");
                camera.transform.SetPositionAndRotation(new Vector3(22.4f,1.9f,19.3f),Quaternion.LookRotation(Vector3.right));
                GuidePresentationCheck.Capture("Logs/ShowerExit.png");
                camera.transform.SetPositionAndRotation(position,rotation);
                WalkRoute();
                Assert(station.Answer("Which wipe for Class A?").Contains("Anticon Gold"),"Authored cleaning answer missing");
                Assert(station.Answer("What oven temperature?").Contains("approved local procedure"),"Unsupported procedure answer");
                Finish("PASS: both trials; reject wrong wipe, stationary wipe, scrubbing, missing lift and reused face; correct four-pass progression; one camera/listener; no missing scripts; lamp activation and timed aiming; wipe selection; four physical passes with lift and trigger face changes; Quest continue event; parrot narrator registration; continuous walking route; grounded cleaning answers. Editor simulation only; Quest hardware and standalone build remain untested.");
            }
            if(stage>0 && EditorApplication.timeSinceStartup-since>30)throw new Exception("Timeout stage "+stage);
        }
        catch(Exception e){Debug.LogException(e);Finish("FAIL: "+e);}
    }
}
