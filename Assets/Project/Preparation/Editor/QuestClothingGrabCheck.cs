using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GroupProject.Opening;
using GroupProject.Preparation;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using TouchController = UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile.OculusTouchController;

/// <summary>
/// Opt-in batch regression through the saved rig's real XRI candidate and grip-action paths.
/// No direct SelectEnter or desktop SetPreview calls, and no scene is saved by this check.
/// </summary>
[InitializeOnLoad]
public static class QuestClothingGrabCheck
{
    const string Pending = "QuestClothingGrabCheck.Pending";
    const string Layout = "QuestClothingGrabTestController";
    const string Report = "Logs/QuestClothingGrabCheck.txt";
    static bool running;

    static QuestClothingGrabCheck() { EditorApplication.update += Tick; }

    public static void Run()
    {
        Require(Application.isBatchMode, "Run this check in batch mode with the project closed; it opens the saved scene.");
        Require(!EditorApplication.isPlaying, "The check must start outside Play Mode.");
        EditorSceneManager.OpenScene(ProjectAssembly.Main);
        Directory.CreateDirectory("Logs");
        if (File.Exists(Report)) File.Delete(Report);

        // Substitute tracked poses only. Leave InputActionManager and controller selection bindings intact.
        // Disabling before Awake prevents the desktop fallback from hiding the controller hierarchy.
        foreach (var behaviour in UnityEngine.Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour is DesktopPreview || behaviour is TrackedPoseDriver ||
                behaviour is XRInputModalityManager || behaviour is LocomotionProvider ||
                behaviour is XRBodyTransformer || behaviour is XROrigin ||
                behaviour.GetType().Name == "CharacterControllerDriver")
                behaviour.enabled = false;
        }
        foreach (var guide in UnityEngine.Object.FindObjectsByType<StandaloneAIGuide>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            guide.gameObject.SetActive(false);
        var opening = UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();
        if (opening)
        {
            opening.enabled = false;
            if (opening.locomotionGuide) opening.locomotionGuide.enabled = false;
        }
        var journey = UnityEngine.Object.FindAnyObjectByType<PreparationJourney>(FindObjectsInactive.Include);
        Require(journey && journey.ride && journey.head, "The saved scene is missing the preparation route/player links.");
        journey.enabled = false;
        journey.ride.enabled = false;
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    static async void Tick()
    {
        if (running || !SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying) return;
        running = true;
        int exitCode = 0;
        try
        {
            await Test();
            File.WriteAllText(Report,
                "PASS: twelve saved garments have detectable solid colliders; trigger-only failure reproduced; " +
                "both saved NearFar controllers find clothing by near touch and by aiming through the open locker; original Select actions consume synthetic " +
                "OpenXR grip input; both hands pick up, move, release, recover after cancellation and re-grab " +
                "through XRI without SetPreview or SelectEnter. Invalid fitting returns clothing to its locker. " +
                "No scene saved. Synthetic controller input is not physical Quest Link or headset acceptance.");
            Debug.Log("QuestClothingGrabCheck PASS (synthetic OpenXR controller input).");
        }
        catch (Exception error)
        {
            exitCode = 1;
            File.WriteAllText(Report, "FAIL: " + error);
            Debug.LogException(error);
        }
        finally
        {
            SessionState.SetBool(Pending, false);
            EditorApplication.Exit(exitCode);
        }
    }

    static async Task Test()
    {
        TouchController leftDevice = null, rightDevice = null;
        try
        {
            var journey = UnityEngine.Object.FindAnyObjectByType<PreparationJourney>(FindObjectsInactive.Include);
            var root = journey.ride.playerRoot;
            journey.ride.destinationRoot.SetActive(true);
            var capsule = root.GetComponent<CharacterController>();
            if (capsule) capsule.enabled = false;
            foreach (var collider in journey.ride.departureRoot.GetComponentsInChildren<Collider>(true))
                if (!collider.transform.IsChildOf(root) && !collider.transform.IsChildOf(journey.ride.transform))
                    collider.enabled = false;
            root.position += new Vector3(16, 1.65f, 20.35f) - journey.head.position;
            journey.head.rotation = Quaternion.identity;
            var left = Named(root, "Left Controller");
            var right = Named(root, "Right Controller");
            left.gameObject.SetActive(true);
            right.gameObject.SetActive(true);
            foreach (var door in UnityEngine.Object.FindObjectsByType<LockerDoor>(FindObjectsSortMode.None))
                if (!door.IsOpen) door.Toggle();
            Physics.SyncTransforms();
            await Frames(90);
            Require(!DesktopPreview.Active, "Desktop preview must not intercept this hardware input regression.");
            // The Input System switches from editor state on the first player update.
            // Add test devices after that transition, otherwise they belong to the discarded editor state.
            InputSystem.RegisterLayout<TouchController>(Layout);
            leftDevice = (TouchController)InputSystem.AddDevice(Layout);
            rightDevice = (TouchController)InputSystem.AddDevice(Layout);
            InputSystem.SetDeviceUsage(leftDevice, CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(rightDevice, CommonUsages.RightHand);
            await Frames(5);
            Require(leftDevice.added && rightDevice.added,"Synthetic devices were removed after initialization: "+string.Join(",",InputSystem.devices.Select(d=>d.name)));

            var garments = UnityEngine.Object.FindObjectsByType<WearableGarment>(FindObjectsSortMode.None);
            Require(garments.Length == 12, "Expected all twelve saved locker garments, found " + garments.Length + ".");
            foreach (var garment in garments)
            {
                var colliders = garment.GetComponents<Collider>();
                Require(colliders.Length > 0 && colliders.All(c => c.enabled && !c.isTrigger),
                    garment.DisplayName + " has no enabled solid pickup collider.");
            }
            // The hood stays unwearable until the jacket is stored, so each invalid release must recover.
            var hood = garments.First(g => g.kind == WearableGarment.Kind.Hood);
            Require(!journey.PersonalClothesStored, "Expected a fresh route state.");
            await CheckHand(left, right, leftDevice, InteractorHandedness.Left, hood);
            await CheckHand(right, left, rightDevice, InteractorHandedness.Right, hood);
        }
        finally
        {
            if (leftDevice != null && leftDevice.added) InputSystem.RemoveDevice(leftDevice);
            if (rightDevice != null && rightDevice.added) InputSystem.RemoveDevice(rightDevice);
            InputSystem.RemoveLayout(Layout);
        }
    }

    static async Task CheckHand(Transform controller, Transform other, TouchController device,
        InteractorHandedness handedness, WearableGarment garment)
    {
        other.gameObject.SetActive(false);
        controller.gameObject.SetActive(true);
        var near = controller.GetComponentInChildren<NearFarInteractor>();
        Require(near && near.enabled && near.handedness == handedness, handedness + " saved NearFar interactor is not active.");
        var caster = near.nearInteractionCaster as SphereInteractionCaster;
        Require(caster && caster.physicsTriggerInteraction == QueryTriggerInteraction.Ignore,
            handedness + " saved detector no longer matches the Ignore-triggers regression case.");
        caster.enableStabilization = false;
        near.enableNearCasting = true;
        near.enableFarCasting = false; // Test the physical near grab, not a ray hit on another locker item.
        await Frames(8);
        var action = near.selectInput.inputActionReferencePerformed?.action;
        Require(action != null && action.enabled, handedness + " original Select action is missing or disabled.");
        Require(action.controls.Any(control => control.device == device),
            handedness + " original Select binding did not resolve: " + action.actionMap.name + "/" + action.name +
            "; layout=" + device.layout + "; usages=" + string.Join(",",device.usages) +
            "; isXR=" + InputSystem.IsFirstLayoutBasedOnSecond(device.layout,"XRController") +
            "; grip usages=" + string.Join(",",device.gripPressed.usages) +
            "; bindings=" + string.Join(",",action.bindings.Select(b=>b.effectivePath)) +
            "; controls=" + string.Join(",",action.controls.Select(c=>c.path)));

        var grab = garment.GetComponent<XRGrabInteractable>();
        var collider = garment.GetComponent<BoxCollider>();
        Require(grab && collider, "Hood needs its saved XRGrabInteractable and BoxCollider.");
        var home = garment.transform.position;
        PlaceAtGarment(controller, caster, collider);

        // Deliberately restore the bug, then verify the fix using the same saved detector.
        collider.isTrigger = true;
        Physics.SyncTransforms();
        await Frames(8);
        Require(!Candidates(near).Contains(grab), handedness + " trigger-only baseline unexpectedly detected the hood.");
        collider.isTrigger = false;
        Physics.SyncTransforms();
        await Frames(8);
        Require(Candidates(near).Contains(grab), handedness + " solid hood is absent from actual XRI valid targets.");
        await GrabMoveRelease(controller, near, device, garment, home);

        // A controller/interactor interruption must restore a usable garment rather than leave it stranded.
        PlaceAtGarment(controller, caster, collider);
        await Frames(8);
        Grip(device, true);
        await Frames(8);
        Require(grab.isSelected && near.interactablesSelected.Contains(grab), handedness + " cancellation setup could not grab.");
        controller.position += new Vector3(.2f, -.1f, -.2f);
        await Frames(8);
        near.enabled = false; // XRI's real unregister path sends selectExited with isCanceled.
        await Frames(8);
        Require(!grab.isSelected && !garment.Held && !garment.Equipped && Vector3.Distance(garment.transform.position, home) < .04f,
            handedness + " canceled selection did not return the hood to its locker.");
        Grip(device, false);
        await Frames(4);
        near.enabled = true;
        PlaceAtGarment(controller, caster, collider);
        await Frames(8);
        await GrabMoveRelease(controller, near, device, garment, home);
        // Check distance pickup as well: aim from outside the locker using the saved cone caster.
        var far=near.farInteractionCaster as CurveInteractionCaster;
        Require(far,handedness+" saved far caster missing");
        near.enableNearCasting=false;near.enableFarCasting=true;far.enableStabilization=false;
        controller.rotation=Quaternion.identity;
        controller.position+=collider.bounds.center+Vector3.back*.85f-far.effectiveCastOrigin.position;
        Physics.SyncTransforms();await Frames(12);
        Require(Candidates(near).Contains(grab),handedness+" clothing is not reachable through the open locker with the far ray");
        Grip(device,true);await Frames(15);
        Require(grab.isSelected && near.interactablesSelected.Contains(grab),handedness+" aimed grip did not pick up the garment");
        Grip(device,false);await Frames(12);
        Require(!grab.isSelected&&!garment.Held&&Vector3.Distance(garment.transform.position,home)<.04f,handedness+" distance-grab release did not recover");
        Debug.Log("QuestClothingGrabCheck: " + handedness + " near/far detection, original grip binding, movement, release and cancellation recovery passed.");
    }

    static async Task GrabMoveRelease(Transform controller, NearFarInteractor near, TouchController device,
        WearableGarment garment, Vector3 home)
    {
        var grab = garment.GetComponent<XRGrabInteractable>();
        Grip(device, true);
        await Frames(8);
        Require(near.selectInput.ReadIsPerformed(), near.handedness + " saved input reader did not receive grip.");
        Require(grab.isSelected && near.interactablesSelected.Contains(grab) && !garment.PreviewHeld,
            near.handedness + " grip did not select clothing through XRI.");
        var before = garment.transform.position;
        controller.position += new Vector3(.25f, -.08f, -.2f);
        Physics.SyncTransforms();
        await Frames(15);
        Require(Vector3.Distance(garment.transform.position, before) > .12f,
            near.handedness + " selected garment did not follow the moving controller.");
        Grip(device, false);
        await Frames(12);
        Require(!grab.isSelected && !garment.Held && !garment.Equipped,
            near.handedness + " release did not deselect the garment.");
        Require(Vector3.Distance(garment.transform.position, home) < .04f,
            near.handedness + " invalid fitting did not return the garment to the locker.");
    }

    static List<IXRInteractable> Candidates(NearFarInteractor near)
    {
        var targets = new List<IXRInteractable>();
        near.interactionManager.GetValidTargets(near, targets);
        return targets;
    }

    static void PlaceAtGarment(Transform controller, SphereInteractionCaster caster, Collider collider)
    {
        controller.rotation = Quaternion.identity;
        var point = collider.bounds.center + Vector3.back * (collider.bounds.extents.z + .035f);
        controller.position += point - caster.effectiveCastOrigin.position;
        Physics.SyncTransforms();
    }

    static Transform Named(Transform root, string name)
    {
        var found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        Require(found, "Missing saved rig node: " + name);
        return found;
    }

    static void Grip(TouchController device, bool down)
    {
        // The profile stores gripPressed as a bit. Let the control write its own packed representation.
        using (StateEvent.From(device,out var inputEvent))
        {
            device.grip.WriteValueIntoEvent(down ? 1f : 0f,inputEvent);
            device.gripPressed.WriteValueIntoEvent(down ? 1f : 0f,inputEvent);
            InputSystem.QueueEvent(inputEvent);
        }
    }

    static async Task Frames(int count)
    {
        int start = Time.frameCount;
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (Time.frameCount - start < count)
        {
            Require(EditorApplication.isPlaying && !EditorApplication.isPaused && EditorApplication.timeSinceStartup < deadline,
                "Timed out waiting for running Unity frames.");
            await Task.Yield();
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
