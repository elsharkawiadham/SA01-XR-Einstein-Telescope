# Client cleanroom model assessment

Inspected on 2026-10-07: `042485_A-ETpathfinder Cleanroom.glb` supplied from the client's files. The source in Downloads has not been altered. A copy is now imported at `Assets/Project/ClientCleanroom/Source/ETpathfinder_Cleanroom.glb` using Unity glTFast 6.20.0 and Draco 5.5.0. A separate reference instance is included in `Assets/Project/Elevator/Scenes/02_LowerFloor.unity`; it is not yet a navigable cleanroom.

The GLB 2.0 contains one scene, 131 nodes, 65 meshes (2,224 mesh triangles / 3,705 vertices), seven materials, no textures and no animations. It requires `KHR_draco_mesh_compression`. Its root scale is 0.001; the scaled mesh bounds span approximately 34.9 × 23.215 × 8.4 metres. These are file-derived dimensions, not a survey or client-approved scale check. Confirm vertical axis/orientation on import.

Suggested use: the architectural basis for a separate cleanroom scene, connected to the existing elevator after the client confirms the entry and destination. The existing opening remains the interaction tutorial.

Preparation:
1. Completed: import the original GLB with Unity glTFast and Draco, preserving the source.
2. Create a separate scene and prefab wrapper. Keep the source model unchanged so client revisions can be reimported.
3. Verify a known doorway dimension and the floor/up direction. Name the imported objects and identify doors, floors and walls.
4. Add appropriate collision shapes, lighting, navigation and interaction objects. A geometric model does not implement those behaviors.
5. Check transparent materials and draw calls, then profile on Quest 3. Low polygon count alone does not establish VR performance.

Primary documentation: https://github.com/Unity-Technologies/com.unity.cloud.gltfast/blob/main/Packages/com.unity.cloud.gltfast/Documentation~/features.md
