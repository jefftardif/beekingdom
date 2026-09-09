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

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                half blueDominance = baseColor.b - max(baseColor.r, baseColor.g);
                half brightness = dot(baseColor.rgb, half3(0.333, 0.333, 0.334));
                half whiteFoamCandidate = step(0.78, brightness) * step(-0.03, blueDominance);
                half waterMask = saturate(blueDominance * 6.0 - 0.05);
                waterMask = max(waterMask, whiteFoamCandidate * 0.85);
                if (waterMask <= 0.001)
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

                // Two ripple octaves (different scale/speed) read as choppier, more natural
                // water than a single sine - a single octave looked flat and "not realistic".
                float2 rippleOffset =
                    float2(sin(worldPos.y * 0.05 + t * 1.8), cos(worldPos.x * 0.05 + t * 1.5)) * 0.010
                    + float2(sin(worldPos.x * 0.14 - t * 2.6), cos(worldPos.y * 0.14 - t * 2.1)) * 0.004;
                rippleOffset *= waterMask;
                half4 rippleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + rippleOffset) * _Color;
                half4 flowing = lerp(baseColor, rippleColor, waterMask);

                // Downstream current bands: worldPos.y now correctly increases downhill, so
                // "- t" here travels toward +worldPos.y (downstream) instead of upstream.
                half currentBand = pow(0.5 + 0.5 * sin(worldPos.x * 0.05 + worldPos.y * 0.11 - t * 2.6), 4.0);
                flowing.rgb += currentBand * waterMask * 0.26;

                // Fast, tight glints on top of the current for a more "sparkling water" look.
                half glint = pow(0.5 + 0.5 * sin(worldPos.x * 0.6 + worldPos.y * 0.6 - t * 6.0), 10.0);
                flowing.rgb += glint * waterMask * 0.22;

                // Foam: falling streaks travel down (+worldPos.y) at the waterfall, plus a
                // brightening pulse. Stronger and whiter than before per CEO feedback.
                half foamStreak = pow(0.5 + 0.5 * sin(worldPos.x * 0.4 - worldPos.y * 0.9 + t * 5.0), 3.0);
                half foamPulse = 0.5 + 0.5 * sin(worldPos.x * 0.35 + t * 4.5);
                half foam = whiteFoamCandidate * saturate(foamStreak * 0.6 + foamPulse * 0.4);
                flowing.rgb = lerp(flowing.rgb, half3(1.0, 1.0, 1.0), foam * 0.35);

                flowing.a = baseColor.a;
                return flowing;
            }
            ENDHLSL
        }
    }
}
