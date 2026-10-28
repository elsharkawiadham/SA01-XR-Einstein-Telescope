Shader "Training/AvatarMirror" {
Properties { _BaseColor("Colour",Color)=(1,1,1,1) _BaseMap("Texture",2D)="white" {} _Stencil("Mirror ID",Int)=1 }
SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent"} Pass {
ZWrite On ZTest LEqual Cull Front
Stencil { Ref [_Stencil] Comp Equal Pass Keep }
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;float4 _BaseMap_ST;
CBUFFER_END
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.position=TransformObjectToHClip(a.vertex.xyz);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.normal=TransformObjectToWorldNormal(a.normal);return o;}
struct F {half4 color:SV_Target;float depth:SV_Depth;};
F frag(V i) {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);F o;half shade=.65+.35*abs(dot(normalize(i.normal),normalize(float3(.3,1,.4))));o.color=half4(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb*shade,1);
// The aperture already tests scene occlusion. Reserve a near-depth slice for correct avatar self-occlusion behind the wall.
#if UNITY_REVERSED_Z
 o.depth=.999+.001*i.position.z;
#else
 o.depth=.001*i.position.z;
#endif
return o;}

ENDHLSL
}}}
