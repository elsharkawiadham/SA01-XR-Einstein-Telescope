# SA01 XR group project

## Open and test the prototype

Use **Unity 6000.6.0f1**. Choose **Tools > Opening Scene > Open Scene**, or open:

`Assets/Project/Opening/Scenes/01_Opening.unity`

Press **Play**, then click inside the **Game** view. Without a headset, desktop preview activates automatically:

| Control | Action |
| --- | --- |
| W / S | Walk forward / backward |
| A / D | Step sideways |
| Mouse | Look around |
| Left / right arrow | Turn in 30-degree steps |
| E or left click | Press the elevator button under the crosshair, when nearby |
| Hold V, then release | Ask the offline guide a spoken question |
| Escape | Release the mouse |

With a running XR headset, use the original XR controllers. Keyboard preview is disabled in Quest player builds. Testing with keyboard does not validate headset tracking or performance.

## Where things belong

- `Assets/Project/Opening/Scenes` — the authored opening scene.
- `Assets/Project/Opening/Runtime` — scene behavior and desktop preview.
- `Assets/Project/Opening/Art` — materials, shaders, textures and meshes.
- `Assets/Project/Opening/Audio` — recorded fallback narration; legacy keyboard variants in `Desktop` are unused; guidance always teaches Quest controls.
- `Assets/Project/Opening/Editor` — setup menus and validation checks.
- `Assets/Project/Opening/Documentation` — scene handoff, AI setup and validation notes.
- `Assets/ThirdParty/ModularSciFi` — imported sci-fi environment assets.
- `Assets/ThirdParty/UnityTemplate` — Unity template assets and demo scenes, separate from the authored scene.
- `Assets/Samples`, `XR`, `XRI`, `Settings`, `CompositionLayers`, `TextMesh Pro` — Unity/XR dependencies and configuration. These are retained because assets or package tooling depend on them.
- `Assets/Plugins`, `StreamingAssets` — native libraries and offline models; Unity requires these special folder names.
- `Tools/setup_offline_guide.py` — provisions ignored large AI dependencies for a new checkout.

Read [AI setup](Assets/Project/Opening/Documentation/AI_SETUP.md) before using spoken questions on a fresh clone. Movement and the recorded tutorial can be tested without the AI models. The build menu **Tools > Opening Scene > Build Quest APK** includes the opening and lower-floor scenes; Android Build Support is required.

Keep one shared XR rig. The opening scene's authored environment stays under `Scene01_Opening_Adham` in the Hierarchy. Organize each new group section separately, and preserve `.meta` files whenever assets move.

## Elevator descent

The opening now leads to a controller-operated B1 ride and the separate `Assets/Project/Elevator/Scenes/02_LowerFloor.unity` arrival lobby. Start from the opening scene, call the elevator, step fully inside, then aim at B1 and press the Quest trigger (E/click in desktop preview). See `Assets/Project/Elevator/README.md` for ownership, safety, additive scene loading and validation. The client cleanroom is imported as a separate reference; its walkable room integration remains future work.

## Parrot guide and audio

`Assets/Project/Guide` contains the animated 2D parrot, synchronized dialogue cloud, spatial voice routing, footsteps and elevator effects. Its hierarchy root is `Shared_XR_Player/Parrot_Guide_Adham`. See [guide notes](Assets/Project/Guide/Documentation/README.md).

## Fresh-clone speech dependencies

The small Sherpa managed binding is included to prevent CS0246 compilation errors on a new clone. Before testing spoken questions, close Unity and run `python3 Tools/setup_offline_guide.py`. Large models and platform-native libraries are installed locally, not stored in Git.
