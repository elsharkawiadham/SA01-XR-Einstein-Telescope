using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using GroupProject.Opening;
using GroupProject.Guide;
using GroupProject.Elevator;
using GroupProject.Preparation;
public static class LecturerFeedbackSetup
{
    const string Art="Assets/Project/Guide/Art/";
    public static void Run(){
        var scene=EditorSceneManager.OpenScene(ProjectAssembly.Main);
        var journey=UnityEngine.Object.FindAnyObjectByType<PreparationJourney>(FindObjectsInactive.Include);
        if(!journey||!journey.ride)throw new Exception("Existing journey not found");
        var ride=journey.ride;journey.cautionSpeed=1.3f;journey.excessiveSpeed=2f;
        var parrot=UnityEngine.Object.FindAnyObjectByType<ParrotCompanion>(FindObjectsInactive.Include);if(parrot)parrot.offset.x=-.65f;
        var cabin=Group(ride.transform,"Training feedback");var lower=Group(ride.destinationRoot.transform,"Training feedback");
        var cyan=Material("Control cyan",new Color(.12f,.7f,.8f));var dark=Material("Mirror frame",new Color(.06f,.12f,.15f));var amber=Material("Future amber",new Color(1,.65f,.2f));
        if(ride.display){ride.display.fontSize=.7f;ride.display.enableAutoSizing=true;ride.display.fontSizeMin=.35f;ride.display.fontSizeMax=.7f;ride.display.rectTransform.sizeDelta=new Vector2(1.7f,.7f);ride.display.transform.position=new Vector3(-12.98f,2.25f,8);ride.display.transform.rotation=Quaternion.LookRotation(Vector3.left);}
        var button=UnityEngine.Object.FindAnyObjectByType<ElevatorFloorButton>(FindObjectsInactive.Include);
        Cue(button.gameObject,new[]{button.GetComponent<Renderer>()},false);PressCap(button.gameObject,cyan,Vector3.forward);
        if(button.instruction){button.instruction.text="B1\nAIM + TRIGGER";button.instruction.transform.position=new Vector3(-11.2f,1.75f,9.28f);button.instruction.transform.rotation=Quaternion.identity;button.instruction.fontSize=.6f;button.instruction.rectTransform.sizeDelta=new Vector2(.8f,.5f);}
        Cue(ride.callButton.gameObject,new[]{ride.callButton.GetComponent<Renderer>()},false);
        ride.overheadFloor=Text(cabin,"Overhead floor","G / GROUND",new Vector3(-9.98f,2.65f,8),Vector3.left,1.8f,.32f,2.8f);
        Box(cabin,"Floor indicator backing",new Vector3(-9.94f,2.65f,8),new Vector3(.04f,.46f,1.95f),dark,false);
        Mirror(cabin,"Elevator outfit mirror",new Vector3(-13.03f,1.02f,8),Vector3.right,1,1.18f,1.42f,dark);
        Mirror(lower,"Changing outfit mirror",new Vector3(21.88f,1.1f,16.6f),Vector3.left,2,1.4f,1.55f,dark);
        foreach(var door in UnityEngine.Object.FindObjectsByType<LockerDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
            var handle=door.hinge.GetComponentsInChildren<Renderer>(true).FirstOrDefault(x=>x.name.Contains("Handle"));if(!handle)throw new Exception("Locker handle missing: "+door.name);
            handle.sharedMaterial=cyan;Cue(door.gameObject,new[]{handle},false);
            Text(lower,"Handle cue "+door.name,"TRIGGER  /  OPEN",handle.transform.position+new Vector3(0,.32f,-.09f),Vector3.back,.8f,.18f,1.2f);
        }
        foreach(var garment in UnityEngine.Object.FindObjectsByType<WearableGarment>(FindObjectsInactive.Include,FindObjectsSortMode.None))Cue(garment.gameObject,garment.GetComponentsInChildren<Renderer>(true),true);
        foreach(var tool in ride.destinationRoot.GetComponentsInChildren<GroupProject.Cleaning.CleaningTool>(true))Cue(tool.gameObject,tool.GetComponents<Renderer>(),true);
        foreach(var control in ride.destinationRoot.GetComponentsInChildren<GroupProject.Cleaning.CleaningContinueButton>(true))Cue(control.gameObject,control.GetComponents<Renderer>(),true);
        foreach(var t in ride.destinationRoot.GetComponentsInChildren<TMP_Text>(true)){
            if(t.text.Contains("Open: aim")||t.text=="CHANGING ROOM")t.gameObject.SetActive(false);
        }
        Text(lower,"Changing instructions","CHANGING ROOM\nTrigger: open locker   |   Grip: take clothing\nBring each item to its highlighted fitting target",new Vector3(16,2.65f,21.55f),Vector3.back,5.4f,.7f,2.3f);
        foreach(var shower in UnityEngine.Object.FindObjectsByType<PreparationButton>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(shower.id==4){
            shower.transform.localScale=new Vector3(.28f,.28f,.08f);Cue(shower.gameObject,new[]{shower.GetComponent<Renderer>()},false);PressCap(shower.gameObject,cyan,Vector3.forward);
            Text(lower,"Shower start cue","START AIR SHOWER\nAim + trigger",new Vector3(23.5f,1.57f,20.04f),Vector3.back,1.15f,.38f,.9f);
        }
        if(journey.showerDisplay){journey.showerDisplay.transform.position=new Vector3(23.25f,2.13f,20.04f);journey.showerDisplay.fontSize=.6f;journey.showerDisplay.transform.rotation=Quaternion.identity;journey.showerDisplay.rectTransform.sizeDelta=new Vector2(1.8f,.55f);}
        Light(cabin,"Cabin control fill",new Vector3(-11.3f,2.65f,8.8f),1.5f,3.4f);
        Light(lower,"Locker handle fill",new Vector3(16,2.8f,20.7f),1.8f,6);
        Light(lower,"Shower control fill",new Vector3(23.3f,2.6f,19.3f),1.4f,3);
        var pane=Material("Future transparent",new Color(.2f,.65f,.7f,.16f));pane.SetFloat("_Surface",1);pane.SetFloat("_SrcBlend",5);pane.SetFloat("_DstBlend",10);pane.SetFloat("_ZWrite",0);pane.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");pane.renderQueue=3000;EditorUtility.SetDirty(pane);
        Box(lower,"Pathfinder future barrier",new Vector3(26.65f,1.4f,20.25f),new Vector3(1.1f,2.3f,.06f),pane,true);
        Box(lower,"Pathfinder future left post",new Vector3(26.07f,1.4f,20.25f),new Vector3(.06f,2.3f,.07f),amber,false);
        Box(lower,"Pathfinder future right post",new Vector3(27.23f,1.4f,20.25f),new Vector3(.06f,2.3f,.07f),amber,false);
        Text(lower,"Pathfinder future sign","ET PATHFINDER\nCOMING SOON\nFollow the teal route to cleaning",new Vector3(26.65f,1.65f,20.2f),Vector3.back,1.05f,.8f,1.5f);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/LecturerFeedbackSetup.txt","PASS: targeted saved-scene updates, mirrors, control cues, 1.3/2.0 m/s thresholds, future branch barrier. Existing rooms and rig retained.");
    }
    static Transform Group(Transform parent,string name){var old=parent.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform;}
    static Material Material(string name,Color c){string p=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",c);EditorUtility.SetDirty(m);return m;}
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material m,bool solid){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static TMP_Text Text(Transform parent,string name,string words,Vector3 p,Vector3 normal,float width,float height,float font){var g=new GameObject(name,typeof(RectTransform),typeof(TextMeshPro));g.transform.SetParent(parent,false);var t=g.GetComponent<TextMeshPro>();t.font=TMP_Settings.defaultFontAsset;t.text=words;t.fontSize=font*.4f;t.enableAutoSizing=true;t.fontSizeMin=font*.1f;t.fontSizeMax=font*.4f;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width,height);g.transform.SetPositionAndRotation(p,Quaternion.LookRotation(-normal));return t;}
    static InteractionFeedback Cue(GameObject g,Renderer[] r,bool subtle){var f=g.GetComponent<InteractionFeedback>();if(!f)f=g.AddComponent<InteractionFeedback>();f.surfaces=r.Where(x=>x).ToArray();f.subtle=subtle;return f;}
    static void PressCap(GameObject button,Material material,Vector3 direction){var old=button.transform.Find("Press face");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);var cap=GameObject.CreatePrimitive(PrimitiveType.Cube);cap.name="Press face";cap.transform.SetParent(button.transform,false);cap.transform.localScale=Vector3.one*1.01f;UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());cap.GetComponent<Renderer>().sharedMaterial=material;var cue=button.GetComponent<InteractionFeedback>();cue.surfaces=new[]{cap.GetComponent<Renderer>()};cue.pressVisual=cap.transform;cue.pressDirection=direction;}
    static void Light(Transform parent,string name,Vector3 p,float intensity,float range){var g=new GameObject(name,typeof(UnityEngine.Light));g.transform.SetParent(parent);g.transform.position=p;var l=g.GetComponent<UnityEngine.Light>();l.type=LightType.Point;l.color=new Color(.8f,.92f,1);l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;}
    static void Mirror(Transform parent,string name,Vector3 p,Vector3 normal,int id,float width,float height,Material frame){
        var g=new GameObject(name);g.transform.SetParent(parent);g.transform.SetPositionAndRotation(p,Quaternion.LookRotation(normal));
        var mask=AssetDatabase.LoadAssetAtPath<Material>(Art+name+" mask.mat");if(!mask){mask=new Material(Shader.Find("Training/MirrorMask"));AssetDatabase.CreateAsset(mask,Art+name+" mask.mat");}mask.SetInt("_Stencil",id);
        var reflection=AssetDatabase.LoadAssetAtPath<Material>(Art+name+" reflection.mat");if(!reflection){reflection=new Material(Shader.Find("Training/AvatarMirror"));AssetDatabase.CreateAsset(reflection,Art+name+" reflection.mat");}reflection.SetInt("_Stencil",id);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Mirror aperture";quad.transform.SetParent(g.transform,false);quad.transform.localScale=new Vector3(width,height,1);quad.GetComponent<Renderer>().sharedMaterial=mask;UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        foreach(int side in new[]{-1,1}){var bar=Box(g.transform,"Frame",p+g.transform.right*side*(width/2+.025f),new Vector3(.05f,height+.1f,.04f),frame,false);bar.transform.rotation=g.transform.rotation;}
        var mirror=g.AddComponent<AvatarMirror>();mirror.reflectionMaterial=reflection;
        Text(parent,name+" label","OUTFIT CHECK",p+Vector3.up*(height/2+.16f),normal,width,.25f,1.8f);EditorUtility.SetDirty(mask);EditorUtility.SetDirty(reflection);
    }
}
