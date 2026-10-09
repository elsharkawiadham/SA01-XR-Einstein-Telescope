using System;
using TMPro;
using UnityEngine;
using GroupProject.Opening;
namespace GroupProject.Guide
{
    [DefaultExecutionOrder(100)]
    public sealed class ParrotCompanion : MonoBehaviour
    {
        [Serializable] public struct Line { public AudioClip clip; [TextArea] public string text; }
        public Transform head;
        public StandaloneAIGuide ai;
        public AudioSource[] narrators;
        public Line[] lines;
        public AudioSource voice;
        public TMP_Text dialogue;
        public ParrotGraphic bird;
        public Vector3 offset=new Vector3(-.60f,-.38f,1.65f);
        public bool Speaking => voice && voice.isPlaying;
        public string CurrentDialogue => dialogue ? dialogue.text : "";
        float[] volumes,samples=new float[128];
        bool[] wasPlaying;AudioClip[] clips;
        AudioSource active;AudioClip mirrored;float lastSpeech;
        void Awake()
        {
            volumes=new float[narrators.Length];wasPlaying=new bool[narrators.Length];clips=new AudioClip[narrators.Length];
            for(int i=0;i<narrators.Length;i++)if(narrators[i]){volumes[i]=narrators[i].volume;narrators[i].volume=0;}
        }
        void LateUpdate()
        {
            if(!head)return;
            transform.position=head.TransformPoint(offset);transform.rotation=head.rotation;
            if(bird)bird.rectTransform.anchoredPosition=new Vector2(0,-150+Mathf.Sin(Time.unscaledTime*2.2f)*6);
            // Original channels retain playback timing/cancellation; only the parrot channel is audible.
            for(int i=0;i<narrators.Length;i++)
            {
                var source=narrators[i];if(!source)continue;source.volume=0;
                if(source.isPlaying && (!wasPlaying[i]||clips[i]!=source.clip))
                {
                    active=source;mirrored=source.clip;voice.Stop();voice.clip=mirrored;voice.pitch=source.pitch;voice.volume=Mathf.Max(.65f,volumes[i]);
                    if(mirrored){voice.timeSamples=Mathf.Clamp(source.timeSamples,0,mirrored.samples-1);voice.Play();}
                    string text=ai && source==ai.speaker ? ai.LastReply : null;
                    if(string.IsNullOrEmpty(text))foreach(var line in lines)if(line.clip==mirrored){text=line.text;break;}
                    if(!string.IsNullOrEmpty(text))dialogue.text=text;
                }
                wasPlaying[i]=source.isPlaying;clips[i]=source.clip;
            }
            if(active && (!active.isPlaying || active.clip!=mirrored)){voice.Stop();active=null;}
            float amplitude=0;
            if(voice.isPlaying){voice.GetOutputData(samples,0);foreach(float value in samples)amplitude+=value*value;amplitude=Mathf.Clamp01(Mathf.Sqrt(amplitude/samples.Length)*12);lastSpeech=Time.unscaledTime;}
            else if(Time.unscaledTime-lastSpeech>3 && dialogue)dialogue.text=ai && ai.feedbackText ? ai.feedbackText.text : "Hold B on your right controller to ask me a question.";
            if(bird)bird.speech=Mathf.Lerp(bird.speech,amplitude,Time.unscaledDeltaTime*20);
        }
        void OnDisable()
        {
            if(voice)voice.Stop();
            if(volumes!=null)for(int i=0;i<narrators.Length;i++)if(narrators[i])narrators[i].volume=volumes[i];
        }
    }
}
