# Parrot guide and audio

The scientist macaw is an **AI-synthesized 2D hovering variation** of the user's supplied reference. It is not a rigged 3D character. `ParrotGraphic` deforms a small UI mesh for wing flaps and audio-driven beak motion, with gentle hover movement. It remains on the left side of the tracked view. The world-space dialogue cloud shows the current spoken line.

All welcome, locomotion, elevator reminder, boarding/descent/arrival and local AI speech sources retain their existing timing but are muted at runtime and mirrored through the parrot's spatial audio source. This preserves existing cancellation and sequencing without a second audible narrator. Keyboard testing does not change Quest instructional wording or hide the controller illustration. The explicitly labelled keyboard test controls remain in the desktop-only footer.

Original procedural sound effects cover four varied metal-floor footsteps, elevator button, sliding doors, door latch, motor start/loop/stop and arrival chime. Footsteps depend on actual horizontal movement with a floor beneath the player, not a held key. Teleports and stationary head jitter are rejected. These sounds need no online service.

Start `Assets/Project/Opening/Scenes/01_Opening.unity`. New ownership root: `Shared_XR_Player/Parrot_Guide_Adham`. Runtime/art/audio/setup files are under `Assets/Project/Guide`. The guide remains attached to the existing XR rig through the additive elevator scene transition.

Setup: Tools > Guide > Configure Parrot and Sound. This rebuilds the guide presentation on the saved opening scene. Save manual changes first. Run this after regenerating the older elevator or AI setup, which replaces those referenced objects. Validation: Tools > Guide > Run Presentation Check; output is `Logs/GuidePresentationCheck.txt` and `Logs/ParrotGuide.png`.

Headset stereo presentation, perceived audio levels, comfort and standalone Quest performance require physical device validation. Scripted checks and a desktop render do not establish those results.

## Asset provenance

Source reference: `/Users/adhamelsharkawi/Downloads/FishAudio_image_gpt-image-2_parrot_scientis_2026-10-07T19_15_53_0.png`.
Final texture: `Assets/Project/Guide/Art/ParrotScientist.png`.
Generated with the built-in imagegen tool, transparent background enabled. Original reference file was preserved.

Final generation prompt:

> Use case: identity-preserve. Create a Unity game character cutout from this exact blue and gold macaw robot scientist reference. Preserve his recognizable green crown, white facial markings, black beak, cyan circular ear mechanism, robotic torso and white scientist lab coat. Single full-body character facing mostly forward, friendly face slightly turned toward viewer right, hovering in air with BOTH large blue-and-gold feather wings spread horizontally outward, symmetric broad wing silhouette. No tablet or flask; robotic hands tucked against torso, feet visible. Entire character and wing tips fully within frame with generous transparent margin. True transparent background, no laboratory, no floor, no shadow backdrop, no text or speech bubble. Clean high quality rendered detail, legible as a small VR companion. Wings should occupy outer thirds of wide image; central head/torso in narrow central third, suitable for procedural wing flapping in Unity. This is an AI-synthesized hovering variation of the supplied reference.

## Validation — 2026-10-08

`GuidePresentationCheck` passed: Quest captions and authored AI advice during keyboard preview, fallback and simulated AI speech routing, dialogue synchronization, muted original channels, footstep motion/rest behavior, elevator cue sequence, motor stopping, and companion persistence after arrival. The rendered `Logs/ParrotGuide.png` was visually inspected after fixing missing CanvasRenderer components; parrot, cloud, text and controller illustration are visible without overlap. The AI source was simulated for this routing check; this does not retest speech recognition or model inference.
