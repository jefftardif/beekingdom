using System;
using System.Linq;

namespace BeeKingdom.Gameplay.Communication
{
#pragma warning disable 0649 // JsonUtility renseigne ces champs par reflexion.
    // RAP-OPTIONNEL-COMMUNICATIONS_01 - wire mapping for the group/invitation/preference endpoints.
    // Split out of UnityChatJsonCodec.cs so the original message-path mapping stays readable; the two
    // hooks (TrySerializeGroupRequest / TryDeserializeGroupResponse) are called from there.
    public sealed partial class UnityChatJsonCodec
    {
        private bool TrySerializeGroupRequest(object value, out string json)
        {
            json = null;
            if (value is RemoteCreateGroupRequest create)
            {
                json = backend.ToJson(new WireCreateGroup
                {
                    gameServerId = create.GameServerId,
                    worldId = create.WorldId,
                    title = create.Title,
                    inviteePlayerIds = (create.InviteePlayerIds ?? Array.Empty<string>()).ToArray(),
                    clientRequestId = create.ClientRequestId
                });
                return true;
            }
            if (value is RemoteEmptyChatRequest) { json = "{}"; return true; }
            if (value is RemoteInviteToGroupRequest invite) { json = backend.ToJson(new WireInviteToGroup { inviteePlayerId = invite.InviteePlayerId }); return true; }
            if (value is RemoteRespondToGroupInviteRequest respond) { json = backend.ToJson(new WireRespondToInvite { accept = respond.Accept }); return true; }
            if (value is RemoteTransferGroupLeadershipRequest transfer) { json = backend.ToJson(new WireTransferLeadership { newLeaderPlayerId = transfer.NewLeaderPlayerId }); return true; }
            if (value is RemoteUpdateChatPreferencesRequest preferences) { json = backend.ToJson(new WirePreferences { autoInviteResponse = preferences.AutoInviteResponse }); return true; }
            if (value is RemoteAcknowledgeChatInvitationsRequest acknowledge) { json = backend.ToJson(new WireAcknowledgeInvites { inviteIds = (acknowledge.InviteIds ?? Array.Empty<string>()).ToArray() }); return true; }
            return false;
        }

        private bool TryDeserializeGroupResponse(Type type, string json, out object value)
        {
            value = null;
            if (type == typeof(RemoteChatGroupDetail)) { value = Map(backend.FromJson<WireGroupDetail>(json)); return true; }
            if (type == typeof(RemoteChatGroupInvite)) { value = Map(backend.FromJson<WireGroupInvite>(json)); return true; }
            if (type == typeof(RemoteChatPreferences)) { WirePreferences wire = backend.FromJson<WirePreferences>(json); value = new RemoteChatPreferences { AutoInviteResponse = wire?.autoInviteResponse }; return true; }
            if (type == typeof(RemoteChatGroupLeaveResult)) { WireLeaveResult wire = backend.FromJson<WireLeaveResult>(json); value = new RemoteChatGroupLeaveResult { Left = wire != null && wire.left }; return true; }
            if (type == typeof(RemoteChatInviteAcknowledgeResult)) { WireAcknowledgeResult wire = backend.FromJson<WireAcknowledgeResult>(json); value = new RemoteChatInviteAcknowledgeResult { Acknowledged = wire == null ? 0 : wire.acknowledged }; return true; }
            if (type == typeof(RemoteChatGroupInviteList))
            {
                // The endpoint answers a bare JSON array; JsonUtility only parses objects, so it is
                // wrapped here rather than forcing the server into a non-idiomatic envelope that a
                // web client would then have to unwrap for nothing.
                WireInviteList wire = backend.FromJson<WireInviteList>(WrapArray(json));
                var list = new RemoteChatGroupInviteList();
                if (wire?.items != null) foreach (WireGroupInvite item in wire.items) { RemoteChatGroupInvite mapped = Map(item); if (mapped != null) list.Items.Add(mapped); }
                value = list;
                return true;
            }
            return false;
        }

        private static string WrapArray(string json)
        {
            string trimmed = (json ?? string.Empty).Trim();
            if (trimmed.Length == 0) trimmed = "[]";
            return "{\"items\":" + trimmed + "}";
        }

        private static RemoteChatGroupDetail Map(WireGroupDetail wire)
        {
            if (wire == null) return null;
            var detail = new RemoteChatGroupDetail
            {
                ConversationId = wire.conversationId,
                Title = wire.title,
                OwnerPlayerId = wire.ownerPlayerId,
                ViewerIsLeader = wire.viewerIsLeader
            };
            if (wire.members != null) foreach (WireGroupMember member in wire.members) if (member != null) detail.Members.Add(new RemoteChatGroupMember { PlayerId = member.playerId, DisplayName = member.displayName, Role = member.role, IsLeader = member.isLeader });
            if (wire.pendingInvites != null) foreach (WireGroupInvite invite in wire.pendingInvites) { RemoteChatGroupInvite mapped = Map(invite); if (mapped != null) detail.PendingInvites.Add(mapped); }
            return detail;
        }

        private static RemoteChatGroupInvite Map(WireGroupInvite wire)
        {
            if (wire == null || string.IsNullOrWhiteSpace(wire.inviteId)) return null;
            DateTimeOffset created;
            DateTimeOffset.TryParse(wire.createdAtUtc, out created);
            return new RemoteChatGroupInvite
            {
                InviteId = wire.inviteId,
                ConversationId = wire.conversationId,
                GroupTitle = wire.groupTitle,
                InviterPlayerId = wire.inviterPlayerId,
                InviterDisplayName = wire.inviterDisplayName,
                InviteePlayerId = wire.inviteePlayerId,
                InviteeDisplayName = wire.inviteeDisplayName,
                Status = wire.status,
                CreatedAt = created
            };
        }

        [Serializable] private sealed class WireCreateGroup { public string gameServerId; public string worldId; public string title; public string[] inviteePlayerIds; public string clientRequestId; }
        [Serializable] private sealed class WireInviteToGroup { public string inviteePlayerId; }
        [Serializable] private sealed class WireRespondToInvite { public bool accept; }
        [Serializable] private sealed class WireTransferLeadership { public string newLeaderPlayerId; }
        [Serializable] private sealed class WirePreferences { public string autoInviteResponse; }
        [Serializable] private sealed class WireAcknowledgeInvites { public string[] inviteIds; }
        [Serializable] private sealed class WireAcknowledgeResult { public int acknowledged; }
        [Serializable] private sealed class WireLeaveResult { public bool left; }
        [Serializable] private sealed class WireGroupMember { public string playerId; public string displayName; public string role; public bool isLeader; }
        [Serializable] private sealed class WireGroupInvite { public string inviteId; public string conversationId; public string groupTitle; public string inviterPlayerId; public string inviterDisplayName; public string inviteePlayerId; public string inviteeDisplayName; public string status; public string createdAtUtc; public string respondedAtUtc; }
        [Serializable] private sealed class WireGroupDetail { public string conversationId; public string title; public string ownerPlayerId; public bool viewerIsLeader; public WireGroupMember[] members; public WireGroupInvite[] pendingInvites; }
        [Serializable] private sealed class WireInviteList { public WireGroupInvite[] items; }
    }
#pragma warning restore 0649
}
