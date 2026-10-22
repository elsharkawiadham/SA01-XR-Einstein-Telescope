using System;
using System.Text.RegularExpressions;
using UnityEngine;
namespace GroupProject.Guide
{
    public sealed class RecordedVoiceLibrary : ScriptableObject
    {
        [Serializable] public struct Entry { public string id,text,originalName; public AudioClip clip; }
        public Entry[] entries;
        static RecordedVoiceLibrary library;
        static RecordedVoiceLibrary Library => library?library:(library=Resources.Load<RecordedVoiceLibrary>("RecordedVoiceLibrary"));
        static string Key(string value)=>Regex.Replace((value??"").Trim(),@"\s+"," ").ToLowerInvariant();
        public static AudioClip ForText(string text)
        {var l=Library;if(l)foreach(var e in l.entries)if(Key(e.text)==Key(text))return e.clip;return null;}
        public static AudioClip ForOriginal(AudioClip original,out string text)
        {text=null;var l=Library;if(l&&original)foreach(var e in l.entries)if(e.clip==original||(!string.IsNullOrEmpty(e.originalName)&&e.originalName==original.name)){text=e.text;return e.clip;}return null;}
    }
}
