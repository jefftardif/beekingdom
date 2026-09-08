using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.Gameplay.Communication
{
    public enum LivingHiveChatStatus { NotConfigured, Connecting, Online, Polling, Offline, AuthenticationRequired, Unavailable, Error }
    public enum LivingHiveChatDelivery { Confirmed, Queued, Failed }

    public sealed class LivingHiveChatConversation
    {
        public string ConversationId { get; set; }
        public string Title { get; set; }
        public string ChannelType { get; set; }
        public long LastSequence { get; set; }
        public int UnreadCount { get; set; }
        public int MentionCount { get; set; }
    }

    public sealed class LivingHiveChatMessage
    {
        public string MessageId { get; set; }
        public string ConversationId { get; set; }
        public string ClientRequestId { get; set; }
        public string SenderPlayerId { get; set; }
        public string SenderDisplayName { get; set; }
        public string OriginalBody { get; set; }
        public string VisibleBody { get; set; }
        public string SourceLocale { get; set; }
        public string TargetLocale { get; set; }
        public long Sequence { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public LivingHiveChatDelivery Delivery { get; set; }
        public bool IsTranslated { get; set; }
    }

    // RAP-OPTIONNEL-COMMUNICATIONS_01
    public sealed class LivingHiveChatGroupMember
    {
        public string PlayerId { get; set; }
        public string DisplayName { get; set; }
        public bool IsLeader { get; set; }
    }

    public sealed class LivingHiveChatInvitation
    {
        public string InviteId { get; set; }
        public string ConversationId { get; set; }
        public string GroupTitle { get; set; }
        public string InviterPlayerId { get; set; }
        public string InviterDisplayName { get; set; }
        public string InviteePlayerId { get; set; }
        public string InviteeDisplayName { get; set; }
        public string Status { get; set; }
    }

    public sealed class LivingHiveChatGroupDetail
    {
        public string ConversationId { get; set; }
        public string Title { get; set; }
        public string OwnerPlayerId { get; set; }
        public bool ViewerIsLeader { get; set; }
        public IReadOnlyList<LivingHiveChatGroupMember> Members { get; set; } = Array.Empty<LivingHiveChatGroupMember>();
        public IReadOnlyList<LivingHiveChatInvitation> PendingInvites { get; set; } = Array.Empty<LivingHiveChatInvitation>();
    }

    public sealed class LivingHiveChatSnapshot
    {
        public LivingHiveChatStatus Status { get; set; }
        public string ErrorCode { get; set; }
        public string SelectedConversationId { get; set; }
        public IReadOnlyList<LivingHiveChatConversation> Conversations { get; set; } = Array.Empty<LivingHiveChatConversation>();
        public IReadOnlyList<LivingHiveChatMessage> Messages { get; set; } = Array.Empty<LivingHiveChatMessage>();
        public int PendingCount { get; set; }
        public bool TranslationAvailable { get; set; }
        public string TranslationModelVersion { get; set; }
        // RAP-OPTIONNEL-COMMUNICATIONS_01 - invitations the viewer has received and not answered yet.
        // Surfaced in the snapshot (rather than through an event) so the global alert can be drawn by
        // the IMGUI root loop, which only ever reads a snapshot.
        public IReadOnlyList<LivingHiveChatInvitation> PendingInvitations { get; set; } = Array.Empty<LivingHiveChatInvitation>();
        // Answers to invitations the viewer SENT, not yet acknowledged: the "X a refuse de rejoindre Y"
        // toast source. Acknowledged after display so the toast is shown once.
        public IReadOnlyList<LivingHiveChatInvitation> SentInvitationResponses { get; set; } = Array.Empty<LivingHiveChatInvitation>();
        public LivingHiveChatGroupDetail SelectedGroup { get; set; }
        public string AutoInviteResponse { get; set; } = RemoteChatAutoInviteResponse.Ask;
        public bool GroupScopeConfigured { get; set; }
        public int TotalUnread => Conversations.Sum(value => Math.Max(0, value.UnreadCount));
        public LivingHiveChatMessage LastMessage => Messages.LastOrDefault();
    }

    public sealed partial class LivingHiveChatController
    {
        public const int DefaultRecentMessageLimit = 100;
        private readonly ServerChatProvider provider;
        private readonly ChatTranslationController translations;
        private readonly int recentMessageLimit;
        private readonly IChatDelay delay;
        private readonly TimeSpan pollInterval;
        private readonly IChatRecentCache recentCache;
        private readonly object gate = new object();
        private readonly List<LivingHiveChatConversation> conversations = new List<LivingHiveChatConversation>();
        private readonly List<LivingHiveChatMessage> messages = new List<LivingHiveChatMessage>();
        private LivingHiveChatStatus status = LivingHiveChatStatus.Offline;
        private string errorCode;
        private string selectedConversationId;
        private int pendingCount;
        private CancellationTokenSource liveUpdates;
        private Task pollingTask = Task.CompletedTask;
        private Task realtimeReceiptTask = Task.CompletedTask;
        // M059D-CL - indique si OpenAsync est encore en vol : utilise par
        // LivingHiveChatBridgeBootstrap.Update() pour eviter de relancer une ouverture en double
        // pendant que la precedente tourne encore.
        private volatile bool openInFlight;
        public bool OpenInProgressForDiagnostics => openInFlight;

        public LivingHiveChatController(ServerChatProvider provider, int recentMessageLimit = DefaultRecentMessageLimit, IChatDelay delay = null, TimeSpan? pollInterval = null, IChatRecentCache recentCache = null)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            if (recentMessageLimit < 20 || recentMessageLimit > 500) throw new ArgumentOutOfRangeException(nameof(recentMessageLimit));
            this.recentMessageLimit = recentMessageLimit;
            this.delay = delay ?? new SystemChatDelay();
            this.pollInterval = pollInterval ?? TimeSpan.FromSeconds(5);
            this.recentCache = recentCache;
            if (this.pollInterval < TimeSpan.FromSeconds(1) || this.pollInterval > TimeSpan.FromMinutes(1)) throw new ArgumentOutOfRangeException(nameof(pollInterval));
            translations = new ChatTranslationController(provider);
            provider.RealtimeEventApplied += OnRealtimeEventApplied;
        }

        public LivingHiveChatSnapshot Snapshot()
        {
            lock (gate)
            {
                RemoteCapabilities capabilities = provider.NegotiatedCapabilities;
                return new LivingHiveChatSnapshot
                {
                    Status = status,
                    ErrorCode = errorCode,
                    SelectedConversationId = selectedConversationId,
                    PendingCount = pendingCount,
                    Conversations = conversations.Select(Clone).ToArray(),
                    Messages = messages.Select(Clone).ToArray(),
                    TranslationAvailable = capabilities != null && capabilities.TranslationAvailable,
                    TranslationModelVersion = capabilities?.TranslationModelVersion,
                    PendingInvitations = pendingInvitations.Select(Clone).ToArray(),
                    SentInvitationResponses = sentInvitationResponses.Select(Clone).ToArray(),
                    SelectedGroup = CloneSelectedGroupDetail(),
                    AutoInviteResponse = autoInviteResponse,
                    GroupScopeConfigured = HasGroupScope
                };
            }
        }

        public async Task OpenAsync(CancellationToken ct)
        {
            RestoreRecentCache();
            SetStatus(LivingHiveChatStatus.Connecting, null);
            openInFlight = true;
            try
            {
                bool alreadyConnected = provider.ConnectionState == RemoteChatConnectionState.Realtime
                    || provider.ConnectionState == RemoteChatConnectionState.Polling;
                if (!alreadyConnected) await provider.ConnectAsync(ct);
                RemoteConversationLoadResult loaded = await provider.LoadAllConversationsAsync(new ChatPaginationPolicy(50, 10), ct);
                IReadOnlyList<RemoteConversation> accessible = loaded?.Items ?? Array.Empty<RemoteConversation>();
                lock (gate)
                {
                    conversations.Clear();
                    conversations.AddRange(accessible.Where(value => value != null && !string.IsNullOrWhiteSpace(value.ConversationId)).Select(MapConversation));
                    if (!conversations.Any(value => string.Equals(value.ConversationId, selectedConversationId, StringComparison.Ordinal)))
                        selectedConversationId = conversations.FirstOrDefault()?.ConversationId;
                }
                await provider.EnsureRealtimeSubscriptionsAsync(accessible.Select(value => value.ConversationId), ct);
                // RAP-OPTIONNEL-COMMUNICATIONS_01 : le scope monde vient des capacites negociees,
                // jamais d'une valeur inventee cote client. S'il manque (serveur plus ancien), la
                // creation de groupe reste refusee et l'interface le dit.
                RemoteCapabilities negotiated = provider.NegotiatedCapabilities;
                if (negotiated != null) ConfigureGroupScope(negotiated.GameServerId, negotiated.DefaultWorldId);
                // Volontairement PAS de sondage des invitations/preferences ici : OpenAsync est le
                // chemin critique d'ouverture, teste par une longue suite existante dont les faux
                // transports comptent les appels. Les invitations arrivent de toute facon au premier
                // tick de sondage, et les preferences sont relues a l'ouverture de l'ecran
                // Parametres - deux appels reseau de moins a l'ouverture, aucun comportement perdu.
                await RefreshSelectedAsync(ct);
                SetStatus(provider.ConnectionState == RemoteChatConnectionState.Realtime ? LivingHiveChatStatus.Online : LivingHiveChatStatus.Polling, null);
                EnsureLiveUpdates(ct, provider.ConnectionState == RemoteChatConnectionState.Polling);
            }
            catch (RemoteChatTransportException exception)
            {
                SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString());
            }
            // M059D-CL - preuve runtime (session CEO du 2026-09-07, stack trace complete) :
            // OpenAsync -> ConnectAsync -> NegotiateCapabilitiesAsync -> Send -> le transport HTTP
            // observe l'annulation du CancellationToken qui lui a ete transmis et leve
            // TaskCanceledException/OperationCanceledException. Dans cette architecture, AUCUN
            // timeout reseau ne passe par l'annulation d'un token : un timeout UnityWebRequest
            // ressort en ConnectionError -> RemoteChatTransportException (catch ci-dessus), jamais
            // en OperationCanceledException. Donc quand ct.IsCancellationRequested est vrai ici,
            // c'est TOUJOURS parce que le proprietaire de ce token (ReconfigureAsync/ResetAsync sur
            // LivingHiveChatRuntime, ou le transition token de LivingHiveChatSessionCoordinator) l'a
            // annule deliberement pour superseder cette ouverture par une autre - jamais un accident
            // reseau. On journalise et on rend un etat coherent (Offline, jamais bloque a Connecting
            // pour le reste de la session) sans relancer : cette ouverture est fire-and-forget
            // (LivingHiveChatBridgeBootstrap.Update()), personne n'attend cette tache, donc laisser
            // l'exception se propager ne ferait que produire un fault non observe.
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                SetStatus(LivingHiveChatStatus.Offline, "local_open_superseded");
            }
            // Meme raisonnement que ci-dessus pour tout type d'exception non prevu par les deux
            // catch precedents : journalise avec le type exact (jamais avale silencieusement dans
            // la tache fire-and-forget) et rend un etat coherent au lieu de laisser Connecting
            // bloque indefiniment.
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError("LivingHiveChatController.OpenAsync - unexpected exception type: " + exception.GetType().FullName + " - " + exception.Message);
                SetStatus(LivingHiveChatStatus.Error, "local_open_unexpected_exception");
            }
            finally
            {
                openInFlight = false;
            }
        }

        public async Task SelectConversationAsync(string conversationId, CancellationToken ct)
        {
            lock (gate)
            {
                if (!conversations.Any(value => string.Equals(value.ConversationId, conversationId, StringComparison.Ordinal))) throw new ArgumentException("Conversation is not accessible in the current server snapshot.", nameof(conversationId));
                selectedConversationId = conversationId;
                messages.Clear();
            }
            await RefreshSelectedAsync(ct);
        }

        // M043Q-CL: opens a conversation the caller already knows the id of (e.g. Alliance.ChatConversationId)
        // even when it hasn't shown up yet in the aggregate "list all my conversations" result -
        // SelectConversationAsync refuses anything not already in that cached list, but the underlying
        // wire calls (ReconcileFullyAsync/SendAsync, both reused as-is via RefreshSelectedAsync) only
        // ever needed a raw conversation id, so this is additive, not a fork of the read/send path.
        public async Task SelectKnownConversationAsync(string conversationId, string title, string channelType, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("A conversation id is required.", nameof(conversationId));
            lock (gate)
            {
                if (!conversations.Any(value => string.Equals(value.ConversationId, conversationId, StringComparison.Ordinal)))
                {
                    conversations.Add(new LivingHiveChatConversation { ConversationId = conversationId, Title = title, ChannelType = channelType });
                }
                selectedConversationId = conversationId;
                messages.Clear();
            }
            await RefreshSelectedAsync(ct);
        }

        // M059C-CL - le "Discuter" du selecteur de joueur (player search -> Nouvelle discussion)
        // n'a JAMAIS appele aucun endpoint : il se contentait de basculer sur l'onglet "Private"
        // et de choisir la PREMIERE conversation privee existante, sans rapport avec le joueur
        // reellement tape. Cette methode complete le cablage manquant en reutilisant exactement
        // le meme point d'entree serveur que /chat/v1/conversations sert deja pour Alliance/
        // Server/Leaders/Group (provider.CreateConversationAsync) - aucun nouvel endpoint, aucune
        // nouvelle regle serveur. Cote serveur (ChatService.CreateConversation), la cle
        // d'audience "Private" est deja purement derivee des participants tries - c'est un
        // "creer OU retrouver" idempotent par construction : rappeler cette methode pour la MEME
        // paire de joueurs retombe toujours sur la MEME conversation, jamais un doublon.
        public async Task<string> CreatePrivateConversationAsync(string participantPlayerId, CancellationToken ct)
        {
            string trimmed = participantPlayerId?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) throw new ArgumentException("A participant player id is required.", nameof(participantPlayerId));

            // M065-CL - preuve runtime (session CEO du 2026-09-07, apres nettoyage complet de la
            // base de production) : cette methode n'a JAMAIS rempli GameServerId/WorldId sur la
            // requete de creation - le serveur exige ces deux champs (Guid non-nullable), donc
            // chaque VRAIE creation echouait avec un 400 muet avant meme d'atteindre le code de
            // l'endpoint (echec de liaison du modele ASP.NET, invisible cote client : aucune
            // exception applicative, juste un corps de reponse vide). Masque tout ce temps parce que
            // le tiroir de chat Alliance (seule preuve fonctionnelle anterieure) n'a jamais cree de
            // conversation depuis zero - il ouvre un id deja connu - et parce que les conversations
            // privees de test existaient deja en base (creation "reussie" = simple retrouvaille
            // idempotente par cle d'audience, jamais une vraie creation). Meme scope, meme garde-fou
            // que CreateGroupAsync juste au-dessus (LivingHiveChatController.Groups.cs) : jamais
            // inventer un GUID, refuser proprement si le scope n'est pas encore connu.
            string gameServerId;
            string worldId;
            lock (gate) { gameServerId = groupGameServerId; worldId = groupWorldId; }
            if (string.IsNullOrWhiteSpace(gameServerId) || string.IsNullOrWhiteSpace(worldId))
            {
                SetStatus(LivingHiveChatStatus.Error, "chat_private_scope_not_configured");
                return null;
            }

            try
            {
                RemoteCreateConversationResult result = await provider.CreateConversationAsync(new RemoteCreateConversationRequest
                {
                    ChannelType = "Private",
                    GameServerId = gameServerId,
                    WorldId = worldId,
                    ParticipantIds = new List<string> { trimmed },
                    ClientRequestId = Guid.NewGuid().ToString("N")
                }, ct);
                if (result?.Conversation == null || string.IsNullOrWhiteSpace(result.Conversation.ConversationId)) return null;

                lock (gate)
                {
                    conversations.RemoveAll(value => string.Equals(value.ConversationId, result.Conversation.ConversationId, StringComparison.Ordinal));
                    conversations.Add(MapConversation(result.Conversation));
                }
                await SelectKnownConversationAsync(result.Conversation.ConversationId, result.Conversation.Title, "Private", ct);
                return result.Conversation.ConversationId;
            }
            catch (RemoteChatTransportException exception)
            {
                SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString());
                return null;
            }
        }

        public async Task RefreshSelectedAsync(CancellationToken ct)
        {
            string conversationId;
            lock (gate) conversationId = selectedConversationId;
            if (string.IsNullOrWhiteSpace(conversationId)) { lock (gate) messages.Clear(); return; }
            try
            {
                long afterSequence = provider.GetConfirmedSequence(conversationId);
                RemoteReconciliationResult result = await provider.ReconcileFullyAsync(conversationId, afterSequence, new ChatPaginationPolicy(100, 20), ct);
                IReadOnlyList<RemoteChatMessage> source = result?.Items ?? Array.Empty<RemoteChatMessage>();
                long latest = source.Count == 0 ? 0 : source.Max(value => value.Sequence);
                lock (gate)
                {
                    messages.Clear();
                    messages.AddRange(source.OrderBy(value => value.Sequence).TakeLast(recentMessageLimit).Select(MapMessage));
                }
                if (latest > 0)
                {
                    await provider.MarkReadAsync(conversationId, latest, ct);
                    lock (gate)
                    {
                        LivingHiveChatConversation selected = conversations.FirstOrDefault(value => string.Equals(value.ConversationId, conversationId, StringComparison.Ordinal));
                        if (selected != null) { selected.UnreadCount = 0; selected.MentionCount = 0; selected.LastSequence = Math.Max(selected.LastSequence, latest); }
                    }
                }
                PersistRecentCache();
            }
            catch (RemoteChatTransportException exception)
            {
                SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString());
            }
        }

        public async Task SendAsync(string body, CancellationToken ct)
        {
            string normalized = body?.Trim();
            if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("Message body is required.", nameof(body));
            string conversationId;
            lock (gate) conversationId = selectedConversationId;
            if (string.IsNullOrWhiteSpace(conversationId)) throw new InvalidOperationException("No accessible conversation is selected.");
            string requestId = Guid.NewGuid().ToString("N");
            var optimistic = new LivingHiveChatMessage { ConversationId = conversationId, ClientRequestId = requestId, OriginalBody = normalized, VisibleBody = normalized, CreatedAt = DateTimeOffset.UtcNow, Delivery = LivingHiveChatDelivery.Queued };
            lock (gate) { messages.Add(optimistic); TrimMessages(); pendingCount++; }
            try
            {
                RemoteSendResult result = await provider.SendAsync(conversationId, normalized, requestId, ct);
                lock (gate)
                {
                    int index = messages.FindIndex(value => string.Equals(value.ClientRequestId, requestId, StringComparison.Ordinal));
                    if (index >= 0) messages[index] = MapMessage(result.Message);
                    pendingCount = Math.Max(0, pendingCount - 1);
                    TrimMessages();
                }
                PersistRecentCache();
            }
            catch (RemoteChatTransportException exception)
            {
                lock (gate)
                {
                    LivingHiveChatMessage pending = messages.FirstOrDefault(value => string.Equals(value.ClientRequestId, requestId, StringComparison.Ordinal));
                    if (pending != null) pending.Delivery = IsRetryable(exception.Error) ? LivingHiveChatDelivery.Queued : LivingHiveChatDelivery.Failed;
                }
                SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString());
            }
        }

        public async Task ResumeAsync(CancellationToken ct)
        {
            SetStatus(LivingHiveChatStatus.Connecting, null);
            try
            {
                ChatPendingDrainResult drained = await provider.DrainPendingAsync(ct);
                lock (gate) pendingCount = drained?.Remaining?.Total ?? 0;
                await OpenAsync(ct);
            }
            catch (ChatPendingDrainException exception)
            {
                lock (gate) pendingCount = exception.Result?.Remaining?.Total ?? pendingCount;
                SetStatus(LivingHiveChatStatus.Offline, "pending_drain_incomplete");
            }
            catch (RemoteChatTransportException exception) { SetStatus(MapStatus(exception.Error), exception.ServerCode ?? exception.Error.ToString()); }
        }

        public async Task TranslateAsync(string messageId, string targetLocale, string modelVersion, CancellationToken ct)
        {
            LivingHiveChatMessage message;
            lock (gate) message = messages.FirstOrDefault(value => string.Equals(value.MessageId, messageId, StringComparison.Ordinal));
            if (message == null || string.IsNullOrWhiteSpace(message.MessageId)) throw new ArgumentException("A confirmed visible message is required.", nameof(messageId));
            TranslationDisplayState state = await translations.TranslateAsync(new RemoteChatMessage { MessageId = message.MessageId, ConversationId = message.ConversationId, OriginalBody = message.OriginalBody }, targetLocale, modelVersion, ct);
            lock (gate)
            {
                LivingHiveChatMessage current = messages.FirstOrDefault(value => string.Equals(value.MessageId, messageId, StringComparison.Ordinal));
                if (current != null && state.Mode == TranslationDisplayMode.Translated) { current.VisibleBody = state.VisibleText; current.SourceLocale = state.SourceLocale; current.TargetLocale = state.TargetLocale; current.IsTranslated = true; }
            }
        }

        public void ShowOriginal(string messageId)
        {
            lock (gate)
            {
                LivingHiveChatMessage current = messages.FirstOrDefault(value => string.Equals(value.MessageId, messageId, StringComparison.Ordinal));
                if (current != null) { current.VisibleBody = current.OriginalBody; current.IsTranslated = false; }
            }
        }

        public async Task CloseAsync(CancellationToken ct)
        {
            liveUpdates?.Cancel();
            try { await Task.WhenAll(pollingTask, RealtimeReceiptTask()); } catch (Exception) { }
            liveUpdates?.Dispose();
            liveUpdates = null;
            pollingTask = Task.CompletedTask;
            await provider.DisconnectAsync(ct);
            lock (gate) { messages.Clear(); conversations.Clear(); selectedConversationId = null; pendingCount = 0; status = LivingHiveChatStatus.Offline; errorCode = null; ClearGroupState(); }
        }

        private void SetStatus(LivingHiveChatStatus value, string code) { lock (gate) { status = value; errorCode = code; } }
        public Task AwaitRealtimeReceiptsAsync() => RealtimeReceiptTask();
        private Task RealtimeReceiptTask() { lock (gate) return realtimeReceiptTask; }
        private void EnsureLiveUpdates(CancellationToken lifetime, bool usePolling)
        {
            if (liveUpdates != null && !liveUpdates.IsCancellationRequested) return;
            liveUpdates?.Cancel();
            liveUpdates?.Dispose();
            liveUpdates = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            pollingTask = usePolling ? PollLoopAsync(liveUpdates.Token) : Task.CompletedTask;
        }
        private async Task PollLoopAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    await delay.WaitAsync(pollInterval, ct);
                    await RefreshConversationListAsync(ct);
                    await RefreshSelectedAsync(ct);
                    // RAP-OPTIONNEL-COMMUNICATIONS_01: invitations ride the EXISTING poll tick on
                    // purpose - a second timer would double the request rate against a chat backend
                    // that is rate limited per player.
                    await RefreshInvitationsAsync(ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { SetStatus(LivingHiveChatStatus.Offline, "polling_interrupted"); }
        }
        private async Task RefreshConversationListAsync(CancellationToken ct)
        {
            RemoteConversationLoadResult loaded = await provider.LoadAllConversationsAsync(new ChatPaginationPolicy(50, 10), ct);
            IReadOnlyList<RemoteConversation> accessible = loaded?.Items ?? Array.Empty<RemoteConversation>();
            lock (gate)
            {
                conversations.Clear();
                conversations.AddRange(accessible.Where(value => value != null && !string.IsNullOrWhiteSpace(value.ConversationId)).Select(MapConversation));
            }
            await provider.EnsureRealtimeSubscriptionsAsync(accessible.Select(value => value.ConversationId), ct);
            PersistRecentCache();
        }
        private void OnRealtimeEventApplied(RemoteChatEvent evt)
        {
            if (evt?.Message == null) return;
            bool changed = false;
            lock (gate)
            {
                LivingHiveChatConversation conversation = conversations.FirstOrDefault(value => string.Equals(value.ConversationId, evt.ConversationId, StringComparison.Ordinal));
                if (conversation != null) conversation.LastSequence = Math.Max(conversation.LastSequence, evt.Message.Sequence);
                if (!string.Equals(selectedConversationId, evt.ConversationId, StringComparison.Ordinal))
                {
                    if (conversation != null) conversation.UnreadCount++;
                    changed = conversation != null;
                }
                else
                {
                    int index = messages.FindIndex(value => value.Sequence == evt.Message.Sequence || !string.IsNullOrEmpty(evt.Message.ClientRequestId) && string.Equals(value.ClientRequestId, evt.Message.ClientRequestId, StringComparison.Ordinal));
                    LivingHiveChatMessage mapped = MapMessage(evt.Message);
                    if (index >= 0) messages[index] = mapped; else messages.Add(mapped);
                    messages.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
                    TrimMessages();
                    changed = true;
                    CancellationToken token = liveUpdates?.Token ?? CancellationToken.None;
                    Task previous = realtimeReceiptTask;
                    realtimeReceiptTask = AcknowledgeRealtimeReadAsync(previous, evt.ConversationId, evt.Message.Sequence, token);
                }
            }
            if (changed) PersistRecentCache();
        }
        private async Task AcknowledgeRealtimeReadAsync(Task previous, string conversationId, long sequence, CancellationToken ct)
        {
            await Task.Yield();
            try { await previous; } catch (OperationCanceledException) { }
            ct.ThrowIfCancellationRequested();
            await provider.MarkReadAsync(conversationId, sequence, ct);
            lock (gate)
            {
                LivingHiveChatConversation conversation = conversations.FirstOrDefault(value => string.Equals(value.ConversationId, conversationId, StringComparison.Ordinal));
                if (conversation != null) { conversation.UnreadCount = 0; conversation.MentionCount = 0; }
            }
            PersistRecentCache();
        }
        private void TrimMessages() { if (messages.Count > recentMessageLimit) messages.RemoveRange(0, messages.Count - recentMessageLimit); }
        private void RestoreRecentCache()
        {
            if (recentCache == null) return;
            lock (gate) if (conversations.Count > 0 || messages.Count > 0) return;
            try
            {
                ChatRecentCacheSnapshot cached = recentCache.Load();
                lock (gate)
                {
                    conversations.Clear();
                    conversations.AddRange((cached.Conversations ?? Array.Empty<LivingHiveChatConversation>()).Select(Clone));
                    selectedConversationId = cached.SelectedConversationId;
                    messages.Clear();
                    messages.AddRange((cached.Messages ?? Array.Empty<LivingHiveChatMessage>()).Where(item => item != null && item.Delivery == LivingHiveChatDelivery.Confirmed).TakeLast(recentMessageLimit).Select(Clone));
                }
            }
            catch (ChatRecentCacheException) { SetStatus(LivingHiveChatStatus.Offline, "local_recent_cache_quarantined"); }
            catch (Exception) { SetStatus(LivingHiveChatStatus.Offline, "local_recent_cache_unavailable"); }
        }

        private void PersistRecentCache()
        {
            if (recentCache == null) return;
            try
            {
                ChatRecentCacheSnapshot snapshot;
                lock (gate) snapshot = new ChatRecentCacheSnapshot { SelectedConversationId = selectedConversationId, Conversations = conversations.Select(Clone).ToArray(), Messages = messages.Where(item => item.Delivery == LivingHiveChatDelivery.Confirmed).Select(Clone).ToArray() };
                recentCache.Save(snapshot);
            }
            // M059D-CL - preuve runtime (session CEO du 2026-09-07) : ce cache est un
            // best-effort purement LOCAL (relecture au demarrage pour un affichage instantane hors
            // ligne) - il n'a aucun rapport avec la connexion serveur elle-meme. Avant ce correctif,
            // un echec ici (confirme en runtime : ChatProtectedStoreException lors du chiffrement
            // logiciel de secours hors Android) faisait pourtant basculer TOUT le statut de connexion
            // a Offline juste apres un envoi reussi (SendAsync ne voyait aucune erreur, cette methode
            // avalait l'exception elle-meme) - un chat parfaitement en ligne se faisait donc marquer
            // "indisponible" par un probleme d'ecriture disque local sans aucun rapport. Ne plus jamais
            // degrader l'etat de connexion pour ca ; seulement journaliser, le prochain appel reessaiera
            // de lui-meme.
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning("[ChatRoyal] Recent-cache save failed (local-only, connection unaffected): " + exception.GetType().Name);
            }
        }
        private static bool IsRetryable(RemoteChatError error) => error == RemoteChatError.Transport || error == RemoteChatError.Offline || error == RemoteChatError.RateLimited || error == RemoteChatError.Cancelled;
        private static LivingHiveChatStatus MapStatus(RemoteChatError error) => error == RemoteChatError.Unauthorized || error == RemoteChatError.LocalAccountMismatch ? LivingHiveChatStatus.AuthenticationRequired : error == RemoteChatError.Disabled || error == RemoteChatError.Incompatible ? LivingHiveChatStatus.Unavailable : error == RemoteChatError.Transport || error == RemoteChatError.Offline || error == RemoteChatError.RateLimited ? LivingHiveChatStatus.Offline : LivingHiveChatStatus.Error;
        private static LivingHiveChatConversation MapConversation(RemoteConversation value) => new LivingHiveChatConversation { ConversationId = value.ConversationId, Title = value.Title, ChannelType = value.ChannelType, LastSequence = value.LastSequence, UnreadCount = Math.Max(0, value.UnreadCount), MentionCount = Math.Max(0, value.MentionCount) };
        private static LivingHiveChatMessage MapMessage(RemoteChatMessage value) => new LivingHiveChatMessage { MessageId = value.MessageId, ConversationId = value.ConversationId, ClientRequestId = value.ClientRequestId, SenderPlayerId = value.SenderId, SenderDisplayName = value.SenderDisplayName, OriginalBody = value.OriginalBody, VisibleBody = value.OriginalBody, Sequence = value.Sequence, CreatedAt = value.CreatedAt, Delivery = LivingHiveChatDelivery.Confirmed };
        private static LivingHiveChatConversation Clone(LivingHiveChatConversation value) => new LivingHiveChatConversation { ConversationId = value.ConversationId, Title = value.Title, ChannelType = value.ChannelType, LastSequence = value.LastSequence, UnreadCount = value.UnreadCount, MentionCount = value.MentionCount };
        private static LivingHiveChatMessage Clone(LivingHiveChatMessage value) => new LivingHiveChatMessage { MessageId = value.MessageId, ConversationId = value.ConversationId, ClientRequestId = value.ClientRequestId, SenderPlayerId = value.SenderPlayerId, SenderDisplayName = value.SenderDisplayName, OriginalBody = value.OriginalBody, VisibleBody = value.VisibleBody, SourceLocale = value.SourceLocale, TargetLocale = value.TargetLocale, Sequence = value.Sequence, CreatedAt = value.CreatedAt, Delivery = value.Delivery, IsTranslated = value.IsTranslated };
    }

    public static class LivingHiveChatRuntime
    {
        private static readonly object Gate = new object();
        private static LivingHiveChatController controller;
        private static CancellationTokenSource lifetime;

        public static bool IsConfigured { get { lock (Gate) return controller != null; } }
        public static bool OpenInProgressForDiagnostics { get { lock (Gate) return controller != null && controller.OpenInProgressForDiagnostics; } }
        public static LivingHiveChatSnapshot Snapshot { get { lock (Gate) return controller?.Snapshot() ?? new LivingHiveChatSnapshot { Status = LivingHiveChatStatus.NotConfigured }; } }
        public static async Task ReconfigureAsync(LivingHiveChatController value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            await ResetAsync();
            lock (Gate) { controller = value; lifetime = new CancellationTokenSource(); }
        }
        public static Task OpenAsync() { lock (Gate) return controller == null ? Task.CompletedTask : controller.OpenAsync(lifetime.Token); }
        public static Task SelectAsync(string id) { lock (Gate) return controller == null ? Task.CompletedTask : controller.SelectConversationAsync(id, lifetime.Token); }
        public static Task SelectKnownAsync(string id, string title, string channelType) { lock (Gate) return controller == null ? Task.CompletedTask : controller.SelectKnownConversationAsync(id, title, channelType, lifetime.Token); }
        // M059C-CL - meme facade "jamais d'exception au site d'appel IMGUI" que les autres
        // methodes ci-dessous : un controller absent repond simplement null plutot que de faire
        // planter l'ecran Chat Royal, qui se dessine a chaque frame.
        public static Task<string> CreatePrivateConversationAsync(string participantPlayerId) { lock (Gate) return controller == null ? Task.FromResult<string>(null) : controller.CreatePrivateConversationAsync(participantPlayerId, lifetime.Token); }
        public static Task SendAsync(string body) { lock (Gate) return controller == null ? Task.CompletedTask : controller.SendAsync(body, lifetime.Token); }
        public static Task ResumeAsync() { lock (Gate) return controller == null ? Task.CompletedTask : controller.ResumeAsync(lifetime.Token); }
        public static Task TranslateAsync(string messageId, string locale, string modelVersion) { lock (Gate) return controller == null ? Task.CompletedTask : controller.TranslateAsync(messageId, locale, modelVersion, lifetime.Token); }
        public static void ShowOriginal(string messageId) { lock (Gate) controller?.ShowOriginal(messageId); }

        // RAP-OPTIONNEL-COMMUNICATIONS_01 - group facade. Same shape as the calls above: never throws
        // at the IMGUI call site (a null controller answers a completed task), because the Chat Royal
        // screen is drawn every frame and must not depend on the chat being configured to render.
        public static void ConfigureGroupScope(string gameServerId, string worldId) { lock (Gate) controller?.ConfigureGroupScope(gameServerId, worldId); }
        public static Task<string> CreateGroupAsync(string title, IReadOnlyList<string> inviteePlayerIds) { lock (Gate) return controller == null ? Task.FromResult<string>(null) : controller.CreateGroupAsync(title, inviteePlayerIds, lifetime.Token); }
        public static Task InviteToGroupAsync(string conversationId, string inviteePlayerId) { lock (Gate) return controller == null ? Task.CompletedTask : controller.InviteToGroupAsync(conversationId, inviteePlayerId, lifetime.Token); }
        public static Task<string> RespondToInvitationAsync(string inviteId, bool accept) { lock (Gate) return controller == null ? Task.FromResult<string>(null) : controller.RespondToInvitationAsync(inviteId, accept, lifetime.Token); }
        public static Task RemoveGroupMemberAsync(string conversationId, string playerId) { lock (Gate) return controller == null ? Task.CompletedTask : controller.RemoveGroupMemberAsync(conversationId, playerId, lifetime.Token); }
        public static Task TransferLeadershipAsync(string conversationId, string newLeaderPlayerId) { lock (Gate) return controller == null ? Task.CompletedTask : controller.TransferLeadershipAsync(conversationId, newLeaderPlayerId, lifetime.Token); }
        public static Task LeaveGroupAsync(string conversationId) { lock (Gate) return controller == null ? Task.CompletedTask : controller.LeaveGroupAsync(conversationId, lifetime.Token); }
        public static Task RefreshInvitationsAsync() { lock (Gate) return controller == null ? Task.CompletedTask : controller.RefreshInvitationsAsync(lifetime.Token); }
        public static Task RefreshGroupDetailAsync(string conversationId) { lock (Gate) return controller == null ? Task.CompletedTask : controller.RefreshGroupDetailAsync(conversationId, lifetime.Token); }
        public static Task AcknowledgeSentInvitationResponsesAsync(IReadOnlyList<string> inviteIds) { lock (Gate) return controller == null ? Task.CompletedTask : controller.AcknowledgeSentInvitationResponsesAsync(inviteIds, lifetime.Token); }
        public static Task RefreshPreferencesAsync() { lock (Gate) return controller == null ? Task.CompletedTask : controller.RefreshPreferencesAsync(lifetime.Token); }
        public static Task UpdateAutoInviteResponseAsync(string rule) { lock (Gate) return controller == null ? Task.CompletedTask : controller.UpdateAutoInviteResponseAsync(rule, lifetime.Token); }
        public static async Task CloseAsync() { LivingHiveChatController value; CancellationToken token; lock (Gate) { value = controller; token = lifetime?.Token ?? CancellationToken.None; } if (value != null) await value.CloseAsync(token); }
        public static async Task ResetAsync()
        {
            LivingHiveChatController value;
            lock (Gate) { lifetime?.Cancel(); value = controller; }
            if (value != null) try { await value.CloseAsync(CancellationToken.None); } catch { }
            lock (Gate) { lifetime?.Dispose(); lifetime = null; controller = null; }
        }
    }
}
