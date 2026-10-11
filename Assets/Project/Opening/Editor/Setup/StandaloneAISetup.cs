using System;
using System.IO;
using LLMUnity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using GroupProject.Opening;

public static class StandaloneAISetup
{
    [MenuItem("Tools/Opening Scene/Build Quest APK")]
    public static void BuildQuest()
    {
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
            throw new BuildFailedException("Install Android Build Support, SDK/NDK and OpenJDK for Unity 6000.6.0f1 in Unity Hub first.");
        Configure();
        GuideSceneSetup.Configure();
        Directory.CreateDirectory("Builds");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{Root+"/Scenes/01_Opening.unity", ElevatorSceneSetup.ArrivalPath},
            locationPathName="Builds/OpeningQuest.apk",target=BuildTarget.Android,options=BuildOptions.None
        });
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new BuildFailedException("Quest APK build did not succeed. Inspect the Unity build report.");
    }
    static void ConfigureAudio(StandaloneAIGuide guide)
    {
        var voice=guide.transform.Find("Overhead guide voice");
        if(!voice){voice=new GameObject("Overhead guide voice").transform;voice.SetParent(guide.transform,false);}
        var source=voice.GetComponent<AudioSource>();if(!source)source=voice.gameObject.AddComponent<AudioSource>();
        if(guide.speaker && guide.speaker!=source)UnityEngine.Object.DestroyImmediate(guide.speaker);
        guide.speaker=source;source.playOnAwake=false;source.spatialBlend=1;source.minDistance=3;source.maxDistance=15;source.rolloffMode=AudioRolloffMode.Linear;
    }
    const string Root="Assets/Project/Opening";
    [MenuItem("Tools/Opening Scene/Configure Standalone AI")]
    public static void Configure()
    {
        foreach(string p in Directory.GetFiles("Assets/Plugins/SherpaOnnx","*",SearchOption.AllDirectories))
        {
            if(!(AssetImporter.GetAtPath(p) is PluginImporter importer))continue;
            importer.SetCompatibleWithAnyPlatform(false);
            if(p.EndsWith(".dll")){importer.SetCompatibleWithEditor(true);importer.SetCompatibleWithPlatform(BuildTarget.Android,true);importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX,true);}
            else if(p.Contains("arm64-v8a")){importer.SetCompatibleWithEditor(false);importer.SetCompatibleWithPlatform(BuildTarget.Android,true);importer.SetPlatformData(BuildTarget.Android,"CPU","ARM64");}
            else if(p.Contains("osx-arm64")){importer.SetCompatibleWithEditor(true);importer.SetEditorData("OS","OSX");importer.SetEditorData("CPU","ARM64");importer.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX,true);importer.SetPlatformData(BuildTarget.StandaloneOSX,"CPU","ARM64");}
            importer.SaveAndReimport();
        }
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/01_Opening.unity");
        var root=GameObject.Find("Scene01_Opening_Adham");
        string source="Assets/StreamingAssets/qwen2.5-0.5b-instruct-q4_k_m.gguf";
        if(!File.Exists(source))throw new FileNotFoundException("Run Tools/setup_offline_guide.py first",source);
        var existing=LLMManager.Get(Path.GetFileName(source));
        if(existing!=null)existing.path=Path.GetFullPath(source);
        string modelFile=LLMManager.LoadModel(source);
        LLMManager.SetDownloadOnStart(false);
        LLMManager.SaveToDisk();
        var installed=root.GetComponentInChildren<StandaloneAIGuide>(true);
        if(installed){ConfigureAudio(installed);installed.agent.systemPrompt=StandaloneAIGuide.SystemPrompt; installed.model.model=modelFile;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return;}
        var ai=new GameObject("Standalone_AI_Guide");ai.SetActive(false);ai.transform.SetParent(root.transform,false);
        var model=ai.AddComponent<LLM>();model.dontDestroyOnLoad=false;model.remote=false;model.numThreads=2;model.numGPULayers=0;model.contextSize=2048;model.parallelPrompts=1;model.batchSize=128;
        model.model=modelFile;
        var agent=ai.AddComponent<LLMAgent>();agent.llm=model;agent.remote=false;agent.numPredict=100;agent.temperature=.2f;agent.systemPrompt=StandaloneAIGuide.SystemPrompt;
        var guide=ai.AddComponent<StandaloneAIGuide>();guide.model=model;guide.agent=agent;guide.tutorial=root.GetComponent<LocomotionGuide>();guide.opening=root.GetComponent<OpeningSequence>();guide.elevator=root.GetComponentInChildren<ElevatorCallButton>();guide.playerRoot=GameObject.Find("Shared_XR_Player").transform;
        ConfigureAudio(guide);
        guide.tutorial.aiGuide=guide;guide.opening.aiGuide=guide;guide.elevator.aiGuide=guide;
        var panel=new GameObject("AI conversation feedback",typeof(RectTransform),typeof(Canvas));panel.transform.SetParent(ai.transform,false);var rect=panel.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(600,240);rect.localScale=Vector3.one*.0015f;
        var canvas=panel.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;guide.feedbackPanel=canvas;
        var bg=new GameObject("Background",typeof(RectTransform),typeof(Image));bg.transform.SetParent(panel.transform,false);bg.GetComponent<RectTransform>().sizeDelta=rect.sizeDelta;bg.GetComponent<Image>().color=new Color(.01f,.025f,.04f,.94f);bg.GetComponent<Image>().raycastTarget=false;
        var label=new GameObject("Transcript and status",typeof(RectTransform),typeof(Text));label.transform.SetParent(panel.transform,false);label.GetComponent<RectTransform>().sizeDelta=new Vector2(550,210);var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=24;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;text.text="Hold B on the right controller to ask. Release to send.";guide.feedbackText=text;
        ai.SetActive(true);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.macOS.microphoneUsageDescription="Use your microphone to ask the offline VR guide questions.";
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("STANDALONE AI SETUP PASS");
    }
}
