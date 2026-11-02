using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.XR.CoreUtils;
using GroupProject.Opening;
using GroupProject.Guide;
using GroupProject.Preparation;
using GroupProject.Elevator;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TouchController = UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile.OculusTouchController;

[InitializeOnLoad]
public static class ImmersiveControlsCheck
{
    static bool running;
    static ImmersiveControlsCheck(){EditorApplication.update+=Tick;}
    public static void Run(){
        EditorSceneManager.OpenScene(ProjectAssembly.Main);
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        var opening=UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();opening.fadeSeconds=.1f;
        foreach(var ai in UnityEngine.Object.FindObjectsByType<StandaloneAIGuide>(FindObjectsInactive.Include))ai.gameObject.SetActive(false);
        SessionState.SetBool("ET.ControlsCheck",true);EditorApplication.isPlaying=true;
    }
    static void Tick(){
        int exit=SessionState.GetInt("ET.ControlsExit",-1);if(exit>=0&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt("ET.ControlsExit",-1);EditorApplication.Exit(exit);return;}
        if(running||!SessionState.GetBool("ET.ControlsCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        running=true;Check();
    }
    static void Need(bool pass,string message){if(!pass)throw new Exception(message);}
    static async Task Until(Func<bool> pass,string message){double start=EditorApplication.timeSinceStartup;while(!pass()){if(EditorApplication.timeSinceStartup-start>20)throw new Exception(message);await Task.Delay(30);}}
    static void Move(ElevatorRide r,Vector3 head){var body=r.playerRoot.GetComponent<CharacterController>();bool was=body&&body.enabled;if(was)body.enabled=false;r.playerRoot.position+=head-r.playerHead.position;if(was)body.enabled=true;Physics.SyncTransforms();}
    static TrainingControls Input(int i){var s=new TrainingControls();switch(i){case 0:s.move=Vector2.up;break;case 1:s.move=Vector2.down;break;case 2:s.move=Vector2.left;break;case 3:s.move=Vector2.right;break;case 4:s.turn=Vector2.left;break;case 5:s.turn=Vector2.right;break;case 6:s.leftTrigger=true;break;case 7:s.rightTrigger=true;break;case 8:s.leftGrip=true;break;case 9:s.rightGrip=true;break;case 10:s.teleport=true;break;case 11:s.speak=true;break;case 12:s.checklist=true;break;}return s;}
    static void Stick(TouchController device,Vector2 value){using(StateEvent.From(device,out var e)){device.thumbstick.WriteValueIntoEvent(value,e);InputSystem.QueueEvent(e);}}
    static async Task SimulateSeconds(float seconds){
        float start=Time.time;double deadline=EditorApplication.timeSinceStartup+30;
        while(Time.time-start<seconds){Need(EditorApplication.timeSinceStartup<deadline,"Unity stopped advancing simulation frames");await Task.Delay(10);}
    }
    static async Task CheckXR(ElevatorRide ride,DesktopPreview desktop){
        desktop.enabled=false;
        var rig=ride.playerRoot;
        foreach(var behaviour in rig.GetComponentsInChildren<Behaviour>(true))
            if(behaviour.GetType().Name=="TrackedPoseDriver"||behaviour.GetType().Name=="XRInputModalityManager")behaviour.enabled=false;
        ride.playerHead.localPosition=Vector3.zero;ride.playerHead.localRotation=Quaternion.identity;
        rig.rotation=Quaternion.identity;Move(ride,new Vector3(-5,1.65f,8));
        InputSystem.RegisterLayout<TouchController>("ETMovementTestTouch");
        var left=(TouchController)InputSystem.AddDevice("ETMovementTestTouch");var right=(TouchController)InputSystem.AddDevice("ETMovementTestTouch");
        InputSystem.SetDeviceUsage(left,CommonUsages.LeftHand);InputSystem.SetDeviceUsage(right,CommonUsages.RightHand);
        try{
            await Task.Delay(250);var locomotion=rig.GetComponent<FacilityLocomotion>();
            Need(!DesktopPreview.Active&&locomotion.MoveProvider.enabled&&locomotion.TurnProvider.enabled,"XR providers disabled after desktop restore");
            for(int pass=0;pass<2;pass++){
                if(pass==1){rig.rotation=Quaternion.identity;Need(rig.GetComponent<FacilityTeleport>().TryTeleport(new Vector3(-4,0,8)),"XR teleport rejected");await Task.Delay(100);}
                var start=rig.position;Stick(left,Vector2.up);await SimulateSeconds(.35f);
                File.AppendAllText("Logs/XRInputDiagnostic.txt","pass="+pass+" device="+left.thumbstick.ReadValue()+" action="+locomotion.MoveProvider.leftHandMoveInput.ReadValue()+" controls="+locomotion.MoveProvider.leftHandMoveInput.inputAction.controls.Count+" enabled="+locomotion.MoveProvider.enabled+" active="+locomotion.MoveProvider.gameObject.activeInHierarchy+" start="+start+" end="+rig.position+" transformer="+rig.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.XRBodyTransformer>(true).enabled+"\n");
                Stick(left,Vector2.zero);await Task.Delay(80);
                Need(Vector3.ProjectOnPlane(rig.position-start,Vector3.up).magnitude>.06f,"Actual XR continuous walking failed, pass "+pass);
                var rotation=rig.rotation;Stick(right,Vector2.right);await SimulateSeconds(.3f);Stick(right,Vector2.zero);await Task.Delay(80);
                float angle=Quaternion.Angle(rotation,rig.rotation);Need(angle>2&&angle<26,"Actual XR smooth turn failed: "+angle+", pass "+pass);
            }
            var avatar=rig.GetComponent<PlayerBodyAvatar>();
            foreach(var angles in new[]{Vector3.zero,new Vector3(45,65,0),new Vector3(-30,-65,0)}){
                ride.playerHead.localRotation=Quaternion.Euler(angles);await Task.Delay(100);
                Need(Mathf.Abs(avatar.BodyScale-1)<.001f,"Body scaled to wearer height");
                var bounds=avatar.NeckBridge.GetComponent<Renderer>().bounds;
                Need(bounds.Contains(avatar.CollarPosition)&&bounds.Contains(avatar.MirrorHead.TransformPoint(new Vector3(0,-.065f,-.015f))),"Neck disconnected from torso/head");
            }
            ride.playerHead.localRotation=Quaternion.identity;await Task.Delay(100);CaptureAvatar(avatar);
            File.WriteAllText("Logs/XRMovementRepairCheck.txt","PASS: original 13-step controller artwork and first recording retained; actual OpenXR left-stick walking and right-stick smooth turning work after desktop-to-XR restore and again after teleport. Standard body scale and neck connection verified through head rotations. Synthetic controllers; physical headset review remains required.\n");
        }finally{InputSystem.RemoveDevice(left);InputSystem.RemoveDevice(right);}
    }
    static void CaptureAvatar(PlayerBodyAvatar avatar){
        var visible=avatar.VisualRoot.GetComponentsInChildren<Renderer>().Where(r=>r.shadowCastingMode==ShadowCastingMode.ShadowsOnly).ToArray();
        foreach(var renderer in visible)renderer.shadowCastingMode=ShadowCastingMode.On;
        var camera=new GameObject("Avatar validation camera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;camera.GetUniversalAdditionalCameraData().allowXRRendering=false;camera.aspect=1;camera.ResetProjectionMatrix();camera.ResetWorldToCameraMatrix();
        var centre=avatar.VisualRoot.position+Vector3.up*.9f;camera.transform.position=centre+avatar.BodyRotation*new Vector3(0,.2f,2.2f);camera.transform.LookAt(centre);
        var target=new RenderTexture(900,900,24);target.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        var previous=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(900,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,900,900),0,0);pixels.Apply();File.WriteAllBytes("Logs/Connected-Avatar.png",pixels.EncodeToPNG());RenderTexture.active=previous;
        UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camera.gameObject);foreach(var renderer in visible)renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
    }
    static async void Check(){int exit=1;try{
        await Task.Delay(700);
        var tutorial=UnityEngine.Object.FindAnyObjectByType<LocomotionGuide>();var ride=UnityEngine.Object.FindAnyObjectByType<ElevatorRide>();var rig=ride.playerRoot;var desktop=rig.GetComponent<DesktopPreview>();var journey=rig.GetComponent<PreparationJourney>();
        Need(AnimatedControllerHands.Active&&AnimatedControllerHands.Active.Initialized,"Human hands not initialized");
        Need(tutorial&&desktop&&LocomotionGuide.Locked,"Initial movement lock missing");var initial=rig.position;desktop.Move(Vector2.up,1);Need(Vector3.Distance(initial,rig.position)<.001f,"Can walk before onboarding");
        var originalPanel=tutorial.panel;
        tutorial.Begin();await Until(()=>tutorial.CurrentStep==0,"Onboarding did not start");
        Need(tutorial.panel==originalPanel&&originalPanel.GetComponentsInChildren<UnityEngine.UI.Image>().Count(x=>x.name=="Controller face")==2,"Original controller artwork replaced");
        Need(tutorial.narrator.clip==RecordedVoiceLibrary.ForText("Push the left stick forward to walk ahead."),"Original first voice not retained");
        var panelPosition=tutorial.panel.transform.position;var panelRotation=tutorial.panel.transform.rotation;
        tutorial.headset.localRotation=Quaternion.Euler(0,35,0);await Task.Delay(100);
        Need(Vector3.Distance(panelPosition,tutorial.panel.transform.position)<.001f&&Quaternion.Angle(panelRotation,tutorial.panel.transform.rotation)<.001f,"Instruction panel follows view");tutorial.headset.localRotation=Quaternion.identity;
        GuidePresentationCheck.Capture("Logs/Controls-Onboarding.png");
        for(int i=0;i<LocomotionGuide.Captions.Length;i++){
            await Until(()=>tutorial.CurrentStep==i,"Missing control step "+i);
            Need(Resources.Load<AudioClip>("Onboarding/Step"+i),"No voice asset "+i);
            tutorial.Observe(default,.2f);tutorial.Observe(Input((i+1)%13),.3f);tutorial.Observe(default,.2f);Need(!tutorial.ActionComplete,"Wrong control accepted "+i);
            tutorial.Observe(Input(i),.3f);Need(!tutorial.ActionComplete,"Held button completed before release "+i);
            tutorial.Observe(default,.2f);Need(tutorial.ActionComplete,"Correct control rejected "+i);
            tutorial.narrator.Stop();
            if(i==5){await Task.Delay(80);GuidePresentationCheck.Capture("Logs/Controls-GreenProgress.png");}
        }
        await Until(()=>tutorial.Completed,"Completed checks did not unlock");Need(!LocomotionGuide.Locked&&!desktop.MovementLocked,"Movement still locked");
        var teleport=rig.GetComponent<FacilityTeleport>();Need(teleport,"Teleport not attached");
        Move(ride,new Vector3(0,1.65f,1));await Task.Delay(80);
        Need(teleport.TryTeleport(new Vector3(0,0,3)),"Opening floor teleport rejected");Need(Mathf.Abs(ride.playerHead.position.z-3)<.05f,"Teleport missed destination");
        Need(!teleport.TryTeleport(new Vector3(12,0,19)),"Teleport bypassed route/walls");
        var companion=UnityEngine.Object.FindAnyObjectByType<ParrotCompanion>();var station=companion.GetComponent<ParrotStations>();Need(station,"Vantage stations missing");var anchor=station.Choose();Move(ride,new Vector3(.3f,1.65f,3.1f));await Task.Delay(70);Need(station.Choose()==anchor,"Station follows small movement");
        Move(ride,new Vector3(-8.5f,1.65f,8));var call=ride.callButton;call.openingSeconds=.12f;ride.doorSeconds=.15f;ride.travelSeconds=.8f;Need(call.TryCall(),"Lift call failed");await Task.Delay(220);
        Move(ride,new Vector3(-11.4f,2.6f,8));Need(ride.InsideCabin(),"Tall player rejected from cabin");Move(ride,new Vector3(-11.4f,.8f,8));Need(ride.InsideCabin(),"Short player rejected from cabin");Move(ride,new Vector3(-11.4f,1.65f,8));
        Need(teleport.TryTeleport(new Vector3(-11.8f,0,8)),"Cabin floor not teleportable: "+teleport.LastRejection);Need(ride.TryDepart(),"Lift departure failed");await Until(()=>ride.State==ElevatorRide.Phase.Descending,"Lift travel failed");
        Need(!desktop.MovementLocked,"Lift locks movement");var before=rig.position;desktop.Move(new Vector2(.1f,0),.1f);Need(Vector3.Distance(before,rig.position)>.001f,"Cannot move in cabin");
        Need(station.Choose().name=="Inside elevator","Guide missing inside elevator");
        await Until(()=>ride.State==ElevatorRide.Phase.Arrived,"Lift arrival failed");
        Move(ride,new Vector3(-7,1.65f,8));Need(teleport.TryTeleport(new Vector3(-5,0,8)),"B1 floor not teleportable: "+teleport.LastRejection);
        Move(ride,new Vector3(15,1.85f,19.3f));await Task.Delay(80);Need(teleport.TryTeleport(new Vector3(17,.22f,19.3f)),"Dressing floor not teleportable: "+teleport.LastRejection);Need(!teleport.TryTeleport(new Vector3(23,.22f,19.3f)),"Teleport bypassed shower door");
        journey.StorePersonalClothes();for(int kind=1;kind<=11;kind++)journey.Equip(kind);
        await Until(()=>journey.entryDoor.IsOpen,"Shower entry failed to open");
        Move(ride,new Vector3(23,1.85f,19.3f));await Task.Delay(80);
        Need(teleport.TryTeleport(new Vector3(23.2f,.22f,19.3f)),"Shower floor not teleportable: "+teleport.LastRejection);
        Need(journey.Select(5),"Shower door close rejected");await Until(()=>journey.entryDoor.IsClosed,"Shower door not closed");journey.showerSeconds=.2f;
        Need(journey.Select(4),"Shower start failed");await Until(()=>journey.ShowerComplete&&journey.exitDoor.IsOpen,"Shower exit failed");
        Move(ride,new Vector3(26,1.85f,19.3f));await Task.Delay(80);Need(teleport.TryTeleport(new Vector3(29,.22f,19.3f)),"Final hallway floor not teleportable: "+teleport.LastRejection);
        Move(ride,new Vector3(34,1.85f,25));await Task.Delay(80);Need(!teleport.TryTeleport(new Vector3(34,.22f,30)),"Teleport crossed final locked facility door");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Hand contact test wall";wall.transform.position=new Vector3(1000,1,.5f);wall.transform.localScale=new Vector3(2,3,.1f);Physics.SyncTransforms();
        var resolved=HandWallContact.Resolve(rig,new Vector3(1000,1,0),new Vector3(1000,1,1),out bool blocked);Need(blocked&&resolved.z<.33f,"Hand penetrates wall");
        resolved=HandWallContact.Resolve(rig,new Vector3(1000,1,0),new Vector3(1000,1,.1f),out blocked);Need(!blocked,"Free hand wrongly blocked");UnityEngine.Object.Destroy(wall);
        await CheckXR(ride,desktop);
        foreach(float eye in new[]{1.05f,2.15f}){
            var obj=new GameObject("Height calibration check");obj.SetActive(false);var o=obj.AddComponent<XROrigin>();o.enabled=false;
            var offset=new GameObject("Tracking offset");offset.transform.SetParent(obj.transform);o.CameraFloorOffsetObject=offset;
            var camera=new GameObject("Test head").AddComponent<Camera>();camera.enabled=false;camera.transform.SetParent(offset.transform);camera.transform.localPosition=Vector3.zero;o.Camera=camera;
            var calibration=obj.AddComponent<StartingViewHeight>();obj.SetActive(true);calibration.Apply(eye);Need(Mathf.Abs(camera.transform.position.y-1.65f)<.001f,"Height normalization failed "+eye);
            camera.transform.localPosition-=Vector3.up*.2f;Need(Mathf.Abs(camera.transform.position.y-1.45f)<.001f,"Crouching overwritten");UnityEngine.Object.Destroy(obj);
        }
        File.WriteAllText("Logs/ImmersiveControlsCheck.txt","PASS: required input checks and release detection, wrong-input rejection, initial movement lock and completion unlock, static centered panel, offline voice assets, ground/B1/dressing/cabin/shower/final-hallway teleport and closed-door/final-boundary rejection, tall/short cabin occupancy, in-cabin movement during descent, discrete guide stations, hand-wall clamping, standard device-relative eye-height offset with tracked crouching preserved. Physical headset and haptic sensation require device review.\n");exit=0;
    }catch(Exception e){File.WriteAllText("Logs/ImmersiveControlsCheck.txt","FAIL: "+e);Debug.LogException(e);}
    SessionState.SetBool("ET.ControlsCheck",false);SessionState.SetInt("ET.ControlsExit",exit);EditorApplication.isPlaying=false;
    }
}
