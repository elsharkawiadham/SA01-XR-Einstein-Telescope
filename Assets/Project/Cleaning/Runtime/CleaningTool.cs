using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Cleaning
{
    [RequireComponent(typeof(XRGrabInteractable),typeof(Rigidbody))]
    public sealed class CleaningTool : MonoBehaviour
    {
        public enum Kind { Lamp, AnticonGold, KimtechW4 }
        public Kind kind;
        public CleaningStation station;
        public Transform tip;
        public Light beam;
        public LineRenderer beamRay;
        public bool DesktopHeld { get; private set; }
        public bool Held => DesktopHeld || (grab && grab.isSelected);
        public bool LampOn { get; private set; }
        XRGrabInteractable grab;
        Rigidbody body;
        Vector3 home;Quaternion homeRotation;
        void Awake(){grab=GetComponent<XRGrabInteractable>();body=GetComponent<Rigidbody>();home=transform.position;homeRotation=transform.rotation;}
        void OnEnable(){var g=GetComponent<XRGrabInteractable>();g.selectEntered.AddListener(Picked);g.selectExited.AddListener(Dropped);g.activated.AddListener(Use);g.deactivated.AddListener(StopUse);}
        void OnDisable(){var g=GetComponent<XRGrabInteractable>();g.selectEntered.RemoveListener(Picked);g.selectExited.RemoveListener(Dropped);g.activated.RemoveListener(Use);g.deactivated.RemoveListener(StopUse);}
        void Picked(SelectEnterEventArgs e){station.Pick(this);}
        void Dropped(SelectExitEventArgs e){LampOn=false;station.Release(this);}
        void Use(ActivateEventArgs e){Activate();}
        void StopUse(DeactivateEventArgs e){LampOn=false;}
        public void Activate(){if(kind==Kind.Lamp)LampOn=true;else station.Flip(this);}
        public void SetDesktop(bool held){DesktopHeld=held;body.isKinematic=held;if(held)station.Pick(this);else{LampOn=false;station.Release(this);}}
        public void SetLamp(bool on){LampOn=on;}
        public void ReturnHome(){if(Held)return;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;transform.SetPositionAndRotation(home,homeRotation);}
        void Update(){
            if(beamRay){beamRay.enabled=LampOn && Held;beamRay.SetPosition(0,tip.position);var ray=new Ray(tip.position,tip.forward);float distance=Physics.Raycast(ray,out var hit,1.5f)?hit.distance:1.5f;beamRay.SetPosition(1,ray.GetPoint(distance));}
            if(beam)beam.enabled=LampOn && Held;if(!Held && (transform.position.y<station.transform.position.y-.2f || Vector3.Distance(transform.position,home)>4))ReturnHome();}
    }
}
