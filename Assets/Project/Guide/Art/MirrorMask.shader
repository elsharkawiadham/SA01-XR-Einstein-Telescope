Shader "Training/MirrorMask" {
Properties {_BaseColor("Tint",Color)=(.13,.22,.25,.94) _Stencil("Mirror ID",Int)=1}
SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10"} Pass {
ZWrite Off ZTest LEqual Cull Off Blend SrcAlpha OneMinusSrcAlpha
Stencil {Ref [_Stencil] Comp Always Pass Replace}
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A {float4 vertex:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};struct V {float4 position:SV_POSITION;UNITY_VERTEX_OUTPUT_STEREO};
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
CBUFFER_END
V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.position=TransformObjectToHClip(a.vertex.xyz);return o;}
half4 frag(V i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return _BaseColor;}
ENDHLSL
}}}
