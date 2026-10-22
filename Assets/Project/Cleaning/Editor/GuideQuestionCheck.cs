using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using GroupProject.Cleaning;
using GroupProject.Opening;
public static class GuideQuestionCheck
{
    [MenuItem("Tools/Cleaning/Check Guide Questions")]
    public static void Run()
    {
        var go=new GameObject("Temporary answer check"){hideFlags=HideFlags.HideAndDontSave};
        try{
            var station=go.AddComponent<CleaningStation>();station.tools=Array.Empty<CleaningTool>();
            Check(StandaloneAIGuide.SocialReply("How are you?").Contains("thanks"),"Greeting");
            Check(StandaloneAIGuide.SocialReply("Hello, how are you doing today?").Contains("thanks"),"Greeting variant");
            Check(StandaloneAIGuide.SocialReply("How am I doing?")==null,"Progress versus greeting");
            Check(station.Answer("Where is the lamp?").Contains("far left"),"Lamp location");
            Check(station.Answer("Where can I find it?").Contains("far left"),"Contextual lamp location");
            Check(station.Answer("How do I use the lamp?").Contains("hold the trigger"),"Lamp usage");
            Check(station.Answer("How are you?").Contains("thanks"),"Greeting in cleaning scene");
            Check(station.Answer("How is the weather?")==null,"Unrelated question must not repeat task");
            Check(station.Answer("What should I do next?")==station.Exercise.Feedback,"Explicit task question");
            Check(station.Answer("Can you help me do this?").Contains("purple lamp"),"Contextual help paraphrase");
            Check(station.Answer("I am stuck").Contains("press grip"),"Actionable help");
            station.Exercise.Inspected();
            Check(station.Answer("Where can I find it?").Contains("Anticon"),"Context changes to wipe");
            Check(station.Answer("What oven temperature?").Contains("approved local procedure"),"Procedure boundary");
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/GuideQuestionCheck.txt","PASS: greetings, contextual lamp/wipe location, usage, task requests, unrelated questions and procedure boundaries. Text routing only; microphone recognition not tested.");
            Debug.Log("Guide question checks PASS");
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static void Check(bool result,string name){if(!result)throw new Exception("Guide question check failed: "+name);}
}
