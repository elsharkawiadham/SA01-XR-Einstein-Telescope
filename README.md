# Einstein Telescope — VR Cleanroom Training

A Unity group project for introducing users to an Einstein Telescope cleanroom environment. The goal is to practise VR movement, follow a guided elevator journey, and learn basic surface-cleaning choices and technique with a talking parrot guide.

The target device is **Meta Quest 3 running standalone**. A Quest controller preview is available for development and demonstrations. This is a training prototype, not a certified cleaning procedure or a finished reproduction of the client's facility.

## Start here

### Open the project

1. Install **Unity Hub** and **Unity 6000.6.0f1**.
2. Clone this repository and add its root folder to Unity Hub. Select the folder containing `Assets`, `Packages` and `ProjectSettings`.
3. Open it and let Unity finish importing. A fresh checkout needs internet to restore packages and, if required, download the offline voice models.
4. Choose **Tools > Opening Scene > Open Scene**, then press **Play**. Use the virtual Quest controls in the **Game** view.

The single main scene is `Assets/Project/Scenes/01_Opening.unity`. It contains the opening, elevator, changing room, shower room and cleaning station, with one shared player and parrot. It is the only scene in Build Settings and the configured Play Mode start scene. Open this scene to edit or demonstrate the project.

The elevator reveals the lower-floor group after descent; it does not load another scene. `02_LowerFloor` is intentionally inactive before arrival. Original scene snapshots live in `Assets/Project/Scenes/Archive` for recovery and are excluded from builds.

### What to demonstrate

- The eyes-opening reveal begins at spawn. Explore freely for 20 seconds before the parrot arrives. Movement instructions wait for you to perform each action before continuing.
- Follow the lit corridor and floor markers, then turn left to discover the elevator.
- Approach the down-arrow button and interact. Step fully into the cabin, then select **B1**.
- The simulated descent to **-300 m** lasts 45 seconds, plus door movement and loading. The camera stays in a stationary cabin.
- On the arrival landing, follow the teal floor line through the preparation corridor into the changing room, then through the shower room, and finally into the cleaning area. There is no direct landing-to-bench shortcut. Walk back along the same route to return.
- Inspect the training plate, choose a suitable wipe, and complete four parallel, slightly overlapping passes with a fresh face for each pass. Complete the guided Class A trial, then the Class B trial.

**Current layout:** Editable changing and air-shower rooms are under `02_LowerFloor/02_Preparation_Interactive/01_ChangingRoom` and `02_AirShower`. They use the modular sci-fi environment kit from the opening. Open lockers with the controller trigger; grip your personal jacket and release it inside the personal locker, then grip and release each cleanroom item near its body area: hood and mask at the head, coverall at the torso, gloves at each hand and boots at each foot. The seven garment meshes are original simplified models based on the client clothing reference, with rigid attachment rather than simulated fabric or full-body tracking. All seven plus stored personal clothing unlock the air shower. Start it from inside; leaving cancels the cycle. Directional air particles, status lighting and a spatial fan accompany the configurable 15-second demonstration cycle. Exact gowning sequence and real cycle duration remain unverified. The client GLB remains an inactive reference.

### Jump directly to cleaning

- **Tools > Cleaning > Preview With Shared Player**: quick interaction preview without AI initialization.
- **Tools > Cleaning > Preview With Offline Q&A**: the same preview with spoken questions enabled.

These previews start at the bench. Stopping Play reopens the saved opening scene. Preview positions are not saved into that scene.

## Controls

All instructions use Meta Quest controls: left joystick to move, right joystick to turn, grip to pick up/release, trigger to interact or use the held tool, and hold right B to ask the guide. Raise the held wipe before pressing trigger to expose a fresh face.

Without a headset, Play mode automatically shows a mouse-operated **Quest controller preview**. Drag the left/right virtual joysticks to move/turn. Drag the head/hand aim pad to aim the centre reticle. Select Grip to pick up or release. Toggle Trigger on/off to simulate holding/releasing it. Toggle Raise hand to lift the held wipe. Hold B to speak and release to send. Aim at CONTINUE and activate Trigger to advance after cleaning.

This preview simulates the interaction controls, not tracked six-degree-of-freedom controllers or Quest performance. It is hidden on a running headset.

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
3. Choose **Tools > Opening Scene > Build Quest APK**. The project targets IL2CPP / ARM64; output is `Builds/OpeningQuest.apk`.
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

