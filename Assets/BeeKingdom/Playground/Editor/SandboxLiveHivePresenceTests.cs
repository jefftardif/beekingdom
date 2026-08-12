using System;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class SandboxLiveHivePresenceTests
    {
        public static void ValidateLiveHivePresence()
        {
            var tests = new SandboxLiveHivePresenceTests();
            tests.LiveHivePresenceCatalogAndBudgetMeetBEE680Gate();
            tests.TargetedBee663To680ContractsExposeExpectedFields();
            tests.ProductionPolishBEE681To700ContractsAndGatePass();
            tests.RuntimeBridgeBEE701To720ContractsAndGatePass();
            tests.RuntimeBridgeOfflineFallbackIsConsultationOnly();
            tests.ServerFirstBEE721To740ContractsAndGatePass();
            tests.ServerFirstLanguageAndFailureStatesStayNonLive();
            tests.LiveHivePresenceKeepsReferenceHotspotsClickable();
            tests.ProductionPolishFeedbackPulseAndPanelAnimationAreLocal();
            tests.ReducedMotionAndPortraitThrottleArePlayerFacing();
            tests.LiveHivePresenceDeclaresNoServerAuthority();
            Debug.Log("BEE-661 to BEE-740 live hive presence, runtime bridge and server-first validation completed.");
        }

        [Test]
        public void LiveHivePresenceCatalogAndBudgetMeetBEE680Gate()
        {
            Assert.That(HiveViewProductUiPresenter.VisibleBeeCatalog.VisibleFamilyCount, Is.GreaterThanOrEqualTo(4));
            Assert.That(HiveViewProductUiPresenter.GetLiveHiveBeeAgentCountForProof(), Is.GreaterThanOrEqualTo(12));
            Assert.That(HiveViewProductUiPresenter.BeeDensityBudget.DesktopVisibleBees, Is.EqualTo(14));
            Assert.That(HiveViewProductUiPresenter.BeeDensityBudget.PortraitVisibleBees, Is.LessThanOrEqualTo(7));
            Assert.That(HiveViewProductUiPresenter.BeeCharacterMotionScorecard.MeetsUiThreshold, Is.True);
            Assert.That(HiveViewProductUiPresenter.LivePresenceGate.Verdict, Is.EqualTo(BeePresenceGateVerdict.Pass));
            Assert.That(HiveViewProductUiPresenter.LivePresenceGate.Bee681Blocked, Is.True);
        }

        [Test]
        public void TargetedBee663To680ContractsExposeExpectedFields()
        {
            IBeePresenceContract[] contracts =
            {
                HiveViewProductUiPresenter.BeeCharacterStyleBrief,
                HiveViewProductUiPresenter.BeeIdleLoops,
                HiveViewProductUiPresenter.BeeShortFlightPlan,
                HiveViewProductUiPresenter.BeeWalkCrawlSet,
                HiveViewProductUiPresenter.LocalCollectionGesture,
                HiveViewProductUiPresenter.BeeAssetPipeline,
                HiveViewProductUiPresenter.BeeAnimationHandoff,
                HiveViewProductUiPresenter.LivePresenceBuilderBundle,
                HiveViewProductUiPresenter.LivePresenceGate
            };

            foreach (IBeePresenceContract contract in contracts)
            {
                Assert.That(contract.CharacterOrAssetScope, Is.Not.Empty);
                Assert.That(contract.AnimationRule, Is.Not.Empty);
                Assert.That(contract.ZoneReadabilityRule, Is.Not.Empty);
                Assert.That(contract.MobilePerformanceRule, Is.Not.Empty);
                Assert.That(contract.EvidenceRequirement, Is.Not.Empty);
                Assert.That(contract.NonLiveBoundary, Is.Not.Empty);
            }

            Assert.That(File.Exists("Docs/Demos/BEE-673_BeeAssetManifest.md"), Is.True);
            Assert.That(File.Exists("Docs/Demos/BEE-674_BeeAnimationHandoff.md"), Is.True);
            Assert.That(HiveViewProductUiPresenter.BeeAssetPipeline.CharacterOrAssetScope, Does.Contain("Bee_[Family]"));
            Assert.That(HiveViewProductUiPresenter.BeeAnimationHandoff.ReducedMotionRule, Does.Contain("idle"));
        }

        [Test]
        public void ProductionPolishBEE681To700ContractsAndGatePass()
        {
            ILiveHiveProductionPolishContract[] contracts =
            {
                HiveViewProductUiPresenter.ProductionPolishIntake,
                HiveViewProductUiPresenter.FeedbackPulseLanguage,
                HiveViewProductUiPresenter.ProductionMotionCurves,
                HiveViewProductUiPresenter.ActivityLayerComposition,
                HiveViewProductUiPresenter.ZoneActivityPolish,
                HiveViewProductUiPresenter.HoverTapFeedback,
                HiveViewProductUiPresenter.AnimatedPanelResponse,
                HiveViewProductUiPresenter.ReducedMotionAccessibility,
                HiveViewProductUiPresenter.MobileMotionThrottle,
                HiveViewProductUiPresenter.ProductionAssetChecklist,
                HiveViewProductUiPresenter.DebugPlayerViewSeparation,
                HiveViewProductUiPresenter.SoundlessFeedbackFallback,
                HiveViewProductUiPresenter.MotionRegressionLock,
                HiveViewProductUiPresenter.ProductionPerformancePack,
                HiveViewProductUiPresenter.FeedbackQaProtocol,
                HiveViewProductUiPresenter.ProductionMotionScorecard,
                HiveViewProductUiPresenter.MotionIntegrationShotList,
                HiveViewProductUiPresenter.ProductionServerNonLiveAudit,
                HiveViewProductUiPresenter.MotionIntegrationBuilderBundle,
                HiveViewProductUiPresenter.ProductionPolishGate
            };

            foreach (ILiveHiveProductionPolishContract contract in contracts)
            {
                Assert.That(contract.FeedbackScope, Is.Not.Empty);
                Assert.That(contract.MotionIntegrationRule, Is.Not.Empty);
                Assert.That(contract.PlayerReadabilityRule, Is.Not.Empty);
                Assert.That(contract.PerformanceEvidence, Is.Not.Empty);
                Assert.That(contract.DemoQaRequirement, Is.Not.Empty);
                Assert.That(contract.NonLiveBoundary, Is.Not.Empty);
            }

            Assert.That(HiveViewProductUiPresenter.GetProductionPolishContractNamesForProof().Length, Is.EqualTo(20));
            Assert.That(HiveViewProductUiPresenter.ProductionPerformancePack.MeetsPreviewBudget, Is.True);
            Assert.That(HiveViewProductUiPresenter.ProductionMotionScorecard.MeetsGate, Is.True);
            Assert.That(HiveViewProductUiPresenter.ProductionServerNonLiveAudit.PassesBoundary, Is.True);
            Assert.That(HiveViewProductUiPresenter.ProductionPolishGate.Verdict, Is.EqualTo(LiveHiveProductionPolishVerdict.PassWithReserves));
            Assert.That(HiveViewProductUiPresenter.ProductionPolishGate.Bee701Blocked, Is.True);
            Assert.That(HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof(), Is.False);
        }

        [Test]
        public void RuntimeBridgeBEE701To720ContractsAndGatePass()
        {
            IProductionRuntimeBridgeContract[] contracts = HiveViewProductUiPresenter.GetProductionRuntimeBridgeContracts();

            Assert.That(contracts.Length, Is.EqualTo(20));
            foreach (IProductionRuntimeBridgeContract contract in contracts)
            {
                Assert.That(contract.Scope, Is.Not.Empty);
                Assert.That(contract.PlayerVisibleState, Is.Not.Empty);
                Assert.That(contract.ServerBoundary, Is.Not.Empty);
                Assert.That(contract.EvidenceRequirement, Is.Not.Empty);
                Assert.That(contract.NonClaimRule, Is.Not.Empty);
                Assert.That(contract.NextGate, Is.Not.Empty);
            }

            Assert.That(HiveViewProductUiPresenter.RuntimeBridgeCatalog.HasCompleteLot, Is.True);
            Assert.That(HiveViewProductUiPresenter.RuntimeBridgeGate.Verdict, Is.EqualTo(RuntimeBridgeGateVerdict.PassWithReserves));
            Assert.That(HiveViewProductUiPresenter.RuntimeBridgeGate.Bee721Blocked, Is.True);
            Assert.That(HiveViewProductUiPresenter.RuntimeBridgeEvidence.Bee700BaselinePreserved, Is.True);
            Assert.That(HiveViewProductUiPresenter.RuntimeBridgeEvidence.NonClaims, Does.Contain("Consultation demo non officielle"));
            Assert.That(HiveViewProductUiPresenter.RuntimeBridgeEvidence.NonClaims, Does.Contain("Connexion serveur requise pour jeu officiel"));
            Assert.That(HiveViewProductUiPresenter.RuntimeBridgeIntroducesLiveGameplayForProof(), Is.False);
            Assert.That(HiveViewProductUiPresenter.NonGameplayHandshake.ServerBoundary, Does.Contain("Aucune ressource"));
            Assert.That(HiveViewProductUiPresenter.PlayerEntryShell.NonClaimRule, Does.Contain("MMO actif"));
            Assert.That(HiveViewProductUiPresenter.PlayerEntryShell.PlayerVisibleState, Does.Contain("Connexion serveur"));
        }

        [Test]
        public void RuntimeBridgeOfflineFallbackIsConsultationOnly()
        {
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.OfflineFallback);
            RuntimeBridgePlayerFacingState offline = HiveViewProductUiPresenter.RuntimeBridgePlayerState;

            Assert.That(offline.Mode, Is.EqualTo(RuntimeBridgePlayerMode.OfflineFallback));
            Assert.That(offline.OfflineConsultationAvailable, Is.True);
            Assert.That(offline.OfficialGameplayRequiresServer, Is.True);
            Assert.That(offline.GameplayMutationAllowed, Is.False);
            Assert.That(offline.StatusTitle, Does.Contain("Serveur indisponible"));
            Assert.That(offline.PrimaryAction, Does.Contain("Consulter"));
            Assert.That(offline.Disclosure, Does.Contain("Consultation seulement"));
            Assert.That(offline.Disclosure, Does.Contain("Aucune progression"));
            Assert.That(offline.Disclosure, Does.Contain("sauvegarde"));
            Assert.That(offline.Disclosure, Does.Contain("economie"));
            Assert.That(HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof(), Is.False);

            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.LocalPreview);
        }

        [Test]
        public void ServerFirstBEE721To740ContractsAndGatePass()
        {
            IProductionRuntimeBridgeContract[] contracts = HiveViewProductUiPresenter.GetServerFirstConnectionContracts();

            Assert.That(contracts.Length, Is.EqualTo(20));
            foreach (IProductionRuntimeBridgeContract contract in contracts)
            {
                Assert.That(contract.Scope, Is.Not.Empty);
                Assert.That(contract.PlayerVisibleState, Is.Not.Empty);
                Assert.That(contract.ServerBoundary, Is.Not.Empty);
                Assert.That(contract.EvidenceRequirement, Is.Not.Empty);
                Assert.That(contract.NonClaimRule, Is.Not.Empty);
                Assert.That(contract.NextGate, Is.Not.Empty);
            }

            Assert.That(HiveViewProductUiPresenter.ServerFirstCatalog.HasCompleteLot, Is.True);
            Assert.That(HiveViewProductUiPresenter.ServerFirstGate.Verdict, Is.EqualTo(RuntimeBridgeGateVerdict.PassWithReserves));
            Assert.That(HiveViewProductUiPresenter.ServerFirstGate.Bee741Blocked, Is.True);
            Assert.That(HiveViewProductUiPresenter.ServerFirstGate.OfficialGameplayRequiresServer, Is.True);
            Assert.That(HiveViewProductUiPresenter.ServerFirstGate.OfflineIsConsultationOnly, Is.True);
            Assert.That(HiveViewProductUiPresenter.ServerFirstEvidence.ProductionRouteNonRouted, Is.True);
            Assert.That(HiveViewProductUiPresenter.ServerFirstEvidence.Server023HandshakeLocalFact, Is.True);
            Assert.That(HiveViewProductUiPresenter.ServerFirstIntroducesLiveGameplayForProof(), Is.False);
            Assert.That(HiveViewProductUiPresenter.OfflineConsultationGuard.NonClaimRule, Does.Contain("Aucune progression"));
            Assert.That(HiveViewProductUiPresenter.HandshakeAvailabilityGate.ServerBoundary, Does.Contain("aucune ressource").IgnoreCase);
            Assert.That(HiveViewProductUiPresenter.ServerConnectionBuilderBundle.NonClaimRule, Does.Contain("Aucune mutation gameplay"));
        }

        [Test]
        public void ServerFirstLanguageAndFailureStatesStayNonLive()
        {
            Assert.That(HiveViewProductUiPresenter.ServerFirstEvidence.ForbiddenLanguage, Does.Contain("Ancien lexique offline jouable"));
            Assert.That(HiveViewProductUiPresenter.ServerFirstEvidence.ForbiddenLanguage.All(label => label.Contains("Connexion serveur", StringComparison.OrdinalIgnoreCase) == false), Is.True);
            Assert.That(HiveViewProductUiPresenter.ServerFirstEvidence.RequiredLanguage, Does.Contain("Connexion serveur requise"));
            Assert.That(HiveViewProductUiPresenter.ServerFirstFailureCatalog.KeepsOfflineConsultationOnly, Is.True);

            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            RuntimeBridgePlayerFacingState server = HiveViewProductUiPresenter.RuntimeBridgePlayerState;
            Assert.That(server.PrimaryAction, Does.Contain("Connexion serveur"));
            Assert.That(server.Disclosure, Does.Contain("Voie officielle"));
            Assert.That(server.GameplayMutationAllowed, Is.False);
            Assert.That(server.OfficialGameplayRequiresServer, Is.True);

            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.OfflineFallback);
            RuntimeBridgePlayerFacingState offline = HiveViewProductUiPresenter.RuntimeBridgePlayerState;
            Assert.That(offline.PrimaryAction, Does.Contain("Consulter"));
            Assert.That(offline.Disclosure, Does.Contain("Consultation seulement"));
            Assert.That(offline.GameplayMutationAllowed, Is.False);
            Assert.That(offline.OfficialGameplayRequiresServer, Is.True);

            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.LocalPreview);
        }

        [Test]
        public void ProductionPolishFeedbackPulseAndPanelAnimationAreLocal()
        {
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            Assert.That(HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof("guard_post"), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Caserne"));
            Assert.That(HiveViewProductUiPresenter.GetProductionFeedbackKindForProof(), Is.EqualTo("server"));
            Assert.That(HiveViewProductUiPresenter.IsProductionFeedbackPulseActiveForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.IsProductionDetailPanelAnimatingForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.ProductionPolishGate.NonLiveBoundary, Does.Contain("no live server"));
        }

        [Test]
        public void ReducedMotionAndPortraitThrottleArePlayerFacing()
        {
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(true);
            Assert.That(HiveViewProductUiPresenter.ProductionReducedMotionEnabledForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.IsProductionFeedbackPulseActiveForProof(), Is.True);
            Assert.That(HiveViewProductUiPresenter.IsProductionDetailPanelAnimatingForProof(), Is.False);
            Assert.That(HiveViewProductUiPresenter.ReducedMotionAccessibility.MotionIntegrationRule, Does.Contain("idle/static").Or.Contain("idle"));
            Assert.That(HiveViewProductUiPresenter.BeeDensityBudget.PortraitVisibleBees, Is.LessThanOrEqualTo(7));
            Assert.That(HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof(), Is.False);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
        }

        [Test]
        public void LiveHivePresenceKeepsReferenceHotspotsClickable()
        {
            Assert.That(HiveViewProductUiPresenter.ReferenceHotspotCount, Is.EqualTo(14));
            Assert.That(HiveViewProductUiPresenter.BeeOcclusionGuard.ClickThroughToHotspots, Is.True);
            Assert.That(HiveViewProductUiPresenter.BeeOcclusionGuard.BlocksHudPanelOcclusion, Is.True);
            Assert.That(HiveViewProductUiPresenter.BeeStateTokenCompatibility.SupportedStates, Does.Contain("selected"));
            Assert.That(HiveViewProductUiPresenter.BeeStateTokenCompatibility.SupportedStates, Does.Contain("server"));

            Assert.That(HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(784f, 178f), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Reserve miel"));
            Assert.That(HiveViewProductUiPresenter.TrySelectReferenceHotspotAtArtPointForProof(780f, 680f), Is.True);
            Assert.That(HiveViewProductUiPresenter.GetReferenceFocusedHotspotLabelForProof(), Is.EqualTo("Centre alliance"));

            string[] motions = HiveViewProductUiPresenter.GetLiveHiveBeeMotionKindsForProof();
            Assert.That(motions, Does.Contain(BeePresenceMotionKind.Idle.ToString()));
            Assert.That(motions, Does.Contain(BeePresenceMotionKind.ShortFlight.ToString()));
            Assert.That(motions, Does.Contain(BeePresenceMotionKind.WalkCrawl.ToString()));
            Assert.That(motions, Does.Contain(BeePresenceMotionKind.LocalCollection.ToString()));
            Assert.That(motions, Does.Contain(BeePresenceMotionKind.ZoneEntryExit.ToString()));
        }

        [Test]
        public void LiveHivePresenceDeclaresNoServerAuthority()
        {
            Assert.That(HiveViewProductUiPresenter.LocalCollectionGesture.WritesEconomy, Is.False);
            Assert.That(HiveViewProductUiPresenter.ServerNonLiveAudit.PassesUnityBoundary, Is.True);
            Assert.That(HiveViewProductUiPresenter.ServerNonLiveAudit.ForbiddenClaims, Does.Contain("chat live"));
            Assert.That(HiveViewProductUiPresenter.LivePresenceIntakeLedger.Boundary, Does.Contain("LOCAL PREVIEW"));
            Assert.That(HiveViewProductUiPresenter.LivePresenceBuilderBundle.SandboxPlaygroundIntegrated, Is.True);
            Assert.That(HiveViewProductUiPresenter.BeeAudioHapticBoundary.AudioPreviewOnly, Is.True);
            Assert.That(HiveViewProductUiPresenter.StaticVersusInhabitedDemoContract.RequiredShots.Any(shot => shot.Contains("mobile")), Is.True);
        }
    }
}
