Shader "BeeKingdom/WaterfallFX/MeshAdditiveURP"
{
    // M073B-CL: mesh-only variant of TazoWaterfallAdditiveURP - see
    // TazoWaterfallMeshAlphaBlendedURP.shader's header for why this exists
    // (no vertex color dependency, used for the pack's plain FBX meshes
    // rather than its ParticleSystem-driven pieces).
    //
    // CORRECTED (M073B-CL visual polish pass, 2026-09-09): same edge-feather
    // and UV-continuity properties as the alpha-blended mesh shader, but
    // since "Blend One One" ignores the alpha channel entirely, the feather
    // here multiplies straight into the RGB contribution instead. _Exposure
    // replaces the previous hardcoded *2.0.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _Exposure ("Exposure", Range(0.5, 2.0)) = 1.1
        _EdgeFeatherUV ("Edge Feather (UV)", Range(0.0, 0.5)) = 0.3
        _FeatherLeft ("Feather Left Edge", Range(0,1)) = 0
        _FeatherRight ("Feather Right Edge", Range(0,1)) = 0
        _UvOffsetX ("UV Continuity Offset X", Float) = 0
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
                float2 rawUV : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float _Exposure;
                float _EdgeFeatherUV;
            CBUFFER_END

            // See TazoWaterfallMeshAlphaBlendedURP.shader's header: these must
            // stay outside UnityPerMaterial for per-renderer
            // MaterialPropertyBlock overrides to actually apply under the
            // SRP Batcher.
            float _FeatherLeft;
            float _FeatherRight;
            float _UvOffsetX;

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.rawUV = input.uv;
                float2 offsetUv = input.uv + float2(_UvOffsetX, 0);
                o.uv = TRANSFORM_TEX(offsetUv, _MainTex);
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
                half feather = max(_EdgeFeatherUV, 1e-4);
                half fadeTop    = smoothstep(0.0, feather, input.rawUV.y);
                half fadeBottom = smoothstep(0.0, feather, 1.0 - input.rawUV.y);
                half fadeLeft   = lerp(1.0, smoothstep(0.0, feather, input.rawUV.x), _FeatherLeft);
                half fadeRight  = lerp(1.0, smoothstep(0.0, feather, 1.0 - input.rawUV.x), _FeatherRight);
                half edgeFade = fadeTop * fadeBottom * fadeLeft * fadeRight;

                half3 rgb = tex.rgb * tex.a * _TintColor.rgb * _TintColor.a * _Exposure * edgeFade;
                return half4(rgb, 0.0);
            }
            ENDHLSL
        }
    }
}
