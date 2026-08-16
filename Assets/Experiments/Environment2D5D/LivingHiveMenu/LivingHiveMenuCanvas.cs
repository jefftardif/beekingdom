using System.Collections.Generic;
using UnityEngine;
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
        private const string FontResource = "LegacyRuntime.ttf";

        private readonly LivingHiveMenuState state = new LivingHiveMenuState();

        private Canvas canvas;
        private RectTransform railRoot;
        private readonly Dictionary<string, Button> railButtons = new Dictionary<string, Button>();
        private readonly Dictionary<string, RailButtonVisual> railVisuals = new Dictionary<string, RailButtonVisual>();
        private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();
        private Vector2 lastScreenSize;

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
            public Text Value;
            public Text Label;
        }

        // Références nécessaires à RefreshRailHighlights pour reproduire l'état actif
        // (DrawIconButton) : fond premium (normal/actif), glow, socket, icône et libellé.
        private sealed class RailButtonVisual
        {
            public Image Panel;
            public Image Glow;
            public Image Socket;
            public Image Icon;
            public Text Label;
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
                if (state.ChatOpen) return LivingHiveMenuSpec.ChatId;
                if (state.IsMoreActiveForProof()) return LivingHiveMenuSpec.MoreId;
                if (string.IsNullOrEmpty(state.ActiveMenuId)) return string.Empty;
                return state.ActiveMenuId;
            }
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
            canvasObj.AddComponent<GraphicRaycaster>();

            BuildRail(canvasObj.transform);
            BuildHeader(canvasObj.transform);
            BuildPanels(canvasObj.transform);
            RefreshAll();
            lastScreenSize = new Vector2(Screen.width, Screen.height);
        }

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

            // Icône couronne (46 en paysage, 36 en portrait).
            float icon = portrait ? 36f : 46f;
            Image iconImage = NewImage(container, "Icon", LivingHiveMenuVisuals.IconSprite("queen"));
            iconImage.color = LivingHiveMenuVisuals.IconTintActive;
            iconImage.raycastTarget = false;
            Rect2Local(iconImage, (portrait ? 8f : 12f), (portrait ? 4f : 9f), icon, icon);

            // Nom "Reine" + niveau.
            Text name = CreateLabel(container, "Reine", portrait ? 15 : 18, TextAnchor.MiddleLeft, true);
            name.color = new Color(1f, 0.90f, 0.58f);
            Rect2Local(name, (portrait ? 52f : 70f), (portrait ? 4f : 6f), 90f, 20f);

            Text level = CreateLabel(container, "Niv. " + LivingHiveMenuHeaderData.PreviewQueenLevel.ToString(),
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
                Rect ui = ScreenRectToUiRect(rects[i]);
                string id = ids[i];
                RectTransform container = NewContainer(parent, "HeaderChip_" + id, ui);

                Image panel = container.gameObject.AddComponent<Image>();
                panel.sprite = LivingHiveMenuVisuals.ButtonNormalSprite();
                panel.type = Image.Type.Sliced;
                panel.color = new Color(1f, 1f, 1f, 0.7f);

                Color accent = LivingHiveMenuHeaderData.ResourceAccent(id);
                Image icon = NewImage(container, "Icon", LivingHiveMenuVisuals.IconSprite(id));
                icon.color = accent;
                icon.raycastTarget = false;
                Rect2Local(icon, 6f, (portrait ? 9f : 7f), portrait ? 18f : 24f, portrait ? 18f : 24f);

                Text value = CreateLabel(container, "0", portrait ? 14 : 15, TextAnchor.MiddleRight, true);
                value.color = new Color(0.96f, 0.94f, 0.86f);
                Rect2Local(value, portrait ? 28f : 36f, portrait ? 4f : 4f,
                    ui.width - (portrait ? 36f : 44f), portrait ? 20f : 22f);

                Text label = CreateLabel(container, HeaderResourceLabel(id), portrait ? 10 : 11, TextAnchor.MiddleCenter);
                label.color = LivingHiveMenuVisuals.LabelInactiveColor;
                Rect2Local(label, 4f, portrait ? 24f : 24f, ui.width - 8f, 12f);

                headerChipVisuals[id] = new HeaderChipVisual { Panel = panel, Icon = icon, Value = value, Label = label };
            }
        }

        private static string HeaderResourceLabel(string resourceId)
        {
            switch (resourceId)
            {
                case "honey": return "Miel";
                case "wax": return "Cire";
                case "pollen": return "Pollen";
                case "bees": return "Abeilles";
                case "capacity": return "Capacite";
                default: return resourceId;
            }
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

        private void RefreshHeader()
        {
            bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
            foreach (KeyValuePair<string, HeaderChipVisual> pair in headerChipVisuals)
            {
                HeaderChipVisual v = pair.Value;
                int value = LivingHiveMenuHeaderData.PreviewValue(pair.Key);
                string text = LivingHiveMenuHeaderData.FormatResource(value);
                if (string.Equals(pair.Key, "capacity", System.StringComparison.Ordinal))
                {
                    text = LivingHiveMenuHeaderData.FormatResource(LivingHiveMenuHeaderData.PreviewCapacityUsed)
                        + "/" + LivingHiveMenuHeaderData.FormatResource(LivingHiveMenuHeaderData.PreviewCapacityMax);
                }
                v.Value.text = text;
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
            Text label = CreateLabel(go.transform, entry.Label, 11, TextAnchor.MiddleCenter);
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
            state.ToggleEntry(itemId);
            RefreshAll();
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
            BuildPanel(parent, LivingHiveMenuSpec.QuestsId, "Journal de quetes", BuildQuestsContent, new Rect(10f, 96f, 520f, 460f));
            BuildPanel(parent, LivingHiveMenuSpec.BagId, "Sac & stocks", BuildBagContent, new Rect(10f, 96f, 520f, 460f));
            BuildPanel(parent, LivingHiveMenuSpec.MoreId, "Plus", BuildMoreContent, new Rect(10f, 96f, 420f, 520f));
            BuildPanel(parent, LivingHiveMenuSpec.SettingsId, "Parametres", BuildSettingsContent, new Rect(10f, 96f, 460f, 520f));
            BuildPanel(parent, LivingHiveMenuSpec.ChatId, "Chat", BuildChatContent, new Rect(10f, 96f, 440f, 420f));
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
            go.SetActive(false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.026f, 0.024f, 0.020f, 0.97f);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(size.x, size.y);
            rect.sizeDelta = new Vector2(size.width, size.height);

            Text titleLabel = CreateLabel(go.transform, title, 22, TextAnchor.MiddleLeft, true);
            LabelRect(titleLabel.GetComponent<RectTransform>(), 14f, 12f);

            contentBuilder(go.transform, size);
            panels[panelId] = go;
        }

        private void BuildQuestsContent(Transform parent, Rect panel)
        {
            string[] quests =
            {
                "Recolter 5x nectar",
                "Produire 3x miel",
                "Elever une couveuse",
                "Reconnaitre 2 zones"
            };
            float y = 52f;
            for (int i = 0; i < quests.Length; i++)
            {
                Text row = CreateLabel(parent, "  " + quests[i] + "   [en cours]", 15, TextAnchor.MiddleLeft);
                row.color = new Color(0.92f, 0.62f, 0.16f);
                LabelRect(row.GetComponent<RectTransform>(), 16f, y);
                y += 34f;
            }
        }

        private void BuildBagContent(Transform parent, Rect panel)
        {
            string[] rows = { "Nectar : 120", "Pollen : 80", "Cire : 45", "Miel : 67", "Capacite : 400/500" };
            float y = 52f;
            for (int i = 0; i < rows.Length; i++)
            {
                Text row = CreateLabel(parent, rows[i], 16, TextAnchor.MiddleLeft);
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
                Text t = CreateLabel(go.transform, entry, 16, TextAnchor.MiddleLeft);
                LabelRect(t.GetComponent<RectTransform>(), 14f, 0f);
                string captureEntry = entry;
                b.onClick.AddListener(() => OnMoreRowClicked(captureEntry));
                y += rowH + 4f;
            }
        }

        private void OnMoreRowClicked(string entry)
        {
            if (string.Equals(entry, "Parametres", System.StringComparison.Ordinal))
            {
                state.OpenSettings();
            }
            RefreshAll();
        }

        private void BuildSettingsContent(Transform parent, Rect panel)
        {
            AddSettingToggle(parent, "Mouvement reduit", state.ReducedMotionEnabled, state.SetReducedMotion, 52f);
            AddSettingToggle(parent, "Mode economie", state.EconomyModeEnabled, state.SetEconomyMode, 110f);
            AddSettingToggle(parent, "Effets sonores", state.SoundEnabled, state.SetSoundEnabled, 168f);
            AddSettingToggle(parent, "Musique", state.MusicEnabled, state.SetMusicEnabled, 226f);

            Text lang = CreateLabel(parent, "Langue : " + state.CurrentLocale, 16, TextAnchor.MiddleLeft);
            lang.color = new Color(0.92f, 0.62f, 0.16f);
            LabelRect(lang.GetComponent<RectTransform>(), 16f, 300f);
        }

        private void AddSettingToggle(Transform parent, string label, bool current, System.Action<bool> setter, float y)
        {
            GameObject go = new GameObject("Setting_" + label);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.035f, 0.030f, 0.023f, 0.94f);
            rect2(go, 10f, y, 400f, 44f);
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            Text t = CreateLabel(go.transform, label + "   :   " + (current ? "ON" : "OFF"), 16, TextAnchor.MiddleLeft);
            LabelRect(t.GetComponent<RectTransform>(), 14f, 0f);
            b.onClick.AddListener(() => setter(!current));
        }

        private void BuildChatContent(Transform parent, Rect panel)
        {
            Text hint = CreateLabel(parent, "Mini chat (canal ruche) - preview locale", 15, TextAnchor.MiddleLeft);
            hint.color = new Color(0.90f, 0.90f, 0.90f);
            LabelRect(hint.GetComponent<RectTransform>(), 16f, 52f);
            Text msg = CreateLabel(parent, "Bienvenue dans le canal ruche !", 15, TextAnchor.MiddleLeft);
            LabelRect(msg.GetComponent<RectTransform>(), 16f, 92f);
        }

        private void BuildWorldMapOverlay(Transform parent, Rect panel)
        {
            Text title = CreateLabel(parent, state.SurfaceMode == LivingHiveMenuState.SurfaceBoundary.World
                ? "Mode Carte : surface changement active"
                : "Mode Ruche", 18, TextAnchor.MiddleCenter, true);
            LabelRect(title.GetComponent<RectTransform>(), 0f, 16f);

            Text hint = CreateLabel(parent, "La surface active est : " + state.SurfaceSwitchLabelForProof + ".",
                15, TextAnchor.MiddleCenter);
            LabelRect(hint.GetComponent<RectTransform>(), 0f, 60f);

            Text route = CreateLabel(parent, "Routes : Goldenheart - Silverstream - Meadowguard - Amberfall"
                + " - Stonepeak - Sunblossom - Frostwing - Thornwatch - Crimson.",
                14, TextAnchor.MiddleCenter);
            route.color = new Color(0.86f, 0.68f, 0.30f);
            LabelRect(route.GetComponent<RectTransform>(), 0f, 100f);
        }

        // Panneau profil Reine : coquille (titre + niveau + progression), sans stats
        // /arbre/couvain/atelier/plein écran — fonctionnel, contenu enrichi plus tard.
        private void BuildQueenProfileContent(Transform parent, Rect panel)
        {
            Text level = CreateLabel(parent, "Niveau " + LivingHiveMenuHeaderData.PreviewQueenLevel.ToString(),
                17, TextAnchor.MiddleLeft, true);
            level.color = new Color(1f, 0.90f, 0.58f);
            LabelRect(level.GetComponent<RectTransform>(), 16f, 52f);

            Text progress = CreateLabel(parent, "Progression vers le niveau suivant : 12%",
                14, TextAnchor.MiddleLeft);
            progress.color = new Color(0.92f, 0.92f, 0.92f);
            LabelRect(progress.GetComponent<RectTransform>(), 16f, 90f);

            Text preview = CreateLabel(parent, "Donnees preview locales (economie future branchable ici).",
                12, TextAnchor.MiddleLeft);
            preview.color = LivingHiveMenuVisuals.LabelInactiveColor;
            LabelRect(preview.GetComponent<RectTransform>(), 16f, 128f);
        }

        // Panneau Boutique : accès uniquement, contenu différé (coquille présente).
        private void BuildShopContent(Transform parent, Rect panel)
        {
            Text hint = CreateLabel(parent, "Boutique - acces d'essai.", 16, TextAnchor.MiddleCenter, true);
            hint.color = new Color(0.96f, 0.94f, 0.86f);
            LabelRect(hint.GetComponent<RectTransform>(), 0f, 48f);

            Text message = CreateLabel(parent, "Le contenu (abonnements, passes, achats)"
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

        private void RefreshPanels()
        {
            foreach (KeyValuePair<string, GameObject> pair in panels)
            {
                bool shouldShow = ShouldShowPanel(pair.Key);
                if (pair.Value.activeSelf != shouldShow) pair.Value.SetActive(shouldShow);
            }
        }

        private bool ShouldShowPanel(string panelId)
        {
            if (string.Equals(panelId, "QueenProfile", System.StringComparison.Ordinal)) return queenProfileOpen;
            if (string.Equals(panelId, "Shop", System.StringComparison.Ordinal)) return shopOpen;
            if (state.ChatOpen && panelId == LivingHiveMenuSpec.ChatId) return true;
            if (string.Equals(panelId, "Carte", System.StringComparison.Ordinal))
            {
                return state.SurfaceMode == LivingHiveMenuState.SurfaceBoundary.World;
            }
            if (string.IsNullOrEmpty(state.ActiveMenuId)) return false;
            return string.Equals(panelId, state.ActiveMenuId, System.StringComparison.Ordinal);
        }

        // --- Helpers uGUI ---

        private static Text CreateLabel(Transform parent, string text, int size, TextAnchor align, bool bold = false)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>(FontResource);
            t.fontSize = size;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = Color.white;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
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