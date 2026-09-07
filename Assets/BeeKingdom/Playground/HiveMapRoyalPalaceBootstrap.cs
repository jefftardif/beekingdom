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
        // La fenetre affiche desormais niveau + prochain niveau + conditions + couts +
        // deblocages : sur un ecran mobile ca deborde, d'ou le defilement.
        private Vector2 royalPalaceScroll;

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
            try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyWindowOpened("administration_core"); } catch {}
            try { BeeKingdom.Tutorial.TutorialGameplayNotifier.NotifyBuildingSelected("administration_core"); } catch {}
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
            royalPalaceScroll = GUILayout.BeginScrollView(royalPalaceScroll);
            GUILayout.Space(8f);

            // --- Niveau actuel = niveau de la colonie (source unique : administration_core) ---
            int currentLevel = HiveViewProductUiPresenter.RoyalPalaceLevelForExternalHost();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(
                BeeLocalization.Text("royal_palace.current_level", "Palais Royal") + " - " +
                BeeLocalization.Text("royal_palace.level_word", "Niveau") + " " +
                currentLevel.ToString(CultureInfo.InvariantCulture),
                new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            GUILayout.FlexibleSpace();
            GUILayout.Label(HiveViewProductUiPresenter.RoyalPalaceLevelAuthorityForExternalHost(), GUI.skin.label);
            GUILayout.EndHorizontal();
            GUILayout.Label(
                BeeLocalization.Text("royal_palace.colony_level", "Le niveau du Palais Royal est le niveau de la colonie."),
                new GUIStyle(GUI.skin.label) { wordWrap = true });
            if (HiveViewProductUiPresenter.RoyalPalaceProgressionIsAlphaForExternalHost())
                GUILayout.Label(
                    BeeLocalization.Text("royal_palace.alpha_balance", "Equilibrage Alpha provisoire."),
                    new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true });
            GUILayout.EndVertical();

            // --- Prochain niveau : conditions, cout, duree, deblocages ---
            if (HiveViewProductUiPresenter.RoyalPalaceProgressionAvailableForExternalHost())
                DrawNextLevelSection();

            GUILayout.Space(6f);
            GUILayout.Label(building.Role, new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Space(8f);
            GUILayout.Label(building.Disclosure, new GUIStyle(GUI.skin.label) { wordWrap = true });

            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(BeeLocalization.Text("royal_palace.upgrade", "Amelioration"), GUI.skin.label);
            GUILayout.Label(HiveViewProductUiPresenter.RoyalPalaceUpgradeStatusForExternalHost(), new GUIStyle(GUI.skin.label) { wordWrap = true });
            Rect progress = GUILayoutUtility.GetRect(1f, 8f, GUILayout.ExpandWidth(true));
            DrawProgressBar(progress, HiveViewProductUiPresenter.RoyalPalaceUpgradeProgressForExternalHost());
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
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

        // M055-CL. Repond, dans l'ordre, aux questions posees par la mission :
        // quel est le prochain niveau, que faut-il pour l'atteindre (satisfait vs manquant,
        // visuellement distincts), combien ca coute, combien de temps ca prend, et ce que
        // ca debloque. Toutes les valeurs viennent du serveur - rien n'est calcule ici.
        private void DrawNextLevelSection()
        {
            GUILayout.Space(8f);
            GUILayout.BeginVertical(GUI.skin.box);

            if (HiveViewProductUiPresenter.RoyalPalaceAtMaxLevelForExternalHost())
            {
                GUILayout.Label(
                    BeeLocalization.Text("royal_palace.max_level", "Niveau maximum disponible pour l'Alpha atteint."),
                    new GUIStyle(GUI.skin.label) { wordWrap = true });
                GUILayout.EndVertical();
                return;
            }

            int nextLevel = HiveViewProductUiPresenter.RoyalPalaceNextLevelForExternalHost();
            GUILayout.Label(
                BeeLocalization.Text("royal_palace.next_level", "Prochain niveau") + " - " +
                BeeLocalization.Text("royal_palace.level_word", "Niveau") + " " +
                nextLevel.ToString(CultureInfo.InvariantCulture),
                new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });

            string description = HiveViewProductUiPresenter.RoyalPalaceNextLevelDescriptionForExternalHost();
            if (!string.IsNullOrEmpty(description))
                GUILayout.Label(description, new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });

            // Conditions : coche verte quand satisfaite, croix rouge quand manquante.
            int requirementCount = HiveViewProductUiPresenter.RoyalPalaceRequirementCountForExternalHost();
            GUILayout.Space(4f);
            GUILayout.Label(BeeLocalization.Text("royal_palace.requirements", "Conditions"), GUI.skin.label);
            if (requirementCount == 0)
                GUILayout.Label(
                    BeeLocalization.Text("royal_palace.requirements.none", "Aucune condition de batiment."),
                    new GUIStyle(GUI.skin.label) { fontSize = 11 });
            for (int index = 0; index < requirementCount; index++)
            {
                bool satisfied;
                string label = HiveViewProductUiPresenter.RoyalPalaceRequirementLabelForExternalHost(index, out satisfied);
                Color previous = GUI.color;
                GUI.color = satisfied ? new Color(0.55f, 0.92f, 0.55f, 1f) : new Color(1f, 0.52f, 0.45f, 1f);
                GUILayout.Label((satisfied ? "✓  " : "✕  ") + label,
                    new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12 });
                GUI.color = previous;
            }

            // Cout et duree reels du palier (catalogue serveur existant).
            string cost = HiveViewProductUiPresenter.RoyalPalaceNextCostTextForExternalHost();
            string duration = HiveViewProductUiPresenter.RoyalPalaceNextDurationTextForExternalHost();
            if (!string.IsNullOrEmpty(cost) || !string.IsNullOrEmpty(duration))
            {
                GUILayout.Space(4f);
                GUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(cost))
                    GUILayout.Label(BeeLocalization.Text("royal_palace.cost", "Cout") + " : " + cost, GUI.skin.label);
                GUILayout.FlexibleSpace();
                if (!string.IsNullOrEmpty(duration))
                    GUILayout.Label(BeeLocalization.Text("royal_palace.duration", "Duree") + " : " + duration, GUI.skin.label);
                GUILayout.EndHorizontal();
            }

            // Deblocages : on n'annonce comme "debloque" que ce qui est reellement impose
            // aujourd'hui ; le reste est explicitement presente comme a venir.
            int unlockCount = HiveViewProductUiPresenter.RoyalPalaceNextUnlockCountForExternalHost();
            if (unlockCount > 0)
            {
                GUILayout.Space(4f);
                GUILayout.Label(
                    string.Format(CultureInfo.InvariantCulture,
                        BeeLocalization.Text("royal_palace.unlocks_at", "Debloque au niveau {0}"),
                        nextLevel.ToString(CultureInfo.InvariantCulture)),
                    GUI.skin.label);
                for (int index = 0; index < unlockCount; index++)
                {
                    bool enforced;
                    string label = HiveViewProductUiPresenter.RoyalPalaceNextUnlockLabelForExternalHost(index, out enforced);
                    GUILayout.Label(
                        "•  " + label + (enforced ? string.Empty : "  (" + BeeLocalization.Text("royal_palace.unlock.upcoming", "a venir") + ")"),
                        new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });
                }
            }

            // Blocage : jamais un simple bouton grise - on dit exactement ce qui manque et
            // on propose d'aller vers le batiment concerne.
            string blockedReason = HiveViewProductUiPresenter.RoyalPalaceBlockedReasonForExternalHost();
            if (!string.IsNullOrEmpty(blockedReason))
            {
                GUILayout.Space(6f);
                Color previous = GUI.color;
                GUI.color = new Color(1f, 0.52f, 0.45f, 1f);
                GUILayout.Label(blockedReason, new GUIStyle(GUI.skin.label) { wordWrap = true, fontStyle = FontStyle.Bold });
                GUI.color = previous;
                if (!string.IsNullOrEmpty(HiveViewProductUiPresenter.RoyalPalaceBlockingBuildingTypeForExternalHost())
                    && GUILayout.Button(BeeLocalization.Text("royal_palace.goto_building", "Voir le batiment requis")))
                {
                    HiveViewProductUiPresenter.TryFocusRoyalPalaceBlockingBuildingForExternalHost();
                    OverlayOpenForExternalHost = false;
                }
            }

            GUILayout.EndVertical();
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
