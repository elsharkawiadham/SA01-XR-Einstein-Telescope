using Unity.XR.CoreUtils;
using UnityEngine;
namespace GroupProject.Opening
{
    // Standard virtual eye height using a device-relative origin. No wearer-height sampling or avatar resizing.
    public sealed class StartingViewHeight:MonoBehaviour
    {
        public const float EyeHeight=1.65f;
        public bool Calibrated {get;private set;}
        XROrigin origin;
        void Awake(){Configure();}
        void Configure(){
            origin=GetComponent<XROrigin>();if(!origin)return;
            origin.CameraYOffset=EyeHeight;
            origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Device;
            if(origin.CameraFloorOffsetObject){var t=origin.CameraFloorOffsetObject.transform;var position=t.localPosition;position.y=EyeHeight;t.localPosition=position;}
            Calibrated=true;
        }
        // Kept for existing callers; the measured height is intentionally not used.
        public void Apply(float measuredHeight){Configure();}
    }
}
