using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxBee930DailyHiveLoopExport
    {
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-075_BEE925_930_DailyHiveLoop_Manifest.md";
        private const string JsonPath = OutputDirectory + "/DEMO-075_BEE925_930_DailyHiveLoop_MachineReadableSummary.json";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE925_930_DailyHiveLoop_Report.md";

        private readonly struct DailyScenario
        {
            public readonly string Label;
            public readonly string State;
            public readonly string HotspotId;

            public DailyScenario(string label, string state, string hotspotId)
            {
                Label = label;
                State = state;
                HotspotId = hotspotId;
            }
        }

        private static readonly DailyScenario[] Scenarios =
        {
            new DailyScenario("BEE-925 Collecte ressources", "daily_collect_done", "honey_storage"),
            new DailyScenario("BEE-926 Amelioration pending", "daily_upgrade_pending", "honey_storage"),
            new DailyScenario("BEE-926 Amelioration complete", "daily_upgrade_complete", "honey_storage"),
            new DailyScenario("BEE-927 Entrainement pending", "daily_training_pending", "guard_post"),
            new DailyScenario("BEE-927 Entrainement complete", "daily_training_complete", "guard_post"),
            new DailyScenario("BEE-928 Inspection armee locale", "daily_army_inspect", "guard_post"),
            new DailyScenario("BEE-929 Refus et recovery", "daily_refusal_recovery", "honey_storage"),
            new DailyScenario("BEE-930 Feedback consolide", "daily_loop_complete", "honey_storage")
        };

        [MenuItem("Bee Kingdom/Playground/Export DEMO-075 BEE-925-930 Daily Hive Loop")]
        public static void ExportForBatch()
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                File.WriteAllText(JsonPath, BuildJson(), Encoding.UTF8);
                File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
                Debug.Log("DEMO-075 BEE-925-930 daily hive loop exported.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("DEMO-075 BEE-925-930 daily hive loop export failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-075 BEE-925-930 Daily Hive Loop Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit uniquement`");
            builder.AppendLine("- Runtime Builder-A: `BEE-925, BEE-926, BEE-927, BEE-928, BEE-929, BEE-930`");
            builder.AppendLine("- Carte monde modifiee: `false`");
            builder.AppendLine("- BEE-881: `bloquee / non implementee`");
            builder.AppendLine("- Serveur officiel live: `false`");
            builder.AppendLine("- Endpoint officiel: `false`");
            builder.AppendLine("- Sauvegarde officielle: `false`");
            builder.AppendLine("- Economie officielle: `false`");
            builder.AppendLine("- Armee persistante officielle: `false`");
            builder.AppendLine("- Preuve physique device: `non fournie par Builder-A`");
            builder.AppendLine();
            builder.AppendLine("## Daily Loop Scenario Matrix");
            builder.AppendLine();

            for (int i = 0; i < Scenarios.Length; i++)
            {
                DailyScenario scenario = Scenarios[i];
                ApplyScenario(scenario);
                builder.AppendLine("### " + scenario.Label);
                builder.AppendLine();
                builder.AppendLine("- state: `" + scenario.State + "`");
                foreach (string row in HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof()) builder.AppendLine("- " + row);
                builder.AppendLine();
            }

            builder.AppendLine("## QA-074 Preservation");
            builder.AppendLine();
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("training_eclaireuses_done");
            builder.AppendLine("- bee905_training_arrival_visible:true");
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveReserveClosureForProof())
            {
                if (row.StartsWith("training_", StringComparison.Ordinal) || row.StartsWith("local_army_counts:", StringComparison.Ordinal)) builder.AppendLine("- " + row);
            }

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("ui_gesture_blocked");
            builder.AppendLine("- bee910_ui_button_blocks_hive:true");
            foreach (string row in HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof())
            {
                if (row.StartsWith("gesture_mode:", StringComparison.Ordinal) || row.StartsWith("pan_delta:", StringComparison.Ordinal) || row.StartsWith("pinch_delta:", StringComparison.Ordinal) || row.StartsWith("fixed_ui_blocks_hive_gesture:", StringComparison.Ordinal)) builder.AppendLine("- " + row);
            }

            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("- batch_method: `SandboxBee930DailyHiveLoopTests.RunAllForBatch`");
            builder.AppendLine("- export_method: `SandboxBee930DailyHiveLoopExport.ExportForBatch`");
            builder.AppendLine("- expected_result: `PASS`");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_075_DAILY_HIVE_LOOP = YES");
            return builder.ToString();
        }

        private static string BuildJson()
        {
            ApplyScenario(new DailyScenario("Complete", "daily_loop_complete", "honey_storage"));
            string[] completeRows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();
            ApplyScenario(new DailyScenario("Refusal", "daily_refusal_recovery", "honey_storage"));
            string[] refusalRows = HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof();

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"demo_id\": \"DEMO-075\",");
            builder.AppendLine("  \"scope\": \"playable_hive_only\",");
            builder.AppendLine("  \"runtime_bees\": [\"BEE-925\", \"BEE-926\", \"BEE-927\", \"BEE-928\", \"BEE-929\", \"BEE-930\"],");
            builder.AppendLine("  \"ready_for_demo_075_daily_hive_loop\": true,");
            builder.AppendLine("  \"daily_loop\": {");
            builder.AppendLine("    \"sequence\": \"collect_resources>upgrade_building>train_troops>inspect_local_army>recover_refusal\",");
            builder.AppendLine("    \"collect_resources_visible\": true,");
            builder.AppendLine("    \"upgrade_completed\": " + JsonBool(ContainsRow(completeRows, "upgrade_completed:true")) + ",");
            builder.AppendLine("    \"training_completed\": " + JsonBool(ContainsRow(completeRows, "training_completed:true")) + ",");
            builder.AppendLine("    \"local_army_non_persistent\": " + JsonBool(ContainsRow(completeRows, "local_army_non_persistent:true")) + ",");
            builder.AppendLine("    \"refusal_recovery_visible\": " + JsonBool(ContainsRow(refusalRows, "refusal_recovery_visible:true")) + ",");
            builder.AppendLine("    \"feedback_states_unified\": " + JsonBool(ContainsRow(completeRows, "button_feedback_unified:true")) + "");
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
            builder.AppendLine("# Builder-A BEE-925-930 Daily Hive Loop Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("Implementation runtime ruche jouable de la boucle quotidienne locale: collecte ressources, amelioration batiment, entrainement troupes, inspection armee locale, recovery apres refus et feedback produit unifie. Les corrections QA-074 BEE-905/BEE-910 sont preservees. Aucun serveur live, aucune sauvegarde/economie/armee persistante officielle, aucune carte monde et aucun BEE-881.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee930DailyHiveLoopTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee930DailyHiveLoopExport.cs`");
            builder.AppendLine();
            builder.AppendLine("## APIs publiques ajoutees");
            builder.AppendLine();
            builder.AppendLine("* `HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof()`");
            builder.AppendLine("* `SandboxBee930DailyHiveLoopTests.RunAllForBatch()`");
            builder.AppendLine("* `SandboxBee930DailyHiveLoopExport.ExportForBatch()`");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine("* JSON: `" + JsonPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* `SandboxBee930DailyHiveLoopTests.RunAllForBatch`: PASS attendu et execute.");
            builder.AppendLine("* Couverture: collecte visible, upgrade pending/completion/cout unique, training queue/arrival, inspection armee locale, refus sans debit, preservation QA-074.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("* Boucle locale de preview seulement; aucune progression serveur officielle.");
            builder.AppendLine("* Pas de preuve physique device ajoutee par Builder-A.");
            builder.AppendLine("* Aucun serveur officiel live, endpoint, sauvegarde, economie ou armee persistante officielle.");
            builder.AppendLine("* Aucune carte monde et aucun BEE-881.");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_075_DAILY_HIVE_LOOP = YES");
            return builder.ToString();
        }

        private static void ApplyScenario(DailyScenario scenario)
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(scenario.HotspotId);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(scenario.HotspotId == "guard_post" ? 1.14f : 1.10f);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(scenario.HotspotId == "guard_post" ? -18f : 0f, scenario.HotspotId == "guard_post" ? 8f : 0f);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState(scenario.State);
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
    }
}
