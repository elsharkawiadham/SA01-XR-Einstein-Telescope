using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Preparation
{
    public sealed class DressingRoomExperience : MonoBehaviour
    {
        public PreparationJourney journey;
        DressingChecklist list; LineRenderer ring; TMP_Text targetText; Material markerMaterial;
        public WearableGarment Held {get;private set;}
        public bool InChangingRoom => journey.ride.destinationRoot.activeInHierarchy && journey.head.position.x>10 && journey.head.position.x<22 && journey.head.position.z>15 && journey.head.position.z<23.5f;
        WearableGarment[] items;
        public void Initialize(PreparationJourney owner){journey=owner;
            var root=owner.ride.destinationRoot.transform.Find("02_Preparation_Interactive/01_ChangingRoom");if(!root)return;
            foreach(var w in root.GetComponentsInChildren<WearableGarment>(true))ApplyVisual(w);
            Add(root,WearableGarment.Kind.HairCap,new Vector3(15.76f,2.02f,21.96f));Add(root,WearableGarment.Kind.SafetyGlasses,new Vector3(16.20f,.83f,21.96f));
            Add(root,WearableGarment.Kind.LeftOvershoe,new Vector3(19.80f,.35f,21.96f));Add(root,WearableGarment.Kind.RightOvershoe,new Vector3(20.20f,.35f,21.96f));
            items=root.GetComponentsInChildren<WearableGarment>(true);
            var ui=new GameObject("Dressing checklist",typeof(RectTransform));ui.transform.SetParent(transform,false);list=ui.AddComponent<DressingChecklist>();list.Build(owner);list.Show(false);
            markerMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));markerMaterial.color=new Color(.2f,.85f,.95f);
            var target=new GameObject("Fitting target");target.transform.SetParent(transform,false);ring=target.AddComponent<LineRenderer>();ring.sharedMaterial=markerMaterial;ring.widthMultiplier=.008f;ring.positionCount=49;ring.loop=false;ring.useWorldSpace=true;
            var caption=new GameObject("Fitting target instruction");caption.transform.SetParent(transform,false);targetText=caption.AddComponent<TextMeshPro>();targetText.font=TMP_Settings.defaultFontAsset;targetText.fontSize=1.1f;targetText.alignment=TextAlignmentOptions.Center;targetText.rectTransform.sizeDelta=new Vector2(.7f,.25f);ring.enabled=false;targetText.gameObject.SetActive(false);
        }
        public void SetChecklistVisible(bool value){if(list)list.SetVisible(value);}
        public bool ChecklistVisible => list && list.Visible;
        public static string MeshKey(WearableGarment.Kind kind){switch(kind){case WearableGarment.Kind.LeftBoot:case WearableGarment.Kind.RightBoot:return "Boot";case WearableGarment.Kind.LeftOvershoe:case WearableGarment.Kind.RightOvershoe:return "Overshoe";default:return kind.ToString();}}
        static void ApplyVisual(WearableGarment w){string key=MeshKey(w.kind);var mesh=Resources.Load<Mesh>("DressingMeshes/"+key);if(!mesh)return;var mf=w.GetComponent<MeshFilter>();if(mf)mf.sharedMesh=mesh;
            var mats=new Material[4];for(int i=0;i<4;i++)mats[i]=Resources.Load<Material>("DressingMaterials/"+key+"_"+i);var obsolete=w.transform.Find("Front zipper");if(obsolete)Destroy(obsolete.gameObject);var renderer=w.GetComponent<MeshRenderer>();if(renderer)renderer.sharedMaterials=mats;
            var col=w.GetComponent<BoxCollider>();if(col){col.center=mesh.bounds.center;col.size=Vector3.Max(mesh.bounds.size,Vector3.one*.07f);}
        }
        void Add(Transform parent,WearableGarment.Kind kind,Vector3 pos){foreach(var w in parent.GetComponentsInChildren<WearableGarment>(true))if(w.kind==kind)return;
            var g=new GameObject(WearableGarment.NameFor(kind));g.SetActive(false);g.transform.SetParent(parent,false);g.transform.position=pos;g.AddComponent<MeshFilter>();g.AddComponent<MeshRenderer>();var c=g.AddComponent<BoxCollider>();c.isTrigger=true;var body=g.AddComponent<Rigidbody>();body.isKinematic=true;var item=g.AddComponent<WearableGarment>();item.kind=kind;item.journey=journey;ApplyVisual(item);g.SetActive(true);
        }
        void Update(){if(list==null)return;Held=null;if(items!=null)foreach(var w in items)if(w&&w.Held){Held=w;break;}
            bool visible=InChangingRoom;list.Show(visible);list.Refresh(Held,Held?Held.Hint:"Open a locker with the trigger. Grip one item to see where it belongs.",Held&&Held.ReadyToFit);
            if(!visible||!Held){ring.enabled=false;targetText.gameObject.SetActive(false);return;}
            ring.enabled=true;targetText.gameObject.SetActive(true);var centre=Held.TargetPosition();var yaw=Quaternion.Euler(0,journey.head.eulerAngles.y,0);
            // A floating placement ring marks footwear drop location; no arbitrary floor drop succeeds.
            bool floor=Held.Footwear;float radius=floor?.28f:.20f;
            var color=Held.ReadyToFit?new Color(.2f,.9f,.45f):new Color(.25f,.8f,1);markerMaterial.color=color;
            for(int i=0;i<49;i++){float angle=i*Mathf.PI*2/48;var offset=floor?new Vector3(Mathf.Cos(angle)*radius,-.1f,Mathf.Sin(angle)*radius):new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);ring.SetPosition(i,centre+yaw*offset);}
            targetText.text=Held.DisplayName+"\n"+(Held.ReadyToFit?"READY: release grip":floor?"Place above this foot space":"Bring item here");targetText.color=color;targetText.transform.SetPositionAndRotation(centre+Vector3.up*.25f,journey.head.rotation);
        }
        void LateUpdate(){if(list)list.transform.SetPositionAndRotation(journey.head.TransformPoint(new Vector3(-.66f,.70f,1.35f)),journey.head.rotation);}
        void OnDestroy(){if(markerMaterial)Destroy(markerMaterial);}
    }
}
