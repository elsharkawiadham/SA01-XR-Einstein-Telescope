using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace GroupProject.Opening
{
    /// <summary>Body-check mirror. Reflects only the avatar, without a second camera or room reflection.</summary>
    [DefaultExecutionOrder(220)]
    public sealed class AvatarMirror : MonoBehaviour
    {
        public Material reflectionMaterial;
        public float range=3.5f,refreshRate=20;
        public int ReflectedRendererCount=>entries.Count;
        public double LastBakeMilliseconds {get;private set;}
        sealed class Entry { public Renderer source,copy;public Mesh mesh;public readonly List<Vector3> vertices=new List<Vector3>();public readonly List<Vector3> normals=new List<Vector3>();public Material[] materials; }
        readonly List<Entry> entries=new List<Entry>();
        readonly HashSet<Renderer> known=new HashSet<Renderer>();
        Transform reflectionRoot;float nextScan,nextFrame;
        void Start(){reflectionRoot=new GameObject("Reflected outfit (visual only)").transform;reflectionRoot.SetParent(transform);reflectionRoot.SetPositionAndRotation(Vector3.zero,Quaternion.identity);reflectionRoot.localScale=Vector3.one;}
        void Collect(Transform root){if(!root)return;foreach(var source in root.GetComponentsInChildren<Renderer>(true)){
            if(known.Contains(source)||source is not SkinnedMeshRenderer && source is not MeshRenderer)continue;
            if(source.transform.IsChildOf(transform))continue;
            var filter=source.GetComponent<MeshFilter>();if(source is MeshRenderer&&(!filter||!filter.sharedMesh))continue;
            var go=new GameObject(source.name+" reflection",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(reflectionRoot,false);
            var entry=new Entry{source=source,copy=go.GetComponent<MeshRenderer>(),mesh=new Mesh{name="Mirror pose"}};entry.mesh.MarkDynamic();go.GetComponent<MeshFilter>().sharedMesh=entry.mesh;
            var originals=source.sharedMaterials;entry.materials=new Material[originals.Length];for(int i=0;i<originals.Length;i++)entry.materials[i]=new Material(reflectionMaterial);
            entry.copy.sharedMaterials=entry.materials;entry.copy.shadowCastingMode=ShadowCastingMode.Off;entry.copy.receiveShadows=false;entry.copy.enabled=false;entries.Add(entry);known.Add(source);
        }}
        void LateUpdate(){var body=PlayerBodyAvatar.Active;if(!body||!body.head||!reflectionMaterial||!reflectionRoot)return;
            var delta=body.head.position-transform.position;bool near=delta.sqrMagnitude<range*range&&Vector3.Dot(delta,transform.forward)>.05f;
            if(!near){foreach(var e in entries)if(e.copy)e.copy.enabled=false;return;}
            if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+1;Collect(body.VisualRoot);var hands=AnimatedControllerHands.Active;if(hands){Collect(hands.LeftVisualRoot);Collect(hands.RightVisualRoot);}foreach(var g in FindObjectsByType<GroupProject.Preparation.WearableGarment>(FindObjectsSortMode.None))if(g.Equipped)Collect(g.transform);}
            if(Time.unscaledTime<nextFrame)return;nextFrame=Time.unscaledTime+1/Mathf.Max(1,refreshRate);
            var watch=System.Diagnostics.Stopwatch.StartNew();Vector3 normal=transform.forward,point=transform.position;
            foreach(var e in entries){bool visible=e.source&&e.source.enabled&&e.source.gameObject.activeInHierarchy&&!e.source.forceRenderingOff;e.copy.enabled=visible;if(!visible)continue;
                if(e.source is SkinnedMeshRenderer skinned)skinned.BakeMesh(e.mesh);else{var original=e.source.GetComponent<MeshFilter>().sharedMesh;e.mesh.Clear();e.mesh.vertices=original.vertices;e.mesh.normals=original.normals;e.mesh.uv=original.uv;e.mesh.subMeshCount=original.subMeshCount;for(int i=0;i<original.subMeshCount;i++)e.mesh.SetTriangles(original.GetTriangles(i),i);}
                e.mesh.GetVertices(e.vertices);e.mesh.GetNormals(e.normals);for(int i=0;i<e.vertices.Count;i++){var p=e.source.transform.TransformPoint(e.vertices[i]);e.vertices[i]=p-2*Vector3.Dot(p-point,normal)*normal;}
                for(int i=0;i<e.normals.Count;i++){var n=e.source.transform.TransformDirection(e.normals[i]);e.normals[i]=Vector3.Reflect(n,normal);}
                e.mesh.SetVertices(e.vertices);e.mesh.SetNormals(e.normals);e.mesh.RecalculateBounds();var originals=e.source.sharedMaterials;
                for(int i=0;i<e.materials.Length&&i<originals.Length;i++){var m=originals[i];if(!m)continue;e.materials[i].SetColor("_BaseColor",m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white);if(m.HasProperty("_BaseMap"))e.materials[i].SetTexture("_BaseMap",m.GetTexture("_BaseMap"));}
            }watch.Stop();LastBakeMilliseconds=watch.Elapsed.TotalMilliseconds;
        }
        void OnDisable(){foreach(var e in entries)if(e.copy)e.copy.enabled=false;}
        void OnDestroy(){foreach(var e in entries){if(e.mesh)Destroy(e.mesh);foreach(var m in e.materials)if(m)Destroy(m);}if(reflectionRoot)Destroy(reflectionRoot.gameObject);}
    }
}
