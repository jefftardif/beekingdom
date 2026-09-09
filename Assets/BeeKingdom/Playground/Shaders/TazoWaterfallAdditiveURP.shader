Shader "BeeKingdom/WaterfallFX/ParticleAdditiveURP"
{
    // M073B-CL: URP-compatible replacement for the Tazo_fx "Realistic Waterfall
    // Prefab" package's "Legacy Shaders/Particles/Additive" shader (same pink-
    // under-URP problem as ParticleAlphaBlendedURP - see that shader's header
    // for the full explanation). Additive blend (One One) instead of alpha
    // blend, used by the pack's foam/caustics materials.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // See TazoWaterfallMeshAdditiveURP.shader's header: the pack's
                // additive sprites store their shape in alpha over near-white
                // RGB, so tex.a must mask the additive contribution.
                half3 rgb = tex.rgb * tex.a * _TintColor.rgb * input.color.rgb * _TintColor.a * input.color.a * 2.0;
                return half4(rgb, 0.0);
            }
            ENDHLSL
        }
    }
}
