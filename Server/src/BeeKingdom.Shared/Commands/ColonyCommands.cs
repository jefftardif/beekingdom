using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Commands;

public sealed record BuildStructureCommand(
    Guid CommandId,
    DateTimeOffset CreatedAtUtc,
    ColonyId ColonyId,
    string BuildingDefinitionId,
    HexCoordinate Position,
    ContractVersion ContractVersion) : ICommand;

public sealed record UpgradeBuildingCommand(
    Guid CommandId,
    DateTimeOffset CreatedAtUtc,
    ColonyId ColonyId,
    BuildingId BuildingId,
    ContractVersion ContractVersion) : ICommand;

public sealed record MoveBeeCommand(
    Guid CommandId,
    DateTimeOffset CreatedAtUtc,
    ColonyId ColonyId,
    BeeId BeeId,
    HexCoordinate Destination,
    ContractVersion ContractVersion) : ICommand;

public sealed record JoinAllianceCommand(
    Guid CommandId,
    DateTimeOffset CreatedAtUtc,
    PlayerId PlayerId,
    AllianceId AllianceId,
    ContractVersion ContractVersion) : ICommand;
