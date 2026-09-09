Shader "BeeKingdom/WaterfallFX/ParticleAlphaBlendedURP"
{
    // M073B-CL: URP-compatible replacement for the Tazo_fx "Realistic Waterfall
    // Prefab" package's "Legacy Shaders/Particles/Alpha Blended" shader, which
    // renders pink/magenta under URP (this project's pipeline - the vendor
    // package targets the Built-in Render Pipeline). Same property set
    // (_MainTex, _TintColor) and the same "double" texture*tint*vertexColor*2
    // combine the legacy fixed-function shader used, so materials copied onto
    // this shader look the same as the vendor intended once ported to URP.
    // Assets/Tazo_fx itself is never modified - this shader lives in BeeKingdom
    // and is applied only to copied material instances.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                half3 rgb = tex.rgb * _TintColor.rgb * input.color.rgb * 2.0;
                half alpha = tex.a * _TintColor.a * input.color.a;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
