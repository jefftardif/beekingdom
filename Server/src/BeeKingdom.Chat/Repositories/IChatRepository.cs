using BeeKingdom.Chat.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat.Repositories;

public interface IChatRepository
{
    ChatConversation SaveConversation(ChatConversation conversation, IReadOnlyList<ChatConversationParticipant> participants);
    ChatConversation? GetConversation(Guid conversationId);
    ChatConversation? GetConversationByAudience(Guid gameServerId, Guid worldId, ChatChannelType channelType, string audienceKey);
    IReadOnlyList<ChatConversation> ListConversations(PlayerId playerId, int offset, int limit);
    IReadOnlyList<ChatConversationParticipant> ListParticipants(Guid conversationId);
    ChatConversationParticipant? GetParticipant(Guid conversationId, PlayerId playerId);
    ChatConversationParticipant EnsureParticipant(ChatConversationParticipant participant);
    // M042-CL: Ensure* only adds a brand-new row and never touches an existing one (including a
    // previously-removed one) - Upsert additionally reactivates/updates it, and Remove marks it
    // gone. Added specifically to let Alliance membership changes (join/leave/kick/application-
    // accepted/invitation-accepted) drive real chat participation without a second chat system.
    ChatConversationParticipant UpsertParticipant(ChatConversationParticipant participant);
    ChatConversationParticipant? RemoveParticipant(Guid conversationId, PlayerId playerId, DateTimeOffset removedAtUtc);
    // RAP-OPTIONNEL-COMMUNICATIONS_01: role-only mutation. SaveConversation rewrites the whole
    // participant set, which is the wrong grain for a leadership transfer (two rows change, nothing
    // else must move); UpsertParticipant would need the caller to rebuild JoinedAtUtc/permissions.
    ChatConversationParticipant? UpdateParticipantRole(Guid conversationId, PlayerId playerId, ChatPermissionRole role);
    ChatGroupInvite SaveGroupInvite(ChatGroupInvite invite);
    ChatGroupInvite? GetGroupInvite(Guid inviteId);
    ChatGroupInvite? GetPendingInvite(Guid conversationId, PlayerId inviteePlayerId);
    IReadOnlyList<ChatGroupInvite> ListPendingInvitesForPlayer(PlayerId playerId);
    IReadOnlyList<ChatGroupInvite> ListInvitesSentByPlayer(PlayerId playerId, bool onlyUnacknowledgedResponses);
    IReadOnlyList<ChatGroupInvite> ListPendingInvitesForConversation(Guid conversationId);
    ChatGroupInvite? UpdateGroupInviteStatus(Guid inviteId, string status, DateTimeOffset respondedAtUtc);
    int AcknowledgeInviteResponses(PlayerId inviterPlayerId, IReadOnlyList<Guid> inviteIds);
    ChatPreferences? GetChatPreferences(PlayerId playerId);
    ChatPreferences SaveChatPreferences(ChatPreferences preferences);
    long NextSequence(Guid conversationId);
    ChatOutboxReceipt? GetOutboxReceipt(PlayerId playerId, Guid conversationId, string clientRequestId);
    ChatOutboxReceipt SaveOutboxReceipt(ChatOutboxReceipt receipt);
    ChatConversationCreationReceipt? GetConversationCreationReceipt(PlayerId playerId,string clientRequestId);
    ChatConversationCreationReceipt SaveConversationCreationReceipt(ChatConversationCreationReceipt receipt);
    ChatMessage SaveMessage(ChatMessage message);
    ChatMessage? GetMessage(Guid messageId);
    IReadOnlyList<ChatMessage> ListMessages(Guid conversationId, long afterSequence, int limit);
    long GetLastSequence(Guid conversationId);
    ChatInboxEntry SaveInbox(ChatInboxEntry entry);
    ChatInboxEntry? GetInbox(PlayerId playerId, Guid conversationId);
    IReadOnlyList<ChatInboxEntry> ListInboxEntries(Guid conversationId);
    ChatModerationReport SaveModerationReport(ChatModerationReport report);
    ChatModerationReport? GetModerationReport(Guid reportId);
    ChatModerationReportReceipt? GetModerationReportReceipt(PlayerId reporterPlayerId,string clientRequestId);
    ChatModerationReportReceipt SaveModerationReportReceipt(ChatModerationReportReceipt receipt);
    ChatModerationReport SaveModerationReportIdempotent(ChatModerationReport report,ChatModerationReportReceipt receipt);
    int PurgeExpiredReceipts(DateTimeOffset cutoffUtc);

    // M056-CL: erases one player's PERSONAL chat footprint for the Admin-gated account-deletion
    // cascade - their inbox entries plus their own outbox/creation/moderation-report receipts, all
    // of which are keyed by PlayerId and are meaningless once the account is gone.
    //
    // Deliberately does NOT touch dbo.ChatMessages. A deleted account's authored messages sit
    // inside conversations that belong to OTHER, real players; hard-deleting them would silently
    // punch holes in a third party's readable history (and break sequence continuity) because an
    // unrelated test account was removed. Removing them as a PARTICIPANT is the correct, already-
    // established convention here - AllianceService.RemoveMember does exactly that via
    // RemoveParticipant on kick/leave - so the deletion cascade reuses it rather than inventing a
    // destructive second behaviour. Participant removal is done by the caller, not here, so it
    // stays visible in the cascade summary returned to the admin.
    int DeletePlayerChatFootprint(PlayerId playerId);
}
