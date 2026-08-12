using System;
using System.Collections.Generic;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using UnityEngine;
using CollectiveBehaviorDefinition = BeeKingdom.Population.CollectiveBehaviorDefinition;
using CollectiveBehaviorType = BeeKingdom.Population.CollectiveBehaviorType;
using CollectiveIntelligenceManager = BeeKingdom.Population.CollectiveIntelligenceManager;
using ColonyPriorityType = BeeKingdom.Population.ColonyPriorityType;
using ColonyStateContext = BeeKingdom.Population.ColonyStateContext;
using CommunicationChannel = BeeKingdom.Population.CommunicationChannel;
using CommunicationKind = BeeKingdom.Population.CommunicationKind;
using CommunicationSignal = BeeKingdom.Population.CommunicationSignal;
using CommunicationSignalType = BeeKingdom.Population.CommunicationSignalType;
using SwarmSignal = BeeKingdom.Population.SwarmSignal;
using SwarmSignalType = BeeKingdom.Population.SwarmSignalType;
using SwarmCommunicationManager = BeeKingdom.Population.SwarmCommunicationManager;

namespace BeeKingdom.Playground
{
    public sealed class CommunicationLabBootstrap : MonoBehaviour
    {
        private const double FixedStepSeconds = 0.1d;

        private readonly Dictionary<string, GameObject> anchors = new Dictionary<string, GameObject>();
        private readonly Queue<string> eventHistory = new Queue<string>();
        private readonly Dictionary<string, Vector3> signalOrigins = new Dictionary<string, Vector3>();

        private SimulationTickEngine tickEngine;
        private PlayableHiveState hiveState;
        private SwarmCommunicationManager communicationManager;
        private CollectiveIntelligenceManager collectiveManager;
        private Camera sceneCamera;
        private double nextScenarioPulse;
        private int scenarioIndex;
        private bool showPheromones = true;
        private bool showCommunications = true;
        private bool showPropagation = true;
        private bool showInfluences = true;
        private bool showDecisionChanges = true;
        private bool showRecruitments = true;
        private bool showStatistics = true;
        private bool showDiagnostics = true;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;

        public SwarmCommunicationManager CommunicationManager => communicationManager;
        public CollectiveIntelligenceManager CollectiveManager => collectiveManager;

        private void Awake()
        {
            sceneCamera = Camera.main;
            tickEngine = new SimulationTickEngine(FixedStepSeconds);
        }

        private void Start()
        {
            CreateFrameworkState();
            BuildScenePrimitives();
            RunScenarioPulse();
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
            StarterHiveProfile hive = StarterHiveProfile.CreateDefault();
            hiveState = new NewGameInitializer().CreateNewGame(hive, population, StarterResourceProfile.CreateDefault());
            ActivateQueen(hiveState);

            communicationManager = new SwarmCommunicationManager();
            communicationManager.RegisterCommunicationChannel(new CommunicationChannel("pheromone", CommunicationKind.Pheromone, 24d));
            communicationManager.RegisterCommunicationChannel(new CommunicationChannel("alarm", CommunicationKind.EmergencySignal, 8d));
            communicationManager.RegisterCommunicationChannel(new CommunicationChannel("recruitment", CommunicationKind.RecruitmentSignal, 12d));
            communicationManager.RegisterCommunicationChannel(new CommunicationChannel("queen", CommunicationKind.QueenSignal, 6d));

            collectiveManager = new CollectiveIntelligenceManager();
            collectiveManager.RegisterCollectiveBehavior(new CollectiveBehaviorDefinition("food-gathering", CollectiveBehaviorType.FoodGathering, ColonyPriorityType.Produce, 0.25d));
            collectiveManager.RegisterCollectiveBehavior(new CollectiveBehaviorDefinition("emergency-defense", CollectiveBehaviorType.EmergencyDefense, ColonyPriorityType.Defend, 0.4d));
            collectiveManager.RegisterCollectiveBehavior(new CollectiveBehaviorDefinition("colony-expansion", CollectiveBehaviorType.ColonyExpansion, ColonyPriorityType.Build, 0.35d));
        }

        private static void ActivateQueen(PlayableHiveState state)
        {
            state.QueenManager.UpdateState(state.QueenId, QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.ActiveQueen);
        }

