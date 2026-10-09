using System;
using System.Threading;
using System.Threading.Tasks;
using LLMUnity;
using PonyuDev.SherpaOnnx.Asr.Offline;
using PonyuDev.SherpaOnnx.Tts;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

namespace GroupProject.Opening
{
    /// <summary>Local-only dialogue and speech. No microphone samples are uploaded.</summary>
    public sealed class StandaloneAIGuide : MonoBehaviour
    {
        public LLM model;
        public LLMAgent agent;
        public LocomotionGuide tutorial;
        public OpeningSequence opening;
        public ElevatorCallButton elevator;
        public Transform playerRoot;
        public AudioSource speaker;
        public Canvas feedbackPanel;
        public GroupProject.Guide.ParrotCompanion companion;
        public Text feedbackText;
        public bool Ready { get; private set; }
        public bool IsBusy { get; private set; }
        public string LastReply { get; private set; }
        public string Failure { get; private set; }
        IAsrService recognition;
        ITtsService synthesis;
        readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        Task initialization, operation;
        AudioClip microphone;
        bool held, recording;
        float recordStarted;
        int speechVersion;
        Vector3 previousPosition;
        bool movedForward, movedBack, turned;
        float previousYaw;
        float forwardDistance, backwardDistance;
        public const string SystemPrompt = "Classify a player's question about a VR tutorial. Output exactly one label and nothing else. " +
            "TURN: rotating or turning. MOVE: walking, backward, sideways, locomotion. " +
            "ELEVATOR: pressing the elevator button, door or floor travel. ROUTE: where to go or finding the elevator. " +
            "PROGRESS: how the player is doing or what they completed. HELP: controls overview or asking questions. " +
            "OTHER: anything unrelated or unclear. Never provide instructions, only a label.";
        string TalkKey => "B on your right controller";
        void Awake()
        {
            if(playerRoot){previousPosition=playerRoot.position;previousYaw=playerRoot.eulerAngles.y;}
            agent.systemPrompt=SystemPrompt;
            initialization=Initialize();
        }
        async Task Initialize()
        {
            try
            {
                Status("Preparing the offline guide...");
                recognition=new AsrService { EnginePoolSize=1 };
                synthesis=new TtsService { EnginePoolSize=1 };
                await synthesis.InitializeAsync(ct:lifetime.Token);
                await recognition.InitializeAsync(ct:lifetime.Token);
                var ready=model.WaitUntilReady();
                if(await Task.WhenAny(ready,Task.Delay(90000,lifetime.Token))!=ready)throw new TimeoutException("Language model initialization timed out.");
                await ready;
                lifetime.Token.ThrowIfCancellationRequested();
                if(model.failed || !synthesis.IsReady || !recognition.IsReady)throw new InvalidOperationException("Local AI models are not ready.");
                Ready=true;Status("Hold " + TalkKey + " to ask.\nRelease to send.");
            }
            catch(Exception e)
            {
                if(!lifetime.IsCancellationRequested){Failure=e.Message;Status("Offline AI unavailable. The recorded guide is still available.");Debug.LogWarning("Standalone guide: "+e.Message);}
            }
        }
        public string SceneContext()
        {
            Vector3 p=playerRoot?playerRoot.position:Vector3.zero;
            string location=p.x < -2?"in the left corridor toward the elevator":p.z>=6?"at the corner, ready to turn left":"in the first corridor before the corner";
            return "Player location: "+location+". Elevator called: "+(elevator&&elevator.Called)+
                ". Observed forward movement: "+movedForward+"; backward movement: "+movedBack+"; root turn: "+turned+
                ". Tutorial step: "+(tutorial?tutorial.CurrentStep:-1)+".";
        }
        public Task<bool> SpeakInstruction(string facts, int step=-1)
        {
            if(IsBusy)return Task.FromResult(false);
            IsBusy=true;
            var task=Instruction(facts,step);operation=task;return task;
        }
        async Task<bool> Instruction(string facts,int step)
        {
            int version=++speechVersion;
            try
            {
                if(!Ready)
                {
                    await Task.WhenAny(initialization,Task.Delay(12000,lifetime.Token));
                    if(!Ready)return false;
                }
                string response=facts;
                if(version!=speechVersion)return true;
                // Keep required control facts visible even when the spoken wording changes.
                if(step>=0 && tutorial && tutorial.instruction) tutorial.instruction.text=LocomotionGuide.CaptionForStep(step);
                await Speak(response,version);
                return true;
            }
            catch(Exception e){if(!lifetime.IsCancellationRequested)Debug.LogWarning("AI instruction unavailable: "+e.Message);return false;}
            finally{IsBusy=false;}
        }
        async Task<string> Reply(string prompt)
        {
            var task=agent.Chat(prompt,addToHistory:false);
            if(await Task.WhenAny(task,Task.Delay(45000,lifetime.Token))!=task){if(model && model.started)agent.CancelRequests();throw new TimeoutException("AI reply timed out.");}
            string result=await task;lifetime.Token.ThrowIfCancellationRequested();
            if(string.IsNullOrWhiteSpace(result))throw new InvalidOperationException("Empty AI response.");
            return GroundedReply(prompt, result.Trim());
        }
        string GroundedReply(string question, string category)
        {
            // Never speak model-generated controller claims. Resolve only to authored facts.
            var ride = elevator ? elevator.GetComponentInParent<GroupProject.Elevator.ElevatorRide>() : null;
            string q=question.ToLowerInvariant();
            if(q.Contains("turn") || q.Contains("rotat")) category="TURN";
            else if(q.Contains("button") || q.Contains("door") || q.Contains("floor") || q.Contains("press")) category="ELEVATOR";
            else if(q.Contains("where") || q.Contains("find") || q.Contains("lost")) category="ROUTE";
            else if(q.Contains("backward") || q.Contains("forward") || q.Contains("walk") || q.Contains("strafe") || q.Contains("sideways")) category="MOVE";
            else if(q.Contains("doing") || q.Contains("progress") || q.Contains("completed")) category="PROGRESS";
            switch(category.Trim().Trim('.', '!', ' ', '\n').ToUpperInvariant())
            {
                case "TURN": return "Flick the right joystick left or right to turn in small steps. Use the left joystick to walk forward.";
                case "MOVE": return "Use the left joystick to move forward or backward. Move it left or right to step sideways.";
                case "ELEVATOR": return elevator && elevator.Called
                    ? ride ? ride.Instruction : "Enter the cabin through the open door."
                    : "Go close to the elevator. Aim at the down arrow and press the controller trigger to open the door.";
                case "ROUTE": if (ride && (ride.IsTravelling || ride.State == GroupProject.Elevator.ElevatorRide.Phase.Arrived)) return ride.Instruction; return playerRoot && playerRoot.position.x < -2
                    ? "You have reached the left corridor. Follow the floor arrows toward the elevator."
                    : "Follow the floor arrows forward to the corner. Then turn left to find the elevator.";
                case "PROGRESS": return "So far, " + (movedForward ? "you have moved forward" : "I have not yet detected forward movement") +
                    (movedBack ? ", moved backward" : "") + (turned ? ", and turned" : ". Try a small turn") +
                    (elevator && elevator.Called ? ". You have also called the elevator." : ". Follow the arrows toward the elevator.");
                case "HELP": return "Use the left joystick to walk and the right joystick to turn. Hold B on the right controller to ask a question, then release it.";
                default: return "I can help with walking, turning, finding the elevator, or your progress. What would you like to practise?";
            }
        }
        async Task Speak(string text,int version)
        {
            using(var result=await synthesis.GenerateAsync(text,lifetime.Token))
            {
                lifetime.Token.ThrowIfCancellationRequested();
                if(version!=speechVersion)return;
                var clip=result.ToAudioClip("Local guide speech");
                try
                {
                    LastReply=text;Status(text+"\n\nHold "+TalkKey+" to ask after the guide finishes.");speaker.clip=clip;speaker.Play();
                    do{await Task.Delay(30,lifetime.Token);}while(speaker && speaker.isPlaying && version==speechVersion);
                }
                finally{if(speaker){speaker.Stop();speaker.clip=null;}Destroy(clip);}
            }
        }
        public void CancelSpeech(){speechVersion++;if(speaker)speaker.Stop();}
        void Update()
        {
            if(playerRoot)
            {
                Vector3 delta=playerRoot.position-previousPosition;
                float forward=Vector3.Dot(delta,playerRoot.forward);
                if (forward > 0) forwardDistance += forward; else backwardDistance -= forward;
                movedForward |= forwardDistance > .25f; movedBack |= backwardDistance > .25f;
                turned|=Mathf.Abs(Mathf.DeltaAngle(previousYaw,playerRoot.eulerAngles.y))>10;
                previousPosition=playerRoot.position;previousYaw=playerRoot.eulerAngles.y;
            }
            bool pressed=false;
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton,out pressed);
            if(DesktopPreview.Active) pressed|=Keyboard.current!=null&&Keyboard.current.vKey.isPressed;
            if(pressed&&!held)StartRecording();
            if(!pressed&&held&&recording)FinishRecording();
            held=pressed;
            if(recording&&Time.unscaledTime-recordStarted>=12)FinishRecording();
        }
        void LateUpdate()
        {
            if(companion){if(feedbackPanel)feedbackPanel.gameObject.SetActive(false);return;}
            if(!feedbackPanel || !opening || !opening.headsetCamera)return;
            if(speaker && speaker.transform!=transform)speaker.transform.position=opening.headsetCamera.transform.position+Vector3.up*2;
            bool show=!(tutorial&&tutorial.IsRunning) && !DesktopPreview.Active;
            feedbackPanel.gameObject.SetActive(show);
            feedbackPanel.transform.position=opening.headsetCamera.transform.TransformPoint(new Vector3(-.68f,-.32f,1.55f));
            feedbackPanel.transform.rotation=opening.headsetCamera.transform.rotation;
        }
        void StartRecording()
        {
            if(!Ready){Status("Offline guide is not ready yet.");return;}
            if((companion && companion.Speaking) || IsBusy || (tutorial&&tutorial.IsRunning) || (opening.welcome&&opening.welcome.isPlaying)){Status("Please wait until the guide finishes speaking.");return;}
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
                Status("Allow microphone access, then hold B again.");return;
            }
#endif
            if(Microphone.devices.Length==0){Status("No microphone available.");return;}
            microphone=Microphone.Start(null,false,13,16000);recordStarted=Time.unscaledTime;recording=true;IsBusy=true;
            Status("Listening... release "+TalkKey+" to ask.");
        }
        void FinishRecording()
        {
            if(!recording)return;recording=false;
            int frames=Microphone.GetPosition(null);Microphone.End(null);
            var clip=microphone;microphone=null;
            if(!clip||frames<1600){IsBusy=false;if(clip)Destroy(clip);Status("I did not hear a question. Hold "+TalkKey+" and try again.");return;}
            float[] raw=new float[frames*clip.channels];clip.GetData(raw,0);
            float[] mono=new float[frames]; double energy=0;
            for(int i=0;i<frames;i++){for(int ch=0;ch<clip.channels;ch++)mono[i]+=raw[i*clip.channels+ch]/clip.channels;energy+=mono[i]*mono[i];}
            int rate=clip.frequency;Destroy(clip);
            if(energy/frames < .000001){IsBusy=false;Status("I did not hear a question. Hold "+TalkKey+" and try again.");return;}
            IsBusy=true;operation=Answer(mono,rate);
        }
        async Task Answer(float[] samples,int rate)
        {
            int version=++speechVersion;
            try
            {
                Status("Understanding your question...");
                var heard=await recognition.RecognizeAsync(samples,rate);lifetime.Token.ThrowIfCancellationRequested();
                if(heard==null||string.IsNullOrWhiteSpace(heard.Text)){Status("I did not catch that. Please try again.");return;}
                Status("You: "+heard.Text+"\nThinking...");
                string reply=await Reply(heard.Text);
                await Speak(reply,version);
            }
            catch(Exception e){if(!lifetime.IsCancellationRequested){Status("I could not answer that. Please try again.");Debug.LogWarning("Local question failed: "+e.Message);}}
            finally{IsBusy=false;}
        }
        void Status(string text){if(this&&feedbackText)feedbackText.text=text;}
        void OnApplicationPause(bool pause){if(pause){if(recording){Microphone.End(null);recording=false;IsBusy=false;if(microphone)Destroy(microphone);}CancelSpeech();}}
        async void OnDestroy()
        {
            lifetime.Cancel();if(recording)Microphone.End(null);if(microphone)Destroy(microphone);
            Ready=false;
            // LLM owns native request shutdown. Do not cancel through an agent after its model has been disposed.
            try{if(initialization!=null)await initialization;if(operation!=null)await operation;}catch{}
            recognition?.Dispose();synthesis?.Dispose();lifetime.Dispose();
        }
    }
}
