namespace BeeKingdom.Chat.Models;

public enum ChatChannelType
{
    Alliance = 0,
    Server = 1,
    Private = 2,
    Leaders = 3,
    // RAP-OPTIONNEL-COMMUNICATIONS_01: player-created group rooms. Unlike the four fixed channels
    // above, a Group is NOT resolved by IChatAudienceResolver (which answers "who may be in THIS
    // kind of channel"); its membership is an explicit, mutable invite list owned by a Leader.
    Group = 4
}
