using UnityEngine;
using GroupProject.Opening;
using GroupProject.Elevator;
namespace GroupProject.Guide
{
    public sealed class ElevatorSoundEffects : MonoBehaviour
    {
        public ElevatorCallButton call;
        public ElevatorRide ride;
        public AudioSource doorLoop,effects;
        public AudioClip button,doorStop,startMotor,stopMotor,arrival;
        public int CuesPlayed { get; private set; }
        Vector3 lastDoor;bool called,moving;ElevatorRide.Phase phase;
        void Start(){if(doorLoop)doorLoop.volume*=.3f;if(effects)effects.volume*=.3f;if(ride && ride.motor)ride.motor.volume*=.3f;lastDoor=call.door.position;phase=ride.State;}
        void Cue(AudioClip clip){if(clip){effects.PlayOneShot(clip);CuesPlayed++;}}
        void LateUpdate()
        {
            if(!called&&call.Called){called=true;Cue(button);}
            bool nowMoving=(call.door.position-lastDoor).sqrMagnitude>.00000001f;lastDoor=call.door.position;
            if(nowMoving&&!moving)doorLoop.Play();
            if(!nowMoving&&moving){doorLoop.Stop();Cue(doorStop);}moving=nowMoving;
            if(ride.State!=phase)
            {
                if(ride.State==ElevatorRide.Phase.Closing)Cue(button);
                if(ride.State==ElevatorRide.Phase.Descending)Cue(startMotor);
                if(phase==ElevatorRide.Phase.Descending){Cue(stopMotor);if(ride.State==ElevatorRide.Phase.Opening)Cue(arrival);}
                phase=ride.State;
            }
        }
        void OnDisable(){if(doorLoop)doorLoop.Stop();if(effects)effects.Stop();}
    }
}
