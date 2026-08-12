using BeeKingdom.Playground;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Economy;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class ConstructionDemoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ConstructionDemo.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Construction Demo Scene")]
        public static void RebuildConstructionDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ConstructionDemo";

            GameObject bootstrap = new GameObject("Construction Demo");
            bootstrap.AddComponent<ConstructionDemoBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 9f, -13f);
            cameraObject.transform.rotation = Quaternion.Euler(38f, 0f, 0f);

            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/LivingHive.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateConstructionDemoScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new System.InvalidOperationException("Construction Demo scene could not be opened.");
            }

            if (Object.FindFirstObjectByType<ConstructionDemoBootstrap>() == null)
            {
                throw new System.InvalidOperationException("Construction Demo scene does not contain ConstructionDemoBootstrap.");
            }

            StarterPopulationProfile population = new StarterPopulationProfile(22, 4, 12, 4, 4, 100, 100, null);
            StarterResourceProfile resources = new StarterResourceProfile(
                new System.Collections.Generic.Dictionary<ResourceType, double>
                {
                    { ResourceType.Nectar, 160d },
                    { ResourceType.Pollen, 140d },
                    { ResourceType.Water, 90d },
                    { ResourceType.Wax, 260d },
                    { ResourceType.Honey, 120d }
                },
                300d);
            PlayableHiveState state = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), population, resources);
            for (int i = 0; i < 5; i++)
            {
                HiveExpansionPlan plan = state.GrowthManager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.Utility, state.BeeIds.Count, 260d, 28d, true, new[] { "starter-beekeeping" }));
                if (plan.IsApproved)
                {
                    state.GrowthManager.CreateChamber(plan, "chamber-1");
                }
            }

            const double deltaSeconds = 0.1d;
            for (int i = 0; i < 18000; i++)
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

            if (state.GrowthManager.Diagnostics.CompletedChambers <= 0 || state.Diagnostics.ErrorCount != 0)
            {
                throw new System.InvalidOperationException("Construction Demo validation failed.");
            }

            Debug.Log("Construction Demo validation completed: 18000 ticks, completed chambers " + state.GrowthManager.Diagnostics.CompletedChambers + ", population " + state.Diagnostics.Population + ".");
        }
    }
}
