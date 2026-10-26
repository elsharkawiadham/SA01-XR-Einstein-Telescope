using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GroupProject.Opening;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace GroupProject.Preparation
{
    [RequireComponent(typeof(XRGrabInteractable),typeof(Rigidbody))]
    public sealed class WearableGarment : MonoBehaviour
    {
        public enum Kind { PersonalJacket, Hood, Mask, Coverall, LeftGlove, RightGlove, LeftBoot, RightBoot, HairCap, SafetyGlasses, LeftOvershoe, RightOvershoe }
        public Kind kind;
        public PreparationJourney journey;
        public Transform storagePoint;
        public bool Equipped { get; private set; }
        public bool Stored { get; private set; }
        public bool PreviewHeld { get; private set; }
        public bool Held => PreviewHeld || grab && grab.isSelected;
        public bool Footwear => kind==Kind.LeftBoot||kind==Kind.RightBoot||kind==Kind.LeftOvershoe||kind==Kind.RightOvershoe;
        public bool ReadyToFit => Prerequisite()==null && (Footwear ? CanDropAtFoot() : InTarget());
        public string DisplayName => NameFor(kind);
        public static string NameFor(Kind k){switch(k){case Kind.PersonalJacket:return "Personal jacket";case Kind.HairCap:return "Hair cap";case Kind.SafetyGlasses:return "Safety glasses";case Kind.LeftOvershoe:return "Left overshoe";case Kind.RightOvershoe:return "Right overshoe";case Kind.LeftGlove:return "Left glove";case Kind.RightGlove:return "Right glove";case Kind.LeftBoot:return "Left boot";case Kind.RightBoot:return "Right boot";default:return k.ToString();}}
        InteractorHandedness selectedSide;
        XRGrabInteractable grab; Rigidbody body; Transform selectingHand; MeshFilter garmentMesh;
        readonly RaycastHit[] floorHits=new RaycastHit[24];
        Vector3 home; Quaternion homeRotation; bool dropping; float heldTime; int releaseGeneration;
        public string Hint => Prerequisite() ?? (ReadyToFit?"Ready: release grip to confirm.":kind==Kind.PersonalJacket?"Release your jacket inside the personal locker.":Footwear?"Release over the marked foot space. Let it settle onto the floor.":kind==Kind.LeftGlove||kind==Kind.RightGlove?"Bring the glove to the other hand, then release grip.":kind==Kind.Coverall?"Bring the suit toward your chest, then release grip.":"Bring this item near your face or head, then release grip.");
        void Awake(){grab=GetComponent<XRGrabInteractable>();body=GetComponent<Rigidbody>();garmentMesh=GetComponent<MeshFilter>();home=transform.position;homeRotation=transform.rotation;body.isKinematic=true;body.useGravity=false;grab.throwOnDetach=false;grab.useDynamicAttach=true;}
        void OnEnable(){GetComponent<XRGrabInteractable>().selectEntered.AddListener(Picked);GetComponent<XRGrabInteractable>().selectExited.AddListener(Dropped);}
        void OnDisable(){GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(Picked);GetComponent<XRGrabInteractable>().selectExited.RemoveListener(Dropped);}
        void Picked(SelectEnterEventArgs e){releaseGeneration++;dropping=false;selectingHand=e.interactorObject.transform;selectedSide=e.interactorObject.handedness;heldTime=0;journey?.Tell(DisplayName+" selected. "+Hint);}
        void Dropped(SelectExitEventArgs e)
        {
            if(Equipped||Stored)return;
            if(e.isCanceled){int version=++releaseGeneration;PreviewHeld=false;dropping=false;
                if(gameObject.activeInHierarchy)StartCoroutine(RecoverCanceledGrab(version));
                else ResetToLocker();
                return;
            }
            StartCoroutine(Release());
        }
        IEnumerator RecoverCanceledGrab(int version)
        {
            // XRI completes its detach after selectExited; do not race that final pose update.
            yield return null;
            if(version!=releaseGeneration||Held||Equipped||Stored)yield break;
            ResetToLocker();journey?.Tell(DisplayName+" returned to its locker after the grab was interrupted. Try again.");
        }
        void ResetToLocker()
        {
            dropping=false;PreviewHeld=false;selectingHand=null;selectedSide=InteractorHandedness.None;
            transform.SetPositionAndRotation(home,homeRotation);body.isKinematic=true;body.useGravity=false;
        }
        public void SetPreview(bool held){PreviewHeld=held;body.isKinematic=true;if(held){releaseGeneration++;dropping=false;heldTime=0;}else StartCoroutine(Release());}
        IEnumerator Release(){int version=++releaseGeneration;yield return null;if(version!=releaseGeneration||Held||Equipped||Stored)yield break;if(!Footwear&&TryPlace())yield break;
            if(Footwear&&Prerequisite()==null){dropping=true;var start=transform.position;var target=TargetPosition();float floor=target.y;
                // Only accept a release over the foot zone, never a drop elsewhere in the room.
                if(CanDropAtFoot()){for(float t=0;t<.6f;t+=Time.deltaTime){if(Held||version!=releaseGeneration)yield break;var settle=Vector3.Lerp(start,target,t/.6f);transform.position=new Vector3(settle.x,Mathf.Max(floor,start.y-4.9f*t*t),settle.z);yield return null;}transform.position=target;if(TryPlace()){dropping=false;yield break;}}
            }
            dropping=false;ReturnToLocker();
        }
        void ReturnToLocker(){var reason=Prerequisite();ResetToLocker();journey?.Tell(reason ?? (Footwear?"Place this over the marked foot space. It has returned to its locker.":DisplayName+": move it to the highlighted target and release. Try again."));}
        public string Prerequisite(){if(!DesktopPreview.Active&&((kind==Kind.LeftGlove&&selectedSide==InteractorHandedness.Left)||(kind==Kind.RightGlove&&selectedSide==InteractorHandedness.Right)))return "Use the opposite hand to put on this glove.";if(!journey)return "Preparing dressing area.";if(kind!=Kind.PersonalJacket&&!journey.PersonalClothesStored)return "First store your personal jacket in the personal locker.";if(kind==Kind.LeftOvershoe&&!journey.HasGarment(Kind.LeftBoot))return "Put on the left boot before its overshoe.";if(kind==Kind.RightOvershoe&&!journey.HasGarment(Kind.RightBoot))return "Put on the right boot before its overshoe.";return null;}
        Vector3 Hand(bool left){var h=journey.head;var device=InputDevices.GetDeviceAtXRNode(left?XRNode.LeftHand:XRNode.RightHand);
            if(!DesktopPreview.Active&&device.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 local))return h.parent?h.parent.TransformPoint(local):local;
            return h.position+Quaternion.Euler(0,h.eulerAngles.y,0)*new Vector3(left?-.22f:.22f,-.4f,.32f);}
        public Vector3 TargetPosition(){var h=journey.head;var yaw=Quaternion.Euler(0,h.eulerAngles.y,0);var basePoint=new Vector3(h.position.x,journey.ride.playerRoot.position.y,h.position.z);
            switch(kind){case Kind.PersonalJacket:return storagePoint?storagePoint.position:home;
            case Kind.Hood:case Kind.HairCap:return h.position+Vector3.up*.08f;
            case Kind.Mask:case Kind.SafetyGlasses:return h.position+h.forward*.15f-Vector3.up*(kind==Kind.Mask?.08f:0);
            case Kind.Coverall:return h.position-Vector3.up*.48f;
            case Kind.LeftBoot:case Kind.LeftOvershoe:return FootTarget(basePoint+yaw*new Vector3(-.16f,0,.18f));
            case Kind.RightBoot:case Kind.RightOvershoe:return FootTarget(basePoint+yaw*new Vector3(.16f,0,.18f));
            case Kind.LeftGlove:return Hand(true);default:return Hand(false);}}
        Vector3 FootTarget(Vector3 point)
        {
            // The rig root already moves with the floor. Sample the room instead of adding its authored height twice.
            float floor=journey.ride.playerRoot.position.y, nearest=float.PositiveInfinity;
            int count=Physics.RaycastNonAlloc(new Vector3(point.x,floor+.5f,point.z),Vector3.down,floorHits,2f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=floorHits[i];var collider=hit.collider;
                if(!collider||hit.normal.y<.65f||collider.transform.IsChildOf(journey.ride.playerRoot)||collider.GetComponentInParent<WearableGarment>())continue;
                if(hit.distance<nearest){nearest=hit.distance;floor=hit.point.y;}
            }
            float sole=-.126f;
            if(garmentMesh&&garmentMesh.sharedMesh)
            {
                var bounds=garmentMesh.sharedMesh.bounds;float scale=transform.lossyScale.y;
                sole=Mathf.Min(bounds.min.y*scale,bounds.max.y*scale);
            }
            point.y=floor-sole;return point;
        }
        bool CanDropAtFoot(){if(!journey||!journey.head)return false;var target=TargetPosition();var start=transform.position;
            return new Vector2(start.x-target.x,start.z-target.z).magnitude<.38f && start.y>=target.y-.1f && start.y<journey.head.position.y+.1f;}
        bool InTarget(){float radius=kind==Kind.PersonalJacket?.5f:kind==Kind.Coverall?.48f:Footwear?.30f:.32f;
            if((kind==Kind.LeftGlove||kind==Kind.RightGlove)&&selectingHand&&!DesktopPreview.Active&&Vector3.Distance(selectingHand.position,TargetPosition())<.08f&&heldTime<.4f)return false;
            return Vector3.Distance(transform.position,TargetPosition())<=radius;}
        public bool TryPlace(){if(Equipped||Stored)return true;if(Prerequisite()!=null||!InTarget())return false;
            if(kind==Kind.PersonalJacket){Stored=true;transform.SetPositionAndRotation(TargetPosition(),storagePoint?storagePoint.rotation:homeRotation);journey.StorePersonalClothes();}
            else{Equipped=true;journey.Equip((int)kind);}
            body.isKinematic=true;grab.enabled=false;foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
            if(selectingHand){var node=selectedSide==InteractorHandedness.Right?XRNode.RightHand:XRNode.LeftHand;InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0,.35f,.10f);}return true;}
        void LateUpdate(){if(!journey||!journey.head)return;if(Held)heldTime+=Time.deltaTime;
            if(Equipped){transform.position=TargetPosition();transform.rotation=Quaternion.Euler(0,journey.head.eulerAngles.y,0);if(kind==Kind.LeftGlove||kind==Kind.RightGlove){var dev=InputDevices.GetDeviceAtXRNode(kind==Kind.LeftGlove?XRNode.LeftHand:XRNode.RightHand);if(!DesktopPreview.Active&&dev.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion rot))transform.rotation=(journey.head.parent?journey.head.parent.rotation:Quaternion.identity)*rot;}if(kind==Kind.Hood||kind==Kind.Mask||kind==Kind.HairCap||kind==Kind.SafetyGlasses)foreach(var r in GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;}
            // Keep the jacket on the locker hook: it can now be picked up before it is stored.
            else if(!Held&&!Stored&&!dropping&&Vector3.Distance(transform.position,home)>12)ReturnToLocker();
        }
    }
}
