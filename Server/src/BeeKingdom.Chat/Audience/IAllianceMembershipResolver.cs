using BeeKingdom.Chat.Models;

namespace BeeKingdom.Chat.Audience;

// M042-CL: dependency-inversion seam so LocalChatAudienceResolver can ask "what is this player's
// REAL, server-authoritative role in this alliance?" without BeeKingdom.Chat taking a project
// reference on BeeKingdom.Alliance (which already references BeeKingdom.Chat to create/link the
// alliance conversation - a reference the other way would be circular). BeeKingdom.Alliance
// provides the real implementation (wrapping IAllianceRepository.GetActiveMembership) and
// registers it in DI; BeeKingdom.Chat only depends on this interface and ships a safe fail-closed
// default (NullAllianceMembershipResolver) so the chat module still compiles/runs standalone.
public interface IAllianceMembershipResolver
{
    // Returns the requester's real role in that alliance, or null if they are not an active
    // member (or the alliance/membership system isn't wired up) - null must always mean "deny",
    // never "fall back to trusting the client".
    ChatPermissionRole? GetMemberRole(Guid allianceId, Guid playerId);
}

// Fail-closed default: until BeeKingdom.Alliance's real resolver is registered on top of this
// one, every alliance/leaders channel access is denied rather than silently trusting a
// client-declared role.
public sealed class NullAllianceMembershipResolver : IAllianceMembershipResolver
{
    public ChatPermissionRole? GetMemberRole(Guid allianceId, Guid playerId) => null;
}
