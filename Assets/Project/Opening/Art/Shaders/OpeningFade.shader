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
 struct Varyings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
 CBUFFER_START(UnityPerMaterial)
 float _Alpha;
 CBUFFER_END
 Varyings vert(Attributes input) {
 Varyings o; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 o.positionCS = TransformObjectToHClip(input.positionOS.xyz); return o;
 }
 half4 frag(Varyings i) : SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return half4(0,0,0,_Alpha); }
 ENDHLSL
 }
 }
}
