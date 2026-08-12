using System;
using System.Collections.Generic;
using BeeKingdom.AI;
using BeeKingdom.Builders;
using BeeKingdom.Buildings;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class ConstructionDemoBootstrap : MonoBehaviour
    {
        private const double FixedStepSeconds = 0.1d;

        private readonly Dictionary<string, GameObject> siteVisuals = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> beeVisuals = new Dictionary<string, GameObject>();
        private readonly List<DeliveryOrder> deliveryOrders = new List<DeliveryOrder>();
        private readonly List<string> unavailableFeatures = new List<string>();

        private SimulationTickEngine tickEngine;
        private PlayableHiveState hiveState;
        private BuildingPlacementManager placementManager;
        private ResourceDeliveryManager deliveryManager;
        private ConstructionDiagnosticsManager diagnosticsManager;
        private ConstructionDiagnosticReport diagnosticReport;
        private Camera sceneCamera;
        private string selectedSiteId;
        private bool followSelectedSite;
        private bool showSites = true;
        private bool showReservations = true;
        private bool showResourceFlow = true;
        private bool showPaths;
        private bool showTeams = true;
        private bool showTasks = true;
        private bool showDiagnostics = true;
        private bool showDetailedStats;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;

        public PlayableHiveState HiveState => hiveState;

        private void Awake()
        {
            sceneCamera = Camera.main;
            tickEngine = new SimulationTickEngine(FixedStepSeconds);
        }

        private void Start()
        {
            CreateFrameworkState();
            CreateConstructionOrders();
            BuildScenePrimitives();
            RefreshVisuals();
        }

        private void Update()
        {
            UpdateFps();
            HandleDebugKeys();
            HandleSelection();
            AdvanceSimulation();
            RefreshVisuals();
            MoveCamera();
        }

        private void CreateFrameworkState()
        {
            StarterPopulationProfile population = new StarterPopulationProfile(22, 4, 12, 4, 4, 100, 100, null);
            StarterHiveProfile hive = StarterHiveProfile.CreateDefault();
            StarterResourceProfile resources = new StarterResourceProfile(
                new Dictionary<ResourceType, double>
                {
                    { ResourceType.Nectar, 160d },
                    { ResourceType.Pollen, 140d },
                    { ResourceType.Water, 90d },
                    { ResourceType.Wax, 260d },
                    { ResourceType.Honey, 120d }
                },
                300d);

            hiveState = new NewGameInitializer().CreateNewGame(hive, population, resources);
            RegisterBehaviors(hiveState.AIManager);

            BuildingRegistry registry = new BuildingRegistry();
            RegisterConstructionDefinitions(registry);
            placementManager = new BuildingPlacementManager(registry, new PlacementGrid(16, 12), new PlacementRules(0, 4, false));
            deliveryManager = new ResourceDeliveryManager();
            diagnosticsManager = new ConstructionDiagnosticsManager();

            unavailableFeatures.Add("Placement framework validates and reserves grid positions, but is not yet integrated directly into HiveGrowthManager topology.");
            unavailableFeatures.Add("ResourceDeliveryManager exposes delivery lifecycle state, but no simulation system currently links delivery completion to HiveGrowthManager progress.");
            unavailableFeatures.Add("Builder team composition is inferred from task reservations and AI diagnostics because MultiAgentCoordinator is not wired into PlayableHiveState.");
            unavailableFeatures.Add("Path data and route rendering are unavailable in the current construction state surface.");
        }

        private static void RegisterBehaviors(BeeAIManager ai)
        {
            ai.RegisterBehavior(new BeeKingdom.AI.BehaviorDefinition("build-cell", BeeKingdom.Population.BeeIntent.Build, BehaviorActionType.Build, 10d));
            ai.RegisterBehavior(new BeeKingdom.AI.BehaviorDefinition("transport-resource", BeeKingdom.Population.BeeIntent.Transport, BehaviorActionType.Transport, 6d));
            ai.RegisterBehavior(new BeeKingdom.AI.BehaviorDefinition("repair-site", BeeKingdom.Population.BeeIntent.Repair, BehaviorActionType.Repair, 8d));
        }

        private static void RegisterConstructionDefinitions(BuildingRegistry registry)
        {
            registry.RegisterDefinition(new BuildingDefinition("storage-chamber", "Storage Chamber", BuildingCategory.Storage, new BuildingSize(2, 2), constructionCost: Cost("Wax", 10d), constructionTimeSeconds: 100d));
            registry.RegisterDefinition(new BuildingDefinition("nursery", "Nursery", BuildingCategory.Nursery, new BuildingSize(2, 2), constructionCost: Cost("Wax", 10d), constructionTimeSeconds: 100d));
            registry.RegisterDefinition(new BuildingDefinition("corridor", "Corridor", BuildingCategory.Corridor, new BuildingSize(1, 2), constructionCost: Cost("Wax", 8d), constructionTimeSeconds: 80d));
            registry.RegisterDefinition(new BuildingDefinition("honey-chamber", "Honey Chamber", BuildingCategory.Storage, new BuildingSize(2, 2), constructionCost: Cost("Wax", 10d), constructionTimeSeconds: 100d));
            registry.RegisterDefinition(new BuildingDefinition("pollen-chamber", "Pollen Chamber", BuildingCategory.Storage, new BuildingSize(2, 2), constructionCost: Cost("Wax", 10d), constructionTimeSeconds: 100d));
        }

        private static IReadOnlyList<BuildingResourceCost> Cost(string resourceId, double amount)
        {
            return new[] { new BuildingResourceCost(resourceId, amount) };
        }

        private void CreateConstructionOrders()
        {
            CreateOrder("storage-chamber", HiveChamberType.Utility, new BuildingPosition(1, 1), 90);
            CreateOrder("nursery", HiveChamberType.Nursery, new BuildingPosition(4, 1), 85);
            CreateOrder("corridor", HiveChamberType.Utility, new BuildingPosition(7, 1), 75);
            CreateOrder("honey-chamber", HiveChamberType.HoneyStorage, new BuildingPosition(10, 1), 80);
            CreateOrder("pollen-chamber", HiveChamberType.PollenStorage, new BuildingPosition(13, 1), 80);
            UpdateConstructionDiagnostics();
        }

        private void CreateOrder(string buildingId, HiveChamberType chamberType, BuildingPosition position, int priority)
        {
            PlacementRequest request = new PlacementRequest(buildingId, position);
            if (placementManager.ReservePlacement(request, 0d, 600d, out PlacementReservation reservation))
            {
                placementManager.ConfirmPlacement(reservation.ReservationId);
            }

            double wax = hiveState.ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Wax);
            HiveExpansionPlan plan = hiveState.GrowthManager.PlanExpansion(new HiveExpansionRequest(chamberType, hiveState.BeeIds.Count, wax, 28d, true, new[] { "starter-beekeeping" }));
            if (plan.IsApproved)
            {
                ConstructionSite site = hiveState.GrowthManager.CreateChamber(plan, "chamber-1");
                DeliveryOrder delivery = deliveryManager.CreateDeliveryRequest(site.SiteId, DeliveryResourceType.Wax, site.WaxCost, priority);
                deliveryManager.ReserveResources(delivery.OrderId, wax);
                deliveryManager.AssignTransporters(delivery.OrderId, 3);
                deliveryManager.StartDelivery(delivery.OrderId);
                deliveryOrders.Add(delivery);
            }
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

            if (ticks > 0)
            {
                UpdateConstructionDiagnostics();
            }
        }

        private SimulationExecutionContext CreateContext(double deltaSeconds)
        {
            SimulationTimestamp timestamp = new SimulationTimestamp(tickEngine.TickIndex, tickEngine.TotalSeconds);
            int totalMinutes = (int)(tickEngine.TotalSeconds / 60d);
            SimulationCalendar calendar = new SimulationCalendar(1 + totalMinutes / 1440, totalMinutes / 60 % 24, totalMinutes % 60, SimulationSeason.Spring);
            return new SimulationExecutionContext(timestamp, calendar, SimulationTickFrequency.TenHz, deltaSeconds, null);
        }

        private void UpdateConstructionDiagnostics()
        {
            HiveTopologySnapshot layout = hiveState.GrowthManager.GetLayout();
            int active = 0;
            double progress = 0d;
            foreach (ConstructionSite site in layout.ConstructionSites)
            {
                if (site.State == ConstructionSiteState.UnderConstruction)
                {
                    active++;
                }

                progress += GetProgress(site);
            }

            int builders = hiveState.AIManager.GetStatistics().ActiveCount;
            TaskStatistics taskStats = hiveState.TaskManager.GetStatistics();
            diagnosticReport = diagnosticsManager.GenerateDiagnostics(new ConstructionStatistics(
                layout.ConstructionSites.Count,
                averageDuration: 0d,
                averageProgress: layout.ConstructionSites.Count == 0 ? 0d : progress / layout.ConstructionSites.Count,
                waitingResources: 0,
                availableBuilders: Math.Max(0, 12 - builders),
                busyBuilders: builders,
                waitingTime: taskStats.QueuedTasks,
                congestions: 0,
                interruptions: hiveState.AIManager.Diagnostics.InterruptedCount,
                efficiency: layout.ConstructionSites.Count == 0 ? 1d : progress / layout.ConstructionSites.Count));
        }

        private void BuildScenePrimitives()
        {
            CreatePrimitive("Ground", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 18f), new Color(0.18f, 0.27f, 0.22f));
            CreatePrimitive("Hive Core", PrimitiveType.Sphere, new Vector3(-9f, 1.2f, 0f), new Vector3(2.2f, 2.2f, 2.2f), new Color(0.93f, 0.66f, 0.2f));
            CreatePrimitive("Nursery", PrimitiveType.Cube, new Vector3(-6f, 0.45f, 2.5f), Vector3.one, new Color(0.96f, 0.55f, 0.72f));
            CreatePrimitive("Storage", PrimitiveType.Cube, new Vector3(-6f, 0.45f, -2.5f), Vector3.one, new Color(0.45f, 0.7f, 0.95f));
            CreatePrimitive("Food Reserve", PrimitiveType.Cylinder, new Vector3(-10.5f, 0.35f, 3.8f), new Vector3(1.2f, 0.7f, 1.2f), new Color(0.95f, 0.78f, 0.24f));
            CreatePrimitive("Wax Reserve", PrimitiveType.Cylinder, new Vector3(-10.5f, 0.35f, -3.8f), new Vector3(1.2f, 0.7f, 1.2f), new Color(0.98f, 0.91f, 0.58f));

            for (int i = 0; i < 8; i++)
            {
                CreatePrimitive("Resource Source " + (i + 1), PrimitiveType.Sphere, new Vector3(8f + (i % 4) * 1.8f, 0.3f, -4f + (i / 4) * 8f), new Vector3(0.6f, 0.6f, 0.6f), new Color(0.33f, 0.74f, 0.48f));
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

        private void RefreshVisuals()
        {
            HiveTopologySnapshot layout = hiveState.GrowthManager.GetLayout();
            for (int i = 0; i < layout.ConstructionSites.Count; i++)
            {
                ConstructionSite site = layout.ConstructionSites[i];
                if (!siteVisuals.TryGetValue(site.SiteId, out GameObject visual))
                {
                    visual = CreatePrimitive("Construction " + site.SiteId, PrimitiveType.Cube, GetSitePosition(i), new Vector3(1.4f, 0.35f, 1.4f), Color.gray);
                    visual.AddComponent<ConstructionSiteVisual>().SiteId = site.SiteId;
                    siteVisuals.Add(site.SiteId, visual);
                }

                float height = Mathf.Lerp(0.25f, 1.6f, (float)GetProgress(site));
                visual.transform.localScale = new Vector3(1.4f, height, 1.4f);
                visual.transform.position = new Vector3(GetSitePosition(i).x, height * 0.5f, GetSitePosition(i).z);
                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = site.SiteId == selectedSiteId ? Color.white : SiteColor(site);
                }
            }

            RefreshBeeVisuals(layout);
        }

        private void RefreshBeeVisuals(HiveTopologySnapshot layout)
        {
            IReadOnlyList<string> bees = hiveState.BeeIds;
            for (int i = 0; i < bees.Count; i++)
            {
                string beeId = bees[i];
                if (beeId == hiveState.QueenId)
                {
                    continue;
                }

                if (!beeVisuals.TryGetValue(beeId, out GameObject visual))
                {
                    visual = CreatePrimitive("Builder Bee " + beeId, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), new Color(1f, 0.78f, 0.12f));
                    beeVisuals.Add(beeId, visual);
                }

                ConstructionSite target = layout.ConstructionSites.Count == 0 ? null : layout.ConstructionSites[i % layout.ConstructionSites.Count];
                Vector3 center = target == null ? new Vector3(-9f, 0f, 0f) : GetSitePosition(i % layout.ConstructionSites.Count);
                float angle = i * 2.399963f + (float)tickEngine.TotalSeconds * 0.8f;
                visual.transform.position = center + new Vector3(Mathf.Cos(angle) * 1.2f, 0.75f + (i % 3) * 0.08f, Mathf.Sin(angle) * 1.2f);
            }
        }

        private static Vector3 GetSitePosition(int index)
        {
            return new Vector3(-1.5f + index * 3f, 0.2f, 0f);
        }

        private static Color SiteColor(ConstructionSite site)
        {
            if (site.State == ConstructionSiteState.Upgradeable) return new Color(0.25f, 0.82f, 0.43f);
            if (site.State == ConstructionSiteState.Completed) return new Color(0.3f, 0.75f, 0.5f);
            if (site.State == ConstructionSiteState.UnderConstruction) return new Color(0.84f, 0.58f, 0.28f);
            return new Color(0.52f, 0.52f, 0.52f);
        }

        private static double GetProgress(ConstructionSite site)
        {
            return site.RequiredWorkSeconds <= 0d ? 1d : Math.Min(1d, site.ProgressSeconds / site.RequiredWorkSeconds);
        }

        private void HandleDebugKeys()
        {
            if (Input.GetKeyDown(KeyCode.F1)) showSites = !showSites;
            if (Input.GetKeyDown(KeyCode.F2)) showReservations = !showReservations;
            if (Input.GetKeyDown(KeyCode.F3)) showResourceFlow = !showResourceFlow;
            if (Input.GetKeyDown(KeyCode.F4)) showPaths = !showPaths;
            if (Input.GetKeyDown(KeyCode.F5)) showTeams = !showTeams;
            if (Input.GetKeyDown(KeyCode.F6)) showTasks = !showTasks;
            if (Input.GetKeyDown(KeyCode.F7)) showDiagnostics = !showDiagnostics;
            if (Input.GetKeyDown(KeyCode.F8)) showDetailedStats = !showDetailedStats;
            if (Input.GetKeyDown(KeyCode.F)) followSelectedSite = !followSelectedSite;
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
                ConstructionSiteVisual visual = hit.collider.GetComponent<ConstructionSiteVisual>();
                selectedSiteId = visual != null ? visual.SiteId : null;
            }
        }

        private void MoveCamera()
        {
            if (sceneCamera == null)
            {
                return;
            }

            Transform cameraTransform = sceneCamera.transform;
            if (followSelectedSite && !string.IsNullOrEmpty(selectedSiteId) && siteVisuals.TryGetValue(selectedSiteId, out GameObject visual))
            {
                Vector3 target = visual.transform.position + new Vector3(0f, 5f, -7f);
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, target, Time.deltaTime * 4f);
                cameraTransform.LookAt(visual.transform.position);
                return;
            }

            float speed = Input.GetKey(KeyCode.LeftShift) ? 15f : 7f;
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            cameraTransform.position += cameraTransform.TransformDirection(input) * speed * Time.deltaTime;
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
            if (hiveState == null)
            {
                return;
            }

            GUI.Box(new Rect(12, 12, 390, showDetailedStats ? 620 : 460), "Construction Demo");
            GUILayout.BeginArea(new Rect(24, 40, 366, showDetailedStats ? 580 : 420));
            DrawStats();
            DrawSelectedSite();
            GUILayout.EndArea();
        }

        private void DrawStats()
        {
            HiveTopologySnapshot layout = hiveState.GrowthManager.GetLayout();
            TaskStatistics tasks = hiveState.TaskManager.GetStatistics();
            StorageStatistics inventory = hiveState.InventoryManager.QueryInventory();

            GUILayout.Label("Tick: " + tickEngine.TickIndex + " | TPS: " + (1d / FixedStepSeconds).ToString("0.0") + " | Time: " + hiveState.Diagnostics.SimulatedSeconds.ToString("0.0") + "s");
            GUILayout.Label("Construction: " + ActiveSites(layout) + " active, " + hiveState.GrowthManager.Diagnostics.CompletedChambers + " completed, " + layout.ConstructionSites.Count + " sites");
            GUILayout.Label("Progress avg: " + (diagnosticReport?.Statistics.AverageProgress * 100d ?? 0d).ToString("0.0") + "% | Health: " + (diagnosticReport?.Health.ToString() ?? "Normal"));
            GUILayout.Label("Resources: wax " + hiveState.ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Wax).ToString("0.0") + " | stored " + inventory.TotalAmount.ToString("0.0") + "/" + inventory.TotalCapacity.ToString("0.0"));
            GUILayout.Label("Tasks: " + tasks.TotalTasks + " total, " + tasks.QueuedTasks + " queued, " + tasks.AssignedTasks + " assigned");
            GUILayout.Label("Reservations: placement " + placementManager.ReservationCount + ", task " + hiveState.TaskManager.Diagnostics.ReservationCount + ", storage " + hiveState.InventoryManager.Diagnostics.ReservationCount);
            GUILayout.Label("Deliveries: " + deliveryOrders.Count + " orders, " + deliveryManager.Diagnostics.Started + " started, " + deliveryManager.Diagnostics.Completed + " completed");
            GUILayout.Label("Performance: " + fps.ToString("0") + " FPS | " + (GC.GetTotalMemory(false) / 1048576f).ToString("0.0") + " MB");
            GUILayout.Label("F1 Sites " + Toggle(showSites) + " F2 Reservations " + Toggle(showReservations) + " F3 Flow " + Toggle(showResourceFlow));
            GUILayout.Label("F4 Paths " + Toggle(showPaths) + " F5 Teams " + Toggle(showTeams) + " F6 Tasks " + Toggle(showTasks));
            GUILayout.Label("F7 Diagnostics " + Toggle(showDiagnostics) + " F8 Details " + Toggle(showDetailedStats));

            if (showDetailedStats)
            {
                GUILayout.Label("Placement validations: " + placementManager.Diagnostics.Validations + " | confirmations: " + placementManager.Diagnostics.Confirmations);
                GUILayout.Label("Growth planned: " + hiveState.GrowthManager.Diagnostics.PlannedChambers + " | topology rev: " + hiveState.GrowthManager.Diagnostics.TopologyRevisions);
                GUILayout.Label("AI active: " + hiveState.AIManager.GetStatistics().ActiveCount + " | waiting: " + hiveState.AIManager.GetStatistics().WaitingCount);
                GUILayout.Label("Unavailable documented: " + unavailableFeatures.Count);
            }

            if (showDiagnostics)
            {
                ColonyIntegrationDemoDiagnostics.DrawSceneItems("ConstructionDemo", 3);
            }
        }

        private static int ActiveSites(HiveTopologySnapshot layout)
        {
            int active = 0;
            foreach (ConstructionSite site in layout.ConstructionSites)
            {
                if (site.State == ConstructionSiteState.UnderConstruction)
                {
                    active++;
                }
            }

            return active;
        }

        private void DrawSelectedSite()
        {
            if (string.IsNullOrEmpty(selectedSiteId))
            {
                GUILayout.Label("Selected site: none");
                return;
            }

            ConstructionSite site = null;
            foreach (ConstructionSite candidate in hiveState.GrowthManager.GetLayout().ConstructionSites)
            {
                if (candidate.SiteId == selectedSiteId)
                {
                    site = candidate;
                    break;
                }
            }

            if (site == null)
            {
                return;
            }

            GUILayout.Space(8);
            GUILayout.Label("Selected site: " + site.SiteId);
            GUILayout.Label("Type: " + site.ChamberType + " | State: " + site.State);
            GUILayout.Label("Progress: " + (GetProgress(site) * 100d).ToString("0.0") + "%");
            GUILayout.Label("Wax required: " + site.WaxCost.ToString("0.0") + " | Work: " + site.ProgressSeconds.ToString("0.0") + "/" + site.RequiredWorkSeconds.ToString("0.0"));
            GUILayout.Label("Task: " + site.TaskId);
            GUILayout.Label("Follow selected: " + Toggle(followSelectedSite) + " (F)");
        }

        private static string Toggle(bool value)
        {
            return value ? "on" : "off";
        }
    }

    public sealed class ConstructionSiteVisual : MonoBehaviour
    {
        public string SiteId { get; set; }
    }
}
