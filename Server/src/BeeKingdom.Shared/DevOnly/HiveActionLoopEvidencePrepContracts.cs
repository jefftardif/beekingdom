using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.DevOnly;

public static class HiveActionLoopEvidencePrep
{
    public const string EvidenceId = "SERVER-045-BEE-914-BEE-916";
    public const string CarryForwardEvidenceId = "SERVER-046-BEE-935-BEE-937";
    public const string ServerFutureSupportEvidenceId = "SERVER-047-BEE-958";
    public const string OfficialServerClaimBoundaryEvidenceId = "SERVER-048-BEE-975";
    public const string ServerLiveClaimVisualGuardEvidenceId = "SERVER-049-BEE-997";

    public static HiveOfficialPersistenceNonClaimGuard CreateNonClaimGuard()
    {
        return new HiveOfficialPersistenceNonClaimGuard(
            EvidenceId,
            DevOnly: true,
            NonLive: true,
            OfficialLiveServerClaimAllowed: false,
            OfficialEndpointClaimAllowed: false,
            OfficialSaveClaimAllowed: false,
            OfficialEconomyClaimAllowed: false,
            OfficialPersistentArmyClaimAllowed: false,
            ProductionMigrationAllowed: false,
            ProductionPublishAllowed: false,
            Bee881ScopeAllowed: false,
            WorldMapScopeAllowed: false,
            RequiredLabels:
            [
                "local_preview",
                "dev_only",
                "future_server_required",
                "not_official_save",
                "not_official_endpoint"
            ],
            ForbiddenLabels:
            [
                "live server active",
                "officially saved",
                "synced to production",
                "official economy",
                "persistent official army"
            ],
            ContractVersion.Current);
    }

    public static HiveActionLoopEvidenceQaChecklist CreateQaChecklist()
    {
        return new HiveActionLoopEvidenceQaChecklist(
            EvidenceId,
            Criteria:
            [
                "Evidence states local/dev-only source.",
                "Evidence does not claim official live server.",
                "Evidence does not claim official endpoint.",
                "Evidence does not claim official save.",
                "Evidence does not claim official economy.",
                "Evidence does not claim official persistent army.",
                "Idempotency evidence includes idempotency key label, payload hash label, replay result and conflict result.",
                "Snapshot evidence includes snapshot version, before revision, after revision, delta list and reconciliation outcome.",
                "No BEE-881, world map, exploration, alliance, war or MMO scope appears."
            ],
            DevOnly: true,
            NonLive: true,
            OfficialQaClosureClaimAllowed: false,
            ContractVersion.Current);
    }

    public static HiveNonClaimEvidenceCarryForward CreateCarryForward()
    {
        return new HiveNonClaimEvidenceCarryForward(
            CarryForwardEvidenceId,
            SourceEvidenceId: EvidenceId,
            TargetDemo: "DEMO-075",
            TargetQa: "QA-075",
            DevOnly: true,
            NonLive: true,
            OfficialLiveServerClaimAllowed: false,
            OfficialEndpointClaimAllowed: false,
            OfficialSaveClaimAllowed: false,
            OfficialEconomyClaimAllowed: false,
            OfficialPersistentArmyClaimAllowed: false,
            ProductionMigrationAllowed: false,
            ProductionDeploymentAllowed: false,
            RequiredCarryForwardLabels:
            [
                "local_preview",
                "demo_proof",
                "dev_only",
                "official_live_false",
                "official_endpoint_false",
                "official_save_false",
                "official_economy_false",
                "official_persistent_army_false"
            ],
            RequiredQaChecks:
            [
                "Reject any live endpoint claim.",
                "Reject any official save claim.",
                "Reject any official economy claim.",
                "Reject any official persistent army claim.",
                "Reject any BEE-881 or world map scope leak."
            ],
            ContractVersion.Current);
    }

