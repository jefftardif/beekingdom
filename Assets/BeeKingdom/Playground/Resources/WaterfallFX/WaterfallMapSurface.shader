Shader "BeeKingdom/WaterfallFX/MapSurface"
{
    Properties
    {
        _MainTex ("Purchased waterfall texture", 2D) = "white" {}
        _FoamTex ("Purchased foam texture", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 0.82
        _Flow ("Flow XY, water and foam speeds ZW", Vector) = (0, 1, 0.24, 0.31)
        _Feather ("Left right foot lip feather", Vector) = (0.065, 0.065, 0.15, 0.12)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        // One non-overlapping surface writes straight RGBA. IMGUI applies the
        // alpha exactly once when compositing the capture onto the painted map.
        Blend One Zero
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ WATERFALL_CUSTOM_FLOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_FoamTex); SAMPLER(sampler_FoamTex);
            CBUFFER_START(UnityPerMaterial)
                float _Opacity;
                float4 _Flow;
                float4 _Feather;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                // Positive V offset advects details downward (V=1 at the lip).
                #if defined(WATERFALL_CUSTOM_FLOW)
                half4 water = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x * 2.6, uv.y * 0.85) + _Time.y * _Flow.z * _Flow.xy);
                half4 foam = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, float2(uv.x * 3.1 + 0.23, uv.y) + _Time.y * _Flow.w * _Flow.xy);
                #else
                half4 water = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, float2(uv.x * 2.6, uv.y * 0.85 + _Time.y * 0.24));
                half4 foam = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, float2(uv.x * 3.1 + 0.23, uv.y + _Time.y * 0.31));
                #endif
                half luminance = dot(water.rgb, half3(0.2126, 0.7152, 0.0722));
                half detail = smoothstep(0.16, 0.65, luminance);
                #if defined(WATERFALL_CUSTOM_FLOW)
                half edge = smoothstep(0, _Feather.x, uv.x) * smoothstep(0, _Feather.y, 1 - uv.x);
                edge *= smoothstep(0, _Feather.z, uv.y) * smoothstep(0, _Feather.w, 1 - uv.y);
                #else
                half edge = smoothstep(0, 0.065, uv.x) * smoothstep(0, 0.065, 1 - uv.x);
                edge *= smoothstep(0, 0.15, uv.y) * smoothstep(0, 0.12, 1 - uv.y);
                #endif
                half alpha = edge * _Opacity * saturate(0.25 + detail * 0.65 + foam.a * 0.25);
                half3 rgb = lerp(half3(0.28, 0.37, 0.43), half3(0.95, 0.97, 1.0), saturate(detail + foam.a * 0.25));
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
