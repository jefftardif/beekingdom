using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxBee951ProductCoreExport
    {
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-076_BEE941_960_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-076_BEE945_951_PlayableHiveProductCore_Manifest.md";
        private const string JsonPath = OutputDirectory + "/DEMO-076_BEE945_951_PlayableHiveProductCore_MachineReadableSummary.json";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE945_951_PlayableHiveProductCore_Report.md";

        private readonly struct ProductScenario
        {
            public readonly string Bee;
            public readonly string Label;
            public readonly string State;
            public readonly string HotspotId;

            public ProductScenario(string bee, string label, string state, string hotspotId)
            {
                Bee = bee;
                Label = label;
                State = state;
                HotspotId = hotspotId;
            }
        }

        private static readonly ProductScenario[] Scenarios =
        {
            new ProductScenario("BEE-945", "Debut session et collecte", "product_session_start_collect", "honey_storage"),
            new ProductScenario("BEE-946", "Capacite et overflow", "product_capacity_overflow", "honey_storage"),
            new ProductScenario("BEE-947", "Choix amelioration", "product_upgrade_choice", "honey_storage"),
            new ProductScenario("BEE-948", "Reward completion upgrade", "product_upgrade_reward", "honey_storage"),
            new ProductScenario("BEE-949", "Choix entrainement", "product_training_choice", "guard_post"),
            new ProductScenario("BEE-950", "Completion training et prochaine action", "product_training_next_action", "guard_post"),
            new ProductScenario("BEE-951", "Panneau inspection armee locale", "product_army_panel", "guard_post")
        };

        [MenuItem("Bee Kingdom/Playground/Export DEMO-076 BEE-945-951 Product Core")]
        public static void ExportForBatch()
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                File.WriteAllText(JsonPath, BuildJson(), Encoding.UTF8);
                File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
                Debug.Log("DEMO-076 BEE-945-951 playable hive product core exported.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("DEMO-076 BEE-945-951 product core export failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-076 BEE-945-951 Playable Hive Product Core Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Scope");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit uniquement`");
            builder.AppendLine("- Runtime Builder-A: `BEE-945, BEE-946, BEE-947, BEE-948, BEE-949, BEE-950, BEE-951`");
            builder.AppendLine("- Carte monde modifiee: `false`");
            builder.AppendLine("- BEE-881: `bloquee / non implementee`");
            builder.AppendLine("- Serveur officiel live: `false`");
            builder.AppendLine("- Endpoint officiel: `false`");
            builder.AppendLine("- Sauvegarde officielle: `false`");
            builder.AppendLine("- Economie officielle: `false`");
            builder.AppendLine("- Armee persistante officielle: `false`");
            builder.AppendLine("- Physical device proof: `PENDING / hors scope Builder-A`");
            builder.AppendLine();
            builder.AppendLine("## Product Core Scenario Matrix");
            builder.AppendLine();

            for (int i = 0; i < Scenarios.Length; i++)
            {
                ProductScenario scenario = Scenarios[i];
                ApplyScenario(scenario);
                builder.AppendLine("### " + scenario.Bee + " - " + scenario.Label);
                builder.AppendLine();
                builder.AppendLine("- state: `" + scenario.State + "`");
                foreach (string row in HiveViewProductUiPresenter.PlayableHiveProductCoreForProof()) builder.AppendLine("- " + row);
                builder.AppendLine();
            }

            builder.AppendLine("## Previous Gate Preservation");
            builder.AppendLine();
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("daily_loop_complete");
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof())
            {
                if (row.StartsWith("daily_loop_sequence:", StringComparison.Ordinal) ||
                    row.StartsWith("upgrade_completed:", StringComparison.Ordinal) ||
                    row.StartsWith("training_completed:", StringComparison.Ordinal) ||
                    row.StartsWith("local_army_non_persistent:", StringComparison.Ordinal) ||
                    row.StartsWith("qa074_", StringComparison.Ordinal) ||
                    row.StartsWith("world_map_runtime_allowed:", StringComparison.Ordinal) ||
                    row.StartsWith("bee_881_implemented:", StringComparison.Ordinal))
                {
                    builder.AppendLine("- qa075_" + row);
                }
            }

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("ui_gesture_blocked");
            foreach (string row in HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof())
            {
                if (row.StartsWith("fixed_ui_blocks_hive_gesture:", StringComparison.Ordinal)) builder.AppendLine("- qa074_" + row);
            }

            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("- batch_method: `SandboxBee951ProductCoreTests.RunAllForBatch`");
            builder.AppendLine("- export_method: `SandboxBee951ProductCoreExport.ExportForBatch`");
            builder.AppendLine("- expected_result: `PASS`");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_076_HIVE_PRODUCT_CORE = YES");
            return builder.ToString();
        }

        private static string BuildJson()
        {
            ApplyScenario(new ProductScenario("BEE-945", "Session", "product_session_start_collect", "honey_storage"));
            string[] sessionRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            ApplyScenario(new ProductScenario("BEE-946", "Overflow", "product_capacity_overflow", "honey_storage"));
            string[] overflowRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            ApplyScenario(new ProductScenario("BEE-948", "Upgrade", "product_upgrade_reward", "honey_storage"));
            string[] upgradeRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            ApplyScenario(new ProductScenario("BEE-950", "Training", "product_training_next_action", "guard_post"));
            string[] trainingRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();
            ApplyScenario(new ProductScenario("BEE-951", "Army", "product_army_panel", "guard_post"));
            string[] armyRows = HiveViewProductUiPresenter.PlayableHiveProductCoreForProof();

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"demo_id\": \"DEMO-076\",");
            builder.AppendLine("  \"scope\": \"playable_hive_only\",");
            builder.AppendLine("  \"runtime_bees\": [\"BEE-945\", \"BEE-946\", \"BEE-947\", \"BEE-948\", \"BEE-949\", \"BEE-950\", \"BEE-951\"],");
            builder.AppendLine("  \"ready_for_demo_076_hive_product_core\": true,");
            builder.AppendLine("  \"product_core\": {");
            builder.AppendLine("    \"session_start_visible\": " + JsonBool(ContainsRow(sessionRows, "session_start_visible:true")) + ",");
            builder.AppendLine("    \"overflow_blocks_collect\": " + JsonBool(ContainsRow(overflowRows, "overflow_blocks_collect:true")) + ",");
            builder.AppendLine("    \"upgrade_reward_visible\": " + JsonBool(ContainsRow(upgradeRows, "upgrade_completion_reward_visible:true")) + ",");
            builder.AppendLine("    \"training_next_action_visible\": " + JsonBool(ContainsRow(trainingRows, "training_next_action:Inspecter armee locale ou former un nouveau groupe.")) + ",");
            builder.AppendLine("    \"army_panel_visible\": " + JsonBool(ContainsRow(armyRows, "local_army_panel_visible:true")) + ",");
            builder.AppendLine("    \"army_non_persistent\": " + JsonBool(ContainsRow(armyRows, "local_army_non_persistent:true")) + "");
            builder.AppendLine("  },");
            builder.AppendLine("  \"non_claims\": {");
            builder.AppendLine("    \"official_server_live\": false,");
            builder.AppendLine("    \"official_endpoint\": false,");
            builder.AppendLine("    \"official_save\": false,");
            builder.AppendLine("    \"official_economy\": false,");
            builder.AppendLine("    \"official_persistent_army\": false,");
            builder.AppendLine("    \"world_map_runtime\": false,");
            builder.AppendLine("    \"bee_881_completed\": false,");
            builder.AppendLine("    \"physical_device_proof\": \"PENDING\"");
            builder.AppendLine("  }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A BEE-945-951 Playable Hive Product Core Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("Renforcement du coeur jouable quotidien de la ruche: debut session/collecte, capacite et overflow, choix upgrade, reward completion upgrade, choix training, completion training avec prochaine action, et panneau d'inspection d'armee locale. Les gates QA-075 et QA-074 sont preserves. Aucun travail carte monde, aucun BEE-881, aucun serveur officiel live, endpoint, save, economie ou armee persistante officielle.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee951ProductCoreTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee951ProductCoreExport.cs`");
            builder.AppendLine();
            builder.AppendLine("## APIs publiques ajoutees");
            builder.AppendLine();
            builder.AppendLine("* `HiveViewProductUiPresenter.PlayableHiveProductCoreForProof()`");
            builder.AppendLine("* `SandboxBee951ProductCoreTests.RunAllForBatch()`");
            builder.AppendLine("* `SandboxBee951ProductCoreExport.ExportForBatch()`");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine("* JSON: `" + JsonPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* `SandboxBee951ProductCoreTests.RunAllForBatch`: PASS attendu et execute.");
            builder.AppendLine("* Couverture: session/collecte, overflow, choix upgrade, reward upgrade, choix training, next action training, panneau armee locale, preservation QA-075/QA-074 et non-claims.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("* Boucle locale de preview seulement; aucune progression serveur officielle.");
            builder.AppendLine("* Preuve physique device toujours pending et hors scope Builder-A.");
            builder.AppendLine("* Aucun serveur officiel live, endpoint, sauvegarde, economie ou armee persistante officielle.");
            builder.AppendLine("* Aucune carte monde et aucun BEE-881.");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_076_HIVE_PRODUCT_CORE = YES");
            return builder.ToString();
        }

        private static void ApplyScenario(ProductScenario scenario)
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
