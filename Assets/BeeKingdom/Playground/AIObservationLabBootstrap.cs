using System;
using System.Collections.Generic;
using BeeKingdom.AI;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using BeeKingdom.Economy;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class AIObservationLabBootstrap : MonoBehaviour
    {
        private const double FixedStepSeconds = 0.1d;

        private readonly Dictionary<string, BeeVisual> beeVisuals = new Dictionary<string, BeeVisual>();
        private readonly Queue<string> decisionHistory = new Queue<string>();
        private SimulationTickEngine tickEngine;
        private PlayableHiveState hiveState;
        private Camera sceneCamera;
        private string selectedBeeId;
        private bool followSelected;
        private bool manualStepMode;
        private int queuedManualTicks;
        private bool showDecisions = true;
        private bool showBehaviorTrees;
        private bool showReservations = true;
        private bool showPaths;
        private bool showCommunications;
        private bool showTeams;
        private bool showScores;
        private bool showDiagnostics = true;
        private long lastAssignments;
        private long decisionChanges;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;

        private void Awake()
        {
            sceneCamera = Camera.main;
            tickEngine = new SimulationTickEngine(FixedStepSeconds);
        }

        private void Start()
        {
            CreateLabState();
            BuildScenePrimitives();
            RefreshBeeVisuals();
        }

        private void Update()
        {
            UpdateFps();
            HandleDebugKeys();
            HandleSelection();
            AdvanceSimulation();
            RefreshBeeVisuals();
            MoveCamera();
        }

        private void CreateLabState()
        {
            StarterPopulationProfile population = new StarterPopulationProfile(20, 5, 8, 6, 5, 100, 100, null);
            StarterHiveProfile hive = new StarterHiveProfile(
                "ai-lab-hive",
                "player",
                "queen-1",
                new HiveCapacity(512, 128, 128),
                1,
                1f,
                1d,
                new[] { HiveChamberType.Entrance, HiveChamberType.RoyalChamber, HiveChamberType.Nursery, HiveChamberType.HoneyStorage, HiveChamberType.PollenStorage },
                new[] { "starter-beekeeping" });
            StarterResourceProfile resources = new StarterResourceProfile(
                new Dictionary<ResourceType, double>
                {
                    { ResourceType.Nectar, 140d },
                    { ResourceType.Pollen, 120d },
                    { ResourceType.Water, 80d },
                    { ResourceType.Wax, 220d },
                    { ResourceType.Honey, 90d }
                },
                250d);

            hiveState = new NewGameInitializer().CreateNewGame(hive, population, resources);
            ActivateQueen(hiveState);
            RegisterBehaviors(hiveState.AIManager);
            CreateObservationWorkload();
        }

        private static void ActivateQueen(PlayableHiveState state)
        {
            state.QueenManager.UpdateState(state.QueenId, QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.ActiveQueen);
        }

        private static void RegisterBehaviors(BeeAIManager ai)
        {
            ai.RegisterBehavior(new BehaviorDefinition("lab-build", BeeKingdom.Population.BeeIntent.Build, BehaviorActionType.Build, 8d));
            ai.RegisterBehavior(new BehaviorDefinition("lab-gather", BeeKingdom.Population.BeeIntent.Gather, BehaviorActionType.Gather, 6d));
            ai.RegisterBehavior(new BehaviorDefinition("lab-defend", BeeKingdom.Population.BeeIntent.Defend, BehaviorActionType.Defend, 10d));
            ai.RegisterBehavior(new BehaviorDefinition("lab-nurse", BeeKingdom.Population.BeeIntent.Nurse, BehaviorActionType.Feed, 6d));
        }

        private void CreateObservationWorkload()
        {
            double wax = hiveState.ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Wax);
            HiveChamberType[] types = { HiveChamberType.Nursery, HiveChamberType.HoneyStorage, HiveChamberType.PollenStorage, HiveChamberType.WaxWorkshop, HiveChamberType.Defense };
            for (int i = 0; i < types.Length; i++)
            {
                HiveExpansionPlan plan = hiveState.GrowthManager.PlanExpansion(new HiveExpansionRequest(types[i], hiveState.BeeIds.Count, wax, 28d, true, new[] { "starter-beekeeping" }));
                if (plan.IsApproved) hiveState.GrowthManager.CreateChamber(plan, "chamber-1");
            }
        }

        private void AdvanceSimulation()
        {
            int ticks = manualStepMode ? queuedManualTicks : tickEngine.Advance(Time.deltaTime, SimulationTickMode.Fixed);
            queuedManualTicks = 0;
            for (int i = 0; i < ticks; i++)
            {
                SimulationExecutionContext context = CreateContext(FixedStepSeconds);
                hiveState.Controller.Execute(context);
                hiveState.AIManager.Execute(context);
                CaptureAIHistory();
            }
        }

        private SimulationExecutionContext CreateContext(double deltaSeconds)
        {
            SimulationTimestamp timestamp = new SimulationTimestamp(tickEngine.TickIndex, tickEngine.TotalSeconds);
            int totalMinutes = (int)(tickEngine.TotalSeconds / 60d);
            return new SimulationExecutionContext(timestamp, new SimulationCalendar(1 + totalMinutes / 1440, totalMinutes / 60 % 24, totalMinutes % 60, SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
        }

        private void CaptureAIHistory()
        {
            int assignments = hiveState.TaskManager.Diagnostics.AssignmentCount;
            if (assignments != lastAssignments)
            {
                decisionChanges += assignments - lastAssignments;
                EnqueueHistory("tick " + tickEngine.TickIndex + ": task assignments=" + assignments);
                lastAssignments = assignments;
            }
        }

        private void EnqueueHistory(string entry)
        {
            decisionHistory.Enqueue(entry);
            while (decisionHistory.Count > 20) decisionHistory.Dequeue();
        }

        private void BuildScenePrimitives()
        {
            CreatePrimitive("Ground", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 18f), new Color(0.16f, 0.22f, 0.25f));
            CreatePrimitive("Hive Lab", PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 2.2f, 2.4f), new Color(0.9f, 0.66f, 0.2f));
            CreatePrimitive("Task Board", PrimitiveType.Cube, new Vector3(-5f, 1f, 3f), new Vector3(2f, 2f, 0.2f), new Color(0.35f, 0.45f, 0.55f));
            CreatePrimitive("Reservation Board", PrimitiveType.Cube, new Vector3(5f, 1f, 3f), new Vector3(2f, 2f, 0.2f), new Color(0.45f, 0.38f, 0.58f));
            for (int i = 0; i < 12; i++)
            {
                CreatePrimitive("Lab Flower " + (i + 1), PrimitiveType.Sphere, new Vector3(-10f + (i % 6) * 4f, 0.3f, -5f + (i / 6) * 10f), new Vector3(0.45f, 0.45f, 0.45f), new Color(0.86f, 0.36f, 0.55f));
            }
        }

        private static GameObject CreatePrimitive(string objectName, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = objectName;
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
            return primitive;
        }

        private void RefreshBeeVisuals()
        {
            IReadOnlyList<string> bees = hiveState.BeeIds;
            for (int i = 0; i < bees.Count; i++)
            {
                string beeId = bees[i];
                BeeLifecycleBee record = hiveState.LifecycleManager.GetBee(beeId);
                if (!record.Alive) continue;
                if (!beeVisuals.TryGetValue(beeId, out BeeVisual visual))
                {
                    GameObject bee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bee.name = "AI Bee " + beeId;
                    bee.transform.localScale = beeId == hiveState.QueenId ? new Vector3(0.48f, 0.48f, 0.48f) : new Vector3(0.22f, 0.22f, 0.22f);
                    visual = bee.AddComponent<BeeVisual>();
                    visual.BeeId = beeId;
                    beeVisuals.Add(beeId, visual);
                }

                Vector3 center = ResolveCenter(record.CurrentRole);
                float angle = i * 2.399963f + (float)tickEngine.TotalSeconds * ResolveSpeed(beeId);
                visual.transform.position = center + new Vector3(Mathf.Cos(angle) * 1.5f, 0.75f + i % 4 * 0.08f, Mathf.Sin(angle) * 1.5f);
                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = beeId == selectedBeeId ? Color.white : StateColor(beeId, record.CurrentRole);
            }
        }

        private Vector3 ResolveCenter(BeeLifecycleRole role)
        {
            switch (role)
            {
                case BeeLifecycleRole.Nurse: return new Vector3(-4f, 0f, 0f);
                case BeeLifecycleRole.Builder: return new Vector3(0f, 0f, -3.5f);
                case BeeLifecycleRole.Scout: return new Vector3(5f, 0f, -2f);
                case BeeLifecycleRole.Soldier: return new Vector3(5f, 0f, 2f);
                default: return Vector3.zero;
            }
        }

        private float ResolveSpeed(string beeId)
        {
            if (beeId == hiveState.QueenId) return 0.05f;
            BeeBehaviorState state = hiveState.AIManager.GetCurrentState(beeId);
            return state == BeeBehaviorState.Idle ? 0.12f : 0.55f;
        }

        private Color StateColor(string beeId, BeeLifecycleRole role)
        {
            if (beeId == hiveState.QueenId) return new Color(0.74f, 0.28f, 0.88f);
            BeeBehaviorState state = hiveState.AIManager.GetCurrentState(beeId);
            if (state == BeeBehaviorState.Building) return new Color(0.9f, 0.55f, 0.2f);
            if (state == BeeBehaviorState.Guarding) return new Color(0.85f, 0.23f, 0.22f);
            if (state == BeeBehaviorState.Harvesting) return new Color(0.28f, 0.68f, 0.95f);
            if (role == BeeLifecycleRole.Nurse) return new Color(0.95f, 0.47f, 0.62f);
            return new Color(1f, 0.78f, 0.16f);
        }

        private void HandleDebugKeys()
        {
            if (Input.GetKeyDown(KeyCode.Space)) manualStepMode = !manualStepMode;
            if (Input.GetKeyDown(KeyCode.N)) queuedManualTicks += 1;
            if (Input.GetKeyDown(KeyCode.M)) queuedManualTicks += 10;
            if (Input.GetKeyDown(KeyCode.Comma)) queuedManualTicks += 100;
            if (Input.GetKeyDown(KeyCode.Return)) manualStepMode = false;
            if (Input.GetKeyDown(KeyCode.F1)) showDecisions = !showDecisions;
            if (Input.GetKeyDown(KeyCode.F2)) showBehaviorTrees = !showBehaviorTrees;
            if (Input.GetKeyDown(KeyCode.F3)) showReservations = !showReservations;
            if (Input.GetKeyDown(KeyCode.F4)) showPaths = !showPaths;
            if (Input.GetKeyDown(KeyCode.F5)) showCommunications = !showCommunications;
            if (Input.GetKeyDown(KeyCode.F6)) showTeams = !showTeams;
            if (Input.GetKeyDown(KeyCode.F7)) showScores = !showScores;
            if (Input.GetKeyDown(KeyCode.F8)) showDiagnostics = !showDiagnostics;
            if (Input.GetKeyDown(KeyCode.F)) followSelected = !followSelected;
        }

        private void HandleSelection()
        {
            if (!Input.GetMouseButtonDown(0) || sceneCamera == null) return;
            if (Physics.Raycast(sceneCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            {
                BeeVisual visual = hit.collider.GetComponent<BeeVisual>();
                selectedBeeId = visual != null ? visual.BeeId : null;
            }
        }

        private void MoveCamera()
        {
            if (sceneCamera == null) return;
            Transform cameraTransform = sceneCamera.transform;
            if (followSelected && !string.IsNullOrEmpty(selectedBeeId) && beeVisuals.TryGetValue(selectedBeeId, out BeeVisual visual))
            {
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, visual.transform.position + new Vector3(0f, 5f, -7f), Time.deltaTime * 4f);
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
            if (Math.Abs(scroll) > 0.01f) cameraTransform.position += cameraTransform.forward * scroll * 3f;
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
            GUI.Box(new Rect(12, 12, 430, 650), "AI Observation Lab");
            GUILayout.BeginArea(new Rect(24, 40, 406, 610));
            DrawDashboard();
            DrawSelectedBee();
            GUILayout.EndArea();
        }

        private void DrawDashboard()
        {
            TaskStatistics tasks = hiveState.TaskManager.GetStatistics();
            BeeAIStatistics ai = hiveState.AIManager.GetStatistics();
            GUILayout.Label("Tick: " + tickEngine.TickIndex + " | TPS: " + (1d / FixedStepSeconds).ToString("0.0") + " | Mode: " + (manualStepMode ? "step" : "live"));
            GUILayout.Label("Decisions/sec: task assignments " + hiveState.TaskManager.Diagnostics.AssignmentCount + " | Changes: " + decisionChanges);
            GUILayout.Label("Interruptions: " + hiveState.AIManager.Diagnostics.InterruptedCount + " | Resumes: framework events not counted");
            GUILayout.Label("Tasks active: " + tasks.AssignedTasks + " | waiting: " + tasks.QueuedTasks + " | reservations: " + hiveState.TaskManager.Diagnostics.ReservationCount);
            GUILayout.Label("AI brains: " + ai.BrainCount + " | active: " + ai.ActiveCount + " | waiting: " + ai.WaitingCount);
            GUILayout.Label("Behavior Trees active: not wired | Communications: not wired");
            GUILayout.Label("Performance: FPS " + fps.ToString("0") + " | sim CPU from framework: not exposed | AI updates " + hiveState.AIManager.Diagnostics.Updates);
            GUILayout.Label("Step controls: Space step/live, N +1, M +10, , +100, Enter live");
            GUILayout.Label("F1 Decisions " + Toggle(showDecisions) + " F2 BT " + Toggle(showBehaviorTrees) + " F3 Reservations " + Toggle(showReservations));
            GUILayout.Label("F4 Paths " + Toggle(showPaths) + " F5 Comms " + Toggle(showCommunications) + " F6 Teams " + Toggle(showTeams));
            GUILayout.Label("F7 Scores " + Toggle(showScores) + " F8 Diagnostics " + Toggle(showDiagnostics));

            if (showDecisions)
            {
                foreach (string entry in decisionHistory) GUILayout.Label(entry);
            }

            if (showDiagnostics)
            {
                ColonyIntegrationDemoDiagnostics.DrawSceneItems("AIObservationLab", 3);
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
            GUILayout.Label("Selected: " + selectedBeeId + " | Caste: " + bee.CurrentRole + " | Age: " + bee.Age.AgeSeconds.ToString("0.0"));
            GUILayout.Label("Health: " + bee.Health + " | Fatigue/needs/personality: not wired");
            if (selectedBeeId != hiveState.QueenId)
            {
                BeeBehaviorState state = hiveState.AIManager.GetCurrentState(selectedBeeId);
                BehaviorContext behavior = hiveState.AIManager.QueryBehavior(selectedBeeId);
                GUILayout.Label("Decision/intention: task-derived " + state + " | Score: not exposed");
                GUILayout.Label("Behavior: " + (behavior == null ? "no behavior context" : behavior.BehaviorId + " " + behavior.State));
            }
            GUILayout.Label("Task reservation: see TaskManager diagnostics | History: lab assignment history");
        }

        private static string Toggle(bool value) => value ? "on" : "off";
    }
}
