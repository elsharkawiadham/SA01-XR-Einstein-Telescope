using UnityEngine;
namespace GroupProject.Preparation
{
    /// <summary>Air-flow cues, not water or a measure of decontamination.</summary>
    public sealed class AirShowerEffects : MonoBehaviour
    {
        public PreparationJourney journey;
        public AudioSource fan;
        public ParticleSystem[] jets;
        public Light cycleLight;
        [Range(0,1)] public float fanVolume=.22f;
        bool active;
        void Update()
        {
            bool run=journey&&journey.ShowerRunning;
            if(run!=active){active=run;foreach(var jet in jets)if(jet){if(run)jet.Play();else jet.Stop(true,ParticleSystemStopBehavior.StopEmitting);}if(run&&fan&&!fan.isPlaying)fan.Play();}
            if(fan){fan.volume=Mathf.MoveTowards(fan.volume,run?fanVolume:0,Time.deltaTime*.25f);if(!run&&fan.volume<=0)fan.Stop();}
            if(cycleLight)cycleLight.color=run?new Color(.25f,.65f,1):journey&&journey.ShowerComplete?Color.green:new Color(.65f,.8f,1);
        }
        void OnDisable(){if(fan)fan.Stop();if(jets!=null)foreach(var jet in jets)if(jet)jet.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);active=false;}
    }
}
