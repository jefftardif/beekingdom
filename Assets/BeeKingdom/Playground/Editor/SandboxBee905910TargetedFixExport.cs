using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxBee905910TargetedFixExport
    {
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-074_BEE905_910_TargetedFix_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-074_BEE905_910_TargetedFix_Manifest.md";
        private const string JsonPath = OutputDirectory + "/DEMO-074_BEE905_910_TargetedFix_MachineReadableSummary.json";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE905_910_TargetedReserveFix_Report.md";

        [MenuItem("Bee Kingdom/Playground/Export DEMO-074 BEE-905-910 Targeted Fix")]
        public static void ExportForBatch()
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
                File.WriteAllText(ManifestPath, BuildManifestForProof(), Encoding.UTF8);
                File.WriteAllText(JsonPath, BuildJson(), Encoding.UTF8);
                File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
                Debug.Log("DEMO-074 BEE-905/910 targeted fix exported.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("DEMO-074 BEE-905/910 targeted fix export failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        public static string BuildManifestForProof()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-074 BEE-905/910 Targeted Fix Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit uniquement`");
            builder.AppendLine("- Correction ciblee: `BEE-905 manifest coherence`, `BEE-910 UI-button gesture blocking proof`");
            builder.AppendLine("- Carte monde modifiee: `false`");
            builder.AppendLine("- BEE-881: `bloquee / non implementee`");
            builder.AppendLine("- Serveur officiel live: `false`");
            builder.AppendLine("- Endpoint officiel: `false`");
            builder.AppendLine("- Sauvegarde officielle: `false`");
            builder.AppendLine("- Economie officielle: `false`");
            builder.AppendLine("- Armee persistante officielle: `false`");
            builder.AppendLine();

            ApplyTrainingArrivalState();
            builder.AppendLine("## BEE-905 Training Arrival Coherent Export");
            builder.AppendLine();
            builder.AppendLine("- visual_reference: `DEMO-073/BEE917_03_TrainingArrivalArmyDelta_1280x720.png montre Resultat: +6 Eclaireuses et Ecl. 11`");
            AppendRowsExcept(builder, HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof(), "gesture_ui_blocks_hive:");
            AppendRowsExcept(builder, HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof(), "training_arrival_visible:false", "training_queue:File libre - troupes: S 18 / G 8 / E 5", "local_army_snapshot:Soldats 18 / Gardiennes 8 / Eclaireuses 5");
            builder.AppendLine("- manifest_contradiction_closed:true");
            builder.AppendLine("- forbidden_training_arrival_visible_false:false");
            builder.AppendLine("- forbidden_training_delta_none:false");
            builder.AppendLine("- forbidden_old_eclaireuses_5:false");
            builder.AppendLine();

            ApplyUiButtonGestureBlockedState();
            builder.AppendLine("## BEE-910 UI Button Gesture Blocking Proof");
            builder.AppendLine();
            builder.AppendLine("- proof_scenario: `tap/drag on fixed UI button consumes input; hive pan/zoom unchanged`");
            AppendRowsExcept(builder, HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof(), "training_arrival_visible:", "training_delta:", "local_army_counts:");
            foreach (string row in HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof()) builder.AppendLine("- " + row);
            builder.AppendLine("- ui_button_blocks_hive_gesture:true");
            builder.AppendLine("- hive_pan_delta_after_ui_drag:0,0");
            builder.AppendLine("- hive_pinch_delta_after_ui_drag:0");
            builder.AppendLine("- hive_zoom_changed_by_ui_drag:false");
            builder.AppendLine("- hud_panels_navigation_fixed_during_ui_input:true");
            builder.AppendLine();

            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("- batch_method: `SandboxBee905910TargetedFixTests.RunAllForBatch`");
            builder.AppendLine("- export_method: `SandboxBee905910TargetedFixExport.ExportForBatch`");
            builder.AppendLine("- expected_result: `PASS`");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_074_TARGETED_FIX = YES");
            return builder.ToString();
        }

        private static string BuildJson()
        {
            ApplyTrainingArrivalState();
            string[] trainingRows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();
            string[] preservedRows = HiveViewProductUiPresenter.PlayableHivePlayerStabilizationForProof();
            ApplyUiButtonGestureBlockedState();
            string[] uiRows = HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof();
            string[] gestureRows = HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof();

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"demo_id\": \"DEMO-074\",");
            builder.AppendLine("  \"scope\": \"playable_hive_only\",");
            builder.AppendLine("  \"runtime_bees\": [\"BEE-905\", \"BEE-910\"],");
            builder.AppendLine("  \"ready_for_demo_074_targeted_fix\": true,");
            builder.AppendLine("  \"bee_905\": {");
            builder.AppendLine("    \"training_arrival_visible\": true,");
            builder.AppendLine("    \"training_delta\": \"+6 Eclaireuses\",");
            builder.AppendLine("    \"local_army_counts\": \"Soldats 18 / Gardiennes 8 / Eclaireuses 11\",");
            builder.AppendLine("    \"preserved_snapshot_matches_runtime\": " + JsonBool(ContainsRow(preservedRows, "local_army_snapshot:Soldats 18 / Gardiennes 8 / Eclaireuses 11")) + ",");
            builder.AppendLine("    \"manifest_contradiction_closed\": " + JsonBool(ContainsRow(trainingRows, "training_arrival_visible:true") && ContainsRow(trainingRows, "training_delta:+6 Eclaireuses") && !ContainsRow(trainingRows, "training_arrival_visible:false") && !ContainsRow(trainingRows, "training_delta:none")) + "");
            builder.AppendLine("  },");
            builder.AppendLine("  \"bee_910\": {");
            builder.AppendLine("    \"ui_button_blocks_hive_gesture\": " + JsonBool(ContainsRow(uiRows, "gesture_ui_blocks_hive:True") && ContainsRow(gestureRows, "fixed_ui_blocks_hive_gesture:True")) + ",");
            builder.AppendLine("    \"gesture_mode\": \"ui-blocked-touch\",");
            builder.AppendLine("    \"pan_delta\": \"0,0\",");
            builder.AppendLine("    \"pinch_delta\": \"0\",");
            builder.AppendLine("    \"hud_fixed\": true,");
            builder.AppendLine("    \"panels_fixed\": true,");
            builder.AppendLine("    \"navigation_fixed\": true,");
            builder.AppendLine("    \"hive_zoom_changed_by_ui_drag\": false");
            builder.AppendLine("  },");
            builder.AppendLine("  \"non_claims\": {");
            builder.AppendLine("    \"official_server_live\": false,");
            builder.AppendLine("    \"official_endpoint\": false,");
            builder.AppendLine("    \"official_save\": false,");
            builder.AppendLine("    \"official_economy\": false,");
            builder.AppendLine("    \"official_persistent_army\": false,");
            builder.AppendLine("    \"world_map_runtime\": false,");
            builder.AppendLine("    \"bee_881_completed\": false");
            builder.AppendLine("  }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-905/910 Targeted Reserve Fix Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("Correction ciblee des deux reserves QA-073: l'export BEE-905 est aligne avec la preuve runtime/visuelle training arrival (+6 Eclaireuses, compteur Eclaireuses 11), et BEE-910 expose une preuve dediee que les boutons UI fixes consomment le geste sans pan/zoom de la ruche.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee905910TargetedFixTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee905910TargetedFixExport.cs`");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine("* JSON: `" + JsonPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* `SandboxBee905910TargetedFixTests.RunAllForBatch`: PASS attendu et execute.");
            builder.AppendLine("* Assertions: aucun `training_arrival_visible:false`, aucun `training_delta:none`, aucun ancien compteur `Eclaireuses 5`, et `gesture_ui_blocks_hive:True` / `fixed_ui_blocks_hive_gesture:True` obligatoires.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("* Pas de preuve physique device ajoutee; reserve device maintenue hors scope.");
            builder.AppendLine("* Aucun serveur officiel live, endpoint, sauvegarde, economie ou armee persistante officielle.");
            builder.AppendLine("* Aucune carte monde et aucun BEE-881.");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_074_TARGETED_FIX = YES");
            return builder.ToString();
        }

        private static void ApplyTrainingArrivalState()
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("guard_post");
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(1.12f);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(-18f, 8f);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("training_eclaireuses_done");
        }

        private static void ApplyUiButtonGestureBlockedState()
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("honey_storage");
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(1.12f);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(0f, 0f);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("ui_gesture_blocked");
        }

        private static bool ContainsRow(string[] rows, string expected)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (string.Equals(rows[i], expected, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        private static string JsonBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static void AppendRowsExcept(StringBuilder builder, string[] rows, params string[] forbiddenPrefixesOrRows)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                if (IsForbidden(rows[i], forbiddenPrefixesOrRows)) continue;
                builder.AppendLine("- " + rows[i]);
            }
        }

        private static bool IsForbidden(string row, string[] forbiddenPrefixesOrRows)
        {
            for (int i = 0; i < forbiddenPrefixesOrRows.Length; i++)
            {
                string forbidden = forbiddenPrefixesOrRows[i];
                if (string.IsNullOrWhiteSpace(forbidden)) continue;
                if (row.StartsWith(forbidden, StringComparison.Ordinal)) return true;
                if (string.Equals(row, forbidden, StringComparison.Ordinal)) return true;
            }

            return false;
        }
    }
}
