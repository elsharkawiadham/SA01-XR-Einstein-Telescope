# Elevator descent (Adham)

Start Play mode from `Assets/Project/Opening/Scenes/01_Opening.unity`.

1. Follow the opening route, turn left, and use the exterior down-arrow button.
2. Enter fully into the cabin. Aim either controller at the cyan **B1** button on the side wall and press the trigger.
3. Doors close, the floor display counts down, and the motor/announcement indicate descent.
4. The separate `Scenes/02_LowerFloor.unity` loads additively, then the doors open onto the B1 arrival lobby. Walk out with the left joystick.

Desktop preview: WASD, mouse look, arrow-key snap turns, E/click on B1. The same ride logic handles both input paths. The existing B / V voice-question control remains available; elevator and route answers now reflect ride state.

The cabin remains stationary in world coordinates for comfort: no forced camera translation, shake or rotation. Joystick movement pauses during travel; head tracking stays active. Selection requires the head and rig origin to be safely inside the cabin. Leaving that volume while doors close reopens them. Duplicate selections are ignored. A missing destination scene reopens the origin door and restores movement so the player can retry.

## Ownership and scenes

- Opening hierarchy: `Scene01_Opening_Adham/ElevatorEntrance/Scene02_Elevator_Adham` holds the new panel, boarding volume, display and audio.
- `ElevatorRide` is on `ElevatorEntrance` and takes over the existing door after boarding.
- Destination hierarchy: `Scene02_LowerFloor_Adham`.
- The original scene stays loaded to retain one XR rig, interaction manager and offline AI. Departure renderers/colliders/lights are disabled after successful loading; cabin geometry stays active.
- Both scenes are enabled in Build Settings and the custom Quest build command.
- `Tools > Elevator > Configure Descent and Lower Floor` rebuilds the authored prototype configuration. Save first; it regenerates the destination scene, so do not use after hand-editing that scene without retaining your changes.
- `Tools > Elevator > Run Descent Check` runs scripted play-mode validation and writes `Logs/ElevatorRideCheck.txt`.

## Client model

The original GLB is preserved under `Assets/Project/ClientCleanroom/Source`, imported with official Unity glTFast 6.20.0 and Draco 5.5.0. A full-scale reference is placed separately beyond the lobby. It is **not yet a navigable cleanroom**; confirm the client's entrance, scale and collision layout before extending the lobby into it. B1 is a prototype floor designation, not a client-confirmed floor number.

Prerecorded prototype announcements provide offline reliability. The descent does not need an LLM response or network. Quest performance, actual controller selection, seated/standing comfort and Android build still require device validation; editor checks are not headset acceptance.

The descent to B1 (-300 m) lasts 45 seconds, plus door movement and scene loading. The cabin display shows eased depth progress and remaining travel time; the motor loop continues until arrival. Adjust `ElevatorRide.travelSeconds` in the opening scene to tune the pacing. This is a stationary-cabin simulation, not physical movement of the XR camera.
