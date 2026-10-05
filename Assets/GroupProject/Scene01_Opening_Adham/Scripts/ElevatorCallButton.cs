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
        public AudioSource welcomeVoice;
        public Transform door;
        public Vector3 openOffset = new Vector3(0, 3.1f, 0);
        [Min(.1f)] public float activationDistance = 2f;
        [Min(.1f)] public float hintDelay = 3f;
        [Min(.1f)] public float openingSeconds = 2f;
        public UnityEvent onCalled = new UnityEvent();
        public bool Called { get; private set; }
        public bool HintPlayed { get; private set; }
        public bool Nearby { get; private set; }
        Vector3 closedPosition;
        float waiting, opening;
        void Awake()
        {
            if (door) closedPosition = door.localPosition;
            if (proximityVisual) proximityVisual.SetActive(false);
            if (indicator && readyMaterial) indicator.sharedMaterial = readyMaterial;
        }
        void OnEnable() { GetComponent<XRSimpleInteractable>().selectEntered.AddListener(Selected); }
        void OnDisable()
        {
            GetComponent<XRSimpleInteractable>().selectEntered.RemoveListener(Selected);
            waiting = 0;
            if (hintVoice) hintVoice.Stop();
        }
        void Selected(SelectEnterEventArgs args) { TryCall(); }
        public bool TryCall()
        {
            if (Called || !IsInRange()) return false;
            Called = true;
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
        void Update()
        {
            Nearby = IsInRange();
            if (proximityVisual) proximityVisual.SetActive(Nearby || Called);
            if (Called)
            {
                opening = Mathf.Min(opening + Time.deltaTime, openingSeconds);
                if (door) door.localPosition = closedPosition + openOffset * Mathf.SmoothStep(0, 1, opening / openingSeconds);
                return;
            }
            if (!Nearby)
            {
                waiting = 0;
                if (hintVoice && hintVoice.isPlaying) hintVoice.Stop();
                return;
            }
            if (HintPlayed || (welcomeVoice && welcomeVoice.isPlaying)) return;
            waiting += Time.deltaTime;
            if (waiting >= hintDelay && hintVoice && hintVoice.clip)
            {
                HintPlayed = true;
                hintVoice.Play();
            }
        }
    }
}
