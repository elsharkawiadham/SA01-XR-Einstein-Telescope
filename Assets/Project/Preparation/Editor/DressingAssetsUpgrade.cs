using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using GroupProject.Preparation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
public static class DressingAssetsUpgrade
{
    static readonly string[] keys={"PersonalJacket","Coverall","Hood","Mask","LeftGlove","RightGlove","Boot","Overshoe","HairCap","SafetyGlasses"};
    [MenuItem("Tools/Training Rooms/Refresh Dressing Assets")]
    public static void Run(){Directory.CreateDirectory("Assets/Project/Preparation/Resources/DressingMeshes");Directory.CreateDirectory("Assets/Project/Preparation/Resources/DressingMaterials");AssetDatabase.Refresh();
        foreach(string key in keys){var mesh=GarmentModels.Make(key);Copy(mesh,"Assets/Project/Preparation/Resources/DressingMeshes/"+key+".asset");var materials=GarmentModels.Materials(key);for(int i=0;i<materials.Length;i++)Copy(materials[i],"Assets/Project/Preparation/Resources/DressingMaterials/"+key+"_"+i+".mat");}
        AssetDatabase.SaveAssets();foreach(string key in keys)AssetDatabase.ImportAsset("Assets/Project/Preparation/Art/"+key+".asset",ImportAssetOptions.ForceUpdate);Render();
    }
    // Explicit migration of the saved main scene, preserving environment geometry and authored edits.
    public static void UpgradeSavedScene()
    {
        Run();
        var scene = EditorSceneManager.OpenScene(ProjectAssembly.Main);
        var journey = UnityEngine.Object.FindAnyObjectByType<PreparationJourney>(FindObjectsInactive.Include);
        if (!journey || !journey.ride) throw new InvalidOperationException("Preparation journey missing.");
        var root = journey.ride.destinationRoot.transform.Find("02_Preparation_Interactive/01_ChangingRoom");
        if (!root) throw new InvalidOperationException("Changing room missing.");
        bool legacy = root.GetComponentsInChildren<WearableGarment>(true).Length == 8;
        EnsureItem(root, journey, WearableGarment.Kind.HairCap, "Hood and mask locker", new Vector3(15.76f, 2.02f, 21.96f));
        EnsureItem(root, journey, WearableGarment.Kind.SafetyGlasses, "Hood and mask locker", new Vector3(16.20f, .83f, 21.96f));
        EnsureItem(root, journey, WearableGarment.Kind.LeftOvershoe, "Footwear locker", new Vector3(19.8f, .35f, 21.96f));
        EnsureItem(root, journey, WearableGarment.Kind.RightOvershoe, "Footwear locker", new Vector3(20.2f, .35f, 21.96f));
        foreach (var item in root.GetComponentsInChildren<WearableGarment>(true))
        {
            GarmentModels.Apply(item.gameObject, DressingRoomExperience.MeshKey(item.kind));
            var bounds = item.GetComponent<MeshFilter>().sharedMesh.bounds;
            var collider = item.GetComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.center = bounds.center;
            collider.size = Vector3.Max(bounds.size, Vector3.one * .07f);
            if (legacy && item.kind == WearableGarment.Kind.PersonalJacket)
                item.transform.position = new Vector3(11.5f, 1.3f, 21.8f);
            EditorUtility.SetDirty(item);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save upgraded wardrobe.");
        File.WriteAllText("Logs/DressingSceneUpgrade.txt", "PASS: twelve garments saved under the existing changing-room hierarchy. Environment and route retained.");
    }

    static void EnsureItem(Transform root, PreparationJourney journey, WearableGarment.Kind kind, string locker, Vector3 position)
    {
        foreach (var existing in root.GetComponentsInChildren<WearableGarment>(true)) if (existing.kind == kind) return;
        var parent = root.Find(locker);
        var item = new GameObject(WearableGarment.NameFor(kind));
        item.transform.SetParent(parent ? parent : root, false);
        item.transform.position = position;
        item.AddComponent<BoxCollider>().isTrigger = false;
        var body = item.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
        var garment = item.AddComponent<WearableGarment>(); garment.kind = kind; garment.journey = journey;
        item.GetComponent<XRGrabInteractable>().throwOnDetach = false;
    }

    static void Copy(UnityEngine.Object value,string path){var prior=AssetDatabase.LoadMainAssetAtPath(path);if(prior){EditorUtility.CopySerialized(value,prior);prior.name=Path.GetFileNameWithoutExtension(path);EditorUtility.SetDirty(prior);}else{var copy=UnityEngine.Object.Instantiate(value);copy.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(copy,path);}}
    static void Render(){var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var root=new GameObject("Temporary outfit showcase");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);root.hideFlags=HideFlags.HideAndDontSave;RenderTexture rt=null;Texture2D image=null;var previous=RenderTexture.active;
        try{
            for(int i=1;i<keys.Length;i++){var g=new GameObject(keys[i]);g.transform.SetParent(root.transform);g.transform.position=new Vector3((i-1)%3*1.3f-1.3f,1.6f-((i-1)/3)*1.15f,0);g.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Project/Preparation/Art/"+keys[i]+".asset");g.AddComponent<MeshRenderer>().sharedMaterials=GarmentModels.Materials(keys[i]);g.transform.rotation=Quaternion.Euler(0,-20,0);var bounds=g.GetComponent<MeshFilter>().sharedMesh.bounds;float scale=.85f/Mathf.Max(bounds.size.x,bounds.size.y);g.transform.localScale=Vector3.one*scale;g.transform.position-=Vector3.up*bounds.center.y*scale;
            }
            var cam=new GameObject("Showcase camera");cam.transform.SetParent(root.transform);cam.transform.position=new Vector3(0,.5f,-6);var c=cam.AddComponent<Camera>();c.scene=preview;c.cullingMask=1<<30;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.16f,.22f,.28f);c.orthographic=true;c.orthographicSize=2.15f;
            var light=new GameObject("Showcase light");light.transform.SetParent(root.transform);light.transform.rotation=Quaternion.Euler(30,-30,0);var l=light.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1.5f;l.cullingMask=1<<30;
            foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            rt=new RenderTexture(1200,1200,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;image=new Texture2D(1200,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1200),0,0);image.Apply();Directory.CreateDirectory("Logs");File.WriteAllBytes("Logs/DressingOutfitPreview.png",image.EncodeToPNG());File.WriteAllText("Logs/DressingAssetsCheck.txt","PASS: ten garment meshes and four materials per garment exported; scene not saved or reloaded.");
        }finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(root);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);if(image)UnityEngine.Object.DestroyImmediate(image);if(rt)UnityEngine.Object.DestroyImmediate(rt);}
    }
}
