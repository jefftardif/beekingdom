using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Help;

public enum AllianceHelpRequestStatus { Open, Completed, Expired, Cancelled }

// M045-CL: Alliance Help never maintains its own authoritative timer. OperationCategory +
// OperationTargetId are exactly the (category, targetId) pair OperationTimerReduction (in
// BeeKingdom.HiveOperations) already understands for the real Construction/Research/Training/
// Healing operation this request is attached to - that real operation stays the single source of
// truth for remaining duration. OperationId is the underlying HiveOperation/ResearchOperation/
// DoctrineTrainingOperation/BroodVitalityOperation id, kept here only for display/verification -
// never re-derived or duplicated as timing state.
public sealed record AllianceHelpRequest(
    Guid HelpRequestId,
    AllianceId AllianceId,
    PlayerId RequestingPlayerId,
    Guid RequestingHiveId,
    string OperationCategory,
    string OperationTargetId,
    Guid OperationId,
    DateTimeOffset CreatedAtUtc,
    AllianceHelpRequestStatus Status,
    long OriginalDurationSeconds,
    int HelpCount,
    int MaxHelpCount,
    long Revision,
    string ClientRequestId);

public sealed record AllianceHelpContribution(
    Guid HelpRequestId,
    PlayerId HelperPlayerId,
    DateTimeOffset HelpedAtUtc,
    long DurationReductionSeconds,
    string ClientRequestId);

public sealed record CreateAllianceHelpRequestCommand(Guid HiveId, string OperationCategory, string OperationTargetId, string ClientRequestId);

public sealed record AllianceHelpCommandResult(bool Succeeded, string Code, AllianceHelpRequest? Request);

public sealed record ContributeAllianceHelpResult(bool Succeeded, string Code, AllianceHelpRequest? Request, long? DurationReductionSeconds);

public sealed record ContributeAllianceHelpAllResult(IReadOnlyList<ContributeAllianceHelpResult> Results);

// Read-model for the Alliance Center "Aides" list: DisplayName resolved (never a raw GUID) and
// RemainingSeconds computed live against the real operation at read time - never a value cached on
// the request row itself, so a reduction from a DIFFERENT helper a moment ago is always reflected.
public sealed record AllianceHelpRequestView(
    Guid HelpRequestId,
    Guid RequestingPlayerId,
    string RequestingDisplayName,
    string OperationCategory,
    string OperationTargetId,
    long RemainingSeconds,
    int HelpCount,
    int MaxHelpCount,
    bool AlreadyHelpedByMe,
    DateTimeOffset CreatedAtUtc);

