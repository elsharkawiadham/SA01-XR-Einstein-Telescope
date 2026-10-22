using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Preparation
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PreparationButton : MonoBehaviour
    {
        public PreparationJourney journey;public int id;
        void OnEnable(){GetComponent<XRSimpleInteractable>().selectEntered.AddListener(Selected);}
        void OnDisable(){GetComponent<XRSimpleInteractable>().selectEntered.RemoveListener(Selected);}
        void Selected(SelectEnterEventArgs e){Press();}
        public void Press(){journey.Select(id);if(id<4)GetComponent<Renderer>().material.color=Color.green;}
    }
}