    public static HivePreviewDemoLiveStateMatrix CreatePreviewDemoLiveStateMatrix()
    {
        return new HivePreviewDemoLiveStateMatrix(
            CarryForwardEvidenceId,
            Rows:
            [
                new HivePreviewDemoLiveStateMatrixRow("resource_collect", LocalPreview: true, DemoProofAllowed: true, OfficialLiveAllowed: false, RequiredLabel: "local_preview", FalseClaimRisk: "resource gain must not imply official economy"),
                new HivePreviewDemoLiveStateMatrixRow("upgrade_building", LocalPreview: true, DemoProofAllowed: true, OfficialLiveAllowed: false, RequiredLabel: "demo_proof", FalseClaimRisk: "upgrade must not imply official save"),
                new HivePreviewDemoLiveStateMatrixRow("training_queue", LocalPreview: true, DemoProofAllowed: true, OfficialLiveAllowed: false, RequiredLabel: "dev_only", FalseClaimRisk: "queue must not imply official persistent army"),
                new HivePreviewDemoLiveStateMatrixRow("idempotency_replay", LocalPreview: true, DemoProofAllowed: true, OfficialLiveAllowed: false, RequiredLabel: "same_payload_single_result", FalseClaimRisk: "replay safety must not imply live endpoint"),
                new HivePreviewDemoLiveStateMatrixRow("snapshot_delta", LocalPreview: true, DemoProofAllowed: true, OfficialLiveAllowed: false, RequiredLabel: "snapshot_delta_preview", FalseClaimRisk: "snapshot delta must not imply official restore")
            ],
            DevOnly: true,
            NonLive: true,
            ContractVersion.Current);
    }

    public static HiveServerFutureSupportNonClaimManifest CreateServerFutureSupportManifest()
    {
        return new HiveServerFutureSupportNonClaimManifest(
            ServerFutureSupportEvidenceId,
            TargetDemo: "DEMO-076",
            TargetQa: "QA-076",
            LocalPreview: true,
            DemoProof: true,
            ApkTraceable: true,
            PhysicalDeviceProofPendingAllowed: true,
            OfficialServerLive: false,
            Endpoint: false,
            Save: false,
            Economy: false,
            ArmyPersistence: false,
            OfficialEndpointClaimAllowed: false,
            OfficialSaveClaimAllowed: false,
            OfficialEconomyClaimAllowed: false,
            OfficialPersistentArmyClaimAllowed: false,
            ProductionMigrationAllowed: false,
            ProductionDeploymentAllowed: false,
            RequiredDemo076Fields:
            [
                "official_server_live",
                "endpoint",
                "save",
                "economy",
                "army_persistence",
                "physical_device_proof"
            ],
            ForbiddenDemo076Claims:
            [
                "server live",
                "official endpoint active",
                "official save active",
                "official economy active",
                "official persistent army active"
            ],
            ContractVersion.Current);
    }

    public static HiveServerFutureSupportQaLiveClaimChecklist CreateServerFutureSupportQaChecklist()
    {
        return new HiveServerFutureSupportQaLiveClaimChecklist(
            ServerFutureSupportEvidenceId,
            Criteria:
            [
                "Reject any official live server claim.",
                "Reject any official endpoint claim.",
                "Reject any official save claim.",
                "Reject any official economy claim.",
                "Reject any official persistent army claim.",
                "Reject any production migration or deployment claim.",
                "Keep physical device proof pending unless real device install, launch and capture evidence exists.",
                "Confirm local preview, demo proof, APK traceable and official server are separate states."
            ],
            DevOnly: true,
            NonLive: true,
            OfficialLivePassAllowed: false,
            ContractVersion.Current);
    }

