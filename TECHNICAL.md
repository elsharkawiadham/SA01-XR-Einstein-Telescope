# Technical guide — Einstein Telescope VR Training

For developers and assessors. For the purpose, controls and quick start, see [README.md](README.md). Validation statements below describe recorded checks, not a release certification.

## Scene and editing

The single main scene is `Assets/Project/Scenes/01_Opening.unity`. It contains the opening, elevator, changing room, shower room and cleaning station, with one shared player and parrot. It is the only scene in Build Settings and the configured Play Mode start scene. Open this scene to edit or demonstrate the project.

The elevator reveals the lower-floor group after descent; it does not load another scene. `02_LowerFloor` is intentionally inactive before arrival. Original scene snapshots live in `Assets/Project/Scenes/Archive` for recovery and are excluded from builds.

### Jump directly to cleaning

- **Tools > Cleaning > Preview With Shared Player**: quick interaction preview without AI initialization.
- **Tools > Cleaning > Preview With Offline Q&A**: the same preview with spoken questions enabled.

These previews start at the bench. Stopping Play reopens the saved opening scene. Preview positions are not saved into that scene.

## Offline spoken questions

Speech recognition, question routing and speech synthesis run locally. No API key, Ollama server, PC connection or microphone upload is required by the Q&A runtime. Answers stay within authored training facts; this is not unrestricted conversation.

On a **fresh clone**, close Unity and run this from the repository root using Python 3.10+ and curl:

```sh
python3 Tools/setup_offline_guide.py
```

This downloads checksum-verified models and native libraries for Apple Silicon and Quest ARM64. Reopen Unity, allow LLMUnity's native setup to finish, then choose **Tools > Opening Scene > Configure Standalone AI**. Large dependencies are excluded from Git, so each fresh checkout needs this one-time setup. Windows editor speech requires matching Sherpa 1.13.0 Windows x64 native libraries; the supplied provisioning script targets Mac Apple Silicon and Quest.

Wait for the guide to prepare and finish speaking before asking. Allow microphone permission when prompted. Hold B, speak, then release; recordings stop after 12 seconds. Example questions:

- “How do I turn?”
- “Which wipe should I use for Class A?”
- “How do I use the inspection lamp?”
- “How do I change the wipe face?”
- “What should I do next?”

If models are unavailable, recorded instructions and the cleaning exercise still work. Spoken questions need the local backend to be ready. If voice initialization fails, confirm that provisioning completed and read the Unity Console.

## Training content and limits

The cleaning exercise follows the client's *Cleaning and decontamination* slides: Anticon Gold polyester for Class A; KIMTECH W4 polypropylene for Class B and below, not Class A. The client does not recommend Anticon Gold for Class B. Wiping uses parallel, slightly overlapping passes with a lift at the end and fresh faces in the sequence flip, refold, flip.

The lamp, visible particles, pass spacing and trigger-operated folds are teaching simplifications. They do not measure or certify cleanliness. Fingerprints and oils may remain invisible. Chemical use, ultrasonic baths, oven operation, microscope training and gowning procedures are not implemented as approved training modules. Follow the client's approved procedures and trained staff for real work.

## Build for Quest

1. Install Android Build Support, Android SDK/NDK and OpenJDK for this Unity version through Unity Hub.
2. Provision the offline models and finish their Unity import.
3. Choose **Tools > Opening Scene > Build Quest APK**. The project targets IL2CPP / ARM64; output is `Builds/OpeningQuest.apk`. This is a build procedure, not evidence that an APK has been produced.
4. Install the APK on a developer-enabled Quest using your normal device deployment workflow. Grant microphone access and test with Wi-Fi disabled.

The build menu runs setup tools that can regenerate guide objects: save and review your scene changes first. Only `Assets/Project/Scenes/01_Opening.unity` is included in the build.

**Validation status:** Mac editor checks passed for the combined-scene elevator arrival, the route through changing and shower rooms, the cleaning trials, lamp/tool interactions, walking-route geometry, player-controller movement in both directions and shared camera/audio setup. Offline speech recognition → local model routing → spoken reply also passed with external outbound networking blocked, using synthetic questions. An APK build and physical Quest microphone/controller, comfort and performance checks remain outstanding. Unity currently also reports a separate internal SearchDatabase indexing exception and unavailable OpenXR runtime on this Mac; these are distinct from the passing exercise checks.

## Project organization

