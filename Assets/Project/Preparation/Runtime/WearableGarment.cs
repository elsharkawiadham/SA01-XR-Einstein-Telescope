using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Preparation
{
    [RequireComponent(typeof(XRGrabInteractable),typeof(Rigidbody))]
    public sealed class WearableGarment : MonoBehaviour
    {
        public enum Kind { PersonalJacket, Hood, Mask, Coverall, LeftGlove, RightGlove, LeftBoot, RightBoot }
        public Kind kind;
        public PreparationJourney journey;
        public Transform storagePoint;
        public bool Equipped { get; private set; }
        public bool Stored { get; private set; }
        public bool PreviewHeld { get; private set; }
        public bool Held => PreviewHeld || grab&&grab.isSelected;
        XRGrabInteractable grab;Rigidbody body;
        Vector3 home;Quaternion homeRotation;bool initialJacket;
        void Awake(){grab=GetComponent<XRGrabInteractable>();body=GetComponent<Rigidbody>();home=transform.position;homeRotation=transform.rotation;initialJacket=kind==Kind.PersonalJacket;}
        void OnEnable(){GetComponent<XRGrabInteractable>().selectEntered.AddListener(Picked);GetComponent<XRGrabInteractable>().selectExited.AddListener(Dropped);}
        void OnDisable(){GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(Picked);GetComponent<XRGrabInteractable>().selectExited.RemoveListener(Dropped);}
        void Picked(SelectEnterEventArgs e){initialJacket=false;}
        void Dropped(SelectExitEventArgs e){if(!TryPlace())ReturnToLocker();}
        public void SetPreview(bool held){PreviewHeld=held;body.isKinematic=true;if(held)initialJacket=false;else if(!TryPlace())ReturnToLocker();}
        void ReturnToLocker(){transform.SetPositionAndRotation(home,homeRotation);body.isKinematic=true;journey.Tell(kind==Kind.PersonalJacket?"Release the jacket inside your personal locker.":"Bring the garment to its matching body area before releasing it.");}
        public Vector3 TargetPosition()
        {
            var h=journey.head;var yaw=Quaternion.Euler(0,h.eulerAngles.y,0);var floor=journey.ride.playerRoot.position.y;
            var basePoint=new Vector3(h.position.x,floor,h.position.z);
            switch(kind){
                case Kind.PersonalJacket:return storagePoint.position;
                case Kind.Hood:return h.position+Vector3.up*.06f;
                case Kind.Mask:return h.position+h.forward*.08f-Vector3.up*.1f;
                case Kind.Coverall:return basePoint+yaw*new Vector3(0,Mathf.Max(.85f,h.position.y-floor-.55f),.01f);
                case Kind.LeftBoot:return basePoint+yaw*new Vector3(-.12f,.18f,.06f);
                case Kind.RightBoot:return basePoint+yaw*new Vector3(.12f,.18f,.06f);
                default:
                    string name=kind==Kind.LeftGlove?"Left Controller":"Right Controller";
                    if(!GroupProject.Opening.DesktopPreview.Active)foreach(var t in journey.ride.playerRoot.GetComponentsInChildren<Transform>(true))if(t.name==name)return t.position;
                    return basePoint+yaw*new Vector3(kind==Kind.LeftGlove?-.23f:.23f,1.08f,.38f);
            }
        }
        public bool TryPlace()
        {
            if(Equipped||Stored)return true;
            if(Vector3.Distance(transform.position,TargetPosition())>(kind==Kind.Coverall?.48f:kind==Kind.PersonalJacket?.45f:.3f))return false;
            if(kind!=Kind.PersonalJacket&&!journey.PersonalClothesStored){journey.Tell("Store your personal jacket in the locker first.");return false;}
            if(kind==Kind.PersonalJacket){Stored=true;transform.SetPositionAndRotation(storagePoint.position,storagePoint.rotation);journey.StorePersonalClothes();}
            else {Equipped=true;journey.Equip((int)kind);}
            body.isKinematic=true;grab.enabled=false;foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
            return true;
        }
        void LateUpdate()
        {
            if(!journey||!journey.head)return;
            if(Equipped||initialJacket){
                var yaw=Quaternion.Euler(0,journey.head.eulerAngles.y,0);
                transform.position=initialJacket?journey.head.position-Vector3.up*.55f:TargetPosition();transform.rotation=yaw;
                if(Equipped&&(kind==Kind.Hood||kind==Kind.Mask))foreach(var r in GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            else if(!Held&&!Stored&&(transform.position.y<journey.ride.playerRoot.position.y-.1f||Vector3.Distance(transform.position,home)>12)){body.linearVelocity=Vector3.zero;transform.SetPositionAndRotation(home,homeRotation);}
        }
    }
}
