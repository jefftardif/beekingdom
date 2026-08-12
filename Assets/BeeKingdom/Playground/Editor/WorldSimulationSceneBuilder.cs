using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using BeeKingdom.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldSimulationSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/WorldSimulation.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild World Simulation Scene")]
        public static void RebuildWorldSimulationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WorldSimulation";

            GameObject bootstrap = new GameObject("World Simulation Demo");
            bootstrap.AddComponent<WorldSimulationBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 14f, -16f);
            cameraObject.transform.rotation = Quaternion.Euler(52f, 0f, 0f);

            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/LivingHive.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/ConstructionDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/PopulationDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/AIObservationLab.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/LogisticsDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/CommunicationLab.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateWorldSimulationScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("World Simulation scene could not be opened.");
            if (Object.FindFirstObjectByType<WorldSimulationBootstrap>() == null) throw new System.InvalidOperationException("World Simulation scene does not contain WorldSimulationBootstrap.");

            WorldManager worldManager = new WorldManager();
            WorldState world = worldManager.CreateWorld(new WorldSeed("demo-007-validation"), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Standard));
            if (!worldManager.ValidateWorld().IsValid) throw new System.InvalidOperationException("Generated world is invalid.");

            RegionManager regionManager = new RegionManager();
            regionManager.RegisterRegion(new RegionDefinition("validation-prairie", world.WorldId, world.Seed, WorldBiomeType.Prairie, WorldWeather.Clear, SimulationSeason.Spring, 18d, 0.6d, 8, 4, 16));
            regionManager.RegisterRegion(new RegionDefinition("validation-forest", world.WorldId, world.Seed, WorldBiomeType.Forest, WorldWeather.Cloudy, SimulationSeason.Spring, 18d, 0.6d, 8, 4, 16));
            regionManager.LoadRegion("validation-prairie");
            regionManager.LoadRegion("validation-forest");
            regionManager.SetState("validation-forest", RegionSimulationState.Suspended);
            regionManager.UnloadRegion("validation-forest");
            regionManager.LoadRegion("validation-forest");

            PlayableHiveState colonyA = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), new StarterPopulationProfile(18, 4, 5, 4, 4, 100, 100, null), StarterResourceProfile.CreateDefault());
            PlayableHiveState colonyB = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), new StarterPopulationProfile(16, 4, 5, 4, 4, 100, 100, null), StarterResourceProfile.CreateDefault());
            ActivateQueen(colonyA);
            ActivateQueen(colonyB);

            const double deltaSeconds = 0.1d;
            for (int i = 0; i < 36000; i++)
            {
                double totalSeconds = (i + 1) * deltaSeconds;
                SimulationExecutionContext context = new SimulationExecutionContext(new SimulationTimestamp(i + 1, totalSeconds), new SimulationCalendar(1, 0, (int)(totalSeconds / 60d), SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
                worldManager.Execute(context);
                colonyA.Controller.Execute(context);
                colonyA.AIManager.Execute(context);
                colonyB.Controller.Execute(context);
                colonyB.AIManager.Execute(context);
            }

            if (worldManager.GetStatistics().RegionCount <= 0 || regionManager.QueryRegion("validation-prairie") == null || colonyA.BeeIds.Count <= 0 || colonyB.BeeIds.Count <= 0 || colonyA.Diagnostics.ErrorCount != 0 || colonyB.Diagnostics.ErrorCount != 0)
            {
                throw new System.InvalidOperationException("World Simulation validation failed.");
            }

            Debug.Log("World Simulation validation completed: 36000 ticks, world regions " + worldManager.GetStatistics().RegionCount + ", colonies 2, population " + (colonyA.BeeIds.Count + colonyB.BeeIds.Count) + ".");
        }

        private static void ActivateQueen(PlayableHiveState state)
        {
            state.QueenManager.UpdateState(state.QueenId, QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.ActiveQueen);
        }
    }
}
