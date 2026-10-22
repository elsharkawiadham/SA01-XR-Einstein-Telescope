using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Cleaning
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class CleaningContinueButton : MonoBehaviour
    {
        public CleaningStation station;
        void OnEnable(){GetComponent<XRSimpleInteractable>().activated.AddListener(Press);}
        void OnDisable(){GetComponent<XRSimpleInteractable>().activated.RemoveListener(Press);}
        void Press(ActivateEventArgs args){station.Continue();}
    }
}
