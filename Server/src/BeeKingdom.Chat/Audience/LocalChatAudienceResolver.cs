using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Chat.Audience;

public sealed class LocalChatAudienceResolver : IChatAudienceResolver
{
    private readonly ChatOptions options;
    private readonly IAllianceMembershipResolver allianceMembership;

    public LocalChatAudienceResolver(IOptions<ChatOptions> options, IAllianceMembershipResolver? allianceMembership = null)
    {
        this.options = options.Value;
        // Optional constructor parameter (defaults to fail-closed) so every existing call site
        // that doesn't care about alliance channels - and every non-DI test that constructs this
        // directly - keeps compiling without having to know about alliance membership at all.
        this.allianceMembership = allianceMembership ?? new NullAllianceMembershipResolver();
    }

    public ChatAudienceDecision ResolveConversationAccess(PlayerId requester, CreateChatConversationRequest request)
    {
        // M042-CL: RequesterAllianceRole is still accepted on the wire (kept in
        // CreateChatConversationRequest for backward compatibility with older clients still
        // sending it) but is no longer trusted for Alliance/Leaders channels - the server derives
        // the real role from IAllianceMembershipResolver instead. It's still used as-is for
        // Private (where it's irrelevant to security, only a display fallback) and Server (not
        // read at all).
        ChatPermissionRole? requesterRole = ResolveStagingRole(request.RequesterAllianceRole);

        return request.ChannelType switch
        {
            ChatChannelType.Server => ChatAudienceDecision.Allow(ChatPermissionRole.Member, [requester]),
            ChatChannelType.Private => ResolvePrivate(requester, request, requesterRole ?? ChatPermissionRole.Member),
            ChatChannelType.Alliance => ResolveAlliance(requester, request, allianceMembership),
            ChatChannelType.Leaders => ResolveLeaders(requester, request, allianceMembership),
            _ => ChatAudienceDecision.Deny("channel_type_invalid")
        };
    }

    public ChatAudienceDecision ResolveAnnouncementAccess(PlayerId requester, Guid allianceId, CreateAllianceAnnouncementRequest request)
    {
        ChatPermissionRole? requesterRole = allianceMembership.GetMemberRole(allianceId, requester.Value);
        if (requesterRole == null || !IsAllianceLeaderRole(requesterRole.Value))
        {
            return ChatAudienceDecision.Deny("alliance_leader_role_required");
        }

        IReadOnlyList<PlayerId> participants = BuildParticipantList(requester, request.MemberPlayerIds);
        return ChatAudienceDecision.Allow(requesterRole.Value, participants);
    }

    private ChatAudienceDecision ResolvePrivate(PlayerId requester, CreateChatConversationRequest request, ChatPermissionRole requesterRole)
    {
        if (request.ParticipantIds == null || request.ParticipantIds.Count == 0 || request.ParticipantIds.Count > options.MaxPrivateRecipients)
        {
            return ChatAudienceDecision.Deny("private_participants_invalid");
        }

        return ChatAudienceDecision.Allow(requesterRole, BuildParticipantList(requester, request.ParticipantIds));
    }

    private static ChatAudienceDecision ResolveAlliance(PlayerId requester, CreateChatConversationRequest request, IAllianceMembershipResolver allianceMembership)
    {
        if (!TryParseAllianceIdFromAudience(request.AudienceKey, "alliance:", out Guid allianceId))
        {
            return ChatAudienceDecision.Deny(string.IsNullOrWhiteSpace(request.AudienceKey) ? "alliance_audience_required" : "alliance_audience_invalid");
        }

        ChatPermissionRole? requesterRole = allianceMembership.GetMemberRole(allianceId, requester.Value);
        return requesterRole.HasValue && IsAllianceMemberRole(requesterRole.Value)
            ? ChatAudienceDecision.Allow(requesterRole.Value, BuildParticipantList(requester, request.ParticipantIds ?? Array.Empty<Guid>()))
            : ChatAudienceDecision.Deny("alliance_membership_required");
    }

    private static ChatAudienceDecision ResolveLeaders(PlayerId requester, CreateChatConversationRequest request, IAllianceMembershipResolver allianceMembership)
    {
        if (!TryParseAllianceIdFromAudience(request.AudienceKey, "leaders:", out Guid allianceId))
        {
            return ChatAudienceDecision.Deny(string.IsNullOrWhiteSpace(request.AudienceKey) ? "leaders_audience_required" : "leaders_audience_invalid");
        }

        ChatPermissionRole? requesterRole = allianceMembership.GetMemberRole(allianceId, requester.Value);
        return requesterRole.HasValue && IsAllianceLeaderRole(requesterRole.Value)
            ? ChatAudienceDecision.Allow(requesterRole.Value, BuildParticipantList(requester, request.ParticipantIds ?? Array.Empty<Guid>()))
            : ChatAudienceDecision.Deny("alliance_leader_role_required");
    }

    private static bool TryParseAllianceIdFromAudience(string? audienceKey, string prefix, out Guid allianceId)
    {
        allianceId = Guid.Empty;
        if (!AudienceStartsWith(audienceKey, prefix)) return false;
        string idPart = audienceKey!.Trim()[prefix.Length..];
        return Guid.TryParseExact(idPart, "N", out allianceId) || Guid.TryParse(idPart, out allianceId);
    }

    private static IReadOnlyList<PlayerId> BuildParticipantList(PlayerId requester, IEnumerable<Guid> participantIds)
    {
        return participantIds
            .Append(requester.Value)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Select(id => new PlayerId(id))
            .ToArray();
    }

    private static bool AudienceStartsWith(string? audienceKey, string prefix)
    {
        return !string.IsNullOrWhiteSpace(audienceKey)
            && audienceKey.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllianceMemberRole(ChatPermissionRole role)
    {
        return role is ChatPermissionRole.Member or ChatPermissionRole.Officer or ChatPermissionRole.Leader;
    }

    private static bool IsAllianceLeaderRole(ChatPermissionRole role)
    {
        return role is ChatPermissionRole.Officer or ChatPermissionRole.Leader;
    }

    private static ChatPermissionRole? ResolveStagingRole(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "officer" => ChatPermissionRole.Officer,
            "leader" => ChatPermissionRole.Leader,
            "moderator" => ChatPermissionRole.Moderator,
            "member" => ChatPermissionRole.Member,
            _ => null
        };
    }
}
