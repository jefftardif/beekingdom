using BeeKingdom.Shared.Contracts;

namespace BeeKingdom.Shared.Commands;

public interface ICommand : IContract
{
    Guid CommandId { get; }
    DateTimeOffset CreatedAtUtc { get; }
}
