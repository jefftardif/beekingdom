using System;
using System.Collections.Generic;
using BeeKingdom.Audio;
using BeeKingdom.Networking;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Shared Activities-v4 fullscreen building presentation.
    /// This class owns display only. Gameplay controllers remain authoritative;
    /// no unsupported building gains a fabricated gameplay action.
    /// </summary>
    public sealed class HiveMapPremiumBuildingWindow : MonoBehaviour
    {
        // Founder-approved BeeKingdom royal healing emblem. The other building
        // icons and all existing functionality retain their original mapping.
        public const string InfirmaryPremiumIconResourcePath =
            "PremiumBeeIcons/infirmary_royal_healing";

        private static Sprite nurseryProgressSprite;
        private static readonly Color Gold = new Color(1f, .77f, .36f);
        private static readonly Color Ink = new Color(.055f, .037f, .024f);
        private static readonly Color Paper = new Color(1f, .95f, .82f);
        private static readonly Color Muted = new Color(.80f, .73f, .61f);
        private static readonly Color Card = new Color(.15f, .095f, .045f);
        private static readonly Color Panel = new Color(.085f, .060f, .036f);
        private Canvas canvas;
        private RectTransform safeRoot;
        private TMP_FontAsset displayFont;
        private ScrollRect scroll;
        private Vector2 lastSize;
        private Rect lastSafe;
        private string lastLanguage;
        private BuildingDefinition selected;
        private string titleFr;
        private string titleEn;
        private string detailFr;
        private string detailEn;
        private Action closeAction;
        private Action upgradeAction;
        private bool windowOpen;
        private bool suppressed;

        // Infirmerie v1 reads the SAME server-backed recovering queue used by combat
        // patrol. No local troop credit, no artificial healing or invented cost.
        private bool infirmaryMode;
        private bool nurseryMode;
        private IHiveBroodVitalityPanelController nurseryController;
        private float nextNurseryRefreshAt;
        private float nextNurseryLabelsAt;
        private TextMeshProUGUI nurserySummaryLabel;
        private TextMeshProUGUI nurseryStatusLabel;
        private TextMeshProUGUI nurseryNutritionLabel;
        private TextMeshProUGUI nurseryStabilityLabel;
        private TextMeshProUGUI nurseryOperationLabel;
        private Image nurseryNutritionFill;
        private Image nurseryStabilityFill;
        private Image nurseryOperationFill;
        private Button nurseryFeedButton;
        private Button nurseryStabilizeButton;
        private Button nurseryCompleteButton;
        private Button nurseryRetryButton;
        private ICombatPatrolPanelController infirmaryController;
        private float nextInfirmaryRefreshAt;
        private float nextInfirmaryLabelAt;
        private TextMeshProUGUI infirmaryTotalLabel;
        private TextMeshProUGUI infirmaryStatusLabel;
        private readonly Dictionary<string, TextMeshProUGUI> infirmaryCountLabels =
            new Dictionary<string, TextMeshProUGUI>(StringComparer.Ordinal);
        private readonly Dictionary<string, TextMeshProUGUI> infirmaryTimerLabels =
            new Dictionary<string, TextMeshProUGUI>(StringComparer.Ordinal);

        public bool IsOpen => windowOpen;
        public bool InfirmaryActiveForProof => windowOpen && infirmaryMode;
        public bool NurseryActiveForProof => windowOpen && nurseryMode;

        // The Nursery's genuine server-backed feed/stabilize operations.
        public void BindNurseryController(IHiveBroodVitalityPanelController controller)
        {
            nurseryController = controller;
        }

        // Inject the existing shared patrol controller before showing the Infirmary.
        public void BindInfirmaryController(ICombatPatrolPanelController controller)
        {
            infirmaryController = controller;
        }

        public void Show(BuildingDefinition building, string titleFrench, string titleEnglish,
            string infoFrench, string infoEnglish, Action onClose, Action onUpgrade)
        {
            if (building == null) return;
            selected = building;
            titleFr = titleFrench ?? building.DisplayName;
            titleEn = titleEnglish ?? titleFrench ?? building.DisplayName;
            detailFr = infoFrench ?? string.Empty;
            detailEn = infoEnglish ?? detailFr;
            closeAction = onClose;
            upgradeAction = onUpgrade;
            infirmaryMode = string.Equals(building.BuildingType,
                BuildingTypes.Infirmary, StringComparison.Ordinal);
            nurseryMode = string.Equals(building.BuildingType,
                BuildingTypes.Nursery, StringComparison.Ordinal);
            if (!nurseryMode) nurseryController = null;
            if (!infirmaryMode) infirmaryController = null;
            windowOpen = true;
            suppressed = false;
            nextInfirmaryLabelAt = 0f;
            nextInfirmaryRefreshAt = Time.unscaledTime + 15f;
            nextNurseryLabelsAt = 0f;
            nextNurseryRefreshAt = Time.unscaledTime + 15f;
            Render();
            if (infirmaryMode) RefreshInfirmary();
            if (nurseryMode) RefreshNursery();

        }

        public void Hide()
        {
            windowOpen = false;
            infirmaryMode = false;
            nurseryMode = false;
            nurseryController = null;
            nurserySummaryLabel = null;
            nurseryStatusLabel = null;
            nurseryNutritionLabel = null;
            nurseryStabilityLabel = null;
            nurseryOperationLabel = null;
            nurseryNutritionFill = null;
            nurseryStabilityFill = null;
            nurseryOperationFill = null;
            nurseryFeedButton = null;
            nurseryStabilizeButton = null;
            nurseryCompleteButton = null;
            nurseryRetryButton = null;
            infirmaryController = null;
            infirmaryTotalLabel = null;
            infirmaryStatusLabel = null;
            infirmaryCountLabels.Clear();
            infirmaryTimerLabels.Clear();
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        // Activities and Army take precedence, as they did with the legacy IMGUI popup.
        public void SetSuppressed(bool hide)
        {
            suppressed = hide;
            if (canvas != null)
                canvas.gameObject.SetActive(windowOpen && !suppressed);
        }

        private void Update()
        {
            if (!windowOpen || suppressed) return;
            if (lastSize != new Vector2(Screen.width, Screen.height)
                || lastSafe != Screen.safeArea
                || !string.Equals(lastLanguage, BeeLocalization.CurrentLocale,
                    StringComparison.Ordinal))
                Render();
            if (infirmaryMode)
            {
                // Countdown labels update without rebuilding ScrollRect under a drag.
                float now = Time.unscaledTime;
                if (now >= nextInfirmaryLabelAt)
                {
                    nextInfirmaryLabelAt = now + .5f;
                    RefreshInfirmaryLabels();
                }
                if (now >= nextInfirmaryRefreshAt)
                {
                    nextInfirmaryRefreshAt = now + 15f;
                    RefreshInfirmary();
                }
            }
            if (nurseryMode)
            {
                float now = Time.unscaledTime;
                if (now >= nextNurseryLabelsAt)
                {
                    nextNurseryLabelsAt = now + .5f;
                    RefreshNurseryLabels();
                }
                if (now >= nextNurseryRefreshAt)
                {
                    nextNurseryRefreshAt = now + 15f;
                    RefreshNursery();
                }
            }
        }

        private void RefreshNursery()
        {
            if (!nurseryMode || !windowOpen) return;
            if (nurseryController != null && nurseryController.IsConfigured
                && !nurseryController.IsBusy
                && !(nurseryController.Model != null && nurseryController.Model.IsPending))
                nurseryController.Refresh();
            RefreshNurseryLabels();
        }

        private void NurseryStart(string type)
        {
            if (!nurseryMode || nurseryController == null ||
                !nurseryController.IsConfigured || nurseryController.IsBusy) return;
            HiveBroodVitalityScreenModel model = nurseryController.Model;
            if (model == null || model.IsPending || model.IsReadOnly
                || !model.CanStart(type)) return;
            nurseryController.Start(type);
            RefreshNurseryLabels();
        }

        private void NurseryComplete()
        {
            if (!nurseryMode || nurseryController == null ||
                !nurseryController.IsConfigured || nurseryController.IsBusy) return;
            HiveBroodVitalityScreenModel model = nurseryController.Model;
            if (model == null || !model.CanComplete(nurseryController.Elapsed)) return;
            nurseryController.Complete();
            RefreshNurseryLabels();
        }

        private void NurseryRetry()
        {
            if (!nurseryMode || nurseryController == null ||
                !nurseryController.IsConfigured || nurseryController.IsBusy) return;
            HiveBroodVitalityScreenModel model = nurseryController.Model;
            if (model == null || !model.IsPending || model.IsReadOnly) return;
            nurseryController.Retry();
            RefreshNurseryLabels();
        }

        private static string NurseryRemaining(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            long hours = (long)Math.Floor(remaining.TotalHours);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:00}:{1:00}:{2:00}", hours, remaining.Minutes, remaining.Seconds);
        }

        private void RefreshNurseryLabels()
        {
            if (!nurseryMode || !windowOpen || suppressed
                || nurserySummaryLabel == null || nurseryStatusLabel == null) return;
            bool fr = string.IsNullOrEmpty(BeeLocalization.CurrentLocale)
                || BeeLocalization.CurrentLocale.StartsWith("fr",
                    StringComparison.OrdinalIgnoreCase);
            bool configured = nurseryController != null && nurseryController.IsConfigured;
            HiveBroodVitalityScreenModel model = configured ? nurseryController.Model : null;
            bool loaded = model != null && model.Initialized;
            bool busy = configured && nurseryController.IsBusy;
            bool error = model != null && model.State == HiveBroodVitalityScreenState.Error;
            nurserySummaryLabel.text = !configured
                ? (fr ? "Connexion requise" : "Connection required")
                : error ? BeeLocalization.Text("nursery.status.sync_failed")
                : !loaded ? (fr ? "Chargement..." : "Loading...")
                : (fr ? "Vitalité de la colonie : " : "Colony vitality: ") + model.Quality + " / 100";
            if (nurseryNutritionLabel != null)
                nurseryNutritionLabel.text = loaded ? model.Nutrition + " / 100" : "-- / 100";
            if (nurseryStabilityLabel != null)
                nurseryStabilityLabel.text = loaded ? model.Stability + " / 100" : "-- / 100";
            if (nurseryNutritionFill != null)
                nurseryNutritionFill.fillAmount = loaded ? model.Nutrition / 100f : 0f;
            if (nurseryStabilityFill != null)
                nurseryStabilityFill.fillAmount = loaded ? model.Stability / 100f : 0f;

            bool hasOperation = loaded && model.ActiveOperation != null;
            bool pending = loaded && model.IsPending;
            bool readOnly = loaded && model.IsReadOnly;

            TimeSpan remaining = hasOperation
                ? model.Remaining(nurseryController.Elapsed) : TimeSpan.Zero;
            if (!configured) nurseryStatusLabel.text = fr
                ? "Connecte-toi pour accéder aux soins de la Nurserie."
                : "Sign in to access Nursery care.";
            else if (error) nurseryStatusLabel.text =
                BeeLocalization.Text("nursery.status.retry_hint") + " (" + model.ErrorCode + ")";
            else if (!loaded) nurseryStatusLabel.text = fr
                ? "Chargement des données officielles..." : "Loading server data...";
            else if (busy) nurseryStatusLabel.text = fr
                ? "Synchronisation en cours..." : "Syncing...";
            else if (pending) nurseryStatusLabel.text = fr
                ? "Commande en attente de confirmation. Utilise Vérifier la commande."
                : "Command awaiting confirmation. Use Verify command.";
            else if (readOnly) nurseryStatusLabel.text = fr
                ? "Hors ligne : consultation seulement." : "Offline: read-only.";
            else if (error) nurseryStatusLabel.text = fr
                ? "Erreur du serveur : " + model.ErrorCode
                : "Server error: " + model.ErrorCode;
            else nurseryStatusLabel.text = fr
                ? "Les soins améliorent la nutrition et la stabilité des larves."
                : "Care improves brood nutrition and stability.";

            if (nurseryOperationLabel != null)
            {
                string kind = hasOperation
                    ? (string.Equals(model.ActiveOperation.Type,
                        HiveBroodVitalityClient.FeedingType, StringComparison.Ordinal)
                        ? (fr ? "Nourrissage" : "Feeding")
                        : (fr ? "Stabilisation" : "Stabilization"))
                    : string.Empty;
                nurseryOperationLabel.text = !configured || !loaded
                    ? (fr ? "En attente du serveur" : "Waiting for server")
                    : pending ? (fr ? "Commande incertaine : vérification requise"
                                    : "Uncertain command: verification required")
                    : hasOperation
                        ? kind + " — " + (remaining <= TimeSpan.Zero
                            ? (fr ? "prêt à terminer" : "ready to complete")
                            : NurseryRemaining(remaining))
                        : (fr ? "Aucun soin en cours" : "No active care operation");
            }
            if (nurseryOperationFill != null)
                nurseryOperationFill.fillAmount = hasOperation
                    ? (float)model.Progress01(nurseryController.Elapsed) : 0f;

            bool mutationAvailable = configured && loaded && !busy && !readOnly && !pending;
            if (nurseryFeedButton != null)
                nurseryFeedButton.interactable = mutationAvailable &&
                    model.CanStart(HiveBroodVitalityClient.FeedingType);
            if (nurseryStabilizeButton != null)
                nurseryStabilizeButton.interactable = mutationAvailable &&
                    model.CanStart(HiveBroodVitalityClient.StabilizationType);
            if (nurseryCompleteButton != null)
            {
                nurseryCompleteButton.gameObject.SetActive(hasOperation && !pending);
                nurseryCompleteButton.interactable = mutationAvailable && hasOperation
                    && model.CanComplete(nurseryController.Elapsed);
            }
            if (nurseryRetryButton != null)
            {
                nurseryRetryButton.gameObject.SetActive(pending);
                nurseryRetryButton.interactable = configured && loaded && !busy && !readOnly && pending;
            }
        }

        private void RefreshInfirmary()
        {
            if (!infirmaryMode || !windowOpen) return;
            if (infirmaryController != null && infirmaryController.IsConfigured
                && !infirmaryController.IsBusy)
                infirmaryController.Refresh();
            RefreshInfirmaryLabels();
        }

        private void RefreshInfirmaryLabels()
        {
            if (!infirmaryMode || !windowOpen || suppressed
                || infirmaryTotalLabel == null || infirmaryStatusLabel == null) return;
            bool french = string.IsNullOrEmpty(BeeLocalization.CurrentLocale)
                || BeeLocalization.CurrentLocale.StartsWith("fr",
                    StringComparison.OrdinalIgnoreCase);
            bool configured = infirmaryController != null && infirmaryController.IsConfigured;
            CombatPatrolScreenModel model = configured ? infirmaryController.Model : null;
            bool busy = configured && infirmaryController.IsBusy;
            IReadOnlyList<InfirmaryRecoveryRow> rows = InfirmaryRecoveryOverview.Aggregate(
                model != null ? model.Recovering : null);
            long total = InfirmaryRecoveryOverview.Total(rows);
            infirmaryTotalLabel.text = configured
                ? total.ToString(System.Globalization.CultureInfo.CurrentCulture)
                    + (french ? " blessées en soins" : " recovering troops")
                : (french ? "Connexion requise" : "Connection required");
            if (!configured)
                infirmaryStatusLabel.text = french
                    ? "Connecte-toi pour consulter les blessées enregistrées par le serveur."
                    : "Sign in to view server-recorded wounded troops.";
            else if (busy)
                infirmaryStatusLabel.text = french
                    ? "Synchronisation de la convalescence..." : "Syncing recovery...";
            else if (model != null && model.State == CombatPatrolScreenState.Error)
                infirmaryStatusLabel.text = french
                    ? "Lecture indisponible. Appuie sur Actualiser pour réessayer."
                    : "Data unavailable. Tap Refresh to try again.";
            else
                infirmaryStatusLabel.text = total > 0
                    ? (french ? "Retour automatique dans la Caserne à la fin des soins."
                              : "Troops return to Barracks automatically once recovered.")
                    : (french ? "Aucune blessée actuellement en convalescence."
                              : "No troops are currently recovering.");

            foreach (InfirmaryRecoveryRow row in rows)
            {
                if (infirmaryCountLabels.TryGetValue(row.Family, out TextMeshProUGUI count)
                    && count != null)
                    count.text = configured
                        ? row.Wounded.ToString(System.Globalization.CultureInfo.CurrentCulture)
                        : "--";
                if (infirmaryTimerLabels.TryGetValue(row.Family, out TextMeshProUGUI timer)
                    && timer != null)
                    timer.text = configured
                        ? InfirmaryRecoveryOverview.Countdown(row.NextReadyAtUtc,
                            DateTimeOffset.UtcNow, french)
                        : (french ? "En attente du serveur" : "Awaiting server");
            }
        }

        public static Rect HeaderRectForProof(float safeWidth, float safeHeight)
        {
            bool landscape = safeWidth > safeHeight && safeWidth >= 600f;
            return new Rect(0f, 0f, safeWidth, landscape ? 88f : 132f);
        }

        public static Rect ScrollViewportForProof(float safeWidth, float safeHeight)
        {
            float header = HeaderRectForProof(safeWidth, safeHeight).height;
            // Dedicated right rail never consumes or overlaps a content card.
            return new Rect(0f, header + 12f, Mathf.Max(1f, safeWidth - 18f),
                Mathf.Max(1f, safeHeight - header - 24f));
        }

        public static Rect ScrollTrackForProof(float safeWidth, float safeHeight)
        {
            Rect body = ScrollViewportForProof(safeWidth, safeHeight);
            return new Rect(safeWidth - 13f, body.y + 7f, 6f,
                Mathf.Max(1f, body.height - 14f));
        }

        private void Render()
        {
            if (!windowOpen || suppressed || selected == null) return;
            if (canvas == null)
            {
                RectTransform root = Node(transform, "BuildingActivitiesCanvas", new Rect());
                canvas = root.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 420;
                root.gameObject.AddComponent<GraphicRaycaster>();
                CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                displayFont = Resources.Load<TMP_FontAsset>("Cinzel-Regular SDF");
            }

            canvas.gameObject.SetActive(true);
            // EditMode can reopen this canvas during automated QA. In Play we defer
            // destruction as usual; in EditMode objects must be removed immediately.
            // Iterate backwards because DestroyImmediate mutates the child hierarchy.
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                child.gameObject.SetActive(false);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            lastSize = new Vector2(Screen.width, Screen.height);
            lastSafe = Screen.safeArea;
            lastLanguage = BeeLocalization.CurrentLocale;
            float scale = PremiumCanvasScale(lastSize);
            canvas.GetComponent<CanvasScaler>().scaleFactor = scale;
            Rect safe = PremiumSafeCanvasRect(lastSize, lastSafe, scale);
            float width = safe.width;
            float height = safe.height;
            bool fr = string.IsNullOrEmpty(lastLanguage)
                || lastLanguage.StartsWith("fr", StringComparison.OrdinalIgnoreCase);

            Plate(canvas.transform, "Backdrop",
                new Rect(0f, 0f, Screen.width / scale, Screen.height / scale),
                Ink, "cave-background", true);
            safeRoot = Node(canvas.transform, "SafeArea", safe);
            Rect headerRect = HeaderRectForProof(width, height);
            RectTransform header = Plate(safeRoot, "Header", headerRect, Ink, "panel-fill");
            Texture2D banner = Resources.Load<Texture2D>(
                "PremiumBeeReference/BuildingBanners/alliance_future_hall");
            if (banner != null)
                Art(header, new Rect(width * .40f, 0f, width * .60f, headerRect.height),
                    banner, new Color(1f, 1f, 1f, .32f));

            string name = fr ? titleFr : titleEn;
            string role = selected.Role ?? string.Empty;
            if (!fr) role = "Building overview";
            ButtonAt(header, new Rect(16f, 12f, 86f, 48f),
                fr ? "Retour" : "Back", false, closeAction);
            bool landscape = width > height && width >= 600f;
            TextMeshProUGUI title = Label(header, landscape
                    ? new Rect(120f, 12f, Mathf.Max(100f, width - 262f), 39f)
                    : new Rect(18f, headerRect.height - 65f, width - 36f, 37f),
                name.ToUpperInvariant(), landscape ? 26f : 23f, Gold, true, true);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            Label(header, landscape
                    ? new Rect(122f, 53f, Mathf.Max(100f, width - 262f), 23f)
                    : new Rect(20f, headerRect.height - 27f, width - 40f, 22f),
                role, 13f, Paper);
            Plate(header, "GoldRule",
                new Rect(0f, headerRect.height - 1f, width, 1f),
                Gold, "etched-line");

            Rect viewportRect = ScrollViewportForProof(width, height);
            RectTransform viewport = Node(safeRoot, "Viewport", viewportRect);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            RectTransform content = Node(viewport, "BuildingCards", new Rect(
                0f, 0f, viewportRect.width, 0f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.sizeDelta = Vector2.zero;

            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            scroll.decelerationRate = .10f;

            Rect trackRect = ScrollTrackForProof(width, height);
            RectTransform rail = Plate(safeRoot, "ScrollTrack", trackRect, Panel, "panel-fill");
            RectTransform slide = Node(rail, "Sliding", new Rect(
                1f, 1f, trackRect.width - 2f, trackRect.height - 2f));
            RectTransform handle = Node(slide, "ScrollHandle", new Rect());
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = handle.offsetMax = Vector2.zero;
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = Gold;
            handleImage.raycastTarget = true;
            Scrollbar scrollbar = rail.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.numberOfSteps = 0;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            float cWidth = viewportRect.width;
            float gap = 20f;
            float cardWidth = cWidth - gap * 2f;
            float cursor;
            if (infirmaryMode)
            {
                RenderInfirmaryCards(content, cardWidth, gap, out cursor,
                    fr, viewportRect.height);
            }
            else if (nurseryMode)
            {
                RenderNurseryCards(content, cardWidth, gap, out cursor,
                    fr, viewportRect.height);
            }
            else
            {
                cursor = 14f;
                RectTransform overview = CardAt(content, new Rect(gap, cursor, cardWidth, 176f),
                    fr ? "APERÇU DU BÂTIMENT" : "BUILDING OVERVIEW", selected.IconId);
                Label(overview, new Rect(18f, 76f, cardWidth - 36f, 74f),
                    fr ? "Bâtiment du royaume : " + name : "Kingdom building: " + name,
                    17f, Paper, true);
                cursor += 196f;

                RectTransform availability = CardAt(content,
                    new Rect(gap, cursor, cardWidth, 248f),
                    fr ? "CAPACITÉS ET DÉVELOPPEMENT" : "CAPABILITIES & DEVELOPMENT",
                    "research");
                Label(availability, new Rect(18f, 70f, cardWidth - 36f, 56f),
                    fr ? "Fonctionnalité en préparation" : "Feature in development",
                    17f, Gold, true);
                Label(availability, new Rect(18f, 121f, cardWidth - 36f, 111f),
                    fr ? detailFr : detailEn, 14f, Muted);
                cursor += 268f;

                RectTransform actions = CardAt(content, new Rect(gap, cursor, cardWidth, 154f),
                    fr ? "GESTION DU BÂTIMENT" : "BUILDING MANAGEMENT", "build");
                Label(actions, new Rect(18f, 72f, cardWidth - 36f, 32f),
                    fr ? "L'amélioration reste accessible via Construction."
                        : "Upgrades remain available through Construction.",
                    13f, Muted);
                if (upgradeAction != null)
                    ButtonAt(actions,
                        new Rect(cardWidth - 176f, 107f, 158f, 40f),
                        fr ? "Améliorer" : "Upgrade", true, upgradeAction);
                cursor += 178f;
                content.sizeDelta = new Vector2(0f,
                    Mathf.Max(cursor + 28f, viewportRect.height + 1f));
            }
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1f;
        }

        private void RenderInfirmaryCards(Transform parent, float cardWidth,
            float left, out float cursor, bool french, float viewportHeight)
        {
            infirmaryCountLabels.Clear();
            infirmaryTimerLabels.Clear();
            cursor = 14f;
            RectTransform summary = CardAt(parent, new Rect(left, cursor, cardWidth, 158f),
                french ? "CENTRE DE CONVALESCENCE" : "RECOVERY WARD", "help");
            infirmaryTotalLabel = Label(summary, new Rect(18f, 76f, cardWidth - 36f, 33f),
                "--", 24f, Gold, true);
            infirmaryStatusLabel = Label(summary, new Rect(18f, 113f, cardWidth - 36f, 40f),
                string.Empty, 13f, Muted);
            cursor += 178f;

            string[] families = {
                InfirmaryRecoveryOverview.Guardians,
                InfirmaryRecoveryOverview.Wingrunners,
                InfirmaryRecoveryOverview.Darters
            };
            for (int i = 0; i < families.Length; i++)
            {
                string family = families[i];
                RectTransform ward = CardAt(parent,
                    new Rect(left, cursor, cardWidth, 124f),
                    InfirmaryRecoveryOverview.FamilyName(family, french).ToUpperInvariant(),
                    "bees");
                infirmaryCountLabels[family] = Label(ward,
                    new Rect(18f, 73f, 110f, 37f), "--", 28f, Paper, true);
                infirmaryTimerLabels[family] = Label(ward,
                    new Rect(130f, 77f, Mathf.Max(90f, cardWidth - 148f), 38f),
                    string.Empty, 14f, Muted);
                cursor += 142f;
            }

            RectTransform actions = CardAt(parent,
                new Rect(left, cursor, cardWidth, 164f),
                french ? "SUIVI DES SOINS" : "RECOVERY MANAGEMENT", "help");
            Label(actions, new Rect(18f, 74f, cardWidth - 36f, 37f),
                french ? "Les blessées reviennent automatiquement après leur convalescence."
                    : "Wounded bees return automatically once their recovery finishes.",
                13f, Muted);
            float buttonWidth = Mathf.Max(100f, Mathf.Min(172f, (cardWidth - 52f) / 2f));
            ButtonAt(actions, new Rect(18f, 117f, buttonWidth, 42f),
                french ? "Actualiser" : "Refresh", false, RefreshInfirmary);
            if (upgradeAction != null)
                ButtonAt(actions, new Rect(cardWidth - buttonWidth - 18f,
                    117f, buttonWidth, 42f),
                    french ? "Améliorer" : "Upgrade", true, upgradeAction);
            cursor += 184f;
            float contentHeight = Mathf.Max(cursor + 24f, viewportHeight + 1f);
            RectTransform parentRect = parent as RectTransform;
            if (parentRect != null)
                parentRect.sizeDelta = new Vector2(0f, contentHeight);
            RefreshInfirmaryLabels();
        }

        private static Image NurseryProgressBar(Transform parent, Rect rect)
        {
            RectTransform back = Node(parent, "ProgressTrack", rect);
            Image baseImage = back.gameObject.AddComponent<Image>();
            baseImage.color = new Color(.11f, .08f, .045f, 1f);
            baseImage.raycastTarget = false;
            RectTransform front = Node(back, "ProgressFill", new Rect(1f, 1f,
                Mathf.Max(1f, rect.width - 2f), Mathf.Max(1f, rect.height - 2f)));
            Image fill = front.gameObject.AddComponent<Image>();
            fill.color = Gold;
            if (nurseryProgressSprite == null)
            {
                Texture2D white = Texture2D.whiteTexture;
                nurseryProgressSprite = Sprite.Create(white,
                    new Rect(0f, 0f, white.width, white.height), Vector2.one * .5f);
                nurseryProgressSprite.name = "Nursery progress source";
                nurseryProgressSprite.hideFlags = HideFlags.HideAndDontSave;
            }
            // Image.Type.Filled needs a sprite; without one an empty gauge looks full.
            fill.sprite = nurseryProgressSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            return fill;
        }

        private void RenderNurseryCards(Transform parent, float width,
            float left, out float cursor, bool fr, float viewportHeight)
        {
            nurserySummaryLabel = null;
            nurseryStatusLabel = null;
            nurseryFeedButton = nurseryStabilizeButton = nurseryCompleteButton = nurseryRetryButton = null;
            cursor = 14f;
            RectTransform summary = CardAt(parent, new Rect(left, cursor, width, 178f),
                fr ? "VITALITÉ DE LA NURSERIE" : "NURSERY VITALITY", "bees");
            nurserySummaryLabel = Label(summary, new Rect(18f, 76f, width - 36f, 35f),
                "--", 24f, Gold, true);
            nurseryStatusLabel = Label(summary, new Rect(18f, 119f, width - 36f, 50f),
                string.Empty, 13f, Muted);
            cursor += 197f;

            RectTransform nutrition = CardAt(parent,
                new Rect(left, cursor, width, 150f),
                fr ? "NUTRITION DES LARVES" : "BROOD NUTRITION", "bees");
            nurseryNutritionLabel = Label(nutrition,
                new Rect(18f, 75f, width - 36f, 35f), "-- / 100", 22f, Paper, true);
            nurseryNutritionFill = NurseryProgressBar(nutrition,
                new Rect(18f, 121f, Mathf.Max(1f, width - 36f), 14f));
            cursor += 169f;

            RectTransform stability = CardAt(parent,
                new Rect(left, cursor, width, 150f),
                fr ? "STABILITÉ DE LA COLONIE" : "COLONY STABILITY", "bees");
            nurseryStabilityLabel = Label(stability,
                new Rect(18f, 75f, width - 36f, 35f), "-- / 100", 22f, Paper, true);
            nurseryStabilityFill = NurseryProgressBar(stability,
                new Rect(18f, 121f, Mathf.Max(1f, width - 36f), 14f));
            cursor += 169f;

            RectTransform operations = CardAt(parent,
                new Rect(left, cursor, width, 271f),
                fr ? "SOINS ET OPÉRATIONS" : "CARE OPERATIONS", "research");
            nurseryOperationLabel = Label(operations,
                new Rect(18f, 77f, width - 36f, 45f),
                string.Empty, 16f, Paper, true);
            nurseryOperationFill = NurseryProgressBar(operations,
                new Rect(18f, 129f, Mathf.Max(1f, width - 36f), 12f));
            float btnW = Mathf.Max(92f, (width - 50f) / 2f);
            nurseryFeedButton = ButtonAt(operations,
                new Rect(18f, 154f, btnW, 43f),
                (fr ? "Nourrir (" : "Feed (") +
                    HiveBroodVitalityClient.FeedingHoneyCost +
                    (fr ? " miel)" : " honey)"), true,
                () => NurseryStart(HiveBroodVitalityClient.FeedingType));
            nurseryStabilizeButton = ButtonAt(operations,
                new Rect(width - btnW - 18f, 154f, btnW, 43f),
                (fr ? "Stabiliser (" : "Stabilize (") +
                    HiveBroodVitalityClient.StabilizationWaxCost +
                    (fr ? " cire)" : " wax)"), true,
                () => NurseryStart(HiveBroodVitalityClient.StabilizationType));
            nurseryCompleteButton = ButtonAt(operations,
                new Rect(18f, 210f, btnW, 43f),
                fr ? "Terminer" : "Complete", false, NurseryComplete);
            nurseryRetryButton = ButtonAt(operations,
                new Rect(width - btnW - 18f, 210f, btnW, 43f),
                fr ? "Vérifier" : "Verify", false, NurseryRetry);
            cursor += 290f;

            RectTransform cohorts = CardAt(parent,
                new Rect(left, cursor, width, 174f),
                fr ? "COUVÉES ROYALES" : "ROYAL BROOD COHORTS", "nursery");
            Label(cohorts, new Rect(18f, 76f, width - 36f, 43f),
                fr ? "Incubation et réserve de recrues"
                    : "Incubation and recruit reserve",
                17f, Paper, true);
            Label(cohorts, new Rect(18f, 120f, width - 36f, 45f),
                fr ? "Prototype serveur désactivé : aucune couvée ne peut encore être lancée."
                    : "Server prototype disabled: cohorts cannot yet be started.",
                13f, Muted);
            cursor += 193f;

            RectTransform management = CardAt(parent,
                new Rect(left, cursor, width, 166f),
                fr ? "GESTION DE LA NURSERIE" : "NURSERY MANAGEMENT", "build");
            Label(management, new Rect(18f, 76f, width - 36f, 35f),
                fr ? "Les soins sont validés et enregistrés par le serveur."
                    : "Care is authorized and saved by the server.", 13f, Muted);
            float managementWidth = Mathf.Max(94f, (width - 54f) / 2f);
            ButtonAt(management, new Rect(18f, 117f, managementWidth, 42f),
                fr ? "Actualiser" : "Refresh", false, RefreshNursery);
            if (upgradeAction != null)
                ButtonAt(management,
                    new Rect(width - 18f - managementWidth, 117f, managementWidth, 42f),
                    fr ? "Améliorer" : "Upgrade", true, upgradeAction);
            cursor += 184f;

            RectTransform content = parent as RectTransform;
            if (content != null)
                content.sizeDelta = new Vector2(0f,
                    Mathf.Max(cursor + 24f, viewportHeight + 1f));
            RefreshNurseryLabels();
        }

        private RectTransform CardAt(Transform parent, Rect rect, string title, string icon)
        {
            RectTransform card = Plate(parent, "Card", rect, Card, "panel-fill");
            Icon(card, new Rect(18f, 12f, 48f, 48f), icon);
            Label(card, new Rect(78f, 17f, rect.width - 96f, 42f),
                title, 17f, Gold, true);
            Plate(card, "Separator", new Rect(18f, 64f, rect.width - 36f, 1f),
                new Color(.43f, .29f, .12f), "etched-line");
            return card;
        }

        private Button ButtonAt(Transform parent, Rect rect, string text,
            bool primary, Action callback)
        {
            RectTransform node = Plate(parent, "Button", rect,
                primary ? new Color(.80f, .49f, .13f)
                    : new Color(.22f, .15f, .07f), "panel-fill");
            RawImage graphic = node.GetComponent<RawImage>();
            graphic.raycastTarget = true;
            Button button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            TextMeshProUGUI caption = Label(node,
                new Rect(5f, 0f, rect.width - 10f, rect.height), text,
                15f, primary ? Ink : Paper, true);
            caption.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayUIClick();
                callback?.Invoke();
            });
            return button;
        }

        // Self-contained layout and resource lookup: no reliance on deleted legacy
        // HiveMapActivitiesBootstrap or monolith texture helper functions.
        public static float PremiumCanvasScale(Vector2 size)
        {
            if (size.x <= 0f || size.y <= 0f) return 1f;
            // Match the approved Activities-v4 density; 1920x1080 uses 1.2, not 1.5.
            float basis = size.y > size.x ? size.x / 390f : size.y / 900f;
            return size.y > size.x ? Mathf.Clamp(basis, 1f, 3f) : Mathf.Clamp(basis, 1f, 2f);
        }

        public static Rect PremiumSafeCanvasRect(Vector2 size, Rect safeArea, float scale)
        {
            if (scale <= 0f) throw new ArgumentOutOfRangeException(nameof(scale));
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, size.x, size.y);
            // Unity Screen.safeArea is bottom-left anchored; all Nursery UI
            // rectangles are top-left anchored.
            return new Rect(safeArea.xMin / scale,
                (size.y - safeArea.yMax) / scale,
                safeArea.width / scale, safeArea.height / scale);
        }

        private static Texture PremiumMaterial(string kind)
        {
            // Use the original subtle Activities materials, never stretch a city image
            // across each card. The bridge is source-owned and compilation-checked.
            return HiveViewProductUiPresenter.BuildingWindowMaterial(kind)
                ?? Texture2D.whiteTexture;
        }

        private static Texture PremiumIcon(string key)
        {
            string resourceKey = string.IsNullOrWhiteSpace(key) ? "nursery" : key;
            if (string.Equals(resourceKey, "bees", StringComparison.Ordinal))
                resourceKey = "nursery";
            else if (string.Equals(resourceKey, "build", StringComparison.Ordinal))
                resourceKey = "upgrade";
            Texture2D texture = Resources.Load<Texture2D>(
                "PremiumBeeIcons/" + resourceKey);
            return texture ?? Resources.Load<Texture2D>("PremiumBeeIcons/nursery");
        }

        private static RectTransform Node(Transform parent, string name, Rect rect)
        {
            RectTransform node = new GameObject(name,
                typeof(RectTransform)).GetComponent<RectTransform>();
            node.SetParent(parent, false);
            node.anchorMin = node.anchorMax = new Vector2(0f, 1f);
            node.pivot = new Vector2(0f, 1f);
            node.anchoredPosition = new Vector2(rect.x, -rect.y);
            node.sizeDelta = rect.size;
            return node;
        }

        private static RectTransform Plate(Transform parent, string name, Rect rect,
            Color fill, string texture, bool hitTarget = false)
        {
            RectTransform node = Node(parent, name, rect);
            RawImage plate = node.gameObject.AddComponent<RawImage>();
            plate.texture = Texture2D.whiteTexture;
            plate.color = fill;
            plate.raycastTarget = hitTarget;
            RawImage grain = Node(node, "Material", rect)
                .gameObject.AddComponent<RawImage>();
            grain.rectTransform.anchorMin = Vector2.zero;
            grain.rectTransform.anchorMax = Vector2.one;
            grain.rectTransform.offsetMin = grain.rectTransform.offsetMax = Vector2.zero;
            grain.texture = PremiumMaterial(
                texture == "panel-fill" ? "panel-grain" : texture);
            grain.color = new Color(1f, 1f, 1f, .16f);
            grain.raycastTarget = false;
            return node;
        }

        private static void Art(Transform parent, Rect rect,
            Texture texture, Color color)
        {
            RawImage image = Node(parent, "Artwork", rect)
                .gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
        }

        private void Icon(Transform parent, Rect rect, string key)
        {
            // The premium image already contains its own golden frame; don't draw
            // a second ornamental socket underneath. Limit the replacement to
            // the Infirmary's recovery and care-section icons.
            if (infirmaryMode && string.Equals(key, "help", StringComparison.Ordinal))
            {
                Texture2D healing = Resources.Load<Texture2D>(
                    InfirmaryPremiumIconResourcePath);
                if (healing != null)
                {
                    Art(parent, new Rect(rect.x - 4f, rect.y - 4f,
                        rect.width + 8f, rect.height + 8f), healing, Color.white);
                    return;
                }
            }
            Texture icon = PremiumIcon(key);
            if (icon != null) Art(parent, rect, icon, Color.white);
        }

        private TextMeshProUGUI Label(Transform parent, Rect rect,
            string text, float size, Color color, bool bold = false, bool display = false)
        {
            TextMeshProUGUI label = Node(parent, "Label", rect)
                .gameObject.AddComponent<TextMeshProUGUI>();
            if (display && displayFont != null) label.font = displayFont;
            label.text = text ?? string.Empty;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.richText = false;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }
    }
}
