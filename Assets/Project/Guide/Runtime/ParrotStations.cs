using UnityEngine;
using GroupProject.Elevator;
namespace GroupProject.Guide
{
    // Discrete, authored world positions. The guide never tracks the camera within a station.
    public sealed class ParrotStations:MonoBehaviour
    {
        ParrotCompanion companion;ElevatorRide ride;Transform root;
        Transform opening,corner,cabin,landing,approach,shower,finish;
        Transform presentedStation;
        Quaternion presentationRotation;
        public Quaternion PresentationRotation(Transform station){
            // Choose the readable side once on arrival. Keep the guide world-fixed while looking around.
            if(station!=presentedStation){
                presentedStation=station;
                float yaw=station?station.eulerAngles.y:0;
                var forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
                if(station&&companion&&companion.head&&Vector3.Dot(companion.head.position-station.position,forward)>0)yaw+=180;
                presentationRotation=Quaternion.Euler(0,yaw,0);
            }
            return presentationRotation;
        }
        public void Initialize(ParrotCompanion owner,ElevatorRide lift){
            companion=owner;ride=lift;root=new GameObject("Guide vantage points (runtime)").transform;
            opening=Point("Welcome",new Vector3(1.6f,1.4f,2.3f),0);
            corner=Point("Ground corner",new Vector3(-2.2f,1.4f,9.3f),0);
            cabin=Point("Inside elevator",new Vector3(-12.5f,1.4f,8.9f),-90);
            landing=owner.lowerAnchor?owner.lowerAnchor:Point("B1 landing",new Vector3(-6.5f,1.4f,9.5f),0);
            approach=Point("Changing approach",new Vector3(10.6f,1.6f,13.7f),-90);
            shower=Point("Shower exit",new Vector3(26.5f,1.6f,20.15f),0);
            finish=Point("Facility endpoint",new Vector3(34f,1.6f,27.5f),0);
        }
        Transform Point(string name,Vector3 p,float yaw){var t=new GameObject(name).transform;t.SetParent(root);t.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));return t;}
        public Transform Choose(){
            if(!companion||!companion.head)return opening;
            var p=companion.head.position;
            if(ride&&(ride.InsideCabin()||ride.IsTravelling))return cabin;
            if(!ride||!ride.AtB1)return p.z>5?corner:opening;
            if(p.x>30)return finish;if(p.x>22)return shower;if(p.x>8)return approach;return landing;
        }
        void OnDestroy(){if(root)Destroy(root.gameObject);}
    }
}
