# Opening corridor — Adham

Open `Scenes/Scene01_Opening_Adham.unity`. All authored environment content belongs to `Scene01_Opening_Adham` in the Hierarchy. The separate `Shared_XR_Player` uses the existing Starter Assets controller rig. The parent is organizational, not an edit lock: agree scene ownership with teammates.

## Prototype behavior
- Three-second black fade when the scene starts, followed by overhead synthesized English welcome audio.
- Cyan floor guidance toward a closed elevator; dim corridor sides.
- Walk forward to the corner, then turn left to discover the elevator. Solid corridor walls occlude the elevator from the spawn.
- After the short welcome, the guide teaches left-stick forward/backward/sideways movement and right-stick snap turning. A left-view, world-space Quest Touch Plus schematic animates the sticks and highlights each spoken direction.
- The guide follows the headset while active and disappears after its final instruction. The elevator hint waits until narration finishes.
- Existing controller locomotion, with move speed reduced to 1.4 m/s.
- Within 2 metres horizontally of the call button, a cyan down arrow appears. Aim the controller and press the select trigger. It turns green and the door lifts over 2 seconds, revealing a stationary enclosed cabin.
- After 3 seconds nearby without selecting, an overhead voice says: "To push the button, click on the button." The timer resets on leaving, the hint plays once per scene run, and selection stops it. It waits for the initial welcome voice to finish.
- Floor travel and next-scene handoff remain deferred. ElevatorCallButton.onCalled is available for future integration.

The fade currently begins on scene start, not on detecting headset wear. Restart Play Mode to replay it. Offline TTS now provides the normal guide voice. Existing Daniel/Samantha WAV clips remain as fallback audio.

## Group integration
Use Unity 6000.6.0f1 consistently. Commit Assets and their .meta files together. Keep imported vendor assets unchanged. Assign each teammate a scene to avoid simultaneous scene YAML edits. Use 1 Unity unit = 1 metre, Y up, +Z forward. Keep one shared XR rig and one AudioListener when composing scenes; remove/disable duplicate rigs during integration. This scene is not automatically added to the group's build list.

## Editing
Adjust lighting, panel positions, and guidance directly in the saved scene. OpeningSequence exposes fade duration, camera, audio, and fade material. Tools > Adham > Build Opening Scene creates the scene only if absent; it never overwrites an existing scene. Tools > Adham > Capture Opening Preview renders an editor-only image into Temp/OpeningReview.

## Device acceptance still required
The current target is Quest standalone. Test on the headset: tracking, controller movement/snap turn, collisions, stereo fade coverage, text readability, overhead audio, and frame timing. Mac editor rendering does not establish device compatibility or performance. Existing OpenXR project settings are preserved.

## Elevator settings
Select `ElevatorEntrance/Down button - aim and press trigger` to change activation distance, hint delay, opening time, voice, and open offset. Physical hand-poke input has not been implemented; controller selection uses XRSimpleInteractable. `ElevatorInteractionCheck.Run` provides a batch-mode scripted Play Mode check.

## Locomotion guide
The root LocomotionGuide exposes the five voice clips, narrator, UI references, and headset-relative panel offset. Spoken captions and direction highlights advance on actual audio completion, rather than estimated timestamps. The controller drawing is a simplified instructional schematic. It does not yet score whether the player performs each action. When offline AI is unavailable, the welcome uses Daniel and locomotion uses Samantha. Inactive original corridor modules remain in the hierarchy for reference; the live route is an L shape.

## Offline spoken questions
See [AI_SETUP.md](AI_SETUP.md) for provisioning, supported questions, dependency attribution and hardware validation limits. Hold B on the right controller to record a question; release to send. The native local model routes questions to authored answers with scene progress. No cloud speech service or PC server is used. **Tools > Opening Scene > Build Quest APK** builds this scene explicitly without changing the group's default scene list, once Android Build Support is installed.