    public static HiveOfficialServerClaimBoundary CreateOfficialServerClaimBoundary()
    {
        return new HiveOfficialServerClaimBoundary(
            OfficialServerClaimBoundaryEvidenceId,
            TargetDemo: "DEMO-077",
            TargetQa: "QA-077",
            LocalDemoProofAllowed: true,
            PhysicalDeviceProofSeparate: true,
            PhysicalDeviceProofPendingAllowed: true,
            OfficialServerLiveAllowed: false,
            OfficialEndpointAllowed: false,
            OfficialSaveAllowed: false,
            OfficialEconomyAllowed: false,
            OfficialPersistentArmyAllowed: false,
            ProductionMigrationAllowed: false,
            ProductionDeploymentAllowed: false,
            RequiredManifestFields:
            [
                "local_demo_proof",
                "physical_device_proof",
                "official_server_live",
                "endpoint",
                "save",
                "economy",
                "army_persistence"
            ],
            ForbiddenTerms:
            [
                "Live",
                "Serveur officiel",
                "Synchronise",
                "Endpoint actif",
                "Sauvegarde officielle",
                "Economie officielle",
                "Armee persistante"
            ],
            ContractVersion.Current);
    }

    public static HiveOfficialServerClaimBoundaryQaCriteria CreateOfficialServerClaimBoundaryQaCriteria()
    {
        return new HiveOfficialServerClaimBoundaryQaCriteria(
            OfficialServerClaimBoundaryEvidenceId,
            Criteria:
            [
                "Reject any artifact claiming official live server.",
                "Reject any artifact claiming an official endpoint is active.",
                "Reject any artifact claiming official save.",
                "Reject any artifact claiming official economy.",
                "Reject any artifact claiming official persistent army.",
                "Reject any artifact merging local/demo proof with physical device proof.",
                "Keep PHYSICAL_DEVICE_PROOF=PENDING unless real device artifacts exist.",
                "Confirm local/demo proof, physical device proof and official server are separate statuses."
            ],
            DevOnly: true,
            NonLive: true,
            OfficialServerClaimAllowed: false,
            ContractVersion.Current);
    }

    public static HiveServerLiveClaimVisualGuard CreateServerLiveClaimVisualGuard()
    {
        return new HiveServerLiveClaimVisualGuard(
            ServerLiveClaimVisualGuardEvidenceId,
            TargetDemo: "DEMO-078",
            TargetQa: "QA-078",
            RequiresRealImageOrVideoArtifact: true,
            TextPlanCountsAsVisualProof: false,
            PhysicalDeviceProofSeparate: true,
            PhysicalDeviceProofPendingWithoutRealDeviceArtifacts: true,
            OfficialServerLiveAllowed: false,
            OfficialEndpointAllowed: false,
            OfficialSaveAllowed: false,
            OfficialEconomyAllowed: false,
            OfficialPersistentArmyAllowed: false,
            ProductionMigrationAllowed: false,
            ProductionDeploymentAllowed: false,
            RequiredArtifactFields:
            [
                "artifact_path",
                "artifact_kind",
                "dimensions",
                "caption",
                "local_demo",
                "physical_device",
                "official_server_live",
                "endpoint",
                "save",
                "economy",
                "army_persistence"
            ],
            ForbiddenVisualClaims:
            [
                "server live",
                "official endpoint",
                "official save",
                "official economy",
                "official persistent army",
                "physical device proof complete"
            ],
            ContractVersion.Current);
    }

    public static HiveServerLiveClaimVisualGuardQaCriteria CreateServerLiveClaimVisualGuardQaCriteria()
    {
        return new HiveServerLiveClaimVisualGuardQaCriteria(
            ServerLiveClaimVisualGuardEvidenceId,
            Criteria:
            [
                "Reject any screenshot, contact sheet, video or manifest claiming official server/live.",
                "Reject any screenshot, contact sheet, video or manifest claiming official endpoint.",
                "Reject any screenshot, contact sheet, video or manifest claiming official save.",
                "Reject any screenshot, contact sheet, video or manifest claiming official economy.",
                "Reject any screenshot, contact sheet, video or manifest claiming official persistent army.",
                "Reject any visual proof declared without a real image or video file.",
                "Reject any physical device proof closure without real device artifacts.",
                "Accept textual plans only as support, never as visual proof."
            ],
            DevOnly: true,
            NonLive: true,
            OfficialLiveClaimAllowed: false,
            ContractVersion.Current);
    }
}

