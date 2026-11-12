using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Localization;
using BeeKingdom.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    // Official Hall: full-screen presentation, server-authoritative roster and
    // mutations; never invent local ownership, costs, or assigned champions.
    public sealed class HiveMapChampionHallBootstrap : MonoBehaviour
    {
        private const string RuntimeRootName = "HiveMap Champion Hall Runtime";
        private const float PollSeconds = 20f;
        private static readonly Color Background = new Color(.025f, .017f, .010f, .99f);
        private static readonly Color Card = new Color(.090f, .054f, .029f, .98f);
        private static readonly Color Gold = new Color(.98f, .73f, .28f);
        private static readonly Color Muted = new Color(.79f, .72f, .60f);
        private static readonly Color Bright = new Color(1f, .95f, .83f);

        public static bool OverlayOpenForExternalHost { get; private set; }
        private BuildingDefinition selectedBuilding;
        private BuildingInteractionController subscribedController;
        private RemoteChampionBeeSnapshot snapshot;
        private Vector2 scroll;
        private bool busy;
        private bool mutating;
        private bool error;
        private float nextRefreshAt;
        private int requestVersion;
        private Guid currentHive;
        private string feedbackKey = "status.loading";
        private string selectedRole = "";
        private string selectedBeeId = "";
        private string pendingUpgradeBeeId = "";
        private const int ServerChampionMaxLevel = 10;
        private Texture2D hallBanner;

        private static string T(string key, string fallback) => BeeLocalization.Text("champion_hall." + key, fallback);
        private static string ChampionName(ChampionBeeDefinition bee) =>
            BeeLocalization.Text("champion_bees.name." + bee.Id, bee.FallbackName);
        private static string RoleName(ChampionBeeRole role)
        {
            switch (role)
            {
                case ChampionBeeRole.Guardians: return BeeLocalization.Text("champion_bees.role.guardians", "Gardiennes");
                case ChampionBeeRole.Wingrunners: return BeeLocalization.Text("champion_bees.role.wingrunners", "Voltigeuses");
                case ChampionBeeRole.Darters: return BeeLocalization.Text("champion_bees.role.darters", "Lanceuses");
                default: return BeeLocalization.Text("champion_bees.role.civilian", "Collecte");
            }
        }
        private static string RarityName(ChampionBeeRarity rarity) =>
            rarity == ChampionBeeRarity.Legendary
                ? BeeLocalization.Text("champion_bees.rarity.legendary", "Légendaire")
                : BeeLocalization.Text("champion_bees.rarity.rare", "Rare");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (!Application.isPlaying) return;
            Scene active = SceneManager.GetActiveScene();
            if (!IsEnvironmentScene(active) || FindAnyObjectByType<HiveMapChampionHallBootstrap>() != null) return;
            InitializeForScene(active);
        }

        private static bool IsEnvironmentScene(Scene scene) =>
            scene.IsValid() && scene.isLoaded && scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal);

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying || !IsEnvironmentScene(scene)
                || FindAnyObjectByType<HiveMapChampionHallBootstrap>() != null) return;
            GameObject root = new GameObject(RuntimeRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<HiveMapChampionHallBootstrap>();
        }

        private void Update()
        {
            if (!HiveViewProductUiPresenter.HasEnteredHiveForExternalHost) return;
            if (subscribedController == null)
            {
                BuildingInteractionController controller = FindAnyObjectByType<BuildingInteractionController>();
                if (controller != null)
                {
                    controller.Selection.BuildingClicked += OnBuildingClicked;
                    subscribedController = controller;
                }
            }

            if (!OverlayOpenForExternalHost) return;
            Guid hive = MobileAccountSessionRuntimeBootstrap.GameplayHiveId;
            if (hive != currentHive)
            {
                currentHive = hive;
                snapshot = null;
                busy = false;
                mutating = false;
                pendingUpgradeBeeId = "";
                unchecked { requestVersion++; }
                feedbackKey = "status.loading";
                nextRefreshAt = Time.unscaledTime;
            }
            if (!busy && !mutating && Time.unscaledTime >= nextRefreshAt)
            {
                nextRefreshAt = Time.unscaledTime + PollSeconds;
                RefreshChampionState();
            }
        }

        private void OnDestroy()
        {
            unchecked { requestVersion++; }
            if (subscribedController != null)
                subscribedController.Selection.BuildingClicked -= OnBuildingClicked;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (building == null ||
                !string.Equals(building.BuildingType, BuildingTypes.ChampionHall, StringComparison.Ordinal)) return;
            selectedBuilding = building;
            OverlayOpenForExternalHost = true;
            busy = false;
            mutating = false;
            error = false;
            scroll = Vector2.zero;
            currentHive = MobileAccountSessionRuntimeBootstrap.GameplayHiveId;
            unchecked { requestVersion++; }
            snapshot = null;
            selectedRole = "";
            selectedBeeId = "";
            pendingUpgradeBeeId = "";
            feedbackKey = "status.loading";
            nextRefreshAt = Time.unscaledTime + PollSeconds;
            RefreshChampionState();
        }

        private void Close()
        {
            OverlayOpenForExternalHost = false;
            selectedBuilding = null;
            snapshot = null;
            busy = false;
            mutating = false;
            pendingUpgradeBeeId = "";
            unchecked { requestVersion++; }
        }

        private static void DrawSurface(Rect rect, Color fill, Color outline)
        {
            Color old = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = outline;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1.5f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1.5f, rect.width, 1.5f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1.5f, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 1.5f, rect.y, 1.5f, rect.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static GUIStyle Label(int size, Color color, bool bold = false)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = color },
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
        }

        private static bool Button(Rect rect, string text, bool enabled = true,
            bool primary = false, bool showUpgradeIcon = false)
        {
            DrawSurface(rect,
                enabled ? (primary ? new Color(.81f, .51f, .16f, 1f) : new Color(.21f, .135f, .07f, 1f))
                        : new Color(.12f, .085f, .055f, 1f),
                enabled ? Gold : Muted);
            bool previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            GUIStyle style = Label(Mathf.Clamp((int)(rect.height * .34f), 13, 19),
                enabled ? (primary ? Color.black : Bright) : Muted, true);
            style.alignment = TextAnchor.MiddleCenter;
            style.padding = new RectOffset(8, 8, 3, 3);
            Texture2D upgradeIcon = showUpgradeIcon
                ? Resources.Load<Texture2D>("PremiumBeeIcons/upgrade") : null;
            if (upgradeIcon != null) style.imagePosition = ImagePosition.ImageLeft;
            bool clicked = GUI.Button(rect, upgradeIcon != null
                ? new GUIContent(text, upgradeIcon) : new GUIContent(text), style);
            GUI.enabled = previous;
            return clicked && enabled;
        }
        private void OnGUI()
        {
            if (!OverlayOpenForExternalHost || selectedBuilding == null) return;
            if (HiveMapActivitiesBootstrap.ModalOpenForExternalHost || HiveMapArmyBootstrap.ModalOpenForExternalHost) return;

            Rect safe = WorldMapHudLayout.SafeGuiRect(Screen.width,Screen.height,Screen.safeArea);
            bool compact = safe.width < 900f;
            float pad = compact ? 12f : 26f;
            Rect full = new Rect(0, 0, Screen.width, Screen.height);
            DrawSurface(full, Background, new Color(.38f, .24f, .09f));
            Rect header = new Rect(safe.x+pad,safe.y+8f,safe.width-2f*pad,compact?114f:96f);
            if (hallBanner == null) hallBanner = Resources.Load<Texture2D>(HiveMapPremiumBuildingWindow.BannerResourcePathForProof(
                BuildingLegacyKeys.ArchivesHoneyfall));
            if (hallBanner != null) GUI.DrawTexture(header, hallBanner, ScaleMode.ScaleAndCrop, true);
            DrawSurface(header, new Color(.06f, .034f, .018f, .78f), Gold);

            if (HiveViewProductUiPresenter.DrawPremiumBackButtonForExternalHost(new Rect(header.x + 8f, header.y + 8f, 48f, 48f)))
            { Close(); return; }
            float titleX = header.x + (compact ? 12f : 126f);
            GUI.Label(new Rect(titleX, header.y + (compact ? 60f : 5f),
                compact ? header.width - 24f : header.width - 340f,
                compact ? 27f : 39f),
                T("title", "HALL DES CHAMPIONNES"), Label(compact ? 19 : 28, Gold, true));
            GUI.Label(new Rect(titleX, header.y + (compact ? 86f : 43f),
                compact ? header.width - 24f : header.width - 340f, 23f),
                T("subtitle", "Collection, talents et affectations officielles"),
                Label(compact ? 12 : 16, Muted));
            // The only building upgrade is the top-right, matching the other premium windows.
            if (Button(new Rect(header.xMax - (compact ? 130f : 192f), header.y + 13f,
                compact ? 124f : 180f, 48f), T("upgrade_building", "Améliorer"), !mutating, false, true))
            {
                var key = selectedBuilding.LegacyKey;
                Close();
                HiveViewProductUiPresenter.OpenConstructionOverlayForExternalHost(key);
                return;
            }

            float y = header.yMax + 14f;
            Rect summary = new Rect(header.x,y,header.width,compact?105f:106f);
            DrawSurface(summary, Card, new Color(.57f, .37f, .13f));
            int owned = snapshot?.Levels?.Count ?? 0;
            int assigned = snapshot?.AssignedBeeIds?.Count ?? 0;
            int maximum = snapshot?.MaxAssigned ?? 0;
            GUI.Label(new Rect(summary.x + 18f, summary.y + 10f, summary.width - 36f, 33f),
                string.Format(CultureInfo.CurrentCulture, T("summary", "Possédées : {0}/{1}  ·  En mission : {2}/{3}"),
                    snapshot == null ? "-" : owned.ToString(), ChampionBeeCatalog.All.Count,
                    snapshot == null ? "-" : assigned.ToString(), snapshot == null ? "-" : maximum.ToString()),
                Label(compact ? 14 : 21, Bright, true));
            string info = snapshot == null ? T("status.server_required", "Session officielle requise")
                : T("status.connected", "Synchronisation officielle active");
            if (busy && snapshot == null) info = T("status.loading", "Synchronisation en cours...");
            else if (mutating) info = T("status.saving", "Enregistrement sur le serveur...");
            else if (error) info = T(feedbackKey, "Serveur indisponible, données non confirmées");
            else if (feedbackKey == "status.saved") info = T("status.saved", "Mise à jour confirmée");
            GUI.Label(new Rect(summary.x + 18f, summary.y + 47f, summary.width - 36f, 28f),
                info, Label(compact ? 12 : 16, error ? new Color(1f, .55f, .45f) : Muted));

            y = summary.yMax + 12f;
            Rect filters = new Rect(header.x,y,header.width,46f);
            string[] ids = { "", "guardians", "wingrunners", "darters", "civilian" };
            string[] labels = {
                T("all", "Toutes"),
                BeeLocalization.Text("champion_bees.role.guardians", "Gardiennes"),
                BeeLocalization.Text("champion_bees.role.wingrunners", "Voltigeuses"),
                BeeLocalization.Text("champion_bees.role.darters", "Lanceuses"),
                BeeLocalization.Text("champion_bees.role.civilian", "Collecte")
            };
            float tabW = filters.width / ids.Length;
            for (int i = 0; i < ids.Length; i++)
            {
                if (Button(new Rect(filters.x + i * tabW + 2f, y, tabW - 4f, 40f),
                    labels[i], true, selectedRole == ids[i]))
                    selectedRole = ids[i];
            }

            y += 53f;
            Rect viewport = new Rect(header.x,y,header.width,Mathf.Max(48f,safe.yMax-y-18f));
            var filtered = ChampionBeeCatalog.All.Where(b =>
                string.IsNullOrEmpty(selectedRole) || RoleId(b.Role) == selectedRole).ToList();
            float rowH = 220f;
            float contentHeight = 10f;
            foreach (ChampionBeeDefinition bee in filtered)
                contentHeight += rowH + 12f + (selectedBeeId == bee.Id ? 115f : 0f);
            contentHeight = Mathf.Max(viewport.height, contentHeight);
            scroll = GUI.BeginScrollView(viewport, scroll,
                new Rect(0f, 0f, viewport.width - 24f, contentHeight), false, true);

            float rowOffset = 4f;
            foreach (ChampionBeeDefinition bee in filtered)
            {
                float height = rowH + (selectedBeeId == bee.Id ? 115f : 0f);
                DrawChampionRow(new Rect(4f, rowOffset, viewport.width - 34f, height), bee, compact);
                rowOffset += height + 12f;
            }
            GUI.EndScrollView();
        }

        private static string RoleId(ChampionBeeRole role)
        {
            switch (role)
            {
                case ChampionBeeRole.Guardians: return "guardians";
                case ChampionBeeRole.Wingrunners: return "wingrunners";
                case ChampionBeeRole.Darters: return "darters";
                default: return "civilian";
            }
        }

        // Global assignment must not alter a prepared patrol, including when
        // the squad snapshot is still loading or otherwise unavailable.
        private static bool AssignmentLockedByPatrol()
        {
            IHiveSquadReservationPanelController reservation =
                MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap;
            HiveSquadReservationScreenModel model = reservation?.Model;
            return reservation == null || !reservation.IsConfigured || model == null
                || model.State != HiveSquadReservationScreenState.Ready
                || model.HasReservation;
        }
        private void DrawChampionRow(Rect row, ChampionBeeDefinition bee, bool compact)
        {
            DrawSurface(row, Card, new Color(.51f, .32f, .11f));
            int lvl = 0;
            bool owned = snapshot?.Levels != null && snapshot.Levels.TryGetValue(bee.Id, out lvl);
            bool isAssigned = snapshot?.AssignedBeeIds != null && snapshot.AssignedBeeIds.Contains(bee.Id);
            bool expanded = selectedBeeId == bee.Id;
            float left = row.x + 16f;
            float width = row.width - 32f;
            // Every champion uses the premium art already provided in Resources.
            // Art lookup is visual only; ID/ownership always come from the server.
            float portraitSide = compact ? 76f : 96f;
            Rect portrait = new Rect(left, row.y + 12f, portraitSide, portraitSide);
            DrawSurface(portrait, new Color(.07f, .04f, .02f), Gold);
            Texture2D artwork = Resources.Load<Texture2D>("PremiumBeeReference/ChampionBees/" + bee.Id);
            if (artwork != null)
                GUI.DrawTexture(new Rect(portrait.x + 3f, portrait.y + 3f,
                    portrait.width - 6f, portrait.height - 6f), artwork, ScaleMode.ScaleToFit, true);
            float textX = left + portraitSide + 12f;
            float textWidth = row.xMax - textX - 12f;
            GUI.Label(new Rect(textX, row.y + 7f, textWidth - 118f, 29f),
                ChampionName(bee), Label(compact ? 17 : 22, Gold, true));
            GUI.Label(new Rect(textX, row.y + 40f, textWidth, 24f),
                RarityName(bee.Rarity) + "  ·  " + RoleName(bee.Role),
                Label(compact ? 12 : 15, Muted));
            string state = snapshot == null ? T("unknown", "État indisponible")
                : owned ? string.Format(CultureInfo.CurrentCulture,
                    T("owned", "Niveau {0}{1}"), lvl,
                    isAssigned ? " · " + T("assigned", "En mission") : " · " + T("available", "Disponible"))
                : T("not_owned", "Non possédée");
            bool inPreparedPatrol = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap?.Model?.ChampionBeeIds?.Contains(bee.Id) ?? false;
            if (inPreparedPatrol) state += " · " + T("in_patrol", "Dans la patrouille préparée");
            GUI.Label(new Rect(textX, row.y + 70f, textWidth, 26f),
                state, Label(compact ? 12 : 16, Bright, true));
            if (Button(new Rect(row.xMax - 126f, row.y + 8f, 114f, 30f),
                expanded ? T("less", "Réduire") : T("details", "Détails")))
            {
                selectedBeeId = expanded ? "" : bee.Id;
                pendingUpgradeBeeId = "";
            }
            string effect = bee.Role == ChampionBeeRole.Civilian
                ? string.Format(CultureInfo.CurrentCulture,
                    T("production_effect", "Production (catalogue) : +{0}% par niveau"),
                    bee.ProductionBonusPercentPerLevel.ToString("0.#", CultureInfo.InvariantCulture))
                : string.Format(CultureInfo.CurrentCulture,
                    T("combat_effect", "Bonus de puissance au combat : +{0}% par niveau"),
                    bee.RoleStatBonusPercentPerLevel.ToString("0.#", CultureInfo.InvariantCulture));
            GUI.Label(new Rect(left, row.y + 115f, width - 12f, 26f), effect, Label(compact ? 12 : 15, Muted));

            if (owned && lvl < ServerChampionMaxLevel)
            {
                Vector2 cost = ChampionBeeCatalog.LevelUpCost(bee, lvl);
                GUI.Label(new Rect(left, row.y + 144f, width - 12f, 24f),
                    string.Format(CultureInfo.CurrentCulture,
                        T("level_cost", "Prochain niveau : {0} miel + {1} pollen"),
                        Mathf.RoundToInt(cost.x), Mathf.RoundToInt(cost.y)),
                    Label(compact ? 12 : 15, Gold, true));
            }
            else if (owned)
            {
                GUI.Label(new Rect(left, row.y + 144f, width - 12f, 24f),
                    T("max_level", "Niveau maximum atteint"), Label(compact ? 12 : 15, Muted));
            }

            if (expanded)
            {
                GUI.Label(new Rect(left, row.y + 176f, width - 12f, 83f),
                    BeeLocalization.Text("champion_bees.lore." + bee.Id, bee.FallbackLore),
                    Label(compact ? 11 : 14, Bright));
                if (bee.Role != ChampionBeeRole.Civilian)
                {
                    GUI.Label(new Rect(left, row.y + 262f, width - 12f, 20f),
                        string.Format(CultureInfo.CurrentCulture,
                            T("global_bonus", "Bonus global : +{0}% par niveau"),
                            bee.GlobalStatBonusPercentPerLevel.ToString("0.#", CultureInfo.InvariantCulture)),
                        Label(compact ? 11 : 13, Muted));
                }
            }

            if (snapshot == null) return;
            float buttonY = row.yMax - 51f;
            float gap = 8f;
            float buttonWidth = compact ? (width - gap) * .5f : Mathf.Min(202f, (width - gap) * .5f);
            bool hasPreparedPatrol = MobileAccountSessionRuntimeBootstrap.SquadReservationControllerForHiveMap?.Model?.HasReservation ?? false;
            bool assignmentLocked = AssignmentLockedByPatrol();
            if (owned)
            {
                bool pending = pendingUpgradeBeeId == bee.Id;
                bool canLevelUp = !mutating && !error && lvl < ServerChampionMaxLevel;
                if (Button(new Rect(left, buttonY, buttonWidth, 42f),
                    pending ? T("confirm", "Confirmer") : T("level_up", "Améliorer la championne"),
                    canLevelUp, true))
                {
                    if (pending)
                    {
                        pendingUpgradeBeeId = "";
                        MutateChampion("level", bee);
                    }
                    else pendingUpgradeBeeId = bee.Id;
                }
                bool assignmentLimit = snapshot.AssignedBeeIds != null &&
                    snapshot.AssignedBeeIds.Count >= snapshot.MaxAssigned && !isAssigned;
                if (Button(new Rect(left + buttonWidth + gap, buttonY, buttonWidth, 42f),
                    pending ? T("cancel", "Annuler")
                    : hasPreparedPatrol ? T("patrol_locked", "Patrouille verrouillée")
                    : assignmentLocked ? T("assignment_loading", "Affectations en synchronisation")
                    : assignmentLimit ? T("slots_full", "Affectations complètes")
                    : isAssigned ? T("unassign", "Retirer") : T("assign", "Affecter"),
                    pending || (!mutating && !error && !assignmentLocked && !assignmentLimit)))
                {
                    if (pending) pendingUpgradeBeeId = "";
                    else MutateChampion("assignment", bee);
                }
            }
            else
            {
                GUI.Label(new Rect(left, buttonY, width, 38f),
                    string.Format(CultureInfo.CurrentCulture,
                        T("unlock", "Déblocage à partir du Cœur royal niveau {0}"),
                        ChampionBeeCatalog.UnlockCoeurRoyalLevel(bee.Rarity)),
                    Label(compact ? 12 : 15, Muted));
            }
        }
        private async void RefreshChampionState()
        {
            if (busy || mutating || !OverlayOpenForExternalHost) return;
            IHiveChampionBeeClient client = MobileAccountSessionRuntimeBootstrap.ChampionBeeClient;
            Guid hive = MobileAccountSessionRuntimeBootstrap.GameplayHiveId;
            if (client == null || hive == Guid.Empty)
            {
                snapshot = null;
                error = true;
                feedbackKey = "status.server_required";
                return;
            }
            int version = requestVersion;
            busy = true;
            // A background refresh must not change visible UI or action state.
            if (snapshot == null) error = false;
            try
            {
                RemoteChampionBeeSnapshot result = await client.ReadAsync(hive);
                if (!OverlayOpenForExternalHost || version != requestVersion
                    || hive != MobileAccountSessionRuntimeBootstrap.GameplayHiveId) return;
                snapshot = result;
                error = false;
                feedbackKey = "status.connected";
            }
            catch (Exception)
            {
                if (version == requestVersion && snapshot == null)
                {
                    error = true;
                    feedbackKey = "status.error";
                }
            }
            finally { if (version == requestVersion) busy = false; }
        }

        private async void MutateChampion(string operation, ChampionBeeDefinition bee)
        {
            if (mutating || snapshot == null || error) return;
            if (busy)
            {
                // A click takes priority over passive polling. Invalidate the
                // pending GET so its late result can never overwrite the
                // authoritative reply to this mutation.
                unchecked { requestVersion++; }
                busy = false;
            }
            IHiveChampionBeeClient client = MobileAccountSessionRuntimeBootstrap.ChampionBeeClient;
            Guid hive = MobileAccountSessionRuntimeBootstrap.GameplayHiveId;
            if (client == null || hive == Guid.Empty) return;
            var nextIds = new List<string>(snapshot.AssignedBeeIds ?? new List<string>());
            if (operation == "assignment")
            {
                if (AssignmentLockedByPatrol()) return;
                if (nextIds.Contains(bee.Id)) nextIds.Remove(bee.Id);
                else
                {
                    if (nextIds.Count >= snapshot.MaxAssigned) return;
                    nextIds.Add(bee.Id);
                }
            }
            mutating = true;
            int version = requestVersion;
            long revision = snapshot.Revision;
            try
            {
                string idempotency = "hall-" + operation + "-" + Guid.NewGuid().ToString("N");
                RemoteChampionBeeMutationResult result = operation == "level"
                    ? await client.LevelUpAsync(hive, bee.Id, revision, idempotency)
                    : await client.SetAssignmentAsync(hive, nextIds, revision, idempotency);
                if (!OverlayOpenForExternalHost || version != requestVersion
                    || hive != MobileAccountSessionRuntimeBootstrap.GameplayHiveId) return;
                if (result.Succeeded)
                {
                    // The server mutation response is authoritative. Apply
                    // only the returned level, assignment and revision while
                    // preserving all other previously confirmed data. Avoid
                    // clearing the full UI to Loading after a user action.
                    RemoteChampionBeeSnapshot previous = snapshot;
                    var levels = new Dictionary<string, int>(
                        previous.Levels ?? new Dictionary<string, int>(),
                        StringComparer.Ordinal);
                    if (operation == "level" && result.Level > 0 &&
                        !string.IsNullOrWhiteSpace(result.BeeId))
                        levels[result.BeeId] = result.Level;
                    snapshot = new RemoteChampionBeeSnapshot
                    {
                        Levels = levels,
                        AssignedBeeIds = result.AssignedBeeIds != null
                            ? new List<string>(result.AssignedBeeIds)
                            : new List<string>(previous.AssignedBeeIds ?? new List<string>()),
                        MaxAssigned = previous.MaxAssigned,
                        Revision = result.Revision
                    };
                    // Reconcile soon via a quiet GET; no spinner, reset or
                    // input interruption during normal background reads.
                    nextRefreshAt = Time.unscaledTime + 2f;
                    feedbackKey = "status.saved";
                    error = false;
                }
                else
                {
                    feedbackKey = "status.error";
                    error = true;
                    nextRefreshAt = Time.unscaledTime;
                }
            }
            catch (Exception)
            {
                if (version == requestVersion)
                {
                    feedbackKey = "status.error";
                    error = true;
                    nextRefreshAt = Time.unscaledTime;
                }
            }
            finally
            {
                if (version == requestVersion) mutating = false;
            }
        }
    }
}