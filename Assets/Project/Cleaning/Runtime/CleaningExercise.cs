using UnityEngine;
namespace GroupProject.Cleaning
{
    // Client slides 9-13: a rehearsal, not certification of physical cleanliness.
    public sealed class CleaningExercise
    {
        public enum Stage { Inspect, ChooseWipe, Wipe, Review, Complete }
        public enum WipeKind { AnticonGold, KimtechW4 }
        public Stage State { get; private set; }
        public int Trial { get; private set; }
        public int Passes { get; private set; }
        public int Corrections { get; private set; }
        public bool NeedsFreshFace { get; private set; }
        public bool Contact { get; private set; }
        public string Feedback { get; private set; }
        public bool Guided => Trial == 0;
        public string PartClass => Guided ? "A" : "B";
        bool validStroke, reachedEnd;
        float farthest;
        public float Lane => -.12f + Passes * .08f;
        public CleaningExercise() { Feedback = "Inspect this Class A training plate. Grip the inspection lamp, then hold the trigger and aim at the plate."; }
        public void Inspected()
        {
            if(State != Stage.Inspect)return;
            State=Stage.ChooseWipe;
            Feedback="Particles are visible in this demonstration. Choose the wipe suitable for Class " + PartClass + ". Grip a wipe to pick it up.";
        }
        public bool Select(WipeKind kind)
        {
            if(State==Stage.Inspect){Feedback="Inspect the plate with the lamp before choosing a wipe.";return false;}
            if(State!=Stage.ChooseWipe && State!=Stage.Wipe)return false;
            var expected=Guided?WipeKind.AnticonGold:WipeKind.KimtechW4;
            if(kind!=expected){Corrections++;Feedback=Guided?"KIMTECH W4 is not suitable for Class A. Choose Anticon Gold polyester.":"Anticon Gold is intended for Class A; the client does not recommend it for Class B. Choose KIMTECH W4 here.";return false;}
            State=Stage.Wipe;
            Feedback="Use four parallel, slightly overlapping passes. Pull from left to right and lift at each end. Use a fresh face for each pass.";
            return true;
        }
        public void FreshFace()
        {
            if(State!=Stage.Wipe)return;
            if(Contact){Feedback="Lift the wipe off the plate before changing its face.";return;}
            if(!NeedsFreshFace){Feedback="This face is ready. Make the next pass.";return;}
            NeedsFreshFace=false;
            Feedback=Passes==2?"Refolded to a fresh face. Make the third pass.":"Flipped to a fresh face. Make the next pass.";
        }
        public void Sample(bool touching, Vector2 point)
        {
            if(State!=Stage.Wipe)return;
            if(!touching)
            {
                if(Contact && validStroke && reachedEnd && point.x>=.18f && Mathf.Abs(point.y-Lane)<.055f)
                {
                    Passes++;NeedsFreshFace=true;
                    if(Passes==4){State=Stage.Review;Feedback="Four passes completed. Visible particles are reduced; this does not certify cleanliness. Fingerprints and other molecular contamination may remain invisible. Select CONTINUE.";}
                    else Feedback=Passes==2?"Pass complete. Lifted correctly. Press the trigger to refold to a fresh face.":"Pass complete. Lifted correctly. Press the trigger to flip to a fresh face.";
                }
                Contact=false;validStroke=false;reachedEnd=false;return;
            }
            if(!Contact)
            {
                Contact=true;farthest=point.x;reachedEnd=false;
                validStroke=!NeedsFreshFace && point.x<=-.18f && Mathf.Abs(point.y-Lane)<.045f;
                if(!validStroke){Corrections++;Feedback=NeedsFreshFace?"That face has already been used. Lift, then press the trigger for a fresh face.":"Lift and start at the left edge of the next parallel pass.";}
            }
            if(!validStroke)return;
            if(point.x < farthest-.035f || Mathf.Abs(point.y-Lane)>.055f)
            {validStroke=false;Corrections++;Feedback="Avoid scrubbing back and forth or crossing the passes. Lift and retry this pass from the left.";return;}
            farthest=Mathf.Max(farthest,point.x);
            if(point.x>=.18f){reachedEnd=true;Feedback="Now lift the wipe to finish this pass.";}
        }
        public void Continue()
        {
            if(State!=Stage.Review)return;
            if(Trial==0){Trial=1;Passes=0;NeedsFreshFace=false;Contact=false;State=Stage.Inspect;Feedback="Now try a Class B plate independently. Inspect, choose a suitable wipe, then repeat the four-pass technique.";}
            else{State=Stage.Complete;Feedback="Rehearsal complete. You practised inspection, wipe selection, fresh faces and parallel passes. Follow the approved local procedure for real cleaning; ask trained staff about solvents, ultrasonic baths and baking.";}
        }
    }
}
