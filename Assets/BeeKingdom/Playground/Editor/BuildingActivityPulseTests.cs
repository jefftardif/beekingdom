using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    // M049-CL: BuildingActivityPulse is a plain struct with no MonoBehaviour/scene dependency,
    // so its color/amplitude math is directly testable here - the per-frame Research/Construction
    // building resolution (HiveMapResearchVisualStateBootstrap/HiveMapBuildingUpgradeVisualStateBootstrap)
    // is not: both depend on HiveViewProductUiPresenter's static runtime controller state, the
    // same live-Play-Mode-only constraint already documented for every other test file in this
    // project that touches that presenter (see AllianceClientTests.cs's own top-of-file comment).
    public sealed class BuildingActivityPulseTests
    {
        [Test]
        public void ResearchPresetIsEmeraldGreen_NotConstructionBlue()
        {
            Color research = BuildingActivityPulse.Research.Color;
            Color construction = BuildingActivityPulse.Construction.Color;

            Assert.That(research.g, Is.GreaterThan(research.r), "Research must read as green, not the Construction blue hue");
            Assert.That(research.g, Is.GreaterThan(research.b), "green channel must dominate for an emerald reading");
            Assert.That(construction.b, Is.GreaterThan(construction.g), "Construction must remain its own existing blue - unaffected by this mission");
            Assert.That(research, Is.Not.EqualTo(construction));
        }

        [Test]
        public void ResearchPresetIsNotNeonLimeOrToxicGreen()
        {
            // "Premium emerald", not a flat/saturated neon lime (roughly equal-high R and G) nor a
            // toxic acid green (very high G with near-zero R and B) - the mission's explicit taste
            // guardrail, expressed as a cheap sanity check rather than a purely aesthetic judgment.
            Color research = BuildingActivityPulse.Research.Color;
            Assert.That(research.r, Is.LessThan(research.g * 0.6f), "must not read as a flat lime (high red alongside high green)");
            Assert.That(research.b, Is.GreaterThan(0.15f), "must carry enough blue to read as emerald rather than a pure acid green");
        }

        [Test]
        public void ResearchAndConstruction_ShareTheSameAmplitudeSpans()
        {
            // Mission requirement: Research must be "approximately as visually strong" as the
            // already-accepted Construction pulse - same spans, only the hue differs.
            BuildingActivityPulse construction = BuildingActivityPulse.Construction;
            BuildingActivityPulse research = BuildingActivityPulse.Research;

            Assert.That(research.AlphaMin, Is.EqualTo(construction.AlphaMin));
            Assert.That(research.AlphaMax, Is.EqualTo(construction.AlphaMax));
            Assert.That(research.WidthMin, Is.EqualTo(construction.WidthMin));
            Assert.That(research.WidthMax, Is.EqualTo(construction.WidthMax));
            Assert.That(research.IntensityMin, Is.EqualTo(construction.IntensityMin));
            Assert.That(research.IntensityMax, Is.EqualTo(construction.IntensityMax));
            Assert.That(research.PeriodSeconds, Is.EqualTo(construction.PeriodSeconds));
        }

        [Test]
        public void ConstructionPreset_MatchesTheCurrentAcceptedBootstrapConstants()
        {
            // Guards against silently drifting Construction's own (CEO-approved, M046C) pulse
            // while adding this shared struct - these numbers must mirror
            // HiveMapBuildingUpgradeVisualStateBootstrap's own PulseAlphaMin/Max/
            // PulseOutlineWidthMin/Max/PulseIntensityMin/Max/PulsePeriodSeconds exactly.
            BuildingActivityPulse construction = BuildingActivityPulse.Construction;
            Assert.That(construction.AlphaMin, Is.EqualTo(0.88f));
            Assert.That(construction.AlphaMax, Is.EqualTo(1f));
            Assert.That(construction.WidthMin, Is.EqualTo(4.5f));
            Assert.That(construction.WidthMax, Is.EqualTo(8f));
            Assert.That(construction.IntensityMin, Is.EqualTo(1f));
            Assert.That(construction.IntensityMax, Is.EqualTo(1.48f));
            Assert.That(construction.PeriodSeconds, Is.EqualTo(1.35f));
        }

        [Test]
        public void PulseValues_StayWithinConfiguredBoundsAcrossAFullPeriod()
        {
            BuildingActivityPulse pulse = BuildingActivityPulse.Research;
            for (float t = 0f; t < pulse.PeriodSeconds * 2f; t += 0.05f)
            {
                float alpha = pulse.Alpha(t);
                float width = pulse.Width(t);
                float intensity = pulse.Intensity(t);
                Assert.That(alpha, Is.InRange(pulse.AlphaMin, pulse.AlphaMax));
                Assert.That(width, Is.InRange(pulse.WidthMin, pulse.WidthMax));
                Assert.That(intensity, Is.InRange(pulse.IntensityMin, pulse.IntensityMax));
            }
        }

        [Test]
        public void PulseIsClearlyPerceptible_NotATimidVariation()
        {
            // Direct regression against the exact M046 lesson quoted in this mission: the first
            // Construction pulse was "technically working but practically invisible". Assert the
            // amplitude spans are wide enough to read as a real breathing effect, for both presets.
            foreach (BuildingActivityPulse pulse in new[] { BuildingActivityPulse.Construction, BuildingActivityPulse.Research })
            {
                Assert.That(pulse.AlphaMax - pulse.AlphaMin, Is.GreaterThanOrEqualTo(0.1f), "alpha swing too timid");
                Assert.That(pulse.WidthMax - pulse.WidthMin, Is.GreaterThanOrEqualTo(2f), "outline width swing too timid");
                Assert.That(pulse.IntensityMax - pulse.IntensityMin, Is.GreaterThanOrEqualTo(0.3f), "intensity swing too timid");
            }
        }
    }
}
