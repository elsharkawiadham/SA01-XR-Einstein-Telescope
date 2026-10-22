using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Preparation;
using GroupProject.Opening;
[InitializeOnLoad]
public static class PreparationCheck
{
    static int step;static double since;
    static PreparationCheck(){EditorApplication.update+=Tick;}
    public static void Run(){EditorSceneManager.OpenScene(ProjectAssembly.Main);foreach(var a in UnityEngine.Object.FindObjectsByType<StandaloneAIGuide>(FindObjectsSortMode.None))a.gameObject.SetActive(false);var opening=UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();if(opening){opening.enabled=false;opening.locomotionGuide.enabled=false;}SessionState.SetBool("PreparationCheck",true);EditorApplication.isPlaying=true;}
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Move(PreparationJourney j,Vector3 p,float yaw=0){var root=j.ride.playerRoot;var c=root.GetComponent<CharacterController>();if(c)c.enabled=false;root.position+=p-j.head.position;root.rotation=Quaternion.Euler(0,yaw,0);Physics.SyncTransforms();}
    static void Place(WearableGarment g){g.SetPreview(true);g.transform.position=g.TargetPosition();g.SetPreview(false);}
    static void Tick(){if(!SessionState.GetBool("PreparationCheck",false)||!EditorApplication.isPlaying)return;
        try{var j=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>();if(!j||!j.transform.Find("Training checklist"))return;
            if(step==0){
                j.ride.destinationRoot.SetActive(true);j.ride.enabled=false;j.guide=null;
                foreach(var c in j.ride.departureRoot.GetComponentsInChildren<Collider>(true))if(!c.transform.IsChildOf(j.ride.transform))c.enabled=false;
                Move(j,new Vector3(16,1.7f,19.5f));
                foreach(var d in UnityEngine.Object.FindObjectsByType<LockerDoor>(FindObjectsSortMode.None))d.Toggle();step=10;since=EditorApplication.timeSinceStartup;return;
            }
            if(step==10){
                if(UnityEngine.Object.FindObjectsByType<LockerDoor>(FindObjectsSortMode.None).Any(d=>Quaternion.Angle(d.hinge.localRotation,Quaternion.identity)<100)){if(EditorApplication.timeSinceStartup-since>30)throw new Exception("Locker animation timeout");return;}
                GuidePresentationCheck.Capture("Logs/GarmentsInLockers.png");
                var clothes=UnityEngine.Object.FindObjectsByType<WearableGarment>(FindObjectsSortMode.None);Require(clothes.Length==8,"Missing garment assets");
                var hood=clothes.First(g=>g.kind==WearableGarment.Kind.Hood);Place(hood);Require(!hood.Equipped,"Can wear before storing personal clothes");
                var jacket=clothes.First(g=>g.kind==WearableGarment.Kind.PersonalJacket);Place(jacket);Require(j.PersonalClothesStored,"Jacket was not stored");
                hood.SetPreview(true);hood.transform.position=new Vector3(16,1,20);hood.SetPreview(false);Require(!hood.Equipped,"Wrong body zone accepted");
                foreach(var g in clothes.Where(g=>g.kind!=WearableGarment.Kind.PersonalJacket))Place(g);
                Require(j.Dressed&&!j.showerGate.activeSelf,"Dressing did not unlock shower");
                j.transform.Find("Training checklist").gameObject.SetActive(true);step=1;since=EditorApplication.timeSinceStartup;
            }else if(step==1&&EditorApplication.timeSinceStartup-since>2){
                foreach(var d in UnityEngine.Object.FindObjectsByType<LockerDoor>(FindObjectsSortMode.None))Require(d.IsOpen&&Quaternion.Angle(d.hinge.localRotation,Quaternion.identity)>90,"Locker failed to open");
                GuidePresentationCheck.Capture("Logs/ChangingRoomRebuilt.png");Move(j,new Vector3(23,1.7f,19.3f));j.showerSeconds=2;j.Select(4);Require(j.ShowerRunning,"Shower start failed");step=2;since=EditorApplication.timeSinceStartup;
            }else if(step==2&&EditorApplication.timeSinceStartup-since>.6){
                var fx=UnityEngine.Object.FindAnyObjectByType<AirShowerEffects>();Require(fx.fan.isPlaying&&fx.fan.volume>0,"Fan not audible");Require(fx.jets.All(p=>p.isPlaying),"Air jets not started");GuidePresentationCheck.Capture("Logs/AirShowerRunning.png");Move(j,new Vector3(21,1.7f,19.3f));step=3;since=EditorApplication.timeSinceStartup;
            }else if(step==3&&EditorApplication.timeSinceStartup-since>.3){Require(!j.ShowerRunning&&!j.ShowerComplete,"Leaving shower failed to cancel");Move(j,new Vector3(23,1.7f,19.3f));j.showerSeconds=.5f;j.Select(4);step=4;since=EditorApplication.timeSinceStartup;
            }else if(step==4&&j.ShowerComplete){
                Require(!j.cleanGate.activeSelf,"Exit remained closed");ProjectAssembly.ValidateRoute();
                File.WriteAllText("Logs/PreparationCheck.txt","PASS: 8 garment assets, personal-storage prerequisite, rejection of wrong body placement, seven equipped garments, animated locker opening, shower fan and six particle streams, cancellation on exit, restart/completion, route floor and capsule clearance. Editor simulation only; no Quest hardware acceptance.");SessionState.SetBool("PreparationCheck",false);EditorApplication.Exit(0);
            }else if(step==4&&EditorApplication.timeSinceStartup-since>30)throw new Exception("Shower timeout");
        }catch(Exception e){Debug.LogException(e);SessionState.SetBool("PreparationCheck",false);EditorApplication.Exit(1);}
    }
}
