using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxBee992T0T8ScreenshotStatesTests
    {
        private readonly struct Scenario
        {
            public readonly string FrameId;
            public readonly string State;
            public readonly string HotspotId;

            public Scenario(string frameId, string state, string hotspotId)
            {
                FrameId = frameId;
                State = state;
                HotspotId = hotspotId;
            }
        }

        private static readonly Scenario[] Scenarios =
        {
            new Scenario("T0", "product_session_start_collect", "honey_storage"),
            new Scenario("T1", "player_action_confirm_upgrade", "honey_storage"),
            new Scenario("T2", "player_disabled_insufficient_resources", "honey_storage"),
            new Scenario("T3", "player_refusal_recovery", "honey_storage"),
            new Scenario("T4", "player_upgrade_completion", "honey_storage"),
            new Scenario("T5", "player_training_completion", "guard_post"),
            new Scenario("T6", "player_army_inspection", "guard_post"),
            new Scenario("T7", "ui_gesture_blocked", "honey_storage"),
            new Scenario("T8", "player_non_claim_scope_lock", "honey_storage")
        };

        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxBee992T0T8ScreenshotStatesTests();
                tests.AllFramesExposeStableScreenshotStateRows();
                tests.FrameSpecificPlayerFacingMarkersArePresent();
                tests.ScopeLocksArePreserved();
                Debug.Log("BEE-984-992 T0-T8 screenshot state checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("BEE-984-992 T0-T8 screenshot state checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void AllFramesExposeStableScreenshotStateRows()
        {
            for (int i = 0; i < Scenarios.Length; i++)
            {
                Scenario scenario = Scenarios[i];
                ApplyScenario(scenario);
                string[] rows = HiveViewProductUiPresenter.PlayableHiveT0T8ScreenshotStateForProof(scenario.FrameId);
                AssertRow(rows, "bee_984_992_scope:t0_t8_player_facing_screenshot_states");
                AssertRow(rows, "frame_id:" + scenario.FrameId);
                AssertRow(rows, "surface:playable_hive_only");
                AssertRow(rows, "scene:SandboxPlayground");
                AssertRow(rows, "hud_resources_visible:true");
                AssertRow(rows, "local_preview_label_visible:true");
                AssertRow(rows, "visual_artifact_required:true");
            }
        }

        [Test]
        public void FrameSpecificPlayerFacingMarkersArePresent()
        {
            AssertFrame("T0", "product_session_start_collect", "honey_storage", "session_start_visible:true");
            AssertFrame("T1", "player_action_confirm_upgrade", "honey_storage", "action_confirmation_visible:true");
            AssertFrame("T2", "player_disabled_insufficient_resources", "honey_storage", "disabled_state_visible:true");
            AssertFrame("T3", "player_refusal_recovery", "honey_storage", "refusal_recovery_visible:true");
            AssertFrame("T4", "player_upgrade_completion", "honey_storage", "upgrade_completion_player_visible:true");
            AssertFrame("T5", "player_training_completion", "guard_post", "training_completion_player_visible:true");
            AssertFrame("T6", "player_army_inspection", "guard_post", "local_army_inspection_visible:true");
            AssertFrame("T7", "ui_gesture_blocked", "honey_storage", "gesture_ui_fixed_local_proof_visible:true");
            AssertFrame("T8", "player_non_claim_scope_lock", "honey_storage", "non_claim_scope_lock_visible:true");
        }

        [Test]
        public void ScopeLocksArePreserved()
        {
            ApplyScenario(new Scenario("T8", "player_non_claim_scope_lock", "honey_storage"));
            string[] rows = HiveViewProductUiPresenter.PlayableHiveT0T8ScreenshotStateForProof("T8");
            AssertRow(rows, "physical_device_proof:pending");
            AssertRow(rows, "local_demo_only:true");
            AssertRow(rows, "official_server_live:false");
            AssertRow(rows, "official_endpoint:false");
            AssertRow(rows, "official_save:false");
            AssertRow(rows, "official_economy:false");
            AssertRow(rows, "official_army_persistence:false");
            AssertRow(rows, "world_map_runtime_allowed:false");
            AssertRow(rows, "bee_881_implemented:false");
        }

        private static void AssertFrame(string frameId, string state, string hotspotId, string expectedRow)
        {
            ApplyScenario(new Scenario(frameId, state, hotspotId));
            AssertRow(HiveViewProductUiPresenter.PlayableHiveT0T8ScreenshotStateForProof(frameId), expectedRow);
        }

        private static void ApplyScenario(Scenario scenario)
        {
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(scenario.HotspotId);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(scenario.HotspotId == "guard_post" ? -18f : 0f, scenario.HotspotId == "guard_post" ? 8f : 0f);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(scenario.HotspotId == "guard_post" ? 1.14f : 1.10f);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState(scenario.State);
        }

        private static void AssertRow(string[] rows, string expected)
        {
            if (!rows.Any(row => string.Equals(row, expected, StringComparison.Ordinal)))
            {
                Assert.Fail("Expected proof row not found: " + expected + Environment.NewLine + string.Join(Environment.NewLine, rows));
            }
        }
    }
}
