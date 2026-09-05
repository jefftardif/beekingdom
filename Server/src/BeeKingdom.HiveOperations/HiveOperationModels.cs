namespace BeeKingdom.HiveOperations;

public enum HiveOperationStatus { Running, AwaitingCollection, Collected }
public enum HiveOperationKind { BuildingUpgrade, Training, Production }

public sealed record ResourceBalance(long Amount, long Capacity);

public sealed record HiveOperation(
    Guid OperationId, string BuildingKey, int FromLevel, int ToLevel,
    DateTimeOffset StartedAtUtc, DateTimeOffset CompletesAtUtc,
    HiveOperationStatus Status, string ProducedResourceKey, long ProducedAmount,
    DateTimeOffset? CollectedAtUtc, HiveOperationKind Kind = HiveOperationKind.BuildingUpgrade);

public sealed record PlayerHiveState(
    Guid PlayerId, Guid HiveId, int ModelVersion, long Revision,
    Dictionary<string, ResourceBalance> Resources,
    Dictionary<string, int> BuildingLevels,
    List<HiveOperation> Operations,
    Dictionary<string, IdempotencyReceipt> Receipts,
    TutorialProgress? Tutorial = null,
    Dictionary<string, RewardState>? Rewards = null,
    bool InstallationComplete = false,
    FoundationDotationState? FoundationDotation = null,
    Chapter1CertificationState? Chapter1Certification = null,
    BroodVitalityState? BroodVitality = null,
    WorkshopBatchQualificationState? WorkshopBatchQualification = null,
    HiveResearchState? Research = null,
    HiveDailyRoundState? DailyRound = null,
    StrategicPathState? StrategicPath = null,
    DoctrineRosterState? DoctrineRoster = null,
    SquadReservationState? SquadReservation = null,
    HivePerimeterSortieState? HivePerimeterSortie = null,
    HiveOfflineProductionState? OfflineProduction = null,
    Dictionary<string, HiveDailyRoundStoredReceipt>? DailyRoundReceipts = null,
    Dictionary<string, BroodCareStoredReceipt>? BroodCareReceipts = null,
     CombatPatrolState? CombatPatrol = null,
     IReadOnlyList<AdminAuditEntry>? AdminAudit = null,
     ChampionBeeProgressState? ChampionBees = null,
     TroopTierState? TroopTierProgress = null,
     VipProgressState? Vip = null,
     WorldResourceCollectionState? WorldResourceCollection = null,
     HiveMilestoneEventState? MilestoneEvent = null,
     BestiaryCodexState? BestiaryCodex = null,
     bool ImplicitBuildingDefaultsApplied = false,
     Dictionary<string, int>? SpeedUps = null,
     RewardLedgerState? RewardLedger = null,
     // M054-CL: the player's own persistent "Sceaux Royaux" (Royal Seals) wallet balance - see
     // BeeKingdom.HiveOperations.RoyalSealsWallet for the canonical read/credit surface. Lives here
     // (not in any Alliance-owned or Alliance-membership-scoped state) because PlayerHiveState is
     // this codebase's own established "durable player-owned bucket" convention: VIP progress,
     // Champion Bee progression, and the SpeedUps inventory already live here despite none of them
     // being hive-mechanics per se, precisely because every player has exactly one hive row in the
     // live game today (no hive-creation endpoint exists; ListHiveIdsAsync exists only for ownership
     // validation - see the M054 report section 3 for the full evidence trail). RoyalSealsWallet
     // still defensively sums across every owned hive on read, so the balance stays correct even in
     // the theoretical case a second hive ever appears, without requiring a schema change then.
     long RoyalSeals = 0);

// Append-only trail of manual admin/support mutations against a hive (resource/roster
// adjustments, compensation slot grants). Written inside the SAME atomic mutation as the
// change it documents, so it can never drift out of sync with what actually happened.
public sealed record AdminAuditEntry(Guid EntryId, DateTimeOffset AtUtc, string Action, string Details, string Reason);
    
    

public sealed record TutorialProgress(string ChapterKey, string SafeResumeStepKey, string LastObservedStepKey, DateTimeOffset UpdatedAtUtc);
public sealed record Chapter1CertificationState(string StepKey, long Revision, DateTimeOffset AcceptedAtUtc, string? FinalProof);
public sealed record BroodVitalityState(int Nutrition, int Stability, long Revision, DateTimeOffset UpdatedAtUtc, BroodVitalityOperation? ActiveOperation);
public sealed record BroodVitalityOperation(Guid OperationId, string Type, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc);
public static class BroodVitalityOperationTypes { public const string Feeding = "feeding"; public const string Stabilization = "stabilization"; public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal) { Feeding, Stabilization }; }
public sealed record RewardState(string RewardKey, string ResourceKey, long Amount, bool Claimed, DateTimeOffset? ClaimedAtUtc);

