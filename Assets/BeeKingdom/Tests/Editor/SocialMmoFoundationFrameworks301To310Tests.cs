using System;
using BeeKingdom.Colony;
using NUnit.Framework;

namespace BeeKingdom.Tests
{
    public sealed class SocialMmoFoundationFrameworks301To310Tests
    {
        [Test]
        public void PlayerHiveIdentity_BlocksMissingSourcePersistenceAndServerBypass()
        {
            var identity = new PlayerHiveIdentity(
                "identity-a",
                "Golden Hive",
                new PlayerHiveIdentitySource(string.Empty, "BEE-301"),
                string.Empty,
                new PlayerHivePublicProfileProjection("Golden Hive", "defensive", "public", "none", "logistics", Array.Empty<string>()),
                new PlayerHiveIdentityVisibilityPolicy(true, true, string.Empty),
                new PlayerHiveIdentityServerAuthorityMarker(SocialServerAuthorityMarker.ServerAuthorityRequired, "Bee Server", accountBypassRequested: true),
                persistentProfileRequested: true);

            PlayerHiveIdentityDiagnostics diagnostics = identity.Evaluate();

            Assert.That(diagnostics.Contains(PlayerHiveIdentityDiagnosticCode.PlayerHiveIdentitySourceMissing), Is.True);
            Assert.That(diagnostics.Contains(PlayerHiveIdentityDiagnosticCode.PlayerReferenceNotAuthoritative), Is.True);
            Assert.That(diagnostics.Contains(PlayerHiveIdentityDiagnosticCode.PersistentProfileRequested), Is.True);
            Assert.That(diagnostics.Contains(PlayerHiveIdentityDiagnosticCode.IdentityVisibilityMissing), Is.True);
            Assert.That(diagnostics.Contains(PlayerHiveIdentityDiagnosticCode.ServerAccountBypassRequested), Is.True);
        }

        [Test]
        public void InvestmentAndProgression_BlockMonetizationRankingsAndServerComparisons()
        {
            var investment = new PlayerInvestmentProfile(
                "investment-a",
                "identity-a",
                new[]
                {
                    new InvestmentAxis(InvestmentAxisType.EconomicInvestmentProjection, 1, "store-preview", "projection only", SocialMmoProductPillar.Economy)
                },
                Array.Empty<InvestmentProjectionImpact>(),
                new[]
                {
                    new InvestmentBalanceRisk("risk-a", payToWinUnclassified: true, competitiveRewardRequested: true)
                },
                new[]
                {
                    new InvestmentServerAuthorityRequirement("server-a", "monetization")
                },
                monetizationRuntimeRequested: true);

            PlayerInvestmentProfileDiagnostics investmentDiagnostics = investment.Evaluate();
            Assert.That(investmentDiagnostics.Contains(PlayerInvestmentDiagnosticCode.MonetizationRuntimeRequested), Is.True);
            Assert.That(investmentDiagnostics.Contains(PlayerInvestmentDiagnosticCode.PayToWinRiskUnclassified), Is.True);
            Assert.That(investmentDiagnostics.Contains(PlayerInvestmentDiagnosticCode.CompetitiveRewardRequested), Is.True);
            Assert.That(investmentDiagnostics.Contains(PlayerInvestmentDiagnosticCode.ServerAuthorityRequiredForInvestment), Is.True);

            var comparison = new AsymmetricProgressionComparison(
                "comparison-a",
                new[]
                {
                    new AsymmetricHiveProgressionProfile("profile-a", string.Empty, Array.Empty<HiveProgressionDivergenceAxis>(), new HiveSpecializationProjection("spec-a", "support"), Array.Empty<HiveBalanceWarning>(), new[] { "server-required" })
                },
                Array.Empty<string>(),
                Array.Empty<HiveBalanceWarning>(),
                Array.Empty<string>(),
                competitiveRankingRequested: true);

            AsymmetricHiveProgressionDiagnostics progressionDiagnostics = comparison.Evaluate();
            Assert.That(progressionDiagnostics.Contains(AsymmetricHiveProgressionDiagnosticCode.AsymmetricAxisMissing), Is.True);
            Assert.That(progressionDiagnostics.Contains(AsymmetricHiveProgressionDiagnosticCode.CompetitiveRankingRequested), Is.True);
            Assert.That(progressionDiagnostics.Contains(AsymmetricHiveProgressionDiagnosticCode.ProgressionBalanceRiskMissing), Is.True);
            Assert.That(progressionDiagnostics.Contains(AsymmetricHiveProgressionDiagnosticCode.ServerAuthorityRequiredForComparison), Is.True);
            Assert.That(progressionDiagnostics.Contains(AsymmetricHiveProgressionDiagnosticCode.HiveComparisonSourceMissing), Is.True);
        }

