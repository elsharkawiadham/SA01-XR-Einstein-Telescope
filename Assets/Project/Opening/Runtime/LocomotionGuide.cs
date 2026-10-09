using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GroupProject.Opening
{
    public sealed class LocomotionGuide : MonoBehaviour
    {
        public Transform headset;
        public StandaloneAIGuide aiGuide;
        public GameObject panel;
        public AudioSource narrator;
        public AudioClip[] steps;
        public AudioClip[] desktopSteps;
        public static readonly string[] DesktopCaptions = {
            "W / FORWARD\nPress W to walk ahead.",
            "S / BACKWARD\nPress S to walk backward.",
            "A D / SIDEWAYS\nPress A or D to step sideways.",
            "MOUSE / TURN\nMove the mouse to look around. Use the arrow keys to turn in small steps.",
            "YOUR TURN\nWalk forward. Turn left at the corner to find the elevator."
        };
        public static string CaptionForStep(int step) => Captions[Mathf.Clamp(step, 0, 4)];
        public Text instruction;
        public RectTransform leftStick, rightStick;
        public Image[] directionIndicators;
        public Vector3 panelOffset = new Vector3(-.64f, -.03f, 1.55f);
        public bool IsRunning { get; private set; }
        public int CurrentStep { get; private set; } = -1;
        public static readonly string[] Captions = {
            "LEFT STICK / FORWARD\nPush the left stick forward to walk ahead.",
            "LEFT STICK / BACKWARD\nPull the left stick back to move backward.",
            "LEFT STICK / SIDEWAYS\nMove it left or right to step sideways.",
            "RIGHT STICK / TURN\nFlick left or right to turn in small steps.",
            "YOUR TURN\nWalk forward. Turn left at the corner\nto find the elevator."
        };
        void Awake() { if(panel) panel.SetActive(false); }
        public void Begin()
        {
            if(IsRunning) return;
            StartCoroutine(Guide());
        }
        IEnumerator Guide()
        {
            IsRunning=true;
            if(panel) panel.SetActive(true);
            for(int i=0;i<steps.Length;i++)
            {
                CurrentStep=i; ShowStep(i);
                bool spoken=false;
                if(aiGuide){
                    var speech=aiGuide.SpeakInstruction(CaptionForStep(i).Substring(CaptionForStep(i).IndexOf('\n')+1).Replace("\n", " "),i);
                    while(!speech.IsCompleted)yield return null;
                    spoken=speech.Status==System.Threading.Tasks.TaskStatus.RanToCompletion && speech.Result;
                }
                var clip=steps[i];
                if(!spoken && narrator && clip)
                {
                    narrator.clip=clip;narrator.Play();
                    yield return null;
                    while(narrator.isPlaying) yield return null;
                }
                else if(!spoken) yield return new WaitForSecondsRealtime(3);
                yield return new WaitForSecondsRealtime(.3f);
            }
            if(panel) panel.SetActive(false);
            CurrentStep=-1;IsRunning=false;
        }
        public void ShowStep(int step)
        {
            CurrentStep=step;
            if(instruction) instruction.text=CaptionForStep(step);
            for(int i=0;i<directionIndicators.Length;i++)
            {
                bool active=(step==0&&i==0)||(step==1&&i==1)||(step==2&&(i==2||i==3))||(step==3&&i>=4);
                directionIndicators[i].color=active?new Color(.1f,1,.9f):new Color(.28f,.36f,.43f);
            }
        }
        void LateUpdate()
        {
            if(!panel || !panel.activeSelf || !headset) return;
            panel.transform.position=headset.TransformPoint(panelOffset);
            panel.transform.rotation=headset.rotation;
            float pulse=Mathf.Sin(Time.unscaledTime*3)*12;
            if(leftStick) leftStick.anchoredPosition=new Vector2(-125,35)+(CurrentStep==0?new Vector2(0,12+Mathf.Abs(pulse)):CurrentStep==1?new Vector2(0,-12-Mathf.Abs(pulse)):CurrentStep==2?new Vector2(pulse,0):Vector2.zero);
            if(rightStick) rightStick.anchoredPosition=new Vector2(125,35)+(CurrentStep==3?new Vector2(pulse,0):Vector2.zero);
        }
        void OnDisable()
        {
            StopAllCoroutines();
            if(narrator) narrator.Stop();
            if(panel) panel.SetActive(false);
            IsRunning=false;CurrentStep=-1;
        }
    }
}
