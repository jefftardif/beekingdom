using UnityEngine;
using BeeKingdom.Buildings.Interaction;

namespace BeeKingdom.Playground
{
    // M049-CL: small, reusable "silhouette outline breathing pulse" concept shared by every
    // real-operation activity visual on a HiveMap building - Construction's existing blue
    // "upgrading" pulse (HiveMapBuildingUpgradeVisualStateBootstrap) and Research's new emerald
    // "researching" pulse both drive the exact same BuildingSelectionHighlight technique
    // (silhouette outline via ArtworkOutline.shader) through one alpha/width/intensity easing
    // curve, so a future activity type (Training/Healing/Manufacturing) only needs its own
    // color + BuildingActivityPulse preset - never a new pulse *system*, never re-derived math.
    //
    // Deliberately NOT a MonoBehaviour and NOT a framework: a plain immutable value + the same
    // easing curve M046C already tuned and the CEO already accepted for Construction. Owning
    // bootstraps remain responsible for their own per-frame building resolution/highlight
    // lifecycle (create/reuse/hide) - this only answers "what should the outline look like right
    // now" and applies it to whichever BuildingSelectionHighlight instance it's given.
    public readonly struct BuildingActivityPulse
    {
        public Color Color { get; }
        public float AlphaMin { get; }
        public float AlphaMax { get; }
        public float WidthMin { get; }
        public float WidthMax { get; }
        public float IntensityMin { get; }
        public float IntensityMax { get; }
        public float PeriodSeconds { get; }

        public BuildingActivityPulse(Color color, float alphaMin, float alphaMax, float widthMin, float widthMax, float intensityMin, float intensityMax, float periodSeconds)
        {
            Color = color;
            AlphaMin = alphaMin;
            AlphaMax = alphaMax;
            WidthMin = widthMin;
            WidthMax = widthMax;
            IntensityMin = intensityMin;
            IntensityMax = intensityMax;
            PeriodSeconds = periodSeconds;
        }

        // Same perceptual amplitude as the current, CEO-accepted Construction pulse
        // (HiveMapBuildingUpgradeVisualStateBootstrap's own constants, unchanged by this
        // mission - width 4.5->8 texels, intensity 1->1.48, alpha 0.88->1, period 1.35s). Kept
        // here too so Research can explicitly inherit the SAME numbers (mission requirement)
        // without Research's bootstrap reaching into Construction's file for constants.
        public static readonly BuildingActivityPulse Construction = new BuildingActivityPulse(
            new Color(0.35f, 0.75f, 1f, 1f), 0.88f, 1f, 4.5f, 8f, 1f, 1.48f, 1.35f);

        // Emerald / living green - premium fantasy, biological/scientific, not neon/toxic lime.
        // Same alpha/width/intensity spans and period as Construction (mission requirement: the
        // Research pulse must be "approximately as visually strong" as the accepted Construction
        // one) - only the hue differs.
        public static readonly BuildingActivityPulse Research = new BuildingActivityPulse(
            new Color(0.16f, 0.86f, 0.52f, 1f), 0.88f, 1f, 4.5f, 8f, 1f, 1.48f, 1.35f);

        private static float Phase01(float time, float periodSeconds)
        {
            float phase = Mathf.Sin(time / periodSeconds * Mathf.PI * 2f) * 0.5f + 0.5f;
            float shaped = Mathf.SmoothStep(0f, 1f, phase);
            return shaped * shaped * (3f - 2f * shaped);
        }

        public float Alpha(float time) => Mathf.Lerp(AlphaMin, AlphaMax, Phase01(time, PeriodSeconds));
        public float Width(float time) => Mathf.Lerp(WidthMin, WidthMax, Phase01(time, PeriodSeconds));
        public float Intensity(float time) => Mathf.Lerp(IntensityMin, IntensityMax, Phase01(time, PeriodSeconds));

        public Color ColorForProof(float time)
        {
            Color color = Color;
            color.a = Alpha(time);
            return color;
        }

        // One-time setup when a highlight instance starts representing this activity - separate
        // from the per-frame Apply() below so a caller only pays Show()'s silhouette-clone cost
        // once per building/state-change, matching the existing Construction bootstrap's own
        // create-vs-update split.
        public void Configure(BuildingSelectionHighlight highlight)
        {
            if (highlight == null) return;
            highlight.TintColor = Color;
            highlight.OutlineIntensity = IntensityMin;
            highlight.OutlineWidthTexels = WidthMin;
        }

        public void Apply(BuildingSelectionHighlight highlight, float time)
        {
            if (highlight == null) return;
            highlight.SetVisualState(Alpha(time), Width(time), Intensity(time));
        }
    }
}
