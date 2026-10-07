using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using GroupProject.Opening;

public static class LocomotionSceneUpgrade
{
    const string Root="Assets/GroupProject/Scene01_Opening_Adham";
    static Material cyan,dark;
    static Sprite circle;
    [MenuItem("Tools/Adham/Add Left Turn And Locomotion Guide")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/Scene01_Opening_Adham.unity");
        var root=GameObject.Find("Scene01_Opening_Adham").transform;
        if(root.GetComponent<LocomotionGuide>())return;
        cyan=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Guidance_Cyan.mat");
        dark=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Panel_Dark.mat");
        var env=root.Find("Environment");
        var originals=env.Cast<Transform>().ToArray();
        var floor=originals.First(t=>t.name.StartsWith("Floor") && Mathf.Abs(t.localPosition.z)<.1f);
        var ceiling=originals.First(t=>t.name.StartsWith("Ceiling") && Mathf.Abs(t.localPosition.z)<.1f);
        var left=originals.First(t=>t.name.StartsWith("Wall") && t.localPosition.x<0 && Mathf.Abs(t.localPosition.z)<.1f);
        var right=originals.First(t=>t.name.StartsWith("Wall") && t.localPosition.x>0 && Mathf.Abs(t.localPosition.z)<.1f);
        foreach(var t in originals)
            if(t.localPosition.z>10 || (t.name.StartsWith("Wall") && t.localPosition.x<0 && t.localPosition.z>6)) t.gameObject.SetActive(false);
        Clone(left,env,"Corner end wall",new Vector3(0,1.6f,10.08f),90);
        for(int i=0;i<2;i++) {
            float x=-4-i*4;
            Clone(floor,env,"Left wing floor "+i,new Vector3(x,-.12f,8),-90);
            Clone(ceiling,env,"Left wing ceiling "+i,new Vector3(x,3.35f,8),-90);
            Clone(left,env,"Left wing south wall "+i,new Vector3(x,1.6f,5.92f),-90);
            Clone(right,env,"Left wing north wall "+i,new Vector3(x,1.6f,10.08f),-90);
        }
        var lighting=root.Find("Lighting");
        foreach(var t in lighting.Cast<Transform>().ToArray()) if(t.localPosition.z>10)t.gameObject.SetActive(false);
        var ceilingLamp=lighting.Cast<Transform>().First(t=>t.name=="Ceiling light");
        Clone(ceilingLamp,lighting,"Wing ceiling light 1",new Vector3(-4,3.19f,8),-90);
        Clone(ceilingLamp,lighting,"Wing ceiling light 2",new Vector3(-8,3.19f,8),-90);
        var elevator=root.Find("ElevatorEntrance");elevator.localRotation=Quaternion.Euler(0,-90,0);elevator.localPosition=new Vector3(4,0,8);
        var path=root.Find("FloorGuidance");
        foreach(var t in path.Cast<Transform>().ToArray()) if(t.localPosition.z>7.2f)t.gameObject.SetActive(false);
        for(int i=0;i<17;i++) Cube("Left-turn path "+i,path,new Vector3(-i*.55f,.025f,8),new Vector3(.38f,.018f,.065f),cyan);
        for(int i=0;i<3;i++) {
            float x=-2.5f-i*2.5f;
            var a=Cube("Left arrow upper",path,new Vector3(x,.03f,8.13f),new Vector3(.38f,.02f,.035f),cyan);a.localRotation=Quaternion.Euler(0,-45,0);
            var b=Cube("Left arrow lower",path,new Vector3(x,.03f,7.87f),new Vector3(.38f,.02f,.035f),cyan);b.localRotation=Quaternion.Euler(0,45,0);
        }
        var ui=root.Find("InstructionUI");
        ui.Find("Movement instructions - left wall").gameObject.SetActive(false);
        ui.Find("Welcome transcript").gameObject.SetActive(false);
        var prompt=ui.Find("Forward prompt");prompt.localPosition=new Vector3(0,2.25f,9.91f);
        prompt.GetComponent<TextMesh>().text="TURN LEFT\nFOLLOW THE LIGHT";
        var guide=root.gameObject.AddComponent<LocomotionGuide>();
        var seq=root.GetComponent<OpeningSequence>();seq.locomotionGuide=guide;guide.headset=seq.headsetCamera.transform;
        seq.welcome.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/Welcome_Short.wav");
        var voice=new GameObject("Locomotion narrator - Samantha").AddComponent<AudioSource>();voice.transform.SetParent(root.Find("WelcomeAudio"),false);voice.playOnAwake=false;voice.spatialBlend=0;voice.volume=.85f;
        guide.narrator=voice;
        guide.steps=new[]{"Move_Forward","Move_Backward","Move_Sideways","Move_Turn","Move_Explore"}.Select(n=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/"+n+".wav")).ToArray();
        root.GetComponentInChildren<ElevatorCallButton>(true).locomotionGuide=guide;
        BuildPanel(guide,ui);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("LOCOMOTION UPGRADE PASS");
        Capture();
    }
    static void Clone(Transform source,Transform parent,string name,Vector3 pos,float yaw) {
        var g=UnityEngine.Object.Instantiate(source.gameObject,parent);g.name=name;g.SetActive(true);g.transform.localPosition=pos;g.transform.localRotation=Quaternion.Euler(0,yaw,0)*source.localRotation;
    }
    static Transform Cube(string name,Transform parent,Vector3 pos,Vector3 size,Material mat) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;
    }
    static void BuildPanel(LocomotionGuide guide,Transform parent) {
        var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float a=Mathf.Clamp01(31-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f)));tex.SetPixel(x,y,new Color(1,1,1,a));}
        tex.Apply();File.WriteAllBytes(Root+"/Materials/ControllerCircle.png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(Root+"/Materials/ControllerCircle.png");var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Materials/ControllerCircle.png");importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        circle=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Materials/ControllerCircle.png");
        var p=new GameObject("Quest locomotion guide - left view",typeof(RectTransform),typeof(Canvas));p.transform.SetParent(parent,false);
        var canvas=p.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.sortingOrder=20;
        var rect=p.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(560,470);rect.localScale=Vector3.one*.0014f;
        guide.panel=p;
        Image("Backdrop",rect,Vector2.zero,new Vector2(560,470),new Color(.014f,.032f,.055f,.96f));
        Image("Accent",rect,new Vector2(-266,0),new Vector2(4,430),new Color(.1f,1,.9f));
        Label("Title",rect,"EXPLORE LOCOMOTION",new Vector2(0,200),new Vector2(520,40),26);
        Label("Device",rect,"QUEST TOUCH PLUS  /  STICK GUIDE",new Vector2(0,165),new Vector2(510,28),16);
        foreach(int sign in new[]{-1,1}) {
            float x=125*sign;
            var grip=Image("Controller grip",rect,new Vector2(x,-30),new Vector2(70,150),new Color(.64f,.71f,.76f),true);grip.rectTransform.localRotation=Quaternion.Euler(0,0,sign*-12);
            Image("Controller face",rect,new Vector2(x,25),new Vector2(115,130),new Color(.81f,.85f,.88f),true);
            Image("Stick base",rect,new Vector2(x,35),new Vector2(48,48),new Color(.1f,.15f,.2f),true);
            Image("Face button",rect,new Vector2(x+sign*29,1),new Vector2(17,17),new Color(.16f,.2f,.24f),true);
            Image("Face button",rect,new Vector2(x+sign*24,-22),new Vector2(17,17),new Color(.16f,.2f,.24f),true);
            Label("Hand label",rect,sign<0?"LEFT / MOVE":"RIGHT / TURN",new Vector2(x,-118),new Vector2(230,30),19);
        }
        guide.leftStick=Image("Left joystick",rect,new Vector2(-125,35),new Vector2(28,28),new Color(.1f,1,.9f),true).rectTransform;
        guide.rightStick=Image("Right joystick",rect,new Vector2(125,35),new Vector2(28,28),new Color(.1f,1,.9f),true).rectTransform;
        // Visible direction tiles are highlighted in the same order as the spoken clips.
        guide.directionIndicators=new[]{
            Tile(rect,new Vector2(-125,112),"FORWARD"),Tile(rect,new Vector2(-125,-83),"BACK"),
            Tile(rect,new Vector2(-224,35),"LEFT"),Tile(rect,new Vector2(-26,35),"RIGHT"),
            Tile(rect,new Vector2(26,35),"LEFT"),Tile(rect,new Vector2(224,35),"RIGHT")};
        guide.instruction=Label("Spoken instruction",rect,"",new Vector2(0,-181),new Vector2(520,88),21);
        p.SetActive(false);
    }
    static Image Tile(Transform parent,Vector2 pos,string label) {
        var img=Image(label,parent,pos,new Vector2(68,25),new Color(.28f,.36f,.43f));
        var t=Label(label+" label",img.transform,label,Vector2.zero,new Vector2(68,25),11);t.color=Color.black;return img;
    }
    static Image Image(string name,Transform parent,Vector2 pos,Vector2 size,Color color,bool rounded=false) {
        var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.sizeDelta=size;r.anchoredPosition=pos;var i=g.GetComponent<Image>();i.color=color;i.raycastTarget=false;if(rounded)i.sprite=circle;return i;
    }
    static Text Label(string name,Transform parent,string value,Vector2 pos,Vector2 size,int fontSize) {
        var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.sizeDelta=size;r.anchoredPosition=pos;var t=g.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=fontSize;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;return t;
    }
    public static void Polish() {
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/Scene01_Opening_Adham.unity");
        var guide=UnityEngine.Object.FindFirstObjectByType<LocomotionGuide>();
        guide.panelOffset=new Vector3(-.72f,-.03f,1.55f);guide.panel.transform.localScale=Vector3.one*.0017f;
        var path=Root+"/Materials/WorldText.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("GroupProject/WorldText"));mat.mainTexture=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf").material.mainTexture;AssetDatabase.CreateAsset(mat,path);}
        foreach(var text in GameObject.Find("Scene01_Opening_Adham").GetComponentsInChildren<TextMesh>(true)) text.GetComponent<Renderer>().sharedMaterial=mat;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Capture();
    }
    public static void Capture() {
        EditorSceneManager.OpenScene(Root+"/Scenes/Scene01_Opening_Adham.unity");
        var guide=UnityEngine.Object.FindFirstObjectByType<LocomotionGuide>();
        var c=new GameObject("Review camera").AddComponent<Camera>();c.fieldOfView=80;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.black;
        var rt=new RenderTexture(1400,900,24);c.targetTexture=rt;var tex=new Texture2D(1400,900,TextureFormat.RGB24,false);
        for(int i=0;i<3;i++) {
            c.transform.position=i==2?new Vector3(-1,1.65f,8):new Vector3(0,1.65f,-.8f);c.transform.rotation=Quaternion.Euler(0,i==2?-90:0,0);
            guide.panel.SetActive(i==1);if(i==1){guide.ShowStep(0);guide.panel.transform.position=c.transform.TransformPoint(guide.panelOffset);guide.panel.transform.rotation=c.transform.rotation;Canvas.ForceUpdateCanvases();}
            c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1400,900),0,0);tex.Apply();File.WriteAllBytes("Logs/Locomotion"+i+".png",tex.EncodeToPNG());
        }
        RenderTexture.active=null;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(c.gameObject);
    }
}
