using UnityEngine;
namespace GroupProject.Guide
{
    public sealed class FootstepAudio : MonoBehaviour
    {
        public Transform head;
        public AudioSource source;
        public AudioClip[] steps;
        public float stride=.65f;
        public int StepsPlayed { get; private set; }
        Vector3 previous;float distance,lastStep,previousYaw;int index;
        void OnEnable(){if(head)previous=head.position;distance=0;previousYaw=transform.eulerAngles.y;}
        void Update()
        {
            if(!head||!source||steps==null||steps.Length==0)return;
            Vector3 delta=head.position-previous;previous=head.position;delta.y=0;
            float travelled=delta.magnitude;
            float turn=Mathf.Abs(Mathf.DeltaAngle(previousYaw,transform.eulerAngles.y));previousYaw=transform.eulerAngles.y;
            if(turn>10){distance=0;return;}
            // Teleports, snap turns and headset jitter should not sound like walking.
            if(travelled>Mathf.Max(.5f,Time.deltaTime*5)){distance=0;return;}
            if(travelled<Time.deltaTime*.10f)return;
            var feet=new Vector3(head.position.x,transform.position.y+.15f,head.position.z);
            if(!Physics.Raycast(feet,Vector3.down,out var hit,.5f,~(1<<2),QueryTriggerInteraction.Ignore))return;
            if(hit.collider.transform.IsChildOf(transform))return;
            distance+=travelled;
            if(distance<stride||Time.time-lastStep<.25f)return;
            distance%=stride;lastStep=Time.time;source.transform.position=hit.point+Vector3.up*.05f;
            source.pitch=1+(index%3-1)*.035f;source.PlayOneShot(steps[index++%steps.Length],.22f);StepsPlayed++;
        }
    }
}
