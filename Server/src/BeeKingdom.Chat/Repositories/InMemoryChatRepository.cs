using BeeKingdom.Chat.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Chat.Repositories;

public sealed class InMemoryChatRepository : IChatRepository
{
    private readonly Dictionary<Guid, ChatConversation> conversations = new();
    private readonly Dictionary<string, Guid> conversationIdByAudience = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, List<ChatConversationParticipant>> participantsByConversation = new();
    private readonly Dictionary<Guid, List<ChatMessage>> messagesByConversation = new();
    private readonly Dictionary<Guid, ChatMessage> messagesById = new();
    private readonly Dictionary<Guid, long> nextSequenceByConversation = new();
    private readonly Dictionary<string, ChatOutboxReceipt> outbox = new(StringComparer.Ordinal);
    private readonly Dictionary<string,ChatConversationCreationReceipt> creationReceipts=new(StringComparer.Ordinal);
    private readonly Dictionary<string, ChatInboxEntry> inbox = new(StringComparer.Ordinal);
    private readonly List<ChatModerationReport> reports = new();
    private readonly Dictionary<string,ChatModerationReportReceipt> reportReceipts=new(StringComparer.Ordinal);
    private readonly object sync = new();

    public ChatConversation SaveConversation(ChatConversation conversation, IReadOnlyList<ChatConversationParticipant> participants)
    {
        lock (sync)
        {
            conversations[conversation.ConversationId] = conversation;
            conversationIdByAudience[AudienceKey(conversation.GameServerId, conversation.WorldId, conversation.ChannelType, conversation.AudienceKey)] = conversation.ConversationId;
            participantsByConversation[conversation.ConversationId] = participants.ToList();
            nextSequenceByConversation.TryAdd(conversation.ConversationId, 1);
            return conversation;
        }
    }

    public ChatConversation? GetConversation(Guid conversationId)
    {
        lock (sync)
        {
            return conversations.TryGetValue(conversationId, out ChatConversation? conversation) ? conversation : null;
        }
    }

    public ChatConversation? GetConversationByAudience(Guid gameServerId, Guid worldId, ChatChannelType channelType, string audienceKey)
    {
        lock (sync)
        {
            return conversationIdByAudience.TryGetValue(AudienceKey(gameServerId, worldId, channelType, audienceKey), out Guid conversationId)
                ? GetConversation(conversationId)
                : null;
        }
    }

    public IReadOnlyList<ChatConversation> ListConversations(PlayerId playerId, int offset, int limit)
    {
        lock (sync)
        {
            return participantsByConversation
                .Where(pair => pair.Value.Any(participant => participant.PlayerId == playerId && participant.RemovedAtUtc == null && participant.CanRead))
                .Select(pair => conversations[pair.Key])
                .OrderByDescending(conversation => conversation.LastActivityAtUtc ?? conversation.CreatedAtUtc)
                .ThenBy(conversation => conversation.ConversationId)
                .Skip(Math.Max(0,offset))
                .Take(Math.Clamp(limit,1,101))
                .ToArray();
        }
    }

    public IReadOnlyList<ChatConversationParticipant> ListParticipants(Guid conversationId)
    {
        lock (sync)
        {
            return participantsByConversation.TryGetValue(conversationId, out List<ChatConversationParticipant>? participants)
                ? participants.ToArray()
                : Array.Empty<ChatConversationParticipant>();
        }
    }

    public ChatConversationParticipant? GetParticipant(Guid conversationId, PlayerId playerId)
    {
        lock (sync)
        {
            return ListParticipants(conversationId).FirstOrDefault(participant => participant.PlayerId == playerId);
        }
    }

    public ChatConversationParticipant EnsureParticipant(ChatConversationParticipant participant)
    {
        lock (sync)
        {
            if (!participantsByConversation.TryGetValue(participant.ConversationId, out List<ChatConversationParticipant>? participants))
            {
                participants = new List<ChatConversationParticipant>();
                participantsByConversation[participant.ConversationId] = participants;
            }

            ChatConversationParticipant? existing = participants.FirstOrDefault(item => item.PlayerId == participant.PlayerId);
            if (existing != null)
            {
                return existing;
            }

            participants.Add(participant);
            return participant;
        }
    }

