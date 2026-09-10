using System;
using System.Collections.Generic;
using System.Globalization;
using BeeKingdom.Core.Integration;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // M016-CX: replaces the fake uGUI Activities rows with a fullscreen HiveMap modal
    // backed by the existing Daily Round and Milestone Event controllers.
    public sealed class HiveMapActivitiesBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Activities Runtime";
        private const float HeaderHeight = 116f;
        private const float ContentMaxWidth = 720f;

        public static bool ModalOpenForExternalHost { get; private set; }

        private Vector2 scroll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active)) return;
            if (FindFirstObjectByType<HiveMapActivitiesBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, active);
            root.AddComponent<HiveMapActivitiesBootstrap>();
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
            if (FindFirstObjectByType<HiveMapActivitiesBootstrap>() != null) return;

            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapActivitiesBootstrap>();
        }

        private void Start()
        {
            LivingHiveActivitiesBridge.SetHandlers(() => ModalOpenForExternalHost, OpenModal);
        }

        private void OnGUI()
        {
            if (!ModalOpenForExternalHost) return;

            DrawFullscreenBackground();
            DrawHeader();
            if (!ModalOpenForExternalHost) return;

            float contentWidth = Mathf.Min(ContentMaxWidth, Screen.width - 28f);
            Rect content = new Rect(
                (Screen.width - contentWidth) * 0.5f,
                HeaderHeight + 14f,
                contentWidth,
                Screen.height - HeaderHeight - 28f);

            GUILayout.BeginArea(content);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(content.width), GUILayout.Height(content.height));
            DrawDailyRoundSection();
            GUILayout.Space(10f);
            DrawMilestoneEventSection();
            GUILayout.Space(10f);
            DrawQuestChainSection();
            GUILayout.Space(8f);
            GUILayout.Label(
                Text(
                    "Le Centre des missions reste exclu de M016 : le catalogue actuel est une présentation legacy/locale sans modèle serveur comparable à la Ronde quotidienne ou à l'Événement jalon.",
                    "Mission Center stays out of M016: the current catalog is legacy/local presentation without a server model comparable to Daily Round or Milestone Event."),
                new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static void OpenModal()
        {
            ModalOpenForExternalHost = true;
            MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession();
            MobileAccountSessionRuntimeBootstrap.DailyRoundControllerForHiveMap.Refresh();
            MobileAccountSessionRuntimeBootstrap.MilestoneEventControllerForHiveMap.Refresh();
            MobileAccountSessionRuntimeBootstrap.QuestChainControllerForHiveMap.Refresh();
        }

        private static void DrawDailyRoundSection()
        {
            IHiveDailyRoundPanelController controller =
                MobileAccountSessionRuntimeBootstrap.DailyRoundControllerForHiveMap;
            HiveDailyRoundScreenModel model = controller.Model;

            GUILayout.BeginVertical(GUI.skin.box);
            DrawSectionHeader(
                Text("RONDE QUOTIDIENNE", "DAILY ROUND"),
                DailyStateLabel(model),
                controller.IsBusy);

            if (model.State == HiveDailyRoundScreenState.NotConfigured)
            {
                DrawWrapped(Text(
                    "Session officielle non configurée. Aucune tâche, progression ou récompense n'est simulée.",
                    "Official session is not configured. No task, progress or reward is simulated."));
            }
            else
            {
                DrawWrapped(Text(
                    "Progression : " + model.CompletedCount.ToString(CultureInfo.InvariantCulture) + " / 3",
                    "Progress: " + model.CompletedCount.ToString(CultureInfo.InvariantCulture) + " / 3"));
                // M078B-CL: 3 vraies missions comprehensibles au lieu de signaux techniques
                // ("Collecte reçue"/"Opération lancée"/"Stocks lus") - voir
                // M078B-CL-Daily-Round-Player-Friendly-Objectives.md pour ce que signifiait
                // reellement chaque ancien signal et ce qui a change cote serveur.
                DrawDailyRoundObjective(
                    Text("Récolteur du royaume", "Kingdom Harvester"),
                    Text("Collecter une ressource (Sac de la ruche ou World Map)", "Collect a resource (hive storage or World Map)"),
                    model.CollectionReceived);
                DrawDailyRoundObjective(
                    Text("Développement de la ruche", "Hive Development"),
                    Text("Lancer une opération (amélioration, recherche, entraînement...)", "Start an operation (upgrade, research, training...)"),
                    model.OperationLaunched);
                DrawDailyRoundObjective(
                    Text("En mission", "On a Mission"),
                    Text("Envoyer une expédition sur la World Map", "Send an expedition on the World Map"),
                    model.SnapshotRead);
                DrawWrapped(Text(
                    "Récompense : " + model.HoneyReward.ToString(CultureInfo.InvariantCulture) + " miel, " + model.PollenReward.ToString(CultureInfo.InvariantCulture) + " pollen",
                    "Reward: " + model.HoneyReward.ToString(CultureInfo.InvariantCulture) + " honey, " + model.PollenReward.ToString(CultureInfo.InvariantCulture) + " pollen"));
                if (model.IsReadOnly)
                    DrawWrapped(Text("Lecture hors ligne protégée : réclamation désactivée.", "Protected offline read: claim disabled."));
                if (model.IsClaimed)
                    DrawWrapped(Text("Récompense déjà réclamée.", "Reward already claimed."));
                if (!string.IsNullOrWhiteSpace(model.ReceiptCode))
                    DrawWrapped(Text("Reçu : ", "Receipt: ") + model.ReceiptCode);
                if (model.State == HiveDailyRoundScreenState.Error)
                    DrawWrapped(Text("Erreur : ", "Error: ") + model.ErrorCode);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Text("Rafraîchir", "Refresh"))) controller.Refresh();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = model.CanClaim && !controller.IsBusy;
            if (GUILayout.Button(Text("Réclamer", "Claim"))) controller.Claim();
            GUI.enabled = model.CanRetryClaim && !controller.IsBusy;
            if (GUILayout.Button(Text("Vérifier", "Verify"))) controller.RetryClaim();
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawMilestoneEventSection()
        {
            IHiveMilestoneEventPanelController controller =
                MobileAccountSessionRuntimeBootstrap.MilestoneEventControllerForHiveMap;
            HiveMilestoneEventScreenModel model = controller.Model;

            GUILayout.BeginVertical(GUI.skin.box);
            DrawSectionHeader(
                Text("ÉVÉNEMENT JALON", "MILESTONE EVENT"),
                MilestoneStateLabel(model),
                controller.IsBusy);

            if (model.State == HiveMilestoneEventScreenState.NotConfigured)
            {
                DrawWrapped(Text(
                    "Aucun événement jalon officiel n'est configuré pour cette session.",
                    "No official milestone event is configured for this session."));
            }
            else
            {
                DrawWrapped(Text(
                    "Objectifs : " + model.CompletedCount.ToString(CultureInfo.InvariantCulture) + " / " + model.RequiredObjectiveCount.ToString(CultureInfo.InvariantCulture),
                    "Objectives: " + model.CompletedCount.ToString(CultureInfo.InvariantCulture) + " / " + model.RequiredObjectiveCount.ToString(CultureInfo.InvariantCulture)));
                if (model.WindowEndsAtUtc != default(DateTimeOffset))
                    DrawWrapped(Text("Fin : ", "Ends: ") + model.WindowEndsAtUtc.ToString("u", CultureInfo.InvariantCulture));

                IReadOnlyList<RemoteHiveMilestoneObjective> objectives =
                    model.Objectives ?? Array.Empty<RemoteHiveMilestoneObjective>();
                if (objectives.Count == 0)
                {
                    DrawWrapped(Text("Aucun objectif reçu du serveur.", "No objective received from the server."));
                }
                else
                {
                    for (int i = 0; i < objectives.Count; i++)
                    {
                        RemoteHiveMilestoneObjective objective = objectives[i];
                        DrawWrapped(StatusMark(objective.Done) + " " + ObjectiveLabel(objective));
                    }
                }

                DrawWrapped(Text("Récompense : ", "Reward: ") + RewardLabel(model.Reward));
                if (model.Claimed)
                    DrawWrapped(Text("Récompense déjà réclamée.", "Reward already claimed."));
                if (model.WindowExpired)
                    DrawWrapped(Text("Fenêtre expirée.", "Window expired."));
                if (model.State == HiveMilestoneEventScreenState.Error)
                    DrawWrapped(Text("Erreur : ", "Error: ") + model.ErrorCode);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Text("Rafraîchir", "Refresh"))) controller.Refresh();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = model.CanClaim && !controller.IsBusy;
            if (GUILayout.Button(Text("Réclamer", "Claim"))) controller.Claim();
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        // M077-CL: petite chaine d'objectifs Alpha - meme emplacement reel que la Ronde
        // quotidienne et l'Evenement jalon ci-dessus (le seul endroit ou la scene officielle
        // Environment2D5D expose deja ce type de contenu), mais reclamation individuelle par
        // objectif plutot qu'un seul bouton pour tout le lot.
        private static void DrawQuestChainSection()
        {
            IQuestChainPanelController controller =
                MobileAccountSessionRuntimeBootstrap.QuestChainControllerForHiveMap;
            QuestChainScreenModel model = controller.Model;

            GUILayout.BeginVertical(GUI.skin.box);
            DrawSectionHeader(
                Text("OBJECTIFS DU ROYAUME", "KINGDOM OBJECTIVES"),
                QuestChainStateLabel(model),
                controller.IsBusy);

            if (model.State == QuestChainScreenState.NotConfigured)
            {
                DrawWrapped(Text(
                    "Aucune chaine d'objectifs officielle n'est configurée pour cette session.",
                    "No official objective chain is configured for this session."));
            }
            else
            {
                DrawWrapped(Text(
                    "Progression : " + model.CompletedCount.ToString(CultureInfo.InvariantCulture) + " / " + (model.Objectives?.Count ?? 0).ToString(CultureInfo.InvariantCulture),
                    "Progress: " + model.CompletedCount.ToString(CultureInfo.InvariantCulture) + " / " + (model.Objectives?.Count ?? 0).ToString(CultureInfo.InvariantCulture)));

                IReadOnlyList<RemoteQuestChainObjective> objectives =
                    model.Objectives ?? Array.Empty<RemoteQuestChainObjective>();
                if (objectives.Count == 0)
                {
                    DrawWrapped(Text("Aucun objectif reçu du serveur.", "No objective received from the server."));
                }
                else
                {
                    for (int i = 0; i < objectives.Count; i++)
                    {
                        RemoteQuestChainObjective objective = objectives[i];
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(QuestChainStatusMark(objective) + " " + QuestChainObjectiveLabel(objective.ObjectiveKey), new GUIStyle(GUI.skin.label) { wordWrap = true });
                        GUILayout.FlexibleSpace();
                        if (objective.Claimed)
                        {
                            GUILayout.Label(Text("Réclamé", "Claimed"), GUI.skin.label);
                        }
                        else
                        {
                            bool previousEnabled = GUI.enabled;
                            GUI.enabled = objective.CanClaim && !controller.IsBusy;
                            string buttonLabel = objective.CanClaim
                                ? Text("Réclamer", "Claim")
                                : "+" + objective.RewardAmount.ToString(CultureInfo.InvariantCulture) + " " + objective.RewardResourceKey;
                            if (GUILayout.Button(buttonLabel)) controller.Claim(objective.ObjectiveKey);
                            GUI.enabled = previousEnabled;
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                if (model.State == QuestChainScreenState.Error)
                    DrawWrapped(Text("Erreur : ", "Error: ") + model.ErrorCode);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Text("Rafraîchir", "Refresh"))) controller.Refresh();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static string QuestChainStateLabel(QuestChainScreenModel model)
        {
            switch (model.State)
            {
                case QuestChainScreenState.Loading: return Text("Chargement", "Loading");
                case QuestChainScreenState.Ready: return model.AnyClaimable ? Text("Récompense prête", "Reward ready") : Text("En cours", "In progress");
                case QuestChainScreenState.Mutating: return Text("Réclamation", "Claiming");
                case QuestChainScreenState.Error: return Text("Erreur", "Error");
                default: return Text("Non configuré", "Not configured");
            }
        }

        private static string QuestChainStatusMark(RemoteQuestChainObjective objective)
        {
            return objective.Claimed ? "[x]" : objective.Done ? "[o]" : "[ ]";
        }

        private static string QuestChainObjectiveLabel(string objectiveKey)
        {
            switch (objectiveKey)
            {
                case "q1_building_upgrade": return Text("Royaume en croissance : améliore un bâtiment", "Growing kingdom: upgrade a building");
                case "q2_troop_recruit": return Text("Préparer la garde : entraîne des troupes", "Prepare the guard: train troops");
                case "q3_research_complete": return Text("Le savoir du royaume : termine une recherche", "Kingdom knowledge: finish a research");
                case "q4_world_map_visit": return Text("Explorer le royaume : ouvre la World Map", "Explore the kingdom: open the World Map");
                case "q5_world_resource_collect": return Text("Richesses sauvages : collecte une ressource sur la World Map", "Wild riches: collect a World Map resource");
                default: return objectiveKey;
            }
        }

        private static void DrawFullscreenBackground()
        {
            Rect full = new Rect(0f, 0f, Screen.width, Screen.height);
            Color previous = GUI.color;
            GUI.color = new Color(0.006f, 0.005f, 0.004f, 0.99f);
            GUI.DrawTexture(full, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            GUI.color = previous;
        }

        private static void DrawHeader()
        {
            Rect banner = new Rect(0f, 0f, Screen.width, HeaderHeight);
            Color previous = GUI.color;
            GUI.color = new Color(0.13f, 0.085f, 0.024f, 0.98f);
            GUI.DrawTexture(banner, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = new Color(0f, 0f, 0f, 0.38f);
            GUI.DrawTexture(banner, Texture2D.blackTexture, ScaleMode.StretchToFill, false);
            GUI.color = previous;

            if (HiveViewProductUiPresenter.DrawPremiumBackButtonForExternalHost(new Rect(4f, 2f, 48f, 46f)))
            {
                ModalOpenForExternalHost = false;
                return;
            }

            GUI.Label(
                new Rect(68f, 12f, Screen.width - 220f, 30f),
                BeeLocalization.Text("ui.activities.fullscreen_title", Text("ACTIVITÉS", "ACTIVITIES")),
                new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold });
            GUI.Label(
                new Rect(70f, 42f, Screen.width - 220f, 22f),
                Text("Ronde quotidienne et événements jalons officiels", "Official daily round and milestone events"),
                new GUIStyle(GUI.skin.label) { fontSize = 13 });

            if (GUI.Button(new Rect(Screen.width - 112f, 14f, 96f, 34f), Text("Rafraîchir", "Refresh")))
            {
                MobileAccountSessionRuntimeBootstrap.DailyRoundControllerForHiveMap.Refresh();
                MobileAccountSessionRuntimeBootstrap.MilestoneEventControllerForHiveMap.Refresh();
                MobileAccountSessionRuntimeBootstrap.QuestChainControllerForHiveMap.Refresh();
            }

            GUI.color = new Color(1f, 0.60f, 0.14f, 0.95f);
            GUI.DrawTexture(new Rect(0f, HeaderHeight - 1f, Screen.width, 1f), Texture2D.whiteTexture, ScaleMode.StretchToFill, false);
            GUI.color = Color.white;
        }

        private static void DrawSectionHeader(string title, string state, bool busy)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 15 });
            GUILayout.FlexibleSpace();
            GUILayout.Label(busy ? Text("Chargement...", "Loading...") : state, GUI.skin.label);
            GUILayout.EndHorizontal();
        }

        private static void DrawWrapped(string text)
        {
            GUILayout.Label(text ?? string.Empty, new GUIStyle(GUI.skin.label) { wordWrap = true });
        }

        // M078B-CL: titre + description en langage joueur, progression numerique (0/1 -> 1/1)
        // au lieu de la case a cocher [ ]/[x] partagee avec l'Evenement jalon (StatusMark) -
        // demande explicite du CEO ("afficher une vraie progression"). Usage local a la Ronde
        // quotidienne uniquement ; StatusMark reste inchange pour l'Evenement jalon.
        private static void DrawDailyRoundObjective(string title, string description, bool done)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label(title, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label(description, new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label((done ? "1" : "0") + " / 1", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.EndHorizontal();
        }

        private static string DailyStateLabel(HiveDailyRoundScreenModel model)
        {
            switch (model.State)
            {
                case HiveDailyRoundScreenState.Loading: return Text("Chargement", "Loading");
                case HiveDailyRoundScreenState.Ready: return model.CanClaim ? Text("Récompense prête", "Reward ready") : Text("Disponible", "Available");
                case HiveDailyRoundScreenState.OfflineReadOnly: return Text("Lecture seule", "Read only");
                case HiveDailyRoundScreenState.PreparingClaim: return Text("Préparation", "Preparing");
                case HiveDailyRoundScreenState.Claiming: return Text("Réclamation", "Claiming");
                case HiveDailyRoundScreenState.ClaimPendingConfirmation: return Text("À vérifier", "Verify required");
                case HiveDailyRoundScreenState.Error: return Text("Erreur", "Error");
                default: return Text("Non configuré", "Not configured");
            }
        }

        private static string MilestoneStateLabel(HiveMilestoneEventScreenModel model)
        {
            switch (model.State)
            {
                case HiveMilestoneEventScreenState.Loading: return Text("Chargement", "Loading");
                case HiveMilestoneEventScreenState.Ready: return model.CanClaim ? Text("Récompense prête", "Reward ready") : Text("Disponible", "Available");
                case HiveMilestoneEventScreenState.Mutating: return Text("Réclamation", "Claiming");
                case HiveMilestoneEventScreenState.Error: return Text("Erreur", "Error");
                default: return Text("Non configuré", "Not configured");
            }
        }

        private static string ObjectiveLabel(RemoteHiveMilestoneObjective objective)
        {
            if (objective == null) return Text("Objectif inconnu", "Unknown objective");
            string key = string.IsNullOrWhiteSpace(objective.ObjectiveKey)
                ? Text("Objectif", "Objective")
                : objective.ObjectiveKey;
            return key;
        }

        private static string RewardLabel(IReadOnlyDictionary<string, long> reward)
        {
            if (reward == null || reward.Count == 0) return Text("aucune récompense reçue", "no reward received");

            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, long> pair in reward)
            {
                parts.Add(pair.Value.ToString(CultureInfo.InvariantCulture) + " " + pair.Key);
            }
            return string.Join(", ", parts);
        }

        private static string StatusMark(bool done)
        {
            return done ? "[x]" : "[ ]";
        }

        private static string Text(string french, string english)
        {
            return string.Equals(BeeLocalization.CurrentLocale, "en-US", StringComparison.OrdinalIgnoreCase)
                ? english
                : french;
        }
    }
}
