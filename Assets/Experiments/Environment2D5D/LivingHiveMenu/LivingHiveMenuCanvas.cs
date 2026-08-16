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
        private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();
        private Vector2 lastScreenSize;

        public LivingHiveMenuState State => state;

        public int RailButtonCount => railButtons.Count;

        public bool IsRailBuilt => railRoot != null;

        public bool PanelShown(string panelId)
        {
            GameObject panel;
            return panels.TryGetValue(panelId, out panel) && panel != null && panel.activeSelf;
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
                RefreshAll();
            }
        }

        // --- Rail du bas (géométrie pixel écran du monolithe, 1 unité uGUI = 1 pixel) ---

        private void BuildRail(Transform parent)
        {
            bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
            Rect rail = ScreenRectToUiRect(LivingHiveMenuSpec.RailRectForProof(portrait, Screen.width, Screen.height));

            GameObject bg = new GameObject("RailBackdrop");
            bg.transform.SetParent(parent, false);
            bg.transform.SetSiblingIndex(0);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.020f, 0.018f, 0.014f, 0.86f);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            PositionRect(bgRect, rail);

            railRoot = bgRect;

            LivingHiveMenuEntry[] entries = LivingHiveMenuSpec.Entries(portrait);
            Rect[] itemRects = LivingHiveMenuSpec.ItemRects(portrait, Screen.width, Screen.height);
            for (int i = 0; i < entries.Length && i < itemRects.Length; i++)
            {
                TravelTo(parent, entries[i], itemRects[i]);
            }
        }

        private void RebuildRail()
        {
            if (canvas == null) return;
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = canvas.transform.GetChild(i).gameObject;
                if (child.name.StartsWith("RailBackdrop", System.StringComparison.Ordinal)
                    || child.name.StartsWith("Button_", System.StringComparison.Ordinal))
                {
                    if (Application.isPlaying) Object.Destroy(child);
                    else Object.DestroyImmediate(child);
                }
            }
            railButtons.Clear();
            railRoot = null;
            BuildRail(canvas.transform);
        }

        private void TravelTo(Transform parent, LivingHiveMenuEntry entry, Rect imguiScreenRect)
        {
            GameObject go = new GameObject("Button_" + entry.ItemId);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

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

            Text label = CreateLabel(go.transform, entry.Label, 16, TextAnchor.MiddleCenter);
            FillRect(label.GetComponent<RectTransform>());

            LivingHiveMenuEntry capture = entry;
            button.onClick.AddListener(() => OnEntryClicked(capture.ItemId));
            railButtons[entry.ItemId] = button;
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

        public void RefreshAll()
        {
            for (int i = 0; i < railButtons.Count; i++) { /* immuable */ }
            RefreshRailHighlights();
            RefreshPanels();
        }

        private void RefreshRailHighlights()
        {
            bool portrait = LivingHiveMenuSpec.IsPortrait(Screen.width, Screen.height);
            LivingHiveMenuEntry[] entries = LivingHiveMenuSpec.Entries(portrait);
            string active = ActiveRailItemForProof;
            foreach (KeyValuePair<string, Button> pair in railButtons)
            {
                bool isActive = string.Equals(pair.Key, active, System.StringComparison.Ordinal);
                Color target = isActive ? new Color(1f, 0.9f, 0.3f, 1f) : new Color(0.8f, 0.8f, 0.8f, 1f);
                Text label = pair.Value.GetComponentInChildren<Text>(true);
                if (label != null) label.color = target;
                Image bg = pair.Value.GetComponent<Image>();
                if (bg != null) bg.color = isActive
                    ? new Color(1f, 1f, 1f, 0.20f)
                    : new Color(1f, 1f, 1f, 0f);
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