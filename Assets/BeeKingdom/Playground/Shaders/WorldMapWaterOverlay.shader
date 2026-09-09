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

                // NOTE: an earlier revision added a fwidth(waterMask)-based "shoreline foam"
                // here. It was pulled - fwidth on a mask derived from the painted texture's
                // own noisy colors reacts to ordinary rock/moss shading detail everywhere,
                // not just real water edges, and produced a full-screen diagonal hatch over
                // the whole map (confirmed from a CEO screenshot). Do not reintroduce edge
                // detection driven by this per-pixel color mask without a much stricter,
                // neighborhood-averaged gate.
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

                // CEO: every river/waterfall on this map runs southeast. worldPos.x
                // increases east and worldPos.y increases south (see the V-flip note
                // above), so southeast is the single fixed direction float2(1, 1)
                // normalized. All scrolling noise below is advected along this one
                // direction so every effect drifts the same way instead of each
                // picking its own diagonal.
                float2 FlowDir = float2(0.7071068, 0.7071068);
                float2 FlowPerp = float2(-FlowDir.y, FlowDir.x);

                // Real flowing water shows streaks/reflections elongated ALONG the current,
                // not round blobs - isotropic noise (equal frequency on both axes) was
                // reading as flat, static-looking mottling on calm stretches (CEO: "il
                // manque encore quelque chose"). Sample noise in a flow-aligned frame -
                // low frequency along the flow, higher frequency across it - so the noise
                // itself is stretched into long bands running the same way as the current,
                // then scroll that frame along the flow direction over time.
                float along = dot(worldPos, FlowDir);
                float across = dot(worldPos, FlowPerp);

                float2 streakUv1 = float2(across * 0.05, along * 0.006 - t * 1.1);
                float2 streakUv2 = float2(across * 0.12, along * 0.014 - t * 1.9);
                float n1 = wmNoise(streakUv1) - 0.5;
                float n2 = wmNoise(streakUv2) - 0.5;
                float2 rippleOffset = FlowDir * (n1 * 0.016 + n2 * 0.008) * waterMask;
                half4 rippleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + rippleOffset) * _Color;
                half4 flowing = lerp(baseColor, rippleColor, waterMask);

                // Downstream current bands driven by the same streak field for cohesion.
                half currentBand = saturate(n1 * 1.6 + 0.5);
                currentBand = pow(currentBand, 3.0);
                flowing.rgb += currentBand * waterMask * 0.28;

                // Fast, tight glints - also streak-shaped and flow-aligned - for a
                // "sparkling water" look without reading as a regular grid.
                float2 glintUv = float2(across * 0.22, along * 0.03 - t * 2.6);
                float n3 = wmNoise(glintUv);
                half glint = pow(saturate(n3), 6.0);
                flowing.rgb += glint * waterMask * 0.22;

                // Reference note (VDB waterfall breakdown): real falling water reads almost
                // white up top with cyan/blue only showing near the base, and the white
                // breaks up into patches via turbulence rather than sitting in smooth bands.
                // Approximate both cheaply: two noise octaves at different scale/speed
                // "tear up" the painted foam into moving clumps instead of a smooth pulse,
                // and calm (non-foam) water gets a small push toward cyan for richer color.
                float foamNoiseA = wmNoise(worldPos * 0.09 - FlowDir * (t * 2.4));
                float foamNoiseB = wmNoise(worldPos * 0.22 - FlowDir * (t * 3.4));
                half foamTurbulence = saturate(foamNoiseA * 0.6 + foamNoiseB * 0.4);
                half foam = whiteFoamCandidate * saturate(pow(foamTurbulence, 1.4) * 1.35);
                flowing.rgb = lerp(flowing.rgb, half3(1.0, 1.0, 1.0), foam * 0.55);
                flowing.rgb += foam * 0.12;

                half cyanPush = waterMask * (1.0 - whiteFoamCandidate) * 0.14;
                flowing.rgb = lerp(flowing.rgb, flowing.rgb * half3(0.86, 1.0, 1.18), cyanPush);

                flowing.a = baseColor.a;
                return flowing;
            }
            ENDHLSL
        }
    }
}
