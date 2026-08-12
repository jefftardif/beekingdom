using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public static class HiveVisualContourImportRuntime
    {
        public const string SchemaVersion = "bee-hive-visual-contours-v1";
        public const string ResourcePath = "BeeKingdom/HiveVisualContours";
        public const string ExpectedUnityAssetPath = "Assets/BeeKingdom/Playground/Resources/BeeKingdom/HiveVisualContours.json";
        public const string CoordinateSpace = "normalized_0_1_reference_hive_art";
        public const string AuthoringRecommendation = "Inkscape SVG paths named by zone, converted to normalized JSON points";

        private static Dictionary<string, Vector2[]> importedContours;
        private static string importStatus = "not_loaded";

        public static bool HasImportedContours
        {
            get
            {
                EnsureLoaded();
                return importedContours.Count > 0;
            }
        }

        public static bool TryGetVisualContour(string hotspotId, out Vector2[] artPoints)
        {
            EnsureLoaded();
            if (hotspotId != null && importedContours.TryGetValue(NormalizeZoneId(hotspotId), out artPoints)) return true;
            artPoints = Array.Empty<Vector2>();
            return false;
        }

        public static bool TryGetVisualAnchor(string hotspotId, out Vector2 artPoint)
        {
            EnsureLoaded();
            if (hotspotId != null && importedContours.TryGetValue(NormalizeZoneId(hotspotId), out Vector2[] points) && points.Length >= 3)
            {
                float minX = points[0].x;
                float maxX = points[0].x;
                float minY = points[0].y;
                float maxY = points[0].y;
                for (int i = 1; i < points.Length; i++)
                {
                    minX = Mathf.Min(minX, points[i].x);
                    maxX = Mathf.Max(maxX, points[i].x);
                    minY = Mathf.Min(minY, points[i].y);
                    maxY = Mathf.Max(maxY, points[i].y);
                }

                artPoint = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
                return true;
            }

            artPoint = Vector2.zero;
            return false;
        }

        public static bool TryHitVisualContour(Vector2 artPoint, out string hotspotId)
        {
            EnsureLoaded();
            foreach (KeyValuePair<string, Vector2[]> contour in importedContours)
            {
                if (!ContainsOrTouches(contour.Value, artPoint, 9f)) continue;
                hotspotId = contour.Key;
                return true;
            }

            hotspotId = string.Empty;
            return false;
        }

        public static string[] ImportedZoneIds()
        {
            EnsureLoaded();
            string[] ids = new string[importedContours.Count];
            importedContours.Keys.CopyTo(ids, 0);
            return ids;
        }

        public static string[] ImportStatusRows()
        {
            EnsureLoaded();
            return new[]
            {
                "visual_contour_import_schema:" + SchemaVersion,
                "visual_contour_resource_path:" + ResourcePath,
                "visual_contour_expected_asset_path:" + ExpectedUnityAssetPath,
                "visual_contour_coordinate_space:" + CoordinateSpace,
                "visual_contour_authoring_recommendation:" + AuthoringRecommendation,
                "external_visual_contour_loaded:" + (importedContours.Count > 0 ? "true" : "false"),
                "external_visual_contour_zone_count:" + importedContours.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "external_visual_contour_import_status:" + importStatus,
                "fallback_final_visual_contour:false",
                "technical_calibration_hitbox_available:true"
            };
        }

        public static string[] RequiredZoneIds()
        {
            return new[]
            {
                "honey_storage",
                "administration_core",
                "nursery_cluster",
                "guard_post",
                "research_node",
                "genetics_garden",
                "warehouse_cells",
                "wax_workshop",
                "alliance_future_hall",
                "hive_bank",
                "infirmary_grove",
                "archives_honeyfall",
                "defense_growth",
                "academy_canopy"
            };
        }

        private static void EnsureLoaded()
        {
            if (importedContours != null) return;

            importedContours = new Dictionary<string, Vector2[]>(StringComparer.Ordinal);
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                importStatus = "missing_resources_json_waiting_ui_b";
                return;
            }

            try
            {
                HiveVisualContourDocument document = JsonUtility.FromJson<HiveVisualContourDocument>(asset.text);
                if (document == null || document.zones == null || document.zones.Length == 0)
                {
                    importStatus = "empty_or_invalid_document";
                    return;
                }

                for (int i = 0; i < document.zones.Length; i++)
                {
                    HiveVisualContourZone zone = document.zones[i];
                    if (string.IsNullOrWhiteSpace(zone.id) || zone.points == null || zone.points.Length < 3) continue;
                    importedContours[NormalizeZoneId(zone.id)] = ToArtPoints(zone.points);
                }

                importStatus = importedContours.Count > 0 ? "loaded" : "no_valid_zones";
            }
            catch (Exception exception)
            {
                importedContours.Clear();
                importStatus = "parse_failed:" + exception.GetType().Name;
            }
        }

        private static Vector2[] ToArtPoints(HiveVisualContourPoint[] points)
        {
            Vector2[] artPoints = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                artPoints[i] = new Vector2(
                    Mathf.Clamp01(points[i].x) * 1672f,
                    Mathf.Clamp01(points[i].y) * 941f);
            }

            return artPoints;
        }

        private static string NormalizeZoneId(string zoneId)
        {
            if (string.IsNullOrWhiteSpace(zoneId)) return string.Empty;

            string value = zoneId.Trim().Replace(" ", string.Empty).Replace("_", string.Empty);
            if (string.Equals(value, "Nurserie", StringComparison.OrdinalIgnoreCase)) return "nursery_cluster";
            if (string.Equals(value, "ReserveMiel", StringComparison.OrdinalIgnoreCase)) return "honey_storage";
            if (string.Equals(value, "Caserne", StringComparison.OrdinalIgnoreCase)) return "guard_post";
            if (string.Equals(value, "Entrepot", StringComparison.OrdinalIgnoreCase)) return "warehouse_cells";
            if (string.Equals(value, "Transformation", StringComparison.OrdinalIgnoreCase)) return "wax_workshop";
            if (string.Equals(value, "CentreAlliance", StringComparison.OrdinalIgnoreCase)) return "alliance_future_hall";
            if (string.Equals(value, "Recherche", StringComparison.OrdinalIgnoreCase)) return "research_node";
            if (string.Equals(value, "Genetique", StringComparison.OrdinalIgnoreCase)) return "genetics_garden";
            if (string.Equals(value, "Banque", StringComparison.OrdinalIgnoreCase)) return "hive_bank";
            if (string.Equals(value, "Informerie", StringComparison.OrdinalIgnoreCase)) return "infirmary_grove";
            if (string.Equals(value, "Infirmerie", StringComparison.OrdinalIgnoreCase)) return "infirmary_grove";
            if (string.Equals(value, "Administration", StringComparison.OrdinalIgnoreCase)) return "administration_core";
            if (string.Equals(value, "Archives", StringComparison.OrdinalIgnoreCase)) return "archives_honeyfall";
            if (string.Equals(value, "Defense", StringComparison.OrdinalIgnoreCase)) return "defense_growth";
            if (string.Equals(value, "Accademie", StringComparison.OrdinalIgnoreCase)) return "academy_canopy";
            if (string.Equals(value, "Academie", StringComparison.OrdinalIgnoreCase)) return "academy_canopy";
            return zoneId;
        }

        private static bool ContainsOrTouches(Vector2[] polygon, Vector2 point, float tolerance)
        {
            if (polygon == null || polygon.Length < 3) return false;

            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool crosses = (a.y > point.y) != (b.y > point.y);
                float denominator = Mathf.Abs(b.y - a.y) < 0.0001f ? 0.0001f : b.y - a.y;
                if (crosses && point.x < (b.x - a.x) * (point.y - a.y) / denominator + a.x) inside = !inside;
                if (DistanceToSegment(point, a, b) <= tolerance) return true;
            }

            return inside;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(point - a, ab) / Mathf.Max(0.0001f, Vector2.Dot(ab, ab));
            return Vector2.Distance(point, a + ab * Mathf.Clamp01(t));
        }

        [Serializable]
        private sealed class HiveVisualContourDocument
        {
            public string schema;
            public string coordinateSpace;
            public string sourceImage;
            public HiveVisualContourZone[] zones;
        }

        [Serializable]
        private sealed class HiveVisualContourZone
        {
            public string id;
            public string label;
            public string svgPathName;
            public HiveVisualContourPoint[] points;
        }

        [Serializable]
        private struct HiveVisualContourPoint
        {
            public float x;
            public float y;
        }
    }
}
