using System;
using System.Collections.Generic;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using BeeKingdom.World;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class SeasonWeatherDemoBootstrap : MonoBehaviour
    {
        private const double FixedStepSeconds = 0.1d;

        private SimulationTickEngine tickEngine;
        private SeasonManager seasonManager;
        private WeatherManager weatherManager;
        private RegenerationManager regenerationManager;
        private PlayableHiveState colony;
        private Camera sceneCamera;
        private bool paused;
        private double speed = 10d;
        private bool showSeasons = true;
        private bool showWeather = true;
        private bool showResources = true;
        private bool showAI = true;
        private bool showComms = true;
        private bool showStats = true;
        private bool showEvents = true;
        private bool showDiagnostics = true;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;

        public SeasonManager SeasonManager => seasonManager;
        public WeatherManager WeatherManager => weatherManager;
        public RegenerationManager RegenerationManager => regenerationManager;
        public PlayableHiveState Colony => colony;

        private void Awake()
        {
            sceneCamera = Camera.main;
            tickEngine = new SimulationTickEngine(FixedStepSeconds);
        }

        private void Start()
        {
            CreateFrameworkState();
            BuildScenePrimitives();
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
            seasonManager = new SeasonManager(300d);
            weatherManager = new WeatherManager(new WorldSeed("demo-008-weather"), WeatherProfile.Temperate(), ClimateRules.CreateDefault(), 120d);
            regenerationManager = new RegenerationManager();
            regenerationManager.RegisterNode(new NaturalResourceNode("spring-nectar", "demo-region", new HexCoordinates(0, 0), ResourceType.Nectar, 200d, 120d, new ResourceNodeLifecycle(0.08d, 0.2d)));
            regenerationManager.RegisterNode(new NaturalResourceNode("pollen-field", "demo-region", new HexCoordinates(1, 0), ResourceType.Pollen, 180d, 100d, new ResourceNodeLifecycle(0.06d, 0.2d)));
            regenerationManager.RegisterNode(new NaturalResourceNode("river-water", "demo-region", new HexCoordinates(2, 0), ResourceType.Water, 260d, 220d, new ResourceNodeLifecycle(0.1d, 0.2d)));

            colony = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), new StarterPopulationProfile(24, 5, 8, 6, 5, 100, 100, null), StarterResourceProfile.CreateDefault());
            ActivateQueen(colony);
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
            if (paused) return;
            int ticks = tickEngine.Advance(Time.deltaTime * (float)speed, SimulationTickMode.Fixed);
            for (int i = 0; i < ticks; i++)
            {
                SimulationExecutionContext context = CreateContext(FixedStepSeconds);
                seasonManager.Execute(context);
                weatherManager.Execute(context);
                regenerationManager.Execute(context);
                colony.Controller.Execute(context);
                colony.AIManager.Execute(context);
            }
        }

        private SimulationExecutionContext CreateContext(double deltaSeconds)
        {
            SimulationTimestamp timestamp = new SimulationTimestamp(tickEngine.TickIndex, tickEngine.TotalSeconds);
            int totalMinutes = (int)(tickEngine.TotalSeconds / 60d);
            return new SimulationExecutionContext(timestamp, new SimulationCalendar(1 + totalMinutes / 1440, totalMinutes / 60 % 24, totalMinutes % 60, seasonManager.CurrentSeason), SimulationTickFrequency.TenHz, deltaSeconds, null);
        }

        private void BuildScenePrimitives()
        {
            CreatePrimitive("Meadow", PrimitiveType.Cube, Vector3.zero, new Vector3(24f, 0.2f, 16f), new Color(0.28f, 0.58f, 0.32f));
            CreatePrimitive("River", PrimitiveType.Cube, new Vector3(-6f, 0.05f, 0f), new Vector3(2f, 0.1f, 15f), new Color(0.22f, 0.52f, 0.88f));
            CreatePrimitive("Hive", PrimitiveType.Sphere, new Vector3(1f, 1f, 0f), new Vector3(2f, 2f, 2f), new Color(0.94f, 0.68f, 0.18f));
            for (int i = 0; i < 10; i++)
            {
                CreatePrimitive("Season Flower " + i, PrimitiveType.Sphere, new Vector3(-9f + i * 2f, 0.4f, 4f), new Vector3(0.45f, 0.45f, 0.45f), new Color(0.85f, 0.34f, 0.55f));
            }
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
            if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
            if (Input.GetKeyDown(KeyCode.Alpha1)) speed = 1d;
            if (Input.GetKeyDown(KeyCode.Alpha2)) speed = 10d;
            if (Input.GetKeyDown(KeyCode.Alpha3)) speed = 50d;
            if (Input.GetKeyDown(KeyCode.Q)) seasonManager.SetSeason(SimulationSeason.Spring);
            if (Input.GetKeyDown(KeyCode.W)) seasonManager.SetSeason(SimulationSeason.Summer);
            if (Input.GetKeyDown(KeyCode.E)) seasonManager.SetSeason(SimulationSeason.Autumn);
            if (Input.GetKeyDown(KeyCode.R)) seasonManager.SetSeason(SimulationSeason.Winter);
            if (Input.GetKeyDown(KeyCode.T)) weatherManager.SetWeather(WorldWeather.Clear);
            if (Input.GetKeyDown(KeyCode.Y)) weatherManager.SetWeather(WorldWeather.Rain);
            if (Input.GetKeyDown(KeyCode.U)) weatherManager.SetWeather(WorldWeather.Storm);
            if (Input.GetKeyDown(KeyCode.F1)) showSeasons = !showSeasons;
            if (Input.GetKeyDown(KeyCode.F2)) showWeather = !showWeather;
            if (Input.GetKeyDown(KeyCode.F3)) showResources = !showResources;
            if (Input.GetKeyDown(KeyCode.F4)) showAI = !showAI;
            if (Input.GetKeyDown(KeyCode.F5)) showComms = !showComms;
            if (Input.GetKeyDown(KeyCode.F6)) showStats = !showStats;
            if (Input.GetKeyDown(KeyCode.F7)) showEvents = !showEvents;
            if (Input.GetKeyDown(KeyCode.F8)) showDiagnostics = !showDiagnostics;
        }

        private void MoveCamera()
        {
            if (sceneCamera == null) return;
            float cameraSpeed = Input.GetKey(KeyCode.LeftShift) ? 15f : 7f;
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            sceneCamera.transform.position += sceneCamera.transform.TransformDirection(input) * cameraSpeed * Time.deltaTime;
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
            if (seasonManager == null) return;
            GUI.Box(new Rect(12, 12, 430, 570), "Seasons & Weather Simulation");
            GUILayout.BeginArea(new Rect(24, 40, 406, 530));
            GUILayout.Label("Tick: " + tickEngine.TickIndex + " | FPS " + fps.ToString("0") + " | speed x" + speed.ToString("0") + " | " + (paused ? "paused" : "running"));
            GUILayout.Label("F1 Seasons " + Toggle(showSeasons) + " F2 Weather " + Toggle(showWeather) + " F3 Resources " + Toggle(showResources));
            GUILayout.Label("F4 AI " + Toggle(showAI) + " F5 Comms " + Toggle(showComms) + " F6 Stats " + Toggle(showStats));
            if (showSeasons) GUILayout.Label("Season: " + seasonManager.CurrentSeason + " | production x" + weatherManager.GetProductionModifier(seasonManager.CurrentSeason).ToString("0.00") + " | consumption x" + weatherManager.GetConsumptionModifier(seasonManager.CurrentSeason).ToString("0.00"));
            if (showWeather) GUILayout.Label("Weather: " + weatherManager.CurrentWeather + " | movement x" + weatherManager.GetMovementModifier().ToString("0.00") + " | fog not exposed");
            if (showResources) GUILayout.Label("Resource nodes: " + regenerationManager.Diagnostics.NodeCount + " | available " + regenerationManager.Diagnostics.AvailableNodes + " | depleted " + regenerationManager.Diagnostics.DepletedNodes + " | regenerated " + regenerationManager.Diagnostics.RegeneratedEvents);
            if (showStats) GUILayout.Label("Population: " + colony.BeeIds.Count + " | active AI " + colony.AIManager.GetStatistics().ActiveCount + " | honey " + colony.ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Honey).ToString("0.0"));
            if (showDiagnostics) GUILayout.Label("Weather effects are exposed as modifiers; direct bee behavior/weather coupling is not wired.");
            GUILayout.Label("Controls: Space pause, 1/2/3 speed, Q/W/E/R season, T/Y/U weather");
            GUILayout.EndArea();
        }

        private static string Toggle(bool value) => value ? "on" : "off";
    }
}
