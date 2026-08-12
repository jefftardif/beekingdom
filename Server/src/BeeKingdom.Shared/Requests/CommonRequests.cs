using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Requests;

public sealed record GetPlayerRequest(Guid RequestId, DateTimeOffset CreatedAtUtc, PlayerId PlayerId, ContractVersion ContractVersion) : IRequest;

public sealed record GetColonyRequest(Guid RequestId, DateTimeOffset CreatedAtUtc, ColonyId ColonyId, ContractVersion ContractVersion) : IRequest;

public sealed record GetAllianceRequest(Guid RequestId, DateTimeOffset CreatedAtUtc, AllianceId AllianceId, ContractVersion ContractVersion) : IRequest;