public sealed record HiveOfficialPersistenceNonClaimGuard(
    string EvidenceId,
    bool DevOnly,
    bool NonLive,
    bool OfficialLiveServerClaimAllowed,
    bool OfficialEndpointClaimAllowed,
    bool OfficialSaveClaimAllowed,
    bool OfficialEconomyClaimAllowed,
    bool OfficialPersistentArmyClaimAllowed,
    bool ProductionMigrationAllowed,
    bool ProductionPublishAllowed,
    bool Bee881ScopeAllowed,
    bool WorldMapScopeAllowed,
    IReadOnlyList<string> RequiredLabels,
    IReadOnlyList<string> ForbiddenLabels,
    ContractVersion ContractVersion);

public sealed record HiveIdempotencyReplayEvidenceFieldSet(
    string EvidenceId,
    Guid ActionId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string ActionKind,
    string IdempotencyKeyLabel,
    string PayloadHashLabel,
    HiveFutureIdempotencyReplayDecision ReplayDecision,
    HiveActionLoopDevOnlyResponseStatus ResponseStatus,
    bool SamePayloadReturnedSameResult,
    bool DifferentPayloadRejectedAsConflict,
    bool CostAppliedOnce,
    bool QueueCreatedOnce,
    bool DevOnly,
    bool NonLive,
    bool OfficialProgressionApplied,
    bool OfficialEconomyApplied,
    ContractVersion ContractVersion);

public sealed record HiveSnapshotDeltaReconciliationEvidenceFieldSet(
    string EvidenceId,
    Guid EvidenceRunId,
    PlayerId PlayerId,
    WorldId WorldId,
    GameServerId GameServerId,
    string SnapshotVersion,
    long BeforeRevision,
    long AfterRevision,
    IReadOnlyList<HiveSnapshotDeltaEntryDevOnly> Deltas,
    HiveLocalServerReconciliationOutcome ReconciliationOutcome,
    HiveActionLoopDevOnlyErrorCode? ReconciliationError,
    bool DevOnly,
    bool NonLive,
    bool OfficialRestoreApplied,
    bool OfficialSaveApplied,
    bool OfficialEndpointUsed,
    ContractVersion ContractVersion);

public sealed record HiveActionLoopEvidenceQaChecklist(
    string EvidenceId,
    IReadOnlyList<string> Criteria,
    bool DevOnly,
    bool NonLive,
    bool OfficialQaClosureClaimAllowed,
    ContractVersion ContractVersion);

public sealed record HiveNonClaimEvidenceCarryForward(
    string EvidenceId,
    string SourceEvidenceId,
    string TargetDemo,
    string TargetQa,
    bool DevOnly,
    bool NonLive,
    bool OfficialLiveServerClaimAllowed,
    bool OfficialEndpointClaimAllowed,
    bool OfficialSaveClaimAllowed,
    bool OfficialEconomyClaimAllowed,
    bool OfficialPersistentArmyClaimAllowed,
    bool ProductionMigrationAllowed,
    bool ProductionDeploymentAllowed,
    IReadOnlyList<string> RequiredCarryForwardLabels,
    IReadOnlyList<string> RequiredQaChecks,
    ContractVersion ContractVersion);

public sealed record HiveIdempotencySnapshotEvidenceContinuity(
    string EvidenceId,
    string SourceEvidenceId,
    Guid EvidenceRunId,
    bool IdempotencyKeyLabelCarried,
    bool PayloadHashLabelCarried,
    bool SamePayloadReplayCarried,
    bool DifferentPayloadConflictCarried,
    bool CostAppliedOnceCarried,
    bool QueueCreatedOnceCarried,
    bool SnapshotVersionCarried,
    bool BeforeAfterRevisionCarried,
    bool DeltaListCarried,
    bool ReconciliationOutcomeCarried,
    bool DevOnly,
    bool NonLive,
    bool OfficialEndpointUsed,
    bool OfficialSaveApplied,
    ContractVersion ContractVersion);

