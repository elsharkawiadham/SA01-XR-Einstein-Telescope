using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.XR.CoreUtils;
using GroupProject.Opening;
using GroupProject.Preparation;

/// <summary>Updates the existing single-scene player without rebuilding rooms or replacing the XR rig.</summary>
public static class ControllerEmbodimentSetup
{
    [MenuItem("Tools/Training Rooms/Configure Controller Hands and Body")]
    public static void Run()
    {
        AnimatedHandSetup.Run();
        var scene=EditorSceneManager.OpenScene(ProjectAssembly.Main);
        var journey=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>(FindObjectsInactive.Include);
        if(!journey||!journey.ride||!journey.ride.playerRoot||!journey.head)
            throw new InvalidOperationException("The saved preparation journey needs the existing XR player and head.");
        var player=journey.ride.playerRoot;
        var origin=player.GetComponent<XROrigin>();
        if(!origin)throw new InvalidOperationException("Saved player has no XR Origin.");
        // Quest Link and standalone both report the user's real eye height relative to their configured floor.
        origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Floor;
        EditorUtility.SetDirty(origin);PrefabUtility.RecordPrefabInstancePropertyModifications(origin);
        var left=Find(player,"Left Controller");var right=Find(player,"Right Controller");
        if(!left||!right)throw new InvalidOperationException("Cannot find the existing left and right controller transforms.");
        var hands=player.GetComponent<AnimatedControllerHands>();
        if(!hands)hands=player.gameObject.AddComponent<AnimatedControllerHands>();
        hands.head=journey.head;hands.leftController=left;hands.rightController=right;
        hands.leftHandPrefab=Resources.Load<GameObject>("PlayerHands/Left Human Hand");
        hands.rightHandPrefab=Resources.Load<GameObject>("PlayerHands/Right Human Hand");
        hands.bareSkinMaterial=Resources.Load<Material>("PlayerHands/Bare Skin");
        hands.gloveMaterial=Resources.Load<Material>("PlayerHands/Cleanroom Glove");
        var avatar=player.GetComponent<PlayerBodyAvatar>();
        if(!avatar)avatar=player.gameObject.AddComponent<PlayerBodyAvatar>();
        avatar.playerRoot=player;avatar.head=journey.head;avatar.leftController=left;avatar.rightController=right;
        int count=0;
        foreach(var garment in UnityEngine.Object.FindObjectsByType<WearableGarment>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            foreach(var collider in garment.GetComponents<Collider>()){collider.isTrigger=false;EditorUtility.SetDirty(collider);}
            count++;
        }
        EditorUtility.SetDirty(hands);EditorUtility.SetDirty(avatar);
        PrefabUtility.RecordPrefabInstancePropertyModifications(hands);
        PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/ControllerEmbodimentSetup.txt","PASS: existing player retained; animated human hands and calibrated body configured; "+count+" garment colliders made detectable by the existing controller casters. No room rebuild.");
    }
    static Transform Find(Transform root,string name)
    {
        foreach(var child in root.GetComponentsInChildren<Transform>(true))if(child.name==name)return child;
        return null;
    }
}
