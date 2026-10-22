using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using GroupProject.Opening;
using GroupProject.Guide;
namespace GroupProject.Cleaning
{
    public sealed class CleaningStation : MonoBehaviour
    {
        [Serializable] public struct SpokenLine { public AudioClip clip; [TextArea] public string text; }
        public Transform plate;
        public TMP_Text heading, status, progress;
        public GameObject[] particles, lanes;
        public CleaningTool[] tools;
        public SpokenLine[] speech;
        public AudioSource narrator;
        public CleaningExercise Exercise { get; private set; } = new CleaningExercise();
        public static CleaningStation Active { get; private set; }
        CleaningTool held, desktopTool;
        Camera head;ParrotCompanion parrot;
        float inspection, lastFeedbackTime;
        string shown;
        bool registered, near;
        public bool InReach => head && Vector3.Distance(head.transform.position,plate.position)<2.8f;
        void Update()
        {
            if(!head)head=Camera.main;
            if(!head)return;
            near=InReach;
            if(!near && desktopTool){var dropped=desktopTool;desktopTool=null;dropped.SetDesktop(false);dropped.ReturnHome();}
            if(!near && narrator.isPlaying)narrator.Stop();
            if(near)Active=this;else if(Active==this)Active=null;
            if(!registered)
            {
                parrot=FindFirstObjectByType<ParrotCompanion>();
                if(parrot){var lines=new ParrotCompanion.Line[speech.Length];for(int i=0;i<speech.Length;i++)lines[i]=new ParrotCompanion.Line{clip=speech[i].clip,text=speech[i].text};parrot.RegisterNarrator(narrator,lines);registered=true;}
            }
            if(DesktopPreview.Active && near)DesktopInput();
            if(held && held.Held && near)
            {
                if(held.kind==CleaningTool.Kind.Lamp)
                {
                    var ray=new Ray(held.tip.position,held.tip.forward);
                    var plane=new Plane(plate.up,plate.position);
                    bool onPlate=false;
                    if(plane.Raycast(ray,out float distance) && distance<1.5f)
                    {var p=plate.InverseTransformPoint(ray.GetPoint(distance));onPlate=Mathf.Abs(p.x)<.27f && Mathf.Abs(p.z)<.22f;}
                    inspection=held.LampOn && onPlate?inspection+Time.deltaTime:0;
                    if(inspection>1.2f)Exercise.Inspected();
                }
                else if(Exercise.State==CleaningExercise.Stage.Wipe)
                {
                    var p=plate.InverseTransformPoint(held.tip.position);
                    bool contact=Mathf.Abs(p.y)<.035f && Mathf.Abs(p.x)<.29f && Mathf.Abs(p.z)<.23f;
                    Exercise.Sample(contact,contact || p.y>.035f ? new Vector2(p.x,p.z) : Vector2.zero);
                }
            }
            else Exercise.Sample(false,Vector2.zero);
            Refresh();
        }
        public void Pick(CleaningTool tool)
        {
            if(!InReach)return;
            if(held!=tool)Exercise.Sample(false,Vector2.zero);
            held=tool;
            if(tool.kind!=CleaningTool.Kind.Lamp && !Exercise.Select(tool.kind==CleaningTool.Kind.AnticonGold?CleaningExercise.WipeKind.AnticonGold:CleaningExercise.WipeKind.KimtechW4))held=null;
        }
        public void Release(CleaningTool tool){if(held==tool){Exercise.Sample(false,Vector2.zero);held=null;}}
        public void Flip(CleaningTool tool){if(held==tool)Exercise.FreshFace();}
        public void Continue()
        {
            if(!InReach || Exercise.State!=CleaningExercise.Stage.Review)return;
            Exercise.Continue();held=null;
            if(desktopTool){desktopTool.SetDesktop(false);desktopTool=null;}
            foreach(var tool in tools)tool.ReturnHome();
            inspection=0;
        }
        void Refresh()
        {
            heading.text="SURFACE CARE  /  CLASS "+Exercise.PartClass;
            status.text=Exercise.Feedback;
            progress.text=Exercise.State==CleaningExercise.Stage.Complete?"PRACTICE COMPLETE":$"{(Exercise.Guided?"GUIDED":"INDEPENDENT")}  /  PASSES {Exercise.Passes}/4\nGrip: pick up / release   |   Trigger: lamp / fresh face";
            for(int i=0;i<particles.Length;i++)particles[i].SetActive(Exercise.State!=CleaningExercise.Stage.Inspect && i/6>=Exercise.Passes);
            for(int i=0;i<lanes.Length;i++)lanes[i].SetActive(Exercise.Guided && Exercise.State==CleaningExercise.Stage.Wipe && i==Exercise.Passes);
            if(!near || shown==Exercise.Feedback || Time.unscaledTime-lastFeedbackTime<2)return;
            if(parrot && parrot.Speaking && (!narrator.isPlaying || parrot.voice.clip!=narrator.clip))return;
            shown=Exercise.Feedback;lastFeedbackTime=Time.unscaledTime;
            if(parrot && parrot.ai && parrot.ai.feedbackText)parrot.ai.feedbackText.text=shown;
            foreach(var line in speech)if(line.text==shown){narrator.clip=line.clip;narrator.Play();break;}
        }
        void DesktopInput()
        {

            if(QuestControllerPreview.GripPressed)
            {
                if(desktopTool){desktopTool.SetDesktop(false);desktopTool=null;}
                else if(Physics.Raycast(head.ViewportPointToRay(new Vector3(.5f,.5f)),out var hit,3))
                {var tool=hit.collider.GetComponentInParent<CleaningTool>();if(tool && tool.station==this){desktopTool=tool;tool.SetDesktop(true);}}
            }
            if(desktopTool)
            {
                var ray=head.ViewportPointToRay(new Vector3(.5f,.5f));
                if(desktopTool.kind==CleaningTool.Kind.Lamp)
                {desktopTool.transform.SetPositionAndRotation(head.transform.TransformPoint(new Vector3(.2f,-.16f,.35f)),head.transform.rotation);desktopTool.SetLamp(QuestControllerPreview.Trigger);}
                else
                {
                    var surface=new Plane(plate.up,plate.position);
                    if(surface.Raycast(ray,out float distance) && distance<3)
                    {
                        desktopTool.transform.rotation=plate.rotation;
                        var desired=ray.GetPoint(distance)+plate.up*(QuestControllerPreview.Lift?.16f:0);
                        desktopTool.transform.position+=desired-desktopTool.tip.position;
                    }
                    else desktopTool.transform.position=head.transform.TransformPoint(new Vector3(.2f,-.2f,.5f));
                    if(QuestControllerPreview.TriggerPressed)desktopTool.Activate();
                }
            }
            if(QuestControllerPreview.TriggerPressed && !desktopTool && Physics.Raycast(head.ViewportPointToRay(new Vector3(.5f,.5f)),out var buttonHit,3) && buttonHit.collider.GetComponent<CleaningContinueButton>())Continue();
        }
        void OnDisable(){if(Active==this)Active=null;}
        string LocateTool(CleaningTool.Kind kind)
        {
            var tool=Array.Find(tools,t=>t && t.kind==kind);
            string name=kind==CleaningTool.Kind.Lamp?"inspection lamp":kind==CleaningTool.Kind.AnticonGold?"Anticon Gold wipe":"KIMTECH W4 wipe";
            if(tool && tool.Held)return "You are already holding the "+name+".";
            if(tool && plate){
                Vector3 p=plate.InverseTransformPoint(tool.transform.position);
                if(Mathf.Abs(p.x)>1.3f || Mathf.Abs(p.z)>.8f || p.y<-.3f)return "The "+name+" has moved away from its labelled place. Look around the bench and on the floor nearby.";
            }
            return kind==CleaningTool.Kind.Lamp?"Look down at the far left of the workbench, while facing the instructions. The purple tool beneath INSPECTION is the lamp. Aim at it and use the grip to pick it up.":
                kind==CleaningTool.Kind.AnticonGold?"The Anticon Gold wipe is to the left of the central tray, beside the lamp, beneath the gold ANTICON GOLD label.":
                "The KIMTECH W4 wipe is to the right of the central tray, beneath the teal KIMTECH W4 label.";
        }
        public string HelpForCurrentStep()
        {
            switch(Exercise.State){
                case CleaningExercise.Stage.Inspect: return "Let's inspect it together. Look down at the purple lamp on the far left of the bench. Aim at it and press grip to pick it up. Hold the trigger and point the lamp at the plate in the centre tray for just over a second.";
                case CleaningExercise.Stage.ChooseWipe: return LocateTool(Exercise.Guided?CleaningTool.Kind.AnticonGold:CleaningTool.Kind.KimtechW4)+" Aim at that wipe and use grip to pick it up.";
                case CleaningExercise.Stage.Wipe: return Exercise.NeedsFreshFace?"Lift the wipe clear of the plate, then press the trigger to expose a fresh face. Lower it at the left end of the next pass.":"Place the wipe at the left edge of the plate. Pull it steadily to the right, then lift it clear. Avoid scrubbing back and forth. After each pass, press the trigger while lifted for a fresh face.";
                case CleaningExercise.Stage.Review: return "The four passes are finished. Find CONTINUE at the far right of the bench, aim at it, and press the trigger.";
                default:return "You have finished both practice trials. You can ask about the wipe choices or return along the corridor to the elevator landing.";
            }
        }
        public string Answer(string question)
        {
            string q=question.ToLowerInvariant();
            string social=StandaloneAIGuide.SocialReply(question);if(social!=null)return social;
            bool location=q.Contains("where") || q.Contains("find") || q.Contains("locate") || q.Contains("see it");
            if(location)
            {
                if(q.Contains("lamp") || q.Contains("light") || q.Contains("torch"))return LocateTool(CleaningTool.Kind.Lamp);
                if(q.Contains("anticon") || q.Contains("gold"))return LocateTool(CleaningTool.Kind.AnticonGold);
                if(q.Contains("kimtech") || q.Contains("w4"))return LocateTool(CleaningTool.Kind.KimtechW4);
                if(q.Contains("wipe"))return LocateTool(Exercise.Guided?CleaningTool.Kind.AnticonGold:CleaningTool.Kind.KimtechW4);
                if(q.Contains("plate"))return "The training plate is in the dark rectangular tray in the centre of the workbench, below the instructions.";
                if(q.Contains("continue") || q.Contains("button"))return "The CONTINUE button is on the far right of the workbench. Aim at it and press the trigger.";
                if(q.Contains("it") || q.Contains("that") || q.Contains("one")){
                    if(Exercise.State==CleaningExercise.Stage.Inspect)return LocateTool(CleaningTool.Kind.Lamp);
                    if(Exercise.State==CleaningExercise.Stage.ChooseWipe || Exercise.State==CleaningExercise.Stage.Wipe)return LocateTool(Exercise.Guided?CleaningTool.Kind.AnticonGold:CleaningTool.Kind.KimtechW4);
                    return "Do you mean the plate, a wipe, or the CONTINUE button?";
                }
                if(!q.Contains("elevator"))return "Which item are you looking for: the inspection lamp, the plate, or a wipe?";
            }
            if(q.Contains("help") || q.Contains("stuck") || q.Contains("show me") || q.Contains("guide me") || q.Contains("how do i do") || q.Contains("how to do") || q.Contains("don t understand") || q.Contains("don't understand"))return HelpForCurrentStep();
            if(q.Contains("joystick")||q.Contains("walk")||q.Contains("turn")||q.Contains("move forward")||q.Contains("elevator"))return null;
            if(q.Contains("solvent")||q.Contains("alcohol")||q.Contains("oven")||q.Contains("bath")||q.Contains("bake")||q.Contains("temperature"))return "This exercise does not cover chemical, ultrasonic or oven operation. Ask trained staff for the approved local procedure.";
            if(q.Contains("finger")||q.Contains("oil")||q.Contains("clean enough"))return "Fingerprints and skin oils are molecular contamination. They may remain invisible after wiping. This exercise does not certify that a part is clean.";
            if(q.Contains("lamp")||q.Contains("inspect"))return "Grip the inspection lamp, hold the trigger, and aim at the training plate for just over one second. The highlighted particles are demonstration visuals, not a cleanliness measurement.";
            if(q.Contains("fold")||q.Contains("face")||q.Contains("flip"))return "Lift the wipe, then press the trigger to expose a fresh face. After the first pass flip, after the second refold, and after the third flip again.";
            if(q.Contains("pass")||q.Contains("scrub")||(q.Contains("how")&&q.Contains("wipe")))return "Make four parallel, slightly overlapping passes from left to right. Lift at the end of each pass, then use a fresh face. Do not scrub back and forth.";
            if(q.Contains("wipe")||q.Contains("class")||q.Contains("kimtech")||q.Contains("anticon"))return "For Class A, use Anticon Gold polyester. KIMTECH W4 polypropylene is for Class B and below, not Class A. Anticon Gold is not recommended for Class B.";
            if(q.Contains("what should i do")||q.Contains("what do i do")||q.Contains("next step")||q.Contains("next task")||q.Contains("my progress")||q.Contains("how am i doing")||q=="help")return Exercise.Feedback;
            return null;
        }
    }
}
