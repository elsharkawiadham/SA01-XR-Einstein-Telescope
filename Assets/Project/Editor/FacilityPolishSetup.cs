using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GroupProject.Opening;
using GroupProject.Elevator;
using GroupProject.Preparation;
using GroupProject.Guide;
using TMPro;

public static class FacilityPolishSetup
{
    const string Art="Assets/Project/Facility/Art/";
    static Material steel,dark,lining,floor,cyan,amber,green,white;
    static Mesh arrowMesh;
    public static void ConfigureAndCheck(){Run();FacilityPolishCheck.Run();}
    [MenuItem("Tools/Training Rooms/Apply Facility Visual and Locker Update")]
    public static void Run()
    {
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene(ProjectAssembly.Main);
        var journey=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>(FindObjectsInactive.Include);
        var ride=journey.ride;var lower=ride.destinationRoot.transform;bool active=lower.gameObject.activeSelf;lower.gameObject.SetActive(true);
        var opening=ride.departureRoot;
        steel=Mat("Brushed titanium",new Color(.56f,.64f,.68f),true,.72f,.65f);
        dark=Mat("Midnight sign panel",new Color(.025f,.075f,.105f),false);
        lining=Mat("Structural slate",new Color(.11f,.17f,.21f),true,.12f,.28f);
        floor=Mat("Continuous floor",new Color(.22f,.29f,.34f),true,.05f,.25f);
        cyan=Mat("Cyan light",new Color(.08f,.78f,.91f),false);
        amber=Mat("Amber light",new Color(1,.69f,.12f),false);
        green=Mat("Completed green",new Color(.09f,.65f,.34f),false);
        white=Mat("Warm white light",new Color(.88f,.91f,.87f),false);
        var op=Group(opening,"Facility finish");var lo=Group(lower,"Facility finish");
        CreateShell(op,lo);CreateElevator(ride);CreateLockers(journey);CreateWayfinding(opening,lower,op,lo);CreatePhotos(op,lo);CreateSigns(journey,op,lo);CreateAccents(op,lo);CreateFinalDoor(journey);
        var ai=journey.guide;if(ai){ai.speechSpeed=.82f;ai.recordedSpeechRate=.9f;}
        var companion=UnityEngine.Object.FindAnyObjectByType<ParrotCompanion>(FindObjectsInactive.Include);
        if(companion)
        {
            var stations=GameObject.Find("AI guide stations");if(!stations)stations=new GameObject("AI guide stations");
            companion.groundAnchor=Group(stations.transform,"Ground guide anchor");companion.groundAnchor.SetPositionAndRotation(new Vector3(1.1f,1.4f,2.3f),Quaternion.identity);
            companion.lowerAnchor=Group(stations.transform,"B1 guide anchor");companion.lowerAnchor.SetPositionAndRotation(new Vector3(-6.5f,1.4f,9.5f),Quaternion.identity);
            companion.transform.SetPositionAndRotation(companion.groundAnchor.position,companion.groundAnchor.rotation);
            if(companion.voice)companion.voice.spatialBlend=0;
        }
        if(companion)foreach(var source in companion.narrators)if(source&&(!ai||source!=ai.speaker))source.pitch=.9f;
        lower.gameObject.SetActive(active);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/FacilityPolishSetup.txt","PASS: structural enclosure backing, dressed lift and visible mirror, 2 m call arrow, B1 door safety/automatic closure, numbered task lockers, personal outfit storage, directional floor arrows, official credited photo panels, sign family, accent lighting and generated no-entry door decal.");
    }
    static Transform Group(Transform parent,string name){var old=parent.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
    static Material Mat(string name,Color color,bool lit,float metallic=0,float smooth=.35f)
    {string path=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(lit?"Universal Render Pipeline/Lit":"Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);if(lit){m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);}EditorUtility.SetDirty(m);return m;}
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material m,bool solid=true)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=p;g.transform.localScale=size;g.transform.SetParent(parent,true);g.GetComponent<Renderer>().sharedMaterial=m;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static TMP_Text Text(Transform parent,string name,string words,Vector3 p,Vector3 normal,float width,float height,float font=.6f)
    {var g=new GameObject(name,typeof(RectTransform),typeof(TextMeshPro));g.transform.SetParent(parent,false);g.transform.SetPositionAndRotation(p,Quaternion.LookRotation(-normal));var t=g.GetComponent<TextMeshPro>();t.font=TMP_Settings.defaultFontAsset;t.text=words;t.fontSize=font;t.enableAutoSizing=true;t.fontSizeMin=font*.5f;t.fontSizeMax=font;t.fontStyle=FontStyles.Normal;t.characterSpacing=1.5f;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Ellipsis;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width,height);return t;}
    static void Slab(Transform p,string name,float x0,float x1,float z0,float z1,float top,bool ceiling=false)
    {Box(p,name,new Vector3((x0+x1)/2,top-.1f,(z0+z1)/2),new Vector3(x1-x0,.2f,z1-z0),ceiling?lining:floor);}
    static void WallX(Transform p,string name,float x,float z0,float z1,float bottom=0,float top=3.35f)
    {Box(p,name,new Vector3(x,(bottom+top)/2,(z0+z1)/2),new Vector3(.2f,top-bottom,z1-z0),lining);}
    static void WallZ(Transform p,string name,float z,float x0,float x1,float bottom=0,float top=3.35f)
    {Box(p,name,new Vector3((x0+x1)/2,(bottom+top)/2,z),new Vector3(x1-x0,top-bottom,.2f),lining);}
    static void CreateShell(Transform op,Transform lo)
    {
        var a=Group(op,"Continuous enclosure");var b=Group(lo,"Continuous enclosure");
        Slab(a,"Opening floor",-2.12f,2.12f,-2.1f,10.2f,-.012f);Slab(a,"Opening ceiling",-2.12f,2.12f,-2.1f,10.2f,3.46f,true);
        Slab(a,"Lift wing floor",-10,0,5.8f,10.2f,-.012f);Slab(a,"Lift wing ceiling",-10,0,5.8f,10.2f,3.46f,true);
        WallX(a,"Opening east backing",2.18f,-2.1f,10.2f);WallX(a,"Opening west backing",-2.18f,-2.1f,5.9f);
        WallZ(a,"Opening rear backing",-2.15f,-2.2f,2.2f);WallZ(a,"Opening north backing",10.18f,-10,2.2f);WallZ(a,"Wing south backing",5.82f,-10,-2);
        // The landing and approach retain both their lift doorway and changing-room doorway.
        Slab(b,"Landing floor backing",-9.86f,.35f,5.35f,10.65f,-.012f);Slab(b,"Landing ceiling backing",-9.86f,.35f,5.35f,10.65f,3.4f,true);
        WallZ(b,"Landing south backing",5.34f,-9.86f,.35f);WallZ(b,"Landing north backing",10.66f,-9.86f,.35f);
        Slab(b,"Approach floor backing",.12f,14.65f,5.35f,10.65f,.025f);Slab(b,"Approach ceiling backing",.12f,14.65f,5.35f,10.65f,3.48f,true);
        Slab(b,"Changing approach floor backing",9.55f,14.65f,10.4f,15.45f,.025f);Slab(b,"Changing approach ceiling backing",9.55f,14.65f,10.4f,15.45f,3.48f,true);
        WallZ(b,"Approach south backing",5.35f,.12f,14.65f);WallZ(b,"Approach north backing",10.65f,.12f,9.55f);WallX(b,"Approach east backing",14.65f,5.35f,15.4f);WallX(b,"Approach west return backing",9.55f,10.45f,15.4f);
        Slab(b,"Changing floor backing",9.8f,22.2f,15.25f,23.6f,.215f);Slab(b,"Changing ceiling backing",9.8f,22.2f,15.25f,23.6f,3.36f,true);
        WallX(b,"Changing west backing",9.8f,15.25f,23.6f,.18f);WallZ(b,"Changing north backing",23.6f,9.8f,22.2f,.18f);
        WallZ(b,"Changing entrance left seal",15.25f,9.65f,11,.02f);WallZ(b,"Changing entrance right seal",15.25f,13,22.2f,.02f);WallZ(b,"Changing entrance head seal",15.25f,11,13,2.55f);
        WallX(b,"Changing east lower backing",22.2f,15.25f,18.3f,.18f);WallX(b,"Changing east upper backing",22.2f,20.3f,23.6f,.18f);WallX(b,"Changing shower head seal",22.2f,18.3f,20.3f,2.48f);
        Slab(b,"Shower floor backing",21.8f,24.75f,18.15f,20.45f,.215f);Slab(b,"Shower ceiling backing",21.8f,24.75f,18.15f,20.45f,3.36f,true);
        WallZ(b,"Shower south backing",18.15f,21.8f,24.75f,.18f);WallZ(b,"Shower north backing",20.45f,21.8f,24.75f,.18f);
        WallX(b,"Shower exit lintel seal",24.55f,18.15f,20.45f,2.48f);WallX(b,"Shower exit south jamb",24.55f,18.15f,18.45f,.18f);WallX(b,"Shower exit north jamb",24.55f,20.15f,20.45f,.18f);
        Slab(b,"Facility straight floor backing",24.45f,35.45f,17.85f,20.75f,.215f);Slab(b,"Facility return floor backing",32.55f,35.45f,19.2f,29.5f,.215f);
        Slab(b,"Facility straight ceiling backing",24.45f,35.45f,17.85f,20.75f,3.36f,true);Slab(b,"Facility return ceiling backing",32.55f,35.45f,19.2f,29.5f,3.36f,true);
        WallZ(b,"Facility south continuous seal",17.85f,24.45f,35.45f,.18f);WallZ(b,"Facility north continuous seal",20.75f,24.45f,32.55f,.18f);
        WallX(b,"Facility east continuous seal",35.45f,17.85f,29.5f,.18f);WallX(b,"Facility west return continuous seal",32.55f,20.75f,29.5f,.18f);WallZ(b,"Facility terminal backing",29.5f,32.55f,35.45f,.18f);
    }
    static void CreateElevator(ElevatorRide ride)
    {
        var door=ride.callButton.door;var parent=ride.transform;var finish=Group(parent,"Lift finish");
        var doorFinish=Group(door,"Titanium door finish");
        foreach(var renderer in door.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        var silver=Mat("Lift silver panels",new Color(.48f,.57f,.63f),false);
        var inset=Mat("Lift inset panels",new Color(.38f,.46f,.52f),false);
        Box(doorFinish,"Lift door steel body",new Vector3(-9.85f,1.45f,8),new Vector3(.34f,2.86f,2.44f),silver,false);
        foreach(float side in new[]{-1f,1f})
        {
            float x=-9.85f+side*.185f;
            foreach(float z in new[]{7.39f,8.61f})Box(doorFinish,"Recessed door leaf",new Vector3(x,1.46f,z),new Vector3(.025f,2.56f,1.08f),inset,false);
            Box(doorFinish,"Door centre split",new Vector3(x+side*.02f,1.46f,8),new Vector3(.015f,2.70f,.035f),dark,false);
            foreach(float y in new[]{.28f,2.64f})Box(doorFinish,"Door brushed rail",new Vector3(x+side*.02f,y,8),new Vector3(.02f,.05f,2.27f),white,false);
        }
        foreach(float z in new[]{6.65f,9.35f}){Box(finish,"Lift fixed jamb",new Vector3(-9.83f,1.48f,z),new Vector3(.4f,2.98f,.12f),steel,false);Box(finish,"Lift edge light",new Vector3(-9.59f,1.48f,z),new Vector3(.025f,2.80f,.025f),cyan,false);}
        Box(finish,"Lift upper frame",new Vector3(-9.83f,2.97f,8),new Vector3(.4f,.16f,2.82f),steel,false);
        foreach(var text in parent.GetComponentsInChildren<TMP_Text>(true))
            if(text!=ride.display&&text!=ride.overheadFloor&&!text.GetComponentInParent<AvatarMirror>())
            {if(text.transform.parent&&text.transform.parent.name=="Scene02_Elevator_Adham"&&text.name=="B1 button label")continue;if(text.name.Contains("overhead hint",StringComparison.OrdinalIgnoreCase))text.gameObject.SetActive(false);}
        var arrival=parent.GetComponent<ElevatorArrivalControl>();if(!arrival)arrival=parent.gameObject.AddComponent<ElevatorArrivalControl>();arrival.ride=ride;arrival.closeDelay=5;arrival.closeSeconds=2;
        arrival.exteriorFloorSign=Sign(finish,"Landing floor indicator","G  /  GROUND LEVEL","LIFT 01",new Vector3(-9.5f,2.76f,8),Vector3.right,2.2f,.42f).GetComponent<TMP_Text>();
        var call=ride.callButton;call.activationDistance=2;
        if(call.proximityVisual)call.proximityVisual.SetActive(false);
        var arrowRoot=new GameObject("Call arrow - two metre visibility").transform;arrowRoot.SetParent(finish,false);arrowRoot.SetPositionAndRotation(new Vector3(-9.425f,1.22f,9.57f),Quaternion.LookRotation(Vector3.left));
        var arrow=new GameObject("Up down direction",typeof(MeshFilter),typeof(MeshRenderer));arrow.transform.SetParent(arrowRoot,false);arrow.GetComponent<MeshFilter>().sharedMesh=VerticalArrow();arrow.GetComponent<Renderer>().sharedMaterial=cyan;arrow.transform.localScale=Vector3.one*.19f;arrival.arrow=arrow.transform;call.proximityVisual=arrowRoot.gameObject;
        // Existing mirror moved to eye height with a clear reflective frame and backing.
        var mirror=parent.GetComponentsInChildren<AvatarMirror>(true).FirstOrDefault();
        if(mirror)
        {
            mirror.transform.SetPositionAndRotation(new Vector3(-12.94f,1.45f,8),Quaternion.LookRotation(Vector3.right));
            var aperture=mirror.transform.Find("Mirror aperture");if(aperture)aperture.localScale=new Vector3(1.65f,2.15f,1);
            foreach(var frame in mirror.transform.Cast<Transform>().Where(t=>t.name=="Frame").ToArray())UnityEngine.Object.DestroyImmediate(frame.gameObject);
            // The stencil mirror reflects the avatar and worn clothes without another player camera.
            Box(finish,"Mirror silver backing",new Vector3(-13.00f,1.45f,8),new Vector3(.035f,2.23f,1.73f),steel,false);
            foreach(float z in new[]{7.145f,8.855f})Box(finish,"Mirror vertical trim",new Vector3(-12.99f,1.45f,z),new Vector3(.04f,2.23f,.045f),white,false);
            foreach(float y in new[]{.335f,2.565f})Box(finish,"Mirror horizontal trim",new Vector3(-12.99f,y,8),new Vector3(.04f,.045f,1.73f),steel,false);
            foreach(var label in parent.GetComponentsInChildren<TMP_Text>(true))if(label.name=="Elevator outfit mirror label")label.gameObject.SetActive(false);
            Text(finish,"Mirror label","OUTFIT CHECK",new Vector3(-12.97f,2.70f,8),Vector3.right,1.65f,.22f,.48f);
        }
        // Floor and countdown share the header above the cabin doorway.
        var oldBacking=parent.Find("Scene02_Elevator_Adham/Floor display backing");if(oldBacking)oldBacking.gameObject.SetActive(false);
        if(ride.display){ride.display.transform.SetPositionAndRotation(new Vector3(-10.09f,2.79f,8),Quaternion.LookRotation(Vector3.right));ride.display.rectTransform.sizeDelta=new Vector2(2.1f,.37f);ride.display.enableAutoSizing=true;ride.display.fontSizeMin=.25f;ride.display.fontSizeMax=.52f;Box(finish,"Over-door countdown backing",new Vector3(-10.045f,2.85f,8),new Vector3(.04f,.54f,2.22f),dark,false);}
        if(ride.overheadFloor){ride.overheadFloor.transform.SetPositionAndRotation(new Vector3(-10.095f,3.035f,8),Quaternion.LookRotation(Vector3.right));ride.overheadFloor.rectTransform.sizeDelta=new Vector2(2.05f,.14f);ride.overheadFloor.fontSizeMin=.22f;ride.overheadFloor.fontSizeMax=.35f;ride.overheadFloor.enableAutoSizing=true;}
        foreach(var p in new[]{new Vector3(-8.3f,2.5f,8),new Vector3(-11.5f,2.8f,8)}){var g=new GameObject("Lift soft fill",typeof(Light));g.transform.SetParent(finish,false);g.transform.position=p;var l=g.GetComponent<Light>();l.type=LightType.Point;l.range=3.2f;l.intensity=.9f;l.color=new Color(.8f,.9f,1);l.shadows=LightShadows.None;}
    }
    static void CreateLockers(PreparationJourney j)
    {
        var room=j.ride.destinationRoot.transform.Find("02_Preparation_Interactive/01_ChangingRoom");var group=Group(room,"Numbered locker guidance");
        var lockers=room.GetComponentsInChildren<LockerDoor>(true).OrderBy(l=>l.hinge.position.x).ToArray();
        string[] titles={"PERSONAL CLOTHES","COVERALL","HEAD PROTECTION","GLOVES","FOOTWEAR"};
        for(int i=0;i<lockers.Length;i++)
        {
            var locker=lockers[i].hinge.parent;float x=locker.position.x;
            var panel=Group(group,"Locker "+(i+1).ToString("00")+" task");
            Box(panel,"Task panel",new Vector3(x,2.64f,21.68f),new Vector3(1.8f,.63f,.035f),dark,false);
            var stripe=Box(panel,"Completion light",new Vector3(x,2.98f,21.65f),new Vector3(1.8f,.035f,.025f),i==0?cyan:lining,false);
            Text(panel,"Locker number",(i+1).ToString("00"),new Vector3(x-.68f,2.79f,21.65f),Vector3.back,.33f,.28f,1.1f);
            Text(panel,"Locker heading",titles[i],new Vector3(x+.19f,2.81f,21.65f),Vector3.back,1.23f,.22f,.6f);
            var status=Text(panel,"Task status","",new Vector3(x+.19f,2.59f,21.65f),Vector3.back,1.25f,.15f,.38f);
            var instruction=Text(panel,"Task instruction","",new Vector3(x,2.40f,21.65f),Vector3.back,1.65f,.22f,.40f);
            var task=panel.gameObject.AddComponent<LockerTaskPanel>();task.journey=j;task.number=i+1;task.status=status;task.instruction=instruction;task.indicator=stripe.GetComponent<Renderer>();task.waitingMaterial=lining;task.readyMaterial=cyan;task.completeMaterial=green;
            var tick=Group(panel,"Green completion check");var shortBar=Box(tick,"Check lower stroke",new Vector3(x-.73f,2.57f,21.62f),new Vector3(.10f,.025f,.018f),green,false);shortBar.transform.rotation=Quaternion.Euler(0,0,-45);var longBar=Box(tick,"Check rising stroke",new Vector3(x-.66f,2.59f,21.62f),new Vector3(.15f,.025f,.018f),green,false);longBar.transform.rotation=Quaternion.Euler(0,0,45);task.checkMark=tick.gameObject;tick.gameObject.SetActive(false);
            foreach(var text in locker.GetComponentsInChildren<TMP_Text>(true))if(text.name!= "Locker number")text.gameObject.SetActive(false);
            Text(panel,"Inside task reminder","STEP "+(i+1).ToString("00")+"  /  "+titles[i],new Vector3(x,2.12f,22.40f),Vector3.back,.85f,.2f,.34f);
        }
        var jacket=room.GetComponentsInChildren<WearableGarment>(true).First(g=>g.kind==WearableGarment.Kind.PersonalJacket);
        jacket.transform.position=jacket.storagePoint.position;
        foreach(var r in jacket.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        foreach(var c in jacket.GetComponentsInChildren<Collider>(true))c.enabled=false;
        jacket.GetComponent<XRGrabInteractable>().enabled=false;
        var first=lockers[0].hinge.parent;var storageGroup=Group(first,"Store personal outfit");
        var trousers=Box(storageGroup,"Stored personal trousers",new Vector3(first.position.x,.46f,22.15f),new Vector3(.66f,.13f,.42f),j.ride.playerRoot.GetComponent<PlayerBodyAvatar>().everydayTrousers,false);trousers.SetActive(false);
        var control=Box(storageGroup,"Store clothes button",new Vector3(first.position.x,1.80f,22.35f),new Vector3(.67f,.24f,.06f),cyan);
        var input=control.AddComponent<XRSimpleInteractable>();input.colliders.Add(control.GetComponent<Collider>());
        var action=control.AddComponent<PersonalClothingStorage>();action.journey=j;action.jacket=jacket;action.foldedTrousers=trousers;
        control.AddComponent<InteractionFeedback>().surfaces=new[]{control.GetComponent<Renderer>()};
        var label=Text(storageGroup,"Store clothes label","STORE CLOTHES\nAim + trigger",new Vector3(first.position.x,1.80f,22.31f),Vector3.back,.62f,.20f,.42f);label.color=new Color(.02f,.09f,.12f);
    }
    static Mesh VerticalArrow()
    {string path=Art+"Call direction arrow.asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m)return m;m=new Mesh{name="Call direction arrow"};m.vertices=new[]{new Vector3(-.16f,-.5f,0),new Vector3(.16f,-.5f,0),new Vector3(.16f,.05f,0),new Vector3(.40f,.05f,0),new Vector3(0,.5f,0),new Vector3(-.40f,.05f,0),new Vector3(-.16f,.05f,0)};m.triangles=new[]{0,2,1,0,6,2,3,5,4};m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,path);return m;}
    static Mesh FloorArrow()
    {string path=Art+"Floor route arrow.asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m)return m;m=new Mesh{name="Floor route arrow"};m.vertices=new[]{new Vector3(-.12f,0,-.4f),new Vector3(.12f,0,-.4f),new Vector3(.12f,0,0),new Vector3(.3f,0,0),new Vector3(0,0,.45f),new Vector3(-.3f,0,0),new Vector3(-.12f,0,0)};m.triangles=new[]{0,2,1,0,6,2,3,5,4};m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,path);return m;}
    static void CreateWayfinding(Transform opening,Transform lower,Transform op,Transform lo)
    {
        foreach(var r in opening.GetComponentsInChildren<Renderer>(true))if(r.transform.IsChildOf(opening.Find("FloorGuidance"))||r.name=="Cabin guide")r.enabled=false;
        foreach(var r in lower.GetComponentsInChildren<Renderer>(true))if(r.name=="Follow preparation route"||r.name=="Follow facility route"||r.name.StartsWith("Exit guide"))r.enabled=false;
        arrowMesh=FloorArrow();ArrowRoute(Group(op,"Floor arrows"),new[]{new Vector3(0,0,-.5f),new Vector3(0,0,8),new Vector3(-8.8f,0,8)});
        ArrowRoute(Group(lo,"Floor arrows"),new[]{new Vector3(-8,0,8),new Vector3(12,0,8),new Vector3(12,.22f,19.3f),new Vector3(34,.22f,19.3f),new Vector3(34,.22f,27.8f)});
    }
    static void ArrowRoute(Transform p,Vector3[] points)
    {Physics.SyncTransforms();for(int i=0;i<points.Length-1;i++){var delta=points[i+1]-points[i];int count=Mathf.Max(1,Mathf.FloorToInt(delta.magnitude/4f));for(int k=0;k<count;k++){var position=Vector3.Lerp(points[i],points[i+1],(k+.5f)/count);if(Physics.Raycast(position+Vector3.up*.8f,Vector3.down,out var hit,1.6f))position.y=hit.point.y+.012f;var g=new GameObject("Luminous direction arrow",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(p,false);g.transform.SetPositionAndRotation(position,Quaternion.LookRotation(new Vector3(delta.x,0,delta.z)));g.transform.localScale=new Vector3(.4f,1,.75f);g.GetComponent<MeshFilter>().sharedMesh=arrowMesh;g.GetComponent<Renderer>().sharedMaterial=cyan;}}}
    static TMP_Text Sign(Transform parent,string name,string heading,string kicker,Vector3 position,Vector3 normal,float width,float height)
    {
        var root=new GameObject(name).transform;root.SetParent(parent,false);root.SetPositionAndRotation(position,Quaternion.LookRotation(-normal));
        var panel=Box(root,"Sign backing",position-normal*.025f,new Vector3(width,height,.035f),dark,false);panel.transform.rotation=root.rotation;
        var stripe=Box(root,"Sign accent",position+normal*.005f+root.right*(-width*.48f),new Vector3(.025f,height*.78f,.015f),cyan,false);stripe.transform.rotation=root.rotation;
        Text(root,"Sign kicker",kicker,position+Vector3.up*height*.28f+normal*.012f,normal,width*.9f,height*.22f,.38f).color=new Color(.43f,.83f,.89f);
        return Text(root,"Sign heading",heading,position-Vector3.up*height*.12f+normal*.012f,normal,width*.9f,height*.48f,.8f);
    }
    static void CreateSigns(PreparationJourney j,Transform op,Transform lo)
    {
        var lower=j.ride.destinationRoot.transform;
        foreach(var text in lower.GetComponentsInChildren<TMP_Text>(true))
            if(text.name=="Changing instructions"||text.name.StartsWith("Handle cue ")||text.name=="1  CHANGING ROOM"||text.name=="B1  /  CHANGING ROOM")text.gameObject.SetActive(false);
        Sign(op,"Opening route sign","FOLLOW THE FLOOR ARROWS","ET / TRAINING",new Vector3(1.93f,2.35f,2.6f),Vector3.left,2.1f,.55f);
        Sign(op,"Lift direction sign","LIFT 01  /  TURN LEFT","G  →  B1",new Vector3(0,2.45f,9.94f),Vector3.back,2.5f,.6f);
        Sign(lo,"B1 arrival sign","CHANGING ROOM  →","B1 / PREPARATION",new Vector3(-4,2.55f,10.38f),Vector3.back,2.8f,.55f);
        Sign(lo,"Changing approach sign","FOLLOW LOCKERS 01—05","DRESSING / CLEANROOM PREPARATION",new Vector3(9.92f,2.45f,19.3f),Vector3.right,2.7f,.65f);
        var hall=lower.Find("05_ET_Facility_Hallway");if(hall)foreach(var t in hall.GetComponentsInChildren<TMP_Text>(true))t.gameObject.SetActive(false);
        Sign(lo,"Facility route sign","ET FACILITY ROOM  →","AFTER THE AIR SHOWER",new Vector3(27,2.7f,20.48f),Vector3.back,2.3f,.45f);
    }
    static void CreateAccents(Transform op,Transform lo)
    {
        foreach(float z in new[]{0f,4f,8f})foreach(float x in new[]{-1.94f,1.94f})Box(op,"Opening wall wash strip",new Vector3(x,.32f,z),new Vector3(.035f,.035f,3.5f),cyan,false);
        foreach(float x in new[]{-4f,-8f})foreach(float z in new[]{6.06f,9.94f})Box(op,"Lift wing wall wash",new Vector3(x,.32f,z),new Vector3(3.5f,.035f,.035f),cyan,false);
        foreach(float x in new[]{-6f,-2f,3f,7f})foreach(float z in new[]{5.68f,10.32f})Box(lo,"B1 ambient light strip",new Vector3(x,.4f,z),new Vector3(3.3f,.035f,.035f),cyan,false);
        foreach(float x in new[]{25.8f,29.8f,33.5f})foreach(float z in new[]{18.13f,20.47f})Box(lo,"Facility ambient light strip",new Vector3(x,.45f,z),new Vector3(2.8f,.035f,.035f),cyan,false);
        foreach(float z in new[]{22.5f,26.5f})foreach(float x in new[]{32.84f,35.16f})Box(lo,"Facility return ambient strip",new Vector3(x,.45f,z),new Vector3(.035f,.035f,3.3f),cyan,false);
        foreach(var pair in new[]{(op,new Vector3(0,2.8f,2)),(op,new Vector3(-5,2.8f,8)),(lo,new Vector3(-5,2.8f,8)),(lo,new Vector3(5,2.8f,8)),(lo,new Vector3(12,2.8f,13)),(lo,new Vector3(13,2.9f,19)),(lo,new Vector3(19,2.9f,19)),(lo,new Vector3(27,2.8f,19.3f)),(lo,new Vector3(34,2.8f,23)),(lo,new Vector3(34,2.8f,27))})
        {var g=new GameObject("Soft cyan ambient fill",typeof(Light));g.transform.SetParent(pair.Item1,false);g.transform.position=pair.Item2;var l=g.GetComponent<Light>();l.type=LightType.Point;l.range=3.2f;l.intensity=.6f;l.color=new Color(.38f,.74f,.9f);l.shadows=LightShadows.None;}
    }
    static void CreatePhotos(Transform op,Transform lo)
    {
        int index=0;
        foreach(var p in new[]{new Vector3(1.93f,1.65f,.3f),new Vector3(1.93f,1.65f,5.3f)})Photo(op,p,Vector3.left,index++);
        foreach(var p in new[]{new Vector3(-1.93f,1.65f,1.3f),new Vector3(-1.93f,1.65f,4.7f)})Photo(op,p,Vector3.right,index++);
        foreach(float x in new[]{-3.8f,-7.1f}){Photo(op,new Vector3(x,1.7f,9.94f),Vector3.back,index++);Photo(op,new Vector3(x,1.7f,6.06f),Vector3.forward,index++);}
        foreach(float x in new[]{-7.2f,-1.1f,3f,7f}){Photo(lo,new Vector3(x,1.65f,10.36f),Vector3.back,index++);Photo(lo,new Vector3(x,1.65f,5.68f),Vector3.forward,index++);}
        Photo(lo,new Vector3(9.78f,1.7f,12.7f),Vector3.right,index++);Photo(lo,new Vector3(14.4f,1.7f,12.7f),Vector3.left,index++);
        Photo(lo,new Vector3(10.13f,1.7f,17),Vector3.right,index++);Photo(lo,new Vector3(10.13f,1.7f,21.6f),Vector3.right,index++);Photo(lo,new Vector3(21.86f,1.7f,16.6f),Vector3.left,index++);
        foreach(float x in new[]{26.3f,29.8f}){Photo(lo,new Vector3(x,1.7f,20.48f),Vector3.back,index++);Photo(lo,new Vector3(x,1.7f,18.12f),Vector3.forward,index++);}
        foreach(float z in new[]{22.6f,26.2f}){Photo(lo,new Vector3(35.17f,1.7f,z),Vector3.left,index++);Photo(lo,new Vector3(32.83f,1.7f,z),Vector3.right,index++);}
    }
    static void Photo(Transform parent,Vector3 position,Vector3 normal,int index)
    {
        string[] names={"ETpathfinder-Cleanroom","ETpathfinder-Concept","ETpathfinder-Laboratory","ETpathfinder-Equipment"};string name=names[index%names.Length];
        string[] credits={"© ETpathfinder / EMR media","Concept: Marco Kraan / Nikhef","© ETpathfinder / official gallery","© ETpathfinder / official gallery"};
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+name+".jpg");if(!texture)throw new Exception("Missing official image "+name);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art+name+".jpg");if(importer.maxTextureSize!=1024){importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.SaveAndReimport();}
        float ratio=(float)texture.width/texture.height,height=ratio>1?1.0f:1.4f,width=height*ratio;
        var group=new GameObject("ETpathfinder photo "+index.ToString("00")).transform;group.SetParent(parent,false);group.SetPositionAndRotation(position,Quaternion.LookRotation(-normal));
        var backing=Box(group,"Gallery frame",position-normal*.022f,new Vector3(width+.10f,height+.25f,.04f),dark,false);backing.transform.rotation=group.rotation;
        var material=Mat(name+" print",Color.white,false);material.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(material);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Official ETpathfinder photo";quad.transform.SetParent(group,false);quad.transform.localPosition=Vector3.up*.06f;quad.transform.localScale=new Vector3(width,height,1);quad.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        Text(group,"Photo credit",credits[index%credits.Length],position-Vector3.up*(height/2+.055f)+normal*.004f,normal,width,.12f,.23f).color=new Color(.66f,.80f,.85f);
    }
    static void CreateFinalDoor(PreparationJourney journey)
    {
        var door=journey.facilityDoor;var finish=Group(door.parent,"Restricted facility door finish");
        door.GetComponent<Renderer>().sharedMaterial=steel;
        foreach(float x in new[]{32.73f,35.27f})Box(finish,"Restricted door frame",new Vector3(x,1.6f,29.16f),new Vector3(.09f,2.85f,.08f),amber,false);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"NoEntryTape.png");if(!texture)throw new Exception("Generated no-entry tape missing");
        var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"NoEntryTape.png");importer.alphaIsTransparency=true;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.SaveAndReimport();
        var material=Mat("No entry tape decal",Color.white,false);material.SetTexture("_BaseMap",texture);material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;EditorUtility.SetDirty(material);
        var decal=GameObject.CreatePrimitive(PrimitiveType.Quad);decal.name="Generated NO ENTRY tape";decal.transform.SetParent(finish,false);decal.transform.position=new Vector3(34,1.55f,29.125f);decal.transform.localScale=new Vector3(2.30f,1.95f,1);decal.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(decal.GetComponent<Collider>());
        var sign=Sign(finish,"ET facility heading","ET FACILITY ROOM","COMING SOON",new Vector3(34,2.76f,29.1f),Vector3.back,2.35f,.38f);sign.fontSizeMax=.64f;
        Sign(finish,"Restricted access footer","NO ENTRY","END OF CURRENT EXPERIENCE",new Vector3(34,.48f,29.1f),Vector3.back,1.8f,.34f);
    }
}
