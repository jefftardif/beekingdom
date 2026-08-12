namespace BeeKingdom.Protocol.Messages;

public enum ProtocolMessageType
{
    Request = 0,
    Response = 1,
    Command = 2,
    Event = 3,
    Notification = 4,
    Heartbeat = 5,
    Acknowledgement = 6,
    Error = 7
}
