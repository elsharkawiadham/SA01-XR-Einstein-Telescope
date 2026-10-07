# Validation — 2026-10-01

Unity 6000.6.0f1, macOS editor.

- Scripts compiled without reported C# errors; no shader compilation errors found.
- Saved scene contains a single XR camera and assigned sequence/audio/fade references.
- Play Mode check passed after fade: runtime overlay removed; welcome AudioSource playing.
- Editor-rendered forward and left-facing images inspected. Corrected reversed/oversized text and lighting that was unavailable under the existing performance URP preset.
- Existing controller rig reused. No physical controller movement or standalone/PC build validation performed.
- Elevator opening/travel and headset-wear detection are not implemented in this stage.
- Unrelated Unity AI assistant entitlement/network errors appeared in the project log.

Before creating the new scene, Unity requested saving the already-modified SampleScene. It was saved to preserve the open editor's state; its scene and scene-template serialization changes should be reviewed separately from this folder.

## Elevator update — 2026-10-05

Scripted Play Mode check passed: distant arrow hidden and presses rejected; near arrow visible; leaving resets hint delay; delayed hint triggers; XR select event calls once and cancels voice; repeated selection ignored; door reaches its open position; indicator turns green; prompt suppressed after immediate selection. These tests invoke the XR select event programmatically, not through a physical controller. OpenXR reports runtime unavailable on this Mac; Quest standalone and PC Link acceptance remain outstanding.

## Locomotion route and guide — 2026-10-06

Scripted Play Mode validation passed: physics linecasts confirm the elevator is occluded from spawn and visible after the left turn; forward and left walking paths are clear; the welcome finishes before the guide; five real voice clips match their captions; the panel follows the left view and hides after narration; elevator help waits for the guide then resumes. Forward, guide-visible, and left-turned editor renders were inspected. World-space labels now use depth-tested text so the elevator label cannot show through the wall.

This is an introductory demonstration, not a scored movement exercise. The controller illustration is schematic. Physical Quest stick mappings, stereo text legibility, comfort of the headset-relative guide, collisions while walking, and standalone/PC performance still require device testing.

The relocated elevator also passed the existing scripted regression check: nearby visibility, distant press rejection, hint delay/reset, selection cancellation, repeated selection handling, door opening, green indicator, and prompt suppression after immediate selection.


## Offline spoken guide — 2026-10-06

- Unity 6000.6.0f1 compilation passed with pinned LLMUnity, Sherpa ONNX and UniTask packages.
- `StandaloneAICheck.Run`: PASS, exit 0. Native local TTS synthesized “How do I turn left to find the elevator?”, Whisper transcribed it correctly, LLMUnity classified the question, and the authored right-joystick answer was synthesized and played. Automatic authored narration also passed. Last complete run: 13.6 seconds including initialization, inference and playback on this Mac; this is not headset latency.
- The initial unrestricted 0.5B model gave incorrect controller advice. Final implementation only speaks authored answers selected by question intent and measured progress. It does not offer unrestricted general conversation.
- An agent cancellation call after native model disposal caused an initial teardown crash. Removed that unsafe shutdown call; final test exited Play Mode and the Unity process with code 0.
- `LocomotionGuideCheck.Run`: PASS, exit 0 with offline narration enabled. Welcome precedes all five synchronized controller lessons; corridor occlusion/path checks pass; left panel follows the head and hides on completion; the elevator reminder waits and resumes.
- Offline dependency provisioning script ran successfully against checksum-verified archives. TTS native pool is limited to one engine. Generated models/native binaries are excluded from Git; settings, importer metadata and provisioning instructions remain in source.
- Quest APK not built: Android Build Support/SDK/NDK/OpenJDK are absent from this editor installation. Microphone permissions, physical B input, Android native loading, frame rate, spatial audio and response latency require a real Quest 3 standalone test with Wi-Fi disabled.
- `ElevatorInteractionCheck.Run`: PASS, exit 0 after integration, with AI disabled to exercise recorded fallback. Proximity, timer reset, XR select, single-call behavior, stopped hint, green indicator and open-door endpoint passed.
