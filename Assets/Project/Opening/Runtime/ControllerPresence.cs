using UnityEngine;
using Unity.XR.CoreUtils;
namespace GroupProject.Opening
{
    // Controller-relative glove silhouettes; no colliders or interaction components.
    public sealed class ControllerPresence : MonoBehaviour
    {
        Material glove;
        void Start()
        {
            glove=new Material(Shader.Find("Universal Render Pipeline/Lit"));glove.color=new Color(.72f,.79f,.82f);
            foreach(string side in new[]{"Left Controller","Right Controller"}){
                Transform controller=null;foreach(var t in GetComponentsInChildren<Transform>(true))if(t.name==side){controller=t;break;}
                if(!controller)continue;
                Transform parent=controller;
                if(DesktopPreview.Active){
                    var proxy=new GameObject(side+" visual preview");parent=proxy.transform;parent.SetParent(transform,false);parent.localPosition=new Vector3(side.StartsWith("Left")?-.23f:.23f,1.08f,.38f);parent.localRotation=Quaternion.Euler(20,0,0);
                    foreach(var source in controller.GetComponentsInChildren<MeshFilter>(true)){
                        var renderer=source.GetComponent<MeshRenderer>();if(!renderer || !source.sharedMesh)continue;
                        var copy=new GameObject(source.name);copy.transform.SetParent(parent,false);copy.transform.localPosition=controller.InverseTransformPoint(source.transform.position);copy.transform.localRotation=Quaternion.Inverse(controller.rotation)*source.transform.rotation;copy.transform.localScale=source.transform.lossyScale;
                        copy.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;copy.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
                    }
                }
                Shape(parent,"Glove palm",new Vector3(0,-.035f,-.025f),new Vector3(.075f,.08f,.045f));
                for(int i=0;i<4;i++)Shape(parent,"Curled glove finger",new Vector3(0,-.014f-i*.017f,.005f),new Vector3(.075f,.014f,.032f));
                Shape(parent,"Glove thumb",new Vector3(side.StartsWith("Left")?.035f:-.035f,.005f,-.005f),new Vector3(.024f,.05f,.025f));
            }
        }
        void Shape(Transform parent,string name,Vector3 position,Vector3 size){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=size;Destroy(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=glove;}
        void OnDestroy(){if(glove)Destroy(glove);}
    }
}
