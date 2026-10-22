using UnityEngine;
namespace GroupProject.Opening
{
    /// <summary>Mouse-operated Quest control preview. Does not emulate headset tracking or XR hardware.</summary>
    public sealed class QuestControllerPreview : MonoBehaviour
    {
        public static Vector2 Move, Turn, Aim;
        public static bool Trigger, Speak, Lift;
        static int gripFrame=-10, triggerFrame=-10;
        public static bool GripPressed => Time.frameCount==gripFrame+1;
        public static bool TriggerPressed => Time.frameCount==triggerFrame+1;
        int dragging;
        Vector2 anchor;
        public static void ResetControls(){Move=Turn=Aim=Vector2.zero;Trigger=Speak=Lift=false;gripFrame=triggerFrame=-10;}
        void OnEnable(){ResetControls();}
        void OnDisable(){ResetControls();}
        void OnApplicationFocus(bool focus){if(!focus){ResetControls();dragging=0;}}
        void OnGUI()
        {
            if(!DesktopPreview.Active)return;
            float scale=Mathf.Min(Screen.width/1000f,Screen.height/650f);
            var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            GUI.Box(new Rect(12,h-205,225,193),"LEFT QUEST CONTROLLER");
            Pad(new Rect(70,h-165,110,110),1,"Move",ref Move);
            GUI.Box(new Rect(w-237,h-270,225,258),"RIGHT QUEST CONTROLLER");
            Pad(new Rect(w-180,h-230,110,110),2,"Turn",ref Turn);
            if(GUI.Button(new Rect(w-225,h-110,96,32),"Grip"))gripFrame=Time.frameCount;
            bool trigger=GUI.Toggle(new Rect(w-120,h-110,96,32),Trigger,"Trigger");
            if(trigger&&!Trigger)triggerFrame=Time.frameCount;Trigger=trigger;
            Speak=GUI.RepeatButton(new Rect(w-225,h-70,96,32),"Hold B · Ask");
            Lift=GUI.Toggle(new Rect(w-120,h-70,110,32),Lift,"Raise hand");
            Pad(new Rect(w/2-65,h-155,130,100),3,"Head / hand aim",ref Aim);
            GUI.Label(new Rect(w/2-190,h-42,380,30),"QUEST PREVIEW · Drag sticks · Grip · Trigger · B");
            GUI.matrix=old;
        }
        void Pad(Rect rect,int id,string label,ref Vector2 value)
        {
            var e=Event.current;
            GUI.Box(rect,label);
            if(e.type==EventType.MouseDown&&e.button==0&&rect.Contains(e.mousePosition)){dragging=id;anchor=rect.center;e.Use();}
            if(dragging==id){
                if(e.type==EventType.MouseDrag||e.type==EventType.MouseDown){var d=e.mousePosition-anchor;value=Vector2.ClampMagnitude(new Vector2(d.x,-d.y)/(rect.width*.4f),1);e.Use();}
                if(e.rawType==EventType.MouseUp){dragging=0;value=Vector2.zero;}
            }
            var knob=rect.center+new Vector2(value.x,-value.y)*rect.width*.3f;
            GUI.Box(new Rect(knob.x-12,knob.y-12,24,24),"●");
        }
    }
}
