using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GroupProject.Opening;

public static class ElevatorSceneUpgrade
{
    const string Root="Assets/GroupProject/Scene01_Opening_Adham";
    [MenuItem("Tools/Adham/Add Elevator Call Interaction")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying) throw new Exception("Exit Play Mode before upgrading.");
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/Scene01_Opening_Adham.unity");
        var parent=GameObject.Find("Scene01_Opening_Adham").transform.Find("ElevatorEntrance");
        if(parent.GetComponentInChildren<ElevatorCallButton>(true)) { Debug.Log("Elevator already upgraded; preserving scene edits.");return; }
        var old=parent.Find("Call indicator");if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var panel=parent.Find("Call panel - future interaction");panel.name="Elevator call panel";
        panel.localScale=new Vector3(.3f,.45f,.06f);
        var cyan=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Guidance_Cyan.mat");
        var dark=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Panel_Dark.mat");
        var green=new Material(cyan);green.color=new Color(.15f,1,.35f);AssetDatabase.CreateAsset(green,Root+"/Materials/Elevator_Called.mat");
        var button=GameObject.CreatePrimitive(PrimitiveType.Cube);button.name="Down button - aim and press trigger";button.transform.SetParent(parent,false);
        button.transform.localPosition=new Vector3(1.57f,1.22f,13.49f);button.transform.localScale=new Vector3(.22f,.22f,.07f);button.GetComponent<Renderer>().sharedMaterial=dark;
        var xr=button.AddComponent<XRSimpleInteractable>();xr.colliders.Add(button.GetComponent<Collider>());
        var call=button.AddComponent<ElevatorCallButton>();
        call.playerHead=UnityEngine.Object.FindFirstObjectByType<OpeningSequence>().headsetCamera.transform;
        call.welcomeVoice=UnityEngine.Object.FindFirstObjectByType<OpeningSequence>().welcome;
        call.door=parent.Find("Door1 module");if(!call.door) throw new Exception("Door module missing");
        // Mesh arrow avoids reliance on font glyph support. Facing the corridor (-Z).
        var arrow=new GameObject("Down arrow - visible nearby");arrow.transform.SetParent(parent,false);arrow.transform.localPosition=new Vector3(1.57f,1.22f,13.449f);
        var mesh=new Mesh {name="ElevatorDownArrow"};
        mesh.vertices=new[]{new Vector3(-.023f,.07f,0),new Vector3(.023f,.07f,0),new Vector3(.023f,0,0),new Vector3(.075f,0,0),new Vector3(0,-.08f,0),new Vector3(-.075f,0,0),new Vector3(-.023f,0,0)};
        mesh.triangles=new[]{0,1,2,0,2,6,6,2,4,2,3,4,5,6,4};mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Root+"/Materials/ElevatorDownArrow.asset");arrow.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=arrow.AddComponent<MeshRenderer>();renderer.sharedMaterial=cyan;
        call.proximityVisual=arrow;call.indicator=renderer;call.readyMaterial=cyan;call.calledMaterial=green;
        var voice=new GameObject("Elevator overhead hint").AddComponent<AudioSource>();voice.transform.SetParent(parent,false);voice.transform.localPosition=new Vector3(0,2.8f,12.5f);
        voice.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/ElevatorHint_Prototype.wav");voice.playOnAwake=false;voice.spatialBlend=1;voice.minDistance=3;voice.maxDistance=12;voice.pitch=.88f;call.hintVoice=voice;
        // Enclosed landing behind the door prevents walking into an empty world.
        var cabin=new GameObject("Elevator cabin - stationary").transform;cabin.SetParent(parent,false);
        Cube("Cabin floor",cabin,new Vector3(0,-.12f,15.4f),new Vector3(3,.24f,3.5f),dark);
        Cube("Cabin back",cabin,new Vector3(0,1.6f,17.1f),new Vector3(3,3.2f,.15f),dark);
        Cube("Cabin left",cabin,new Vector3(-1.5f,1.6f,15.4f),new Vector3(.15f,3.2f,3.5f),dark);
        Cube("Cabin right",cabin,new Vector3(1.5f,1.6f,15.4f),new Vector3(.15f,3.2f,3.5f),dark);
        Cube("Cabin ceiling",cabin,new Vector3(0,3.2f,15.4f),new Vector3(3,.15f,3.5f),dark);
        Cube("Cabin guide",cabin,new Vector3(0,.01f,15.4f),new Vector3(.065f,.01f,2.4f),cyan);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Temp/ElevatorUpgrade.txt","PASS: elevator interaction scene saved");
        Debug.Log("ELEVATOR UPGRADE PASS");
    }
    public static void Capture() {
        EditorSceneManager.OpenScene(Root+"/Scenes/Scene01_Opening_Adham.unity");
        var call=UnityEngine.Object.FindFirstObjectByType<ElevatorCallButton>();call.proximityVisual.SetActive(true);
        var g=new GameObject("Review camera");var c=g.AddComponent<Camera>();
        c.transform.position=new Vector3(.6f,1.6f,11.7f);c.transform.LookAt(new Vector3(.7f,1.45f,13.6f));
        c.fieldOfView=65;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=Color.black;
        var rt=new RenderTexture(1000,800,24);c.targetTexture=rt;
        var tex=new Texture2D(1000,800,TextureFormat.RGB24,false);
        for(int i=0;i<2;i++) {
            if(i==1){call.door.localPosition+=call.openOffset;call.indicator.sharedMaterial=call.calledMaterial;}
            c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1000,800),0,0);tex.Apply();
            File.WriteAllBytes("Logs/Elevator"+(i==0?"Ready":"Open")+".png",tex.EncodeToPNG());
        }
        RenderTexture.active=null;c.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(g);
        // Preview only: do not save modified scene state.
    }
    static void Cube(string name,Transform parent,Vector3 position,Vector3 size,Material material) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
    }
}
