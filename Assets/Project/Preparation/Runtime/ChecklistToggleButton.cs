using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Preparation
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class ChecklistToggleButton:MonoBehaviour
    {
        public ChecklistClipboard checklist;
        public DressingChecklist outfit;
        float nextPress;
        public static void Attach(RectTransform tab,ChecklistClipboard checklist=null,DressingChecklist outfit=null)
        {
            var collider=tab.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(tab.rect.width,tab.rect.height,24);collider.center=new Vector3(tab.rect.width*.5f,-tab.rect.height*.5f,0);
            var input=tab.gameObject.AddComponent<XRSimpleInteractable>();input.colliders.Add(collider);
            var button=tab.gameObject.AddComponent<ChecklistToggleButton>();button.checklist=checklist;button.outfit=outfit;
        }
        void OnEnable(){var input=GetComponent<XRSimpleInteractable>();input.selectEntered.AddListener(Selected);input.activated.AddListener(Activated);}
        void OnDisable(){var input=GetComponent<XRSimpleInteractable>();input.selectEntered.RemoveListener(Selected);input.activated.RemoveListener(Activated);}
        void Selected(SelectEnterEventArgs e)=>Press();
        void Activated(ActivateEventArgs e)=>Press();
        public void Press(){if(Time.unscaledTime<nextPress)return;nextPress=Time.unscaledTime+.3f;if(checklist)checklist.SetVisible(!checklist.Visible);if(outfit)outfit.SetVisible(!outfit.Visible);}
    }
}
