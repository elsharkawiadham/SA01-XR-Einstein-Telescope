using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace GroupProject.Preparation
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class PreparationButton : MonoBehaviour
    {
        public PreparationJourney journey;public int id;float nextPress;
        void OnEnable(){var button=GetComponent<XRSimpleInteractable>();button.selectEntered.AddListener(Selected);button.activated.AddListener(Activated);}
        void OnDisable(){var button=GetComponent<XRSimpleInteractable>();button.selectEntered.RemoveListener(Selected);button.activated.RemoveListener(Activated);}
        void Selected(SelectEnterEventArgs e){Press();}
        void Activated(ActivateEventArgs e){Press();}
        public bool Press(){if(Time.unscaledTime<nextPress)return false;nextPress=Time.unscaledTime+.3f;bool accepted=journey&&journey.Select(id);GetComponent<GroupProject.Guide.InteractionFeedback>()?.Pulse(accepted);return accepted;}
        void Update(){var cue=GetComponent<GroupProject.Guide.InteractionFeedback>();if(cue&&journey)cue.SetLatched(journey.ShowerRunning||journey.ShowerComplete);}
    }
}
