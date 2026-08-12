using System;
using System.Collections.Generic;
using BeeKingdom.Audio;
using BeeKingdom.AI;
using BeeKingdom.Colony;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using BeeKingdom.Population;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class LivingHiveDemoBootstrap : MonoBehaviour
    {
        private const double FixedStepSeconds = 0.1d;
        private static readonly bool UseProductHiveView = true;

        private readonly Dictionary<string, BeeVisual> beeVisuals = new Dictionary<string, BeeVisual>();
        private readonly List<string> unavailableFeatures = new List<string>();

        private SimulationTickEngine tickEngine;
        private PlayableHiveState hiveState;
        private DemoScenario demoScenario;
        private Camera sceneCamera;
        private Transform beeRoot;
        private string selectedBeeId;
        private bool followSelected;
        private bool showTasks = true;
        private bool showReservations = true;
        private bool showCommunication;
        private bool showPaths;
        private bool showBehaviorTrees;
        private bool showWorkZones;
        private bool showBuildings = true;
        private bool showDetailedStats;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;
        private bool productViewStarted;
        private bool applicationPaused;
        private bool applicationFocused = true;
        private bool productViewSuspended;

        public PlayableHiveState HiveState => hiveState;
        public DemoScenario DemoScenario => demoScenario;
        public IReadOnlyList<string> UnavailableFeatures => unavailableFeatures;

        private void Awake()
        {
            sceneCamera = SandboxPlaygroundBootstrap.EnsureRenderableCamera(Camera.main);
            tickEngine = new SimulationTickEngine(FixedStepSeconds);
        }

        private void Start()
        {
            MusicManager.EnsureInstance().Play(MusicTrack.Hive);

            if (UseProductHiveView)
            {
                HiveViewProductUiPresenter.EnsureSceneObjects();
                // Ne force plus l'entree directe dans la ruche (splashAuthGateState = EnteredHive) :
                // Jeff a besoin de l'ecran de demarrage reel (onglet Connexion) pour s'authentifier
                // contre le serveur officiel. Le splash s'affiche desormais normalement a chaque
                // Play Mode ; cliquer "Connexion"/"Continuer en demo locale" y mene comme avant.
                HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.LocalPreview);
                if (!HiveViewProductUiPresenter.ResumeGuidedWorldTransitionAfterHiveLoad())
                {
                    HiveViewProductUiPresenter.StartGuidedCollectionTutorial();
                }
                productViewStarted = true;
                UpdateProductViewSuspension();
                return;
            }

            StartLegacySimulationPrototype();
        }

        private void StartLegacySimulationPrototype()
        {
            StartLivingHiveDemo();
            BuildScenePrimitives();
            RefreshBeeVisuals();
        }

        private void Update()
        {
            UpdateFps();
            if (UseProductHiveView)
            {
                HiveViewProductUiPresenter.HandlePointer(sceneCamera);
                return;
            }

            HandleDebugKeys();
            HandleSelection();
            AdvanceSimulation();
            RefreshBeeVisuals();
            MoveCamera();
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            UpdateProductViewSuspension();
        }

        private void OnApplicationFocus(bool focused)
        {
            applicationFocused = focused;
            UpdateProductViewSuspension();
        }

        private void OnApplicationQuit()
        {
            if (UseProductHiveView && productViewStarted)
                HiveViewProductUiPresenter.FlushLocalPreviewManualProductionForRuntime();
        }

        private void OnDisable()
        {
            if (UseProductHiveView && productViewStarted)
                HiveViewProductUiPresenter.FlushLocalPreviewManualProductionForRuntime();
        }

        private void UpdateProductViewSuspension()
        {
            if (!UseProductHiveView || !productViewStarted) return;
            bool shouldSuspend = applicationPaused || !applicationFocused;
            if (shouldSuspend == productViewSuspended) return;
            productViewSuspended = shouldSuspend;
            if (productViewSuspended)
                HiveViewProductUiPresenter.FlushLocalPreviewManualProductionForRuntime();
            else
                HiveViewProductUiPresenter.ResumeLocalPreviewManualProductionForRuntime();
        }

        private void StartLivingHiveDemo()
        {
            DemoManager demoManager = new DemoManager();
            demoManager.RegisterDemo(DemoDefinition.CreateLivingHive());
            demoScenario = demoManager.StartDemo("living-hive");

            StarterPopulationProfile populationProfile = new StarterPopulationProfile(
                initialWorkers: 30,
                initialNurses: 5,
                initialBuilders: 5,
                initialScouts: 5,
                initialSoldiers: 4,
                health: 100,
                energy: 100,
                lifecycleRules: null);

            StarterHiveProfile hiveProfile = StarterHiveProfile.CreateDefault();
            StarterResourceProfile resourceProfile = StarterResourceProfile.CreateDefault();
            hiveState = new NewGameInitializer().CreateNewGame(hiveProfile, populationProfile, resourceProfile);

            RegisterAvailableBehaviors(hiveState.AIManager);
            CreateActiveConstructionSite(hiveProfile, resourceProfile);
            RecordUnavailableFeatures();
        }

        private static void RegisterAvailableBehaviors(BeeAIManager aiManager)
        {
            aiManager.RegisterBehavior(new BehaviorDefinition("build-cell", BeeIntent.Build, BehaviorActionType.Build, 12d));
            aiManager.RegisterBehavior(new BehaviorDefinition("gather-resource", BeeIntent.Gather, BehaviorActionType.Gather, 8d));
            aiManager.RegisterBehavior(new BehaviorDefinition("transport-resource", BeeIntent.Transport, BehaviorActionType.Transport, 7d));
            aiManager.RegisterBehavior(new BehaviorDefinition("feed-larvae", BeeIntent.Nurse, BehaviorActionType.Feed, 6d));
            aiManager.RegisterBehavior(new BehaviorDefinition("defend-hive", BeeIntent.Defend, BehaviorActionType.Defend, 10d));
        }

        private void CreateActiveConstructionSite(StarterHiveProfile hiveProfile, StarterResourceProfile resourceProfile)
        {
            double wax = resourceProfile.Amounts.TryGetValue(ResourceType.Wax, out double amount) ? amount : 0d;
            HiveExpansionPlan plan = hiveState.GrowthManager.PlanExpansion(
                new HiveExpansionRequest(
                    HiveChamberType.WaxWorkshop,
                    hiveState.BeeIds.Count,
                    wax,
                    28d,
                    true,
                    hiveProfile.UnlockedTechnologyIds));

            if (plan.IsApproved)
            {
                hiveState.GrowthManager.CreateChamber(plan, "chamber-1");
            }
        }

        private void RecordUnavailableFeatures()
        {
            unavailableFeatures.Add("Harvester and Guard are represented by the available Scout and Soldier lifecycle roles.");
            unavailableFeatures.Add("Pathfinding and destination data are not exposed by the current frameworks.");
            unavailableFeatures.Add("Bee needs, personality, memory, experience, and behavior tree instance data are framework-level features not wired into PlayableHiveState yet.");
            unavailableFeatures.Add("Communication and pheromone runtime data are not exposed by PlayableHiveState yet.");
        }

        private void AdvanceSimulation()
        {
            int ticks = tickEngine.Advance(Time.deltaTime, SimulationTickMode.Fixed);
            for (int i = 0; i < ticks; i++)
            {
                SimulationExecutionContext context = CreateContext(FixedStepSeconds);
                hiveState.Controller.Execute(context);
                hiveState.AIManager.Execute(context);
            }
        }

        private SimulationExecutionContext CreateContext(double deltaSeconds)
        {
            SimulationTimestamp timestamp = new SimulationTimestamp(tickEngine.TickIndex, tickEngine.TotalSeconds);
            int totalMinutes = (int)(tickEngine.TotalSeconds / 60d);
            SimulationCalendar calendar = new SimulationCalendar(1 + totalMinutes / 1440, totalMinutes / 60 % 24, totalMinutes % 60, SimulationSeason.Spring);
            return new SimulationExecutionContext(timestamp, calendar, SimulationTickFrequency.TenHz, deltaSeconds, null);
        }

        private void BuildScenePrimitives()
        {
            beeRoot = new GameObject("Bee Visuals").transform;
            CreatePrimitive("Ground", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(28f, 0.2f, 18f), new Color(0.22f, 0.33f, 0.21f));
            CreatePrimitive("Hive", PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), new Vector3(2.6f, 2.4f, 2.6f), new Color(0.93f, 0.68f, 0.22f));
            CreatePrimitive("Nursery", PrimitiveType.Cube, new Vector3(-3.2f, 0.45f, 1.8f), Vector3.one, new Color(0.95f, 0.55f, 0.68f));
            CreatePrimitive("Food Reserve", PrimitiveType.Cube, new Vector3(3.2f, 0.45f, 1.8f), Vector3.one, new Color(0.96f, 0.82f, 0.28f));
            CreatePrimitive("Wax Reserve", PrimitiveType.Cube, new Vector3(3.2f, 0.45f, -1.8f), Vector3.one, new Color(0.97f, 0.91f, 0.62f));
            CreatePrimitive("Storage Chamber", PrimitiveType.Cube, new Vector3(-3.2f, 0.45f, -1.8f), Vector3.one, new Color(0.52f, 0.72f, 0.95f));
            CreatePrimitive("Construction Site", PrimitiveType.Cylinder, new Vector3(5.2f, 0.25f, -3f), new Vector3(1.4f, 0.5f, 1.4f), new Color(0.75f, 0.53f, 0.32f));

            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 9f, 0.25f, Mathf.Sin(angle) * 5f);
                CreatePrimitive("Flower " + (i + 1), PrimitiveType.Sphere, position, new Vector3(0.45f, 0.45f, 0.45f), i % 2 == 0 ? new Color(0.92f, 0.35f, 0.4f) : new Color(0.42f, 0.62f, 0.95f));
            }
        }

        private static GameObject CreatePrimitive(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = objectName;
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            return primitive;
        }

        private void RefreshBeeVisuals()
        {
            IReadOnlyList<string> beeIds = hiveState.BeeIds;
            for (int i = 0; i < beeIds.Count; i++)
            {
                string beeId = beeIds[i];
                if (!beeVisuals.TryGetValue(beeId, out BeeVisual visual))
                {
                    GameObject bee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bee.name = "Bee " + beeId;
                    bee.transform.SetParent(beeRoot, false);
                    bee.transform.localScale = beeId == hiveState.QueenId ? new Vector3(0.45f, 0.45f, 0.45f) : new Vector3(0.22f, 0.22f, 0.22f);
                    visual = bee.AddComponent<BeeVisual>();
                    visual.BeeId = beeId;
                    beeVisuals.Add(beeId, visual);
                }

                BeeLifecycleBee beeRecord = hiveState.LifecycleManager.GetBee(beeId);
                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                renderer.material.color = GetRoleColor(beeRecord.CurrentRole, beeId == selectedBeeId);
                }

                visual.transform.position = ResolveBeePosition(beeId, beeRecord.CurrentRole, i);
            }
        }

        private Vector3 ResolveBeePosition(string beeId, BeeLifecycleRole role, int index)
        {
            float time = (float)tickEngine.TotalSeconds;
            float ring = 1.6f + index % 9 * 0.18f;
            float angle = index * 2.399963f + time * ResolveStateSpeed(beeId);
            Vector3 center = ResolveRoleCenter(role);
            return center + new Vector3(Mathf.Cos(angle) * ring, 0.75f + (index % 4) * 0.08f, Mathf.Sin(angle) * ring);
        }

        private float ResolveStateSpeed(string beeId)
        {
            if (beeId == hiveState.QueenId)
            {
                return 0.05f;
            }

            BeeBehaviorState state = hiveState.AIManager.GetCurrentState(beeId);
            if (state == BeeBehaviorState.Building || state == BeeBehaviorState.Harvesting || state == BeeBehaviorState.Exploring)
            {
                return 0.55f;
            }

            if (state == BeeBehaviorState.Guarding)
            {
                return 0.25f;
            }

            return 0.12f;
        }

        private static Vector3 ResolveRoleCenter(BeeLifecycleRole role)
        {
            switch (role)
            {
                case BeeLifecycleRole.Queen: return new Vector3(0f, 0f, 0f);
                case BeeLifecycleRole.Nurse: return new Vector3(-3.2f, 0f, 1.8f);
                case BeeLifecycleRole.Builder: return new Vector3(5.2f, 0f, -3f);
                case BeeLifecycleRole.Scout: return new Vector3(6.5f, 0f, 3.5f);
                case BeeLifecycleRole.Soldier: return new Vector3(-5.5f, 0f, -3f);
                default: return new Vector3(0f, 0f, 0f);
            }
        }

        private static Color GetRoleColor(BeeLifecycleRole role, bool selected)
        {
            if (selected)
            {
                return Color.white;
            }

            switch (role)
            {
                case BeeLifecycleRole.Queen: return new Color(0.74f, 0.28f, 0.88f);
                case BeeLifecycleRole.Nurse: return new Color(0.95f, 0.47f, 0.62f);
                case BeeLifecycleRole.Builder: return new Color(0.86f, 0.58f, 0.25f);
                case BeeLifecycleRole.Scout: return new Color(0.28f, 0.68f, 0.95f);
                case BeeLifecycleRole.Soldier: return new Color(0.85f, 0.23f, 0.22f);
                default: return new Color(1f, 0.78f, 0.16f);
            }
        }

        private void HandleDebugKeys()
        {
            if (Input.GetKeyDown(KeyCode.F1)) showTasks = !showTasks;
            if (Input.GetKeyDown(KeyCode.F2)) showReservations = !showReservations;
            if (Input.GetKeyDown(KeyCode.F3)) showCommunication = !showCommunication;
            if (Input.GetKeyDown(KeyCode.F4)) showPaths = !showPaths;
            if (Input.GetKeyDown(KeyCode.F5)) showBehaviorTrees = !showBehaviorTrees;
            if (Input.GetKeyDown(KeyCode.F6)) showWorkZones = !showWorkZones;
            if (Input.GetKeyDown(KeyCode.F7)) showBuildings = !showBuildings;
            if (Input.GetKeyDown(KeyCode.F8)) showDetailedStats = !showDetailedStats;
            if (Input.GetKeyDown(KeyCode.F)) followSelected = !followSelected;
        }

        private void HandleSelection()
        {
            if (!Input.GetMouseButtonDown(0) || sceneCamera == null)
            {
                return;
            }

            Ray ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                BeeVisual visual = hit.collider.GetComponent<BeeVisual>();
                selectedBeeId = visual != null ? visual.BeeId : null;
            }
        }

        private void MoveCamera()
        {
            if (sceneCamera == null)
            {
                return;
            }

            Transform cameraTransform = sceneCamera.transform;
            if (followSelected && !string.IsNullOrEmpty(selectedBeeId) && beeVisuals.TryGetValue(selectedBeeId, out BeeVisual visual))
            {
                Vector3 target = visual.transform.position + new Vector3(0f, 5f, -7f);
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, target, Time.deltaTime * 4f);
                cameraTransform.LookAt(visual.transform.position);
                return;
            }

            float speed = Input.GetKey(KeyCode.LeftShift) ? 15f : 7f;
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            cameraTransform.position += cameraTransform.TransformDirection(input) * speed * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q)) cameraTransform.position += Vector3.down * speed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) cameraTransform.position += Vector3.up * speed * Time.deltaTime;
            if (Input.GetMouseButton(1))
            {
                cameraTransform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * 4f, Space.World);
                cameraTransform.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * 4f, Space.Self);
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Math.Abs(scroll) > 0.01f)
            {
                cameraTransform.position += cameraTransform.forward * scroll * 3f;
            }
        }

        private void UpdateFps()
        {
            float delta = Time.unscaledDeltaTime;
            fpsAccumulator += delta > 0f ? 1f / delta : 0f;
            fpsFrames++;
            fpsTimer += delta;
            if (fpsTimer >= 0.5f)
            {
                fps = fpsAccumulator / Math.Max(1, fpsFrames);
                fpsAccumulator = 0f;
                fpsFrames = 0;
                fpsTimer = 0f;
            }
        }

        private void OnGUI()
        {
            if (UseProductHiveView)
            {
                HiveViewProductUiPresenter.Draw(fps, Screen.width < 900);
                return;
            }

            if (hiveState == null)
            {
                return;
            }

            GUI.Box(new Rect(12, 12, 360, showDetailedStats ? 560 : 405), "Living Hive");
            GUILayout.BeginArea(new Rect(24, 40, 336, showDetailedStats ? 520 : 365));
            DrawMainStats();
            DrawSelectedBee();
            GUILayout.EndArea();
        }

        private void DrawMainStats()
        {
            IntegrationDiagnostics diagnostics = hiveState.Diagnostics;
            StorageStatistics inventory = hiveState.InventoryManager.QueryInventory();
            TaskStatistics tasks = hiveState.TaskManager.GetStatistics();
            BeeAIStatistics ai = hiveState.AIManager.GetStatistics();

            GUILayout.Label("Demo: " + demoScenario.State);
            GUILayout.Label("Tick: " + tickEngine.TickIndex + " | TPS: " + (1d / FixedStepSeconds).ToString("0.0") + " | Time: " + diagnostics.SimulatedSeconds.ToString("0.0") + "s");
            GUILayout.Label("Population: " + diagnostics.Population + " | Queen: " + hiveState.QueenId);
            GUILayout.Label("Inventory: " + inventory.TotalAmount.ToString("0.0") + "/" + inventory.TotalCapacity.ToString("0.0"));
            GUILayout.Label("Nectar: " + hiveState.ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Nectar).ToString("0.0") +
                " | Pollen: " + hiveState.ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Pollen).ToString("0.0"));
            GUILayout.Label("Wax: " + hiveState.ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Wax).ToString("0.0") +
                " | Honey: " + hiveState.ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Honey).ToString("0.0"));
            GUILayout.Label("Tasks: " + tasks.TotalTasks + " total, " + tasks.QueuedTasks + " queued, " + tasks.AssignedTasks + " assigned");
            GUILayout.Label("AI: " + ai.BrainCount + " brains, " + ai.ActiveCount + " active, " + ai.WaitingCount + " waiting");
            GUILayout.Label("Performance: " + fps.ToString("0") + " FPS | Tick dt: " + diagnostics.AverageTickSeconds.ToString("0.000") + "s | Mem: " + (GC.GetTotalMemory(false) / 1048576f).ToString("0.0") + " MB");
            GUILayout.Label("F1 Tasks " + Toggle(showTasks) + "  F2 Reservations " + Toggle(showReservations) + "  F7 Buildings " + Toggle(showBuildings));

            if (showDetailedStats)
            {
                GUILayout.Label("F3 Communication " + Toggle(showCommunication) + " | F4 Paths " + Toggle(showPaths));
                GUILayout.Label("F5 Behavior Trees " + Toggle(showBehaviorTrees) + " | F6 Zones " + Toggle(showWorkZones));
                GUILayout.Label("Lifecycle: " + hiveState.LifecycleManager.Diagnostics.BeeCount + " total, " + hiveState.LifecycleManager.Diagnostics.AliveCount + " alive");
                GUILayout.Label("Construction: " + hiveState.GrowthManager.Diagnostics.CompletedChambers + " completed, topology rev " + hiveState.GrowthManager.Diagnostics.TopologyRevisions);
                GUILayout.Label("Reservations: tasks " + hiveState.TaskManager.Diagnostics.ReservationCount + ", storage " + hiveState.InventoryManager.Diagnostics.ReservationCount);
                GUILayout.Label("Unavailable: " + unavailableFeatures.Count + " items documented in report.");
            }
        }

        private void DrawSelectedBee()
        {
            if (string.IsNullOrEmpty(selectedBeeId))
            {
                GUILayout.Label("Selected bee: none");
                return;
            }

            BeeLifecycleBee bee = hiveState.LifecycleManager.GetBee(selectedBeeId);
            GUILayout.Space(8);
            GUILayout.Label("Selected bee: " + selectedBeeId);
            GUILayout.Label("Caste/role: " + bee.CurrentRole + " | Stage: " + bee.CurrentStage);
            GUILayout.Label("Age: " + bee.Age.AgeSeconds.ToString("0.0") + "s | Health: " + bee.Health + " | Energy: " + bee.Energy);

            if (selectedBeeId != hiveState.QueenId)
            {
                BeeBehaviorState state = hiveState.AIManager.GetCurrentState(selectedBeeId);
                BehaviorContext behavior = hiveState.AIManager.QueryBehavior(selectedBeeId);
                GUILayout.Label("AI state: " + state);
                GUILayout.Label("Behavior: " + (behavior == null ? "not exposed/idle" : behavior.BehaviorId + " " + behavior.State));
            }
            else
            {
                GUILayout.Label("AI state: queen managed by QueenManager");
            }

            GUILayout.Label("Follow selected: " + Toggle(followSelected) + " (F)");
        }

        private static string Toggle(bool value)
        {
            return value ? "on" : "off";
        }
    }

    public sealed class BeeVisual : MonoBehaviour
    {
        public string BeeId { get; set; }
    }
}
