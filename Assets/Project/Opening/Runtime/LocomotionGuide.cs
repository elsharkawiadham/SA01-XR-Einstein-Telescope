using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace GroupProject.Opening
{
    [DefaultExecutionOrder(-80)]
    public sealed class LocomotionGuide : MonoBehaviour
    {
        public Transform headset;
        public StandaloneAIGuide aiGuide;
        public GameObject panel;
        public AudioSource narrator;
        public AudioClip[] steps,desktopSteps;
        public Text instruction;
        public RectTransform leftStick,rightStick;
        public Image[] directionIndicators;
        public Vector3 panelOffset=new Vector3(0,0,1.5f);
        public bool IsRunning {get;private set;}
        public bool Completed {get;private set;}
        public bool MovementRestricted=>isActiveAndEnabled&&!Completed;
        public int CurrentStep {get;private set;}=-1;
        public bool ActionComplete {get;private set;}
        public static LocomotionGuide Active {get;private set;}
        public static bool Locked=>Active&&Active.MovementRestricted;
        public static readonly string[] Captions={
            "LEFT STICK / FORWARD\nPush the left stick forward, then let it return to the centre.",
            "LEFT STICK / BACKWARD\nPull the left stick backward, then release it.",
            "LEFT STICK / LEFT\nPush the left stick left, then release it.",
            "LEFT STICK / RIGHT\nPush the left stick right, then release it.",
            "RIGHT STICK / TURN LEFT\nPush the right stick left, then release it.",
            "RIGHT STICK / TURN RIGHT\nPush the right stick right, then release it.",
            "LEFT TRIGGER / SELECT\nPress and release the left index-finger trigger. Use triggers to select buttons and doors.",
            "RIGHT TRIGGER / SELECT\nPress and release the right index-finger trigger.",
            "LEFT GRIP / PICK UP\nSqueeze the left side grip, then release it. Hold grip to carry clothing; release to fit it.",
            "RIGHT GRIP / PICK UP\nSqueeze the right side grip, then release it.",
            "RIGHT A / TELEPORT\nHold A on the right controller to aim at a clear floor. Release A to teleport. Try holding and releasing A now.",
            "RIGHT B / ASK THE GUIDE\nHold B on the right controller while speaking, then release to ask. Try holding and releasing B now.",
            "LEFT X / CHECKLIST\nPress and release X on the left controller to hide or show your checklist."
        };
        public static string CaptionForStep(int step)=>Captions[Mathf.Clamp(step,0,Captions.Length-1)];
        readonly Dictionary<Behaviour,bool> suspended=new Dictionary<Behaviour,bool>();
        readonly List<Image> progress=new List<Image>();
        readonly List<Image> controls=new List<Image>();
        readonly Dictionary<Image,Color> controlColors=new Dictionary<Image,Color>();
        readonly bool[] done=new bool[13];
        Transform player;DesktopPreview desktop;StartingViewHeight height;Text heading,status;
        bool neutral,pressed,accepting;float held;Vector3 lockedPosition;Quaternion lockedRotation;
        void Awake(){
            Active=this;var origin=headset?headset.GetComponentInParent<XROrigin>():null;player=origin?origin.transform:null;
            if(panel)panel.SetActive(false);
            if(!player)return;
            desktop=player.GetComponent<DesktopPreview>();height=player.GetComponent<StartingViewHeight>();if(!height)height=player.gameObject.AddComponent<StartingViewHeight>();
            if(!player.GetComponent<FacilityTeleport>())player.gameObject.AddComponent<FacilityTeleport>();
            if(!player.GetComponent<FacilityLocomotion>())player.gameObject.AddComponent<FacilityLocomotion>();
            lockedPosition=player.position;lockedRotation=player.rotation;if(enabled)Lock();
        }
        void Lock(){
            if(!player)return;
            foreach(var b in player.GetComponentsInChildren<Behaviour>(true))if(b is LocomotionProvider||b is XRBaseInteractor){if(!suspended.ContainsKey(b))suspended[b]=b.enabled;b.enabled=false;}
            if(desktop)desktop.MovementLocked=true;
        }
        void Unlock(){foreach(var p in suspended)if(p.Key)p.Key.enabled=p.Value;suspended.Clear();if(desktop)desktop.MovementLocked=false;if(player&&player.TryGetComponent<FacilityLocomotion>(out var locomotion))locomotion.Refresh();}
        public void Begin(){if(IsRunning||Completed)return;IsRunning=true;StartCoroutine(Guide());}
        IEnumerator Guide(){
            while(height&&!height.Calibrated)yield return null;
            BuildPanel();
            for(int i=0;i<Captions.Length;i++){
                ShowStep(i);neutral=pressed=false;held=0;ActionComplete=false;accepting=true;
                var words=SpokenForStep(i);
                var recorded=GroupProject.Guide.RecordedVoiceLibrary.ForText(words);
                bool spoken=false;
                if(recorded&&narrator){narrator.clip=recorded;narrator.pitch=aiGuide?aiGuide.recordedSpeechRate:.9f;narrator.Play();spoken=true;}
                else if(aiGuide){var speech=aiGuide.SpeakInstruction(words);while(!speech.IsCompleted)yield return null;spoken=speech.Status==System.Threading.Tasks.TaskStatus.RanToCompletion&&speech.Result;}
                if(!spoken&&narrator){var fallback=Resources.Load<AudioClip>("Onboarding/Step"+i);if(fallback){narrator.clip=fallback;narrator.pitch=1;narrator.Play();}}
                while(!ActionComplete)yield return null;
                accepting=false;done[i]=true;progress[i].color=new Color(.2f,1,.45f);RefreshControls();
                status.text="Well done";status.color=new Color(.2f,1,.45f);
                while(narrator&&narrator.isPlaying)yield return null;
                yield return new WaitForSecondsRealtime(.45f);
            }
            status.text="All controls checked. Release the controls to begin.";
            while(!AllReleased(TrainingControls.Read()))yield return null;
            Completed=true;IsRunning=false;CurrentStep=-1;Unlock();
            heading.text="YOU ARE READY";instruction.text="Follow the floor arrows to the elevator.";
            yield return new WaitForSecondsRealtime(2);if(panel)panel.SetActive(false);
        }
        static bool AllReleased(TrainingControls s)=>s.move.magnitude<.2f&&s.turn.magnitude<.2f&&!s.leftTrigger&&!s.rightTrigger&&!s.leftGrip&&!s.rightGrip&&!s.teleport&&!s.speak&&!s.checklist;
        public void Observe(TrainingControls s,float delta){
            if(!accepting||ActionComplete)return;
            float value=CurrentStep switch {0=>s.move.y,1=>-s.move.y,2=>-s.move.x,3=>s.move.x,4=>-s.turn.x,5=>s.turn.x,6=>s.leftTrigger?1:0,7=>s.rightTrigger?1:0,8=>s.leftGrip?1:0,9=>s.rightGrip?1:0,10=>s.teleport?1:0,11=>s.speak?1:0,12=>s.checklist?1:0,_=>0};
            bool released=CurrentStep<4?s.move.magnitude<.2f:CurrentStep<6?s.turn.magnitude<.2f:value<.2f;
            if(!neutral){if(released)neutral=true;return;}
            if(value>.65f){held+=Mathf.Max(0,delta);if(CurrentStep==6||CurrentStep==7||CurrentStep==12||held>=.18f)pressed=true;}else if(!pressed)held=0;
            if(pressed&&released)ActionComplete=true;
        }
        void Update(){if(MovementRestricted){Lock();if(player){player.SetPositionAndRotation(lockedPosition,lockedRotation);}Observe(TrainingControls.Read(),Time.unscaledDeltaTime);}}
        void LateUpdate(){
            if(MovementRestricted&&player)player.SetPositionAndRotation(lockedPosition,lockedRotation);
            if(!IsRunning||!panel||!panel.activeSelf)return;
            float pulse=12+Mathf.Abs(Mathf.Sin(Time.unscaledTime*3)*12);
            if(leftStick)leftStick.anchoredPosition=new Vector2(-125,35)+(CurrentStep==0?Vector2.up:CurrentStep==1?Vector2.down:CurrentStep==2?Vector2.left:CurrentStep==3?Vector2.right:Vector2.zero)*pulse;
            if(rightStick)rightStick.anchoredPosition=new Vector2(125,35)+(CurrentStep==4?Vector2.left:CurrentStep==5?Vector2.right:Vector2.zero)*pulse;
        }
        public static string SpokenForStep(int step)=>step switch {
            0=>"Push the left stick forward to walk ahead.",1=>"Pull the left stick back to move backward.",
            2 or 3=>"Move it left or right to step sideways.",_=>CaptionForStep(step).Split('\n')[1]};
        public void ShowStep(int step){CurrentStep=step;if(instruction){instruction.text=CaptionForStep(step);instruction.color=Color.white;}if(status){status.text="TRY + RELEASE  /  "+(step+1)+" OF "+Captions.Length;status.color=Color.white;}RefreshControls();}
        void RefreshControls(){
            foreach(var entry in controlColors)entry.Key.color=entry.Value;
            for(int i=0;i<controls.Count;i++)if(done[i]&&controls[i])controls[i].color=new Color(.2f,1,.45f);
            if(CurrentStep>=0&&CurrentStep<controls.Count&&!done[CurrentStep]&&controls[CurrentStep])controls[CurrentStep].color=new Color(.1f,1,.9f);
        }
        void BuildPanel(){
            // Reuse the authored controller artwork, stick animations, fonts and background.
            if(!panel||!instruction){Debug.LogError("Original controller tutorial panel is missing.",this);return;}
            panel.transform.SetParent(null,true);
            var yaw=Quaternion.Euler(0,headset.eulerAngles.y,0);
            panel.transform.SetPositionAndRotation(headset.position+yaw*new Vector3(0,0,1.5f),yaw);
            heading=panel.transform.Find("Title").GetComponent<Text>();status=panel.transform.Find("Device").GetComponent<Text>();
            heading.text="CONTROLLER CHECK";instruction.resizeTextForBestFit=true;instruction.resizeTextMinSize=16;instruction.resizeTextMaxSize=22;
            for(int i=0;i<6;i++)controls.Add(directionIndicators[i]);
            controls.Add(Marker("Left trigger",new Vector2(-165,78),new Vector2(30,16),"TRG"));
            controls.Add(Marker("Right trigger",new Vector2(165,78),new Vector2(30,16),"TRG"));
            var grips=new List<Image>();var faces=new List<Image>();
            foreach(var graphic in panel.GetComponentsInChildren<Image>(true)){
                if(graphic.name=="Controller grip")grips.Add(graphic);
                if(graphic.name=="Face button")faces.Add(graphic);
            }
            grips.Sort((x,y)=>x.rectTransform.anchoredPosition.x.CompareTo(y.rectTransform.anchoredPosition.x));
            controls.Add(grips[0]);controls.Add(grips[1]);
            Image aButton=null,bButton=null,xButton=null;
            foreach(var button in faces){var pos=button.rectTransform.anchoredPosition;string label=pos.x>0?(pos.y<0?"A":"B"):(pos.y<0?"X":"Y");
                var text=Label(label,button.transform,Vector2.zero,new Vector2(17,17),11);text.color=Color.white;
                if(label=="A")aButton=button;if(label=="B")bButton=button;if(label=="X")xButton=button;
            }
            controls.Add(aButton);controls.Add(bButton);controls.Add(xButton);
            foreach(var graphic in controls)if(graphic&&!controlColors.ContainsKey(graphic))controlColors[graphic]=graphic.color;
            for(int i=0;i<Captions.Length;i++)progress.Add(Marker("Control check "+(i+1),new Vector2((i-6)*30,140),new Vector2(22,14),(i+1).ToString()));
            panel.SetActive(true);RefreshControls();
        }
        Image Marker(string name,Vector2 p,Vector2 size,string label){
            var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(panel.transform,false);var rt=(RectTransform)g.transform;rt.anchoredPosition=p;rt.sizeDelta=size;
            var image=g.AddComponent<Image>();image.color=new Color(.28f,.36f,.43f);image.raycastTarget=false;
            Label(label,g.transform,Vector2.zero,size,10);return image;
        }
        Text Label(string text,Transform parent,Vector2 p,Vector2 size,int fontSize){var g=new GameObject(text,typeof(RectTransform));g.transform.SetParent(parent,false);var rt=(RectTransform)g.transform;rt.sizeDelta=size;rt.anchoredPosition=p;var label=g.AddComponent<Text>();label.font=instruction.font;label.fontSize=fontSize;label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;label.text=text;label.raycastTarget=false;return label;}
        void OnDisable(){StopAllCoroutines();if(narrator)narrator.Stop();if(panel)panel.SetActive(false);Unlock();IsRunning=false;}
        void OnDestroy(){if(Active==this)Active=null;if(panel)Destroy(panel);}
    }
}
