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
    public static class AIObservationLabSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/AIObservationLab.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild AI Observation Lab Scene")]
        public static void RebuildAIObservationLabScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AIObservationLab";

            GameObject bootstrap = new GameObject("AI Observation Lab");
            bootstrap.AddComponent<AIObservationLabBootstrap>();

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
                new EditorBuildSettingsScene("Assets/Scenes/ConstructionDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/PopulationDemo.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateAIObservationLabScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("AI Observation Lab scene could not be opened.");
            if (Object.FindFirstObjectByType<AIObservationLabBootstrap>() == null) throw new System.InvalidOperationException("AI Observation Lab scene does not contain AIObservationLabBootstrap.");

            StarterPopulationProfile population = new StarterPopulationProfile(20, 5, 8, 6, 5, 100, 100, null);
            StarterHiveProfile hive = StarterHiveProfile.CreateDefault();
            PlayableHiveState state = new NewGameInitializer().CreateNewGame(hive, population, StarterResourceProfile.CreateDefault());
            state.QueenManager.UpdateState(state.QueenId, QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.ActiveQueen);
            for (int i = 0; i < 5; i++)
            {
                HiveExpansionPlan plan = state.GrowthManager.PlanExpansion(new HiveExpansionRequest(HiveChamberType.Utility, state.BeeIds.Count, 200d, 28d, true, new[] { "starter-beekeeping" }));
                if (plan.IsApproved) state.GrowthManager.CreateChamber(plan, "chamber-1");
            }

            const double deltaSeconds = 0.1d;
            for (int i = 0; i < 36000; i++)
            {
                double totalSeconds = (i + 1) * deltaSeconds;
                SimulationExecutionContext context = new SimulationExecutionContext(new SimulationTimestamp(i + 1, totalSeconds), new SimulationCalendar(1, 0, (int)(totalSeconds / 60d), SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
                state.Controller.Execute(context);
                state.AIManager.Execute(context);
            }

            if (state.AIManager.GetStatistics().BrainCount <= 0 || state.TaskManager.Diagnostics.ReservationCount <= 0 || state.Diagnostics.ErrorCount != 0)
            {
                throw new System.InvalidOperationException("AI Observation Lab validation failed.");
            }

            Debug.Log("AI Observation Lab validation completed: 36000 ticks, brains " + state.AIManager.GetStatistics().BrainCount + ", task reservations " + state.TaskManager.Diagnostics.ReservationCount + ".");
        }
    }
}
