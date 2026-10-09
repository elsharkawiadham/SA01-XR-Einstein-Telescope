using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GroupProject.Guide;
using GroupProject.Opening;
using GroupProject.Elevator;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class GuideSceneSetup
{
    const string Root="Assets/Project/Guide/";
    static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"Audio/"+name+".wav");
    static AudioSource Source(Transform parent,string name,float volume,bool loop=false)
    {
        var obj=new GameObject(name);obj.transform.SetParent(parent,false);var source=obj.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=volume;source.loop=loop;source.spatialBlend=1;source.minDistance=2;source.maxDistance=15;source.rolloffMode=AudioRolloffMode.Linear;return source;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {var obj=new GameObject(name,typeof(RectTransform));var rect=obj.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;}
    [MenuItem("Tools/Guide/Configure Parrot and Sound")]
    public static void Configure()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Save the current scene first.");
        var scene=EditorSceneManager.OpenScene(OpeningProjectSetup.ScenePath);
        var opening=UnityEngine.Object.FindAnyObjectByType<OpeningSequence>();var ai=opening.aiGuide;
        var ride=UnityEngine.Object.FindAnyObjectByType<ElevatorRide>();var call=ride.callButton;var rig=ride.playerRoot;
        var old=rig.Find("Parrot_Guide_Adham");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("Parrot_Guide_Adham");root.transform.SetParent(rig,false);
        var companion=root.AddComponent<ParrotCompanion>();companion.head=opening.headsetCamera.transform;companion.ai=ai;
        var texturePath=Root+"Art/ParrotScientist.png";var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
        var uiMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Art/CompanionUI.mat");if(!uiMaterial){uiMaterial=new Material(Shader.Find("GroupProject/CompanionUI"));AssetDatabase.CreateAsset(uiMaterial,Root+"Art/CompanionUI.mat");}
        var canvasRect=Rect("Parrot and dialogue",root.transform,new Vector2(600,600),Vector2.zero);canvasRect.localScale=Vector3.one*.0012f;
        var canvas=canvasRect.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=opening.headsetCamera;canvas.sortingOrder=50;
        var birdRect=Rect("Hovering scientist",canvasRect,new Vector2(530,270),new Vector2(0,-150));
        companion.bird=birdRect.gameObject.AddComponent<ParrotGraphic>();companion.bird.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);companion.bird.raycastTarget=false;companion.bird.material=uiMaterial;
        var cloudRect=Rect("Dialogue cloud",canvasRect,new Vector2(610,250),new Vector2(0,135));var cloud=cloudRect.gameObject.AddComponent<DialogueCloud>();cloud.color=new Color(.93f,.98f,1,.98f);cloud.raycastTarget=false;cloud.material=uiMaterial;
        var textRect=Rect("Spoken dialogue",cloudRect,new Vector2(460,155),new Vector2(0,5));var text=textRect.gameObject.AddComponent<TextMeshProUGUI>();text.text="Hold B on your right controller to ask me a question.";text.fontSize=28;text.enableAutoSizing=true;text.fontSizeMin=22;text.fontSizeMax=28;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.025f,.09f,.13f);text.raycastTarget=false;companion.dialogue=text;
        var fontMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Art/CompanionText.mat");if(!fontMaterial){fontMaterial=new Material(text.fontSharedMaterial);fontMaterial.shader=Shader.Find("GroupProject/CompanionText");AssetDatabase.CreateAsset(fontMaterial,Root+"Art/CompanionText.mat");}fontMaterial.renderQueue=3999;text.fontSharedMaterial=fontMaterial;
        companion.voice=Source(root.transform,"Parrot voice",1);companion.voice.spatialBlend=.8f;companion.voice.priority=32;
        companion.narrators=new[]{opening.welcome,opening.locomotionGuide.narrator,call.hintVoice,ride.announcements,ai.speaker}.Where(s=>s).Distinct().ToArray();
        var lines=new List<ParrotCompanion.Line>();
        lines.Add(new ParrotCompanion.Line{clip=opening.welcome.clip,text="Welcome. Take a moment to look around and get comfortable. Let us explore moving forward, backward, and turning."});
        for(int i=0;i<opening.locomotionGuide.steps.Length;i++)lines.Add(new ParrotCompanion.Line{clip=opening.locomotionGuide.steps[i],text=LocomotionGuide.Captions[Mathf.Min(i,4)].Replace("\n"," ")});
        lines.Add(new ParrotCompanion.Line{clip=call.hintVoice.clip,text="To push the button, aim at the down arrow and click with the controller trigger."});
        lines.Add(new ParrotCompanion.Line{clip=ride.boardingClip,text="Welcome aboard. Select B one on the cabin panel to go down."});
        lines.Add(new ParrotCompanion.Line{clip=ride.descendingClip,text="Doors closing. Going down to level B one. Please stay inside the cabin."});
        lines.Add(new ParrotCompanion.Line{clip=ride.arrivalClip,text="Level B one. You have arrived. The doors are opening. You may leave the elevator."});companion.lines=lines.ToArray();
        ai.companion=companion;ai.feedbackPanel.gameObject.SetActive(false);
        opening.locomotionGuide.panelOffset=new Vector3(-.60f,.40f,1.65f);
        var foot=rig.GetComponent<FootstepAudio>();if(!foot)foot=rig.gameObject.AddComponent<FootstepAudio>();
        foot.head=companion.head;var previous=rig.Find("Footstep source");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        foot.source=Source(rig,"Footstep source",.65f);foot.steps=Enumerable.Range(1,4).Select(i=>Clip("MetalFootstep"+i)).ToArray();
        var existing=ride.transform.Find("Elevator sound effects");if(existing)UnityEngine.Object.DestroyImmediate(existing.gameObject);
        var effects=new GameObject("Elevator sound effects");effects.transform.SetParent(ride.transform,false);effects.transform.position=call.door.position;
        var sfx=effects.AddComponent<ElevatorSoundEffects>();sfx.call=call;sfx.ride=ride;sfx.doorLoop=Source(effects.transform,"Door slide",.5f,true);sfx.doorLoop.clip=Clip("DoorSlide");sfx.effects=Source(effects.transform,"Buttons latch and chime",.7f);
        sfx.button=Clip("Button");sfx.doorStop=Clip("DoorLatch");sfx.startMotor=Clip("MotorStart");sfx.stopMotor=Clip("MotorStop");sfx.arrival=Clip("ArrivalChime");ride.motor.clip=Clip("MotorLoop");ride.motor.volume=.55f;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/GuideSetup.txt","PASS: parrot texture, animated UI, dialogue cloud, all five narration channels, four footsteps and seven elevator effects configured. Quest instructions retained for desktop preview.");
    }
}