// Ledger des recompenses et evenements (demande de Jeff, 2026-08-09) : le pipeline de settlement
// server-authoritative qui rendait les collections `Rewards`/`Events` de la reponse SpeedUp
// effectives. Chaque octroi cree une entree de ledger (source, notification, montant) ET une
// recompense claimable dans `Rewards`; la reclamation (HiveOperationService.ClaimRewardAsync)
// synchronise l'entree (Claimed, CreditedAmount, ClaimedAtUtc) et append un evenement
// `reward_claimed`. La completion des files (status AwaitingCollection) est recensee une seule
// fois par operation (SettledOperationIds) comme evenement `queue_completed`. Tout est ecrit
// dans la MEME mutation atomique que le changement documente - jamais de desynchronisation.
public sealed record RewardLedgerEntry(string RewardKey, string Source, string ResourceKey, long Amount, long CreditedAmount, bool Claimed, DateTimeOffset GrantedAtUtc, DateTimeOffset? ClaimedAtUtc, string? NotificationKey);
public sealed record RewardLedgerEvent(string EventKey, string TargetKey, DateTimeOffset AtUtc);
public sealed record RewardLedgerState(long Revision, Dictionary<string, RewardLedgerEntry> Entries, List<RewardLedgerEvent> Events, HashSet<string> SettledOperationIds, Dictionary<string, IdempotencyReceipt> Receipts);
public sealed record GrantRewardCommand(Guid PlayerId, Guid HiveId, string RewardKey, string Source, string ResourceKey, long Amount, long ExpectedRevision, string IdempotencyKey, string? NotificationKey = null);
public sealed record RewardLedgerEntryReadModel(string RewardKey, string Source, string ResourceKey, long Amount, long CreditedAmount, bool Claimed, string? NotificationKey);
public sealed record RewardLedgerEventReadModel(string EventKey, string TargetKey, DateTimeOffset AtUtc);
public sealed record RewardLedgerReadSnapshot(Guid PlayerId, Guid HiveId, string ContractVersion, long Revision, DateTimeOffset ServerTimeUtc, IReadOnlyList<RewardLedgerEntryReadModel> Rewards, IReadOnlyList<RewardLedgerEventReadModel> Events);
public sealed record RewardLedgerCommandResult(bool Succeeded, string Code, RewardLedgerReadSnapshot Snapshot);
public sealed record FoundationDotationState(string Choice, long HoneyAwarded, long PollenAwarded, string Proof, DateTimeOffset ClaimedAtUtc);
public sealed record WorkshopBatchQualificationState(string Specialization, long CollectedAmount, string StepKey, long Revision);
public sealed record HiveResearchState(Dictionary<string, ResearchCompletion> Completed, ResearchOperation? ActiveOperation);
public sealed record ResearchCompletion(string ResearchId, DateTimeOffset CompletedAtUtc, ResearchEffects Effects);
public sealed record HiveOfflineProductionState(DateTimeOffset ProductionAsOfUtc, Dictionary<string, decimal> PendingByBuilding, long Revision, Dictionary<string, OfflineProductionStoredReceipt> Receipts);
public sealed record ResearchOperation(Guid OperationId, string ResearchId, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc, long Revision);
public sealed record HiveDailyRoundState(DateTimeOffset DayUtc, bool CollectionReceived, bool OperationLaunched, bool SnapshotRead, DateTimeOffset? ClaimedAtUtc);
public sealed record HiveDailyRoundStoredReceipt(string PayloadHash, bool Succeeded, DateTimeOffset DayUtc, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, long CreditedHoney, long CreditedPollen, string Code);
public sealed record BroodCareStoredReceipt(string PayloadHash, bool Succeeded, string Type, Guid OperationId, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, string Code);
public sealed record StrategicPathState(string CatalogVersion, string? SelectedPath, long Revision, DateTimeOffset UpdatedAtUtc, Dictionary<string, IdempotencyReceipt> Receipts);
public sealed record StrategicPathSnapshot(Guid PlayerId, Guid HiveId, string CatalogVersion, IReadOnlyList<string> CanonicalPaths, string? SelectedPath, long Revision, DateTimeOffset UpdatedAtUtc);
public sealed record DoctrineTrainingOperation(Guid OperationId, string Family, int BatchSize, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc, long Revision, string IdempotencyKey, string PayloadHash, bool Claimed);
public sealed record DoctrineRosterState(long Revision, Dictionary<string, long> Counts, DoctrineTrainingOperation? ActiveOperation, Dictionary<string, IdempotencyReceipt> Receipts);
public sealed record SquadReservationState(long Revision, int Capacity, Dictionary<string, long> Reserved, string? ReservationId, Dictionary<string, IdempotencyReceipt> Receipts);
public sealed record HivePerimeterSortieState(long Revision, DateTimeOffset CycleStartedAtUtc, DateTimeOffset CycleEndsAtUtc, HivePerimeterActiveSortie? Active, Dictionary<string, IdempotencyReceipt> Receipts, HashSet<string>? CompletedSignalKeys = null, Dictionary<string, HivePerimeterClaimReceipt>? ClaimReceipts = null);
public sealed record HivePerimeterActiveSortie(Guid SortieId, string SignalKey, string SignalInstanceId, string ReservationId, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc, long Revision, string LaunchIdempotencyKey, string PayloadHash);
public sealed record HivePerimeterClaimReceipt(Guid PlayerId, Guid HiveId, Guid SortieId, string SignalKey, string SignalInstanceId, DateTimeOffset CycleStartedAtUtc, DateTimeOffset CycleEndsAtUtc, long Revision, DateTimeOffset ServerTimeUtc, Dictionary<string, long> CreditedByResource, Dictionary<string, ResourceBalance> ResultingBalances);
public sealed record DailyRoundCommandResult(bool Succeeded, string Code, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, PlayerHiveState State);
public sealed record CombatPatrolActiveEncounter(Guid EncounterId, int Tier, Dictionary<string, long> CommittedTroops, DateTimeOffset StartedAtUtc, DateTimeOffset EndsAtUtc, string LaunchIdempotencyKey, string PayloadHash);
public sealed record CombatPatrolClaimReceipt(Guid PlayerId, Guid HiveId, Guid EncounterId, int Tier, string Band, DateTimeOffset ServerTimeUtc, Dictionary<string, long> PermanentLosses, Dictionary<string, long> WoundedLosses, Dictionary<string, long> CreditedByResource, Dictionary<string, ResourceBalance> ResultingBalances, List<string> ContributingChampionBeeIds, Dictionary<string, long> ChampionPowerBonusBpByFamily, Dictionary<string, int> TroopTierByFamily, Dictionary<string, long> TroopPowerBonusBpByFamily, long AvailablePower, long RequiredPower, long ReadinessBp, string? StrategicPathId, Dictionary<string, long> StrategicPathPowerBonusBpByFamily, bool DailyFocusApplied = false, bool WorldEventApplied = false, string WorldEventKey = "");
public sealed record CombatPatrolRecoveringBatch(string Family, long Count, DateTimeOffset ReadyAtUtc);
public sealed record CombatPatrolState(long Revision, List<CombatPatrolActiveEncounter> ActiveEncounters, Dictionary<int, DateTimeOffset> TierCooldownEndsAtUtc, Dictionary<string, IdempotencyReceipt> Receipts, Dictionary<string, CombatPatrolClaimReceipt>? ClaimReceipts = null, List<CombatPatrolRecoveringBatch>? Recovering = null, int ResourcePurchasedSlots = 0, int PremiumPurchasedSlots = 0);

