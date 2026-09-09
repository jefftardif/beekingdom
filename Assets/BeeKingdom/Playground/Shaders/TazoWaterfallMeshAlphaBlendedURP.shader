Shader "BeeKingdom/WaterfallFX/MeshAlphaBlendedURP"
{
    // M073B-CL: mesh-only variant of TazoWaterfallAlphaBlendedURP. That shader
    // multiplies by vertex color (COLOR0) to replicate the legacy particle
    // shader's per-particle tint/fade - correct for the pack's ParticleSystem
    // materials, but the pack's plain FBX meshes (the falling-water sheets)
    // carry no baked vertex color channel at all. Binding a missing COLOR0
    // stream is not guaranteed to fall back to white/opaque, and multiplying
    // by an unreliable value risked rendering the main water sheet fully
    // invisible - the likely cause of "no waterfall visible anywhere" on the
    // first Play Mode test. This variant never reads vertex color at all.
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
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
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half3 rgb = tex.rgb * _TintColor.rgb * 2.0;
                half alpha = tex.a * _TintColor.a;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
