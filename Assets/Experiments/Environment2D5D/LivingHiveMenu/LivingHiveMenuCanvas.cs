using System.Collections.Generic;
using BeeKingdom.Core.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeeKingdom.LivingHiveMenu
{
    // VUE uGUI AUTONOME du menu inférieur LivingHive pour la scène Environment2D5D_SpatialV3.
    //
    // Construit au runtime : un Canvas Screen Space Overlay, le rail du bas (5 boutons en
    // portrait, 10 en paysage, ordre et libellés conformes au monolithe), et les panneaux
    // fonctionnels ouverts par le rail (Quêtes, Sac, Plus, Paramètres, Chat) plus l'overlay
    // Carte (changement de surface Ruche <-> Carte). Aucun scène/existant n'est touché.
    //
    // Le composant est testable hors play-mode : Build() crée l'hiérarchie, RefreshPanels()
    // recharge entièrement l'état depuis LivingHiveMenuState (pur C#).
    public sealed class LivingHiveMenuCanvas : MonoBehaviour
    {
        // Mirrors SplashDevelopmentSceneConfig.WorldMapScenePath / Wave5PremiumMapModeKey
        // (Assets/BeeKingdom/Playground/SplashDevelopmentSceneConfig.cs). Duplicated as
        // constants rather than referenced directly: that type lives in the default
        // Assembly-CSharp assembly, which no .asmdef — including this package's
        // BeeKingdom.LivingHiveMenu — is allowed to reference.
        private const string WorldMapScenePath = "Assets/Scenes/WorldMapWave6Wave5Method12288Preview.unity";
        private const string Wave5PremiumMapModeKey = "BeeKingdom.Dev.WorldMapMode.Wave5Premium25x25";

        private readonly LivingHiveMenuState state = new LivingHiveMenuState();

        private Canvas canvas;
        private RectTransform railRoot;
        private readonly Dictionary<string, Button> railButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, RailButtonVisual> railVisuals = new Dictionary<string, RailButtonVisual>();
        private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, PanelAnimState> panelAnimStates = new Dictionary<string, PanelAnimState>();
        private Vector2 lastScreenSize;

        // Etat de fondu d'un panneau - voir BuildPanel/RefreshPanels/TickPanelAnimations.
        private sealed class PanelAnimState
        {
            public GameObject Root;
            public CanvasGroup Group;
            public bool Closing;
            public float StartedAt;
        }

        // --- Header supérieur (Reine / ressources / Boutique) ---
        private GameObject headerRoot;
        private bool headerBuilt;
        private bool queenProfileOpen;
        private bool shopOpen;
        private readonly Dictionary<string, HeaderChipVisual> headerChipVisuals = new Dictionary<string, HeaderChipVisual>();

        // ID des éléments Header exposés ForProof.
        public const string HeaderQueenElementId = "Queen";
        public const string HeaderShopElementId = "Shop";

        // ID logiques des ressources (3 portrait / 5 paysage).
        private static readonly string[] PortraitResourceIds = { "honey", "wax", "pollen" };
        private static readonly string[] LandscapeResourceIds = { "honey", "wax", "pollen", "bees", "capacity" };

        private sealed class HeaderChipVisual
        {
            public Image Panel;
            public Image Icon;
            public TextMeshProUGUI Value;

            // Animation de comptage ("roll-up") - voir RefreshHeader. DisplayedValue est la
            // valeur actuellement affichee (interpolee), TargetValue la derniere valeur reelle
            // connue ; RollUpStartValue/RollUpStartedAt bornent l'interpolation en cours.
            public float DisplayedValue = float.NaN;
            public float TargetValue;
            public float RollUpStartValue;
            public float RollUpStartedAt;
        }

        // Références nécessaires à RefreshRailHighlights pour reproduire l'état actif
        // (DrawIconButton) : fond premium (normal/actif), glow, socket, icône et libellé.
        private sealed class RailButtonVisual
        {
            public Image Panel;
            public Image Glow;
            public Image Socket;
            public Image Icon;
            public TextMeshProUGUI Label;
            public GameObject HeaderBand;
            public GameObject ProgressLine;
        }

        public LivingHiveMenuState State => state;

        public int RailButtonCount => railButtons.Count;

        public bool IsRailBuilt => railRoot != null;

        public bool PanelShown(string panelId)
        {
            GameObject panel;
            return panels.TryGetValue(panelId, out panel) && panel != null && panel.activeSelf;
        }

        public bool IsHeaderBuilt => headerBuilt;

        public bool QueenProfileShown => queenProfileOpen;

        public bool ShopShown => shopOpen;

        // Nombre de ressources affichées dans le Header selon l'orientation courante.
        public int HeaderResourceChipCount { get; private set; }

        // Valeur de ressource actuellement affichée pour une ressource donnée (ForProof).
        public string HeaderResourceValue(string resourceId)
        {
            HeaderChipVisual v;
            if (!headerChipVisuals.TryGetValue(resourceId, out v) || v.Value == null) return string.Empty;
            return v.Value.text;
        }

        public string ActiveRailItemForProof
        {
            get
            {
                if (state.ActivitiesOpen || LivingHiveActivitiesBridge.IsOpen) return LivingHiveMenuSpec.ActivitiesId;
                if (state.CommunicationOpen) return LivingHiveMenuSpec.CommunicationId;
                if (state.IsMoreActiveForProof() || LivingHiveSettingsBridge.IsOpen) return LivingHiveMenuSpec.MoreId;
                if (string.IsNullOrEmpty(state.ActiveMenuId)) return string.Empty;
                return state.ActiveMenuId;
            }
        }

        private GraphicRaycaster raycaster;

        // Lets a full-screen IMGUI overlay drawn on top of this canvas (Alliance/
        // Communication/Barrack, see HiveMapOverlayInputGateBootstrap) block clicks from
        // reaching the rail/header underneath. IMGUI draws are visual only - they never
        // stop uGUI's own EventSystem raycasts, so without this a click meant for the
        // overlay's own close button also fires whatever uGUI element (or, separately, 3D
        // building collider - see BuildingInteractionController.IsEnabled) happens to sit
        // at that same screen position.
        public void SetInputBlocked(bool blocked)
        {
            if (raycaster != null) raycaster.enabled = !blocked;
        }

        public void Build()
        {
            if (canvas != null) return;
            state.LoadFromPlayerPrefs();

            GameObject canvasObj = new GameObject("LivingHiveMenu");
            canvasObj.transform.SetParent(transform, false);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // PAS de CanvasScaler : le monolithe affiche des rects en pixels écran absolus
            // (IMGUI). Un scaler avec référence 800x600 par défaut étirerait ces pixels et
            // chasserait le rail de la zone visible. L'overlay sans scaler mappe
            // 1 unité uGUI = 1 pixel écran, reproduisant fidèlement la géométrie du
            // monolithe ; le relayout sur changement de résolution gère les réglages.
            raycaster = canvasObj.AddComponent<GraphicRaycaster>();

            BuildRail(canvasObj.transform);
            BuildHeader(canvasObj.transform);
            BuildPanels(canvasObj.transform);
            RefreshAll();
            lastScreenSize = new Vector2(Screen.width, Screen.height);
        }

        private const float HeaderLiveRefreshIntervalSeconds = 0.5f;
        private float headerLiveRefreshLastAt = -100f;

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (canvas == null) return;
            if (Screen.width != (int)lastScreenSize.x || Screen.height != (int)lastScreenSize.y)
            {
                lastScreenSize = new Vector2(Screen.width, Screen.height);
                RebuildRail();
                RebuildHeader();
                RepositionHeaderOverlayPanels();
                RefreshAll();
                return;
            }

            // LivingHiveMenuHeaderData's resource values are pushed from outside
            // (HiveMapResourceHudBootstrap, Assembly-CSharp) whenever they change server-
            // side/via a collection - this package has no way to be notified directly (see
            // the cross-assembly constraint noted at the top of this file), so it just
            // polls its own already-cheap RefreshHeader() every frame instead (needed for the
            // roll-up count animation below to actually animate smoothly, not just jump once
            // every 0.5s).
            RefreshHeader();
            TickPanelAnimations();

            if (Time.unscaledTime - headerLiveRefreshLastAt >= HeaderLiveRefreshIntervalSeconds)
            {
                headerLiveRefreshLastAt = Time.unscaledTime;
                // The real Settings overlay (IMGUI) can close itself (its own back button)
                // without going through OnMoreRowClicked, which is the only other place the
                // rail's "More" highlight gets refreshed - poll it here so the glow doesn't
                // stay stuck on after that happens.
                RefreshRailHighlights();
            }
        }

        // --- Rail du bas (géométrie pixel écran du monolithe, 1 unité uGUI = 1 pixel) ---

        private void BuildRail(Transform parent)
        {
            bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
            Rect rail = ScreenRectToUiRect(LivingHiveMenuSpec.RailRectForProof(portrait, Screen.width, Screen.height));

            // Fond premium (miroir DrawPremiumPanel du rail) : géométrie inchangée, seul le
            // rendu change (couleur plate -> sprite 9-slice grain/bordure/coins).
            GameObject bg = new GameObject("RailBackdrop");
            bg.transform.SetParent(parent, false);
            bg.transform.SetSiblingIndex(0);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.sprite = LivingHiveMenuVisuals.RailBackdropSprite();
            bgImage.type = Image.Type.Sliced;
            // Transparence réelle du monolithe (DrawBottomRail : fill alpha 0.84). RGB
            // inchangé (sprites premium intacts) : seuls les sprites restent tels quels,
            // l'alpha global est imposé par Image.color pour laisser la scène transparaître.
            bgImage.color = new Color(1f, 1f, 1f, LivingHiveMenuVisuals.RailFill.a);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            PositionRect(bgRect, rail);

            railRoot = bgRect;

            // Ornement du rail (miroir DrawBottomRailOnnament) : bande fine étirée sur toute
            // la largeur, superposée au fond.
            GameObject ornament = new GameObject("RailOrnament");
            ornament.transform.SetParent(parent, false);
            Image ornamentImage = ornament.AddComponent<Image>();
            ornamentImage.sprite = LivingHiveMenuVisuals.RailOrnamentSprite();
            ornamentImage.type = Image.Type.Simple;
            ornamentImage.raycastTarget = false;
            PositionRect(ornament.GetComponent<RectTransform>(), rail);

            LivingHiveMenuEntry[] entries = LivingHiveMenuSpec.Entries(portrait);
            Rect[] itemRects = LivingHiveMenuSpec.ItemRects(portrait, Screen.width, Screen.height);
            for (int i = 0; i < entries.Length && i < itemRects.Length; i++)
            {
                if (i > 0) BuildDivider(parent, itemRects[i - 1], itemRects[i], rail);
                TravelTo(parent, entries[i], itemRects[i]);
            }
        }

        // Miroir de DrawRailDivider : un liseré fin entre deux boutons consécutifs, positionné
        // au milieu de l'écart (identique à l'appel monolithe `item.x - gap * 0.5f - 1f`).
        private void BuildDivider(Transform parent, Rect prevImguiRect, Rect currImguiRect, Rect railUiRect)
        {
            GameObject go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = LivingHiveMenuVisuals.DividerSprite();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;

            float gap = currImguiRect.x - prevImguiRect.xMax;
            float dividerX = prevImguiRect.xMax + gap * 0.5f - 1f;
            Rect imguiDivider = new Rect(dividerX, currImguiRect.y + 6f, 2f, currImguiRect.height - 12f);
            Rect uiDivider = ScreenRectToUiRect(imguiDivider);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(uiDivider.x, uiDivider.y);
            rect.sizeDelta = new Vector2(uiDivider.width, uiDivider.height);
        }

        private void RebuildRail()
        {
            if (canvas == null) return;
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = canvas.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("RailBackdrop", System.StringComparison.Ordinal)
                    || child.name.StartsWith("RailOrnament", System.StringComparison.Ordinal)
                    || child.name.StartsWith("Divider", System.StringComparison.Ordinal)
                    || child.name.StartsWith("Button_", System.StringComparison.Ordinal))
                {
                    if (Application.isPlaying) Object.Destroy(child);
                    else Object.DestroyImmediate(child);
                }
            }
            railButtons.Clear();
            railVisuals.Clear();
            railRoot = null;
            BuildRail(canvas.transform);
        }

        // --- Header supérieur (miroir de DrawPortraitTopHud / DrawStrategyTopHud du monolithe) ---

        private void BuildHeader(Transform parent)
        {
            headerChipVisuals.Clear();
            HeaderResourceChipCount = 0;
            bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
            float w = Screen.width, h = Screen.height;
            Rect headerRect = ScreenRectToUiRect(portrait
                ? LivingHiveMenuHeaderData.PortraitHeaderRect(w, h)
                : LivingHiveMenuHeaderData.LandscapeHeaderRect(w, h));

            // Fond premium sombre du Header (direction artistique : noir profond neutre,
            // doré réservé aux accents, transparence réelle comme le rail).
            GameObject bg = new GameObject("HeaderBackdrop");
            bg.transform.SetParent(parent, false);
            bg.transform.SetSiblingIndex(1);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.sprite = LivingHiveMenuVisuals.RailBackdropSprite();
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(1f, 1f, 1f, 0.84f);
            bgImage.raycastTarget = false;
            PositionRect(bg.GetComponent<RectTransform>(), headerRect);

            BuildQueenButton(parent, portrait, w, h);
            BuildResourceChips(parent, portrait, w, h);
            BuildRoyalJellyChip(parent, portrait, w, h);
            BuildShopButton(parent, portrait, w, h);

            headerRoot = bg;
            headerBuilt = true;
        }

        private void BuildQueenButton(Transform parent, bool portrait, float w, float h)
        {
            Rect imgui = portrait
                ? LivingHiveMenuHeaderData.PortraitQueenRect(w, h)
                : LivingHiveMenuHeaderData.LandscapeQueenRect(w, h);
            Rect ui = ScreenRectToUiRect(imgui);
            RectTransform container = NewContainer(parent, "HeaderQueen", ui);

            Image panel = container.gameObject.AddComponent<Image>();
            panel.sprite = LivingHiveMenuVisuals.ButtonNormalSprite();
            panel.type = Image.Type.Sliced;
            panel.color = new Color(1f, 1f, 1f, LivingHiveMenuVisuals.ButtonNormalFill.a);

            Button button = container.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnHeaderClicked(HeaderQueenElementId));

            // Icône couronne (46 en paysage, 36 en portrait), avec un halo dore doux derriere
            // (SoftRadialGlowSprite, voir son commentaire - meme traitement "premium" que les
            // chips de ressources ci-dessous). Le halo deborde volontairement du conteneur
            // (uGUI ne le decoupe pas sans Mask), c'est voulu : il donne du poids visuel a
            // l'icone sans agrandir la zone cliquable ni toucher la geometrie testee.
            float icon = portrait ? 36f : 46f;
            float glowSize = icon * 1.7f;
            float iconX = (portrait ? 8f : 12f);
            float iconY = (portrait ? 4f : 9f);
            Image glow = NewImage(container, "IconGlow", LivingHiveMenuVisuals.SoftRadialGlowSprite());
            glow.color = new Color(1f, 0.82f, 0.32f, 0.55f);
            glow.raycastTarget = false;
            Rect2Local(glow, iconX + icon * 0.5f - glowSize * 0.5f, iconY + icon * 0.5f - glowSize * 0.5f, glowSize, glowSize);

            Image iconImage = NewImage(container, "Icon", LivingHiveMenuVisuals.IconSprite("queen"));
            iconImage.color = LivingHiveMenuVisuals.IconTintActive;
            iconImage.raycastTarget = false;
            Rect2Local(iconImage, iconX, iconY, icon, icon);

            // Nom "Reine" + niveau.
            TextMeshProUGUI name = CreateLabel(container, "Reine", portrait ? 15 : 18, TextAnchor.MiddleLeft, true);
            name.color = new Color(1f, 0.90f, 0.58f);
            Rect2Local(name, (portrait ? 52f : 70f), (portrait ? 4f : 6f), 90f, 20f);

            TextMeshProUGUI level = CreateLabel(container, "Niv. " + LivingHiveMenuHeaderData.PreviewQueenLevel.ToString(),
                portrait ? 11 : 12, TextAnchor.MiddleLeft);
            level.color = LivingHiveMenuVisuals.LabelInactiveColor;
            Rect2Local(level, (portrait ? 52f : 70f), portrait ? 24f : 28f, 80f, 16f);

            // Barre de progression vers le niveau suivant (paysage uniquement).
            if (!portrait)
            {
                Image progress = NewImage(container, "LevelProgress", LivingHiveMenuVisuals.ProgressFillSprite());
                progress.color = new Color(1f, 0.72f, 0.16f, 0.76f);
                progress.raycastTarget = false;
                Rect2Local(progress, 70f, 50f, 96f, 4f);
            }
        }

        private void BuildResourceChips(Transform parent, bool portrait, float w, float h)
        {
            string[] ids = portrait ? PortraitResourceIds : LandscapeResourceIds;
            Rect[] rects = portrait
                ? LivingHiveMenuHeaderData.PortraitResourceChipRects(w, h)
                : LivingHiveMenuHeaderData.LandscapeResourceRects(w, h);
            HeaderResourceChipCount = ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                BuildOneResourceChip(parent, ids[i], ScreenRectToUiRect(rects[i]), portrait, "HeaderChip_" + ids[i]);
            }
        }

        // Gelee Royale (monnaie premium) : sa propre pastille additive juste avant la
        // Boutique - voir LandscapeRoyalJellyRect/PortraitRoyalJellyRect pour pourquoi ce
        // n'est pas un 6e element de BuildResourceChips. Reutilise le meme chip visuel que
        // les ressources en vrac (meme dictionnaire headerChipVisuals => RefreshHeader() le
        // met a jour automatiquement, HeaderResourceValue("royalJelly") fonctionne aussi
        // gratuitement), avec en plus un petit bouton "+" qui ouvre la Boutique - aucune
        // logique d'achat reelle n'existe encore, ce n'est qu'une redirection.
        private void BuildRoyalJellyChip(Transform parent, bool portrait, float w, float h)
        {
            Rect imgui = portrait
                ? LivingHiveMenuHeaderData.PortraitRoyalJellyRect(w, h)
                : LivingHiveMenuHeaderData.LandscapeRoyalJellyRect(w, h);
            RectTransform container = BuildOneResourceChip(parent, "royalJelly", ScreenRectToUiRect(imgui), portrait, "HeaderRoyalJelly");

            float plusSize = portrait ? 20f : 24f;
            RectTransform plusContainer = NewContainer(container, "PlusButton", new Rect(container.sizeDelta.x - plusSize - 4f, (container.sizeDelta.y - plusSize) * 0.5f, plusSize, plusSize));
            Image plusPanel = plusContainer.gameObject.AddComponent<Image>();
            plusPanel.sprite = LivingHiveMenuVisuals.ButtonActiveSprite();
            plusPanel.type = Image.Type.Sliced;
            plusPanel.color = new Color(1f, 1f, 1f, LivingHiveMenuVisuals.ButtonActiveFill.a);
            Button plusButton = plusContainer.gameObject.AddComponent<Button>();
            plusButton.targetGraphic = plusPanel;
            plusButton.transition = Selectable.Transition.None;
            plusButton.onClick.AddListener(() => OnHeaderClicked(HeaderShopElementId));

            TextMeshProUGUI plusLabel = CreateLabel(plusContainer, "+", portrait ? 14 : 16, TextAnchor.MiddleCenter, true);
            plusLabel.color = new Color(1f, 0.9f, 0.4f);
            Rect2Local(plusLabel, 0f, 0f, plusSize, plusSize);
        }

        // Corps partage d'une pastille de ressource : fond premium, icone + halo dore doux
        // derriere (SoftRadialGlowSprite, teinte a l'accent de la ressource - real art comme
        // fallback procedural en beneficient tous les deux, contrairement au tint plat qui ne
        // s'appliquait qu'au fallback), valeur, libelle. Retourne le conteneur pour permettre
        // d'y ajouter un element de plus (voir BuildRoyalJellyChip's "+" button).
        private RectTransform BuildOneResourceChip(Transform parent, string id, Rect ui, bool portrait, string containerName)
        {
            RectTransform container = NewContainer(parent, containerName, ui);

            Image panel = container.gameObject.AddComponent<Image>();
            panel.sprite = LivingHiveMenuVisuals.ButtonNormalSprite();
            panel.type = Image.Type.Sliced;
            panel.color = new Color(1f, 1f, 1f, 0.7f);

            Color accent = LivingHiveMenuHeaderData.ResourceAccent(id);
            // Jeff (2026-08-19): l'icone se suffit a elle-meme - plus besoin du libelle texte
            // ("Miel"/"Cire"/...) qui vivait sous la valeur. Sans lui a caser, l'icone et la
            // valeur prennent toute la hauteur de la pastille au lieu de se partager 2 rangees.
            float iconSize = Mathf.Min(ui.height - 6f, portrait ? 30f : 36f);
            float iconX = 6f;
            float iconY = (ui.height - iconSize) * 0.5f;

            float glowSize = iconSize * 1.6f;
            Image glow = NewImage(container, "IconGlow", LivingHiveMenuVisuals.SoftRadialGlowSprite());
            glow.color = new Color(accent.r, accent.g, accent.b, 0.45f);
            glow.raycastTarget = false;
            Rect2Local(glow, iconX + iconSize * 0.5f - glowSize * 0.5f, iconY + iconSize * 0.5f - glowSize * 0.5f, glowSize, glowSize);

            Image icon = NewImage(container, "Icon", LivingHiveMenuVisuals.IconSprite(id));
            // Real painted art (PremiumBeeIcons/honey, wax, pollen, ...) already has its
            // own color - only the procedural hex-badge fallback needs the accent tint
            // to read as a distinct resource.
            icon.color = LivingHiveMenuVisuals.IconIsRealArt(id) ? Color.white : accent;
            icon.raycastTarget = false;
            Rect2Local(icon, iconX, iconY, iconSize, iconSize);

            float valueX = iconX + iconSize + 6f;
            TextMeshProUGUI value = CreateLabel(container, "0", portrait ? 15 : 16, TextAnchor.MiddleRight, true);
            value.color = new Color(0.96f, 0.94f, 0.86f);
            Rect2Local(value, valueX, 0f, ui.width - valueX - 6f, ui.height);

            headerChipVisuals[id] = new HeaderChipVisual { Panel = panel, Icon = icon, Value = value };
            return container;
        }

        private void BuildShopButton(Transform parent, bool portrait, float w, float h)
        {
            Rect imgui = portrait
                ? LivingHiveMenuHeaderData.PortraitShopRect(w, h)
                : LivingHiveMenuHeaderData.LandscapeShopRect(w, h);
            Rect ui = ScreenRectToUiRect(imgui);
            RectTransform container = NewContainer(parent, "HeaderShop", ui);

            Image panel = container.gameObject.AddComponent<Image>();
            panel.sprite = LivingHiveMenuVisuals.ButtonNormalSprite();
            panel.type = Image.Type.Sliced;
            panel.color = new Color(1f, 1f, 1f, LivingHiveMenuVisuals.ButtonNormalFill.a);

            Button button = container.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnHeaderClicked(HeaderShopElementId));

            Image icon = NewImage(container, "Icon", LivingHiveMenuVisuals.IconSprite("shop"));
            icon.color = new Color(1f, 0.90f, 0.58f);
            icon.raycastTarget = false;
            float iconSize = portrait ? 26f : 30f;
            Rect2Local(icon, (ui.width - iconSize) * 0.5f, (ui.height - iconSize) * 0.5f, iconSize, iconSize);
        }

        private void OnHeaderClicked(string elementId)
        {
            if (elementId == HeaderQueenElementId)
            {
                queenProfileOpen = !queenProfileOpen;
            }
            else if (elementId == HeaderShopElementId)
            {
                shopOpen = !shopOpen;
            }
            RefreshAll();
        }

        public void SimulateHeaderClick(string elementId)
        {
            OnHeaderClicked(elementId);
        }

        private void RebuildHeader()
        {
            if (canvas == null) return;
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = canvas.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("Header", System.StringComparison.Ordinal)
                    || child.name.StartsWith("QueenProfilePanel", System.StringComparison.Ordinal)
                    || child.name.StartsWith("ShopPanel", System.StringComparison.Ordinal))
                {
                    if (Application.isPlaying) Object.Destroy(child);
                    else Object.DestroyImmediate(child);
                }
            }
            headerRoot = null;
            headerBuilt = false;
            BuildHeader(canvas.transform);
        }

        // Jeff (2026-08-19, "fluidite"): les valeurs sautaient directement au nouveau nombre.
        // Chaque chip (sauf "capacity", composee de 2 nombres - snap conserve) anime
        // maintenant de l'ancien affichage vers le nouveau sur RollUpDurationSeconds des que
        // la vraie valeur (PreviewValue) change, au lieu d'un saut instantane.
        private const float RollUpDurationSeconds = 0.5f;

        private void RefreshHeader()
        {
            float now = Time.unscaledTime;
            foreach (KeyValuePair<string, HeaderChipVisual> pair in headerChipVisuals)
            {
                HeaderChipVisual v = pair.Value;
                if (string.Equals(pair.Key, "capacity", System.StringComparison.Ordinal))
                {
                    v.Value.text = LivingHiveMenuHeaderData.FormatResource(LivingHiveMenuHeaderData.PreviewCapacityUsed)
                        + "/" + LivingHiveMenuHeaderData.FormatResource(LivingHiveMenuHeaderData.PreviewCapacityMax);
                    continue;
                }

                int target = LivingHiveMenuHeaderData.PreviewValue(pair.Key);
                if (float.IsNaN(v.DisplayedValue))
                {
                    // Premier passage (juste apres Build) - pas d'animation a jouer depuis rien.
                    v.DisplayedValue = target;
                    v.TargetValue = target;
                }
                else if (!Mathf.Approximately(target, v.TargetValue))
                {
                    v.RollUpStartValue = v.DisplayedValue;
                    v.TargetValue = target;
                    v.RollUpStartedAt = now;
                }

                float t = Mathf.Clamp01((now - v.RollUpStartedAt) / RollUpDurationSeconds);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                v.DisplayedValue = Mathf.Lerp(v.RollUpStartValue, v.TargetValue, eased);
                v.Value.text = LivingHiveMenuHeaderData.FormatResource(Mathf.RoundToInt(v.DisplayedValue));
            }
        }

        private static RectTransform NewContainer(Transform parent, string name, Rect uiRect)
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

        private static Image NewImage(RectTransform parent, string name, Sprite sprite)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            return image;
        }

        private static void Rect2Local(Component target, float x, float y, float w, float h)
        {
            RectTransform rect = target.transform as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }

        private void TravelTo(Transform parent, LivingHiveMenuEntry entry, Rect imguiScreenRect)
        {
            GameObject go = new GameObject("Button_" + entry.ItemId);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = LivingHiveMenuVisuals.ButtonNormalSprite();
            image.type = Image.Type.Sliced;
            // Transparence réelle du monolithe (DrawIconButton : fill idle alpha 0.78).
            // RGB inchangé ; RefreshRailHighlights impose l'alpha actif (0.94) si besoin.
            image.color = new Color(1f, 1f, 1f, LivingHiveMenuVisuals.ButtonNormalFill.a);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            // Le sprite premium sert déjà de feedback visuel (RefreshRailHighlights) ; la
            // transition de couleur par défaut du Button assombrirait le rendu par-dessus.
            button.transition = Selectable.Transition.None;

            // Convertit le rect IMGUI (origine haut-gauche) en anchoredPosition uGUI
            // (origine bas-gauche, 1 unité = 1 pixel) : ancré sur le canvas, pas sur un
            // backdrop — le système de coordonnées n'est plus relativisé/étiré par erreur.
            Rect uiRect = ScreenRectToUiRect(imguiScreenRect);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(uiRect.x, uiRect.y);
            rect.sizeDelta = new Vector2(uiRect.width, uiRect.height);

            float bw = uiRect.width;
            float bh = uiRect.height;

            // Bandeau doré actif (DrawPremiumHeaderBand), masqué par défaut.
            GameObject headerBand = new GameObject("HeaderBand");
            headerBand.transform.SetParent(go.transform, false);
            Image headerImage = headerBand.AddComponent<Image>();
            headerImage.sprite = LivingHiveMenuVisuals.HeaderBandSprite();
            headerImage.raycastTarget = false;
            rect2(headerBand, 6f, bh - Mathf.Max(8f, bh * 0.34f) - 5f, Mathf.Max(1f, bw - 12f), Mathf.Max(8f, bh * 0.34f));
            headerBand.SetActive(false);

            // Icône : glow (derrière) + socket hexagonal + icône elle-même, insetée (miroir DrawMenuIcon).
            float iconSize = Mathf.Min(74f, bh * 0.76f);
            float iconX = bw * 0.5f - iconSize * 0.5f;
            float iconY = Mathf.Max(2f, bh - 2f - iconSize);

            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            Image glowImage = glow.AddComponent<Image>();
            glowImage.sprite = LivingHiveMenuVisuals.GlowInactiveSprite();
            glowImage.color = LivingHiveMenuVisuals.GlowTintInactive;
            glowImage.raycastTarget = false;
            float glowInset = iconSize * 0.22f;
            rect2(glow, iconX - glowInset, iconY - glowInset, iconSize + glowInset * 2f, iconSize + glowInset * 2f);

            GameObject socket = new GameObject("Socket");
            socket.transform.SetParent(go.transform, false);
            Image socketImage = socket.AddComponent<Image>();
            socketImage.sprite = LivingHiveMenuVisuals.IconSocketSprite();
            socketImage.color = LivingHiveMenuVisuals.SocketTintInactive;
            socketImage.raycastTarget = false;
            rect2(socket, iconX, iconY, iconSize, iconSize);

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(go.transform, false);
            Image iconImage = icon.AddComponent<Image>();
            iconImage.sprite = LivingHiveMenuVisuals.IconSprite(LivingHiveMenuSpec.NavIconId(entry.IconKey));
            iconImage.color = LivingHiveMenuVisuals.IconTintInactive;
            iconImage.raycastTarget = false;
            float iconInset = iconSize * 0.14f;
            rect2(icon, iconX + iconInset, iconY + iconInset, iconSize - iconInset * 2f, iconSize - iconInset * 2f);

            // Barre de progression lumineuse sous le bouton actif.
            GameObject progressLine = new GameObject("ProgressLine");
            progressLine.transform.SetParent(go.transform, false);
            Image progressImage = progressLine.AddComponent<Image>();
            progressImage.sprite = LivingHiveMenuVisuals.ProgressFillSprite();
            progressImage.color = new Color(1f, 0.72f, 0.16f, 0.76f);
            progressImage.raycastTarget = false;
            rect2(progressLine, bw * 0.22f, 3f, bw * 0.56f, 2f);
            progressLine.SetActive(false);

            // Libellé en bande basse (taille 11, conforme au monolithe — DrawIconButton labelStyle).
            TextMeshProUGUI label = CreateLabel(go.transform, entry.Label, 11, TextAnchor.MiddleCenter);
            label.color = LivingHiveMenuVisuals.LabelInactiveColor;
            rect2(label.gameObject, 2f, 2f, Mathf.Max(1f, bw - 4f), 20f);

            LivingHiveMenuEntry capture = entry;
            button.onClick.AddListener(() => OnEntryClicked(capture.ItemId));
            railButtons[entry.ItemId] = button;
            railVisuals[entry.ItemId] = new RailButtonVisual
            {
                Panel = image,
                Glow = glowImage,
                Socket = socketImage,
                Icon = iconImage,
                Label = label,
                HeaderBand = headerBand,
                ProgressLine = progressLine
            };
        }

        // Convertit un rect IMGUI (origine haut-gauche) en rect uGUI (origine bas-gauche),
        // mêmes pixels -> même position physique à l'écran.
        private static Rect ScreenRectToUiRect(Rect imguiRect)
        {
            return new Rect(
                imguiRect.x,
                Screen.height - (imguiRect.y + imguiRect.height),
                imguiRect.width,
                imguiRect.height);
        }

        private static void PositionRect(RectTransform rect, Rect r)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(r.x, r.y);
            rect.sizeDelta = new Vector2(r.width, r.height);
        }

        private static void FillRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnEntryClicked(string itemId)
        {
            if (LivingHiveMenuSpec.IsSurfaceSwitch(itemId))
            {
                OpenWorldMap();
                return;
            }
            if (LivingHiveMenuSpec.IsCommunication(itemId))
            {
                // Toggles HiveViewProductUiPresenter's own mini-chat / "CHAT ROYAL" IMGUI
                // overlay (see LivingHiveChatBridge.ToggleOverlay + LivingHiveChatBridgeBootstrap
                // .OnGUI) — the exact widget SandboxPlayground's Communication button opens,
                // not a uGUI panel built here.
                LivingHiveChatBridge.ToggleOverlay();
                return;
            }
            if (LivingHiveMenuSpec.IsActivities(itemId))
            {
                LivingHiveActivitiesBridge.OpenOverlay();
                return;
            }
            state.ToggleEntry(itemId);
            RefreshAll();
        }

        // Mirrors HiveViewProductUiPresenter.OpenCanonicalWorldMap(): CARTE is a hard
        // LoadSceneMode.Single switch to the real world map, not the local "Carte" overlay
        // stub (BuildWorldMapOverlay/state.SurfaceMode below, kept only because
        // LivingHiveMenuState is still directly unit-tested as a standalone state model).
        // Two effects mirrored from the monolith: disable the dev-only Wave5 Premium 25x25
        // test-scene override (a real bug once stranded a player on that debug scene with
        // no way back — see SplashDevelopmentSceneConfig.DisableWave5PremiumMapMode), then
        // load. Guarded by isPlaying: EditMode tests (SimulateEntryClick) have no Play
        // context to load a scene into.
        private static void OpenWorldMap()
        {
            if (!Application.isPlaying) return;
            if (SceneUtility.GetBuildIndexByScenePath(WorldMapScenePath) < 0)
            {
                Debug.LogWarning("[LivingHiveMenu] World map scene not in Build Settings: " + WorldMapScenePath);
                return;
            }
            PlayerPrefs.SetInt(Wave5PremiumMapModeKey, 0);
            PlayerPrefs.Save();
            SceneManager.LoadScene(WorldMapScenePath, LoadSceneMode.Single);
        }

        public void SimulateEntryClick(string itemId)
        {
            OnEntryClicked(itemId);
        }

        public void SimulateMoreRowClick(string entry)
        {
            OnMoreRowClicked(entry);
        }

        // --- Panneaux fonctionnels ---

        private void BuildPanels(Transform parent)
        {
            BuildPanel(parent, LivingHiveMenuSpec.ActivitiesId, "Activites", BuildActivitiesContent, new Rect(10f, 96f, 520f, 460f));
            // Communication no longer builds a uGUI panel here: it toggles
            // HiveViewProductUiPresenter's own IMGUI mini-chat overlay instead (see
            // OnEntryClicked / LivingHiveChatBridge.ToggleOverlay).
            BuildPanel(parent, LivingHiveMenuSpec.BagId, "Sac & stocks", BuildBagContent, new Rect(10f, 96f, 520f, 460f));
            BuildPanel(parent, LivingHiveMenuSpec.MoreId, "Plus", BuildMoreContent, new Rect(10f, 96f, 420f, 520f));
            // Settings ("Parametres") no longer builds a uGUI panel here: it toggles
            // HiveViewProductUiPresenter's real Settings overlay instead (see
            // OnMoreRowClicked / LivingHiveSettingsBridge.ToggleOverlay).
            BuildPanel(parent, "Carte", "Carte du monde", BuildWorldMapOverlay, new Rect(10f, 96f, 720f, 540f));
            // Reine/Boutique : contrairement aux panneaux ci-dessus (déjà en coordonnées
            // uGUI bas-gauche constantes), QueenProfilePanelRect/ShopPanelRect sont des rects
            // IMGUI haut-gauche (miroir DrawPortraitTopHud/DrawStrategyTopHud, comme tout le
            // reste du Header) — il faut donc les convertir via ScreenRectToUiRect avant de
            // les traiter comme un anchoredPosition, comme BuildHeader le fait déjà partout.
            BuildPanel(parent, "QueenProfile", "Reine", BuildQueenProfileContent, ScreenRectToUiRect(QueenProfileImguiRect()));
            BuildPanel(parent, "Shop", "Boutique", BuildShopContent, ScreenRectToUiRect(ShopImguiRect()));
        }

        private static Rect QueenProfileImguiRect()
        {
            bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
            return LivingHiveMenuHeaderData.QueenProfilePanelRect(portrait, Screen.width, Screen.height);
        }

        private static Rect ShopImguiRect()
        {
            bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
            return LivingHiveMenuHeaderData.ShopPanelRect(portrait, Screen.width, Screen.height);
        }

        // Repositionne uniquement Panel_QueenProfile et Panel_Shop sur leur RectTransform
        // (sans détruire/reconstruire leur contenu — leur taille est constante, seule leur
        // position dépend de la résolution). Appelé au changement de résolution/orientation,
        // en miroir de RebuildRail()/RebuildHeader() mais ciblé sur ces deux panneaux.
        private void RepositionHeaderOverlayPanels()
        {
            RepositionHeaderOverlayPanel("QueenProfile", QueenProfileImguiRect());
            RepositionHeaderOverlayPanel("Shop", ShopImguiRect());
        }

        private void RepositionHeaderOverlayPanel(string panelId, Rect imguiRect)
        {
            GameObject panel;
            if (!panels.TryGetValue(panelId, out panel) || panel == null) return;
            PositionRect(panel.GetComponent<RectTransform>(), ScreenRectToUiRect(imguiRect));
        }

        // Point d'entrée de test : force un repositionnement sans dépendre d'un vrai
        // changement de Screen.width/height (impossible à simuler en EditMode).
        public void SimulateHeaderOverlayRepositionForProof()
        {
            RepositionHeaderOverlayPanels();
        }

        private void BuildPanel(
            Transform parent,
            string panelId,
            string title,
            System.Action<Transform, Rect> contentBuilder,
            Rect size)
        {
            GameObject go = new GameObject("Panel_" + panelId);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.026f, 0.024f, 0.020f, 0.97f);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(size.x, size.y);
            rect.sizeDelta = new Vector2(size.width, size.height);

            TextMeshProUGUI titleLabel = CreateLabel(go.transform, title, 22, TextAnchor.MiddleLeft, true);
            LabelRect(titleLabel.GetComponent<RectTransform>(), 14f, 12f);

            contentBuilder(go.transform, size);
            panels[panelId] = go;

            // Fondu d'ouverture/fermeture (Jeff, 2026-08-19, "fluidite") au lieu d'un
            // SetActive instantane - voir TickPanelAnimations/RefreshPanels. Pas d'animation
            // d'echelle : le pivot (0,0) de ce rect est le coin bas-gauche (convention
            // partagee par PositionRect/Rect2Local/LabelRect dans tout ce fichier), donc
            // scaler ce transform ferait "grandir" le panneau depuis son coin au lieu de son
            // centre - un fondu seul reste propre sans toucher a cette convention partagee.
            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            panelAnimStates[panelId] = new PanelAnimState { Root = go, Group = group };
            go.SetActive(false);
        }

        private void BuildActivitiesContent(Transform parent, Rect panel)
        {
            TextMeshProUGUI hint = CreateLabel(parent, "Ouverture des activites officielles...", 15, TextAnchor.MiddleCenter);
            hint.color = new Color(0.92f, 0.82f, 0.58f);
            LabelRect(hint.GetComponent<RectTransform>(), 16f, 52f);
        }

        private void BuildBagContent(Transform parent, Rect panel)
        {
            string[] rows = { "Nectar : 120", "Pollen : 80", "Cire : 45", "Miel : 67", "Capacite : 400/500" };
            float y = 52f;
            for (int i = 0; i < rows.Length; i++)
            {
                TextMeshProUGUI row = CreateLabel(parent, rows[i], 16, TextAnchor.MiddleLeft);
                LabelRect(row.GetComponent<RectTransform>(), 16f, y);
                y += 38f;
            }
        }

        private void BuildMoreContent(Transform parent, Rect panel)
        {
            string[] entries = LivingHiveMenuSpec.MoreMenuEntries;
            float y = 52f;
            float rowH = 42f;
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                GameObject go = new GameObject("MoreRow_" + i);
                go.transform.SetParent(parent, false);
                Image img = go.AddComponent<Image>();
                img.color = new Color(0.12f, 0.075f, 0.025f, 0.9f);
                rect2(go, 10f, y, panel.width - 20f, rowH);
                Button b = go.AddComponent<Button>();
                b.targetGraphic = img;
                TextMeshProUGUI t = CreateLabel(go.transform, entry, 16, TextAnchor.MiddleLeft);
                LabelRect(t.GetComponent<RectTransform>(), 14f, 0f);
                string captureEntry = entry;
                b.onClick.AddListener(() => OnMoreRowClicked(captureEntry));
                // M038C-CL: "Armée" lives inside the "Plus" submenu, not on the bottom rail
                // directly - publish its real RectTransform via the SAME bridge pattern already
                // used elsewhere (this assembly cannot reference BeeKingdom.Tutorial directly),
                // so the FTUE arrow can target the actual row once this submenu is open, instead
                // of a guessed screen fraction. Consumed in BeeKingdom.Playground (see
                // HiveMapArmyBootstrap.cs), which can see both this bridge and BeeKingdom.Tutorial.
                if (string.Equals(entry, "Armée", System.StringComparison.Ordinal))
                {
                    RectTransform rowRect = go.GetComponent<RectTransform>();
                    LivingHiveArmyBridge.SetArmyRowRectQuery(() => rowRect);
                }
                y += rowH + 4f;
            }
        }

        private void OnMoreRowClicked(string entry)
        {
            if (string.Equals(entry, "Armée", System.StringComparison.Ordinal))
            {
                state.CloseActiveMenuPanel();
                LivingHiveArmyBridge.OpenOverlay();
                return;
            }
            if (string.Equals(entry, "Parametres", System.StringComparison.Ordinal))
            {
                // Close the Plus panel first: the real Settings overlay draws via IMGUI on
                // top of it, and IMGUI never occludes uGUI's own EventSystem raycasts (see
                // BuildingInteractionController.HandlePointer) - leaving the Plus panel's
                // uGUI buttons live underneath would let a click meant for a Settings toggle
                // also fire whatever Plus row happens to sit at that same screen position.
                state.CloseActiveMenuPanel();
                LivingHiveSettingsBridge.ToggleOverlay();
            }
            RefreshAll();
        }

        private void BuildChatContent(Transform parent, Rect panel)
        {
            TextMeshProUGUI hint = CreateLabel(parent, "Mini chat (canal ruche) - preview locale", 15, TextAnchor.MiddleLeft);
            hint.color = new Color(0.90f, 0.90f, 0.90f);
            LabelRect(hint.GetComponent<RectTransform>(), 16f, 52f);
            TextMeshProUGUI msg = CreateLabel(parent, "Bienvenue dans le canal ruche !", 15, TextAnchor.MiddleLeft);
            LabelRect(msg.GetComponent<RectTransform>(), 16f, 92f);
        }

        private void BuildWorldMapOverlay(Transform parent, Rect panel)
        {
            TextMeshProUGUI title = CreateLabel(parent, state.SurfaceMode == LivingHiveMenuState.SurfaceBoundary.World
                ? "Mode Carte : surface changement active"
                : "Mode Ruche", 18, TextAnchor.MiddleCenter, true);
            LabelRect(title.GetComponent<RectTransform>(), 0f, 16f);

            TextMeshProUGUI hint = CreateLabel(parent, "La surface active est : " + state.SurfaceSwitchLabelForProof + ".",
                15, TextAnchor.MiddleCenter);
            LabelRect(hint.GetComponent<RectTransform>(), 0f, 60f);

            TextMeshProUGUI route = CreateLabel(parent, "Routes : Goldenheart - Silverstream - Meadowguard - Amberfall"
                + " - Stonepeak - Sunblossom - Frostwing - Thornwatch - Crimson.",
                14, TextAnchor.MiddleCenter);
            route.color = new Color(0.86f, 0.68f, 0.30f);
            LabelRect(route.GetComponent<RectTransform>(), 0f, 100f);
        }

        // Panneau profil Reine : coquille (titre + niveau + progression), sans stats
        // /arbre/couvain/atelier/plein écran — fonctionnel, contenu enrichi plus tard.
        private void BuildQueenProfileContent(Transform parent, Rect panel)
        {
            TextMeshProUGUI level = CreateLabel(parent, "Niveau " + LivingHiveMenuHeaderData.PreviewQueenLevel.ToString(),
                17, TextAnchor.MiddleLeft, true);
            level.color = new Color(1f, 0.90f, 0.58f);
            LabelRect(level.GetComponent<RectTransform>(), 16f, 52f);

            TextMeshProUGUI progress = CreateLabel(parent, "Progression vers le niveau suivant : 12%",
                14, TextAnchor.MiddleLeft);
            progress.color = new Color(0.92f, 0.92f, 0.92f);
            LabelRect(progress.GetComponent<RectTransform>(), 16f, 90f);

            TextMeshProUGUI preview = CreateLabel(parent, "Donnees preview locales (economie future branchable ici).",
                12, TextAnchor.MiddleLeft);
            preview.color = LivingHiveMenuVisuals.LabelInactiveColor;
            LabelRect(preview.GetComponent<RectTransform>(), 16f, 128f);
        }

        // Panneau Boutique : accès uniquement, contenu différé (coquille présente).
        private void BuildShopContent(Transform parent, Rect panel)
        {
            TextMeshProUGUI hint = CreateLabel(parent, "Boutique - acces d'essai.", 16, TextAnchor.MiddleCenter, true);
            hint.color = new Color(0.96f, 0.94f, 0.86f);
            LabelRect(hint.GetComponent<RectTransform>(), 0f, 48f);

            TextMeshProUGUI message = CreateLabel(parent, "Le contenu (abonnements, passes, achats)"
                + " sera ajoute dans une mission dediee.",
                13, TextAnchor.MiddleCenter);
            message.color = new Color(0.88f, 0.88f, 0.88f);
            LabelRect(message.GetComponent<RectTransform>(), 0f, 90f);
        }

        public void RefreshAll()
        {
            for (int i = 0; i < railButtons.Count; i++) { /* immuable */ }
            RefreshRailHighlights();
            RefreshHeader();
            RefreshPanels();
        }

        // Miroir de l'état actif de DrawIconButton : fond premium actif/normal, bandeau doré,
        // barre de progression, teintes glow/socket/icône, couleur de libellé.
        private void RefreshRailHighlights()
        {
            string active = ActiveRailItemForProof;
            foreach (KeyValuePair<string, RailButtonVisual> pair in railVisuals)
            {
                bool isActive = string.Equals(pair.Key, active, System.StringComparison.Ordinal);
                RailButtonVisual v = pair.Value;

                v.Panel.sprite = isActive ? LivingHiveMenuVisuals.ButtonActiveSprite() : LivingHiveMenuVisuals.ButtonNormalSprite();
                // Alpha réel du monolithe par état (0.94 actif / 0.78 inactif) : le sprite
                // change, la transparence aussi (miroir des fills de DrawIconButton).
                v.Panel.color = isActive
                    ? new Color(1f, 1f, 1f, LivingHiveMenuVisuals.ButtonActiveFill.a)
                    : new Color(1f, 1f, 1f, LivingHiveMenuVisuals.ButtonNormalFill.a);
                v.Label.color = isActive ? LivingHiveMenuVisuals.LabelActiveColor : LivingHiveMenuVisuals.LabelInactiveColor;

                v.Glow.sprite = isActive ? LivingHiveMenuVisuals.GlowActiveSprite() : LivingHiveMenuVisuals.GlowInactiveSprite();
                v.Glow.color = isActive ? LivingHiveMenuVisuals.GlowTintActive : LivingHiveMenuVisuals.GlowTintInactive;
                v.Socket.color = isActive ? LivingHiveMenuVisuals.SocketTintActive : LivingHiveMenuVisuals.SocketTintInactive;
                v.Icon.color = isActive ? LivingHiveMenuVisuals.IconTintActive : LivingHiveMenuVisuals.IconTintInactive;

                if (v.HeaderBand != null) v.HeaderBand.SetActive(isActive);
                if (v.ProgressLine != null) v.ProgressLine.SetActive(isActive);
            }
        }

        private const float PanelFadeInSeconds = 0.16f;
        private const float PanelFadeOutSeconds = 0.12f;

        // Jeff (2026-08-19, "fluidite"): ouverture/fermeture instantanee (SetActive brut)
        // remplacee par un fondu - voir TickPanelAnimations pour l'avancement par frame et le
        // commentaire de BuildPanel pour pourquoi ce n'est qu'un fondu (pas d'echelle).
        private void RefreshPanels()
        {
            float now = Time.unscaledTime;
            foreach (KeyValuePair<string, GameObject> pair in panels)
            {
                bool shouldShow = ShouldShowPanel(pair.Key);
                PanelAnimState state = panelAnimStates[pair.Key];
                bool isActive = pair.Value.activeSelf;

                if (shouldShow && !isActive)
                {
                    pair.Value.SetActive(true);
                    state.Group.alpha = 0f;
                    state.Closing = false;
                    state.StartedAt = now;
                }
                else if (!shouldShow && isActive && !state.Closing)
                {
                    state.Closing = true;
                    state.StartedAt = now;
                }
                else if (shouldShow && isActive && state.Closing)
                {
                    // Rouvert pendant le fondu de fermeture - repart en ouverture depuis
                    // l'alpha courant (evite un saut visible).
                    state.Closing = false;
                    state.StartedAt = now - (1f - state.Group.alpha) * PanelFadeInSeconds;
                }
            }
        }

        private void TickPanelAnimations()
        {
            float now = Time.unscaledTime;
            foreach (KeyValuePair<string, PanelAnimState> pair in panelAnimStates)
            {
                PanelAnimState state = pair.Value;
                if (state.Root == null || !state.Root.activeSelf) continue;

                float duration = state.Closing ? PanelFadeOutSeconds : PanelFadeInSeconds;
                float t = duration > 0f ? Mathf.Clamp01((now - state.StartedAt) / duration) : 1f;
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                state.Group.alpha = state.Closing ? 1f - eased : eased;

                if (state.Closing && t >= 1f) state.Root.SetActive(false);
            }
        }

        private bool ShouldShowPanel(string panelId)
        {
            if (string.Equals(panelId, "QueenProfile", System.StringComparison.Ordinal)) return queenProfileOpen;
            if (string.Equals(panelId, "Shop", System.StringComparison.Ordinal)) return shopOpen;
            if (state.ActivitiesOpen && panelId == LivingHiveMenuSpec.ActivitiesId) return true;
            if (state.CommunicationOpen && panelId == LivingHiveMenuSpec.CommunicationId) return true;
            if (string.Equals(panelId, "Carte", System.StringComparison.Ordinal))
            {
                return state.SurfaceMode == LivingHiveMenuState.SurfaceBoundary.World;
            }
            if (string.IsNullOrEmpty(state.ActiveMenuId)) return false;
            return string.Equals(panelId, state.ActiveMenuId, System.StringComparison.Ordinal);
        }

        // --- Helpers uGUI ---

        // Jeff (2026-08-19): "netteté" pass, étape 1 - legacy UnityEngine.UI.Text/
        // LegacyRuntime.ttf rendait flou, surtout redimensionné. TextMeshProUGUI (deja
        // importe et utilise partout ailleurs dans le projet - Assets/_Project/Scripts/UI/*)
        // rend net via SDF a n'importe quelle taille. Police Baloo 2 ExtraBold (choisie par
        // Jeff le 2026-08-19 pour se rapprocher du HUD de reference) plutot que la police par
        // defaut LiberationSans - voir HudFont ci-dessous.
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
            return t;
        }

        private static TMP_FontAsset cachedHudFont;

        // Assets/Fonts/Resources/Baloo2-ExtraBold SDF.asset (instance statique Poids 800
        // extraite de la police variable Google Fonts Baloo 2, licence OFL). Repli sur la
        // police TMP par defaut du projet si jamais l'asset venait a manquer - jamais de null.
        //
        // Le contour sombre (Jeff, 2026-08-19 - lisibilite sur n'importe quel fond de la
        // ruche, meme intention que le style "contour + ombre" du HUD de reference) est pose
        // UNE FOIS sur le materiau partage de la police plutot que via
        // TMP_Text.outlineWidth/outlineColor par instance : ce setter touche
        // m_sharedMaterial en interne, qui n'existe pas encore tant que le GameObject n'a pas
        // traverse Awake/OnEnable - or BuildPanel cree ses labels sous un panneau
        // SetActive(false), donc Awake/OnEnable ne se declenchent jamais avant qu'on essaie
        // de regler le contour -> NullReferenceException dans SetOutlineThickness. Le
        // materiau partage, lui, existe des la creation de l'asset et n'a pas ce probleme.
        private static TMP_FontAsset HudFont()
        {
            if (cachedHudFont != null) return cachedHudFont;
            // Jeff (2026-08-19): apres Baloo 2 (trop rond) et Montserrat (encore trop gras),
            // changement pour Cinzel Regular - registre "titulature romaine/royaume", voulu
            // explicitement pour l'esthetique regale du jeu plutot qu'un sans-serif d'app.
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

        private static void LabelRect(RectTransform rect, float x, float y)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(600f, 30f);
        }

        private static void rect2(GameObject go, float x, float y, float w, float h)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
        }
    }
}
