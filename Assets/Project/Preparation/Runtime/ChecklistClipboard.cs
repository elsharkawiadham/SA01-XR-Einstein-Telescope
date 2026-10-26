using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using TMPro;
namespace GroupProject.Preparation
{
    public sealed class ChecklistClipboard : MonoBehaviour
    {
        public TMP_Text Pace { get; private set; }
        public bool Visible { get; private set; } = true;
        Canvas canvas; RectTransform paper, tab; TMP_Text title, toggle;
        readonly TMP_Text[] rows = new TMP_Text[5];
        readonly Image[] boxes = new Image[5];
        readonly GameObject[] ticks=new GameObject[5];
        Material panelMaterial, textMaterial; bool wasPressed;
        static readonly string[] names={"Cleanroom clothing", "Air shower", "Inspect the plate", "Guided Class A cleaning", "Independent Class B cleaning"};
        public void Build()
        {
            canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.overrideSorting=true;canvas.sortingOrder=30000;
            var rect=(RectTransform)transform;rect.sizeDelta=new Vector2(460,580);rect.pivot=new Vector2(0,1);
            panelMaterial=new Material(Resources.Load<Shader>("ChecklistPanel"));
            textMaterial=new Material(TMP_Settings.defaultFontAsset.material);textMaterial.shader=Resources.Load<Shader>("ChecklistText");textMaterial.renderQueue=4010;
            paper=Panel("Clipboard",rect,0,0,460,580,new Color(.55f,.38f,.20f));
            var sheet=Panel("Paper",paper,16,-20,428,540,new Color(.98f,.97f,.91f));
            Panel("Metal clip",paper,158,10,144,34,new Color(.18f,.23f,.25f));
            title=Text(sheet,"FIELD CHECKLIST",24,-38,380,36,28,FontStyles.Bold);
            Text(sheet,"CLEANROOM PREPARATION",24,-79,380,26,16,FontStyles.Normal);
            for(int i=0;i<5;i++){
                float y=-130-i*62;
                boxes[i]=Panel("Checkbox",sheet,24,y,27,27,new Color(.18f,.25f,.27f)).GetComponent<Image>();
                var tick=Panel("Tick",boxes[i].transform,5,-12,10,4,Color.white);tick.localRotation=Quaternion.Euler(0,0,-45);ticks[i]=tick.gameObject;
                var up=Panel("Tick stroke",tick,7,0,20,4,Color.white);up.localRotation=Quaternion.Euler(0,0,90);
                rows[i]=Text(sheet,names[i],66,y+2,340,50,22,FontStyles.Normal);
                Panel("Rule",sheet,66,y-46,332,1,new Color(.72f,.73f,.69f));
            }
            Pace=Text(sheet,"Move slowly",24,-456,380,52,18,FontStyles.Bold);
            tab=Panel("Checklist toggle",rect,0,40,460,34,new Color(.12f,.22f,.24f));
            toggle=Text(tab,"X  Hide checklist",12,-3,436,30,19,FontStyles.Bold);toggle.color=Color.white;
            transform.localScale=Vector3.one*.001f;
        }
        RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color color){var g=new GameObject(name,typeof(RectTransform),typeof(Image));var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);var im=g.GetComponent<Image>();im.color=color;im.material=panelMaterial;im.raycastTarget=false;return r;}
        TMP_Text Text(Transform parent,string value,float x,float y,float w,float h,float size,FontStyles style){var g=new GameObject(value,typeof(RectTransform));g.transform.SetParent(parent,false);var t=g.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSharedMaterial=textMaterial;t.fontSize=size;t.fontStyle=style;t.color=new Color(.1f,.17f,.19f);t.text=value;t.raycastTarget=false;var r=t.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return t;}
        public void SetVisible(bool value){Visible=value;paper.gameObject.SetActive(value);toggle.text=value?"X  Hide checklist":"X  Show checklist";}
        public void Refresh(bool[] done,bool showering,float seconds){int count=0;for(int i=0;i<5;i++){if(done[i])count++;boxes[i].color=done[i]?new Color(.12f,.55f,.36f):new Color(.72f,.75f,.71f);ticks[i].SetActive(done[i]);rows[i].text=names[i]+(i==1&&showering?"  "+Mathf.CeilToInt(seconds)+" s":"");rows[i].color=done[i]?new Color(.16f,.40f,.28f):new Color(.1f,.17f,.19f);}title.text="FIELD CHECKLIST   <size=18>"+count+" / 5</size>";}
        void Update(){bool pressed;InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out pressed);if(pressed&&!wasPressed)SetVisible(!Visible);wasPressed=pressed;
            if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame&&Camera.main&&RectTransformUtility.RectangleContainsScreenPoint(tab,Mouse.current.position.ReadValue(),Camera.main))SetVisible(!Visible);
        }
        void OnDestroy(){if(panelMaterial)Destroy(panelMaterial);if(textMaterial)Destroy(textMaterial);}
    }
}
