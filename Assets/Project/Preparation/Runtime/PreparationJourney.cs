using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GroupProject.Opening;
using GroupProject.Elevator;
using GroupProject.Cleaning;
using GroupProject.Guide;
namespace GroupProject.Preparation
{
    // Prototype rehearsal settings, not facility-approved timings or a gowning SOP.
    public sealed class PreparationJourney : MonoBehaviour
    {
        public ElevatorRide ride;
        public Transform head;
        public StandaloneAIGuide guide;
        public float showerSeconds=15, cautionSpeed=.65f, excessiveSpeed=1f;
        public bool PersonalClothesStored { get; private set; }
        public bool Dressed => PersonalClothesStored && garments.Count==7;
        public bool ShowerComplete { get; private set; }
        public float Speed { get; private set; }
        public bool ShowerRunning => showering;
        readonly HashSet<int> garments=new HashSet<int>();
        readonly Queue<string> speech=new Queue<string>();
        TMP_Text checklist,speedText,roomText;
        Transform hud;
        public GameObject showerGate,cleanGate;
        public TMPro.TMP_Text showerDisplay;
        WearableGarment heldGarment;
        Vector3 previous;
        Quaternion previousRotation;
        bool briefed,showering,inspected,guided;
        float remaining,fastFor,nextWarning;
        public static readonly string[] Briefing={
            "Before entering the cleaning area, prepare yourself and follow these five steps.",
            "Put on your cleanroom clothing before entering the air shower.",
            "Enter the air shower and wait until the cycle finishes.",
            "Pick up the lamp and inspect the training plate.",
            "Choose the Class A wipe. Complete four passes, using a fresh face each time.",
            "Choose the correct wipe for Class B and repeat the cleaning technique."
        };
        void Start()
        {
            previous=head.position;previousRotation=ride.playerRoot.rotation;
            hud=new GameObject("Training checklist").transform;hud.SetParent(transform);
            checklist=Label(hud,"Checklist",Vector3.zero,new Vector2(1.15f,.6f), .05f);checklist.rectTransform.pivot=new Vector2(0,1);
            speedText=Label(hud,"Walking pace",new Vector3(0,-.52f,0),new Vector2(1.15f,.15f),.045f);
            speedText.rectTransform.pivot=new Vector2(0,1);
            hud.localScale=Vector3.one*.45f;hud.gameObject.SetActive(false);
        }
        static TMP_Text Label(Transform parent,string name,Vector3 pos,Vector2 size,float font)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;
            var t=g.AddComponent<TextMeshPro>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=font*10;t.rectTransform.sizeDelta=size;t.color=Color.white;t.fontMaterial.SetFloat("_OutlineWidth",.18f);t.fontMaterial.SetColor("_OutlineColor",Color.black);t.textWrappingMode=TextWrappingModes.Normal;return t;
        }
        public void Tell(string text){if(speech.Count<4)speech.Enqueue(text);}
        public void StorePersonalClothes(){PersonalClothesStored=true;Tell("Your jacket is stored. Take the cleanroom clothing from the lockers.");}
        public void Equip(int kind){if(kind<1||kind>7||!PersonalClothesStored)return;if(garments.Add(kind))Tell(Dressed?"You are dressed. Continue to the air shower.":"Item fitted. Take the next cleanroom garment.");if(showerGate)showerGate.SetActive(!Dressed);}
        public string Answer(string question)
        {
            if(!ride.destinationRoot.activeInHierarchy)return null;
            var q=question.ToLowerInvariant();
            if(q.Contains("speed")||q.Contains("too fast")||q.Contains("slow"))return "Move the left joystick gently. Keep the pace indicator green. The coloured thresholds are demonstration settings.";
            if(q.Contains("cloth")||q.Contains("dress")||q.Contains("glove"))return "Open the locker with the trigger. Grip your jacket and release it inside the personal locker. Grip each cleanroom garment, bring it to the matching body area, then release: hood and mask at your head, suit at your chest, gloves at your hands and footwear at your feet.";
            if(q.Contains("shower"))return "Follow the floor line from the changing room to the air shower. After wearing all cleanroom garments, step inside and select START AIR SHOWER with the trigger. Stay inside until the countdown ends.";
            if(!ShowerComplete&&head.position.x<24.6f&&(q.Contains("help")||q.Contains("next")||q.Contains("what do")||q.Contains("where do")))return Dressed?"Enter the air shower and select its start button. Stay inside until the cycle ends.":"Follow the floor line to the changing room. Open the lockers, store your jacket, then grip and fit the cleanroom clothing.";
            return null;
        }
        public void Select(int id)
        {
            if(!ride.destinationRoot.activeInHierarchy)return;
            if(id<0||id>4)return;
            if(id!=4)return;
            if(!Dressed){speech.Enqueue("Store your jacket and put on all the cleanroom garments first.");return;}
            if(ShowerComplete||showering)return;
            if(!InsideShower()){speech.Enqueue("Step inside the air shower before starting.");return;}
            showering=true;remaining=showerSeconds;speech.Enqueue("Air shower started. Stay inside until the cycle finishes.");
        }
        bool InsideShower(){var p=head.position;return p.x>22&&p.x<24.5f&&Mathf.Abs(p.z-19.3f)<.85f;}
        void Update()
        {
            if(!briefed&&ride.State==ElevatorRide.Phase.Descending){briefed=true;hud.gameObject.SetActive(true);foreach(var line in Briefing)speech.Enqueue(line);}
            var delta=head.position-previous;previous=head.position;delta.y=0;
            float instant=Time.deltaTime>0?delta.magnitude/Time.deltaTime:0;
            if(ride.IsTravelling||delta.magnitude>.5f||Quaternion.Angle(previousRotation,ride.playerRoot.rotation)>1)instant=0;
            previousRotation=ride.playerRoot.rotation;
            Speed=Mathf.Lerp(Speed,instant,1-Mathf.Exp(-Time.deltaTime*4));
            bool walking=briefed&&ride.State==ElevatorRide.Phase.Arrived;
            fastFor=walking&&Speed>excessiveSpeed?fastFor+Time.deltaTime:0;
            if(fastFor>2&&Time.time>nextWarning&&speech.Count==0&&guide&&guide.Ready&&!guide.IsBusy){var parrot=FindFirstObjectByType<ParrotCompanion>();if(!parrot||!parrot.Speaking){speech.Enqueue("Please walk slowly and keep your movements gentle.");nextWarning=Time.time+20;fastFor=0;}}
            speedText.text=walking?"PACE  "+(Speed>excessiveSpeed?"Too fast":Speed>cautionSpeed?"Slow down":"Good pace"):"Move slowly • Keep clean items off the floor";
            speedText.color=Speed>excessiveSpeed?Color.red:Speed>cautionSpeed?Color.yellow:Color.green;
            if(showering){if(!InsideShower()){showering=false;remaining=showerSeconds;speech.Enqueue("The cycle stopped. Return inside and start again.");}else{remaining-=Time.deltaTime;if(remaining<=0){showering=false;ShowerComplete=true;cleanGate.SetActive(false);speech.Enqueue("Air shower complete. Follow the route to the inspection lamp.");}}}
            var station=CleaningStation.Active;
            if(station){var e=station.Exercise;inspected|=e.State!=CleaningExercise.Stage.Inspect;guided|=e.Trial>0||e.State==CleaningExercise.Stage.Review;}
            checklist.text="YOUR FIVE TASKS\n"+Mark(Dressed)+"1. Cleanroom clothing\n"+Mark(ShowerComplete)+"2. Air shower"+(showering?" — "+Mathf.CeilToInt(remaining)+" s":"")+"\n"+Mark(inspected)+"3. Inspect the plate\n"+Mark(guided)+"4. Guided Class A cleaning\n"+Mark(station&&station.Exercise.State==CleaningExercise.Stage.Complete)+"5. Independent Class B cleaning";
            if(speech.Count>0&&guide&&guide.Ready&&!guide.IsBusy){var p=FindFirstObjectByType<ParrotCompanion>();if(!p||!p.Speaking)_=guide.SpeakInstruction(speech.Dequeue());}
            if(showerDisplay)showerDisplay.text=showering?"AIR CYCLE\n"+Mathf.CeilToInt(remaining)+" s":ShowerComplete?"CYCLE COMPLETE\nContinue to cleaning":"AIR SHOWER\nAim + trigger to start";
            if(DesktopPreview.Active){
                if(heldGarment){float reach=(heldGarment.kind==WearableGarment.Kind.LeftBoot||heldGarment.kind==WearableGarment.Kind.RightBoot)&&head.forward.y<-.5f?Mathf.Clamp(head.position.y-ride.playerRoot.position.y-.15f,.7f,1.8f):.5f;
                    if(QuestControllerPreview.Lift&&(heldGarment.kind==WearableGarment.Kind.Hood||heldGarment.kind==WearableGarment.Kind.Mask))reach=.12f;
                    heldGarment.transform.SetPositionAndRotation(head.position+head.forward*reach+(QuestControllerPreview.Lift?Vector3.up*.12f:Vector3.zero),Quaternion.Euler(0,head.eulerAngles.y,0));if(QuestControllerPreview.GripPressed){var released=heldGarment;heldGarment=null;released.SetPreview(false);}}
                else if(QuestControllerPreview.GripPressed&&Physics.Raycast(head.position,head.forward,out var grabHit,2)){var g=grabHit.collider.GetComponentInParent<WearableGarment>();if(g&&!g.Equipped&&!g.Stored){heldGarment=g;g.SetPreview(true);}}
                if(QuestControllerPreview.TriggerPressed&&Physics.Raycast(head.position,head.forward,out var hit,3)){var b=hit.collider.GetComponentInParent<PreparationButton>();if(b)b.Press();var door=hit.collider.GetComponentInParent<LockerDoor>();if(door)door.Toggle();}
            }
        }
        static string Mark(bool done)=>done?"[Done] ":"[ ] ";
        void LateUpdate(){if(hud){hud.position=head.TransformPoint(new Vector3(-.69f,.46f,1.35f));hud.rotation=head.rotation;}}
    }
}
