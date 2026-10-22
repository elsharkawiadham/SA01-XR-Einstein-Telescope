Shader "GroupProject/CleaningSurface"
{
    Properties { _Color ("Surface colour", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // A small fixed directional fill keeps the rehearsal readable on the performance tier,
            // which disables additional lights. No per-frame light or shadow cost.
            half4 _Color;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; half shade:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                float3 normal=TransformObjectToWorldNormal(input.normalOS);
                output.shade=.62h+.38h*saturate(dot(normal,normalize(float3(-.3,1,-.5))));
                return output;
            }
            half4 Frag(Varyings input):SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);return half4(_Color.rgb*input.shade,1); }
            ENDHLSL
        }
    }
}