| Location | Purpose |
| --- | --- |
| `Assets/Project/Scenes` | Single main scene and archived pre-merge snapshots |
| `Assets/Project/Opening` | Opening effects, locomotion tutorial and controller preview |
| `Assets/Project/Elevator` | Elevator journey and interaction logic |
| `Assets/Project/Guide` | Parrot, dialogue cloud, footsteps and elevator sounds |
| `Assets/Project/Preparation/Prefabs` | Jacy's reusable changing and shower environment |
| `Assets/Project/Cleaning` | Cleaning station, tools, authored dialogue and checks |
| `Assets/Project/ClientCleanroom` | Original client GLB reference |
| `Assets/ThirdParty`, imported asset packs | Environment and dependency assets |
| `Assets/Plugins`, `Assets/StreamingAssets` | Native libraries and offline models; Unity requires these folder names |
| `Packages`, `ProjectSettings` | Package versions and Unity configuration |
| `Tools` | Offline dependency setup, network-test profile and sound generator |

Keep one **Shared_XR_Player**. The parrot remains under that player. The main Hierarchy contains `Scene01_Opening_Adham` and `02_LowerFloor`; the latter groups `01_ArrivalLanding`, `02_Preparation_Jacy` (original approach/reference), `02_Preparation_Interactive` (editable rebuilt rooms), `03_Cleaning_Adham`, and `04_ConnectingRoute`. The elevator stays with the opening so the player remains in the same cabin during descent. Preserve `.meta` files when moving assets. Imported locker assets are under `Assets/ThirdParty/LowPolyLockers`. Unreferenced vendor demo scenes and the superseded lower-floor template have been removed; shared XR sample prefabs, models, materials, licences and packages are retained.

The README is the user guide; this file contains technical setup, research and validation details. Narration `.txt` files are functional inputs to the station builder, not extra documentation. License files and supplied third-party asset documentation remain with their dependencies. `Library`, `Temp`, `Logs`, `.offline-guide-cache` and local model binaries are generated/downloaded data excluded from Git; their size is not the size of the source repository. Do not delete installed models if you need offline Q&A.

For maintenance, `Tools > Cleaning > Select Training Station` selects the existing station without regenerating the route. `Tools > Cleaning > Run Exercise Checks` checks the exercise and walks through the combined preparation route. `Tools > Elevator > Run Descent Check` checks single-scene arrival and doorway safety. `StandaloneAICheck.ConfigureAndRun` is the batch entry point for local speech testing; `Tools/offline_qa_no_network.sb` is the macOS network restriction profile used for offline verification. Results go to ignored `Logs/`. `Tools/generate_guide_sounds.py` regenerates procedural sound effects when run from the repository root.

## Dependencies and attribution

Packages are pinned in `Packages/manifest.json` and `Packages/packages-lock.json`.

- Unity XR Interaction Toolkit, OpenXR, URP, glTFast and Draco provide XR interaction, rendering and client-model import.
- LLMUnity 3.0.3 uses Qwen2.5-0.5B-Instruct Q4_K_M locally for question routing (Qwen model: Apache-2.0).
- Unity Sherpa ONNX uses Sherpa 1.13.0, Whisper tiny.en and Piper en_US-lessac-low. Keep downloaded model cards and voice terms with their assets.
- The unmodified Sherpa .NET Standard binding is distributed under Apache-2.0; see `Assets/Plugins/SherpaOnnx/LICENSE.txt`. Native files and models are provisioned by the setup script.
- The scientist parrot is an AI-synthesized 2D variation of the supplied reference, with procedural wing/beak animation. It is not a rigged 3D character. Recorded prototype prompts use macOS voices; footsteps and elevator effects are procedurally generated.
- Client slides and the cleanroom GLB are supplied project reference materials. Imported asset packs retain their own terms and notices.

## Voice replacement

`Voiceover/SpokenLines.csv` contains the authored narration and Q&A wording, including expanded dynamic variants, for recording a replacement voice. Keep the IDs with your exported audio. This export does not replace the current clips automatically. The testing version uses rigged Unity XR Hands meshes with controller-driven finger poses; see the visible-body section below.

### Preparation rehearsal and walking pace

The elevator introduces five checklist tasks: clothing, air shower, inspection, guided Class A cleaning and independent Class B cleaning. Use trigger for locker doors and the shower start button; use grip to take/release clothing. In the desktop Quest preview, hold the virtual trigger to bring the held garment to its fitting target, then use grip to release. The checklist follows the existing cleaning exercise. Walking pace shows green (good pace), amber (slow down) and red (too fast); sustained fast movement prompts the parrot with a 20-second cooldown. Configurable thresholds of 0.65 and 1.0 m/s are prototype feedback settings, not published ETpathfinder limits. Elevator travel and large position jumps are excluded.