        [Test]
        public void PlaystyleAllianceAndDiplomacy_BlockFinalClaimsAndRuntimeRequests()
        {
            var posture = new PlayerPlaystylePosture(
                "posture-a",
                "identity-a",
                PlaystylePostureType.Militant,
                new PlaystylePostureConfidence(0.8, "projection"),
                new[] { new PlaystylePostureSignal("signal-a", string.Empty, 1, 1, "missing source") },
                Array.Empty<PlaystylePostureConsequenceProjection>(),
                Array.Empty<PlaystylePostureLimitation>(),
                finalVerdictClaimed: true,
                playerSanctionRequested: true,
                diplomacyRuntimeRequested: true,
                pvpActivationRequested: true);

            PlayerPlaystylePostureDiagnostics postureDiagnostics = posture.Evaluate();
            Assert.That(postureDiagnostics.Contains(PlayerPlaystylePostureDiagnosticCode.PlaystyleSignalMissing), Is.True);
            Assert.That(postureDiagnostics.Contains(PlayerPlaystylePostureDiagnosticCode.PostureVerdictFinalClaimed), Is.True);
            Assert.That(postureDiagnostics.Contains(PlayerPlaystylePostureDiagnosticCode.PlayerSanctionRequested), Is.True);
            Assert.That(postureDiagnostics.Contains(PlayerPlaystylePostureDiagnosticCode.DiplomacyRuntimeRequested), Is.True);
            Assert.That(postureDiagnostics.Contains(PlayerPlaystylePostureDiagnosticCode.PvpActivationRequested), Is.True);

            var alliance = new AllianceMembershipProjection(
                "alliance-a",
                string.Empty,
                "alliance-hint",
                new AllianceMembershipRoleHint("role-a", "Leader", finalRoleClaimed: true),
                Array.Empty<AllianceContributionSignal>(),
                new AlliancePermissionBoundary(runtimePermissionRequested: true, bankRuntimeRequested: true),
                new AllianceServerAuthorityMarker(true, "membership"),
                AllianceMembershipStatus.ServerAuthorityRequired);

            AllianceMembershipDiagnostics allianceDiagnostics = alliance.Evaluate();
            Assert.That(allianceDiagnostics.Contains(AllianceMembershipDiagnosticCode.AllianceMembershipSourceMissing), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceMembershipDiagnosticCode.AllianceRoleFinalClaimed), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceMembershipDiagnosticCode.AlliancePermissionRuntimeRequested), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceMembershipDiagnosticCode.AllianceBankRuntimeRequested), Is.True);
            Assert.That(allianceDiagnostics.Contains(AllianceMembershipDiagnosticCode.AllianceServerAuthorityRequired), Is.True);

            var diplomacy = new DiplomacyRelationshipProjection(
                "diplomacy-a",
                string.Empty,
                string.Empty,
                DiplomacyRelationshipIntent.WarCandidate,
                new DiplomacyTrustSignal(0, "projection"),
                new DiplomacyConflictRisk("risk-a", "border", 3, "server"),
                new DiplomacyServerAuthorityMarker(true, "treaty"),
                treatyRuntimeRequested: true,
                warDeclarationRuntimeRequested: true);

            DiplomacyRelationshipDiagnostics diplomacyDiagnostics = diplomacy.Evaluate();
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipDiagnosticCode.DiplomacySourceMissing), Is.True);
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipDiagnosticCode.DiplomacyTargetMissing), Is.True);
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipDiagnosticCode.DiplomacyTreatyRuntimeRequested), Is.True);
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipDiagnosticCode.WarDeclarationRuntimeRequested), Is.True);
            Assert.That(diplomacyDiagnostics.Contains(DiplomacyRelationshipDiagnosticCode.DiplomacyServerAuthorityRequired), Is.True);
        }

        [Test]
        public void CommunicationArmyAndPvp_BlockRuntimeChatArmyCombatAndLocalWar()
        {
            var channel = new SocialCommunicationChannelProjection(
                string.Empty,
                CommunicationChannelType.Alliance,
                new CommunicationVisibilityRule("alliance", true, "projection"),
                new CommunicationModerationRequirement(false, "missing", string.Empty),
                new CommunicationServerAuthorityMarker(true, "chat"),
                chatRuntimeRequested: true,
                messagePersistenceRequested: true);

            SocialCommunicationChannelDiagnostics channelDiagnostics = channel.Evaluate();
            Assert.That(channelDiagnostics.Contains(SocialCommunicationChannelDiagnosticCode.CommunicationChannelMissing), Is.True);
            Assert.That(channelDiagnostics.Contains(SocialCommunicationChannelDiagnosticCode.ChatRuntimeRequested), Is.True);
            Assert.That(channelDiagnostics.Contains(SocialCommunicationChannelDiagnosticCode.MessagePersistenceRequested), Is.True);
            Assert.That(channelDiagnostics.Contains(SocialCommunicationChannelDiagnosticCode.ModerationMissing), Is.True);
            Assert.That(channelDiagnostics.Contains(SocialCommunicationChannelDiagnosticCode.CommunicationServerAuthorityRequired), Is.True);

            var army = new ArmyTrainingDomainBoundary(
                string.Empty,
                Array.Empty<ArmyTrainingDomainResponsibility>(),
                Array.Empty<ArmyTrainingSocialUseCase>(),
                new[] { new ArmyTrainingForbiddenRuntimeAction("train-now", "server authoritative") },
                armyRuntimeRequested: true,
                parallelCombatSystemRequested: true,
                serverAuthorityMissing: true);

            ArmyTrainingDomainDiagnostics armyDiagnostics = army.Evaluate();
            Assert.That(armyDiagnostics.Contains(ArmyTrainingDiagnosticCode.ArmyBoundarySourceMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyTrainingDiagnosticCode.ArmySocialUseCaseMissing), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyTrainingDiagnosticCode.ArmyRuntimeRequested), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyTrainingDiagnosticCode.ParallelCombatSystemRequested), Is.True);
            Assert.That(armyDiagnostics.Contains(ArmyTrainingDiagnosticCode.ServerAuthorityForArmyMissing), Is.True);

            var pvp = new PvpWarServerAuthorityBoundary(
                string.Empty,
                Array.Empty<PvpWarOwnershipTopic>(),
                Array.Empty<ClientWarProjectionLimit>(),
                null,
                localWarResolutionRequested: true,
                localRewardMutationRequested: true,
                localRankingRequested: true,
                matchmakingRuntimeRequested: true,
                monetizationRuntimeRequested: true);

            PvpWarServerAuthorityDiagnostics pvpDiagnostics = pvp.Evaluate();
            Assert.That(pvpDiagnostics.Contains(PvpWarServerAuthorityDiagnosticCode.PvpAuthorityTopicMissing), Is.True);
            Assert.That(pvpDiagnostics.Contains(PvpWarServerAuthorityDiagnosticCode.LocalWarResolutionRequested), Is.True);
            Assert.That(pvpDiagnostics.Contains(PvpWarServerAuthorityDiagnosticCode.LocalRewardMutationRequested), Is.True);
            Assert.That(pvpDiagnostics.Contains(PvpWarServerAuthorityDiagnosticCode.LocalRankingRequested), Is.True);
            Assert.That(pvpDiagnostics.Contains(PvpWarServerAuthorityDiagnosticCode.MatchmakingRuntimeRequested), Is.True);
            Assert.That(pvpDiagnostics.Contains(PvpWarServerAuthorityDiagnosticCode.MonetizationRuntimeRequested), Is.True);
            Assert.That(pvpDiagnostics.Contains(PvpWarServerAuthorityDiagnosticCode.ServerSpecRequired), Is.True);
        }

        [Test]
        public void SocialMmoFoundationsGate_BlocksBee311AndExportsServerBoundary()
        {
            var gate = new SocialMmoFoundationsGate(
                new SocialMmoFoundationsInputSet("identity", "investment", "progression", "posture", "alliance", "diplomacy", "communication", "army", "pvp"),
                new SocialMmoProductPillarCoverage(new[] { SocialMmoProductPillar.SocialMmo, SocialMmoProductPillar.Alliance, SocialMmoProductPillar.Diplomacy, SocialMmoProductPillar.Communication }, simulationOnly: false, demoEvidencePresent: true),
                new[]
                {
                    new SocialMmoFoundationGap("BEE-309", SocialMmoProductPillar.PvpWar, "Bee Server", "Specify authoritative PvP and war services")
                },
                serverAuthorityGapOpen: true,
                bee311Premature: true);

            SocialMmoFoundationsVerdict verdict = gate.Evaluate();
            SocialMmoFoundationsExport export = gate.Export("export-a");

            Assert.That(verdict.VerdictType, Is.EqualTo(SocialMmoFoundationsVerdictType.BlockedByBee311Premature));
            Assert.That(verdict.Contains(SocialMmoFoundationsDiagnosticCode.PvpAuthorityGapOpen), Is.True);
            Assert.That(verdict.Contains(SocialMmoFoundationsDiagnosticCode.Bee311Premature), Is.True);
            Assert.That(export.Bee311Status, Is.EqualTo(SocialMmoFoundationsGate.Bee311BlockedStatus));
        }
    }
}
