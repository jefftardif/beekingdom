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

            // Three octaves at unrelated frequencies/offsets so their grid cells never line
            // up. A single octave of wmNoise, once pushed through a hard threshold/pow (as
            // the current band and glints do to read as bold motion), shows its own square
            // interpolation cells as blocky "1980s TV static" - this breaks that alignment
            // up into something organic instead.
            float wmFbm(float2 p)
            {
                float f = wmNoise(p) * 0.55;
                f += wmNoise(p * 2.37 + float2(11.7, 3.1)) * 0.30;
                f += wmNoise(p * 4.81 + float2(-5.3, 8.9)) * 0.15;
                return f;
            }

            // Flow-aligned streak field for calm river current. Unlike wmFbm above (which
            // just shrinks the same aspect-ratio cell every octave - still visibly "blocky
            // rectangles" once the base cell is long and thin), this deliberately shrinks
            // the ALONG axis much faster than the ACROSS axis per octave: the base octave
            // keeps the long "streak" character, but the higher octaves are almost square/
            // isotropic fine detail that breaks up its edges instead of just rescaling them.
            float wmStreakField(float across, float along, float acrossFreq, float alongFreq, float speed, float t)
            {
                float2 uvA = float2(across * acrossFreq, along * alongFreq - t * speed);
                float2 uvB = float2(across * acrossFreq * 2.6, along * alongFreq * 9.0 - t * speed * 1.7);
                float2 uvC = float2(across * acrossFreq * 5.5, along * alongFreq * 22.0 - t * speed * 2.4);
                return wmNoise(uvA) * 0.5 + wmNoise(uvB) * 0.3 + wmNoise(uvC) * 0.2;
            }

            // Same water-color heuristic as the main mask below, isolated so the local
            // flow-direction probe further down can reuse it at neighboring UVs.
            half wmWaterMaskAt(float2 uv)
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;
                half bd = c.b - max(c.r, c.g);
                half br = dot(c.rgb, half3(0.333, 0.333, 0.334));
                half foam = step(0.78, br) * step(-0.03, bd);
                half m = saturate(bd * 6.0 - 0.05);
                return max(m, foam * 0.85);
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

                // Auto-detected local flow direction: probe the water mask a fixed WORLD
                // distance (not screen/texel distance) to each side of this pixel and take
                // the gradient across the river banks; the flow tangent runs perpendicular
                // to that gradient (along the banks, not across them). This follows real
                // bends in the river instead of assuming one direction for the whole map.
                //
                // Deliberately NOT screen-space derivatives (ddx/ddy/fwidth) here: an
                // earlier revision used fwidth() on the per-pixel color mask and it reacted
                // to ordinary rock/moss shading noise everywhere, producing a full-screen
                // diagonal hatch (confirmed from a CEO screenshot). Sampling at a fixed,
                // fairly large WORLD-space radius instead of adjacent screen pixels acts as
                // a low-pass filter - it only sees real water/bank transitions, not paint
                // texture grain, and its result no longer depends on zoom level.
                const float FlowProbeWorldRadius = 14.0;
                float2 worldPerTexelUv = _WmWaterSrcRect.zw / max(_WmWaterWorldRect.zw, 0.0001);
                float2 probeUv = worldPerTexelUv * FlowProbeWorldRadius;
                half maskEast = wmWaterMaskAt(input.uv + float2(probeUv.x, 0.0));
                half maskWest = wmWaterMaskAt(input.uv - float2(probeUv.x, 0.0));
                // input.uv's V axis is flipped relative to world Y (see the note below on
                // worldPos), so "+V" is a step NORTH in world space, not south.
                half maskNorth = wmWaterMaskAt(input.uv + float2(0.0, probeUv.y));
                half maskSouth = wmWaterMaskAt(input.uv - float2(0.0, probeUv.y));

                // maskNorth/maskSouth were sampled along +V/-V, which is -worldY/+worldY (the
                // same flip as above) - negate that component so bankGradient is a genuine
                // (d/dWorldX, d/dWorldY) gradient, matching bankGradient.x's un-flipped sign.
                float2 bankGradient = float2(maskEast - maskWest, -(maskNorth - maskSouth));
                float gradientStrength = length(bankGradient);

                // River/waterfall on this map generally trend southeast - used only to
                // pick a fallback in open water where the bank gradient is too weak/noisy
                // to trust, and to resolve the tangent's 180-degree ambiguity (the gradient
                // alone can't tell upstream from downstream).
                float2 FallbackFlowDir = float2(0.7071068, 0.7071068);
                float2 FlowDir = FallbackFlowDir;
                if (gradientStrength > 0.02)
                {
                    float2 tangent = normalize(float2(-bankGradient.y, bankGradient.x));
                    if (dot(tangent, FallbackFlowDir) < 0.0) tangent = -tangent;
                    float confidence = saturate((gradientStrength - 0.02) * 8.0);
                    FlowDir = normalize(lerp(FallbackFlowDir, tangent, confidence));
                }

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

                // CEO: previous pass was too subtle to read as motion even zoomed in, then
                // (after boosting it) looked blocky/pixelated on calm river stretches - a
                // single anisotropic octave stretched into "streaks" still reads as visible
                // rectangular blocks once thresholded hard. wmStreakField below fixes that
                // by shrinking the along-flow axis much faster than the across axis per
                // octave, so the fine octaves are near-isotropic detail that breaks up the
                // base streak's edges instead of just rescaling the same rectangle.
                float field1 = wmStreakField(across, along, 0.05, 0.006, 3.2, t) - 0.5;
                float field2 = wmStreakField(across, along, 0.12, 0.014, 5.4, t) - 0.5;
                float2 rippleOffset = FlowDir * (field1 * 0.05 + field2 * 0.025) * waterMask;
                half4 rippleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + rippleOffset) * _Color;
                half4 flowing = lerp(baseColor, rippleColor, waterMask);

                // Downstream current bands driven by the same streak field for cohesion.
                half currentBand = smoothstep(0.35, 0.65, field1 + 0.5);
                flowing.rgb += currentBand * waterMask * 0.55;

                // Fast, tight glints - also streak-shaped and flow-aligned - for a
                // "sparkling water" look. Same streak-field treatment as the current band.
                half glintField = wmStreakField(across, along, 0.22, 0.03, 7.0, t);
                half glint = smoothstep(0.55, 0.82, glintField);
                flowing.rgb += glint * waterMask * 0.4;

                // Reference note (VDB waterfall breakdown): real falling water reads almost
                // white up top with cyan/blue only showing near the base, and the white
                // breaks up into patches via turbulence rather than sitting in smooth bands.
                // Approximate both cheaply: two noise octaves at different scale/speed
                // "tear up" the painted foam into moving clumps instead of a smooth pulse,
                // and calm (non-foam) water gets a small push toward cyan for richer color.
                half foamTurbulence = wmFbm(worldPos * 0.1 - FlowDir * (t * 6.0));
                half foam = whiteFoamCandidate * smoothstep(0.3, 0.7, foamTurbulence);
                flowing.rgb = lerp(flowing.rgb, half3(1.0, 1.0, 1.0), foam * 0.6);
                flowing.rgb += foam * 0.15;

                half cyanPush = waterMask * (1.0 - whiteFoamCandidate) * 0.14;
                flowing.rgb = lerp(flowing.rgb, flowing.rgb * half3(0.86, 1.0, 1.18), cyanPush);

                flowing.a = baseColor.a;
                return flowing;
            }
            ENDHLSL
        }
    }
}
