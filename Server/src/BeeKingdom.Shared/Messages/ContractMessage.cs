using BeeKingdom.Shared.Contracts;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Messages;

public sealed record ContractMessage<TContract>(
    Guid MessageId,
    DateTimeOffset CreatedAtUtc,
    string MessageType,
    TContract Payload,
    ContractVersion ContractVersion)
    where TContract : IContract;
