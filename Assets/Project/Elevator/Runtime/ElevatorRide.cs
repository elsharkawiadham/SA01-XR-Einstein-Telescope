using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using GroupProject.Opening;

namespace GroupProject.Elevator
{
    /// <summary>Round-trip lift. Keep the tracked rig in one cabin and swap the visible landing geometry.</summary>
    public sealed class ElevatorRide : MonoBehaviour
    {
        public enum Phase { Waiting, Boarding, Closing, Descending, Opening, Arrived, Failed, Ascending, GroundArrived }
        public ElevatorCallButton callButton;
        public Transform playerHead, playerRoot;
        public BoxCollider safeCabin;
        public Transform departureRoot;
        public TMP_Text display,overheadFloor;
        public float DescentProgress {get;private set;}
        public bool AtB1 {get;private set;}
        public bool GoingUp {get;private set;}
        public bool AtLanding=>State==Phase.Arrived||State==Phase.GroundArrived;
        public string TargetFloor=>GoingUp?"G":"B1";
        public string FloorCaption => State==Phase.Descending||State==Phase.Ascending?(GoingUp?"B1  >  G":"G  >  B1")+"\n"+Mathf.RoundToInt(-300*DescentProgress)+" m":AtB1?"B1  /  -300 m":"G  /  GROUND";
        public AudioSource motor, announcements;
        public AudioClip boardingClip, descendingClip, arrivalClip;
        public GameObject destinationRoot;
        [Min(.1f)] public float doorSeconds = 2, travelSeconds = 45;
        public Phase State { get; private set; }
        public bool IsTravelling => State == Phase.Closing || State == Phase.Descending || State == Phase.Ascending || State == Phase.Opening;
        public string Instruction => State == Phase.Arrived ? "B1: leave the cabin, or select G inside to return to the ground floor."
            : State==Phase.GroundArrived?"G: ground floor. Leave through the open door, or select B1 to go down again."
            : IsTravelling ? (GoingUp?"Returning to ground floor. Please stay inside the cabin.":"Going down to B1. Please stay inside the cabin.")
            : State == Phase.Failed ? "Journey unavailable. Step outside and try the B1 button again."
            : "Enter the cabin, aim at B1 and press the controller trigger to go down.";
        Vector3 closedDoor;
        readonly List<Behaviour> suspended = new List<Behaviour>();
        readonly Dictionary<Renderer,bool> departureRenderers=new Dictionary<Renderer,bool>();
        readonly Dictionary<Collider,bool> departureColliders=new Dictionary<Collider,bool>();
        readonly Dictionary<Light,bool> departureLights=new Dictionary<Light,bool>();
        readonly Dictionary<Transform,Transform> wornParents=new Dictionary<Transform,Transform>();
        bool announced;
        void Awake() { closedDoor = callButton.door.localPosition; if(enabled&&destinationRoot)destinationRoot.SetActive(false); }
        void Update()
        {
            if (State == Phase.Waiting && callButton.Called) State = Phase.Boarding;
            if (State == Phase.Boarding && InsideCabin() && !announced)
            { announced = true; StartCoroutine(BoardingInstruction()); }
            if(overheadFloor)overheadFloor.text=FloorCaption;
            if (display && (State == Phase.Waiting || State == Phase.Boarding))
                display.text = "G  /  B1 (-300 m)\n" + (InsideCabin() ? "SELECT B1 TO DESCEND" : "STEP INSIDE");
            else if(display&&AtLanding)display.text=AtB1?"B1 / -300 m\nSELECT G TO RETURN":"G / GROUND FLOOR\nSELECT B1 TO DESCEND";
        }
        IEnumerator BoardingInstruction()
        {
            foreach(var line in new[]{"This lift connects the ground floor with B1, where cleanroom preparation begins."})
            {
                if(State!=Phase.Boarding)yield break;
                var guide=callButton.aiGuide;bool delivered=false;
                if(guide&&guide.Ready&&!guide.IsBusy){var speech=guide.SpeakInstruction(line);while(!speech.IsCompleted&&State==Phase.Boarding)yield return null;if(State!=Phase.Boarding)yield break;delivered=speech.Status==System.Threading.Tasks.TaskStatus.RanToCompletion&&speech.Result;}
                if(!delivered){var parrot=FindAnyObjectByType<GroupProject.Guide.ParrotCompanion>();if(parrot)parrot.PresentCaption(line,7);float until=Time.unscaledTime+7;while(Time.unscaledTime<until&&State==Phase.Boarding)yield return null;}
            }
        }
        public bool InsideCabin()
        {
            if (!safeCabin || !playerHead || !playerRoot) return false;
            var bounds = safeCabin.bounds;
            var head = playerHead.position; var feet = playerRoot.position;
            feet.y = bounds.center.y; head.y = bounds.center.y;
            return bounds.Contains(head) && bounds.Contains(feet);
        }
        public bool TryDepart()
        {
            if ((State != Phase.Boarding && State != Phase.Failed && !AtLanding) || !callButton.Called || !InsideCabin()) return false;
            if (Vector3.Distance(callButton.door.localPosition, closedDoor + callButton.openOffset) > .03f) return false;
            GoingUp=AtB1;
            var source=State;State=Phase.Closing; // Reject duplicate input events synchronously.
            StartCoroutine(Journey(source)); return true;
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
        void HideDeparture()
        {
            departureRenderers.Clear();departureColliders.Clear();departureLights.Clear();
            foreach(var r in departureRoot.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(transform)){departureRenderers[r]=r.enabled;r.enabled=false;}
            foreach(var c in departureRoot.GetComponentsInChildren<Collider>(true))if(!c.transform.IsChildOf(transform)){departureColliders[c]=c.enabled;c.enabled=false;}
            foreach(var l in departureRoot.GetComponentsInChildren<Light>(true))if(!l.transform.IsChildOf(transform)){departureLights[l]=l.enabled;l.enabled=false;}
        }
        void RestoreDeparture()
        {foreach(var pair in departureRenderers)if(pair.Key)pair.Key.enabled=pair.Value;foreach(var pair in departureColliders)if(pair.Key)pair.Key.enabled=pair.Value;foreach(var pair in departureLights)if(pair.Key)pair.Key.enabled=pair.Value;}
        void KeepWornClothes()
        {foreach(var garment in destinationRoot.GetComponentsInChildren<GroupProject.Preparation.WearableGarment>(true))if(garment.Equipped){wornParents[garment.transform]=garment.transform.parent;garment.transform.SetParent(playerRoot,true);}}
        void RestoreWornParents()
        {foreach(var pair in wornParents)if(pair.Key&&pair.Value)pair.Key.SetParent(pair.Value,true);wornParents.Clear();}
        void AnnounceText(string words)
        {if(announcements)announcements.Stop();var guide=callButton.aiGuide;if(guide&&guide.Ready&&!guide.IsBusy)_=guide.SpeakInstruction(words);var parrot=FindAnyObjectByType<GroupProject.Guide.ParrotCompanion>();if(parrot)parrot.PresentCaption(words,7);}
        IEnumerator Journey(Phase source)
        {
            State = Phase.Closing; callButton.ExternalDoorControl = true;
            // Keep movement available inside the enclosed cabin.
            if (display) display.text = TargetFloor+" SELECTED\nDOORS CLOSING";
            yield return Door(false, true);
            if (!InsideCabin())
            {
                if (display) display.text = "DOORWAY OCCUPIED\nSTEP INSIDE";
                yield return Door(true); RestoreMovement(); State = source==Phase.Failed?Phase.Boarding:source; yield break;
            }
            State = GoingUp?Phase.Ascending:Phase.Descending;
            if(GoingUp)AnnounceText("Doors closing. Returning to the ground floor.");else Announce(descendingClip);
            if (motor) motor.Play();
            for (float elapsed = 0; elapsed < travelSeconds; elapsed += Time.deltaTime)
            {
                float progress=Mathf.SmoothStep(0,1,elapsed/travelSeconds);DescentProgress=GoingUp?1-progress:progress;
                if (display)
                {
                    int depth = Mathf.RoundToInt(-300 * DescentProgress);
                    display.text = (GoingUp?"GOING UP / G":"GOING DOWN / B1")+"\n" + depth + " m\n" + Mathf.CeilToInt(travelSeconds - elapsed) + " s";
                }
                yield return null;
            }
            if (!destinationRoot)
            {
                if(motor)motor.Stop();
                if(display)display.text="DESTINATION NOT CONFIGURED";
                yield return Door(true);RestoreMovement();State=Phase.Failed;yield break;
            }
            AtB1=!GoingUp;DescentProgress=AtB1?1:0;
            if(AtB1){RestoreWornParents();destinationRoot.SetActive(true);HideDeparture();}
            else{KeepWornClothes();destinationRoot.SetActive(false);RestoreDeparture();}

            if (motor) motor.Stop();
            State = Phase.Opening; if (display) display.text = (AtB1?"B1 / -300 m":"G / GROUND FLOOR")+"\nARRIVED";
            if(AtB1)Announce(arrivalClip);else AnnounceText("Ground floor. The doors are opening.");yield return Door(true);
            State = AtB1?Phase.Arrived:Phase.GroundArrived; RestoreMovement();
        }
        void OnDisable() { StopAllCoroutines(); if (motor) motor.Stop(); RestoreMovement(); }
    }
}
