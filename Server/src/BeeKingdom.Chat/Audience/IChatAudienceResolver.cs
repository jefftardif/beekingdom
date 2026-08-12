using BeeKingdom.Chat.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat.Audience;

public interface IChatAudienceResolver
{
    ChatAudienceDecision ResolveConversationAccess(PlayerId requester, CreateChatConversationRequest request);
    ChatAudienceDecision ResolveAnnouncementAccess(PlayerId requester, Guid allianceId, CreateAllianceAnnouncementRequest request);
}