// Carnet du Bestiaire (demande de Jeff, 2026-08-01 - Game Design valide separement) : l'histoire
// personnelle du joueur avec chaque palier de creature, construite entierement a partir de donnees
// deja produites par le flux de reclamation Combat Patrol existant (voir CombatPatrolService.FinishAsync)
// - aucune nouvelle commande joueur, aucun nouveau risque de confiance. Suit par TIER (1-7), pas par
// variante cosmetique : le serveur n'a jamais connu et ne connaitra jamais la variante (purement
// client, voir WorldBestiaryNode.Variant) - "Apercue" par variante reste un etat client-local.
public sealed record BestiaryCodexTierState(
    int Tier, long EncounterCount, string BestBand, bool Mastered, bool Legendary,
    DateTimeOffset FirstEncounteredAtUtc, DateTimeOffset LastEncounteredAtUtc,
    long TotalHoneyCredited, long TotalPollenCredited, long DailyFocusEncounterCount,
    List<string> LastContributingChampionBeeIds, string? LastStrategicPathId,
    long LastHoneyCredited = 0, long LastPollenCredited = 0, DateTimeOffset? BestBandAchievedAtUtc = null,
    string LastBand = "");
public sealed record BestiaryCodexState(Dictionary<int, BestiaryCodexTierState> Tiers);
public sealed record ClaimHiveDailyRoundCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, string IdempotencyKey, string ExpectedDayUtc = "");

