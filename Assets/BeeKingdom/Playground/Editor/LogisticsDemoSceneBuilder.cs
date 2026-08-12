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
    public static class LogisticsDemoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/LogisticsDemo.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Logistics Demo Scene")]
        public static void RebuildLogisticsDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "LogisticsDemo";

            GameObject bootstrap = new GameObject("Resource & Logistics Flow");
            bootstrap.AddComponent<LogisticsDemoBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 9f, -14f);
            cameraObject.transform.rotation = Quaternion.Euler(38f, 0f, 0f);

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
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateLogisticsDemoScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("Logistics Demo scene could not be opened.");
            if (Object.FindFirstObjectByType<LogisticsDemoBootstrap>() == null) throw new System.InvalidOperationException("Logistics Demo scene does not contain LogisticsDemoBootstrap.");

            StarterPopulationProfile population = new StarterPopulationProfile(24, 5, 8, 8, 5, 100, 100, null);
            PlayableHiveState state = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), population, StarterResourceProfile.CreateDefault());
            state.QueenManager.UpdateState(state.QueenId, QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.ActiveQueen);
            state.InventoryManager.CreateCell("validation-honey", new StoragePosition(0, 0), ResourceType.Honey, 100d, "food");
            StorageReservation storageReservation = state.InventoryManager.ReserveSpace(ResourceType.Honey, 20d, new StoragePosition(0, 0), StoragePolicy.Specialized);
            if (!storageReservation.IsValid || !state.InventoryManager.Deposit(storageReservation, 0d)) throw new System.InvalidOperationException("Logistics storage validation failed.");

            const double deltaSeconds = 0.1d;
            for (int i = 0; i < 36000; i++)
            {
                double totalSeconds = (i + 1) * deltaSeconds;
                SimulationExecutionContext context = new SimulationExecutionContext(new SimulationTimestamp(i + 1, totalSeconds), new SimulationCalendar(1, 0, (int)(totalSeconds / 60d), SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
                state.Controller.Execute(context);
                state.AIManager.Execute(context);
                if (i % 600 == 0)
                {
                    state.ResourceFlowManager.Produce("validation-flower", "field-cache", ResourceType.Nectar, 8d, totalSeconds);
                    state.ResourceFlowManager.Transfer("field-cache", "colony-reserve", ResourceType.Nectar, 4d, totalSeconds);
                    ResourceReservation reservation = state.ResourceFlowManager.Reserve("colony-reserve", ResourceType.Nectar, 1d);
                    if (reservation.IsValid) state.ResourceFlowManager.Consume(reservation, totalSeconds);
                }
            }

            if (state.ResourceFlowManager.Diagnostics.TransactionCount <= 0 || state.InventoryManager.QueryInventory().TotalAmount <= 0d || state.Diagnostics.ErrorCount != 0)
            {
                throw new System.InvalidOperationException("Logistics Demo validation failed.");
            }

            Debug.Log("Logistics Demo validation completed: 36000 ticks, transactions " + state.ResourceFlowManager.Diagnostics.TransactionCount + ", inventory " + state.InventoryManager.QueryInventory().TotalAmount.ToString("0.0") + ".");
        }
    }
}
