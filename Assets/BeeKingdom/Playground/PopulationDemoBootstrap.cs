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
    public sealed class PopulationDemoBootstrap : MonoBehaviour
    {
        private const double FixedStepSeconds = 0.1d;

        private readonly Dictionary<string, BeeVisual> beeVisuals = new Dictionary<string, BeeVisual>();
        private readonly List<string> unavailableFeatures = new List<string>();
        private SimulationTickEngine tickEngine;
        private PlayableHiveState hiveState;
        private Camera sceneCamera;
        private string selectedBeeId;
        private bool followSelected;
        private bool showCastes = true;
        private bool showNeeds;
        private bool showAges = true;
        private bool showGenerations;
        private bool showTasks = true;
        private bool showDecisions;
        private bool showStatistics = true;
        private bool showDiagnostics = true;
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
            CreatePopulationState();
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

        private void CreatePopulationState()
        {
            BeeLifecycleRules fastLifecycle = new BeeLifecycleRules(
                new BeeDevelopmentProfile(30d, 60d, 90d, 120d, 180d),
                new BeeMortalityProfile(360d),
                8f);

            StarterPopulationProfile population = new StarterPopulationProfile(12, 4, 3, 3, 2, 100, 100, fastLifecycle);
            StarterHiveProfile hive = new StarterHiveProfile(
                "population-demo-hive",
                "player",
                "queen-1",
                new HiveCapacity(512, 128, 128),
                1,
                3f,
                1d,
                new[] { HiveChamberType.Entrance, HiveChamberType.RoyalChamber, HiveChamberType.Nursery, HiveChamberType.HoneyStorage, HiveChamberType.PollenStorage },
                new[] { "starter-beekeeping" });

            hiveState = new NewGameInitializer().CreateNewGame(hive, population, StarterResourceProfile.CreateDefault());
            ActivateQueen(hiveState);
            hiveState.AIManager.RegisterBehavior(new BehaviorDefinition("population-idle", BeeKingdom.Population.BeeIntent.Idle, BehaviorActionType.Rest, 3d));
            hiveState.AIManager.RegisterBehavior(new BehaviorDefinition("population-feed", BeeKingdom.Population.BeeIntent.Nurse, BehaviorActionType.Feed, 6d));

            unavailableFeatures.Add("Genetics, needs, fatigue, experience, memory, personality, decision and communication frameworks exist under BeeKingdom.Population but are not yet wired into PlayableHiveState.");
            unavailableFeatures.Add("Generation and parent identifiers are not exposed by the active Hive lifecycle records.");
            unavailableFeatures.Add("Destination and recent history are not exposed by the current playable population state.");
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
                SimulationExecutionContext context = CreateContext(FixedStepSeconds * tickEngine.TimeScale);
                hiveState.Controller.Execute(context);
                hiveState.AIManager.Execute(context);
            }
        }

        private SimulationExecutionContext CreateContext(double deltaSeconds)
        {
            SimulationTimestamp timestamp = new SimulationTimestamp(tickEngine.TickIndex, tickEngine.TotalSeconds);
            int totalMinutes = (int)(tickEngine.TotalSeconds / 60d);
            return new SimulationExecutionContext(
                timestamp,
                new SimulationCalendar(1 + totalMinutes / 1440, totalMinutes / 60 % 24, totalMinutes % 60, SimulationSeason.Spring),
                SimulationTickFrequency.TenHz,
                deltaSeconds,
                null);
        }

        private void BuildScenePrimitives()
        {
            CreatePrimitive("Ground", PrimitiveType.Cube, new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 18f), new Color(0.18f, 0.27f, 0.22f));
            CreatePrimitive("Hive", PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 2.2f, 2.4f), new Color(0.93f, 0.66f, 0.2f));
            CreatePrimitive("Nursery", PrimitiveType.Cube, new Vector3(-3f, 0.45f, 2f), Vector3.one, new Color(0.96f, 0.55f, 0.72f));
            CreatePrimitive("Brood Chamber", PrimitiveType.Cube, new Vector3(0f, 0.45f, 3.2f), Vector3.one, new Color(0.7f, 0.54f, 0.9f));
            CreatePrimitive("Reserve", PrimitiveType.Cube, new Vector3(3f, 0.45f, 2f), Vector3.one, new Color(0.45f, 0.7f, 0.95f));
            CreatePrimitive("Expansion Space", PrimitiveType.Cylinder, new Vector3(5.5f, 0.25f, -2.5f), new Vector3(1.5f, 0.5f, 1.5f), new Color(0.5f, 0.62f, 0.45f));

            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * 2f / 10f;
                CreatePrimitive("Flower " + (i + 1), PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * 9f, 0.3f, Mathf.Sin(angle) * 5f), new Vector3(0.45f, 0.45f, 0.45f), new Color(0.92f, 0.35f, 0.46f));
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
                if (!beeVisuals.TryGetValue(beeId, out BeeVisual visual))
                {
                    GameObject bee = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bee.name = "Population Bee " + beeId;
                    bee.transform.localScale = beeId == hiveState.QueenId ? new Vector3(0.5f, 0.5f, 0.5f) : new Vector3(0.23f, 0.23f, 0.23f);
                    visual = bee.AddComponent<BeeVisual>();
                    visual.BeeId = beeId;
                    beeVisuals.Add(beeId, visual);
                }

                visual.gameObject.SetActive(record.Alive);
                if (!record.Alive) continue;
                visual.transform.position = ResolvePosition(record, i);
                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = beeId == selectedBeeId ? Color.white : StageColor(record.CurrentStage, record.CurrentRole);
            }
        }

        private Vector3 ResolvePosition(BeeLifecycleBee bee, int index)
        {
            Vector3 center = bee.CurrentStage == BeeLifecycleStage.Egg || bee.CurrentStage == BeeLifecycleStage.Larva || bee.CurrentStage == BeeLifecycleStage.Pupa
                ? new Vector3(-2.5f, 0f, 2.4f)
                : RoleCenter(bee.CurrentRole);
            float angle = index * 2.399963f + (float)tickEngine.TotalSeconds * 0.35f;
            float radius = 0.8f + index % 8 * 0.16f;
            return center + new Vector3(Mathf.Cos(angle) * radius, 0.75f + index % 3 * 0.08f, Mathf.Sin(angle) * radius);
        }

        private static Vector3 RoleCenter(BeeLifecycleRole role)
        {
            switch (role)
            {
                case BeeLifecycleRole.Queen: return Vector3.zero;
                case BeeLifecycleRole.Nurse: return new Vector3(-3f, 0f, 2f);
                case BeeLifecycleRole.Builder: return new Vector3(5.5f, 0f, -2.5f);
                case BeeLifecycleRole.Scout: return new Vector3(6f, 0f, 3f);
                case BeeLifecycleRole.Soldier: return new Vector3(-5f, 0f, -3f);
                default: return Vector3.zero;
            }
        }

        private static Color StageColor(BeeLifecycleStage stage, BeeLifecycleRole role)
        {
            if (stage == BeeLifecycleStage.Egg) return new Color(0.95f, 0.95f, 0.82f);
            if (stage == BeeLifecycleStage.Larva) return new Color(0.86f, 0.78f, 0.95f);
            if (stage == BeeLifecycleStage.Pupa) return new Color(0.62f, 0.58f, 0.9f);
            if (role == BeeLifecycleRole.Queen) return new Color(0.74f, 0.28f, 0.88f);
            if (role == BeeLifecycleRole.Nurse) return new Color(0.95f, 0.47f, 0.62f);
            if (role == BeeLifecycleRole.Builder) return new Color(0.86f, 0.58f, 0.25f);
            if (role == BeeLifecycleRole.Soldier) return new Color(0.85f, 0.23f, 0.22f);
            return new Color(1f, 0.78f, 0.16f);
        }

        private void HandleDebugKeys()
        {
            if (Input.GetKeyDown(KeyCode.Space)) tickEngine.SetPaused(!tickEngine.IsPaused);
            if (Input.GetKeyDown(KeyCode.Alpha1)) tickEngine.SetTimeScale(1d);
            if (Input.GetKeyDown(KeyCode.Alpha2)) tickEngine.SetTimeScale(2d);
            if (Input.GetKeyDown(KeyCode.Alpha3)) tickEngine.SetTimeScale(5d);
            if (Input.GetKeyDown(KeyCode.Alpha4)) tickEngine.SetTimeScale(10d);
            if (Input.GetKeyDown(KeyCode.Alpha5)) tickEngine.SetTimeScale(50d);
            if (Input.GetKeyDown(KeyCode.Alpha6)) tickEngine.SetTimeScale(100d);
            if (Input.GetKeyDown(KeyCode.F1)) showCastes = !showCastes;
            if (Input.GetKeyDown(KeyCode.F2)) showNeeds = !showNeeds;
            if (Input.GetKeyDown(KeyCode.F3)) showAges = !showAges;
            if (Input.GetKeyDown(KeyCode.F4)) showGenerations = !showGenerations;
            if (Input.GetKeyDown(KeyCode.F5)) showTasks = !showTasks;
            if (Input.GetKeyDown(KeyCode.F6)) showDecisions = !showDecisions;
            if (Input.GetKeyDown(KeyCode.F7)) showStatistics = !showStatistics;
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
            GUI.Box(new Rect(12, 12, 390, 560), "Population Demo");
            GUILayout.BeginArea(new Rect(24, 40, 366, 520));
            DrawStats();
            DrawSelectedBee();
            GUILayout.EndArea();
        }

        private void DrawStats()
        {
            CountStages(out int eggs, out int larvae, out int pupae, out int alive);
            GUILayout.Label("Tick: " + tickEngine.TickIndex + " | TPS: " + (1d / FixedStepSeconds).ToString("0.0") + " | Scale: x" + tickEngine.TimeScale.ToString("0"));
            GUILayout.Label("Paused: " + tickEngine.IsPaused + " | Sim time: " + hiveState.Diagnostics.SimulatedSeconds.ToString("0.0") + "s");
            GUILayout.Label("Population: " + hiveState.LifecycleManager.Diagnostics.BeeCount + " total, " + alive + " active, " + hiveState.LifecycleManager.Diagnostics.DeadCount + " dead");
            GUILayout.Label("Births/new records: " + hiveState.LifecycleManager.Diagnostics.BeeCount + " | Natural deaths: " + hiveState.LifecycleManager.Diagnostics.DeadCount);
            GUILayout.Label("Brood: eggs " + eggs + " | larvae " + larvae + " | pupae " + pupae);
            GUILayout.Label("Queen: " + hiveState.HiveId + "/" + hiveState.QueenId + " | Eggs produced: " + hiveState.QueenManager.Diagnostics.EggsProduced + " | Valid: " + hiveState.QueenManager.Diagnostics.LastValidationPassed);
            GUILayout.Label("Performance: " + fps.ToString("0") + " FPS | " + (GC.GetTotalMemory(false) / 1048576f).ToString("0.0") + " MB");
            GUILayout.Label("Controls: Space pause, 1/2/3/4/5/6 => x1/x2/x5/x10/x50/x100");
            GUILayout.Label("F1 Castes " + Toggle(showCastes) + " F2 Needs " + Toggle(showNeeds) + " F3 Ages " + Toggle(showAges));
            GUILayout.Label("F4 Generations " + Toggle(showGenerations) + " F5 Tasks " + Toggle(showTasks) + " F6 Decisions " + Toggle(showDecisions));
            GUILayout.Label("F7 Statistics " + Toggle(showStatistics) + " F8 Diagnostics " + Toggle(showDiagnostics));
            if (showCastes) DrawRoleCounts();
            if (showDiagnostics)
            {
                GUILayout.Label("Unavailable framework links documented: " + unavailableFeatures.Count);
                ColonyIntegrationDemoDiagnostics.DrawSceneItems("PopulationDemo", 3);
            }
        }

        private void DrawRoleCounts()
        {
            Dictionary<BeeLifecycleRole, int> counts = new Dictionary<BeeLifecycleRole, int>();
            foreach (string beeId in hiveState.BeeIds)
            {
                BeeLifecycleBee bee = hiveState.LifecycleManager.GetBee(beeId);
                if (!bee.Alive) continue;
                counts.TryGetValue(bee.CurrentRole, out int count);
                counts[bee.CurrentRole] = count + 1;
            }
            foreach (var pair in counts) GUILayout.Label(pair.Key + ": " + pair.Value);
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
            GUILayout.Label("Generation: not exposed | Parents: not exposed");
            GUILayout.Label("Age: " + bee.Age.AgeSeconds.ToString("0.0") + "s | Bio age: " + bee.Age.BiologicalAgeSeconds.ToString("0.0") + "s");
            GUILayout.Label("Life expectancy: " + "360 biological seconds (demo profile)");
            GUILayout.Label("Caste/role: " + bee.CurrentRole + " | Stage: " + bee.CurrentStage + " | Alive: " + bee.Alive);
            GUILayout.Label("Health: " + bee.Health + " | Fatigue: not wired | Experience: " + bee.Experience);
            if (selectedBeeId != hiveState.QueenId) GUILayout.Label("Task/decision: " + hiveState.AIManager.GetCurrentState(selectedBeeId));
            GUILayout.Label("Destination/history/personality: not exposed");
        }

        private void CountStages(out int eggs, out int larvae, out int pupae, out int alive)
        {
            eggs = 0; larvae = 0; pupae = 0; alive = 0;
            foreach (string beeId in hiveState.BeeIds)
            {
                BeeLifecycleBee bee = hiveState.LifecycleManager.GetBee(beeId);
                if (bee.Alive) alive++;
                if (bee.CurrentStage == BeeLifecycleStage.Egg) eggs++;
                else if (bee.CurrentStage == BeeLifecycleStage.Larva) larvae++;
                else if (bee.CurrentStage == BeeLifecycleStage.Pupa) pupae++;
            }
        }

        private static string Toggle(bool value) => value ? "on" : "off";
    }
}
