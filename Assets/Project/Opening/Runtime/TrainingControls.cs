using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace GroupProject.Opening
{
    public struct TrainingControls
    {
        public Vector2 move, turn;
        public bool leftTrigger, rightTrigger, leftGrip, rightGrip, teleport, speak, checklist;
        public static TrainingControls Read()
        {
            var s=new TrainingControls();
            if(DesktopPreview.Active) {
                s.move=QuestControllerPreview.Move;s.turn=QuestControllerPreview.Turn;
                s.leftTrigger=QuestControllerPreview.LeftTrigger;s.rightTrigger=QuestControllerPreview.Trigger;
                s.leftGrip=QuestControllerPreview.LeftGrip;s.rightGrip=QuestControllerPreview.RightGrip;
                s.teleport=QuestControllerPreview.Teleport;s.speak=QuestControllerPreview.Speak;s.checklist=QuestControllerPreview.Checklist;
                var k=Keyboard.current;if(k!=null){s.teleport|=k.tKey.isPressed;s.checklist|=k.xKey.isPressed;}
                return s;
            }
            var l=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);var r=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            l.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis,out s.move);r.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis,out s.turn);
            l.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,out s.leftTrigger);r.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,out s.rightTrigger);
            l.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton,out s.leftGrip);r.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton,out s.rightGrip);
            r.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out s.teleport);r.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton,out s.speak);
            l.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out s.checklist);return s;
        }
    }
}
