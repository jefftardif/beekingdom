using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using BeeKingdom.Audio;
using BeeKingdom.Gameplay.Communication;

namespace BeeKingdom.Playground
{
    // RAP-OPTIONNEL-COMMUNICATIONS_01 - CHAT ROYAL branche sur le vrai backend.
    //
    // L'ecran restait une maquette 100 % locale (BuildChatConversations/BuildChatMessages + un
    // simulateur de faux messages). Il lit desormais LivingHiveChatRuntime.Snapshot des que le chat
    // serveur est configure et connecte.
    //
    // Choix explicite : les donnees locales ne sont PAS supprimees, elles deviennent un mode demo.
    // Le chat est desactive en PRODUCTION (Chat__Enabled=false) ; supprimer la maquette viderait donc
    // l'ecran du CEO sans rien livrer en echange. Le mode reel prend le dessus des qu'il y a une
    // vraie connexion, et un bandeau dit lequel des deux est a l'ecran - on ne fait jamais passer des
    // donnees de demo pour des donnees serveur.
    public static partial class HiveViewProductUiPresenter
    {
        private const string ChatGroupsChannelId = "groups";
        private const string ChatAccentPreferenceKey = "beekingdom.chat.accent";

        private static bool chatUsingServerData;
        private static string chatServerStatusLabel = string.Empty;
        private static float chatServerSyncedAt = -10f;

        private static bool chatNewDiscussionOpen;
        private static bool chatNewGroupOpen;
        private static bool chatSettingsOpen;
        private static bool chatGroupMembersOpen;

        private static string chatPickerQuery = string.Empty;
        private static float chatPickerQueryChangedAt = -10f;
        private static string chatPickerLastSentQuery = string.Empty;
        private static readonly List<ChatPlayerPickerEntry> chatGroupSelection = new List<ChatPlayerPickerEntry>();
        private static string chatNewGroupTitle = string.Empty;
        private static Vector2 chatPickerScroll;
        private static Vector2 chatGroupMembersScroll;

        private static string chatTransferTargetPlayerId = string.Empty;
        private static readonly HashSet<string> chatAnnouncedInviteResponses = new HashSet<string>(StringComparer.Ordinal);

        // M067-CL : confirmation simple avant d'exclure un membre, meme forme (arme/confirme sous
        // 5s) que DrawAllianceMemberAdminActionButton - pas de nouveau systeme, juste le meme motif.
        private static string chatGroupKickConfirmArmedId = string.Empty;
        private static float chatGroupKickConfirmArmedAt = -999f;

        private static int chatAccentIndex = -1;

        private static IChatPlayerPickerController chatPlayerPicker = new UnavailableChatPlayerPickerController();

        // M065-CL : chatPlayerPicker (et son propre cache de noms) est recree a zero chaque fois
        // que MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession() tourne -
        // et plusieurs bootstraps HiveMap sans rapport avec le chat l'appellent chacun,
        // potentiellement plusieurs fois par session, effacant a chaque fois le nom qu'on venait
        // d'apprendre via "Nouvelle discussion". Ce dictionnaire vit au niveau de l'ecran, jamais
        // touche par cette recreation, donc un nom appris une fois reste connu pour le reste de la
        // session - meme source de donnees (entry.DisplayName), juste un endroit stable.
        private static readonly Dictionary<Guid, string> chatKnownDisplayNames = new Dictionary<Guid, string>();

        private static readonly string[] ChatAccentNames = { "Ambre royal", "Miel clair", "Azur", "Emeraude", "Pourpre" };

        private static readonly Color[] ChatAccentColors =
        {
            new Color(1f, 0.68f, 0.16f, 1f),
            new Color(1f, 0.86f, 0.42f, 1f),
            new Color(0.42f, 0.72f, 1f, 1f),
            new Color(0.42f, 0.88f, 0.58f, 1f),
            new Color(0.78f, 0.56f, 1f, 1f)
        };

        public static void ConfigureChatPlayerPickerForRuntime(IChatPlayerPickerController controller)
        {
            chatPlayerPicker = controller ?? new UnavailableChatPlayerPickerController();
        }

        public static void ResetChatPlayerPickerForRuntime()
        {
            chatPlayerPicker = new UnavailableChatPlayerPickerController();
        }

        // ==================== accent (preference locale) ====================
        // Purement cosmetique et par appareil : PlayerPrefs, pas le serveur. La regle
        // d'auto-reponse aux invitations est l'inverse (serveur), parce qu'elle doit s'appliquer
        // meme joueur hors ligne.
        private static int ChatAccentIndex()
        {
            if (chatAccentIndex < 0)
                chatAccentIndex = Mathf.Clamp(PlayerPrefs.GetInt(ChatAccentPreferenceKey, 0), 0, ChatAccentColors.Length - 1);
            return chatAccentIndex;
        }

        private static Color ChatAccentColor() => ChatAccentColors[ChatAccentIndex()];

        private static void SetChatAccentIndex(int index)
        {
            chatAccentIndex = Mathf.Clamp(index, 0, ChatAccentColors.Length - 1);
            PlayerPrefs.SetInt(ChatAccentPreferenceKey, chatAccentIndex);
            PlayerPrefs.Save();
        }

        // ==================== synchronisation serveur ====================

        // M068-CL : sur desktop (non compact), le panneau Membres devient un panneau ancre a droite
        // de la conversation (voir DrawChatGroupMembersPanel / ChatGroupMembersPanelVisible) au lieu
        // d'un modal plein ecran - il coexiste avec le reste de l'ecran et ne doit donc plus le
        // masquer. Sur mobile (compact), l'espace manque pour un 4e panneau : chatGroupMembersOpen
        // continue d'ouvrir le modal plein ecran existant, qui doit donc toujours gater le dessous.
        private static bool ChatRoyalOwnOverlayOpen(bool compact) =>
            chatNewDiscussionOpen || chatNewGroupOpen || chatSettingsOpen || (compact && chatGroupMembersOpen) || ChatHasPendingInvitation();

        private static LivingHiveChatSnapshot ChatServerSnapshot()
        {
            return LivingHiveChatRuntime.IsConfigured ? LivingHiveChatRuntime.Snapshot : null;
        }

        private static bool ChatServerConnected()
        {
            LivingHiveChatSnapshot snapshot = ChatServerSnapshot();
            return snapshot != null
                && (snapshot.Status == LivingHiveChatStatus.Online || snapshot.Status == LivingHiveChatStatus.Polling);
        }

        private static string ChatChannelIdFor(string channelType)
        {
            if (string.Equals(channelType, "Group", StringComparison.OrdinalIgnoreCase)) return ChatGroupsChannelId;
            if (string.Equals(channelType, "Private", StringComparison.OrdinalIgnoreCase)) return "private";
            if (string.Equals(channelType, "Server", StringComparison.OrdinalIgnoreCase)) return "world";
            return "alliance";
        }

