using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace GroupProject.Preparation
{
    public sealed class PaceReadout:MonoBehaviour
    {
        public TMP_Text Label{get;private set;}
        public Canvas Canvas{get;private set;}
        Transform head;
        Material panelMaterial,textMaterial;
        public void Build(Transform view)
        {
            head=view;transform.SetParent(head,false);
            var rect=(RectTransform)transform;rect.sizeDelta=new Vector2(520,76);transform.localScale=Vector3.one*.001f;
            Canvas=gameObject.AddComponent<Canvas>();Canvas.renderMode=RenderMode.WorldSpace;Canvas.overrideSorting=true;Canvas.sortingOrder=30020;
            panelMaterial=new Material(Resources.Load<Shader>("ChecklistPanel"));
            var background=gameObject.AddComponent<Image>();background.material=panelMaterial;background.color=new Color(.025f,.09f,.12f,.95f);background.raycastTarget=false;
            var g=new GameObject("Live walking pace",typeof(RectTransform));g.transform.SetParent(transform,false);Label=g.AddComponent<TextMeshProUGUI>();Label.font=TMP_Settings.defaultFontAsset;
            textMaterial=new Material(Label.font.material);textMaterial.shader=Resources.Load<Shader>("ChecklistText");textMaterial.renderQueue=4010;Label.fontSharedMaterial=textMaterial;Label.fontSize=24;Label.alignment=TextAlignmentOptions.Center;Label.raycastTarget=false;Label.rectTransform.sizeDelta=new Vector2(500,66);
        }
        public void Refresh(float speed,int band,bool travelling)
        {Label.text="<size=17>PACE</size>   "+speed.ToString("F2")+" m/s   <size=19>"+(travelling?"Lift travelling":band==2?"Too fast":band==1?"Slow down":"Good pace")+"</size>";Label.color=band==2?new Color(1,.65f,.40f):band==1?new Color(1,.84f,.38f):new Color(.55f,.94f,.77f);}
        void LateUpdate()=>Place();
        public void Place()
        {
            if(!head)return;var camera=head.GetComponent<Camera>();float halfY=.55f;if(camera)halfY=1.15f/Mathf.Abs(camera.projectionMatrix.m11);
            if(camera&&camera.stereoEnabled)for(int i=0;i<2;i++){var p=camera.GetStereoProjectionMatrix((Camera.StereoscopicEye)i);halfY=Mathf.Min(halfY,1.15f*(1-Mathf.Abs(p.m12))/Mathf.Abs(p.m11));}
            transform.localPosition=new Vector3(0,-Mathf.Min(.38f,halfY*.65f),1.15f);transform.localRotation=Quaternion.identity;
        }
        void OnDestroy(){if(panelMaterial)Destroy(panelMaterial);if(textMaterial)Destroy(textMaterial);}
    }
}