    public ChatConversationParticipant UpsertParticipant(ChatConversationParticipant participant)
    {
        lock (sync)
        {
            if (!participantsByConversation.TryGetValue(participant.ConversationId, out List<ChatConversationParticipant>? participants))
            {
                participants = new List<ChatConversationParticipant>();
                participantsByConversation[participant.ConversationId] = participants;
            }

            int index = participants.FindIndex(item => item.PlayerId == participant.PlayerId);
            if (index >= 0) participants[index] = participant;
            else participants.Add(participant);
            return participant;
        }
    }

    public ChatConversationParticipant? RemoveParticipant(Guid conversationId, PlayerId playerId, DateTimeOffset removedAtUtc)
    {
        lock (sync)
        {
            if (!participantsByConversation.TryGetValue(conversationId, out List<ChatConversationParticipant>? participants)) return null;
            int index = participants.FindIndex(item => item.PlayerId == playerId);
            if (index < 0) return null;
            ChatConversationParticipant removed = participants[index] with { RemovedAtUtc = removedAtUtc, CanRead = false, CanWrite = false };
            participants[index] = removed;
            return removed;
        }
    }

    public long NextSequence(Guid conversationId)
    {
        lock (sync)
        {
            long sequence = nextSequenceByConversation.TryGetValue(conversationId, out long current) ? current : 1;
            nextSequenceByConversation[conversationId] = sequence + 1;
            return sequence;
        }
    }

    public ChatOutboxReceipt? GetOutboxReceipt(PlayerId playerId, Guid conversationId, string clientRequestId)
    {
        lock (sync)
        {
            return outbox.TryGetValue(OutboxKey(playerId, conversationId, clientRequestId), out ChatOutboxReceipt? receipt) ? receipt : null;
        }
    }

    public ChatOutboxReceipt SaveOutboxReceipt(ChatOutboxReceipt receipt)
    {
        lock (sync)
        {
            outbox[OutboxKey(receipt.PlayerId, receipt.ConversationId, receipt.ClientRequestId)] = receipt;
            return receipt;
        }
    }
    public ChatConversationCreationReceipt? GetConversationCreationReceipt(PlayerId playerId,string clientRequestId){lock(sync)return creationReceipts.GetValueOrDefault($"{playerId.Value:N}:{clientRequestId}");}
    public ChatConversationCreationReceipt SaveConversationCreationReceipt(ChatConversationCreationReceipt receipt){lock(sync){creationReceipts[$"{receipt.PlayerId.Value:N}:{receipt.ClientRequestId}"]=receipt;return receipt;}}

    public ChatMessage SaveMessage(ChatMessage message)
    {
        lock (sync)
        {
            messagesById[message.MessageId] = message;
            if (!messagesByConversation.TryGetValue(message.ConversationId, out List<ChatMessage>? messages))
            {
                messages = new List<ChatMessage>();
                messagesByConversation[message.ConversationId] = messages;
            }

            int index = messages.FindIndex(existing => existing.MessageId == message.MessageId);
            if (index >= 0)
            {
                messages[index] = message;
            }
            else
            {
                messages.Add(message);
            }

            if (conversations.TryGetValue(message.ConversationId, out ChatConversation? conversation))
            {
                conversations[message.ConversationId] = conversation with
                {
                    LastMessageId = message.MessageId,
                    LastActivityAtUtc = message.AcceptedAtUtc
                };
            }

            return message;
        }
    }

    public ChatMessage? GetMessage(Guid messageId)
    {
        lock (sync)
        {
            return messagesById.TryGetValue(messageId, out ChatMessage? message) ? message : null;
        }
    }

    public IReadOnlyList<ChatMessage> ListMessages(Guid conversationId, long afterSequence, int limit)
    {
        lock (sync)
        {
            return messagesByConversation.TryGetValue(conversationId, out List<ChatMessage>? messages)
                ? messages.Where(message => message.Sequence > afterSequence).OrderBy(message => message.Sequence).Take(limit).ToArray()
                : Array.Empty<ChatMessage>();
        }
    }

