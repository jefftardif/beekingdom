using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    // RAP-OPTIONNEL-COMMUNICATIONS_01 - group / invitation / preference state for the Chat Royal screen.
    //
    // The group scope (game server + world) is NOT invented here. The chat client historically never
    // created a conversation from scratch (the alliance drawer always opened a conversation id it was
    // handed), so nothing in the Unity runtime ever needed a world scope. Rather than fabricate GUIDs
    // that would silently create rooms in the wrong world, group creation is refused until the host
    // supplies a real scope through ConfigureGroupScope - and the UI says so instead of failing blind.
    public sealed partial class LivingHiveChatController
    {
        private readonly List<LivingHiveChatInvitation> pendingInvitations = new List<LivingHiveChatInvitation>();
        private readonly List<LivingHiveChatInvitation> sentInvitationResponses = new List<LivingHiveChatInvitation>();
        private readonly Dictionary<string, LivingHiveChatGroupDetail> groupDetails = new Dictionary<string, LivingHiveChatGroupDetail>(StringComparer.Ordinal);
        private string autoInviteResponse = RemoteChatAutoInviteResponse.Ask;
        private string groupGameServerId;
        private string groupWorldId;

        public bool HasGroupScope => !string.IsNullOrWhiteSpace(groupGameServerId) && !string.IsNullOrWhiteSpace(groupWorldId);

        public void ConfigureGroupScope(string gameServerId, string worldId)
        {
            lock (gate)
            {
                groupGameServerId = string.IsNullOrWhiteSpace(gameServerId) ? null : gameServerId.Trim();
                groupWorldId = string.IsNullOrWhiteSpace(worldId) ? null : worldId.Trim();
            }
        }

        public async Task<string> CreateGroupAsync(string title, IReadOnlyList<string> inviteePlayerIds, CancellationToken ct)
        {
            string normalizedTitle = title?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedTitle)) throw new ArgumentException("A group title is required.", nameof(title));
            string gameServerId;
            string worldId;
            lock (gate) { gameServerId = groupGameServerId; worldId = groupWorldId; }
            if (string.IsNullOrWhiteSpace(gameServerId) || string.IsNullOrWhiteSpace(worldId))
            {
                SetStatus(LivingHiveChatStatus.Error, "chat_group_scope_not_configured");
                return null;
            }

            try
            {
                RemoteChatGroupDetail detail = await provider.CreateGroupAsync(new RemoteCreateGroupRequest
                {
                    GameServerId = gameServerId,
                    WorldId = worldId,
                    Title = normalizedTitle,
                    InviteePlayerIds = inviteePlayerIds ?? Array.Empty<string>(),
                    ClientRequestId = Guid.NewGuid().ToString("N")
                }, ct);
                if (detail == null || string.IsNullOrWhiteSpace(detail.ConversationId)) return null;

                StoreGroupDetail(detail);
                await SelectKnownConversationAsync(detail.ConversationId, detail.Title, "Group", ct);
                return detail.ConversationId;
            }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); return null; }
        }

        public Task InviteToGroupAsync(string conversationId, string inviteePlayerId, CancellationToken ct)
            => RunGroupMutationAsync(() => provider.InviteToGroupAsync(conversationId, inviteePlayerId, ct), conversationId, ct);

        public Task RemoveGroupMemberAsync(string conversationId, string playerId, CancellationToken ct)
            => RunGroupMutationAsync(() => provider.RemoveGroupMemberAsync(conversationId, playerId, ct), conversationId, ct);

        public Task TransferLeadershipAsync(string conversationId, string newLeaderPlayerId, CancellationToken ct)
            => RunGroupMutationAsync(() => provider.TransferGroupLeadershipAsync(conversationId, newLeaderPlayerId, ct), conversationId, ct);

        public async Task LeaveGroupAsync(string conversationId, CancellationToken ct)
        {
            try
            {
                await provider.LeaveGroupAsync(conversationId, ct);
                lock (gate)
                {
                    groupDetails.Remove(conversationId ?? string.Empty);
                    conversations.RemoveAll(value => string.Equals(value.ConversationId, conversationId, StringComparison.Ordinal));
                    if (string.Equals(selectedConversationId, conversationId, StringComparison.Ordinal))
                    {
                        selectedConversationId = conversations.FirstOrDefault()?.ConversationId;
                        messages.Clear();
                    }
                }
                await RefreshSelectedAsync(ct);
            }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); }
        }

        // Returns the conversation id to open when the invitation was accepted, null otherwise: the
        // caller (global alert) uses it to jump straight to Communication -> Groupes -> that group.
        public async Task<string> RespondToInvitationAsync(string inviteId, bool accept, CancellationToken ct)
        {
            try
            {
                RemoteChatGroupInvite answered = await provider.RespondToInvitationAsync(inviteId, accept, ct);
                lock (gate) pendingInvitations.RemoveAll(value => string.Equals(value.InviteId, inviteId, StringComparison.Ordinal));
                if (answered == null || !accept) return null;

                await RefreshConversationListAsync(ct);
                await SelectKnownConversationAsync(answered.ConversationId, answered.GroupTitle, "Group", ct);
                await RefreshGroupDetailAsync(answered.ConversationId, ct);
                return answered.ConversationId;
            }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); return null; }
        }

        public async Task RefreshGroupDetailAsync(string conversationId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(conversationId)) return;
            try { StoreGroupDetail(await provider.GetGroupDetailAsync(conversationId, ct)); }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); }
        }

        public async Task RefreshInvitationsAsync(CancellationToken ct)
        {
            try
            {
                IReadOnlyList<RemoteChatGroupInvite> received = await provider.ListInvitationsAsync(ct);
                IReadOnlyList<RemoteChatGroupInvite> answered = await provider.ListSentInvitationResponsesAsync(ct);
                lock (gate)
                {
                    pendingInvitations.Clear();
                    pendingInvitations.AddRange(received.Where(value => value != null).Select(Map));
                    sentInvitationResponses.Clear();
                    sentInvitationResponses.AddRange(answered.Where(value => value != null).Select(Map));
                }

                string selected;
                lock (gate) selected = selectedConversationId;
                if (IsGroupConversation(selected)) await RefreshGroupDetailAsync(selected, ct);
            }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); }
        }

        // Called once the refusal/acceptance toast has actually been shown, so the server stops
        // replaying it at every poll.
        public async Task AcknowledgeSentInvitationResponsesAsync(IReadOnlyList<string> inviteIds, CancellationToken ct)
        {
            string[] ids = (inviteIds ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            if (ids.Length == 0) return;
            try
            {
                await provider.AcknowledgeInvitationResponsesAsync(ids, ct);
                lock (gate) sentInvitationResponses.RemoveAll(value => ids.Contains(value.InviteId, StringComparer.Ordinal));
            }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); }
        }

        public async Task RefreshPreferencesAsync(CancellationToken ct)
        {
            try
            {
                RemoteChatPreferences preferences = await provider.GetChatPreferencesAsync(ct);
                lock (gate) autoInviteResponse = Normalize(preferences?.AutoInviteResponse);
            }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); }
        }

        public async Task UpdateAutoInviteResponseAsync(string rule, CancellationToken ct)
        {
            if (!RemoteChatAutoInviteResponse.IsKnown(rule)) throw new ArgumentException("Unknown auto invite response rule.", nameof(rule));
            try
            {
                RemoteChatPreferences preferences = await provider.UpdateChatPreferencesAsync(rule, ct);
                lock (gate) autoInviteResponse = Normalize(preferences?.AutoInviteResponse ?? rule);
            }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); }
        }

        // ==================== internals ====================

        private async Task RunGroupMutationAsync(Func<Task<RemoteChatGroupDetail>> mutation, string conversationId, CancellationToken ct)
        {
            try { StoreGroupDetail(await mutation()); }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); await RefreshGroupDetailAsync(conversationId, ct); }
        }

        private async Task RunGroupMutationAsync(Func<Task<RemoteChatGroupInvite>> mutation, string conversationId, CancellationToken ct)
        {
            try { await mutation(); }
            catch (RemoteChatTransportException exception) { ReportGroupFailure(exception); }
            await RefreshGroupDetailAsync(conversationId, ct);
        }

        private bool IsGroupConversation(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId)) return false;
            lock (gate) return conversations.Any(value => string.Equals(value.ConversationId, conversationId, StringComparison.Ordinal)
                && string.Equals(value.ChannelType, "Group", StringComparison.OrdinalIgnoreCase));
        }

        private void StoreGroupDetail(RemoteChatGroupDetail detail)
        {
            if (detail == null || string.IsNullOrWhiteSpace(detail.ConversationId)) return;
            LivingHiveChatGroupDetail mapped = new LivingHiveChatGroupDetail
            {
                ConversationId = detail.ConversationId,
                Title = detail.Title,
                OwnerPlayerId = detail.OwnerPlayerId,
                ViewerIsLeader = detail.ViewerIsLeader,
                Members = detail.Members.Where(value => value != null).Select(value => new LivingHiveChatGroupMember { PlayerId = value.PlayerId, DisplayName = value.DisplayName, IsLeader = value.IsLeader }).ToArray(),
                PendingInvites = detail.PendingInvites.Where(value => value != null).Select(Map).ToArray()
            };
            lock (gate) groupDetails[detail.ConversationId] = mapped;
        }

        private LivingHiveChatGroupDetail CloneSelectedGroupDetail()
        {
            LivingHiveChatGroupDetail detail;
            if (string.IsNullOrWhiteSpace(selectedConversationId) || !groupDetails.TryGetValue(selectedConversationId, out detail) || detail == null) return null;
            return new LivingHiveChatGroupDetail
            {
                ConversationId = detail.ConversationId,
                Title = detail.Title,
                OwnerPlayerId = detail.OwnerPlayerId,
                ViewerIsLeader = detail.ViewerIsLeader,
                Members = detail.Members.Select(value => new LivingHiveChatGroupMember { PlayerId = value.PlayerId, DisplayName = value.DisplayName, IsLeader = value.IsLeader }).ToArray(),
                PendingInvites = detail.PendingInvites.Select(Clone).ToArray()
            };
        }

        private void ClearGroupState()
        {
            pendingInvitations.Clear();
            sentInvitationResponses.Clear();
            groupDetails.Clear();
            autoInviteResponse = RemoteChatAutoInviteResponse.Ask;
        }

        private void ReportGroupFailure(RemoteChatTransportException exception)
            => SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString());

        private static string Normalize(string rule)
            => RemoteChatAutoInviteResponse.IsKnown(rule) ? rule : RemoteChatAutoInviteResponse.Ask;

        private static LivingHiveChatInvitation Map(RemoteChatGroupInvite value) => new LivingHiveChatInvitation
        {
            InviteId = value.InviteId,
            ConversationId = value.ConversationId,
            GroupTitle = value.GroupTitle,
            InviterPlayerId = value.InviterPlayerId,
            InviterDisplayName = value.InviterDisplayName,
            InviteePlayerId = value.InviteePlayerId,
            InviteeDisplayName = value.InviteeDisplayName,
            Status = value.Status
        };

        private static LivingHiveChatInvitation Clone(LivingHiveChatInvitation value) => new LivingHiveChatInvitation
        {
            InviteId = value.InviteId,
            ConversationId = value.ConversationId,
            GroupTitle = value.GroupTitle,
            InviterPlayerId = value.InviterPlayerId,
            InviterDisplayName = value.InviterDisplayName,
            InviteePlayerId = value.InviteePlayerId,
            InviteeDisplayName = value.InviteeDisplayName,
            Status = value.Status
        };
    }
}
