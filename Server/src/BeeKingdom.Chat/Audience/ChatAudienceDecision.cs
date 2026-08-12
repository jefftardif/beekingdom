using BeeKingdom.Chat.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat.Audience;

public sealed record ChatAudienceDecision(
    bool Allowed,
    ChatPermissionRole RequesterRole,
    IReadOnlyList<PlayerId> Participants,
    string? ReasonCode)
{
    public static ChatAudienceDecision Allow(ChatPermissionRole requesterRole, IReadOnlyList<PlayerId> participants)
    {
        return new ChatAudienceDecision(true, requesterRole, participants, null);
    }

    public static ChatAudienceDecision Deny(string reasonCode)
    {
        return new ChatAudienceDecision(false, ChatPermissionRole.Member, Array.Empty<PlayerId>(), reasonCode);
    }
}