    public long GetLastSequence(Guid conversationId)
    { lock (sync) return messagesByConversation.TryGetValue(conversationId,out List<ChatMessage>? messages) && messages.Count>0 ? messages.Max(x=>x.Sequence) : 0; }

    public ChatInboxEntry SaveInbox(ChatInboxEntry entry)
    {
        lock (sync)
        {
            string key=InboxKey(entry.PlayerId,entry.ConversationId);
            if(inbox.TryGetValue(key,out ChatInboxEntry? current)&&current.ReadCursorSequence>entry.ReadCursorSequence)
            {
                entry=entry with{ReadCursorSequence=current.ReadCursorSequence,UnreadCount=Math.Min(current.UnreadCount,entry.UnreadCount),MentionCount=Math.Min(current.MentionCount,entry.MentionCount)};
            }
            inbox[key] = entry;
            return entry;
        }
    }

    public ChatInboxEntry? GetInbox(PlayerId playerId, Guid conversationId)
    {
        lock (sync)
        {
            return inbox.TryGetValue(InboxKey(playerId, conversationId), out ChatInboxEntry? entry) ? entry : null;
        }
    }

    public IReadOnlyList<ChatInboxEntry> ListInboxEntries(Guid conversationId)
    {
        lock (sync)
        {
            return inbox.Values.Where(entry => entry.ConversationId == conversationId).ToArray();
        }
    }

    public ChatModerationReport SaveModerationReport(ChatModerationReport report)
    {
        lock (sync)
        {
            reports.Add(report);
            return report;
        }
    }
    public ChatModerationReport? GetModerationReport(Guid reportId){lock(sync)return reports.FirstOrDefault(x=>x.ReportId==reportId);}
    public ChatModerationReportReceipt? GetModerationReportReceipt(PlayerId reporterPlayerId,string clientRequestId){lock(sync)return reportReceipts.GetValueOrDefault($"{reporterPlayerId.Value:N}:{clientRequestId}");}
    public ChatModerationReportReceipt SaveModerationReportReceipt(ChatModerationReportReceipt receipt){lock(sync){string key=$"{receipt.ReporterPlayerId.Value:N}:{receipt.ClientRequestId}";return reportReceipts.TryGetValue(key,out var existing)?existing:reportReceipts[key]=receipt;}}
    public ChatModerationReport SaveModerationReportIdempotent(ChatModerationReport report,ChatModerationReportReceipt receipt)
    {lock(sync){string key=$"{receipt.ReporterPlayerId.Value:N}:{receipt.ClientRequestId}";if(reportReceipts.TryGetValue(key,out var existing)){if(!string.Equals(existing.PayloadHash,receipt.PayloadHash,StringComparison.Ordinal))throw new InvalidOperationException("idempotency_conflict");return reports.Single(x=>x.ReportId==existing.ReportId);}reports.Add(report);reportReceipts[key]=receipt;return report;}}
    public int PurgeExpiredReceipts(DateTimeOffset cutoffUtc)
    {lock(sync){int removed=0;foreach(string key in creationReceipts.Where(x=>x.Value.CreatedAtUtc<cutoffUtc).Select(x=>x.Key).ToArray()){creationReceipts.Remove(key);removed++;}foreach(string key in outbox.Where(x=>x.Value.AcceptedAtUtc.HasValue&&x.Value.AcceptedAtUtc<cutoffUtc).Select(x=>x.Key).ToArray()){outbox.Remove(key);removed++;}foreach(string key in reportReceipts.Where(x=>x.Value.CreatedAtUtc<cutoffUtc).Select(x=>x.Key).ToArray()){reportReceipts.Remove(key);removed++;}return removed;}}

    private static string AudienceKey(Guid gameServerId, Guid worldId, ChatChannelType channelType, string audienceKey)
        => $"{gameServerId:N}:{worldId:N}:{channelType}:{audienceKey}";

    private static string OutboxKey(PlayerId playerId, Guid conversationId, string clientRequestId)
        => $"{playerId.Value:N}:{conversationId:N}:{clientRequestId}";

    private static string InboxKey(PlayerId playerId, Guid conversationId)
        => $"{playerId.Value:N}:{conversationId:N}";
}
