using UnityEngine;
namespace GroupProject.Opening
{
    /// <summary>Compatibility entry point used by the opening sequence and older scene builders.</summary>
    public sealed class ControllerPresence : MonoBehaviour
    {
        void Awake()
        {
            // Keep the tracked controllers and interactors; replace their visual representation only.
            var hands = GetComponent<AnimatedControllerHands>();
            if (!hands) hands = gameObject.AddComponent<AnimatedControllerHands>();
            hands.Initialize();
        }
    }
}
