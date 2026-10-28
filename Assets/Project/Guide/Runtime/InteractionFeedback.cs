using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Guide
{
    /// <summary>Shared hover/accepted/rejected cues. Never performs the action or changes its collider.</summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public sealed class InteractionFeedback : MonoBehaviour
    {
        public Renderer[] surfaces;
        public Transform pressVisual;
        public Vector3 pressDirection=Vector3.forward;
        public float pressDistance=.008f;
        public bool subtle;
        public Color ready=new Color(.12f,.65f,.72f),hover= new Color(.65f,.95f,1),accepted=new Color(.2f,.9f,.45f),rejected=new Color(1,.5f,.12f);
        public int AcceptedCount {get;private set;}
        public bool Latched {get;private set;}
        XRBaseInteractable interactable;MaterialPropertyBlock block;Color[] originals;
        Vector3 rest;float pulseUntil;bool success;
        void Awake(){interactable=GetComponent<XRBaseInteractable>();block=new MaterialPropertyBlock();if(surfaces==null||surfaces.Length==0)surfaces=GetComponents<Renderer>();originals=new Color[surfaces.Length];for(int i=0;i<surfaces.Length;i++){var m=surfaces[i]?surfaces[i].sharedMaterial:null;originals[i]=m&&m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white;}if(pressVisual)rest=pressVisual.localPosition;}
        void OnEnable(){GetComponent<XRBaseInteractable>().selectEntered.AddListener(Selected);}
        void Selected(SelectEnterEventArgs args){if(subtle)Pulse(true);}
        public void Pulse(bool wasAccepted){success=wasAccepted;pulseUntil=Time.unscaledTime+.32f;if(wasAccepted)AcceptedCount++;}
        public void SetLatched(bool value){Latched=value;}
        void LateUpdate()
        {
            bool pulse=Time.unscaledTime<pulseUntil;
            var color=pulse?(success?accepted:rejected):Latched?accepted:interactable.isHovered?hover:ready;
            for(int i=0;i<surfaces.Length;i++)if(surfaces[i]){surfaces[i].GetPropertyBlock(block);block.SetColor("_BaseColor",subtle?Color.Lerp(originals[i],color,pulse||interactable.isHovered?.28f:0):color);surfaces[i].SetPropertyBlock(block);}
            if(pressVisual){float t=Mathf.Clamp01((pulseUntil-Time.unscaledTime)/.32f);float amount=pulse?Mathf.Sin(t*Mathf.PI)*pressDistance:0;pressVisual.localPosition=rest+(pressVisual.parent?pressVisual.parent.InverseTransformVector(pressDirection.normalized*amount):pressDirection.normalized*amount);}
        }
        void OnDisable(){GetComponent<XRBaseInteractable>().selectEntered.RemoveListener(Selected);if(pressVisual)pressVisual.localPosition=rest;for(int i=0;surfaces!=null&&i<surfaces.Length;i++)if(surfaces[i])surfaces[i].SetPropertyBlock(null);}
    }
}
