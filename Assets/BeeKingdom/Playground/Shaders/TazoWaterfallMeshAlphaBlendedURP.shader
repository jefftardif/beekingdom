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
    //
    // CORRECTED (M073B-CL visual polish pass, 2026-09-09): 3 side-by-side
    // instances read as an obvious tiled repeat (hard rectangular edges,
    // identical texture starting over on each copy). _EdgeFeatherUV fades
    // alpha to 0 near the mesh's own UV bounds so no segment shows a hard
    // geometric edge; _FeatherLeft/_FeatherRight (set per-instance at
    // runtime via MaterialPropertyBlock in WorldMapWaterfallFxBootstrap) gate
    // that fade off on the touching inner edges between adjacent segments,
    // so only the two true outer edges of the combined group feather into
    // the painted background - the inner joins stay full-opacity and rely on
    // instance overlap + _UvOffsetX continuity instead. _Exposure replaces
    // the previous hardcoded *2.0 so brightness/saturation can be tuned down
    // to stay under the painted background's own water color (CEO: keep the
    // background dominant, animation subtle).
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
        // Two-part blend: RGB uses standard straight-alpha "over" (SrcAlpha,
        // OneMinusSrcAlpha), but ALPHA needs its own (One, OneMinusSrcAlpha) -
        // without this, compositing several semi-transparent layers onto an
        // already-transparent render target (this shader draws into an
        // offscreen RenderTexture cleared to alpha=0) squares the alpha each
        // layer (result.a = src.a * src.a + dst.a * (1-src.a) with dst.a
        // starting at 0), making the whole waterfall look far fainter than the
        // tint colors intend. CEO: "effet presque transparent, très peu
        // visible" - this was the cause.
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
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

            // Deliberately OUTSIDE UnityPerMaterial: URP's SRP Batcher pins
            // that cbuffer's values per-MATERIAL, silently ignoring any
            // per-renderer MaterialPropertyBlock override on properties
            // inside it - the 3 waterfall segments share these material
            // assets, so _FeatherLeft/_FeatherRight/_UvOffsetX (which must
            // differ per segment, set via SetPropertyBlock in
            // WorldMapWaterfallFxBootstrap.ApplySegmentBlending) have to live
            // out here to actually take effect (loses SRP batching for this
            // shader - an acceptable trade for a handful of small meshes).
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
                half3 rgb = tex.rgb * _TintColor.rgb * _Exposure;
                half alpha = tex.a * _TintColor.a;

                half feather = max(_EdgeFeatherUV, 1e-4);
                half fadeTop    = smoothstep(0.0, feather, input.rawUV.y);
                half fadeBottom = smoothstep(0.0, feather, 1.0 - input.rawUV.y);
                half fadeLeft   = lerp(1.0, smoothstep(0.0, feather, input.rawUV.x), _FeatherLeft);
                half fadeRight  = lerp(1.0, smoothstep(0.0, feather, 1.0 - input.rawUV.x), _FeatherRight);
                alpha *= fadeTop * fadeBottom * fadeLeft * fadeRight;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
