using System;
using System.Globalization;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // M013-CX wave 4: Administration is the legacy "administration_core" building,
    // mapped to RoyalPalace. HiveMap exposes the current Coeur royal level/cap role,
    // upgrade state/action, and colony overview access without reusing LivingHive layout.
    public sealed class HiveMapRoyalPalaceBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Royal Palace Runtime";
        private const float HeaderHeight = 132f;
        private const float ContentMaxWidth = 520f;

        public static bool OverlayOpenForExternalHost { get; private set; }
        public static bool ModalOpenForExternalHost =>
            OverlayOpenForExternalHost || HiveViewProductUiPresenter.ColonyOverviewOpenForExternalHost;

        private BuildingDefinition selectedBuilding;
        private BuildingInteractionController subscribedController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindAnyObjectByType<HiveMapRoyalPalaceBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapRoyalPalaceBootstrap>();
        }

        private static bool IsEnvironmentScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return false;
            return scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);
        }

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!IsEnvironmentScene(scene)) return;
            if (FindAnyObjectByType<HiveMapRoyalPalaceBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapRoyalPalaceBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (subscribedController != null) return;
            BuildingInteractionController controller = FindAnyObjectByType<BuildingInteractionController>();
            if (controller == null) return;
            controller.Selection.BuildingClicked += OnBuildingClicked;
            subscribedController = controller;
        }

        private void OnDestroy()
        {
            if (subscribedController != null) subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null || !string.Equals(building.BuildingType, BuildingTypes.RoyalPalace, StringComparison.Ordinal)) return;
            selectedBuilding = building;
            OverlayOpenForExternalHost = true;
            HiveViewProductUiPresenter.RefreshRoyalPalaceUpgradeForExternalHost();
        }

        private void OnGUI()
        {
            if (HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapArmyBootstrap.ModalOpenForExternalHost) return;
            if (HiveViewProductUiPresenter.ColonyOverviewOpenForExternalHost)
            {
                HiveViewProductUiPresenter.DrawColonyOverviewOverlayForExternalHost(Screen.width < 900);
                return;
            }

            if (!OverlayOpenForExternalHost) return;
            BuildingDefinition building = selectedBuilding;
            if (building == null)
            {
                OverlayOpenForExternalHost = false;
                return;
            }

            DrawFullscreenBackground();
            DrawHeader(building);
            if (!OverlayOpenForExternalHost) return;

            float contentWidth = Mathf.Min(ContentMaxWidth, Screen.width - 28f);
            Rect content = new Rect(
                (Screen.width - contentWidth) * 0.5f,
                HeaderHeight + 18f,
                contentWidth,
                Screen.height - HeaderHeight - 36f);

            GUILayout.BeginArea(content);
            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Niveau " + HiveViewProductUiPresenter.RoyalPalaceLevelForExternalHost().ToString(CultureInfo.InvariantCulture), GUI.skin.label);
            GUILayout.FlexibleSpace();
            GUILayout.Label(HiveViewProductUiPresenter.RoyalPalaceLevelAuthorityForExternalHost(), GUI.skin.label);
            GUILayout.EndHorizontal();
            GUILayout.Label("Plafond actuel des autres batiments : niveau du Coeur royal.", new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.EndVertical();

            GUILayout.Space(6f);
            GUILayout.Label(building.Role, new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Space(8f);
            GUILayout.Label(building.Disclosure, new GUIStyle(GUI.skin.label) { wordWrap = true });

            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Amelioration", GUI.skin.label);
            GUILayout.Label(HiveViewProductUiPresenter.RoyalPalaceUpgradeStatusForExternalHost(), new GUIStyle(GUI.skin.label) { wordWrap = true });
            Rect progress = GUILayoutUtility.GetRect(1f, 8f, GUILayout.ExpandWidth(true));
            DrawProgressBar(progress, HiveViewProductUiPresenter.RoyalPalaceUpgradeProgressForExternalHost());
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Vue colonie"))
            {
                OverlayOpenForExternalHost = false;
                HiveViewProductUiPresenter.OpenColonyOverviewForExternalHost();
            }
            bool previousEnabled = GUI.enabled;
            GUI.enabled = HiveViewProductUiPresenter.RoyalPalaceUpgradeActionEnabledForExternalHost();
            if (GUILayout.Button(HiveViewProductUiPresenter.RoyalPalaceUpgradeActionLabelForExternalHost()))
            {
                HiveViewProductUiPresenter.RunRoyalPalaceUpgradeActionForExternalHost();
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static void DrawFullscreenBackground()
        {
            Rect full = new Rect(0f, 0f, Screen.width, Screen.height);
            Color previous = GUI.color;
            GUI.color = new Color(0.006f, 0.005f, 0.004f, 0.99f);
            GUI.DrawTexture(full, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            GUI.color = previous;
        }

        private static void DrawHeader(BuildingDefinition building)
        {
            Rect banner = new Rect(0f, 0f, Screen.width, HeaderHeight);
            Texture2D texture = Resources.Load<Texture2D>("PremiumBeeReference/BuildingBanners/" + building.LegacyKey);
            if (texture != null) GUI.DrawTexture(banner, texture, ScaleMode.ScaleAndCrop, true);
            else
            {
                Color previous = GUI.color;
                GUI.color = new Color(0.14f, 0.085f, 0.025f, 0.98f);
                GUI.DrawTexture(banner, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
                GUI.color = previous;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.58f);
            GUI.DrawTexture(banner, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            GUI.color = previousColor;

            if (HiveViewProductUiPresenter.DrawPremiumBackButtonForExternalHost(new Rect(4f, 2f, 48f, 46f)))
            {
                OverlayOpenForExternalHost = false;
                return;
            }

            GUI.Label(
                new Rect(68f, 12f, Screen.width - 220f, 30f),
                BeeLocalization.Text("building.administration_core.fullscreen_title", "PALAIS ROYAL"),
                new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold });
            GUI.Label(
                new Rect(70f, 42f, Screen.width - 220f, 22f),
                BeeLocalization.Text("building.administration_core.role", "Coeur royal - centre de gestion de la ruche"),
                new GUIStyle(GUI.skin.label) { fontSize = 13 });

            if (GUI.Button(new Rect(Screen.width - 112f, 14f, 96f, 34f), "Rafraichir"))
                HiveViewProductUiPresenter.RefreshRoyalPalaceUpgradeForExternalHost();

            GUI.color = new Color(1f, 0.60f, 0.14f, 0.95f);
            GUI.DrawTexture(new Rect(0f, HeaderHeight - 1f, Screen.width, 1f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
        }

        private static void DrawProgressBar(Rect rect, float value)
        {
            GUI.Box(rect, string.Empty);
            float width = Mathf.Max(0f, rect.width - 4f) * Mathf.Clamp01(value);
            if (width <= 0f) return;
            Color previous = GUI.color;
            GUI.color = new Color(0.95f, 0.72f, 0.22f, 0.95f);
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, width, Mathf.Max(1f, rect.height - 4f)), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