This README is the single project guide. Narration `.txt` files are functional inputs to the station builder, not extra documentation. License files and supplied third-party asset documentation remain with their dependencies. `Library`, `Temp`, `Logs`, `.offline-guide-cache` and local model binaries are generated/downloaded data excluded from Git; their size is not the size of the source repository. Do not delete installed models if you need offline Q&A.

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

`Voiceover/SpokenLines.csv` contains the authored narration and Q&A wording, including expanded dynamic variants, for recording a replacement voice. Keep the IDs with your exported audio. This export does not replace the current clips automatically. Controller-held gloves are simple prototype visuals; they are not a fully rigged hand-animation asset.

### Preparation rehearsal and walking pace

The elevator introduces five checklist tasks: clothing, air shower, inspection, guided Class A cleaning and independent Class B cleaning. Use trigger for locker doors and the shower start button; use grip to take/release clothing. In the desktop Quest preview, hold the virtual trigger to bring the held garment to its fitting target, then use grip to release. The checklist follows the existing cleaning exercise. Walking pace shows green (good pace), amber (slow down) and red (too fast); sustained fast movement prompts the parrot with a 20-second cooldown. Configurable thresholds of 0.65 and 1.0 m/s are prototype feedback settings, not published ETpathfinder limits. Elevator travel and large position jumps are excluded.

Dedicated clothing and an air shower are documented by [Maastricht University](https://www.maastrichtuniversity.nl/news/looking-back-etpathfinders-opening). Exact clothing requirements, order and shower timing still require client confirmation. Run `PreparationCheck.Run` for the scripted preparation checks.

Recorded voices `VOICE_001`–`VOICE_060` are imported under `Assets/Project/Guide/Audio/Generated`. The recorded-voice library maps existing narration clips and exact spoken text to these recordings; unmatched lines retain the offline voice. Source MP3s remain in `audio/`. IDs 061–087 and newer preparation dialogue have no supplied recording yet. `Tools > Guide > Import Recorded Voices` rebuilds the library from its import manifest.

Use **Tools > Training Rooms > Select Changing and Shower Rooms** or **Select Cleaning Station** to find and frame the relevant scene section. The tool enables the lower floor for editing; the elevator hides it automatically on normal Play Mode startup until arrival. The checklist uses a compact paper design, draws in front of scenery and can be hidden with left-controller X, its clickable header or a spoken checklist request. The welcome recording was replaced with the supplied `voice_019.mp3`.

### Dressing interactions

The changing-room prototype now has a dedicated clothing checklist grouped by head, body, hands and feet. Store the personal jacket, then fit the cap, hood, mask, safety glasses, coverall, two gloves, two boots and two overshoes. Grip an item to reveal its labelled target; release grip when the target is ready. Gloves use the opposite tracked hand. Release footwear above the marked foot space and wait for it to settle; arbitrary floor drops do not count. Overshoes require the matching boot. The shower unlocks only when every item is fitted. These requirements demonstrate the supplied clothing reference, not a client-approved gowning order.

In the mouse-operated Quest preview, grip to pick up, hold the virtual trigger to bring the held item to its fitting target, then grip again to release. This assists the virtual hand only; headset users physically move their controllers. Individual items remain checked, and the parrot provides item-specific feedback. An interrupted grab returns an unfinished item to its locker; a fresh grab cancels the old recovery. Footwear settles onto the actual room floor, using the sole of its mesh to avoid hovering.

Clothing uses original mesh assets with seams, pleats, material details and rigid body attachments; it is not cloth simulation or a tracked full-body avatar. All twelve dressing items are saved under `02_LowerFloor/02_Preparation_Interactive/01_ChangingRoom` so you can edit them outside Play mode. The outfit list appears in the upper left while in that room; hide/show it with left-controller X, its header, or say “hide my outfit list” / “show my clothing list”. Refresh generated meshes with **Tools > Training Rooms > Refresh Dressing Assets**. **Check Dressing Interactions (Play Mode)** runs an isolated scripted check without saving the open scene. Quest comfort, actual tracked dressing and Android performance require headset validation.

Editor validation passed for all twelve saved items, dressing gestures, interrupted-grab recovery, rapid regrabbing, independent sole-to-floor contact at different elevations, shower unlocking, and persistent checklist voice hide/show. The checklist uses consistent spacing, grouped body regions, and both text and checkmarks for feedback. These editor checks do not verify tracked Quest hands, stereo comfort or Android performance.
