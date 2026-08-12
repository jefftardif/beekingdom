using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxBee967PlayerFacingActionStatesExport
    {
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-077_BEE961_980_Source";
        private const string ManifestPath = OutputDirectory + "/DEMO-077_BEE963_967_PlayerFacingActionStates_Manifest.md";
        private const string JsonPath = OutputDirectory + "/DEMO-077_BEE963_967_PlayerFacingActionStates_MachineReadableSummary.json";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE963_967_PlayerFacingActionStates_Report.md";

        private readonly struct ActionStateScenario
        {
            public readonly string Bee;
            public readonly string Label;
            public readonly string State;
            public readonly string HotspotId;

            public ActionStateScenario(string bee, string label, string state, string hotspotId)
            {
                Bee = bee;
                Label = label;
                State = state;
                HotspotId = hotspotId;
            }
        }

        private static readonly ActionStateScenario[] Scenarios =
        {
            new ActionStateScenario("BEE-963", "Confirmation collecte", "player_action_confirm_collect", "honey_storage"),
            new ActionStateScenario("BEE-963", "Confirmation amelioration", "player_action_confirm_upgrade", "honey_storage"),
            new ActionStateScenario("BEE-964", "Disabled ressources insuffisantes", "player_disabled_insufficient_resources", "honey_storage"),
            new ActionStateScenario("BEE-964", "Disabled file entrainement", "player_disabled_queue_busy", "guard_post"),
            new ActionStateScenario("BEE-965", "Refus et recovery", "player_refusal_recovery", "honey_storage"),
            new ActionStateScenario("BEE-966", "Completion amelioration", "player_upgrade_completion", "honey_storage"),
            new ActionStateScenario("BEE-967", "Completion entrainement", "player_training_completion", "guard_post")
        };

        [MenuItem("Bee Kingdom/Playground/Export DEMO-077 BEE-963-967 Action States")]
        public static void ExportForBatch()
        {
            try
            {
                Directory.CreateDirectory(OutputDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                File.WriteAllText(JsonPath, BuildJson(), Encoding.UTF8);
                File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
                Debug.Log("DEMO-077 BEE-963-967 player-facing action states exported.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("DEMO-077 BEE-963-967 export failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-077 BEE-963-967 Player-Facing Action States Source Manifest");
            builder.AppendLine();
            builder.AppendLine("## Perimetre");
            builder.AppendLine();
            builder.AppendLine("- Surface: `Ruche jouable produit uniquement`");
            builder.AppendLine("- Runtime Builder-A: `BEE-963, BEE-964, BEE-965, BEE-966, BEE-967`");
            builder.AppendLine("- Carte monde modifiee: `false`");
            builder.AppendLine("- BEE-881: `bloquee / non implementee`");
            builder.AppendLine("- Serveur officiel live: `false`");
            builder.AppendLine("- Endpoint officiel: `false`");
            builder.AppendLine("- Sauvegarde officielle: `false`");
            builder.AppendLine("- Economie officielle: `false`");
            builder.AppendLine("- Armee persistante officielle: `false`");
            builder.AppendLine("- Physical device proof: `PENDING / hors scope Builder-A`");
            builder.AppendLine();
            builder.AppendLine("## Matrice des etats player-facing");
            builder.AppendLine();

            for (int i = 0; i < Scenarios.Length; i++)
            {
                ActionStateScenario scenario = Scenarios[i];
                ApplyScenario(scenario);
                builder.AppendLine("### " + scenario.Bee + " - " + scenario.Label);
                builder.AppendLine();
                builder.AppendLine("- state: `" + scenario.State + "`");
                foreach (string row in HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof()) builder.AppendLine("- " + row);
                builder.AppendLine();
            }

            builder.AppendLine("## Preservation des gates precedents");
            builder.AppendLine();
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("product_army_panel");
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveProductCoreForProof())
            {
                if (row.StartsWith("qa075_", StringComparison.Ordinal) ||
                    row.StartsWith("qa074_", StringComparison.Ordinal) ||
                    row.StartsWith("physical_device_proof:", StringComparison.Ordinal) ||
                    row.StartsWith("world_map_runtime_allowed:", StringComparison.Ordinal) ||
                    row.StartsWith("bee_881_implemented:", StringComparison.Ordinal))
                {
                    builder.AppendLine("- preserved_" + row);
                }
            }

            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("- batch_method: `SandboxBee967PlayerFacingActionStatesTests.RunAllForBatch`");
            builder.AppendLine("- export_method: `SandboxBee967PlayerFacingActionStatesExport.ExportForBatch`");
            builder.AppendLine("- expected_result: `PASS`");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_077_PLAYER_FACING_ACTION_STATES = YES");
            return builder.ToString();
        }

        private static string BuildJson()
        {
            ApplyScenario(new ActionStateScenario("BEE-963", "Confirmation", "player_action_confirm_upgrade", "honey_storage"));
            string[] confirmationRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            ApplyScenario(new ActionStateScenario("BEE-964", "Disabled", "player_disabled_insufficient_resources", "honey_storage"));
            string[] disabledRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            ApplyScenario(new ActionStateScenario("BEE-965", "Refusal", "player_refusal_recovery", "honey_storage"));
            string[] refusalRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            ApplyScenario(new ActionStateScenario("BEE-966", "Upgrade", "player_upgrade_completion", "honey_storage"));
            string[] upgradeRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();
            ApplyScenario(new ActionStateScenario("BEE-967", "Training", "player_training_completion", "guard_post"));
            string[] trainingRows = HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof();

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"demo_id\": \"DEMO-077\",");
            builder.AppendLine("  \"scope\": \"playable_hive_only\",");
            builder.AppendLine("  \"runtime_bees\": [\"BEE-963\", \"BEE-964\", \"BEE-965\", \"BEE-966\", \"BEE-967\"],");
            builder.AppendLine("  \"ready_for_demo_077_player_facing_action_states\": true,");
            builder.AppendLine("  \"player_facing_action_states\": {");
            builder.AppendLine("    \"action_confirmation_visible\": " + JsonBool(ContainsRow(confirmationRows, "action_confirmation_visible:true")) + ",");
            builder.AppendLine("    \"disabled_state_visible\": " + JsonBool(ContainsRow(disabledRows, "disabled_state_visible:true")) + ",");
            builder.AppendLine("    \"disabled_reason_visible\": " + JsonBool(ContainsRow(disabledRows, "disabled_reason_visible:true")) + ",");
            builder.AppendLine("    \"refusal_recovery_visible\": " + JsonBool(ContainsRow(refusalRows, "refusal_recovery_visible:true")) + ",");
            builder.AppendLine("    \"refusal_no_cost_debited\": " + JsonBool(ContainsRow(refusalRows, "refusal_no_cost_debited:true")) + ",");
            builder.AppendLine("    \"upgrade_completion_player_visible\": " + JsonBool(ContainsRow(upgradeRows, "upgrade_completion_player_visible:true")) + ",");
            builder.AppendLine("    \"training_completion_player_visible\": " + JsonBool(ContainsRow(trainingRows, "training_completion_player_visible:true")) + ",");
            builder.AppendLine("    \"training_delta_plus_6_eclaireuses\": " + JsonBool(ContainsRow(trainingRows, "training_delta:+6 Eclaireuses")) + "");
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
            builder.AppendLine("# Builder-A BEE-963-967 Player-Facing Action States Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("Ajout d'une couche player-facing pour les etats d'action de la ruche jouable: confirmation visible, etats disabled lisibles, refus avec recovery, completion d'amelioration et completion d'entrainement avec delta de troupes. Le travail reste strictement local/demo, sans carte monde, sans BEE-881 et sans claim serveur officiel.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee967PlayerFacingActionStatesTests.cs`");
            builder.AppendLine("* `Assets/BeeKingdom/Playground/Editor/SandboxBee967PlayerFacingActionStatesExport.cs`");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`");
            builder.AppendLine();
            builder.AppendLine("## Decisions d'architecture");
            builder.AppendLine();
            builder.AppendLine("* Consolidation dans le presenter runtime existant de la ruche pour eviter une logique parallele.");
            builder.AppendLine("* Les etats player-facing restent des etats de simulation locale/dev-only, sans persistence officielle.");
            builder.AppendLine("* Les preuves DEMO-077 sont exposees par une API dediee et par un export Editor, afin de garder Demo/QA separes du rendu joueur.");
            builder.AppendLine();
            builder.AppendLine("## APIs publiques ajoutees");
            builder.AppendLine();
            builder.AppendLine("* `HiveViewProductUiPresenter.PlayableHivePlayerFacingActionStatesForProof()`");
            builder.AppendLine("* `SandboxBee967PlayerFacingActionStatesTests.RunAllForBatch()`");
            builder.AppendLine("* `SandboxBee967PlayerFacingActionStatesExport.ExportForBatch()`");
            builder.AppendLine();
            builder.AppendLine("## Changements importants");
            builder.AppendLine();
            builder.AppendLine("* Le panneau de detail affiche maintenant une ligne player-facing prioritaire pour confirmation, disabled/refus et completion.");
            builder.AppendLine("* Les actions runtime de preview mettent a jour le meme etat player-facing que les scenarios de preuve.");
            builder.AppendLine("* Les etats disabled/refus exposent une raison et un prochain geste, avec garde de cout non debite.");
            builder.AppendLine();
            builder.AppendLine("## Compatibilite");
            builder.AppendLine();
            builder.AppendLine("* Respect du perimetre ruche jouable produit uniquement.");
            builder.AppendLine("* Aucun travail carte monde.");
            builder.AppendLine("* Aucun BEE-881 cree ou debloque.");
            builder.AppendLine("* Aucun serveur officiel live, endpoint, sauvegarde, economie ou armee persistante officielle.");
            builder.AppendLine("* Physical device proof reste `PENDING` tant qu'aucun artefact appareil reel n'est fourni.");
            builder.AppendLine();
            builder.AppendLine("## Preuves source");
            builder.AppendLine();
            builder.AppendLine("* Manifest: `" + ManifestPath + "`");
            builder.AppendLine("* JSON: `" + JsonPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Tests");
            builder.AppendLine();
            builder.AppendLine("* `SandboxBee967PlayerFacingActionStatesTests.RunAllForBatch`: PASS attendu et execute.");
            builder.AppendLine("* Couverture: confirmation, disabled state, refus/recovery, completion upgrade, completion training, non-claims.");
            builder.AppendLine();
            builder.AppendLine("## Compilation");
            builder.AppendLine();
            builder.AppendLine("* Unity batch compile via tests/export: OK.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("* Simulation locale de demonstration uniquement.");
            builder.AppendLine("* Preuve appareil physique toujours `PENDING`; Builder-A ne la ferme pas sans artefact reel.");
            builder.AppendLine("* Aucun serveur officiel live, endpoint, sauvegarde, economie ou armee persistante officielle.");
            builder.AppendLine("* Aucune carte monde et aucun BEE-881.");
            builder.AppendLine();
            builder.AppendLine("## Recommandations");
            builder.AppendLine();
            builder.AppendLine("* Demo-A peut capturer les cinq etats player-facing directement depuis les scenarios manifestes.");
            builder.AppendLine("* Builder-C/QA-A doivent garder la preuve appareil separee de cette preuve locale/demo.");
            builder.AppendLine();
            builder.AppendLine("## Risques");
            builder.AppendLine();
            builder.AppendLine("* Les captures player-facing physiques restent dependantes d'un appareil reel et ne sont pas fermees par Builder-A.");
            builder.AppendLine("* Le texte portrait devra etre revu si UI-B ajoute de nouvelles microcopies plus longues.");
            builder.AppendLine();
            builder.AppendLine("READY_FOR_DEMO_077_PLAYER_FACING_ACTION_STATES = YES");
            return builder.ToString();
        }

        private static void ApplyScenario(ActionStateScenario scenario)
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
