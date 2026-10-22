using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class GarmentModels
{
    const string Folder="Assets/Project/Preparation/Art/";
    static List<Vector3> v;static List<int> t;static List<Vector2> uv;
    static void Begin(){v=new List<Vector3>();t=new List<int>();uv=new List<Vector2>();}
    static void Tube(Vector3 a,Vector3 b,float rx,float rz,float endScale=1,int rings=10)
    {
        var axis=(b-a).normalized;var x=Vector3.Cross(axis,Mathf.Abs(axis.y)>.8f?Vector3.forward:Vector3.up).normalized;var z=Vector3.Cross(x,axis);int start=v.Count,n=20;
        for(int j=0;j<=rings;j++){float f=j/(float)rings;float taper=Mathf.Lerp(1,endScale,f)*(1+.025f*Mathf.Sin(f*37));for(int i=0;i<n;i++){float angle=i*Mathf.PI*2/n;v.Add(Vector3.Lerp(a,b,f)+(x*Mathf.Cos(angle)*rx+z*Mathf.Sin(angle)*rz)*taper);uv.Add(new Vector2(i/(float)n,f));}}
        for(int j=0;j<rings;j++)for(int i=0;i<n;i++){int aa=start+j*n+i,bb=start+j*n+(i+1)%n,cc=aa+n,dd=bb+n;t.AddRange(new[]{aa,cc,bb,bb,cc,dd});}
        int ca=v.Count;v.Add(a);uv.Add(Vector2.zero);int cb=v.Count;v.Add(b);uv.Add(Vector2.one);
        for(int i=0;i<n;i++){t.AddRange(new[]{ca,start+i,start+(i+1)%n,cb,start+rings*n+(i+1)%n,start+rings*n+i});}
    }
    static Mesh Save(string name){var m=new Mesh{name=name};m.SetVertices(v);m.SetTriangles(t,0);m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();string path=Folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(m,existing);Object.DestroyImmediate(m);return existing;}AssetDatabase.CreateAsset(m,path);return m;}
    public static Mesh Make(string type)
    {
        Begin();
        if(type=="Coverall"||type=="PersonalJacket"){
            Tube(new Vector3(0,-.12f,0),new Vector3(0,.36f,0),.22f,.115f,.92f);
            Tube(new Vector3(0,.32f,0),new Vector3(0,.45f,0),.09f,.075f,1.12f,4);
            foreach(float side in new[]{-1f,1f}){
                Tube(new Vector3(side*.18f,.31f,0),new Vector3(side*.36f,-.12f,.01f),.085f,.08f,.65f);
                if(type=="Coverall")Tube(new Vector3(side*.11f,-.1f,0),new Vector3(side*.13f,-.85f,0),.105f,.105f,.65f);
            }
        }
        else if(type=="Boot"){
            Tube(new Vector3(0,-.12f,-.06f),new Vector3(0,-.02f,-.06f),.085f,.16f,.95f,4);
            Tube(new Vector3(0,-.03f,.01f),new Vector3(0,.22f,.01f),.078f,.09f,.9f,8);
        }
        else if(type=="Glove"){
            Tube(new Vector3(0,-.065f,0),new Vector3(0,.035f,0),.045f,.019f,.9f,6);
            for(int i=0;i<4;i++)Tube(new Vector3(-.03f+i*.02f,.025f,0),new Vector3(-.033f+i*.022f,.09f+(i==1?.015f:0),-.012f),.009f,.01f,.55f,6);
            Tube(new Vector3(-.039f,-.025f,0),new Vector3(-.073f,.025f,-.015f),.012f,.012f,.6f,6);
        }
        else if(type=="Hood"){
            int n=32,rings=15;
            for(int j=0;j<=rings;j++){float f=j/(float)rings;float size=Mathf.Cos(f*Mathf.PI*.48f);for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;v.Add(new Vector3(Mathf.Cos(a)*.14f*size,Mathf.Sin(a)*.19f*size-.015f,-.09f+f*.24f));uv.Add(new Vector2(i/(float)n,f));}}
            for(int j=0;j<rings;j++)for(int i=0;i<n;i++){int a=j*n+i,b=j*n+(i+1)%n;t.AddRange(new[]{a,b,a+n,b,b+n,a+n});}
            Tube(new Vector3(0,-.2f,.06f),new Vector3(0,-.1f,.06f),.17f,.1f,.8f,4);
        }
        else if(type=="Mask"){
            for(int i=0;i<5;i++)Tube(new Vector3(-.075f,-.04f+i*.018f,0),new Vector3(.075f,-.04f+i*.018f,0),.014f,.018f,1,8);
        }
        return Save(type);
    }
}
