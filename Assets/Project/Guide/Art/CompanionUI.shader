Shader "GroupProject/CompanionUI"
{
 Properties { _MainTex ("Font atlas", 2D) = "white" {} }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay-1" "RenderType"="Transparent" }
 Pass {
 Blend SrcAlpha OneMinusSrcAlpha
 ZWrite Off ZTest Always Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
 struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; UNITY_VERTEX_OUTPUT_STEREO };
 Varyings vert(Attributes v) { Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;return o; }
 half4 frag(Varyings i):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return i.color*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv); }
 ENDHLSL
 }
 }
}
