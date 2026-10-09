using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
public static class ElevatorPreview
{
    [MenuItem("Tools/Elevator/Render Cabin Preview")]
    public static void Capture()
    {
        var source=Object.FindFirstObjectByType<Camera>();
        var obj=new GameObject("Temporary preview camera");var camera=obj.AddComponent<Camera>();camera.CopyFrom(source); camera.enabled=false; camera.cullingMask=-1;camera.nearClipPlane=.03f;camera.farClipPlane=100;camera.fieldOfView=70;camera.aspect=4f/3f;camera.ResetProjectionMatrix();camera.ResetWorldToCameraMatrix(); camera.GetUniversalAdditionalCameraData().allowXRRendering=false;
        obj.transform.position=new Vector3(-11.9f,1.65f,7.7f);obj.transform.LookAt(new Vector3(-11.4f,1.55f,9.2f));
        var target=new RenderTexture(1200,900,24);target.Create();
        RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
        var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1200,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();
        File.WriteAllBytes("Logs/ElevatorCabin.png",image.EncodeToPNG());RenderTexture.active=previous;
        Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(obj);
    }
}
