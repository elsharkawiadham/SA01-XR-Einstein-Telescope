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
        public float showerSeconds=15, cautionSpeed=1.3f, excessiveSpeed=2f;
        public bool PersonalClothesStored { get; private set; }
        public bool Dressed => PersonalClothesStored && garments.Count==11;
        public bool ShowerComplete { get; private set; }
        public float Speed { get; private set; }
        public bool ShowerRunning => showering;
        readonly HashSet<int> garments=new HashSet<int>();
        readonly Queue<string> speech=new Queue<string>();
        TMP_Text speedText,roomText;
        ChecklistClipboard clipboard;
        Transform hud;
        public GameObject showerGate,cleanGate;
        public TMPro.TMP_Text showerDisplay;
        WearableGarment heldGarment;
        DressingRoomExperience dressing;
        public bool HasGarment(WearableGarment.Kind kind)=>garments.Contains((int)kind);
        Vector3 previous;
        Quaternion previousRotation;
        bool briefed,showering,inspected,guided,lockersExplained;
        float remaining,fastFor,nextWarning;
        public static readonly string[] Briefing={
            TrainingGuidance.Checklist,
            "Put on your cleanroom clothing before entering the air shower.",
            "Enter the air shower and wait until the cycle finishes.",
            "Pick up the lamp and inspect the training plate.",
            "Choose the Class A wipe. Complete four passes, using a fresh face each time.",
            "Choose the correct wipe for Class B and repeat the cleaning technique."
        };
        void Start()
        {
            previous=head.position;previousRotation=ride.playerRoot.rotation;
            hud=new GameObject("Training checklist",typeof(RectTransform)).transform;hud.SetParent(transform);
            clipboard=hud.gameObject.AddComponent<ChecklistClipboard>();clipboard.Build();speedText=clipboard.Pace;
            hud.gameObject.SetActive(false);
            dressing=gameObject.AddComponent<DressingRoomExperience>();dressing.Initialize(this);
        }

        public void Tell(string text){if(speech.Count<4)speech.Enqueue(text);}
        public void StorePersonalClothes(){PersonalClothesStored=true;Tell("Your jacket is stored. Take the cleanroom clothing from the lockers.");}
        public void Equip(int kind){if(kind<1||kind>11||!PersonalClothesStored)return;if(garments.Add(kind))Tell(Dressed?"You are dressed. Continue to the air shower.":WearableGarment.NameFor((WearableGarment.Kind)kind)+" fitted. Check your clothing list for the remaining items.");if(showerGate)showerGate.SetActive(!Dressed);}
        public string Answer(string question)
        {
            var q=question.ToLowerInvariant();
            if(q.Contains("checklist")||q.Contains("check list")||q.Contains("task list")||q.Contains("to do list")||q.Contains("todo list")||q.Contains("outfit list")||q.Contains("clothing list")||q.Contains("dressing list")){
                if(clipboard&&(q.Contains("hide")||q.Contains("close")||q.Contains("dismiss"))){if(dressing&&dressing.InChangingRoom)dressing.SetChecklistVisible(false);else clipboard.SetVisible(false);return "Checklist hidden. Say show the checklist to bring it back.";}
                if(clipboard&&(q.Contains("show")||q.Contains("open")||q.Contains("display"))){if(dressing&&dressing.InChangingRoom)dressing.SetChecklistVisible(true);else{hud.gameObject.SetActive(true);clipboard.SetVisible(true);}return "Here is your checklist.";}
            }
            if(!ride.destinationRoot.activeInHierarchy)return null;
            if(dressing&&dressing.InChangingRoom){
                if(dressing.Held&&(q.Contains("help")||q.Contains("how")||q.Contains("where")))return dressing.Held.DisplayName+": "+dressing.Held.Hint;
                if(q.Contains("boot")||q.Contains("shoe")||q.Contains("footwear"))return "Footwear is in the right-hand locker. Grip a boot and release it above the marked foot space. Both boots have separate checkmarks. Put the overshoes on afterwards.";
                if(q.Contains("cap")||q.Contains("hood")||q.Contains("mask")||q.Contains("glass"))return "The headwear is in the hood and mask locker: hair cap above the hood, mask below it, and glasses on the lower shelf. Grip one item, bring it near your head and release when the target says ready.";
            }
            if(q.Contains("speed")||q.Contains("too fast")||q.Contains("slow"))return "Move the left joystick gently. Keep the pace indicator green. The coloured thresholds are demonstration settings.";
            if(q.Contains("cloth")||q.Contains("dress")||q.Contains("glove"))return "Open the locker with the trigger. Grip your jacket and release it inside the personal locker. Grip one item and follow its highlighted fitting target. Put the cap, hood, mask and glasses near your head, the suit at your chest, and gloves near the other hand. Release boots over their marked foot spaces, then fit the overshoes. In the controller preview, hold the trigger to bring the held item to its target, then use grip to release.";
            if(q.Contains("shower"))return "Follow the floor line from the changing room to the air shower. After wearing all cleanroom garments, step inside and select START AIR SHOWER with the trigger. Stay inside until the countdown ends.";
            if(!ShowerComplete&&head.position.x<24.6f&&(q.Contains("help")||q.Contains("next")||q.Contains("what do")||q.Contains("where do")))return Dressed?"Enter the air shower and select its start button. Stay inside until the cycle ends.":"Follow the floor line to the changing room. Open the lockers, store your jacket, then grip and fit the cleanroom clothing.";
            return null;
        }
        public bool Select(int id)
        {
            if(!ride.destinationRoot.activeInHierarchy||id!=4)return false;
            if(!Dressed){Tell("Store your jacket and put on all the cleanroom garments first.");return false;}
            if(ShowerComplete||showering)return false;
            if(!InsideShower()){Tell("Step inside the air shower before starting.");return false;}
            showering=true;remaining=showerSeconds;Tell("Air shower started. Stay inside until the cycle finishes.");return true;
        }
        public bool AboveWarningThreshold(float speed)=>speed>excessiveSpeed;
        public int PaceBand(float speed)=>AboveWarningThreshold(speed)?2:speed>cautionSpeed?1:0;
        bool InsideShower(){var p=head.position;return p.x>22&&p.x<24.5f&&Mathf.Abs(p.z-19.3f)<.85f;}
        void Update()
        {
            if(!lockersExplained&&dressing&&dressing.InChangingRoom&&!PersonalClothesStored){lockersExplained=true;Tell(TrainingGuidance.Lockers);}
            if(!briefed&&ride.State==ElevatorRide.Phase.Descending){briefed=true;hud.gameObject.SetActive(true);foreach(var line in Briefing)speech.Enqueue(line);}
            var delta=head.position-previous;previous=head.position;delta.y=0;
            float instant=Time.deltaTime>0?delta.magnitude/Time.deltaTime:0;
            if(ride.IsTravelling||delta.magnitude>.5f||Quaternion.Angle(previousRotation,ride.playerRoot.rotation)>1)instant=0;
            previousRotation=ride.playerRoot.rotation;
            Speed=Mathf.Lerp(Speed,instant,1-Mathf.Exp(-Time.deltaTime*4));
            bool walking=briefed&&ride.State==ElevatorRide.Phase.Arrived;
            fastFor=walking&&AboveWarningThreshold(Speed)?fastFor+Time.deltaTime:0;
            if(fastFor>2&&AboveWarningThreshold(Speed)&&Time.time>nextWarning&&speech.Count==0&&guide&&guide.Ready&&!guide.IsBusy){var parrot=FindFirstObjectByType<ParrotCompanion>();if(!parrot||!parrot.Speaking){speech.Enqueue("Please walk slowly and keep your movements gentle.");nextWarning=Time.time+20;fastFor=0;}}
            speedText.text=walking?"PACE  "+(Speed>excessiveSpeed?"Too fast":Speed>cautionSpeed?"Slow down":"Good pace"):"Move slowly • Keep clean items off the floor";
            speedText.color=Speed>excessiveSpeed?new Color(.75f,.12f,.08f):Speed>cautionSpeed?new Color(.55f,.32f,0):new Color(.12f,.40f,.24f);
            if(showering){if(!InsideShower()){showering=false;remaining=showerSeconds;speech.Enqueue("The cycle stopped. Return inside and start again.");}else{remaining-=Time.deltaTime;if(remaining<=0){showering=false;ShowerComplete=true;cleanGate.SetActive(false);speech.Enqueue("Air shower complete. Follow the route to the inspection lamp.");}}}
            var station=CleaningStation.Active;
            if(station){var e=station.Exercise;inspected|=e.State!=CleaningExercise.Stage.Inspect;guided|=e.Trial>0||e.State==CleaningExercise.Stage.Review;}
            clipboard.Refresh(new[]{Dressed,ShowerComplete,inspected,guided,station&&station.Exercise.State==CleaningExercise.Stage.Complete},showering,remaining);
            if(speech.Count>0&&guide&&guide.Ready&&!guide.IsBusy){var p=FindFirstObjectByType<ParrotCompanion>();if(!p||!p.Speaking)_=guide.SpeakInstruction(speech.Dequeue());}
            if(showerDisplay)showerDisplay.text=showering?"AIR CYCLE\n"+Mathf.CeilToInt(remaining)+" s":ShowerComplete?"CYCLE COMPLETE\nContinue to cleaning":"AIR SHOWER\nAim + trigger to start";
            if(DesktopPreview.Active){
                if(heldGarment){PreviewPose(heldGarment,Time.deltaTime);if(heldGarment.Equipped||heldGarment.Stored)heldGarment=null;
                    else if(QuestControllerPreview.GripPressed){var released=heldGarment;heldGarment=null;released.SetPreview(false);QuestControllerPreview.Trigger=false;}}
                else if(QuestControllerPreview.GripPressed&&Physics.Raycast(head.position,head.forward,out var grabHit,2)){var g=grabHit.collider.GetComponentInParent<WearableGarment>();if(g&&!g.Equipped&&!g.Stored){heldGarment=g;QuestControllerPreview.Trigger=false;g.SetPreview(true);Tell(g.DisplayName+" selected. In the preview, hold the trigger to bring it to the target, then release with grip.");}}
                if(!heldGarment&&QuestControllerPreview.TriggerPressed&&Physics.Raycast(head.position,head.forward,out var hit,3)){var b=hit.collider.GetComponentInParent<PreparationButton>();if(b)b.Press();var door=hit.collider.GetComponentInParent<LockerDoor>();if(door)door.Toggle();}
            }
        }
        public void PreviewPose(WearableGarment g,float deltaTime)
        {
            var target=QuestControllerPreview.Trigger?g.TargetPosition()+(g.Footwear?Vector3.up*.45f:Vector3.zero):head.position+head.forward*.5f;
            g.transform.position=Vector3.MoveTowards(g.transform.position,target,Mathf.Max(0,deltaTime)*1.8f);
            g.transform.rotation=Quaternion.Euler(0,head.eulerAngles.y,0);
        }
        void LateUpdate(){if(hud){if(briefed)hud.gameObject.SetActive(!dressing||!dressing.InChangingRoom);hud.position=head.TransformPoint(new Vector3(-.72f,.70f,1.35f));hud.rotation=head.rotation;}}
    }
}
