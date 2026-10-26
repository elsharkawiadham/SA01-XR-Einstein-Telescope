Shader "Project/Checklist Panel" {
Properties { _MainTex ("Texture", 2D) = "white" {} }
SubShader { Tags { "Queue"="Overlay" "RenderType"="Transparent" } Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
struct appdata {float4 vertex:POSITION; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID};
struct output {float4 pos:SV_POSITION; float4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO};
output vert(appdata v){output o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;return o;}
fixed4 frag(output i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return i.color;}
ENDCG } } }
