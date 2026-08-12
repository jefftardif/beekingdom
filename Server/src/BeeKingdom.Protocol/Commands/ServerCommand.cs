namespace BeeKingdom.Protocol.Commands;

public interface IServerCommand
{
    Guid CommandId { get; }
}
