using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using GroupProject.Guide;
public static class RecordedVoiceImport
{
    [Serializable] public class Row { public string id,text,originalName,path; }
    [Serializable] public class Manifest { public Row[] entries; }
    [MenuItem("Tools/Guide/Import Recorded Voices")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory("Assets/Project/Guide/Resources");AssetDatabase.Refresh();
        const string path="Assets/Project/Guide/Resources/RecordedVoiceLibrary.asset";
        var library=AssetDatabase.LoadAssetAtPath<RecordedVoiceLibrary>(path);
        if(!library){library=ScriptableObject.CreateInstance<RecordedVoiceLibrary>();AssetDatabase.CreateAsset(library,path);}
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/Project/Guide/Audio/Generated/VoiceManifest.json"));
        library.entries=new RecordedVoiceLibrary.Entry[manifest.entries.Length];
        for(int i=0;i<manifest.entries.Length;i++){
            var row=manifest.entries[i];var importer=(AudioImporter)AssetImporter.GetAtPath(row.path);
            importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.CompressedInMemory;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(row.path);if(!clip||clip.length<=0)throw new Exception("Invalid recording: "+row.id);
            library.entries[i]=new RecordedVoiceLibrary.Entry{id=row.id,text=row.text,originalName=row.originalName,clip=clip};
        }
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        foreach(var e in library.entries)if(RecordedVoiceLibrary.ForText(e.text)!=e.clip)throw new Exception("Text mapping failed: "+e.id);
        foreach(var e in library.entries){string caption;if(RecordedVoiceLibrary.ForOriginal(e.clip,out caption)!=e.clip||caption!=e.text)throw new Exception("Replay subtitle mapping failed: "+e.id);}
        if(RecordedVoiceLibrary.ForText("A sentence with no recording")!=null)throw new Exception("Unrecorded sentence incorrectly matched");
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/RecordedVoiceCheck.txt","PASS: "+library.entries.Length+" recordings imported with positive duration; text lookup, repeat-playback subtitles and missing-line fallback verified.");
    }
}
