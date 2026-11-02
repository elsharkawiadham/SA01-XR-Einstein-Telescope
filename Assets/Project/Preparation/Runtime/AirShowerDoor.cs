using UnityEngine;

namespace GroupProject.Preparation
{
    public sealed class AirShowerDoor : MonoBehaviour
    {
        public Vector3 slide = new Vector3(0, 0, 1.9f);
        public float speed = 1.6f;
        public bool Open { get; private set; }
        public bool IsClosed => !Open && Vector3.Distance(transform.localPosition, closed) < .005f;
        public bool IsOpen => Open && Vector3.Distance(transform.localPosition, closed + slide) < .005f;
        Vector3 closed;
        void Awake() { closed = transform.localPosition; }
        public void SetOpen(bool value) { Open = value; }
        void Update() { transform.localPosition = Vector3.MoveTowards(transform.localPosition, closed + (Open ? slide : Vector3.zero), speed * Time.deltaTime); }
    }
}
