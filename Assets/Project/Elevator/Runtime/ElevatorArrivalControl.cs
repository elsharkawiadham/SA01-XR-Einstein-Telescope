using UnityEngine;
using TMPro;

namespace GroupProject.Elevator
{
    // Delayed closure and call-button reopening on either landing.
    public sealed class ElevatorArrivalControl : MonoBehaviour
    {
        public ElevatorRide ride;
        public Transform arrow;
        public TMP_Text exteriorFloorSign;
        public float closeDelay=5,closeSeconds=2;
        public bool AtB1=>ride&&ride.AtB1;
        public bool Closed=>ride&&ride.AtLanding&&Vector3.Distance(ride.callButton.door.localPosition,closed)<.01f;
        Vector3 closed;
        Bounds doorway;
        float outsideFor;
        bool closing;
        void Awake()
        {
            closed=ride.callButton.door.localPosition;
            doorway=new Bounds(new Vector3(-9.85f,1.4f,8),new Vector3(.6f,2.8f,2.7f));
            var colliders=ride.callButton.door.GetComponentsInChildren<Collider>();
            if(colliders.Length>0){doorway=colliders[0].bounds;foreach(var c in colliders)doorway.Encapsulate(c.bounds);}
            doorway.Expand(new Vector3(.8f,0,.6f));
        }
        public bool DoorwayOccupied
        {
            get{if(!ride.playerHead)return false;var p=ride.playerHead.position;return p.x>=doorway.min.x&&p.x<=doorway.max.x&&p.z>=doorway.min.z&&p.z<=doorway.max.z;}
        }
        public bool Reopen(){if(!ride.AtLanding)return false;outsideFor=0;closing=false;return true;}
        void Update()
        {
            if(arrow)arrow.localRotation=Quaternion.Euler(0,0,AtB1?0:180);
            if(exteriorFloorSign)exteriorFloorSign.text=ride.IsTravelling?"LIFT IN TRANSIT":AtB1?"B1  /  LOWER LEVEL":"G  /  GROUND LEVEL";
            if(!ride.AtLanding){outsideFor=0;closing=false;return;}
            ride.callButton.ExternalDoorControl=true;
            if(ride.callButton.indicator)ride.callButton.indicator.sharedMaterial=Closed?ride.callButton.readyMaterial:ride.callButton.calledMaterial;
            bool occupied=ride.InsideCabin()||DoorwayOccupied;
            if(occupied){outsideFor=0;closing=false;}
            else if(!closing){outsideFor+=Time.deltaTime;if(outsideFor>=closeDelay)closing=true;}
            var target=closed+(closing?Vector3.zero:ride.callButton.openOffset);
            ride.callButton.door.localPosition=Vector3.MoveTowards(ride.callButton.door.localPosition,target,ride.callButton.openOffset.magnitude/Mathf.Max(.1f,closeSeconds)*Time.deltaTime);
        }
    }
}
