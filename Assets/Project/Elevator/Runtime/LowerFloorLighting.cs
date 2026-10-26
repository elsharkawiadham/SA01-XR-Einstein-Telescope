using UnityEngine;
using UnityEngine.Rendering;
namespace GroupProject.Elevator
{
    // The opening stays dark; B1 needs readable surfaces even outside a ceiling light's range.
    public class LowerFloorLighting : MonoBehaviour
    {
        AmbientMode previousMode;
        Color previousAmbient;
        bool applied;
        void OnEnable()
        {
            previousMode = RenderSettings.ambientMode;
            previousAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.57f);
            applied = true;
        }
        void OnDisable()
        {
            if (!applied) return;
            RenderSettings.ambientMode = previousMode;
            RenderSettings.ambientLight = previousAmbient;
            applied = false;
        }
    }
}
