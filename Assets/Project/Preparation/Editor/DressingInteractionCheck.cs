using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GroupProject.Preparation;
using GroupProject.Opening;
using GroupProject.Elevator;

/// <summary>Opt-in isolated play-mode regression. Never opens or saves the user's scene.</summary>
public static class DressingInteractionCheck
{
    static bool running;

    [MenuItem("Tools/Training Rooms/Check Dressing Interactions (Play Mode)")]
    public static async void Run()
    {
        try { await Test(); }
        catch (Exception error) { Debug.LogException(error); }
    }

    [MenuItem("Tools/Training Rooms/Check Dressing Interactions (Play Mode)", true)]
    static bool CanRun() => EditorApplication.isPlaying && !EditorApplication.isPaused && !running;

    public static async Task Test()
    {
        if (!CanRun() || Time.timeScale <= 0) throw new InvalidOperationException("Run the dressing check in unpaused Play mode.");
        running = true;
        GameObject root = null;
        Scene isolated = default;
        try
        {
            isolated = SceneManager.CreateScene("Temporary Dressing Interaction Check " + Guid.NewGuid().ToString("N"));
            root = new GameObject("Temporary dressing test");
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, isolated);
            root.transform.position = new Vector3(1000, 0, 1000);

            var player = Child(root, "Test player").transform;
            // Deliberately separate the root and floor heights: a fixed authored .22m offset must fail.
            player.localPosition = new Vector3(0, .31f, 0);
            var floorObject = Child(root, "Test actual floor");
            floorObject.transform.localPosition = new Vector3(0, .07f, 0);
            var floorCollider = floorObject.AddComponent<BoxCollider>();
            floorCollider.size = new Vector3(12, .3f, 12); // independent top surface is .22m
            var head = Child(player.gameObject, "Test head").transform;
            head.localPosition = new Vector3(0, 1.65f, 0);
            var journey = Child(root, "Test preparation").AddComponent<PreparationJourney>();
            journey.enabled = false;
            journey.head = head;
            journey.showerGate = Child(root, "Test shower gate");
            journey.cleanGate = Child(root, "Test cleanroom gate");
            var rig = Child(root, "Test elevator");
            var ride = rig.AddComponent<ElevatorRide>();
            ride.enabled = false;
            ride.playerRoot = player;
            ride.playerHead = head;
            var callObject = Child(root, "Test call button");
            var call = callObject.AddComponent<ElevatorCallButton>();
            call.enabled = false;
            call.door = Child(root, "Test door").transform;
            ride.callButton = call;
            ride.destinationRoot = Child(root, "Test destination");
            journey.ride = ride;
            var manager = Child(root, "Test interaction manager").AddComponent<XRInteractionManager>();
            call.GetComponent<XRSimpleInteractable>().interactionManager = manager;
            var storage = Child(root, "Test personal storage").transform;
            storage.localPosition = new Vector3(0, 1.1f, 1.2f);

            var garments = new WearableGarment[Enum.GetValues(typeof(WearableGarment.Kind)).Length];
            for (int i = 0; i < garments.Length; i++)
            {
                var item = Child(root, "Test " + (WearableGarment.Kind)i);
                item.transform.localPosition = new Vector3(2 + i * .2f, 1.1f, 2);
                item.AddComponent<BoxCollider>().isTrigger = true;
                item.AddComponent<Rigidbody>().isKinematic = true;
                var grab = item.AddComponent<XRGrabInteractable>();
                grab.interactionManager = manager;
                var garment = item.AddComponent<WearableGarment>();
                garment.kind = (WearableGarment.Kind)i;
                garment.journey = journey;
                garment.storagePoint = storage;
                if (garment.Footwear)
                {
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Project/Preparation/Art/" + DressingRoomExperience.MeshKey(garment.kind) + ".asset");
                    Require(mesh, "Missing footwear mesh for floor-contact check.");
                    item.AddComponent<MeshFilter>().sharedMesh = mesh;
                    item.AddComponent<MeshRenderer>();
                }
                garments[i] = garment;
            }
            root.SetActive(true);
            await Frames(2);
            Physics.SyncTransforms();

            // A correct body gesture cannot bypass the personal-storage prerequisite.
            var hood = garments[(int)WearableGarment.Kind.Hood];
            await ApproachAndRelease(journey, hood);
            Require(!hood.Equipped && !journey.Dressed, "Garment accepted before personal clothing was stored.");

            var jacket = garments[(int)WearableGarment.Kind.PersonalJacket];
            jacket.SetPreview(true);
            jacket.transform.position = storage.position;
            jacket.SetPreview(false);
            await Frames(3);
            Require(jacket.Stored && journey.PersonalClothesStored, "Personal jacket failed to store after release.");

            // The registered XR event must cancel a queued valid release, not equip or strand the item.
            var hoodHome = hood.transform.position;
            hood.SetPreview(true);
            hood.transform.position = hood.TargetPosition();
            hood.SetPreview(false);
            hood.GetComponent<XRGrabInteractable>().selectExited.Invoke(new SelectExitEventArgs { isCanceled = true });
            await Frames(3);
            Require(!hood.Equipped && !hood.Held && Vector3.Distance(hood.transform.position, hoodHome) < .001f,
                "Canceled XR grab equipped or stranded the garment instead of returning it home.");
            // A fresh grab supersedes recovery; the old callback must not pull the object from the new hand.
            hood.GetComponent<XRGrabInteractable>().selectExited.Invoke(new SelectExitEventArgs { isCanceled = true });
            hood.SetPreview(true);
            var regrabPosition = head.position + head.forward * .55f;
            hood.transform.position = regrabPosition;
            await Frames(3);
            Require(hood.Held && !hood.Equipped && Vector3.Distance(hood.transform.position, regrabPosition) < .001f,
                "Stale cancellation reset a newly grabbed item.");
            hood.transform.position = hoodHome;
            hood.SetPreview(false);
            await Frames(3);

            var overshoe = garments[(int)WearableGarment.Kind.LeftOvershoe];
            await ApproachAndRelease(journey, overshoe);
            Require(!overshoe.Equipped, "Overshoe accepted before its boot.");

            var leftBoot = garments[(int)WearableGarment.Kind.LeftBoot];
            leftBoot.SetPreview(true);
            leftBoot.transform.position = leftBoot.TargetPosition() + new Vector3(1.2f, .45f, 0);
            leftBoot.SetPreview(false);
            await Seconds(.8f);
            Require(!leftBoot.Equipped && !journey.HasGarment(WearableGarment.Kind.LeftBoot), "Arbitrary floor drop counted as footwear fitted.");

            // This is above, not inside, the final sphere: exercise the settling coroutine.
            leftBoot.SetPreview(true);
            leftBoot.transform.position = leftBoot.TargetPosition() + Vector3.up * .45f;
            Require(leftBoot.ReadyToFit, "Boot drop zone did not show ready feedback.");
            leftBoot.SetPreview(false);
            await Frames(2);
            Require(!leftBoot.Equipped, "Boot counted before its floor drop settled.");
            await Seconds(.8f);
            Require(leftBoot.Equipped, "Boot did not settle into the marked foot area.");
            AssertSoleOnFloor(leftBoot, floorCollider);
            // Moving the floor and grounded origin must not preserve a baked-in height offset.
            floorObject.transform.position += Vector3.up * .17f;
            player.position += Vector3.up * .17f;
            Physics.SyncTransforms();
            await Frames(2);
            AssertSoleOnFloor(leftBoot, floorCollider);
            Require(!journey.HasGarment(WearableGarment.Kind.RightBoot) && !journey.Dressed, "One boot completed the pair or entire outfit.");

            // Every remaining item goes through PreviewPose from the actual distant hold,
            // so tests cannot pass simply by teleporting all items to TargetPosition().
            foreach (var garment in garments)
            {
                if (garment.Stored || garment.Equipped) continue;
                await ApproachAndRelease(journey, garment);
                Require(garment.Equipped, garment.DisplayName + " cannot be fitted through the preview gesture.");
                if (garment.Footwear) AssertSoleOnFloor(garment, floorCollider);
            }
            Require(journey.Dressed && !journey.showerGate.activeSelf, "Completed outfit did not unlock the shower.");
            foreach (var garment in garments)
                Require(garment.TryPlace(), "Already fitted item was not idempotent.");
            Require(journey.Dressed, "Repeated placement changed completion state.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/DressingInteractionCheck.txt", "PASS: canceled XR event recovery; regrab supersedes stale cancellation; mesh soles touch independent floor collider at changed elevations; personal-storage prerequisite; invalid floor release rejected; boot drop settles before completion; one boot does not complete the pair; overshoe prerequisite; all garment preview approaches and releases; idempotent completion; shower unlock. Isolated editor simulation, not Quest tracking or comfort validation.");
            Debug.Log("Dressing interaction check passed. See Logs/DressingInteractionCheck.txt.");
        }
        catch (Exception error)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/DressingInteractionCheck.txt", "FAIL: " + error);
            throw;
        }
        finally
        {
            if (root) UnityEngine.Object.Destroy(root);
            if (isolated.IsValid() && isolated.isLoaded) await SceneManager.UnloadSceneAsync(isolated);
            running = false;
        }
    }

    static void AssertSoleOnFloor(WearableGarment garment, Collider floor)
    {
        var bounds = garment.GetComponent<MeshFilter>().sharedMesh.bounds;
        float actualSole = garment.transform.TransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)).y;
        Require(Mathf.Abs(actualSole - floor.bounds.max.y) < .002f,
            garment.DisplayName + " sole does not contact the actual floor: " + actualSole + " vs " + floor.bounds.max.y);
    }

    static GameObject Child(GameObject parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    static async Task ApproachAndRelease(PreparationJourney journey, WearableGarment garment)
    {
        garment.SetPreview(true);
        garment.transform.position = journey.head.position + journey.head.forward * .5f;
        for (int i = 0; i < 45; i++)
        {
            // Do not leave simulated inputs active while other live scene scripts update.
            bool previous = QuestControllerPreview.Trigger;
            try { QuestControllerPreview.Trigger = true; journey.PreviewPose(garment, 1f / 30f); }
            finally { QuestControllerPreview.Trigger = previous; }
            await Frames(1);
        }
        garment.SetPreview(false);
        await Seconds(.8f);
    }

    static async Task Frames(int count)
    {
        int target = Time.frameCount + count;
        double timeout = EditorApplication.timeSinceStartup + 10;
        while (Time.frameCount < target)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.timeSinceStartup > timeout)
                throw new InvalidOperationException("Dressing test stopped because Play mode paused or ended.");
            await Task.Yield();
        }
    }
    static async Task Seconds(float seconds)
    {
        float until = Time.time + seconds;
        double timeout = EditorApplication.timeSinceStartup + seconds + 10;
        while (Time.time < until)
        {
            if (Time.timeScale <= 0 || EditorApplication.timeSinceStartup > timeout)
                throw new InvalidOperationException("Dressing test timed out while waiting for the release gesture.");
            await Frames(1);
        }
    }
    static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
