using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Events;

public sealed record BuildingCompleted(Guid EventId, DateTimeOffset OccurredAtUtc, ColonyId ColonyId, BuildingId BuildingId, ContractVersion ContractVersion) : IDomainEvent;

public sealed record BeeBorn(Guid EventId, DateTimeOffset OccurredAtUtc, ColonyId ColonyId, BeeId BeeId, ContractVersion ContractVersion) : IDomainEvent;

public sealed record QueenDied(Guid EventId, DateTimeOffset OccurredAtUtc, ColonyId ColonyId, BeeId QueenId, ContractVersion ContractVersion) : IDomainEvent;

public sealed record ColonyExpanded(Guid EventId, DateTimeOffset OccurredAtUtc, ColonyId ColonyId, int NewChamberCount, ContractVersion ContractVersion) : IDomainEvent;

public sealed record AllianceCreated(Guid EventId, DateTimeOffset OccurredAtUtc, AllianceId AllianceId, PlayerId FounderId, ContractVersion ContractVersion) : IDomainEvent;
