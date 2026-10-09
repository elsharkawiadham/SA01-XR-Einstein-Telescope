using UnityEngine;
using UnityEngine.UI;
namespace GroupProject.Guide
{
    /// <summary>Lightweight textured UI mesh: flap the feather regions while retaining the central body.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ParrotGraphic : MaskableGraphic
    {
        public Texture texture;
        public float speech;
        public override Texture mainTexture => texture ? texture : Texture2D.whiteTexture;
        void Update() { SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=GetPixelAdjustedRect();const int columns=40,rows=24;
            float flap=Mathf.Sin(Time.unscaledTime*9);
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float u=x/(float)columns,v=y/(float)rows;
                float wing=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(u-.5f)-.12f)/.34f));
                var point=new Vector3(rect.x+rect.width*(u-.5f*wing*(u>.5f?1:-1)*(1-flap)*.16f),rect.y+rect.height*(v+flap*wing*.12f),0);
                // Subtle audio-driven beak/jaw motion; this is a 2D puppet, not a skeletal model.
                float jaw=Mathf.Clamp01(1-Mathf.Abs(u-.55f)/.055f)*Mathf.Clamp01(1-Mathf.Abs(v-.83f)/.065f);
                point.y-=jaw*speech*rect.height*.018f;
                mesh.AddVert(point,color,new Vector2(u,v));
            }
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++) {int a=y*(columns+1)+x;mesh.AddTriangle(a,a+columns+1,a+1);mesh.AddTriangle(a+1,a+columns+1,a+columns+2);}
        }
    }
}
