using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Opening
{
    public static class HandWallContact
    {
        public const float PalmRadius=.13f;
        public static Collider LastBarrier {get;private set;}
        static bool Barrier(Collider c){
            if(FacilityTeleport.IsFloor(c))return true;
            for(var t=c.transform;t;t=t.parent){var n=t.name.ToLowerInvariant();
                if(n.Contains("wall")||n.Contains("ceiling")||n.Contains("roof")||n.Contains("door")||n=="cabin back"||n=="cabin left"||n=="cabin right")return true;
            }
            return false;
        }
        public static Vector3 Resolve(Transform rig,Vector3 shoulder,Vector3 desired,out bool blocked){
            LastBarrier=null;blocked=false;var delta=desired-shoulder;float length=delta.magnitude;if(length<.001f)return desired;
            float distance=length;
            foreach(var h in Physics.SphereCastAll(shoulder,PalmRadius,delta/length,length,~0,QueryTriggerInteraction.Ignore)){
                var c=h.collider;if(c.transform.IsChildOf(rig)||c.GetComponentInParent<XRGrabInteractable>()||!Barrier(c))continue;
                if(h.distance<distance){distance=Mathf.Max(0,h.distance-.008f);blocked=true;LastBarrier=c;}
            }
            return shoulder+delta.normalized*distance;
        }
    }
}
