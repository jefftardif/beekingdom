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
    public sealed class WorldSimulationBootstrap : MonoBehaviour
    {
        private const double FixedStepSeconds = 0.1d;

        private readonly List<PlayableHiveState> colonies = new List<PlayableHiveState>();
        private readonly List<string> regionIds = new List<string>();
        private readonly Dictionary<string, GameObject> regionVisuals = new Dictionary<string, GameObject>();

        private SimulationTickEngine tickEngine;
        private WorldManager worldManager;
        private RegionManager regionManager;
        private WorldState worldState;
        private Camera sceneCamera;
        private double nextStreamingPulse;
        private int streamingIndex;
        private bool showRegions = true;
        private bool showColonies = true;
        private bool showResources = true;
        private bool showBorders = true;
        private bool showStreaming = true;
        private bool showEvents = true;
        private bool showStatistics = true;
        private bool showDiagnostics = true;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;

        public WorldManager WorldManager => worldManager;
        public RegionManager RegionManager => regionManager;
        public IReadOnlyList<PlayableHiveState> Colonies => colonies;

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
            worldManager = new WorldManager();
            worldState = worldManager.CreateWorld(new WorldSeed("demo-007-world"), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Standard));
            worldManager.ValidateWorld();

            regionManager = new RegionManager();
            RegisterRegion("prairie", WorldBiomeType.Prairie, WorldWeather.Clear, SimulationSeason.Spring, new[] { "forest", "river" });
            RegisterRegion("forest", WorldBiomeType.Forest, WorldWeather.Cloudy, SimulationSeason.Spring, new[] { "prairie", "orchard" });
            RegisterRegion("river", WorldBiomeType.River, WorldWeather.Rain, SimulationSeason.Spring, new[] { "prairie", "orchard" });
            RegisterRegion("orchard", WorldBiomeType.Orchard, WorldWeather.Clear, SimulationSeason.Spring, new[] { "forest", "river" });

            for (int i = 0; i < regionIds.Count; i++)
            {
                regionManager.LoadRegion(regionIds[i]);
                if (i == 2) regionManager.SetState(regionIds[i], RegionSimulationState.Suspended);
            }

            CreateColony("Alpha", 22, 120d, 0);
            CreateColony("Bravo", 18, 100d, 1);
            CreateColony("Charlie", 16, 90d, 2);
            CreateColony("Delta", 20, 110d, 3);
        }

        private void RegisterRegion(string id, WorldBiomeType biome, WorldWeather weather, SimulationSeason season, IReadOnlyList<string> neighbors)
        {
            regionIds.Add(id);
            regionManager.RegisterRegion(new RegionDefinition(id, worldState.WorldId, worldState.Seed, biome, weather, season, 18d, 0.65d, 8, 4, 16, neighbors));
        }

        private void CreateColony(string name, int workers, double honey, int index)
        {
            StarterPopulationProfile population = new StarterPopulationProfile(workers, 4, 6, 5, 4, 100, 100, null);
            StarterResourceProfile resources = new StarterResourceProfile(
                new Dictionary<ResourceType, double>
                {
                    { ResourceType.Nectar, 100d + index * 10d },
                    { ResourceType.Pollen, 80d + index * 8d },
                    { ResourceType.Water, 60d },
                    { ResourceType.Wax, 160d + index * 20d },
                    { ResourceType.Honey, honey }
                },
                280d);
            PlayableHiveState state = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), population, resources);
            ActivateQueen(state);
            colonies.Add(state);
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
                worldManager.Execute(context);
                for (int c = 0; c < colonies.Count; c++)
                {
                    colonies[c].Controller.Execute(context);
                    colonies[c].AIManager.Execute(context);
                }

                if (tickEngine.TotalSeconds >= nextStreamingPulse)
                {
                    RunStreamingPulse();
                    nextStreamingPulse = tickEngine.TotalSeconds + 30d;
                }
            }
        }

        private SimulationExecutionContext CreateContext(double deltaSeconds)
        {
            SimulationTimestamp timestamp = new SimulationTimestamp(tickEngine.TickIndex, tickEngine.TotalSeconds);
            int totalMinutes = (int)(tickEngine.TotalSeconds / 60d);
            return new SimulationExecutionContext(timestamp, new SimulationCalendar(1 + totalMinutes / 1440, totalMinutes / 60 % 24, totalMinutes % 60, SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
        }

        private void RunStreamingPulse()
        {
            string regionId = regionIds[streamingIndex % regionIds.Count];
            int phase = streamingIndex % 4;
            if (phase == 0) regionManager.SetState(regionId, RegionSimulationState.Active);
            if (phase == 1) regionManager.SetState(regionId, RegionSimulationState.Suspended);
            if (phase == 2) regionManager.UnloadRegion(regionId);
            if (phase == 3) regionManager.LoadRegion(regionId);
            streamingIndex++;
            RefreshRegionColors();
        }

        private void BuildScenePrimitives()
        {
            CreateRegionVisual("prairie", new Vector3(-5f, 0f, 4f), new Color(0.34f, 0.68f, 0.36f));
            CreateRegionVisual("forest", new Vector3(5f, 0f, 4f), new Color(0.16f, 0.44f, 0.28f));
            CreateRegionVisual("river", new Vector3(-5f, 0f, -4f), new Color(0.22f, 0.52f, 0.85f));
            CreateRegionVisual("orchard", new Vector3(5f, 0f, -4f), new Color(0.66f, 0.52f, 0.32f));
            RefreshRegionColors();
        }

        private void CreateRegionVisual(string regionId, Vector3 position, Color color)
        {
            GameObject region = GameObject.CreatePrimitive(PrimitiveType.Cube);
            region.name = "Region " + regionId;
            region.transform.position = position;
            region.transform.localScale = new Vector3(8f, 0.25f, 6f);
            Renderer renderer = region.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
            regionVisuals[regionId] = region;

            GameObject colony = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            colony.name = "Colony " + regionId;
            colony.transform.position = position + Vector3.up * 0.8f;
            colony.transform.localScale = Vector3.one;
            Renderer colonyRenderer = colony.GetComponent<Renderer>();
            if (colonyRenderer != null) colonyRenderer.material.color = new Color(0.95f, 0.72f, 0.18f);
        }

        private void RefreshRegionColors()
        {
            foreach (string regionId in regionIds)
            {
                if (!regionVisuals.TryGetValue(regionId, out GameObject visual)) continue;
                Renderer renderer = visual.GetComponent<Renderer>();
                if (renderer == null) continue;
                RegionInstance instance = regionManager.QueryRegion(regionId);
                if (instance == null) renderer.material.color = Color.gray;
                else if (instance.Snapshot.State == RegionSimulationState.Active) renderer.material.color = new Color(0.32f, 0.72f, 0.42f);
                else if (instance.Snapshot.State == RegionSimulationState.Suspended) renderer.material.color = new Color(0.85f, 0.72f, 0.24f);
                else renderer.material.color = new Color(0.45f, 0.5f, 0.58f);
            }
        }

        private void HandleDebugKeys()
        {
            if (Input.GetKeyDown(KeyCode.F1)) showRegions = !showRegions;
            if (Input.GetKeyDown(KeyCode.F2)) showColonies = !showColonies;
            if (Input.GetKeyDown(KeyCode.F3)) showResources = !showResources;
            if (Input.GetKeyDown(KeyCode.F4)) showBorders = !showBorders;
            if (Input.GetKeyDown(KeyCode.F5)) showStreaming = !showStreaming;
            if (Input.GetKeyDown(KeyCode.F6)) showEvents = !showEvents;
            if (Input.GetKeyDown(KeyCode.F7)) showStatistics = !showStatistics;
            if (Input.GetKeyDown(KeyCode.F8)) showDiagnostics = !showDiagnostics;
        }

        private void MoveCamera()
        {
            if (sceneCamera == null) return;
            float speed = Input.GetKey(KeyCode.LeftShift) ? 18f : 9f;
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            sceneCamera.transform.position += sceneCamera.transform.TransformDirection(input) * speed * Time.deltaTime;
            if (Input.GetMouseButton(1))
            {
                sceneCamera.transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * 4f, Space.World);
                sceneCamera.transform.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * 4f, Space.Self);
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Math.Abs(scroll) > 0.01f) sceneCamera.transform.position += sceneCamera.transform.forward * scroll * 4f;
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
            if (worldManager == null) return;
            GUI.Box(new Rect(12, 12, 440, 610), "World Simulation Demo");
            GUILayout.BeginArea(new Rect(24, 40, 416, 570));
            DrawOverlay();
            GUILayout.EndArea();
        }

        private void DrawOverlay()
        {
            WorldStatistics stats = worldManager.GetStatistics();
            int loaded = 0;
            int active = 0;
            int suspended = 0;
            foreach (string regionId in regionIds)
            {
                RegionInstance region = regionManager.QueryRegion(regionId);
                if (region == null) continue;
                loaded++;
                if (region.Snapshot.State == RegionSimulationState.Active) active++;
                if (region.Snapshot.State == RegionSimulationState.Suspended) suspended++;
            }

            int totalPopulation = 0;
            for (int i = 0; i < colonies.Count; i++) totalPopulation += colonies[i].BeeIds.Count;

            GUILayout.Label("Tick: " + tickEngine.TickIndex + " | FPS: " + fps.ToString("0") + " | TPS: " + (1d / FixedStepSeconds).ToString("0"));
            GUILayout.Label("F1 Regions " + Toggle(showRegions) + " F2 Colonies " + Toggle(showColonies) + " F3 Resources " + Toggle(showResources));
            GUILayout.Label("F4 Borders " + Toggle(showBorders) + " F5 Streaming " + Toggle(showStreaming) + " F6 Events " + Toggle(showEvents));
            GUILayout.Label("F7 Stats " + Toggle(showStatistics) + " F8 Diagnostics " + Toggle(showDiagnostics));
            if (showStatistics)
            {
                GUILayout.Space(6);
                GUILayout.Label("World regions: " + stats.RegionCount + " | chunks " + stats.ChunkCount + " | richness " + stats.AverageRichness.ToString("0.00"));
                GUILayout.Label("Streaming: loaded " + loaded + " | active " + active + " | suspended " + suspended + " | unloaded " + Math.Max(0, regionIds.Count - loaded));
                GUILayout.Label("Colonies: " + colonies.Count + " | total population " + totalPopulation);
                for (int i = 0; i < colonies.Count; i++)
                {
                    GUILayout.Label("Colony " + (i + 1) + ": bees " + colonies[i].BeeIds.Count + " | honey " + colonies[i].ResourceFlowManager.QueryFlow("colony-reserve", ResourceType.Honey).ToString("0.0"));
                }
            }

            if (showDiagnostics)
            {
                GUILayout.Space(6);
                GUILayout.Label("World generated regions: " + worldManager.Diagnostics.RegionsGenerated + " | loaded " + worldManager.Diagnostics.RegionsLoaded);
                GUILayout.Label("Events/local exploration/strategy: not exposed as integrated world-colony links.");
                GUILayout.Label("Memory: Unity runtime metric not exposed to framework overlay.");
                ColonyIntegrationDemoDiagnostics.DrawSceneItems("WorldSimulation", 5);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showBorders || regionVisuals.Count == 0) return;
            Gizmos.color = Color.white;
            foreach (GameObject visual in regionVisuals.Values)
            {
                if (visual == null) continue;
                Gizmos.DrawWireCube(visual.transform.position, visual.transform.localScale);
            }
        }

        private static string Toggle(bool value) => value ? "on" : "off";
    }
}
