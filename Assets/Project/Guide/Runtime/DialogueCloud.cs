using UnityEngine;
using UnityEngine.UI;
namespace GroupProject.Guide
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DialogueCloud : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();
            Ellipse(mesh,new Vector2(r.center.x,r.center.y),new Vector2(r.width*.49f,r.height*.43f));
            Ellipse(mesh,new Vector2(r.center.x-r.width*.26f,r.center.y+r.height*.17f),new Vector2(r.width*.21f,r.height*.38f));
            Ellipse(mesh,new Vector2(r.center.x+r.width*.06f,r.center.y+r.height*.23f),new Vector2(r.width*.28f,r.height*.37f));
            int n=mesh.currentVertCount;mesh.AddVert(new Vector3(-24,r.y+25,0),color,Vector2.zero);mesh.AddVert(new Vector3(24,r.y+25,0),color,Vector2.zero);mesh.AddVert(new Vector3(0,r.y-32,0),color,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);
        }
        void Ellipse(VertexHelper mesh,Vector2 c,Vector2 radius)
        {
            int first=mesh.currentVertCount;mesh.AddVert(c,color,Vector2.zero);
            for(int i=0;i<=40;i++){float a=i*Mathf.PI*2/40;mesh.AddVert(c+new Vector2(Mathf.Cos(a)*radius.x,Mathf.Sin(a)*radius.y),color,Vector2.zero);if(i>0)mesh.AddTriangle(first,first+i,first+i+1);}
        }
    }
}
