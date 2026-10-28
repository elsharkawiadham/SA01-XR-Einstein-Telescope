using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Elevator
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class ElevatorFloorButton : MonoBehaviour
    {
        public ElevatorRide ride;
        public float reach = 2f;
        public TMPro.TMP_Text instruction;
        void Update() { if(instruction) instruction.text = ride.IsTravelling?"B1 SELECTED":ride.State==ElevatorRide.Phase.Arrived?"B1 ARRIVED":"B1\nAIM + TRIGGER";var cue=GetComponent<GroupProject.Guide.InteractionFeedback>();if(cue)cue.SetLatched(ride.IsTravelling||ride.State==ElevatorRide.Phase.Arrived); }
        public bool InRange => ride && ride.playerHead && Vector3.Distance(ride.playerHead.position, transform.position) <= reach;
        void OnEnable() { GetComponent<XRSimpleInteractable>().selectEntered.AddListener(Select); GetComponent<XRSimpleInteractable>().activated.AddListener(Activate); }
        void OnDisable() { GetComponent<XRSimpleInteractable>().selectEntered.RemoveListener(Select); GetComponent<XRSimpleInteractable>().activated.RemoveListener(Activate); }
        void Activate(ActivateEventArgs args) { TryPress(); }
        void Select(SelectEnterEventArgs args) { TryPress(); }
        public bool TryPress() { bool accepted=InRange && ride.TryDepart();if(accepted)GetComponent<GroupProject.Guide.InteractionFeedback>()?.Pulse(true);return accepted; }
    }
}
