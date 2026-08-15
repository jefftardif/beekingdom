using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D
{
    // GROUND SURFACE RESOLVER — unique source of truth for "what is ground at world X?".
    //
    // This is the production-ready abstraction extracted from GroundAnchorDiagnostic: the
    // diagnostic is now only a CONSUMER of this engine (reporting + skyline), never the
    // author of the terrain rule.
    //
    // Rule (validated visually by the ROYAL_PALACE prototype):
    //
    //     terrainY = TerrainYFromX(x)          -> interpolation of the live AnchorMarker
    //                                              ground trace (sorted by X, clamped at
    //                                              the extremes). Fallback to the 4 known
    //                                              anchors if no live marker exists.
    //     GCP(x)   = (x, terrainY(x), BuildingZ)  with BuildingZ = BackdropZ - 0.05 = 29.95
    //
    // The painted skyline of PlayerHive.png is NOT a ground source: it is multi-terraced
    // and is only used as diagnostic information (see GroundAnchorDiagnostic).
    //
    // ARTWORK CONTRACT (for any future real building):
    //   - The building ROOT transform MUST be placed at GCP(x): the root IS the Ground
    //     Contact Point.
    //   - The ARTWORK is a child of the root whose visual base maps to local (0,0,0) so
    //     the whole silhouette rises ABOVE the contact point (no floating, no centered
    //     canvas pivot). This is exactly what PremiumBuildingFactory already does with an
    //     offset quad, and what the prototype demonstrates.
    //   - Sprite path (later): use spriteAlignment = Custom with the pivot at the contact
    //     point, OR the PremiumBuildingFactory offset-quad path. Never the SpriteRenderer
    //     centered (0.5,0.5) pivot.
    public static class GroundSurfaceResolver
    {
        public const float BackdropZ = AnchorMarker.BackdropZ;
        public const float BuildingDepthOffset = 0.05f;
        public const float BuildingZ = BackdropZ - BuildingDepthOffset;

        // Fallback ground trace when no live AnchorMarker exists (same values as the
        // validated diagnostic fallback).
        private static readonly float[] FallbackAnchorX = { -15f, 0f, 10f, 35f };
        private static readonly float[] FallbackAnchorY = { 43.009f, 30.0049f, 14.0003f, 18.0029f };

        /// <summary>Collects the live AnchorMarker ground trace, sorted by X, with the
        /// validated fallback if none (or fewer than 2) is found.</summary>
        public static List<Vector3> CollectAnchors()
        {
            AnchorMarker[] markers = Object.FindObjectsByType<AnchorMarker>(FindObjectsSortMode.None);
            if (markers == null || markers.Length == 0) return BuildFallback();

            List<Vector3> result = new List<Vector3>(markers.Length);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i]) result.Add(markers[i].transform.position);
            }
            if (result.Count < 2) return BuildFallback();

            result.Sort((a, b) => a.x.CompareTo(b.x));
            return result;
        }

        private static List<Vector3> BuildFallback()
        {
            List<Vector3> result = new List<Vector3>(FallbackAnchorX.Length);
            for (int i = 0; i < FallbackAnchorX.Length; i++)
            {
                result.Add(new Vector3(FallbackAnchorX[i], FallbackAnchorY[i], BackdropZ));
            }
            result.Sort((a, b) => a.x.CompareTo(b.x));
            return result;
        }

        /// <summary>Piecewise-linear interpolation of the ground trace at world X.
        /// Clamped at the first/last anchor outside the trace range.</summary>
        public static float InterpolateTerrainY(List<Vector3> anchors, float x)
        {
            if (anchors == null || anchors.Count == 0) return 0f;
            if (anchors[anchors.Count - 1].x <= anchors[0].x) return anchors[0].y;
            if (x <= anchors[0].x) return anchors[0].y;
            if (x >= anchors[anchors.Count - 1].x) return anchors[anchors.Count - 1].y;
            for (int i = 0; i < anchors.Count - 1; i++)
            {
                if (x >= anchors[i].x && x <= anchors[i + 1].x)
                {
                    float t = (x - anchors[i].x) / Mathf.Max(0.0001f, anchors[i + 1].x - anchors[i].x);
                    return Mathf.Lerp(anchors[i].y, anchors[i + 1].y, t);
                }
            }
            return anchors[anchors.Count - 1].y;
        }

        /// <summary>Resolves the world terrain height at X (the single authority:
        /// terrainY(1.83) = 27.076, terrainY(35) = 18.003).</summary>
        public static float TerrainYFromX(float x)
        {
            return InterpolateTerrainY(CollectAnchors(), x);
        }

        /// <summary>World Ground Contact Point for a building footprint at X.</summary>
        public static Vector3 GroundContactPoint(float x)
        {
            return new Vector3(x, TerrainYFromX(x), BuildingZ);
        }

        /// <summary>Validated self-check: terrainY at the BuildingPremium anchor X (35) must
        /// reproduce the real building anchor height (18.003).</summary>
        public static float SelfCheckBuildingX35
        {
            get { return TerrainYFromX(35f); }
        }
    }
}