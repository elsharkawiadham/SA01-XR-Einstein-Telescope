using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GroupProject.Opening;

namespace GroupProject.Preparation
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PersonalClothingStorage:MonoBehaviour
    {
        public PreparationJourney journey;
        public WearableGarment jacket;
        public GameObject foldedTrousers;
        float nextPress;
        void OnEnable(){var input=GetComponent<XRSimpleInteractable>();input.selectEntered.AddListener(Select);input.activated.AddListener(Activate);}
        void OnDisable(){var input=GetComponent<XRSimpleInteractable>();input.selectEntered.RemoveListener(Select);input.activated.RemoveListener(Activate);}
        void Select(SelectEnterEventArgs e)=>Store();
        void Activate(ActivateEventArgs e)=>Store();
        public bool Store()
        {
            if(!journey||!jacket||journey.PersonalClothesStored||Time.unscaledTime<nextPress||Vector3.Distance(journey.head.position,transform.position)>2.5f)return false;
            nextPress=Time.unscaledTime+.3f;
            var avatar=journey.ride.playerRoot.GetComponent<PlayerBodyAvatar>();
            if(foldedTrousers&&avatar&&avatar.BodyRenderer)foldedTrousers.GetComponent<Renderer>().sharedMaterial=avatar.BodyRenderer.sharedMaterials[1];
            jacket.StoreFromWardrobe();
            if(foldedTrousers)foldedTrousers.SetActive(true);
            GetComponent<GroupProject.Guide.InteractionFeedback>()?.Pulse(true);return true;
        }
        void Update()
        {
            if(foldedTrousers&&journey.PersonalClothesStored&&!foldedTrousers.activeSelf)foldedTrousers.SetActive(true);
            if(DesktopPreview.Active&&QuestControllerPreview.TriggerPressed&&Physics.Raycast(journey.head.position,journey.head.forward,out var hit,3)&&hit.collider.GetComponentInParent<PersonalClothingStorage>()==this)Store();
        }
    }
}
