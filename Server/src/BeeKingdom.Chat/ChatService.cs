using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeeKingdom.Chat.Audience;
using BeeKingdom.Chat.Configuration;
using BeeKingdom.Chat.Models;
using BeeKingdom.Chat.Realtime;
using BeeKingdom.Chat.Repositories;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Chat;

public interface IChatService
{
    ChatCapabilities GetCapabilities();
    ChatReadiness GetReadiness();
    CreateChatConversationResult CreateConversation(PlayerId playerId, CreateChatConversationRequest request);
    ChatConversationPage ListConversations(PlayerId playerId, int limit, string? cursor = null);
    ChatInboxEntry? GetInbox(PlayerId playerId, Guid conversationId);
    ChatMessagePage GetMessages(PlayerId playerId, Guid conversationId, long afterSequence, int limit);
    Task<SendChatMessageResult> SendMessageAsync(PlayerId playerId, Guid conversationId, SendChatMessageRequest request, CancellationToken cancellationToken = default);
    ChatInboxEntry MarkRead(PlayerId playerId, Guid conversationId, long sequence);
    ChatModerationReport ReportMessage(PlayerId playerId, Guid messageId, ReportChatMessageRequest request);
    Task<CreateAllianceAnnouncementResult> SendAllianceAnnouncementAsync(PlayerId playerId, Guid allianceId, CreateAllianceAnnouncementRequest request, CancellationToken cancellationToken = default);
    void EnsureCanRead(PlayerId playerId, Guid conversationId);
    long GetLastSequence(Guid conversationId);
}

public sealed class ChatService : IChatService
{
    public long GetLastSequence(Guid conversationId) => repository.GetLastSequence(conversationId);
    public void EnsureCanRead(PlayerId playerId, Guid conversationId) => RequireRead(conversationId, playerId);
    private static readonly JsonSerializerOptions JsonOptions = BeeJson.CreateDefaultOptions();
    private readonly IChatRepository repository;
    private readonly IChatAudienceResolver audienceResolver;
    private readonly IChatRealtimeDispatcher realtime;
    private readonly IServerClock clock;
    private readonly ChatOptions options;
    private readonly Dictionary<Guid,Queue<DateTimeOffset>> messageAttemptsByPlayer=new();
    private readonly Dictionary<Guid,Queue<DateTimeOffset>> messageAttemptsByConversation=new();
    private readonly object rateSync=new();
    private DateTimeOffset nextReceiptPurgeUtc=DateTimeOffset.MinValue;

    public ChatService(IChatRepository repository, IChatAudienceResolver audienceResolver, IChatRealtimeDispatcher realtime, IServerClock clock, IOptions<ChatOptions> options)
    {
        this.repository = repository;
        this.audienceResolver = audienceResolver;
        this.realtime = realtime;
        this.clock = clock;
        this.options = options.Value;
    }

    public ChatCapabilities GetCapabilities()
    {
        return new ChatCapabilities(
            "server",
            Server: options.Enabled,
            OfficialGain: false,
            options.ProtocolVersion,
            [ChatChannelType.Alliance, ChatChannelType.Server, ChatChannelType.Private, ChatChannelType.Leaders],
            Emojis: true,
            Mentions: true,
            OfflineDelivery: true,
            ReadCursors: true,
            ModerationReports: true,
            Realtime: options.Enabled && options.RealtimeEnabled,
            new ChatLimits(options.BodyMaxCharacters, options.MessagesPerMinutePerPlayer, options.MessagesPerTenSecondsPerConversation, options.PrivateConversationCreatesPerHour, options.MaxPrivateRecipients),
            options.IdempotencyReceiptRetentionDays,
            TranslationAvailable: !string.Equals(options.TranslationModelVersion, ChatOptions.DisabledTranslationModelVersion, StringComparison.Ordinal),
            options.TranslationModelVersion);
    }

    public ChatReadiness GetReadiness()
    {
        List<string> blockers = [];
        if (!options.Enabled)
        {
            blockers.Add("Chat__Enabled is false; REST mutations are gated.");
        }

        if (!options.RealtimeEnabled)
        {
            blockers.Add("Chat__RealtimeEnabled is false; realtime hub aborts connections.");
        }

        blockers.Add("SQL repository implementation is scheduled for Phase 2; Phase 1 uses in-memory repository unless SqlServer is selected.");

        return new ChatReadiness("PreparationOnly", options.Enabled, options.RealtimeEnabled, PersistentSqlSchemaPrepared: true, LiveDeploymentAllowed: false, blockers);
    }

