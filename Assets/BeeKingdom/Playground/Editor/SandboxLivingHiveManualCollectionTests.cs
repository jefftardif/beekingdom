using System;
using System.Globalization;
using System.Linq;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLivingHiveManualCollectionTests
    {
        public static void RunAllForBatch()
        {
            try
            {
                var tests = new SandboxLivingHiveManualCollectionTests();
                tests.CollectionIsIconOnlyAndPreservesCanonicalArt();
                tests.LocalizationCatalogsSwitchTutorialAndBuildingTextAtRuntime();
                tests.SplashLanguageSelectorPersistsOnDeviceAndUsesMobileSafeRects();
                tests.OfficialAccountShellNeverCollectsCredentialsWhileEitherGateIsClosed();
                var mobileAccountUi = new MobileAccountSessionUiTests();
                mobileAccountUi.OfficialLoginTargetsAreMobileSafeAndStayInsidePanel(true, 390f, 844f);
                mobileAccountUi.OfficialLoginTargetsAreMobileSafeAndStayInsidePanel(false, 1600f, 900f);
                mobileAccountUi.PresenterOnlyExposesCredentialFormWhenClientAndServerGatesAreReady();
                mobileAccountUi.OfficialTransportRequiresTlsAndKeepsRoutesExplicit();
                mobileAccountUi.EditorNeverPretendsToProvideAndroidProtectedTokenStorage();
                mobileAccountUi.OfficialGameTransportRequiresTlsAndOwnsNoAutomaticRetry();
                mobileAccountUi.EditorNeverPretendsToProvideAndroidProtectedGameCache();
                tests.ChapterOneFirstSessionSurfaceHasFrenchAndEnglishCopy();
                tests.EveryGuidedChapterOpensWithNarratedFullscreenIntro();
                tests.LeftQueueRailReflectsOnlyRealRuntimeActions();
                tests.LocalPreviewQueuesRestoreAcrossRestartAndCompleteOnce();
                tests.QueueReturnSummaryKeepsCompletedAwayVisibleAndRoutesWithoutCollection();
                tests.LocalPreviewResearchPersistsCostsAndCompletesExactlyOnce();
                tests.ResearchShortageShowsExactMissingAmountAndRoutesWithoutProgress();
                tests.LocalPreviewQueueJournalMigratesV1ToResearchAwareV2();
                tests.HiveProgressMigratesV1WithoutInventingDoctrineCounts();
                tests.HiveProgressPersistsMultipleBuildingsAndPopulationsAcrossRestarts();
                tests.CompletedQueueOperationsMergeIntoHiveProgressWithoutRollback();
                tests.HiveProgressRejectsWrongProfilesAndSanitizesBoundedData();
                SandboxLivingHiveOfflineProductionTests.RunAllAssertions();
                SandboxLivingHiveBuildingUpgradeTests.RunAllAssertions();
                SandboxLivingHiveOfficialResearchTests.RunAllAssertions();
                SandboxLivingHiveOfficialStockTests.RunAllAssertions();
                SandboxLivingHiveOfficialDailyRoundTests.RunAllAssertions();
                SandboxLivingHiveOfficialBroodCareTests.RunAllAssertions();
                SandboxLivingHiveMobileComfortTests.RunAllAssertions();
                SandboxLivingHiveProductionForecastTests.RunAllAssertions();
                SandboxLivingHiveBuildingActivityTests.RunAllAssertions();
                SandboxLivingHiveSpecializedBeeActivityTests.RunAllAssertions();
                SandboxLivingHiveReactiveAmbienceTests.RunAllAssertions();
                SandboxLivingHiveStrategicPathTests.RunAllAssertions();
                SandboxLivingHiveFormationReadinessTests.RunAllAssertions();
                SandboxLivingHivePerimeterSortieTests.RunAllAssertions();
                tests.HiveLedgerExposesStocksCommitmentsAndManualNavigationOnly();
                tests.DailyHiveRoundRequiresThreeRealActionsAndClaimsExactlyOnce();
                tests.GuidedTutorialRestartsAtSafeChapterBoundaryWithoutRewardReplay();
                tests.StrategicProfileSeparatesStructuralAndOperationalEffects();
                tests.StrategicProfileRestoresChoicesAndDerivedEffects();
                tests.StrategicProfileMigratesV1AndAppliesOpeningCharterOnce();
                tests.StrategicProfileMigratesV9WithoutReplayingWorkshopCommission();
                tests.StrategicProfileMigratesV10WithoutReplayingWorldBriefing();
                tests.StrategicProfileMigratesV11WithNoImplicitOpeningReward();
                Assert.That(HiveViewProductUiPresenter.PlayerProfileRowsDoNotOverlapForProof(), Is.True);
                tests.ProductionAccumulatesWithoutCreditingTheHud();
                tests.CollectingHoneyCreditsCapacityAndEmptiesTheBuilding();
                tests.FullCapacityBlocksCollectionWithoutLosingProduction();
                tests.NonProductionBuildingsCannotBeCollected();
                tests.GuidedFirstChapterTargetsHoneyAndOpensBroodChapter();
                tests.GuidedFirstChapterCanPrioritizeReserveYieldInstead();
                tests.GuidedOpeningActPacingProfileExposesTheCurrentGap();
                tests.GuidedOpeningActObjectivePacingIdentifiesTheWeakestInteractionChapter();
                tests.GuidedBroodChapterTargetsNurseryAndFeedsOnce();
                tests.GuidedBroodChapterCanSpendWaxForStrongerStability();
                tests.GuidedBroodGrowthPlanDiscountsChapterThreeCare();
                tests.GuidedBroodCareCircuitRequiresTwoManualCollectionsAndTreatments();
                tests.GuidedBroodIncubationCanPrioritizeResilience();
                tests.GuidedWorkerChapterFormsTheFirstWorkerAfterTheTimer();
                tests.GuidedWorkerCanTryNurseryBeforeChoosingTheReserve();
                tests.GuidedWorkerCertificationRequiresTwoQualityControlledRounds();
                tests.GuidedWorkerApplicationToolkitReducesTheFirstWorkshopApplicationOnce();
                tests.GuidedUpgradeChapterImprovesWaxProductionAfterTheTimer();
                tests.GuidedUpgradeChapterCanExpandWaxStorageInstead();
                tests.GuidedUpgradeOperationsCanAdoptTheCadenceDoctrine();
                tests.GuidedDefenseChapterMobilizesGuardiansAndResolvesThreat();
                tests.GuidedDefenseChapterCanBuildAWaxBarrierInstead();
                tests.GuidedReadinessLoopRequiresTwoManualProductionCycles();
                tests.GuidedOpeningActTracksSeparatelyClaimedChapterRewards();
                tests.GuidedWorldChapterUsesTheCanonical50x50MapAndReturnsToHive();
                tests.GuidedForagingChapterDispatchesAndRequiresManualClaim();
                tests.AmbientTrafficUsesPremiumBeeWithinMobileBudgets();
                tests.NurseryVitalityLayerUsesAuthoritativeSnapshotBoundariesAndMobileSafeRects();
                tests.BroodFeedingConsumesHoneyAndStopsAtFullNutrition();
                tests.NurseryFormsOneWorkerOnlyAfterTheTrainingTimer();
                Debug.Log("LivingHive manual collection checks passed.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("LivingHive manual collection checks failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [Test]
        public void CollectionIsIconOnlyAndPreservesCanonicalArt()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            string[] rows = HiveViewProductUiPresenter.ManualProductionCollectionForProof();

            AssertRow(rows, "manual_collection_enabled:true");
            AssertRow(rows, "automatic_resource_credit:false");
            AssertRow(rows, "ready_marker:resource_icon_only");
            AssertRow(rows, "ready_marker_permanent_text:false");
            AssertRow(rows, "hive_background_image_modified:false");
            AssertRow(rows, "world_map_terrain_modified:false");
        }

        [Test]
        public void EveryGuidedChapterOpensWithNarratedFullscreenIntro()
        {
            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            AssertGuidedChapterIntro(1, "tutorial.chapter_01.intro.fr-CA", HiveViewProductUiPresenter.BeginGuidedCollectionTutorialForProof);
            AssertGuidedChapterIntro(2, "tutorial.chapter_02.intro.fr-CA", HiveViewProductUiPresenter.BeginGuidedBroodTutorialForProof);
            AssertGuidedChapterIntro(3, "tutorial.chapter_03.intro.fr-CA", HiveViewProductUiPresenter.BeginGuidedWorkerTutorialForProof);
            AssertGuidedChapterIntro(4, "tutorial.chapter_04.intro.fr-CA", HiveViewProductUiPresenter.BeginGuidedUpgradeTutorialForProof);
            AssertGuidedChapterIntro(5, "tutorial.chapter_05.intro.fr-CA", HiveViewProductUiPresenter.BeginGuidedDefenseTutorialForProof);
            AssertGuidedChapterIntro(6, "tutorial.chapter_06.intro.fr-CA", HiveViewProductUiPresenter.BeginGuidedWorldTransitionForProof);
            AssertGuidedChapterIntro(7, "tutorial.chapter_07.intro.fr-CA", HiveViewProductUiPresenter.BeginGuidedForagingTutorialForProof);
        }

        [Test]
        public void LocalizationCatalogsSwitchTutorialAndBuildingTextAtRuntime()
        {
            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "tutorial.chapter_04.intro.narration"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "tutorial.chapter_04.intro.narration"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "chat.translate"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "chat.translate"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "tutorial.chapter_02.handoff.body"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "tutorial.chapter_02.handoff.body"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "tutorial.chapter_01.route_feedback.nursery"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "tutorial.chapter_01.route_feedback.reserve"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "ui.strategic_profile.choice.brood_bridge"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "ui.strategic_profile.choice.secure_reserve"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "tutorial.chapter_05.world_briefing.choice.title"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "tutorial.chapter_05.world_briefing.simulation.signal.correct"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "tutorial.chapter_06.locate.sun_beacon"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "ui.strategic_profile.choice.guarded_return"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "tutorial.chapter_04.doctrine.check.traceability"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "tutorial.chapter_04.doctrine.cadence.result"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "tutorial.chapter_01.charter.check.ledger"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "tutorial.chapter_01.charter.check.alert"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "tutorial.chapter_05.debrief.check.breach"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "tutorial.chapter_05.debrief.recommendation.watch"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "living_hive.brood_vitality.metrics"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "living_hive.brood_vitality.tier.thriving"), Is.True);
            Assert.That(BeeLocalization.HasText("fr-CA", "tutorial.chapter_04.batch_qualification.production.body"), Is.True);
            Assert.That(BeeLocalization.HasText("en-US", "tutorial.chapter_04.batch_qualification.confirmed.capacity"), Is.True);

            HiveViewProductUiPresenter.BeginGuidedUpgradeTutorialForProof();
            string frenchNarration = HiveViewProductUiPresenter.GuidedChapterNarrationTextForProof();
            Assert.That(HiveViewProductUiPresenter.GuidedChapterNarrationCueForProof(), Is.EqualTo("tutorial.chapter_04.intro.fr-CA"));
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("nursery_cluster");
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Nurserie"));

            Assert.That(HiveViewProductUiPresenter.SetLocaleForRuntime("en-US"), Is.True);
            string englishNarration = HiveViewProductUiPresenter.GuidedChapterNarrationTextForProof();
            Assert.That(englishNarration, Is.Not.EqualTo(frenchNarration));
            Assert.That(HiveViewProductUiPresenter.GuidedChapterNarrationCueForProof(), Is.EqualTo("tutorial.chapter_04.intro.en-US"));
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Nursery"));

            Assert.That(HiveViewProductUiPresenter.SetLocaleForRuntime("fr-CA"), Is.True);
        }

        [Test]
        public void ChapterOneFirstSessionSurfaceHasFrenchAndEnglishCopy()
        {
            string[] keys =
            {
                "tutorial.shell.chapter",
                "tutorial.shell.act_progress",
                "tutorial.shell.current_objective",
                "tutorial.chapter_01.welcome.title",
                "tutorial.chapter_01.welcome.body",
                "tutorial.chapter_01.collect.body",
                "tutorial.chapter_01.first_collection.body",
                "tutorial.chapter_01.production.choice.body",
                "tutorial.chapter_01.production.steady.button",
                "tutorial.chapter_01.production.surge.button",
                "tutorial.chapter_01.allocation.body",
                "tutorial.chapter_01.allocation.brood.button",
                "tutorial.chapter_01.allocation.reserve.button",
                "tutorial.chapter_01.installation.body",
                "tutorial.chapter_01.circuit.route.choice.title",
                "tutorial.chapter_01.circuit.route.nursery.body",
                "tutorial.chapter_01.circuit.collect.body",
                "tutorial.chapter_01.circuit.maintenance.choice.body",
                "tutorial.chapter_01.circuit.result.body",
                "tutorial.chapter_01.circuit.check.body",
                "tutorial.chapter_01.commissioning.load.progress",
                "tutorial.chapter_01.brood_supply.jelly.button",
                "tutorial.chapter_01.brood_supply.check.freshness",
                "tutorial.chapter_01.brood_supply.progress"
            };

            foreach (string key in keys)
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, "Clé fr-CA absente: " + key);
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, "Clé en-US absente: " + key);
            }

            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            string frenchWelcome = BeeLocalization.Text("tutorial.chapter_01.welcome.body");
            Assert.That(BeeLocalization.Format("tutorial.shell.chapter", 1), Is.EqualTo("CHAPITRE 1"));
            Assert.That(BeeLocalization.Format("tutorial.chapter_01.first_collection.body", 240), Does.Contain("240 miel"));

            Assert.That(BeeLocalization.SetLocale("en-US"), Is.True);
            Assert.That(BeeLocalization.Text("tutorial.chapter_01.welcome.body"), Is.Not.EqualTo(frenchWelcome));
            Assert.That(BeeLocalization.Format("tutorial.shell.chapter", 1), Is.EqualTo("CHAPTER 1"));
            Assert.That(BeeLocalization.Format("tutorial.chapter_01.first_collection.body", 240), Does.Contain("240 honey"));

            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
        }

        [Test]
        public void SplashLanguageSelectorPersistsOnDeviceAndUsesMobileSafeRects()
        {
            string originalSavedLocale = BeeLocalization.SavedLocale;
            string originalActiveLocale = BeeLocalization.CurrentLocale;
            try
            {
                BeeLocalization.ClearSavedLocale();
                Assert.That(BeeLocalization.ApplySavedOrSystemLocale(SystemLanguage.French), Is.True);
                Assert.That(BeeLocalization.CurrentLocale, Is.EqualTo("fr-CA"));
                Assert.That(BeeLocalization.SavedLocale, Is.Empty);

                Assert.That(HiveViewProductUiPresenter.SetPreferredLocaleForRuntime("en-US"), Is.True);
                Assert.That(BeeLocalization.CurrentLocale, Is.EqualTo("en-US"));
                Assert.That(BeeLocalization.SavedLocale, Is.EqualTo("en-US"));
                Assert.That(BeeLocalization.Text("splash.tab.home"), Is.EqualTo("Home"));
                Assert.That(BeeLocalization.Text("splash.message.language_saved"), Does.Contain("device"));

                Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
                Assert.That(BeeLocalization.ApplySavedOrSystemLocale(SystemLanguage.French), Is.True);
                Assert.That(BeeLocalization.CurrentLocale, Is.EqualTo("en-US"), "La préférence sauvegardée doit primer sur la langue système.");
                Assert.That(BeeLocalization.SetLocaleAndSave("de-DE"), Is.False);
                Assert.That(BeeLocalization.SavedLocale, Is.EqualTo("en-US"), "Une locale non prise en charge ne doit pas écraser la préférence valide.");

                AssertLanguageButtonsAreMobileSafe(true, 390f, 844f);
                AssertLanguageButtonsAreMobileSafe(false, 1600f, 900f);
                foreach (string key in new[]
                {
                    "splash.language.label",
                    "splash.demo.disclaimer",
                    "splash.home.play",
                    "splash.login.submit",
                    "splash.create.submit",
                    "splash.message.name_required",
                    "splash.message.language_saved"
                })
                {
                    Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, "Clé d'entrée fr-CA absente: " + key);
                    Assert.That(BeeLocalization.HasText("en-US", key), Is.True, "Clé d'entrée en-US absente: " + key);
                }
            }
            finally
            {
                BeeLocalization.ClearSavedLocale();
                if (!string.IsNullOrWhiteSpace(originalSavedLocale))
                {
                    BeeLocalization.SetLocaleAndSave(originalSavedLocale);
                }
                else
                {
                    BeeLocalization.SetLocale(originalActiveLocale);
                }
            }
        }

        private static void AssertLanguageButtonsAreMobileSafe(bool portrait, float screenWidth, float screenHeight)
        {
            Rect panel = HiveViewProductUiPresenter.SplashAuthPanelRectForProof(portrait, screenWidth, screenHeight);
            Rect[] buttons = HiveViewProductUiPresenter.SplashLanguageSelectorRectsForProof(portrait, screenWidth, screenHeight);
            Assert.That(buttons.Length, Is.EqualTo(2));
            Assert.That(buttons[0].width, Is.GreaterThanOrEqualTo(44f));
            Assert.That(buttons[0].height, Is.GreaterThanOrEqualTo(44f));
            Assert.That(buttons[1].width, Is.GreaterThanOrEqualTo(44f));
            Assert.That(buttons[1].height, Is.GreaterThanOrEqualTo(44f));
            Assert.That(buttons[0].xMin, Is.GreaterThanOrEqualTo(panel.xMin));
            Assert.That(buttons[1].xMax, Is.LessThanOrEqualTo(panel.xMax));
            Assert.That(buttons[0].yMin, Is.GreaterThanOrEqualTo(panel.yMin));
            Assert.That(buttons[1].yMax, Is.LessThanOrEqualTo(panel.yMax));
            Assert.That(buttons[0].Overlaps(buttons[1]), Is.False);
        }

        [Test]
        public void OfficialAccountShellNeverCollectsCredentialsWhileEitherGateIsClosed()
        {
            var gate = new MobileAccountSessionGate();
            Assert.That(gate.CanCollectCredentials, Is.False);
            Assert.That(gate.CanSubmitLogin, Is.False);
            Assert.That(gate.CanCreateOfficialAccount, Is.False);

            gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(false, false, false, false, false));
            Assert.That(gate.Snapshot.State, Is.EqualTo(AccountSessionReadinessState.PreparationOnly));
            Assert.That(gate.CanCollectCredentials, Is.False, "Un transport mobile ne doit pas contourner une porte serveur fermée.");

            gate.ConfigureTransport(false);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(true, true, true, true, true));
            Assert.That(gate.Snapshot.State, Is.EqualTo(AccountSessionReadinessState.Ready));
            Assert.That(gate.CanCollectCredentials, Is.False, "Un serveur prêt ne doit pas contourner l’absence de transport mobile sécurisé.");

            gate.ConfigureTransport(true);
            Assert.That(gate.CanCollectCredentials, Is.True);
            Assert.That(gate.CanSubmitLogin, Is.True);
            Assert.That(gate.CanCreateOfficialAccount, Is.True);

            gate.ResetForLogoutOrPlayerChange();
            Assert.That(gate.CanCollectCredentials, Is.False);
            Assert.That(gate.TransportConfigured, Is.False);
            Assert.That(gate.Snapshot.State, Is.EqualTo(AccountSessionReadinessState.NotConfigured));

            HiveViewProductUiPresenter.SetLocaleForRuntime("fr-CA");
            HiveViewProductUiPresenter.SetSplashAuthGateForProof("login");
            HiveViewProductUiPresenter.SetAccountSessionReadinessForProof("preparation");
            string[] rows = HiveViewProductUiPresenter.SplashAuthDemoForProof();
            AssertRow(rows, "login_credential_form_visible:false");
            AssertRow(rows, "official_account_creation_form_visible:false");
            AssertRow(rows, "password_collection_while_closed:false");
            AssertRow(rows, "account_shell_state:PreparationOnly");
            AssertRow(rows, "credential_collection_allowed:false");
            AssertRow(rows, "access_token_stored_here:false");
            AssertRow(rows, "refresh_token_stored_here:false");
            AssertRow(rows, "password_stored_here:false");

            foreach (string key in new[]
            {
                "splash.account.not_configured.title",
                "splash.account.not_configured.body",
                "splash.account.preparation.title",
                "splash.login.local_demo",
                "splash.create.demo_body",
                "splash.message.official_unavailable"
            })
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, "Clé auth fr-CA absente: " + key);
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, "Clé auth en-US absente: " + key);
            }
        }

        [Test]
        public void LeftQueueRailReflectsOnlyRealRuntimeActions()
        {
            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            string[] idleRows = HiveViewProductUiPresenter.QueueRailForProof();
            AssertRow(idleRows, "queue_slots:3");
            AssertRow(idleRows, "construction_active:false");
            AssertRow(idleRows, "training_active:false");
            AssertRow(idleRows, "research_active:false");
            AssertRow(idleRows, "static_placeholder_timers:false");

            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("nursery_cluster");
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_action_confirm_upgrade");
            string[] upgradeRows = HiveViewProductUiPresenter.QueueRailForProof();
            AssertRow(upgradeRows, "construction_active:true");
            AssertRow(upgradeRows, "construction_target:nursery_cluster");
            AssertRow(upgradeRows, "construction_label:Nurserie");

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("player_disabled_queue_busy");
            string[] trainingRows = HiveViewProductUiPresenter.QueueRailForProof();
            AssertRow(trainingRows, "construction_active:false");
            AssertRow(trainingRows, "training_active:true");
            AssertRow(trainingRows, "training_target:Eclaireuses");
        }

        [Test]
        public void LocalPreviewQueuesRestoreAcrossRestartAndCompleteOnce()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            var store = new MemoryQueueJournalStore();
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(store);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("wax_workshop");

            Assert.That(HiveViewProductUiPresenter.StartFocusedUpgradeForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.StartSoldierTrainingForProof(), Is.True);
            string[] committed = HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            string upgradeOperationId = ProofValue(committed, "upgrade_operation_id");
            string trainingOperationId = ProofValue(committed, "training_operation_id");
            Assert.That(upgradeOperationId, Is.Not.EqualTo("none"));
            Assert.That(trainingOperationId, Is.Not.EqualTo("none"));
            Assert.That(upgradeOperationId, Is.Not.EqualTo(trainingOperationId));
            AssertRow(committed, "authority:local_preview_non_official");
            Assert.That(ProofValue(committed, "upgrade_cost"), Does.Contain("miel").And.Contain("cire"));
            Assert.That(ProofValue(committed, "training_cost"), Does.Contain("miel").And.Contain("pollen"));

            HiveViewProductUiPresenter.SimulateLocalPreviewQueueRestartForProof();
            string[] restored = HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            AssertRow(restored, "restore_status:restored_local_preview");
            AssertRow(restored, "resume_notice_visible:true");
            AssertRow(restored, "resume_notice_localized:true");
            AssertRow(restored, "resume_notice_dismissible:true");
            Assert.That(ProofValue(restored, "resume_history"), Does.Contain("Amélioration reprise").And.Contain("Formation reprise"));
            Assert.That(ProofValue(restored, "upgrade_operation_id"), Is.EqualTo(upgradeOperationId));
            Assert.That(ProofValue(restored, "training_operation_id"), Is.EqualTo(trainingOperationId));
            AssertRow(HiveViewProductUiPresenter.QueueRailForProof(), "construction_active:true");
            AssertRow(HiveViewProductUiPresenter.QueueRailForProof(), "training_active:true");

            HiveViewProductUiPresenter.ExpireLocalPreviewQueuesForProof();
            HiveViewProductUiPresenter.CompleteExpiredLocalPreviewQueuesForProof();
            string[] completed = HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            AssertRow(completed, "upgrade_completion_claimed:true");
            AssertRow(completed, "training_completion_claimed:true");
            AssertRow(completed, "completion_idempotent:true");

            HiveViewProductUiPresenter.SimulateLocalPreviewQueueRestartForProof();
            HiveViewProductUiPresenter.CompleteExpiredLocalPreviewQueuesForProof();
            AssertRow(HiveViewProductUiPresenter.QueueRailForProof(), "construction_active:false");
            AssertRow(HiveViewProductUiPresenter.QueueRailForProof(), "training_active:false");
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
        }

        [Test]
        public void QueueReturnSummaryKeepsCompletedAwayVisibleAndRoutesWithoutCollection()
        {
            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            long now = DateTime.UtcNow.Ticks;
            var journal = new LocalPreviewQueueJournal
            {
                upgrade = new LocalPreviewQueueOperation
                {
                    operationId = "upgrade-away",
                    targetId = "wax_workshop",
                    startedUtcTicks = now - TimeSpan.FromSeconds(20d).Ticks,
                    endsUtcTicks = now - TimeSpan.FromSeconds(2d).Ticks,
                    honeyCost = 2484f,
                    waxCost = 902f,
                    resultValue = 23
                },
                research = new LocalPreviewQueueOperation
                {
                    operationId = "research-active",
                    targetId = LocalPreviewResearchCatalog.ForagingRoutesId,
                    startedUtcTicks = now - TimeSpan.FromSeconds(4d).Ticks,
                    endsUtcTicks = now + TimeSpan.FromSeconds(42d).Ticks,
                    honeyCost = 240f,
                    pollenCost = 90f,
                    resultValue = 1
                }
            };
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore(JsonUtility.ToJson(journal)));

            string[] restored = HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            AssertRow(restored, "return_item_count:2");
            AssertRow(restored, "return_completed_away_count:1");
            AssertRow(restored, "return_active_count:1");
            AssertRow(restored, "return_min_touch_size:44");
            AssertRow(restored, "return_auto_collection:false");
            AssertRow(restored, "return_official_source:server_utc_revisioned_operations");
            Assert.That(ProofValue(restored, "resume_history"), Does.Contain("Terminée pendant l’absence").And.Contain("Recherche reprise"));

            int collectionCount = ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count");
            HiveViewProductUiPresenter.CompleteExpiredLocalPreviewQueuesForProof();
            string[] afterCompletion = HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            AssertRow(afterCompletion, "upgrade_completion_claimed:true");
            AssertRow(afterCompletion, "resume_notice_visible:true");
            AssertRow(afterCompletion, "return_completed_away_count:1");

            HiveViewProductUiPresenter.NavigateToQueueReturnForProof();
            string[] navigated = HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            AssertRow(navigated, "return_last_route:wax_workshop");
            AssertRow(navigated, "resume_notice_visible:false");
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Atelier de cire"));
            Assert.That(ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count"), Is.EqualTo(collectionCount));

            Rect portrait = HiveViewProductUiPresenter.QueueReturnPanelRectForProof(true, 390f, 844f);
            Rect landscape = HiveViewProductUiPresenter.QueueReturnPanelRectForProof(false, 1600f, 900f);
            Assert.That(portrait.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(portrait.xMax, Is.LessThanOrEqualTo(390f));
            Assert.That(portrait.yMin, Is.GreaterThanOrEqualTo(126f));
            Assert.That(portrait.yMax, Is.LessThanOrEqualTo(766f));
            Assert.That(landscape.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(landscape.xMax, Is.LessThanOrEqualTo(1600f));
            Assert.That(landscape.yMax, Is.LessThanOrEqualTo(900f));

            foreach (string key in new[]
            {
                "ui.queue.return.title",
                "ui.queue.return.completed",
                "ui.queue.return.active",
                "ui.queue.return.disclosure",
                "ui.queue.return.view"
            })
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, "ClÃ© retour fr-CA absente: " + key);
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, "ClÃ© retour en-US absente: " + key);
            }
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
        }

        [Test]
        public void LocalPreviewResearchPersistsCostsAndCompletesExactlyOnce()
        {
            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            var store = new MemoryQueueJournalStore();
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(store);
            int honeyBefore = ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_honey");
            int pollenBefore = ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_pollen");

            Assert.That(HiveViewProductUiPresenter.StartLocalPreviewResearchForProof(LocalPreviewResearchCatalog.ForagingRoutesId), Is.True);
            Assert.That(HiveViewProductUiPresenter.StartLocalPreviewResearchForProof(LocalPreviewResearchCatalog.ForagingRoutesId), Is.False);
            Assert.That(HiveViewProductUiPresenter.StartLocalPreviewResearchForProof(LocalPreviewResearchCatalog.TemperedCombsId), Is.False);
            Assert.That(ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_honey"), Is.EqualTo(honeyBefore - 240));
            Assert.That(ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_pollen"), Is.EqualTo(pollenBefore - 90));
            AssertRow(HiveViewProductUiPresenter.QueueRailForProof(), "research_active:true");
            AssertRow(HiveViewProductUiPresenter.QueueRailForProof(), "research_target:foraging_routes_i");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewResearchForProof(), "research_commit_count:1");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewResearchForProof(), "research_repeated_blocked_count:2");

            HiveViewProductUiPresenter.SimulateLocalPreviewQueueRestartForProof();
            string[] restored = HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            Assert.That(ProofValue(restored, "resume_history"), Does.Contain("Recherche reprise").And.Contain("Danse des routes I"));
            AssertRow(HiveViewProductUiPresenter.QueueRailForProof(), "research_active:true");

            HiveViewProductUiPresenter.ExpireLocalPreviewQueuesForProof();
            HiveViewProductUiPresenter.CompleteExpiredLocalPreviewQueuesForProof();
            AssertRow(HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof(), "research_completion_claimed:true");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewResearchForProof(), "research_honey_bonus:0.02");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewResearchForProof(), "research_wax_capacity_bonus:0");
            Assert.That(ProofValue(HiveViewProductUiPresenter.LocalPreviewResearchForProof(), "research_completed_ids"), Is.EqualTo("foraging_routes_i"));
            HiveViewProductUiPresenter.CompleteExpiredLocalPreviewQueuesForProof();
            Assert.That(ProofValue(HiveViewProductUiPresenter.LocalPreviewResearchForProof(), "research_completed_ids"), Is.EqualTo("foraging_routes_i"));
            Assert.That(HiveViewProductUiPresenter.StartLocalPreviewResearchForProof(LocalPreviewResearchCatalog.ForagingRoutesId), Is.False);

            Assert.That(HiveViewProductUiPresenter.StartLocalPreviewResearchForProof(LocalPreviewResearchCatalog.TemperedCombsId), Is.True);
            HiveViewProductUiPresenter.ExpireLocalPreviewQueuesForProof();
            HiveViewProductUiPresenter.CompleteExpiredLocalPreviewQueuesForProof();
            string[] completed = HiveViewProductUiPresenter.LocalPreviewResearchForProof();
            AssertRow(completed, "research_honey_bonus:0.02");
            AssertRow(completed, "research_wax_capacity_bonus:0.05");
            Assert.That(ProofValue(completed, "research_completed_ids"), Is.EqualTo("foraging_routes_i,tempered_combs_i"));

            HiveViewProductUiPresenter.SimulateLocalPreviewQueueRestartForProof();
            string[] afterRestart = HiveViewProductUiPresenter.LocalPreviewResearchForProof();
            AssertRow(afterRestart, "research_active:false");
            AssertRow(afterRestart, "research_honey_bonus:0.02");
            AssertRow(afterRestart, "research_wax_capacity_bonus:0.05");
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
        }

        [Test]
        public void ResearchShortageShowsExactMissingAmountAndRoutesWithoutProgress()
        {
            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
            HiveViewProductUiPresenter.UseLocalPreviewDailyRoundStoreForProof(new MemoryDailyRoundStore());
            int collectionCount = ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count");

            HiveViewProductUiPresenter.SetLocalPreviewResearchResourcesForProof(100f, 1000f);
            string[] honey = HiveViewProductUiPresenter.LocalPreviewResearchResolutionForProof(LocalPreviewResearchCatalog.ForagingRoutesId);
            AssertRow(honey, "research_resolution_reason:Miel manquant : 140");
            AssertRow(honey, "research_resolution_resource:honey");
            AssertRow(honey, "research_resolution_hotspot:honey_storage");
            AssertRow(honey, "research_resolution_min_touch_size:44");
            AssertRow(honey, "research_resolution_starts_operation:false");
            AssertRow(honey, "research_resolution_collects_resource:false");
            AssertRow(honey, "research_resolution_completes_daily_task:false");
            HiveViewProductUiPresenter.NavigateToResearchResourceSourceForProof(LocalPreviewResearchCatalog.ForagingRoutesId);
            AssertRow(HiveViewProductUiPresenter.LocalPreviewResearchResolutionForProof(LocalPreviewResearchCatalog.ForagingRoutesId), "research_resolution_last_route:honey_storage");
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Réserve de miel"));
            AssertRow(HiveViewProductUiPresenter.LocalPreviewResearchForProof(), "research_commit_count:0");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_tasks_mask:0");

            HiveViewProductUiPresenter.SetLocalPreviewResearchResourcesForProof(1000f, 20f);
            string[] pollen = HiveViewProductUiPresenter.LocalPreviewResearchResolutionForProof(LocalPreviewResearchCatalog.ForagingRoutesId);
            AssertRow(pollen, "research_resolution_reason:Pollen manquant : 70");
            AssertRow(pollen, "research_resolution_resource:pollen");
            AssertRow(pollen, "research_resolution_hotspot:warehouse_cells");
            HiveViewProductUiPresenter.NavigateToResearchResourceSourceForProof(LocalPreviewResearchCatalog.ForagingRoutesId);
            AssertRow(HiveViewProductUiPresenter.LocalPreviewResearchResolutionForProof(LocalPreviewResearchCatalog.ForagingRoutesId), "research_resolution_last_route:warehouse_cells");

            Assert.That(ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count"), Is.EqualTo(collectionCount));
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_tasks_mask:0");
            HiveViewProductUiPresenter.SetLocalPreviewResearchResourcesForProof(1000f, 1000f);
            string[] sufficient = HiveViewProductUiPresenter.LocalPreviewResearchResolutionForProof(LocalPreviewResearchCatalog.ForagingRoutesId);
            AssertRow(sufficient, "research_resolution_reason:");
            AssertRow(sufficient, "research_resolution_resource:none");

            foreach (string key in new[]
            {
                "research.reason.honey_missing",
                "research.reason.pollen_missing",
                "research.source.button",
                "research.source.feedback"
            })
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, "ClÃ© source fr-CA absente: " + key);
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, "ClÃ© source en-US absente: " + key);
            }
            HiveViewProductUiPresenter.UseLocalPreviewDailyRoundStoreForProof(new MemoryDailyRoundStore());
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
        }

        [Test]
        public void LocalPreviewQueueJournalMigratesV1ToResearchAwareV2()
        {
            const string v1 = "{\"version\":1,\"upgrade\":{\"operationId\":\"legacy-upgrade\",\"targetId\":\"wax_workshop\",\"startedUtcTicks\":1,\"endsUtcTicks\":2,\"honeyCost\":10,\"waxCost\":5,\"pollenCost\":0,\"completionClaimed\":true,\"resultValue\":23},\"training\":{}}";
            var store = new MemoryQueueJournalStore(v1);
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(store);
            string[] migrated = HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            AssertRow(migrated, "journal_version:2");
            AssertRow(migrated, "upgrade_operation_id:legacy-upgrade");
            AssertRow(migrated, "research_operation_id:none");
            AssertRow(migrated, "research_completed_ids:none");
            Assert.That(store.Read(), Does.Contain("\"version\":2"));
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
        }

        [Test]
        public void HiveProgressMigratesV1WithoutInventingDoctrineCounts()
        {
            const string profileId = "profile-doctrine-migration";
            const string v1 = "{\"version\":1,\"profileId\":\"profile-doctrine-migration\",\"revision\":7,\"buildings\":[],\"workers\":36,\"soldiers\":34,\"guardians\":16,\"scouts\":17}";
            var store = new MemoryHiveProgressStore(v1);

            LocalPreviewHiveProgressReadResult migrated = LocalPreviewHiveProgressCodec.Read(store, profileId);

            Assert.That(migrated.Status, Is.EqualTo(LocalPreviewHiveProgressReadStatus.Sanitized));
            Assert.That(migrated.Progress.version, Is.EqualTo(LocalPreviewHiveProgressCodec.CurrentVersion));
            Assert.That(migrated.Progress.soldiers, Is.EqualTo(34));
            Assert.That(migrated.Progress.guardians, Is.EqualTo(16));
            Assert.That(migrated.Progress.scouts, Is.EqualTo(17));
            Assert.That(migrated.Progress.wingrunners, Is.Zero);
            Assert.That(migrated.Progress.darters, Is.Zero);
            Assert.That(store.Read(), Does.Contain("\"version\":" + LocalPreviewHiveProgressCodec.CurrentVersion).And.Contain("\"wingrunners\":0").And.Contain("\"darters\":0"));
        }

        [Test]
        public void HiveProgressPersistsMultipleBuildingsAndPopulationsAcrossRestarts()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            var store = new MemoryHiveProgressStore();
            HiveViewProductUiPresenter.UseLocalPreviewHiveProgressStoreForProof(store);

            HiveViewProductUiPresenter.PersistLocalPreviewBuildingLevelForProof("honey_storage", 28);
            HiveViewProductUiPresenter.PersistLocalPreviewBuildingLevelForProof("wax_workshop", 25);
            HiveViewProductUiPresenter.PersistLocalPreviewBuildingLevelForProof("guard_post", 27);
            HiveViewProductUiPresenter.PersistLocalPreviewPopulationForProof("workers", 36);
            HiveViewProductUiPresenter.PersistLocalPreviewPopulationForProof("soldiers", 34);
            HiveViewProductUiPresenter.PersistLocalPreviewPopulationForProof("guardians", 16);
            HiveViewProductUiPresenter.PersistLocalPreviewPopulationForProof("scouts", 17);
            HiveViewProductUiPresenter.PersistLocalPreviewPopulationForProof("wingrunners", 12);
            HiveViewProductUiPresenter.PersistLocalPreviewPopulationForProof("darters", 8);

            HiveViewProductUiPresenter.SimulateLocalPreviewHiveProgressRestartForProof();
            string[] restored = HiveViewProductUiPresenter.LocalPreviewHiveProgressForProof();
            AssertRow(restored, "progress_version:" + LocalPreviewHiveProgressCodec.CurrentVersion);
            AssertRow(restored, "progress_restore_status:restored");
            AssertRow(restored, "progress_building_count:3");
            Assert.That(ProofValue(restored, "progress_buildings"), Does.Contain("guard_post=27").And.Contain("honey_storage=28").And.Contain("wax_workshop=25"));
            AssertRow(restored, "progress_workers:36");
            AssertRow(restored, "progress_soldiers:34");
            AssertRow(restored, "progress_guardians:16");
            AssertRow(restored, "progress_scouts:17");
            AssertRow(restored, "progress_wingrunners:12");
            AssertRow(restored, "progress_darters:8");
            AssertRow(restored, "progress_legacy_counts_converted:false");
            AssertRow(restored, "progress_device_cache_persistent:true");
            AssertRow(restored, "progress_device_cache_protected:false");
            AssertRow(restored, "progress_min_touch_size:44");
            AssertRow(restored, "progress_official_authority:server_revisioned_snapshot");
            AssertRow(restored, "progress_world_map_modified:false");
            AssertRow(restored, "progress_hive_image_modified:false");

            HiveViewProductUiPresenter.PersistLocalPreviewBuildingLevelForProof("nursery_cluster", 26);
            HiveViewProductUiPresenter.PersistLocalPreviewPopulationForProof("soldiers", 42);
            HiveViewProductUiPresenter.SimulateLocalPreviewHiveProgressRestartForProof();
            string[] secondRestart = HiveViewProductUiPresenter.LocalPreviewHiveProgressForProof();
            AssertRow(secondRestart, "progress_building_count:4");
            Assert.That(ProofValue(secondRestart, "progress_buildings"), Does.Contain("wax_workshop=25").And.Contain("nursery_cluster=26"));
            AssertRow(secondRestart, "progress_soldiers:42");
            AssertRow(secondRestart, "progress_scouts:17");

            foreach (string key in new[]
            {
                "hive_progress.army.title",
                "hive_progress.army.subtitle",
                "hive_progress.army.summary",
                "hive_progress.building.disclosure",
                "hive_progress.building.saved",
                "hive_progress.local_preview.server"
            })
            {
                Assert.That(BeeLocalization.HasText("fr-CA", key), Is.True, "Clé progression fr-CA absente: " + key);
                Assert.That(BeeLocalization.HasText("en-US", key), Is.True, "Clé progression en-US absente: " + key);
            }

            HiveViewProductUiPresenter.UseLocalPreviewHiveProgressStoreForProof(new MemoryHiveProgressStore());
        }

        [Test]
        public void CompletedQueueOperationsMergeIntoHiveProgressWithoutRollback()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            var progressStore = new MemoryHiveProgressStore();
            HiveViewProductUiPresenter.UseLocalPreviewHiveProgressStoreForProof(progressStore);
            HiveViewProductUiPresenter.PersistLocalPreviewBuildingLevelForProof("honey_storage", 28);
            HiveViewProductUiPresenter.PersistLocalPreviewBuildingLevelForProof("wax_workshop", 25);
            HiveViewProductUiPresenter.PersistLocalPreviewPopulationForProof("soldiers", 34);
            HiveViewProductUiPresenter.PersistLocalPreviewPopulationForProof("scouts", 17);

            var laterQueue = new LocalPreviewQueueJournal
            {
                upgrade = new LocalPreviewQueueOperation
                {
                    operationId = "legacy-guard-upgrade",
                    targetId = "guard_post",
                    startedUtcTicks = 1,
                    endsUtcTicks = 2,
                    completionClaimed = true,
                    resultValue = 27
                },
                training = new LocalPreviewQueueOperation
                {
                    operationId = "legacy-guardian-training",
                    targetId = "Gardiennes",
                    startedUtcTicks = 1,
                    endsUtcTicks = 2,
                    completionClaimed = true,
                    resultValue = 16
                }
            };
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore(JsonUtility.ToJson(laterQueue)));
            HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            HiveViewProductUiPresenter.SimulateLocalPreviewHiveProgressRestartForProof();
            string[] merged = HiveViewProductUiPresenter.LocalPreviewHiveProgressForProof();
            Assert.That(ProofValue(merged, "progress_buildings"), Does.Contain("honey_storage=28").And.Contain("wax_workshop=25").And.Contain("guard_post=27"));
            AssertRow(merged, "progress_soldiers:34");
            AssertRow(merged, "progress_guardians:16");
            AssertRow(merged, "progress_scouts:17");

            var staleQueue = new LocalPreviewQueueJournal
            {
                upgrade = new LocalPreviewQueueOperation
                {
                    operationId = "stale-wax-upgrade",
                    targetId = "wax_workshop",
                    startedUtcTicks = 1,
                    endsUtcTicks = 2,
                    completionClaimed = true,
                    resultValue = 23
                },
                training = new LocalPreviewQueueOperation
                {
                    operationId = "stale-soldier-training",
                    targetId = "Soldats",
                    startedUtcTicks = 1,
                    endsUtcTicks = 2,
                    completionClaimed = true,
                    resultValue = 26
                }
            };
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore(JsonUtility.ToJson(staleQueue)));
            HiveViewProductUiPresenter.LocalPreviewQueuePersistenceForProof();
            HiveViewProductUiPresenter.SimulateLocalPreviewHiveProgressRestartForProof();
            string[] noRollback = HiveViewProductUiPresenter.LocalPreviewHiveProgressForProof();
            Assert.That(ProofValue(noRollback, "progress_buildings"), Does.Contain("wax_workshop=25"));
            AssertRow(noRollback, "progress_soldiers:34");
            AssertRow(noRollback, "progress_queue_merge:monotonic_idempotent");

            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
            HiveViewProductUiPresenter.UseLocalPreviewHiveProgressStoreForProof(new MemoryHiveProgressStore());
        }

        [Test]
        public void HiveProgressRejectsWrongProfilesAndSanitizesBoundedData()
        {
            const string expectedProfile = "profile-b";
            var otherProfile = LocalPreviewHiveProgressCodec.CreateDefault("profile-a");
            otherProfile.soldiers = 999;
            var mismatchStore = new MemoryHiveProgressStore(JsonUtility.ToJson(otherProfile));
            LocalPreviewHiveProgressReadResult mismatch = LocalPreviewHiveProgressCodec.Read(mismatchStore, expectedProfile);
            Assert.That(mismatch.Status, Is.EqualTo(LocalPreviewHiveProgressReadStatus.ProfileMismatch));
            Assert.That(mismatch.Progress.profileId, Is.EqualTo(expectedProfile));
            Assert.That(mismatch.Progress.soldiers, Is.EqualTo(LocalPreviewHiveProgressCodec.DefaultSoldiers));

            LocalPreviewHiveProgressReadResult corrupt = LocalPreviewHiveProgressCodec.Read(new MemoryHiveProgressStore("{not-json"), expectedProfile);
            Assert.That(corrupt.Status, Is.EqualTo(LocalPreviewHiveProgressReadStatus.Corrupt));
            Assert.That(corrupt.Progress.workers, Is.EqualTo(LocalPreviewHiveProgressCodec.DefaultWorkers));

            var unsupported = LocalPreviewHiveProgressCodec.CreateDefault(expectedProfile);
            unsupported.version = 99;
            LocalPreviewHiveProgressReadResult unsupportedResult = LocalPreviewHiveProgressCodec.Read(new MemoryHiveProgressStore(JsonUtility.ToJson(unsupported)), expectedProfile);
            Assert.That(unsupportedResult.Status, Is.EqualTo(LocalPreviewHiveProgressReadStatus.UnsupportedVersion));

            var unsafeProgress = LocalPreviewHiveProgressCodec.CreateDefault(expectedProfile);
            unsafeProgress.revision = -4;
            unsafeProgress.workers = -1;
            unsafeProgress.soldiers = LocalPreviewHiveProgressCodec.MaxPopulationCount + 10;
            unsafeProgress.buildings.Add(new LocalPreviewBuildingProgress { hotspotId = "wax_workshop", level = 23 });
            unsafeProgress.buildings.Add(new LocalPreviewBuildingProgress { hotspotId = "wax_workshop", level = 25 });
            unsafeProgress.buildings.Add(new LocalPreviewBuildingProgress { hotspotId = string.Empty, level = 8 });
            for (int index = 0; index < 40; index++)
                unsafeProgress.buildings.Add(new LocalPreviewBuildingProgress { hotspotId = "bounded_" + index.ToString(CultureInfo.InvariantCulture), level = index + 1 });

            var boundedStore = new MemoryHiveProgressStore(JsonUtility.ToJson(unsafeProgress));
            LocalPreviewHiveProgressReadResult bounded = LocalPreviewHiveProgressCodec.Read(boundedStore, expectedProfile);
            Assert.That(bounded.Status, Is.EqualTo(LocalPreviewHiveProgressReadStatus.Sanitized));
            Assert.That(bounded.Progress.revision, Is.EqualTo(0));
            Assert.That(bounded.Progress.workers, Is.EqualTo(0));
            Assert.That(bounded.Progress.soldiers, Is.EqualTo(LocalPreviewHiveProgressCodec.MaxPopulationCount));
            Assert.That(bounded.Progress.buildings.Count, Is.EqualTo(LocalPreviewHiveProgressCodec.MaxBuildingEntries));
            Assert.That(LocalPreviewHiveProgressCodec.TryGetBuildingLevel(bounded.Progress, "wax_workshop", out int waxLevel), Is.True);
            Assert.That(waxLevel, Is.EqualTo(25));
            Assert.That(boundedStore.Read(), Does.Contain("\"revision\":0"));
        }

        [Test]
        public void HiveLedgerExposesStocksCommitmentsAndManualNavigationOnly()
        {
            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
            HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 840f, 61650f);
            Assert.That(HiveViewProductUiPresenter.StartLocalPreviewResearchForProof(LocalPreviewResearchCatalog.ForagingRoutesId), Is.True);

            string[] ledger = HiveViewProductUiPresenter.HiveLedgerForProof();
            AssertRow(ledger, "ledger_authority:local_preview_non_official");
            AssertRow(ledger, "ledger_official_authority:server");
            AssertRow(ledger, "ledger_direct_collection:false");
            AssertRow(ledger, "ledger_min_touch_size:44");
            AssertRow(ledger, "ledger_honey_available:125560");
            AssertRow(ledger, "ledger_honey_pending:840");
            AssertRow(ledger, "ledger_honey_engaged:240");
            AssertRow(ledger, "ledger_pollen_available:98360");
            AssertRow(ledger, "ledger_pollen_engaged:90");
            AssertRow(ledger, "ledger_active_commitments:1");

            int collectionsBefore = ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count");
            HiveViewProductUiPresenter.OpenHiveLedgerResourceForProof("honey_storage");
            int collectionsAfter = ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count");
            Assert.That(collectionsAfter, Is.EqualTo(collectionsBefore));
            AssertRow(HiveViewProductUiPresenter.HiveLedgerForProof(), "ledger_last_navigation:honey_storage");
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Réserve de miel"));

            Rect portrait = HiveViewProductUiPresenter.HiveLedgerPanelRectForProof(true, 390f, 844f);
            Assert.That(portrait.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(portrait.xMax, Is.LessThanOrEqualTo(390f));
            Assert.That(portrait.yMin, Is.GreaterThanOrEqualTo(134f));
            Assert.That(portrait.yMax, Is.LessThanOrEqualTo(766f));
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
        }

        [Test]
        public void DailyHiveRoundRequiresThreeRealActionsAndClaimsExactlyOnce()
        {
            Assert.That(BeeLocalization.SetLocale("fr-CA"), Is.True);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            var dailyStore = new MemoryDailyRoundStore();
            HiveViewProductUiPresenter.UseLocalPreviewDailyRoundStoreForProof(dailyStore);
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_tasks_mask:0");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_min_touch_size:44");
            Assert.That(HiveViewProductUiPresenter.MenuBadgeTextForProof("Quests"), Is.Empty);
            Assert.That(HiveViewProductUiPresenter.MenuBadgeTextForProof("Mail"), Is.Empty);
            Assert.That(HiveViewProductUiPresenter.MenuBadgeTextForProof("Alliance"), Is.Empty);
            Assert.That(HiveViewProductUiPresenter.MenuBadgeTextForProof("More"), Is.Empty);

            HiveViewProductUiPresenter.NavigateToDailyRoundTaskForProof("collect");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_last_route:honey_storage");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_tasks_mask:0");
            HiveViewProductUiPresenter.NavigateToDailyRoundTaskForProof("operation");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_last_route:research");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_tasks_mask:0");
            HiveViewProductUiPresenter.NavigateToDailyRoundTaskForProof("ledger");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_last_route:ledger");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_tasks_mask:0");

            HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 840f, 61650f);
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.EqualTo(840f).Within(0.01f));
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_collect_complete:true");
            Assert.That(HiveViewProductUiPresenter.StartLocalPreviewResearchForProof(LocalPreviewResearchCatalog.ForagingRoutesId), Is.True);
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_operation_complete:true");
            HiveViewProductUiPresenter.OpenHiveLedgerResourceForProof("honey_storage");

            string[] ready = HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof();
            AssertRow(ready, "daily_round_tasks_mask:7");
            AssertRow(ready, "daily_round_completed_tasks:3");
            AssertRow(ready, "daily_round_ledger_complete:true");
            AssertRow(ready, "daily_round_reward_ready:true");
            AssertRow(ready, "daily_round_authority:local_preview_non_official");
            AssertRow(ready, "daily_round_official_authority:server_utc");
            Assert.That(HiveViewProductUiPresenter.MenuBadgeTextForProof("Quests"), Is.EqualTo("!"));
            HiveViewProductUiPresenter.RecordDailyRoundTaskForProof("collect");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_tasks_mask:7");

            int honeyBefore = ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_honey");
            int pollenBefore = ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_pollen");
            int capacityBefore = ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "capacity_used");
            Assert.That(HiveViewProductUiPresenter.ClaimLocalPreviewDailyRoundForProof(), Is.True);
            Assert.That(ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_honey"), Is.EqualTo(honeyBefore + 120));
            Assert.That(ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_pollen"), Is.EqualTo(pollenBefore + 60));
            Assert.That(ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "capacity_used"), Is.EqualTo(capacityBefore + 180));
            string[] claimed = HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof();
            AssertRow(claimed, "daily_round_reward_claimed:true");
            AssertRow(claimed, "daily_round_reward_ready:false");
            AssertRow(claimed, "daily_round_claim_commit_count:1");
            Assert.That(ProofValue(claimed, "daily_round_claim_operation_id"), Is.Not.EqualTo("none"));
            Assert.That(HiveViewProductUiPresenter.MenuBadgeTextForProof("Quests"), Is.Empty);

            Assert.That(HiveViewProductUiPresenter.ClaimLocalPreviewDailyRoundForProof(), Is.False);
            HiveViewProductUiPresenter.SimulateLocalPreviewDailyRoundRestartForProof();
            Assert.That(HiveViewProductUiPresenter.ClaimLocalPreviewDailyRoundForProof(), Is.False);
            Assert.That(ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_honey"), Is.EqualTo(honeyBefore + 120));
            Assert.That(ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_pollen"), Is.EqualTo(pollenBefore + 60));
            AssertRow(HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof(), "daily_round_claim_commit_count:1");

            var oldRound = new LocalPreviewDailyRound { utcDay = "2000-01-01", tasksMask = 7, rewardClaimed = true, claimOperationId = "old-day" };
            HiveViewProductUiPresenter.UseLocalPreviewDailyRoundStoreForProof(new MemoryDailyRoundStore(JsonUtility.ToJson(oldRound)));
            string[] rolled = HiveViewProductUiPresenter.LocalPreviewDailyRoundForProof();
            AssertRow(rolled, "daily_round_tasks_mask:0");
            AssertRow(rolled, "daily_round_reward_claimed:false");
            Assert.That(ProofValue(rolled, "daily_round_utc_day"), Is.Not.EqualTo("2000-01-01"));

            Rect portrait = HiveViewProductUiPresenter.GuidedQuestMenuPanelRectForProof(true, 390f, 844f);
            Assert.That(portrait.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(portrait.xMax, Is.LessThanOrEqualTo(390f));
            Assert.That(portrait.yMin, Is.GreaterThanOrEqualTo(134f));
            Assert.That(portrait.yMax, Is.LessThanOrEqualTo(766f));
            HiveViewProductUiPresenter.UseLocalPreviewDailyRoundStoreForProof(new MemoryDailyRoundStore());
            HiveViewProductUiPresenter.UseLocalPreviewQueueJournalStoreForProof(new MemoryQueueJournalStore());
        }

        [Test]
        public void GuidedTutorialRestartsAtSafeChapterBoundaryWithoutRewardReplay()
        {
            var store = new MemoryTutorialCheckpointStore();
            HiveViewProductUiPresenter.UseLocalPreviewTutorialCheckpointStoreForProof(store);
            HiveViewProductUiPresenter.BeginGuidedBroodTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] committed = HiveViewProductUiPresenter.LocalPreviewTutorialCheckpointForProof();
            AssertRow(committed, "checkpoint_exists:true");
            AssertRow(committed, "checkpoint_chapter:2");
            AssertRow(committed, "interrupted_objective:BroodHandoffReview");
            string checkpointId = ProofValue(committed, "checkpoint_id");

            HiveViewProductUiPresenter.SimulateLocalPreviewTutorialRestartForProof();
            string[] restored = HiveViewProductUiPresenter.LocalPreviewTutorialCheckpointForProof();
            Assert.That(ProofValue(restored, "checkpoint_id"), Is.EqualTo(checkpointId));
            AssertRow(restored, "restored_step:BroodWelcome");
            AssertRow(restored, "resume_notice_visible:true");
            AssertRow(restored, "resume_policy:chapter_start_transaction_boundary");
            AssertRow(restored, "cost_replayed:false");
            AssertRow(restored, "reward_granted_on_restore:false");
            AssertRow(restored, "official_server_progression:false");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_act_milestones:1");
            HiveViewProductUiPresenter.UseLocalPreviewTutorialCheckpointStoreForProof(new MemoryTutorialCheckpointStore());
        }

        [Test]
        public void StrategicProfileSeparatesStructuralAndOperationalEffects()
        {
            var profile = new LocalPreviewStrategicProfile
            {
                openingBroodSupply = "royal_jelly_cache", broodDevelopment = "growth", broodDoctrine = "resilience", broodWorkerHandoff = "reinforced_operculum", workerAssignment = "honey", workerWorkshopHandoff = "wax_convoy", workerWorkshopCommission = "application_toolkit",
                workshopSpecialization = "production", workshopDoctrine = "precision", workshopCertification = "thermal", defenseExpeditionMandate = "guardian_escort", defenseWorldBriefing = "guarded_return",
                operationalHoneyProductionBonus = 0.02f, operationalWaxProductionBonus = 0.03f,
                operationalBroodCareBonus = 2f
            };
            LocalPreviewStrategicEffects effects = LocalPreviewStrategicProfileRules.Derive(profile);
            Assert.That(effects.HoneyProductionBonus, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(effects.WaxProductionBonus, Is.EqualTo(0.11f).Within(0.0001f));
            Assert.That(effects.BroodCareBonus, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(effects.WorkerRationHoneyDiscount, Is.EqualTo(80f));
            Assert.That(effects.WorkshopWaxDiscount, Is.EqualTo(80f));
            Assert.That(effects.ForagingPollenBonus, Is.EqualTo(4f));
            Assert.That(effects.ExpeditionSecurityBonus, Is.EqualTo(2f));
            Assert.That(effects.WorkerEmergenceWaxDiscount, Is.EqualTo(40f));
            Assert.That(effects.WorkshopCalibrationWaxBonus, Is.EqualTo(0f));
            Assert.That(effects.WorkshopApplicationWaxDiscount, Is.EqualTo(40f));
            Assert.That(effects.WorldNavigationHintLevel, Is.EqualTo(0f));
            Assert.That(effects.WorldBriefingSecurityBonus, Is.EqualTo(3f));
        }

        [Test]
        public void StrategicProfileRestoresChoicesAndDerivedEffects()
        {
            var profile = new LocalPreviewStrategicProfile { profileId = "profile-proof", revision = 5, openingBroodSupply = "thermal_escort", openingReward = "mixed_foundation", broodDevelopment = "growth", broodDoctrine = "resilience", broodWorkerHandoff = "emergence_ration", workerAssignment = "honey", workerWorkshopHandoff = "honey_logistics", workerWorkshopCommission = "calibration_template", workshopSpecialization = "production", workshopDoctrine = "precision", workshopCertification = "thermal", defenseExpeditionMandate = "scout_corridor", defenseWorldBriefing = "sun_beacon", operationalHoneyProductionBonus = 0.02f };
            var store = new MemoryStrategicProfileStore { Json = JsonUtility.ToJson(profile) };
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(store);
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            string[] rows = HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof();
            AssertRow(rows, "profile_id:profile-proof"); AssertRow(rows, "profile_revision:5");
            AssertRow(rows, "brood_development:growth"); AssertRow(rows, "worker_assignment:honey");
            AssertRow(rows, "workshop_doctrine:precision"); AssertRow(rows, "workshop_certification:thermal"); AssertRow(rows, "honey_bonus:0.05");
            AssertRow(rows, "wax_production_bonus:0.08"); AssertRow(rows, "local_non_official:true");
            AssertRow(rows, "worker_workshop_handoff:honey_logistics"); AssertRow(rows, "workshop_honey_discount:120");
            AssertRow(rows, "worker_workshop_commission:calibration_template"); AssertRow(rows, "workshop_calibration_wax_bonus:40");
            AssertRow(rows, "defense_expedition_mandate:scout_corridor"); AssertRow(rows, "foraging_pollen_bonus:6");
            AssertRow(rows, "defense_world_briefing:sun_beacon"); AssertRow(rows, "world_navigation_hint_level:1"); AssertRow(rows, "world_briefing_security_bonus:0");
            AssertRow(rows, "opening_brood_supply:thermal_escort");
            AssertRow(rows, "opening_reward:mixed_foundation");
            AssertRow(rows, "brood_worker_handoff:emergence_ration");
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new MemoryStrategicProfileStore());
        }

        [Test]
        public void StrategicProfileMigratesV1AndAppliesOpeningCharterOnce()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            const string legacyJson = "{\"version\":1,\"profileId\":\"legacy-proof\",\"revision\":3,\"openingCharter\":\"secure_reserve\"}";
            var store = new MemoryStrategicProfileStore { Json = legacyJson };
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(store);
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "profile_version:12");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "profile_revision:4");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "opening_charter:secure_reserve");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "opening_reward:none");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicRuntimeEffectsForProof(), "runtime_capacity_max:85000");

            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicRuntimeEffectsForProof(), "runtime_capacity_max:85000");
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new MemoryStrategicProfileStore());
        }

        [Test]
        public void StrategicProfileMigratesV9WithoutReplayingWorkshopCommission()
        {
            const string legacyJson = "{\"version\":9,\"profileId\":\"legacy-v9\",\"revision\":7,\"workerWorkshopHandoff\":\"wax_convoy\"}";
            var store = new MemoryStrategicProfileStore { Json = legacyJson };
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(store);
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            string[] rows = HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof();
            AssertRow(rows, "profile_version:12");
            AssertRow(rows, "profile_revision:8");
            AssertRow(rows, "worker_workshop_handoff:wax_convoy");
            AssertRow(rows, "worker_workshop_commission:none");
            AssertRow(rows, "opening_reward:none");
            AssertRow(rows, "workshop_calibration_wax_bonus:0");
            AssertRow(rows, "workshop_application_wax_discount:0");
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new MemoryStrategicProfileStore());
        }

        [Test]
        public void StrategicProfileMigratesV10WithoutReplayingWorldBriefing()
        {
            const string legacyJson = "{\"version\":10,\"profileId\":\"legacy-v10\",\"revision\":9,\"defenseExpeditionMandate\":\"guardian_escort\"}";
            var store = new MemoryStrategicProfileStore { Json = legacyJson };
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(store);
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            string[] rows = HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof();
            AssertRow(rows, "profile_version:12");
            AssertRow(rows, "profile_revision:10");
            AssertRow(rows, "defense_expedition_mandate:guardian_escort");
            AssertRow(rows, "defense_world_briefing:none");
            AssertRow(rows, "opening_reward:none");
            AssertRow(rows, "world_navigation_hint_level:0");
            AssertRow(rows, "world_briefing_security_bonus:0");
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "profile_revision:10");
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new MemoryStrategicProfileStore());
        }

        [Test]
        public void StrategicProfileMigratesV11WithNoImplicitOpeningReward()
        {
            const string legacyJson = "{\"version\":11,\"profileId\":\"legacy-v11\",\"revision\":11,\"defenseWorldBriefing\":\"sun_beacon\"}";
            var store = new MemoryStrategicProfileStore { Json = legacyJson };
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(store);
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            string[] rows = HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof();
            AssertRow(rows, "profile_version:12");
            AssertRow(rows, "profile_revision:12");
            AssertRow(rows, "defense_world_briefing:sun_beacon");
            AssertRow(rows, "opening_reward:none");
            AssertRow(rows, "world_navigation_hint_level:1");
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "profile_revision:12");
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new MemoryStrategicProfileStore());
        }

        private sealed class MemoryQueueJournalStore : ILocalPreviewQueueJournalStore
        {
            private string json;
            public MemoryQueueJournalStore(string initialJson = "") => json = initialJson ?? string.Empty;
            public string Read() => json;
            public void Write(string value) => json = value ?? string.Empty;
            public void Delete() => json = string.Empty;
        }

        private sealed class MemoryHiveProgressStore : ILocalPreviewHiveProgressStore
        {
            private string json;
            public MemoryHiveProgressStore(string initialJson = "") => json = initialJson ?? string.Empty;
            public string Read() => json;
            public void Write(string value) => json = value ?? string.Empty;
            public void Delete() => json = string.Empty;
        }

        private sealed class MemoryDailyRoundStore : ILocalPreviewDailyRoundStore
        {
            private string json;
            public MemoryDailyRoundStore(string initialJson = "") => json = initialJson ?? string.Empty;
            public string Read() => json;
            public void Write(string value) => json = value ?? string.Empty;
            public void Delete() => json = string.Empty;
        }

        private sealed class MemoryTutorialCheckpointStore : ILocalPreviewTutorialCheckpointStore
        {
            private string json = string.Empty;
            public string Read() => json;
            public void Write(string value) => json = value ?? string.Empty;
            public void Delete() => json = string.Empty;
        }

        private sealed class MemoryStrategicProfileStore : ILocalPreviewStrategicProfileStore
        {
            public string Json = string.Empty;
            public string Read() => Json;
            public void Write(string value) => Json = value ?? string.Empty;
            public void Delete() => Json = string.Empty;
        }

        [Test]
        public void ProductionAccumulatesWithoutCreditingTheHud()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 0f, 61650f);
            int honeyBefore = ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_honey");

            HiveViewProductUiPresenter.AdvanceManualProductionForProof(3600f);

            int honeyAfter = ProofInt(HiveViewProductUiPresenter.PlayableHiveDailyLoopForProof(), "resource_honey");
            string[] collectionRows = HiveViewProductUiPresenter.ManualProductionCollectionForProof();
            Assert.That(honeyAfter, Is.EqualTo(honeyBefore));
            AssertRow(collectionRows, "honey_pending:2540");
        }

        [Test]
        public void CollectingHoneyCreditsCapacityAndEmptiesTheBuilding()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 840f, 61650f);

            float collected = HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
            string[] rows = HiveViewProductUiPresenter.ManualProductionCollectionForProof();

            Assert.That(collected, Is.EqualTo(840f).Within(0.01f));
            AssertRow(rows, "honey_pending:0");
            AssertRow(rows, "last_collection_hotspot:honey_storage");
            AssertRow(rows, "last_collection_icon:honey");
            AssertRow(rows, "manual_collection_count:1");
            AssertRow(rows, "capacity_used:62490");
        }

        [Test]
        public void FullCapacityBlocksCollectionWithoutLosingProduction()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.SetManualProductionForProof("wax_workshop", 260f, 80000f);

            float collected = HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop");
            string[] rows = HiveViewProductUiPresenter.ManualProductionCollectionForProof();

            Assert.That(collected, Is.Zero);
            AssertRow(rows, "wax_pending:260");
            AssertRow(rows, "manual_collection_count:0");
            AssertRow(rows, "capacity_used:80000");
        }

        [Test]
        public void NonProductionBuildingsCannotBeCollected()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");

            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("guard_post"), Is.Zero);
            AssertRow(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "manual_collection_count:0");
        }

        [Test]
        public void GuidedFirstChapterTargetsHoneyAndOpensBroodChapter()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.BeginGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:welcome");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:collect_honey");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_hotspot:honey_storage");

            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.Zero);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "blocked_clicks:1");

            float collected = HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
            Assert.That(collected, Is.EqualTo(840f).Within(0.01f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:collected");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:honey_workers_ready");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "reward_claimed:false");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_objective_index:2");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_objective_target:14");

            HiveViewProductUiPresenter.ChooseGuidedOpeningProductionForProof(true);
            string[] productionRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(productionRows, "tutorial_step:honey_production");
            AssertRow(productionRows, "opening_production_plan:steady");
            AssertRow(productionRows, "opening_production_plan_commit_count:1");
            AssertRow(productionRows, "opening_assigned_workers:3");
            AssertRow(productionRows, "opening_production_commit_count:1");
            AssertRow(productionRows, "opening_production_pending:true");
            AssertRow(productionRows, "opening_production_duration_seconds:22");
            AssertRow(productionRows, "opening_production_yield:360");
            AssertRow(productionRows, "opening_production_brood_stability_gain:4");
            AssertRow(productionRows, "opening_requires_second_manual_collection:true");

            HiveViewProductUiPresenter.ChooseGuidedOpeningProductionForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_production_plan_commit_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_production_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedOpeningProductionForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:honey_second_collect");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_hotspot:honey_storage");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "reward_claimed:false");

            float secondCollection = HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
            Assert.That(secondCollection, Is.EqualTo(360f).Within(0.01f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:honey_second_collected");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_second_collection:360");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "reward_claimed:false");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_objective_index:5");

            HiveViewProductUiPresenter.ChooseGuidedOpeningAllocationForProof(true);
            string[] allocationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(allocationRows, "tutorial_step:honey_allocation_applied");
            AssertRow(allocationRows, "opening_allocation:brood");
            AssertRow(allocationRows, "opening_allocation_commit_count:1");
            AssertRow(allocationRows, "opening_allocation_honey_cost:120");
            AssertRow(allocationRows, "opening_allocation_brood_stability_gain:8");
            AssertRow(allocationRows, "opening_objective_index:6");
            AssertRow(allocationRows, "reward_claimed:false");

            HiveViewProductUiPresenter.ChooseGuidedOpeningAllocationForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_allocation_commit_count:1");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_installation_welcome");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_circuit_route_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_circuit_round:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_objective_index:6");

            HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitRouteForProof(true);
            string[] firstRouteRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstRouteRows, "opening_circuit_route_plan:nursery_relay");
            AssertRow(firstRouteRows, "opening_circuit_route_duration_seconds:22");
            AssertRow(firstRouteRows, "opening_circuit_route_assigned_bees:3");
            AssertRow(firstRouteRows, "opening_circuit_route_commit_count:1");
            HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitRouteForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_circuit_route_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitRouteForProof();
            string[] firstRouteFeedbackRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstRouteFeedbackRows, "tutorial_step:opening_circuit_collect");
            AssertRow(firstRouteFeedbackRows, "opening_route_feedback_active:true");
            AssertRow(firstRouteFeedbackRows, "opening_route_feedback_plan:nursery_relay");
            AssertRow(firstRouteFeedbackRows, "opening_route_feedback_honey:140");
            AssertRow(firstRouteFeedbackRows, "opening_route_feedback_stability:3");
            AssertRow(firstRouteFeedbackRows, "opening_route_feedback_sequence:1");
            AssertRow(firstRouteFeedbackRows, "opening_route_feedback_destination:nursery_cluster");
            AssertRow(firstRouteFeedbackRows, "opening_route_feedback_bees:3");
            AssertRow(firstRouteFeedbackRows, "opening_route_feedback_permanent_icon:false");
            AssertRow(firstRouteFeedbackRows, "opening_route_feedback_hive_background_modified:false");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.EqualTo(140f).Within(0.01f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_circuit_maintenance_choice");

            HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitMaintenanceForProof(true);
            string[] firstMaintenanceRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstMaintenanceRows, "opening_circuit_maintenance_plan:wax_seal");
            AssertRow(firstMaintenanceRows, "opening_circuit_maintenance_duration_seconds:20");
            AssertRow(firstMaintenanceRows, "opening_circuit_maintenance_wax_cost:45");
            AssertRow(firstMaintenanceRows, "opening_circuit_maintenance_commit_count:1");
            HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitMaintenanceForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_circuit_maintenance_commit_count:1");
            HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitMaintenanceForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_circuit_round_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_circuit_completed_rounds:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_circuit_round:2");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_objective_index:7");
            HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitRouteForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitRouteForProof();
            string[] secondRouteFeedbackRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(secondRouteFeedbackRows, "opening_route_feedback_active:true");
            AssertRow(secondRouteFeedbackRows, "opening_route_feedback_plan:reserve_route");
            AssertRow(secondRouteFeedbackRows, "opening_route_feedback_honey:180");
            AssertRow(secondRouteFeedbackRows, "opening_route_feedback_stability:0");
            AssertRow(secondRouteFeedbackRows, "opening_route_feedback_sequence:2");
            AssertRow(secondRouteFeedbackRows, "opening_route_feedback_destination:honey_storage");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.EqualTo(180f).Within(0.01f));
            HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitMaintenanceForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitMaintenanceForProof();
            string[] twoRoundRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(twoRoundRows, "opening_circuit_completed_rounds:2");
            AssertRow(twoRoundRows, "opening_circuit_collected_honey:320");
            AssertRow(twoRoundRows, "opening_circuit_stability_gain:6");
            AssertRow(twoRoundRows, "opening_circuit_honey_production_gain_percent:1");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_commissioning_check");
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(2);
            string[] checkRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(checkRows, "tutorial_step:opening_charter_choice");
            AssertRow(checkRows, "opening_commissioning_check_count:3");
            AssertRow(checkRows, "opening_commissioning_checks_mask:7");

            HiveViewProductUiPresenter.ChooseGuidedOpeningCharterForProof(true);
            string[] charterRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(charterRows, "tutorial_step:opening_charter_running");
            AssertRow(charterRows, "opening_charter_plan:brood_bridge");
            AssertRow(charterRows, "opening_charter_duration_seconds:28");
            AssertRow(charterRows, "opening_charter_assigned_bees:2");
            AssertRow(charterRows, "opening_charter_honey_cost:60");
            AssertRow(charterRows, "opening_charter_commit_count:1");
            HiveViewProductUiPresenter.ChooseGuidedOpeningCharterForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_charter_commit_count:1");
            HiveViewProductUiPresenter.CompleteGuidedOpeningCharterForProof();
            string[] charterCheckRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(charterCheckRows, "tutorial_step:opening_charter_check");
            AssertRow(charterCheckRows, "opening_charter_check_count:0");
            AssertRow(charterCheckRows, "opening_charter_stability_gain:0");
            AssertRow(charterCheckRows, "opening_charter_care_gain:0");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "opening_charter:none");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(true), Is.True);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(0);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_charter_check_count:1");
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(1);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_charter_check");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "opening_charter:none");
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(2);
            string[] ratifiedCharterRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(ratifiedCharterRows, "tutorial_step:opening_commissioning_load_choice");
            AssertRow(ratifiedCharterRows, "opening_charter_check_count:3");
            AssertRow(ratifiedCharterRows, "opening_charter_checks_mask:7");
            AssertRow(ratifiedCharterRows, "opening_charter_last_check:alert_signal");
            AssertRow(ratifiedCharterRows, "opening_charter_stability_gain:4");
            AssertRow(ratifiedCharterRows, "opening_charter_care_gain:1");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "opening_charter:brood_bridge");
            HiveViewProductUiPresenter.ChooseGuidedOpeningCommissioningLoadForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCommissioningLoadForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThanOrEqualTo(210f));
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCommissioningSealForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCommissioningSealForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_brood_supply_choice");
            HiveViewProductUiPresenter.ChooseGuidedOpeningBroodSupplyForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_brood_supply_plan:royal_jelly_cache");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_brood_supply_duration_seconds:14");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_brood_supply_assigned_bees:2");
            HiveViewProductUiPresenter.CompleteGuidedOpeningBroodSupplyForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells"), Is.GreaterThanOrEqualTo(60f));
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_brood_supply_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_brood_supply_check_count:3");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_hygiene_purge_choice");
            HiveViewProductUiPresenter.ChooseGuidedOpeningHygienePurgeForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_hygiene_purge_plan:enzymatic");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_hygiene_purge_duration_seconds:12");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_hygiene_purge_assigned_bees:2");
            HiveViewProductUiPresenter.CompleteGuidedOpeningHygienePurgeForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(20f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_hygiene_purge_check");
            HiveViewProductUiPresenter.RegisterGuidedOpeningHygienePurgeCheckForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_hygiene_purge_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_hygiene_purge_check_done:true");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] rewardChoiceRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(rewardChoiceRows, "tutorial_step:opening_reward_choice");
            AssertRow(rewardChoiceRows, "opening_reward_plan:none");
            AssertRow(rewardChoiceRows, "opening_reward_commit_count:0");
            AssertRow(rewardChoiceRows, "opening_reward_honey:0");
            AssertRow(rewardChoiceRows, "opening_reward_pollen:0");
            AssertRow(rewardChoiceRows, "opening_reward_total:250");
            AssertRow(rewardChoiceRows, "opening_reward_paid_advantage:false");
            AssertRow(rewardChoiceRows, "reward_claimed:false");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);

            HiveViewProductUiPresenter.ChooseGuidedOpeningRewardForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedOpeningRewardForProof(false);
            string[] installedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(installedRows, "tutorial_step:opening_installation_completed");
            AssertRow(installedRows, "opening_charter_stability_gain:4");
            AssertRow(installedRows, "opening_charter_care_gain:1");
            AssertRow(installedRows, "opening_objective_index:15");
            AssertRow(installedRows, "opening_commissioning_validation_count:3");
            AssertRow(installedRows, "opening_commissioning_seal_plan:brood_seal");
            AssertRow(installedRows, "opening_reward_plan:mixed_foundation");
            AssertRow(installedRows, "opening_reward_commit_count:1");
            AssertRow(installedRows, "opening_reward_honey:170");
            AssertRow(installedRows, "opening_reward_pollen:80");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "opening_reward:mixed_foundation");
            AssertRow(installedRows, "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "reward_claimed:true");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "reward_honey:170");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_act_milestones:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_reward_claim_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_reward_last_id:chapter_1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_reward_last_honey:170");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_reward_last_pollen:80");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_welcome");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_id:chapter_2_brood_care");
        }

        [Test]
        public void GuidedFirstChapterCanPrioritizeReserveYieldInstead()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.BeginGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.EqualTo(840f).Within(0.01f));
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();

            HiveViewProductUiPresenter.ChooseGuidedOpeningProductionForProof(false);
            string[] productionRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(productionRows, "tutorial_step:honey_production");
            AssertRow(productionRows, "opening_production_plan:surge");
            AssertRow(productionRows, "opening_assigned_workers:5");
            AssertRow(productionRows, "opening_production_duration_seconds:14");
            AssertRow(productionRows, "opening_production_yield:480");
            AssertRow(productionRows, "opening_production_brood_stability_gain:0");

            HiveViewProductUiPresenter.CompleteGuidedOpeningProductionForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.EqualTo(480f).Within(0.01f));
            HiveViewProductUiPresenter.ChooseGuidedOpeningAllocationForProof(false);

            string[] allocationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(allocationRows, "tutorial_step:honey_allocation_applied");
            AssertRow(allocationRows, "opening_allocation:reserve");
            AssertRow(allocationRows, "opening_allocation_commit_count:1");
            AssertRow(allocationRows, "opening_allocation_honey_cost:0");
            AssertRow(allocationRows, "opening_allocation_brood_stability_gain:0");
            AssertRow(allocationRows, "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitRouteForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitRouteForProof();
                Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.EqualTo(180f).Within(0.01f));
                HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitMaintenanceForProof(true);
                HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitMaintenanceForProof();
                if (round == 0) HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            }

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCharterForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCharterForProof();
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCommissioningLoadForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCommissioningLoadForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThanOrEqualTo(170f));
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCommissioningSealForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCommissioningSealForProof();
            HiveViewProductUiPresenter.ChooseGuidedOpeningBroodSupplyForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningBroodSupplyForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThanOrEqualTo(120f));
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(2);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_hygiene_purge_choice");
            HiveViewProductUiPresenter.ChooseGuidedOpeningHygienePurgeForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningHygienePurgeForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(20f));
            HiveViewProductUiPresenter.RegisterGuidedOpeningHygienePurgeCheckForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:opening_reward_choice");
            HiveViewProductUiPresenter.ChooseGuidedOpeningRewardForProof(false);

            string[] reserveCharterRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(reserveCharterRows, "tutorial_step:opening_installation_completed");
            AssertRow(reserveCharterRows, "opening_circuit_collected_honey:360");
            AssertRow(reserveCharterRows, "opening_circuit_honey_production_gain_percent:2");
            AssertRow(reserveCharterRows, "opening_charter_plan:secure_reserve");
            AssertRow(reserveCharterRows, "opening_charter_duration_seconds:24");
            AssertRow(reserveCharterRows, "opening_charter_honey_cost:80");
            AssertRow(reserveCharterRows, "opening_charter_capacity_gain:5000");
            AssertRow(reserveCharterRows, "opening_charter_stability_gain:0");
            AssertRow(reserveCharterRows, "opening_charter_care_gain:0");
            AssertRow(reserveCharterRows, "opening_reward_plan:honey_reserve");
            AssertRow(reserveCharterRows, "opening_reward_commit_count:1");
            AssertRow(reserveCharterRows, "opening_reward_honey:250");
            AssertRow(reserveCharterRows, "opening_reward_pollen:0");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "opening_reward:honey_reserve");
            AssertRow(reserveCharterRows, "opening_paid_advantage:false");
        }

        [Test]
        public void GuidedOpeningActPacingProfileExposesTheCurrentGap()
        {
            string[] rows = HiveViewProductUiPresenter.GuidedOpeningActPacingForProof();

            AssertRow(rows, "pacing_profile:opening_act_1");
            AssertRow(rows, "pacing_measure:mandatory_timed_tasks_only");
            AssertRow(rows, "chapter_1_objectives:15");
            AssertRow(rows, "chapter_1_decisions:12");
            AssertRow(rows, "chapter_1_active_commissioning_checks:13");
            AssertRow(rows, "chapter_1_manual_collections:7");
            AssertRow(rows, "chapter_1_timed_seconds_fast:162");
            AssertRow(rows, "chapter_1_timed_seconds_slow:200");
            AssertRow(rows, "chapter_2_timed_seconds_fast:194");
            AssertRow(rows, "chapter_2_timed_seconds_slow:242");
            AssertRow(rows, "chapter_2_objectives:16");
            AssertRow(rows, "chapter_2_decisions:14");
            AssertRow(rows, "chapter_2_active_vital_checks:3");
            AssertRow(rows, "chapter_2_active_incubation_checks:6");
            AssertRow(rows, "chapter_2_active_checks_total:13");
            AssertRow(rows, "chapter_2_manual_collections:4");
            AssertRow(rows, "chapter_3_objectives:13");
            AssertRow(rows, "chapter_3_decisions:10");
            AssertRow(rows, "chapter_3_active_quality_checks:15");
            AssertRow(rows, "chapter_3_manual_collections_max:5");
            AssertRow(rows, "chapter_4_objectives:14");
            AssertRow(rows, "chapter_4_decisions:11");
            AssertRow(rows, "chapter_4_active_structural_checks:16");
            AssertRow(rows, "chapter_4_manual_collections:5");
            AssertRow(rows, "chapter_4_timed_seconds_fast:155");
            AssertRow(rows, "chapter_4_timed_seconds_slow:179");
            AssertRow(rows, "chapter_5_objectives:14");
            AssertRow(rows, "chapter_5_decisions:14");
            AssertRow(rows, "chapter_5_active_checks:13");
            AssertRow(rows, "chapter_5_manual_collections:5");
            AssertRow(rows, "chapter_5_timed_seconds_fast:176");
            AssertRow(rows, "chapter_5_timed_seconds_slow:222");
            AssertRow(rows, "chapter_3_timed_seconds_fast:145");
            AssertRow(rows, "chapter_3_timed_seconds_slow:170");
            AssertRow(rows, "act_1_timed_seconds_fast:832");
            AssertRow(rows, "act_1_timed_seconds_slow:1013");
            AssertRow(rows, "act_1_target_seconds_min:1860");
            AssertRow(rows, "pacing_target_status:requires_more_active_objectives");
            AssertRow(rows, "waiting_alone_counts_as_content:false");
        }

        [Test]
        public void GuidedOpeningActObjectivePacingIdentifiesTheWeakestInteractionChapter()
        {
            string[] rows = HiveViewProductUiPresenter.GuidedOpeningActObjectivePacingForProof();
            int[] objectives = new int[5];
            int[] fastSeconds = new int[5];
            int[] slowSeconds = new int[5];
            int[] activeInteractions = new int[5];

            foreach (string row in rows.Where(value => value.StartsWith("objective:", StringComparison.Ordinal)))
            {
                string[] fields = row.Split('|');
                int chapter = int.Parse(fields[0].Substring("objective:".Length).Split('.')[0], CultureInfo.InvariantCulture) - 1;
                objectives[chapter]++;
                fastSeconds[chapter] += int.Parse(fields[2].Substring("timed_fast:".Length), CultureInfo.InvariantCulture);
                slowSeconds[chapter] += int.Parse(fields[3].Substring("timed_slow:".Length), CultureInfo.InvariantCulture);
                activeInteractions[chapter] += int.Parse(fields[4].Substring("decisions:".Length), CultureInfo.InvariantCulture);
                activeInteractions[chapter] += int.Parse(fields[5].Substring("checks:".Length), CultureInfo.InvariantCulture);
                activeInteractions[chapter] += int.Parse(fields[6].Substring("collections:".Length), CultureInfo.InvariantCulture);
            }

            Assert.That(objectives, Is.EqualTo(new[] { 15, 16, 13, 14, 14 }));
            Assert.That(fastSeconds, Is.EqualTo(new[] { 162, 194, 145, 155, 176 }));
            Assert.That(slowSeconds, Is.EqualTo(new[] { 200, 242, 170, 179, 222 }));
            Assert.That(activeInteractions, Is.EqualTo(new[] { 32, 33, 30, 32, 32 }));
            AssertRow(rows, "chapter_5_active_checks:13");
            AssertRow(rows, "chapter_3_active_checks:15");
            AssertRow(rows, "chapter_2_active_interactions:33");
            AssertRow(rows, "lowest_active_interaction_chapter:3");
            AssertRow(rows, "lowest_active_interaction_tie:3");
            AssertRow(rows, "completed_pacing_slice:chapter_2_wax_consolidation_extension");
            AssertRow(rows, "recommended_next_pacing_slice:chapter_3_active_extension");
        }

        [Test]
        public void GuidedBroodChapterTargetsNurseryAndFeedsOnce()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            CompleteGuidedFirstChapter();
            HiveViewProductUiPresenter.SetBroodCareForProof(35f, 1000f);

            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_welcome");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] handoffRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(handoffRows, "tutorial_step:brood_handoff_review");
            AssertRow(handoffRows, "brood_handoff_visible:true");
            AssertRow(handoffRows, "brood_handoff_acknowledged:false");
            AssertRow(handoffRows, "brood_handoff_checks_mask:7");
            AssertRow(handoffRows, "brood_handoff_stability_gain:11");
            AssertRow(handoffRows, "brood_handoff_starting_stability:57");
            AssertRow(handoffRows, "brood_handoff_capacity_gain:5000");
            AssertRow(handoffRows, "brood_handoff_paid_advantage:false");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:inspect_nursery");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_handoff_acknowledged:true");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_hotspot:nursery_cluster");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "camera_controlled:true");

            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("guard_post"), Is.False);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "blocked_clicks:1");
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("nursery_cluster"), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_diagnosis_ready");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:2");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] diagnosisRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(diagnosisRows, "tutorial_step:brood_diagnosis_running");
            AssertRow(diagnosisRows, "brood_diagnosis_pending:true");
            AssertRow(diagnosisRows, "brood_diagnosis_duration_seconds:6");
            AssertRow(diagnosisRows, "brood_diagnosis_commit_count:1");
            AssertRow(diagnosisRows, "brood_stability:57");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_diagnosis_commit_count:1");
            HiveViewProductUiPresenter.CompleteGuidedBroodDiagnosisForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_preparation_ready");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:3");

            HiveViewProductUiPresenter.ChooseGuidedBroodPreparationForProof(false);
            string[] preparationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(preparationRows, "tutorial_step:brood_preparation_running");
            AssertRow(preparationRows, "brood_preparation_mode:ventilation");
            AssertRow(preparationRows, "brood_preparation_assigned_bees:3");
            AssertRow(preparationRows, "brood_preparation_duration_seconds:9");
            AssertRow(preparationRows, "brood_preparation_wax_cost:0");
            AssertRow(preparationRows, "brood_preparation_commit_count:1");
            AssertRow(preparationRows, "brood_preparation_pending:true");
            AssertRow(preparationRows, "brood_stability:57");

            HiveViewProductUiPresenter.ChooseGuidedBroodPreparationForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_preparation_commit_count:1");
            HiveViewProductUiPresenter.CompleteGuidedBroodPreparationForProof();
            string[] preparedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(preparedRows, "tutorial_step:brood_preparation_result");
            AssertRow(preparedRows, "brood_stability:65");
            AssertRow(preparedRows, "brood_preparation_care_bonus:1");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_ready");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:4");

            HiveViewProductUiPresenter.ChooseGuidedBroodCareForProof(true);
            string[] workingRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(workingRows, "tutorial_step:brood_nurses_working");
            AssertRow(workingRows, "brood_care_mode:attentive");
            AssertRow(workingRows, "brood_assigned_nurses:2");
            AssertRow(workingRows, "brood_care_duration_seconds:12");
            AssertRow(workingRows, "brood_care_commit_count:1");
            AssertRow(workingRows, "brood_care_pending:true");
            AssertRow(workingRows, "brood_pending_nutrition_gain:25");
            AssertRow(workingRows, "brood_nutrition:35");
            AssertRow(workingRows, "brood_feed_count:0");

            HiveViewProductUiPresenter.CompleteGuidedBroodCareForProof();
            string[] fedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(fedRows, "tutorial_step:brood_fed");
            AssertRow(fedRows, "brood_nutrition:60");
            AssertRow(fedRows, "brood_feed_count:1");
            AssertRow(fedRows, "brood_care_pending:false");
            AssertRow(fedRows, "brood_pending_nutrition_gain:0");
            AssertRow(fedRows, "brood_care_choice:attentive_2_or_rapid_4");
            AssertRow(fedRows, "population_granted_immediately:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] vitalRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(vitalRows, "tutorial_step:brood_vital_check");
            AssertRow(vitalRows, "brood_objective_index:5");
            AssertRow(vitalRows, "brood_objective_target:16");
            AssertRow(vitalRows, "brood_vital_check_count:0");
            AssertRow(vitalRows, "brood_vital_checks_required:3");

            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(0);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_vital_checks_mask:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_vital_check_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_vital_last_check:temperature");
            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(2);
            string[] developmentRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(developmentRows, "tutorial_step:brood_development_ready");
            AssertRow(developmentRows, "brood_vital_checks_mask:7");
            AssertRow(developmentRows, "brood_vital_check_count:3");
            AssertRow(developmentRows, "brood_vital_last_check:respiration");
            AssertRow(developmentRows, "brood_objective_index:6");

            HiveViewProductUiPresenter.ChooseGuidedBroodDevelopmentForProof(false);
            HiveViewProductUiPresenter.ChooseGuidedBroodDevelopmentForProof(false);
            string[] resilienceRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(resilienceRows, "tutorial_step:brood_development_applied");
            AssertRow(resilienceRows, "brood_development_plan:resilience");
            AssertRow(resilienceRows, "brood_development_commit_count:1");
            AssertRow(resilienceRows, "brood_development_pollen_cost:0");
            AssertRow(resilienceRows, "brood_development_stability_gain:6");
            AssertRow(resilienceRows, "worker_care_honey_discount:0");
            AssertRow(resilienceRows, "brood_stability:71");
            AssertRow(resilienceRows, "brood_objective_index:7");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_circuit_welcome");
            CompleteGuidedBroodCareCircuit();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_circuit_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:9");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_incubation_welcome");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_target:16");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_act_milestones:1");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_incubation_inspection_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_incubation_round:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:10");

            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(true);
            string[] preciseRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(preciseRows, "tutorial_step:brood_incubation_inspection_running");
            AssertRow(preciseRows, "brood_incubation_inspection_plan:precise");
            AssertRow(preciseRows, "brood_incubation_inspection_duration_seconds:18");
            AssertRow(preciseRows, "brood_incubation_inspection_assigned_bees:2");
            AssertRow(preciseRows, "brood_incubation_inspection_commit_count:1");
            HiveViewProductUiPresenter.CompleteGuidedBroodIncubationInspectionForProof();
            string[] vitalityRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(vitalityRows, "tutorial_step:brood_incubation_vitality_choice");
            AssertRow(vitalityRows, "brood_stability:83");
            AssertRow(vitalityRows, "brood_incubation_vitality_priority:nutrition");
            AssertRow(vitalityRows, "brood_incubation_treatment_recommendation:jelly_support");
            AssertRow(vitalityRows, "brood_incubation_vitality_decision_time_seconds:0");
            AssertRow(vitalityRows, "brood_incubation_vitality_resource_cost:0");
            AssertRow(vitalityRows, "brood_incubation_vitality_mobile_authority:local_preview_non_official");
            AssertRow(vitalityRows, "brood_incubation_vitality_server_authority:tutorial_progress_revision");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);

            HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(0);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_incubation_check_count:0");
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationVitalityPriorityForProof(false);
            string[] retryRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(retryRows, "tutorial_step:brood_incubation_vitality_choice");
            AssertRow(retryRows, "brood_incubation_vitality_choice:stability");
            AssertRow(retryRows, "brood_incubation_vitality_resolved:false");
            AssertRow(retryRows, "brood_incubation_vitality_attempt_count:1");
            AssertRow(retryRows, "brood_incubation_vitality_mistake_count:1");
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationVitalityPriorityForProof(true);
            string[] understoodRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(understoodRows, "tutorial_step:brood_incubation_check");
            AssertRow(understoodRows, "brood_incubation_vitality_choice:nutrition");
            AssertRow(understoodRows, "brood_incubation_vitality_resolved:true");
            AssertRow(understoodRows, "brood_incubation_vitality_attempt_count:2");
            AssertRow(understoodRows, "brood_incubation_vitality_success_count:1");

            HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(0);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_incubation_check_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_incubation_total_check_count:1");
            HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_incubation_treatment_choice");

            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationTreatmentForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationTreatmentForProof(true);
            string[] jellyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(jellyRows, "brood_incubation_treatment_plan:jelly_support");
            AssertRow(jellyRows, "brood_incubation_treatment_duration_seconds:22");
            AssertRow(jellyRows, "brood_incubation_treatment_assigned_bees:3");
            AssertRow(jellyRows, "brood_incubation_treatment_commit_count:1");
            AssertRow(jellyRows, "brood_incubation_honey_cost:90");
            AssertRow(jellyRows, "brood_incubation_pollen_cost:45");
            HiveViewProductUiPresenter.CompleteGuidedBroodIncubationTreatmentForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_nutrition:81");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_stability:85");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_incubation_round:2");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_incubation_completed_rounds:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:11");
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodIncubationInspectionForProof();
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationVitalityPriorityForProof(true);
            HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationTreatmentForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodIncubationTreatmentForProof();
            string[] secondCohortRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(secondCohortRows, "tutorial_step:brood_incubation_round_result");
            AssertRow(secondCohortRows, "brood_incubation_inspection_commit_count:2");
            AssertRow(secondCohortRows, "brood_incubation_treatment_commit_count:2");
            AssertRow(secondCohortRows, "brood_incubation_total_check_count:6");
            AssertRow(secondCohortRows, "brood_incubation_inspection_stability_gain:2");
            AssertRow(secondCohortRows, "brood_incubation_nutrition_gain:12");
            AssertRow(secondCohortRows, "brood_incubation_treatment_stability_gain:8");
            AssertRow(secondCohortRows, "brood_incubation_wax_cost:35");
            AssertRow(secondCohortRows, "brood_nutrition:85");
            AssertRow(secondCohortRows, "brood_stability:91");
            AssertRow(secondCohortRows, "brood_incubation_vitality_attempt_count:3");
            AssertRow(secondCohortRows, "brood_incubation_vitality_success_count:2");
            AssertRow(secondCohortRows, "brood_incubation_vitality_mistake_count:1");
            AssertRow(secondCohortRows, "brood_incubation_paid_advantage:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_incubation_doctrine_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_incubation_completed_rounds:2");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:12");
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationDoctrineForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationDoctrineForProof(true);
            string[] doctrineRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(doctrineRows, "brood_incubation_doctrine_plan:first_shift");
            AssertRow(doctrineRows, "brood_incubation_doctrine_duration_seconds:26");
            AssertRow(doctrineRows, "brood_incubation_doctrine_assigned_bees:3");
            AssertRow(doctrineRows, "brood_incubation_doctrine_commit_count:1");
            AssertRow(doctrineRows, "brood_incubation_doctrine_honey_cost:140");
            AssertRow(doctrineRows, "brood_incubation_doctrine_pollen_cost:60");
            HiveViewProductUiPresenter.CompleteGuidedBroodIncubationDoctrineForProof();
            string[] incubationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(incubationRows, "tutorial_step:brood_incubation_completed");
            AssertRow(incubationRows, "brood_objective_index:13");
            AssertRow(incubationRows, "brood_incubation_first_shift_bonus:true");
            AssertRow(incubationRows, "opening_act_milestones:1");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_worker_handoff_choice");
            HiveViewProductUiPresenter.ChooseGuidedBroodWorkerHandoffForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_worker_handoff_plan:reinforced_operculum");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_worker_handoff_duration_seconds:16");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_worker_handoff_assigned_bees:2");
            HiveViewProductUiPresenter.CompleteGuidedBroodWorkerHandoffForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(60f));
            HiveViewProductUiPresenter.RegisterGuidedBroodWorkerHandoffCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedBroodWorkerHandoffCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedBroodWorkerHandoffCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedBroodWorkerHandoffCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_worker_handoff_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_emergence_wax_discount:40");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "brood_worker_handoff:reinforced_operculum");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_wax_consolidation_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:15");
            HiveViewProductUiPresenter.ChooseGuidedBroodWaxConsolidationForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_wax_consolidation_plan:thorough");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_wax_consolidation_duration_seconds:18");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_wax_consolidation_assigned_bees:3");
            HiveViewProductUiPresenter.CompleteGuidedBroodWaxConsolidationForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_wax_consolidation_collect");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThan(0f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_wax_consolidation_check");
            HiveViewProductUiPresenter.RegisterGuidedBroodWaxConsolidationCheckForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_wax_consolidation_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_wax_consolidation_check_done:true");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "brood_wax_consolidation:thorough");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "wax_production_bonus:0.02");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:16");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] chapterThreeRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(chapterThreeRows, "tutorial_step:worker_welcome");
            AssertRow(chapterThreeRows, "chapter_id:chapter_3_first_worker");
            AssertRow(chapterThreeRows, "opening_act_milestones:2");
            AssertRow(chapterThreeRows, "chapter_reward_claim_count:2");
            AssertRow(chapterThreeRows, "chapter_reward_last_id:chapter_2");
            AssertRow(chapterThreeRows, "chapter_reward_last_honey:180");
            AssertRow(chapterThreeRows, "chapter_reward_last_wax:40");
        }

        [Test]
        public void GuidedBroodChapterCanSpendWaxForStrongerStability()
        {
            HiveViewProductUiPresenter.BeginGuidedBroodTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("nursery_cluster"), Is.True);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedBroodDiagnosisForProof();

            HiveViewProductUiPresenter.ChooseGuidedBroodPreparationForProof(true);
            string[] runningRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(runningRows, "brood_preparation_mode:wax_lining");
            AssertRow(runningRows, "brood_preparation_assigned_bees:2");
            AssertRow(runningRows, "brood_preparation_duration_seconds:12");
            AssertRow(runningRows, "brood_preparation_wax_cost:80");
            AssertRow(runningRows, "brood_preparation_commit_count:1");
            AssertRow(runningRows, "wax_balance:72220");

            HiveViewProductUiPresenter.ChooseGuidedBroodPreparationForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_preparation_commit_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "wax_balance:72220");

            HiveViewProductUiPresenter.CompleteGuidedBroodPreparationForProof();
            string[] resultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(resultRows, "tutorial_step:brood_preparation_result");
            AssertRow(resultRows, "brood_stability:60");
            AssertRow(resultRows, "brood_preparation_care_bonus:3");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedBroodCareForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_pending_nutrition_gain:25");
        }

        [Test]
        public void GuidedBroodGrowthPlanDiscountsChapterThreeCare()
        {
            HiveViewProductUiPresenter.BeginGuidedBroodTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("nursery_cluster"), Is.True);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedBroodDiagnosisForProof();
            HiveViewProductUiPresenter.ChooseGuidedBroodPreparationForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodPreparationForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedBroodCareForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodCareForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(2);

            HiveViewProductUiPresenter.ChooseGuidedBroodDevelopmentForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedBroodDevelopmentForProof(true);
            string[] growthRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(growthRows, "tutorial_step:brood_development_applied");
            AssertRow(growthRows, "brood_development_plan:growth");
            AssertRow(growthRows, "brood_development_commit_count:1");
            AssertRow(growthRows, "brood_development_pollen_cost:60");
            AssertRow(growthRows, "brood_development_stability_gain:0");
            AssertRow(growthRows, "worker_care_honey_discount:80");
            AssertRow(growthRows, "pollen_balance:98390");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            CompleteGuidedBroodCareCircuit();
            CompleteGuidedBroodIncubation();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("nursery_cluster"), Is.True);
            HiveViewProductUiPresenter.ChooseGuidedWorkerCareForProof(true);
            string[] careRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(careRows, "tutorial_step:worker_feed_progress");
            AssertRow(careRows, "worker_care_honey_discount:140");
            AssertRow(careRows, "worker_care_pending_honey:360");
        }

        [Test]
        public void GuidedBroodCareCircuitRequiresTwoManualCollectionsAndTreatments()
        {
            HiveViewProductUiPresenter.BeginGuidedBroodCircuitForProof();
            HiveViewProductUiPresenter.SetManualProductionForProof("honey_storage", 0f, 1000f);

            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_circuit_welcome");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_circuit_supply_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_circuit_round:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:7");

            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitSupplyForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitSupplyForProof(true);
            string[] nectarRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(nectarRows, "tutorial_step:brood_circuit_supply_running");
            AssertRow(nectarRows, "brood_circuit_supply_plan:nectar");
            AssertRow(nectarRows, "brood_circuit_supply_duration_seconds:16");
            AssertRow(nectarRows, "brood_circuit_supply_assigned_bees:3");
            AssertRow(nectarRows, "brood_circuit_supply_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedBroodCircuitSupplyForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_circuit_collect_honey");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_hotspot:honey_storage");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells"), Is.EqualTo(0f));
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThanOrEqualTo(180f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_circuit_treatment_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_circuit_collected_honey:180");

            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitTreatmentForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitTreatmentForProof(true);
            string[] nurseRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(nurseRows, "tutorial_step:brood_circuit_treatment_running");
            AssertRow(nurseRows, "brood_circuit_treatment_plan:nurse_rotation");
            AssertRow(nurseRows, "brood_circuit_treatment_duration_seconds:16");
            AssertRow(nurseRows, "brood_circuit_treatment_assigned_bees:3");
            AssertRow(nurseRows, "brood_circuit_treatment_commit_count:1");
            AssertRow(nurseRows, "brood_circuit_honey_cost:110");
            AssertRow(nurseRows, "brood_circuit_pollen_cost:60");
            AssertRow(nurseRows, "brood_circuit_wax_cost:0");

            HiveViewProductUiPresenter.CompleteGuidedBroodCircuitTreatmentForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_circuit_round_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_circuit_nutrition_gain:8");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_circuit_stability_gain:3");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();

            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_circuit_supply_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_circuit_round:2");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_circuit_completed_rounds:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_objective_index:8");

            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitSupplyForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodCircuitSupplyForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_circuit_collect_pollen");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_hotspot:warehouse_cells");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells"), Is.GreaterThanOrEqualTo(140f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_circuit_collected_pollen:140");

            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitTreatmentForProof(false);
            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitTreatmentForProof(false);
            string[] thermalRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(thermalRows, "brood_circuit_treatment_plan:thermal_watch");
            AssertRow(thermalRows, "brood_circuit_treatment_duration_seconds:13");
            AssertRow(thermalRows, "brood_circuit_treatment_assigned_bees:2");
            AssertRow(thermalRows, "brood_circuit_treatment_commit_count:2");
            AssertRow(thermalRows, "brood_circuit_wax_cost:45");

            HiveViewProductUiPresenter.CompleteGuidedBroodCircuitTreatmentForProof();
            string[] secondResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(secondResultRows, "tutorial_step:brood_circuit_round_result");
            AssertRow(secondResultRows, "brood_circuit_nutrition_gain:13");
            AssertRow(secondResultRows, "brood_circuit_stability_gain:10");
            AssertRow(secondResultRows, "brood_circuit_supply_commit_count:2");
            AssertRow(secondResultRows, "brood_circuit_paid_advantage:false");
            AssertRow(secondResultRows, "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] completedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(completedRows, "tutorial_step:brood_circuit_completed");
            AssertRow(completedRows, "brood_circuit_completed_rounds:2");
            AssertRow(completedRows, "brood_objective_index:9");
            AssertRow(completedRows, "opening_act_milestones:1");
        }

        [Test]
        public void GuidedBroodIncubationCanPrioritizeResilience()
        {
            HiveViewProductUiPresenter.BeginGuidedBroodIncubationForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_incubation_welcome");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.SetBroodVitalityForProof(80f, 46f);

            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedBroodIncubationInspectionForProof();
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationVitalityPriorityForProof(false);
                AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_incubation_vitality_priority:stability");
                AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_incubation_treatment_recommendation:hygiene_rotation");
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(0);
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(1);
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(2);
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationTreatmentForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedBroodIncubationTreatmentForProof();
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            }

            string[] choiceRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(choiceRows, "tutorial_step:brood_incubation_doctrine_choice");
            AssertRow(choiceRows, "brood_incubation_completed_rounds:2");
            AssertRow(choiceRows, "brood_incubation_total_check_count:6");
            AssertRow(choiceRows, "brood_incubation_vitality_success_count:2");
            AssertRow(choiceRows, "brood_incubation_wax_cost:70");
            AssertRow(choiceRows, "brood_stability:58");

            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationDoctrineForProof(false);
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationDoctrineForProof(false);
            string[] runningRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(runningRows, "tutorial_step:brood_incubation_doctrine_running");
            AssertRow(runningRows, "brood_incubation_doctrine_plan:resilience");
            AssertRow(runningRows, "brood_incubation_doctrine_duration_seconds:30");
            AssertRow(runningRows, "brood_incubation_doctrine_assigned_bees:2");
            AssertRow(runningRows, "brood_incubation_doctrine_commit_count:1");
            AssertRow(runningRows, "brood_incubation_doctrine_honey_cost:80");
            AssertRow(runningRows, "brood_incubation_doctrine_wax_cost:40");

            HiveViewProductUiPresenter.CompleteGuidedBroodIncubationDoctrineForProof();
            string[] completedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(completedRows, "tutorial_step:brood_incubation_completed");
            AssertRow(completedRows, "brood_stability:66");
            AssertRow(completedRows, "brood_incubation_doctrine_stability_gain:8");
            AssertRow(completedRows, "brood_incubation_doctrine_care_gain:1");
            AssertRow(completedRows, "brood_incubation_first_shift_bonus:false");
            AssertRow(completedRows, "worker_brood_care_bonus:1");
            AssertRow(completedRows, "brood_incubation_paid_advantage:false");
        }

        [Test]
        public void GuidedWorkerChapterFormsTheFirstWorkerAfterTheTimer()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            CompleteGuidedFirstChapter();
            HiveViewProductUiPresenter.SetBroodCareForProof(35f, 3000f);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("nursery_cluster"), Is.True);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedBroodDiagnosisForProof();
            HiveViewProductUiPresenter.ChooseGuidedBroodPreparationForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodPreparationForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_nurses_working");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_care_mode:rapid");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_assigned_nurses:4");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_care_duration_seconds:7");
            HiveViewProductUiPresenter.CompleteGuidedBroodCareForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedBroodVitalCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedBroodDevelopmentForProof(false);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            CompleteGuidedBroodCareCircuit();
            CompleteGuidedBroodIncubation(true);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();

            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_welcome");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_inspect_nursery");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_hotspot:nursery_cluster");

            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("guard_post"), Is.False);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "blocked_clicks:1");
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("nursery_cluster"), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_feed_ready");

            HiveViewProductUiPresenter.ChooseGuidedWorkerCareForProof(true);
            string[] careRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(careRows, "tutorial_step:worker_feed_progress");
            AssertRow(careRows, "worker_care_mode:measured");
            AssertRow(careRows, "worker_care_assigned_nurses:3");
            AssertRow(careRows, "worker_care_duration_seconds:12");
            AssertRow(careRows, "worker_care_pending_honey:500");
            AssertRow(careRows, "worker_care_pending_nutrition:21");
            AssertRow(careRows, "worker_care_commit_count:1");
            AssertRow(careRows, "brood_nutrition:79");
            AssertRow(careRows, "worker_count:30");

            HiveViewProductUiPresenter.CompleteGuidedWorkerCareForProof();
            string[] emergenceReadyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(emergenceReadyRows, "tutorial_step:worker_emergence_ready");
            AssertRow(emergenceReadyRows, "worker_care_pending:false");
            AssertRow(emergenceReadyRows, "worker_care_pending_honey:0");
            AssertRow(emergenceReadyRows, "worker_care_pending_nutrition:0");
            AssertRow(emergenceReadyRows, "brood_nutrition:100");
            AssertRow(emergenceReadyRows, "worker_objective_index:2");

            HiveViewProductUiPresenter.ChooseGuidedWorkerEmergenceForProof(false);
            HiveViewProductUiPresenter.ChooseGuidedWorkerEmergenceForProof(false);
            string[] emergenceRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(emergenceRows, "tutorial_step:worker_emergence_running");
            AssertRow(emergenceRows, "worker_emergence_plan:natural");
            AssertRow(emergenceRows, "worker_emergence_duration_seconds:12");
            AssertRow(emergenceRows, "worker_emergence_assigned_bees:2");
            AssertRow(emergenceRows, "worker_emergence_commit_count:1");
            AssertRow(emergenceRows, "worker_emergence_wax_cost:0");
            AssertRow(emergenceRows, "worker_first_shift_enhanced:false");

            HiveViewProductUiPresenter.CompleteGuidedWorkerEmergenceForProof();
            string[] emergenceResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(emergenceResultRows, "tutorial_step:worker_emergence_result");
            AssertRow(emergenceResultRows, "worker_emergence_pending:false");
            AssertRow(emergenceResultRows, "worker_first_shift_enhanced:true");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] readyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(readyRows, "tutorial_step:worker_ready_to_form");
            AssertRow(readyRows, "worker_objective_index:3");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] trainingRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(trainingRows, "tutorial_step:worker_training");
            AssertRow(trainingRows, "worker_training_pending:true");
            AssertRow(trainingRows, "worker_training_commit_count:1");
            AssertRow(trainingRows, "worker_training_cost:420 miel, 180 pollen");
            AssertRow(trainingRows, "worker_count:30");
            AssertRow(trainingRows, "population_granted_immediately:false");

            HiveViewProductUiPresenter.CompleteNurseryWorkerFormationForProof();
            string[] arrivedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(arrivedRows, "tutorial_step:worker_arrived");
            AssertRow(arrivedRows, "worker_training_pending:false");
            AssertRow(arrivedRows, "worker_count:31");
            AssertRow(arrivedRows, "brood_nutrition:60");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] orientationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(orientationRows, "tutorial_step:worker_orientation_check");
            AssertRow(orientationRows, "worker_orientation_check_count:0");
            AssertRow(orientationRows, "worker_orientation_recommendation:nursery");
            AssertRow(orientationRows, "worker_orientation_recommendation_binding:false");

            HiveViewProductUiPresenter.RegisterGuidedWorkerOrientationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerOrientationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerOrientationCheckForProof(1);
            string[] partialOrientationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(partialOrientationRows, "tutorial_step:worker_orientation_check");
            AssertRow(partialOrientationRows, "worker_orientation_check_count:2");
            AssertRow(partialOrientationRows, "worker_orientation_checks_mask:3");

            HiveViewProductUiPresenter.RegisterGuidedWorkerOrientationCheckForProof(2);
            string[] trialReadyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(trialReadyRows, "tutorial_step:worker_trial_ready");
            AssertRow(trialReadyRows, "worker_objective_index:4");
            AssertRow(trialReadyRows, "worker_orientation_check_count:3");
            AssertRow(trialReadyRows, "worker_orientation_checks_mask:7");
            AssertRow(trialReadyRows, "worker_orientation_last_check:orientation_signal");

            HiveViewProductUiPresenter.ChooseGuidedWorkerTrialForProof(false);
            HiveViewProductUiPresenter.ChooseGuidedWorkerTrialForProof(false);
            string[] trialRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(trialRows, "tutorial_step:worker_trial_running");
            AssertRow(trialRows, "worker_trial_mode:honey");
            AssertRow(trialRows, "worker_trial_duration_seconds:10");
            AssertRow(trialRows, "worker_trial_assigned_bees:1");
            AssertRow(trialRows, "worker_trial_commit_count:1");
            AssertRow(trialRows, "worker_trial_pending:true");
            AssertRow(trialRows, "worker_objective_index:5");

            HiveViewProductUiPresenter.CompleteGuidedWorkerTrialForProof();
            string[] collectRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(collectRows, "tutorial_step:worker_trial_collect");
            AssertRow(collectRows, "target_hotspot:honey_storage");
            AssertRow(collectRows, "worker_trial_pending_honey:90");
            AssertRow(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "honey_pending:90");

            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.EqualTo(90f).Within(0.01f));
            string[] resultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(resultRows, "tutorial_step:worker_trial_result");
            AssertRow(resultRows, "worker_trial_collect_pending:false");
            AssertRow(resultRows, "worker_trial_pending_honey:0");
            AssertRow(resultRows, "worker_trial_collected_honey:90");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_assignment_ready");
            HiveViewProductUiPresenter.ChooseGuidedFirstWorkerAssignmentForProof(false);
            string[] assignmentRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(assignmentRows, "tutorial_step:worker_assignment_applied");
            AssertRow(assignmentRows, "worker_assignment:honey");
            AssertRow(assignmentRows, "worker_assignment_commit_count:1");
            AssertRow(assignmentRows, "worker_honey_production_bonus_percent:4");
            AssertRow(assignmentRows, "worker_brood_care_bonus:0");
            AssertRow(assignmentRows, "honey_production_per_hour:2642");
            AssertRow(assignmentRows, "worker_objective_index:6");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] firstShiftRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstShiftRows, "tutorial_step:worker_first_shift_running");
            AssertRow(firstShiftRows, "worker_first_shift_pending:true");
            AssertRow(firstShiftRows, "worker_first_shift_duration_seconds:10");
            AssertRow(firstShiftRows, "worker_first_shift_assigned_bees:1");
            AssertRow(firstShiftRows, "worker_first_shift_commit_count:1");
            AssertRow(firstShiftRows, "worker_objective_index:7");

            HiveViewProductUiPresenter.CompleteGuidedWorkerFirstShiftForProof();
            string[] firstShiftCollectRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstShiftCollectRows, "tutorial_step:worker_first_shift_collect");
            AssertRow(firstShiftCollectRows, "target_hotspot:honey_storage");
            AssertRow(firstShiftCollectRows, "worker_first_shift_pending_honey:150");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.EqualTo(150f).Within(0.01f));

            string[] firstShiftResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstShiftResultRows, "tutorial_step:worker_first_shift_result");
            AssertRow(firstShiftResultRows, "worker_first_shift_collect_pending:false");
            AssertRow(firstShiftResultRows, "worker_first_shift_pending_honey:0");
            AssertRow(firstShiftResultRows, "worker_first_shift_collected_honey:150");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_integration_welcome");
            CompleteGuidedWorkerCertification();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_certification_completed_rounds:2");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_certification_total_check_count:6");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] chapterFourRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(chapterFourRows, "tutorial_step:upgrade_welcome");
            AssertRow(chapterFourRows, "chapter_id:chapter_4_first_upgrade");
            AssertRow(chapterFourRows, "opening_act_milestones:3");
            AssertRow(chapterFourRows, "chapter_reward_claim_count:3");
            AssertRow(chapterFourRows, "chapter_reward_last_id:chapter_3");
            AssertRow(chapterFourRows, "chapter_reward_last_honey:300");
            AssertRow(chapterFourRows, "chapter_reward_last_pollen:120");
        }

        [Test]
        public void GuidedWorkerCanTryNurseryBeforeChoosingTheReserve()
        {
            HiveViewProductUiPresenter.BeginGuidedWorkerOrientationForProof(true);
            HiveViewProductUiPresenter.RegisterGuidedWorkerOrientationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerOrientationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerOrientationCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_orientation_recommendation:honey");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_trial_ready");
            HiveViewProductUiPresenter.ChooseGuidedWorkerTrialForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedWorkerTrialForProof(true);

            string[] trialRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(trialRows, "tutorial_step:worker_trial_running");
            AssertRow(trialRows, "worker_trial_mode:nursery");
            AssertRow(trialRows, "worker_trial_duration_seconds:12");
            AssertRow(trialRows, "worker_trial_assigned_bees:1");
            AssertRow(trialRows, "worker_trial_commit_count:1");
            AssertRow(trialRows, "brood_stability:46");

            HiveViewProductUiPresenter.CompleteGuidedWorkerTrialForProof();
            string[] resultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(resultRows, "tutorial_step:worker_trial_result");
            AssertRow(resultRows, "worker_trial_stability_gain:5");
            AssertRow(resultRows, "brood_stability:51");
            AssertRow(resultRows, "worker_trial_pending_honey:0");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedFirstWorkerAssignmentForProof(false);
            string[] assignmentRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(assignmentRows, "worker_trial_mode:nursery");
            AssertRow(assignmentRows, "worker_trial_final_assignment_unlocked:true");
            AssertRow(assignmentRows, "worker_assignment:honey");
            AssertRow(assignmentRows, "worker_honey_production_bonus_percent:3");
        }

        [Test]
        public void GuidedWorkerCertificationRequiresTwoQualityControlledRounds()
        {
            HiveViewProductUiPresenter.BeginGuidedWorkerCertificationForProof();
            string[] welcomeRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(welcomeRows, "tutorial_step:worker_integration_welcome");
            AssertRow(welcomeRows, "worker_objective_index:8");
            AssertRow(welcomeRows, "worker_certification_paid_advantage:false");
            AssertRow(welcomeRows, "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationTaskForProof(false);
            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationTaskForProof(false);
            string[] reserveRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(reserveRows, "tutorial_step:worker_certification_task_running");
            AssertRow(reserveRows, "worker_certification_round:1");
            AssertRow(reserveRows, "worker_certification_task_plan:reserve");
            AssertRow(reserveRows, "worker_certification_task_duration_seconds:14");
            AssertRow(reserveRows, "worker_certification_task_assigned_bees:1");
            AssertRow(reserveRows, "worker_certification_task_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationTaskForProof();
            string[] collectRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(collectRows, "tutorial_step:worker_certification_collect");
            AssertRow(collectRows, "target_hotspot:honey_storage");
            AssertRow(collectRows, "worker_certification_pending_honey:140");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.EqualTo(140f).Within(0.01f));

            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(2);
            string[] checkRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(checkRows, "tutorial_step:worker_certification_mentorship_choice");
            AssertRow(checkRows, "worker_certification_check_count:3");
            AssertRow(checkRows, "worker_certification_total_check_count:3");
            AssertRow(checkRows, "worker_certification_checks_mask:7");

            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationMentorshipForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationMentorshipForProof(true);
            string[] autonomyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(autonomyRows, "tutorial_step:worker_certification_mentorship_running");
            AssertRow(autonomyRows, "worker_certification_mentorship_plan:autonomy");
            AssertRow(autonomyRows, "worker_certification_mentorship_duration_seconds:18");
            AssertRow(autonomyRows, "worker_certification_mentorship_assigned_bees:1");
            AssertRow(autonomyRows, "worker_certification_mentorship_commit_count:1");
            AssertRow(autonomyRows, "worker_certification_honey_cost:120");
            AssertRow(autonomyRows, "worker_certification_pollen_cost:60");

            HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationMentorshipForProof();
            string[] firstResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstResultRows, "tutorial_step:worker_certification_round_result");
            AssertRow(firstResultRows, "worker_certification_completed_rounds:1");
            AssertRow(firstResultRows, "worker_certification_honey_production_gain_percent:1");
            AssertRow(firstResultRows, "worker_reward_after_commission_only:true");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationTaskForProof(true);
            string[] nurseryRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(nurseryRows, "worker_certification_round:2");
            AssertRow(nurseryRows, "worker_certification_task_plan:nursery");
            AssertRow(nurseryRows, "worker_certification_task_duration_seconds:16");
            AssertRow(nurseryRows, "worker_certification_task_commit_count:2");

            HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationTaskForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_certification_check");
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationMentorshipForProof(false);
            string[] mentorshipRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(mentorshipRows, "worker_certification_mentorship_plan:mentorship");
            AssertRow(mentorshipRows, "worker_certification_mentorship_duration_seconds:20");
            AssertRow(mentorshipRows, "worker_certification_mentorship_assigned_bees:2");
            AssertRow(mentorshipRows, "worker_certification_mentorship_commit_count:2");
            AssertRow(mentorshipRows, "worker_certification_wax_cost:50");

            HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationMentorshipForProof();
            string[] secondResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(secondResultRows, "worker_certification_completed_rounds:2");
            AssertRow(secondResultRows, "worker_certification_total_check_count:6");
            AssertRow(secondResultRows, "worker_certification_task_stability_gain:5");
            AssertRow(secondResultRows, "worker_certification_brood_care_gain:1");
            AssertRow(secondResultRows, "worker_certification_stability_gain:3");
            AssertRow(secondResultRows, "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] completedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(completedRows, "tutorial_step:worker_certification_completed");
            AssertRow(completedRows, "worker_objective_index:10");
            AssertRow(completedRows, "opening_act_milestones:2");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_workshop_handoff_choice");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);
            HiveViewProductUiPresenter.ChooseGuidedWorkerWorkshopHandoffForProof(true);
            string[] handoffRunningRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(handoffRunningRows, "worker_handoff_plan:wax_convoy");
            AssertRow(handoffRunningRows, "worker_handoff_duration_seconds:18");
            AssertRow(handoffRunningRows, "worker_handoff_assigned_bees:3");
            AssertRow(handoffRunningRows, "worker_handoff_commit_count:1");
            HiveViewProductUiPresenter.CompleteGuidedWorkerWorkshopHandoffForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_workshop_handoff_collect");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_hotspot:wax_workshop");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(100f));
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(true), Is.True);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopHandoffCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopHandoffCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopHandoffCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopHandoffCheckForProof(2);
            string[] handoffResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(handoffResultRows, "tutorial_step:worker_workshop_handoff_result");
            AssertRow(handoffResultRows, "worker_handoff_check_count:3");
            AssertRow(handoffResultRows, "worker_handoff_wax_discount:80");
            AssertRow(handoffResultRows, "upgrade_cost:2484 miel, 822 cire");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "worker_workshop_handoff:wax_convoy");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_workshop_commission_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_objective_index:12");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_act_milestones:2");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);

            HiveViewProductUiPresenter.ChooseGuidedWorkerWorkshopCommissionForProof(false);
            HiveViewProductUiPresenter.ChooseGuidedWorkerWorkshopCommissionForProof(false);
            string[] commissionRunningRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(commissionRunningRows, "tutorial_step:worker_workshop_commission_running");
            AssertRow(commissionRunningRows, "worker_commission_plan:calibration_template");
            AssertRow(commissionRunningRows, "worker_commission_duration_seconds:15");
            AssertRow(commissionRunningRows, "worker_commission_assigned_bees:2");
            AssertRow(commissionRunningRows, "worker_commission_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedWorkerWorkshopCommissionForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_workshop_commission_check");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(true), Is.True);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(2);
            string[] commissionResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(commissionResultRows, "tutorial_step:worker_workshop_commission_result");
            AssertRow(commissionResultRows, "worker_commission_checks_mask:7");
            AssertRow(commissionResultRows, "worker_commission_check_count:3");
            AssertRow(commissionResultRows, "workshop_calibration_wax_bonus:40");
            AssertRow(commissionResultRows, "workshop_application_wax_discount:0");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "worker_workshop_commission:calibration_template");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:worker_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_act_milestones:2");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_welcome");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "opening_act_milestones:3");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("wax_workshop"), Is.True);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedUpgradeAuditForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedUpgradePlanForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedUpgradeForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedUpgradeCalibrationForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_calibration_collect");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_calibration_pending_wax:160");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(160f));
            string[] qualificationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(qualificationRows, "tutorial_step:upgrade_batch_qualification_choice");
            AssertRow(qualificationRows, "upgrade_calibration_collected_wax:160");
            HiveViewProductUiPresenter.ChooseGuidedUpgradeBatchQualificationForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedUpgradeApplicationForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_calibration_collected_wax:160");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_application_wax_cost:80");
        }

        [Test]
        public void GuidedUpgradeChapterImprovesWaxProductionAfterTheTimer()
        {
            HiveViewProductUiPresenter.BeginGuidedUpgradeTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_welcome");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_4_resource_primer_visible:true");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_4_resource_roles:honey_energy_wax_construction_pollen_growth");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_4_cost_gain_legend:true");
            Assert.That(HiveViewProductUiPresenter.GuidedChapterFourResourcePrimerAcceptsInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.GuidedChapterFourResourcePrimerAcceptsInputForProof(true), Is.True);

            HiveViewProductUiPresenter.DismissGuidedChapterFourResourcePrimerForProof();
            Assert.That(HiveViewProductUiPresenter.GuidedChapterFourResourcePrimerVisibleForProof(), Is.False);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_welcome");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_4_resource_primer_dismissed:true");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_inspect_workshop");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_hotspot:wax_workshop");

            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("honey_storage"), Is.False);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "blocked_clicks:1");
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("wax_workshop"), Is.True);
            string[] auditReadyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(auditReadyRows, "tutorial_step:upgrade_audit_ready");
            AssertRow(auditReadyRows, "upgrade_objective_index:2");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] auditRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(auditRows, "tutorial_step:upgrade_auditing");
            AssertRow(auditRows, "upgrade_audit_pending:true");
            AssertRow(auditRows, "upgrade_audit_duration_seconds:6");
            AssertRow(auditRows, "upgrade_audit_assigned_builders:2");
            AssertRow(auditRows, "upgrade_audit_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedUpgradeAuditForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_audit_result");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] readyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(readyRows, "tutorial_step:upgrade_ready");
            AssertRow(readyRows, "upgrade_objective_index:3");
            AssertRow(readyRows, "upgrade_level:22");
            AssertRow(readyRows, "upgrade_cost:2484 miel, 902 cire");
            AssertRow(readyRows, "upgrade_duration_seconds:18");
            AssertRow(readyRows, "wax_production_per_hour:1180");

            HiveViewProductUiPresenter.ChooseGuidedUpgradePlanForProof(true);
            string[] runningRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(runningRows, "tutorial_step:upgrade_running");
            AssertRow(runningRows, "upgrade_pending:true");
            AssertRow(runningRows, "upgrade_hotspot:wax_workshop");
            AssertRow(runningRows, "upgrade_commit_count:1");
            AssertRow(runningRows, "upgrade_plan:production");
            AssertRow(runningRows, "upgrade_plan_commit_count:1");
            AssertRow(runningRows, "upgrade_assigned_builders:5");
            AssertRow(runningRows, "wax_production_bonus_percent:0");
            AssertRow(runningRows, "wax_capacity_bonus_percent:0");
            AssertRow(runningRows, "wax_production_per_hour:1180");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_commit_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_plan_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedUpgradeForProof();
            string[] resultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(resultRows, "tutorial_step:upgrade_result");
            AssertRow(resultRows, "upgrade_pending:false");
            AssertRow(resultRows, "upgrade_level:23");
            AssertRow(resultRows, "wax_production_bonus_percent:2");
            AssertRow(resultRows, "wax_capacity_bonus_percent:0");
            AssertRow(resultRows, "wax_production_per_hour:1251");
            AssertRow(resultRows, "wax_local_capacity:1180");
            AssertRow(resultRows, "upgrade_reward:+1 niveau, production de cire +6 %");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] calibrationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(calibrationRows, "tutorial_step:upgrade_calibration_running");
            AssertRow(calibrationRows, "upgrade_calibration_pending:true");
            AssertRow(calibrationRows, "upgrade_calibration_duration_seconds:10");
            AssertRow(calibrationRows, "upgrade_calibration_assigned_builders:2");
            AssertRow(calibrationRows, "upgrade_calibration_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedUpgradeCalibrationForProof();
            string[] collectRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(collectRows, "tutorial_step:upgrade_calibration_collect");
            AssertRow(collectRows, "target_hotspot:wax_workshop");
            AssertRow(collectRows, "upgrade_calibration_pending_wax:120");
            Assert.That(
                ProofInt(HiveViewProductUiPresenter.ManualProductionCollectionForProof(), "wax_pending"),
                Is.GreaterThanOrEqualTo(120));

            Assert.That(
                HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"),
                Is.GreaterThanOrEqualTo(120f));
            string[] qualificationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(qualificationRows, "tutorial_step:upgrade_batch_qualification_choice");
            AssertRow(qualificationRows, "upgrade_calibration_collected_wax:120");
            AssertRow(qualificationRows, "upgrade_calibration_pending_wax:0");
            AssertRow(qualificationRows, "upgrade_batch_qualification_expected:heat");
            AssertRow(qualificationRows, "upgrade_batch_qualification_attempt_count:0");
            AssertRow(qualificationRows, "upgrade_batch_qualification_error_count:0");
            AssertRow(qualificationRows, "upgrade_batch_qualification_success_count:0");
            AssertRow(qualificationRows, "upgrade_batch_qualification_recommendation:none");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);

            HiveViewProductUiPresenter.ChooseGuidedUpgradeBatchQualificationForProof(false);
            string[] retryRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(retryRows, "tutorial_step:upgrade_batch_qualification_choice");
            AssertRow(retryRows, "upgrade_batch_qualification_attempt_count:1");
            AssertRow(retryRows, "upgrade_batch_qualification_error_count:1");
            AssertRow(retryRows, "upgrade_batch_qualification_success_count:0");
            AssertRow(retryRows, "upgrade_batch_qualification_recommendation:none");
            AssertProofValuesEqual(
                qualificationRows,
                retryRows,
                "honey_balance",
                "wax_balance",
                "pollen_balance",
                "brood_stability",
                "wax_production_bonus_percent",
                "wax_capacity_bonus_percent",
                "upgrade_commit_count",
                "upgrade_plan_commit_count",
                "upgrade_calibration_commit_count",
                "upgrade_calibration_collected_wax",
                "upgrade_application_commit_count");

            HiveViewProductUiPresenter.ChooseGuidedUpgradeBatchQualificationForProof(true);
            string[] applicationReadyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(applicationReadyRows, "tutorial_step:upgrade_application_ready");
            AssertRow(applicationReadyRows, "upgrade_batch_qualification_attempt_count:2");
            AssertRow(applicationReadyRows, "upgrade_batch_qualification_error_count:1");
            AssertRow(applicationReadyRows, "upgrade_batch_qualification_success_count:1");
            AssertRow(applicationReadyRows, "upgrade_batch_qualification_recommendation:reserve");
            AssertRow(applicationReadyRows, "upgrade_objective_index:6");
            AssertRow(applicationReadyRows, "upgrade_objective_target:13");

            HiveViewProductUiPresenter.ChooseGuidedUpgradeBatchQualificationForProof(true);
            string[] repeatedQualificationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(repeatedQualificationRows, "tutorial_step:upgrade_application_ready");
            AssertRow(repeatedQualificationRows, "upgrade_batch_qualification_attempt_count:2");
            AssertRow(repeatedQualificationRows, "upgrade_batch_qualification_success_count:1");

            HiveViewProductUiPresenter.ChooseGuidedUpgradeApplicationForProof(false);
            string[] applicationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(applicationRows, "tutorial_step:upgrade_application_running");
            AssertRow(applicationRows, "upgrade_application_pending:true");
            AssertRow(applicationRows, "upgrade_application_plan:reserve");
            AssertRow(applicationRows, "upgrade_application_duration_seconds:10");
            AssertRow(applicationRows, "upgrade_application_assigned_builders:2");
            AssertRow(applicationRows, "upgrade_application_commit_count:1");
            AssertRow(applicationRows, "upgrade_application_wax_cost:80");

            HiveViewProductUiPresenter.ChooseGuidedUpgradeApplicationForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_application_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedUpgradeApplicationForProof();
            string[] applicationResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(applicationResultRows, "tutorial_step:upgrade_application_result");
            AssertRow(applicationResultRows, "upgrade_application_pending:false");
            AssertRow(applicationResultRows, "upgrade_application_honey_production_gain_percent:1");
            AssertRow(applicationResultRows, "upgrade_objective_index:6");
            AssertRow(applicationResultRows, "upgrade_objective_target:13");
            AssertRow(applicationResultRows, "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] operationsWelcomeRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(operationsWelcomeRows, "tutorial_step:upgrade_operations_welcome");
            AssertRow(operationsWelcomeRows, "upgrade_objective_index:7");
            AssertRow(operationsWelcomeRows, "upgrade_operations_round_target:2");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_supply_choice");
            HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsSupplyForProof(false);
            string[] recycledRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(recycledRows, "tutorial_step:upgrade_operations_supply_running");
            AssertRow(recycledRows, "upgrade_operations_round:1");
            AssertRow(recycledRows, "upgrade_operations_supply_plan:recycled");
            AssertRow(recycledRows, "upgrade_operations_supply_duration_seconds:13");
            AssertRow(recycledRows, "upgrade_operations_supply_assigned_builders:2");
            AssertRow(recycledRows, "upgrade_operations_supply_commit_count:1");
            HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsSupplyForProof();
            string[] firstCollectRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstCollectRows, "tutorial_step:upgrade_operations_collect");
            AssertRow(firstCollectRows, "target_hotspot:wax_workshop");
            AssertRow(firstCollectRows, "upgrade_operations_pending_wax:90");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(90f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_deployment_choice");

            HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsDeploymentForProof(false);
            string[] firstDeploymentRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstDeploymentRows, "tutorial_step:upgrade_operations_deployment_running");
            AssertRow(firstDeploymentRows, "upgrade_operations_deployment_plan:reserve");
            AssertRow(firstDeploymentRows, "upgrade_operations_deployment_duration_seconds:15");
            AssertRow(firstDeploymentRows, "upgrade_operations_deployment_assigned_builders:2");
            AssertRow(firstDeploymentRows, "upgrade_operations_deployment_wax_cost:80");
            HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsDeploymentForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_check");
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(2);
            string[] firstRoundRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstRoundRows, "tutorial_step:upgrade_operations_round_result");
            AssertRow(firstRoundRows, "upgrade_operations_completed_rounds:1");
            AssertRow(firstRoundRows, "upgrade_operations_total_check_count:3");
            AssertRow(firstRoundRows, "upgrade_operations_honey_production_gain_percent:1");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_objective_index:8");
            HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsSupplyForProof(true);
            string[] freshRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(freshRows, "upgrade_operations_round:2");
            AssertRow(freshRows, "upgrade_operations_supply_plan:fresh");
            AssertRow(freshRows, "upgrade_operations_supply_duration_seconds:16");
            AssertRow(freshRows, "upgrade_operations_supply_assigned_builders:3");
            AssertRow(freshRows, "upgrade_operations_supply_commit_count:2");
            AssertRow(freshRows, "upgrade_operations_supply_honey_cost:70");
            HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsSupplyForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_operations_pending_wax:120");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(120f));

            HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsDeploymentForProof(true);
            string[] secondDeploymentRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(secondDeploymentRows, "upgrade_operations_deployment_plan:nursery");
            AssertRow(secondDeploymentRows, "upgrade_operations_deployment_duration_seconds:17");
            AssertRow(secondDeploymentRows, "upgrade_operations_deployment_assigned_builders:3");
            AssertRow(secondDeploymentRows, "upgrade_operations_deployment_commit_count:2");
            AssertRow(secondDeploymentRows, "upgrade_operations_deployment_wax_cost:160");
            HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsDeploymentForProof();
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(2);
            string[] secondRoundRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(secondRoundRows, "tutorial_step:upgrade_operations_round_result");
            AssertRow(secondRoundRows, "upgrade_operations_completed_rounds:2");
            AssertRow(secondRoundRows, "upgrade_operations_total_check_count:6");
            AssertRow(secondRoundRows, "upgrade_operations_brood_stability_gain:4");
            AssertRow(secondRoundRows, "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_certification_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_objective_index:9");
            HiveViewProductUiPresenter.ChooseGuidedUpgradeCertificationForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_certification_duration_seconds:18");
            HiveViewProductUiPresenter.CompleteGuidedUpgradeCertificationForProof();
            HiveViewProductUiPresenter.RegisterGuidedUpgradeCertificationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeCertificationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeCertificationCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_certification_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_certification_plan:thermal");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_doctrine_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_objective_index:10");
            HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsDoctrineForProof(true);
            string[] doctrineRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(doctrineRows, "tutorial_step:upgrade_operations_doctrine_running");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_plan:precision");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_duration_seconds:18");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_assigned_builders:2");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_honey_cost:60");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_wax_cost:40");
            HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsDoctrineForProof();
            string[] doctrineCheckRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(doctrineCheckRows, "tutorial_step:upgrade_operations_doctrine_check");
            AssertRow(doctrineCheckRows, "upgrade_operations_doctrine_production_gain_percent:0");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "workshop_doctrine:none");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(true), Is.True);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(0);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_operations_doctrine_check_count:1");
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(1);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_doctrine_check");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_operations_doctrine_production_gain_percent:0");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "workshop_doctrine:none");
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(2);
            string[] certifiedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(certifiedRows, "tutorial_step:upgrade_operations_completed");
            AssertRow(certifiedRows, "upgrade_objective_index:11");
            AssertRow(certifiedRows, "upgrade_operations_doctrine_check_count:3");
            AssertRow(certifiedRows, "upgrade_operations_doctrine_checks_mask:7");
            AssertRow(certifiedRows, "upgrade_operations_doctrine_last_check:traceability");
            AssertRow(certifiedRows, "upgrade_operations_doctrine_production_gain_percent:1");
            AssertRow(certifiedRows, "wax_production_bonus_percent:4");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "workshop_doctrine:precision");
            AssertRow(certifiedRows, "reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:workshop_defense_handoff_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_objective_index:12");
            HiveViewProductUiPresenter.ChooseGuidedWorkshopDefenseHandoffForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "workshop_defense_handoff_plan:wax_shields");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "workshop_defense_handoff_duration_seconds:15");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "workshop_defense_handoff_assigned_builders:3");
            HiveViewProductUiPresenter.CompleteGuidedWorkshopDefenseHandoffForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:workshop_defense_handoff_collect");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.RegisterGuidedWorkshopDefenseHandoffCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkshopDefenseHandoffCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkshopDefenseHandoffCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkshopDefenseHandoffCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:workshop_defense_handoff_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "workshop_defense_handoff_check_count:3");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_barrier_wax_discount:60");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "workshop_defense_handoff:wax_shields");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_stock_check_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_objective_index:13");
            HiveViewProductUiPresenter.ChooseGuidedUpgradeStockCheckForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_stock_check_plan:thorough");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_stock_check_duration_seconds:14");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_stock_check_assigned_builders:3");
            HiveViewProductUiPresenter.CompleteGuidedUpgradeStockCheckForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_stock_check_collect");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThan(0f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_stock_check_verify");
            HiveViewProductUiPresenter.RegisterGuidedUpgradeStockCheckCheckForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_stock_check_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_stock_check_done:true");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "wax_capacity_bonus_percent:2");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_objective_index:14");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] chapterFiveRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(chapterFiveRows, "tutorial_step:defense_welcome");
            AssertRow(chapterFiveRows, "chapter_id:chapter_5_first_defense");
            AssertRow(chapterFiveRows, "opening_act_milestones:4");
            AssertRow(chapterFiveRows, "chapter_reward_claim_count:4");
            AssertRow(chapterFiveRows, "chapter_reward_last_id:chapter_4");
            AssertRow(chapterFiveRows, "chapter_reward_last_honey:120");
            AssertRow(chapterFiveRows, "chapter_reward_last_wax:160");
        }

        [Test]
        public void GuidedWorkerApplicationToolkitReducesTheFirstWorkshopApplicationOnce()
        {
            HiveViewProductUiPresenter.BeginGuidedWorkerWorkshopCommissionForProof();
            HiveViewProductUiPresenter.ChooseGuidedWorkerWorkshopCommissionForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedWorkerWorkshopCommissionForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_commission_plan:application_toolkit");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_commission_commit_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_commission_duration_seconds:18");

            HiveViewProductUiPresenter.CompleteGuidedWorkerWorkshopCommissionForProof();
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_commission_check_count:3");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "workshop_calibration_wax_bonus:0");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "workshop_application_wax_discount:40");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "worker_workshop_commission:application_toolkit");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "workshop_application_wax_discount:40");

            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicRuntimeEffectsForProof(), "runtime_workshop_application_wax_discount:40");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("wax_workshop"), Is.True);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedUpgradeAuditForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedUpgradePlanForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedUpgradeForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedUpgradeCalibrationForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(120f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_batch_qualification_choice");
            HiveViewProductUiPresenter.ChooseGuidedUpgradeBatchQualificationForProof(true);
            HiveViewProductUiPresenter.ChooseGuidedUpgradeApplicationForProof(false);
            HiveViewProductUiPresenter.ChooseGuidedUpgradeApplicationForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_application_running");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_application_wax_cost:40");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_application_commit_count:1");
        }

        [Test]
        public void GuidedUpgradeChapterCanExpandWaxStorageInstead()
        {
            HiveViewProductUiPresenter.BeginGuidedUpgradeTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("wax_workshop"), Is.True);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedUpgradeAuditForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();

            HiveViewProductUiPresenter.ChooseGuidedUpgradePlanForProof(false);
            string[] runningRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(runningRows, "tutorial_step:upgrade_running");
            AssertRow(runningRows, "upgrade_plan:capacity");
            AssertRow(runningRows, "upgrade_plan_commit_count:1");
            AssertRow(runningRows, "upgrade_assigned_builders:4");
            AssertRow(runningRows, "wax_production_bonus_percent:0");
            AssertRow(runningRows, "wax_capacity_bonus_percent:0");

            HiveViewProductUiPresenter.CompleteGuidedUpgradeForProof();
            string[] resultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(resultRows, "tutorial_step:upgrade_result");
            AssertRow(resultRows, "upgrade_level:23");
            AssertRow(resultRows, "wax_production_bonus_percent:0");
            AssertRow(resultRows, "wax_capacity_bonus_percent:20");
            AssertRow(resultRows, "wax_production_per_hour:1227");
            AssertRow(resultRows, "wax_local_capacity:1416");
            AssertRow(resultRows, "upgrade_reward:+1 niveau, stockage de cire +20 %");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedUpgradeCalibrationForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(120f));
            string[] qualificationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(qualificationRows, "tutorial_step:upgrade_batch_qualification_choice");
            AssertRow(qualificationRows, "upgrade_batch_qualification_expected:load");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);

            HiveViewProductUiPresenter.ChooseGuidedUpgradeBatchQualificationForProof(true);
            string[] retryRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(retryRows, "tutorial_step:upgrade_batch_qualification_choice");
            AssertRow(retryRows, "upgrade_batch_qualification_attempt_count:1");
            AssertRow(retryRows, "upgrade_batch_qualification_error_count:1");
            AssertRow(retryRows, "upgrade_batch_qualification_success_count:0");
            AssertProofValuesEqual(
                qualificationRows,
                retryRows,
                "honey_balance",
                "wax_balance",
                "pollen_balance",
                "brood_stability",
                "wax_production_bonus_percent",
                "wax_capacity_bonus_percent",
                "upgrade_commit_count",
                "upgrade_plan_commit_count",
                "upgrade_calibration_commit_count",
                "upgrade_calibration_collected_wax",
                "upgrade_application_commit_count");

            HiveViewProductUiPresenter.ChooseGuidedUpgradeBatchQualificationForProof(false);
            string[] applicationReadyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(applicationReadyRows, "tutorial_step:upgrade_application_ready");
            AssertRow(applicationReadyRows, "upgrade_batch_qualification_attempt_count:2");
            AssertRow(applicationReadyRows, "upgrade_batch_qualification_error_count:1");
            AssertRow(applicationReadyRows, "upgrade_batch_qualification_success_count:1");
            AssertRow(applicationReadyRows, "upgrade_batch_qualification_recommendation:nursery");
        }

        [Test]
        public void GuidedUpgradeOperationsCanAdoptTheCadenceDoctrine()
        {
            HiveViewProductUiPresenter.BeginGuidedUpgradeOperationsForProof();

            for (int round = 1; round <= 2; round++)
            {
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
                AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_supply_choice");
                Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
                Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);
                HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsSupplyForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsSupplyForProof();
                Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThanOrEqualTo(90f));
                AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_deployment_choice");
                Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
                Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);
                HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsDeploymentForProof(round == 2);
                HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsDeploymentForProof();
                AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_check");
                Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(false), Is.True);
                Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(true), Is.True);
                HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(0);
                HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(1);
                HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsCheckForProof(2);
                AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_round_result");
            }

            string[] deliveredRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(deliveredRows, "upgrade_operations_completed_rounds:2");
            AssertRow(deliveredRows, "upgrade_operations_supply_commit_count:2");
            AssertRow(deliveredRows, "upgrade_operations_deployment_commit_count:2");
            AssertRow(deliveredRows, "upgrade_operations_collected_wax:180");
            AssertRow(deliveredRows, "upgrade_operations_deployment_wax_cost:160");
            AssertRow(deliveredRows, "upgrade_operations_total_check_count:6");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_certification_choice");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);
            HiveViewProductUiPresenter.ChooseGuidedUpgradeCertificationForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedUpgradeCertificationForProof();
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(true), Is.True);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeCertificationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeCertificationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeCertificationCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "upgrade_certification_plan:load");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_doctrine_choice");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedChoiceButtonsAcceptInputForProof(true), Is.True);
            HiveViewProductUiPresenter.ChooseGuidedUpgradeOperationsDoctrineForProof(false);
            string[] doctrineRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(doctrineRows, "tutorial_step:upgrade_operations_doctrine_running");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_plan:cadence");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_duration_seconds:14");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_assigned_builders:4");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_honey_cost:80");
            AssertRow(doctrineRows, "upgrade_operations_doctrine_pollen_cost:30");

            HiveViewProductUiPresenter.CompleteGuidedUpgradeOperationsDoctrineForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:upgrade_operations_doctrine_check");
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedUpgradeOperationsDoctrineCheckForProof(2);
            string[] completedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(completedRows, "tutorial_step:upgrade_operations_completed");
            AssertRow(completedRows, "upgrade_operations_doctrine_check_count:3");
            AssertRow(completedRows, "upgrade_operations_doctrine_capacity_gain_percent:5");
            AssertRow(completedRows, "wax_capacity_bonus_percent:10");
            AssertRow(completedRows, "reward_claimed:false");
        }

        [Test]
        public void GuidedDefenseChapterMobilizesGuardiansAndResolvesThreat()
        {
            HiveViewProductUiPresenter.BeginGuidedDefenseTutorialForProof();
            string[] welcomeRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(welcomeRows, "tutorial_step:defense_welcome");
            AssertRow(welcomeRows, "defense_threat:fausse_teigne_de_cire");
            AssertRow(welcomeRows, "defense_threat_integrity:100");
            AssertRow(welcomeRows, "guardians_available:8");
            AssertRow(welcomeRows, "hive_security:72");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_inspect_guard_post");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_hotspot:guard_post");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "camera_controlled:true");

            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("wax_workshop"), Is.False);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "blocked_clicks:1");
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("guard_post"), Is.True);
            string[] readyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(readyRows, "tutorial_step:defense_ready");
            AssertRow(readyRows, "defense_scout_duration_seconds:6");
            AssertRow(readyRows, "defense_power_purchase:false");
            AssertRow(readyRows, "defense_server_authoritative:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] scoutingRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(scoutingRows, "tutorial_step:defense_scouting");
            AssertRow(scoutingRows, "defense_scouting_pending:true");
            AssertRow(scoutingRows, "defense_scouts_assigned:2");
            AssertRow(scoutingRows, "defense_scout_commit_count:1");
            AssertRow(scoutingRows, "defense_threat_integrity:100");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_scout_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedDefenseScoutingForProof();
            string[] strategyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(strategyRows, "tutorial_step:defense_strategy_ready");
            AssertRow(strategyRows, "defense_scouting_pending:false");
            AssertRow(strategyRows, "defense_threat_integrity:100");
            AssertRow(strategyRows, "defense_choice:interception_5_or_barrier_4_120_wax");

            HiveViewProductUiPresenter.ChooseGuidedDefensePlanForProof(false);
            string[] runningRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(runningRows, "tutorial_step:defense_mobilizing");
            AssertRow(runningRows, "defense_pending:true");
            AssertRow(runningRows, "defense_commit_count:1");
            AssertRow(runningRows, "defense_plan:interception");
            AssertRow(runningRows, "defense_plan_commit_count:1");
            AssertRow(runningRows, "defense_duration_seconds:8");
            AssertRow(runningRows, "defense_guardians_mobilized:5");
            AssertRow(runningRows, "guardians_available:8");
            AssertRow(runningRows, "defense_security_gain:10");
            AssertRow(runningRows, "defense_wax_cost:0");
            AssertRow(runningRows, "wax_balance:72300");

            HiveViewProductUiPresenter.ChooseGuidedDefensePlanForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_commit_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_plan_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedDefenseForProof();
            string[] resolvedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(resolvedRows, "tutorial_step:defense_resolved");
            AssertRow(resolvedRows, "defense_pending:false");
            AssertRow(resolvedRows, "defense_threat_integrity:0");
            AssertRow(resolvedRows, "hive_security:82");
            AssertRow(resolvedRows, "guardians_available:8");
            AssertRow(resolvedRows, "hive_background_image_modified:false");
            AssertRow(resolvedRows, "world_map_terrain_modified:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] debriefRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(debriefRows, "tutorial_step:defense_debrief_check");
            AssertRow(debriefRows, "defense_debrief_check_count:0");
            AssertRow(debriefRows, "defense_debrief_recommendation:watch");
            AssertRow(debriefRows, "hive_security:82");
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.CurrentGuidedCheckButtonsAcceptInputForProof(true), Is.True);
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(0);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_debrief_check_count:1");
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(1);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_debrief_check");
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(2);
            string[] recoveryReadyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(recoveryReadyRows, "tutorial_step:defense_recovery_ready");
            AssertRow(recoveryReadyRows, "defense_debrief_check_count:3");
            AssertRow(recoveryReadyRows, "defense_debrief_checks_mask:7");
            AssertRow(recoveryReadyRows, "defense_debrief_last_check:supply_line");
            AssertRow(recoveryReadyRows, "defense_objective_index:5");
            AssertRow(recoveryReadyRows, "defense_objective_target:14");

            HiveViewProductUiPresenter.ChooseGuidedDefenseRecoveryForProof(true);
            string[] recoveryRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(recoveryRows, "tutorial_step:defense_recovery_running");
            AssertRow(recoveryRows, "defense_recovery_pending:true");
            AssertRow(recoveryRows, "defense_recovery_plan:cleanup");
            AssertRow(recoveryRows, "defense_recovery_duration_seconds:9");
            AssertRow(recoveryRows, "defense_recovery_assigned_bees:3");
            AssertRow(recoveryRows, "defense_recovery_commit_count:1");
            AssertRow(recoveryRows, "defense_recovery_security_gain:2");
            AssertRow(recoveryRows, "defense_recovery_wax_recovered:60");

            HiveViewProductUiPresenter.ChooseGuidedDefenseRecoveryForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_recovery_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedDefenseRecoveryForProof();
            string[] recoveryResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(recoveryResultRows, "tutorial_step:defense_recovery_result");
            AssertRow(recoveryResultRows, "defense_recovery_pending:false");
            AssertRow(recoveryResultRows, "hive_security:84");
            AssertRow(recoveryResultRows, "wax_balance:72360");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] doctrineReadyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(doctrineReadyRows, "tutorial_step:defense_doctrine_ready");
            AssertRow(doctrineReadyRows, "defense_objective_index:6");
            AssertRow(doctrineReadyRows, "defense_objective_target:14");

            HiveViewProductUiPresenter.ChooseGuidedDefenseDoctrineForProof(false);
            string[] doctrineRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(doctrineRows, "tutorial_step:defense_doctrine_running");
            AssertRow(doctrineRows, "defense_doctrine_pending:true");
            AssertRow(doctrineRows, "defense_doctrine_plan:patrol");
            AssertRow(doctrineRows, "defense_doctrine_duration_seconds:10");
            AssertRow(doctrineRows, "defense_doctrine_assigned_bees:3");
            AssertRow(doctrineRows, "defense_doctrine_commit_count:1");
            AssertRow(doctrineRows, "defense_doctrine_security_gain:3");
            AssertRow(doctrineRows, "defense_doctrine_wax_cost:0");

            HiveViewProductUiPresenter.ChooseGuidedDefenseDoctrineForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_doctrine_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedDefenseDoctrineForProof();
            string[] doctrineResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(doctrineResultRows, "tutorial_step:defense_doctrine_result");
            AssertRow(doctrineResultRows, "defense_doctrine_pending:false");
            AssertRow(doctrineResultRows, "hive_security:87");
            AssertRow(doctrineResultRows, "defense_objective_index:6");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:readiness_welcome");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            CompleteGuidedReadinessRound(false, true, false);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            CompleteGuidedReadinessRound(true, false, true);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:readiness_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_objective_index:9");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_expedition_mandate_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_reward_claim_count:4");
            HiveViewProductUiPresenter.ChooseGuidedDefenseExpeditionMandateForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_mandate:scout_corridor");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_mandate_duration_seconds:14");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_mandate_assigned_bees:3");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_mandate_commit_count:1");
            HiveViewProductUiPresenter.ChooseGuidedDefenseExpeditionMandateForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_mandate_commit_count:1");
            HiveViewProductUiPresenter.CompleteGuidedDefenseExpeditionMandateForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_expedition_mandate_check");
            HiveViewProductUiPresenter.RegisterGuidedDefenseExpeditionMandateCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedDefenseExpeditionMandateCheckForProof(0);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_mandate_check_count:1");
            HiveViewProductUiPresenter.RegisterGuidedDefenseExpeditionMandateCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedDefenseExpeditionMandateCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_expedition_mandate_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_pollen_bonus:6");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "defense_expedition_mandate:scout_corridor");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "profile_version:12");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_world_briefing_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_objective_index:12");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_objective_target:14");

            HiveViewProductUiPresenter.ChooseGuidedDefenseWorldBriefingForProof(false);
            string[] briefingRunningRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(briefingRunningRows, "tutorial_step:defense_world_briefing_running");
            AssertRow(briefingRunningRows, "world_briefing_plan:sun_beacon");
            AssertRow(briefingRunningRows, "world_briefing_duration_seconds:15");
            AssertRow(briefingRunningRows, "world_briefing_assigned_bees:3");
            AssertRow(briefingRunningRows, "world_briefing_commit_count:1");
            HiveViewProductUiPresenter.ChooseGuidedDefenseWorldBriefingForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedDefenseWorldBriefingForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_world_briefing_simulation");
            HiveViewProductUiPresenter.ResolveGuidedDefenseWorldBriefingSimulationForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_mistake_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_simulation_stage:0");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_last_decision:force_route");
            HiveViewProductUiPresenter.ResolveGuidedDefenseWorldBriefingSimulationForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_success_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_simulation_stage:1");
            HiveViewProductUiPresenter.ResolveGuidedDefenseWorldBriefingSimulationForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_mistake_count:2");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_last_decision:scatter_team");
            HiveViewProductUiPresenter.ResolveGuidedDefenseWorldBriefingSimulationForProof(false);
            string[] briefingResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(briefingResultRows, "tutorial_step:defense_world_briefing_result");
            AssertRow(briefingResultRows, "world_briefing_success_count:2");
            AssertRow(briefingResultRows, "world_navigation_hint_level:1");
            AssertRow(briefingResultRows, "world_transition_coordinate_hint:C32_32");
            AssertRow(briefingResultRows, "world_briefing_paid_advantage:false");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "defense_world_briefing:sun_beacon");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "world_navigation_hint_level:1");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_vigilance_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_objective_index:13");
            HiveViewProductUiPresenter.ChooseGuidedDefenseVigilanceForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_vigilance_plan:thorough");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_vigilance_duration_seconds:14");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_vigilance_assigned_bees:3");
            HiveViewProductUiPresenter.CompleteGuidedDefenseVigilanceForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_vigilance_collect");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_vigilance_check");
            HiveViewProductUiPresenter.RegisterGuidedDefenseVigilanceCheckForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_vigilance_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_vigilance_check_done:true");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_objective_index:14");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] worldRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(worldRows, "tutorial_step:world_welcome");
            AssertRow(worldRows, "chapter_id:chapter_6_first_world_transition");
            AssertRow(worldRows, "opening_act_milestones:5");
            AssertRow(worldRows, "chapter_reward_claim_count:5");
            AssertRow(worldRows, "chapter_reward_last_id:chapter_5");
            AssertRow(worldRows, "chapter_reward_last_honey:200");
            AssertRow(worldRows, "chapter_reward_last_wax:80");
            AssertRow(worldRows, "opening_act_reward_honey:1050");
            AssertRow(worldRows, "opening_act_reward_wax:280");
            AssertRow(worldRows, "opening_act_reward_pollen:120");
        }

        [Test]
        public void GuidedDefenseChapterCanBuildAWaxBarrierInstead()
        {
            HiveViewProductUiPresenter.BeginGuidedDefenseTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("guard_post"), Is.True);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedDefenseScoutingForProof();

            HiveViewProductUiPresenter.ChooseGuidedDefensePlanForProof(true);
            string[] runningRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(runningRows, "tutorial_step:defense_mobilizing");
            AssertRow(runningRows, "defense_plan:barrier");
            AssertRow(runningRows, "defense_plan_commit_count:1");
            AssertRow(runningRows, "defense_commit_count:1");
            AssertRow(runningRows, "defense_duration_seconds:12");
            AssertRow(runningRows, "defense_guardians_mobilized:4");
            AssertRow(runningRows, "defense_security_gain:14");
            AssertRow(runningRows, "defense_wax_cost:60");
            AssertRow(runningRows, "wax_balance:72240");

            HiveViewProductUiPresenter.ChooseGuidedDefensePlanForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_plan_commit_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_commit_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "wax_balance:72240");

            HiveViewProductUiPresenter.CompleteGuidedDefenseForProof();
            string[] resolvedRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(resolvedRows, "tutorial_step:defense_resolved");
            AssertRow(resolvedRows, "defense_pending:false");
            AssertRow(resolvedRows, "defense_threat_integrity:0");
            AssertRow(resolvedRows, "hive_security:86");
            AssertRow(resolvedRows, "guardians_available:8");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_debrief_check");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_debrief_recommendation:cleanup");
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_recovery_ready");
            HiveViewProductUiPresenter.ChooseGuidedDefenseRecoveryForProof(false);
            string[] recoveryRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(recoveryRows, "tutorial_step:defense_recovery_running");
            AssertRow(recoveryRows, "defense_recovery_plan:watch");
            AssertRow(recoveryRows, "defense_recovery_duration_seconds:12");
            AssertRow(recoveryRows, "defense_recovery_assigned_bees:2");
            AssertRow(recoveryRows, "defense_recovery_security_gain:5");
            AssertRow(recoveryRows, "defense_recovery_wax_recovered:0");
            AssertRow(recoveryRows, "wax_balance:72240");

            HiveViewProductUiPresenter.CompleteGuidedDefenseRecoveryForProof();
            string[] recoveryResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(recoveryResultRows, "tutorial_step:defense_recovery_result");
            AssertRow(recoveryResultRows, "hive_security:91");
            AssertRow(recoveryResultRows, "wax_balance:72240");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_doctrine_ready");
            HiveViewProductUiPresenter.ChooseGuidedDefenseDoctrineForProof(true);
            string[] doctrineRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(doctrineRows, "tutorial_step:defense_doctrine_running");
            AssertRow(doctrineRows, "defense_doctrine_plan:propolis");
            AssertRow(doctrineRows, "defense_doctrine_duration_seconds:12");
            AssertRow(doctrineRows, "defense_doctrine_assigned_bees:2");
            AssertRow(doctrineRows, "defense_doctrine_commit_count:1");
            AssertRow(doctrineRows, "defense_doctrine_security_gain:6");
            AssertRow(doctrineRows, "defense_doctrine_wax_cost:70");
            AssertRow(doctrineRows, "wax_balance:72170");

            HiveViewProductUiPresenter.ChooseGuidedDefenseDoctrineForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_doctrine_commit_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "wax_balance:72170");

            HiveViewProductUiPresenter.CompleteGuidedDefenseDoctrineForProof();
            string[] doctrineResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(doctrineResultRows, "tutorial_step:defense_doctrine_result");
            AssertRow(doctrineResultRows, "hive_security:97");
            AssertRow(doctrineResultRows, "wax_balance:72170");
        }

        [Test]
        public void GuidedReadinessLoopRequiresTwoManualProductionCycles()
        {
            HiveViewProductUiPresenter.BeginGuidedReadinessLoopForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:readiness_welcome");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] firstChoiceRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstChoiceRows, "tutorial_step:readiness_production_choice");
            AssertRow(firstChoiceRows, "readiness_round:1");
            AssertRow(firstChoiceRows, "readiness_round_target:2");
            AssertRow(firstChoiceRows, "readiness_manual_collections_required:4");
            AssertRow(firstChoiceRows, "readiness_paid_advantage:false");
            AssertRow(firstChoiceRows, "defense_objective_index:7");
            AssertRow(firstChoiceRows, "defense_objective_target:14");

            HiveViewProductUiPresenter.ChooseGuidedReadinessProductionForProof(false);
            string[] productionRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(productionRows, "tutorial_step:readiness_production_running");
            AssertRow(productionRows, "readiness_production_plan:surge");
            AssertRow(productionRows, "readiness_production_duration_seconds:16");
            AssertRow(productionRows, "readiness_production_assigned_bees:6");
            AssertRow(productionRows, "readiness_production_commit_count:1");
            HiveViewProductUiPresenter.ChooseGuidedReadinessProductionForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_production_commit_count:1");

            HiveViewProductUiPresenter.CompleteGuidedReadinessProductionForProof();
            string[] honeyRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(honeyRows, "tutorial_step:readiness_collect_honey");
            AssertRow(honeyRows, "readiness_pending_honey:300");
            AssertRow(honeyRows, "readiness_pending_pollen:100");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:readiness_collect_pollen");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells"), Is.GreaterThan(0f));
            string[] populationChoiceRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(populationChoiceRows, "tutorial_step:readiness_population_choice");
            AssertRow(populationChoiceRows, "readiness_collected_honey:300");
            AssertRow(populationChoiceRows, "readiness_collected_pollen:100");
            int honeyBeforePopulation = ProofInt(populationChoiceRows, "honey_balance");
            int pollenBeforePopulation = ProofInt(populationChoiceRows, "pollen_balance");
            int workersBeforePopulation = ProofInt(populationChoiceRows, "worker_count");

            HiveViewProductUiPresenter.ChooseGuidedReadinessPopulationForProof(true);
            string[] populationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(populationRows, "tutorial_step:readiness_population_running");
            AssertRow(populationRows, "readiness_population_plan:nurses");
            AssertRow(populationRows, "readiness_population_duration_seconds:18");
            AssertRow(populationRows, "readiness_population_assigned_bees:2");
            AssertRow(populationRows, "readiness_population_commit_count:1");
            Assert.That(ProofInt(populationRows, "honey_balance"), Is.EqualTo(honeyBeforePopulation - 180));
            Assert.That(ProofInt(populationRows, "pollen_balance"), Is.EqualTo(pollenBeforePopulation - 90));
            HiveViewProductUiPresenter.ChooseGuidedReadinessPopulationForProof(true);
            string[] repeatedPopulationRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(repeatedPopulationRows, "readiness_population_commit_count:1");
            Assert.That(ProofInt(repeatedPopulationRows, "honey_balance"), Is.EqualTo(honeyBeforePopulation - 180));
            Assert.That(ProofInt(repeatedPopulationRows, "pollen_balance"), Is.EqualTo(pollenBeforePopulation - 90));

            HiveViewProductUiPresenter.CompleteGuidedReadinessPopulationForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:readiness_defense_choice");
            Assert.That(ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "worker_count"), Is.EqualTo(workersBeforePopulation + 1));
            HiveViewProductUiPresenter.ChooseGuidedReadinessDefenseForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_defense_commit_count:1");
            HiveViewProductUiPresenter.ChooseGuidedReadinessDefenseForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_defense_commit_count:1");
            HiveViewProductUiPresenter.CompleteGuidedReadinessDefenseForProof();
            string[] firstCheckRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(firstCheckRows, "tutorial_step:readiness_check");
            AssertRow(firstCheckRows, "readiness_completed_rounds:0");
            AssertRow(firstCheckRows, "readiness_check_count:0");
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(0);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_check_count:1");
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_completed_rounds:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_total_check_count:3");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_check_security_gain:2");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_round:2");
            HiveViewProductUiPresenter.ChooseGuidedReadinessProductionForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_production_duration_seconds:24");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_production_commit_count:2");
            HiveViewProductUiPresenter.CompleteGuidedReadinessProductionForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells"), Is.GreaterThan(0f));
            int guardiansBeforeFormation = ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "guardians_available");
            HiveViewProductUiPresenter.ChooseGuidedReadinessPopulationForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_population_commit_count:2");
            HiveViewProductUiPresenter.CompleteGuidedReadinessPopulationForProof();
            Assert.That(ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "guardians_available"), Is.EqualTo(guardiansBeforeFormation + 2));
            int waxBeforeSeal = ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "wax_balance");
            HiveViewProductUiPresenter.ChooseGuidedReadinessDefenseForProof(true);
            Assert.That(ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "wax_balance"), Is.EqualTo(waxBeforeSeal - 60));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_defense_commit_count:2");
            HiveViewProductUiPresenter.ChooseGuidedReadinessDefenseForProof(true);
            Assert.That(ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "wax_balance"), Is.EqualTo(waxBeforeSeal - 60));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_defense_commit_count:2");
            HiveViewProductUiPresenter.CompleteGuidedReadinessDefenseForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:readiness_check");
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_completed_rounds:2");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_total_check_count:6");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_collected_honey:520");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "readiness_collected_pollen:260");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:readiness_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_objective_index:9");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_expedition_mandate_choice");
            int securityBeforeEscort = ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "hive_security");
            HiveViewProductUiPresenter.ChooseGuidedDefenseExpeditionMandateForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_mandate:guardian_escort");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_mandate_duration_seconds:16");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_mandate_assigned_bees:2");
            HiveViewProductUiPresenter.CompleteGuidedDefenseExpeditionMandateForProof();
            HiveViewProductUiPresenter.RegisterGuidedDefenseExpeditionMandateCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedDefenseExpeditionMandateCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedDefenseExpeditionMandateCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "expedition_pollen_bonus:4");
            Assert.That(ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "hive_security"), Is.EqualTo(Math.Min(100, securityBeforeEscort + 2)));
            int securityAfterEscort = ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "hive_security");
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            Assert.That(ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "hive_security"), Is.EqualTo(securityAfterEscort));
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "defense_expedition_mandate:guardian_escort");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_world_briefing_choice");
            int securityBeforeGuardedReturn = ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "hive_security");
            HiveViewProductUiPresenter.ChooseGuidedDefenseWorldBriefingForProof(true);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_plan:guarded_return");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_duration_seconds:18");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_briefing_assigned_bees:2");
            HiveViewProductUiPresenter.CompleteGuidedDefenseWorldBriefingForProof();
            HiveViewProductUiPresenter.ResolveGuidedDefenseWorldBriefingSimulationForProof(true);
            HiveViewProductUiPresenter.ResolveGuidedDefenseWorldBriefingSimulationForProof(false);
            string[] guardedReturnRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(guardedReturnRows, "tutorial_step:defense_world_briefing_result");
            AssertRow(guardedReturnRows, "world_transition_guarded_return:true");
            AssertRow(guardedReturnRows, "world_navigation_hint_level:0");
            Assert.That(ProofInt(guardedReturnRows, "hive_security"), Is.EqualTo(Math.Min(100, securityBeforeGuardedReturn + 3)));
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "defense_world_briefing:guarded_return");
            AssertRow(HiveViewProductUiPresenter.LocalPreviewStrategicProfileForProof(), "world_briefing_security_bonus:3");
            int securityAfterGuardedReturn = ProofInt(guardedReturnRows, "hive_security");
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            Assert.That(ProofInt(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "hive_security"), Is.EqualTo(securityAfterGuardedReturn));
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_vigilance_choice");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_objective_index:13");
            HiveViewProductUiPresenter.ChooseGuidedDefenseVigilanceForProof(false);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_vigilance_plan:quick");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_vigilance_duration_seconds:10");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_vigilance_assigned_bees:2");
            HiveViewProductUiPresenter.CompleteGuidedDefenseVigilanceForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_vigilance_collect");
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_vigilance_check");
            HiveViewProductUiPresenter.RegisterGuidedDefenseVigilanceCheckForProof();
            string[] vigilanceResultRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(vigilanceResultRows, "tutorial_step:defense_vigilance_result");
            Assert.That(ProofInt(vigilanceResultRows, "hive_security"), Is.EqualTo(Math.Min(100, securityAfterGuardedReturn + 1)));
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:defense_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "defense_objective_index:14");
        }

        [Test]
        public void GuidedOpeningActTracksSeparatelyClaimedChapterRewards()
        {
            HiveViewProductUiPresenter.PreviewGuidedOpeningActQuestForProof(3);
            string[] rows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();

            AssertRow(rows, "opening_act_milestones:3");
            AssertRow(rows, "opening_act_target:5");
            AssertRow(rows, "chapter_reward_claim_count:3");
            AssertRow(rows, "opening_act_reward_honey:730");
            AssertRow(rows, "opening_act_reward_wax:40");
            AssertRow(rows, "opening_act_reward_pollen:120");
            AssertRow(rows, "chapter_rewards_separately_claimed:true");
            AssertRow(rows, "chapter_reward_storage_protected:true");
            AssertRow(rows, "quest_menu_functional:true");
            AssertRow(rows, "quest_act_objectives:5");
            AssertRow(rows, "quest_menu_open:true");

            HiveViewProductUiPresenter.CloseGuidedOpeningActQuestForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "quest_menu_open:false");
        }

        [Test]
        public void GuidedWorldChapterUsesTheCanonical50x50MapAndReturnsToHive()
        {
            var profile = new LocalPreviewStrategicProfile { profileId = "world-briefing-proof", defenseWorldBriefing = "sun_beacon" };
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new MemoryStrategicProfileStore { Json = JsonUtility.ToJson(profile) });
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            HiveViewProductUiPresenter.BeginGuidedWorldTransitionForProof();
            string[] welcomeRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(welcomeRows, "tutorial_step:world_welcome");
            AssertRow(welcomeRows, "chapter_id:chapter_6_first_world_transition");
            AssertRow(welcomeRows, "world_transition_grid:50x50");
            AssertRow(welcomeRows, "world_transition_contextual_button:single,Carte_then_Ruche");
            AssertRow(welcomeRows, "world_transition_coordinate_hint:C32_32");
            AssertRow(welcomeRows, "world_transition_guarded_return:false");
            AssertRow(welcomeRows, "world_map_terrain_modified:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] openRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(openRows, "tutorial_step:world_open_map");
            AssertRow(openRows, "target_control:contextual_surface_switch");
            AssertRow(openRows, "underlying_hive_chrome_input_blocked:false");
            Assert.That(HiveViewProductUiPresenter.GuidedTutorialBlocksUnderlyingHiveChromeInputForProof(), Is.False);

            HiveViewProductUiPresenter.DepartToGuidedWorldMapForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:world_map_arrival");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] locateRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(locateRows, "tutorial_step:world_locate_hive");
            AssertRow(locateRows, "target_world_hive:hive_player_test");

            Assert.That(HiveViewProductUiPresenter.SelectGuidedWorldMapHiveForRuntime("hive_ally_mid"), Is.False);
            Assert.That(HiveViewProductUiPresenter.SelectGuidedWorldMapHiveForRuntime("hive_player_test"), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:world_hive_located");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            string[] returnRows = HiveViewProductUiPresenter.GuidedCollectionTutorialForProof();
            AssertRow(returnRows, "tutorial_step:world_return_hive");
            AssertRow(returnRows, "target_control:contextual_surface_switch");
            Assert.That(HiveViewProductUiPresenter.TryBeginGuidedWorldMapReturnForRuntime(), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:world_returning_to_hive");
            Assert.That(HiveViewProductUiPresenter.ResumeGuidedWorldTransitionAfterHiveLoad(), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:world_returned");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_welcome");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_id:chapter_7_first_foraging_flight");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "hive_background_image_modified:false");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_map_terrain_modified:false");
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new MemoryStrategicProfileStore());
        }

        [Test]
        public void GuidedForagingChapterDispatchesAndRequiresManualClaim()
        {
            var profile = new LocalPreviewStrategicProfile { profileId = "foraging-mandate-proof", defenseExpeditionMandate = "scout_corridor" };
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new MemoryStrategicProfileStore { Json = JsonUtility.ToJson(profile) });
            HiveViewProductUiPresenter.SimulateLocalPreviewStrategicProfileRestartForProof();
            HiveViewProductUiPresenter.BeginGuidedForagingTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_welcome");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "chapter_id:chapter_7_first_foraging_flight");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_manual_claim:true");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_open_map");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_control:contextual_surface_switch");

            HiveViewProductUiPresenter.DepartToGuidedWorldMapForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_map_arrival");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_select_resource");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "target_world_resource:res_pollen_core");

            Assert.That(HiveViewProductUiPresenter.SelectGuidedWorldMapResourceForRuntime("res_nectar_core"), Is.False);
            Assert.That(HiveViewProductUiPresenter.SelectGuidedWorldMapResourceForRuntime("res_pollen_core"), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_resource_selected");
            Assert.That(HiveViewProductUiPresenter.RegisterGuidedForagingFlightStartedForRuntime("hive_player_test", "res_pollen_core"), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_flying");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_dispatch_count:1");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_reward_claimed:false");

            Assert.That(HiveViewProductUiPresenter.CompleteGuidedForagingFlightForRuntime("res_pollen_core", 20f), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_flight_complete");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_pending_pollen:26");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_return_hive");
            Assert.That(HiveViewProductUiPresenter.TryBeginGuidedWorldMapReturnForRuntime(), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_returning_to_hive");
            Assert.That(HiveViewProductUiPresenter.ResumeGuidedWorldTransitionAfterHiveLoad(), Is.True);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_claim");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_reward_claimed:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:forage_completed");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_pending_pollen:0");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_claimed_pollen:26");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_reward_claimed:true");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_paid_advantage:false");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "foraging_server_authoritative:false");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "world_map_terrain_modified:false");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:inactive");
            HiveViewProductUiPresenter.UseLocalPreviewStrategicProfileStoreForProof(new MemoryStrategicProfileStore());
        }

        [Test]
        public void AmbientTrafficUsesPremiumBeeWithinMobileBudgets()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            string[] rows = HiveViewProductUiPresenter.LivingHiveAmbientTrafficForProof();

            AssertRow(rows, "ambient_traffic_enabled:true");
            AssertRow(rows, "ambient_landscape_count:8");
            AssertRow(rows, "ambient_portrait_count:5");
            AssertRow(rows, "landscape_budget:8");
            AssertRow(rows, "portrait_budget:5");
            AssertRow(rows, "future_locked_server_traffic:false");
            AssertRow(rows, "producer_fill_drives_cadence:true");
            AssertRow(rows, "tutorial_suppresses_ambient_traffic:true");
            AssertRow(rows, "guided_task_activity_visible:true");
            AssertRow(rows, "collection_carrier_bee:true");
            AssertRow(rows, "worker_sprite:worker-bee-premium-v2");
            AssertRow(rows, "hive_background_image_modified:false");
            AssertRow(rows, "world_map_terrain_modified:false");
        }

        [Test]
        public void NurseryVitalityLayerUsesAuthoritativeSnapshotBoundariesAndMobileSafeRects()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.SetBroodVitalityForProof(32f, 72f);
            string[] careRows = HiveViewProductUiPresenter.LivingHiveBroodVitalityForProof();
            AssertRow(careRows, "brood_vitality_layer_enabled:true");
            AssertRow(careRows, "tier:care_required");
            AssertRow(careRows, "phone_cache:last_acknowledged_snapshot_only");
            AssertRow(careRows, "offline_mutations:false");
            AssertRow(careRows, "server_authority:nutrition|stability|revision|updated_at_utc|active_operation");
            AssertRow(careRows, "current_authority:local_preview_non_official");
            AssertRow(careRows, "hive_background_image_modified:false");
            AssertRow(careRows, "world_map_terrain_modified:false");

            HiveViewProductUiPresenter.SetBroodVitalityForProof(62f, 64f);
            AssertRow(HiveViewProductUiPresenter.LivingHiveBroodVitalityForProof(), "tier:watch");
            HiveViewProductUiPresenter.SetBroodVitalityForProof(72f, 88f);
            AssertRow(HiveViewProductUiPresenter.LivingHiveBroodVitalityForProof(), "tier:stable");
            HiveViewProductUiPresenter.SetBroodVitalityForProof(92f, 90f);
            AssertRow(HiveViewProductUiPresenter.LivingHiveBroodVitalityForProof(), "tier:thriving");

            Rect portrait = HiveViewProductUiPresenter.NurseryVitalityCardRectForProof(true, 390f, 844f, 195f, 287f);
            Assert.That(portrait.width, Is.EqualTo(210f).Within(0.01f));
            Assert.That(portrait.x, Is.GreaterThanOrEqualTo(8f));
            Assert.That(portrait.xMax, Is.LessThanOrEqualTo(382f));
            Assert.That(portrait.y, Is.GreaterThanOrEqualTo(126f));
            Assert.That(portrait.yMax, Is.LessThanOrEqualTo(574f));

            Rect landscape = HiveViewProductUiPresenter.NurseryVitalityCardRectForProof(false, 1600f, 900f, 800f, 279f);
            Assert.That(landscape.width, Is.EqualTo(232f).Within(0.01f));
            Assert.That(landscape.x, Is.GreaterThanOrEqualTo(156f));
            Assert.That(landscape.xMax, Is.LessThanOrEqualTo(1576f));
            Assert.That(landscape.y, Is.GreaterThanOrEqualTo(84f));
            Assert.That(landscape.yMax, Is.LessThanOrEqualTo(760f));

            HiveViewProductUiPresenter.BeginGuidedBroodIncubationForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(true);
            HiveViewProductUiPresenter.FreezeGuidedBroodIncubationInspectionForProof(0.42f);
            string[] activeRows = HiveViewProductUiPresenter.LivingHiveBroodVitalityForProof();
            AssertRow(activeRows, "operation_active:true");
            AssertRow(activeRows, "operation_id:incubation_inspection");
            AssertRow(activeRows, "operation_remaining_seconds:11");
            AssertRow(activeRows, "reduced_motion_static_state:true");

            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
        }

        [Test]
        public void BroodFeedingConsumesHoneyAndStopsAtFullNutrition()
        {
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.SetBroodCareForProof(78f, 1000f);

            float firstGain = HiveViewProductUiPresenter.FeedBroodForProof();
            Assert.That(firstGain, Is.EqualTo(22f).Within(0.01f));
            string[] fullRows = HiveViewProductUiPresenter.BroodCareForProof();
            AssertRow(fullRows, "brood_care_enabled:true");
            AssertRow(fullRows, "brood_care_manual:true");
            AssertRow(fullRows, "automatic_brood_feeding:false");
            AssertRow(fullRows, "nutrition:100");
            AssertRow(fullRows, "honey:700");
            AssertRow(fullRows, "feed_count:1");
            AssertRow(fullRows, "nursery_bee_feedback:true");
            AssertRow(fullRows, "population_granted_immediately:false");
            AssertRow(fullRows, "paid_power_shortcut:false");

            float blockedGain = HiveViewProductUiPresenter.FeedBroodForProof();
            Assert.That(blockedGain, Is.Zero);
            string[] blockedRows = HiveViewProductUiPresenter.BroodCareForProof();
            AssertRow(blockedRows, "nutrition:100");
            AssertRow(blockedRows, "honey:700");
            AssertRow(blockedRows, "feed_count:1");
            AssertRow(blockedRows, "hive_background_image_modified:false");
            AssertRow(blockedRows, "world_map_terrain_modified:false");
        }

        [Test]
        public void NurseryFormsOneWorkerOnlyAfterTheTrainingTimer()
        {
            HiveViewProductUiPresenter.SetNurseryWorkerFormationForProof(35f, 1000f, 1000f, 30);

            Assert.That(HiveViewProductUiPresenter.StartNurseryWorkerFormationForProof(), Is.False);
            string[] blockedRows = HiveViewProductUiPresenter.NurseryWorkerFormationForProof();
            AssertRow(blockedRows, "worker_count:30");
            AssertRow(blockedRows, "nutrition:35");
            AssertRow(blockedRows, "honey:1000");
            AssertRow(blockedRows, "pollen:1000");
            AssertRow(blockedRows, "training_commit_count:0");

            HiveViewProductUiPresenter.SetNurseryWorkerFormationForProof(100f, 1000f, 1000f, 30);
            Assert.That(HiveViewProductUiPresenter.StartNurseryWorkerFormationForProof(), Is.True);
            string[] startedRows = HiveViewProductUiPresenter.NurseryWorkerFormationForProof();
            AssertRow(startedRows, "training_pending:true");
            AssertRow(startedRows, "worker_count:30");
            AssertRow(startedRows, "nutrition:100");
            AssertRow(startedRows, "honey:580");
            AssertRow(startedRows, "pollen:820");
            AssertRow(startedRows, "training_commit_count:1");
            AssertRow(startedRows, "worker_granted_before_completion:false");

            Assert.That(HiveViewProductUiPresenter.StartNurseryWorkerFormationForProof(), Is.False);
            string[] repeatedRows = HiveViewProductUiPresenter.NurseryWorkerFormationForProof();
            AssertRow(repeatedRows, "honey:580");
            AssertRow(repeatedRows, "pollen:820");
            AssertRow(repeatedRows, "training_commit_count:1");
            AssertRow(repeatedRows, "repeated_start_blocked_count:1");

            HiveViewProductUiPresenter.CompleteNurseryWorkerFormationForProof();
            string[] completedRows = HiveViewProductUiPresenter.NurseryWorkerFormationForProof();
            AssertRow(completedRows, "training_pending:false");
            AssertRow(completedRows, "worker_count:31");
            AssertRow(completedRows, "nutrition:60");
            AssertRow(completedRows, "completion_type:Ouvriere");
            AssertRow(completedRows, "worker_granted_only_after_timer:true");
            AssertRow(completedRows, "nursery_upgrade_preserved:true");
            AssertRow(completedRows, "paid_power_shortcut:false");
            AssertRow(completedRows, "hive_background_image_modified:false");
            AssertRow(completedRows, "world_map_terrain_modified:false");
        }

        private static void AssertGuidedChapterIntro(int chapterNumber, string expectedCue, Action beginChapter)
        {
            beginChapter();

            Assert.That(HiveViewProductUiPresenter.GuidedChapterIntroVisibleForProof(chapterNumber), Is.True);
            Assert.That(HiveViewProductUiPresenter.GuidedChapterNarrationCueForProof(), Is.EqualTo(expectedCue));
            Assert.That(HiveViewProductUiPresenter.GuidedChapterNarrationTextForProof(), Is.Not.Empty);
            Assert.That(HiveViewProductUiPresenter.GuidedChapterIntroAcceptsInputForProof(false), Is.True);
            Assert.That(HiveViewProductUiPresenter.GuidedChapterIntroAcceptsInputForProof(true), Is.True);
            Assert.That(HiveViewProductUiPresenter.GuidedTutorialBlocksUnderlyingHiveChromeInputForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.GuidedChapterIntroOpacityForProof(), Is.InRange(0f, 1f));

            Assert.That(HiveViewProductUiPresenter.TryGetGuidedChapterNarrationForRuntime(
                out string runtimeCue,
                out string runtimeNarration,
                out int runtimeVisibleCharacters,
                out float runtimeOpacity), Is.True);
            Assert.That(runtimeCue, Is.EqualTo(expectedCue));
            Assert.That(runtimeNarration, Is.EqualTo(HiveViewProductUiPresenter.GuidedChapterNarrationTextForProof()));
            Assert.That(runtimeVisibleCharacters, Is.InRange(0, runtimeNarration.Length));
            Assert.That(runtimeOpacity, Is.InRange(0f, 1f));

            HiveViewProductUiPresenter.RevealGuidedChapterNarrationForProof();
            Assert.That(HiveViewProductUiPresenter.GuidedChapterNarrationVisibleCharactersForProof(), Is.EqualTo(runtimeNarration.Length));
            HiveViewProductUiPresenter.DismissGuidedChapterIntroForProof(chapterNumber);
            Assert.That(HiveViewProductUiPresenter.GuidedChapterIntroVisibleForProof(chapterNumber), Is.False);
        }

        private static void CompleteGuidedFirstChapter()
        {
            HiveViewProductUiPresenter.BeginGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedOpeningProductionForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningProductionForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.ChooseGuidedOpeningAllocationForProof(false);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitRouteForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitRouteForProof();
                Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
                HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitMaintenanceForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitMaintenanceForProof();
                if (round == 0) HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            }
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCharterForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCharterForProof();
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCharterCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCommissioningLoadForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCommissioningLoadForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningValidationForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedOpeningCommissioningSealForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningCommissioningSealForProof();
            HiveViewProductUiPresenter.ChooseGuidedOpeningBroodSupplyForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningBroodSupplyForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningBroodSupplyCheckForProof(2);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedOpeningHygienePurgeForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedOpeningHygienePurgeForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.RegisterGuidedOpeningHygienePurgeCheckForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedOpeningRewardForProof(false);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_welcome");
        }

        private static void CompleteGuidedBroodCareCircuit()
        {
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitSupplyForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedBroodCircuitSupplyForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitTreatmentForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedBroodCircuitTreatmentForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();

            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitSupplyForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodCircuitSupplyForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.ChooseGuidedBroodCircuitTreatmentForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodCircuitTreatmentForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
        }

        private static void CompleteGuidedBroodIncubation(bool firstShift = false)
        {
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationInspectionForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedBroodIncubationInspectionForProof();
                HiveViewProductUiPresenter.ChooseExpectedGuidedBroodIncubationVitalityPriorityForProof();
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(0);
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(1);
                HiveViewProductUiPresenter.RegisterGuidedBroodIncubationCheckForProof(2);
                HiveViewProductUiPresenter.ChooseGuidedBroodIncubationTreatmentForProof(false);
                HiveViewProductUiPresenter.CompleteGuidedBroodIncubationTreatmentForProof();
                HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            }
            HiveViewProductUiPresenter.ChooseGuidedBroodIncubationDoctrineForProof(firstShift);
            HiveViewProductUiPresenter.CompleteGuidedBroodIncubationDoctrineForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_incubation_completed");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_worker_handoff_choice");
            HiveViewProductUiPresenter.ChooseGuidedBroodWorkerHandoffForProof(!firstShift);
            HiveViewProductUiPresenter.CompleteGuidedBroodWorkerHandoffForProof();
            string resource = firstShift ? "wax_workshop" : "honey_storage";
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof(resource), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.RegisterGuidedBroodWorkerHandoffCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedBroodWorkerHandoffCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedBroodWorkerHandoffCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedBroodWorkerHandoffCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_worker_handoff_result");
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "brood_worker_handoff_check_count:3");

            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_wax_consolidation_choice");
            HiveViewProductUiPresenter.ChooseGuidedBroodWaxConsolidationForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedBroodWaxConsolidationForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("wax_workshop"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.RegisterGuidedBroodWaxConsolidationCheckForProof();
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:brood_wax_consolidation_result");
        }

        private static void CompleteGuidedWorkerCertification()
        {
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationTaskForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationTaskForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationMentorshipForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationMentorshipForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationTaskForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationTaskForProof();
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerCertificationCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedWorkerCertificationMentorshipForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedWorkerCertificationMentorshipForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedWorkerWorkshopHandoffForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedWorkerWorkshopHandoffForProof();
            HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopHandoffCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopHandoffCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopHandoffCheckForProof(2);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ChooseGuidedWorkerWorkshopCommissionForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedWorkerWorkshopCommissionForProof();
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedWorkerWorkshopCommissionCheckForProof(2);
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
        }

        private static void CompleteGuidedReadinessRound(bool balancedProduction, bool nurses, bool seal)
        {
            HiveViewProductUiPresenter.ChooseGuidedReadinessProductionForProof(balancedProduction);
            HiveViewProductUiPresenter.CompleteGuidedReadinessProductionForProof();
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage"), Is.GreaterThan(0f));
            Assert.That(HiveViewProductUiPresenter.CollectManualProductionForProof("warehouse_cells"), Is.GreaterThan(0f));
            HiveViewProductUiPresenter.ChooseGuidedReadinessPopulationForProof(nurses);
            HiveViewProductUiPresenter.CompleteGuidedReadinessPopulationForProof();
            HiveViewProductUiPresenter.ChooseGuidedReadinessDefenseForProof(seal);
            HiveViewProductUiPresenter.CompleteGuidedReadinessDefenseForProof();
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedReadinessCheckForProof(2);
            AssertRow(HiveViewProductUiPresenter.GuidedCollectionTutorialForProof(), "tutorial_step:readiness_round_result");
        }

        private static int ProofInt(string[] rows, string key)
        {
            string prefix = key + ":";
            string row = rows.First(value => value.StartsWith(prefix, StringComparison.Ordinal));
            return int.Parse(row.Substring(prefix.Length), CultureInfo.InvariantCulture);
        }

        private static string ProofValue(string[] rows, string key)
        {
            string prefix = key + ":";
            string row = rows.First(value => value.StartsWith(prefix, StringComparison.Ordinal));
            return row.Substring(prefix.Length);
        }

        private static void AssertProofValuesEqual(string[] before, string[] after, params string[] keys)
        {
            foreach (string key in keys)
            {
                Assert.That(
                    ProofValue(after, key),
                    Is.EqualTo(ProofValue(before, key)),
                    "Proof value changed unexpectedly: " + key);
            }
        }

        private static void AssertRow(string[] rows, string expected)
        {
            if (!rows.Any(row => string.Equals(row, expected, StringComparison.Ordinal)))
            {
                Assert.Fail("Expected proof row not found: " + expected + Environment.NewLine + string.Join(Environment.NewLine, rows));
            }
        }
    }

    internal static class LivingHiveManualCollectionMenu
    {
        [MenuItem("Bee Kingdom/Playground/QA/Run LivingHive Manual Collection Checks _F8")]
        private static void RunChecks()
        {
            SandboxLivingHiveManualCollectionTests.RunAllForBatch();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Act I Quest Journal")]
        private static void PreviewActOneQuestJournal()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing the Act I quest journal.");
                return;
            }

            HiveViewProductUiPresenter.PreviewGuidedOpeningActQuestForProof(3);
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 1 Commissioning #F3")]
        private static void PreviewChapterOneCommissioning()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing Chapter 1 commissioning.");
                return;
            }

            PrepareChapterOneInstallationPreview(false);
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 1 Charter #F2")]
        private static void PreviewChapterOneCharter()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing the Chapter 1 charter.");
                return;
            }

            PrepareChapterOneInstallationPreview(true);
        }

        private static void PrepareChapterOneInstallationPreview(bool advanceToCharter)
        {
            HiveViewProductUiPresenter.BeginGuidedOpeningInstallationForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            for (int round = 0; round < 2; round++)
            {
                HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitRouteForProof(round == 0);
                HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitRouteForProof();
                HiveViewProductUiPresenter.CollectManualProductionForProof("honey_storage");
                HiveViewProductUiPresenter.ChooseGuidedOpeningCircuitMaintenanceForProof(round == 1);
                HiveViewProductUiPresenter.CompleteGuidedOpeningCircuitMaintenanceForProof();
                if (round == 0) HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            }
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            if (!advanceToCharter) return;

            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedOpeningCommissioningCheckForProof(2);
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 2")]
        private static void PreviewChapterTwo()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing Chapter 2.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedBroodTutorialForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 2 Care Circuit #F8")]
        private static void PreviewChapterTwoCareCircuit()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing the Chapter 2 care circuit.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedBroodCircuitForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 2 Incubation")]
        private static void PreviewChapterTwoIncubation()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing Chapter 2 incubation.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedBroodIncubationForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 3")]
        private static void PreviewChapterThree()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing Chapter 3.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedWorkerTutorialForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 3 Apprenticeship")]
        private static void PreviewChapterThreeApprenticeship()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing the Chapter 3 apprenticeship.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedWorkerTrialForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 3 Certification #F7")]
        private static void PreviewChapterThreeCertification()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing the Chapter 3 certification.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedWorkerCertificationForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 4")]
        private static void PreviewChapterFour()
        {
            LivingHiveTutorialPreviewLauncher.Request(LivingHiveTutorialPreviewLauncher.ChapterFour);
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 4 Test Batch Choice")]
        private static void PreviewChapterFourTestBatchChoice()
        {
            LivingHiveTutorialPreviewLauncher.Request(LivingHiveTutorialPreviewLauncher.ChapterFourTestBatchChoice);
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 4 Structural Checks #F6")]
        private static void PreviewChapterFourStructuralChecks()
        {
            LivingHiveTutorialPreviewLauncher.Request(LivingHiveTutorialPreviewLauncher.ChapterFourStructuralChecks);
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 4 Supply Choice #F5")]
        private static void PreviewChapterFourSupplyChoice()
        {
            LivingHiveTutorialPreviewLauncher.Request(LivingHiveTutorialPreviewLauncher.ChapterFourSupplyChoice);
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 4 Doctrine Choice #F4")]
        private static void PreviewChapterFourDoctrineChoice()
        {
            LivingHiveTutorialPreviewLauncher.Request(LivingHiveTutorialPreviewLauncher.ChapterFourDoctrineChoice);
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 5")]
        private static void PreviewChapterFive()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing Chapter 5.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedDefenseTutorialForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 5 Doctrine Choice")]
        private static void PreviewChapterFiveDoctrineChoice()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing the Chapter 5 doctrine choice.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedDefenseTutorialForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.ActivateGuidedCollectionTutorialTargetForProof("guard_post");
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.CompleteGuidedDefenseScoutingForProof();
            HiveViewProductUiPresenter.ChooseGuidedDefensePlanForProof(false);
            HiveViewProductUiPresenter.CompleteGuidedDefenseForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(0);
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(1);
            HiveViewProductUiPresenter.RegisterGuidedDefenseDebriefCheckForProof(2);
            HiveViewProductUiPresenter.ChooseGuidedDefenseRecoveryForProof(true);
            HiveViewProductUiPresenter.CompleteGuidedDefenseRecoveryForProof();
            HiveViewProductUiPresenter.AdvanceGuidedCollectionTutorialForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 5 Readiness Loop #F9")]
        private static void PreviewChapterFiveReadinessLoop()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing the Chapter 5 readiness loop.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedReadinessLoopForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 6")]
        private static void PreviewChapterSix()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing Chapter 6.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedWorldTransitionForProof();
        }

        [MenuItem("Bee Kingdom/Playground/QA/Preview LivingHive Chapter 7")]
        private static void PreviewChapterSeven()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Start Play Mode in LivingHive before previewing Chapter 7.");
                return;
            }

            HiveViewProductUiPresenter.BeginGuidedForagingTutorialForProof();
        }
    }
}
