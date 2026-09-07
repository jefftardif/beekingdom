using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    // RAP-OPTIONNEL-COMMUNICATIONS_01 - player-created group rooms, invitations and chat preferences.
    //
    // These calls deliberately do NOT go through the pending/offline replay stores used by
    // SendAsync/CreateConversationAsync. A queued message is worth replaying hours later; a queued
    // "kick this member" or "accept this invitation" is not - the group state may have changed under
    // it, and replaying it blind would resurrect decisions the player already saw fail. So they fail
    // fast and are retried by the player, while group CREATION still carries a ClientRequestId because
    // the server dedupes it (idempotency receipt) and a double-tap must not create two rooms.
    public sealed partial class ServerChatProvider
    {
        private const int MaxChatResourceIdCharacters = 128;

        public async Task<RemoteChatGroupDetail> CreateGroupAsync(RemoteCreateGroupRequest request, CancellationToken ct)
        {
            EnsureRemoteOperationReady("create_group");
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.ClientRequestId)) throw new ArgumentException("A stable client request id is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Title)) throw new ArgumentException("A group title is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.GameServerId) || string.IsNullOrWhiteSpace(request.WorldId)) throw new ArgumentException("A game server and world scope are required.", nameof(request));

            var normalized = new RemoteCreateGroupRequest
            {
                GameServerId = request.GameServerId.Trim(),
                WorldId = request.WorldId.Trim(),
                Title = request.Title.Trim(),
                ClientRequestId = request.ClientRequestId.Trim(),
                InviteePlayerIds = (request.InviteePlayerIds ?? Array.Empty<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
            };
            foreach (string invitee in normalized.InviteePlayerIds) ValidateResourceId(invitee, "inviteePlayerId");
            return await PostAsync<RemoteChatGroupDetail>("/chat/v1/groups", normalized, ct);
        }

        public async Task<RemoteChatGroupDetail> GetGroupDetailAsync(string conversationId, CancellationToken ct)
        {
            EnsureRemoteOperationReady("group_detail");
            return await GetAsync<RemoteChatGroupDetail>("/chat/v1/groups/" + Escape(conversationId, nameof(conversationId)), ct);
        }

        public async Task<RemoteChatGroupInvite> InviteToGroupAsync(string conversationId, string inviteePlayerId, CancellationToken ct)
        {
            EnsureRemoteOperationReady("invite_to_group");
            ValidateResourceId(inviteePlayerId, nameof(inviteePlayerId));
            return await PostAsync<RemoteChatGroupInvite>(
                "/chat/v1/groups/" + Escape(conversationId, nameof(conversationId)) + "/invitations",
                new RemoteInviteToGroupRequest { InviteePlayerId = inviteePlayerId.Trim() },
                ct);
        }

        public async Task<RemoteChatGroupDetail> RemoveGroupMemberAsync(string conversationId, string playerId, CancellationToken ct)
        {
            EnsureRemoteOperationReady("remove_group_member");
            return await PostAsync<RemoteChatGroupDetail>(
                "/chat/v1/groups/" + Escape(conversationId, nameof(conversationId)) + "/members/" + Escape(playerId, nameof(playerId)) + "/remove",
                new RemoteEmptyChatRequest(),
                ct);
        }

        public async Task<RemoteChatGroupDetail> TransferGroupLeadershipAsync(string conversationId, string newLeaderPlayerId, CancellationToken ct)
        {
            EnsureRemoteOperationReady("transfer_group_leadership");
            ValidateResourceId(newLeaderPlayerId, nameof(newLeaderPlayerId));
            return await PostAsync<RemoteChatGroupDetail>(
                "/chat/v1/groups/" + Escape(conversationId, nameof(conversationId)) + "/leadership/transfer",
                new RemoteTransferGroupLeadershipRequest { NewLeaderPlayerId = newLeaderPlayerId.Trim() },
                ct);
        }

        public async Task<bool> LeaveGroupAsync(string conversationId, CancellationToken ct)
        {
            EnsureRemoteOperationReady("leave_group");
            RemoteChatGroupLeaveResult result = await PostAsync<RemoteChatGroupLeaveResult>(
                "/chat/v1/groups/" + Escape(conversationId, nameof(conversationId)) + "/leave", new RemoteEmptyChatRequest(), ct);
            return result != null && result.Left;
        }

        public async Task<IReadOnlyList<RemoteChatGroupInvite>> ListInvitationsAsync(CancellationToken ct)
        {
            EnsureRemoteOperationReady("list_invitations");
            RemoteChatGroupInviteList list = await GetAsync<RemoteChatGroupInviteList>("/chat/v1/invitations", ct);
            return list?.Items ?? (IReadOnlyList<RemoteChatGroupInvite>)Array.Empty<RemoteChatGroupInvite>();
        }

        public async Task<IReadOnlyList<RemoteChatGroupInvite>> ListSentInvitationResponsesAsync(CancellationToken ct)
        {
            EnsureRemoteOperationReady("list_sent_invitations");
            RemoteChatGroupInviteList list = await GetAsync<RemoteChatGroupInviteList>("/chat/v1/invitations/sent", ct);
            return list?.Items ?? (IReadOnlyList<RemoteChatGroupInvite>)Array.Empty<RemoteChatGroupInvite>();
        }

        public async Task<int> AcknowledgeInvitationResponsesAsync(IReadOnlyList<string> inviteIds, CancellationToken ct)
        {
            EnsureRemoteOperationReady("acknowledge_invitations");
            string[] normalized = (inviteIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (normalized.Length == 0) return 0;
            foreach (string inviteId in normalized) ValidateResourceId(inviteId, "inviteId");
            RemoteChatInviteAcknowledgeResult result = await PostAsync<RemoteChatInviteAcknowledgeResult>(
                "/chat/v1/invitations/sent/acknowledge",
                new RemoteAcknowledgeChatInvitationsRequest { InviteIds = normalized },
                ct);
            return result?.Acknowledged ?? 0;
        }

        public async Task<RemoteChatGroupInvite> RespondToInvitationAsync(string inviteId, bool accept, CancellationToken ct)
        {
            EnsureRemoteOperationReady("respond_to_invitation");
            return await PostAsync<RemoteChatGroupInvite>(
                "/chat/v1/invitations/" + Escape(inviteId, nameof(inviteId)) + "/respond",
                new RemoteRespondToGroupInviteRequest { Accept = accept },
                ct);
        }

        public async Task<RemoteChatPreferences> GetChatPreferencesAsync(CancellationToken ct)
        {
            EnsureRemoteOperationReady("get_chat_preferences");
            return await GetAsync<RemoteChatPreferences>("/chat/v1/preferences", ct);
        }

        public async Task<RemoteChatPreferences> UpdateChatPreferencesAsync(string autoInviteResponse, CancellationToken ct)
        {
            EnsureRemoteOperationReady("update_chat_preferences");
            if (!RemoteChatAutoInviteResponse.IsKnown(autoInviteResponse)) throw new ArgumentException("Unknown auto invite response rule.", nameof(autoInviteResponse));
            return await PostAsync<RemoteChatPreferences>("/chat/v1/preferences", new RemoteUpdateChatPreferencesRequest { AutoInviteResponse = autoInviteResponse }, ct);
        }

        // Ids reach the server inside the URL path, so they are validated before escaping rather than
        // after: an id carrying a control character or a separator is a bug (or an attack), not
        // something to silently percent-encode and let the server reject.
        private static void ValidateResourceId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaxChatResourceIdCharacters) throw new ArgumentException("A chat resource id is required.", parameterName);
            string trimmed = value.Trim();
            for (int index = 0; index < trimmed.Length; index++)
            {
                char character = trimmed[index];
                bool alphaNumeric = (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9');
                if (!alphaNumeric && character != '-' && character != '_') throw new ArgumentException("A chat resource id contains invalid characters.", parameterName);
            }
        }

        private static string Escape(string value, string parameterName)
        {
            ValidateResourceId(value, parameterName);
            return Uri.EscapeDataString(value.Trim());
        }
    }
}
