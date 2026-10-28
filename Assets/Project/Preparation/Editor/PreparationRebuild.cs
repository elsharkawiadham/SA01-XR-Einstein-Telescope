using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using GroupProject.Preparation;
using GroupProject.Elevator;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
public static class PreparationRebuild
{
    const string Art="Assets/Project/Preparation/Art/";
    static Material white,blue,metal,trim,air;
    static PreparationJourney journey;
    static Transform root;
    static string pack="Assets/ThirdParty/ModularSciFi/Prefabs/Cleen/";
    public static void Run()
    {
        Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene(ProjectAssembly.Main);
        journey=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>();
        var lower=journey.ride.destinationRoot;lower.SetActive(true);
        var prep=lower.transform.Find("02_Preparation_Jacy");
        foreach(var name in new[]{"Locker Room","Shower Room"}){var old=prep.Find(name);if(old)old.gameObject.SetActive(false);}
        var connections=lower.transform.Find("04_ConnectingRoute");
        var threshold=connections.Find("Changing threshold ramp");if(threshold)threshold.gameObject.SetActive(true);
        foreach(Transform child in connections)if(child.name!="Changing threshold ramp" && (child.name.Contains("Partition")||child.name.Contains("Changing")||child.name.Contains("Shower")||child.name.StartsWith("2  ")||child.name.StartsWith("3  ")))child.gameObject.SetActive(false);
        var prior=lower.transform.Find("02_Preparation_Interactive");if(prior)UnityEngine.Object.DestroyImmediate(prior.gameObject);
        root=new GameObject("02_Preparation_Interactive").transform;root.SetParent(lower.transform,false);
        white=Mat("Cleanroom fabric",new Color(.88f,.92f,.96f),0,.25f);white.SetFloat("_Cull",0);
        blue=Mat("Personal jacket fabric",new Color(.06f,.12f,.2f),0,.2f);
        metal=Mat("Locker steel",new Color(.3f,.4f,.45f),.65f,.42f);
        trim=Mat("Stitched seams",new Color(.16f,.38f,.48f),0,.3f);
        air=Mat("Airflow",new Color(.4f,.85f,1),0,0);
        air.shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");air.SetFloat("_Surface",1);air.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);air.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);air.SetFloat("_ZWrite",0);air.renderQueue=3000;air.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        string texturePath=Art+"AirJet.png";var tex=new Texture2D(32,32);for(int yy=0;yy<32;yy++)for(int xx=0;xx<32;xx++){float d=Vector2.Distance(new Vector2(xx,yy),new Vector2(15.5f,15.5f))/15.5f;tex.SetPixel(xx,yy,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2)));}tex.Apply();File.WriteAllBytes(texturePath,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(texturePath);air.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));EditorUtility.SetDirty(air);
        var change=Group("01_ChangingRoom",root);var shower=Group("02_AirShower",root);
        for(int x=0;x<3;x++)for(int z=0;z<2;z++){
            Module("Floor/Floor4_4.prefab",change,new Vector3(12+x*4,.17f,17.4f+z*4),new Vector3(4,.1f,4),0);
            Module("Ceiling/Ceiling4_4.prefab",change,new Vector3(12+x*4,3.15f,17.4f+z*4),new Vector3(4,.15f,4),0);
            Lamp(change,new Vector3(12+x*4,2.95f,17.4f+z*4));
        }
        // Modular walls leave a 2 m entrance and a 2 m shower doorway.
        for(int x=0;x<6;x++){float px=11+x*2;Wall(change,new Vector3(px,1.7f,23.4f),0);if(x!=0&&x!=1)Wall(change,new Vector3(px,1.7f,15.4f),180);}
        Wall(change,new Vector3(10.5f,1.7f,15.4f),180,new Vector3(1,3,.16f));Wall(change,new Vector3(13.5f,1.7f,15.4f),180,new Vector3(1,3,.16f));
        for(int z=0;z<4;z++){Wall(change,new Vector3(10,1.7f,16.4f+z*2),90);if(z!=1&&z!=2)Wall(change,new Vector3(22,1.7f,16.4f+z*2),90);}
        // Door opening at 19.3 is framed with the matching wall material.
        var wallMat=AssetDatabase.LoadAssetAtPath<GameObject>(pack+"Wall1/Wall2_3.prefab").GetComponentInChildren<Renderer>().sharedMaterial;
        Box(change,"Shower doorway south pier",new Vector3(22,1.7f,17.85f),new Vector3(.16f,3,.9f),wallMat);
        Box(change,"Shower doorway north pier",new Vector3(22,1.7f,20.85f),new Vector3(.16f,3,1.1f),wallMat);
        Box(change,"Shower doorway lintel",new Vector3(22,2.92f,19.3f),new Vector3(.16f,.55f,2),wallMat);
        Module("Floor/Floor4_4.prefab",shower,new Vector3(23.25f,.17f,19.3f),new Vector3(2.5f,.1f,2),0);
        Module("Ceiling/Ceiling4_4.prefab",shower,new Vector3(23.25f,3.15f,19.3f),new Vector3(2.5f,.15f,2),0);
        Wall(shower,new Vector3(23.25f,1.7f,18.3f),0,new Vector3(2.5f,3,.16f));Wall(shower,new Vector3(23.25f,1.7f,20.3f),180,new Vector3(2.5f,3,.16f));
        Module("Floor/Floor4_4.prefab",shower,new Vector3(25.25f,.17f,19.3f),new Vector3(1.5f,.1f,2),0);
        Sign(change,"CHANGING ROOM",new Vector3(16,2.65f,22.8f),0,2.5f);
        Sign(change,"Open: aim + trigger   |   Take / release: grip",new Vector3(16,2.35f,22.8f),0,3.5f,.13f);
        var personal=Locker(change,"Personal clothing",11.5f);var storage=new GameObject("Store personal jacket here").transform;storage.SetParent(personal,false);storage.localPosition=new Vector3(0,1.1f,0);
        var clean1=Locker(change,"Coverall",14);var clean2=Locker(change,"Hood and mask",16);var clean3=Locker(change,"Gloves",18);var clean4=Locker(change,"Footwear",20);
        Garment(change,"Personal jacket",WearableGarment.Kind.PersonalJacket,"PersonalJacket",new Vector3(11.5f,1.3f,21),blue,storage);
        Garment(clean1,"Cleanroom coverall",WearableGarment.Kind.Coverall,"Coverall",new Vector3(14,1.25f,22.1f),white,null);
        Garment(clean2,"Cleanroom hood",WearableGarment.Kind.Hood,"Hood",new Vector3(16,1.65f,22.1f),white,null);
        Garment(clean2,"Face covering",WearableGarment.Kind.Mask,"Mask",new Vector3(16,1.1f,22.05f),white,null);
        Garment(clean3,"Left glove",WearableGarment.Kind.LeftGlove,"Glove",new Vector3(17.8f,1.4f,22.1f),white,null);
        Garment(clean3,"Right glove",WearableGarment.Kind.RightGlove,"Glove",new Vector3(18.2f,1.4f,22.1f),white,null);
        Garment(clean4,"Left cleanroom boot",WearableGarment.Kind.LeftBoot,"Boot",new Vector3(19.8f,.65f,22.1f),white,null);
        Garment(clean4,"Right cleanroom boot",WearableGarment.Kind.RightBoot,"Boot",new Vector3(20.2f,.65f,22.1f),white,null);
        journey.showerGate=Box(shower,"Gowning checkpoint door",new Vector3(21.95f,1.35f,19.3f),new Vector3(.08f,2.25f,1.7f),metal);
        journey.cleanGate=Box(shower,"Air shower exit",new Vector3(24.55f,1.35f,19.3f),new Vector3(.08f,2.25f,1.7f),metal);
        var button=Box(shower,"Start air shower",new Vector3(23.5f,1.25f,20.12f),new Vector3(.18f,.18f,.08f),trim);var action=button.AddComponent<PreparationButton>();action.id=4;action.journey=journey;
        journey.showerDisplay=Sign(shower,"AIR SHOWER\nAim + trigger to start",new Vector3(23.25f,1.9f,20.1f),0,.9f,.065f);
        var fx=shower.gameObject.AddComponent<AirShowerEffects>();fx.journey=journey;
        var fan=new GameObject("Spatial filtered air fan");fan.transform.SetParent(shower,false);fan.transform.position=new Vector3(23.25f,2.5f,19.3f);fx.fan=fan.AddComponent<AudioSource>();fx.fan.clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Project/Preparation/Audio/AirShowerFan.wav");fx.fan.loop=true;fx.fan.playOnAwake=false;fx.fan.volume=0;fx.fan.spatialBlend=1;fx.fan.minDistance=1;fx.fan.maxDistance=6;
        fx.cycleLight=Lamp(shower,new Vector3(23.25f,2.8f,19.3f));
        var jets=new List<ParticleSystem>();foreach(float z in new[]{18.4f,20.2f})foreach(float y in new[]{.65f,1.25f,1.9f}){
            var nozzle=GameObject.CreatePrimitive(PrimitiveType.Cylinder);nozzle.name="Directional air nozzle";nozzle.transform.SetParent(shower,false);nozzle.transform.position=new Vector3(23.25f,y,z);nozzle.transform.rotation=Quaternion.Euler(90,0,0);nozzle.transform.localScale=new Vector3(.12f,.035f,.12f);nozzle.GetComponent<Renderer>().sharedMaterial=metal;
            var jet=new GameObject("Air stream").AddComponent<ParticleSystem>();jet.transform.SetParent(shower,false);jet.transform.position=new Vector3(23.25f,y,z);jet.transform.rotation=Quaternion.LookRotation(z<19?Vector3.forward:Vector3.back);jet.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=jet.main;main.playOnAwake=false;main.loop=true;main.startLifetime=.45f;main.startSpeed=2.5f;main.startSize=.035f;main.maxParticles=80;main.startColor=new Color(.5f,.85f,1,.18f);var emission=jet.emission;emission.rateOverTime=22;var shape=jet.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.045f;jet.GetComponent<ParticleSystemRenderer>().sharedMaterial=air;jets.Add(jet);
        }fx.jets=jets.ToArray();
        lower.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/PreparationRebuild.txt","Built editable room geometry, clothing meshes, lockers, seven wearable items, personal jacket storage, shower jets and spatial fan.");
    }
    static Transform Group(string name,Transform p){var g=new GameObject(name);g.transform.SetParent(p,false);return g.transform;}
    static Material Mat(string name,Color color,float metallic,float smooth){string p=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
    static GameObject Box(Transform p,string name,Vector3 pos,Vector3 size,Material mat){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(p,false);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
    static GameObject Module(string path,Transform p,Vector3 pos,Vector3 size,float yaw){var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(pack+path));g.transform.SetParent(p,false);var renderers=g.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);g.transform.localScale=new Vector3(size.x/Mathf.Max(bounds.size.x,.01f),size.y/Mathf.Max(bounds.size.y,.01f),size.z/Mathf.Max(bounds.size.z,.01f));g.transform.rotation=Quaternion.Euler(0,yaw,0);bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);g.transform.position+=pos-bounds.center;return g;}
    static void Wall(Transform p,Vector3 pos,float yaw,Vector3? size=null){Module("Wall1/Wall2_3.prefab",p,pos,size??new Vector3(2,3,.16f),yaw);}
    static Light Lamp(Transform p,Vector3 pos){var g=new GameObject("Ceiling light — matching opening");g.transform.SetParent(p,false);g.transform.position=pos;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.72f,.87f,1);l.intensity=2.2f;l.range=5;l.shadows=LightShadows.None;return l;}
    static TMP_Text Sign(Transform p,string text,Vector3 pos,float yaw,float width,float font=.22f){var g=new GameObject(text.Split('\n')[0]);g.transform.SetParent(p,false);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);var t=g.AddComponent<TextMeshPro>();t.font=TMP_Settings.defaultFontAsset;t.text=text;t.fontSize=font*10;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width,.6f);return t;}
    static Transform Locker(Transform p,string name,float x){var g=Group(name+" locker",p);var pos=new Vector3(x,0,22.1f);g.position=pos;
        Box(g,"Back",pos+new Vector3(0,1.25f,.4f),new Vector3(1,2.1f,.07f),metal);foreach(float side in new[]{-.5f,.5f})Box(g,"Side",pos+new Vector3(side,1.25f,0),new Vector3(.06f,2.1f,.8f),metal);
        foreach(float y in new[]{.23f,2.3f})Box(g,"Shelf",pos+new Vector3(0,y,0),new Vector3(1,.05f,.8f),metal);
        var hinge=Group("Door hinge",g);hinge.localPosition=new Vector3(-.48f,.23f,-.42f);
        var leaf=Box(hinge,"Locker door — aim + trigger",Vector3.zero,new Vector3(.96f,2.05f,.055f),metal);leaf.transform.localPosition=new Vector3(.48f,1.025f,0);var d=leaf.AddComponent<LockerDoor>();d.hinge=hinge;
        var handle=Box(hinge,"Handle",Vector3.zero,new Vector3(.035f,.22f,.06f),trim);handle.transform.localPosition=new Vector3(.85f,1.05f,-.06f);UnityEngine.Object.DestroyImmediate(handle.GetComponent<Collider>());
        Sign(hinge,name,Vector3.zero,0,.8f,.1f).transform.localPosition=new Vector3(.48f,1.65f,-.04f);return g;}
    static void Garment(Transform p,string name,WearableGarment.Kind kind,string mesh,Vector3 pos,Material mat,Transform storage){var g=new GameObject(name);g.transform.SetParent(p,false);g.transform.position=pos;g.AddComponent<MeshFilter>().sharedMesh=GarmentModels.Make(mesh);g.AddComponent<MeshRenderer>().sharedMaterial=mat;var col=g.AddComponent<BoxCollider>();col.size=g.GetComponent<MeshFilter>().sharedMesh.bounds.size;col.center=g.GetComponent<MeshFilter>().sharedMesh.bounds.center;col.isTrigger=false;var rb=g.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;var wear=g.AddComponent<WearableGarment>();wear.kind=kind;wear.journey=journey;wear.storagePoint=storage;g.GetComponent<XRGrabInteractable>().throwOnDetach=false;
        if(mesh=="Coverall"||mesh=="PersonalJacket"){var zip=Box(g.transform,"Front zipper",Vector3.zero,new Vector3(.012f,.4f,.012f),trim);zip.transform.localPosition=new Vector3(0,.12f,-.118f);UnityEngine.Object.DestroyImmediate(zip.GetComponent<Collider>());}
    }
}
