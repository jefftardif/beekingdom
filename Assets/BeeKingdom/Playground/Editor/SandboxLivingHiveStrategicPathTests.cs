using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Gameplay.Progression;
using BeeKingdom.Localization;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveStrategicPathTests
    {
        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveStrategicPathTests();
            tests.CatalogMatchesFiveCanonicalLevelTenClasses();
            tests.PreviewProofKeepsOfficialSelectionClosedAndAppliesNoBonus();
            tests.PortraitAndLandscapeCardsMeetMobileTouchMinimum();
            tests.OverlayLifecycleBlocksUnderlyingHiveInput();
            tests.TrialCatalogHasOneBoundedScenarioPerPath();
            tests.TrialProofIsVolatileAndNeverAppliesGameplay();
            tests.TrialControlsMeetMobileTouchMinimum();
            tests.TrialLifecycleResetsOnOverlayClose();
            tests.CombatDoctrineCatalogHasOneBalancedCycle();
            tests.CombatDoctrineProofIsVolatileAndNeverSimulatesBattle();
            tests.CombatDoctrineControlsMeetMobileTouchMinimum();
            tests.CombatDoctrineLifecycleResetsOnOverlayClose();
            tests.AllStrategicPathCopyExistsInBothCatalogs();
        }

        [Test]
        public void CatalogMatchesFiveCanonicalLevelTenClasses()
        {
            Assert.That(HiveStrategicPathCatalog.All.Count, Is.EqualTo(5));
            CollectionAssert.AreEqual(
                new[] { PlayerClass.RoyalGuard, PlayerClass.Striker, PlayerClass.Nurturer, PlayerClass.Scout, PlayerClass.Alchemist },
                HiveStrategicPathCatalog.All.Select(definition => definition.ClassId).ToArray());
            CollectionAssert.AreEqual(
                new[] { "royal_guard", "striker", "nurturer", "scout", "alchemist" },
                HiveStrategicPathCatalog.All.Select(definition => definition.Token).ToArray());
            Assert.That(HiveStrategicPathCatalog.All, Has.None.Property("ClassId").EqualTo(PlayerClass.Neutral));
            Assert.That(PlayerSkillState.ClassUnlockLevel, Is.EqualTo(10));
        }

        [Test]
        public void PreviewProofKeepsOfficialSelectionClosedAndAppliesNoBonus()
        {
            string[] rows = HiveViewProductUiPresenter.StrategicPathPreviewForProof("nurturer", true, 390f, 844f);
            AssertRow(rows, "strategic_path_preview_enabled:true");
            AssertRow(rows, "strategic_path_option_count:5");
            AssertRow(rows, "strategic_path_selected_preview:nurturer");
            AssertRow(rows, "strategic_path_neutral_selectable:false");
            AssertRow(rows, "strategic_path_tap_changes_preview_only:true");
            AssertRow(rows, "strategic_path_official_selection_enabled:false");
            AssertRow(rows, "strategic_path_official_state_source:authenticated_server_snapshot_required");
            AssertRow(rows, "strategic_path_server_contract_status:http_contract_feature_closed_not_connected");
            AssertRow(rows, "strategic_path_bonus_applied:false");
            AssertRow(rows, "strategic_path_mutates_gameplay:false");
            AssertRow(rows, "strategic_path_changes_protected_art:false");
        }

        [Test]
        public void PortraitAndLandscapeCardsMeetMobileTouchMinimum()
        {
            AssertCards(HiveViewProductUiPresenter.StrategicPathCardRectsForProof(true, 390f, 844f), 390f, 844f);
            AssertCards(HiveViewProductUiPresenter.StrategicPathCardRectsForProof(false, 1600f, 900f), 1600f, 900f);
            AssertRow(
                HiveViewProductUiPresenter.StrategicPathPreviewForProof("scout", false, 1600f, 900f),
                "strategic_path_card_min_touch:56");
        }

        [Test]
        public void OverlayLifecycleBlocksUnderlyingHiveInput()
        {
            try
            {
                HiveViewProductUiPresenter.ResetAntLegionHudForProof();
                Assert.That(HiveViewProductUiPresenter.StrategicPathPreviewPanelOpenForProof, Is.False);
                HiveViewProductUiPresenter.OpenStrategicPathPreviewForProof("scout");
                Assert.That(HiveViewProductUiPresenter.StrategicPathPreviewPanelOpenForProof, Is.True);
                Assert.That(HiveViewProductUiPresenter.GuidedTutorialBlocksUnderlyingHiveChromeInputForProof(), Is.True);
                HiveViewProductUiPresenter.CloseStrategicPathPreviewForProof();
                Assert.That(HiveViewProductUiPresenter.StrategicPathPreviewPanelOpenForProof, Is.False);
                Assert.That(HiveViewProductUiPresenter.GuidedTutorialBlocksUnderlyingHiveChromeInputForProof(), Is.False);
            }
            finally
            {
                HiveViewProductUiPresenter.CloseStrategicPathPreviewForProof();
            }
        }

        [Test]
        public void TrialCatalogHasOneBoundedScenarioPerPath()
        {
            Assert.That(HiveStrategicPathCatalog.All.Select(definition => definition.TrialScenarioId), Is.Unique);
            Assert.That(HiveStrategicPathCatalog.All.All(definition => !string.IsNullOrWhiteSpace(definition.TrialScenarioId)), Is.True);
            Assert.That(HiveStrategicPathCatalog.All.All(definition => definition.PreferredTrialChoice >= 0 && definition.PreferredTrialChoice < 2), Is.True);
        }

        [Test]
        public void TrialProofIsVolatileAndNeverAppliesGameplay()
        {
            string[] fit = HiveViewProductUiPresenter.StrategicPathTrialForProof("scout", 1, true, 390f, 844f);
            AssertRow(fit, "strategic_path_trial_enabled:true");
            AssertRow(fit, "strategic_path_trial_scenario:survey_orchard_route");
            AssertRow(fit, "strategic_path_trial_result:identity_fit");
            AssertRow(fit, "strategic_path_trial_device_state:volatile_in_memory_only");
            AssertRow(fit, "strategic_path_trial_server_persistence:false");
            AssertRow(fit, "strategic_path_trial_official_selection_unchanged:true");
            AssertRow(fit, "strategic_path_trial_bonus_applied:false");
            AssertRow(fit, "strategic_path_trial_mutates_gameplay:false");
            AssertRow(fit, "strategic_path_trial_changes_protected_art:false");

            AssertRow(
                HiveViewProductUiPresenter.StrategicPathTrialForProof("scout", 0, false, 1600f, 900f),
                "strategic_path_trial_result:tradeoff_exposed");
        }

        [Test]
        public void TrialControlsMeetMobileTouchMinimum()
        {
            AssertTrialControls(true, 390f, 844f);
            AssertTrialControls(false, 1600f, 900f);
        }

        [Test]
        public void TrialLifecycleResetsOnOverlayClose()
        {
            try
            {
                HiveViewProductUiPresenter.ResetAntLegionHudForProof();
                HiveViewProductUiPresenter.OpenStrategicPathTrialForProof("striker");
                Assert.That(HiveViewProductUiPresenter.StrategicPathTrialOpenForProof, Is.True);
                Assert.That(HiveViewProductUiPresenter.StrategicPathTrialChoiceForProof, Is.EqualTo(-1));
                HiveViewProductUiPresenter.ChooseStrategicPathTrialForProof(1);
                Assert.That(HiveViewProductUiPresenter.StrategicPathTrialChoiceForProof, Is.EqualTo(1));
                HiveViewProductUiPresenter.CloseStrategicPathPreviewForProof();
                Assert.That(HiveViewProductUiPresenter.StrategicPathTrialOpenForProof, Is.False);
                Assert.That(HiveViewProductUiPresenter.StrategicPathTrialChoiceForProof, Is.EqualTo(-1));
            }
            finally
            {
                HiveViewProductUiPresenter.CloseStrategicPathPreviewForProof();
            }
        }

        [Test]
        public void CombatDoctrineCatalogHasOneBalancedCycle()
        {
            Assert.That(HiveCombatDoctrineCatalog.Version, Is.EqualTo("phase4-combat-v1"));
            Assert.That(HiveCombatDoctrineCatalog.All.Count, Is.EqualTo(3));
            CollectionAssert.AreEqual(
                new[] { "guardians", "wingrunners", "darters" },
                HiveCombatDoctrineCatalog.All.Select(definition => definition.Token).ToArray());
            Assert.That(HiveCombatDoctrineCatalog.All.Select(definition => definition.Token), Is.Unique);
            Assert.That(HiveCombatDoctrineCatalog.All.Select(definition => definition.Family), Is.Unique);

            foreach (HiveCombatDoctrineDefinition definition in HiveCombatDoctrineCatalog.All)
            {
                Assert.That(definition.Beats, Is.Not.EqualTo(definition.Family));
                Assert.That(definition.LosesTo, Is.Not.EqualTo(definition.Family));
                Assert.That(definition.Beats, Is.Not.EqualTo(definition.LosesTo));
                Assert.That(HiveCombatDoctrineCatalog.Evaluate(definition.Family, definition.Family), Is.EqualTo(HiveCombatDoctrineOutcome.Even));
                Assert.That(HiveCombatDoctrineCatalog.Evaluate(definition.Family, definition.Beats), Is.EqualTo(HiveCombatDoctrineOutcome.Advantage));
                Assert.That(HiveCombatDoctrineCatalog.Evaluate(definition.Family, definition.LosesTo), Is.EqualTo(HiveCombatDoctrineOutcome.Vulnerable));
            }
        }

        [Test]
        public void CombatDoctrineProofIsVolatileAndNeverSimulatesBattle()
        {
            string[] advantage = HiveViewProductUiPresenter.CombatDoctrineForProof("guardians", "darters", true, 390f, 844f);
            AssertRow(advantage, "combat_doctrine_enabled:true");
            AssertRow(advantage, "combat_doctrine_catalog_version:phase4-combat-v1");
            AssertRow(advantage, "combat_doctrine_family_count:3");
            AssertRow(advantage, "combat_doctrine_cycle:guardians>darters>wingrunners>guardians");
            AssertRow(advantage, "combat_doctrine_outcome:advantage");
            AssertRow(advantage, "combat_doctrine_no_dominant_family:true");
            AssertRow(advantage, "combat_doctrine_numeric_coefficients_exposed:false");
            AssertRow(advantage, "combat_doctrine_battle_simulated:false");
            AssertRow(advantage, "combat_doctrine_result_guarantees_victory:false");
            AssertRow(advantage, "combat_doctrine_device_state:volatile_in_memory_only");
            AssertRow(advantage, "combat_doctrine_server_contract_status:http_catalog_feature_closed_not_connected");
            AssertRow(advantage, "combat_doctrine_official_selection_unchanged:true");
            AssertRow(advantage, "combat_doctrine_mutates_gameplay:false");
            AssertRow(advantage, "combat_doctrine_changes_protected_art:false");

            AssertRow(
                HiveViewProductUiPresenter.CombatDoctrineForProof("guardians", "wingrunners", false, 1600f, 900f),
                "combat_doctrine_outcome:vulnerable");
            AssertRow(
                HiveViewProductUiPresenter.CombatDoctrineForProof("darters", "darters", false, 1600f, 900f),
                "combat_doctrine_outcome:even");
        }

        [Test]
        public void CombatDoctrineControlsMeetMobileTouchMinimum()
        {
            AssertCombatDoctrineControls(true, 390f, 844f, 64f);
            AssertCombatDoctrineControls(false, 1600f, 900f, 76f);
        }

        [Test]
        public void CombatDoctrineLifecycleResetsOnOverlayClose()
        {
            try
            {
                HiveViewProductUiPresenter.ResetAntLegionHudForProof();
                HiveViewProductUiPresenter.OpenCombatDoctrineForProof("wingrunners");
                Assert.That(HiveViewProductUiPresenter.CombatDoctrineOpenForProof, Is.True);
                Assert.That(HiveViewProductUiPresenter.CombatDoctrineAttackerIndexForProof, Is.EqualTo(1));
                Assert.That(HiveViewProductUiPresenter.CombatDoctrineDefenderIndexForProof, Is.EqualTo(-1));
                HiveViewProductUiPresenter.ChooseCombatDoctrineForProof(1, 0);
                Assert.That(HiveViewProductUiPresenter.CombatDoctrineDefenderIndexForProof, Is.EqualTo(0));
                HiveViewProductUiPresenter.CloseStrategicPathPreviewForProof();
                Assert.That(HiveViewProductUiPresenter.CombatDoctrineOpenForProof, Is.False);
                Assert.That(HiveViewProductUiPresenter.CombatDoctrineAttackerIndexForProof, Is.EqualTo(0));
                Assert.That(HiveViewProductUiPresenter.CombatDoctrineDefenderIndexForProof, Is.EqualTo(-1));
            }
            finally
            {
                HiveViewProductUiPresenter.CloseStrategicPathPreviewForProof();
            }
        }

        [Test]
        public void AllStrategicPathCopyExistsInBothCatalogs()
        {
            var keys = new List<string>
            {
                "strategic_path.entry.title",
                "strategic_path.entry.value",
                "strategic_path.title",
                "strategic_path.subtitle",
                "strategic_path.local_disclosure",
                "strategic_path.official_unavailable",
                "strategic_path.tradeoff_prefix",
                "strategic_path.trial.button",
                "strategic_path.trial.started",
                "strategic_path.trial.title",
                "strategic_path.trial.back",
                "strategic_path.trial.local_disclosure",
                "strategic_path.trial.choose",
                "strategic_path.trial.choose_hint",
                "strategic_path.trial.fit_label",
                "strategic_path.trial.tradeoff_label",
                "strategic_path.trial.choice_made",
                "combat_doctrine.entry.open",
                "combat_doctrine.entry.back",
                "combat_doctrine.opened",
                "combat_doctrine.closed",
                "combat_doctrine.title",
                "combat_doctrine.subtitle",
                "combat_doctrine.local_disclosure",
                "combat_doctrine.intro_title",
                "combat_doctrine.intro_body",
                "combat_doctrine.attacker_label",
                "combat_doctrine.defender_label",
                "combat_doctrine.attacker_changed",
                "combat_doctrine.defender_changed",
                "combat_doctrine.outcome.pending.title",
                "combat_doctrine.outcome.pending.body",
                "combat_doctrine.outcome.advantage.title",
                "combat_doctrine.outcome.advantage.body",
                "combat_doctrine.outcome.vulnerable.title",
                "combat_doctrine.outcome.vulnerable.body",
                "combat_doctrine.outcome.even.title",
                "combat_doctrine.outcome.even.body",
                "combat_doctrine.balance_rule",
                "combat_doctrine.cycle_title",
                "combat_doctrine.cycle_hint",
                "combat_doctrine.server_disclosure"
            };
            foreach (HiveStrategicPathDefinition definition in HiveStrategicPathCatalog.All)
            {
                keys.Add(definition.NameKey);
                keys.Add(definition.RoleKey);
                keys.Add(definition.SummaryKey);
                keys.Add(definition.StrengthOneKey);
                keys.Add(definition.StrengthTwoKey);
                keys.Add(definition.TradeoffKey);
                keys.Add(definition.TrialScenarioKey);
                keys.Add(definition.TrialChoiceOneKey);
                keys.Add(definition.TrialChoiceTwoKey);
                keys.Add(definition.TrialFitKey);
                keys.Add(definition.TrialTradeoffKey);
            }
            foreach (HiveCombatDoctrineDefinition definition in HiveCombatDoctrineCatalog.All)
            {
                keys.Add(definition.NameKey);
                keys.Add(definition.RoleKey);
                keys.Add(definition.TechniqueKey);
            }
            foreach (string key in keys)
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, "Missing fr-CA " + key);
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, "Missing en-US " + key);
            }
        }

        private static void AssertCards(Rect[] cards, float screenWidth, float screenHeight)
        {
            Assert.That(cards.Length, Is.EqualTo(5));
            foreach (Rect card in cards)
            {
                Assert.That(card.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(card.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(card.x, Is.GreaterThanOrEqualTo(0f));
                Assert.That(card.y, Is.GreaterThanOrEqualTo(0f));
                Assert.That(card.xMax, Is.LessThanOrEqualTo(screenWidth));
                Assert.That(card.yMax, Is.LessThanOrEqualTo(screenHeight));
            }
            for (int index = 1; index < cards.Length; index++)
                Assert.That(cards[index - 1].yMax, Is.LessThanOrEqualTo(cards[index].y));
        }

        private static void AssertTrialControls(bool portrait, float screenWidth, float screenHeight)
        {
            Rect details = HiveViewProductUiPresenter.StrategicPathDetailRectForProof(portrait, screenWidth, screenHeight);
            Rect launch = HiveViewProductUiPresenter.StrategicPathTrialLaunchRectForProof(portrait, screenWidth, screenHeight);
            Rect back = HiveViewProductUiPresenter.StrategicPathTrialBackRectForProof(portrait, screenWidth, screenHeight);
            Rect result = HiveViewProductUiPresenter.StrategicPathTrialResultRectForProof(portrait, screenWidth, screenHeight);
            Rect[] choices = HiveViewProductUiPresenter.StrategicPathTrialChoiceRectsForProof(portrait, screenWidth, screenHeight);
            Assert.That(choices.Length, Is.EqualTo(2));
            foreach (Rect control in choices.Concat(new[] { launch, back }))
            {
                Assert.That(control.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(control.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(details.Contains(control.min), Is.True);
                Assert.That(details.Contains(new Vector2(control.xMax - 0.01f, control.yMax - 0.01f)), Is.True);
            }
            Assert.That(choices[0].xMax, Is.LessThanOrEqualTo(choices[1].x));
            Assert.That(result.height, Is.GreaterThanOrEqualTo(40f));
            AssertRow(
                HiveViewProductUiPresenter.StrategicPathTrialForProof("nurturer", 0, portrait, screenWidth, screenHeight),
                "strategic_path_trial_min_touch:" + (portrait ? "50" : "56"));
        }

        private static void AssertCombatDoctrineControls(bool portrait, float screenWidth, float screenHeight, float expectedMinimum)
        {
            Rect panel = HiveViewProductUiPresenter.StrategicPathPanelRectForProof(portrait, screenWidth, screenHeight);
            Rect entry = HiveViewProductUiPresenter.CombatDoctrineEntryRectForProof(portrait, screenWidth, screenHeight);
            Rect intro = HiveViewProductUiPresenter.CombatDoctrineIntroRectForProof(portrait, screenWidth, screenHeight);
            Rect result = HiveViewProductUiPresenter.CombatDoctrineResultRectForProof(portrait, screenWidth, screenHeight);
            Rect cycle = HiveViewProductUiPresenter.CombatDoctrineCycleRectForProof(portrait, screenWidth, screenHeight);
            Rect server = HiveViewProductUiPresenter.CombatDoctrineServerRectForProof(portrait, screenWidth, screenHeight);
            Rect[] attackers = HiveViewProductUiPresenter.CombatDoctrineFamilyRectsForProof(true, portrait, screenWidth, screenHeight);
            Rect[] defenders = HiveViewProductUiPresenter.CombatDoctrineFamilyRectsForProof(false, portrait, screenWidth, screenHeight);
            Assert.That(attackers.Length, Is.EqualTo(3));
            Assert.That(defenders.Length, Is.EqualTo(3));
            foreach (Rect control in attackers.Concat(defenders).Concat(new[] { entry }))
            {
                Assert.That(control.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(control.height, Is.GreaterThanOrEqualTo(44f));
                Assert.That(panel.Contains(control.min), Is.True);
                Assert.That(panel.Contains(new Vector2(control.xMax - 0.01f, control.yMax - 0.01f)), Is.True);
            }
            foreach (Rect surface in new[] { intro, result, cycle, server })
            {
                Assert.That(panel.Contains(surface.min), Is.True);
                Assert.That(panel.Contains(new Vector2(surface.xMax - 0.01f, surface.yMax - 0.01f)), Is.True);
            }
            for (int index = 1; index < attackers.Length; index++)
            {
                Assert.That(attackers[index - 1].xMax, Is.LessThanOrEqualTo(attackers[index].x));
                Assert.That(defenders[index - 1].xMax, Is.LessThanOrEqualTo(defenders[index].x));
            }
            Assert.That(attackers[0].yMax, Is.LessThanOrEqualTo(defenders[0].y));
            Assert.That(defenders[0].yMax, Is.LessThanOrEqualTo(result.y));
            Assert.That(result.yMax, Is.LessThanOrEqualTo(cycle.y));
            Assert.That(cycle.yMax, Is.LessThanOrEqualTo(server.y));
            AssertRow(
                HiveViewProductUiPresenter.CombatDoctrineForProof("guardians", "darters", portrait, screenWidth, screenHeight),
                "combat_doctrine_min_touch:" + expectedMinimum.ToString("0"));
        }

        private static void AssertRow(IEnumerable<string> rows, string expected)
        {
            Assert.That(rows, Does.Contain(expected));
        }
    }
}
