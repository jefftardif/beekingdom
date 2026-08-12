namespace BeeKingdom.Chat.Models;

public enum ChatMessageState
{
    Queued = 0,
    Accepted = 1,
    Delivered = 2,
    Failed = 3,
    Hidden = 4,
    Deleted = 5,
    Expired = 6
}
