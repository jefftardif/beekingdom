using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Localization;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveFormationReadinessTests
    {
        public static void RunAllAssertions()
        {
            var tests = new SandboxLivingHiveFormationReadinessTests();
            tests.ProjectionKeepsDoctrineRosterSeparateFromLegacyRoles();
            tests.ZeroDoctrineCountsRemainKnownButUnavailable();
            tests.CompositionClampsToRosterAndCapacity();
            tests.RecommendationBuildsAnExplainableMixedSquad();
            tests.MixedCompositionProofSeparatesReadoutFromReservation();
            tests.ProofKeepsLegacyRolesUnclassifiedAndOfficialCommitClosed();
            tests.PortraitAndLandscapeLayoutsStayInsideTheMobilePanel();
            tests.DraftLifecycleAllowsEmptyRecruitableFamiliesAndResetsOnClose();
            tests.RecruitmentCompletesIntoTheNewRosterWithoutConvertingLegacyCounts();
            tests.AllFormationReadinessCopyExistsInBothCatalogs();
        }

        [Test]
        public void ProjectionKeepsDoctrineRosterSeparateFromLegacyRoles()
        {
            HiveFormationReadinessSnapshot snapshot = HiveFormationReadinessProjection.Project(8, 6, 8, 18, 5);

            Assert.That(snapshot.Families.Count, Is.EqualTo(3));
            Assert.That(snapshot.Find("guardians").State, Is.EqualTo(HiveFormationRosterState.Available));
            Assert.That(snapshot.Find("guardians").EligibleCount, Is.EqualTo(8));
            Assert.That(snapshot.Find("guardians").SourcePopulationId, Is.EqualTo("guardians"));
            Assert.That(snapshot.Find("wingrunners").State, Is.EqualTo(HiveFormationRosterState.Available));
            Assert.That(snapshot.Find("wingrunners").EligibleCount, Is.EqualTo(6));
            Assert.That(snapshot.Find("wingrunners").SourcePopulationId, Is.EqualTo("wingrunners"));
            Assert.That(snapshot.Find("darters").State, Is.EqualTo(HiveFormationRosterState.Available));
            Assert.That(snapshot.Find("darters").EligibleCount, Is.EqualTo(8));
            Assert.That(snapshot.Find("darters").SourcePopulationId, Is.EqualTo("darters"));
            Assert.That(snapshot.UnclassifiedSoldiers, Is.EqualTo(18));
            Assert.That(snapshot.UnclassifiedScouts, Is.EqualTo(5));
        }

        [Test]
        public void ZeroDoctrineCountsRemainKnownButUnavailable()
        {
            HiveFormationReadinessSnapshot snapshot = HiveFormationReadinessProjection.Project(-4, -3, -2, -2, -9);

            Assert.That(snapshot.Find("guardians").State, Is.EqualTo(HiveFormationRosterState.Empty));
            Assert.That(snapshot.Find("guardians").HasTrustedLocalMapping, Is.True);
            Assert.That(snapshot.Find("guardians").EligibleCount, Is.Zero);
            Assert.That(snapshot.Find("guardians").CanPrefillDraft, Is.False);
            Assert.That(snapshot.Find("wingrunners").State, Is.EqualTo(HiveFormationRosterState.Empty));
            Assert.That(snapshot.Find("wingrunners").HasTrustedLocalMapping, Is.True);
            Assert.That(snapshot.Find("darters").State, Is.EqualTo(HiveFormationRosterState.Empty));
            Assert.That(snapshot.Find("darters").HasTrustedLocalMapping, Is.True);
            Assert.That(snapshot.UnclassifiedSoldiers, Is.Zero);
            Assert.That(snapshot.UnclassifiedScouts, Is.Zero);
        }

        [Test]
        public void CompositionClampsToRosterAndCapacity()
        {
            HiveFormationReadinessSnapshot roster = HiveFormationReadinessProjection.Project(8, 6, 8, 18, 5);
            HiveSquadCompositionSnapshot draft = HiveSquadCompositionPlanner.CreateInitial(roster);
            Assert.That(draft.Guardians, Is.EqualTo(8));
            Assert.That(draft.Total, Is.EqualTo(8));

            draft = HiveSquadCompositionPlanner.Adjust(draft, roster, "wingrunners", 99);
            Assert.That(draft.Wingrunners, Is.EqualTo(4));
            Assert.That(draft.Total, Is.EqualTo(HiveSquadCompositionPlanner.InitialCapacity));
            draft = HiveSquadCompositionPlanner.Adjust(draft, roster, "darters", 1);
            Assert.That(draft.Darters, Is.Zero);

            draft = HiveSquadCompositionPlanner.Adjust(draft, roster, "guardians", -3);
            draft = HiveSquadCompositionPlanner.Adjust(draft, roster, "darters", 99);
            Assert.That(draft.Guardians, Is.EqualTo(5));
            Assert.That(draft.Wingrunners, Is.EqualTo(4));
            Assert.That(draft.Darters, Is.EqualTo(3));
            Assert.That(draft.Total, Is.EqualTo(12));
        }

        [Test]
        public void RecommendationBuildsAnExplainableMixedSquad()
        {
            HiveFormationReadinessSnapshot roster = HiveFormationReadinessProjection.Project(8, 6, 8, 18, 5);
            HiveSquadCompositionSnapshot recommendation = HiveSquadCompositionPlanner.Recommend(roster, "guardians");
            Assert.That(recommendation.Guardians, Is.EqualTo(3));
            Assert.That(recommendation.Wingrunners, Is.EqualTo(6));
            Assert.That(recommendation.Darters, Is.EqualTo(3));
            Assert.That(recommendation.Total, Is.EqualTo(12));

            HiveSquadDoctrineAssessment assessment = HiveSquadCompositionPlanner.Assess(recommendation, "guardians");
            Assert.That(assessment.ResponsiveCount, Is.EqualTo(6));
            Assert.That(assessment.ExposedCount, Is.EqualTo(3));
            Assert.That(assessment.NeutralCount, Is.EqualTo(3));
            Assert.That(assessment.Outcome, Is.EqualTo(HiveCombatDoctrineOutcome.Advantage));
        }

        [Test]
        public void MixedCompositionProofSeparatesReadoutFromReservation()
        {
            string[] rows = HiveViewProductUiPresenter.FormationReadinessForProof(
                8, 6, 8, 18, 5,
                3, 6, 3,
                "wingrunners", "guardians",
                true, 390f, 844f);

            AssertRow(rows, "formation_readiness_composition_total:12");
            AssertRow(rows, "formation_readiness_composition_remaining:0");
            AssertRow(rows, "formation_readiness_responsive_count:6");
            AssertRow(rows, "formation_readiness_exposed_count:3");
            AssertRow(rows, "formation_readiness_neutral_count:3");
            AssertRow(rows, "formation_readiness_outcome:advantage");
            AssertRow(rows, "formation_readiness_units_reserved_locally:false");
            AssertRow(rows, "formation_readiness_official_commit_enabled:false");
            AssertRow(rows, "formation_readiness_battle_simulated:false");
        }

        [Test]
        public void ProofKeepsLegacyRolesUnclassifiedAndOfficialCommitClosed()
        {
            string[] rows = HiveViewProductUiPresenter.FormationReadinessForProof(8, 6, 8, 18, 5, "guardians", "darters", true, 390f, 844f);

            AssertRow(rows, "formation_readiness_enabled:true");
            AssertRow(rows, "formation_readiness_catalog_version:phase4-combat-v1");
            AssertRow(rows, "formation_readiness_guardians_mapped:true");
            AssertRow(rows, "formation_readiness_guardians_count:8");
            AssertRow(rows, "formation_readiness_wingrunners_mapped:true");
            AssertRow(rows, "formation_readiness_wingrunners_count:6");
            AssertRow(rows, "formation_readiness_darters_mapped:true");
            AssertRow(rows, "formation_readiness_darters_count:8");
            AssertRow(rows, "formation_readiness_legacy_soldiers_unclassified:18");
            AssertRow(rows, "formation_readiness_legacy_scouts_unclassified:5");
            AssertRow(rows, "formation_readiness_auto_maps_soldiers_to_darters:false");
            AssertRow(rows, "formation_readiness_auto_maps_scouts_to_wingrunners:false");
            AssertRow(rows, "formation_readiness_prefill_count:8");
            AssertRow(rows, "formation_readiness_composition_contract_version:phase4-combat-squad-reservation-v1");
            AssertRow(rows, "formation_readiness_composition_capacity:12");
            AssertRow(rows, "formation_readiness_composition_guardians:8");
            AssertRow(rows, "formation_readiness_composition_wingrunners:0");
            AssertRow(rows, "formation_readiness_composition_darters:0");
            AssertRow(rows, "formation_readiness_composition_total:8");
            AssertRow(rows, "formation_readiness_responsive_count:8");
            AssertRow(rows, "formation_readiness_exposed_count:0");
            AssertRow(rows, "formation_readiness_outcome:advantage");
            AssertRow(rows, "formation_readiness_device_roster_source:local_preview_non_official");
            AssertRow(rows, "formation_readiness_recruitment_catalog_version:phase4-combat-recruitment-v1");
            AssertRow(rows, "formation_readiness_recruitment_source:guard_post");
            AssertRow(rows, "formation_readiness_legacy_counts_converted:false");
            AssertRow(rows, "formation_readiness_device_draft:volatile_in_memory_only");
            AssertRow(rows, "formation_readiness_server_contract_status:http_recruitment_readiness_and_squad_reservation_features_closed_not_connected");
            AssertRow(rows, "formation_readiness_official_commit_enabled:false");
            AssertRow(rows, "formation_readiness_units_reserved_locally:false");
            AssertRow(rows, "formation_readiness_battle_simulated:false");
            AssertRow(rows, "formation_readiness_mutates_gameplay:false");
            AssertRow(rows, "formation_readiness_changes_protected_art:false");

            string[] unavailable = HiveViewProductUiPresenter.FormationReadinessForProof(8, 0, 0, 18, 5, "darters", "wingrunners", false, 1600f, 900f);
            AssertRow(unavailable, "formation_readiness_selected_family_available:false");
            AssertRow(unavailable, "formation_readiness_prefill_count:0");
            AssertRow(unavailable, "formation_readiness_outcome:pending");
        }

        [Test]
        public void PortraitAndLandscapeLayoutsStayInsideTheMobilePanel()
        {
            AssertLayout(true, 390f, 844f);
            AssertLayout(false, 1600f, 900f);
        }

        [Test]
        public void DraftLifecycleAllowsEmptyRecruitableFamiliesAndResetsOnClose()
        {
            try
            {
                HiveViewProductUiPresenter.PrepareFormationReadinessCaptureForProof(true);
                Assert.That(HiveViewProductUiPresenter.FormationReadinessOpenForProof, Is.True);
                Assert.That(HiveViewProductUiPresenter.FormationReadinessFamilyIndexForProof, Is.Zero);
                Assert.That(HiveViewProductUiPresenter.FormationDraftGuardiansForProof, Is.EqualTo(8));
                HiveViewProductUiPresenter.PrepareSquadCompositionCaptureForProof(true);
                HiveViewProductUiPresenter.ChooseFormationReadinessForProof(1, 0);
                HiveViewProductUiPresenter.RecommendFormationCompositionForProof();
                Assert.That(HiveViewProductUiPresenter.FormationReadinessFamilyIndexForProof, Is.EqualTo(1));
                Assert.That(HiveViewProductUiPresenter.FormationReadinessThreatIndexForProof, Is.Zero);
                Assert.That(HiveViewProductUiPresenter.FormationDraftGuardiansForProof, Is.EqualTo(3));
                Assert.That(HiveViewProductUiPresenter.FormationDraftWingrunnersForProof, Is.EqualTo(6));
                Assert.That(HiveViewProductUiPresenter.FormationDraftDartersForProof, Is.EqualTo(3));
                HiveViewProductUiPresenter.AdjustFormationCompositionForProof("guardians", -1);
                Assert.That(HiveViewProductUiPresenter.FormationDraftGuardiansForProof, Is.EqualTo(2));
                HiveViewProductUiPresenter.CloseFormationReadinessForProof();
                Assert.That(HiveViewProductUiPresenter.FormationReadinessOpenForProof, Is.False);
                Assert.That(HiveViewProductUiPresenter.FormationReadinessFamilyIndexForProof, Is.Zero);
                Assert.That(HiveViewProductUiPresenter.FormationReadinessThreatIndexForProof, Is.EqualTo(-1));
                Assert.That(HiveViewProductUiPresenter.FormationDraftGuardiansForProof, Is.Zero);
                Assert.That(HiveViewProductUiPresenter.FormationDraftWingrunnersForProof, Is.Zero);
                Assert.That(HiveViewProductUiPresenter.FormationDraftDartersForProof, Is.Zero);
            }
            finally
            {
                HiveViewProductUiPresenter.CloseFormationReadinessForProof();
            }
        }

        [Test]
        public void RecruitmentCompletesIntoTheNewRosterWithoutConvertingLegacyCounts()
        {
            try
            {
                HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
                var progressStore = new MemoryHiveProgressStore();
                HiveViewProductUiPresenter.UseLocalPreviewHiveProgressStoreForProof(progressStore);
                HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueStore());

                Assert.That(HiveViewProductUiPresenter.StartDoctrineRecruitmentForProof("wingrunners"), Is.True);
                AssertRow(HiveViewProductUiPresenter.DoctrineRecruitmentForProof(), "doctrine_recruitment_queue_target:Voltigeuses");
                HiveViewProductUiPresenter.CompleteDoctrineRecruitmentForProof();

                string[] completed = HiveViewProductUiPresenter.DoctrineRecruitmentForProof();
                AssertRow(completed, "doctrine_recruitment_wingrunners:6");
                AssertRow(completed, "doctrine_recruitment_legacy_soldiers:18");
                AssertRow(completed, "doctrine_recruitment_legacy_scouts:5");
                AssertRow(completed, "doctrine_recruitment_legacy_counts_converted:false");

                HiveViewProductUiPresenter.SimulateLocalPreviewHiveProgressRestartForProof();
                string[] restored = HiveViewProductUiPresenter.LocalPreviewHiveProgressForProof();
                AssertRow(restored, "progress_wingrunners:6");
                AssertRow(restored, "progress_darters:0");
                AssertRow(restored, "progress_soldiers:18");
                AssertRow(restored, "progress_scouts:5");
            }
            finally
            {
                HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueStore());
                HiveViewProductUiPresenter.UseLocalPreviewHiveProgressStoreForProof(new MemoryHiveProgressStore());
            }
        }

        [Test]
        public void AllFormationReadinessCopyExistsInBothCatalogs()
        {
            string[] keys =
            {
                "formation_readiness.entry",
                "formation_readiness.opened",
                "formation_readiness.closed",
                "formation_readiness.title",
                "formation_readiness.subtitle",
                "formation_readiness.back",
                "formation_readiness.local_disclosure",
                "formation_readiness.roster_label",
                "formation_readiness.reserve",
                "formation_readiness.no_auto_mapping",
                "formation_readiness.server_title",
                "formation_readiness.server_disclosure",
                "formation_readiness.family_selected",
                "formation_readiness.family_empty",
                "formation_readiness.family_not_mapped",
                "formation_readiness.family_available",
                "formation_readiness.composition.family_count",
                "formation_readiness.composition.total",
                "formation_readiness.composition.recommend",
                "formation_readiness.composition.added",
                "formation_readiness.composition.removed",
                "formation_readiness.composition.full",
                "formation_readiness.composition.bound",
                "formation_readiness.composition.need_threat",
                "formation_readiness.composition.recommended",
                "formation_readiness.commit.empty",
                "formation_readiness.commit.closed",
                "formation_readiness.family_empty_short",
                "formation_readiness.family_not_recorded",
                "formation_readiness.role.guardians.short",
                "formation_readiness.role.wingrunners.short",
                "formation_readiness.role.darters.short",
                "formation_readiness.threat_label",
                "formation_readiness.threat_selected",
                "formation_readiness.result.unavailable.title",
                "formation_readiness.result.unavailable.body",
                "formation_readiness.result.ready.title",
                "formation_readiness.result.ready.body",
                "formation_readiness.result.advantage.title",
                "formation_readiness.result.advantage.body",
                "formation_readiness.result.vulnerable.title",
                "formation_readiness.result.vulnerable.body",
                "formation_readiness.result.even.title",
                "formation_readiness.result.even.body",
                "formation_readiness.recruitment.start",
                "formation_readiness.recruitment.running",
                "formation_readiness.recruitment.cost"
            };
            foreach (string key in keys)
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, "Missing fr-CA " + key);
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, "Missing en-US " + key);
            }
        }

        private static void AssertLayout(bool portrait, float screenWidth, float screenHeight)
        {
            Rect panel = HiveViewProductUiPresenter.ArmyMenuPanelRectForProof(portrait, screenWidth, screenHeight, true);
            Rect back = HiveViewProductUiPresenter.FormationReadinessBackRectForProof(portrait, screenWidth, screenHeight);
            Rect[] families = HiveViewProductUiPresenter.FormationReadinessFamilyRectsForProof(portrait, screenWidth, screenHeight);
            Rect composer = HiveViewProductUiPresenter.FormationReadinessComposerRectForProof(portrait, screenWidth, screenHeight);
            Rect decrease = HiveViewProductUiPresenter.FormationReadinessDecreaseRectForProof(portrait, screenWidth, screenHeight);
            Rect increase = HiveViewProductUiPresenter.FormationReadinessIncreaseRectForProof(portrait, screenWidth, screenHeight);
            Rect recommend = HiveViewProductUiPresenter.FormationReadinessRecommendRectForProof(portrait, screenWidth, screenHeight);
            Rect reserve = HiveViewProductUiPresenter.FormationReadinessReserveRectForProof(portrait, screenWidth, screenHeight);
            Rect[] threats = HiveViewProductUiPresenter.FormationReadinessThreatRectsForProof(portrait, screenWidth, screenHeight);
            Rect result = HiveViewProductUiPresenter.FormationReadinessResultRectForProof(portrait, screenWidth, screenHeight);
            Rect recruit = HiveViewProductUiPresenter.FormationReadinessRecruitRectForProof(portrait, screenWidth, screenHeight);
            Rect server = HiveViewProductUiPresenter.FormationReadinessServerRectForProof(portrait, screenWidth, screenHeight);
            Rect commit = HiveViewProductUiPresenter.FormationReadinessCommitRectForProof(portrait, screenWidth, screenHeight);
            Assert.That(families.Length, Is.EqualTo(3));
            Assert.That(threats.Length, Is.EqualTo(3));
            foreach (Rect control in families.Concat(threats).Concat(new[] { back, recruit, decrease, increase, recommend, commit }))
            {
                Assert.That(control.width, Is.GreaterThanOrEqualTo(44f));
                Assert.That(control.height, Is.GreaterThanOrEqualTo(44f));
                AssertInside(panel, control);
            }
            foreach (Rect surface in new[] { composer, reserve, result, server }) AssertInside(panel, surface);
            for (int index = 1; index < families.Length; index++)
            {
                Assert.That(families[index - 1].xMax, Is.LessThanOrEqualTo(families[index].x));
                Assert.That(threats[index - 1].xMax, Is.LessThanOrEqualTo(threats[index].x));
            }
            Assert.That(families[0].yMax, Is.LessThanOrEqualTo(composer.y));
            Assert.That(composer.yMax, Is.LessThanOrEqualTo(reserve.y));
            Assert.That(reserve.yMax, Is.LessThanOrEqualTo(threats[0].y));
            Assert.That(threats[0].yMax, Is.LessThanOrEqualTo(result.y));
            Assert.That(result.yMax, Is.LessThanOrEqualTo(server.y));
            AssertRow(
                HiveViewProductUiPresenter.FormationReadinessForProof(8, 6, 8, 18, 5, "guardians", "darters", portrait, screenWidth, screenHeight),
                "formation_readiness_min_touch:44");
        }

        private sealed class MemoryHiveProgressStore : ILocalPreviewHiveProgressStore
        {
            private string value = string.Empty;
            public string Read() => value;
            public void Write(string json) => value = json ?? string.Empty;
            public void Delete() => value = string.Empty;
        }

        private sealed class MemoryQueueStore : ILocalPreviewQueueJournalStore
        {
            private string value = string.Empty;
            public string Read() => value;
            public void Write(string json) => value = json ?? string.Empty;
            public void Delete() => value = string.Empty;
        }

        private static void AssertInside(Rect outer, Rect inner)
        {
            Assert.That(outer.Contains(inner.min), Is.True);
            Assert.That(outer.Contains(new Vector2(inner.xMax - 0.01f, inner.yMax - 0.01f)), Is.True);
        }

        private static void AssertRow(IEnumerable<string> rows, string expected)
        {
            Assert.That(rows, Does.Contain(expected));
        }
    }
}
