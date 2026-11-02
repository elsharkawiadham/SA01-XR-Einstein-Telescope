using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GroupProject.Opening;
using GroupProject.Elevator;
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
        PaceReadout pace;
        public PaceReadout Pace=>pace;
        public bool InDressingRoom=>ride&&ride.destinationRoot&&ride.destinationRoot.activeInHierarchy&&head&&head.position.x>10&&head.position.x<22&&head.position.z>15&&head.position.z<23.5f;
        ChecklistClipboard clipboard;
        Transform hud;
        public GameObject showerGate,cleanGate;
        public AirShowerDoor entryDoor,exitDoor;
        public Transform facilityDoor;
        public TMPro.TMP_Text closeDoorLabel;
        public bool FacilityReached { get; private set; }
        public TMPro.TMP_Text showerDisplay;
        WearableGarment heldGarment;
        DressingRoomExperience dressing;
        public bool HasGarment(WearableGarment.Kind kind)=>garments.Contains((int)kind);
        Vector3 previous;
        Quaternion previousRotation;
        bool briefed,showering,wasInDressingRoom;
        float remaining,fastFor,nextWarning;
        public static readonly string[] Briefing={
            TrainingGuidance.Checklist,
            "Put on your cleanroom clothing before entering the air shower.",
            "Enter the air shower, select Close door, then select Start air shower. Wait until the cycle finishes.",
            "When the exit door opens, follow the hallway to the ET facility room. That room is coming soon and its door stays locked."
        };
        void Start()
        {
            previous=head.position;previousRotation=ride.playerRoot.rotation;
            hud=new GameObject("Training checklist",typeof(RectTransform)).transform;hud.SetParent(transform);
            clipboard=hud.gameObject.AddComponent<ChecklistClipboard>();clipboard.Build();
            pace=new GameObject("Always visible walking pace",typeof(RectTransform)).AddComponent<PaceReadout>();pace.Build(head);
            hud.gameObject.SetActive(false);
            dressing=gameObject.AddComponent<DressingRoomExperience>();dressing.Initialize(this);
        }

        public void Tell(string text){if(!InDressingRoom&&speech.Count<4)speech.Enqueue(text);}
        public void StorePersonalClothes(){PersonalClothesStored=true;Tell("Your personal clothes are stored. Locker one is complete. Continue to locker two for the coverall.");}
        public void Equip(int kind){if(kind<1||kind>11||!PersonalClothesStored)return;if(garments.Add(kind)){Tell(Dressed?"You are dressed. Continue to the air shower.":WearableGarment.NameFor((WearableGarment.Kind)kind)+" fitted. Check your clothing list for the remaining items.");if(entryDoor&&Dressed&&!ShowerComplete&&!showering)entryDoor.SetOpen(true);else if(!entryDoor&&showerGate)showerGate.SetActive(!Dressed);}}
        public string Answer(string question)
        {
            var q=question.ToLowerInvariant();
            if(q.Contains("checklist")||q.Contains("check list")||q.Contains("task list")||q.Contains("to do list")||q.Contains("todo list")||q.Contains("outfit list")||q.Contains("clothing list")||q.Contains("dressing list")){
                if(clipboard&&(q.Contains("hide")||q.Contains("close")||q.Contains("dismiss"))){if(dressing&&dressing.InChangingRoom)dressing.SetChecklistVisible(false);else clipboard.SetVisible(false);return "Checklist hidden. Say show the checklist to bring it back.";}
                if(clipboard&&(q.Contains("show")||q.Contains("open")||q.Contains("display"))){if(dressing&&dressing.InChangingRoom)dressing.SetChecklistVisible(true);else{hud.gameObject.SetActive(true);clipboard.SetVisible(true);}return "Here is your checklist.";}
            }
            if(!ride.destinationRoot.activeInHierarchy)return null;
            if(ShowerComplete&&(q.Contains("back")||q.Contains("return")||q.Contains("dressing room")))return "Walk back through the open shower exit, then select OPEN ENTRY DOOR inside the shower to return to the dressing room. Your completed tasks stay saved.";
            if(dressing&&dressing.InChangingRoom){
                if(dressing.Held&&(q.Contains("help")||q.Contains("how")||q.Contains("where")))return dressing.Held.DisplayName+": "+dressing.Held.Hint;
                if(q.Contains("boot")||q.Contains("shoe")||q.Contains("footwear"))return "Footwear is in the right-hand locker. Grip a boot and release it above the marked foot space. Both boots have separate checkmarks. Put the overshoes on afterwards.";
                if(q.Contains("cap")||q.Contains("hood")||q.Contains("mask")||q.Contains("glass"))return "The headwear is in the hood and mask locker: hair cap above the hood, mask below it, and glasses on the lower shelf. Grip one item, bring it near your head and release when the target says ready.";
            }
            if(q.Contains("speed")||q.Contains("too fast")||q.Contains("slow"))return "Move the left joystick gently. Keep the pace indicator green. The coloured thresholds are demonstration settings.";
            if(q.Contains("cloth")||q.Contains("dress")||q.Contains("glove"))return "Follow lockers one to five from left to right. Open locker one and select Store clothes to transfer your personal outfit into it. Each task panel turns green when complete and points to the next locker. Grip a cleanroom item and follow its highlighted fitting target: coverall at the chest, headwear near the head, gloves at the opposite hand, and footwear above the foot markers.";
            if(q.Contains("shower"))return "Put on all garments, step inside the air shower, then aim at CLOSE DOOR and press the trigger. Wait until the door is fully closed before selecting START AIR SHOWER. The exit door opens after the countdown.";
            if(ShowerComplete&&(q.Contains("next")||q.Contains("where")||q.Contains("facility")||q.Contains("cleaning")||q.Contains("help")))return "Follow the hallway through the open air shower exit to the door labelled ET facility room (coming soon). This is the end of the current experience. The facility door stays locked.";
            if(!ShowerComplete&&head.position.x<24.6f&&(q.Contains("help")||q.Contains("next")||q.Contains("what do")||q.Contains("where do")))return Dressed?"Enter the air shower, select Close door, then select Start air shower. Stay inside until the cycle ends.":"Follow the floor arrows to the changing room. Open locker one and select Store clothes. Then follow the numbered locker panels to fit your cleanroom clothing.";
            return null;
        }
        public bool Select(int id)
        {
            if(!ride.destinationRoot.activeInHierarchy||(id!=4&&id!=5))return false;
            if(!Dressed){Tell("Store your jacket and put on all the cleanroom garments first.");return false;}
            if(id==5){
                if(!entryDoor||!exitDoor)return false;
                if(!InsideShower()&&(!ShowerComplete||Vector3.Distance(head.position,new Vector3(23.25f,head.position.y,19.3f))>3)){Tell("Approach the air shower door control.");return false;}
                if(!entryDoor.Open){showering=false;remaining=showerSeconds;entryDoor.SetOpen(true);if(!ShowerComplete)exitDoor.SetOpen(false);Tell(ShowerComplete?"Entry door opening. You can return to the dressing room.":"Entry door opening. Close it again before starting the shower.");}
                else {if(head.position.x<22.3f||head.position.x>24.2f){Tell("Step fully inside, clear of both doorways, before closing the door.");return false;}entryDoor.SetOpen(false);if(!ShowerComplete)exitDoor.SetOpen(false);Tell(ShowerComplete?"Entry door closing.":"Entry door closing. Wait until it is fully closed, then start the air shower.");}
                return true;
            }
            if(ShowerComplete)return false;
            if(!InsideShower()){Tell("Step inside the air shower before starting.");return false;}
            if(showering)return false;
            if(!entryDoor||!entryDoor.IsClosed||!exitDoor||!exitDoor.IsClosed){Tell("Select Close door and wait until both doors are fully closed before starting.");return false;}
            showering=true;remaining=showerSeconds;Tell("Air shower started. Stay inside until the cycle finishes.");return true;
        }
        public bool AboveWarningThreshold(float speed)=>speed>excessiveSpeed;
        public int PaceBand(float speed)=>AboveWarningThreshold(speed)?2:speed>cautionSpeed?1:0;
        bool InsideShower(){var p=head.position;return p.x>22&&p.x<24.5f&&Mathf.Abs(p.z-19.3f)<.85f;}
        void Update()
        {
            if(InDressingRoom){speech.Clear();if(!wasInDressingRoom&&guide)guide.CancelSpeech();}
            wasInDressingRoom=InDressingRoom;
            if(!briefed&&ride.State==ElevatorRide.Phase.Descending){briefed=true;hud.gameObject.SetActive(true);foreach(var line in Briefing)speech.Enqueue(line);}
            var delta=head.position-previous;previous=head.position;delta.y=0;
            float instant=Time.deltaTime>0?delta.magnitude/Time.deltaTime:0;
            if(ride.IsTravelling||delta.magnitude>.5f||Quaternion.Angle(previousRotation,ride.playerRoot.rotation)>1)instant=0;
            previousRotation=ride.playerRoot.rotation;
            Speed=Mathf.Lerp(Speed,instant,1-Mathf.Exp(-Time.deltaTime*4));
            bool walking=briefed&&ride.State==ElevatorRide.Phase.Arrived;
            fastFor=walking&&AboveWarningThreshold(Speed)?fastFor+Time.deltaTime:0;
            if(!InDressingRoom&&fastFor>2&&AboveWarningThreshold(Speed)&&Time.time>nextWarning&&speech.Count==0&&guide&&guide.Ready&&!guide.IsBusy){var parrot=FindFirstObjectByType<ParrotCompanion>();if(!parrot||!parrot.Speaking){speech.Enqueue("Please walk slowly and keep your movements gentle.");nextWarning=Time.time+20;fastFor=0;}}
            pace.Refresh(Speed,PaceBand(Speed),ride.IsTravelling);
            if(showering){if(!InsideShower()||!entryDoor.IsClosed||!exitDoor.IsClosed){showering=false;remaining=showerSeconds;entryDoor.SetOpen(true);speech.Enqueue("The cycle stopped. Close the entry door and start again from inside.");}else{remaining-=Time.deltaTime;if(remaining<=0){showering=false;ShowerComplete=true;exitDoor.SetOpen(true);speech.Enqueue("Air shower complete. The exit door is opening. Follow the hallway to the ET facility room door. That room is coming soon.");}}}
            if(ShowerComplete&&!FacilityReached&&facilityDoor&&Vector2.Distance(new Vector2(head.position.x,head.position.z),new Vector2(facilityDoor.position.x,facilityDoor.position.z))<2.6f){FacilityReached=true;Tell("You have completed the preparation journey. ET facility room is coming soon. This door remains closed.");}
            clipboard.Refresh(new[]{Dressed,ShowerComplete,FacilityReached},showering,remaining);
            if(!InDressingRoom&&speech.Count>0&&guide&&guide.Ready&&!guide.IsBusy){var p=FindFirstObjectByType<ParrotCompanion>();if(!p||!p.Speaking)_=guide.SpeakInstruction(speech.Dequeue());}
            if(showerDisplay)showerDisplay.text=showering?"AIR CYCLE\n"+Mathf.CeilToInt(remaining)+" s":ShowerComplete?"CYCLE COMPLETE\nExit door opening":entryDoor&&entryDoor.IsClosed?"DOORS CLOSED\nSelect START AIR SHOWER":"AIR SHOWER\nSelect CLOSE DOOR first";
            if(closeDoorLabel)closeDoorLabel.text=entryDoor&&!entryDoor.Open?(ShowerComplete?"OPEN ENTRY DOOR\nReturn to dressing":"OPEN DOOR\nCancel / return"):"CLOSE DOOR\nAim + trigger";
            if(DesktopPreview.Active){
                if(heldGarment){PreviewPose(heldGarment,Time.deltaTime);if(heldGarment.Equipped||heldGarment.Stored)heldGarment=null;
                    else if(QuestControllerPreview.GripPressed){var released=heldGarment;heldGarment=null;released.SetPreview(false);QuestControllerPreview.Trigger=false;}}
                else if(QuestControllerPreview.GripPressed&&Physics.Raycast(head.position,head.forward,out var grabHit,2)){var g=grabHit.collider.GetComponentInParent<WearableGarment>();if(g&&!g.Equipped&&!g.Stored){heldGarment=g;QuestControllerPreview.Trigger=false;g.SetPreview(true);Tell(g.DisplayName+" selected. In the preview, hold the trigger to bring it to the target, then release with grip.");}}
                if(!heldGarment&&QuestControllerPreview.TriggerPressed&&Physics.Raycast(head.position,head.forward,out var hit,3)){var toggle=hit.collider.GetComponentInParent<ChecklistToggleButton>();if(toggle)toggle.Press();var b=hit.collider.GetComponentInParent<PreparationButton>();if(b)b.Press();var door=hit.collider.GetComponentInParent<LockerDoor>();if(door)door.Toggle();}
            }
        }
        public void PreviewPose(WearableGarment g,float deltaTime)
        {
            var target=QuestControllerPreview.Trigger?g.TargetPosition()+(g.Footwear?Vector3.up*.45f:Vector3.zero):head.position+head.forward*.5f;
            g.transform.position=Vector3.MoveTowards(g.transform.position,target,Mathf.Max(0,deltaTime)*1.8f);
            g.transform.rotation=Quaternion.Euler(0,head.eulerAngles.y,0);
        }
        void LateUpdate(){if(hud){if(briefed)hud.gameObject.SetActive(!dressing||!dressing.InChangingRoom);ChecklistPlacement.Place(hud,head,460,420,40);}}
    }
}
