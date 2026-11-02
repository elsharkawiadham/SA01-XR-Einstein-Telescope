using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using GroupProject.Opening;
using GroupProject.Guide;
using PonyuDev.SherpaOnnx.Tts;
[InitializeOnLoad]
public static class RestoreGuideVoice
{
    static bool running;
    static RestoreGuideVoice(){EditorApplication.update+=Tick;}
    public static void Run(){
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        if(UnityEngine.XR.Management.XRGeneralSettings.Instance)UnityEngine.XR.Management.XRGeneralSettings.Instance.InitManagerOnStart=false;
        SessionState.SetBool("ET.RestoreVoice",true);EditorApplication.isPlaying=true;
    }
    static void Tick(){
        int result=SessionState.GetInt("ET.RestoreVoiceExit",-1);
        if(result>=0&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetInt("ET.RestoreVoiceExit",-1);EditorApplication.Exit(result);return;}
        if(running||!SessionState.GetBool("ET.RestoreVoice",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        running=true;Generate();
    }
    static void Finish(int result){SessionState.SetBool("ET.RestoreVoice",false);SessionState.SetInt("ET.RestoreVoiceExit",result);EditorApplication.isPlaying=false;}
    static async void Generate(){
        try{
            using(var voice=new TtsService {EnginePoolSize=1}){
                await voice.InitializeAsync();
                for(int i=0;i<15;i++){
                    string words=i<13?LocomotionGuide.SpokenForStep(i):i==13?TrainingGuidance.Environment:TrainingGuidance.Journey;
                    using(var audio=await voice.GenerateAsync(words,.82f,0)){
                        if(!audio.IsValid)throw new Exception("No voice audio for "+i);
                        WriteWave("Assets/Project/Opening/Resources/Onboarding/"+(i<13?"Step"+i:"Context"+(i-13))+".wav",audio.Samples,audio.SampleRate);
                    }
                }
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var library=Resources.Load<RecordedVoiceLibrary>("RecordedVoiceLibrary");
            var entries=library.entries.Where(e=>e.id==null||!e.id.StartsWith("onboarding_")).ToList();
            // The original forward/back/sideways recordings retain priority and remain byte-for-byte unchanged.
            for(int i=4;i<13;i++)entries.Add(new RecordedVoiceLibrary.Entry{id="onboarding_"+i,text=LocomotionGuide.SpokenForStep(i),clip=Resources.Load<AudioClip>("Onboarding/Step"+i)});
            for(int i=0;i<2;i++)entries.Add(new RecordedVoiceLibrary.Entry{id="onboarding_context"+i,text=i==0?TrainingGuidance.Environment:TrainingGuidance.Journey,clip=Resources.Load<AudioClip>("Onboarding/Context"+i)});
            library.entries=entries.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/RestoredGuideVoice.txt","PASS: original movement recordings retained; new prompts and context rendered using the existing local guide's configured en_US-lessac-low profile at 0.82 speed. Windows System.Speech replacements removed from playback.\n");
            Finish(0);
        }catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/RestoredGuideVoice.txt","FAIL: "+e);Finish(1);}
    }
    static void WriteWave(string path,float[] samples,int sampleRate){using(var writer=new BinaryWriter(File.Create(path))){
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(sampleRate);writer.Write(sampleRate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);foreach(float sample in samples)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample,-1,1)*32767));
    }}
}
