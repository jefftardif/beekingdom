Shader "BeeKingdom/WorldMapWaterOverlay"
{
    // M073-CL prototype: animates water on the existing World Map terrain art
    // without any new art asset. The water mask is derived per-pixel from the
    // painted texture's own colors (blue-dominant = water) so it automatically
    // follows the river/waterfall shapes already baked into the background.
    // URP unlit, mirrors Assets/Experiments/Environment2D5D/Shaders/ArtworkUnlit.shader.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _WmWaterSrcRect ("Source UV Rect (xMin,yMin,w,h)", Vector) = (0, 0, 1, 1)
        _WmWaterWorldRect ("World Rect (xMin,yMin,w,h)", Vector) = (0, 0, 512, 512)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
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
                float4 _Color;
                float4 _WmWaterSrcRect;
                float4 _WmWaterWorldRect;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            // Cheap value noise (no texture) - used instead of pure sine so the current
            // reads as choppy/organic rather than a visibly regular sine grid.
            float wmHash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float wmNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = wmHash(i);
                float b = wmHash(i + float2(1, 0));
                float c = wmHash(i + float2(0, 1));
                float d = wmHash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                half blueDominance = baseColor.b - max(baseColor.r, baseColor.g);
                half brightness = dot(baseColor.rgb, half3(0.333, 0.333, 0.334));
                half whiteFoamCandidate = step(0.78, brightness) * step(-0.03, blueDominance);
                half waterMask = saturate(blueDominance * 6.0 - 0.05);
                waterMask = max(waterMask, whiteFoamCandidate * 0.85);

                // Shore/bank foam: computed from the mask's own screen-space rate of change
                // (fwidth), so a bright animated foam line hugs the water/land boundary the
                // way a typical 2D water shader does, instead of only relying on whatever
                // white pixels happen to already be painted in the art. Must be evaluated
                // before any branch/return so the derivative isn't taken across a divergent
                // "if" (that would produce garbage at the exact edge we need it for).
                half shoreEdge = saturate(fwidth(waterMask) * 6.0);

                if (waterMask <= 0.001 && shoreEdge <= 0.001)
                {
                    return baseColor;
                }

                // input.uv's V axis runs opposite to on-screen "down" (Unity flips V so the
                // image renders right-side-up), but world Y increases downward on screen
                // (see WorldMapMmoFullscreenFoundationBootstrap.WorldToScreen). Un-flip V
                // here so worldPos.y actually increases going down the screen/world - without
                // this the current/foam motion reads as flowing backwards.
                float2 local01 = saturate((input.uv - _WmWaterSrcRect.xy) / max(_WmWaterSrcRect.zw, 0.0001));
                float2 worldPos = float2(
                    _WmWaterWorldRect.x + local01.x * _WmWaterWorldRect.z,
                    _WmWaterWorldRect.y + (1.0 - local01.y) * _WmWaterWorldRect.w);

                float t = _Time.y;

                // Two noise octaves scrolling downstream (+worldPos.y, see comment above)
                // read as choppy/organic water instead of a visibly regular sine grid.
                float2 flowUv1 = worldPos * 0.02 + float2(t * 0.35, t * 0.55);
                float2 flowUv2 = worldPos * 0.06 - float2(t * 0.22, t * 0.4);
                float n1 = wmNoise(flowUv1) - 0.5;
                float n2 = wmNoise(flowUv2) - 0.5;
                float2 rippleOffset = (float2(n1, n1) * 0.016 + float2(n2, n2) * 0.008) * waterMask;
                half4 rippleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + rippleOffset) * _Color;
                half4 flowing = lerp(baseColor, rippleColor, waterMask);

                // Downstream current bands driven by the same noise field for cohesion.
                half currentBand = saturate(n1 * 1.6 + 0.5);
                currentBand = pow(currentBand, 3.0);
                flowing.rgb += currentBand * waterMask * 0.28;

                // Fast, tight glints on top of the current for a "sparkling water" look.
                half glint = pow(0.5 + 0.5 * sin(worldPos.x * 0.6 + worldPos.y * 0.6 - t * 6.0), 10.0);
                flowing.rgb += glint * waterMask * 0.22;

                // Painted-foam pulse (waterfall base already painted white in the art).
                half foamStreak = pow(0.5 + 0.5 * sin(worldPos.x * 0.4 - worldPos.y * 0.9 + t * 5.0), 3.0);
                half foamPulse = 0.5 + 0.5 * sin(worldPos.x * 0.35 + t * 4.5);
                half paintedFoam = whiteFoamCandidate * saturate(foamStreak * 0.6 + foamPulse * 0.4);

                // Animated shore-line foam: a bright band that laps along the water/bank
                // boundary, scrolling lengthwise so it visibly moves rather than sitting static.
                half shoreLap = 0.55 + 0.45 * sin(worldPos.x * 0.25 + worldPos.y * 0.25 - t * 3.2);
                half shoreFoam = shoreEdge * shoreLap;

                half foam = saturate(paintedFoam + shoreFoam);
                flowing.rgb = lerp(flowing.rgb, half3(1.0, 1.0, 1.0), foam * 0.4);

                flowing.a = baseColor.a;
                return flowing;
            }
            ENDHLSL
        }
    }
}