        private void AdvanceSimulation()
        {
            int ticks = tickEngine.Advance(Time.deltaTime, SimulationTickMode.Fixed);
            for (int i = 0; i < ticks; i++)
            {
                SimulationExecutionContext context = CreateContext(FixedStepSeconds);
                hiveState.Controller.Execute(context);
                hiveState.AIManager.Execute(context);
                communicationManager.PropagateSignal(FixedStepSeconds);
                communicationManager.ExpireSignal();
                ReceiveVisibleSignals();
                if (tickEngine.TotalSeconds >= nextScenarioPulse)
                {
                    RunScenarioPulse();
                    nextScenarioPulse = tickEngine.TotalSeconds + 18d;
                }
            }
        }

        private SimulationExecutionContext CreateContext(double deltaSeconds)
        {
            SimulationTimestamp timestamp = new SimulationTimestamp(tickEngine.TickIndex, tickEngine.TotalSeconds);
            int totalMinutes = (int)(tickEngine.TotalSeconds / 60d);
            return new SimulationExecutionContext(timestamp, new SimulationCalendar(1 + totalMinutes / 1440, totalMinutes / 60 % 24, totalMinutes % 60, SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
        }

        private void RunScenarioPulse()
        {
            switch (scenarioIndex % 4)
            {
                case 0:
                    BroadcastCommunication("pheromone", CommunicationSignalType.FoodFound, "forager-" + scenarioIndex, 0.5d, 1d, 0.018d, 36d, 0.7d, "Flowers");
                    collectiveManager.BroadcastSignal(new SwarmSignal("food-" + scenarioIndex, SwarmSignalType.FoodPheromone, 0.85d, 8d, 0.05d, 0.6d, 0.5d));
                    collectiveManager.EvaluateColonyIntent(new ColonyStateContext(resourcePressure: 0.2d, playerGoalPressure: 0.4d));
                    AddHistory("Food discovery signal broadcast by framework.");
                    break;
                case 1:
                    BroadcastCommunication("alarm", CommunicationSignalType.DangerDetected, "guard-" + scenarioIndex, 0.25d, 1d, 0.025d, 28d, 1d, "Danger");
                    collectiveManager.BroadcastSignal(new SwarmSignal("alarm-" + scenarioIndex, SwarmSignalType.AlarmPheromone, 1d, 10d, 0.04d, 0.75d, 1d));
                    collectiveManager.EvaluateColonyIntent(new ColonyStateContext(threatPressure: 0.9d, healthPressure: 0.4d));
                    AddHistory("Danger signal broadcast by framework.");
                    break;
                case 2:
                    CreateConstructionStimulus();
                    BroadcastCommunication("recruitment", CommunicationSignalType.ConstructionNeeded, "builder-" + scenarioIndex, 0.2d, 0.9d, 0.016d, 42d, 0.65d, "Construction");
                    collectiveManager.BroadcastSignal(new SwarmSignal("build-" + scenarioIndex, SwarmSignalType.ConstructionSignal, 0.8d, 7d, 0.05d, 0.55d, 0.7d));
                    collectiveManager.EvaluateColonyIntent(new ColonyStateContext(populationPressure: 0.7d, playerGoalPressure: 0.8d));
                    AddHistory("Construction recruitment signal broadcast by framework.");
                    break;
                default:
                    BroadcastCommunication("queen", CommunicationSignalType.QueenNeedsHelp, hiveState.QueenId, 0.1d, 0.75d, 0.012d, 48d, 0.8d, "Queen");
                    collectiveManager.BroadcastSignal(new SwarmSignal("queen-" + scenarioIndex, SwarmSignalType.RoyalPheromone, 0.9d, 12d, 0.06d, 0.45d, 0.8d));
                    collectiveManager.EvaluateColonyIntent(new ColonyStateContext(populationPressure: 0.45d, resourcePressure: 0.35d));
                    AddHistory("Queen pheromone signal broadcast by framework.");
                    break;
            }

            scenarioIndex++;
        }

        private void BroadcastCommunication(string channel, CommunicationSignalType type, string origin, double radius, double intensity, double decay, double lifetime, double priority, string anchor)
        {
            CommunicationSignal signal = communicationManager.BroadcastSignal(channel, type, origin, radius, intensity, decay, lifetime, priority);
            if (signal != null && anchors.TryGetValue(anchor, out GameObject node))
            {
                signalOrigins[signal.SignalId] = node.transform.position;
            }
        }

        private void CreateConstructionStimulus()
        {
            double wax = hiveState.ResourceFlowManager.QueryFlow("colony-reserve", BeeKingdom.Economy.ResourceType.Wax);
            HiveExpansionPlan plan = hiveState.GrowthManager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.Utility, hiveState.BeeIds.Count, wax, 28d, true, new[] { "starter-beekeeping" }));
            if (plan.IsApproved) hiveState.GrowthManager.CreateChamber(plan, "chamber-1");
        }

