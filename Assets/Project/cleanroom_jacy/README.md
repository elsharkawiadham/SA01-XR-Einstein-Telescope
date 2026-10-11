# cleanroom_jacy — start here

Open `Assets/Project/cleanroom_jacy/Scenes/cleanroom_jacy.unity` and build under the `cleanroom_jacy` Hierarchy root. This is a real scene, already connected to the elevator arrival at -300 m. No new scene or scene template is needed.

The starter contains the landing floor, walls, lights and original client cleanroom reference. Keep the landing entrance aligned at x=-9.85, z=8, with the floor surface at y=0. The prototype descent is simulated; do not move the scene to y=-300. Expand toward +X from the lobby. Replace/open the end wall as the new layout develops. The client model is a visual reference, not yet a walkable environment: add appropriate floor and wall colliders as you build.

Store your scripts in Runtime, materials/models in Art, sounds in Audio and reusable objects in Prefabs. Preserve .meta files.

## Test the complete experience

Open `Assets/Project/Opening/Scenes/01_Opening.unity`, press Play and use the elevator. After the 45-second descent, this scene loads automatically. The existing XR player, controllers and parrot stay alive; do not add another XR Origin, Main Camera or Audio Listener here. Opening cleanroom_jacy alone is for scene editing; it has no standalone player. Keyboard testing uses WASD, mouse look and E/click for buttons.

The original `02_LowerFloor.unity` remains as a reference template and is no longer the runtime destination. Elevator setup preserves the existing cleanroom_jacy scene. Use the opening scene for headset/keyboard testing and install the offline guide dependencies following the root README if spoken AI is needed.
