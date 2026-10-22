using System.Collections;
using UnityEngine;

namespace GroupProject.Opening
{
    public sealed class OpeningSequence : MonoBehaviour
    {
        [Min(0.1f)] public float fadeSeconds = 3f;
        public Camera headsetCamera;
        public Material fadeMaterial;
        public AudioSource welcome;
        public LocomotionGuide locomotionGuide;
        public StandaloneAIGuide aiGuide;
        public const string WelcomeText="Welcome. Take a moment to look around and get comfortable. Let us explore moving forward, backward, and turning.";
        public const float ExplorationSeconds=20f;
        void Awake()
        {
            if(!enabled)return;
            var parrot=FindFirstObjectByType<GroupProject.Guide.ParrotCompanion>();
            if(parrot)parrot.ArriveAfter(ExplorationSeconds);
            if(headsetCamera){var rig=headsetCamera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();if(rig && !rig.GetComponent<ControllerPresence>())rig.gameObject.AddComponent<ControllerPresence>();}
        }
        Material instance;
        GameObject overlay;
        IEnumerator Start()
        {
            float spawned=Time.unscaledTime;
            if (!headsetCamera || !fadeMaterial) yield break;
            overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlay.name = "Opening fade (runtime)";
            Destroy(overlay.GetComponent<Collider>());
            overlay.transform.SetParent(headsetCamera.transform, false);
            overlay.transform.localPosition = new Vector3(0, 0, headsetCamera.nearClipPlane + 0.02f);
            overlay.transform.localScale = Vector3.one * 4f;
            instance = new Material(fadeMaterial);
            overlay.GetComponent<Renderer>().sharedMaterial = instance;
            instance.SetFloat("_Alpha", 1);
            yield return null;
            float elapsed = 0;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                instance.SetFloat("_Alpha", 1 - Mathf.SmoothStep(0, 1, elapsed / fadeSeconds));
                yield return null;
            }
            Destroy(overlay);
            while(Time.unscaledTime-spawned<ExplorationSeconds)yield return null;
            bool spoken=false;
            if(aiGuide){
                var speech=aiGuide.SpeakInstruction(WelcomeText);
                while(!speech.IsCompleted)yield return null;
                spoken=speech.Status==System.Threading.Tasks.TaskStatus.RanToCompletion && speech.Result;
            }
            if (!spoken && welcome) {
                welcome.Play();
                yield return null;
                while(welcome.isPlaying) yield return null;
            }
            if(locomotionGuide) locomotionGuide.Begin();
        }
        void OnDestroy()
        {
            if (overlay) Destroy(overlay);
            if (instance) Destroy(instance);
        }
    }
}
