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
            keys = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            player.CapturePointer();
            Vector3 start = player.transform.position;
            InputSystem.QueueStateEvent(keys, new KeyboardState(Key.W)); await Task.Delay(650);
            InputSystem.QueueStateEvent(keys, new KeyboardState()); await Task.Delay(80);
            Assert(player.transform.position.z > start.z + .35f, "W key did not move forward: " + start + " -> " + player.transform.position + "; yaw=" + player.transform.eulerAngles + "; deltaTime=" + Time.deltaTime);
            float z = player.transform.position.z;
            InputSystem.QueueStateEvent(keys, new KeyboardState(Key.S)); await Task.Delay(450);
            InputSystem.QueueStateEvent(keys, new KeyboardState()); await Task.Delay(80);
            Assert(player.transform.position.z < z - .2f, "S key did not move backward: z="+z+" -> "+player.transform.position+" yaw="+player.transform.eulerAngles+" focused="+Application.isFocused);
            float x = player.transform.position.x;
            InputSystem.QueueStateEvent(keys, new KeyboardState(Key.D)); await Task.Delay(350);
            InputSystem.QueueStateEvent(keys, new KeyboardState()); await Task.Delay(80);
            Assert(player.transform.position.x > x + .15f, "D key did not strafe");
            float yaw = player.transform.eulerAngles.y;
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(100,20) }); await Task.Delay(100);
            Assert(Mathf.Abs(Mathf.DeltaAngle(yaw, player.transform.eulerAngles.y)) > 5, "Mouse did not rotate view");
            yaw = player.transform.eulerAngles.y;
            InputSystem.QueueStateEvent(keys, new KeyboardState(Key.LeftArrow)); await Task.Delay(100);
            InputSystem.QueueStateEvent(keys, new KeyboardState()); await Task.Delay(80);
            Assert(Mathf.Abs(Mathf.DeltaAngle(yaw, player.transform.eulerAngles.y) + 30) < 2, "Arrow snap turn failed");
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
            float currentPitch = Mathf.DeltaAngle(0, player.view.transform.localEulerAngles.x);
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(0, (currentPitch-targetPitch)/player.mouseSensitivity) });
            await Task.Delay(100); Physics.SyncTransforms();
            InputSystem.QueueStateEvent(keys, new KeyboardState(Key.E)); await Task.Delay(100);
            InputSystem.QueueStateEvent(keys, new KeyboardState());
            Assert(button.Called, "E key did not call aimed elevator; player="+player.transform.position+" button="+button.transform.position);
            Assert(button.indicator.sharedMaterial == button.calledMaterial, "Call indicator not green");
            await Task.Delay(2200);
            var guide = player.tutorial; guide.Begin(); await Task.Delay(200);
            Assert(guide.instruction.text == LocomotionGuide.Captions[0], "Quest tutorial caption missing during keyboard preview");
            Assert(guide.narrator.clip == guide.steps[0] && guide.narrator.isPlaying, "Quest fallback narration missing");
            Assert(guide.panel.activeSelf, "Quest controller illustration hidden during keyboard preview");
            ScreenCapture.CaptureScreenshot("Logs/DesktopPreview.png"); await Task.Delay(700);
            guide.enabled = false;
            InputSystem.QueueStateEvent(keys, new KeyboardState(Key.Escape)); await Task.Delay(100);
            Assert(Cursor.lockState == CursorLockMode.None, "Escape did not release mouse");
            player.enabled = false;
            Assert(!DesktopPreview.Active, "Desktop flag not cleared on disable");
            bool restored = false;
            foreach(var provider in player.GetComponentsInChildren<LocomotionProvider>(true)) restored |= provider.enabled;
            Assert(restored, "XR providers were not restored");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/DesktopPreviewCheck.txt", "PASS: keyboard W/S/D, mouse look, arrow snap turn, eye height, wall collision, forward/left walking route, E ray interaction, green elevator indicator, Quest tutorial/caption/fallback audio during keyboard testing, Esc cursor release, XR provider restoration. Synthetic Input System events in editor; no physical headset test.");
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
