using System;
using System.Collections.Generic;

namespace BeeKingdom.Gameplay.Communication
{
    // RAP-OPTIONNEL-COMMUNICATIONS_01 - client mirror of the server's player-created group contracts
    // (Server/src/BeeKingdom.Chat/Models/ChatContracts.cs). Display names are already resolved by the
    // server, so nothing here needs a second round trip to the player directory to be rendered - the
    // future web client consumes the exact same payloads.

    public static class RemoteChatGroupInviteStatus
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Declined = "Declined";
        public const string Cancelled = "Cancelled";
    }

    public static class RemoteChatAutoInviteResponse
    {
        public const string Ask = "Ask";
        public const string Accept = "Accept";
        public const string Decline = "Decline";

        public static bool IsKnown(string value) => string.Equals(value, Ask, StringComparison.Ordinal)
            || string.Equals(value, Accept, StringComparison.Ordinal)
            || string.Equals(value, Decline, StringComparison.Ordinal);
    }

    public sealed class RemoteChatGroupMember
    {
        public string PlayerId { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public bool IsLeader { get; set; }
    }

    public sealed class RemoteChatGroupInvite
    {
        public string InviteId { get; set; }
        public string ConversationId { get; set; }
        public string GroupTitle { get; set; }
        public string InviterPlayerId { get; set; }
        public string InviterDisplayName { get; set; }
        public string InviteePlayerId { get; set; }
        public string InviteeDisplayName { get; set; }
        public string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public sealed class RemoteChatGroupDetail
    {
        public string ConversationId { get; set; }
        public string Title { get; set; }
        public string OwnerPlayerId { get; set; }
        public bool ViewerIsLeader { get; set; }
        public List<RemoteChatGroupMember> Members { get; } = new List<RemoteChatGroupMember>();
        public List<RemoteChatGroupInvite> PendingInvites { get; } = new List<RemoteChatGroupInvite>();
    }

    // JsonUtility cannot deserialize a top-level JSON array, so array responses are wrapped by the
    // codec into this envelope before parsing. Kept explicit rather than hidden in the codec so the
    // provider's method signatures stay honest about what comes back.
    public sealed class RemoteChatGroupInviteList
    {
        public List<RemoteChatGroupInvite> Items { get; } = new List<RemoteChatGroupInvite>();
    }

    public sealed class RemoteChatPreferences
    {
        public string AutoInviteResponse { get; set; }
    }

    public sealed class RemoteChatGroupLeaveResult
    {
        public bool Left { get; set; }
    }

    public sealed class RemoteChatInviteAcknowledgeResult
    {
        public int Acknowledged { get; set; }
    }

    public sealed class RemoteCreateGroupRequest
    {
        public string GameServerId { get; set; }
        public string WorldId { get; set; }
        public string Title { get; set; }
        public IReadOnlyList<string> InviteePlayerIds { get; set; } = Array.Empty<string>();
        public string ClientRequestId { get; set; }
    }

    // The remove/leave endpoints take no payload, but the REST transport refuses a POST without a
    // body (a bodyless POST is almost always a bug elsewhere in this codebase), so those calls send
    // an explicit empty object instead of weakening that guard.
    public sealed class RemoteEmptyChatRequest { }

    public sealed class RemoteInviteToGroupRequest { public string InviteePlayerId { get; set; } }
    public sealed class RemoteRespondToGroupInviteRequest { public bool Accept { get; set; } }
    public sealed class RemoteTransferGroupLeadershipRequest { public string NewLeaderPlayerId { get; set; } }
    public sealed class RemoteUpdateChatPreferencesRequest { public string AutoInviteResponse { get; set; } }
    public sealed class RemoteAcknowledgeChatInvitationsRequest { public IReadOnlyList<string> InviteIds { get; set; } = Array.Empty<string>(); }
}
