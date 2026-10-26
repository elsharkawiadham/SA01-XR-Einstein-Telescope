using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using GroupProject.Opening;

namespace GroupProject.Elevator
{
    /// <summary>Closed-cabin descent: retain the tracked XR rig while revealing the lower floor in the same scene.</summary>
    public sealed class ElevatorRide : MonoBehaviour
    {
        public enum Phase { Waiting, Boarding, Closing, Descending, Opening, Arrived, Failed }
        public ElevatorCallButton callButton;
        public Transform playerHead, playerRoot;
        public BoxCollider safeCabin;
        public Transform departureRoot;
        public TMP_Text display;
        public AudioSource motor, announcements;
        public AudioClip boardingClip, descendingClip, arrivalClip;
        public GameObject destinationRoot;
        [Min(.1f)] public float doorSeconds = 2, travelSeconds = 45;
        public Phase State { get; private set; }
        public bool IsTravelling => State == Phase.Closing || State == Phase.Descending || State == Phase.Opening;
        public string Instruction => State == Phase.Arrived ? "B1: Arrived. Leave the cabin through the open door."
            : IsTravelling ? "Going down to B1. Please stay inside the cabin."
            : State == Phase.Failed ? "Journey unavailable. Step outside and try the B1 button again."
            : "Enter the cabin, aim at B1 and press the controller trigger to go down.";
        Vector3 closedDoor;
        readonly List<Behaviour> suspended = new List<Behaviour>();
        bool announced;
        void Awake() { closedDoor = callButton.door.localPosition; if(enabled&&destinationRoot)destinationRoot.SetActive(false); }
        void Update()
        {
            if (State == Phase.Waiting && callButton.Called) State = Phase.Boarding;
            if (State == Phase.Boarding && InsideCabin() && !announced)
            { announced = true; Announce(boardingClip); }
            if (display && (State == Phase.Waiting || State == Phase.Boarding))
                display.text = "G  /  B1 (-300 m)\n" + (InsideCabin() ? "SELECT B1 TO DESCEND" : "STEP INSIDE");
        }
        public bool InsideCabin()
        {
            if (!safeCabin || !playerHead || !playerRoot) return false;
            var bounds = safeCabin.bounds;
            var head = playerHead.position; var feet = playerRoot.position;
            feet.y = head.y;
            return bounds.Contains(head) && bounds.Contains(feet);
        }
        public bool TryDepart()
        {
            if ((State != Phase.Boarding && State != Phase.Failed) || !callButton.Called || !InsideCabin()) return false;
            if (Vector3.Distance(callButton.door.localPosition, closedDoor + callButton.openOffset) > .03f) return false;
            StartCoroutine(Journey()); return true;
        }
        void Announce(AudioClip clip)
        {
            if (callButton.aiGuide) callButton.aiGuide.CancelSpeech();
            if (announcements && clip) { announcements.clip = clip; announcements.Play(); }
        }
        void SuspendMovement()
        {
            foreach (var provider in playerRoot.GetComponentsInChildren<LocomotionProvider>(true))
                if (provider.enabled) { suspended.Add(provider); provider.enabled = false; }
            var desktop = playerRoot.GetComponent<DesktopPreview>();
            if (desktop) desktop.MovementLocked = true;
        }
        void RestoreMovement()
        {
            foreach (var provider in suspended) if (provider) provider.enabled = true;
            suspended.Clear();
            if (playerRoot && playerRoot.TryGetComponent<DesktopPreview>(out var desktop)) desktop.MovementLocked = false;
        }
        IEnumerator Door(bool open, bool checkSafety = false)
        {
            Vector3 start = callButton.door.localPosition;
            Vector3 target = closedDoor + (open ? callButton.openOffset : Vector3.zero);
            for (float elapsed = 0; elapsed < doorSeconds; elapsed += Time.deltaTime)
            {
                if (checkSafety && !InsideCabin()) yield break;
                callButton.door.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, elapsed / doorSeconds));
                yield return null;
            }
            callButton.door.localPosition = target;
        }
        IEnumerator Journey()
        {
            State = Phase.Closing; callButton.ExternalDoorControl = true;
            SuspendMovement();
            if (display) display.text = "B1 SELECTED\nDOORS CLOSING";
            yield return Door(false, true);
            if (!InsideCabin())
            {
                if (display) display.text = "DOORWAY OCCUPIED\nSTEP INSIDE";
                yield return Door(true); RestoreMovement(); State = Phase.Boarding; yield break;
            }
            State = Phase.Descending; Announce(descendingClip);
            if (motor) motor.Play();
            for (float elapsed = 0; elapsed < travelSeconds; elapsed += Time.deltaTime)
            {
                if (display)
                {
                    float progress = Mathf.SmoothStep(0, 1, elapsed / travelSeconds);
                    int depth = Mathf.RoundToInt(-300 * progress);
                    display.text = "GOING DOWN / B1\n" + depth + " m / -300 m\n" + Mathf.CeilToInt(travelSeconds - elapsed) + " s";
                }
                yield return null;
            }
            if (!destinationRoot)
            {
                if(motor)motor.Stop();
                if(display)display.text="DESTINATION NOT CONFIGURED";
                yield return Door(true);RestoreMovement();State=Phase.Failed;yield break;
            }
            destinationRoot.SetActive(true);
            // Keep the cabin, tracked camera, controller interactors and AI alive. Only retire departure geometry.
            foreach (var renderer in departureRoot.GetComponentsInChildren<Renderer>(true))
                if (!renderer.transform.IsChildOf(transform)) renderer.enabled = false;
            foreach (var collider in departureRoot.GetComponentsInChildren<Collider>(true))
                if (!collider.transform.IsChildOf(transform)) collider.enabled = false;
            foreach (var light in departureRoot.GetComponentsInChildren<Light>(true))
                if (!light.transform.IsChildOf(transform)) light.enabled = false;

            if (motor) motor.Stop();
            State = Phase.Opening; if (display) display.text = "B1 / -300 m\nARRIVED";
            Announce(arrivalClip); yield return Door(true);
            State = Phase.Arrived; RestoreMovement();
        }
        void OnDisable() { StopAllCoroutines(); if (motor) motor.Stop(); RestoreMovement(); }
    }
}
