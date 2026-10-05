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
