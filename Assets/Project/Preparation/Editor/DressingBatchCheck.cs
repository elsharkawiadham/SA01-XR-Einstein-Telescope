using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class DressingBatchCheck
{
    static bool running;
    static DressingBatchCheck(){EditorApplication.update+=Tick;}
    public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("RunDressingBatch",true);EditorApplication.isPlaying=true;}
    static async void Tick(){if(running||!SessionState.GetBool("RunDressingBatch",false)||!EditorApplication.isPlaying||Time.time<.3f)return;running=true;try{await DressingInteractionCheck.Test();SessionState.SetBool("RunDressingBatch",false);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);SessionState.SetBool("RunDressingBatch",false);EditorApplication.Exit(1);}}
}
