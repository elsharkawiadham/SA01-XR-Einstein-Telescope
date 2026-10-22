Shader "GroupProject/OpeningFade"
{
 Properties { _Alpha ("Opacity", Range(0,1)) = 1 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
 Pass {
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off ZTest Always Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
 struct Varyings { float4 positionCS : SV_POSITION; float4 screenPos : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
 CBUFFER_START(UnityPerMaterial)
 float _Alpha;
 CBUFFER_END
 Varyings vert(Attributes input) {
 Varyings o; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 o.positionCS = TransformObjectToHClip(input.positionOS.xyz); o.screenPos=ComputeScreenPos(o.positionCS); return o;
 }
 half4 frag(Varyings i) : SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); if(_Alpha>.999)return half4(0,0,0,1);
 float2 uv=i.screenPos.xy/i.screenPos.w;
 float opening=1-_Alpha;
 float edge=abs(uv.y-.5) + .12*pow(abs(uv.x-.5)*2,2);
 float lid=smoothstep(opening*.72-.025,opening*.72+.025,edge);
 return half4(0,0,0,max(lid,_Alpha*.25)); }
 ENDHLSL
 }
 }
}
