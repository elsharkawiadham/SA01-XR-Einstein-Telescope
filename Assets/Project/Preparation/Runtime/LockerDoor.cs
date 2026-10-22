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
        void OnEnable(){GetComponent<XRSimpleInteractable>().selectEntered.AddListener(Select);}
        void OnDisable(){GetComponent<XRSimpleInteractable>().selectEntered.RemoveListener(Select);}
        void Select(SelectEnterEventArgs e){Toggle();}
        public void Toggle(){IsOpen=!IsOpen;}
        void Update(){if(hinge)hinge.localRotation=Quaternion.RotateTowards(hinge.localRotation,Quaternion.Euler(0,IsOpen?openAngle:0,0),Time.deltaTime*110);}
    }
}
