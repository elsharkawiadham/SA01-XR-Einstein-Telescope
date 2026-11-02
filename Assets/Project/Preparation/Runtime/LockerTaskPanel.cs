using UnityEngine;
using TMPro;

namespace GroupProject.Preparation
{
    public sealed class LockerTaskPanel:MonoBehaviour
    {
        public PreparationJourney journey;
        public int number;
        public TMP_Text status,instruction;
        public Renderer indicator;
        public GameObject checkMark;
        public Material waitingMaterial,readyMaterial,completeMaterial;
        public bool Complete=>IsComplete(journey,number);
        public bool Current{get{if(Complete)return false;for(int i=1;i<number;i++)if(!IsComplete(journey,i))return false;return true;}}
        static readonly string[] tasks={"Open locker 01.\nSelect STORE CLOTHES.","Grip coverall. Bring to chest.\nRelease grip to fit.","Fit hair cap, hood, mask\nand safety glasses.","Fit each glove using\nthe opposite hand.","Fit both boots,\nthen both overshoes."};
        public static bool IsComplete(PreparationJourney j,int n)
        {
            if(!j)return false;
            switch(n){case 1:return j.PersonalClothesStored;case 2:return j.HasGarment(WearableGarment.Kind.Coverall);case 3:return j.HasGarment(WearableGarment.Kind.HairCap)&&j.HasGarment(WearableGarment.Kind.Hood)&&j.HasGarment(WearableGarment.Kind.Mask)&&j.HasGarment(WearableGarment.Kind.SafetyGlasses);case 4:return j.HasGarment(WearableGarment.Kind.LeftGlove)&&j.HasGarment(WearableGarment.Kind.RightGlove);case 5:return j.HasGarment(WearableGarment.Kind.LeftBoot)&&j.HasGarment(WearableGarment.Kind.RightBoot)&&j.HasGarment(WearableGarment.Kind.LeftOvershoe)&&j.HasGarment(WearableGarment.Kind.RightOvershoe);default:return false;}
        }
        void Update()
        {
            bool done=Complete,current=true;for(int i=1;i<number;i++)current&=IsComplete(journey,i);current&=!done;
            if(status){status.text=done?"<color=#77F0AC>COMPLETE</color>":current?"<color=#74E4EE>YOUR NEXT STEP</color>":"UPCOMING STEP";}
            if(checkMark)checkMark.SetActive(done);
            if(instruction)instruction.text=done?(number==5?"Outfit ready. Continue to the air shower.":"Completed. Continue to locker "+(number+1).ToString("00")+"."):tasks[Mathf.Clamp(number-1,0,4)];
            if(indicator)indicator.sharedMaterial=done?completeMaterial:current?readyMaterial:waitingMaterial;
        }
    }
}
