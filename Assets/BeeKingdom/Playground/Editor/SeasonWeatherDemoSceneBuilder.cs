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
    public static class SeasonWeatherDemoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SeasonWeatherDemo.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Season Weather Demo Scene")]
        public static void RebuildSeasonWeatherDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SeasonWeatherDemo";

            GameObject bootstrap = new GameObject("Seasons & Weather Simulation");
            bootstrap.AddComponent<SeasonWeatherDemoBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 10f, -14f);
            cameraObject.transform.rotation = Quaternion.Euler(42f, 0f, 0f);

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
                new EditorBuildSettingsScene("Assets/Scenes/WorldSimulation.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateSeasonWeatherDemoScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("Season Weather Demo scene could not be opened.");
            if (Object.FindFirstObjectByType<SeasonWeatherDemoBootstrap>() == null) throw new System.InvalidOperationException("Season Weather Demo scene does not contain SeasonWeatherDemoBootstrap.");

            SeasonManager season = new SeasonManager(300d);
            WeatherManager weather = new WeatherManager(new WorldSeed("demo-008-validation"), WeatherProfile.Temperate(), ClimateRules.CreateDefault(), 120d);
            RegenerationManager regeneration = new RegenerationManager();
            regeneration.RegisterNode(new NaturalResourceNode("validation-nectar", "demo-region", new HexCoordinates(0, 0), BeeKingdom.Economy.ResourceType.Nectar, 200d, 20d, new ResourceNodeLifecycle(0.08d, 0.2d)));
            PlayableHiveState colony = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), new StarterPopulationProfile(20, 4, 6, 5, 4, 100, 100, null), StarterResourceProfile.CreateDefault());
            ActivateQueen(colony);

            const double deltaSeconds = 0.1d;
            for (int i = 0; i < 36000; i++)
            {
                double totalSeconds = (i + 1) * deltaSeconds;
                SimulationExecutionContext context = new SimulationExecutionContext(new SimulationTimestamp(i + 1, totalSeconds), new SimulationCalendar(1, 0, (int)(totalSeconds / 60d), season.CurrentSeason), SimulationTickFrequency.TenHz, deltaSeconds, null);
                season.Execute(context);
                weather.Execute(context);
                regeneration.Execute(context);
                colony.Controller.Execute(context);
                colony.AIManager.Execute(context);
            }

            if (season.CurrentSeason == SimulationSeason.Spring || weather.GetProductionModifier(SimulationSeason.Winter) >= weather.GetProductionModifier(SimulationSeason.Spring) || regeneration.Diagnostics.NodeCount <= 0 || colony.Diagnostics.ErrorCount != 0)
            {
                throw new System.InvalidOperationException("Season Weather Demo validation failed.");
            }

            Debug.Log("Season Weather Demo validation completed: 36000 ticks, season " + season.CurrentSeason + ", weather " + weather.CurrentWeather + ", nodes " + regeneration.Diagnostics.NodeCount + ".");
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
