using BeeKingdom.Shared.Contracts;
using BeeKingdom.Shared.Enums;

namespace BeeKingdom.Shared.Responses;

public interface IResponse : IContract
{
    Guid RequestId { get; }
    ResponseStatus Status { get; }
    IReadOnlyList<ContractError> Errors { get; }
}

public sealed record ContractError(string Code, string Message, string? Field = null);
