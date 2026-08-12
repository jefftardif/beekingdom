using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxBee1024VisualContourSourceValidator
    {
        private const string SourceDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-079_BEE1001_1020_Source/VisualContourSource";
        private const string SourceJsonPath = SourceDirectory + "/HiveVisualContourSource.v1.json";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-079_BEE1001_1020_Source/VisualContourSourceValidation";
        private const string ValidationReportPath = OutputDirectory + "/BEE-1024_VisualContourSourceValidation.md";
        private const string ValidationJsonPath = OutputDirectory + "/BEE-1024_VisualContourSourceValidation.json";
        private const string TemplatePath = OutputDirectory + "/HiveVisualContourSource.v1.template.json";

        private const int ReferenceWidth = 1672;
        private const int ReferenceHeight = 941;
        private const int MinimumOrganicPoints = 32;
        private const float MaxClosureDistancePixels = 3f;
        private const float GenericCircularityTolerance = 0.16f;
        private const float GenericRadiusVariationTolerance = 0.10f;
        private const float GenericAngleStepTolerance = 0.34f;

        private static readonly string[] RequiredPriorityZoneIds =
        {
            "warehouse_cells",
            "wax_workshop",
            "administration_core",
            "honey_storage",
            "nursery_cluster",
            "guard_post",
            "research_node",
            "genetics_garden"
        };

        private static readonly Dictionary<string, string> OfficialDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "warehouse_cells", "Entrepot" },
            { "wax_workshop", "Transformation" },
            { "administration_core", "Administration" },
            { "honey_storage", "ReserveMiel" },
            { "nursery_cluster", "Nurserie" },
            { "guard_post", "Caserne" },
            { "research_node", "Recherche" },
            { "genetics_garden", "Genetique" }
        };

        [MenuItem("Bee Kingdom/Playground/Validate BEE-1024 Visual Contour Source")]
        public static void ValidateForBatch()
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);
                File.WriteAllText(TemplatePath, BuildTemplateJson(), Encoding.UTF8);

                ValidationResult result = ValidateSource();
                File.WriteAllText(ValidationReportPath, BuildReport(result), Encoding.UTF8);
                File.WriteAllText(ValidationJsonPath, BuildJson(result), Encoding.UTF8);

                Debug.Log("BEE-1024 visual contour source validator completed. Source ready: " + result.SourceReadyForRuntimeComparison);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-1024 visual contour source validator failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static ValidationResult ValidateSource()
        {
            ValidationResult result = new ValidationResult
            {
                SourceJsonPath = SourceJsonPath,
                SourceExists = File.Exists(SourceJsonPath),
                ExpectedCoordinateSpace = "reference_hive_art_pixels_1672x941",
                ExpectedReferenceWidth = ReferenceWidth,
                ExpectedReferenceHeight = ReferenceHeight
            };

            result.Notes.Add("NativeAfter reste un dossier de recapture technique; il ne doit pas etre considere QA-ready sans source visuelle UI-B.");

            if (!result.SourceExists)
            {
                result.Errors.Add("SOURCE_VISUELLE_UI_B_ABSENTE: fichier attendu manquant: " + SourceJsonPath);
                return result;
            }

            VisualContourSource source = JsonUtility.FromJson<VisualContourSource>(File.ReadAllText(SourceJsonPath, Encoding.UTF8));
            if (source == null)
            {
                result.Errors.Add("SOURCE_JSON_INVALID: JsonUtility n'a pas pu lire la source.");
                return result;
            }

            result.Schema = source.schema ?? string.Empty;
            result.AuthoringTool = source.authoringTool ?? string.Empty;
            result.SourceVisualFile = source.sourceVisualFile ?? string.Empty;
            result.PathCount = source.paths == null ? 0 : source.paths.Length;

            if (!StringEquals(source.coordinateSpace, result.ExpectedCoordinateSpace)) result.Errors.Add("COORDINATE_SPACE_INVALID: attendu " + result.ExpectedCoordinateSpace);
            if (source.sourceImageWidth != ReferenceWidth || source.sourceImageHeight != ReferenceHeight) result.Errors.Add("REFERENCE_SIZE_INVALID: attendu 1672x941.");
            if (string.IsNullOrWhiteSpace(source.authoringTool)) result.Errors.Add("AUTHORING_TOOL_MISSING: Inkscape/Figma/Illustrator/etc. requis.");
            if (string.IsNullOrWhiteSpace(source.sourceVisualFile)) result.Errors.Add("SOURCE_VISUAL_FILE_MISSING: SVG/JSON auteur attendu.");

            Dictionary<string, VisualContourPath> pathsByZone = new Dictionary<string, VisualContourPath>(StringComparer.Ordinal);
            foreach (VisualContourPath path in source.paths ?? Array.Empty<VisualContourPath>())
            {
                if (path == null || string.IsNullOrWhiteSpace(path.zoneId))
                {
                    result.Errors.Add("ZONE_ID_MISSING: un path n'a pas de zoneId.");
                    continue;
                }

                if (pathsByZone.ContainsKey(path.zoneId))
                {
                    result.Errors.Add("DUPLICATE_ZONE_PATH: " + path.zoneId);
                    continue;
                }

                pathsByZone[path.zoneId] = path;
            }

            foreach (string required in RequiredPriorityZoneIds)
            {
                if (!pathsByZone.TryGetValue(required, out VisualContourPath path))
                {
                    result.Errors.Add("REQUIRED_ZONE_MISSING: " + required + " / " + OfficialDisplayNames[required]);
                    continue;
                }

                ValidatePath(required, path, result);
            }

            foreach (string zoneId in pathsByZone.Keys)
            {
                if (!OfficialDisplayNames.ContainsKey(zoneId)) result.Errors.Add("UNKNOWN_ZONE_ID: " + zoneId);
            }

            result.SourceReadyForRuntimeComparison = result.Errors.Count == 0;
            return result;
        }

        private static void ValidatePath(string zoneId, VisualContourPath path, ValidationResult result)
        {
            string prefix = zoneId + ": ";
            if (!StringEquals(path.displayName, OfficialDisplayNames[zoneId])) result.Errors.Add(prefix + "DISPLAY_NAME_INVALID attendu " + OfficialDisplayNames[zoneId]);
            if (string.IsNullOrWhiteSpace(path.sourcePathId)) result.Errors.Add(prefix + "SOURCE_PATH_ID_MISSING");
            if (path.points == null || path.points.Length < MinimumOrganicPoints)
            {
                result.Errors.Add(prefix + "POINT_COUNT_TOO_LOW attendu >= " + MinimumOrganicPoints.ToString(CultureInfo.InvariantCulture));
                return;
            }

            for (int i = 0; i < path.points.Length; i++)
            {
                Point2 point = path.points[i];
                if (point.x < 0f || point.x > ReferenceWidth || point.y < 0f || point.y > ReferenceHeight)
                {
                    result.Errors.Add(prefix + "POINT_OUT_OF_REFERENCE_IMAGE index " + i.ToString(CultureInfo.InvariantCulture));
                }
            }

            float closure = Distance(path.points[0], path.points[path.points.Length - 1]);
            if (!path.closed && closure > MaxClosureDistancePixels) result.Errors.Add(prefix + "PATH_NOT_CLOSED distance " + closure.ToString("0.###", CultureInfo.InvariantCulture));
            if (LooksGeneric(path.points)) result.Errors.Add(prefix + "GENERIC_CONTOUR_SUSPECT cercle/ellipse/hexagone/polygone uniforme probable.");

            result.ValidatedZones.Add(zoneId + "|points:" + path.points.Length.ToString(CultureInfo.InvariantCulture) + "|closure:" + closure.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static bool LooksGeneric(Point2[] points)
        {
            if (points == null || points.Length < MinimumOrganicPoints) return true;

            float minX = points.Min(p => p.x);
            float maxX = points.Max(p => p.x);
            float minY = points.Min(p => p.y);
            float maxY = points.Max(p => p.y);
            float width = maxX - minX;
            float height = maxY - minY;
            if (width <= 1f || height <= 1f) return true;

            float aspect = width / height;
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;
            float[] radii = points.Select(p => Mathf.Sqrt((p.x - cx) * (p.x - cx) + (p.y - cy) * (p.y - cy))).ToArray();
            float averageRadius = radii.Average();
            float radiusVariation = averageRadius <= 0f ? 0f : (radii.Max() - radii.Min()) / averageRadius;
            if (Mathf.Abs(aspect - 1f) <= GenericCircularityTolerance && radiusVariation <= GenericRadiusVariationTolerance) return true;

            List<float> angles = points.Select(p => Mathf.Atan2(p.y - cy, p.x - cx)).OrderBy(a => a).ToList();
            List<float> deltas = new List<float>();
            for (int i = 0; i < angles.Count; i++)
            {
                float current = angles[i];
                float next = i == angles.Count - 1 ? angles[0] + Mathf.PI * 2f : angles[i + 1];
                deltas.Add(next - current);
            }

            float averageDelta = deltas.Average();
            float maxDeviation = deltas.Max(d => Mathf.Abs(d - averageDelta));
            return maxDeviation <= GenericAngleStepTolerance * averageDelta && radiusVariation <= 0.18f;
        }

        private static float Distance(Point2 a, Point2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static bool StringEquals(string a, string b)
        {
            return string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.Ordinal);
        }

        private static string BuildTemplateJson()
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"schema\": \"bee-hive-visual-contour-source-v1\",");
            builder.AppendLine("  \"coordinateSpace\": \"reference_hive_art_pixels_1672x941\",");
            builder.AppendLine("  \"sourceImageWidth\": 1672,");
            builder.AppendLine("  \"sourceImageHeight\": 941,");
            builder.AppendLine("  \"authoringTool\": \"Inkscape\",");
            builder.AppendLine("  \"sourceVisualFile\": \"HiveVisualContourSource.v1.svg\",");
            builder.AppendLine("  \"paths\": [");
            for (int i = 0; i < RequiredPriorityZoneIds.Length; i++)
            {
                string zoneId = RequiredPriorityZoneIds[i];
                builder.AppendLine("    {");
                builder.AppendLine("      \"zoneId\": \"" + zoneId + "\",");
                builder.AppendLine("      \"displayName\": \"" + OfficialDisplayNames[zoneId] + "\",");
                builder.AppendLine("      \"sourcePathId\": \"path-" + zoneId + "\",");
                builder.AppendLine("      \"closed\": true,");
                builder.AppendLine("      \"points\": [");
                builder.AppendLine("        { \"x\": 0, \"y\": 0 },");
                builder.AppendLine("        { \"x\": 1, \"y\": 1 }");
                builder.AppendLine("      ]");
                builder.Append("    }");
                builder.AppendLine(i == RequiredPriorityZoneIds.Length - 1 ? string.Empty : ",");
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildReport(ValidationResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-1024 Visual Contour Source Validation");
            builder.AppendLine();
            builder.AppendLine("- Scope: `playable_hive_only`");
            builder.AppendLine("- Source expected: `" + SourceJsonPath + "`");
            builder.AppendLine("- Template generated: `" + TemplatePath + "`");
            builder.AppendLine("- NativeAfter kept ready: `true`");
            builder.AppendLine("- NativeAfter QA-ready without UI-B visual source: `false`");
            builder.AppendLine("- External post-produced overlay accepted as final proof: `false`");
            builder.AppendLine("- World map touched: `false`");
            builder.AppendLine("- BEE-881 created or unlocked: `false`");
            builder.AppendLine("- Official server/live: `false`");
            builder.AppendLine();
            builder.AppendLine("## Source Status");
            builder.AppendLine();
            builder.AppendLine("- source_exists: `" + result.SourceExists + "`");
            builder.AppendLine("- source_ready_for_runtime_comparison: `" + result.SourceReadyForRuntimeComparison + "`");
            builder.AppendLine("- schema: `" + result.Schema + "`");
            builder.AppendLine("- authoring_tool: `" + result.AuthoringTool + "`");
            builder.AppendLine("- source_visual_file: `" + result.SourceVisualFile + "`");
            builder.AppendLine("- path_count: `" + result.PathCount.ToString(CultureInfo.InvariantCulture) + "`");
            builder.AppendLine();
            builder.AppendLine("## Required Zones");
            builder.AppendLine();
            foreach (string zoneId in RequiredPriorityZoneIds) builder.AppendLine("- `" + zoneId + "` / `" + OfficialDisplayNames[zoneId] + "`");
            builder.AppendLine();
            builder.AppendLine("## Validation Rules");
            builder.AppendLine();
            builder.AppendLine("- zoneId doit correspondre a la liste officielle.");
            builder.AppendLine("- displayName doit correspondre au nom produit attendu.");
            builder.AppendLine("- un seul path par zone prioritaire.");
            builder.AppendLine("- points dans le repere image 1672x941.");
            builder.AppendLine("- forme fermee ou dernier point a moins de 3 px du premier.");
            builder.AppendLine("- au moins 32 points par contour prioritaire.");
            builder.AppendLine("- detection de forme generique suspecte: cercle/ellipse reguliere/polygone uniforme.");
            builder.AppendLine("- hitbox tactile non validee ici: elle reste separee et invisible cote runtime.");
            builder.AppendLine();
            builder.AppendLine("## Comparison Method");
            builder.AppendLine();
            builder.AppendLine("1. UI-B livre SVG ou JSON auteur dans `VisualContourSource`.");
            builder.AppendLine("2. Builder-B lance ce validateur et obtient `source_ready_for_runtime_comparison:true`.");
            builder.AppendLine("3. Builder-A importe les paths valides dans le runtime ruche.");
            builder.AppendLine("4. Builder-B relance les captures NativeAfter.");
            builder.AppendLine("5. Demo/QA comparent source visuelle et rendu Unity natif par zone: meme repere 1672x941, meme zone, contour visible vs path auteur.");
            builder.AppendLine("6. Les captures finales restent bloquees si le rendu Unity derive du path source ou s'il revient a une enveloppe mathematique.");
            builder.AppendLine();
            builder.AppendLine("## Errors");
            builder.AppendLine();
            if (result.Errors.Count == 0) builder.AppendLine("- none");
            foreach (string error in result.Errors) builder.AppendLine("- " + error);
            builder.AppendLine();
            builder.AppendLine("## Validated Zones");
            builder.AppendLine();
            if (result.ValidatedZones.Count == 0) builder.AppendLine("- none");
            foreach (string zone in result.ValidatedZones) builder.AppendLine("- " + zone);
            builder.AppendLine();
            builder.AppendLine("READY_FOR_VISUAL_CONTOUR_SOURCE_VALIDATION = YES");
            builder.AppendLine("VISUAL_CONTOUR_SOURCE_ACCEPTED = " + (result.SourceReadyForRuntimeComparison ? "YES" : "NO"));
            builder.AppendLine("READY_FOR_QA_PIXEL_CONTOURS = NO");
            return builder.ToString();
        }

        private static string BuildJson(ValidationResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"schema\": \"bee-kingdom.bee1024.visual-contour-source-validation.v1\",");
            builder.AppendLine("  \"readyForVisualContourSourceValidation\": true,");
            builder.AppendLine("  \"visualContourSourceAccepted\": " + JsonBool(result.SourceReadyForRuntimeComparison) + ",");
            builder.AppendLine("  \"readyForQaPixelContours\": false,");
            builder.AppendLine("  \"sourceExists\": " + JsonBool(result.SourceExists) + ",");
            builder.AppendLine("  \"sourceJsonPath\": \"" + JsonEscape(result.SourceJsonPath) + "\",");
            builder.AppendLine("  \"sourceReadyForRuntimeComparison\": " + JsonBool(result.SourceReadyForRuntimeComparison) + ",");
            builder.AppendLine("  \"requiredZoneCount\": " + RequiredPriorityZoneIds.Length.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"pathCount\": " + result.PathCount.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"nativeAfterKeptReady\": true,");
            builder.AppendLine("  \"nativeAfterQaReadyWithoutUiBSource\": false,");
            builder.AppendLine("  \"nonClaims\": {");
            builder.AppendLine("    \"worldMapRuntime\": false,");
            builder.AppendLine("    \"bee881CreatedOrUnlocked\": false,");
            builder.AppendLine("    \"officialServerLive\": false");
            builder.AppendLine("  },");
            builder.AppendLine("  \"errors\": [");
            for (int i = 0; i < result.Errors.Count; i++)
            {
                builder.Append("    \"" + JsonEscape(result.Errors[i]) + "\"");
                builder.AppendLine(i == result.Errors.Count - 1 ? string.Empty : ",");
            }
            builder.AppendLine("  ],");
            builder.AppendLine("  \"validatedZones\": [");
            for (int i = 0; i < result.ValidatedZones.Count; i++)
            {
                builder.Append("    \"" + JsonEscape(result.ValidatedZones[i]) + "\"");
                builder.AppendLine(i == result.ValidatedZones.Count - 1 ? string.Empty : ",");
            }
            builder.AppendLine("  ]");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string JsonEscape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        [Serializable]
        private sealed class VisualContourSource
        {
            public string schema;
            public string coordinateSpace;
            public int sourceImageWidth;
            public int sourceImageHeight;
            public string authoringTool;
            public string sourceVisualFile;
            public VisualContourPath[] paths;
        }

        [Serializable]
        private sealed class VisualContourPath
        {
            public string zoneId;
            public string displayName;
            public string sourcePathId;
            public bool closed;
            public Point2[] points;
        }

        [Serializable]
        private sealed class Point2
        {
            public float x;
            public float y;
        }

        private sealed class ValidationResult
        {
            public string SourceJsonPath;
            public bool SourceExists;
            public bool SourceReadyForRuntimeComparison;
            public string Schema = string.Empty;
            public string AuthoringTool = string.Empty;
            public string SourceVisualFile = string.Empty;
            public string ExpectedCoordinateSpace = string.Empty;
            public int ExpectedReferenceWidth;
            public int ExpectedReferenceHeight;
            public int PathCount;
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Notes = new List<string>();
            public readonly List<string> ValidatedZones = new List<string>();
        }
    }
}