Dedicated clothing and an air shower are documented by [Maastricht University](https://www.maastrichtuniversity.nl/news/looking-back-etpathfinders-opening). Exact clothing requirements, order and shower timing still require client confirmation. Run `PreparationCheck.Run` for the scripted preparation checks.

Recorded voices `VOICE_001`–`VOICE_060` are imported under `Assets/Project/Guide/Audio/Generated`. The recorded-voice library maps existing narration clips and exact spoken text to these recordings; unmatched lines retain the offline voice. Source MP3s remain in `audio/`. IDs 061–087 and newer preparation dialogue have no supplied recording yet. `Tools > Guide > Import Recorded Voices` rebuilds the library from its import manifest.

Use **Tools > Training Rooms > Select Changing and Shower Rooms** or **Select Cleaning Station** to find and frame the relevant scene section. The tool enables the lower floor for editing; the elevator hides it automatically on normal Play Mode startup until arrival. The checklist uses a compact paper design, draws in front of scenery and can be hidden with left-controller X, its clickable header or a spoken checklist request. The welcome recording was replaced with the supplied `voice_019.mp3`.

### Dressing interactions

The changing-room prototype now has a dedicated clothing checklist grouped by head, body, hands and feet. Store the personal jacket, then fit the cap, hood, mask, safety glasses, coverall, two gloves, two boots and two overshoes. Grip an item to reveal its labelled target; release grip when the target is ready. Gloves use the opposite tracked hand. Release footwear above the marked foot space and wait for it to settle; arbitrary floor drops do not count. Overshoes require the matching boot. The shower unlocks only when every item is fitted. These requirements demonstrate the supplied clothing reference, not a client-approved gowning order.

In the mouse-operated Quest preview, grip to pick up, hold the virtual trigger to bring the held item to its fitting target, then grip again to release. This assists the virtual hand only; headset users physically move their controllers. Individual items remain checked, and the parrot provides item-specific feedback. An interrupted grab returns an unfinished item to its locker; a fresh grab cancels the old recovery. Footwear settles onto the actual room floor, using the sole of its mesh to avoid hovering.

Clothing uses original mesh assets with seams, pleats and material details. The fitted suit appears on a deforming body, gloves replace the animated hand materials, and footwear follows inferred foot positions. Headwear casts shadows without obscuring the first-person camera. Body and leg poses are inferred from the headset and controllers; this is not full-body tracking or cloth simulation. All twelve dressing items are saved under `02_LowerFloor/02_Preparation_Interactive/01_ChangingRoom` so you can edit them outside Play mode. The outfit list appears in the upper left while in that room; hide/show it with left-controller X, its header, or say “hide my outfit list” / “show my clothing list”. Refresh generated meshes with **Tools > Training Rooms > Refresh Dressing Assets**. **Check Dressing Interactions (Play Mode)** runs an isolated scripted check without saving the open scene. Quest comfort, actual tracked dressing and Android performance require headset validation.

Editor validation passed for all twelve saved items, dressing gestures, interrupted-grab recovery, rapid regrabbing, independent sole-to-floor contact at different elevations, shower unlocking, and persistent checklist voice hide/show. The checklist uses consistent spacing, grouped body regions, and both text and checkmarks for feedback. These editor checks do not verify tracked Quest hands, stereo comfort or Android performance.

### Visible body and controller hands

`Shared_XR_Player` in the main scene owns `AnimatedControllerHands` and `PlayerBodyAvatar`. The existing Quest controller objects still supply tracking, grip, trigger, joystick and interaction input; only their hardware meshes are hidden. The visible hands reuse the rigged **Unity XR Hands** sample meshes already in the project. Grip curls the middle, ring and little fingers; trigger curls the index finger; thumb touch/buttons animate the thumb. These are controller-driven poses, not optical finger tracking or Meta Capsense. Hands start bare and each fitted glove changes only its matching hand.

Stand comfortably for the first two seconds of valid headset tracking. The saved XR Origin requests floor-relative tracking, and changing from desktop preview to Quest Link starts a fresh height measurement. The body measures floor-to-eye height once and retains those proportions when you crouch. Looking down reveals the torso, sleeves and feet; a small head turn leaves the torso steady, while larger turns let it follow. Arms aim toward the controller wrists. The legs are estimated, so their pose cannot match independently tracked feet. After recentering, `PlayerBodyAvatar.RecalibrateStandingHeight()` can repeat standing calibration without moving the camera.

To follow the implementation, start with `WearableGarment` in `Assets/Project/Preparation/Runtime`: pickup uses an `XRGrabInteractable`, fitting validates the matching body target, and successful fitting updates the checklist plus hands/body appearance. The original pickup failure came from trigger-only clothing colliders while the controller casters ignored triggers. Clothing now has solid query colliders; it ignores the player's walking capsule so a held garment does not push the player around. `AnimatedControllerHands` changes finger bones; `PlayerBodyAvatar` supplies body proportions, arm poses and worn-clothing anchors. Both are in `Assets/Project/Opening/Runtime`.

For maintenance, **Tools > Training Rooms > Configure Controller Hands and Body** updates the existing saved player and garment colliders without rebuilding the rooms. `ControllerEmbodimentCheck.ConfigureAndRun` is the batch entry for body, hand and worn-outfit checks; `QuestClothingGrabCheck.Run` tests the saved XRI near-grab path with synthetic OpenXR controller grip input. Use these with the project closed. Results and before/after images are written to ignored `Logs/`. Synthetic input does not replace a physical Quest Link test.

Current editor checks pass for both saved controllers using near pickup and aimed pickup, grip movement/release, interrupted-grab recovery, independent glove materials, worn body updates, stable crouching proportions, and floor contact at 1.25 m and 1.90 m eye heights. The physical Quest Link test and Android performance/build validation remain pending.


### Lecturer feedback implementation and research (27 October 2026)

The saved route remains `01_Opening`: hallway → elevator → changing room → air shower → working cleaning exercise. `LecturerFeedbackSetup.Run` updates the existing scene without rebuilding rooms. The cabin and lower floor each have a `Training feedback` child containing the new controls, signs, local fill lights and outfit mirrors. The mirrors reflect the avatar and equipped clothing only, not the surrounding room. They use reflected meshes and a stencil aperture, without adding player cameras or listeners, and update only when the user is nearby and in front. This is a scoped outfit-check feature; stereo appearance and CPU cost must still be measured on Quest. Arm smoothing affects inferred elbow bend only; controller wrists and the tracked camera are not delayed.

Controls share cyan ready, pale cyan hover, green accepted and amber rejected feedback through `InteractionFeedback`. B1 and shower controls have an animated graphic face while their collision target stays fixed. Elevator state prevents duplicate departures; the overhead display changes from ground to descent depth and B1. Locker handles and their trigger labels are highlighted, and the room instructions sit above the locker frontage. The checklist and parrot offsets moved left by 0.05 m; controller X and spoken checklist show/hide commands are retained. Local lights have no additional real-time shadows. These choices require a headset visibility and performance review; brightness and frame rate have not been certified for Quest.

**Hallway design: actual team rationale.** The original brief asked the user to walk forward and then turn left before seeing the elevator, specifically to practise joystick navigation. The route and `LocomotionGuide` implement this staged introduction and reveal. We retain that training rationale. We do not claim that a turning hallway reduces cybersickness; turning adds visual motion and may be uncomfortable for some users. The route should be evaluated with new users for legibility, turning comfort and task completion.

**Movement pacing: implementation versus evidence.** The existing joystick movement remains unchanged (desktop preview maximum 1.4 m/s; existing XR provider settings retained). `PreparationJourney` measures horizontal headset displacement per frame, smooths it with `1-exp(-4*deltaTime)`, and excludes elevator travel, large position jumps and rig turns. This is a pacing estimate that includes physical head translation, not a calibrated walking-speed instrument. The visual caution boundary changed from 0.65 to **1.3 m/s**, and the excessive-speed boundary from 1.0 to **2.0 m/s**. Equality does not trigger the higher band. A spoken warning requires more than two continuous seconds above 2.0 m/s, an idle guide, and a 20-second cooldown. The threshold change follows lecturer feedback; it does not increase locomotion speed. The intended benefit is gentle, deliberate movement and fewer interruptions, not a demonstrated reduction in sickness.

[Meta's locomotion guidance](https://developers.meta.com/vr/design/locomotion-best-practices/) recommends predictable movement, limiting provocative acceleration, stable frame rate, and testing different users and comfort options. It does not validate our numerical thresholds. In two experiments, [Widdowson et al., Assessing Postural Instability and Cybersickness Through Linear and Angular Displacement](https://doi.org/10.1177/0018720819881254) found no significant cybersickness difference between the tested speed profiles. Those controlled short exposures do not establish a safe speed for this application. Research and platform guidance therefore motivate measurement and user choice, while our thresholds, warning delays and hallway layout remain team design decisions. Future evaluation should record discomfort before/after a short route, task errors, user preference, and measured headset frame times, allowing users to stop immediately. No comfort study has been conducted in this implementation pass.

**Future ET Pathfinder room.** A transparent, collidable “COMING SOON” boundary reserves a side branch immediately after the air shower, near world position `(26.65, 1.4, 20.25)`. It deliberately leaves the teal route to the completed cleaning exercise open. The planned extension would introduce the Pathfinder environment after preparation; its exhibits, learning objectives and exact facility representation still need client agreement. Only the boundary/signage is implemented. No complete Pathfinder room or new training claims are included.

**Offline guidance and voices.** New exact onboarding, B1, checklist and locker sentences are centralized in `TrainingGuidance`; the existing welcome and generated recordings remain intact. Additional voice IDs are appended to `Voiceover/SpokenLines.csv` and `.txt` for later custom-voice recording. New speech uses the existing local speech system; if that system is unavailable, opening/elevator guidance has a text caption fallback. The existing offline question-answering path is retained. This checkout contains macOS ARM64 and Android ARM64 Sherpa native libraries. Windows Quest Link additionally needs matching Sherpa-ONNX 1.13.0 Windows x64 native libraries/import settings; the provisioning script currently supplies Mac/Android, not Windows. A Mac editor check cannot validate Windows microphone permissions or native loading.

**Verification entry points.** With Unity closed, use `LecturerFeedbackCheck.Run` for the new controls, thresholds, indicators, mirrors, future barrier and route, `ElevatorRideCheck.Run` for departure/door safety/failure recovery, `PreparationCheck.Run` for shower cancellation/completion, `ControllerEmbodimentCheck.Run` and `QuestClothingGrabCheck.Run` for body/dressing and synthetic controller pickup, and `StandaloneAICheck.Run` for local speech/Q&A. `LecturerFeedbackCheck.BuildMac` creates an ignored `Builds/ET-Training.app`. Results and screenshots are under ignored `Logs/`. Windows and Android build modules are not installed in this Mac's Unity 6000.6.0f1 editor. These checks do not substitute for Quest Link/standalone hardware testing.


Validation checkpoint, 28 October 2026: the new feedback check and existing elevator, preparation, synthetic Quest clothing-grab, cleaning, grounded-question and local ASR/LLM/TTS checks passed. The elevator's deliberately missing destination exposed a null reference in the dressing-room presence check; that guard was fixed and the elevator regression rerun with no runtime exceptions. Rendered screenshots were inspected and oversized labels/reflection surface overlap corrected. Mirror baking measured roughly 1.7–6.6 ms for individual sampled updates in the Mac editor; this is not a representative headset frame-time benchmark, and the feature still needs profiling on Quest.

**Build blocker:** a full Mac build is not yet verified. The first attempt could not use the configured IL2CPP backend because that module is absent. A temporary Mono development build reached Burst packaging but failed because ExFAT AppleDouble metadata (`._Assembly-CSharp`) was treated as an assembly inside `Library/Bee/.../ManagedStripped`. Removing verified generated-cache sidecars and retrying with `COPYFILE_DISABLE=1` did not resolve packaging. Original build settings were restored; Burst was not disabled. An APFS-hosted build workspace/cache or a Windows build environment is the next build-validation step. No Windows/Android build or physical headset acceptance is claimed.

Before accepting on Quest Link or standalone, check: both-eye mirror clipping/depth; dressing with short/tall users and seated/standing posture; hand alignment and reaching comfort; panel readability and X/voice hide-show; physical microphone questions and exact B1 guidance; shower visibility; and sustained CPU/GPU frame times along the complete route. The future Pathfinder room and full-room mirror reflections are intentionally deferred. New custom-voice recordings for VOICE_088–VOICE_093 are also pending; local synthesis is used meanwhile.

Final local speech check also synthesized all six new guidance sentences and verified the exact B1 response, “Find the UI button and click on it to go down.” The failed packaging output is labelled `Builds/ET-Training.incomplete-20261028`, not a usable app. Build-generated performance-test resource files were archived in ignored `Logs/` rather than left in project Assets.
