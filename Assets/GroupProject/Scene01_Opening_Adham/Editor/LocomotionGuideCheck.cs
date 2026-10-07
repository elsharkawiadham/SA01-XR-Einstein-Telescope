using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Opening;

[InitializeOnLoad]
public static class LocomotionGuideCheck
{
    static double started,finished;
    static bool welcomed;
    static StandaloneAIGuide ai;
    static int seen;
    static OpeningSequence sequence;
    static LocomotionGuide guide;
    static ElevatorCallButton call;
    static LocomotionGuideCheck(){EditorApplication.update+=Tick;}
    static void Assert(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run(){
        var scene=EditorSceneManager.OpenScene("Assets/GroupProject/Scene01_Opening_Adham/Scenes/Scene01_Opening_Adham.unity");
        var c=UnityEngine.Object.FindFirstObjectByType<ElevatorCallButton>();
        Physics.SyncTransforms();
        Vector3 start=new Vector3(0,1.5f,-.8f),target=c.door.position;
        Assert(Physics.Linecast(start,target,out var hit)&&!hit.transform.IsChildOf(c.door),"Elevator not hidden from spawn");
        Assert(Physics.Linecast(new Vector3(-1,1.5f,8),target,out hit)&&hit.transform.IsChildOf(c.door),"Elevator not revealed after left turn");
        Assert(!Physics.Raycast(start,Vector3.forward,8.8f),"Forward route blocked");
        Assert(!Physics.Raycast(new Vector3(0,1.5f,8),Vector3.left,8),"Left route blocked");
        SessionState.SetBool("Adham.GuideCheck",true);EditorApplication.isPlaying=true;
    }
    static void Tick(){
        int pending=SessionState.GetInt("Adham.GuideExit",-1);
        if(pending>=0 && !EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt("Adham.GuideExit",-1);EditorApplication.Exit(pending);return;}
        if(!SessionState.GetBool("Adham.GuideCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try{
            if(!guide){
                guide=UnityEngine.Object.FindFirstObjectByType<LocomotionGuide>();sequence=guide.GetComponent<OpeningSequence>();call=UnityEngine.Object.FindFirstObjectByType<ElevatorCallButton>();
                var head=new GameObject("Test head near elevator").transform;head.position=Vector3.zero;call.playerHead=head;
                started=EditorApplication.timeSinceStartup;ai=guide.aiGuide;
                Assert(guide.steps.Length==5,"Missing narrated steps");
            }
            Assert(EditorApplication.timeSinceStartup-started<180,"Guide timed out");
            if(sequence.welcome.isPlaying || (ai && ai.IsBusy && guide.CurrentStep<0)){welcomed=true;call.playerHead.position=call.transform.position+call.transform.forward*-1;Assert(!guide.panel.activeSelf&&!guide.narrator.isPlaying,"Guide overlapped welcome");}
            if(guide.IsRunning){
                Assert(welcomed,"Guide started before welcome");Assert(guide.panel.activeSelf,"Panel hidden during guide");
                Assert(!call.HintPlayed,"Elevator hint overlapped guide");
                int step=guide.CurrentStep;Assert(step>=0&&step<5,"Invalid step");seen|=1<<step;
                Assert(guide.instruction.text==LocomotionGuide.Captions[step],"Caption out of sync");
                if(guide.narrator.isPlaying)Assert(guide.narrator.clip==guide.steps[step],"Recorded voice out of sync");
                Assert(!(guide.narrator.isPlaying && ai && ai.speaker.isPlaying),"AI and recorded narration overlap");
                Assert(Vector3.Distance(guide.panel.transform.position,guide.headset.TransformPoint(guide.panelOffset))<.02f,"Panel not following left view");
            }
            if(seen==31&&!guide.IsRunning){
                Assert(!guide.panel.activeSelf,"Panel still visible after guide");
                if(finished==0)finished=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-finished>3.8){
                    Assert(call.HintPlayed,"Elevator hint did not resume after tutorial");
                    File.WriteAllText("Logs/LocomotionCheck.txt","PASS: elevator hidden at spawn and visible after left turn; forward and left paths clear; welcome first; all five voice/caption steps synchronized; left-view panel follows head and hides on finish; elevator hint waits and resumes. Scripted editor evidence, not physical controller acceptance.");
                    SessionState.SetBool("Adham.GuideCheck",false);SessionState.SetInt("Adham.GuideExit",0);EditorApplication.isPlaying=false;
                }
            }
        }catch(Exception e){File.WriteAllText("Logs/LocomotionCheck.txt","FAIL: "+e);Debug.LogException(e);SessionState.SetBool("Adham.GuideCheck",false);SessionState.SetInt("Adham.GuideExit",1);EditorApplication.isPlaying=false;}
    }
}
