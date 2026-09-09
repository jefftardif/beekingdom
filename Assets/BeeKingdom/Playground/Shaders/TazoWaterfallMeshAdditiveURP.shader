Shader "BeeKingdom/WaterfallFX/MeshAdditiveURP"
{
    // M073B-CL: mesh-only variant of TazoWaterfallAdditiveURP - see
    // TazoWaterfallMeshAlphaBlendedURP.shader's header for why this exists
    // (no vertex color dependency, used for the pack's plain FBX meshes
    // rather than its ParticleSystem-driven pieces).
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
                // CORRECTED (M073B-CL live Play Mode debugging, 2026-09-09): the
                // pack's caustics/foam sprites (e.g. highlight_1.png) are stored
                // as near-solid-white RGB with the actual glow/caustic SHAPE
                // baked into the alpha channel - standard convention for
                // additive glow sprites. Without multiplying by tex.a here, the
                // shader added full white brightness across the ENTIRE mesh
                // footprint regardless of the sprite's pattern, washing the
                // colored water sheet underneath into a flat white/grey blob -
                // the CEO's "gris plat" report. tex.a now masks the additive
                // contribution to the actual sprite shape.
                half3 rgb = tex.rgb * tex.a * _TintColor.rgb * _TintColor.a * 2.0;
                return half4(rgb, 0.0);
            }
            ENDHLSL
        }
    }
}
