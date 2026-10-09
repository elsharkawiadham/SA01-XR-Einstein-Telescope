using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GroupProject.Opening
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class ElevatorCallButton : MonoBehaviour
    {
        public Transform playerHead;
        public GameObject proximityVisual;
        public Renderer indicator;
        public Material readyMaterial;
        public Material calledMaterial;
        public AudioSource hintVoice;
        public AudioClip desktopHintClip;
        public AudioSource welcomeVoice;
        public LocomotionGuide locomotionGuide;
        public StandaloneAIGuide aiGuide;
        public Transform door;
        public Vector3 openOffset = new Vector3(0, 3.1f, 0);
        [Min(.1f)] public float activationDistance = 2f;
        [Min(.1f)] public float hintDelay = 3f;
        [Min(.1f)] public float openingSeconds = 2f;
        public UnityEvent onCalled = new UnityEvent();
        public bool Called { get; private set; }
        public bool HintPlayed { get; private set; }
        public bool Nearby { get; private set; }
        public bool ExternalDoorControl { get; set; }
        Vector3 closedPosition;
        float waiting, opening;
        bool aiHintSpeaking;
        void Awake()
        {
            if (door) closedPosition = door.localPosition;
            if (proximityVisual) proximityVisual.SetActive(false);
            if (indicator && readyMaterial) indicator.sharedMaterial = readyMaterial;
        }
        void OnEnable() { GetComponent<XRSimpleInteractable>().selectEntered.AddListener(Selected); GetComponent<XRSimpleInteractable>().activated.AddListener(Activated); }
        void OnDisable()
        {
            GetComponent<XRSimpleInteractable>().selectEntered.RemoveListener(Selected);
            GetComponent<XRSimpleInteractable>().activated.RemoveListener(Activated);
            waiting = 0;
            if (hintVoice) hintVoice.Stop();
        }
        void Activated(ActivateEventArgs args) { TryCall(); }
        void Selected(SelectEnterEventArgs args) { TryCall(); }
        public bool TryCall()
        {
            if (Called || !IsInRange()) return false;
            Called = true;
            if(aiGuide)aiGuide.CancelSpeech();
            if (hintVoice) hintVoice.Stop();
            if (indicator && calledMaterial) indicator.sharedMaterial = calledMaterial;
            if (proximityVisual) proximityVisual.SetActive(true);
            onCalled.Invoke();
            return true;
        }
        bool IsInRange()
        {
            if (!playerHead) return false;
            Vector3 delta = playerHead.position - transform.position;
            // Horizontal distance supports both seated and standing players.
            delta.y = 0;
            return delta.sqrMagnitude <= activationDistance * activationDistance;
        }
        async void SpeakHint()
        {
            aiHintSpeaking=true;
            bool spoken=await aiGuide.SpeakInstruction("To push the button, aim at the down arrow and click with the controller trigger.");
            aiHintSpeaking=false;
            if(this && !Called && Nearby && !spoken && hintVoice)hintVoice.Play();
        }
        void Update()
        {
            Nearby = IsInRange();
            if (proximityVisual) proximityVisual.SetActive(Nearby || Called);
            if (Called)
            {
                if (ExternalDoorControl) return;
                opening = Mathf.Min(opening + Time.deltaTime, openingSeconds);
                if (door) door.localPosition = closedPosition + openOffset * Mathf.SmoothStep(0, 1, opening / openingSeconds);
                return;
            }
            if (!Nearby)
            {
                waiting = 0;
                if(aiHintSpeaking && aiGuide)aiGuide.CancelSpeech();
                if (hintVoice && hintVoice.isPlaying) hintVoice.Stop();
                return;
            }
            if (HintPlayed || (welcomeVoice && welcomeVoice.isPlaying) || (locomotionGuide && locomotionGuide.IsRunning) || (aiGuide && aiGuide.IsBusy)) return;
            waiting += Time.deltaTime;
            if (waiting >= hintDelay && hintVoice && hintVoice.clip)
            {
                HintPlayed = true;
                if(aiGuide && aiGuide.Ready) SpeakHint();
                else hintVoice.Play();
            }
        }
    }
}
