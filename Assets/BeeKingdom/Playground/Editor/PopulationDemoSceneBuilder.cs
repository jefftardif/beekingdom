using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class PopulationDemoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PopulationDemo.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Population Demo Scene")]
        public static void RebuildPopulationDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PopulationDemo";

            GameObject bootstrap = new GameObject("Population Demo");
            bootstrap.AddComponent<PopulationDemoBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 8.5f, -12f);
            cameraObject.transform.rotation = Quaternion.Euler(36f, 0f, 0f);

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
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidatePopulationDemoScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("Population Demo scene could not be opened.");
            if (Object.FindFirstObjectByType<PopulationDemoBootstrap>() == null) throw new System.InvalidOperationException("Population Demo scene does not contain PopulationDemoBootstrap.");

            BeeLifecycleRules fastLifecycle = new BeeLifecycleRules(new BeeDevelopmentProfile(30d, 60d, 90d, 120d, 180d), new BeeMortalityProfile(360d), 8f);
            StarterPopulationProfile population = new StarterPopulationProfile(12, 4, 3, 3, 2, 100, 100, fastLifecycle);
            StarterHiveProfile hive = new StarterHiveProfile("population-demo-hive", "player", "queen-1", new HiveCapacity(512, 128, 128), 1, 3f, 1d, new[] { HiveChamberType.Entrance, HiveChamberType.RoyalChamber, HiveChamberType.Nursery }, new[] { "starter-beekeeping" });
            PlayableHiveState state = new NewGameInitializer().CreateNewGame(hive, population, StarterResourceProfile.CreateDefault());
            state.QueenManager.UpdateState(state.QueenId, QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.ActiveQueen);

            const double deltaSeconds = 0.1d;
            for (int i = 0; i < 36000; i++)
            {
                double totalSeconds = (i + 1) * deltaSeconds;
                SimulationExecutionContext context = new SimulationExecutionContext(
                    new SimulationTimestamp(i + 1, totalSeconds),
                    new SimulationCalendar(1, 0, (int)(totalSeconds / 60d), SimulationSeason.Spring),
                    SimulationTickFrequency.TenHz,
                    deltaSeconds,
                    null);
                state.Controller.Execute(context);
                state.AIManager.Execute(context);
            }

            if (state.LifecycleManager.Diagnostics.BeeCount <= population.TotalBees || state.LifecycleManager.Diagnostics.DeadCount <= 0 || state.Diagnostics.ErrorCount != 0)
            {
                throw new System.InvalidOperationException("Population Demo lifecycle validation failed.");
            }

            Debug.Log("Population Demo validation completed: 36000 ticks, total bees " + state.LifecycleManager.Diagnostics.BeeCount + ", dead " + state.LifecycleManager.Diagnostics.DeadCount + ", population " + state.Diagnostics.Population + ".");
        }
    }
}
