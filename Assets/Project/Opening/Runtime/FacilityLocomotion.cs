using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
namespace GroupProject.Opening
{
    // Walking, smooth turning and A-button teleport coexist. Shared sample action maps cannot turn them off.
    [DefaultExecutionOrder(-60)]
    public sealed class FacilityLocomotion : MonoBehaviour
    {
        ContinuousMoveProvider move;SmoothStickTurnProvider turn;
        InputAction walkAction,turnAction;bool initialized;
        public ContinuousMoveProvider MoveProvider=>move;
        public SmoothStickTurnProvider TurnProvider=>turn;
        public void Initialize(){
            if(initialized)return;
            move=GetComponentInChildren<ContinuousMoveProvider>(true);turn=GetComponentInChildren<SmoothStickTurnProvider>(true);
            if(!move||!turn){Debug.LogError("Facility movement providers are missing.",this);return;}
            var walk=new XRInputValueReader<Vector2>("Facility left stick walk",XRInputValueReader.InputSourceMode.InputAction);
            walk.inputAction.AddBinding("<XRController>{LeftHand}/primary2DAxis");walkAction=walk.inputAction;
            var turning=new XRInputValueReader<Vector2>("Facility right stick smooth turn",XRInputValueReader.InputSourceMode.InputAction);
            turning.inputAction.AddBinding("<XRController>{RightHand}/primary2DAxis");turnAction=turning.inputAction;
            move.leftHandMoveInput=walk;move.rightHandMoveInput=new XRInputValueReader<Vector2>{inputSourceMode=XRInputValueReader.InputSourceMode.Unused};
            turn.rightHandTurnInput=turning;turn.leftHandTurnInput=new XRInputValueReader<Vector2>{inputSourceMode=XRInputValueReader.InputSourceMode.Unused};
            move.enableStrafe=true;move.enableFly=false;turn.enableTurnLeftRight=true;turn.enableTurnAround=false;turn.turnSpeed=45;
            // The custom teleport control owns A. Disable the sample's mutually exclusive stick-teleport mode.
            foreach(var manager in GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.ControllerInputActionManager>(true)){
                manager.smoothMotionEnabled=true;manager.smoothTurnEnabled=true;manager.uiScrollingEnabled=false;
            }
            foreach(var p in GetComponentsInChildren<SnapTurnProvider>(true))p.enabled=false;
            foreach(var p in GetComponentsInChildren<TeleportationProvider>(true))p.enabled=false;
            initialized=true;Refresh();
        }
        void Start()=>Initialize();
        void Update(){if(!initialized)Initialize();else Refresh();}
        public void Refresh(){
            if(!initialized)return;bool allow=!DesktopPreview.Active&&!LocomotionGuide.Locked;
            if(move.enabled!=allow)move.enabled=allow;if(turn.enabled!=allow)turn.enabled=allow;
            if(allow){if(!walkAction.enabled)walkAction.Enable();if(!turnAction.enabled)turnAction.Enable();}
            else{if(walkAction.enabled)walkAction.Disable();if(turnAction.enabled)turnAction.Disable();}
        }
        void OnDestroy(){walkAction?.Dispose();turnAction?.Dispose();}
    }
}
