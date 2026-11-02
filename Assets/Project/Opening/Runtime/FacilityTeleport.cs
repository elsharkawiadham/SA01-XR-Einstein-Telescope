using System;
using UnityEngine;
using Unity.XR.CoreUtils;
using GroupProject.Elevator;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Opening
{
    // Hold right A, aim an arc at a clear floor, release to commit. No separate floor-sized interactables.
    public sealed class FacilityTeleport:MonoBehaviour
    {
        public const float Radius=.23f;
        XROrigin origin;Transform hand,head;CharacterController body;ElevatorRide ride;
        LineRenderer arc,ring;Material material;bool held,valid;Vector3 target;
        readonly Vector3[] points=new Vector3[51];
        public string LastRejection {get;private set;}
        bool Reject(string reason){LastRejection=reason;return false;}
        public bool HasValidTarget=>valid;
        public Vector3 Target=>target;
        void Start(){
            origin=GetComponent<XROrigin>();body=GetComponent<CharacterController>();head=origin?origin.Camera.transform:null;
            foreach(var t in GetComponentsInChildren<Transform>(true))if(t.name=="Right Controller")hand=t;
            ride=FindAnyObjectByType<ElevatorRide>();
            foreach(var c in FindObjectsByType<Collider>(FindObjectsInactive.Include)) {
                if(c.name=="Cabin guide"||c.name=="Follow preparation route"||c.name=="Follow facility route"||c.name.StartsWith("Exit guide"))c.enabled=false;
                else for(var t=c.transform;t;t=t.parent)if(t.name=="FloorGuidance"){c.enabled=false;break;}
            }
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            arc=Line("Teleport arc",.014f);ring=Line("Teleport landing",.018f);ring.loop=true;
        }
        LineRenderer Line(string name,float width){var g=new GameObject(name);g.transform.SetParent(transform);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=material;l.useWorldSpace=true;l.widthMultiplier=width;l.enabled=false;return l;}
        public static bool IsFloor(Collider c){
            if(!c||!c.enabled||c.isTrigger||!c.gameObject.activeInHierarchy)return false;
            for(var t=c.transform;t;t=t.parent){var n=t.name.ToLowerInvariant();if(n.Contains("ceiling")||n.Contains("roof"))return false;}
            for(var t=c.transform;t;t=t.parent){var n=t.name.ToLowerInvariant();if(n.Contains("floor")&&!n.Contains("lowerfloor")&&!n.Contains("floor display")&&!n.Contains("floor indicator"))return true;}
            return false;
        }
        bool Ignore(Collider c)=>!c||c.isTrigger||c.transform.IsChildOf(transform)||c.GetComponentInParent<XRGrabInteractable>();
        bool Hit(Vector3 from,Vector3 to,out RaycastHit hit){
            hit=default;var delta=to-from;float nearest=float.MaxValue;
            foreach(var h in Physics.RaycastAll(from,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if(!Ignore(h.collider)&&h.distance<nearest){hit=h;nearest=h.distance;}
            return nearest<float.MaxValue;
        }
        public bool CanLand(Vector3 position,Collider floor){
            if(!head||!IsFloor(floor))return Reject("Not a floor: "+(floor?floor.name:"none"));
            float currentFloor=PlayerBodyAvatar.Active?PlayerBodyAvatar.Active.FloorHeight:transform.position.y;
            if(Mathf.Abs(position.y-currentFloor)>.65f)return Reject("Floor height delta "+position.y+" / "+currentFloor);
            if(ride&&ride.IsTravelling){var b=ride.safeCabin.bounds;if(position.x<b.min.x+Radius||position.x>b.max.x-Radius||position.z<b.min.z+Radius||position.z>b.max.z-Radius)return false;}
            // A full upright body must fit, including at edges, so the head cannot land through a wall.
            var bottom=position+Vector3.up*(Radius+.035f);var top=position+Vector3.up*(1.65f-Radius);
            foreach(var c in Physics.OverlapCapsule(bottom,top,Radius,~0,QueryTriggerInteraction.Ignore))if(!Ignore(c)&&c!=floor)return Reject("Body blocked by "+c.name);
            foreach(var offset in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back}){
                if(!Hit(position+offset*Radius+Vector3.up*.15f,position+offset*Radius-Vector3.up*.2f,out var edge)||!IsFloor(edge.collider)||edge.normal.y<.8f)return Reject("Floor edge has no support");
            }
            // Keep closed doors, walls and the coming-soon boundary authoritative.
            var start=new Vector3(head.position.x,currentFloor+.35f,head.position.z);
            var end=position+Vector3.up*.35f;
            if(Hit(start,end,out var obstruction)&&!IsFloor(obstruction.collider))return Reject("Path blocked by "+obstruction.collider.name);
            return true;
        }
        void Update(){
            if(!head||!arc)return;
            bool down=TrainingControls.Read().teleport;
            if(LocomotionGuide.Locked){held=false;arc.enabled=ring.enabled=false;return;}
            if(down){Trace();arc.enabled=true;ring.enabled=valid;}
            else {if(held&&valid)TryTeleport(target);arc.enabled=ring.enabled=false;}
            held=down;
        }
        void Trace(){
            var aim=DesktopPreview.Active||!hand?head:hand;var start=aim.position;var velocity=aim.forward*6.5f;points[0]=start;valid=false;int count=1;
            for(int i=1;i<points.Length;i++){
                float t=i*.045f;var p=start+velocity*t+Vector3.down*4.9f*t*t;
                if(Hit(points[i-1],p,out var h)){points[count++]=h.point;target=h.point;valid=h.normal.y>.85f&&CanLand(target,h.collider);break;}
                points[count++]=p;
            }
            arc.positionCount=count;for(int i=0;i<count;i++)arc.SetPosition(i,points[i]);
            material.color=valid?new Color(.15f,1,.55f):new Color(1,.3f,.2f);
            ring.positionCount=40;for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;ring.SetPosition(i,target+new Vector3(Mathf.Cos(a)*Radius,.025f,Mathf.Sin(a)*Radius));}
        }
        public bool TryTeleport(Vector3 destination){
            if(LocomotionGuide.Locked||!head)return Reject("Onboarding locked");
            if(!Hit(destination+Vector3.up*.15f,destination-Vector3.up*.2f,out var h))return Reject("No floor hit");
            if(!CanLand(h.point,h.collider))return false;
            float floor=PlayerBodyAvatar.Active?PlayerBodyAvatar.Active.FloorHeight:transform.position.y;
            var offset=new Vector3(h.point.x-head.position.x,h.point.y-floor,h.point.z-head.position.z);
            bool enabled=body&&body.enabled;if(enabled)body.enabled=false;transform.position+=offset;if(enabled)body.enabled=true;Physics.SyncTransforms();return true;
        }
        void OnDisable(){held=false;if(arc)arc.enabled=false;if(ring)ring.enabled=false;}
        void OnDestroy(){if(material)Destroy(material);}
    }
}