        // Recopie le snapshot serveur dans les structures que tout l'ecran consomme deja
        // (chatConversations / chatMessagesByConversation). Aucun pane de dessin n'est reecrit :
        // seule la SOURCE des donnees change, le rendu reste identique.
        private static void ChatRoyalSyncFromServer()
        {
            EnsureChatData();
            LivingHiveChatSnapshot snapshot = ChatServerSnapshot();
            if (snapshot == null)
            {
                chatUsingServerData = false;
                chatServerStatusLabel = "Mode demo local (chat serveur non configure)";
                return;
            }

            chatServerStatusLabel = ChatStatusLabel(snapshot);
            if (!ChatServerConnected())
            {
                chatUsingServerData = false;
                return;
            }

            bool firstSwitch = !chatUsingServerData;
            chatUsingServerData = true;
            chatServerSyncedAt = NowForUi();

            EnsureChatGroupsChannel();

            // Les canaux "system"/"events" n'ont pas d'equivalent serveur : ils restent locaux et
            // en lecture seule, tout le reste vient du serveur.
            ChatConversationData[] previousConversations = chatConversations.ToArray();
            chatConversations.RemoveAll(item => !string.Equals(item.Channel, "system", StringComparison.Ordinal)
                && !string.Equals(item.Channel, "events", StringComparison.Ordinal));

            foreach (LivingHiveChatConversation conversation in snapshot.Conversations)
            {
                if (conversation == null || string.IsNullOrWhiteSpace(conversation.ConversationId)) continue;
                string channelId = ChatChannelIdFor(conversation.ChannelType);
                string peer = previousConversations.FirstOrDefault(item => item.Id == conversation.ConversationId)?.Peer;
                if (channelId == "private")
                {
                    LivingHiveChatMessage received = snapshot.Messages.FirstOrDefault(message => message != null
                        && message.ConversationId == conversation.ConversationId
                        && Guid.TryParse(message.SenderPlayerId, out Guid senderId)
                        && senderId != MobileAccountSessionRuntimeBootstrap.GameplayPlayerId);
                    if (received != null)
                        peer = ChatPrivateDisplayName(received.SenderPlayerId,
                            !string.IsNullOrWhiteSpace(peer) && !ChatTryPlayerId(peer, out _) ? peer : received.SenderDisplayName);
                }
                if (channelId == "private") peer = ChatPrivateDisplayName(conversation.Title, peer);
                chatConversations.Add(new ChatConversationData
                {
                    Id = conversation.ConversationId,
                    Channel = channelId,
                    Title = channelId == "private" ? peer
                        : !string.IsNullOrWhiteSpace(conversation.Title) ? conversation.Title : "Discussion",
                    Peer = peer,
                    Icon = string.Equals(channelId, ChatGroupsChannelId, StringComparison.Ordinal) ? "members"
                        : string.Equals(channelId, "private", StringComparison.Ordinal) ? "bee"
                        : string.Equals(channelId, "world", StringComparison.Ordinal) ? "world" : "alliance",
                    Presence = "online",
                    Unread = Math.Max(0, conversation.UnreadCount)
                });
            }

            ChatRoyalSyncMessages(snapshot);

            LivingHiveChatConversation selected = snapshot.Conversations.FirstOrDefault(item =>
                string.Equals(item.ConversationId, snapshot.SelectedConversationId, StringComparison.Ordinal));
            if (selected != null && ChatChannelById(chatSelectedChannel)?.ReadOnly != true)
            {
                chatSelectedConversation = selected.ConversationId;
                chatSelectedChannel = ChatChannelIdFor(selected.ChannelType);
            }
            else if (firstSwitch || !chatConversations.Any(item => string.Equals(item.Id, chatSelectedConversation, StringComparison.Ordinal)))
                ChatSelectChannel(chatSelectedChannel ?? "alliance");
        }

        private static string ChatPrivateDisplayName(string title, string peer)
        {
            bool titleIsId = ChatTryPlayerId(title, out Guid titleId);
            bool peerIsId = ChatTryPlayerId(peer, out Guid peerId);
            string directoryName = titleIsId ? ChatDirectoryDisplayName(titleId) : null;
            if (directoryName == null && peerIsId) directoryName = ChatDirectoryDisplayName(peerId);
            if (!string.IsNullOrWhiteSpace(directoryName)) return directoryName;
            if (!string.IsNullOrWhiteSpace(peer) && !peerIsId && peer != "Discussion") return peer;
            if (!string.IsNullOrWhiteSpace(title) && !titleIsId) return title;
            return !string.IsNullOrWhiteSpace(title) ? title
                : !string.IsNullOrWhiteSpace(peer) ? peer : "Discussion";
        }

        private static bool ChatTryPlayerId(string value, out Guid playerId) =>
            Guid.TryParse(value != null && value.StartsWith("player:", StringComparison.OrdinalIgnoreCase)
                ? value.Substring(7) : value, out playerId);

        private static string ChatDirectoryDisplayName(Guid playerId)
        {
            string name = chatKnownDisplayNames.TryGetValue(playerId, out string known) ? known : null;
            name ??= (chatPlayerPicker as ChatPlayerPickerController)?.ResolveDisplayName(playerId)
                ?? chatPlayerPicker.Results.FirstOrDefault(entry => entry.PlayerId == playerId)?.DisplayName;
            return !string.IsNullOrWhiteSpace(name) && !ChatTryPlayerId(name, out _) ? name : null;
        }

        private static void ChatRoyalSyncMessages(LivingHiveChatSnapshot snapshot)
        {
            string conversationId = snapshot.SelectedConversationId;
            if (string.IsNullOrWhiteSpace(conversationId)) return;

            List<ChatMessageData> mapped = new List<ChatMessageData>();
            int index = 0;
            foreach (LivingHiveChatMessage message in snapshot.Messages)
            {
                if (message == null || !string.Equals(message.ConversationId, conversationId, StringComparison.Ordinal)) continue;
                bool fromSelf = Guid.TryParse(message.SenderPlayerId, out Guid senderId)
                    ? senderId != Guid.Empty && senderId == MobileAccountSessionRuntimeBootstrap.GameplayPlayerId
                    : message.Delivery != LivingHiveChatDelivery.Confirmed;
                mapped.Add(new ChatMessageData
                {
                    Id = string.IsNullOrWhiteSpace(message.MessageId) ? "pending-" + index : message.MessageId,
                    ConversationId = conversationId,
                    FromSelf = fromSelf,
                    Author = fromSelf ? ChatSelfName()
                        : ChatConversationById(conversationId)?.Channel == "private"
                            ? ChatPrivateDisplayName(message.SenderPlayerId,
                                ChatDirectoryDisplayName(senderId) ?? ChatConversationById(conversationId)?.Peer ?? message.SenderDisplayName)
                            : message.SenderDisplayName,
                    Presence = "online",
                    AuthorSeed = Math.Abs((message.SenderDisplayName ?? string.Empty).GetHashCode()) % 5,
                    Text = message.VisibleBody ?? message.OriginalBody ?? string.Empty,
                    TimeLabel = message.CreatedAt == default(DateTimeOffset)
                        ? string.Empty
                        : message.CreatedAt.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture),
                    State = message.Delivery == LivingHiveChatDelivery.Confirmed ? 2
                        : message.Delivery == LivingHiveChatDelivery.Queued ? 0 : 3
                });
                index++;
            }
            chatMessagesByConversation[conversationId] = mapped;
        }

        private static async void ChatSelectRuntimeConversation(string conversationId)
        {
            if (!chatUsingServerData || string.IsNullOrWhiteSpace(conversationId)
                || ChatConversationById(conversationId)?.ReadOnly != false
                || string.Equals(ChatServerSnapshot()?.SelectedConversationId, conversationId, StringComparison.Ordinal)) return;
            try { await LivingHiveChatRuntime.SelectAsync(conversationId); }
            catch (Exception exception) { Debug.LogWarning("[ChatRoyal] Selection failed: " + exception.GetType().Name); }
        }

