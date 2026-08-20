using System.Collections.Generic;
using BeeKingdom.Playground;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeeKingdom.LivingHiveMenu
{
    // FENÊTRE PLEIN ÉCRAN Recherche (Local Preview) en uGUI pour la scène
    // Environment2D5D_SpatialV3.
    //
    // Réplique fidèle, en uGUI (Screen Space Overlay, sans CanvasScaler, 1 unité = 1 pixel
    // écran — approche identique à LivingHiveMenuCanvas) de DrawResearchFullscreen /
    // DrawResearchFullscreenCard du monolithe :
    //   - bannière (artwork PremiumBeeReference/BuildingBanners/research_node), titre
    //     "RECHERCHE", sous-titre, séparateur doré ;
    //   - rail de 4 filtres (Toutes les études / Forage / Ressources / Défense), logique
    //     d'appariement = miroir exact de ResearchMatchesFullscreenFilter (L.31791) ;
    //   - grille de cartes responsive (1 colonne portrait / 2 colonnes paysage), chaque carte
    //     pilotée par LivingHiveResearchState (lancer / progression / complétion / raison) ;
    //   - animation d'ouverture 0,18 s (SmoothStep : voile + zoom 0.975 -> 1) ;
    //   - fermeture par bouton retour / fermer (rects IMGUI du monolithe L.33326) et Échap.
    //
    // Aucune dépendance caméra : rendu en ScreenSpaceOverlay, indépendant de la caméra
    // (conforme à la contrainte mission). Testable hors play-mode : Build() construit
    // l'hiérarchie, RefreshAll() recharge l'état depuis LivingHiveResearchState (pur C#).
    public sealed class LivingHiveResearchWindow : MonoBehaviour
    {
        private const string BannerResource = "PremiumBeeReference/BuildingBanners/research_node";

        private readonly LivingHiveResearchState state = new LivingHiveResearchState();

        private Canvas canvas;
        private RectTransform windowRoot;
        private Image veil;
        private Image banner;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subtitleText;
        private Image separator;
        private GameObject filterPanel;
        private GameObject contentPanel;
        private RectTransform cardsContent;
        private readonly Dictionary<string, ResearchCardVisual> cards = new Dictionary<string, ResearchCardVisual>();
        private readonly Dictionary<string, Button> filterButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, Image> filterButtonPanels = new Dictionary<string, Image>();
        private bool open;
        private float openedAt = -10f;
        private Vector2 lastScreenSize;
        private bool built;

        public event System.Action<string> CloseRequested;

        public LivingHiveResearchState State => state;

        public bool IsOpen => open;

        public bool IsBuilt => built;

        public string SelectedFilter => state.SelectedFilterForProof;

        public int FilterCount => filterButtons.Count;

        public int CardCount => cards.Count;

        public bool IsPortraitForProof => LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);

        public int ColumnCountForProof => LivingHiveResearchSpec.ColumnCount(IsPortraitForProof);

        // --- API ForProof ---

        public bool HasCard(string researchId)
        {
            return cards.ContainsKey(researchId);
        }

        public string CardTitleForProof(string researchId)
        {
            ResearchCardVisual card;
            return cards.TryGetValue(researchId, out card) && card.Title != null ? card.Title.text : string.Empty;
        }

        public string CardActionLabelForProof(string researchId)
        {
            ResearchCardVisual card;
            return cards.TryGetValue(researchId, out card) && card.ActionLabel != null ? card.ActionLabel.text : string.Empty;
        }

        public bool CardEnabledForProof(string researchId)
        {
            ResearchCardVisual card;
            return cards.TryGetValue(researchId, out card) && card.Action != null && card.Action.interactable;
        }

        public bool CardShowsProgressForProof(string researchId)
        {
            ResearchCardVisual card;
            return cards.TryGetValue(researchId, out card) && card.ProgressRoot != null && card.ProgressRoot.activeSelf;
        }

        public float CardProgressFillForProof(string researchId)
        {
            ResearchCardVisual card;
            if (cards.TryGetValue(researchId, out card) && card.ProgressFill != null) return card.ProgressFill.fillAmount;
            return 0f;
        }

        public string CardReasonForProof(string researchId)
        {
            ResearchCardVisual card;
            return cards.TryGetValue(researchId, out card) && card.Reason != null ? card.Reason.text : string.Empty;
        }

        public string CardStateForProof(string researchId)
        {
            switch (state.StatusForProof(researchId))
            {
                case LivingHiveResearchState.CardStatus.Completed: return "completed";
                case LivingHiveResearchState.CardStatus.Running: return "running";
                default: return "available";
            }
        }

        public int VisibleCardCountForProof()
        {
            int count = 0;
            foreach (KeyValuePair<string, ResearchCardVisual> pair in cards)
            {
                if (pair.Value.Root != null && pair.Value.Root.activeSelf) count++;
            }
            return count;
        }

        public void Build()
        {
            if (built) return;
            state.EnsureLoaded();
            state.SetFilterForProof(LivingHiveResearchSpec.FilterAll);

            GameObject canvasObj = new GameObject("ResearchFullscreen");
            canvasObj.transform.SetParent(transform, false);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Pas de CanvasScaler : géométrie pixel écran du monolithe (voir LivingHiveMenuCanvas).
            canvasObj.AddComponent<GraphicRaycaster>();

            windowRoot = NewRect(canvasObj.transform, "WindowRoot", FullScreenUiRect());
            BuildVeil();
            BuildBanner();
            BuildTopButtons();
            BuildFilterRail();
            BuildContent();
            RebuildCards();
            RefreshFilterHighlights();
            RefreshCardsStates();
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            built = true;
            gameObject.SetActive(false);
        }

        private Rect FullScreenUiRect()
        {
            return ScreenRectToUiRect(new Rect(0f, 0f, Screen.width, Screen.height));
        }

        private void BuildVeil()
        {
            veil = NewImage(windowRoot, "Veil", LivingHiveMenuVisuals.RailBackdropSprite());
            veil.color = LivingHiveResearchSpec.VeilColor;
            veil.raycastTarget = true;
            veil.type = Image.Type.Sliced;
            FillRect(veil.rectTransform);
        }

        private void BuildBanner()
        {
            bool portrait = IsPortraitForProof;
            float screenW = Screen.width;
            float screenH = Screen.height;
            float bannerH = LivingHiveResearchSpec.BannerHeight(portrait, screenH);

            banner = NewImage(windowRoot, "Banner", BannerSprite());
            banner.type = Image.Type.Simple;
            banner.raycastTarget = true;
            PositionRect(banner.rectTransform, ScreenRectToUiRect(new Rect(0f, 0f, screenW, bannerH)));

            // Voile sombre sur la bannière (miroir L.33358-33360).
            Image shade = NewImage(banner.rectTransform, "BannerShade", LivingHiveMenuVisuals.RailBackdropSprite());
            shade.color = new Color(0.01f, 0.008f, 0.004f, 0.52f);
            shade.raycastTarget = false;
            shade.type = Image.Type.Sliced;
            FillRect(shade.rectTransform);

            titleText = CreateLabel(banner.rectTransform, LivingHiveResearchSpec.BannerTitle, portrait ? 20 : 28, TextAnchor.MiddleLeft, true);
            titleText.transform.SetAsLastSibling();
            titleText.color = Color.white;
            TitleRect(titleText, 28f, bannerH - (portrait ? 42f : 48f));

            subtitleText = CreateLabel(banner.rectTransform, LivingHiveResearchSpec.BannerSubtitle, portrait ? 9 : 12, TextAnchor.MiddleLeft);
            subtitleText.transform.SetAsLastSibling();
            subtitleText.color = LivingHiveMenuVisuals.LabelInactiveColor;
            TitleRect(subtitleText, 30f, bannerH - (portrait ? 20f : 23f));

            separator = NewImage(banner.rectTransform, "Separator", LivingHiveMenuVisuals.RailOrnamentSprite());
            separator.color = LivingHiveResearchSpec.SeparatorColor;
            separator.raycastTarget = false;
            PositionRect(separator.rectTransform, ScreenRectToUiRect(new Rect(0f, bannerH - 1f, screenW, 1f)));
        }

        private void BuildTopButtons()
        {
            Button back = NewButton(windowRoot, "BackButton", "<");
            PositionRect(back.GetComponent<RectTransform>(), ScreenRectToUiRect(LivingHiveResearchSpec.BackButtonRect()));
            back.onClick.AddListener(() => RequestClose("back"));

            Button close = NewButton(windowRoot, "CloseButton", "X");
            PositionRect(close.GetComponent<RectTransform>(), ScreenRectToUiRect(LivingHiveResearchSpec.CloseButtonRect(Screen.width)));
            close.onClick.AddListener(() => RequestClose("close"));
        }

        private void BuildFilterRail()
        {
            bool portrait = IsPortraitForProof;
            float screenW = Screen.width;
            float screenH = Screen.height;
            Rect railUi = ScreenRectToUiRect(LivingHiveResearchSpec.FilterRailRect(portrait, screenW, screenH));

            filterPanel = new GameObject("FilterPanel");
            filterPanel.transform.SetParent(windowRoot, false);
            Image panel = filterPanel.AddComponent<Image>();
            panel.sprite = LivingHiveMenuVisuals.RailBackdropSprite();
            panel.type = Image.Type.Sliced;
            panel.color = Color.white;
            PositionRect(panel.GetComponent<RectTransform>(), railUi);

            string[] filters = LivingHiveResearchSpec.Filters;
            string[] labels = LivingHiveResearchSpec.FilterLabels;
            float gap = 6f;
            float itemWidth = LivingHiveResearchSpec.FilterItemWidth(portrait, railUi.width);
            for (int i = 0; i < filters.Length; i++)
            {
                Image visual = NewImage(filterPanel.transform, "Filter_" + filters[i], LivingHiveMenuVisuals.ButtonNormalSprite());
                visual.type = Image.Type.Sliced;
                visual.raycastTarget = true;
                Rect filterUi = ScreenRectToUiRect(new Rect(6f + i * (itemWidth + gap), 8f, itemWidth, railUi.height - 16f));
                PositionRect(visual.rectTransform, filterUi);

                TextMeshProUGUI label = CreateLabel(visual.rectTransform, labels[i], portrait ? 8 : 10, TextAnchor.MiddleCenter);
                label.transform.SetAsLastSibling();
                FillRect(label.rectTransform);

                Button button = visual.gameObject.AddComponent<Button>();
                button.targetGraphic = visual;
                button.transition = Selectable.Transition.None;
                string capture = filters[i];
                button.onClick.AddListener(() => OnFilterClicked(capture));
                filterButtons[capture] = button;
                filterButtonPanels[capture] = visual;
            }
        }

        private void BuildContent()
        {
            bool portrait = IsPortraitForProof;
            float screenW = Screen.width;
            float screenH = Screen.height;
            Rect contentUi = ScreenRectToUiRect(LivingHiveResearchSpec.ContentRect(portrait, screenW, screenH));

            contentPanel = new GameObject("ContentPanel");
            contentPanel.transform.SetParent(windowRoot, false);
            Image panel = contentPanel.AddComponent<Image>();
            panel.sprite = LivingHiveMenuVisuals.RailBackdropSprite();
            panel.type = Image.Type.Sliced;
            panel.color = Color.white;
            PositionRect(panel.GetComponent<RectTransform>(), contentUi);

            // Scrollview vertical des cartes (miroir du BeginScrollView L.33409).
            ScrollRect scroll = contentPanel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            RectTransform viewport = NewRect(contentPanel.transform, "CardsViewport", new Rect(0f, 0f, contentUi.width, contentUi.height));
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.zero;
            viewport.pivot = new Vector2(0f, 0f);
            viewport.anchoredPosition = Vector2.zero;
            viewport.sizeDelta = new Vector2(contentUi.width, contentUi.height);
            Image mask = viewport.gameObject.AddComponent<Image>();
            mask.color = new Color(0f, 0f, 0f, 0f);
            mask.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            cardsContent = NewRect(viewport, "CardsContent", new Rect(0f, 0f, contentUi.width, contentUi.height));
            cardsContent.anchorMin = new Vector2(0f, 1f);
            cardsContent.anchorMax = new Vector2(0f, 1f);
            cardsContent.pivot = new Vector2(0f, 1f);
            cardsContent.anchoredPosition = Vector2.zero;
            cardsContent.sizeDelta = new Vector2(contentUi.width, contentUi.height);
            scroll.content = cardsContent;
        }

        private void RebuildCards()
        {
            foreach (KeyValuePair<string, ResearchCardVisual> pair in cards)
            {
                if (pair.Value.Root == null) continue;
                if (Application.isPlaying) Object.Destroy(pair.Value.Root);
                else Object.DestroyImmediate(pair.Value.Root);
            }
            cards.Clear();

            bool portrait = IsPortraitForProof;
            IReadOnlyList<LocalPreviewResearchDefinition> definitions = LocalPreviewResearchCatalog.All;
            List<LocalPreviewResearchDefinition> visible = new List<LocalPreviewResearchDefinition>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
            {
                if (LivingHiveResearchSpec.MatchesFilter(definitions[i].ResearchId, state.SelectedFilterForProof))
                    visible.Add(definitions[i]);
            }

            if (cardsContent == null) return;
            float inset = LivingHiveResearchSpec.ViewportInset(portrait);
            float viewportWidth = Mathf.Max(1f, cardsContent.rect.width - inset * 2f);
            int columns = LivingHiveResearchSpec.ColumnCount(portrait);
            float gap = LivingHiveResearchSpec.CardGap(portrait);
            float cardHeight = LivingHiveResearchSpec.CardHeight(portrait);
            float cardWidth = (viewportWidth - gap * (columns - 1)) / columns;
            int rows = Mathf.CeilToInt(visible.Count / (float)columns);
            float contentH = Mathf.Max(cardsContent.rect.height, rows * (cardHeight + gap));
            cardsContent.sizeDelta = new Vector2(viewportWidth, contentH);

            for (int i = 0; i < visible.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Rect cardUi = new Rect(
                    inset + col * (cardWidth + gap),
                    inset + row * (cardHeight + gap),
                    cardWidth,
                    cardHeight);
                BuildCard(visible[i], cardUi, cardWidth, cardHeight, portrait);
            }
        }

        private void BuildCard(LocalPreviewResearchDefinition definition, Rect cardUi, float cardWidth, float cardHeight, bool portrait)
        {
            GameObject go = new GameObject("Card_" + definition.ResearchId);
            go.transform.SetParent(cardsContent, false);
            Image panel = go.AddComponent<Image>();
            panel.sprite = LivingHiveMenuVisuals.ButtonNormalSprite();
            panel.type = Image.Type.Sliced;
            panel.color = Color.white;
            panel.raycastTarget = true;
            PositionRect(panel.GetComponent<RectTransform>(), new Rect(cardUi.x, cardUi.y, cardWidth, cardHeight));

            float iconSize = portrait ? 42f : 52f;
            Image icon = NewImage(panel.rectTransform, "Icon", IconSprite(definition.IconId));
            icon.color = Color.white;
            icon.raycastTarget = false;
            PositionRect(icon.rectTransform, Ui(12f, 14f, iconSize, iconSize, cardHeight));

            float textWidth = cardWidth - iconSize - 24f - (portrait ? 64f : 78f);
            TextMeshProUGUI title = CreateLabel(panel.rectTransform, LivingHiveResearchState.ResearchTitle(definition),
                portrait ? 12 : 14, TextAnchor.MiddleLeft, true);
            title.color = LivingHiveResearchSpec.CardNormalAccent;
            title.raycastTarget = false;
            PositionRect(title.rectTransform, Ui(portrait ? 64f : 78f, 12f, textWidth, 22f, cardHeight));
            title.transform.SetAsLastSibling();

            TextMeshProUGUI summary = CreateLabel(panel.rectTransform, LivingHiveResearchState.ResearchSummary(definition),
                portrait ? 9 : 11, TextAnchor.UpperLeft);
            summary.raycastTarget = false;
            summary.enableWordWrapping = true;
            PositionRect(summary.rectTransform, Ui(portrait ? 64f : 78f, 38f, textWidth, portrait ? 34f : 44f, cardHeight));
            summary.transform.SetAsLastSibling();

            TextMeshProUGUI detail = CreateLabel(panel.rectTransform, LivingHiveResearchState.ResearchCostText(definition),
                portrait ? 8 : 9, TextAnchor.MiddleLeft);
            detail.raycastTarget = false;
            PositionRect(detail.rectTransform, Ui(14f, cardHeight - 30f, cardWidth - 128f, 22f, cardHeight));
            detail.transform.SetAsLastSibling();

            float actionWidth = portrait ? 100f : 118f;
            Image actionPanel = NewImage(panel.rectTransform, "Action", LivingHiveMenuVisuals.ButtonNormalSprite());
            actionPanel.type = Image.Type.Sliced;
            actionPanel.raycastTarget = true;
            PositionRect(actionPanel.rectTransform, Ui(cardWidth - actionWidth - 12f, 14f, actionWidth, 42f, cardHeight));

            TextMeshProUGUI actionLabel = CreateLabel(actionPanel.rectTransform, "Lancer", portrait ? 10 : 11, TextAnchor.MiddleCenter, true);
            actionLabel.color = Color.white;
            actionLabel.raycastTarget = false;
            FillRect(actionLabel.rectTransform);

            Button action = actionPanel.gameObject.AddComponent<Button>();
            action.targetGraphic = actionPanel;
            action.transition = Selectable.Transition.None;
            string researchId = definition.ResearchId;
            action.onClick.AddListener(() => OnActionClicked(researchId));

            // Raison non-lançable sous le bouton (miroir L.33465).
            TextMeshProUGUI reason = CreateLabel(panel.rectTransform, string.Empty, portrait ? 8 : 9, TextAnchor.MiddleCenter);
            reason.raycastTarget = false;
            PositionRect(reason.rectTransform, Ui(cardWidth - actionWidth - 16f, cardHeight - actionWidth - 2f, actionWidth + 8f, 22f, cardHeight));
            reason.transform.SetAsLastSibling();

            // Barre de progression sous le bouton (miroir L.33464).
            GameObject progressRoot = new GameObject("ProgressRoot");
            progressRoot.transform.SetParent(panel.rectTransform, false);
            Image progressBg = progressRoot.AddComponent<Image>();
            progressBg.sprite = LivingHiveMenuVisuals.RailBackdropSprite();
            progressBg.type = Image.Type.Sliced;
            progressBg.color = new Color(0f, 0f, 0f, 0.7f);
            progressBg.raycastTarget = false;
            PositionRect(progressBg.rectTransform, Ui(cardWidth - actionWidth - 12f, 66f, actionWidth, 7f, cardHeight));

            Image progressFill = NewImage(progressRoot.transform, "ProgressFill", LivingHiveMenuVisuals.RailOrnamentSprite());
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFill.color = new Color(1f, 0.72f, 0.16f, 0.9f);
            progressFill.raycastTarget = false;
            PositionRect(progressFill.rectTransform, Ui(0f, 0f, actionWidth, 7f, cardHeight));

            cards[researchId] = new ResearchCardVisual
            {
                Root = go,
                Panel = panel,
                Icon = icon,
                Title = title,
                Summary = summary,
                Detail = detail,
                ActionLabel = actionLabel,
                Action = action,
                Reason = reason,
                ProgressRoot = progressRoot,
                ProgressFill = progressFill
            };
        }

        // Convertit un rect IMGUI enfant (coin haut-gauche de la carte, y descendant) en
        // coordonnées uGUI (coin bas-gauche de la carte, même 1 pixel = 1 unité).
        private static Rect Ui(float imguiX, float imguiY, float w, float h, float parentHeight)
        {
            return new Rect(imguiX, parentHeight - (imguiY + h), w, h);
        }

        private void OnActionClicked(string researchId)
        {
            state.StartPreviewResearch(researchId);
            state.CompletePreviewResearchIfReady();
            RefreshAll();
        }

        private void OnFilterClicked(string filter)
        {
            state.SetFilterForProof(filter);
            RebuildCards();
            RefreshFilterHighlights();
            RefreshCardsStates();
        }

        public void RequestClose(string via)
        {
            if (!open) return;
            Hide();
            System.Action<string> handler = CloseRequested;
            if (handler != null) handler(via);
        }

        public void Open()
        {
            if (!built) Build();
            state.EnsureLoaded();
            state.RefreshRunningFromJournal();
            state.CompletePreviewResearchIfReady();
            open = true;
            openedAt = state.NowForUi();
            gameObject.SetActive(true);
            RefreshAll();
        }

        public void Hide()
        {
            open = false;
            gameObject.SetActive(false);
        }

        public void SimulateActionForProof(string researchId)
        {
            OnActionClicked(researchId);
        }

        public void SimulateFilterClickForProof(string filter)
        {
            OnFilterClicked(filter);
        }

        public void RefreshAll()
        {
            RefreshVeilAndZoom();
            RefreshFilterHighlights();
            RefreshCardsStates();
        }

        private void RefreshVeilAndZoom()
        {
            float anim = Mathf.Clamp01((state.NowForUi() - openedAt) / 0.18f);
            if (veil != null)
            {
                veil.color = new Color(
                    LivingHiveResearchSpec.VeilColor.r,
                    LivingHiveResearchSpec.VeilColor.g,
                    LivingHiveResearchSpec.VeilColor.b,
                    LivingHiveResearchSpec.VeilColor.a * anim);
            }
            if (windowRoot != null)
            {
                float zoom = Mathf.Lerp(0.975f, 1f, anim);
                windowRoot.localScale = new Vector3(zoom, zoom, 1f);
            }
        }

        private void RefreshFilterHighlights()
        {
            foreach (KeyValuePair<string, Button> pair in filterButtons)
            {
                bool selected = string.Equals(pair.Key, state.SelectedFilterForProof, System.StringComparison.Ordinal);
                Image visual;
                if (!filterButtonPanels.TryGetValue(pair.Key, out visual)) continue;
                visual.sprite = selected ? LivingHiveMenuVisuals.ButtonActiveSprite() : LivingHiveMenuVisuals.ButtonNormalSprite();
                visual.color = selected
                    ? new Color(0.38f, 0.22f, 0.05f, 0.98f)
                    : new Color(0.05f, 0.04f, 0.025f, 0.94f);
            }
        }

        private void RefreshCardsStates()
        {
            bool portrait = IsPortraitForProof;
            foreach (KeyValuePair<string, ResearchCardVisual> pair in cards)
            {
                ResearchCardVisual card = pair.Value;
                string researchId = pair.Key;
                bool completed = state.StatusForProof(researchId) == LivingHiveResearchState.CardStatus.Completed;
                bool running = state.StatusForProof(researchId) == LivingHiveResearchState.CardStatus.Running;

                Color accent = completed
                    ? LivingHiveResearchSpec.CardCompletedAccent
                    : running ? LivingHiveResearchSpec.CardRunningAccent : LivingHiveResearchSpec.CardNormalAccent;
                card.Panel.color = completed
                    ? new Color(0.42f, 0.82f, 0.48f, 0.97f)
                    : running ? new Color(0.12f, 0.10f, 0.20f, 0.97f)
                    : new Color(1f, 1f, 1f, 0.94f);
                card.Title.color = accent;
                if (card.Icon != null) card.Icon.color = accent;

                card.ActionLabel.text = completed ? "Terminée" : running ? "En cours" : "Lancer";
                card.Action.interactable = !completed && !running && string.IsNullOrWhiteSpace(state.ResearchDisabledReasonForProof(researchId));

                string reason = running ? string.Empty : state.ResearchDisabledReasonForProof(researchId);
                card.Reason.text = reason;
                card.Reason.gameObject.SetActive(!running && reason.Length > 0);

                if (card.ProgressRoot != null) card.ProgressRoot.SetActive(running);
                if (card.ProgressFill != null) card.ProgressFill.fillAmount = running ? state.ResearchProgress01() : 0f;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || !built) return;
            if (open)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) RequestClose("escape");
                state.CompletePreviewResearchIfReady();
                RefreshAll();
            }
            if (Screen.width != (int)lastScreenSize.x || Screen.height != (int)lastScreenSize.y)
            {
                lastScreenSize = new Vector2(Screen.width, Screen.height);
                RepositionFullscreen();
            }
        }

        private void RepositionFullscreen()
        {
            bool portrait = IsPortraitForProof;
            float screenW = Screen.width;
            float screenH = Screen.height;
            float bannerH = LivingHiveResearchSpec.BannerHeight(portrait, screenH);
            if (banner != null)
            {
                PositionRect(banner.rectTransform, ScreenRectToUiRect(new Rect(0f, 0f, screenW, bannerH)));
            }
            if (titleText != null) TitleRect(titleText, 28f, bannerH - (portrait ? 42f : 48f));
            if (subtitleText != null) TitleRect(subtitleText, 30f, bannerH - (portrait ? 20f : 23f));
            if (separator != null)
            {
                PositionRect(separator.rectTransform, ScreenRectToUiRect(new Rect(0f, bannerH - 1f, screenW, 1f)));
            }
            if (filterPanel != null)
            {
                PositionRect(filterPanel.GetComponent<RectTransform>(), ScreenRectToUiRect(LivingHiveResearchSpec.FilterRailRect(portrait, screenW, screenH)));
            }
            if (contentPanel != null)
            {
                PositionRect(contentPanel.GetComponent<RectTransform>(), ScreenRectToUiRect(LivingHiveResearchSpec.ContentRect(portrait, screenW, screenH)));
            }
            RebuildCards();
            RefreshAll();
        }

        // --- Helpers uGUI ---

        private Sprite BannerSprite()
        {
            Texture2D tex = Resources.Load<Texture2D>(BannerResource);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        private static Sprite IconSprite(string iconId)
        {
            return LivingHiveMenuVisuals.IconSprite(iconId);
        }

        private static RectTransform NewRect(Transform parent, string name, Rect uiRect)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(uiRect.x, uiRect.y);
            rect.sizeDelta = new Vector2(uiRect.width, uiRect.height);
            return rect;
        }

        private static Image NewImage(Transform parent, string name, Sprite sprite)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            return image;
        }

        // Meme passe "netteté" que LivingHiveMenuCanvas.CreateLabel (voir son commentaire) -
        // legacy Text/LegacyRuntime.ttf remplace par TextMeshProUGUI.
        private static TextMeshProUGUI CreateLabel(Transform parent, string text, int size, TextAnchor align, bool bold = false)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.font = HudFont();
            t.fontSize = size;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.color = Color.white;
            t.alignment = ToTmpAlignment(align);
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private static TMP_FontAsset cachedHudFont;

        // Meme police + meme contour materiau-partage que LivingHiveMenuCanvas.HudFont (voir
        // son commentaire pour pourquoi le contour ne peut pas passer par
        // TMP_Text.outlineWidth par instance ici - ce fichier construit aussi ses labels sous
        // des parents SetActive(false) par endroits).
        private static TMP_FontAsset HudFont()
        {
            if (cachedHudFont != null) return cachedHudFont;
            // Meme police que LivingHiveMenuCanvas.HudFont (voir son commentaire) -
            // Cinzel Regular.
            cachedHudFont = Resources.Load<TMP_FontAsset>("Cinzel-Regular SDF");
            if (cachedHudFont == null) return TMP_Settings.defaultFontAsset;
            if (cachedHudFont.material != null)
            {
                cachedHudFont.material.SetFloat("_OutlineWidth", 0.06f);
                cachedHudFont.material.SetColor("_OutlineColor", new Color32(18, 12, 4, 190));
            }
            return cachedHudFont;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Left;
            }
        }

        private Button NewButton(RectTransform parent, string name, string label)
        {
            Image bg = NewImage(parent, name, LivingHiveMenuVisuals.ButtonNormalSprite());
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;
            TextMeshProUGUI text = CreateLabel(bg.rectTransform, label, 18, TextAnchor.MiddleCenter, true);
            text.raycastTarget = false;
            FillRect(text.rectTransform);
            Button button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static void PositionRect(RectTransform rect, Rect uiRect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(uiRect.x, uiRect.y);
            rect.sizeDelta = new Vector2(uiRect.width, uiRect.height);
        }

        private static void FillRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void TitleRect(Component target, float x, float y)
        {
            RectTransform rect = target.transform as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(200f, 26f);
        }

        private static Rect ScreenRectToUiRect(Rect imguiRect)
        {
            return new Rect(
                imguiRect.x,
                Screen.height - (imguiRect.y + imguiRect.height),
                imguiRect.width,
                imguiRect.height);
        }

        private sealed class ResearchCardVisual
        {
            public GameObject Root;
            public Image Panel;
            public Image Icon;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Summary;
            public TextMeshProUGUI Detail;
            public TextMeshProUGUI ActionLabel;
            public Button Action;
            public TextMeshProUGUI Reason;
            public GameObject ProgressRoot;
            public Image ProgressFill;
        }
    }
}