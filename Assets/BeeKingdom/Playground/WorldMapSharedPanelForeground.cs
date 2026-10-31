using System;
using BeeKingdom.LivingHiveMenu;
using BeeKingdom.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// World Map: IMGUI terrain is painted over ScreenSpaceOverlay canvases in
    /// this scene. Draw only the *open* shared Bag / More popup at the front of
    /// the same IMGUI stack, without modifying the menu state or map gameplay.
    /// The normal hive's uGUI canvas remains untouched.
    /// </summary>
    public sealed class WorldMapSharedPanelForeground : MonoBehaviour
    {
        private const int ForegroundDepth = -12000;
        private const string SupportEmail = "support@beekingdomgame.com";
        private LivingHiveMenuCanvas menu;
        private string detail;
        private string lastPanel;
        private GUIStyle panelStyle, rowStyle, hoverStyle, labelStyle, titleStyle, subtitleStyle;
        private GUIStyle closeStyle;
        private bool blockedCanvasLastFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryInstallOnWorldMap();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstallOnWorldMap();
        }

        private static void TryInstallOnWorldMap()
        {
            var map = FindAnyObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
            if (map == null || map.GetComponent<WorldMapSharedPanelForeground>() != null)
                return;
            map.gameObject.AddComponent<WorldMapSharedPanelForeground>();
        }

        private void LateUpdate()
        {
            if (menu == null)
                menu = GetComponentInChildren<LivingHiveMenuCanvas>(true);
            if (menu == null) return;

            if (menu.SharedPanelOpen)
            {
                // The original uGUI shared panel is behind the terrain IMGUI.
                // Disable its otherwise invisible raycaster and let this
                // foreground modal own all pointer gestures.
                menu.SetInputBlocked(true);
                blockedCanvasLastFrame = true;
            }
            else
            {
                if (blockedCanvasLastFrame)
                {
                    blockedCanvasLastFrame = false;
                    detail = null;
                    lastPanel = null;
                    // Restore the raycaster immediately, including in paused
                    // editor sessions where WorldMap.Update has not run yet.
                    // Preserve ownership of any full-screen external modal.
                    bool external = HiveViewProductUiPresenter.SettingsOverlayOpenForExternalHost
                        || HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost;
                    // The World Map updates its own other modal gates on the
                    // following tick. More cannot open Chat or Pollination.
                    menu.SetInputBlocked(external);
                }
            }
        }

        internal static Rect PopupRect(float width, float height, Rect safeArea,
            bool more)
        {
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                safeArea = new Rect(0f, 0f, width, height);
            float w = Mathf.Min(468f, Mathf.Max(1f, safeArea.width - 28f));
            float h = Mathf.Min(more ? 432f : 300f,
                Mathf.Max(1f, safeArea.height - 28f));
            return new Rect(safeArea.center.x - w * .5f,
                height - safeArea.center.y - h * .5f, w, h);
        }

        private static string Local(string french, string english)
        {
            return string.Equals(BeeLocalization.CurrentLocale, "en-US",
                StringComparison.OrdinalIgnoreCase) ? english : french;
        }

        private void EnsureStyles()
        {
            if (panelStyle != null) return;
            panelStyle = new GUIStyle(GUI.skin.box) {
                border = new RectOffset(22, 22, 22, 22), padding = new RectOffset()
            };
            panelStyle.normal.background = PremiumNavigationStyle.RoundedSurface(
                new Color(.13f, .10f, .06f, .985f),
                new Color(.045f, .033f, .018f, .985f),
                new Color(.69f, .46f, .16f, 1f), 22);
            rowStyle = new GUIStyle(GUI.skin.box) {
                border = new RectOffset(17, 17, 17, 17), padding = new RectOffset()
            };
            rowStyle.normal.background = PremiumNavigationStyle.RoundedSurface(
                new Color(.19f, .13f, .066f, .99f),
                new Color(.09f, .063f, .032f, .99f),
                new Color(.40f, .28f, .13f, 1f), 17);
            hoverStyle = new GUIStyle(rowStyle);
            hoverStyle.normal.background = PremiumNavigationStyle.RoundedSurface(
                new Color(.30f, .20f, .092f, 1f),
                new Color(.13f, .085f, .039f, 1f),
                new Color(.94f, .70f, .27f, 1f), 17);
            titleStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 22, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = new Color(1f, .82f, .43f, 1f);
            subtitleStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 13, alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            subtitleStyle.normal.textColor = new Color(.91f, .85f, .70f, 1f);
            labelStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 17, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            labelStyle.normal.textColor = new Color(.98f, .90f, .73f, 1f);
            closeStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 26, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            closeStyle.normal.textColor = new Color(1f, .88f, .57f, 1f);
        }

        private static void ShadeBackground()
        {
            Color saved = GUI.color;
            GUI.color = new Color(.018f, .013f, .007f, .58f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = saved;
        }

        private bool DrawHotspot(Rect bounds)
        {
            return GUI.Button(bounds, GUIContent.none, GUIStyle.none);
        }

        private bool DrawRow(Rect rect, string title, string description)
        {
            bool hovered = rect.Contains(Event.current.mousePosition);
            GUI.Box(rect, GUIContent.none, hovered ? hoverStyle : rowStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 4f, rect.width - 36f, 27f),
                title, labelStyle);
            GUI.Label(new Rect(rect.x + 19f, rect.y + 28f, rect.width - 38f, 19f),
                description, subtitleStyle);
            return DrawHotspot(rect);
        }

        private void Close()
        {
            detail = null;
            lastPanel = null;
            menu.CloseSharedPanel();
        }

        private void ShowMoreRow(string id)
        {
            if (id == "Armée" || id == "Parametres")
            {
                detail = null;
                menu.SimulateMoreRowClick(id);
                return;
            }
            detail = id;
        }

        private void DrawMore(Rect rect)
        {
            if (detail == "Aide")
            {
                GUI.Label(new Rect(rect.x + 20f, rect.y + 108f, rect.width - 40f, 160f),
                    Local("Touchez une ruche, une bestiole ou une ressource pour voir ses actions.\n\nUtilisez la molette ou le pincement pour zoomer.\n\nLe bouton Ruche vous ramène à votre colonie.",
                          "Tap a hive, creature, or resource to view its actions.\n\nUse the wheel or pinch to zoom.\n\nThe Hive button returns to your colony."),
                    subtitleStyle);
                if (DrawRow(new Rect(rect.x + 16f, rect.yMax - 80f, rect.width - 32f, 54f),
                    Local("Retour au menu", "Back to menu"), ""))
                    detail = null;
                return;
            }
            if (detail == "Support")
            {
                GUI.Label(new Rect(rect.x + 20f, rect.y + 113f, rect.width - 40f, 65f),
                    Local("Pour signaler un problème ou obtenir de l'aide :",
                          "To report an issue or request support:"),
                    subtitleStyle);
                GUI.Label(new Rect(rect.x + 20f, rect.y + 173f, rect.width - 40f, 43f),
                    SupportEmail, labelStyle);
                if (DrawRow(new Rect(rect.x + 16f, rect.y + 240f, rect.width - 32f, 54f),
                    Local("Copier l'adresse", "Copy email address"), ""))
                    GUIUtility.systemCopyBuffer = SupportEmail;
                if (DrawRow(new Rect(rect.x + 16f, rect.yMax - 80f, rect.width - 32f, 54f),
                    Local("Retour au menu", "Back to menu"), ""))
                    detail = null;
                return;
            }

            string[] keys = {"Armée", "Parametres", "Aide", "Support"};
            string[] titles = {
                BeeLocalization.Text("nav.army", "Armée"),
                BeeLocalization.Text("settings.panel.title", "Paramètres"),
                BeeLocalization.Text("nav.help", "Aide"),
                BeeLocalization.Text("nav.support", "Support")
            };
            string[] descriptions = {
                Local("Gérer les troupes et leur caserne", "Manage troops and barracks"),
                Local("Préférences et configuration du jeu", "Game settings and preferences"),
                Local("Conseils et navigation", "Help and navigation"),
                Local("Nous joindre", "Contact the team")
            };
            float available = rect.height - 124f;
            float rowH = Mathf.Min(55f, Mathf.Max(35f, (available - 27f) / 4f));
            float gap = 9f;
            float top = rect.y + 83f;
            for (int i = 0; i < keys.Length; i++)
            {
                var row = new Rect(rect.x + 16f,
                    top + i * (rowH + gap), rect.width - 32f, rowH);
                if (DrawRow(row, titles[i], descriptions[i]))
                {
                    ShowMoreRow(keys[i]);
                    return;
                }
            }
        }

        private void DrawBag(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 20f, rect.y + 103f,
                rect.width - 40f, 90f),
                BeeLocalization.Text("bag.data_unavailable",
                    Local("Stocks indisponibles sur cet écran.",
                          "Inventory is unavailable on this screen.")),
                subtitleStyle);
            if (DrawRow(new Rect(rect.x + 16f, rect.yMax - 78f,
                rect.width - 32f, 53f), Local("Fermer", "Close"), ""))
                Close();
        }

        // Invoked at the END of the World Map IMGUI pass. A distinct
        // MonoBehaviour.OnGUI gets painted behind this particular map.
        public void DrawFromMapOnGUI()
        {
            if (menu == null)
                menu = GetComponentInChildren<LivingHiveMenuCanvas>(true);
            if (menu == null || !menu.SharedPanelOpen) return;
            string panelId = menu.SharedPanelId;
            if (panelId != "More" && panelId != "Bag") return;
            if (lastPanel != panelId)
            {
                detail = null;
                lastPanel = panelId;
            }

            int oldDepth = GUI.depth;
            bool wasEnabled = GUI.enabled;
            Color saved = GUI.color;
            try
            {
                GUI.depth = ForegroundDepth;
                GUI.enabled = true;
                EnsureStyles();
                Rect bounds = PopupRect(Screen.width, Screen.height,
                    Screen.safeArea, panelId == "More");

                if (Event.current.type == EventType.KeyDown &&
                    Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                    return;
                }
                if (Event.current.type == EventType.MouseUp &&
                    !bounds.Contains(Event.current.mousePosition))
                {
                    Close();
                    Event.current.Use();
                    return;
                }

                ShadeBackground();
                GUI.Box(bounds, GUIContent.none, panelStyle);
                string title = detail == "Aide" ? BeeLocalization.Text("nav.help", "Aide")
                    : detail == "Support" ? BeeLocalization.Text("nav.support", "Support")
                    : panelId == "More" ? BeeLocalization.Text("nav.more", "Plus")
                    : BeeLocalization.Text("nav.bag", "Sac");
                GUI.Label(new Rect(bounds.x + 21f, bounds.y + 17f,
                    bounds.width - 94f, 33f), title.ToUpperInvariant(), titleStyle);
                GUI.Label(new Rect(bounds.x + 22f, bounds.y + 53f,
                    bounds.width - 45f, 25f),
                    panelId == "More" ? Local("Menu du royaume", "Kingdom menu")
                    : Local("Inventaire du royaume", "Kingdom inventory"), subtitleStyle);
                var closeRect = new Rect(bounds.xMax - 58f, bounds.y + 16f, 40f, 38f);
                GUI.Box(closeRect, GUIContent.none, rowStyle);
                GUI.Label(closeRect, "×", closeStyle);
                if (DrawHotspot(closeRect)) { Close(); return; }

                if (panelId == "More") DrawMore(bounds);
                else DrawBag(bounds);
            }
            finally
            {
                GUI.color = saved;
                GUI.enabled = wasEnabled;
                GUI.depth = oldDepth;
            }
        }

        internal bool IsReadyForProof => menu != null;
        internal string CurrentPanelForProof => menu == null ? string.Empty : menu.SharedPanelId;
        internal string CurrentDetailForProof => detail ?? string.Empty;
    }
}
