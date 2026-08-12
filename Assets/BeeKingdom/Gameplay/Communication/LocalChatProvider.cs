using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BeeKingdom.Gameplay.Communication
{
    public sealed class LocalChatProvider : IChatProvider
    {
        public const string DefaultFixtureSeed = "bee-kingdom-chat-demo-v1";

        private readonly Dictionary<string, LocalChatUser> users = new Dictionary<string, LocalChatUser>(StringComparer.Ordinal);
        private readonly Dictionary<ConversationId, Conversation> conversations = new Dictionary<ConversationId, Conversation>();
        private readonly Dictionary<ConversationId, List<MessageRecord>> messages = new Dictionary<ConversationId, List<MessageRecord>>();
        private readonly Dictionary<MessageId, MessageRecord> messagesById = new Dictionary<MessageId, MessageRecord>();
        private readonly Dictionary<ClientRequestId, MessageRecord> messagesByRequest = new Dictionary<ClientRequestId, MessageRecord>();
        private readonly Dictionary<ClientRequestId, OutboxEntry> outbox = new Dictionary<ClientRequestId, OutboxEntry>();
        private readonly Dictionary<string, Dictionary<ConversationId, int>> readCursors = new Dictionary<string, Dictionary<ConversationId, int>>(StringComparer.Ordinal);
        private readonly List<Action<ChatEvent>> listeners = new List<Action<ChatEvent>>();
        private readonly IChatClock clock;
        private readonly string fixtureSeed;
        private readonly ChatCapabilities capabilities;
        private ConnectionState connectionState;
        private readonly Dictionary<ConversationId, int> sequenceByConversation = new Dictionary<ConversationId, int>();
        private int eventCounter;
        private int reportCounter;
        private int groupCounter;

        public string CurrentPlayerId { get; }
        public IReadOnlyDictionary<ClientRequestId, OutboxEntry> Outbox => new ReadOnlyDictionary<ClientRequestId, OutboxEntry>(outbox);

        public LocalChatProvider(string currentPlayerId = "player_queen", IChatClock clock = null, bool online = true, bool seedFixtures = true, string fixtureSeed = DefaultFixtureSeed)
        {
            if (string.IsNullOrWhiteSpace(currentPlayerId)) throw new ArgumentException("Current player id is required.", nameof(currentPlayerId));
            CurrentPlayerId = currentPlayerId.Trim();
            this.clock = clock ?? new SystemChatClock();
            this.fixtureSeed = string.IsNullOrWhiteSpace(fixtureSeed) ? DefaultFixtureSeed : fixtureSeed;
            capabilities = new ChatCapabilities(this.fixtureSeed, new LocalChatLimits());
            connectionState = online ? ConnectionState.Online : ConnectionState.Offline;
            AddDefaultUsers();
            if (!users.ContainsKey(CurrentPlayerId))
            {
                users[CurrentPlayerId] = new LocalChatUser(CurrentPlayerId, CurrentPlayerId, "alliance_demo", "server_demo");
            }

            if (seedFixtures)
            {
                SeedFixtures();
            }
        }

        public static LocalChatProvider CreateEmpty(string currentPlayerId = "player_queen", IChatClock clock = null, bool online = true)
        {
            return new LocalChatProvider(currentPlayerId, clock, online, false);
        }

        public static LocalChatProvider CreateFixtureProvider(IChatClock clock = null)
        {
            return new LocalChatProvider("player_queen", clock, true, true);
        }

        public ChatCapabilities GetCapabilities() => capabilities;
        public ConnectionState GetConnectionState() => connectionState;

        public void AddOrUpdateUser(LocalChatUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            users[user.PlayerId] = user;
        }

        public ConversationPage ListConversations(string userId, ConversationFilter filter = null)
        {
            if (!users.ContainsKey(userId)) return new ConversationPage(null);
            IEnumerable<Conversation> result = conversations.Values.Where(conversation => CanRead(conversation, userId));
            if (filter != null)
            {
                if (filter.ChannelType.HasValue) result = result.Where(conversation => conversation.ChannelType == filter.ChannelType.Value);
                if (!filter.IncludeArchived) result = result.Where(conversation => !conversation.IsArchivedFor(userId));
                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    result = result.Where(conversation => conversation.Title.IndexOf(filter.SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
                }
            }

            return new ConversationPage(result.OrderByDescending(conversation => conversation.LastActivityAt ?? conversation.CreatedAt).ThenBy(conversation => conversation.Id.Value, StringComparer.Ordinal));
        }

        public MessagePage GetMessages(ConversationId conversationId, int afterSequence = 0, int limit = 50)
        {
            if (!conversations.TryGetValue(conversationId, out Conversation conversation)) return new MessagePage(null);
            EnsureCanRead(conversation, CurrentPlayerId);
            int safeLimit = Math.Max(1, Math.Min(limit, 100));
            List<MessageRecord> all = messages[conversationId].Where(message => message.Sequence.GetValueOrDefault() > afterSequence && message.State != MessageState.Expired).OrderBy(message => message.Sequence).ToList();
            bool hasMore = all.Count > safeLimit;
            List<MessageRecord> page = all.Take(safeLimit).ToList();
            return new MessagePage(page, page.Count == 0 ? (int?)null : page[page.Count - 1].Sequence, hasMore);
        }

        public Conversation CreateConversation(CreateConversationInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            LocalChatUser current = GetUser(CurrentPlayerId);
            List<string> participantIds;
            ConversationId conversationId;
            string title = input.Title;

            switch (input.ChannelType)
            {
                case ChannelType.Alliance:
                    RequireContext(input.ContextId, nameof(input.ContextId));
                    if (!string.Equals(current.AllianceId, input.ContextId, StringComparison.Ordinal)) throw new InvalidOperationException("Current player is outside the alliance audience.");
                    conversationId = ConversationId.ForAlliance(input.ContextId);
                    participantIds = Audience(input.ParticipantIds, user => string.Equals(user.AllianceId, input.ContextId, StringComparison.Ordinal));
                    break;
                case ChannelType.Server:
                    RequireContext(input.ContextId, nameof(input.ContextId));
                    if (!string.Equals(current.ServerId, input.ContextId, StringComparison.Ordinal)) throw new InvalidOperationException("Current player is outside the server audience.");
                    conversationId = ConversationId.ForServer(input.ContextId);
                    participantIds = Audience(input.ParticipantIds, user => string.Equals(user.ServerId, input.ContextId, StringComparison.Ordinal));
                    break;
                case ChannelType.Leadership:
                    RequireContext(input.ContextId, nameof(input.ContextId));
                    if (!string.Equals(current.AllianceId, input.ContextId, StringComparison.Ordinal) || !IsLeadership(current)) throw new InvalidOperationException("Leadership permission is required.");
                    conversationId = ConversationId.ForLeadership(input.ContextId);
                    participantIds = Audience(input.ParticipantIds, user => string.Equals(user.AllianceId, input.ContextId, StringComparison.Ordinal) && IsLeadership(user));
                    break;
                case ChannelType.Private:
                    participantIds = input.ParticipantIds.Concat(new[] { CurrentPlayerId }).Distinct(StringComparer.Ordinal).ToList();
                    if (participantIds.Count < 2 || participantIds.Count > capabilities.Limits.MaxPrivateRecipients + 1) throw new InvalidOperationException("A private conversation needs a valid audience.");
                    foreach (string participantId in participantIds) GetUser(participantId);
                    conversationId = participantIds.Count == 2
                        ? ConversationId.ForPrivatePair(participantIds[0], participantIds[1])
                        : new ConversationId("private:group:" + fixtureSeed + ":" + (++groupCounter).ToString("D4"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (conversations.TryGetValue(conversationId, out Conversation existing)) return existing;
            Conversation created = new Conversation(conversationId, input.ChannelType, title, participantIds, CurrentPlayerId, clock.UtcNow);
            conversations.Add(conversationId, created);
            messages.Add(conversationId, new List<MessageRecord>());
            Publish(new ChatEvent(NextEventId(), ChatEventType.ConversationCreated, clock.UtcNow, conversationId, null, CurrentPlayerId, created));
            return created;
        }

        public SendResult SendMessage(SendMessageInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (messagesByRequest.TryGetValue(input.ClientRequestId, out MessageRecord previous)) return new SendResult(previous, true);
            if (!conversations.TryGetValue(input.ConversationId, out Conversation conversation)) return Failure(input, SendFailureCode.UnknownConversation);
            if (!string.Equals(input.SenderId, CurrentPlayerId, StringComparison.Ordinal) || !CanWrite(conversation, CurrentPlayerId)) return Failure(input, SendFailureCode.Forbidden, conversation.ChannelType);
            if (string.IsNullOrWhiteSpace(input.Body)) return Failure(input, SendFailureCode.EmptyBody, conversation.ChannelType);
            if (input.Body.Trim().Length > capabilities.Limits.MaxBodyCharacters) return Failure(input, SendFailureCode.BodyTooLong, conversation.ChannelType);
            if (!ValidateRecipients(conversation, input)) return Failure(input, SendFailureCode.InvalidRecipient, conversation.ChannelType);

            MessageRecord recent = messages[conversation.Id].LastOrDefault(message => message.SenderId == CurrentPlayerId && message.State != MessageState.Failed);
            if (recent != null && string.Equals(recent.Body, input.Body, StringComparison.Ordinal) && clock.UtcNow - recent.ClientCreatedAt < capabilities.Limits.DuplicateWindow)
            {
                return Failure(input, SendFailureCode.DuplicateSuppressed, conversation.ChannelType);
            }

            MessageRecord message = CreateMessage(conversation, input, MessageState.Queued);
            messagesByRequest[input.ClientRequestId] = message;
            messagesById[message.MessageId] = message;
            if (connectionState == ConnectionState.Offline)
            {
                OutboxEntry pending = new OutboxEntry(input.ClientRequestId, input.ConversationId, message.MessageId, BuildPayloadHash(input), clock.UtcNow);
                outbox[input.ClientRequestId] = pending;
                Publish(new ChatEvent(NextEventId(), ChatEventType.MessageQueued, clock.UtcNow, conversation.Id, null, CurrentPlayerId, message));
                return new SendResult(message);
            }

            return Accept(message, input);
        }

        public SendResult RetryMessage(ClientRequestId clientRequestId)
        {
            if (!messagesByRequest.TryGetValue(clientRequestId, out MessageRecord message)) return new SendResult(null, false, SendFailureCode.InvalidRequest, "unknown_request");
            if (message.State == MessageState.Accepted || message.State == MessageState.Delivered || message.State == MessageState.Hidden) return new SendResult(message, true);
            if (!outbox.ContainsKey(clientRequestId)) return new SendResult(message, false, SendFailureCode.InvalidRequest, "not_retryable");
            if (connectionState == ConnectionState.Offline)
            {
                outbox[clientRequestId].LastErrorCode = SendFailureCode.Offline.ToString().ToLowerInvariant();
                outbox[clientRequestId].AttemptCount++;
                return new SendResult(message, false, SendFailureCode.Offline);
            }

            outbox[clientRequestId].AttemptCount++;
            SendResult result = Accept(message, null);
            if (result.Accepted) outbox.Remove(clientRequestId);
            else outbox[clientRequestId].LastErrorCode = result.ErrorCode;
            return result;
        }

        public int FlushOutbox()
        {
            if (connectionState == ConnectionState.Offline) return 0;
            int accepted = 0;
            foreach (ClientRequestId requestId in outbox.Keys.ToList())
            {
                SendResult result = RetryMessage(requestId);
                if (result.Accepted) accepted++;
            }
            return accepted;
        }

        public void SetConnectionState(ConnectionState state)
        {
            if (connectionState == state) return;
            connectionState = state;
            Publish(new ChatEvent(NextEventId(), ChatEventType.ProviderStatusChanged, clock.UtcNow, default(ConversationId), null, CurrentPlayerId, state));
            if (state == ConnectionState.Online) FlushOutbox();
        }

        public void SetOnline(bool online) => SetConnectionState(online ? ConnectionState.Online : ConnectionState.Offline);
        public void GoOffline() => SetConnectionState(ConnectionState.Offline);
        public void Reconnect() => SetConnectionState(ConnectionState.Online);

        public ReadCursor MarkConversationRead(ConversationId conversationId, int sequence)
        {
            return MarkConversationReadFor(CurrentPlayerId, conversationId, sequence);
        }

        public ReadCursor MarkConversationReadFor(string userId, ConversationId conversationId, int sequence)
        {
            if (!conversations.TryGetValue(conversationId, out Conversation conversation)) throw new InvalidOperationException("Unknown conversation.");
            EnsureCanRead(conversation, userId);
            int lastSequence = messages[conversationId].Where(message => message.Sequence.HasValue).Select(message => message.Sequence.Value).DefaultIfEmpty(0).Max();
            int requested = Math.Max(0, Math.Min(sequence, lastSequence));
            int current = GetReadCursor(userId, conversationId);
            int next = Math.Max(current, requested);
            readCursors[userId][conversationId] = next;
            PublishInboxUpdated(conversationId, userId);
            return new ReadCursor(userId, conversationId, next);
        }

        public InboxEntry SetMuted(ConversationId conversationId, bool muted)
        {
            if (!conversations.TryGetValue(conversationId, out Conversation conversation)) throw new InvalidOperationException("Unknown conversation.");
            EnsureCanRead(conversation, CurrentPlayerId);
            conversation.SetMuted(CurrentPlayerId, muted);
            return GetInboxEntry(CurrentPlayerId, conversationId);
        }

        public void ArchiveConversation(ConversationId conversationId, bool archived = true) => ArchiveConversationFor(CurrentPlayerId, conversationId, archived);

        public void ArchiveConversationFor(string userId, ConversationId conversationId, bool archived = true)
        {
            if (!conversations.TryGetValue(conversationId, out Conversation conversation)) throw new InvalidOperationException("Unknown conversation.");
            EnsureCanRead(conversation, userId);
            conversation.SetArchived(userId, archived);
            PublishInboxUpdated(conversationId, userId);
        }

        public InboxEntry GetInboxEntry(string userId, ConversationId conversationId)
        {
            if (!conversations.TryGetValue(conversationId, out Conversation conversation) || !CanRead(conversation, userId)) return null;
            InboxEntry entry = new InboxEntry(userId, conversationId)
            {
                LastMessageId = conversation.LastMessageId,
                LastActivityAt = conversation.LastActivityAt,
                IsMuted = conversation.IsMutedFor(userId),
                IsArchived = conversation.IsArchivedFor(userId),
                ReadCursor = GetReadCursor(userId, conversationId)
            };
            IEnumerable<MessageRecord> visible = messages[conversationId].Where(message => message.Sequence.HasValue && message.Sequence.Value > entry.ReadCursor && message.State != MessageState.Hidden && message.State != MessageState.Deleted && message.State != MessageState.Expired && message.SenderId != userId);
            entry.UnreadCount = visible.Count();
            entry.MentionCount = visible.Count(message => message.Mentions.Contains(userId, StringComparer.Ordinal));
            return entry;
        }

        public IReadOnlyList<InboxEntry> GetInbox(string userId)
        {
            return new ReadOnlyCollection<InboxEntry>(ListConversations(userId, new ConversationFilter { IncludeArchived = true }).Items.Select(conversation => GetInboxEntry(userId, conversation.Id)).ToList());
        }

        public int GetTotalUnreadCount(string userId)
        {
            return GetInbox(userId).Where(entry => !entry.IsArchived).Sum(entry => entry.UnreadCount);
        }

        public IReadOnlyList<OutboxEntry> GetPendingOutbox()
        {
            return new ReadOnlyCollection<OutboxEntry>(outbox.Values.OrderBy(entry => entry.CreatedAt).ToList());
        }

        public bool DeleteMessage(MessageId messageId, string actorId = null)
        {
            if (!messagesById.TryGetValue(messageId, out MessageRecord message)) return false;
            string actor = string.IsNullOrWhiteSpace(actorId) ? CurrentPlayerId : actorId;
            LocalChatUser user = GetUser(actor);
            if (!string.Equals(actor, message.SenderId, StringComparison.Ordinal) && !IsLeadership(user) && !user.HasRole(ChatRole.Moderator)) return false;
            message.State = MessageState.Deleted;
            message.DeletedAt = clock.UtcNow;
            message.Body = "[message deleted]";
            Publish(new ChatEvent(NextEventId(), ChatEventType.MessageDeleted, clock.UtcNow, message.ConversationId, message.Sequence, actor, message));
            PublishInboxUpdated(message.ConversationId, actor);
            return true;
        }

        public ModerationReport ReportMessage(MessageId messageId, string category)
        {
            if (!messagesById.ContainsKey(messageId)) throw new InvalidOperationException("Unknown message.");
            return new ModerationReport("report_local_" + (++reportCounter).ToString("D6"), messageId, CurrentPlayerId, string.IsNullOrWhiteSpace(category) ? "other" : category.Trim(), clock.UtcNow);
        }

        public IDisposable Subscribe(Action<ChatEvent> listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            listeners.Add(listener);
            return new Subscription(() => listeners.Remove(listener));
        }

        private SendResult Accept(MessageRecord message, SendMessageInput input)
        {
            Conversation conversation = conversations[message.ConversationId];
            SendFailureCode moderationFailure = ApplyModeration(message);
            if (moderationFailure != SendFailureCode.None)
            {
                if (moderationFailure == SendFailureCode.Masked)
                {
                    message.State = MessageState.Hidden;
                    message.AcceptedAt = clock.UtcNow;
                    message.Sequence = NextSequence(conversation.Id);
                    messages[conversation.Id].Add(message);
                    conversation.LastMessageId = message.MessageId;
                    conversation.LastActivityAt = message.AcceptedAt;
                    outbox.Remove(message.ClientRequestId);
                    Publish(new ChatEvent(NextEventId(), ChatEventType.MessageCreated, clock.UtcNow, conversation.Id, message.Sequence, message.SenderId, message));
                }
                else
                {
                    message.State = MessageState.Failed;
                }
                return new SendResult(message, false, moderationFailure);
            }

            message.State = MessageState.Accepted;
            message.AcceptedAt = clock.UtcNow;
            message.Sequence = NextSequence(conversation.Id);
            messages[conversation.Id].Add(message);
            conversation.LastMessageId = message.MessageId;
            conversation.LastActivityAt = message.AcceptedAt;
            outbox.Remove(message.ClientRequestId);
            Publish(new ChatEvent(NextEventId(), ChatEventType.MessageCreated, clock.UtcNow, conversation.Id, message.Sequence, message.SenderId, message));
            foreach (string participantId in conversation.ParticipantIds) PublishInboxUpdated(conversation.Id, participantId);
            return new SendResult(message);
        }

        private SendFailureCode ApplyModeration(MessageRecord message)
        {
            string normalized = string.Join(" ", message.Body.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
            if (normalized.Contains("[blocked]") || normalized.Contains("blocked-term"))
            {
                message.Moderation.Status = ModerationStatus.Blocked;
                message.Moderation.ReasonCode = "blocked_term";
                message.Moderation.CheckedAt = clock.UtcNow;
                Publish(new ChatEvent(NextEventId(), ChatEventType.MessageModerated, clock.UtcNow, message.ConversationId, null, message.SenderId, message));
                return SendFailureCode.Blocked;
            }
            if (normalized.Contains("[masked]"))
            {
                message.Moderation.Status = ModerationStatus.Masked;
                message.Moderation.ReasonCode = "fixture_masked";
                message.Moderation.CheckedAt = clock.UtcNow;
                Publish(new ChatEvent(NextEventId(), ChatEventType.MessageModerated, clock.UtcNow, message.ConversationId, null, message.SenderId, message));
                return SendFailureCode.Masked;
            }
            message.Moderation.Status = ModerationStatus.Clear;
            message.Moderation.CheckedAt = clock.UtcNow;
            return SendFailureCode.None;
        }

        private int NextSequence(ConversationId conversationId)
        {
            int next = sequenceByConversation.TryGetValue(conversationId, out int current) ? current + 1 : 1;
            sequenceByConversation[conversationId] = next;
            return next;
        }

        private MessageRecord CreateMessage(Conversation conversation, SendMessageInput input, MessageState state)
        {
            LocalChatUser sender = GetUser(CurrentPlayerId);
            return new MessageRecord(
                MessageId.ForClientRequest(fixtureSeed, input.ClientRequestId),
                conversation.Id,
                conversation.ChannelType,
                sender.PlayerId,
                sender.DisplayName,
                conversation.ChannelType == ChannelType.Private ? input.RecipientIds : Enumerable.Empty<string>(),
                input.Body,
                input.Mentions,
                input.ReplyToMessageId,
                input.ClientCreatedAt ?? clock.UtcNow,
                input.ClientRequestId,
                state,
                new ModerationInfo { Status = ModerationStatus.Pending, PolicyVersion = "local-v1" });
        }

        private SendResult Failure(SendMessageInput input, SendFailureCode failureCode, ChannelType channelType = ChannelType.Private)
        {
            MessageId messageId = MessageId.ForClientRequest(fixtureSeed, input.ClientRequestId);
            ModerationInfo moderation = new ModerationInfo { Status = failureCode == SendFailureCode.Blocked ? ModerationStatus.Blocked : ModerationStatus.Pending, ReasonCode = failureCode.ToString().ToLowerInvariant(), CheckedAt = clock.UtcNow, PolicyVersion = "local-v1" };
            MessageRecord failed = new MessageRecord(messageId, input.ConversationId, channelType, input.SenderId, input.SenderId, input.RecipientIds, input.Body, input.Mentions, input.ReplyToMessageId, input.ClientCreatedAt ?? clock.UtcNow, input.ClientRequestId, MessageState.Failed, moderation);
            return new SendResult(failed, false, failureCode);
        }

        private bool ValidateRecipients(Conversation conversation, SendMessageInput input)
        {
            if (conversation.ChannelType != ChannelType.Private && input.RecipientIds.Count != 0) return false;
            if (conversation.ChannelType == ChannelType.Private)
            {
                if (input.RecipientIds.Count == 0) return false;
                if (input.RecipientIds.Any(recipient => !conversation.HasParticipant(recipient) || !users.ContainsKey(recipient))) return false;
            }
            return input.Mentions.All(mention => conversation.HasParticipant(mention));
        }

        private bool CanRead(Conversation conversation, string userId)
        {
            if (!conversation.HasParticipant(userId) || !users.TryGetValue(userId, out LocalChatUser user)) return false;
            if (conversation.ChannelType == ChannelType.Leadership) return IsLeadership(user);
            return true;
        }

        private bool CanWrite(Conversation conversation, string userId)
        {
            if (!CanRead(conversation, userId) || !users.TryGetValue(userId, out LocalChatUser user) || user.IsSuspended) return false;
            if (conversation.ChannelType == ChannelType.Server && !user.IsConnected) return false;
            return conversation.ChannelType != ChannelType.Leadership || IsLeadership(user);
        }

        private void EnsureCanRead(Conversation conversation, string userId)
        {
            if (!CanRead(conversation, userId)) throw new UnauthorizedAccessException("Chat read permission denied.");
        }

        private LocalChatUser GetUser(string userId)
        {
            if (!users.TryGetValue(userId, out LocalChatUser user)) throw new InvalidOperationException("Unknown local chat user: " + userId);
            return user;
        }

        private static bool IsLeadership(LocalChatUser user) => user.HasRole(ChatRole.Leader) || user.HasRole(ChatRole.Officer) || user.HasRole(ChatRole.Moderator);

        private List<string> Audience(IEnumerable<string> requested, Func<LocalChatUser, bool> predicate)
        {
            IEnumerable<string> source = requested == null || !requested.Any() ? users.Values.Where(predicate).Select(user => user.PlayerId) : requested;
            return source.Distinct(StringComparer.Ordinal).Select(GetUser).Where(predicate).Select(user => user.PlayerId).OrderBy(value => value, StringComparer.Ordinal).ToList();
        }

        private int GetReadCursor(string userId, ConversationId conversationId)
        {
            if (!readCursors.TryGetValue(userId, out Dictionary<ConversationId, int> byConversation))
            {
                byConversation = new Dictionary<ConversationId, int>();
                readCursors[userId] = byConversation;
            }
            return byConversation.TryGetValue(conversationId, out int cursor) ? cursor : 0;
        }

        private void PublishInboxUpdated(ConversationId conversationId, string userId)
        {
            if (!CanRead(conversations[conversationId], userId)) return;
            Publish(new ChatEvent(NextEventId(), ChatEventType.InboxUpdated, clock.UtcNow, conversationId, null, userId, GetInboxEntry(userId, conversationId)));
        }

        private void Publish(ChatEvent chatEvent)
        {
            foreach (Action<ChatEvent> listener in listeners.ToList()) listener(chatEvent);
        }

        private string NextEventId() => "evt_local_" + (++eventCounter).ToString("D6");

        private static string BuildPayloadHash(SendMessageInput input)
        {
            return (input.ConversationId.Value + "|" + input.SenderId + "|" + input.Body + "|" + string.Join(",", input.RecipientIds.OrderBy(value => value, StringComparer.Ordinal))).GetHashCode().ToString("X8");
        }

        private static void RequireContext(string contextId, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(contextId)) throw new ArgumentException("A channel context is required.", parameterName);
        }

        private void AddDefaultUsers()
        {
            AddOrUpdateUser(new LocalChatUser("player_queen", "Queen", "alliance_demo", "server_demo", new[] { ChatRole.Leader }));
            AddOrUpdateUser(new LocalChatUser("player_officer", "Officer", "alliance_demo", "server_demo", new[] { ChatRole.Officer }));
            AddOrUpdateUser(new LocalChatUser("player_member", "Member", "alliance_demo", "server_demo"));
            AddOrUpdateUser(new LocalChatUser("player_scout", "Scout", "alliance_demo", "server_demo"));
            AddOrUpdateUser(new LocalChatUser("player_outsider", "Outsider", "other_alliance", "other_server"));
        }

        private void SeedFixtures()
        {
            Conversation alliance = CreateConversation(new CreateConversationInput(ChannelType.Alliance, "alliance_demo", "Demo Alliance", null));
            Conversation server = CreateConversation(new CreateConversationInput(ChannelType.Server, "server_demo", "Demo Server", null));
            Conversation leadership = CreateConversation(new CreateConversationInput(ChannelType.Leadership, "alliance_demo", "Leadership", null));
            Conversation privateChat = CreateConversation(new CreateConversationInput(ChannelType.Private, null, "Queen, Scout", new[] { "player_scout" }));
            CreateConversation(new CreateConversationInput(ChannelType.Private, null, "Empty conversation", new[] { "player_member" }));
            AddFixtureMessage(alliance, "fixture_alliance_0001", "Rendez-vous a la porte nord !", null);
            AddFixtureMessage(server, "fixture_server_0001", "Bienvenue sur le serveur local.", null);
            AddFixtureMessage(server, "fixture_server_masked_0001", "[masked] Fixture de moderation locale.", null);
            AddFixtureMessage(leadership, "fixture_leaders_0001", "Annonce dirigeants : la prochaine rotation est planifiee.", new[] { "player_officer" });
            AddFixtureMessage(privateChat, "fixture_private_0001", "Je reste disponible hors ligne.", new[] { "player_scout" });
            SetOnline(false);
            AddFixtureMessage(privateChat, "fixture_private_offline_0001", "Message fixture hors ligne.", new[] { "player_scout" });
            SetOnline(true);
        }

        private void AddFixtureMessage(Conversation conversation, string requestId, string body, IEnumerable<string> mentions)
        {
            SendMessageInput input = new SendMessageInput(conversation.Id, CurrentPlayerId, body, new ClientRequestId(requestId), conversation.ChannelType == ChannelType.Private ? new[] { "player_scout" } : null, mentions, null, clock.UtcNow);
            SendMessage(input);
        }

        private sealed class Subscription : IDisposable
        {
            private Action dispose;
            public Subscription(Action dispose) { this.dispose = dispose; }
            public void Dispose()
            {
                Action action = dispose;
                dispose = null;
                action?.Invoke();
            }
        }

        private sealed class SystemChatClock : IChatClock
        {
            public DateTime UtcNow => DateTime.UtcNow;
        }
    }
}
