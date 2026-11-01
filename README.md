# Einstein Telescope — VR Cleanroom Training

An educational Unity prototype for practising Quest-controller navigation, cleanroom preparation and basic surface-cleaning technique, supported by a talking parrot guide.

## The experience

Follow the corridor to the elevator and descend to B1. Store personal clothing, put on the cleanroom outfit, complete the air shower, then inspect and clean a training plate. A checklist tracks your progress through guided Class A and independent Class B exercises.

The intended device is **Meta Quest 3**. The project also provides an on-screen controller preview for demonstrations without a headset.

## Open and run

1. Install **Unity 6000.6.0f1** through Unity Hub.
2. Add the project folder containing `Assets`, `Packages` and `ProjectSettings` to Unity Hub.
3. Allow package restoration and asset import to finish.
4. Open `Assets/Project/Scenes/01_Opening.unity` and press **Play**.

The experience uses one connected scene. Allow 20 seconds at spawn before the guide arrives. Without a headset, operate the virtual Quest controls with the mouse.

**Fresh download:** offline spoken Q&A needs additional models and platform libraries that are not stored in Git. Complete the [offline setup](TECHNICAL.md#offline-spoken-questions) before testing speech. Windows speech requires additional Windows libraries.

## Controls

| Quest control | Action |
| --- | --- |
| Left joystick | Move |
| Right joystick | Turn |
| Trigger | Press a control or use the held tool |
| Grip | Pick up or release clothing and tools |
| Hold right B, speak, then release | Ask the guide a question |
| Left X | Show or hide the checklist |

In the desktop preview, drag the virtual sticks and aim pad, and use its Grip, Trigger and B controls. This is a demonstration aid, not a headset performance test.

## Using the guide

Wait until the parrot finishes speaking, then ask a short question such as “Where is the lamp?”, “Which wipe should I use for Class A?” or “What should I do next?”. You can also ask to show or hide the checklist. Replies are limited to the training content.

## Status and limits

This is a **testing prototype**, not a certified cleanroom procedure. The particles, dressing actions and air-shower duration are teaching approximations. Follow the client's approved procedures for real work.

Editor interaction and offline speech checks have passed. A successful release build and final Quest hardware validation remain outstanding; the latest Mac packaging attempt failed. The planned ET Pathfinder extension is marked “coming soon”.

For dependencies, licences and asset attribution, editing instructions, research rationale and detailed test/build limitations, see [TECHNICAL.md](TECHNICAL.md). Third-party licence notices remain with their assets.
