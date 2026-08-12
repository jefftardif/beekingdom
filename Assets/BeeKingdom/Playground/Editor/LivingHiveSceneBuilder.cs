using BeeKingdom.Playground;
using BeeKingdom.AI;
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
    public static class LivingHiveSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Living Hive Scene")]
        public static void RebuildLivingHiveScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LivingHive";

            GameObject bootstrap = new GameObject("Living Hive Demo");
            bootstrap.AddComponent<LivingHiveDemoBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 8f, -12f);
            cameraObject.transform.rotation = Quaternion.Euler(35f, 0f, 0f);

            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateLivingHiveScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new System.InvalidOperationException("Living Hive scene could not be opened.");
            }

            if (Object.FindFirstObjectByType<LivingHiveDemoBootstrap>() == null)
            {
                throw new System.InvalidOperationException("Living Hive scene does not contain LivingHiveDemoBootstrap.");
            }

            StarterPopulationProfile populationProfile = new StarterPopulationProfile(30, 5, 5, 5, 4, 100, 100, null);
            StarterHiveProfile hiveProfile = StarterHiveProfile.CreateDefault();
            StarterResourceProfile resourceProfile = StarterResourceProfile.CreateDefault();
            PlayableHiveState state = new NewGameInitializer().CreateNewGame(hiveProfile, populationProfile, resourceProfile);

            state.AIManager.RegisterBehavior(new BehaviorDefinition("build-cell", BeeKingdom.Population.BeeIntent.Build, BehaviorActionType.Build, 12d));
            state.AIManager.RegisterBehavior(new BehaviorDefinition("gather-resource", BeeKingdom.Population.BeeIntent.Gather, BehaviorActionType.Gather, 8d));

            double wax = resourceProfile.Amounts.TryGetValue(ResourceType.Wax, out double amount) ? amount : 0d;
            HiveExpansionPlan plan = state.GrowthManager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.WaxWorkshop, state.BeeIds.Count, wax, 28d, true, hiveProfile.UnlockedTechnologyIds));
            if (plan.IsApproved)
            {
                state.GrowthManager.CreateChamber(plan, "chamber-1");
            }

            const double deltaSeconds = 0.1d;
            const int ticks = 18000;
            for (int i = 0; i < ticks; i++)
            {
                double totalSeconds = (i + 1) * deltaSeconds;
                SimulationTimestamp timestamp = new SimulationTimestamp(i + 1, totalSeconds);
                SimulationCalendar calendar = new SimulationCalendar(1, 0, (int)(totalSeconds / 60d), SimulationSeason.Spring);
                SimulationExecutionContext context = new SimulationExecutionContext(timestamp, calendar, SimulationTickFrequency.TenHz, deltaSeconds, null);
                state.Controller.Execute(context);
                state.AIManager.Execute(context);
            }

            if (state.Diagnostics.Population <= 0 || state.Diagnostics.ErrorCount != 0)
            {
                throw new System.InvalidOperationException("Living Hive simulation validation failed.");
            }

            Debug.Log("Living Hive validation completed: " + ticks + " ticks, " + state.Diagnostics.SimulatedSeconds.ToString("0.0") + " simulated seconds, population " + state.Diagnostics.Population + ".");
        }
    }
}
