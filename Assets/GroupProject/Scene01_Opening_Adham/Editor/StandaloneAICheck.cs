using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GroupProject.Opening;
using PonyuDev.SherpaOnnx.Tts;
using PonyuDev.SherpaOnnx.Asr.Offline;

[InitializeOnLoad]
public static class StandaloneAICheck
{
    static bool running;
    static StandaloneAICheck(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/GroupProject/Scene01_Opening_Adham/Scenes/Scene01_Opening_Adham.unity");
        UnityEngine.Object.FindFirstObjectByType<OpeningSequence>().enabled=false;
        SessionState.SetBool("Adham.AICheck",true);
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        int pending=SessionState.GetInt("Adham.AICheckExit",-1);
        if(pending>=0 && !EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt("Adham.AICheckExit",-1);EditorApplication.Exit(pending);return;}
        if(running||!SessionState.GetBool("Adham.AICheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        running=true;Check();
    }
    static async void Check()
    {
        try
        {
            var guide=UnityEngine.Object.FindFirstObjectByType<StandaloneAIGuide>();
            if(!guide)throw new Exception("No guide in scene");
            double start=EditorApplication.timeSinceStartup;
            while(!guide.Ready&&guide.Failure==null&&EditorApplication.timeSinceStartup-start<120)await Task.Delay(100);
            if(!guide.Ready)throw new Exception("Initialization: "+guide.Failure);
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var tts=(ITtsService)typeof(StandaloneAIGuide).GetField("synthesis",flags).GetValue(guide);
            var asr=(IAsrService)typeof(StandaloneAIGuide).GetField("recognition",flags).GetValue(guide);
            string heard;
            using(var question=await tts.GenerateAsync("How do I turn left to find the elevator?"))
            {
                if(!question.IsValid)throw new Exception("No synthesized question audio");
                var result=await asr.RecognizeAsync(question.Samples,question.SampleRate);
                heard=result.Text;
                if(string.IsNullOrWhiteSpace(heard)||!heard.ToLowerInvariant().Contains("elevator"))throw new Exception("Transcription failed: "+heard);
                var answer=(Task)typeof(StandaloneAIGuide).GetMethod("Answer",flags).Invoke(guide,new object[]{question.Samples,question.SampleRate});
                if(await Task.WhenAny(answer,Task.Delay(90000))!=answer)throw new Exception("Answer timeout");
                await answer;
            }
            if(string.IsNullOrWhiteSpace(guide.LastReply))throw new Exception("No spoken AI reply");
            string reply=guide.LastReply;
            if(!reply.Contains("right joystick") || reply.Contains("trigger"))throw new Exception("Incorrect turning advice: "+reply);
            bool spoken=await guide.SpeakInstruction("Welcome. Follow the floor arrows and walk forward toward the corner.");
            if(!spoken)throw new Exception("Automatic instruction fell back");
            string report="PASS: native local TTS -> ASR -> LLM -> TTS and automatic narration completed.\nRecognized question: "+heard+"\nAnswer: "+reply+"\nAutomatic instruction: "+guide.LastReply+"\nElapsed seconds: "+(EditorApplication.timeSinceStartup-start).ToString("F1")+"\nEditor synthetic-audio test; physical microphone, Quest controls, standalone build and performance NOT tested.\n";
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/StandaloneAICheck.txt",report);Debug.Log(report);
            SessionState.SetBool("Adham.AICheck",false);SessionState.SetInt("Adham.AICheckExit",0);EditorApplication.isPlaying=false;
        }
        catch(Exception e)
        {
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/StandaloneAICheck.txt","FAIL: "+e);Debug.LogException(e);
            SessionState.SetBool("Adham.AICheck",false);SessionState.SetInt("Adham.AICheckExit",1);EditorApplication.isPlaying=false;
        }
    }
}
