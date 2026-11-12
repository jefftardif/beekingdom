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
    public sealed partial class HiveMapPremiumBuildingWindow : MonoBehaviour
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
        private bool defenseMode;
        private bool geneticsMode;
        private TextMeshProUGUI geneticsLevelLabel;
        private TextMeshProUGUI geneticsStatusLabel;
        private TextMeshProUGUI geneticsProgressLabel;
        private TextMeshProUGUI geneticsOfferLabel;
        private float nextGeneticsLabelsAt;
        private float nextGeneticsRefreshAt;
        private IHiveSquadReservationPanelController defenseSquadController;
        private ICombatPatrolPanelController defensePatrolController;
        private HiveDefenseOverviewSnapshot lastKnownDefenseState;
        private HiveSquadReservationScreenModel lastDefenseSquadModel;
        private CombatPatrolScreenModel lastDefensePatrolModel;
        private float nextDefenseLabelsAt;
        private float nextDefenseRefreshAt;
        private TextMeshProUGUI defenseLevelLabel;
        private TextMeshProUGUI defenseStateLabel;
        private TextMeshProUGUI defenseRosterLabel;
        private TextMeshProUGUI defenseReadyLabel;
        private TextMeshProUGUI defenseReservedLabel;
        private TextMeshProUGUI defenseRosterStatusLabel;
        private TextMeshProUGUI defensePatrolLabel;
        private TextMeshProUGUI defenseRecoveryLabel;
        private TextMeshProUGUI defenseChampionLabel;
        private readonly Dictionary<string, TextMeshProUGUI> defenseTroopLines =
            new Dictionary<string, TextMeshProUGUI>(StringComparer.Ordinal);
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
        private bool infirmaryHasLoadedRecovery;
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
        public bool DefenseActiveForProof => windowOpen && defenseMode;
        public bool GeneticsActiveForProof => windowOpen && geneticsMode;

        // The Nursery's genuine server-backed feed/stabilize operations.
        public void BindNurseryController(IHiveBroodVitalityPanelController controller)
        {
            nurseryController = controller;
        }

        // This is the same official account-backed snapshot used by Barracks
        // and Combat Patrol. Defense never modifies these controllers.
        public void BindDefenseControllers(
            IHiveSquadReservationPanelController squad,
            ICombatPatrolPanelController patrol)
        {
            if (!ReferenceEquals(defenseSquadController, squad) ||
                !ReferenceEquals(defensePatrolController, patrol))
                lastKnownDefenseState = null;
            defenseSquadController = squad;
            defensePatrolController = patrol;
            if (defenseMode && windowOpen) RefreshDefenseLabels();
        }
        // Inject the existing shared patrol controller before showing the Infirmary.
        public void BindInfirmaryController(ICombatPatrolPanelController controller)
        {
            if (!ReferenceEquals(infirmaryController, controller)) infirmaryHasLoadedRecovery = false;
            infirmaryController = controller;
            if (infirmaryMode && windowOpen) RefreshInfirmaryLabels();
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
            defenseMode = string.Equals(building.BuildingType,
                BuildingTypes.Defense, StringComparison.Ordinal);
            geneticsMode = string.Equals(building.BuildingType,
                BuildingTypes.Genetics, StringComparison.Ordinal);
            academyMode = string.Equals(building.BuildingType,
                BuildingTypes.Academy, StringComparison.Ordinal);
            if (!nurseryMode) nurseryController = null;
            if (!infirmaryMode) infirmaryController = null;
            if (!defenseMode)
            {
                defenseSquadController = null;
                defensePatrolController = null;
                lastKnownDefenseState = null;
            }
            windowOpen = true;
            suppressed = false;
            nextInfirmaryLabelAt = 0f;
            nextInfirmaryRefreshAt = Time.unscaledTime + 15f;
            nextNurseryLabelsAt = 0f;
            nextNurseryRefreshAt = Time.unscaledTime + 15f;
            nextDefenseRefreshAt = Time.unscaledTime + 30f;
            nextDefenseLabelsAt = 0f;
            nextGeneticsLabelsAt = 0f;
            nextGeneticsRefreshAt = Time.unscaledTime + 20f;
            nextAcademyLabelsAt = 0f;
            nextAcademyRefreshAt = Time.unscaledTime + 12f;
            if (geneticsMode)
                HiveViewProductUiPresenter.RefreshOfficialGeneticsBuildingForExternalHost();
            if (academyMode) RefreshAcademySources();
            Render();
            if (infirmaryMode) RefreshInfirmary();
            if (nurseryMode) RefreshNursery();
            if (defenseMode) RefreshDefense();
            if (geneticsMode) RefreshGeneticsLabels();
            if (academyMode) RefreshAcademyLabels();
            if (productionInfoMode) RefreshProductionInfoLabels();

        }

        public void Hide()
        {
            windowOpen = false;
            infirmaryMode = false;
            nurseryMode = false;
            defenseMode = false;
            geneticsMode = false;
            academyMode = false;
            productionInfoMode = false;
            productionInfoController = null;
            productionInfoStatus = productionInfoStock = productionInfoRate = null;
            productionInfoBalance = productionInfoCapacity = productionInfoResource = null;
            productionInfoHint = null;
            productionInfoRefreshButton = null;
            academyLevelLabel = null;
            academySyncLabel = null;
            academyUpgradeLabel = null;
            academyUpgradeCostLabel = null;
            academyPalaceLabel = null;
            academyPalaceNoteLabel = null;
            academyBarracksLabel = null;
            academyResearchLabel = null;
            geneticsLevelLabel = null;
            geneticsStatusLabel = null;
            geneticsProgressLabel = null;
            geneticsOfferLabel = null;
            defenseSquadController = null;
            defensePatrolController = null;
            lastKnownDefenseState = null;
            lastDefenseSquadModel = null;
            lastDefensePatrolModel = null;
            defenseTroopLines.Clear();
            defenseLevelLabel = null;
            defenseStateLabel = null;
            defenseRosterLabel = null;
            defenseReadyLabel = null;
            defenseReservedLabel = null;
            defenseRosterStatusLabel = null;
            defensePatrolLabel = null;
            defenseRecoveryLabel = null;
            defenseChampionLabel = null;
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
            infirmaryHasLoadedRecovery = false;
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
            if (productionInfoMode) TickProductionInfo();
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
            if (defenseMode)
            {
                float now = Time.unscaledTime;
                if (now >= nextDefenseLabelsAt)
                {
                    nextDefenseLabelsAt = now + .65f;
                    RefreshDefenseLabels();
                }
                if (now >= nextDefenseRefreshAt)
                {
                    nextDefenseRefreshAt = now + 30f;
                    RefreshDefense();
                }
            }
            if (geneticsMode)
            {
                float now = Time.unscaledTime;
                if (now >= nextGeneticsLabelsAt)
                {
                    nextGeneticsLabelsAt = now + 1f;
                    RefreshGeneticsLabels();
                }
                if (now >= nextGeneticsRefreshAt)
                {
                    nextGeneticsRefreshAt = now + 20f;
                    HiveViewProductUiPresenter.RefreshOfficialGeneticsBuildingForExternalHost();
                }
            }
            if (academyMode)
            {
                float now = Time.unscaledTime;
                AcademyTrainingPanelController academy =
                    MobileAccountSessionRuntimeBootstrap.AcademyControllerForHiveMap;
                // Near the deadline, update the four existing labels rather
                // than rebuilding any UI. Server confirmation becomes visible
                // within a frame or two of the authenticated HTTP response.
                float labelsInterval = academy != null &&
                    academy.IsConfigured &&
                    (academy.TrainingElapsed ||
                     academy.Model?.ActiveOperation != null &&
                     academy.Remaining.TotalSeconds < 1d)
                    ? .10f : 1f;
                if (nextAcademyLabelsAt - now > labelsInterval)
                    nextAcademyLabelsAt = now + labelsInterval;
                if (now >= nextAcademyLabelsAt)
                {
                    nextAcademyLabelsAt = now + labelsInterval;
                    RefreshAcademyLabels();
                }
                // Deadline-triggered authoritative GET, issued on the first
                // Update frame at 00:00 rather than waiting for the 12/30s poll.
                // Failures are recovered internally; no player-facing retry.
                if (academy != null && academy.IsConfigured)
                    academy.TickCompletionDeadline();
                // Heavy independent building/research/barracks reads stay
                // infrequent. Deadline and recovery reads address Academy ONLY.
                if (now >= nextAcademyRefreshAt)
                {
                    nextAcademyRefreshAt = now + 30f;
                    RefreshAcademySources();
                }
            }
        }

        private void RefreshDefense()
        {
            if (!defenseMode || !windowOpen) return;
            // Read the same official reservation owned by Barracks; never
            // reserve, release or launch a troop from the Defense view.
            if (defenseSquadController != null &&
                defenseSquadController.IsConfigured &&
                !defenseSquadController.IsBusy &&
                !(defenseSquadController.Model?.IsPending ?? false))
                defenseSquadController.Refresh();
            RefreshDefenseLabels();
        }

        private void DefenseFamilyLabel(string family, long ready,
            long roster, long reserved)
        {
            if (!defenseTroopLines.TryGetValue(family, out TextMeshProUGUI label)
                || label == null) return;
            label.text = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                DefenseText("family.counts"), ready, roster, reserved);
        }
        private void RefreshDefenseLabels()
        {
            if (!windowOpen || !defenseMode || suppressed ||
                defenseLevelLabel == null || defenseReadyLabel == null)
                return;

            HiveSquadReservationScreenModel squad = defenseSquadController != null &&
                defenseSquadController.IsConfigured ? defenseSquadController.Model : null;
            if (squad != null &&
                (squad.State == HiveSquadReservationScreenState.Ready ||
                 squad.State == HiveSquadReservationScreenState.OfflineReadOnly ||
                 squad.State == HiveSquadReservationScreenState.Mutating ||
                 squad.State == HiveSquadReservationScreenState.PendingConfirmation))
                lastDefenseSquadModel = squad;

            CombatPatrolScreenModel combat = defensePatrolController != null &&
                defensePatrolController.IsConfigured ? defensePatrolController.Model : null;
            if (combat != null &&
                combat.State != CombatPatrolScreenState.NotConfigured &&
                combat.State != CombatPatrolScreenState.Loading &&
                combat.State != CombatPatrolScreenState.Error)
                lastDefensePatrolModel = combat;

            lastKnownDefenseState = HiveDefenseOverviewPresentation.Project(
                lastDefenseSquadModel, lastDefensePatrolModel);
            HiveDefenseOverviewSnapshot state = lastKnownDefenseState;
            int? level = HiveViewProductUiPresenter.OfficialDefenseBuildingLevelForExternalHost();
            defenseLevelLabel.text = level.HasValue
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    DefenseText("level"), level.Value)
                : DefenseText("level_unknown");
            defenseStateLabel.text = HiveViewProductUiPresenter.DefenseUpgradeInProgressForExternalHost()
                ? DefenseText("upgrade_active")
                : DefenseText("passive_unavailable");

            if (!state.RosterKnown)
            {
                defenseRosterLabel.text = "--";
                defenseReadyLabel.text = "--";
                defenseReservedLabel.text = "--";
                defenseRosterStatusLabel.text = DefenseText("status.awaiting");
                foreach (TextMeshProUGUI label in defenseTroopLines.Values)
                    if (label != null) label.text = "--";
            }
            else
            {
                defenseRosterLabel.text = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    DefenseText("roster.total"), state.RosterTotal);
                defenseReadyLabel.text = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    DefenseText("roster.ready"), state.ReadyTotal);
                defenseReservedLabel.text = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    DefenseText("roster.reserved"), state.ReservedTotal);
                defenseRosterStatusLabel.text = state.OfflineRoster
                    ? DefenseText("status.offline")
                    : DefenseText("status.official");
                DefenseFamilyLabel("guardians", state.GuardiansReady,
                    state.GuardiansRoster, state.GuardiansReserved);
                DefenseFamilyLabel("wingrunners", state.WingrunnersReady,
                    state.WingrunnersRoster, state.WingrunnersReserved);
                DefenseFamilyLabel("darters", state.DartersReady,
                    state.DartersRoster, state.DartersReserved);
            }

            if (defensePatrolLabel != null)
                defensePatrolLabel.text = state.CombatKnown
                    ? string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        DefenseText("patrol.active"), state.ActivePatrols)
                    : DefenseText("status.awaiting");
            if (defenseRecoveryLabel != null)
                defenseRecoveryLabel.text = state.CombatKnown
                    ? string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        DefenseText("patrol.recovering"), state.RecoveringTroops)
                    : DefenseText("status.awaiting");
            if (defenseChampionLabel == null) return;
            if (!state.RosterKnown)
                defenseChampionLabel.text = DefenseText("status.awaiting");
            else if (!state.HasPreparedPatrol)
                defenseChampionLabel.text = DefenseText("patrol.none_prepared");
            else
            {
                string names = string.Join(", ",
                    System.Linq.Enumerable.Select(state.ReservedChampionIds,
                        id => BeeLocalization.Text("champion_bees.name." + id,
                            id ?? string.Empty)));
                defenseChampionLabel.text = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    DefenseText("patrol.prepared"),
                    state.ReservedTotal,
                    string.IsNullOrWhiteSpace(names) ? DefenseText("patrol.no_champion") : names);
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
                BeeLocalization.Text("nursery.status.retry_hint");
            else if (!loaded) nurseryStatusLabel.text = fr
                ? "Chargement des données officielles..." : "Loading server data...";
            else if (busy) nurseryStatusLabel.text = fr
                ? "Synchronisation en cours..." : "Syncing...";
            else if (pending) nurseryStatusLabel.text = fr
                ? "Commande en attente de confirmation. Utilise Vérifier la commande."
                : "Command awaiting confirmation. Use Verify command.";
            else if (readOnly) nurseryStatusLabel.text = fr
                ? "Hors ligne : consultation seulement." : "Offline: read-only.";

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
                    ? (fr ? "Données non confirmées" : "Data not confirmed")
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
                UpdateCareButton(nurseryFeedButton, mutationAvailable &&
                    model.CanStart(HiveBroodVitalityClient.FeedingType));
            if (nurseryStabilizeButton != null)
                UpdateCareButton(nurseryStabilizeButton, mutationAvailable &&
                    model.CanStart(HiveBroodVitalityClient.StabilizationType));
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
                || BeeLocalization.CurrentLocale.StartsWith("fr", StringComparison.OrdinalIgnoreCase);
            bool configured = infirmaryController != null && infirmaryController.IsConfigured;
            CombatPatrolScreenModel model = configured ? infirmaryController.Model : null;
            bool busy = configured && infirmaryController.IsBusy;
            bool failed = model != null && model.State == CombatPatrolScreenState.Error;
            bool loading = model == null || model.State == CombatPatrolScreenState.NotConfigured
                || model.State == CombatPatrolScreenState.Loading;
            if (configured && !failed && !loading && model.Recovering != null)
                infirmaryHasLoadedRecovery = true;
            // A failed first read is not a ward with zero wounded. A refresh may retain
            // an already received snapshot, but no timer credits troops on the device.
            bool readable = configured && model != null && !failed && infirmaryHasLoadedRecovery;
            var rows = InfirmaryRecoveryOverview.Aggregate(model != null ? model.Recovering : null);
            long total = InfirmaryRecoveryOverview.Total(rows);
            infirmaryTotalLabel.text = !configured
                ? BeeLocalization.Text("infirmary.recovery.connection_required")
                : !readable ? BeeLocalization.Text(failed ? "infirmary.recovery.unavailable" : "infirmary.recovery.loading")
                : total.ToString(System.Globalization.CultureInfo.CurrentCulture)
                    + (french ? " blessées en soins" : " recovering troops");
            if (!configured)
                infirmaryStatusLabel.text = french
                    ? "Connecte-toi pour consulter les blessées enregistrées par le serveur."
                    : "Sign in to view server-recorded wounded troops.";
            else if (failed)
                infirmaryStatusLabel.text = french
                    ? "Lecture indisponible. Nouvelle tentative automatique en cours."
                    : "Data unavailable. Automatic retry is scheduled.";
            else if (busy || loading)
                infirmaryStatusLabel.text = french ? "Synchronisation de la convalescence..." : "Syncing recovery...";
            else
                infirmaryStatusLabel.text = total > 0
                    ? (french ? "Retour automatique dans la Caserne à la fin des soins."
                              : "Troops return to Barracks automatically once recovered.")
                    : (french ? "Aucune blessée actuellement en convalescence."
                              : "No troops are currently recovering.");
            foreach (var row in rows)
            {
                if (infirmaryCountLabels.TryGetValue(row.Family, out TextMeshProUGUI count) && count != null)
                    count.text = readable ? row.Wounded.ToString(System.Globalization.CultureInfo.CurrentCulture) : "--";
                if (infirmaryTimerLabels.TryGetValue(row.Family, out TextMeshProUGUI timer) && timer != null)
                    timer.text = readable
                        ? InfirmaryRecoveryOverview.Countdown(row.NextReadyAtUtc, DateTimeOffset.UtcNow, french)
                        : (french ? "--" : "--");
            }
        }

        public static string BannerResourcePathForProof(string legacyKey)
        {
            if (!BuildingMappingTable.TryGetByLegacyKey(legacyKey, out _))
                return string.Empty;
            return "PremiumBeeReference/BuildingBanners/" + legacyKey;
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
            float retainedDefenseScroll = (defenseMode || geneticsMode || academyMode || productionInfoMode) && scroll != null
                ? scroll.verticalNormalizedPosition : 1f;
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
            // Every building has a dedicated HD banner. Never show Alliance
            // imagery on Infirmary, Nursery, Defense or a future building.
            Texture2D banner = Resources.Load<Texture2D>(
                BannerResourcePathForProof(selected.LegacyKey));
            if (banner != null)
            {
                // The old 2:1 source was stretched into a nearly 7:1 strip.
                // Show the real building-specific HD art with native aspect,
                // at readable contrast and without covering the Upgrade action.
                float artHeight = headerRect.height - 8f;
                float artWidth = Mathf.Min(width * .34f, artHeight * 2.4f);
                float artX = Mathf.Max(width * .54f,
                    width - (upgradeAction != null ? 230f : 28f) - artWidth);
                HeaderArtwork(header, new Rect(artX, 4f, artWidth, artHeight),
                    banner, new Color(1f, 1f, 1f, .66f));
            }
            string name = fr ? titleFr : titleEn;
            string role = productionInfoMode ? ProductionText("subtitle", "Production et reserves de la colonie")
                : academyMode ? AcademyText("subtitle")
                : geneticsMode ? BeeLocalization.Text("genetics.premium.subtitle")
                : selected.Role ?? string.Empty;
            if (!fr && !productionInfoMode && !defenseMode && !geneticsMode && !academyMode)
                role = "Building overview";
            PremiumBackButton(header, closeAction);
            bool landscape = width > height && width >= 600f;
            TextMeshProUGUI title = Label(header, landscape
                    ? new Rect(120f, 12f, Mathf.Max(100f, width - 340f), 39f)
                    : new Rect(18f, headerRect.height - 65f, width - 36f, 37f),
                name.ToUpperInvariant(), landscape ? 26f : 23f, Gold, true, true);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            if (upgradeAction != null)
                HeaderUpgradeButton(header, width, fr);
            Label(header, landscape
                    ? new Rect(122f, 53f, Mathf.Max(100f, width - 340f), 23f)
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
            if (productionInfoMode)
            {
                RenderProductionInfoCards(content, cardWidth, gap, out cursor, viewportRect.height);
            }
            else if (infirmaryMode)
            {
                RenderInfirmaryCards(content, cardWidth, gap, out cursor,
                    fr, viewportRect.height);
            }
            else if (nurseryMode)
            {
                RenderNurseryCards(content, cardWidth, gap, out cursor,
                    fr, viewportRect.height);
            }
            else if (defenseMode)
            {
                RenderDefenseCards(content, cardWidth, gap, out cursor, viewportRect.height);
            }
            else if (geneticsMode)
            {
                RenderGeneticsCards(content, cardWidth, gap, out cursor, viewportRect.height);
            }
            else if (academyMode)
            {
                RenderAcademyCards(content, cardWidth, gap, out cursor, viewportRect.height);
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
            scroll.verticalNormalizedPosition = (defenseMode || geneticsMode || academyMode || productionInfoMode)
                ? retainedDefenseScroll : 1f;
        }

        private static string DefenseText(string key) =>
            BeeLocalization.Text("defense.premium." + key);

        private static string GeneticsText(string key) =>
            BeeLocalization.Text("genetics.premium." + key);

        // Avoid TMP mesh churn and flashing while the model is being polled.
        private static void SetGeneticsLabel(TextMeshProUGUI label, string value)
        {
            if (label != null &&
                !string.Equals(label.text, value, StringComparison.Ordinal))
                label.text = value;
        }

        private static string GeneticsDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;
            if (duration.TotalHours >= 1d)
                return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0}:{1:00}:{2:00}", (long)duration.TotalHours,
                    duration.Minutes, duration.Seconds);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:00}:{1:00}", (int)duration.TotalMinutes, duration.Seconds);
        }

        private void RefreshGeneticsLabels()
        {
            if (!windowOpen || !geneticsMode || suppressed ||
                geneticsLevelLabel == null || geneticsStatusLabel == null) return;

            HiveBuildingUpgradeScreenModel model =
                HiveViewProductUiPresenter.OfficialGeneticsUpgradeModelForExternalHost();
            int? level = HiveViewProductUiPresenter.OfficialGeneticsBuildingLevelForExternalHost();
            string levelText = level.HasValue
                ? string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    GeneticsText("level"), level.Value)
                : GeneticsText("awaiting_level");
            SetGeneticsLabel(geneticsLevelLabel, levelText);

            string syncKey = model == null ||
                model.State == HiveBuildingUpgradeScreenState.NotConfigured
                    ? "sync.disconnected"
                    : model.State == HiveBuildingUpgradeScreenState.Error
                        ? "sync.error"
                        : model.State == HiveBuildingUpgradeScreenState.Loading ||
                          model.State == HiveBuildingUpgradeScreenState.Starting ||
                          model.State == HiveBuildingUpgradeScreenState.Completing
                            ? "sync.loading"
                            : model.State == HiveBuildingUpgradeScreenState.OfflineReadOnly
                                ? "sync.offline" : "sync.ready";
            // A routine refresh begins in Loading with a retained server
            // snapshot. Keep the prior verified/offline/error status during
            // that short request to prevent a misleading status flash.
            bool transientRefresh = model != null && level.HasValue &&
                model.State == HiveBuildingUpgradeScreenState.Loading &&
                (geneticsStatusLabel.text == GeneticsText("sync.ready") ||
                 geneticsStatusLabel.text == GeneticsText("sync.offline") ||
                 geneticsStatusLabel.text == GeneticsText("sync.error"));
            if (!transientRefresh)
                SetGeneticsLabel(geneticsStatusLabel, GeneticsText(syncKey));

            if (geneticsProgressLabel == null || geneticsOfferLabel == null) return;
            HiveBuildingUpgradeOperationModel operation =
                HiveViewProductUiPresenter.OfficialGeneticsUpgradeOperationForExternalHost();
            if (operation != null)
            {
                string text = operation.IsAwaitingCompletion
                    ? string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        GeneticsText("upgrade.complete"), operation.ToLevel)
                    : string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        GeneticsText("upgrade.running"), operation.ToLevel,
                        GeneticsDuration(
                            HiveViewProductUiPresenter.OfficialGeneticsUpgradeRemainingForExternalHost()
                                ?? TimeSpan.Zero));
                SetGeneticsLabel(geneticsProgressLabel, text);
                SetGeneticsLabel(geneticsOfferLabel,
                    GeneticsText("upgrade.construction_link"));
                return;
            }

            HiveBuildingUpgradeOfferModel offer =
                HiveViewProductUiPresenter.OfficialGeneticsUpgradeOfferForExternalHost();
            if (offer == null)
            {
                SetGeneticsLabel(geneticsProgressLabel,
                    GeneticsText(model == null ||
                        model.State == HiveBuildingUpgradeScreenState.Loading ||
                        model.State == HiveBuildingUpgradeScreenState.Error
                        ? "upgrade.awaiting" : "upgrade.max"));
                SetGeneticsLabel(geneticsOfferLabel,
                    GeneticsText("upgrade.construction_link"));
                return;
            }
            SetGeneticsLabel(geneticsProgressLabel,
                string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    GeneticsText("upgrade.next"), offer.FromLevel, offer.ToLevel));
            List<string> costs = new List<string>();
            foreach (KeyValuePair<string, long> cost in offer.Costs)
                costs.Add(BeeLocalization.Text("resource." + cost.Key, cost.Key) +
                    " " + cost.Value.ToString("N0",
                        System.Globalization.CultureInfo.CurrentCulture));
            string costSummary = costs.Count == 0
                ? GeneticsText("upgrade.no_cost") : string.Join("  |  ", costs);
            SetGeneticsLabel(geneticsOfferLabel,
                string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    GeneticsText("upgrade.cost_duration"),
                    costSummary, GeneticsDuration(offer.Duration)));
        }

        // Intentional read-only genetics presentation. Breeding, inheritance
        // and mutations do not yet have authenticated mutation contracts.
        private void RenderGeneticsCards(Transform parent, float width, float left,
            out float cursor, float viewportHeight)
        {
            cursor = 14f;
            bool compact = width < 540f;
            float statusHeight = compact ? 242f : 184f;

            RectTransform status = CardAt(parent,
                new Rect(left, cursor, width, statusHeight),
                GeneticsText("card.status"), "flower");
            geneticsLevelLabel = Label(status,
                new Rect(18f, 74f, width - 36f, 34f),
                GeneticsText("awaiting_level"), width < 450f ? 19f : 23f, Gold, true);
            geneticsLevelLabel.overflowMode = TextOverflowModes.Ellipsis;
            geneticsStatusLabel = Label(status,
                new Rect(18f, 116f, width - 36f, compact ? 54f : 24f),
                GeneticsText("sync.loading"), 15f, Paper);
            Label(status,
                new Rect(18f, compact ? 177f : 148f, width - 36f,
                    compact ? 55f : 27f),
                GeneticsText("level_note"), 13f, Muted);
            cursor += statusHeight + 20f;

            // Construction costs, durations and active countdown come from
            // the official server model; no genetics-specific currency exists.
            float progressionHeight = compact ? 216f : 191f;
            RectTransform progression = CardAt(parent,
                new Rect(left, cursor, width, progressionHeight),
                GeneticsText("card.progression"), "upgrade");
            geneticsProgressLabel = Label(progression,
                new Rect(18f, 74f, width - 36f, compact ? 65f : 49f),
                GeneticsText("upgrade.awaiting"), 17f, Gold, true);
            geneticsOfferLabel = Label(progression,
                new Rect(18f, compact ? 149f : 132f, width - 36f,
                    compact ? 57f : 50f),
                GeneticsText("upgrade.construction_link"), 14f, Paper);
            cursor += progressionHeight + 19f;

            float visionHeight = compact ? 244f : 162f;
            RectTransform introduction = CardAt(parent,
                new Rect(left, cursor, width, visionHeight),
                GeneticsText("card.vision"), "bee");
            Label(introduction,
                new Rect(18f, 76f, width - 36f, compact ? 155f : 80f),
                GeneticsText("vision"), 15f, Paper);
            cursor += visionHeight + 19f;

            // Canonical specialization concepts, never represented as actual
            // unlocked genes, chance percentages or researched bonuses.
            string[] branches = { "foraging", "resilience", "combat" };
            string[] icons = { "flower", "shield", "guard" };
            bool twoColumns = width >= 920f;
            float gap = 16f;
            float branchWidth = twoColumns ? (width - gap) * .5f : width;
            float branchHeight = compact ? 220f : 180f;
            for (int i = 0; i < branches.Length; i++)
            {
                int row = twoColumns ? i / 2 : i;
                int column = twoColumns ? i % 2 : 0;
                float x = left + column * (branchWidth + gap);
                float y = cursor + row * (branchHeight + gap);
                string kind = branches[i];
                RectTransform card = CardAt(parent,
                    new Rect(x, y, branchWidth, branchHeight),
                    GeneticsText("branch." + kind + ".title"),
                    icons[i]);
                Label(card, new Rect(18f, 73f, branchWidth - 36f,
                        compact ? 100f : 62f),
                    GeneticsText("branch." + kind + ".description"), 14f, Paper);
                Label(card, new Rect(18f, compact ? 184f : 137f,
                        branchWidth - 36f, 29f),
                    GeneticsText("preview_only"), 12f, Gold, true);
            }
            int branchRows = twoColumns ? 2 : branches.Length;
            cursor += branchRows * (branchHeight + gap) + 1f;

            float crossingHeight = compact ? 244f : 169f;
            RectTransform crossing = CardAt(parent,
                new Rect(left, cursor, width, crossingHeight),
                GeneticsText("card.crossing"), "nursery");
            Label(crossing, new Rect(18f, 73f, width - 36f,
                    compact ? 127f : 61f),
                GeneticsText("crossing_description"), 14f, Paper);
            Label(crossing, new Rect(18f, compact ? 208f : 140f,
                    width - 36f, 25f),
                GeneticsText("unavailable"), 12f, Gold, true);
            cursor += crossingHeight + 20f;

            float recordHeight = compact ? 199f : 159f;
            RectTransform records = CardAt(parent,
                new Rect(left, cursor, width, recordHeight),
                GeneticsText("card.history"), "research");
            Label(records, new Rect(18f, 72f, width - 36f,
                    compact ? 114f : 78f),
                GeneticsText("no_experiments"), 14f, Muted);
            cursor += recordHeight + 19f;

            float managementHeight = compact ? 254f : 185f;
            RectTransform management = CardAt(parent,
                new Rect(left, cursor, width, managementHeight),
                GeneticsText("card.management"), "research");
            Label(management, new Rect(18f, 75f, width - 36f,
                    compact ? 101f : 42f),
                GeneticsText("research_note"), 14f, Paper);
            float buttonWidth = Mathf.Min(222f, width - 36f);
            ButtonAt(management,
                new Rect(Mathf.Max(18f, width - buttonWidth - 18f),
                    compact ? 198f : 129f, buttonWidth, 44f),
                GeneticsText("open_research"), true, () =>
                {
                    // Navigation to real, official Research; this does not
                    // enable genetic mutation or spend any resource.
                    closeAction?.Invoke();
                    HiveViewProductUiPresenter.OpenResearchOverlayForExternalHost();
                });
            cursor += managementHeight + 19f;

            RectTransform content = parent as RectTransform;
            if (content != null)
                content.sizeDelta = new Vector2(0f,
                    Mathf.Max(cursor + 24f, viewportHeight + 1f));
            RefreshGeneticsLabels();
        }
        private void RenderDefenseCards(Transform parent, float width, float left,
            out float cursor, float viewportHeight)
        {
            defenseTroopLines.Clear();
            cursor = 14f;
            RectTransform status = CardAt(parent,
                new Rect(left, cursor, width, 184f),
                DefenseText("card.status"), "defense");
            defenseLevelLabel = Label(status, new Rect(18f, 74f, width - 36f, 35f),
                "--", 22f, Gold, true);
            defenseStateLabel = Label(status, new Rect(18f, 112f, width - 36f, 30f),
                string.Empty, 15f, Paper);
            Label(status, new Rect(18f, 149f, width - 36f, 28f),
                DefenseText("level_description"), 13f, Muted);
            cursor += 204f;

            RectTransform overview = CardAt(parent,
                new Rect(left, cursor, width, 209f),
                DefenseText("card.roster"), "guard");
            defenseRosterLabel = Label(overview,
                new Rect(18f, 75f, width - 36f, 28f), "--", 19f, Paper, true);
            defenseReadyLabel = Label(overview,
                new Rect(18f, 111f, width - 36f, 28f), "--", 19f, Gold, true);
            defenseReservedLabel = Label(overview,
                new Rect(18f, 145f, width - 36f, 28f), "--", 17f, Paper);
            defenseRosterStatusLabel = Label(overview,
                new Rect(18f, 177f, width - 36f, 27f), string.Empty, 12f, Muted);
            cursor += 228f;

            string[] families = { "guardians", "wingrunners", "darters" };
            for (int i = 0; i < families.Length; i++)
            {
                string family = families[i];
                string name = DefenseText("family." + family);
                RectTransform line = CardAt(parent,
                    new Rect(left, cursor, width, 121f),
                    name.ToUpperInvariant(), family == "guardians" ? "shield" : "guard");
                defenseTroopLines[family] = Label(line,
                    new Rect(18f, 74f, width - 36f, 35f),
                    "--", 20f, Paper, true);
                cursor += 140f;
            }

            RectTransform patrol = CardAt(parent,
                new Rect(left, cursor, width, 219f),
                DefenseText("card.patrol"), "defense");
            defensePatrolLabel = Label(patrol,
                new Rect(18f, 74f, width - 36f, 32f), "--", 18f, Gold, true);
            defenseRecoveryLabel = Label(patrol,
                new Rect(18f, 113f, width - 36f, 30f), "--", 17f, Paper);
            defenseChampionLabel = Label(patrol,
                new Rect(18f, 149f, width - 36f, 28f), "--", 14f, Muted);
            Label(patrol, new Rect(18f, 182f, width - 36f, 31f),
                DefenseText("patrol_note"), 13f, Muted);
            cursor += 238f;

            RectTransform future = CardAt(parent,
                new Rect(left, cursor, width, 208f),
                DefenseText("card.future"), "shield");
            Label(future, new Rect(18f, 75f, width - 36f, 33f),
                DefenseText("future.siege"), 16f, Paper, true);
            Label(future, new Rect(18f, 115f, width - 36f, 33f),
                DefenseText("future.shields"), 16f, Paper, true);
            Label(future, new Rect(18f, 155f, width - 36f, 42f),
                DefenseText("future.warning"), 13f, Muted);
            cursor += 227f;

            RectTransform manage = CardAt(parent,
                new Rect(left, cursor, width, 170f),
                DefenseText("card.management"), "guard");
            Label(manage, new Rect(18f, 75f, width - 36f, 37f),
                DefenseText("management.note"), 14f, Muted);
            ButtonAt(manage,
                new Rect(Mathf.Max(18f, width - 245f), 119f,
                    Mathf.Min(222f, width - 36f), 44f),
                DefenseText("open_barracks"), true, () =>
                {
                    // Navigation only. Do not mutate roster or reservation.
                    closeAction?.Invoke();
                    HiveViewProductUiPresenter.OpenBarrackOverlayForExternalHost();
                });
            cursor += 188f;

            RectTransform content = parent as RectTransform;
            if (content != null)
                content.sizeDelta = new Vector2(0f,
                    Mathf.Max(cursor + 22f, viewportHeight + 1f));
            RefreshDefenseLabels();
        }
        private void RenderInfirmaryCards(Transform parent, float cardWidth,
            float left, out float cursor, bool french, float viewportHeight)
        {
            infirmaryCountLabels.Clear();
            infirmaryTimerLabels.Clear();
            cursor = 14f;
            const float headerGrowth = ProductionPremiumHeaderHeight - 62f;
            RectTransform summary = CardAt(parent, new Rect(left, cursor, cardWidth, 158f + headerGrowth),
                french ? "CENTRE DE CONVALESCENCE" : "RECOVERY WARD",
                "infirmary-card-recovery", true);
            infirmaryTotalLabel = Label(summary, new Rect(18f, 76f + headerGrowth, cardWidth - 36f, 33f),
                "--", 24f, Gold, true);
            infirmaryStatusLabel = Label(summary, new Rect(18f, 113f + headerGrowth, cardWidth - 36f, 40f),
                string.Empty, 13f, Muted);
            cursor += 178f + headerGrowth;

            string[] families = {
                InfirmaryRecoveryOverview.Guardians,
                InfirmaryRecoveryOverview.Wingrunners,
                InfirmaryRecoveryOverview.Darters
            };
            string[] familyIcons = {
                "infirmary-card-guardians",
                "infirmary-card-wingrunners",
                "infirmary-card-darters"
            };
            for (int i = 0; i < families.Length; i++)
            {
                string family = families[i];
                RectTransform ward = CardAt(parent,
                    new Rect(left, cursor, cardWidth, 124f + headerGrowth),
                    InfirmaryRecoveryOverview.FamilyName(family, french).ToUpperInvariant(),
                    familyIcons[i], true);
                infirmaryCountLabels[family] = Label(ward,
                    new Rect(18f, 73f + headerGrowth, 110f, 37f), "--", 28f, Paper, true);
                infirmaryTimerLabels[family] = Label(ward,
                    new Rect(130f, 77f + headerGrowth, Mathf.Max(90f, cardWidth - 148f), 38f),
                    string.Empty, 14f, Muted);
                cursor += 142f + headerGrowth;
            }

            RectTransform actions = CardAt(parent,
                new Rect(left, cursor, cardWidth, 156f + headerGrowth),
                french ? "SUIVI DES SOINS" : "RECOVERY MANAGEMENT",
                "infirmary-card-management", true);
            Label(actions, new Rect(18f, 77f + headerGrowth, cardWidth - 36f, 37f),
                french ? "Les blessées reviennent automatiquement après leur convalescence."
                    : "Wounded bees return automatically once their recovery finishes.",
                14f, Muted);
            Label(actions, new Rect(18f, 122f + headerGrowth, cardWidth - 36f, 26f),
                french ? "●  Mise à jour automatique des soins"
                    : "●  Recovery updates automatically",
                14f, Gold, true);
            cursor += 175f + headerGrowth;
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
                new Rect(left, cursor, width, 156f),
                fr ? "GESTION DE LA NURSERIE" : "NURSERY MANAGEMENT", "build");
            Label(management, new Rect(18f, 76f, width - 36f, 37f),
                fr ? "Les soins sont validés et enregistrés par le serveur."
                    : "Care is authorized and saved by the server.", 14f, Muted);
            Label(management, new Rect(18f, 121f, width - 36f, 28f),
                fr ? "●  Synchronisation automatique des soins"
                    : "●  Care synchronizes automatically",
                14f, Gold, true);
            cursor += 175f;

            RectTransform content = parent as RectTransform;
            if (content != null)
                content.sizeDelta = new Vector2(0f,
                    Mathf.Max(cursor + 24f, viewportHeight + 1f));
            RefreshNurseryLabels();
        }

        // Production cards grow together: artwork and all fields below the header.
        private const float ProductionPremiumHeaderHeight = 126f;
        private const float ProductionPremiumHeaderGrowth = ProductionPremiumHeaderHeight - 84f;

        // Shared premium card header geometry. A contained image never crops
        // the gilded frame of a real resource icon, even for non-square art.
        public static Rect PremiumCardIconContainRectForProof(
            Rect frame, float padding, float sourceWidth, float sourceHeight)
        {
            float inset = Mathf.Clamp(padding, 0f,
                Mathf.Min(frame.width, frame.height) * .45f);
            float innerWidth = Mathf.Max(1f, frame.width - inset * 2f);
            float innerHeight = Mathf.Max(1f, frame.height - inset * 2f);
            float width = sourceWidth > 0f ? sourceWidth : innerWidth;
            float height = sourceHeight > 0f ? sourceHeight : innerHeight;
            float scale = Mathf.Min(innerWidth / width, innerHeight / height);
            float renderWidth = width * scale;
            float renderHeight = height * scale;
            return new Rect(
                frame.x + inset + (innerWidth - renderWidth) * .5f,
                frame.y + inset + (innerHeight - renderHeight) * .5f,
                renderWidth, renderHeight);
        }

        // Production uses the same Plate, ink, type and separator primitives
        // as all other premium cards, but needs a taller, padded header for
        // fully visible premium resource icons.
        private RectTransform CardAt(Transform parent, Rect rect,
            string title, string icon, bool spaciousPremiumHeader = false)
        {
            float headerHeight = spaciousPremiumHeader ? ProductionPremiumHeaderHeight : 62f;
            RectTransform card = Plate(parent, "Card", rect, Card, "panel-fill");
            Plate(card, "HeaderSheen",
                new Rect(1f, 1f, rect.width - 2f, headerHeight),
                new Color(.205f, .135f, .071f, .76f), "panel-fill");
            Plate(card, "GoldAccent", new Rect(1f, 1f, 3f, rect.height - 2f),
                new Color(.72f, .47f, .17f, .86f), "etched-line");
            if (spaciousPremiumHeader)
            {
                Texture artwork = PremiumIcon(icon);
                if (artwork != null)
                {
                    Rect fitted = PremiumCardIconContainRectForProof(
                        new Rect(10f, 9f, 108f, 108f), 4f,
                        artwork.width, artwork.height);
                    RawImage image = Node(card, "PremiumCardIcon", fitted)
                        .gameObject.AddComponent<RawImage>();
                    image.texture = artwork;
                    image.uvRect = new Rect(0f, 0f, 1f, 1f);
                    image.color = Color.white;
                    image.raycastTarget = false;
                }
                TextMeshProUGUI heading = Label(card,
                    new Rect(140f, 13f, Mathf.Max(90f, rect.width - 158f), 102f),
                    title, rect.width < 480f ? 17f : 19f, Gold, true);
                heading.enableAutoSizing = true;
                heading.fontSizeMin = 12f;
                heading.fontSizeMax = rect.width < 480f ? 17f : 19f;
                heading.alignment = TextAlignmentOptions.MidlineLeft;
            }
            else
            {
                Icon(card, new Rect(18f, 12f, 48f, 48f), icon);
                Label(card, new Rect(78f, 17f, rect.width - 96f, 42f),
                    title, (geneticsMode || academyMode) && rect.width < 540f
                        ? 16f : 19f, Gold, true);
            }
            Plate(card, "Separator",
                new Rect(18f, headerHeight + 2f, rect.width - 36f, 1f),
                new Color(.43f, .29f, .12f), "etched-line");
            return card;
        }
        private void PremiumBackButton(Transform header, Action callback)
        {
            // ButtonAt() uses RawImage, whereas the approved Barracks/Chat
            // return design requires UI.Image for sprite-swap and the gold
            // arrow glyph. Build the correct target graphic directly, with
            // NO "Retour" text to wrap into two lines.
            RectTransform node = Node(header, "PremiumBack",
                new Rect(16f, 12f, 52f, 52f));
            Image background = node.gameObject.AddComponent<Image>();
            background.raycastTarget = true;
            Button button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayUIClick();
                callback?.Invoke();
            });
            WorldMapHudLayout.StyleReturnButton(button);
        }
        private Button ButtonAt(Transform parent, Rect rect, string text,
            bool primary, Action callback)
        {
            RectTransform node = Plate(parent, "Button", rect,
                primary ? new Color(.91f, .62f, .23f)
                    : new Color(.22f, .15f, .07f), "panel-fill");
            RawImage graphic = node.GetComponent<RawImage>();
            graphic.raycastTarget = true;
            Button button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = graphic;
            TextMeshProUGUI caption = Label(node,
                new Rect(5f, 0f, rect.width - 10f, rect.height), text,
                16f, primary ? Ink : Paper, true);
            caption.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayUIClick();
                callback?.Invoke();
            });
            return button;
        }

        private void HeaderUpgradeButton(Transform header, float width, bool french)
        {
            float buttonWidth = width >= 560f ? 180f : 146f;
            Button button = ButtonAt(header,
                new Rect(width - buttonWidth - 16f, 11f, buttonWidth, 48f),
                french ? "Améliorer" : "Upgrade", true, upgradeAction);
            // Same original artwork, native gold/black colours and socket as
            // Alliance, Caserne and Banque. Child images do not steal button clicks.
            Texture2D icon = PremiumBuildingUpgradeArtwork.Texture;
            RectTransform socket = Node(button.transform, "UpgradeIconSocket",
                new Rect(6f, 5f, 40f, 38f));
            RawImage darkInset = socket.gameObject.AddComponent<RawImage>();
            darkInset.texture = Texture2D.whiteTexture;
            darkInset.color = PremiumBuildingUpgradeArtwork.SocketColor;
            darkInset.raycastTarget = false;
            if (icon != null)
            {
                RectTransform imageNode = Node(socket, "UpgradeIcon",
                    new Rect(2f, 1f, 36f, 36f));
                RawImage image = imageNode.gameObject.AddComponent<RawImage>();
                image.texture = icon;
                image.color = Color.white;
                image.raycastTarget = false;
            }
            TextMeshProUGUI caption = button.GetComponentInChildren<TextMeshProUGUI>();
            if (caption != null)
            {
                RectTransform labelRect = caption.rectTransform;
                labelRect.anchoredPosition = new Vector2(48f, 0f);
                labelRect.sizeDelta = new Vector2(buttonWidth - 54f, 48f);
                caption.fontSize = width < 560f ? 15f : 17f;
                caption.color = Ink;
            }
        }

        private static void UpdateCareButton(Button button, bool enabled)
        {
            if (button == null) return;
            button.interactable = enabled;
            if (button.targetGraphic is RawImage graphic)
                graphic.color = enabled
                    ? new Color(.91f, .62f, .23f)
                    : new Color(.19f, .14f, .085f);
            TextMeshProUGUI caption = button.GetComponentInChildren<TextMeshProUGUI>();
            if (caption != null)
                caption.color = enabled ? Ink : Muted;
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

        public static Rect HeaderArtworkCropForProof(
            float imageWidth, float imageHeight, float rectWidth, float rectHeight)
        {
            if (imageWidth <= 0f || imageHeight <= 0f ||
                rectWidth <= 0f || rectHeight <= 0f)
                return new Rect(0f,0f,1f,1f);
            float imageAspect = imageWidth / imageHeight;
            float displayAspect = rectWidth / rectHeight;
            if (displayAspect > imageAspect)
            {
                float cropHeight = imageAspect / displayAspect;
                return new Rect(0f,(1f-cropHeight)*.5f,1f,cropHeight);
            }
            float cropWidth = displayAspect / imageAspect;
            return new Rect((1f-cropWidth)*.5f,0f,cropWidth,1f);
        }

        private static void HeaderArtwork(Transform parent, Rect rect,
            Texture2D source, Color tint)
        {
            RawImage image = Node(parent, "BuildingHeaderArtwork", rect)
                .gameObject.AddComponent<RawImage>();
            image.texture=source;
            image.color=tint;
            image.raycastTarget=false;
            image.uvRect=HeaderArtworkCropForProof(
                source.width,source.height,rect.width,rect.height);
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
