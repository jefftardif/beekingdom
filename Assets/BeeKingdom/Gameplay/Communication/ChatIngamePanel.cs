using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeeKingdom.Gameplay.Communication
{
    public sealed class ChatIngamePanel : MonoBehaviour
    {
        [SerializeField] private bool visibleOnStart = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F9;
        [SerializeField] private string currentPlayerId = "player_queen";
        [SerializeField] private string allianceId = "alliance_demo";
        [SerializeField] private string serverId = "server_demo";
        [SerializeField] private string privateRecipientId = "player_scout";
        [SerializeField] private int maxRenderedMessages = 60;
        [SerializeField] private float refreshInterval = 0.35f;

        private static readonly ChannelType[] ChannelOrder = { ChannelType.Alliance, ChannelType.Server, ChannelType.Private, ChannelType.Leadership };

        private static readonly Color PanelBackground = new Color(0.058f, 0.047f, 0.03f, 0.95f);
        private static readonly Color PanelBorder = new Color(0.86f, 0.63f, 0.16f, 1f);
        private static readonly Color TabActiveBackground = new Color(0.86f, 0.63f, 0.16f, 1f);
        private static readonly Color TabInactiveBackground = new Color(0.16f, 0.13f, 0.08f, 1f);
        private static readonly Color TabActiveText = new Color(0.1f, 0.07f, 0.02f, 1f);
        private static readonly Color TabInactiveText = new Color(0.82f, 0.76f, 0.6f, 1f);
        private static readonly Color TextAmber = new Color(1f, 0.86f, 0.55f, 1f);
        private static readonly Color TextMuted = new Color(0.72f, 0.68f, 0.6f, 1f);
        private static readonly Color TextError = new Color(1f, 0.45f, 0.32f, 1f);
        private static readonly Color MessageRowBackground = new Color(0.1f, 0.085f, 0.055f, 0.9f);
        private static readonly Color ComposerBackground = new Color(0.12f, 0.1f, 0.065f, 1f);

        private readonly Dictionary<ChannelType, Conversation> conversations = new Dictionary<ChannelType, Conversation>();
        private readonly Dictionary<ChannelType, string> channelErrors = new Dictionary<ChannelType, string>();
        private readonly Dictionary<ChannelType, Button> channelButtons = new Dictionary<ChannelType, Button>();
        private readonly Dictionary<ChannelType, Image> channelButtonBackgrounds = new Dictionary<ChannelType, Image>();
        private readonly Dictionary<ChannelType, TMP_Text> channelButtonLabels = new Dictionary<ChannelType, TMP_Text>();

        private IChatProvider provider;
        private IDisposable subscription;
        private ChannelType selectedChannel = ChannelType.Alliance;
        private string statusLine = string.Empty;
        private bool visible;
        private float refreshTimer;
        private bool eventPending;

        private RectTransform canvasRoot;
        private GameObject toggleBadge;
        private TMP_Text toggleBadgeLabel;
        private GameObject panelRoot;
        private TMP_Text titleLabel;
        private TMP_Text statusLabel;
        private RectTransform messageContent;
        private TMP_InputField draftInput;
        private Button sendButton;
        private TMP_Text sendButtonLabel;

        public IChatProvider Provider => provider;
        public ChannelType SelectedChannel => selectedChannel;

        private void Awake()
        {
            visible = visibleOnStart;
            BuildUi();
            RefreshLocalizedStaticTexts();
            SetVisible(visible);
            BeeLocalization.LocaleChanged += OnLocaleChanged;

            if (provider == null)
            {
                SetProvider(new LocalChatProvider(currentPlayerId), currentPlayerId);
            }
        }

        private void OnDestroy()
        {
            subscription?.Dispose();
            subscription = null;
            BeeLocalization.LocaleChanged -= OnLocaleChanged;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey)) SetVisible(!visible);
            if (!visible) return;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f || eventPending)
            {
                refreshTimer = refreshInterval;
                eventPending = false;
                RefreshMessages();
            }
        }

        public void SetProvider(IChatProvider chatProvider, string playerId)
        {
            provider = chatProvider ?? throw new ArgumentNullException(nameof(chatProvider));
            currentPlayerId = string.IsNullOrWhiteSpace(playerId) ? currentPlayerId : playerId.Trim();
            subscription?.Dispose();
            subscription = provider.Subscribe(OnChatEvent);
            conversations.Clear();
            channelErrors.Clear();
            EnsureDefaultConversations();
            RefreshChannelTabs();
            statusLine = BuildProviderStatus();
            if (statusLabel != null) statusLabel.text = statusLine;
            RefreshMessages();
        }

        // ----- UI construction -----

        private void BuildUi()
        {
            GameObject canvasGo = new GameObject("BeeKingdomChatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRoot = canvasGo.GetComponent<RectTransform>();

            EnsureEventSystem();
            BuildToggleBadge();
            BuildPanel();
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(eventSystemGo);
        }

        private void BuildToggleBadge()
        {
            toggleBadge = CreateImage("ChatToggleBadge", canvasRoot, TabInactiveBackground);
            RectTransform rect = toggleBadge.GetComponent<RectTransform>();
            SetAnchor(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            rect.sizeDelta = new Vector2(170f, 40f);
            rect.anchoredPosition = new Vector2(-16f, -16f);

            Button button = toggleBadge.AddComponent<Button>();
            button.targetGraphic = toggleBadge.GetComponent<Image>();
            button.onClick.AddListener(() => SetVisible(!visible));

            toggleBadgeLabel = CreateText(toggleBadge.transform, "Label", 14f, TextAmber, TextAlignmentOptions.Center);
            SetStretch(toggleBadgeLabel.rectTransform, 6f, 6f, 4f, 4f);
        }

        private void BuildPanel()
        {
            panelRoot = CreateImage("ChatPanel", canvasRoot, PanelBackground);
            RectTransform rect = panelRoot.GetComponent<RectTransform>();
            SetAnchor(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f));
            rect.sizeDelta = new Vector2(460f, 560f);
            rect.anchoredPosition = new Vector2(-16f, 16f);
            AddBorder(panelRoot.GetComponent<RectTransform>(), PanelBorder, 2f);

            VerticalLayoutGroup layout = panelRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildHeader(panelRoot.transform);
            BuildChannelTabs(panelRoot.transform);
            BuildMessageScroll(panelRoot.transform);
            BuildComposer(panelRoot.transform);
        }

        private void BuildHeader(Transform parent)
        {
            titleLabel = CreateText(parent, "Title", 18f, TextAmber, TextAlignmentOptions.Left);
            titleLabel.fontStyle = FontStyles.Bold;
            AddLayoutElement(titleLabel.rectTransform, preferredHeight: 26f);

            statusLabel = CreateText(parent, "Status", 11f, TextMuted, TextAlignmentOptions.Left);
            AddLayoutElement(statusLabel.rectTransform, preferredHeight: 18f);
        }

        private void BuildChannelTabs(Transform parent)
        {
            GameObject row = new GameObject("ChannelTabs", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            AddLayoutElement(row.GetComponent<RectTransform>(), preferredHeight: 32f);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            foreach (ChannelType channel in ChannelOrder)
            {
                GameObject buttonGo = CreateImage("Tab_" + channel, row.transform, TabInactiveBackground);
                Button button = buttonGo.AddComponent<Button>();
                button.targetGraphic = buttonGo.GetComponent<Image>();
                ChannelType captured = channel;
                button.onClick.AddListener(() => SelectChannel(captured));

                TMP_Text label = CreateText(buttonGo.transform, "Label", 13f, TabInactiveText, TextAlignmentOptions.Center);
                SetStretch(label.rectTransform, 4f, 4f, 2f, 2f);

                channelButtons[channel] = button;
                channelButtonBackgrounds[channel] = buttonGo.GetComponent<Image>();
                channelButtonLabels[channel] = label;
            }
        }

        private void BuildMessageScroll(Transform parent)
        {
            GameObject scrollGo = CreateImage("MessageScroll", parent, new Color(0f, 0f, 0f, 0.18f));
            AddLayoutElement(scrollGo.GetComponent<RectTransform>(), flexibleHeight: 1f);
            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            Image viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            SetStretch(viewportRect, 0f, 0f, 0f, 0f);

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            messageContent = contentGo.GetComponent<RectTransform>();
            SetAnchor(messageContent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            messageContent.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = messageContent;
        }

        private void BuildComposer(Transform parent)
        {
            GameObject row = new GameObject("Composer", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            AddLayoutElement(row.GetComponent<RectTransform>(), preferredHeight: 40f);
            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            GameObject fieldGo = CreateImage("DraftField", row.transform, ComposerBackground);
            LayoutElement fieldLayoutElement = fieldGo.AddComponent<LayoutElement>();
            fieldLayoutElement.flexibleWidth = 1f;
            draftInput = fieldGo.AddComponent<TMP_InputField>();

            TMP_Text placeholder = CreateText(fieldGo.transform, "Placeholder", 13f, TextMuted, TextAlignmentOptions.Left);
            placeholder.fontStyle = FontStyles.Italic;
            SetStretch(placeholder.rectTransform, 8f, 8f, 4f, 4f);

            TMP_Text textComponent = CreateText(fieldGo.transform, "Text", 13f, TextAmber, TextAlignmentOptions.Left);
            SetStretch(textComponent.rectTransform, 8f, 8f, 4f, 4f);

            draftInput.textViewport = fieldGo.GetComponent<RectTransform>();
            draftInput.textComponent = textComponent;
            draftInput.placeholder = placeholder;
            draftInput.lineType = TMP_InputField.LineType.SingleLine;
            draftInput.onSubmit.AddListener(_ => SendDraft());

            GameObject sendGo = CreateImage("SendButton", row.transform, TabActiveBackground);
            LayoutElement sendLayoutElement = sendGo.AddComponent<LayoutElement>();
            sendLayoutElement.preferredWidth = 92f;
            sendButton = sendGo.AddComponent<Button>();
            sendButton.targetGraphic = sendGo.GetComponent<Image>();
            sendButton.onClick.AddListener(SendDraft);
            sendButtonLabel = CreateText(sendGo.transform, "Label", 13f, TabActiveText, TextAlignmentOptions.Center);
            sendButtonLabel.fontStyle = FontStyles.Bold;
            SetStretch(sendButtonLabel.rectTransform, 4f, 4f, 4f, 4f);
        }

        private void SetVisible(bool value)
        {
            visible = value;
            if (panelRoot != null) panelRoot.SetActive(visible);
            if (toggleBadgeLabel != null) toggleBadgeLabel.text = BuildToggleBadgeText();
            if (visible) RefreshMessages();
        }

        private void SelectChannel(ChannelType channel)
        {
            if (!conversations.ContainsKey(channel) || selectedChannel == channel) return;
            selectedChannel = channel;
            statusLine = BuildProviderStatus();
            if (statusLabel != null) statusLabel.text = statusLine;
            RefreshChannelTabs();
            RefreshMessages();
        }

        private void RefreshChannelTabs()
        {
            foreach (ChannelType channel in ChannelOrder)
            {
                bool available = conversations.ContainsKey(channel);
                bool selected = selectedChannel == channel;
                if (channelButtons.TryGetValue(channel, out Button button)) button.interactable = available;
                if (channelButtonBackgrounds.TryGetValue(channel, out Image background)) background.color = selected ? TabActiveBackground : TabInactiveBackground;
                if (channelButtonLabels.TryGetValue(channel, out TMP_Text label)) label.color = selected ? TabActiveText : TabInactiveText;
            }
        }

        // ----- Message rendering -----

        private void RefreshMessages()
        {
            if (messageContent == null) return;
            for (int i = messageContent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(messageContent.GetChild(i).gameObject);
            }

            if (!conversations.TryGetValue(selectedChannel, out Conversation conversation))
            {
                string error = channelErrors.TryGetValue(selectedChannel, out string value) ? value : BeeLocalization.Text("chat.unavailable_channel", "Canal indisponible.");
                CreateMessageRow(error, null, TextError);
                return;
            }

            MessagePage page;
            try
            {
                page = provider.GetMessages(conversation.Id, 0, maxRenderedMessages);
            }
            catch (Exception ex)
            {
                CreateMessageRow("Lecture impossible: " + ex.Message, null, TextError);
                return;
            }

            IReadOnlyList<MessageRecord> messages = page.Items;
            if (messages.Count == 0)
            {
                CreateMessageRow(BeeLocalization.Text("chat.empty", "Aucun message"), null, TextMuted);
            }

            for (int i = 0; i < messages.Count; i++)
            {
                DrawMessage(messages[i]);
            }

            int lastSequence = messages.Where(message => message.Sequence.HasValue).Select(message => message.Sequence.Value).DefaultIfEmpty(0).Max();
            if (lastSequence > 0)
            {
                try { provider.MarkConversationRead(conversation.Id, lastSequence); }
                catch
                {
                    // Read cursors are best-effort for the first ingame panel.
                }
            }
        }

        private void DrawMessage(MessageRecord message)
        {
            bool mine = string.Equals(message.SenderId, currentPlayerId, StringComparison.Ordinal);
            string prefix = mine ? BeeLocalization.Text("chat.mine", "Moi") : (string.IsNullOrEmpty(message.SenderDisplayName) ? BeeLocalization.Text("chat.sender.unknown", "Joueur") : message.SenderDisplayName);
            string sequence = message.Sequence.HasValue ? "#" + message.Sequence.Value.ToString("000") : message.State.ToString();

            GameObject row = CreateImage("Message", messageContent, MessageRowBackground);
            AddLayoutElement(row.GetComponent<RectTransform>(), preferredHeight: -1f);
            VerticalLayoutGroup rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 6, 6);
            rowLayout.spacing = 2f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;
            ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text meta = CreateText(row.transform, "Meta", 11f, TextMuted, TextAlignmentOptions.Left);
            meta.text = prefix + "  " + sequence + "  " + message.State;
            AddLayoutElement(meta.rectTransform, preferredHeight: 16f);

            TMP_Text body = CreateText(row.transform, "Body", 13f, TextAmber, TextAlignmentOptions.Left);
            body.text = message.Body;
            body.enableWordWrapping = true;
            ContentSizeFitter bodyFitter = body.gameObject.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void CreateMessageRow(string text, string meta, Color color)
        {
            GameObject row = new GameObject("InfoRow", typeof(RectTransform));
            row.transform.SetParent(messageContent, false);
            TMP_Text label = CreateText(row.transform, "Text", 13f, color, TextAlignmentOptions.Left);
            label.text = text;
            label.enableWordWrapping = true;
            AddLayoutElement(row.GetComponent<RectTransform>(), preferredHeight: 40f);
            SetStretch(label.rectTransform, 4f, 4f, 2f, 2f);
        }

        private void SendDraft()
        {
            if (draftInput == null || !conversations.TryGetValue(selectedChannel, out Conversation conversation)) return;
            string body = draftInput.text.Trim();
            if (body.Length == 0) return;

            ClientRequestId requestId = new ClientRequestId("unity_ingame_" + DateTime.UtcNow.Ticks.ToString());
            IEnumerable<string> recipients = selectedChannel == ChannelType.Private ? conversation.ParticipantIds.Where(id => !string.Equals(id, currentPlayerId, StringComparison.Ordinal)) : Enumerable.Empty<string>();
            SendMessageInput input = new SendMessageInput(conversation.Id, currentPlayerId, body, requestId, recipients, ExtractMentions(body, conversation));
            SendResult result = provider.SendMessage(input);
            draftInput.text = string.Empty;
            draftInput.ActivateInputField();
            statusLine = result.Accepted || result.Queued
                ? "Message " + result.Message.State + " sur " + selectedChannel
                : "Envoi refuse: " + result.ErrorCode;
            if (statusLabel != null) statusLabel.text = statusLine;
            RefreshMessages();
        }

        private IEnumerable<string> ExtractMentions(string body, Conversation conversation)
        {
            if (string.IsNullOrWhiteSpace(body)) return Enumerable.Empty<string>();
            List<string> mentions = new List<string>();
            string[] parts = body.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (!parts[i].StartsWith("@", StringComparison.Ordinal) || parts[i].Length < 2) continue;
                string token = parts[i].Substring(1).TrimEnd('.', ',', ';', ':', '!', '?');
                if (conversation.ParticipantIds.Contains(token, StringComparer.Ordinal)) mentions.Add(token);
            }

            return mentions.Distinct(StringComparer.Ordinal);
        }

        private void EnsureDefaultConversations()
        {
            TryCreate(ChannelType.Alliance, allianceId, "Alliance", null);
            TryCreate(ChannelType.Server, serverId, "Global", null);
            TryCreate(ChannelType.Private, null, "Prive", new[] { privateRecipientId });
            TryCreate(ChannelType.Leadership, allianceId, "Dirigeants", null);
            if (!conversations.ContainsKey(selectedChannel))
            {
                selectedChannel = conversations.Keys.OrderBy(channel => channel.ToString(), StringComparer.Ordinal).FirstOrDefault();
            }
        }

        private void TryCreate(ChannelType channelType, string contextId, string title, IEnumerable<string> participants)
        {
            try
            {
                Conversation conversation = provider.CreateConversation(new CreateConversationInput(channelType, contextId, title, participants));
                conversations[channelType] = conversation;
                channelErrors.Remove(channelType);
            }
            catch (Exception ex)
            {
                channelErrors[channelType] = ex.Message;
            }
        }

        private void OnChatEvent(ChatEvent chatEvent)
        {
            if (chatEvent == null) return;
            statusLine = chatEvent.EventType + " " + chatEvent.ConversationId.Value;
            eventPending = true;
        }

        private string BuildProviderStatus()
        {
            ChatCapabilities capabilities = provider.GetCapabilities();
            return capabilities.Provider + " | " + provider.GetConnectionState() + " | server=" + capabilities.Server + " | " + ChannelLabel(selectedChannel);
        }

        private string BuildToggleBadgeText()
        {
            return BeeLocalization.Text("chat.title", "Communication") + " " + (visible ? "▲" : "▼") + " (" + toggleKey + ")";
        }

        // ----- Localization -----

        private void OnLocaleChanged(string locale) => RefreshLocalizedStaticTexts();

        private void RefreshLocalizedStaticTexts()
        {
            if (titleLabel != null) titleLabel.text = BeeLocalization.Text("chat.title", "Communication");
            if (toggleBadgeLabel != null) toggleBadgeLabel.text = BuildToggleBadgeText();
            if (sendButtonLabel != null) sendButtonLabel.text = BeeLocalization.Text("chat.send", "Envoyer");
            if (draftInput != null && draftInput.placeholder is TMP_Text placeholder) placeholder.text = BeeLocalization.Text("chat.compose.placeholder", "Ecris un message...");

            foreach (ChannelType channel in ChannelOrder)
            {
                if (channelButtonLabels.TryGetValue(channel, out TMP_Text label)) label.text = ChannelLabel(channel);
            }

            RefreshMessages();
        }

        private static string ChannelLabel(ChannelType channel)
        {
            switch (channel)
            {
                case ChannelType.Alliance: return BeeLocalization.Text("chat.channel.alliance", "Alliance");
                case ChannelType.Server: return BeeLocalization.Text("chat.channel.server", "Global");
                case ChannelType.Private: return BeeLocalization.Text("chat.channel.private", "Prive");
                case ChannelType.Leadership: return BeeLocalization.Text("chat.channel.leadership", "Dirigeants");
                default: return channel.ToString();
            }
        }

        // ----- Small UI helpers -----

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            return text;
        }

        private static void AddLayoutElement(RectTransform rect, float preferredHeight = -1f, float flexibleHeight = -1f)
        {
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
            if (flexibleHeight >= 0f) element.flexibleHeight = flexibleHeight;
        }

        private static void AddBorder(RectTransform hostRect, Color color, float thickness)
        {
            Outline outline = hostRect.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(thickness, -thickness);
        }

        private static void SetAnchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }

        private static void SetStretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
