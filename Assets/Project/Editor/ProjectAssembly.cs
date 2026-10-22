using System;
using System.Linq;
using UnityEngine;

// Shared route geometry checks for the combined training scene.
public static class ProjectAssembly
{
    public const string Main="Assets/Project/Scenes/01_Opening.unity";
    public static readonly Vector3[] RoutePoints={new Vector3(-8,.03f,8),new Vector3(0,.03f,8),new Vector3(12,.06f,8),new Vector3(12,.23f,19.3f),new Vector3(23,.25f,19.3f),new Vector3(26.4f,.25f,19.3f),new Vector3(26.4f,.25f,17.9f),new Vector3(28,.25f,17.9f)};
    public static void ValidateRoute()
    {
        Physics.SyncTransforms();
        for(int i=0;i<RoutePoints.Length-1;i++){
            int n=Mathf.CeilToInt(Vector3.Distance(RoutePoints[i],RoutePoints[i+1])/.1f);
            for(int k=0;k<=n;k++){
                var p=Vector3.Lerp(RoutePoints[i],RoutePoints[i+1],k/(float)n);
                if(!Physics.Raycast(p+Vector3.up*.65f,Vector3.down,out var floor,1,~0,QueryTriggerInteraction.Ignore))throw new Exception("Route lacks floor: "+p);
                var foot=new Vector3(p.x,floor.point.y,p.z);
                var hits=Physics.OverlapCapsule(foot+Vector3.up*.36f,foot+Vector3.up*1.6f,.28f,~0,QueryTriggerInteraction.Ignore).Where(c=>c.transform.root.name!="Shared_XR_Player").ToArray();
                if(hits.Length>0)throw new Exception("Route blocked at "+p+": "+string.Join(", ",hits.Select(c=>c.name+" "+c.bounds)));
            }
        }
    }
}
