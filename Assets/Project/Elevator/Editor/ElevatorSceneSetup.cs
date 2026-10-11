using System;
using System.IO;
using System.Linq;
using GroupProject.Opening;
using GroupProject.Elevator;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class ElevatorSceneSetup
{
    public const string ArrivalPath = "Assets/Project/cleanroom_jacy/Scenes/cleanroom_jacy.unity";
    static Material wall, floor, cyan;
    static Material Material(string name, Color color)
    {
        string path = "Assets/Project/Elevator/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.color = color;
        return material;
    }
    static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position; obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
    }
    static TMP_Text Label(string name, Transform parent, Vector3 position, string text, float size, Quaternion rotation)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent,false); obj.transform.localPosition=position; obj.transform.localRotation=rotation;
        var label=obj.AddComponent<TextMeshPro>(); label.text=text; label.fontSize=size; label.alignment=TextAlignmentOptions.Center;
        label.color=new Color(.45f,1,1); label.rectTransform.sizeDelta=new Vector2(2.3f,.9f); return label;
    }
    static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Project/Elevator/Audio/"+name+".wav");
    [MenuItem("Tools/Elevator/Configure Descent and Lower Floor")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode before configuring scenes.");
        var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (current.isDirty) throw new Exception("Save the current scene before configuring the elevator.");
        var scene = EditorSceneManager.OpenScene(OpeningProjectSetup.ScenePath);
        wall=Material("CabinPanel",new Color(.055f,.075f,.095f)); floor=Material("LandingFloor",new Color(.20f,.27f,.30f)); cyan=Material("ButtonCyan",new Color(.05f,.7f,.85f));
        var call=UnityEngine.Object.FindFirstObjectByType<ElevatorCallButton>();
        var entrance=GameObject.Find("ElevatorEntrance").transform;
        var ride=entrance.GetComponent<ElevatorRide>(); if (!ride) ride=entrance.gameObject.AddComponent<ElevatorRide>();
        ride.destinationScene="cleanroom_jacy";
        ride.callButton=call; ride.playerHead=call.playerHead; ride.playerRoot=GameObject.Find("Shared_XR_Player").transform;
        foreach(var interactor in ride.playerRoot.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>(true))
        { interactor.allowHoveredActivate=true; EditorUtility.SetDirty(interactor); PrefabUtility.RecordPrefabInstancePropertyModifications(interactor); }
        ride.departureRoot=GameObject.Find("Scene01_Opening_Adham").transform;
        var previous=entrance.Find("Scene02_Elevator_Adham"); if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var module=new GameObject("Scene02_Elevator_Adham").transform; module.SetParent(entrance,false);
        // Entrance local +Z points into the cabin; the doorway is z=13.85.
        var safe=new GameObject("Safe boarding volume");safe.layer=2;safe.transform.SetParent(module,false);safe.transform.localPosition=new Vector3(0,1.5f,15.5f);
        ride.safeCabin=safe.AddComponent<BoxCollider>();ride.safeCabin.isTrigger=true;ride.safeCabin.size=new Vector3(2.35f,3,2.3f);
        Box("B1 panel",module,new Vector3(1.39f,1.35f,15.2f),new Vector3(.08f,.9f,.65f),wall);
        var button=Box("B1 - trigger to descend",module,new Vector3(1.31f,1.25f,15.2f),new Vector3(.10f,.24f,.30f),cyan);
        button.AddComponent<XRSimpleInteractable>();button.AddComponent<ElevatorFloorButton>().ride=ride;
        Label("B1 button label",module,new Vector3(1.245f,1.25f,15.2f),"B1",1.6f,Quaternion.Euler(0,90,0));
        button.GetComponent<ElevatorFloorButton>().instruction=Label("Controller instruction",module,new Vector3(1.245f,1.65f,15.2f),"AIM + TRIGGER",.75f,Quaternion.Euler(0,90,0));
        Box("Floor display backing",module,new Vector3(0,2.3f,16.99f),new Vector3(2.4f,1.0f,.08f),wall);
        ride.display=Label("Floor display",module,new Vector3(0,2.3f,16.93f),"G / B1\nSTEP INSIDE",1.7f,Quaternion.identity);
        var cabinLight=new GameObject("Cabin ceiling light");cabinLight.transform.SetParent(module,false);cabinLight.transform.localPosition=new Vector3(0,2.8f,15.5f);var lamp=cabinLight.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=4;lamp.intensity=1.6f;lamp.shadows=LightShadows.None;
        var voice=new GameObject("Cabin announcements");voice.transform.SetParent(module,false);voice.transform.localPosition=new Vector3(0,2.6f,15.5f);
        ride.announcements=voice.AddComponent<AudioSource>();ride.announcements.playOnAwake=false;ride.announcements.spatialBlend=1;ride.announcements.minDistance=3;
        ride.boardingClip=Clip("Boarding");ride.descendingClip=Clip("Descending");ride.arrivalClip=Clip("Arrival");
        ride.motor=voice.AddComponent<AudioSource>();ride.motor.clip=Clip("Motor");ride.motor.loop=true;ride.motor.playOnAwake=false;ride.motor.volume=.22f;ride.motor.spatialBlend=1;ride.motor.minDistance=3;
        var cabin=entrance.Find("Elevator cabin - stationary");if(cabin)cabin.name="Elevator cabin";
        EditorSceneManager.SaveScene(scene);
        // Jacy owns the arrival scene: repeated setup must preserve his authored work.
        if (File.Exists(ArrivalPath))
        {
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(OpeningProjectSetup.ScenePath,true),new EditorBuildSettingsScene(ArrivalPath,true)};
            AssetDatabase.SaveAssets();
            return;
        }
        var arrival=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("cleanroom_jacy").transform;
        Box("B1 landing floor",root,new Vector3(-4.85f,-.12f,8),new Vector3(10,.24f,5),floor);
        Box("B1 north wall",root,new Vector3(-4.85f,1.6f,10.5f),new Vector3(10,3.2f,.15f),wall);
        Box("B1 south wall",root,new Vector3(-4.85f,1.6f,5.5f),new Vector3(10,3.2f,.15f),wall);
        Box("Rear left landing closure",root,new Vector3(-9.85f,1.6f,6),new Vector3(.15f,3.2f,1),wall);
        Box("Rear right landing closure",root,new Vector3(-9.85f,1.6f,10),new Vector3(.15f,3.2f,1),wall);
        Box("B1 ceiling",root,new Vector3(-4.85f,3.2f,8),new Vector3(10,.15f,5),wall);
        Box("B1 end wall",root,new Vector3(.15f,1.6f,8),new Vector3(.15f,3.2f,5),wall);
        Label("Arrival sign",root,new Vector3(-.02f,2.15f,8),"B1\nETPATHFINDER\nLOWER FLOOR",2.2f,Quaternion.Euler(0,90,0));
        for(int i=0;i<4;i++)Box("Exit guide "+i,root,new Vector3(-8+i*2,.012f,8),new Vector3(.85f,.02f,.10f),cyan);
        for(int i=0;i<3;i++) { var obj=new GameObject("Landing light "+i);obj.transform.SetParent(root);obj.transform.position=new Vector3(-8+i*3.5f,2.7f,8);var light=obj.AddComponent<Light>();light.type=LightType.Point;light.range=6;light.intensity=2;light.shadows=LightShadows.None; }
        // Client geometry is preserved as a separate reference, ready for the next room design.
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/ClientCleanroom/Source/ETpathfinder_Cleanroom.glb");
        if(model) {
            var reference=(GameObject)PrefabUtility.InstantiatePrefab(model);reference.name="Client cleanroom reference - full scale";reference.transform.SetParent(root,false);
            reference.transform.rotation=Quaternion.Euler(-90,0,0);
            var renderers=reference.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            reference.transform.position+=new Vector3(5-bounds.min.x,-bounds.min.y,8-bounds.center.z);
        }
        EditorSceneManager.SaveScene(arrival,ArrivalPath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(OpeningProjectSetup.ScenePath,true),new EditorBuildSettingsScene(ArrivalPath,true)};
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(OpeningProjectSetup.ScenePath);
        File.WriteAllText("Logs/ElevatorSetup.txt","PASS: B1 controller button, cabin safety volume, additive landing scene, audio and two build scenes saved. Client model imported: "+(model!=null));
    }
}
