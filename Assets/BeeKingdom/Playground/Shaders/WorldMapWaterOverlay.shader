Shader "BeeKingdom/WorldMapWaterOverlay"
{
    // M073-CL prototype: animates water on the existing World Map terrain art.
    // The water mask is derived per-pixel from the painted texture's own colors
    // (blue-dominant = water) so it automatically follows the river/waterfall
    // shapes already baked into the background - the frozen terrain package
    // itself is never modified or resampled-and-distorted (see revision note
    // below on why resampling was dropped).
    //
    // Revision note: earlier versions distorted (rippleOffset) and resampled
    // the terrain art itself to fake surface ripples. The painted art has its
    // own baked-in grain (confirmed by the CEO: visible even on dry rock with
    // zero water effect running there) which is fine sitting still, but
    // resampling it at a shifting offset every frame made that grain look
    // like it was "swimming" ("television de 1980"). Fixed at the root by
    // adding a small generated (procedural, tileable, grain-free) noise
    // texture - _WmWaterNoise - and driving all animated color contributions
    // from IT instead of from redistorted terrain pixels. baseColor (the
    // terrain pixel) is now sampled exactly once, undistorted, and animation
    // is purely additive color on top - CEO approved adding this one small
    // generated asset for exactly this purpose.
    // URP unlit, mirrors Assets/Experiments/Environment2D5D/Shaders/ArtworkUnlit.shader.
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WmWaterNoise ("Water Noise (generated, tileable)", 2D) = "grey" {}
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
            TEXTURE2D(_WmWaterNoise);
            SAMPLER(sampler_WmWaterNoise);

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

            // Cheap value noise (no texture) - only used now for the low-frequency
            // per-region time-offset below (desyncing calm-water animation phase),
            // not for any visible color pattern - that all comes from _WmWaterNoise.
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

            // Brightness + blue-dominance at a UV, packed together so the neighborhood
            // average below can be computed with one call per sample point.
            half2 wmBrightBlueAt(float2 uv)
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;
                half bd = c.b - max(c.r, c.g);
                half br = dot(c.rgb, half3(0.333, 0.333, 0.334));
                return half2(br, bd);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                // input.uv's V axis runs opposite to on-screen "down" (Unity flips V so the
                // image renders right-side-up), but world Y increases downward on screen
                // (see WorldMapMmoFullscreenFoundationBootstrap.WorldToScreen). Un-flip V
                // here so worldPos.y actually increases going down the screen/world - without
                // this the current/foam motion reads as flowing backwards.
                float2 local01 = saturate((input.uv - _WmWaterSrcRect.xy) / max(_WmWaterSrcRect.zw, 0.0001));
                float2 worldPos = float2(
                    _WmWaterWorldRect.x + local01.x * _WmWaterWorldRect.z,
                    _WmWaterWorldRect.y + (1.0 - local01.y) * _WmWaterWorldRect.w);
                float2 worldPerTexelUv = _WmWaterSrcRect.zw / max(_WmWaterWorldRect.zw, 0.0001);

                half blueDominance = baseColor.b - max(baseColor.r, baseColor.g);
                half brightness = dot(baseColor.rgb, half3(0.333, 0.333, 0.334));

                // CEO: a genuine waterfall has huge PER-PIXEL brightness/color swings (bright
                // foam streaks right next to darker shadowed ribbons, in the same cascade),
                // AND (per the VDB waterfall reference the CEO found earlier) real falling
                // water is legitimately near-white at the top and shifts to cyan/blue near
                // the base - that blue is normal waterfall color, not evidence of "this is
                // actually calm water". Classifying by absolute brightness/blue alone kept
                // splitting one continuous cascade into a white "foam" region and a blue
                // "calm water" region, each getting a different visual treatment - a hard,
                // "trop fenetre" patchwork, worse than either look applied uniformly.
                //
                // Fix: classify primarily by LOCAL TURBULENCE (how much brightness varies
                // across a neighborhood) rather than absolute color. A churning waterfall -
                // white top or blue base alike - has high local contrast (streaks/shadows
                // next to bright foam); a genuinely calm pond or river stretch is smooth and
                // low-contrast even though it may average a similar color. This is computed
                // over the same wide WORLD-space neighborhood as the flow-direction probe
                // below (never screen-space derivatives - see that comment for why), so the
                // whole cascade reads as one consistent turbulent region.
                const float FoamClassifyWorldRadius = 34.0;
                float2 classifyUv = worldPerTexelUv * FoamClassifyWorldRadius;
                half2 bbCenter = half2(brightness, blueDominance);
                half2 bbE = wmBrightBlueAt(input.uv + float2(classifyUv.x, 0.0));
                half2 bbW = wmBrightBlueAt(input.uv - float2(classifyUv.x, 0.0));
                half2 bbN = wmBrightBlueAt(input.uv + float2(0.0, classifyUv.y));
                half2 bbS = wmBrightBlueAt(input.uv - float2(0.0, classifyUv.y));
                half2 bbNE = wmBrightBlueAt(input.uv + classifyUv);
                half2 bbSW = wmBrightBlueAt(input.uv - classifyUv);
                half2 bbNW = wmBrightBlueAt(input.uv + float2(-classifyUv.x, classifyUv.y));
                half2 bbSE = wmBrightBlueAt(input.uv + float2(classifyUv.x, -classifyUv.y));
                half2 bbAvg = (bbCenter + bbE + bbW + bbN + bbS + bbNE + bbSW + bbNW + bbSE) * (1.0 / 9.0);

                half brightMin = min(bbCenter.x, min(min(bbE.x, bbW.x), min(bbN.x, bbS.x)));
                brightMin = min(brightMin, min(min(bbNE.x, bbSW.x), min(bbNW.x, bbSE.x)));
                half brightMax = max(bbCenter.x, max(max(bbE.x, bbW.x), max(bbN.x, bbS.x)));
                brightMax = max(brightMax, max(max(bbNE.x, bbSW.x), max(bbNW.x, bbSE.x)));
                half turbulence = smoothstep(0.06, 0.18, brightMax - brightMin);

                // Loose "is this even water-colored" gate (excludes brown/green rock) - much
                // looser than the brightness/blue classification it used to be, since that
                // job now belongs to turbulence above.
                half waterish = smoothstep(0.15, 0.35, bbAvg.x) * smoothstep(-0.28, -0.05, bbAvg.y);
                half brightFoam = smoothstep(0.24, 0.55, bbAvg.x) * smoothstep(-0.16, 0.0, bbAvg.y);
                half foamWeight = waterish * max(brightFoam, turbulence);
                half waterMask = saturate(blueDominance * 6.0 - 0.05);
                waterMask = max(waterMask, foamWeight * 0.85);

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
                // not round blobs. Sample the generated water-noise texture in a flow-aligned
                // frame - low frequency along the flow, higher frequency across it - so the
                // pattern itself is stretched into long bands running the same way as the
                // current, then scroll that frame along the flow direction over time. Because
                // this is a real filtered texture (not raw per-pixel hash math), it has no
                // blocky grid cells and no grain of its own.
                float along = dot(worldPos, FlowDir);
                float across = dot(worldPos, FlowPerp);

                // Per-region time offset for calm water only (Catlike Coding's flow/texture-
                // distortion tutorial: without this, every part of the water animates on the
                // exact same clock and the whole surface pulses in visible unison, which
                // reads as artificial). A slow, large-scale spatial noise desyncs the phase
                // from one stretch of river to the next - not applied to the foam turbulence
                // below, which already reads fine on its own.
                float tCalm = t + wmNoise(worldPos * 0.006) * 6.0;

                half4 flowing = baseColor;

                // Downstream current bands: the noise texture's R channel, sampled in the
                // flow-aligned frame above. Wide smoothstep range = a soft continuous
                // gradient rather than a near-binary on/off, for a gentle drifting-brightness
                // look. Scaled by (1-foamWeight) so this "calm river" look never stacks with
                // the foam turbulence below on cascading water - stacking both is what made a
                // waterfall look worse in an earlier revision.
                half calmWater = 1.0 - foamWeight;
                float2 currentUv = float2(across * 0.05, along * 0.006 - tCalm * 0.35);
                half currentRaw = SAMPLE_TEXTURE2D(_WmWaterNoise, sampler_WmWaterNoise, currentUv).r;
                half currentBand = smoothstep(0.35, 0.65, currentRaw);
                flowing.rgb += currentBand * waterMask * calmWater * 0.5;

                // Soft, broad glints: the noise texture's G channel (decorrelated from R via
                // a different generation seed) at a finer scale/faster scroll.
                float2 glintUv = float2(across * 0.09, along * 0.018 - tCalm * 0.6);
                half glintRaw = SAMPLE_TEXTURE2D(_WmWaterNoise, sampler_WmWaterNoise, glintUv).g;
                half glint = smoothstep(0.5, 0.82, glintRaw);
                flowing.rgb += glint * waterMask * calmWater * 0.22;

                // Reference note (VDB waterfall breakdown): real falling water reads almost
                // white up top with cyan/blue only showing near the base, and the white
                // breaks up into patches via turbulence rather than sitting in smooth bands.
                // The noise texture's B channel (isotropic sampling - a waterfall doesn't
                // have a "flow-aligned" surface the way a calm river does) approximates the
                // turbulent break-up. Driven by the continuous foamWeight (not a hard
                // brightness step) so this covers the WHOLE cascade, not just its whitest
                // pixels.
                float2 foamUv = worldPos * 0.02 - FlowDir * (t * 0.5);
                half foamRaw = SAMPLE_TEXTURE2D(_WmWaterNoise, sampler_WmWaterNoise, foamUv).b;
                half foam = foamWeight * smoothstep(0.35, 0.65, foamRaw);
                flowing.rgb = lerp(flowing.rgb, half3(1.0, 1.0, 1.0), foam * 0.6);
                flowing.rgb += foam * 0.15;

                // Calm (non-foam) water gets a small push toward cyan for richer color.
                half cyanPush = waterMask * calmWater * 0.14;
                flowing.rgb = lerp(flowing.rgb, flowing.rgb * half3(0.86, 1.0, 1.18), cyanPush);

                flowing.a = baseColor.a;
                return flowing;
            }
            ENDHLSL
        }
    }
}
