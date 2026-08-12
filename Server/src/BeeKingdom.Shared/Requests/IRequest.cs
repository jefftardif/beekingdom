using BeeKingdom.Shared.Contracts;

namespace BeeKingdom.Shared.Requests;

public interface IRequest : IContract
{
    Guid RequestId { get; }
    DateTimeOffset CreatedAtUtc { get; }
}
