using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GroupProject.Opening;

[InitializeOnLoad]
public static class ElevatorInteractionCheck
{
    static ElevatorCallButton button;
    static Transform head;
    static int stage, calls;
    static double since;
    static Vector3 closed;
    static ElevatorInteractionCheck() { EditorApplication.update += Tick; }
    public static void Run() {
        EditorSceneManager.OpenScene("Assets/GroupProject/Scene01_Opening_Adham/Scenes/Scene01_Opening_Adham.unity");
        SessionState.SetBool("Adham.ElevatorCheck",true);
        EditorApplication.isPlaying=true;
    }
    static void Assert(bool value,string reason) { if(!value) throw new Exception(reason); }
    static void Next() {stage++;since=EditorApplication.timeSinceStartup;}
    static void Near() {head.position=button.transform.position+Vector3.back;}
    static void Far() {head.position=Vector3.zero;}
    static void Select() {button.GetComponent<XRSimpleInteractable>().selectEntered.Invoke(new SelectEnterEventArgs());}
    static void Tick() {
        if(!SessionState.GetBool("Adham.ElevatorCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try {
            double elapsed=EditorApplication.timeSinceStartup-since;
            if(stage==0) {
                button=UnityEngine.Object.FindFirstObjectByType<ElevatorCallButton>();Assert(button,"Missing button");
                head=new GameObject("Test head").transform;button.playerHead=head;
                var opening=UnityEngine.Object.FindFirstObjectByType<OpeningSequence>();opening.StopAllCoroutines();opening.enabled=false;
                button.welcomeVoice.Stop();button.hintDelay=.5f;button.openingSeconds=.4f;
                closed=button.door.localPosition;button.onCalled.AddListener(()=>calls++);Far();Next();
            } else if(stage==1 && elapsed>.7) {
                Assert(!button.proximityVisual.activeSelf && !button.HintPlayed,"Far player revealed arrow or triggered hint");
                Assert(!button.TryCall(),"Remote press accepted");Near();Next();
            } else if(stage==2 && elapsed>.15) {
                Assert(button.proximityVisual.activeSelf && !button.HintPlayed,"Early nearby state incorrect");Far();Next();
            } else if(stage==3 && elapsed>.2) {Near();Next();}
            else if(stage==4 && elapsed>.2) {Assert(!button.HintPlayed,"Leaving did not reset timer");Next();}
            else if(stage==5 && elapsed>.5) {
                Assert(button.HintPlayed,"Delayed hint never played");Select();
                Assert(button.Called && calls==1 && !button.hintVoice.isPlaying,"Selection did not call once and cancel hint");
                Select();Assert(calls==1,"Repeated press duplicated call");Next();
            } else if(stage==6 && elapsed>.7) {
                Assert(Vector3.Distance(button.door.localPosition,closed+button.openOffset)<.01f,"Door did not reach open position");
                Assert(button.indicator.sharedMaterial==button.calledMaterial,"Called indicator not green");
                // Fresh instance: immediate selection must prevent delayed prompt.
                var copy=UnityEngine.Object.Instantiate(button.gameObject);button=copy.GetComponent<ElevatorCallButton>();button.playerHead=head;Near();
                Select();Next();
            } else if(stage==7 && elapsed>.8) {
                Assert(button.Called&&!button.HintPlayed&&!button.hintVoice.isPlaying,"Hint played after immediate press");
                File.WriteAllText("Temp/ElevatorCheck.txt","PASS: proximity visibility; distant press rejected; timer resets on leaving; delayed hint; XR select event; cancel hint; duplicate press ignored; door opens; green indicator; immediate press suppresses hint. Scripted editor check only.");
                SessionState.SetBool("Adham.ElevatorCheck",false);EditorApplication.Exit(0);
            }
        } catch(Exception e) {
            File.WriteAllText("Temp/ElevatorCheck.txt","FAIL: "+e);Debug.LogException(e);SessionState.SetBool("Adham.ElevatorCheck",false);EditorApplication.Exit(1);
        }
    }
}