        private void ReceiveVisibleSignals()
        {
            IReadOnlyList<CommunicationSignal> signals = communicationManager.QuerySignals();
            for (int i = 0; i < signals.Count; i++)
            {
                communicationManager.ReceiveSignal(signals[i].SignalId, 18d, 1d);
            }
        }

        private void BuildScenePrimitives()
        {
            CreateAnchor("Ground", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(32f, 0.2f, 18f), new Color(0.14f, 0.2f, 0.22f));
            CreateAnchor("Queen", PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), new Vector3(2.2f, 2.2f, 2.2f), new Color(0.72f, 0.32f, 0.86f));
            CreateAnchor("Flowers", PrimitiveType.Sphere, new Vector3(-9f, 0.45f, -3f), new Vector3(1.6f, 0.8f, 1.6f), new Color(0.88f, 0.36f, 0.58f));
            CreateAnchor("Danger", PrimitiveType.Cube, new Vector3(8f, 0.7f, -3f), new Vector3(1.4f, 1.4f, 1.4f), new Color(0.82f, 0.2f, 0.18f));
            CreateAnchor("Construction", PrimitiveType.Cube, new Vector3(8f, 0.55f, 3f), new Vector3(2.2f, 1f, 2.2f), new Color(0.66f, 0.48f, 0.28f));
            CreateAnchor("Workers", PrimitiveType.Cylinder, new Vector3(-3f, 0.5f, 3.5f), new Vector3(1.4f, 1f, 1.4f), new Color(0.95f, 0.76f, 0.22f));
            CreateAnchor("Guards", PrimitiveType.Cylinder, new Vector3(3f, 0.5f, 3.5f), new Vector3(1.4f, 1f, 1.4f), new Color(0.9f, 0.28f, 0.2f));
        }

        private GameObject CreateAnchor(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject node = GameObject.CreatePrimitive(type);
            node.name = objectName;
            node.transform.position = position;
            node.transform.localScale = scale;
            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
            anchors[objectName] = node;
            return node;
        }

