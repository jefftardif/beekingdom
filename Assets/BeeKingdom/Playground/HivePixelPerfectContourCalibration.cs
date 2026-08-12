using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public enum HiveContourPriorityBand
    {
        P0 = 0,
        P1 = 1,
        P2 = 2
    }

    public sealed class HiveZoneContourDefinition
    {
        public HiveZoneContourDefinition(string hotspotId, string label, HiveContourPriorityBand priorityBand, int priority, Vector2[] visualContour, float tactilePaddingArtPixels)
        {
            HotspotId = Require(hotspotId);
            Label = Require(label);
            PriorityBand = priorityBand;
            Priority = priority;
            VisualContour = visualContour ?? Array.Empty<Vector2>();
            TactileHitbox = ExpandAroundCentroid(VisualContour, tactilePaddingArtPixels);
            TactilePaddingArtPixels = tactilePaddingArtPixels;
        }

        public string HotspotId { get; }
        public string Label { get; }
        public HiveContourPriorityBand PriorityBand { get; }
        public int Priority { get; }
        public Vector2[] VisualContour { get; }
        public Vector2[] TactileHitbox { get; }
        public float TactilePaddingArtPixels { get; }

        public bool ContainsTactilePoint(Vector2 artPoint)
        {
            return ContainsPolygon(artPoint, TactileHitbox) || IsPointNearPolygon(artPoint, TactileHitbox, 4f);
        }

        private static string Require(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Contour definition requires a stable id and label.");
            return value;
        }

        private static Vector2[] ExpandAroundCentroid(Vector2[] points, float padding)
        {
            if (points == null || points.Length == 0) return Array.Empty<Vector2>();

            Vector2 center = Vector2.zero;
            for (int i = 0; i < points.Length; i++) center += points[i];
            center /= points.Length;

            Vector2[] expanded = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 direction = points[i] - center;
                if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;
                expanded[i] = points[i] + direction.normalized * padding;
            }

            return expanded;
        }

        private static bool ContainsPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool crosses = (a.y > point.y) != (b.y > point.y);
                float denominator = b.y - a.y;
                if (Mathf.Abs(denominator) < 0.0001f) denominator = denominator < 0f ? -0.0001f : 0.0001f;
                if (crosses && point.x < (b.x - a.x) * (point.y - a.y) / denominator + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static bool IsPointNearPolygon(Vector2 point, Vector2[] polygon, float tolerance)
        {
            for (int i = 0; i < polygon.Length; i++)
            {
                if (DistanceToSegment(point, polygon[i], polygon[(i + 1) % polygon.Length]) <= tolerance) return true;
            }

            return false;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(point - a, ab) / Mathf.Max(0.0001f, Vector2.Dot(ab, ab));
            t = Mathf.Clamp01(t);
            return Vector2.Distance(point, a + ab * t);
        }
    }

    public static class HivePixelPerfectContourCalibration
    {
        public const string SchemaVersion = "bee-hive-contour-calibration-v1";
        public const string CoordinateSpace = "reference_hive_art_pixels_1672x941";
        public const string CalibrationSource = "Assets/BeeKingdom/Playground/Data/HiveZoneContourCalibration.v1.json";
        public const int OrganicContourMinimumPoints = 32;

        private static readonly HiveZoneContourDefinition[] Definitions =
        {
            O("administration_core", "Administration", HiveContourPriorityBand.P0, 145, 14f, 676,331, 704,314, 722,305, 748,296, 772,286, 799,294, 824,306, 851,321, 870,339, 884,364, 890,391, 898,421, 903,449, 893,478, 877,504, 861,530, 842,551, 816,559, 790,562, 766,566, 744,565, 718,554, 696,538, 674,520, 663,501, 653,478, 647,454, 641,431, 640,408, 645,386, 653,366, 663,347),
            O("honey_storage", "Reserve miel", HiveContourPriorityBand.P0, 138, 13f, 700,91, 719,83, 742,76, 762,70, 784,66, 806,70, 828,79, 851,91, 872,106, 886,127, 895,152, 900,176, 900,201, 891,225, 877,247, 864,268, 848,282, 823,284, 796,284, 770,284, 744,281, 724,269, 704,251, 688,235, 678,217, 677,194, 682,153, 690,121),
            O("research_node", "Recherche", HiveContourPriorityBand.P0, 136, 13f, 837,498, 860,488, 883,481, 906,475, 929,470, 952,479, 974,492, 996,507, 1016,524, 1021,550, 1020,575, 1018,602, 1012,627, 994,648, 974,665, 953,684, 930,696, 904,690, 878,681, 851,671, 828,654, 814,631, 804,606, 795,582, 794,558, 800,540, 810,526, 822,510),
            O("wax_workshop", "Transformation", HiveContourPriorityBand.P0, 137, 13f, 546,499, 568,486, 591,476, 614,467, 637,460, 661,469, 681,480, 702,494, 721,510, 726,535, 726,560, 724,587, 720,612, 704,632, 684,650, 665,670, 642,681, 616,676, 590,670, 565,660, 543,648, 528,627, 516,604, 508,580, 507,557, 513,540, 522,526, 533,511),
            O("guard_post", "Caserne", HiveContourPriorityBand.P0, 134, 14f, 868,176, 891,165, 914,158, 937,151, 960,147, 984,155, 1008,168, 1031,184, 1052,201, 1060,226, 1062,252, 1060,279, 1055,304, 1039,324, 1020,342, 1002,361, 982,369, 957,366, 932,360, 908,353, 886,342, 869,322, 857,298, 847,274, 843,250, 843,229, 848,210, 856,192),
            D("alliance_future_hall", "Centre alliance", HiveContourPriorityBand.P0, 131, 18f, 702,621, 742,600, 782,586, 824,606, 861,632, 866,676, 861,718, 824,748, 785,768, 742,754, 700,734, 674,701, 665,665, 680,640),
            O("nursery_cluster", "Nurserie", HiveContourPriorityBand.P1, 125, 14f, 504,164, 528,153, 553,144, 578,137, 602,133, 627,142, 651,154, 676,168, 697,184, 708,210, 712,236, 713,262, 708,286, 692,306, 675,321, 657,333, 637,337, 612,334, 586,329, 560,322, 537,312, 518,295, 505,276, 494,257, 489,236, 488,216, 490,198, 496,179),
            O("warehouse_cells", "Entrepot", HiveContourPriorityBand.P1, 123, 13f, 383,333, 409,319, 436,307, 462,297, 488,290, 516,298, 543,310, 571,325, 597,342, 610,370, 617,399, 622,428, 620,456, 606,483, 586,505, 566,527, 542,540, 512,541, 482,538, 452,532, 424,524, 401,504, 381,482, 364,457, 354,430, 355,404, 360,380, 370,354),
            O("genetics_garden", "Genetique", HiveContourPriorityBand.P1, 122, 13f, 967,332, 988,322, 1010,315, 1032,310, 1053,306, 1077,315, 1098,329, 1119,345, 1138,362, 1145,387, 1146,413, 1145,439, 1140,464, 1123,485, 1101,504, 1079,523, 1054,533, 1029,527, 1004,518, 979,509, 956,497, 939,475, 929,451, 919,427, 918,403, 923,383, 936,365, 950,347),
            D("defense_growth", "Defense", HiveContourPriorityBand.P1, 116, 16f, 1116,468, 1154,449, 1192,438, 1229,463, 1260,492, 1262,536, 1251,580, 1214,611, 1170,629, 1130,610, 1094,580, 1086,535),
            D("infirmary_grove", "Infirmerie", HiveContourPriorityBand.P2, 104, 18f, 246,503, 286,484, 327,473, 372,493, 413,520, 423,566, 416,612, 383,648, 343,674, 298,663, 254,640, 225,600, 214,559, 226,528),
            D("academy_canopy", "Academie", HiveContourPriorityBand.P2, 102, 17f, 158,100, 190,72, 252,64, 330,80, 402,130, 448,198, 472,280, 452,370, 396,452, 316,545, 242,520, 188,442, 158,324, 150,204),
            D("hive_bank", "Banque", HiveContourPriorityBand.P2, 103, 18f, 564,675, 601,653, 638,642, 675,660, 708,682, 716,722, 708,760, 673,787, 634,802, 596,785, 560,764, 548,719),
            D("archives_honeyfall", "Archives", HiveContourPriorityBand.P2, 100, 18f, 908,72, 932,74, 958,82, 982,94, 1004,120, 1002,145, 980,157, 950,154, 926,140, 910,110)
        };

        private static readonly Dictionary<string, HiveZoneContourDefinition> DefinitionsById = BuildLookup();

        public static IReadOnlyList<HiveZoneContourDefinition> All => Definitions;

        public static bool TryGet(string hotspotId, out HiveZoneContourDefinition definition)
        {
            if (hotspotId == null)
            {
                definition = null;
                return false;
            }

            return DefinitionsById.TryGetValue(hotspotId, out definition);
        }

        public static bool TryHit(Vector2 artPoint, out HiveZoneContourDefinition definition)
        {
            definition = null;
            int bestPriority = int.MinValue;
            for (int i = 0; i < Definitions.Length; i++)
            {
                HiveZoneContourDefinition candidate = Definitions[i];
                if (candidate.Priority < bestPriority || !candidate.ContainsTactilePoint(artPoint)) continue;
                definition = candidate;
                bestPriority = candidate.Priority;
            }

            return definition != null;
        }

        public static string[] InventoryRows()
        {
            string[] rows = new string[Definitions.Length];
            for (int i = 0; i < Definitions.Length; i++)
            {
                HiveZoneContourDefinition definition = Definitions[i];
                rows[i] = definition.HotspotId + "|" + definition.Label + "|" + definition.PriorityBand + "|visual:" + definition.VisualContour.Length + "|hitbox:" + definition.TactileHitbox.Length + "|priority:" + definition.Priority;
            }

            return rows;
        }

        private static Dictionary<string, HiveZoneContourDefinition> BuildLookup()
        {
            Dictionary<string, HiveZoneContourDefinition> lookup = new Dictionary<string, HiveZoneContourDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < Definitions.Length; i++) lookup[Definitions[i].HotspotId] = Definitions[i];
            return lookup;
        }

        private static HiveZoneContourDefinition D(string hotspotId, string label, HiveContourPriorityBand band, int priority, float tactilePadding, params float[] xy)
        {
            return new HiveZoneContourDefinition(hotspotId, label, band, priority, V(xy), tactilePadding);
        }

        private static HiveZoneContourDefinition O(string hotspotId, string label, HiveContourPriorityBand band, int priority, float tactilePadding, params float[] xy)
        {
            return new HiveZoneContourDefinition(hotspotId, label, band, priority, SmoothClosedContour(V(xy), 2), tactilePadding);
        }

        private static Vector2[] SmoothClosedContour(Vector2[] points, int passes)
        {
            if (points == null || points.Length < 3) return points ?? Array.Empty<Vector2>();

            Vector2[] smoothed = points;
            for (int pass = 0; pass < passes; pass++)
            {
                Vector2[] next = new Vector2[smoothed.Length * 2];
                for (int i = 0; i < smoothed.Length; i++)
                {
                    Vector2 current = smoothed[i];
                    Vector2 following = smoothed[(i + 1) % smoothed.Length];
                    next[i * 2] = Vector2.Lerp(current, following, 0.25f);
                    next[i * 2 + 1] = Vector2.Lerp(current, following, 0.75f);
                }

                smoothed = next;
            }

            return smoothed;
        }

        private static Vector2[] V(params float[] xy)
        {
            if (xy == null || xy.Length % 2 != 0) throw new ArgumentException("Contour coordinates must be x/y pairs.");
            Vector2[] points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return points;
        }
    }
}