    public CreateChatConversationResult CreateConversation(PlayerId playerId, CreateChatConversationRequest request)
    {
        EnsureEnabled();
        MaybePurgeReceipts();
        ValidateConversationRequest(request);
        string creationHash = ComputeConversationPayloadHash(request);
        ChatConversationCreationReceipt? creationReceipt=repository.GetConversationCreationReceipt(playerId,request.ClientRequestId);
        if(creationReceipt!=null)
        {
            if(!string.Equals(creationReceipt.PayloadHash,creationHash,StringComparison.Ordinal)) throw new InvalidOperationException("idempotency_conflict");
            ChatConversation replay=repository.GetConversation(creationReceipt.ConversationId)??throw new InvalidOperationException("idempotency_record_missing_conversation");
            return new(replay,repository.GetInbox(playerId,replay.ConversationId)??CreateInbox(playerId,replay.ConversationId,replay.LastMessageId,replay.LastActivityAtUtc));
        }
        ChatAudienceDecision audience = audienceResolver.ResolveConversationAccess(playerId, request);
        if (!audience.Allowed)
        {
            throw new UnauthorizedAccessException(audience.ReasonCode ?? "forbidden");
        }

        string audienceKey = NormalizeAudienceKey(playerId, request);
        ChatConversation? existing = repository.GetConversationByAudience(request.GameServerId, request.WorldId, request.ChannelType, audienceKey);
        if (existing != null)
        {
            if (request.ChannelType == ChatChannelType.Server && repository.GetParticipant(existing.ConversationId, playerId) == null)
            {
                // The "Server" channel is a shared, open room: any player scoped to the same
                // game server/world joins the same conversation on first contact, not just its creator.
                repository.EnsureParticipant(new ChatConversationParticipant(existing.ConversationId, playerId, ChatPermissionRole.Member, clock.UtcNow, null, true, true));
            }

            repository.SaveConversationCreationReceipt(new(playerId,request.ClientRequestId,creationHash,existing.ConversationId,clock.UtcNow));
            ChatInboxEntry existingInbox = repository.GetInbox(playerId, existing.ConversationId) ?? CreateInbox(playerId, existing.ConversationId, null, null);
            return new CreateChatConversationResult(existing, existingInbox);
        }

        DateTimeOffset now = clock.UtcNow;
        ChatConversation conversation = new(
            Guid.NewGuid(),
            request.GameServerId,
            request.WorldId,
            request.ChannelType,
            audienceKey,
            request.Title,
            playerId,
            now,
            null,
            null,
            RetentionPolicyFor(request.ChannelType),
            1);

        IReadOnlyList<ChatConversationParticipant> participants = BuildParticipants(conversation.ConversationId, audience, now);
        repository.SaveConversation(conversation, participants);
        foreach (ChatConversationParticipant participant in participants)
        {
            repository.SaveInbox(CreateInbox(participant.PlayerId, conversation.ConversationId, null, now));
        }

        repository.SaveConversationCreationReceipt(new(playerId,request.ClientRequestId,creationHash,conversation.ConversationId,now));

        return new CreateChatConversationResult(conversation, repository.GetInbox(playerId, conversation.ConversationId)!);
    }