        private static void EnsureChatGroupsChannel()
        {
            if (chatChannels.Any(item => string.Equals(item.Id, ChatGroupsChannelId, StringComparison.Ordinal))) return;
            // Insere avant "system" pour garder les canaux ecrivables groupes ensemble.
            int insertAt = chatChannels.FindIndex(item => string.Equals(item.Id, "system", StringComparison.Ordinal));
            ChatChannelData groups = new ChatChannelData { Id = ChatGroupsChannelId, Name = "Groupes", Icon = "members", ReadOnly = false };
            if (insertAt < 0) chatChannels.Add(groups); else chatChannels.Insert(insertAt, groups);
        }

        private static string ChatStatusLabel(LivingHiveChatSnapshot snapshot)
        {
            switch (snapshot.Status)
            {
                case LivingHiveChatStatus.Online: return "En ligne (temps reel)";
                case LivingHiveChatStatus.Polling: return "En ligne";
                case LivingHiveChatStatus.Connecting: return "Connexion...";
                case LivingHiveChatStatus.AuthenticationRequired: return "Connexion au compte requise";
                case LivingHiveChatStatus.Unavailable: return "Chat serveur desactive";
                case LivingHiveChatStatus.NotConfigured: return "Mode demo local";
                case LivingHiveChatStatus.Offline: return "Hors ligne";
                default: return "Erreur : " + (snapshot.ErrorCode ?? "inconnue");
            }
        }

        // ==================== invitations ====================

        private static LivingHiveChatInvitation ChatFirstPendingInvitation()
        {
            LivingHiveChatSnapshot snapshot = ChatServerSnapshot();
            if (snapshot == null) return null;
            return snapshot.PendingInvitations.FirstOrDefault(item => item != null && !string.IsNullOrWhiteSpace(item.InviteId));
        }

        private static bool ChatHasPendingInvitation() => ChatFirstPendingInvitation() != null;

        // Alerte GLOBALE : dessinee a la racine de la boucle de dessin, donc visible meme quand le
        // joueur n'est pas dans l'ecran Communication (exigence explicite du CEO).
        public static void DrawChatInvitationAlert()
        {
            ChatAnnounceInvitationResponses();

            LivingHiveChatInvitation invitation = ChatFirstPendingInvitation();
            if (invitation == null) return;

            float width = Mathf.Min(420f, Screen.width - 40f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, 64f, width, 158f);
            DrawPremiumPanel(panel, new Color(0.05f, 0.04f, 0.025f, 0.98f), ChatAccentColor());
            GUI.Label(new Rect(panel.x + 16f, panel.y + 12f, panel.width - 32f, 22f), "INVITATION DE GROUPE", new GUIStyle(badgeStyle) { fontSize = 11 });
            GUI.Label(new Rect(panel.x + 16f, panel.y + 40f, panel.width - 32f, 44f),
                (invitation.InviterDisplayName ?? "Un joueur") + " vous invite a rejoindre\n« " + (invitation.GroupTitle ?? "un groupe") + " »",
                new GUIStyle(smallStyle) { fontSize = 12, wordWrap = true });

            float buttonWidth = (panel.width - 44f) * 0.5f;
            Rect accept = new Rect(panel.x + 16f, panel.yMax - 52f, buttonWidth, 36f);
            Rect decline = new Rect(accept.xMax + 12f, accept.y, buttonWidth, 36f);

            DrawPremiumPanel(accept, new Color(0.16f, 0.34f, 0.16f, 0.96f), new Color(0.48f, 0.92f, 0.52f, 0.95f));
            GUI.Label(accept, "Accepter", new GUIStyle(centeredTinyLabelStyle) { fontSize = 11 });
            DrawPremiumPanel(decline, new Color(0.34f, 0.14f, 0.12f, 0.96f), new Color(0.94f, 0.48f, 0.42f, 0.95f));
            GUI.Label(decline, "Refuser", new GUIStyle(centeredTinyLabelStyle) { fontSize = 11 });

            if (GUI.Button(accept, string.Empty, GUIStyle.none))
            {
                AudioManager.Instance?.PlayUIClick();
                ChatAcceptInvitation(invitation);
            }
            else if (GUI.Button(decline, string.Empty, GUIStyle.none))
            {
                AudioManager.Instance?.PlayUIClick();
                LivingHiveChatRuntime.RespondToInvitationAsync(invitation.InviteId, false);
                ShowChatToast("Invitation refusee.");
            }
        }

