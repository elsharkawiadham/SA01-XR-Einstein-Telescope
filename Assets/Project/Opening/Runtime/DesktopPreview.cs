using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace GroupProject.Opening
{
    /// <summary>Desktop fallback using the same camera, collisions and elevator as the VR rig.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class DesktopPreview : MonoBehaviour
    {
        public Camera view;
        public LocomotionGuide tutorial;
        public StandaloneAIGuide guide;
        public float moveSpeed = 1.4f;
        public float mouseSensitivity = .12f;
        public static bool Active { get; private set; }
        public bool IsDesktop { get; private set; }
        public bool MovementLocked { get; set; }
        readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        readonly List<Behaviour> suspended = new List<Behaviour>();
        readonly List<GameObject> hiddenControllers = new List<GameObject>();
        CharacterController body;
        XROrigin origin;
        Transform offset;
        Vector3 savedOffset, savedHeadPosition, savedCenter;
        Quaternion savedHeadRotation;
        float savedHeight, savedRadius, savedStep, savedMinimumMove, pitch, gravity;
        bool savedBodyEnabled, captured;
        GUIStyle hintStyle, crosshairStyle;

        void Awake()
        {
            origin = GetComponent<XROrigin>();
            body = GetComponent<CharacterController>();
            if (!view && origin) view = origin.Camera;
            if (origin && origin.CameraFloorOffsetObject) offset = origin.CameraFloorOffsetObject.transform;
            if (enabled) RefreshMode();
        }

        void OnEnable() { if (origin) RefreshMode(); }

        bool ShouldUseDesktop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return false;
#else
            displays.Clear();
            SubsystemManager.GetSubsystems(displays);
            foreach (var display in displays) if (display.running) return false;
            return !XRSettings.isDeviceActive;
#endif
        }

        void RefreshMode()
        {
            bool requested = ShouldUseDesktop();
            if (requested == IsDesktop || !view || !body || !offset) return;
            if (requested) EnableDesktop(); else RestoreXR();
        }

        void EnableDesktop()
        {
            savedOffset = offset.localPosition;
            savedHeadPosition = view.transform.localPosition;
            savedHeadRotation = view.transform.localRotation;
            savedHeight = body.height; savedRadius = body.radius; savedStep = body.stepOffset;
            savedMinimumMove = body.minMoveDistance; savedCenter = body.center; savedBodyEnabled = body.enabled;
            foreach (var behaviour in GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour.enabled && (behaviour is TrackedPoseDriver || behaviour is LocomotionProvider ||
                    behaviour is XRBodyTransformer || behaviour is XROrigin || behaviour.GetType().Name == "CharacterControllerDriver"))
                {
                    suspended.Add(behaviour); behaviour.enabled = false;
                }
            }
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if ((child.name == "Left Controller" || child.name == "Right Controller") && child.gameObject.activeSelf)
                { hiddenControllers.Add(child.gameObject); child.gameObject.SetActive(false); }
            }
            offset.localPosition = new Vector3(0, 1.65f, 0);
            view.transform.localPosition = Vector3.zero;
            view.transform.localRotation = Quaternion.identity;
            body.height = 1.65f; body.radius = .2f; body.center = new Vector3(0, .825f, 0);
            body.stepOffset = .2f; body.minMoveDistance = 0; body.enabled = true;
            pitch = 0; gravity = 0; IsDesktop = true; Active = true;
            if(!GetComponent<QuestControllerPreview>())gameObject.AddComponent<QuestControllerPreview>();
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }

        void RestoreXR()
        {
            if (!IsDesktop) return;
            IsDesktop = false; Active = false; QuestControllerPreview.ResetControls();
            if (offset) offset.localPosition = savedOffset;
            if (view) { view.transform.localPosition = savedHeadPosition; view.transform.localRotation = savedHeadRotation; }
            if (body) { body.height = savedHeight; body.radius = savedRadius; body.stepOffset = savedStep; body.minMoveDistance = savedMinimumMove; body.center = savedCenter; body.enabled = savedBodyEnabled; }
            foreach (var behaviour in suspended) if (behaviour) behaviour.enabled = true;
            foreach (var controller in hiddenControllers) if (controller) controller.SetActive(true);
            suspended.Clear(); hiddenControllers.Clear();
            ReleaseCursor();
        }

        float nextTurn;
        void Update()
        {
            RefreshMode();if(!IsDesktop)return;
            var turn=QuestControllerPreview.Turn;
            if(Mathf.Abs(turn.x)>.6f && Time.unscaledTime>=nextTurn){transform.Rotate(0,Mathf.Sign(turn.x)*30,0);nextTurn=Time.unscaledTime+.4f;}
            var aim=QuestControllerPreview.Aim;
            transform.Rotate(0,aim.x*45*Time.deltaTime,0);
            pitch=Mathf.Clamp(pitch-aim.y*45*Time.deltaTime,-75,75);
            view.transform.localRotation=Quaternion.Euler(pitch,0,0);
            Move(QuestControllerPreview.Move,Time.deltaTime);
            if(QuestControllerPreview.TriggerPressed)TryInteract();
        }

        public void Move(Vector2 movement, float seconds)
        {
            if (!IsDesktop || !body || !body.enabled || MovementLocked) return;
            movement = Vector2.ClampMagnitude(movement, 1);
            gravity = body.isGrounded ? -2 : Mathf.Max(gravity - 9.81f * seconds, -20);
            Vector3 motion = (transform.right * movement.x + transform.forward * movement.y) * moveSpeed;
            body.Move((motion + Vector3.up * gravity) * seconds);
        }

        ElevatorCallButton AimedButton()
        {
            if (!view || !Physics.Raycast(view.ViewportPointToRay(new Vector3(.5f,.5f,0)), out var hit, 3, ~0, QueryTriggerInteraction.Collide)) return null;
            var button = hit.collider.GetComponentInParent<ElevatorCallButton>();
            if (!button || !button.playerHead) return null;
            Vector3 distance = button.playerHead.position - button.transform.position; distance.y = 0;
            return distance.sqrMagnitude <= button.activationDistance * button.activationDistance ? button : null;
        }

        GroupProject.Elevator.ElevatorFloorButton AimedFloorButton()
        {
            if (!view || !Physics.Raycast(view.ViewportPointToRay(new Vector3(.5f,.5f,0)), out var hit, 3, ~0, QueryTriggerInteraction.Ignore)) return null;
            var button = hit.collider.GetComponentInParent<GroupProject.Elevator.ElevatorFloorButton>();
            return button && button.InRange ? button : null;
        }

        public bool TryInteract()
        {
            var button = IsDesktop ? AimedButton() : null;
            if (button) return button.TryCall();
            var floor = IsDesktop ? AimedFloorButton() : null;
            return floor && floor.TryPress();
        }

        void OnGUI()
        {
            if (!IsDesktop) return;
            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                crosshairStyle = new GUIStyle(hintStyle) { fontSize = 24 };
            }
            string message = tutorial && tutorial.IsRunning ? LocomotionGuide.CaptionForStep(tutorial.CurrentStep) : guide && guide.feedbackText ? guide.feedbackText.text : "";
            if ((!guide || !guide.companion) && !string.IsNullOrEmpty(message))
            {
                float width = Mathf.Min(460, Screen.width - 24);
                GUI.Box(new Rect(12, 12, width, 140), GUIContent.none);
                GUI.Label(new Rect(24, 20, width - 24, 124), message, hintStyle);
            }
            GUI.Label(new Rect(Screen.width/2f-12,Screen.height/2f-15,24,30),"+",crosshairStyle);
            var floor=AimedFloorButton();var target=AimedButton();
            string hint=floor?"Trigger — B1 lower floor":target?(target.Called?"Elevator called":"Trigger — Call elevator"):"";
            GUI.Label(new Rect(Screen.width/2f-170,Screen.height/2f+25,340,36),hint,hintStyle);
        }

        public void CapturePointer() { if (!IsDesktop) return; captured = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void ReleaseCursor() { captured = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnApplicationFocus(bool focused) { if (!focused && IsDesktop) ReleaseCursor(); }
        void OnDisable() { RestoreXR(); }
    }
}
