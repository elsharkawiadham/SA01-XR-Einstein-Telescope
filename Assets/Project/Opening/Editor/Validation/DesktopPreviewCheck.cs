using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using GroupProject.Opening;

[InitializeOnLoad]
public static class DesktopPreviewCheck
{
    static bool running;
    static DesktopPreviewCheck() { EditorApplication.update += Tick; }
    public static void Run()
    {
        EditorSceneManager.OpenScene(OpeningProjectSetup.ScenePath);
        var opening = UnityEngine.Object.FindFirstObjectByType<OpeningSequence>();
        opening.enabled = false;
        var ai = opening.aiGuide;
        ai.gameObject.SetActive(false); opening.aiGuide = null; opening.locomotionGuide.aiGuide = null;
        ai.elevator.aiGuide = null;
        SessionState.SetBool("Opening.DesktopCheck", true);
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        int exit = SessionState.GetInt("Opening.DesktopExit", -1);
        if(exit >= 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
        { SessionState.SetInt("Opening.DesktopExit", -1); EditorApplication.Exit(exit); return; }
        if(running || !SessionState.GetBool("Opening.DesktopCheck", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        running = true; Check();
    }
    static void Assert(bool value, string message) { if(!value) throw new Exception(message); }
    static void Place(DesktopPreview player, Vector3 position, float yaw)
    {
        var body = player.GetComponent<CharacterController>(); body.enabled = false;
        player.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        player.view.transform.localRotation = Quaternion.identity;
        body.enabled = true; Physics.SyncTransforms();
    }
    static async void Check()
    {
        Keyboard keys = null; Mouse mouse = null;
        var originalFocus = InputSystem.settings.editorInputBehaviorInPlayMode;
        var originalBackground = InputSystem.settings.backgroundBehavior;
        try
        {
            await Task.Delay(1500);
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var player = UnityEngine.Object.FindFirstObjectByType<DesktopPreview>();
            var button = UnityEngine.Object.FindFirstObjectByType<ElevatorCallButton>();
            await Task.Delay(150);
            Assert(player.IsDesktop, "Desktop did not activate without a headset");
            Assert(Mathf.Abs(player.view.transform.position.y - player.transform.position.y - 1.65f) < .05f, "Wrong desktop eye height");
            foreach(var provider in player.GetComponentsInChildren<LocomotionProvider>(true)) Assert(!provider.enabled, "XR provider competing with desktop movement");

            player.CapturePointer();
            Vector3 start = player.transform.position;
            QuestControllerPreview.Move=Vector2.up; await Task.Delay(650);
            QuestControllerPreview.Move=Vector2.zero; QuestControllerPreview.Turn=Vector2.zero; await Task.Delay(80);
            Assert(player.transform.position.z > start.z + .35f, "Left joystick forward did not move forward: " + start + " -> " + player.transform.position + "; yaw=" + player.transform.eulerAngles + "; deltaTime=" + Time.deltaTime);
            float z = player.transform.position.z;
            QuestControllerPreview.Move=Vector2.down; await Task.Delay(450);
            QuestControllerPreview.Move=Vector2.zero; QuestControllerPreview.Turn=Vector2.zero; await Task.Delay(80);
            Assert(player.transform.position.z < z - .2f, "Left joystick backward did not move backward: z="+z+" -> "+player.transform.position+" yaw="+player.transform.eulerAngles+" focused="+Application.isFocused);
            float x = player.transform.position.x;
            QuestControllerPreview.Move=Vector2.right; await Task.Delay(350);
            QuestControllerPreview.Move=Vector2.zero; QuestControllerPreview.Turn=Vector2.zero; await Task.Delay(80);
            Assert(player.transform.position.x > x + .15f, "Left joystick sideways did not strafe");
            float yaw = player.transform.eulerAngles.y;
            QuestControllerPreview.Aim=Vector2.right; await Task.Delay(250); QuestControllerPreview.Aim=Vector2.zero;
            Assert(Mathf.Abs(Mathf.DeltaAngle(yaw, player.transform.eulerAngles.y)) > 5, "Aim control did not rotate view");
            yaw = player.transform.eulerAngles.y;
            QuestControllerPreview.Turn=Vector2.left; await Task.Delay(100);
            QuestControllerPreview.Move=Vector2.zero; QuestControllerPreview.Turn=Vector2.zero; await Task.Delay(80);
            Assert(Mathf.Abs(Mathf.DeltaAngle(yaw, player.transform.eulerAngles.y) + 30) < 2, "Right joystick snap turn failed");
            Place(player, new Vector3(0,.1f,2), 0);
            for(int i=0;i<300;i++) player.Move(Vector2.right, 1f/60);
            Assert(player.transform.position.x < 2.5f, "Walked through corridor wall");
            Place(player, new Vector3(0,.1f,-.8f), 0);
            Assert(!player.TryInteract(), "Distant elevator interaction accepted");
            for(int i=0;i<377;i++) player.Move(Vector2.up, 1f/60);
            Assert(player.transform.position.z > 7.5f, "Forward route blocked");
            player.transform.rotation = Quaternion.Euler(0,-90,0);
            for(int i=0;i<370;i++) player.Move(Vector2.up, 1f/60);
            Assert(player.transform.position.x < -7, "Left corridor route blocked");
            await Task.Delay(150);
            var collider = button.GetComponent<Collider>();
            Vector3 direction = collider.bounds.center - player.view.transform.position;
            player.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 0);
            float targetPitch = -Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg;
            player.view.transform.localRotation=Quaternion.Euler(targetPitch,0,0);Physics.SyncTransforms();
            Assert(player.TryInteract(), "Virtual-controller aimed elevator interaction failed");
            Assert(button.indicator.sharedMaterial == button.calledMaterial, "Call indicator not green");
            await Task.Delay(2200);
            var guide = player.tutorial; guide.Begin(); await Task.Delay(200);
            Assert(guide.instruction.text == LocomotionGuide.Captions[0], "Quest tutorial caption missing during controller preview");
            Assert(guide.narrator.clip == guide.steps[0] && guide.narrator.isPlaying, "Quest fallback narration missing");
            Assert(guide.panel.activeSelf, "Quest controller illustration hidden during controller preview");
            ScreenCapture.CaptureScreenshot("Logs/DesktopPreview.png"); await Task.Delay(700);
            guide.enabled = false;
            QuestControllerPreview.ResetControls(); await Task.Delay(100);
            Assert(Cursor.lockState == CursorLockMode.None, "Virtual controls require a visible pointer");
            player.enabled = false;
            Assert(!DesktopPreview.Active, "Desktop flag not cleared on disable");
            bool restored = false;
            foreach(var provider in player.GetComponentsInChildren<LocomotionProvider>(true)) restored |= provider.enabled;
            Assert(restored, "XR providers were not restored");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/DesktopPreviewCheck.txt", "PASS: virtual joystick forward/back/strafe, aim and snap turn, eye height, wall collision, walking route, aimed elevator interaction, Quest captions and narration, XR provider restoration. Synthetic control-state test, not tracked Quest hardware.");
            SessionState.SetInt("Opening.DesktopExit", 0);
        }
        catch(Exception e) { File.WriteAllText("Logs/DesktopPreviewCheck.txt", "FAIL: "+e); Debug.LogException(e); SessionState.SetInt("Opening.DesktopExit", 1); }
        finally
        {
            if(keys != null)InputSystem.RemoveDevice(keys); if(mouse != null)InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = originalFocus; InputSystem.settings.backgroundBehavior = originalBackground;
            SessionState.SetBool("Opening.DesktopCheck", false); EditorApplication.isPlaying = false;
        }
    }
}