        private static void ChatAcceptInvitation(LivingHiveChatInvitation invitation)
        {
            string conversationId = invitation.ConversationId;
            LivingHiveChatRuntime.RespondToInvitationAsync(invitation.InviteId, true);
            // Ouvre Communication -> onglet Groupes -> le groupe accepte, comme demande.
            EnsureChatGroupsChannel();
            communicationPanelOpen = true;
            OpenChatScreenTargeted(ChatGroupsChannelId, conversationId, true);
            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                chatSelectedConversation = conversationId;
                LivingHiveChatRuntime.SelectKnownAsync(conversationId, invitation.GroupTitle, "Group");
                LivingHiveChatRuntime.RefreshGroupDetailAsync(conversationId);
            }
            ShowChatToast("Vous avez rejoint « " + (invitation.GroupTitle ?? "le groupe") + " ».");
        }

        // Cote expediteur : le serveur renvoie les reponses non acquittees a chaque sondage. On les
        // annonce une fois puis on les acquitte, sinon le toast reviendrait a chaque tick.
        private static void ChatAnnounceInvitationResponses()
        {
            LivingHiveChatSnapshot snapshot = ChatServerSnapshot();
            if (snapshot == null || snapshot.SentInvitationResponses.Count == 0) return;

            List<string> handled = new List<string>();
            foreach (LivingHiveChatInvitation response in snapshot.SentInvitationResponses)
            {
                if (response == null || string.IsNullOrWhiteSpace(response.InviteId)) continue;
                if (!chatAnnouncedInviteResponses.Add(response.InviteId)) continue;
                bool declined = string.Equals(response.Status, RemoteChatGroupInviteStatus.Declined, StringComparison.Ordinal);
                ShowChatToast((response.InviteeDisplayName ?? "Un joueur")
                    + (declined ? " a refuse de rejoindre " : " a rejoint ")
                    + "« " + (response.GroupTitle ?? "votre groupe") + " ».");
                handled.Add(response.InviteId);
            }
            if (handled.Count > 0) LivingHiveChatRuntime.AcknowledgeSentInvitationResponsesAsync(handled);
        }

        // ==================== points d'entree ====================

        private static void OpenChatNewDiscussion()
        {
            CloseChatRoyalOverlays();
            chatAddMembersConversationId = string.Empty;
            chatNewDiscussionOpen = true;
        }

        private static void OpenChatNewGroup()
        {
            CloseChatRoyalOverlays();
            chatAddMembersConversationId = string.Empty;
            chatNewGroupOpen = true;
        }

        private static void OpenChatSettings()
        {
            CloseChatRoyalOverlays();
            chatSettingsOpen = true;
            LivingHiveChatRuntime.RefreshPreferencesAsync();
        }

        private static void OpenChatGroupMembers()
        {
            CloseChatRoyalOverlays();
            chatGroupMembersOpen = true;
            if (!string.IsNullOrWhiteSpace(chatSelectedConversation))
                LivingHiveChatRuntime.RefreshGroupDetailAsync(chatSelectedConversation);
        }

        // M068-CL : bouton "Membres" de l'en-tete de conversation (DrawChatMessagesPane) - ouvre/
        // ferme le panneau. Sur desktop c'est un panneau ancre a droite (DrawChatGroupMembersPanel),
        // sur mobile le modal plein ecran existant (DrawChatGroupMembersOverlay) - meme etat
        // (chatGroupMembersOpen), meme donnees, seul l'habillage change.
        private static void ChatToggleGroupMembers()
        {
            if (chatGroupMembersOpen) { chatGroupMembersOpen = false; return; }
            OpenChatGroupMembers();
        }

        private static bool ChatGroupMembersPanelVisible() =>
            chatGroupMembersOpen
            && chatUsingServerData
            && string.Equals(chatSelectedChannel, ChatGroupsChannelId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(chatSelectedConversation);

        // QoL (regle CLAUDE.md du 2026-08-04) : un bandeau discret qui dit en permanence si l'ecran
        // affiche des donnees SERVEUR ou la maquette locale, et l'etat de connexion. Sans lui, un
        // testeur ne peut pas distinguer "le chat marche" de "je regarde des donnees inventees" -
        // c'est exactement le piege qui a coute une mission de diagnostic entiere (M056A).
        // Quand un groupe est ouvert, le bandeau devient aussi le bouton "membres du groupe".
        // M068-CL : le bouton "Membres" qui vivait ici (colle au badge de statut serveur, en haut a
        // droite de l'ecran entier) etait mal place et deconnecte visuellement de la conversation
        // qu'il concernait - deplace dans l'en-tete de la conversation elle-meme
        // (DrawChatMessagesPane), comme sur la reference. Ce badge ne fait plus que son travail
        // d'origine : indiquer si l'ecran affiche des donnees serveur ou la maquette locale.
        private static void DrawChatServerSourceBadge(float mainTop, bool compact)
        {
            if (ChatRoyalOwnOverlayOpen(compact)) return;
            float width = compact ? 190f : 260f;
            Rect badge = new Rect(Screen.width - width - 12f, mainTop - 22f, width, 18f);
            if (badge.y < 0f) return;

            Color tint = chatUsingServerData ? new Color(0.42f, 0.88f, 0.58f, 0.95f) : new Color(0.86f, 0.62f, 0.28f, 0.9f);
            GUI.color = new Color(0.02f, 0.018f, 0.012f, 0.86f);
            GUI.DrawTexture(badge, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
            GUI.Label(badge, (chatUsingServerData ? "● SERVEUR · " : "○ DEMO · ") + chatServerStatusLabel,
                new GUIStyle(centeredTinyLabelStyle) { fontSize = 8, normal = { textColor = tint } });
        }

        // ==================== sous-ecrans (modaux) ====================

        // REGLE CLAUDE.md (2026-09-03) : ces modaux se dessinent par-dessus le contenu de l'ecran
        // Communication ; l'appelant DOIT avoir passe son contenu principal dans
        // DrawUnderOwnOverlayGate(ChatRoyalOwnOverlayOpen, ...) avant d'appeler ceci, sinon IMGUI
        // resout les clics du modal sur les controles invisibles du dessous.
        private static void DrawChatRoyalOverlays(bool compact)
        {
            if (chatNewDiscussionOpen) DrawChatPlayerPickerOverlay(compact, groupMode: false);
            else if (chatNewGroupOpen) DrawChatPlayerPickerOverlay(compact, groupMode: true);
            else if (chatSettingsOpen) DrawChatSettingsOverlay(compact);
            // Desktop dessine le panneau Membres ancre a droite depuis DrawChatMainLayout, pas ici.
            else if (chatGroupMembersOpen && compact) DrawChatGroupMembersOverlay(compact);
        }

        private static Rect ChatOverlayRect(bool compact)
        {
            float width = compact ? Screen.width - 24f : Mathf.Min(560f, Screen.width - 80f);
            float height = compact ? Screen.height - 120f : Mathf.Min(520f, Screen.height - 140f);
            return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        }

        private static void DrawChatOverlayChrome(Rect panel, string title, Action onClose)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
            DrawPremiumPanel(panel, new Color(0.035f, 0.028f, 0.02f, 0.99f), ChatAccentColor());
            GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 80f, 24f), title, new GUIStyle(badgeStyle) { fontSize = 12 });

            Rect close = new Rect(panel.xMax - 42f, panel.y + 10f, 28f, 28f);
            DrawPremiumPanel(close, new Color(0.10f, 0.07f, 0.04f, 0.96f), new Color(0.72f, 0.48f, 0.16f, 0.85f));
            GUI.Label(close, "✕", new GUIStyle(centeredTinyLabelStyle) { fontSize = 12 });
            if (GUI.Button(close, string.Empty, GUIStyle.none))
            {
                AudioManager.Instance?.PlayUIClick();
                onClose();
            }
        }

        private static void CloseChatRoyalOverlays()
        {
            chatNewDiscussionOpen = false;
            chatNewGroupOpen = false;
            chatSettingsOpen = false;
            chatGroupMembersOpen = false;
            chatGroupSelection.Clear();
            chatPickerQuery = string.Empty;
            chatNewGroupTitle = string.Empty;
            chatPlayerPicker.Clear();
            // M083-CL : un champ de recherche/texte du sous-modal ferme ici peut garder le focus
            // clavier IMGUI, laissant un curseur "I" orphelin apparaitre ailleurs a l'affichage
            // suivant (meme correctif que CloseChatScreen).
            ReleaseGuiInputCapture();
        }

        // 3.3 / 3.4 : "Nouvelle discussion" (un joueur) et "Nouveau groupe" (plusieurs + titre)
        // partagent le meme selecteur, seule la validation change.
        private static void DrawChatPlayerPickerOverlay(bool compact, bool groupMode)
        {
            Rect panel = ChatOverlayRect(compact);
            DrawChatOverlayChrome(panel, groupMode ? "NOUVEAU GROUPE" : "NOUVELLE DISCUSSION", CloseChatRoyalOverlays);

            float y = panel.y + 46f;
            if (groupMode)
            {
                GUI.Label(new Rect(panel.x + 18f, y, 120f, 20f), "Titre du groupe", new GUIStyle(smallStyle) { fontSize = 10 });
                y += 20f;
                Rect titleRect = new Rect(panel.x + 18f, y, panel.width - 36f, 30f);
                DrawPremiumPanel(titleRect, new Color(0.05f, 0.04f, 0.028f, 0.96f), new Color(0.62f, 0.42f, 0.14f, 0.7f));
                chatNewGroupTitle = GUI.TextField(new Rect(titleRect.x + 8f, titleRect.y + 5f, titleRect.width - 16f, 20f), chatNewGroupTitle ?? string.Empty, 64, new GUIStyle(smallStyle) { fontSize = 12, normal = { textColor = new Color(1f, 0.92f, 0.74f, 1f) } });
                y += 40f;
            }

            GUI.Label(new Rect(panel.x + 18f, y, 200f, 20f), "Rechercher un joueur", new GUIStyle(smallStyle) { fontSize = 10 });
            y += 20f;
            Rect search = new Rect(panel.x + 18f, y, panel.width - 36f, 30f);
            DrawPremiumPanel(search, new Color(0.05f, 0.04f, 0.028f, 0.96f), new Color(0.62f, 0.42f, 0.14f, 0.7f));
            string typed = GUI.TextField(new Rect(search.x + 8f, search.y + 5f, search.width - 16f, 20f), chatPickerQuery ?? string.Empty, 40, new GUIStyle(smallStyle) { fontSize = 12, normal = { textColor = new Color(1f, 0.92f, 0.74f, 1f) } });
            if (!string.Equals(typed, chatPickerQuery, StringComparison.Ordinal))
            {
                chatPickerQuery = typed;
                chatPickerQueryChangedAt = NowForUi();
            }
            // Anti-rebond : on interroge le serveur ~350 ms apres la derniere frappe, pas a chaque
            // caractere (meme regle que la recherche de joueurs de l'Alliance).
            if (chatPickerQueryChangedAt > 0f && NowForUi() - chatPickerQueryChangedAt > 0.35f)
            {
                chatPickerQueryChangedAt = -10f;
                string trimmed = (chatPickerQuery ?? string.Empty).Trim();
                if (trimmed.Length >= 2 && !string.Equals(trimmed, chatPickerLastSentQuery, StringComparison.Ordinal))
                {
                    chatPickerLastSentQuery = trimmed;
                    chatPlayerPicker.Search(trimmed);
                }
            }
            y += 38f;

            float footerHeight = groupMode ? 92f : 52f;
            Rect list = new Rect(panel.x + 18f, y, panel.width - 36f, panel.yMax - y - footerHeight);
            DrawPremiumPanel(list, new Color(0.02f, 0.018f, 0.014f, 0.94f), new Color(0.5f, 0.34f, 0.12f, 0.55f));

            if (!chatPlayerPicker.IsConfigured)
            {
                GUI.Label(list, "Annuaire des joueurs indisponible\n(compte non connecte)", new GUIStyle(centeredTinyLabelStyle) { fontSize = 10, wordWrap = true });
            }
            else
            {
                IReadOnlyList<ChatPlayerPickerEntry> results = chatPlayerPicker.Results;
                if (results.Count == 0)
                {
                    string empty = chatPlayerPicker.Status == ChatPlayerPickerStatus.Searching ? "Recherche..."
                        : chatPlayerPicker.Status == ChatPlayerPickerStatus.Empty ? "Aucun joueur trouve."
                        : chatPlayerPicker.Status == ChatPlayerPickerStatus.Error ? "Recherche indisponible pour le moment."
                        : "Tapez au moins 2 caracteres.";
                    GUI.Label(list, empty, new GUIStyle(centeredTinyLabelStyle) { fontSize = 10 });
                }
                else
                {
                    float rowH = 34f;
                    chatPickerScroll = GUI.BeginScrollView(list, chatPickerScroll, new Rect(0f, 0f, list.width - 18f, results.Count * (rowH + 4f)));
                    for (int i = 0; i < results.Count; i++)
                    {
                        ChatPlayerPickerEntry entry = results[i];
                        Rect row = new Rect(4f, i * (rowH + 4f) + 2f, list.width - 26f, rowH);
                        bool selected = groupMode && chatGroupSelection.Any(item => item.PlayerId == entry.PlayerId);
                        DrawPremiumPanel(row, selected ? new Color(0.26f, 0.18f, 0.05f, 0.96f) : new Color(0.06f, 0.05f, 0.03f, 0.92f), selected ? ChatAccentColor() : new Color(0.52f, 0.36f, 0.12f, 0.6f));
                        GUI.Label(new Rect(row.x + 10f, row.y + 7f, row.width - 90f, 22f), entry.DisplayName, new GUIStyle(smallStyle) { fontSize = 13, alignment = TextAnchor.MiddleLeft });
                        GUI.Label(new Rect(row.xMax - 84f, row.y + 7f, 76f, 22f), groupMode ? (selected ? "Retirer" : "Ajouter") : "Discuter", new GUIStyle(centeredTinyLabelStyle) { fontSize = 11 });
                        if (GUI.Button(row, string.Empty, GUIStyle.none))
                        {
                            AudioManager.Instance?.PlayUIClick();
                            if (groupMode)
                            {
                                if (selected) chatGroupSelection.RemoveAll(item => item.PlayerId == entry.PlayerId);
                                else chatGroupSelection.Add(entry);
                            }
                            else ChatStartPrivateConversation(entry);
                        }
                    }
                    GUI.EndScrollView();
                }
            }

            if (!groupMode)
            {
                GUI.Label(new Rect(panel.x + 18f, panel.yMax - 42f, panel.width - 36f, 30f),
                    "Choisissez un joueur pour ouvrir une discussion privee.",
                    new GUIStyle(smallStyle) { fontSize = 9, wordWrap = true });
                return;
            }

            GUI.Label(new Rect(panel.x + 18f, panel.yMax - 84f, panel.width - 36f, 20f),
                chatGroupSelection.Count == 0 ? "Aucun membre selectionne." : "Membres : " + string.Join(", ", chatGroupSelection.Select(item => item.DisplayName)),
                new GUIStyle(smallStyle) { fontSize = 9 });

            Rect create = new Rect(panel.x + 18f, panel.yMax - 58f, panel.width - 36f, 38f);
            bool ready = !string.IsNullOrWhiteSpace(chatNewGroupTitle) && chatGroupSelection.Count > 0 && ChatServerConnected();
            DrawPremiumPanel(create, ready ? new Color(0.30f, 0.20f, 0.05f, 0.96f) : new Color(0.08f, 0.07f, 0.05f, 0.9f), ready ? ChatAccentColor() : new Color(0.4f, 0.3f, 0.14f, 0.5f));
            GUI.Label(create, ready ? "Creer le groupe" : "Titre et membres requis", new GUIStyle(centeredTinyLabelStyle) { fontSize = 11 });
            if (ready && GUI.Button(create, string.Empty, GUIStyle.none))
            {
                AudioManager.Instance?.PlayUIClick();
                ChatCreateGroupFromSelection();
            }
        }

        private static void ChatStartPrivateConversation(ChatPlayerPickerEntry entry)
        {
            // M065-CL : "Nouvelle discussion" est le seul endroit qui connait deja le nom du joueur
            // tape (entry.DisplayName) - on le fait entrer ici dans le repertoire stable
            // (chatKnownDisplayNames) utilise pour resoudre PlayerId -> DisplayName partout ailleurs
            // (titre, liste, auteurs des messages). chatPlayerPicker lui-meme est recree a zero par
            // endroits sans rapport avec le chat, donc son propre cache ne suffit pas seul.
            if (!string.IsNullOrWhiteSpace(entry.DisplayName)) chatKnownDisplayNames[entry.PlayerId] = entry.DisplayName;

            if (!ChatServerConnected())
            {
                ShowChatToast("Chat serveur indisponible : impossible d'ouvrir une discussion.");
                return;
            }

            // Le meme selecteur sert a deux choses : ouvrir une discussion privee, ou ajouter un
            // membre a un groupe existant (le leader arrive ici depuis l'ecran Membres).
            if (!string.IsNullOrWhiteSpace(chatAddMembersConversationId))
            {
                LivingHiveChatRuntime.InviteToGroupAsync(chatAddMembersConversationId, entry.PlayerId.ToString("D"));
                ShowChatToast("Invitation envoyee a " + entry.DisplayName + ".");
                return;
            }

            // M059C-CL : ChatSelectChannel("private") seul ne fait que basculer d'onglet et
            // choisir la PREMIERE conversation privee existante - sans lien avec le joueur tape.
            // La vraie ouverture (creation OU retrouve, idempotent cote serveur) passe par
            // CreatePrivateConversationAsync, qui selectionne ensuite la conversation reelle des
            // qu'elle revient - voir LivingHiveChatController.CreatePrivateConversationAsync.
            //
            // M059D-CL - preuve runtime (session CEO du 2026-09-07) : ChatSelectChannel("private")
            // ci-dessus choisit cette PREMIERE conversation existante de facon SYNCHRONE, avant meme
            // que CreatePrivateConversationAsync (asynchrone, fire-and-forget) n'ait cree/selectionne
            // la VRAIE conversation avec le joueur tape cote controleur. Rien ne resynchronisait
            // ensuite chatSelectedConversation (UI) sur la selection reelle du controleur - l'ecran
            // continuait donc d'afficher une autre conversation (existante mais non pertinente,
            // "Aucun message pour le moment") alors que les messages arrivaient bien, correctement
            // rattaches, sous le vrai identifiant retourne ici.
            CloseChatRoyalOverlays();
            ChatSelectChannel("private");
            ChatStartPrivateConversationAndSyncSelection(entry.PlayerId.ToString("D"), entry.DisplayName);
            ShowChatToast("Discussion avec " + entry.DisplayName + " demandee.");
            LivingHiveChatRuntime.RefreshInvitationsAsync();
        }

        // Meme patron fire-and-forget "async void" que ChatSendCurrentToServer / SendAllianceChatMessage :
        // jamais d'exception non observee, capturee par type seulement.
        private static async void ChatStartPrivateConversationAndSyncSelection(string participantPlayerId, string displayName)
        {
            try
            {
                string conversationId = await LivingHiveChatRuntime.CreatePrivateConversationAsync(participantPlayerId);
                if (string.IsNullOrWhiteSpace(conversationId)) return;
                ChatRoyalSyncFromServer();
                ChatConversationData conversation = ChatConversationById(conversationId);
                if (conversation != null)
                {
                    conversation.Peer = displayName;
                    conversation.Title = ChatPrivateDisplayName(ChatServerSnapshot()?.Conversations
                        .FirstOrDefault(item => item.ConversationId == conversationId)?.Title, displayName);
                }
                chatSelectedConversation = conversationId;
                chatMessagesScroll = Vector2.zero;
                chatActionMessageIndex = -1;
            }
            catch (Exception exception) { Debug.LogWarning("[ChatRoyal] Failed to open private conversation: " + exception.GetType().Name); }
        }

        private static void ChatCreateGroupFromSelection()
        {
            string title = (chatNewGroupTitle ?? string.Empty).Trim();
            string[] invitees = chatGroupSelection.Select(item => item.PlayerId.ToString("D")).ToArray();
            ChatCreateGroupAndSyncSelection(title, invitees);
            CloseChatRoyalOverlays();
            EnsureChatGroupsChannel();
            ChatSelectChannel(ChatGroupsChannelId);
            ShowChatToast("Groupe « " + title + " » demande (" + invitees.Length + " invitation(s)).");
        }

        // M066-CL : meme piege que ChatStartPrivateConversation (M059D-CL) - ChatSelectChannel(...)
        // ci-dessus choisit de facon SYNCHRONE le PREMIER groupe deja connu, avant meme que
        // CreateGroupAsync (asynchrone, fire-and-forget) n'ait cree/selectionne le VRAI groupe cote
        // controleur. Meme correctif : on resynchronise chatSelectedConversation sur l'identifiant
        // reellement retourne des qu'il revient - sinon un groupe existant s'affiche a la place du
        // groupe qu'on vient de creer.
        private static async void ChatCreateGroupAndSyncSelection(string title, string[] invitees)
        {
            try
            {
                string conversationId = await LivingHiveChatRuntime.CreateGroupAsync(title, invitees);
                if (string.IsNullOrWhiteSpace(conversationId)) return;
                ChatRoyalSyncFromServer();
                chatSelectedConversation = conversationId;
                chatMessagesScroll = Vector2.zero;
                chatActionMessageIndex = -1;
            }
            catch (Exception exception) { Debug.LogWarning("[ChatRoyal] Failed to open new group: " + exception.GetType().Name); }
        }

        // 3.4 : gestion du groupe selectionne - membres, icone createur, ajouter/exclure, transfert.
        // Modal plein ecran (mobile uniquement depuis M068-CL - voir DrawChatGroupMembersPanel pour
        // le panneau ancre desktop). Meme contenu, factorise dans DrawChatGroupMembersBody.
        private static void DrawChatGroupMembersOverlay(bool compact)
        {
            Rect panel = ChatOverlayRect(compact);
            LivingHiveChatSnapshot snapshot = ChatServerSnapshot();
            LivingHiveChatGroupDetail detail = snapshot?.SelectedGroup;
            DrawChatOverlayChrome(panel, "GROUPE : " + (detail?.Title ?? "—"), CloseChatRoyalOverlays);

            if (detail == null)
            {
                GUI.Label(new Rect(panel.x + 18f, panel.y + 60f, panel.width - 36f, 40f),
                    "Selectionnez un groupe dans l'onglet Groupes.", new GUIStyle(smallStyle) { fontSize = 13, wordWrap = true });
                return;
            }

            DrawChatGroupMembersBody(new Rect(panel.x, panel.y + 44f, panel.width, panel.height - 44f), detail);
        }

        // M068-CL : panneau "MEMBRES (n)" ancre a droite de la conversation sur desktop, comme la
        // reference visuelle fournie par le CEO - dimensionne et positionne par DrawChatMainLayout
        // (via ChatGroupMembersPanelVisible). Meme donnees/logique que le modal mobile
        // (DrawChatGroupMembersBody) : ajouter, exclure, transferer, quitter passent tous par les
        // memes fonctions LivingHiveChatRuntime deja fonctionnelles (M067-CL) - rien de recree ici.
        private static void DrawChatGroupMembersPanel(Rect panel, bool compact)
        {
            DrawPremiumPanel(panel, new Color(0.024f, 0.021f, 0.017f, 0.97f), new Color(0.78f, 0.52f, 0.15f, 0.70f));
            LivingHiveChatSnapshot snapshot = ChatServerSnapshot();
            LivingHiveChatGroupDetail detail = snapshot?.SelectedGroup;

            int memberCount = detail?.Members.Count ?? 0;
            GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 60f, 24f), "MEMBRES (" + memberCount + ")",
                new GUIStyle(badgeStyle) { fontSize = 14, alignment = TextAnchor.MiddleLeft });
            Rect close = new Rect(panel.xMax - 38f, panel.y + 8f, 26f, 26f);
            DrawPremiumPanel(close, new Color(0.10f, 0.07f, 0.04f, 0.96f), new Color(0.72f, 0.48f, 0.16f, 0.85f));
            GUI.Label(close, "✕", new GUIStyle(centeredTinyLabelStyle) { fontSize = 12 });
            if (GUI.Button(close, string.Empty, GUIStyle.none))
            {
                AudioManager.Instance?.PlayUIClick();
                chatGroupMembersOpen = false;
            }
            GUI.color = new Color(1f, 0.60f, 0.14f, 0.80f);
            GUI.DrawTexture(new Rect(panel.x + 12f, panel.y + 42f, panel.width - 24f, 1f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;

            if (detail == null)
            {
                GUI.Label(new Rect(panel.x + 14f, panel.y + 56f, panel.width - 28f, 40f),
                    "Selectionnez un groupe.", new GUIStyle(smallStyle) { fontSize = 12, wordWrap = true });
                return;
            }

            DrawChatGroupMembersBody(new Rect(panel.x, panel.y + 44f, panel.width, panel.height - 44f), detail);
        }

        // Contenu partage entre le modal mobile et le panneau desktop : liste des membres (couronne,
        // "Leader"/"Exclure" avec confirmation M067-CL), invitations en attente, "Ajouter des
        // membres" et "Quitter". `area` est la zone SOUS l'en-tete deja dessine par l'appelant.
        private static void DrawChatGroupMembersBody(Rect area, LivingHiveChatGroupDetail detail)
        {
            float y = area.y + 4f;
            GUI.Label(new Rect(area.x + 14f, y, area.width - 28f, 20f),
                detail.ViewerIsLeader ? "Vous etes le createur de ce groupe." : "Membre du groupe.",
                new GUIStyle(smallStyle) { fontSize = 12 });
            y += 28f;

            if (detail.ViewerIsLeader)
            {
                Rect add = new Rect(area.x + 14f, y, area.width - 28f, 36f);
                DrawPremiumPanel(add, new Color(0.28f, 0.19f, 0.05f, 0.96f), ChatAccentColor());
                GUI.Label(add, "➕ Ajouter des membres", new GUIStyle(centeredTinyLabelStyle) { fontSize = 12 });
                if (GUI.Button(add, string.Empty, GUIStyle.none))
                {
                    AudioManager.Instance?.PlayUIClick();
                    chatGroupMembersOpen = false;
                    chatNewGroupOpen = false;
                    chatNewDiscussionOpen = false;
                    ChatOpenAddMembersPicker(detail);
                }
                y += 46f;
            }

            float footerH = detail.ViewerIsLeader ? 78f : 60f;
            Rect list = new Rect(area.x + 14f, y, area.width - 28f, Mathf.Max(1f, area.yMax - y - footerH));
            DrawPremiumPanel(list, new Color(0.02f, 0.018f, 0.014f, 0.94f), new Color(0.5f, 0.34f, 0.12f, 0.55f));

            IReadOnlyList<LivingHiveChatGroupMember> members = detail.Members;
            float rowH = detail.ViewerIsLeader ? 64f : 48f;
            float gap = 6f;
            chatGroupMembersScroll = GUI.BeginScrollView(list, chatGroupMembersScroll,
                new Rect(0f, 0f, list.width - 18f, Mathf.Max(1, members.Count + detail.PendingInvites.Count) * (rowH + gap)));
            int rowIndex = 0;
            foreach (LivingHiveChatGroupMember member in members)
            {
                Rect row = new Rect(4f, rowIndex * (rowH + gap) + 2f, list.width - 26f, rowH);
                DrawPremiumPanel(row, new Color(0.06f, 0.05f, 0.03f, 0.92f), member.IsLeader ? ChatAccentColor() : new Color(0.52f, 0.36f, 0.12f, 0.6f));
                // M070-CL : avatar rond avec initiales (reference CEO), meme texture que les autres
                // avatars du chat (DrawRoundAvatarBase, defini dans HiveViewProductUiPresenter.cs).
                Rect avatar = new Rect(row.x + 8f, row.y + (rowH - 34f) * 0.5f, 34f, 34f);
                DrawRoundAvatarBase(avatar);
                GUI.Label(avatar, ChatInitials(member.DisplayName), new GUIStyle(titleStyle) { fontSize = 13, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.82f, 0.36f, 1f) } });
                float textX = avatar.xMax + 10f;
                float textW = row.width - (textX - row.x) - 10f;
                GUI.Label(new Rect(textX, row.y + 6f, textW, 20f),
                    (member.IsLeader ? "👑 " : "") + member.DisplayName,
                    new GUIStyle(smallStyle) { fontSize = 13, alignment = TextAnchor.MiddleLeft });
                GUI.Label(new Rect(textX, row.y + 27f, textW, 16f),
                    member.IsLeader ? "CHEF" : "Membre",
                    new GUIStyle(tinyLabelStyle) { fontSize = 9, fontStyle = member.IsLeader ? FontStyle.Bold : FontStyle.Normal, alignment = TextAnchor.MiddleLeft, normal = { textColor = member.IsLeader ? ChatAccentColor() : new Color(0.7f, 0.62f, 0.5f, 0.85f) } });

                if (detail.ViewerIsLeader && !member.IsLeader)
                {
                    float btnW = (row.width - 30f) * 0.5f;
                    Rect promote = new Rect(row.x + 10f, row.yMax - 24f, btnW, 20f);
                    Rect kick = new Rect(promote.xMax + 10f, row.yMax - 24f, btnW, 20f);
                    string kickKey = "kick|" + member.PlayerId;
                    bool kickArmed = string.Equals(chatGroupKickConfirmArmedId, kickKey, StringComparison.Ordinal) && NowForUi() - chatGroupKickConfirmArmedAt <= 5f;
                    DrawPremiumPanel(promote, new Color(0.16f, 0.20f, 0.30f, 0.94f), new Color(0.52f, 0.72f, 1f, 0.8f));
                    GUI.Label(promote, "Leader", new GUIStyle(centeredTinyLabelStyle) { fontSize = 9 });
                    DrawPremiumPanel(kick, new Color(0.30f, 0.12f, 0.10f, 0.94f), kickArmed ? new Color(1f, 0.62f, 0.2f, 0.95f) : new Color(0.92f, 0.46f, 0.40f, 0.85f));
                    GUI.Label(kick, kickArmed ? "Confirmer ?" : "Exclure", new GUIStyle(centeredTinyLabelStyle) { fontSize = 9 });
                    if (GUI.Button(promote, string.Empty, GUIStyle.none))
                    {
                        AudioManager.Instance?.PlayUIClick();
                        LivingHiveChatRuntime.TransferLeadershipAsync(detail.ConversationId, member.PlayerId);
                        ShowChatToast("Leadership transfere a " + member.DisplayName + ".");
                    }
                    else if (GUI.Button(kick, string.Empty, GUIStyle.none))
                    {
                        AudioManager.Instance?.PlayUIClick();
                        if (!kickArmed)
                        {
                            chatGroupKickConfirmArmedId = kickKey;
                            chatGroupKickConfirmArmedAt = NowForUi();
                        }
                        else
                        {
                            chatGroupKickConfirmArmedId = string.Empty;
                            LivingHiveChatRuntime.RemoveGroupMemberAsync(detail.ConversationId, member.PlayerId);
                            ShowChatToast(member.DisplayName + " a ete exclu du groupe.");
                        }
                    }
                }
                rowIndex++;
            }

            foreach (LivingHiveChatInvitation invite in detail.PendingInvites)
            {
                Rect row = new Rect(4f, rowIndex * (rowH + gap) + 2f, list.width - 26f, rowH);
                DrawPremiumPanel(row, new Color(0.05f, 0.045f, 0.03f, 0.86f), new Color(0.42f, 0.32f, 0.14f, 0.5f));
                GUI.Label(new Rect(row.x + 10f, row.y + 4f, row.width - 20f, 20f),
                    "⏳ " + (invite.InviteeDisplayName ?? "Invitation"),
                    new GUIStyle(smallStyle) { fontSize = 12, alignment = TextAnchor.MiddleLeft });
                GUI.Label(new Rect(row.x + 10f, row.y + 24f, row.width - 20f, 16f), "En attente",
                    new GUIStyle(tinyLabelStyle) { fontSize = 9, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.86f, 0.62f, 0.28f, 0.9f) } });
                if (detail.ViewerIsLeader)
                {
                    Rect cancel = new Rect(row.xMax - 82f, row.yMax - 24f, 72f, 20f);
                    DrawPremiumPanel(cancel, new Color(0.24f, 0.14f, 0.10f, 0.92f), new Color(0.82f, 0.48f, 0.36f, 0.7f));
                    GUI.Label(cancel, "Annuler", new GUIStyle(centeredTinyLabelStyle) { fontSize = 9 });
                    if (GUI.Button(cancel, string.Empty, GUIStyle.none))
                    {
                        AudioManager.Instance?.PlayUIClick();
                        LivingHiveChatRuntime.RemoveGroupMemberAsync(detail.ConversationId, invite.InviteePlayerId);
                    }
                }
                rowIndex++;
            }
            GUI.EndScrollView();

            // M070-CL : libelle toujours "QUITTER LE GROUPE" (majuscules, reference CEO) - l'avis
            // "transferez d'abord le leadership" passe en petite ligne au dessus du bouton au lieu
            // de remplacer son libelle, la regle backend elle-meme reste inchangee
            // (ChatLeaveGroupAndClose verifie deja le resultat serveur, voir M067-CL).
            if (detail.ViewerIsLeader)
            {
                GUI.Label(new Rect(area.x + 14f, area.yMax - 66f, area.width - 28f, 16f),
                    "Transferez d'abord le leadership pour quitter.",
                    new GUIStyle(centeredTinyLabelStyle) { fontSize = 9, normal = { textColor = new Color(1f, 0.72f, 0.42f, 0.85f) } });
            }
            Rect leave = new Rect(area.x + 14f, area.yMax - 46f, area.width - 28f, 38f);
            DrawPremiumPanel(leave, new Color(0.30f, 0.08f, 0.07f, 0.96f), new Color(1f, 0.72f, 0.24f, 0.85f));
            GUI.Label(leave, "QUITTER LE GROUPE", new GUIStyle(centeredTinyLabelStyle) { fontSize = 12, fontStyle = FontStyle.Bold });
            if (GUI.Button(leave, string.Empty, GUIStyle.none))
            {
                AudioManager.Instance?.PlayUIClick();
                ChatLeaveGroupAndClose(detail.ConversationId);
            }
        }

        // M067-CL : le controleur avale deja l'exception serveur (leader-must-transfer, regle deja
        // appliquee cote backend - rien recree ici) et se contente de journaliser un statut d'erreur -
        // fermer l'ecran sans verifier laissait croire que "Quitter" avait reussi alors que le
        // serveur avait refuse. On attend le retour, puis on ne ferme que si le groupe a
        // effectivement disparu de la liste des conversations.
        private static async void ChatLeaveGroupAndClose(string conversationId)
        {
            try
            {
                await LivingHiveChatRuntime.LeaveGroupAsync(conversationId);
                ChatRoyalSyncFromServer();
                bool stillMember = ChatServerSnapshot()?.Conversations.Any(item => string.Equals(item.ConversationId, conversationId, StringComparison.Ordinal)) == true;
                if (stillMember)
                {
                    ShowChatToast("Impossible de quitter : transferez d'abord le leadership.");
                    return;
                }
                CloseChatRoyalOverlays();
            }
            catch (Exception exception) { Debug.LogWarning("[ChatRoyal] Failed to leave group: " + exception.GetType().Name); }
        }

        // Ajout de membres apres coup : on reutilise le selecteur, mais la validation invite dans le
        // groupe existant au lieu d'en creer un nouveau.
        private static string chatAddMembersConversationId = string.Empty;

        private static void ChatOpenAddMembersPicker(LivingHiveChatGroupDetail detail)
        {
            chatAddMembersConversationId = detail.ConversationId;
            chatGroupSelection.Clear();
            chatPickerQuery = string.Empty;
            chatPlayerPicker.Clear();
            chatNewDiscussionOpen = true;
        }

        // 3.6 : parametres - couleur d'accent (locale) + regle d'auto-reponse (serveur).
        private static void DrawChatSettingsOverlay(bool compact)
        {
            Rect panel = ChatOverlayRect(compact);
            DrawChatOverlayChrome(panel, "PARAMETRES DU CHAT", CloseChatRoyalOverlays);

            float y = panel.y + 52f;
            GUI.Label(new Rect(panel.x + 18f, y, panel.width - 36f, 20f), "Couleur d'accent (cet appareil)", new GUIStyle(smallStyle) { fontSize = 11 });
            y += 24f;
            float swatchWidth = (panel.width - 36f - (ChatAccentColors.Length - 1) * 8f) / ChatAccentColors.Length;
            for (int i = 0; i < ChatAccentColors.Length; i++)
            {
                Rect swatch = new Rect(panel.x + 18f + i * (swatchWidth + 8f), y, swatchWidth, 44f);
                bool active = i == ChatAccentIndex();
                DrawPremiumPanel(swatch, new Color(ChatAccentColors[i].r * 0.32f, ChatAccentColors[i].g * 0.32f, ChatAccentColors[i].b * 0.32f, 0.96f), active ? Color.white : ChatAccentColors[i]);
                GUI.Label(new Rect(swatch.x, swatch.y + 26f, swatch.width, 16f), ChatAccentNames[i], new GUIStyle(centeredTinyLabelStyle) { fontSize = 7 });
                if (GUI.Button(swatch, string.Empty, GUIStyle.none))
                {
                    AudioManager.Instance?.PlayUIClick();
                    SetChatAccentIndex(i);
                }
            }
            y += 60f;

            GUI.Label(new Rect(panel.x + 18f, y, panel.width - 36f, 20f), "Invitations de groupe", new GUIStyle(smallStyle) { fontSize = 11 });
            y += 22f;
            GUI.Label(new Rect(panel.x + 18f, y, panel.width - 36f, 30f),
                "Cette regle est enregistree sur le serveur : elle s'applique meme lorsque vous etes hors ligne.",
                new GUIStyle(smallStyle) { fontSize = 9, wordWrap = true });
            y += 32f;

            LivingHiveChatSnapshot snapshot = ChatServerSnapshot();
            string current = snapshot?.AutoInviteResponse ?? RemoteChatAutoInviteResponse.Ask;
            string[] rules = { RemoteChatAutoInviteResponse.Ask, RemoteChatAutoInviteResponse.Accept, RemoteChatAutoInviteResponse.Decline };
            string[] ruleLabels = { "Demander a chaque fois", "Accepter automatiquement", "Refuser automatiquement" };
            for (int i = 0; i < rules.Length; i++)
            {
                Rect option = new Rect(panel.x + 18f, y + i * 40f, panel.width - 36f, 34f);
                bool active = string.Equals(current, rules[i], StringComparison.Ordinal);
                DrawPremiumPanel(option, active ? new Color(0.28f, 0.19f, 0.05f, 0.96f) : new Color(0.05f, 0.045f, 0.03f, 0.92f), active ? ChatAccentColor() : new Color(0.5f, 0.34f, 0.12f, 0.55f));
                GUI.Label(new Rect(option.x + 12f, option.y + 8f, option.width - 24f, 20f), (active ? "● " : "○ ") + ruleLabels[i], new GUIStyle(smallStyle) { fontSize = 11, alignment = TextAnchor.MiddleLeft });
                if (!active && GUI.Button(option, string.Empty, GUIStyle.none))
                {
                    AudioManager.Instance?.PlayUIClick();
                    if (!ChatServerConnected()) ShowChatToast("Chat serveur indisponible : preference non enregistree.");
                    else
                    {
                        LivingHiveChatRuntime.UpdateAutoInviteResponseAsync(rules[i]);
                        ShowChatToast("Preference enregistree.");
                    }
                }
            }
        }
    }
}
