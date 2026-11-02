using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using GroupProject.Opening;
using GroupProject.Preparation;
using GroupProject.Guide;
using TMPro;

public static class TrainingRevisionSetup
{
    const string Pack = "Assets/ThirdParty/ModularSciFi/Prefabs/Cleen/";
    const string HallName = "05_ET_Facility_Hallway";
    public static void ConfigureAndCheck(){StandaloneAISetup.Configure();Run();TrainingRevisionCheck.Run();}
    [MenuItem("Tools/Training Rooms/Apply Comfort and Facility Route Update")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ProjectAssembly.Main);
        var journey = UnityEngine.Object.FindAnyObjectByType<PreparationJourney>(FindObjectsInactive.Include);
        var rig = journey.ride.playerRoot;
        foreach (var snap in rig.GetComponentsInChildren<SnapTurnProvider>(true)) snap.enabled = false;
        var smooth = rig.GetComponentInChildren<SmoothStickTurnProvider>(true);
        foreach (var prior in rig.GetComponentsInChildren<ContinuousTurnProvider>(true))
        {
            if (prior is SmoothStickTurnProvider) continue;
            if (!smooth)
            {
                smooth = prior.gameObject.AddComponent<SmoothStickTurnProvider>();
                smooth.mediator = prior.mediator;
                smooth.rightHandTurnInput = prior.rightHandTurnInput;
                smooth.leftHandTurnInput = prior.leftHandTurnInput;
            }
            UnityEngine.Object.DestroyImmediate(prior);
        }
        if (!smooth) throw new Exception("XR continuous turn provider missing");
        smooth.enabled = true; smooth.turnSpeed = SmoothStickTurnProvider.DegreesPerSecond; smooth.enableTurnAround = false;
        foreach (var behaviour in rig.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour && behaviour.GetType().Name == "ControllerInputActionManager" && behaviour.name.Contains("Right"))
            {
                var settings = new SerializedObject(behaviour);
                settings.FindProperty("m_SmoothTurnEnabled").boolValue = true;
                settings.FindProperty("m_SmoothMotionEnabled").boolValue = false;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
        var tutorial = UnityEngine.Object.FindAnyObjectByType<LocomotionGuide>(FindObjectsInactive.Include);
        if (tutorial && tutorial.steps != null && tutorial.steps.Length > 3) tutorial.steps[3] = null;
        var lower = journey.ride.destinationRoot.transform;
        var shower = lower.Find("02_Preparation_Interactive/02_AirShower");
        journey.showerGate.SetActive(true); journey.cleanGate.SetActive(true);
        journey.entryDoor = journey.showerGate.GetComponent<AirShowerDoor>() ?? journey.showerGate.AddComponent<AirShowerDoor>();
        journey.exitDoor = journey.cleanGate.GetComponent<AirShowerDoor>() ?? journey.cleanGate.AddComponent<AirShowerDoor>();
        var material = journey.showerGate.GetComponent<Renderer>().sharedMaterial;
        var controlMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Project/Guide/Art/Control cyan.mat");
        var parrot=UnityEngine.Object.FindAnyObjectByType<ParrotCompanion>(FindObjectsInactive.Include);
        if(parrot)parrot.offset=new Vector3(.65f,-.48f,1.65f);
        var oldControl = shower.Find("Close air shower entry"); if (oldControl) UnityEngine.Object.DestroyImmediate(oldControl.gameObject);
        var close = Box(shower,"Close air shower entry",new Vector3(22.95f,1.48f,20.08f),new Vector3(.18f,.18f,.08f),controlMat);
        var interactable = close.AddComponent<XRSimpleInteractable>(); interactable.colliders.Add(close.GetComponent<Collider>());
        var button = close.AddComponent<PreparationButton>(); button.journey = journey; button.id = 5;
        close.AddComponent<InteractionFeedback>().surfaces = new[] { close.GetComponent<Renderer>() };
        var oldLabel = shower.Find("Close door label"); if(oldLabel) UnityEngine.Object.DestroyImmediate(oldLabel.gameObject);
        journey.closeDoorLabel = Label(shower,"Close door label","CLOSE DOOR\nAim + trigger",new Vector3(22.95f,1.78f,20.03f),0,.55f,.28f);
        journey.closeDoorLabel.fontSizeMax=.26f;journey.closeDoorLabel.fontSizeMin=.16f;
        var start=shower.GetComponentsInChildren<PreparationButton>(true).First(b=>b.id==4);
        start.transform.position=new Vector3(23.55f,1.48f,20.08f);start.transform.localScale=new Vector3(.18f,.18f,.08f);
        var startLabel=lower.Find("Training feedback/Shower start cue");
        if(startLabel){startLabel.position=new Vector3(23.55f,1.78f,20.03f);var t=startLabel.GetComponent<TMP_Text>();t.text="START SHOWER\nAim + trigger";t.fontSize=.26f;t.enableAutoSizing=true;t.fontSizeMin=.16f;t.fontSizeMax=.26f;t.rectTransform.sizeDelta=new Vector2(.55f,.28f);}
        if(journey.showerDisplay){journey.showerDisplay.transform.position=new Vector3(23.25f,2.15f,20.03f);journey.showerDisplay.enableAutoSizing=true;journey.showerDisplay.fontSizeMin=.18f;journey.showerDisplay.fontSizeMax=.32f;journey.showerDisplay.rectTransform.sizeDelta=new Vector2(1.3f,.38f);}
        var feedback = lower.Find("Training feedback");
        if(feedback) foreach(Transform t in feedback.Cast<Transform>().ToArray()) if(t.name.StartsWith("Pathfinder future")) UnityEngine.Object.DestroyImmediate(t.gameObject);
        var cleaning = lower.Find("03_Cleaning_Adham"); if(cleaning) UnityEngine.Object.DestroyImmediate(cleaning.gameObject);
        var route = lower.Find("04_ConnectingRoute");
        if(route)
        {
            if(!File.Exists("Logs/PreviousRouteGeometry.txt"))File.WriteAllLines("Logs/PreviousRouteGeometry.txt",route.Cast<Transform>().Select(t=>t.name+" @ "+t.position+" active="+t.gameObject.activeSelf));
            foreach(var t in route.Cast<Transform>().ToArray())
                if(t.name.Contains("Shower to cleaning")||t.name.StartsWith("3  ")||t.position.x>24.6f)
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
        }
        var oldHall=lower.Find(HallName);if(oldHall)UnityEngine.Object.DestroyImmediate(oldHall.gameObject);
        var hall=new GameObject(HallName).transform;hall.SetParent(lower,false);
        // Same modular floor, ceiling and wall kit as the opening corridor.
        for(int i=0;i<5;i++)
        {
            float x=25.6f+i*2;
            Module("Floor/Floor4_4.prefab",hall,new Vector3(x,.17f,19.3f),new Vector3(2,.1f,2.6f),0);
            Module("Ceiling/Ceiling4_4.prefab",hall,new Vector3(x,3.15f,19.3f),new Vector3(2,.15f,2.6f),0);
            Wall(hall,new Vector3(x,1.7f,18),0);
            if(i<4)Wall(hall,new Vector3(x,1.7f,20.6f),0);
            Box(hall,"Follow facility route",new Vector3(x,.232f,19.3f),new Vector3(1.8f,.008f,.07f),controlMat,false);
        }
        for(int i=0;i<5;i++)
        {
            float z=20.3f+i*2;
            Module("Floor/Floor4_4.prefab",hall,new Vector3(34,.17f,z),new Vector3(2.6f,.1f,2),0);
            Module("Ceiling/Ceiling4_4.prefab",hall,new Vector3(34,3.15f,z),new Vector3(2.6f,.15f,2),0);
            Wall(hall,new Vector3(35.3f,1.7f,z),90);
            if(i>0)Wall(hall,new Vector3(32.7f,1.7f,z),90);
            Box(hall,"Follow facility route",new Vector3(34,.232f,z),new Vector3(.07f,.008f,1.8f),controlMat,false);
            var light=new GameObject("Hallway ceiling light",typeof(Light));light.transform.SetParent(hall,false);light.transform.position=new Vector3(i<2?27+i*4:34,2.9f,i<2?19.3f:20.3f+(i-1)*2);var l=light.GetComponent<Light>();l.type=LightType.Point;l.range=5;l.intensity=2;l.color=new Color(.72f,.87f,1);l.shadows=LightShadows.None;
        }
        // Close the outer corner where the two module runs meet.
        Box(hall,"Outer corner south",new Vector3(34.95f,1.7f,18),new Vector3(.7f,3,.16f),material);
        Box(hall,"Outer corner east",new Vector3(35.3f,1.7f,18.65f),new Vector3(.16f,3,1.3f),material);
        Box(hall,"Inner corner join",new Vector3(32.65f,1.7f,20.6f),new Vector3(.2f,3,.16f),material);
        // A solid terminal door with no interactable, animator or opening script.
        var door=Box(hall,"ET facility room (coming soon)",new Vector3(34,1.6f,29.3f),new Vector3(2.6f,2.8f,.2f),material);
        journey.facilityDoor=door.transform;
        Box(hall,"Locked door centre seam",new Vector3(34,1.6f,29.18f),new Vector3(.035f,2.65f,.02f),controlMat,false);
        Label(hall,"ET facility room sign","ET facility room (coming soon)",new Vector3(34,2.65f,29.16f),0,2.35f,.4f);
        Label(hall,"End of experience","END OF CURRENT EXPERIENCE\nRoom locked / out of bounds",new Vector3(34,1.7f,29.16f),0,2.2f,.6f);
        Label(hall,"Facility direction","ET FACILITY ROOM  >\nFollow the lit hallway",new Vector3(27,2.25f,20.49f),0,2.5f,.6f);
        // Ensure references cannot accidentally bring the removed station back into a build.
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ProjectAssembly.Main,true)};
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("TRAINING REVISION SETUP PASS: smooth turn, shower interlock, facility hallway, cleaning removed.");
        FacilityPolishSetup.Run();
    }
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool solid=true)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static TMP_Text Label(Transform parent,string name,string text,Vector3 position,float yaw,float width,float height)
    {var g=new GameObject(name,typeof(RectTransform),typeof(TextMeshPro));g.transform.SetParent(parent,false);g.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));var t=g.GetComponent<TextMeshPro>();t.font=TMP_Settings.defaultFontAsset;t.text=text;t.fontSize=.55f;t.enableAutoSizing=true;t.fontSizeMin=.2f;t.fontSizeMax=.55f;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width,height);return t;}
    static void Wall(Transform p,Vector3 position,float yaw)=>Module("Wall1/Wall2_3.prefab",p,position,new Vector3(2,3,.16f),yaw);
    static GameObject Module(string path,Transform parent,Vector3 position,Vector3 size,float yaw)
    {var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+path);if(!asset)throw new Exception("Missing corridor module: "+path);var g=(GameObject)PrefabUtility.InstantiatePrefab(asset);g.transform.SetParent(parent,false);var renderers=g.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);g.transform.localScale=new Vector3(size.x/Mathf.Max(bounds.size.x,.01f),size.y/Mathf.Max(bounds.size.y,.01f),size.z/Mathf.Max(bounds.size.z,.01f));g.transform.rotation=Quaternion.Euler(0,yaw,0);bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);g.transform.position+=position-bounds.center;return g;}
}
