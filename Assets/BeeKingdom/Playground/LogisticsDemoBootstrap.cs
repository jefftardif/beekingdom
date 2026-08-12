using System;
using System.Collections.Generic;
using BeeKingdom.Builders;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class LogisticsDemoBootstrap : MonoBehaviour
    {
        private const double FixedStepSeconds = 0.1d;

        private readonly Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();
        private readonly List<DeliveryOrder> deliveries = new List<DeliveryOrder>();
        private readonly Queue<ResourceTransaction> recentTransactions = new Queue<ResourceTransaction>();

        private SimulationTickEngine tickEngine;
        private PlayableHiveState hiveState;
        private ResourceDeliveryManager deliveryManager;
        private Camera sceneCamera;
        private bool showFlows = true;
        private bool showStocks = true;
        private bool showReservations = true;
        private bool showPaths;
        private bool showBottlenecks = true;
        private bool showStatistics = true;
        private bool showProducers = true;
        private bool showConsumers = true;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;
        private double nextLogisticsPulse;
        private int cycleIndex;

        public PlayableHiveState HiveState => hiveState;
        public ResourceDeliveryManager DeliveryManager => deliveryManager;

        private void Awake()
        {
            sceneCamera = Camera.main;
            tickEngine = new SimulationTickEngine(FixedStepSeconds);
        }

        private void Start()
        {
            CreateFrameworkState();
            BuildScenePrimitives();
            RunLogisticsPulse();
        }

        private void Update()
        {
            UpdateFps();
            HandleDebugKeys();
            AdvanceSimulation();
            MoveCamera();
        }

        private void CreateFrameworkState()
        {
            StarterPopulationProfile population = new StarterPopulationProfile(24, 5, 8, 8, 5, 100, 100, null);
            StarterHiveProfile hive = new StarterHiveProfile(
                "logistics-demo-hive",
                "player",
                "queen-1",
                new HiveCapacity(600, 180, 180),
                1,
                1f,
                1d,
                new[] { HiveChamberType.Entrance, HiveChamberType.RoyalChamber, HiveChamberType.Nursery, HiveChamberType.HoneyStorage, HiveChamberType.PollenStorage, HiveChamberType.WaxWorkshop },
                new[] { "starter-beekeeping" });
            StarterResourceProfile resources = new StarterResourceProfile(
                new Dictionary<ResourceType, double>
                {
                    { ResourceType.Nectar, 120d },
                    { ResourceType.Pollen, 100d },
                    { ResourceType.Water, 80d },
                    { ResourceType.Wax, 220d },
                    { ResourceType.Honey, 90d }
                },
                320d);

            hiveState = new NewGameInitializer().CreateNewGame(hive, population, resources);
            ActivateQueen(hiveState);
            deliveryManager = new ResourceDeliveryManager();
            CreateStorageCells();
            CreateConstructionDemand();
        }

        private static void ActivateQueen(PlayableHiveState state)
        {
            state.QueenManager.UpdateState(state.QueenId, QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.ActiveQueen);
        }

        private void CreateStorageCells()
        {
            CreateAndFillCell("nectar-cell", ResourceType.Nectar, new StoragePosition(1, 0), 120d, 40d, "food");
            CreateAndFillCell("pollen-cell", ResourceType.Pollen, new StoragePosition(2, 0), 120d, 35d, "food");
            CreateAndFillCell("water-cell", ResourceType.Water, new StoragePosition(3, 0), 100d, 25d, "water");
            CreateAndFillCell("honey-cell", ResourceType.Honey, new StoragePosition(4, 0), 140d, 45d, "food");
            CreateAndFillCell("wax-cell", ResourceType.Wax, new StoragePosition(5, 0), 180d, 80d, "construction");
        }

        private void CreateAndFillCell(string cellId, ResourceType type, StoragePosition position, double capacity, double amount, string clusterId)
        {
            hiveState.InventoryManager.CreateCell(cellId, position, type, capacity, clusterId);
            StorageReservation reservation = hiveState.InventoryManager.ReserveSpace(type, amount, position, StoragePolicy.Specialized);
            if (reservation.IsValid)
            {
                hiveState.InventoryManager.Deposit(reservation, tickEngine.TotalSeconds);
            }
        }

        private void CreateConstructionDemand()
        {
            for (int i = 0; i < 4; i++)
            {
                double wax = hiveState.ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Wax);
                HiveExpansionPlan plan = hiveState.GrowthManager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.Utility, hiveState.BeeIds.Count, wax, 28d, true, new[] { "starter-beekeeping" }));
                if (!plan.IsApproved) continue;

                ConstructionSite site = hiveState.GrowthManager.CreateChamber(plan, "chamber-1");
                DeliveryOrder order = deliveryManager.CreateDeliveryRequest(site.SiteId, DeliveryResourceType.Wax, site.WaxCost, 80 - i);
                deliveryManager.ReserveResources(order.OrderId, wax);
                deliveryManager.AssignTransporters(order.OrderId, 2);
                deliveryManager.StartDelivery(order.OrderId);
                deliveries.Add(order);
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
                if (tickEngine.TotalSeconds >= nextLogisticsPulse)
                {
                    RunLogisticsPulse();
                    nextLogisticsPulse = tickEngine.TotalSeconds + 12d;
                }
            }
        }

        private SimulationExecutionContext CreateContext(double deltaSeconds)
        {
            SimulationTimestamp timestamp = new SimulationTimestamp(tickEngine.TickIndex, tickEngine.TotalSeconds);
            int totalMinutes = (int)(tickEngine.TotalSeconds / 60d);
            return new SimulationExecutionContext(timestamp, new SimulationCalendar(1 + totalMinutes / 1440, totalMinutes / 60 % 24, totalMinutes % 60, SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
        }

        private void RunLogisticsPulse()
        {
            ResourceFlowManager flow = hiveState.ResourceFlowManager;
            double now = tickEngine.TotalSeconds;
            string flower = "flower-" + (cycleIndex % 6 + 1);
            flow.Produce(flower, "field-cache", ResourceType.Nectar, 8d, now);
            flow.Produce(flower, "field-cache", ResourceType.Pollen, 5d, now);
            flow.Transfer("field-cache", "colony-reserve", ResourceType.Nectar, 4d, now);
            flow.Transfer("field-cache", "colony-reserve", ResourceType.Pollen, 3d, now);
            ResourceReservation nectar = flow.Reserve("colony-reserve", ResourceType.Nectar, 2d);
            if (nectar.IsValid) flow.Consume(nectar, now);
            flow.Produce("honey-processing", "colony-reserve", ResourceType.Honey, 1.4d, now);

            if (deliveries.Count > 0)
            {
                DeliveryOrder order = deliveries[cycleIndex % deliveries.Count];
                if (order.State != DeliveryState.Validated)
                {
                    deliveryManager.CompleteDelivery(order.OrderId, Math.Max(1d, order.Request.Amount / 3d));
                }
            }

            CaptureHistory();
            cycleIndex++;
        }

        private void CaptureHistory()
        {
            foreach (ResourceTransaction transaction in hiveState.ResourceFlowManager.GetHistory())
            {
                recentTransactions.Enqueue(transaction);
                while (recentTransactions.Count > 12) recentTransactions.Dequeue();
            }
        }

        private void BuildScenePrimitives()
        {
            CreateNode("Ground", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(32f, 0.2f, 18f), new Color(0.16f, 0.23f, 0.2f));
            CreateNode("Flowers", PrimitiveType.Sphere, new Vector3(-10f, 0.45f, 0f), new Vector3(2.2f, 0.8f, 2.2f), new Color(0.86f, 0.35f, 0.56f));
            CreateNode("Field Cache", PrimitiveType.Cylinder, new Vector3(-5f, 0.45f, 0f), new Vector3(1.4f, 0.9f, 1.4f), new Color(0.38f, 0.72f, 0.44f));
            CreateNode("Hive Reserve", PrimitiveType.Sphere, new Vector3(0f, 1f, 0f), new Vector3(2.5f, 2f, 2.5f), new Color(0.94f, 0.68f, 0.22f));
            CreateNode("Nursery", PrimitiveType.Cube, new Vector3(4.5f, 0.55f, 2.8f), Vector3.one, new Color(0.94f, 0.5f, 0.68f));
            CreateNode("Honey Storage", PrimitiveType.Cylinder, new Vector3(5f, 0.55f, 0f), new Vector3(1.3f, 1.1f, 1.3f), new Color(0.98f, 0.76f, 0.22f));
            CreateNode("Pollen Storage", PrimitiveType.Cylinder, new Vector3(5f, 0.55f, -2.8f), new Vector3(1.3f, 1.1f, 1.3f), new Color(0.98f, 0.48f, 0.22f));
            CreateNode("Construction Sites", PrimitiveType.Cube, new Vector3(10f, 0.55f, 0f), new Vector3(2.5f, 1f, 4f), new Color(0.54f, 0.44f, 0.34f));
        }

        private GameObject CreateNode(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject node = GameObject.CreatePrimitive(type);
            node.name = objectName;
            node.transform.position = position;
            node.transform.localScale = scale;
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
            nodes[objectName] = node;
            return node;
        }

        private void HandleDebugKeys()
        {
            if (Input.GetKeyDown(KeyCode.F1)) showFlows = !showFlows;
            if (Input.GetKeyDown(KeyCode.F2)) showStocks = !showStocks;
            if (Input.GetKeyDown(KeyCode.F3)) showReservations = !showReservations;
            if (Input.GetKeyDown(KeyCode.F4)) showPaths = !showPaths;
            if (Input.GetKeyDown(KeyCode.F5)) showBottlenecks = !showBottlenecks;
            if (Input.GetKeyDown(KeyCode.F6)) showStatistics = !showStatistics;
            if (Input.GetKeyDown(KeyCode.F7)) showProducers = !showProducers;
            if (Input.GetKeyDown(KeyCode.F8)) showConsumers = !showConsumers;
        }

        private void MoveCamera()
        {
            if (sceneCamera == null) return;
            float speed = Input.GetKey(KeyCode.LeftShift) ? 15f : 7f;
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            sceneCamera.transform.position += sceneCamera.transform.TransformDirection(input) * speed * Time.deltaTime;
            if (Input.GetMouseButton(1))
            {
                sceneCamera.transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * 4f, Space.World);
                sceneCamera.transform.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * 4f, Space.Self);
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Math.Abs(scroll) > 0.01f) sceneCamera.transform.position += sceneCamera.transform.forward * scroll * 3f;
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
            if (hiveState == null) return;
            GUI.Box(new Rect(12, 12, 440, 610), "Resource & Logistics Flow");
            GUILayout.BeginArea(new Rect(24, 40, 416, 570));
            DrawOverlay();
            GUILayout.EndArea();
        }

        private void DrawOverlay()
        {
            ResourceFlowManager flow = hiveState.ResourceFlowManager;
            StorageStatistics inventory = hiveState.InventoryManager.QueryInventory();
            GUILayout.Label("Tick: " + tickEngine.TickIndex + " | FPS: " + fps.ToString("0") + " | TPS: " + (1d / FixedStepSeconds).ToString("0"));
            GUILayout.Label("F1 Flows " + Toggle(showFlows) + " F2 Stocks " + Toggle(showStocks) + " F3 Reservations " + Toggle(showReservations));
            GUILayout.Label("F4 Paths " + Toggle(showPaths) + " F5 Bottlenecks " + Toggle(showBottlenecks) + " F6 Stats " + Toggle(showStatistics));
            GUILayout.Label("F7 Producers " + Toggle(showProducers) + " F8 Consumers " + Toggle(showConsumers));

            if (showStocks)
            {
                GUILayout.Space(6);
                GUILayout.Label("Reserves: Nectar " + flow.QueryFlow("colony-reserve", ResourceType.Nectar).ToString("0.0") + " | Pollen " + flow.QueryFlow("colony-reserve", ResourceType.Pollen).ToString("0.0"));
                GUILayout.Label("Water " + flow.QueryFlow("colony-reserve", ResourceType.Water).ToString("0.0") + " | Wax " + flow.QueryFlow("colony-reserve", ResourceType.Wax).ToString("0.0") + " | Honey " + flow.QueryFlow("colony-reserve", ResourceType.Honey).ToString("0.0"));
                GUILayout.Label("Inventory cells: " + inventory.CellCount + " | amount " + inventory.TotalAmount.ToString("0.0") + "/" + inventory.TotalCapacity.ToString("0.0"));
            }

            if (showStatistics)
            {
                GUILayout.Space(6);
                GUILayout.Label("Flow transactions: " + flow.Diagnostics.TransactionCount + " | shortages " + flow.Diagnostics.ShortageCount + " | full storage " + flow.Diagnostics.StorageFullCount);
                GUILayout.Label("Delivery requests: " + deliveryManager.Diagnostics.Requested + " | reserved " + deliveryManager.Diagnostics.Reserved + " | started " + deliveryManager.Diagnostics.Started + " | completed " + deliveryManager.Diagnostics.Completed);
                GUILayout.Label("Transporters: framework reservations " + deliveryManager.Diagnostics.TransportAssigned + " | active routes: delivery orders " + deliveries.Count);
                GUILayout.Label("Average distance/time/congestion: not exposed by logistics framework");
                ColonyIntegrationDemoDiagnostics.DrawSceneItems("LogisticsDemo", 3);
            }

            if (showFlows)
            {
                GUILayout.Space(6);
                GUILayout.Label("Recent framework transactions:");
                foreach (ResourceTransaction tx in recentTransactions)
                {
                    GUILayout.Label(tx.Status + " " + tx.Amount.ToString("0.0") + " " + tx.ResourceType + " " + tx.OriginId + " -> " + tx.DestinationId);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showFlows || nodes.Count == 0) return;
            DrawFlow("Flowers", "Field Cache", Color.green);
            DrawFlow("Field Cache", "Hive Reserve", Color.cyan);
            DrawFlow("Hive Reserve", "Honey Storage", Color.yellow);
            DrawFlow("Hive Reserve", "Nursery", Color.magenta);
            DrawFlow("Hive Reserve", "Construction Sites", new Color(1f, 0.7f, 0.25f));
        }

        private void DrawFlow(string from, string to, Color color)
        {
            if (!nodes.TryGetValue(from, out GameObject a) || !nodes.TryGetValue(to, out GameObject b)) return;
            Gizmos.color = color;
            Gizmos.DrawLine(a.transform.position + Vector3.up, b.transform.position + Vector3.up);
        }

        private static string Toggle(bool value) => value ? "on" : "off";
    }
}
