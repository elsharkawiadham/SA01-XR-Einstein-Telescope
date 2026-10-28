using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GroupProject.Opening;
using GroupProject.Preparation;

/// <summary>Saved-scene visual/state checks. Actual XRI controller selection is checked separately by QuestClothingGrabCheck.</summary>
[InitializeOnLoad]
public static class ControllerEmbodimentCheck
{
    const string Key="ControllerEmbodimentCheck";
    static bool running;
    static ControllerEmbodimentCheck(){EditorApplication.update+=Tick;}
    public static void ConfigureAndRun(){ControllerEmbodimentSetup.Run();Run();}
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Run this isolated check in batch mode while the editor is closed.");
        EditorSceneManager.OpenScene(ProjectAssembly.Main);
        foreach(var guide in UnityEngine.Object.FindObjectsByType<StandaloneAIGuide>(FindObjectsInactive.Include,FindObjectsSortMode.None))guide.gameObject.SetActive(false);
        foreach(var opening in UnityEngine.Object.FindObjectsByType<OpeningSequence>(FindObjectsSortMode.None))opening.enabled=false;
        foreach(var tutorial in UnityEngine.Object.FindObjectsByType<LocomotionGuide>(FindObjectsSortMode.None))tutorial.enabled=false;
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static async void Tick()
    {
        if(running||!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||Time.time<.4f)return;
        running=true;
        try
        {
            await Test();
            File.WriteAllText("Logs/ControllerEmbodimentCheck.txt","PASS: saved scene has one configured human-hand pair and body; all 12 garments are solid XR query targets; grip/index/thumb animate independently; bare-to-gloved hands follow fitted garment state independently; coverall switches to the deforming body; default shoes hide under fitted footwear; standing calibration persists on crouching; 1.25m and 1.90m eye heights fit chest and footwear with sole contact; overshoes hide underlying boots; small head turns preserve torso yaw; large turns catch up; head remains freely controlled. Looking-down images captured before/after dressing. Editor simulation only; Quest Link tracking, comfort and performance still need a headset check.");
            SessionState.SetBool(Key,false);EditorApplication.Exit(0);
        }
        catch(Exception e){Directory.CreateDirectory("Logs");File.WriteAllText("Logs/ControllerEmbodimentCheck.txt","FAIL: "+e);Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.Exit(1);}
    }
    static async Task Test()
    {
        Directory.CreateDirectory("Logs");
        var journey=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>();
        Require(journey&&journey.ride,"Missing saved journey");
        journey.guide=null;journey.ride.enabled=false;journey.ride.StopAllCoroutines();journey.ride.destinationRoot.SetActive(true);
        var player=journey.ride.playerRoot;var head=journey.head;
        var preview=player.GetComponent<DesktopPreview>();if(preview)preview.enabled=false;
        foreach(var behaviour in player.GetComponentsInChildren<Behaviour>(true))
        {
            string name=behaviour.GetType().Name;
            if(name=="TrackedPoseDriver"||name=="XRInputModalityManager"||name=="CharacterControllerDriver"||name=="XROrigin"||name=="XRBodyTransformer"||name.Contains("MoveProvider")||name.Contains("TurnProvider"))behaviour.enabled=false;
        }
        var capsule=player.GetComponent<CharacterController>();if(capsule)capsule.enabled=false;
        player.SetPositionAndRotation(new Vector3(16,.22f,20.35f),Quaternion.identity);
        head.SetPositionAndRotation(player.position+Vector3.up*1.65f,Quaternion.identity);
        var hands=player.GetComponent<AnimatedControllerHands>();var avatar=player.GetComponent<PlayerBodyAvatar>();
        Require(hands&&avatar,"Missing saved human hands/body setup");
        hands.Initialize();Require(hands.Initialized&&hands.LeftWrist&&hands.RightWrist,"Human hand prefabs failed to initialize");
        Require(UnityEngine.Object.FindObjectsByType<AnimatedControllerHands>(FindObjectsSortMode.None).Length==1,"Duplicate hand driver");
        Require(UnityEngine.Object.FindObjectsByType<PlayerBodyAvatar>(FindObjectsSortMode.None).Length==1,"Duplicate avatar");
        hands.leftController.gameObject.SetActive(true);hands.rightController.gameObject.SetActive(true);
        hands.leftController.SetPositionAndRotation(head.position+new Vector3(-.23f,-.42f,.35f),Quaternion.identity);
        hands.rightController.SetPositionAndRotation(head.position+new Vector3(.23f,-.42f,.35f),Quaternion.identity);
        await Seconds(.3f);
        Require(!avatar.IsCalibrated,"Desktop height persisted after switching to the headset path");
        // There is no tracked headset on this Mac. Feed the explicit standing-calibration command a synthetic pose.
        avatar.RecalibrateStandingHeight();
        Require(avatar.IsCalibrated,"Standing height did not calibrate");
        Require(Mathf.Abs(avatar.CalibratedEyeHeight-1.65f)<.12f,"Avatar calibration does not match floor-to-eye height");
        Require(!hands.IsGloved(true)&&!hands.IsGloved(false),"Hands start gloved before dressing");
        Require(avatar.BodyRenderer&&avatar.BodyRenderer.sharedMesh.vertexCount>200,"Body mesh missing");
        var garments=journey.ride.destinationRoot.GetComponentsInChildren<WearableGarment>(true);
        Require(garments.Length==12,"Expected 12 saved garments");
        foreach(var garment in garments)Require(garment.GetComponents<Collider>().All(c=>!c.isTrigger),"Garment still ignored by XR caster: "+garment.name);

        var middle=Find(hands.LeftVisualRoot,"L_MiddleProximal");var index=Find(hands.LeftVisualRoot,"L_IndexProximal");var thumb=Find(hands.LeftVisualRoot,"L_ThumbProximal");
        Require(middle&&index&&thumb,"Missing finger joints");
        hands.ApplyPose(true,0,0,0,1);var middleRest=middle.localRotation;var indexRest=index.localRotation;var thumbRest=thumb.localRotation;
        hands.ApplyPose(true,1,0,0,1);Require(Quaternion.Angle(middleRest,middle.localRotation)>30,"Grip does not curl fingers");Require(Quaternion.Angle(indexRest,index.localRotation)<1,"Grip wrongly curls trigger index");
        hands.ApplyPose(true,0,1,0,1);Require(Quaternion.Angle(indexRest,index.localRotation)>30,"Trigger does not curl index");
        hands.ApplyPose(true,0,0,1,1);Require(Quaternion.Angle(thumbRest,thumb.localRotation)>15,"Thumb input does not animate thumb");
        hands.ApplyPose(true,0,0,0,1);
        float height=avatar.CalibratedEyeHeight,yaw=avatar.BodyYaw;
        head.rotation=Quaternion.Euler(0,15,0);await Seconds(.35f);
        Require(Mathf.Abs(Mathf.DeltaAngle(yaw,avatar.BodyYaw))<3,"Small glance rotates torso");
        head.position-=Vector3.up*.4f;await Seconds(.4f);
        Require(Mathf.Abs(avatar.CalibratedEyeHeight-height)<.005f,"Crouching shrinks calibrated body");
        head.position+=Vector3.up*.4f;head.rotation=Quaternion.Euler(0,100,0);await Seconds(1);
        Require(Mathf.Abs(Mathf.DeltaAngle(yaw,avatar.BodyYaw))>30,"Torso never follows a full turn");
        head.rotation=Quaternion.identity;await Seconds(1);
        head.rotation=Quaternion.Euler(65,0,0);await Frames(3);
        GuidePresentationCheck.Capture("Logs/BodyBeforeDressing.png");
        var jacket=garments.Single(g=>g.kind==WearableGarment.Kind.PersonalJacket);
        jacket.transform.position=jacket.TargetPosition();Require(jacket.TryPlace()&&avatar.PersonalJacketStored,"Jacket storage did not update body state");
        var suit=garments.Single(g=>g.kind==WearableGarment.Kind.Coverall);
        suit.transform.position=suit.TargetPosition();Require(suit.TryPlace()&&avatar.CoverallWorn,"Suit fitting did not dress body");
        Require(suit.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"Rigid suit overlaps animated body");
        var leftGlove=garments.Single(g=>g.kind==WearableGarment.Kind.LeftGlove);
        leftGlove.transform.position=leftGlove.TargetPosition();Require(leftGlove.TryPlace(),"Left glove fit failed");
        Require(hands.IsGloved(true)&&!hands.IsGloved(false),"One glove changed both hands");
        var rightGlove=garments.Single(g=>g.kind==WearableGarment.Kind.RightGlove);
        rightGlove.transform.position=rightGlove.TargetPosition();Require(rightGlove.TryPlace()&&hands.IsGloved(false),"Right glove did not switch skin");
        foreach(var garment in garments.Where(g=>!g.Equipped&&!g.Stored))
        {
            garment.transform.position=garment.TargetPosition();Require(garment.TryPlace(),"Garment fit failed: "+garment.kind);
            await Frames(2);
        }
        Require(avatar.IsFootwearWorn(true)&&avatar.IsFootwearWorn(false),"Default shoes remain after footwear fits");
        Require(journey.Dressed&&!journey.showerGate.activeSelf,"Full outfit failed to unlock shower");
        foreach(var boot in garments.Where(g=>g.kind==WearableGarment.Kind.LeftBoot||g.kind==WearableGarment.Kind.RightBoot))
            Require(boot.GetComponentsInChildren<Renderer>().All(r=>!r.enabled),"Boot mesh overlaps its fitted overshoe");
        foreach(float standingHeight in new[]{1.25f,1.90f,1.65f})
        {
            head.position=new Vector3(head.position.x,avatar.FloorHeight+standingHeight,head.position.z);
            avatar.RecalibrateStandingHeight();await Frames(3);
            Require(Mathf.Abs(avatar.CalibratedEyeHeight-standingHeight)<.003f,"Standing calibration used an average instead of measured height");
            Require(avatar.Chest.position.y<head.position.y-.15f&&avatar.Chest.position.y>avatar.FloorHeight+.5f,"Chest fitting target does not fit the user's height");
            foreach(var garment in garments.Where(g=>g.Footwear))
            {
                var bounds=garment.GetComponent<MeshFilter>().sharedMesh.bounds;
                float sole=garment.transform.TransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)).y;
                Require(Mathf.Abs(sole-avatar.FloorHeight)<.004f,"Scaled footwear lost floor contact at eye height "+standingHeight);
            }
        }
        await Frames(4);GuidePresentationCheck.Capture("Logs/BodyAfterDressing.png");
        head.rotation=Quaternion.Euler(85,0,0);await Frames(3);GuidePresentationCheck.Capture("Logs/BodyLookingDown.png");
        CaptureBody(avatar,"Logs/BodyThirdPerson.png");
        // Existing release/foot-floor regressions remain isolated from the scene's calibrated avatar.
        await DressingInteractionCheck.Test();
    }
    static void CaptureBody(PlayerBodyAvatar body,string path)
    {
        var source=Camera.main;var obj=new GameObject("Temporary body inspection camera");
        var camera=obj.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
        camera.GetUniversalAdditionalCameraData().allowXRRendering=false;
        camera.aspect=4f/3f;camera.fieldOfView=70;camera.ResetProjectionMatrix();camera.ResetWorldToCameraMatrix();
        var target=body.head.position;target.y=body.FloorHeight+.85f;
        obj.transform.SetPositionAndRotation(target+new Vector3(0,.1f,1.35f),Quaternion.LookRotation(new Vector3(0,-.1f,-1.35f)));
        var rt=new RenderTexture(1200,900,24);rt.Create();var previous=RenderTexture.active;
        try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;
            var texture=new Texture2D(1200,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1200,900),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);}
        finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
    }
    static Transform Find(Transform root,string name){return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);}
    static void Require(bool yes,string reason){if(!yes)throw new InvalidOperationException(reason);}
    static async Task Frames(int count)
    {
        int end=Time.frameCount+count;double deadline=EditorApplication.timeSinceStartup+15;
        while(Time.frameCount<end){if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Play-mode frame timeout");await Task.Yield();}
    }
    static async Task Seconds(float seconds)
    {
        float end=Time.time+seconds;double deadline=EditorApplication.timeSinceStartup+seconds+20;
        while(Time.time<end){if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Play-mode wait timeout");await Frames(1);}
    }
}
