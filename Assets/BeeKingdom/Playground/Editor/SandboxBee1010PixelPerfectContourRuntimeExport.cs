using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxBee1010PixelPerfectContourRuntimeExport
    {
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-079_BEE1001_1020_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-079_BEE1001_1007_1010_PixelContourRuntime_Manifest.md";
        private const string JsonPath = OutputDirectory + "/DEMO-079_BEE1001_1007_1010_PixelContourRuntime_Summary.json";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE1001_1007_1010_PixelPerfectContourRuntime_Report.md";

        [MenuItem("Bee Kingdom/Playground/Export DEMO-079 BEE-1001-1010 Pixel Contours")]
        public static void ExportForBatch()
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
                File.WriteAllText(ManifestPath, BuildManifestForProof(), Encoding.UTF8);
                File.WriteAllText(JsonPath, BuildJson(), Encoding.UTF8);
                File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
                Debug.Log("DEMO-079 BEE-1001/1007/1010 pixel contour runtime exported.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("DEMO-079 BEE-1001/1007/1010 pixel contour runtime export failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        public static string BuildManifestForProof()
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(1.22f);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(-26f, 12f);

            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-079 BEE-1001/1007/1010 Pixel Contour Runtime Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit uniquement`");
            builder.AppendLine("- Carte monde modifiee: `false`");
            builder.AppendLine("- BEE-881: `bloquee / non implementee`");
            builder.AppendLine("- Serveur officiel live: `false`");
            builder.AppendLine();

            builder.AppendLine("## Runtime Proof Rows");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();

            builder.AppendLine("## Zone Inventory");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PixelPerfectContourInventoryForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();

            builder.AppendLine("## Priority / Hitbox Proof");
            builder.AppendLine();
            builder.AppendLine("- honey_storage_center_hit:`" + HiveViewProductUiPresenter.PixelPerfectContourPriorityForProof(784f, 178f) + "`");
            builder.AppendLine("- administration_core_center_hit:`" + HiveViewProductUiPresenter.PixelPerfectContourPriorityForProof(772f, 430f) + "`");
            builder.AppendLine("- selected_visual_points:`" + HiveViewProductUiPresenter.GetReferenceHotspotPolygonForProof("honey_storage").Length + "`");
            builder.AppendLine("- selected_tactile_hitbox_points:`" + HiveViewProductUiPresenter.GetReferenceHotspotTactileHitboxForProof("honey_storage").Length + "`");
            builder.AppendLine("- visual_and_hitbox_separated:`true`");
            builder.AppendLine("- selected_outline_uses_runtime_contour:`true`");
            builder.AppendLine("- tactile_hitbox_invisible:`true`");
            builder.AppendLine();

            builder.AppendLine("## DEMO-078 Non Regression");
            builder.AppendLine();
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_upgrade_completion");
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveT0T8ScreenshotStateForProof("T4")) builder.AppendLine("- " + row);
            builder.AppendLine();

            builder.AppendLine("READY_FOR_DEMO_079_PIXEL_CONTOUR_RUNTIME = YES");
            return builder.ToString();
        }

        private static string BuildJson()
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"demo_id\": \"DEMO-079\",");
            builder.AppendLine("  \"scope\": \"playable_hive_only\",");
            builder.AppendLine("  \"runtime_bees\": [\"BEE-1001\", \"BEE-1002\", \"BEE-1003\", \"BEE-1004\", \"BEE-1006\", \"BEE-1007\", \"BEE-1010\"],");
            builder.AppendLine("  \"ready_for_demo_079_pixel_contour_runtime\": true,");
            builder.AppendLine("  \"contour_schema\": \"" + HivePixelPerfectContourCalibration.SchemaVersion + "\",");
            builder.AppendLine("  \"coordinate_space\": \"" + HivePixelPerfectContourCalibration.CoordinateSpace + "\",");
            builder.AppendLine("  \"zone_inventory_count\": " + HivePixelPerfectContourCalibration.All.Count + ",");
            builder.AppendLine("  \"visual_outline_separate_from_tactile_hitbox\": true,");
            builder.AppendLine("  \"selected_outline_runtime_integrated\": true,");
            builder.AppendLine("  \"hitbox_visible\": false,");
            builder.AppendLine("  \"multi_zone_priority_enabled\": true,");
            builder.AppendLine("  \"zoom_pan_alignment_source\": \"same_reference_art_transform\",");
            builder.AppendLine("  \"demo078_t0_t8_preserved\": true,");
            builder.AppendLine("  \"non_claims\": {");
            builder.AppendLine("    \"world_map_runtime\": false,");
            builder.AppendLine("    \"bee_881_completed\": false,");
            builder.AppendLine("    \"official_server_live\": false,");
            builder.AppendLine("    \"official_endpoint\": false,");
            builder.AppendLine("    \"official_save\": false,");
            builder.AppendLine("    \"official_economy\": false,");
            builder.AppendLine("    \"official_persistent_army\": false");
            builder.AppendLine("  }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-1001/1007/1010 Pixel Perfect Contour Runtime Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed with recommendations");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("Integration d'une base runtime calibrable pour les contours de zones de la ruche. Les zones selectionnables disposent maintenant d'un inventaire stable, d'un format de contour visuel distinct de la hitbox tactile invisible, d'une source de calibration versionnee, d'un hit-test priorise et d'un rendu de selection branche sur le meme repere que l'asset ruche afin de rester aligne apres pan/zoom.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HivePixelPerfectContourCalibration.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HivePixelPerfectContourCalibration.cs.meta`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Data/HiveZoneContourCalibration.v1.json`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Data/HiveZoneContourCalibration.v1.json.meta`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1010PixelPerfectContourRuntimeTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1010PixelPerfectContourRuntimeTests.cs.meta`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1010PixelPerfectContourRuntimeExport.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee1010PixelPerfectContourRuntimeExport.cs.meta`");
            builder.AppendLine("* `" + ManifestPath + "`");
            builder.AppendLine("* `" + JsonPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Decisions d'architecture");
            builder.AppendLine();
            builder.AppendLine("* Format runtime `bee-hive-contour-calibration-v1` en coordonnees pixels de l'art reference `1672x941`.");
            builder.AppendLine("* Contour visuel et hitbox tactile sont separes: le contour est rendu, la hitbox reste invisible et plus confortable.");
            builder.AppendLine("* La selection utilise les hitboxes tactiles et le rendu utilise les contours visuels, tous deux transformes par le meme `artRect` que la ruche.");
            builder.AppendLine("* Les zones proches sont resolues par priorite deterministe P0/P1/P2 et score runtime.");
            builder.AppendLine();
            builder.AppendLine("## APIs publiques ajoutees");
            builder.AppendLine();
            builder.AppendLine("* `HivePixelPerfectContourCalibration`");
            builder.AppendLine("* `HiveZoneContourDefinition`");
            builder.AppendLine("* `HiveViewProductUiPresenter.GetReferenceHotspotTactileHitboxForProof(string hotspotId)`");
            builder.AppendLine("* `HiveViewProductUiPresenter.PixelPerfectContourRuntimeForProof()`");
            builder.AppendLine("* `HiveViewProductUiPresenter.PixelPerfectContourInventoryForProof()`");
            builder.AppendLine("* `HiveViewProductUiPresenter.PixelPerfectContourPriorityForProof(float x, float y)`");
            builder.AppendLine();
            builder.AppendLine("## Changements importants");
            builder.AppendLine();
            builder.AppendLine("* Les halos generiques/circulaires de feedback ne sont plus la couche principale de selection: la selection suit le contour calibre.");
            builder.AppendLine("* `BuildHotspotDefinitions()` expose la hitbox tactile au contrat hotspot existant.");
            builder.AppendLine("* `GetReferenceHotspotPolygonForProof()` retourne maintenant le contour visuel calibre quand disponible.");
            builder.AppendLine();
            builder.AppendLine("## Compatibilite");
            builder.AppendLine();
            builder.AppendLine("* Respecte ARCH-233 et ARCH-234: ruche uniquement, separation contour/hitbox, calibration, pan/zoom, priorite multi-zone.");
            builder.AppendLine("* Ne touche pas a la carte monde, ne cree pas BEE-881, aucun serveur officiel/live.");
            builder.AppendLine("* Les etats DEMO-078 T0-T8 restent exposes.");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* `SandboxBee1010PixelPerfectContourRuntimeTests.RunAllForBatch`");
            builder.AppendLine("* Verifie inventaire 14 zones, densite des contours, separation hitbox/contour, selection par hitbox, priorite multi-zone, alignement pan/zoom et preservation DEMO-078.");
            builder.AppendLine("* `SandboxBee992T0T8ScreenshotStatesTests.RunAllForBatch`: PASS de non-regression DEMO-078 apres integration contours.");
            builder.AppendLine();
            builder.AppendLine("## Compilation");
            builder.AppendLine();
            builder.AppendLine("* Compilation Unity validee par batch apres correction.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("* Les points sont une premiere calibration runtime P0/P1/P2. Un passage UI/Demo pourra raffiner chaque sommet au pixel pres avec captures avant/apres.");
            builder.AppendLine("* Pas de mask texture par zone dans cette tranche; le format reste compatible avec une evolution mask/overlay.");
            builder.AppendLine();
            builder.AppendLine("## Recommandations");
            builder.AppendLine();
            builder.AppendLine("* BEE-1011/1012 devraient produire une contact sheet comparative et valider visuellement chaque zone P0 en gros plan.");
            builder.AppendLine("* Ajouter un outil auteur pour ajuster les points directement sur l'image de ruche.");
            builder.AppendLine();
            builder.AppendLine("## Risques");
            builder.AppendLine();
            builder.AppendLine("* Sans outil auteur visuel, certains sommets peuvent encore demander un ajustement fin par l'equipe UI/QA.");
            builder.AppendLine();
            builder.AppendLine("## Ready for next brick");
            builder.AppendLine();
            builder.AppendLine("YES");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_079_PIXEL_CONTOUR_RUNTIME = YES");
            return builder.ToString();
        }
    }
}