public sealed record HivePreviewDemoLiveStateMatrix(
    string EvidenceId,
    IReadOnlyList<HivePreviewDemoLiveStateMatrixRow> Rows,
    bool DevOnly,
    bool NonLive,
    ContractVersion ContractVersion);

public sealed record HivePreviewDemoLiveStateMatrixRow(
    string Domain,
    bool LocalPreview,
    bool DemoProofAllowed,
    bool OfficialLiveAllowed,
    string RequiredLabel,
    string FalseClaimRisk);

public sealed record HiveServerFutureSupportNonClaimManifest(
    string EvidenceId,
    string TargetDemo,
    string TargetQa,
    bool LocalPreview,
    bool DemoProof,
    bool ApkTraceable,
    bool PhysicalDeviceProofPendingAllowed,
    bool OfficialServerLive,
    bool Endpoint,
    bool Save,
    bool Economy,
    bool ArmyPersistence,
    bool OfficialEndpointClaimAllowed,
    bool OfficialSaveClaimAllowed,
    bool OfficialEconomyClaimAllowed,
    bool OfficialPersistentArmyClaimAllowed,
    bool ProductionMigrationAllowed,
    bool ProductionDeploymentAllowed,
    IReadOnlyList<string> RequiredDemo076Fields,
    IReadOnlyList<string> ForbiddenDemo076Claims,
    ContractVersion ContractVersion);

public sealed record HiveServerFutureSupportQaLiveClaimChecklist(
    string EvidenceId,
    IReadOnlyList<string> Criteria,
    bool DevOnly,
    bool NonLive,
    bool OfficialLivePassAllowed,
    ContractVersion ContractVersion);

public sealed record HiveOfficialServerClaimBoundary(
    string EvidenceId,
    string TargetDemo,
    string TargetQa,
    bool LocalDemoProofAllowed,
    bool PhysicalDeviceProofSeparate,
    bool PhysicalDeviceProofPendingAllowed,
    bool OfficialServerLiveAllowed,
    bool OfficialEndpointAllowed,
    bool OfficialSaveAllowed,
    bool OfficialEconomyAllowed,
    bool OfficialPersistentArmyAllowed,
    bool ProductionMigrationAllowed,
    bool ProductionDeploymentAllowed,
    IReadOnlyList<string> RequiredManifestFields,
    IReadOnlyList<string> ForbiddenTerms,
    ContractVersion ContractVersion);

public sealed record HiveOfficialServerClaimBoundaryQaCriteria(
    string EvidenceId,
    IReadOnlyList<string> Criteria,
    bool DevOnly,
    bool NonLive,
    bool OfficialServerClaimAllowed,
    ContractVersion ContractVersion);

public sealed record HiveServerLiveClaimVisualGuard(
    string EvidenceId,
    string TargetDemo,
    string TargetQa,
    bool RequiresRealImageOrVideoArtifact,
    bool TextPlanCountsAsVisualProof,
    bool PhysicalDeviceProofSeparate,
    bool PhysicalDeviceProofPendingWithoutRealDeviceArtifacts,
    bool OfficialServerLiveAllowed,
    bool OfficialEndpointAllowed,
    bool OfficialSaveAllowed,
    bool OfficialEconomyAllowed,
    bool OfficialPersistentArmyAllowed,
    bool ProductionMigrationAllowed,
    bool ProductionDeploymentAllowed,
    IReadOnlyList<string> RequiredArtifactFields,
    IReadOnlyList<string> ForbiddenVisualClaims,
    ContractVersion ContractVersion);

public sealed record HiveServerLiveClaimVisualGuardQaCriteria(
    string EvidenceId,
    IReadOnlyList<string> Criteria,
    bool DevOnly,
    bool NonLive,
    bool OfficialLiveClaimAllowed,
    ContractVersion ContractVersion);
