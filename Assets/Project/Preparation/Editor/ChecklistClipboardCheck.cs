using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using GroupProject.Preparation;
public static class ChecklistClipboardCheck
{
    [MenuItem("Tools/Training Rooms/Check Clipboard")]
    public static void Run(){GameObject board=null,cam=null;RenderTexture rt=null;Texture2D image=null;
        try{
            board=new GameObject("Temporary checklist check",typeof(RectTransform));board.hideFlags=HideFlags.HideAndDontSave;
            var ui=board.AddComponent<ChecklistClipboard>();ui.Build();ui.Refresh(new[]{true,true,false,false,false},false,15);
            ui.SetVisible(false);if(ui.Visible)throw new Exception("Hide failed");ui.SetVisible(true);if(!ui.Visible)throw new Exception("Show failed");
            board.transform.position=new Vector3(-.23f,.27f,1);
            foreach(var t in board.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            cam=new GameObject("Temporary checklist camera");cam.hideFlags=HideFlags.HideAndDontSave;var c=cam.AddComponent<Camera>();c.cullingMask=1<<31;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.12f,.18f,.21f);c.fieldOfView=40;c.nearClipPlane=.01f;
            Canvas.ForceUpdateCanvases();rt=new RenderTexture(900,900,24);c.targetTexture=rt;c.Render();var previous=RenderTexture.active;RenderTexture.active=rt;image=new Texture2D(900,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();RenderTexture.active=previous;
            Directory.CreateDirectory("Logs");File.WriteAllBytes("Logs/ChecklistPreview.png",image.EncodeToPNG());File.WriteAllText("Logs/ChecklistCheck.txt","PASS: clipboard construction, progress update, hide/show state and render. Controller and microphone hardware testing remains outstanding.");
        }finally{if(board)UnityEngine.Object.DestroyImmediate(board);if(cam)UnityEngine.Object.DestroyImmediate(cam);if(image)UnityEngine.Object.DestroyImmediate(image);if(rt)UnityEngine.Object.DestroyImmediate(rt);}
    }
}
