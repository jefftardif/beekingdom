using System;
using System.Globalization;
using System.Text;
using BeeKingdom.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Presenter = BeeKingdom.Playground.HiveViewProductUiPresenter;

namespace BeeKingdom.Playground
{
    // Presentation only: every value and command uses the existing Palace bridge.
    public sealed class HiveMapRoyalPalacePremiumWindow : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, .77f, .36f);
        private static readonly Color Paper = new Color(1f, .95f, .82f);
        private static readonly Color Muted = new Color(.80f, .73f, .61f);
        private Canvas canvas;
        private ScrollRect scroll;
        private TMP_FontAsset displayFont;
        private TextMeshProUGUI level, authority, status, nextLevel, description;
        private TextMeshProUGUI requirements, cost, duration, unlocks, blocked, actionCaption;
        private GameObject progressionCard, alphaNote;
        private Button actionButton, focusButton;
        private RectTransform progressFill;
        private Action onClose;
        private bool open;
        private Vector2 size;
        private Rect safeArea;
        private string locale;
        private float nextRead;

        public bool IsOpen => open;

        public void Show(Action close)
        {
            onClose = close;
            open = true;
            if (canvas == null || LayoutChanged()) Build();
            canvas.gameObject.SetActive(true);
            RefreshLabels();
        }

        public void Hide()
        {
            open = false;
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        public void SetSuppressed(bool value)
        {
            if (canvas != null) canvas.gameObject.SetActive(open && !value);
        }

        private void Update()
        {
            if (!open || canvas == null || !canvas.gameObject.activeSelf) return;
            if (LayoutChanged()) Build();
            if (Time.unscaledTime < nextRead) return;
            nextRead = Time.unscaledTime + .2f;
            RefreshLabels();
        }

        private bool LayoutChanged() => size != new Vector2(Screen.width, Screen.height)
            || safeArea != Screen.safeArea || locale != BeeLocalization.CurrentLocale;

        private void Build()
        {
            float retainedScroll = scroll != null ? scroll.verticalNormalizedPosition : 1f;
            if (canvas == null)
            {
                RectTransform root = Node(transform, "RoyalPalacePremiumCanvas");
                canvas = root.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 420;
                root.gameObject.AddComponent<GraphicRaycaster>();
                root.gameObject.AddComponent<CanvasScaler>();
                displayFont = Resources.Load<TMP_FontAsset>("Cinzel-Regular SDF");
            }
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = canvas.transform.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            size = new Vector2(Screen.width, Screen.height);
            safeArea = Screen.safeArea;
            locale = BeeLocalization.CurrentLocale;
            float scale = HiveMapPremiumBuildingWindow.PremiumCanvasScale(size);
            canvas.GetComponent<CanvasScaler>().scaleFactor = scale;
            Rect safe = HiveMapPremiumBuildingWindow.PremiumSafeCanvasRect(size, safeArea, scale);
            RectTransform backdrop = Plate(canvas.transform, "Backdrop", new Color(.055f, .037f, .024f));
            Place(backdrop, new Rect(0, 0, size.x / scale, size.y / scale));
            Grain(backdrop, "cave-background", .18f);
            RectTransform rootSafe = Node(canvas.transform, "SafeArea");
            Place(rootSafe, safe);
            float w = safe.width;
            bool narrow = w < 650f;
            float headerHeight = narrow ? 158f : 116f;
            float footerHeight = narrow ? 128f : 78f;
            RectTransform header = Plate(rootSafe, "Header", new Color(.085f, .060f, .036f));
            Place(header, new Rect(0, 0, w, headerHeight));
            Texture2D banner = Resources.Load<Texture2D>("PremiumBeeReference/BuildingBanners/administration_core");
            if (banner != null)
            {
                RectTransform art = Node(header, "PalaceBanner");
                Place(art, new Rect(w * .48f, 0, w * .52f, headerHeight));
                RawImage image = art.gameObject.AddComponent<RawImage>();
                image.texture = banner;
                image.color = new Color(1, 1, 1, .27f);
                image.raycastTarget = false;
            }
            Button back = ButtonAt(header, "Back", Text("common.back"), () => onClose?.Invoke());
            Place((RectTransform)back.transform, new Rect(16, 12, 52, 52));
            WorldMapHudLayout.StyleReturnButton(back);
            Button refresh = ButtonAt(header, "Refresh", Text("building_upgrade.action.refresh"),
                Presenter.RefreshRoyalPalaceUpgradeForExternalHost);
            Place((RectTransform)refresh.transform, new Rect(w - 138, 12, 122, 48));
            TextMeshProUGUI title = Label(header, "Title", 25, Gold, true);
            title.text = Text("building.administration_core.fullscreen_title");
            if (displayFont != null) title.font = displayFont;
            Place((RectTransform)title.transform, narrow
                ? new Rect(20, 72, w - 40, 36) : new Rect(138, 18, w - 290, 40));
            TextMeshProUGUI subtitle = Label(header, "Subtitle", 14, Paper);
            subtitle.text = Text("royal_palace.colony_level");
            Place((RectTransform)subtitle.transform, new Rect(20, headerHeight - 45, w - 40, 38));
            RectTransform rule = Plate(header, "GoldRule", Gold);
            Place(rule, new Rect(0, headerHeight - 1, w, 1));

            float contentWidth = Mathf.Min(w - 32f, 940f);
            RectTransform viewport = Node(rootSafe, "Viewport");
            Place(viewport, new Rect((w - contentWidth) * .5f, headerHeight + 8,
                contentWidth, Mathf.Max(48, safe.height - headerHeight - footerHeight - 16)));
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            RectTransform content = Node(viewport, "PalaceCards");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup cards = content.gameObject.AddComponent<VerticalLayoutGroup>();
            cards.padding = new RectOffset(0, 8, 8, 20);
            cards.spacing = 16;
            cards.childControlWidth = cards.childControlHeight = true;
            cards.childForceExpandWidth = true;
            cards.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            RectTransform rail = Plate(rootSafe, "ScrollRail", new Color(.15f, .095f, .045f));
            Place(rail, new Rect((w + contentWidth) * .5f - 6, headerHeight + 16,
                6, Mathf.Max(32, safe.height - headerHeight - footerHeight - 32)));
            RectTransform handle = Plate(rail, "ScrollHandle", Gold);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = handle.offsetMax = Vector2.zero;
            Scrollbar scrollbar = rail.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            RectTransform overview = Card(content, "Overview", Text("royal_palace.current_level"));
            level = FlowLabel(overview, "Level", 28, Gold);
            authority = FlowLabel(overview, "Authority", 13, Muted);
            alphaNote = FlowLabel(overview, "Alpha", 13, Muted).gameObject;
            alphaNote.GetComponent<TextMeshProUGUI>().text = Text("royal_palace.alpha_balance");

            RectTransform upgrade = Card(content, "UpgradeState", Text("royal_palace.upgrade"));
            status = FlowLabel(upgrade, "Status", 17, Paper);
            RectTransform track = Plate(upgrade, "ProgressTrack", new Color(.04f, .03f, .02f));
            track.gameObject.AddComponent<LayoutElement>().preferredHeight = 8;
            progressFill = Plate(track, "ProgressFill", new Color(.35f, .75f, 1f));
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = new Vector2(0, 1);
            progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;

            RectTransform progression = Card(content, "Progression", Text("royal_palace.next_level"));
            progressionCard = progression.gameObject;
            nextLevel = FlowLabel(progression, "NextLevel", 23, Gold);
            description = FlowLabel(progression, "Description", 15, Paper);
            requirements = FlowLabel(progression, "Requirements", 16, Paper);
            cost = FlowLabel(progression, "Cost", 16, Paper);
            duration = FlowLabel(progression, "Duration", 16, Paper);
            unlocks = FlowLabel(progression, "Unlocks", 16, Paper);
            blocked = FlowLabel(progression, "Blocked", 16, new Color(1f, .65f, .45f));
            focusButton = ButtonAt(progression, "RequiredBuilding", Text("royal_palace.goto_building"), () =>
            {
                if (Presenter.TryFocusRoyalPalaceBlockingBuildingForExternalHost()) onClose?.Invoke();
            });
            focusButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;

            RectTransform footer = Plate(rootSafe, "Actions", new Color(.085f, .060f, .036f));
            Place(footer, new Rect(0, safe.height - footerHeight, w, footerHeight));
            float buttonWidth = narrow ? w - 32 : Mathf.Min(360, (w - 48) * .5f);
            float left = narrow ? 16 : (w - buttonWidth * 2 - 16) * .5f;
            Button colony = ButtonAt(footer, "Colony", Text("royal_palace.colony_view"), () =>
            {
                onClose?.Invoke();
                Presenter.OpenColonyOverviewForExternalHost();
            });
            Place((RectTransform)colony.transform, new Rect(left, 12, buttonWidth, 48));
            actionButton = ButtonAt(footer, "Upgrade", string.Empty, () =>
            {
                // Recheck at click time; the controller owns busy/ambiguous command handling.
                if (Presenter.RoyalPalaceUpgradeActionEnabledForExternalHost())
                    Presenter.RunRoyalPalaceUpgradeActionForExternalHost();
                RefreshLabels();
            }, true);
            Place((RectTransform)actionButton.transform, new Rect(narrow ? 16 : left + buttonWidth + 16,
                narrow ? 68 : 12, buttonWidth, 48));
            actionCaption = actionButton.GetComponentInChildren<TextMeshProUGUI>();
            RefreshLabels();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = retainedScroll;
        }

        // Keep the hierarchy, scroll position and raycast targets alive while refreshing.
        private void RefreshLabels()
        {
            if (level == null) return;
            int? currentLevel = Presenter.PremiumRoyalPalaceLevelForExternalHost();
            Set(level, currentLevel.HasValue ? Text("royal_palace.level_word") + " " +
                currentLevel.Value.ToString(CultureInfo.InvariantCulture) : Text("building_upgrade.level.unavailable"));
            Set(authority, currentLevel.HasValue ? Presenter.PremiumRoyalPalaceAuthorityForExternalHost() : string.Empty);
            Set(status, Presenter.PremiumRoyalPalaceStatusForExternalHost());
            Set(actionCaption, Presenter.RoyalPalaceUpgradeActionLabelForExternalHost());
            actionButton.interactable = Presenter.RoyalPalaceUpgradeActionEnabledForExternalHost();
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(Presenter.PremiumRoyalPalaceProgressForExternalHost()), 1);
            bool available = Presenter.RoyalPalaceProgressionAvailableForExternalHost();
            progressionCard.SetActive(available);
            alphaNote.SetActive(Presenter.RoyalPalaceProgressionIsAlphaForExternalHost());
            if (!available) return;
            bool maximum = Presenter.RoyalPalaceAtMaxLevelForExternalHost();
            int target = Presenter.RoyalPalaceNextLevelForExternalHost();
            Set(nextLevel, maximum ? Text("royal_palace.max_level")
                : Text("royal_palace.level_word") + " " + target.ToString(CultureInfo.InvariantCulture));
            SetOptional(description, maximum ? string.Empty : Presenter.RoyalPalaceNextLevelDescriptionForExternalHost());
            StringBuilder lines = new StringBuilder();
            if (!maximum)
            {
                lines.Append(Text("royal_palace.requirements")).Append('\n');
                int count = Presenter.RoyalPalaceRequirementCountForExternalHost();
                if (count == 0) lines.Append(Text("royal_palace.requirements.none"));
                for (int i = 0; i < count; i++)
                {
                    string label = Presenter.RoyalPalaceRequirementLabelForExternalHost(i, out bool satisfied);
                    lines.Append(satisfied ? "<color=#B3E6A6>✓  " : "<color=#FFAC73>—  ")
                        .Append(Escape(label)).Append("</color>\n");
                }
            }
            SetOptional(requirements, lines.ToString().TrimEnd());
            SetOptional(cost, maximum ? string.Empty : WithCaption("royal_palace.cost", Presenter.RoyalPalaceNextCostTextForExternalHost()));
            SetOptional(duration, maximum ? string.Empty : WithCaption("royal_palace.duration", Presenter.RoyalPalaceNextDurationTextForExternalHost()));
            lines.Clear();
            int unlockCount = maximum ? 0 : Presenter.RoyalPalaceNextUnlockCountForExternalHost();
            if (unlockCount > 0)
                lines.AppendFormat(CultureInfo.InvariantCulture, Text("royal_palace.unlocks_at"), target).Append('\n');
            for (int i = 0; i < unlockCount; i++)
            {
                string label = Presenter.RoyalPalaceNextUnlockLabelForExternalHost(i, out bool enforced);
                lines.Append("•  ").Append(Escape(label));
                if (!enforced) lines.Append(" — ").Append(Text("royal_palace.unlock.upcoming"));
                lines.Append('\n');
            }
            SetOptional(unlocks, lines.ToString().TrimEnd());
            SetOptional(blocked, maximum ? string.Empty : Presenter.RoyalPalaceBlockedReasonForExternalHost());
            focusButton.gameObject.SetActive(!maximum &&
                !string.IsNullOrEmpty(Presenter.RoyalPalaceBlockingBuildingTypeForExternalHost()));
        }

        private static string Text(string key) => BeeLocalization.Text(key);
        private static string Escape(string text) => (text ?? string.Empty).Replace("<", "‹").Replace(">", "›");
        private static string WithCaption(string key, string value) => string.IsNullOrEmpty(value) ? string.Empty : Text(key) + " : " + value;
        private static void Set(TextMeshProUGUI label, string value)
        {
            if (label.text != value) label.text = value;
        }
        private static void SetOptional(TextMeshProUGUI label, string value)
        {
            Set(label, value);
            bool visible = !string.IsNullOrEmpty(value);
            if (label.gameObject.activeSelf != visible) label.gameObject.SetActive(visible);
        }
        private static RectTransform Node(Transform parent, string name)
        {
            RectTransform node = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            node.SetParent(parent, false);
            node.anchorMin = node.anchorMax = node.pivot = new Vector2(0, 1);
            return node;
        }
        private static void Place(RectTransform node, Rect rect)
        {
            node.anchoredPosition = new Vector2(rect.x, -rect.y);
            node.sizeDelta = rect.size;
        }
        private static RectTransform Plate(Transform parent, string name, Color color)
        {
            RectTransform node = Node(parent, name);
            Image plate = node.gameObject.AddComponent<Image>();
            plate.color = color;
            plate.raycastTarget = true;
            return node;
        }
        private static RectTransform Card(Transform parent, string name, string title)
        {
            RectTransform card = Plate(parent, name, new Color(.15f, .095f, .045f));
            Grain(card, "panel-grain", .12f);
            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 20, 22);
            layout.spacing = 12;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            TextMeshProUGUI heading = FlowLabel(card, "Heading", 15, Gold);
            heading.fontStyle = FontStyles.Bold;
            heading.text = title.ToUpperInvariant();
            return card;
        }
        private static void Grain(RectTransform parent, string material, float opacity)
        {
            Texture texture = Presenter.BuildingWindowMaterial(material);
            if (texture == null) return;
            RectTransform node = Node(parent, "Material");
            node.anchorMin = Vector2.zero;
            node.anchorMax = Vector2.one;
            node.offsetMin = node.offsetMax = Vector2.zero;
            node.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            RawImage image = node.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = new Color(1, 1, 1, opacity);
            image.raycastTarget = false;
        }
        private static TextMeshProUGUI Label(Transform parent, string name, float size, Color color, bool bold = false)
        {
            TextMeshProUGUI label = Node(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.color = color;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }
        private static TextMeshProUGUI FlowLabel(Transform parent, string name, float size, Color color)
            => Label(parent, name, size, color);

        private static Button ButtonAt(Transform parent, string name, string text, Action action, bool primary = false)
        {
            RectTransform node = Plate(parent, name, primary ? Gold : new Color(.24f, .16f, .075f));
            Button button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = node.GetComponent<Image>();
            button.onClick.AddListener(() => action?.Invoke());
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(.45f, .42f, .36f);
            button.colors = colors;
            TextMeshProUGUI label = Label(node, "Caption", 16, primary ? new Color(.055f, .037f, .024f) : Paper, true);
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            RectTransform rect = (RectTransform)label.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(10, 4);
            rect.offsetMax = new Vector2(-10, -4);
            label.enableAutoSizing = true;
            label.fontSizeMin = 12;
            label.fontSizeMax = 16;
            return button;
        }
    }
}