        private void HandleDebugKeys()
        {
            if (Input.GetKeyDown(KeyCode.F1)) showPheromones = !showPheromones;
            if (Input.GetKeyDown(KeyCode.F2)) showCommunications = !showCommunications;
            if (Input.GetKeyDown(KeyCode.F3)) showPropagation = !showPropagation;
            if (Input.GetKeyDown(KeyCode.F4)) showInfluences = !showInfluences;
            if (Input.GetKeyDown(KeyCode.F5)) showDecisionChanges = !showDecisionChanges;
            if (Input.GetKeyDown(KeyCode.F6)) showRecruitments = !showRecruitments;
            if (Input.GetKeyDown(KeyCode.F7)) showStatistics = !showStatistics;
            if (Input.GetKeyDown(KeyCode.F8)) showDiagnostics = !showDiagnostics;
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

        private void AddHistory(string entry)
        {
            eventHistory.Enqueue("tick " + tickEngine.TickIndex + ": " + entry);
            while (eventHistory.Count > 12) eventHistory.Dequeue();
        }

        private void OnGUI()
        {
            if (communicationManager == null) return;
            GUI.Box(new Rect(12, 12, 450, 620), "Swarm Communication & Pheromone Lab");
            GUILayout.BeginArea(new Rect(24, 40, 426, 580));
            DrawOverlay();
            GUILayout.EndArea();
        }

        private void DrawOverlay()
        {
            IReadOnlyList<CommunicationSignal> signals = communicationManager.QuerySignals();
            double averageIntensity = 0d;
            double averageRadius = 0d;
            for (int i = 0; i < signals.Count; i++)
            {
                averageIntensity += signals[i].Intensity;
                averageRadius += signals[i].Radius;
            }

            if (signals.Count > 0)
            {
                averageIntensity /= signals.Count;
                averageRadius /= signals.Count;
            }

            GUILayout.Label("Tick: " + tickEngine.TickIndex + " | FPS: " + fps.ToString("0") + " | TPS: " + (1d / FixedStepSeconds).ToString("0"));
            GUILayout.Label("F1 Pheromones " + Toggle(showPheromones) + " F2 Comms " + Toggle(showCommunications) + " F3 Propagation " + Toggle(showPropagation));
            GUILayout.Label("F4 Influence " + Toggle(showInfluences) + " F5 Decisions " + Toggle(showDecisionChanges) + " F6 Recruitments " + Toggle(showRecruitments));
            GUILayout.Label("F7 Stats " + Toggle(showStatistics) + " F8 Diagnostics " + Toggle(showDiagnostics));

            if (showStatistics)
            {
                GUILayout.Space(6);
                GUILayout.Label("Signals active: " + signals.Count + " | broadcasts " + communicationManager.Diagnostics.Broadcast + " | received " + communicationManager.Diagnostics.Received + " | expired " + communicationManager.Diagnostics.Expired);
                GUILayout.Label("Pheromone zones: " + signals.Count + " | avg intensity " + averageIntensity.ToString("0.00") + " | avg radius " + averageRadius.ToString("0.0"));
                GUILayout.Label("Collective behavior: " + collectiveManager.QuerySwarmState().ActiveBehavior + " | cooperation " + collectiveManager.QueryCooperationScore().ToString("0.00"));
                GUILayout.Label("Priority changes: " + collectiveManager.Diagnostics.PriorityChanges + " | behavior activations " + collectiveManager.Diagnostics.BehaviorsActivated + " | emergencies " + collectiveManager.Diagnostics.EmergencyProtocols);
            }

            if (showCommunications)
            {
                GUILayout.Space(6);
                for (int i = 0; i < signals.Count && i < 8; i++)
                {
                    CommunicationSignal signal = signals[i];
                    GUILayout.Label(signal.SignalId + " " + signal.Type + " " + signal.ChannelId + " intensity " + signal.Intensity.ToString("0.00") + " radius " + signal.Radius.ToString("0.0"));
                }
            }

            if (showDiagnostics)
            {
                GUILayout.Space(6);
                foreach (string entry in eventHistory) GUILayout.Label(entry);
                GUILayout.Label("Bee reaction surface: task/AI reactions are not wired to communication managers.");
            }
        }

        private void OnDrawGizmos()
        {
            if (!showPheromones || communicationManager == null) return;
            IReadOnlyList<CommunicationSignal> signals = communicationManager.QuerySignals();
            for (int i = 0; i < signals.Count; i++)
            {
                CommunicationSignal signal = signals[i];
                Vector3 origin = signalOrigins.TryGetValue(signal.SignalId, out Vector3 stored) ? stored : Vector3.zero;
                Gizmos.color = ResolveSignalColor(signal);
                Gizmos.DrawWireSphere(origin + Vector3.up * 0.2f, (float)Math.Max(0.2d, signal.Radius));
                if (showPropagation) Gizmos.DrawLine(origin + Vector3.up, Vector3.zero + Vector3.up);
            }
        }

        private static Color ResolveSignalColor(CommunicationSignal signal)
        {
            switch (signal.Type)
            {
                case CommunicationSignalType.FoodFound: return new Color(0.2f, 0.9f, 0.3f, 1f);
                case CommunicationSignalType.DangerDetected: return new Color(1f, 0.18f, 0.12f, 1f);
                case CommunicationSignalType.ConstructionNeeded: return new Color(1f, 0.65f, 0.18f, 1f);
                case CommunicationSignalType.QueenNeedsHelp: return new Color(0.75f, 0.32f, 1f, 1f);
                default: return Color.white;
            }
        }

        private static string Toggle(bool value) => value ? "on" : "off";
    }
}
