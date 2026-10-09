using System;
using System.IO;
using System.Reflection;
using GroupProject.Guide;
using GroupProject.Opening;
using GroupProject.Elevator;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
[InitializeOnLoad] public static class GuidePresentationCheck
{
    static int stage,footCount;static double since;static ParrotCompanion parrot;static FootstepAudio footsteps;static ElevatorSoundEffects effects;static ElevatorRide ride;static OpeningSequence opening;static AudioSource simulatedAI;
    static GuidePresentationCheck(){EditorApplication.update+=Tick;}
    public static void ConfigureAndRun(){GuideSceneSetup.Configure();Run();}
    [MenuItem("Tools/Guide/Run Presentation Check")]
    public static void Run()
    {
        if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Stop Play mode and save the scene first.");
        EditorSceneManager.OpenScene(OpeningProjectSetup.ScenePath);
        var ai=UnityEngine.Object.FindAnyObjectByType<StandaloneAIGuide>();
        var companion=UnityEngine.Object.FindAnyObjectByType<ParrotCompanion>();
        var source=new GameObject("Simulated AI speech source").AddComponent<AudioSource>();source.playOnAwake=false;
        for(int i=0;i<companion.narrators.Length;i++)if(companion.narrators[i]==ai.speaker)companion.narrators[i]=source;
        ai.speaker=source;ai.opening.aiGuide=null;ai.tutorial.aiGuide=null;ai.elevator.aiGuide=null;ai.gameObject.SetActive(false);
        SessionState.SetBool("GuidePresentationCheck",true);EditorApplication.isPlaying=true;
    }
    static void Assert(bool condition,string reason){if(!condition)throw new Exception(reason);}
    static void Next(){stage++;since=EditorApplication.timeSinceStartup;}
    static void Place(Vector3 position){var body=ride.playerRoot.GetComponent<CharacterController>();body.enabled=false;ride.playerRoot.position=position;body.enabled=true;Physics.SyncTransforms();}
    static void Finish(string result){File.WriteAllText("Logs/GuidePresentationCheck.txt",result);SessionState.SetBool("GuidePresentationCheck",false);EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(result.StartsWith("PASS")?0:1);}
    static void Tick()
    {
        if(!SessionState.GetBool("GuidePresentationCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try
        {
            double elapsed=EditorApplication.timeSinceStartup-since;
            if(stage==0)
            {
                parrot=UnityEngine.Object.FindAnyObjectByType<ParrotCompanion>();footsteps=UnityEngine.Object.FindAnyObjectByType<FootstepAudio>();effects=UnityEngine.Object.FindAnyObjectByType<ElevatorSoundEffects>();ride=UnityEngine.Object.FindAnyObjectByType<ElevatorRide>();opening=UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();
                Assert(parrot&&footsteps&&effects,"Missing guide or sound components");
                opening.StopAllCoroutines();opening.enabled=false;var fade=GameObject.Find("Opening fade (runtime)");if(fade)UnityEngine.Object.Destroy(fade);
                foreach(var source in parrot.narrators)source.Stop();
                Assert(LocomotionGuide.CaptionForStep(0)==LocomotionGuide.Captions[0],"Keyboard substituted for Quest instruction");
                string answer=(string)typeof(StandaloneAIGuide).GetMethod("GroundedReply",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(parrot.ai,new object[]{"How do I move?","MOVE"});
                Assert(answer.Contains("left joystick")&&!answer.Contains("Press W"),"AI guidance not Quest focused");
                Place(new Vector3(0,.03f,2));opening.locomotionGuide.panel.SetActive(true);opening.locomotionGuide.ShowStep(0);
                opening.locomotionGuide.narrator.clip=opening.locomotionGuide.steps[0];opening.locomotionGuide.narrator.Play();Next();
            }
            else if(stage==1&&elapsed>.5)
            {
                Assert(parrot.Speaking&&parrot.voice.clip==opening.locomotionGuide.steps[0],"Fallback speech not routed through parrot");
                Assert(parrot.CurrentDialogue.Contains("left stick"),"Dialogue cloud not synchronized");
                foreach(var source in parrot.narrators)Assert(source.volume==0,"Duplicate audible narrator");
                Assert(Vector3.Distance(parrot.voice.transform.position,parrot.transform.position)<.05f,"Voice is not at the parrot");
                Assert(parrot.bird.canvasRenderer,"Missing parrot CanvasRenderer");var mesh=parrot.bird.canvasRenderer.GetMesh();Assert(mesh && mesh.vertexCount>1000,"Parrot mesh not generated");
                Assert(parrot.GetComponentInChildren<DialogueCloud>().canvasRenderer,"Missing cloud CanvasRenderer");
                Capture("Logs/ParrotGuide.png");Next();
            }
            else if(stage==2)
            {
                if(footsteps.StepsPlayed<1){ride.playerRoot.GetComponent<DesktopPreview>().Move(Vector2.up,Mathf.Clamp(Time.deltaTime,.03f,.15f));}
                else{footCount=footsteps.StepsPlayed;Next();}
            }
            else if(stage==3&&elapsed>.7)
            {
                Assert(footsteps.StepsPlayed==footCount,"Footsteps continue at rest");
                opening.locomotionGuide.narrator.Stop();opening.locomotionGuide.panel.SetActive(false);
                simulatedAI=parrot.ai.speaker;typeof(StandaloneAIGuide).GetProperty("LastReply").SetValue(parrot.ai,"Use the left joystick to move forward.");
                simulatedAI.clip=opening.locomotionGuide.steps[0];simulatedAI.Play();Next();
            }
            else if(stage==4&&elapsed>.3)
            {
                Assert(parrot.Speaking&&parrot.CurrentDialogue=="Use the left joystick to move forward.","AI reply not routed to cloud and parrot");simulatedAI.Stop();
                Place(new Vector3(-8.5f,.03f,8));ride.callButton.openingSeconds=.3f;ride.doorSeconds=.4f;ride.travelSeconds=2;Assert(ride.callButton.TryCall(),"Call failed");Next();
            }
            else if(stage==5&&elapsed>.8)
            {
                Assert(effects.CuesPlayed>=2,"Call beep or door latch missing");Assert(!effects.doorLoop.isPlaying,"Door loop persists after opening");
                Place(new Vector3(-11.4f,.03f,8));var button=UnityEngine.Object.FindAnyObjectByType<ElevatorFloorButton>();button.GetComponent<XRSimpleInteractable>().activated.Invoke(new ActivateEventArgs());Next();
            }
            else if(stage==6&&ride.State==ElevatorRide.Phase.Descending)
            {
                Assert(ride.State==ElevatorRide.Phase.Descending&&ride.motor.isPlaying,"Descent motor missing");Assert(parrot.Speaking,"Descent announcement not routed through parrot");Next();
            }
            else if(stage==7&&ride.State==ElevatorRide.Phase.Arrived)
            {
                Assert(ride.State==ElevatorRide.Phase.Arrived,"Arrival failed");Assert(!ride.motor.isPlaying&&!effects.doorLoop.isPlaying,"Elevator loops still playing after arrival");Assert(effects.CuesPlayed>=7,"Missing elevator event sounds");
                Assert(parrot.gameObject.activeInHierarchy&&parrot.bird.enabled,"Parrot lost on arrival");
                Assert(Vector3.Distance(parrot.transform.position,parrot.head.TransformPoint(parrot.offset))<.03f,"Companion is not following head");
                Finish("PASS: Quest captions and AI control advice in keyboard preview; fallback and AI voice mirrored at parrot; dialogue synchronized; original narrators muted; footsteps follow travel and stop at rest; call/door/button/start/stop/chime cues; descent motor; loops stop at arrival; companion survives scene change. Scripted editor test; Quest hardware and subjective listening still pending.");
            }
            if(stage>0&&EditorApplication.timeSinceStartup-since>90)throw new Exception("Timeout at stage "+stage);
        }
        catch(Exception error){Finish("FAIL: "+error);Debug.LogException(error);}
    }
    public static void Capture(string path)
    {
        var source=Camera.main;var obj=new GameObject("Temporary guide capture camera");var camera=obj.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.aspect=4f/3f;camera.ResetProjectionMatrix();camera.ResetWorldToCameraMatrix();camera.GetUniversalAdditionalCameraData().allowXRRendering=false;obj.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
        Canvas.ForceUpdateCanvases();var target=new RenderTexture(1200,900,24);target.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1200,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(obj);
    }
}
