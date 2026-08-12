using System;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using BeeKingdom.Population;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class CombatDefenseDemoBootstrap : MonoBehaviour
    {
        private const double FixedStepSeconds = 0.1d;

        private SimulationTickEngine tickEngine;
        private PlayableHiveState colony;
        private EmergencyResponseManager emergencyManager;
        private SwarmCommunicationManager communicationManager;
        private CollectiveIntelligenceManager collectiveManager;
        private Camera sceneCamera;
        private double nextThreatPulse;
        private EmergencyIncident activeIncident;
        private bool showEnemies = true;
        private bool showGuards = true;
        private bool showComms = true;
        private bool showAlertZones = true;
        private bool showPaths;
        private bool showCombats = true;
        private bool showStats = true;
        private bool showDiagnostics = true;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;

        public EmergencyResponseManager EmergencyManager => emergencyManager;

        private void Awake()
        {
            sceneCamera = Camera.main;
            tickEngine = new SimulationTickEngine(FixedStepSeconds);
        }

        private void Start()
        {
            CreateFrameworkState();
            BuildScenePrimitives();
            TriggerThreat(0.75d);
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
            colony = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), new StarterPopulationProfile(22, 4, 6, 8, 4, 100, 100, null), StarterResourceProfile.CreateDefault());
            ActivateQueen(colony);

            emergencyManager = new EmergencyResponseManager();
            emergencyManager.RegisterEmergencyType(new EmergencyPlan("predator-attack", EmergencyType.PredatorAttack, 0.35d));
            emergencyManager.RegisterEmergencyType(new EmergencyPlan("hive-breach", EmergencyType.HiveBreach, 0.45d));

            communicationManager = new SwarmCommunicationManager();
            communicationManager.RegisterCommunicationChannel(new CommunicationChannel("alarm", CommunicationKind.EmergencySignal, 12d));

            collectiveManager = new CollectiveIntelligenceManager();
            collectiveManager.RegisterCollectiveBehavior(new CollectiveBehaviorDefinition("emergency-defense", CollectiveBehaviorType.EmergencyDefense, ColonyPriorityType.Defend, 0.35d));
        }

        private static void ActivateQueen(PlayableHiveState state)
        {
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.ActiveQueen);
        }

        private void AdvanceSimulation()
        {
            int ticks = tickEngine.Advance(Time.deltaTime, SimulationTickMode.Fixed);
            for (int i = 0; i < ticks; i++)
            {
                SimulationExecutionContext context = CreateContext(FixedStepSeconds);
                colony.Controller.Execute(context);
                colony.AIManager.Execute(context);
                communicationManager.PropagateSignal(FixedStepSeconds);
                communicationManager.ExpireSignal();
                if (tickEngine.TotalSeconds >= nextThreatPulse)
                {
                    if (activeIncident != null && activeIncident.State != EmergencyState.Resolved) emergencyManager.ResolveEmergency(activeIncident.IncidentId);
                    TriggerThreat(0.55d + (nextThreatPulse % 3d) * 0.15d);
                    nextThreatPulse = tickEngine.TotalSeconds + 45d;
                }
            }
        }

        private SimulationExecutionContext CreateContext(double deltaSeconds)
        {
            SimulationTimestamp timestamp = new SimulationTimestamp(tickEngine.TickIndex, tickEngine.TotalSeconds);
            int totalMinutes = (int)(tickEngine.TotalSeconds / 60d);
            return new SimulationExecutionContext(timestamp, new SimulationCalendar(1 + totalMinutes / 1440, totalMinutes / 60 % 24, totalMinutes % 60, SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
        }

        private void TriggerThreat(double score)
        {
            activeIncident = emergencyManager.DetectEmergency("predator-attack", score);
            if (activeIncident == null) return;
            emergencyManager.ActivateEmergency(activeIncident.IncidentId);
            if (score > 0.8d) emergencyManager.EscalateEmergency(activeIncident.IncidentId, score);
            communicationManager.BroadcastSignal("alarm", CommunicationSignalType.DangerDetected, activeIncident.IncidentId, 0.5d, 1d, 0.02d, 40d, 1d);
            collectiveManager.BroadcastSignal(new SwarmSignal("alarm-" + tickEngine.TickIndex, SwarmSignalType.AlarmPheromone, 1d, 10d, 0.04d, 0.8d, 1d));
            collectiveManager.EvaluateColonyIntent(new ColonyStateContext(threatPressure: score));
        }

        private void BuildScenePrimitives()
        {
            CreatePrimitive("Hive", PrimitiveType.Sphere, Vector3.zero + Vector3.up, new Vector3(2.4f, 2.2f, 2.4f), new Color(0.94f, 0.68f, 0.18f));
            CreatePrimitive("Queen Chamber", PrimitiveType.Cube, new Vector3(0f, 0.5f, 2.8f), new Vector3(1.5f, 1f, 1.5f), new Color(0.72f, 0.32f, 0.86f));
            CreatePrimitive("Entrance", PrimitiveType.Cube, new Vector3(-4f, 0.4f, 0f), new Vector3(1.2f, 0.8f, 1.2f), new Color(0.45f, 0.35f, 0.25f));
            CreatePrimitive("Threat Marker", PrimitiveType.Cube, new Vector3(-8f, 0.7f, 0f), new Vector3(1.4f, 1.4f, 1.4f), new Color(0.85f, 0.18f, 0.16f));
            CreatePrimitive("Guard Rally", PrimitiveType.Cylinder, new Vector3(-2.5f, 0.5f, -2.8f), new Vector3(1.4f, 1f, 1.4f), new Color(0.9f, 0.28f, 0.18f));
        }

        private static void CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
        }

        private void HandleDebugKeys()
        {
            if (Input.GetKeyDown(KeyCode.F1)) showEnemies = !showEnemies;
            if (Input.GetKeyDown(KeyCode.F2)) showGuards = !showGuards;
            if (Input.GetKeyDown(KeyCode.F3)) showComms = !showComms;
            if (Input.GetKeyDown(KeyCode.F4)) showAlertZones = !showAlertZones;
            if (Input.GetKeyDown(KeyCode.F5)) showPaths = !showPaths;
            if (Input.GetKeyDown(KeyCode.F6)) showCombats = !showCombats;
            if (Input.GetKeyDown(KeyCode.F7)) showStats = !showStats;
            if (Input.GetKeyDown(KeyCode.F8)) showDiagnostics = !showDiagnostics;
            if (Input.GetKeyDown(KeyCode.T)) TriggerThreat(0.9d);
        }

        private void MoveCamera()
        {
            if (sceneCamera == null) return;
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            sceneCamera.transform.position += sceneCamera.transform.TransformDirection(input) * 8f * Time.deltaTime;
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
            if (emergencyManager == null) return;
            GUI.Box(new Rect(12, 12, 430, 520), "Combat & Defense Simulation");
            GUILayout.BeginArea(new Rect(24, 40, 406, 480));
            GUILayout.Label("Tick: " + tickEngine.TickIndex + " | FPS " + fps.ToString("0") + " | TPS " + (1d / FixedStepSeconds).ToString("0"));
            GUILayout.Label("F1 Enemies " + Toggle(showEnemies) + " F2 Guards " + Toggle(showGuards) + " F3 Comms " + Toggle(showComms));
            GUILayout.Label("F4 Alert " + Toggle(showAlertZones) + " F5 Paths " + Toggle(showPaths) + " F6 Combat " + Toggle(showCombats));
            GUILayout.Label("Emergencies: detected " + emergencyManager.Diagnostics.Detected + " | activated " + emergencyManager.Diagnostics.Activated + " | resolved " + emergencyManager.Diagnostics.Resolved);
            if (activeIncident != null) GUILayout.Label("Active incident: " + activeIncident.Type + " | " + activeIncident.Severity + " | " + activeIncident.State + " | score " + activeIncident.Score.ToString("0.00"));
            GUILayout.Label("Alarm signals: active " + communicationManager.QuerySignals().Count + " | received " + communicationManager.Diagnostics.Received + " | expired " + communicationManager.Diagnostics.Expired);
            GUILayout.Label("Collective behavior: " + collectiveManager.QuerySwarmState().ActiveBehavior + " | guard AI active " + colony.AIManager.GetStatistics().ActiveCount);
            GUILayout.Label("Physical combat engine / enemy health / casualties: not available.");
            if (showDiagnostics) ColonyIntegrationDemoDiagnostics.DrawSceneItems("CombatDefenseDemo", 3);
            GUILayout.Label("Press T to trigger a framework emergency.");
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            if (!showAlertZones) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(-8f, 0.7f, 0f), 5f);
            Gizmos.DrawLine(new Vector3(-8f, 0.7f, 0f), Vector3.zero + Vector3.up);
        }

        private static string Toggle(bool value) => value ? "on" : "off";
    }
}