public sealed record ChampionBeeProgressState(Dictionary<string, int> Levels, List<string> AssignedBeeIds);
public sealed record TroopTierState(Dictionary<string, int> Tiers);
public sealed record ChampionBeeCommandResult(bool Succeeded, string Code, string BeeId, int Level, IReadOnlyList<string> AssignedBeeIds, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, PlayerHiveState State);
public sealed record TroopTierCommandResult(bool Succeeded, string Code, string PopulationId, int Tier, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, PlayerHiveState State);
public sealed record GrantChampionBeeCommand(Guid PlayerId, Guid HiveId, string BeeId, long ExpectedRevision, string IdempotencyKey);
public sealed record LevelUpChampionBeeCommand(Guid PlayerId, Guid HiveId, string BeeId, long ExpectedRevision, string IdempotencyKey);
public sealed record SetChampionBeeAssignmentCommand(Guid PlayerId, Guid HiveId, IReadOnlyList<string> BeeIds, long ExpectedRevision, string IdempotencyKey);
public sealed record PromoteTroopTierCommand(Guid PlayerId, Guid HiveId, string PopulationId, long ExpectedRevision, string IdempotencyKey);

public sealed record VipProgressState(long LifetimePoints);
public sealed record VipCommandResult(bool Succeeded, string Code, long LifetimePoints, int Level, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, PlayerHiveState State);
public sealed record GrantVipPointsCommand(Guid PlayerId, Guid HiveId, long Points, long ExpectedRevision, string IdempotencyKey, string Source);

public sealed record IdempotencyReceipt(string PayloadHash, bool Succeeded, string Code, Guid? OperationId, DateTimeOffset CreatedAtUtc,
    long? RevisionBefore = null, long? RevisionAfter = null, string? PreviousStep = null, string? ResultingStep = null, string? Answer = null, DateTimeOffset? AcceptedAtUtc = null);

public sealed record StartBuildingOperationCommand(
    Guid PlayerId, Guid HiveId, string BuildingKey, int ExpectedLevel,
    long ExpectedRevision, string IdempotencyKey);

public sealed record CollectBuildingOperationCommand(
    Guid PlayerId, Guid HiveId, Guid OperationId, long ExpectedRevision,
    string IdempotencyKey);

public sealed record SaveTutorialProgressCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, string ChapterKey, string SafeResumeStepKey, string LastObservedStepKey, string IdempotencyKey);
public sealed record CertifyChapter1StepCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, string StepKey, string IdempotencyKey);
public sealed record ClaimRewardCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, string RewardKey, string IdempotencyKey);
public sealed record ClaimFoundationDotationCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, string Choice, string IdempotencyKey);
public sealed record QualifyWorkshopBatchCommand(Guid PlayerId, Guid HiveId, long ExpectedRevision, string Answer, string IdempotencyKey);
public sealed record StartResearchCommand(Guid PlayerId, Guid HiveId, string ResearchId, long ExpectedRevision, string IdempotencyKey);
public sealed record CompleteResearchCommand(Guid PlayerId, Guid HiveId, Guid OperationId, long ExpectedRevision, string IdempotencyKey);
public sealed record StartQueuedOperationCommand(Guid PlayerId, Guid HiveId, string OperationKey, long ExpectedRevision, string IdempotencyKey);

public sealed record BuildingOperationDefinition(
    string BuildingKey, int FromLevel, int ToLevel, TimeSpan Duration,
    IReadOnlyDictionary<string, long> Costs, string ProducedResourceKey,
    long ProducedAmount);

public sealed record QueuedOperationDefinition(
    string OperationKey, HiveOperationKind Kind, string TargetKey,
    TimeSpan Duration, IReadOnlyDictionary<string, long> Costs,
    string ResultKey, long ResultAmount);

public sealed record HiveCommandResult(bool Succeeded, string Code, PlayerHiveState State, Guid? OperationId = null);
public sealed record WorkshopBatchQualificationResult(bool Succeeded, string Code, string PreviousStep, string ResultingStep, string Answer, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, PlayerHiveState State);
public sealed record ResearchCommandResult(bool Succeeded, string Code, string ResearchId, Guid? OperationId, long RevisionBefore, long RevisionAfter, DateTimeOffset AcceptedAtUtc, PlayerHiveState State);

public interface IServerClock { DateTimeOffset UtcNow { get; } }
public sealed class SystemServerClock : IServerClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }

public interface IHiveStateRepository
{
    Task<PlayerHiveState> ExecuteAtomicallyAsync(Guid playerId, Guid hiveId, Func<PlayerHiveState, PlayerHiveState> mutation, CancellationToken cancellationToken = default);
    Task<PlayerHiveState?> ReadAsync(Guid playerId, Guid hiveId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> ListHiveIdsAsync(Guid playerId, CancellationToken cancellationToken = default);
    // Monde vivant (demande de Jeff, 2026-08-01) : un echantillon borne des hives recemment
    // modifiees, tous joueurs confondus - seule maniere honnete de savoir "qui est actif la` "
    // sans introduire un registre/index dedie. Uniquement lecture, jamais utilise pour muter
    // l'etat d'un autre joueur.
    Task<IReadOnlyList<PlayerHiveState>> ListRecentlyActiveAsync(int limit, CancellationToken cancellationToken = default);
}
