using System;
using System.IO;
using System.Linq;
using GroupProject.Elevator;
using GroupProject.Opening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[InitializeOnLoad] public static class ElevatorRideCheck
{
    static ElevatorRide ride; static ElevatorFloorButton floor; static int stage; static double since; static Vector3 closed;static GameObject destination;
    static ElevatorRideCheck(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Elevator/Run Descent Check")]
    public static void Run(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        if(SceneManager.GetActiveScene().isDirty)throw new Exception("Save scene first.");
        EditorSceneManager.OpenScene(OpeningProjectSetup.ScenePath);
        UnityEngine.Object.FindFirstObjectByType<OpeningSequence>().enabled=false;
        var ai=UnityEngine.Object.FindFirstObjectByType<StandaloneAIGuide>();
        if(ai){ai.opening.aiGuide=null;ai.tutorial.aiGuide=null;ai.elevator.aiGuide=null;ai.gameObject.SetActive(false);}
        stage=0; SessionState.SetBool("ElevatorRideCheck",true);EditorApplication.isPlaying=true;
    }
    static void Assert(bool condition,string reason){if(!condition)throw new Exception(reason);}
    static void Next(){stage++;since=EditorApplication.timeSinceStartup;}
    static void Place(Vector3 position){var body=ride.playerRoot.GetComponent<CharacterController>();body.enabled=false;ride.playerRoot.position=position;body.enabled=true;Physics.SyncTransforms();}
    static void Press(){floor.GetComponent<XRSimpleInteractable>().selectEntered.Invoke(new SelectEnterEventArgs());}
    static void Finish(string text){File.WriteAllText("Logs/ElevatorRideCheck.txt",text);SessionState.SetBool("ElevatorRideCheck",false);EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(text.StartsWith("PASS")?0:1);}
    static void Tick(){
        if(!SessionState.GetBool("ElevatorRideCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try{
            double elapsed=EditorApplication.timeSinceStartup-since;
            if(stage==0){
                ride=UnityEngine.Object.FindFirstObjectByType<ElevatorRide>();floor=UnityEngine.Object.FindFirstObjectByType<ElevatorFloorButton>();Assert(ride&&floor,"Missing ride/button");
                var opening=UnityEngine.Object.FindFirstObjectByType<OpeningSequence>();opening.StopAllCoroutines();opening.enabled=false;
                var inputs=ride.playerRoot.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>(true);
                Assert(inputs.Count(i=>i.allowHoveredActivate && i.activateInput.inputActionReferencePerformed && i.activateInput.inputActionReferencePerformed.action.bindings.Any(b=>b.path.Contains("Trigger")))>=2,"Both controller trigger activation paths must be configured");
                closed=ride.callButton.door.localPosition;ride.callButton.openingSeconds=.15f;ride.doorSeconds=.3f;ride.travelSeconds=.5f;
                Assert(!floor.TryPress(),"Remote/unboarded B1 accepted");Place(new Vector3(-8.5f,.03f,8));Assert(ride.callButton.TryCall(),"Call rejected");Next();
            }else if(stage==1&&elapsed>.5){
                Assert(!floor.TryPress(),"B1 accepted outside cabin");Place(new Vector3(-11.4f,.03f,8));Next();
            }else if(stage==2&&elapsed>.1){
                Assert(ride.InsideCabin(),"Cabin volume incorrect");floor.GetComponent<XRSimpleInteractable>().activated.Invoke(new ActivateEventArgs());Assert(ride.State==ElevatorRide.Phase.Closing,"XR select did not close doors");
                Assert(!floor.TryPress(),"Duplicate B1 accepted");Place(new Vector3(-9.9f,.03f,8));Next();
            }else if(stage==3&&elapsed>.7){
                Assert(ride.State==ElevatorRide.Phase.Boarding,"Doorway safety failed to reopen");Assert(Vector3.Distance(ride.callButton.door.localPosition,closed+ride.callButton.openOffset)<.02f,"Safety reopening incomplete");
                Place(new Vector3(-11.4f,.03f,8));destination=ride.destinationRoot;ride.destinationRoot=null;Press();Next();
            }else if(stage==4&&elapsed>1.5){
                Assert(ride.State==ElevatorRide.Phase.Failed,"Load failure did not recover");Assert(!ride.playerRoot.GetComponent<DesktopPreview>().MovementLocked,"Load failure retained movement lock");
                ride.destinationRoot=destination;ride.playerHead.LookAt(floor.transform.position);
                Assert(ride.playerRoot.GetComponent<DesktopPreview>().TryInteract(),"Desktop aimed B1 interaction failed");Next();
            }else if(stage==5&&elapsed>2){
                Assert(ride.State==ElevatorRide.Phase.Arrived,"Did not arrive: "+ride.State);
                Assert(SceneManager.sceneCount==1 && ride.destinationRoot.activeSelf,"Single-scene destination not activated");
                Assert(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length==1,"Duplicate camera");
                Assert(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,"Duplicate listener");
                Assert(!ride.playerRoot.GetComponent<DesktopPreview>().MovementLocked,"Movement not restored");
                Assert(Vector3.Distance(ride.callButton.door.localPosition,closed+ride.callButton.openOffset)<.02f,"Arrival door not open");
                Assert(!floor.TryPress(),"Arrival can trigger duplicate scene load");
                Place(new Vector3(-8,.03f,8));Assert(Physics.Raycast(ride.playerRoot.position+Vector3.up,Vector3.down,2),"No arrival floor collision");
                Finish("PASS: distant and outside selection rejected; XR trigger activation starts ride; desktop aimed B1 interaction; duplicate presses rejected; doorway reopens; missing destination recovers; single-scene B1 arrival; doors reopen; movement restored; single camera/listener; landing collision. Scripted editor checks, not headset validation.");
            }
            if(stage>0&&EditorApplication.timeSinceStartup-since>30)throw new Exception("Timed out at stage "+stage);
        }catch(Exception error){Finish("FAIL: "+error);Debug.LogException(error);}
    }
}
