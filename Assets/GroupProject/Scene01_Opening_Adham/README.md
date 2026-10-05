# Opening corridor — Adham

Open `Scenes/Scene01_Opening_Adham.unity`. All authored environment content belongs to `Scene01_Opening_Adham` in the Hierarchy. The separate `Shared_XR_Player` uses the existing Starter Assets controller rig. The parent is organizational, not an edit lock: agree scene ownership with teammates.

## Prototype behavior
- Three-second black fade when the scene starts, followed by overhead synthesized English welcome audio.
- Cyan floor guidance toward a closed elevator; dim corridor sides.
- Left wall movement instructions and right wall welcome transcript.
- Existing controller locomotion, with move speed reduced to 1.4 m/s.
- Within 2 metres horizontally of the call button, a cyan down arrow appears. Aim the controller and press the select trigger. It turns green and the door lifts over 2 seconds, revealing a stationary enclosed cabin.
- After 3 seconds nearby without selecting, an overhead voice says: "To push the button, click on the button." The timer resets on leaving, the hint plays once per scene run, and selection stops it. It waits for the initial welcome voice to finish.
- Floor travel and next-scene handoff remain deferred. ElevatorCallButton.onCalled is available for future integration.

The fade currently begins on scene start, not on detecting headset wear. Restart Play Mode to replay it. The welcome WAV is a replaceable macOS Daniel synthesized prototype, pitched down on its AudioSource.

## Group integration
Use Unity 6000.6.0f1 consistently. Commit Assets and their .meta files together. Keep imported vendor assets unchanged. Assign each teammate a scene to avoid simultaneous scene YAML edits. Use 1 Unity unit = 1 metre, Y up, +Z forward. Keep one shared XR rig and one AudioListener when composing scenes; remove/disable duplicate rigs during integration. This scene is not automatically added to the group's build list.

## Editing
Adjust lighting, panel positions, and guidance directly in the saved scene. OpeningSequence exposes fade duration, camera, audio, and fade material. Tools > Adham > Build Opening Scene creates the scene only if absent; it never overwrites an existing scene. Tools > Adham > Capture Opening Preview renders an editor-only image into Temp/OpeningReview.

## Device acceptance still required
Test Quest standalone and Windows PC Link separately: tracking, controller movement/snap turn, collisions, stereo fade coverage, text readability, overhead audio, and frame timing. Mac editor rendering does not establish device compatibility or performance. Existing OpenXR project settings are preserved.

## Elevator settings
Select `ElevatorEntrance/Down button - aim and press trigger` to change activation distance, hint delay, opening time, voice, and open offset. Physical hand-poke input has not been implemented; controller selection uses XRSimpleInteractable. `ElevatorInteractionCheck.Run` provides a batch-mode scripted Play Mode check.