    private static string ComputeConversationPayloadHash(CreateChatConversationRequest request)
    {
        string? audience=string.IsNullOrWhiteSpace(request.AudienceKey)?null:request.AudienceKey.Trim();
        string? title=string.IsNullOrWhiteSpace(request.Title)?null:request.Title.Trim();
        Guid[] participants=(request.ParticipantIds??[]).Where(x=>x!=Guid.Empty).Distinct().OrderBy(x=>x).ToArray();
        string canonical=JsonSerializer.Serialize(new{request.ChannelType,request.GameServerId,request.WorldId,AudienceKey=audience,Title=title,ParticipantIds=participants},JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public ChatConversationPage ListConversations(PlayerId playerId, int limit, string? cursor = null)
    {
        EnsureEnabled();
        int boundedLimit = Math.Clamp(limit, 1, 100);
        int offset=DecodeConversationCursor(playerId,cursor);
        IReadOnlyList<ChatConversation> fetched=repository.ListConversations(playerId,offset,boundedLimit+1);
        bool hasMore=fetched.Count>boundedLimit; ChatConversation[] items=fetched.Take(boundedLimit).ToArray();
        return new ChatConversationPage(items,hasMore?EncodeConversationCursor(playerId,offset+items.Length):null);
    }

    public ChatInboxEntry? GetInbox(PlayerId playerId, Guid conversationId)
    {
        EnsureEnabled();
        RequireRead(conversationId, playerId);
        return repository.GetInbox(playerId, conversationId);
    }

    private static string EncodeConversationCursor(PlayerId playerId,int offset)
    {
        string payload=$"1|{PlayerCursorScope(playerId)}|{offset}"; string checksum=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("chat-conversations|"+payload)))[..16];
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload+"|"+checksum)).TrimEnd('=').Replace('+','-').Replace('/','_');
    }
    private static int DecodeConversationCursor(PlayerId playerId,string? cursor)
    {
        if(string.IsNullOrWhiteSpace(cursor))return 0;
        if (cursor.Length > 1024 || cursor.Any(char.IsControl)) throw new ArgumentException("conversation_cursor_invalid");
        try{string encoded=cursor.Replace('-','+').Replace('_','/');encoded=encoded.PadRight((encoded.Length+3)/4*4,'=');string[] parts=Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Split('|');if(parts.Length!=4||parts[0]!="1"||parts[1]!=PlayerCursorScope(playerId)||!int.TryParse(parts[2],out int offset)||offset<=0)throw new FormatException();string payload=string.Join('|',parts[..3]);string expected=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("chat-conversations|"+payload)))[..16];if(!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(parts[3]),Convert.FromHexString(expected)))throw new FormatException();return offset;}catch(Exception exception)when(exception is FormatException or ArgumentException){throw new ArgumentException("conversation_cursor_invalid");}
    }
    private static string PlayerCursorScope(PlayerId playerId)=>Convert.ToHexString(SHA256.HashData(playerId.Value.ToByteArray()))[..16];

    public ChatMessagePage GetMessages(PlayerId playerId, Guid conversationId, long afterSequence, int limit)
    {
        EnsureEnabled();
        if (afterSequence < 0) throw new ArgumentException("after_sequence_invalid");
        RequireRead(conversationId, playerId);
        int boundedLimit = Math.Clamp(limit, 1, 100);
        IReadOnlyList<ChatMessage> messages = repository.ListMessages(conversationId, Math.Max(0, afterSequence), boundedLimit);
        return new ChatMessagePage(messages, messages.Count == boundedLimit ? messages[^1].Sequence : null);
    }

    public async Task<SendChatMessageResult> SendMessageAsync(PlayerId playerId, Guid conversationId, SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureEnabled();
        MaybePurgeReceipts();
        ChatConversationParticipant participant = RequireWrite(conversationId, playerId);
        ChatConversation conversation = repository.GetConversation(conversationId) ?? throw new KeyNotFoundException("conversation_not_found");
        ValidateMessageRequest(request);

        string payloadHash = ComputePayloadHash(request);
        ChatOutboxReceipt? existingReceipt = repository.GetOutboxReceipt(playerId, conversationId, request.ClientRequestId);
        if (existingReceipt != null)
        {
            if (!string.Equals(existingReceipt.PayloadHash, payloadHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("idempotency_conflict");
            }

            ChatMessage existingMessage = existingReceipt.MessageId.HasValue
                ? repository.GetMessage(existingReceipt.MessageId.Value) ?? throw new InvalidOperationException("idempotency_record_missing_message")
                : throw new InvalidOperationException(existingReceipt.LastErrorCode ?? "idempotency_pending");
            return new SendChatMessageResult(existingMessage, Deduplicated: true, existingMessage.Sequence);
        }

        cancellationToken.ThrowIfCancellationRequested();
        RequireMessageRate(playerId,conversationId,clock.UtcNow);

        long sequence = repository.NextSequence(conversationId);
        DateTimeOffset now = clock.UtcNow;
        IReadOnlyList<ChatMention> mentions = (request.Mentions ?? Array.Empty<ChatMentionInput>())
            .Select(mention => new ChatMention(new PlayerId(mention.PlayerId), mention.Label))
            .ToArray();
        ChatMessage message = new(
            Guid.NewGuid(),
            conversationId,
            conversation.GameServerId,
            conversation.WorldId,
            conversation.ChannelType,
            playerId,
            $"player:{playerId.Value:N}",
            request.Body.Trim(),
            request.ContentParts ?? [new ChatContentPart("text", request.Body.Trim(), null, null, null, null, null)],
            mentions,
            request.Emoji ?? Array.Empty<ChatEmoji>(),
            request.ReplyToMessageId,
            request.ClientCreatedAt,
            now,
            sequence,
            request.ClientRequestId.Trim(),
            ChatMessageState.Accepted,
            ChatModerationStatus.Clear,
            null,
            null,
            null,
            1);

        repository.SaveOutboxReceipt(new ChatOutboxReceipt(playerId, conversationId, request.ClientRequestId.Trim(), payloadHash, message.MessageId, now, null));
        repository.SaveMessage(message);
        UpdateInboxAfterMessage(conversation, message, participant);

        await realtime.PublishAsync(new ChatEventEnvelope(
            $"evt_{Guid.NewGuid():N}",
            "message.created",
            now,
            conversationId,
            sequence,
            playerId,
            ChatTransportMapper.Message(message),
            "server",
            1), cancellationToken);

        return new SendChatMessageResult(message, Deduplicated: false, sequence);
    }

    private void RequireMessageRate(PlayerId playerId,Guid conversationId,DateTimeOffset now)
    {
        lock(rateSync)
        {
            if(!Acquire(messageAttemptsByPlayer,playerId.Value,now,TimeSpan.FromMinutes(1),options.MessagesPerMinutePerPlayer) || !Acquire(messageAttemptsByConversation,conversationId,now,TimeSpan.FromSeconds(10),options.MessagesPerTenSecondsPerConversation)) throw new InvalidOperationException("chat_rate_limited");
        }
    }
    private static bool Acquire(Dictionary<Guid,Queue<DateTimeOffset>> store,Guid key,DateTimeOffset now,TimeSpan window,int limit)
    {
        if(!store.TryGetValue(key,out Queue<DateTimeOffset>? queue))store[key]=queue=new(); while(queue.Count>0&&queue.Peek()<=now-window)queue.Dequeue(); if(queue.Count>=limit)return false; queue.Enqueue(now); return true;
    }

    public ChatInboxEntry MarkRead(PlayerId playerId, Guid conversationId, long sequence)
    {
        EnsureEnabled();
        RequireRead(conversationId, playerId);
        ChatInboxEntry current = repository.GetInbox(playerId, conversationId) ?? CreateInbox(playerId, conversationId, null, clock.UtcNow);
        long requested = Math.Max(0, sequence);
        long serverLastSequence = repository.GetLastSequence(conversationId);
        long cursor = Math.Max(current.ReadCursorSequence, Math.Min(requested, serverLastSequence));
        (int unreadCount, int mentionCount) = ComputeAuthoritativeCounts(conversationId, cursor, playerId);
        ChatInboxEntry updated = current with
        {
            ReadCursorSequence = cursor,
            UnreadCount = unreadCount,
            MentionCount = mentionCount,
            UpdatedAtUtc = clock.UtcNow
        };
        return repository.SaveInbox(updated);
    }

    private (int UnreadCount, int MentionCount) ComputeAuthoritativeCounts(Guid conversationId, long afterSequence, PlayerId playerId)
    {
        int unread = 0;
        int mentions = 0;
        long cursor = afterSequence;
        while (true)
        {
            IReadOnlyList<ChatMessage> page = repository.ListMessages(conversationId, cursor, 100);
            if (page.Count == 0) break;
            foreach (ChatMessage message in page)
            {
                if (message.SenderPlayerId == playerId) continue;
                unread++;
                if (message.Mentions.Any(mention => mention.PlayerId == playerId)) mentions++;
            }
            cursor = page[^1].Sequence;
            if (page.Count < 100) break;
        }
        return (unread, mentions);
    }

    public ChatModerationReport ReportMessage(PlayerId playerId, Guid messageId, ReportChatMessageRequest request)
    {
        EnsureEnabled();
        MaybePurgeReceipts();
        if(string.IsNullOrWhiteSpace(request.ClientRequestId))throw new ArgumentException("client_request_id_required");
        if(string.IsNullOrWhiteSpace(request.Category))throw new ArgumentException("category_required");
        string category=request.Category.Trim();
        string payloadHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{messageId:N}|{category}")));
        ChatModerationReportReceipt? receipt=repository.GetModerationReportReceipt(playerId,request.ClientRequestId.Trim());
        if(receipt!=null){if(!string.Equals(receipt.PayloadHash,payloadHash,StringComparison.Ordinal))throw new InvalidOperationException("idempotency_conflict");return repository.GetModerationReport(receipt.ReportId)??throw new InvalidOperationException("idempotency_record_missing_report");}
        ChatMessage message = repository.GetMessage(messageId) ?? throw new KeyNotFoundException("message_not_found");
        RequireRead(message.ConversationId, playerId);
        DateTimeOffset now=clock.UtcNow; ChatModerationReport report=new(Guid.NewGuid(),messageId,playerId,category,now,"open");
        return repository.SaveModerationReportIdempotent(report,new(playerId,request.ClientRequestId.Trim(),payloadHash,report.ReportId,now));
    }

    private void MaybePurgeReceipts()
    {
        DateTimeOffset now=clock.UtcNow;if(now<nextReceiptPurgeUtc)return;
        lock(rateSync){if(now<nextReceiptPurgeUtc)return;repository.PurgeExpiredReceipts(now.AddDays(-options.IdempotencyReceiptRetentionDays));nextReceiptPurgeUtc=now.AddHours(1);}
    }

    public async Task<CreateAllianceAnnouncementResult> SendAllianceAnnouncementAsync(PlayerId playerId, Guid allianceId, CreateAllianceAnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        ChatAudienceDecision audience = audienceResolver.ResolveAnnouncementAccess(playerId, allianceId, request);
        if (!audience.Allowed)
        {
            throw new UnauthorizedAccessException(audience.ReasonCode ?? "forbidden");
        }

        CreateChatConversationResult conversation = CreateConversation(playerId, new CreateChatConversationRequest(
            ChatChannelType.Alliance,
            request.GameServerId,
            request.WorldId,
            $"alliance:{allianceId:N}",
            "Alliance announcements",
            audience.Participants.Select(participant => participant.Value).ToArray(),
            "announcement_conversation_" + request.ClientRequestId,
            request.RequesterAllianceRole));

        SendChatMessageResult send = await SendMessageAsync(playerId, conversation.Conversation.ConversationId, new SendChatMessageRequest(
            request.ClientRequestId,
            request.Body,
            [new ChatContentPart("text", request.Body, null, null, null, null, null)],
            Array.Empty<ChatMentionInput>(),
            Array.Empty<ChatEmoji>(),
            null,
            clock.UtcNow), cancellationToken);

        return new CreateAllianceAnnouncementResult(conversation.Conversation, send);
    }

    private void EnsureEnabled()
    {
        if (!options.Enabled)
        {
            throw new InvalidOperationException("chat_disabled");
        }
    }

    private ChatConversationParticipant RequireRead(Guid conversationId, PlayerId playerId)
    {
        ChatConversationParticipant? participant = repository.GetParticipant(conversationId, playerId);
        if (participant == null || participant.RemovedAtUtc != null || !participant.CanRead)
        {
            throw new UnauthorizedAccessException("forbidden");
        }

        return participant;
    }

    private ChatConversationParticipant RequireWrite(Guid conversationId, PlayerId playerId)
    {
        ChatConversationParticipant participant = RequireRead(conversationId, playerId);
        if (!participant.CanWrite)
        {
            throw new UnauthorizedAccessException("forbidden");
        }

        return participant;
    }

    private void ValidateConversationRequest(CreateChatConversationRequest request)
    {
        if (request.GameServerId == Guid.Empty || request.WorldId == Guid.Empty)
        {
            throw new ArgumentException("scope_required");
        }

        if (string.IsNullOrWhiteSpace(request.ClientRequestId))
        {
            throw new ArgumentException("client_request_id_required");
        }
        if (request.ClientRequestId.Length > 256 || request.Title?.Length > 256 || request.AudienceKey?.Length > 256)
        {
            throw new ArgumentException("conversation_envelope_too_large");
        }

        if (request.ChannelType == ChatChannelType.Private && (request.ParticipantIds == null || request.ParticipantIds.Count == 0 || request.ParticipantIds.Count > options.MaxPrivateRecipients))
        {
            throw new ArgumentException("private_participants_invalid");
        }

    }

    private void ValidateMessageRequest(SendChatMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientRequestId))
        {
            throw new ArgumentException("client_request_id_required");
        }
        if (request.ClientRequestId.Length > 256)
        {
            throw new ArgumentException("client_request_id_too_large");
        }

        string body = request.Body.Trim();
        if (body.Length == 0)
        {
            throw new ArgumentException("body_required");
        }

        if (body.Length > options.BodyMaxCharacters)
        {
            throw new ArgumentException("body_too_large");
        }
    }

    private static string NormalizeAudienceKey(PlayerId playerId, CreateChatConversationRequest request)
    {
        if (request.ChannelType == ChatChannelType.Private)
        {
            List<Guid> participantIds = (request.ParticipantIds ?? Array.Empty<Guid>()).Append(playerId.Value).Distinct().OrderBy(id => id).ToList();
            return "private:" + string.Join("-", participantIds.Select(id => id.ToString("N")));
        }

        if (!string.IsNullOrWhiteSpace(request.AudienceKey))
        {
            string trimmed = request.AudienceKey.Trim();
            if (request.ChannelType == ChatChannelType.Alliance && !trimmed.StartsWith("alliance:", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("alliance_audience_invalid");
            }

            if (request.ChannelType == ChatChannelType.Leaders && !trimmed.StartsWith("leaders:", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("leaders_audience_invalid");
            }

            return trimmed;
        }

        return request.ChannelType switch
        {
            ChatChannelType.Server => $"server:{request.GameServerId:N}:{request.WorldId:N}",
            ChatChannelType.Alliance => throw new ArgumentException("alliance_audience_required"),
            ChatChannelType.Leaders => throw new ArgumentException("leaders_audience_required"),
            _ => throw new ArgumentOutOfRangeException(nameof(request.ChannelType))
        };
    }

    private static string RetentionPolicyFor(ChatChannelType channelType)
    {
        return channelType switch
        {
            ChatChannelType.Alliance => "alliance_standard",
            ChatChannelType.Server => "server_standard",
            ChatChannelType.Private => "private_standard",
            ChatChannelType.Leaders => "leaders_restricted",
            _ => "chat_standard"
        };
    }

    private static IReadOnlyList<ChatConversationParticipant> BuildParticipants(Guid conversationId, ChatAudienceDecision audience, DateTimeOffset now)
    {
        return audience.Participants
            .Distinct()
            .Select(playerId => new ChatConversationParticipant(conversationId, playerId, audience.RequesterRole, now, null, CanRead: true, CanWrite: true))
            .ToArray();
    }

    private ChatInboxEntry CreateInbox(PlayerId playerId, Guid conversationId, Guid? lastMessageId, DateTimeOffset? activityAt)
    {
        return new ChatInboxEntry(playerId, conversationId, lastMessageId, activityAt, 0, 0, 0, false, false, clock.UtcNow);
    }

    private void UpdateInboxAfterMessage(ChatConversation conversation, ChatMessage message, ChatConversationParticipant sender)
    {
        foreach (ChatConversationParticipant participant in repository.ListParticipants(conversation.ConversationId).Where(item => item.RemovedAtUtc == null && item.CanRead))
        {
            ChatInboxEntry current = repository.GetInbox(participant.PlayerId, conversation.ConversationId)
                ?? CreateInbox(participant.PlayerId, conversation.ConversationId, null, message.AcceptedAtUtc);
            bool fromSender = participant.PlayerId == sender.PlayerId;
            bool mentioned = message.Mentions.Any(mention => mention.PlayerId == participant.PlayerId);
            repository.SaveInbox(current with
            {
                LastMessageId = message.MessageId,
                LastActivityAtUtc = message.AcceptedAtUtc,
                UnreadCount = fromSender ? current.UnreadCount : current.UnreadCount + 1,
                MentionCount = !fromSender && mentioned ? current.MentionCount + 1 : current.MentionCount,
                UpdatedAtUtc = clock.UtcNow
            });
        }
    }

    private static string ComputePayloadHash(SendChatMessageRequest request)
    {
        string payload = JsonSerializer.Serialize(request, JsonOptions);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
