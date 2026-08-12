using BeeKingdom.Shared.Enums;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.Responses;

public sealed record ResponseEnvelope<TPayload>(
    Guid RequestId,
    ResponseStatus Status,
    TPayload? Payload,
    IReadOnlyList<ContractError> Errors,
    ContractVersion ContractVersion) : IResponse
{
    public static ResponseEnvelope<TPayload> Success(Guid requestId, TPayload payload)
    {
        return new ResponseEnvelope<TPayload>(requestId, ResponseStatus.Success, payload, Array.Empty<ContractError>(), ContractVersion.Current);
    }

    public static ResponseEnvelope<TPayload> Failure(Guid requestId, ResponseStatus status, IReadOnlyList<ContractError> errors)
    {
        return new ResponseEnvelope<TPayload>(requestId, status, default, errors, ContractVersion.Current);
    }
}
