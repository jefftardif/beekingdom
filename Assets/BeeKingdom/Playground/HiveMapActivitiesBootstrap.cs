using System;
using System.Collections.Generic;
using System.Globalization;
using BeeKingdom.Audio;
using BeeKingdom.Core.Integration;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeeKingdom.Playground
{
    // The retained canvas is presentation only. Controllers remain the sole authority for claims.
    public sealed class HiveMapActivitiesBootstrap : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, .77f, .36f);
        private static readonly Color Ink = new Color(.055f, .037f, .024f);
        private static readonly Color Paper = new Color(1f, .95f, .82f);
        private static readonly Color Muted = new Color(.80f, .73f, .61f);
        private static readonly Color Green = new Color(.56f, .87f, .61f);
        private static HiveMapActivitiesBootstrap instance;
        private Canvas canvas;
        private RectTransform safeRoot;
        private RectTransform content;
        private ScrollRect scroll;
        private HiveDailyRoundScreenModel dailySeen;
        private HiveMilestoneEventScreenModel eventSeen;
        private QuestChainScreenModel questsSeen;
        private Rect lastSafe;
        private Vector2 lastSize;
        private string lastLocale;
        private int lastBusy;
        private float width;
        private float height;
        private TMP_FontAsset displayFont;

        public static bool ModalOpenForExternalHost { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            ModalOpenForExternalHost = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart() => InitializeForScene(SceneManager.GetActiveScene());

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded ||
                !scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal) ||
                FindFirstObjectByType<HiveMapActivitiesBootstrap>() != null) return;
            GameObject root = new GameObject("HiveMap Activities Runtime");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapActivitiesBootstrap>();
        }

        private void Start()
        {
            instance = this;
            LivingHiveActivitiesBridge.SetHandlers(() => ModalOpenForExternalHost, OpenModal);
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            ModalOpenForExternalHost = false;
            instance = null;
            LivingHiveActivitiesBridge.SetHandlers(null, null);
        }

        private static void OpenModal()
        {
            if (instance == null) return;
            ModalOpenForExternalHost = true;
            MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession();
            RefreshAll();
            instance.Render(true);
        }

        private static void RefreshAll()
        {
            MobileAccountSessionRuntimeBootstrap.DailyRoundControllerForHiveMap.Refresh();
            MobileAccountSessionRuntimeBootstrap.MilestoneEventControllerForHiveMap.Refresh();
            MobileAccountSessionRuntimeBootstrap.QuestChainControllerForHiveMap.Refresh();
        }

        private void Close()
        {
            ModalOpenForExternalHost = false;
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!ModalOpenForExternalHost) return;
            Render(false);
        }

        private void Render(bool force)
        {
            var daily = MobileAccountSessionRuntimeBootstrap.DailyRoundControllerForHiveMap;
            var milestone = MobileAccountSessionRuntimeBootstrap.MilestoneEventControllerForHiveMap;
            var quests = MobileAccountSessionRuntimeBootstrap.QuestChainControllerForHiveMap;
            int busy = (daily.IsBusy ? 1 : 0) | (milestone.IsBusy ? 2 : 0) | (quests.IsBusy ? 4 : 0);
            bool layoutChanged = lastSize != new Vector2(Screen.width, Screen.height) || lastSafe != Screen.safeArea;
            bool changed = force || layoutChanged || lastLocale != BeeLocalization.CurrentLocale ||
                dailySeen != daily.Model || eventSeen != milestone.Model || questsSeen != quests.Model || lastBusy != busy;
            if (!changed) return;
            float position = scroll != null && !force ? scroll.verticalNormalizedPosition : 1f;
            dailySeen = daily.Model;
            eventSeen = milestone.Model;
            questsSeen = quests.Model;
            lastBusy = busy;
            lastLocale = BeeLocalization.CurrentLocale;
            lastSize = new Vector2(Screen.width, Screen.height);
            lastSafe = Screen.safeArea;
            RenderSnapshot(daily.Model, milestone.Model, quests.Model, busy, position, Screen.safeArea);
        }

        private void RenderSnapshot(HiveDailyRoundScreenModel daily, HiveMilestoneEventScreenModel milestone,
            QuestChainScreenModel quests, int busy, float position, Rect screenSafeArea)
        {
            BuildShell(screenSafeArea);
            bool columns = width >= 740f;
            float gap = 20f;
            float cardWidth = columns ? (width - 3f * gap) / 2f : width - 2f * gap;
            float dailyHeight = DrawDaily(content, gap, 8, cardWidth, daily, (busy & 1) != 0);
            float eventHeight = DrawEvent(content, columns ? cardWidth + 2f * gap : gap,
                columns ? 8 : dailyHeight + 28f, cardWidth, milestone, (busy & 2) != 0);
            float y = columns ? Mathf.Max(dailyHeight, eventHeight) + 28f : dailyHeight + eventHeight + 48f;
            float questHeight = DrawQuests(content, gap, y, width - 2f * gap, quests, (busy & 4) != 0);
            content.sizeDelta = new Vector2(0f, y + questHeight + 24f);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = position;
            // Keep the thumb in sync before the first ScrollRect LateUpdate after a rebuild.
            scroll.verticalScrollbar.size = Mathf.Clamp01(scroll.viewport.rect.height / content.rect.height);
            scroll.verticalScrollbar.SetValueWithoutNotify(position);
        }

        private void BuildShell(Rect screenSafeArea)
        {
            if (canvas == null)
            {
                RectTransform root = Node(transform, "ActivitiesCanvas", new Rect());
                canvas = root.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 240;
                root.gameObject.AddComponent<GraphicRaycaster>();
                root.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                displayFont = Resources.Load<TMP_FontAsset>("Cinzel-Regular SDF");
            }
            canvas.gameObject.SetActive(true);
            // Disable before deferred destruction so an old claim button cannot receive another click.
            foreach (Transform child in canvas.transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            float scale = CanvasScale(new Vector2(Screen.width, Screen.height));
            canvas.GetComponent<CanvasScaler>().scaleFactor = scale;
            Rect safe = SafeCanvasRect(new Vector2(Screen.width, Screen.height), screenSafeArea, scale);
            width = safe.width;
            height = safe.height;
            Plate(canvas.transform, "Backdrop", new Rect(0, 0, Screen.width / scale, Screen.height / scale), Ink, "cave-background");
            safeRoot = Node(canvas.transform, "SafeArea", safe);
            bool compactHeader = width > height && width >= 600;
            float headerHeight = compactHeader ? 88f : 132f;
            RectTransform header = Plate(safeRoot, "Header", new Rect(0, 0, width, headerHeight), Ink, "panel-fill");
            Texture2D banner = Resources.Load<Texture2D>("PremiumBeeReference/BuildingBanners/alliance_future_hall");
            if (banner != null) Art(header, new Rect(width * .40f, 0, width * .60f, headerHeight), banner, new Color(1, 1, 1, .32f));
            ActionButton(header, new Rect(16, 12, 72, 48), L("back"), true, Close, false);
            ActionButton(header, new Rect(width - 126, 12, 110, 48), L("refresh"), true, RefreshAll, false);
            Label(header, compactHeader ? new Rect(108, 12, width - 250, 38) : new Rect(18, headerHeight - 65, width - 36, 38), L("title"), 27, Gold, true, true);
            Label(header, compactHeader ? new Rect(110, 52, width - 250, 23) : new Rect(20, headerHeight - 28, width - 40, 23), L("subtitle"), 13, Paper);
            Plate(header, "GoldRule", new Rect(0, headerHeight - 1, width, 1), Gold, "etched-line");
            RectTransform viewport = Node(safeRoot, "Viewport", new Rect(0, headerHeight + 12, width, height - headerHeight - 12));
            viewport.gameObject.AddComponent<RectMask2D>();
            Image target = viewport.gameObject.AddComponent<Image>();
            target.color = Color.clear;
            content = Node(viewport, "Activities", new Rect(0, 0, width, 0));
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.sizeDelta = Vector2.zero;
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            scroll.decelerationRate = .10f;
            RectTransform rail = Plate(viewport, "ScrollTrack", new Rect(width - 10, 8, 6, height - headerHeight - 28), Ink, "panel-fill");
            RectTransform handle = Plate(rail, "ScrollHandle", new Rect(), Muted, "panel-fill");
            var scrollbar = rail.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<RawImage>();
            scrollbar.targetGraphic.raycastTarget = true;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private float DrawDaily(Transform parent, float x, float y, float w, HiveDailyRoundScreenModel model, bool busy)
        {
            bool unavailable = model.State == HiveDailyRoundScreenState.NotConfigured;
            string state = busy || model.State == HiveDailyRoundScreenState.Loading ? "loading" : model.IsClaimed ? "claimed" : model.CanClaim ? "claimable" :
                model.State == HiveDailyRoundScreenState.Error ? "error" : model.CanRetryClaim ? "verify" :
                model.IsReadOnly ? "offline" : unavailable ? "unavailable" : model.CompletedCount == 3 ? "done" : "progress";
            RectTransform card = Card(parent, x, y, w, L("daily"), "quests", state);
            float cursor = 114f;
            if (unavailable) cursor = Message(card, cursor, w, L("connect"));
            else
            {
                cursor = Progress(card, cursor, w, model.CompletedCount, 3);
                cursor = Objective(card, cursor, w, L("harvest"), L("harvest_desc"), model.CollectionReceived, "honey");
                cursor = Objective(card, cursor, w, L("develop"), L("develop_desc"), model.OperationLaunched, "build");
                cursor = Objective(card, cursor, w, L("expedition"), L("expedition_desc"), model.SnapshotRead, "map");
                cursor = Rewards(card, cursor, w, new Dictionary<string, long> { { "honey", model.HoneyReward }, { "pollen", model.PollenReward } });
                if (model.IsReadOnly) cursor = Message(card, cursor, w, L("offline_desc"));
                if (model.State == HiveDailyRoundScreenState.Error) cursor = Message(card, cursor, w, L("error_desc"));
                if (model.CanRetryClaim) cursor = Message(card, cursor, w, L("verify_desc"));
            }
            ActionButton(card, new Rect(18, cursor, w - 36, 48), L(model.CanRetryClaim ? "verify" : model.IsClaimed ? "claimed" : "claim"),
                !busy && (model.CanRetryClaim || model.CanClaim), () =>
                {
                    var controller = MobileAccountSessionRuntimeBootstrap.DailyRoundControllerForHiveMap;
                    if (controller.IsBusy) return;
                    if (controller.Model.CanRetryClaim) controller.RetryClaim();
                    else if (controller.Model.CanClaim) controller.Claim();
                }, true);
            return FinishCard(card, cursor + 66f);
        }

        private float DrawEvent(Transform parent, float x, float y, float w, HiveMilestoneEventScreenModel model, bool busy)
        {
            bool unavailable = model.State == HiveMilestoneEventScreenState.NotConfigured;
            string state = busy ? "loading" : model.Claimed ? "claimed" : model.WindowExpired ? "ended" :
                model.State == HiveMilestoneEventScreenState.Error ? "error" : model.CanClaim ? "claimable" : unavailable ? "unavailable" : "progress";
            RectTransform card = Card(parent, x, y, w, L("event"), "alliance", state);
            float cursor = 114f;
            if (unavailable) cursor = Message(card, cursor, w, L("event_empty"));
            else
            {
                if (model.WindowEndsAtUtc != default)
                    cursor = Message(card, cursor, w, string.Format(CultureInfo.CurrentCulture, L("ends"),
                        model.WindowEndsAtUtc.ToLocalTime().ToString("g", CultureInfo.GetCultureInfo(BeeLocalization.CurrentLocale))));
                cursor = Progress(card, cursor, w, model.CompletedCount, model.RequiredObjectiveCount);
                if (model.Objectives == null || model.Objectives.Count == 0) cursor = Message(card, cursor, w, L("empty"));
                else foreach (RemoteHiveMilestoneObjective objective in model.Objectives)
                    if (objective != null) cursor = Objective(card, cursor, w, ObjectiveTitle(objective.ObjectiveKey), "", objective.Done, ObjectiveIcon(objective.ObjectiveKey));
                cursor = Rewards(card, cursor, w, model.Reward);
                if (model.State == HiveMilestoneEventScreenState.Error) cursor = Message(card, cursor, w, L("error_desc"));
            }
            ActionButton(card, new Rect(18, cursor, w - 36, 48), L(model.Claimed ? "claimed" : "claim"), model.CanClaim && !busy, () =>
            {
                var controller = MobileAccountSessionRuntimeBootstrap.MilestoneEventControllerForHiveMap;
                if (controller.Model.CanClaim && !controller.IsBusy) controller.Claim();
            }, true);
            return FinishCard(card, cursor + 66f);
        }

        private float DrawQuests(Transform parent, float x, float y, float w, QuestChainScreenModel model, bool busy)
        {
            int count = model.Objectives?.Count ?? 0;
            string state = busy ? "loading" : model.State == QuestChainScreenState.NotConfigured ? "unavailable" :
                model.State == QuestChainScreenState.Error ? "error" : model.AnyClaimable ? "claimable" :
                count > 0 && model.CompletedCount >= count ? "done" : "progress";
            RectTransform card = Card(parent, x, y, w, L("kingdom"), "queen", state);
            float cursor = 114f;
            if (count == 0) cursor = Message(card, cursor, w, L(model.State == QuestChainScreenState.NotConfigured ? "connect" : "empty"));
            else
            {
                cursor = Progress(card, cursor, w, model.CompletedCount, count);
                bool split = w >= 740;
                float itemWidth = split ? (w - 54) / 2 : w - 36;
                float rowHeight = 0;
                int index = 0;
                foreach (RemoteQuestChainObjective objective in model.Objectives)
                {
                    if (objective == null) continue;
                    RectTransform item = Node(card, "KingdomObjective", new Rect(18 + (split ? index % 2 : 0) * (itemWidth + 18), cursor, itemWidth, 0));
                    float itemY = Objective(item, 0, itemWidth, ObjectiveTitle(objective.ObjectiveKey), "", objective.Done, ObjectiveIcon(objective.ObjectiveKey));
                    itemY = Rewards(item, itemY, itemWidth, new Dictionary<string, long> { { objective.RewardResourceKey ?? "", objective.RewardAmount } });
                    string key = objective.ObjectiveKey;
                    ActionButton(item, new Rect(18, itemY, itemWidth - 36, 48), L(objective.Claimed ? "claimed" : objective.CanClaim ? "claim" : "progress"),
                        objective.CanClaim && !busy, () => ClaimQuest(key), true);
                    itemY += 68;
                    item.sizeDelta = new Vector2(itemWidth, itemY);
                    rowHeight = Mathf.Max(rowHeight, itemY);
                    if (!split || index % 2 == 1) { cursor += rowHeight; rowHeight = 0; }
                    index++;
                }
                cursor += rowHeight;
            }
            if (model.State == QuestChainScreenState.Error) cursor = Message(card, cursor, w, L("error_desc"));
            return FinishCard(card, cursor + 12);
        }

        private static void ClaimQuest(string key)
        {
            var controller = MobileAccountSessionRuntimeBootstrap.QuestChainControllerForHiveMap;
            if (controller.IsBusy || controller.Model.Objectives == null) return;
            foreach (var objective in controller.Model.Objectives)
                if (objective != null && objective.ObjectiveKey == key && objective.CanClaim) { controller.Claim(key); return; }
        }

        private RectTransform Card(Transform parent, float x, float y, float w, string title, string icon, string state)
        {
            RectTransform card = Plate(parent, "ActivityCard", new Rect(x, y, w, 100), new Color(.19f, .125f, .068f), "panel-fill");
            Outline frame = card.gameObject.AddComponent<Outline>();
            frame.effectColor = new Color(.56f, .38f, .15f, .85f);
            frame.effectDistance = new Vector2(1, -1);
            Icon(card, new Rect(18, 19, 54, 54), icon);
            Label(card, new Rect(84, 12, w - 100, 60), title, w < 340 ? 18 : 21, Gold, true, true);
            Color color = state == "claimable" ? Gold : state == "done" || state == "claimed" ? Green : Muted;
            Plate(card, "State", new Rect(84, 73, w - 102, 27), new Color(.055f, .04f, .025f), "panel-fill");
            Label(card, new Rect(94, 75, w - 122, 23), L(state), 13, color, true);
            return card;
        }

        private static float FinishCard(RectTransform card, float height)
        {
            card.sizeDelta = new Vector2(card.sizeDelta.x, height);
            return height;
        }

        private float Objective(Transform parent, float y, float w, string title, string description, bool done, string icon)
        {
            float textWidth = w - 116;
            float titleHeight = Mathf.Max(24, Measure(title, 16, textWidth, true));
            float descriptionHeight = string.IsNullOrEmpty(description) ? 0 : Measure(description, 13, w - 84);
            float h = Mathf.Max(62f, titleHeight + descriptionHeight + 25f);
            Icon(parent, new Rect(18, y + 6, 38, 38), icon);
            Label(parent, new Rect(66, y + 4, textWidth, titleHeight), title, 16, done ? Green : Paper, true);
            Label(parent, new Rect(w - 48, y + 4, 32, 25), done ? "1/1" : "0/1", 13, done ? Green : Muted);
            if (descriptionHeight > 0) Label(parent, new Rect(66, y + titleHeight + 8, w - 84, descriptionHeight), description, 13, Muted);
            Plate(parent, "Divider", new Rect(18, y + h - 6, w - 36, 1), new Color(.4f, .29f, .14f), "etched-line");
            return y + h;
        }

        private float Progress(Transform parent, float y, float w, int completed, int total)
        {
            Label(parent, new Rect(18, y, w - 36, 24), string.Format(CultureInfo.CurrentCulture, L("progress_count"), completed, total), 14, Paper, true);
            Plate(parent, "ProgressTrack", new Rect(18, y + 30, w - 36, 7), Ink, "panel-fill");
            float fraction = total > 0 ? Mathf.Clamp01((float)completed / total) : 0;
            if (fraction > 0) Plate(parent, "ProgressFill", new Rect(18, y + 30, (w - 36) * fraction, 7), Gold, "amber-veil");
            return y + 53;
        }

        private float Rewards(Transform parent, float y, float w, IReadOnlyDictionary<string, long> rewards)
        {
            Label(parent, new Rect(18, y + 5, w - 36, 24), L("rewards"), 13, Gold, true);
            y += 34;
            if (rewards == null || rewards.Count == 0) return Message(parent, y, w, L("no_rewards"));
            int columns = Mathf.Max(1, Mathf.FloorToInt((w - 36) / 145));
            float tileWidth = (w - 36 - (columns - 1) * 8) / columns;
            int index = 0;
            foreach (var reward in rewards)
            {
                float left = 18 + (index % columns) * (tileWidth + 8);
                float top = y + (index / columns) * 68;
                RectTransform tile = Plate(parent, "Reward", new Rect(left, top, tileWidth, 60), new Color(.095f, .063f, .033f), "panel-fill");
                Icon(tile, new Rect(8, 10, 38, 38), ResourceIcon(reward.Key));
                Label(tile, new Rect(52, 6, tileWidth - 58, 25), reward.Value.ToString("N0", CultureInfo.CurrentCulture), 18, Paper, true);
                Label(tile, new Rect(52, 33, tileWidth - 58, 21), ResourceTitle(reward.Key), 12, Muted);
                index++;
            }
            return y + Mathf.CeilToInt((float)index / columns) * 68 + 8;
        }

        private float Message(Transform parent, float y, float w, string message)
        {
            float h = Measure(message, 14, w - 36);
            Label(parent, new Rect(18, y, w - 36, h), message, 14, Muted);
            return y + h + 16;
        }

        private static string ObjectiveTitle(string key)
        {
            switch (key)
            {
                case "building_upgrade": return L("milestone_build");
                case "troop_recruit": return L("milestone_troops");
                case "world_resource": return L("milestone_resource");
                case "strategic_path": return L("milestone_path");
                case "vip_tier": return L("milestone_vip");
                case "q1_building_upgrade": return L("quest_build");
                case "q2_troop_recruit": return L("quest_troops");
                case "q3_research_complete": return L("quest_research");
                case "q4_world_map_visit": return L("quest_world");
                case "q5_world_resource_collect": return L("quest_resource");
                default: return L("objective");
            }
        }

        private static string ObjectiveIcon(string key)
        {
            switch (key)
            {
                case "building_upgrade": case "q1_building_upgrade": return "build";
                case "troop_recruit": case "q2_troop_recruit": return "bees";
                case "world_resource": case "q5_world_resource_collect": return "honey";
                case "vip_tier": return "queen";
                case "q3_research_complete": return "book";
                default: return "quests";
            }
        }

        private static string ResourceIcon(string key)
        {
            switch (key)
            {
                case "honey": case "pollen": case "wax": case "royal_jelly": case "propolis": return key;
                default: return "quests";
            }
        }

        private static string ResourceTitle(string key)
        {
            switch (key)
            {
                case "honey": case "pollen": case "wax": case "royal_jelly": case "propolis": return L(key);
                default: return L("reward");
            }
        }

        private static string L(string key) => BeeLocalization.Text("activities.premium." + key, "");

        internal static Rect SafeCanvasRect(Vector2 screen, Rect safe, float scale) =>
            new Rect(safe.x / scale, (screen.y - safe.yMax) / scale, safe.width / scale, safe.height / scale);

        // Landscape keeps room for both cards even on a 720p desktop; portrait scales for touch.
        internal static float CanvasScale(Vector2 screen) => screen.x > screen.y
            ? Mathf.Clamp(screen.y / 900f, 1f, 2f)
            : Mathf.Clamp(screen.x / 390f, 1f, 3f);

        private static RectTransform Node(Transform parent, string name, Rect rect)
        {
            var node = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            node.SetParent(parent, false);
            node.anchorMin = node.anchorMax = new Vector2(0, 1);
            node.pivot = new Vector2(0, 1);
            node.anchoredPosition = new Vector2(rect.x, -rect.y);
            node.sizeDelta = rect.size;
            return node;
        }

        private static RectTransform Plate(Transform parent, string name, Rect rect, Color color, string texture)
        {
            RectTransform node = Node(parent, name, rect);
            RawImage image = node.gameObject.AddComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = color;
            image.raycastTarget = name == "Backdrop";
            RawImage grain = Node(node, "Material", rect).gameObject.AddComponent<RawImage>();
            grain.rectTransform.anchorMin = Vector2.zero;
            grain.rectTransform.anchorMax = Vector2.one;
            grain.rectTransform.offsetMin = grain.rectTransform.offsetMax = Vector2.zero;
            grain.texture = HiveViewProductUiPresenter.PremiumTextureForActivities(texture == "panel-fill" ? "panel-grain" : texture);
            grain.color = new Color(1, 1, 1, .16f);
            grain.raycastTarget = false;
            return node;
        }

        private static void Art(Transform parent, Rect rect, Texture texture, Color color)
        {
            RawImage image = Node(parent, "Artwork", rect).gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
        }

        private static void Icon(Transform parent, Rect rect, string key)
        {
            Art(parent, rect, HiveViewProductUiPresenter.PremiumTextureForActivities("icon-socket"), Color.white);
            Art(parent, new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6), HiveViewProductUiPresenter.PremiumIconForActivities(key), Color.white);
        }

        private TextMeshProUGUI Label(Transform parent, Rect rect, string text, float size, Color color, bool bold = false, bool display = false)
        {
            var label = Node(parent, "Label", rect).gameObject.AddComponent<TextMeshProUGUI>();
            if (display && displayFont != null) label.font = displayFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.richText = false;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private float Measure(string value, float fontSize, float w, bool bold = false)
        {
            // A single temporary measurement object per rebuild avoids language-specific line estimates.
            if (measureLabel == null)
            {
                measureLabel = Node(transform, "TextMeasurement", new Rect()).gameObject.AddComponent<TextMeshProUGUI>();
                measureLabel.gameObject.SetActive(false);
            }
            measureLabel.fontSize = fontSize;
            measureLabel.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            measureLabel.richText = false;
            return Mathf.Ceil(measureLabel.GetPreferredValues(value, w, float.PositiveInfinity).y) + 3;
        }

        private TextMeshProUGUI measureLabel;

        private void ActionButton(Transform parent, Rect rect, string caption, bool enabled, Action action, bool primary)
        {
            Color fill = primary ? new Color(.80f, .49f, .13f) : new Color(.22f, .15f, .07f);
            RectTransform node = Plate(parent, "Action", rect, enabled ? fill : new Color(.18f, .15f, .11f), "panel-outer-grain");
            RawImage graphic = node.GetComponent<RawImage>();
            graphic.raycastTarget = true;
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.interactable = enabled;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
            colors.pressedColor = new Color(.78f, .78f, .78f);
            colors.disabledColor = Color.white;
            button.colors = colors;
            Plate(node, "ButtonLight", new Rect(2, 1, rect.width - 4, 1), enabled && primary ? Gold : new Color(.43f, .32f, .16f), "etched-line");
            Plate(node, "ButtonBase", new Rect(2, rect.height - 3, rect.width - 4, 2), new Color(.11f, .065f, .02f), "etched-line");
            var label = Label(node, new Rect(6, 0, rect.width - 12, rect.height), caption, 15, enabled && primary ? Ink : Paper, true);
            label.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() => { AudioManager.Instance?.PlayUIClick(); action(); });
        }
    }
}
