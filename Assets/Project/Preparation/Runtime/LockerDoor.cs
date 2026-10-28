using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Preparation
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class LockerDoor : MonoBehaviour
    {
        public Transform hinge;
        public float openAngle=105;
        public bool IsOpen { get; private set; }
        float nextToggle;
        void OnEnable(){GetComponent<XRSimpleInteractable>().selectEntered.AddListener(Select);GetComponent<XRSimpleInteractable>().activated.AddListener(Activate);}
        void OnDisable(){GetComponent<XRSimpleInteractable>().selectEntered.RemoveListener(Select);GetComponent<XRSimpleInteractable>().activated.RemoveListener(Activate);}
        void Select(SelectEnterEventArgs e){Toggle();}
        void Activate(ActivateEventArgs e){Toggle();}
        public void Toggle(){if(Time.unscaledTime<nextToggle)return;nextToggle=Time.unscaledTime+.3f;IsOpen=!IsOpen;var cue=GetComponent<GroupProject.Guide.InteractionFeedback>();if(cue){cue.SetLatched(IsOpen);cue.Pulse(true);}}
        void Update(){if(hinge)hinge.localRotation=Quaternion.RotateTowards(hinge.localRotation,Quaternion.Euler(0,IsOpen?openAngle:0,0),Time.deltaTime*110);}
    }
}
