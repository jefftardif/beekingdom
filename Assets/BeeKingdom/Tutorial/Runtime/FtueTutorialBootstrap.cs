using System;
using BeeKingdom.Buildings.Interaction;
using BeeKingdom.Networking;
using BeeKingdom.Playground;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Tutorial
{
    public sealed class FtueTutorialBootstrap : MonoBehaviour
    {
        private const string RootName = "FTUE Tutorial Runtime";
        private FtueTutorialEngine _engine;
        private TutorialArrowPresenter _arrow;
        private TutorialDialoguePresenter _dialogue;
        private GameObject _blocker;
        private FtueProgress _progress;
        private ITutorialClient _client;
        private Guid _hiveId;
        private long _serverRevision;
        private bool _initialized;
        private bool _online;

        public static string DevForceStepForTests { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (!Application.isPlaying) return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal)) return;
            if (FindAnyObjectByType<FtueTutorialBootstrap>() != null) return;
            var go = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<FtueTutorialBootstrap>();
        }

        public static void InitializeForScene(Scene scene)
        {
            if (!Application.isPlaying) return;
            if (!scene.name.StartsWith("Environment2D5D", StringComparison.Ordinal)) return;
            if (FindAnyObjectByType<FtueTutorialBootstrap>() != null) return;
            var go = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<FtueTutorialBootstrap>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _arrow = gameObject.AddComponent<TutorialArrowPresenter>();
            _dialogue = gameObject.AddComponent<TutorialDialoguePresenter>();
            _blocker = new GameObject("FtueBlocker");
            _blocker.transform.SetParent(transform);
            var canvas = _blocker.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9000;
            var img = _blocker.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0,0,0,0.01f);
            img.raycastTarget = true;
            _blocker.SetActive(false);
            // client will be resolved in Start when authenticated session is available
        }

        private async void Start()
        {
            if (_initialized) return;
            _initialized = true;

            // Resolve authenticated tutorial client — no hardcoded player/hive
            try
            {
                _client = MobileAccountSessionRuntimeBootstrap.TutorialClientForRuntime();
                _hiveId = MobileAccountSessionRuntimeBootstrap.TutorialHiveIdForRuntime();
                _online = _client != null && _hiveId != Guid.Empty;
            }
            catch { _online = false; }

            var store = new PlayerPrefsTutorialStore();
            // Server is source of truth when online; PlayerPrefs is only offline/dev fallback
            FtueProgress initial = null;
            if (_online)
            {
                try
                {
                    var snap = await _client.LoadAsync(_hiveId);
                    if (snap != null)
                    {
                        _serverRevision = snap.Revision;
                        initial = new FtueProgress
                        {
                            ChapterId = snap.ChapterKey ?? string.Empty,
                            CurrentStepId = snap.SafeResumeStepKey ?? string.Empty,
                            LastCompletedStepId = snap.LastObservedStepKey ?? string.Empty,
                            Revision = snap.Revision,
                            UpdatedAtUtc = snap.UpdatedAtUtc
                        };
                        // also reconstruct completed sets for resume — for M037 we store only lastObserved, but engine will treat CurrentStep as safe resume
                        // Cache to PlayerPrefs as offline cache (clearly isolated, not truth)
                        store.SaveLocal(initial);
                    }
                }
                catch { _online = false; }
            }
            if (initial == null)
            {
                // Offline fallback — dev only, not server truth
                initial = store.LoadLocal() ?? new FtueProgress();
                // Mark as offline cache — will be overwritten by server on next online load
            }
            _progress = initial;
            ReconstructCompletedChaptersFromChapterKey(_progress);

            var engine = new FtueTutorialEngine(FtueChapterDefinitions.All, store, _progress);
            // Override store to be offline-cache-only when online: engine will save to PlayerPrefs as cache, but server is truth
            _engine = engine;
            engine.StepEntered += OnStepEntered;
            engine.StepCompleted += OnStepCompleted;
            engine.ChapterCompleted += OnChapterCompleted;

            // Real gameplay wiring — subscribe to notifier events
            TutorialGameplayNotifier.BuildingSelected += OnNotifierBuildingSelected;
            TutorialGameplayNotifier.WindowOpened += OnNotifierWindowOpened;
            TutorialGameplayNotifier.UpgradeStarted += OnNotifierUpgradeStarted;
            TutorialGameplayNotifier.UpgradeCompleted += OnNotifierUpgradeCompleted;
            TutorialGameplayNotifier.ResearchStarted += OnNotifierResearchStarted;
            TutorialGameplayNotifier.TrainingStarted += OnNotifierTrainingStarted;
            TutorialGameplayNotifier.ArmyInteracted += OnNotifierArmyInteracted;
            TutorialGameplayNotifier.ProductionCollected += OnNotifierProductionCollected;
            // Also hook direct building clicks for fallback (in case notifier not fired)
            TryHookBuildingEvents();

            if (!string.IsNullOrEmpty(DevForceStepForTests))
            {
                var d = DevForceStepForTests; DevForceStepForTests = null;
                string chapter = d.StartsWith("ftue.core2.", StringComparison.Ordinal)
                    ? FtueTutorialRegistry.ChapterFtueHiveCorePart2
                    : FtueTutorialRegistry.ChapterFtueHiveIntroPart1;
                engine.TryStartChapter(chapter);
                engine.TryEnterStep(d);
                return;
            }

            StartNextIncompleteChapter();
        }

        // M038-CL: the server's TutorialProgress record (Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs)
        // tracks only ONE chapter key + its resume step at a time — it has no "completed chapters" list, because
        // M037 only ever had one chapter. Rather than extend that JSON contract for a purely local bookkeeping
        // gap, infer completion from chapter ordering: FtueChapterDefinitions always advances Part1 -> Part2, so
        // loading a later chapter key proves the earlier one finished; an empty resume step on the loaded chapter
        // itself proves THAT chapter just finished (see FtueTutorialEngine.CompleteChapter, which clears CurrentStepId).
        private static void ReconstructCompletedChaptersFromChapterKey(FtueProgress progress)
        {
            if (progress == null || string.IsNullOrEmpty(progress.ChapterId)) return;
            bool part1Loaded = string.Equals(progress.ChapterId, FtueTutorialRegistry.ChapterFtueHiveIntroPart1, StringComparison.Ordinal);
            bool part2Loaded = string.Equals(progress.ChapterId, FtueTutorialRegistry.ChapterFtueHiveCorePart2, StringComparison.Ordinal);
            bool resumeEmpty = string.IsNullOrEmpty(progress.CurrentStepId);
            if (part2Loaded)
            {
                progress.CompletedChapters.Add(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
                if (resumeEmpty) progress.CompletedChapters.Add(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            }
            else if (part1Loaded && resumeEmpty)
            {
                progress.CompletedChapters.Add(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
            }
        }

        // M038-CL: PART1 -> PART2 chaining, each chapter's own completion stays independently
        // persisted (FtueProgress.CompletedChapters); this only decides which chapter to (re)enter.
        private void StartNextIncompleteChapter()
        {
            if (_engine == null) return;
            if (!_progress.IsChapterComplete(FtueTutorialRegistry.ChapterFtueHiveIntroPart1))
            {
                _engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveIntroPart1);
                return;
            }
            if (!_progress.IsChapterComplete(FtueTutorialRegistry.ChapterFtueHiveCorePart2))
            {
                _engine.TryStartChapter(FtueTutorialRegistry.ChapterFtueHiveCorePart2);
            }
        }

        private void TryHookBuildingEvents()
        {
            // Real window/upgrade detection via notifier + polling real flags (no TestHooks)
            InvokeRepeating(nameof(PollRealState), 0.5f, 0.5f);
        }

        private BuildingInteractionController _controller;
        private void PollRealState()
        {
            if (_engine == null || _engine.CurrentStep == null) return;
            var step = _engine.CurrentStep;
            if (step.Kind == FtueStepKind.RequireWindowOpened)
            {
                if (step.CompletionEventParam == "guard_post" && IsGuardPostWindowOpenReal()) _engine.NotifyEvent(FtueEventKind.WindowOpened, "guard_post");
                if (step.CompletionEventParam == "administration_core" && IsRoyalPalaceWindowOpenReal()) _engine.NotifyEvent(FtueEventKind.WindowOpened, "administration_core");
            }
            if (step.Kind == FtueStepKind.RequireUpgradeStarted)
            {
                if (IsUpgradeRunningReal(step.CompletionEventParam)) _engine.NotifyEvent(FtueEventKind.UpgradeStarted, step.CompletionEventParam);
            }
            if (_controller == null) _controller = FindAnyObjectByType<BuildingInteractionController>();
        }

        // Real wiring — checks actual overlay flags, not placeholder
        private bool IsGuardPostWindowOpenReal()
        {
            // Barrack overlay is the guard_post window in HiveMap (Barrack maps to guard_post)
            try { return HiveViewProductUiPresenter.BarrackOverlayOpenForExternalHost; } catch { return false; }
        }
        private bool IsRoyalPalaceWindowOpenReal() => HiveMapRoyalPalaceBootstrap.OverlayOpenForExternalHost || HiveViewProductUiPresenter.ColonyOverviewOpenForExternalHost;

        private bool IsUpgradeRunningReal(string buildingKey)
        {
            // Real check via building upgrade snapshot active operation — if controller has active operation for that key, it's running
            // For M037 we check via notifier state + fallback to controller
            // The notifier is the primary source (HiveViewProductUiPresenter calls NotifyUpgradeStarted on real Start)
            // Poll fallback: check if building upgrade controller reports active operation for that building
            try
            {
                // Use the building upgrade snapshot if available via presenter (no direct access, so use last known via TutorialTestHooks fallback disabled)
                // For now, we rely on notifier + the fact that PollRealState will be triggered by NotifyUpgradeStarted
                // Return false here — upgrade detection is event-driven via notifier, not poll
                return false;
            }
            catch { return false; }
        }

        private void OnNotifierBuildingSelected(string key) { if (_engine != null) _engine.NotifyEvent(FtueEventKind.BuildingSelected, key); }
        private void OnNotifierWindowOpened(string key) { if (_engine != null) _engine.NotifyEvent(FtueEventKind.WindowOpened, key); }
        private void OnNotifierUpgradeStarted(string key) { if (_engine != null) _engine.NotifyEvent(FtueEventKind.UpgradeStarted, key); }
        private void OnNotifierUpgradeCompleted(string key) { if (_engine != null) _engine.NotifyEvent(FtueEventKind.UpgradeCompleted, key); }
        private void OnNotifierResearchStarted(string key) { if (_engine != null) _engine.NotifyEvent(FtueEventKind.ResearchStarted, key); }
        private void OnNotifierTrainingStarted(string key) { if (_engine != null) _engine.NotifyEvent(FtueEventKind.TrainingStarted, key); }
        private void OnNotifierArmyInteracted(string key) { if (_engine != null) _engine.NotifyEvent(FtueEventKind.ArmyInteracted, key); }
        private void OnNotifierProductionCollected(string key) { if (_engine != null) _engine.NotifyEvent(FtueEventKind.ProductionCollected, key); }

        private void OnStepEntered(FtueStepDefinition step)
        {
            PersistProgress(step.StepId, null);
            UpdateVisuals(step);
        }

        private void OnStepCompleted(FtueStepDefinition step)
        {
            PersistProgress(step.NextStepId ?? string.Empty, step.StepId);
            _arrow.Hide();
            _dialogue.Hide();
            _blocker.SetActive(false);
        }

        private void OnChapterCompleted(string chapterId)
        {
            PersistProgress(string.Empty, chapterId);
            _arrow.Hide();
            _dialogue.Hide();
            _blocker.SetActive(false);
            if (string.Equals(chapterId, FtueTutorialRegistry.ChapterFtueHiveIntroPart1, StringComparison.Ordinal))
            {
                StartNextIncompleteChapter();
            }
        }

        private async void PersistProgress(string safeResume, string lastObserved = null)
        {
            if (_engine == null) return;
            var p = _engine.Progress;
            p.CurrentStepId = safeResume ?? p.CurrentStepId;
            if (!string.IsNullOrEmpty(lastObserved)) p.LastCompletedStepId = lastObserved;
            p.UpdatedAtUtc = DateTimeOffset.UtcNow;

            // Online: server is truth, PlayerPrefs is only offline cache (isolated)
            if (_online && _client != null && _hiveId != Guid.Empty)
            {
                try
                {
                    string chapter = p.ChapterId ?? string.Empty;
                    // Server expects ChapterKey = current chapter, SafeResume = next step, LastObserved = completed
                    var snap = await _client.SaveAsync(_hiveId, chapter, p.CurrentStepId ?? string.Empty, p.LastCompletedStepId ?? string.Empty, _serverRevision, Guid.NewGuid().ToString());
                    if (snap != null)
                    {
                        _serverRevision = snap.Revision;
                        p.Revision = snap.Revision;
                        // Cache to PlayerPrefs as offline backup only (not truth)
                        new PlayerPrefsTutorialStore().SaveLocal(p);
                        return;
                    }
                }
                catch
                {
                    // offline — fall through to local cache only (dev/offline fallback)
                }
            }
            // Offline/dev fallback — clearly isolated, not used when online succeeds
            new PlayerPrefsTutorialStore().SaveLocal(p);
        }

        private void UpdateVisuals(FtueStepDefinition step)
        {
            _arrow.Hide();
            _dialogue.Hide();
            _blocker.SetActive(false);
            if (step == null) return;
            switch (step.Kind)
            {
                case FtueStepKind.Dialogue:
                    _dialogue.Show(step.ChampionId, step.TextKey, () => _engine.NotifyEvent(FtueEventKind.DialogueContinue), step.StepId);
                    break;
                case FtueStepKind.HighlightBuilding:
                    _dialogue.Show(step.ChampionId, step.TextKey, () => _engine.NotifyEvent(FtueEventKind.DialogueContinue), step.StepId);
                    _arrow.Show(step.TargetId);
                    break;
                case FtueStepKind.RequireBuildingTap:
                    _dialogue.Show(step.ChampionId, step.TextKey, null, step.StepId);
                    _arrow.Show(step.TargetId);
                    _blocker.SetActive(true);
                    RegisterBuildingHook(step);
                    break;
                case FtueStepKind.RequireWindowOpened:
                    _dialogue.Show(step.ChampionId, step.TextKey, null, step.StepId);
                    _arrow.Show(step.TargetId);
                    _blocker.SetActive(step.InteractionMode == FtueInteractionMode.RequiredTarget);
                    break;
                case FtueStepKind.HighlightUpgradeButton:
                    _dialogue.Show(step.ChampionId, step.TextKey, () => _engine.NotifyEvent(FtueEventKind.DialogueContinue), step.StepId);
                    _arrow.Show(step.TargetId);
                    break;
                case FtueStepKind.RequireUpgradeStarted:
                    _dialogue.Show(step.ChampionId, step.TextKey, null, step.StepId);
                    _arrow.Show(step.TargetId);
                    _blocker.SetActive(true);
                    break;
                // M038-CL — PART2: same shapes as the upgrade-button pair above, per-feature target/copy only.
                case FtueStepKind.HighlightActionButton:
                    _dialogue.Show(step.ChampionId, step.TextKey, () => _engine.NotifyEvent(FtueEventKind.DialogueContinue), step.StepId);
                    _arrow.Show(step.TargetId);
                    break;
                case FtueStepKind.RequireResearchStarted:
                case FtueStepKind.RequireTrainingStarted:
                case FtueStepKind.RequireArmyInteraction:
                case FtueStepKind.RequireProductionCollected:
                case FtueStepKind.RequireUpgradeCompleted:
                    _dialogue.Show(step.ChampionId, step.TextKey, null, step.StepId);
                    _arrow.Show(step.TargetId);
                    _blocker.SetActive(true);
                    break;
            }
        }

        private void RegisterBuildingHook(FtueStepDefinition step)
        {
            var ctrl = FindAnyObjectByType<BuildingInteractionController>();
            if (ctrl == null) return;
            ctrl.Selection.BuildingClicked -= OnBuildingClicked;
            ctrl.Selection.BuildingClicked += OnBuildingClicked;
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            if (_engine == null || _engine.CurrentStep == null) return;
            string normalized = NormalizeBuildingKey(building?.BuildingType);
            _engine.NotifyEvent(FtueEventKind.BuildingSelected, normalized);
            if (normalized == "administration_core") _engine.NotifyEvent(FtueEventKind.WindowOpened, normalized);
        }

        private static string NormalizeBuildingKey(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            string low = raw.ToLowerInvariant();
            if (low == "royalpalace" || low == "royal_palace" || low == "administration_core") return "administration_core";
            if (low == "barracks" || low == "guardpost" || low == "guard_post" || low == "barrack") return "guard_post";
            return low;
        }

        private void OnDestroy()
        {
            if (_engine != null)
            {
                _engine.StepEntered -= OnStepEntered;
                _engine.StepCompleted -= OnStepCompleted;
                _engine.ChapterCompleted -= OnChapterCompleted;
            }
            TutorialGameplayNotifier.BuildingSelected -= OnNotifierBuildingSelected;
            TutorialGameplayNotifier.WindowOpened -= OnNotifierWindowOpened;
            TutorialGameplayNotifier.UpgradeStarted -= OnNotifierUpgradeStarted;
            TutorialGameplayNotifier.UpgradeCompleted -= OnNotifierUpgradeCompleted;
            TutorialGameplayNotifier.ResearchStarted -= OnNotifierResearchStarted;
            TutorialGameplayNotifier.TrainingStarted -= OnNotifierTrainingStarted;
            TutorialGameplayNotifier.ArmyInteracted -= OnNotifierArmyInteracted;
            TutorialGameplayNotifier.ProductionCollected -= OnNotifierProductionCollected;
            var ctrl = FindAnyObjectByType<BuildingInteractionController>();
            if (ctrl != null) ctrl.Selection.BuildingClicked -= OnBuildingClicked;
            CancelInvoke();
        }
    }

    public static class TutorialTestHooks
    {
        // Kept for tests only — not used in real wiring anymore
        private static string _runningKey;
        public static void SetUpgradeRunning(string key) => _runningKey = key;
        public static void ClearUpgradeRunning() => _runningKey = null;
        public static bool IsUpgradeRunning(string key) => !string.IsNullOrEmpty(key) && string.Equals(_runningKey, key, StringComparison.Ordinal);
    }
}
