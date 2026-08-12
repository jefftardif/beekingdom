using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using BeeKingdom.Population;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class CommunicationLabSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/CommunicationLab.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Communication Lab Scene")]
        public static void RebuildCommunicationLabScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CommunicationLab";

            GameObject bootstrap = new GameObject("Swarm Communication & Pheromone Lab");
            bootstrap.AddComponent<CommunicationLabBootstrap>();

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
                new EditorBuildSettingsScene("Assets/Scenes/LogisticsDemo.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateCommunicationLabScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("Communication Lab scene could not be opened.");
            if (Object.FindFirstObjectByType<CommunicationLabBootstrap>() == null) throw new System.InvalidOperationException("Communication Lab scene does not contain CommunicationLabBootstrap.");

            StarterPopulationProfile population = new StarterPopulationProfile(24, 5, 8, 8, 5, 100, 100, null);
            PlayableHiveState state = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), population, StarterResourceProfile.CreateDefault());
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.ActiveQueen);

            SwarmCommunicationManager communication = new SwarmCommunicationManager();
            communication.RegisterCommunicationChannel(new CommunicationChannel("pheromone", CommunicationKind.Pheromone, 24d));
            communication.RegisterCommunicationChannel(new CommunicationChannel("alarm", CommunicationKind.EmergencySignal, 8d));
            CollectiveIntelligenceManager collective = new CollectiveIntelligenceManager();
            collective.RegisterCollectiveBehavior(new CollectiveBehaviorDefinition("validation-defense", CollectiveBehaviorType.EmergencyDefense, ColonyPriorityType.Defend, 0.4d));
            collective.RegisterCollectiveBehavior(new CollectiveBehaviorDefinition("validation-food", CollectiveBehaviorType.FoodGathering, ColonyPriorityType.Produce, 0.2d));

            CommunicationSignal food = communication.BroadcastSignal("pheromone", CommunicationSignalType.FoodFound, "validation-forager", 0.5d, 1d, 0.01d, 90d, 0.7d);
            CommunicationSignal alarm = communication.BroadcastSignal("alarm", CommunicationSignalType.DangerDetected, "validation-guard", 0.5d, 1d, 0.015d, 90d, 1d);
            collective.BroadcastSignal(new SwarmSignal("validation-food", SwarmSignalType.FoodPheromone, 0.8d, 8d, 0.05d, 0.6d, 0.6d));
            collective.BroadcastSignal(new SwarmSignal("validation-alarm", SwarmSignalType.AlarmPheromone, 1d, 10d, 0.05d, 0.8d, 1d));

            const double deltaSeconds = 0.1d;
            for (int i = 0; i < 36000; i++)
            {
                double totalSeconds = (i + 1) * deltaSeconds;
                SimulationExecutionContext context = new SimulationExecutionContext(new SimulationTimestamp(i + 1, totalSeconds), new SimulationCalendar(1, 0, (int)(totalSeconds / 60d), SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
                state.Controller.Execute(context);
                state.AIManager.Execute(context);
                communication.PropagateSignal(deltaSeconds);
                communication.ReceiveSignal(food.SignalId, 20d, 1d);
                communication.ReceiveSignal(alarm.SignalId, 20d, 1d);
                if (i % 1200 == 0) collective.EvaluateColonyIntent(new ColonyStateContext(threatPressure: i % 2400 == 0 ? 0.9d : 0.1d, playerGoalPressure: 0.5d));
                communication.ExpireSignal();
            }

            if (communication.Diagnostics.Broadcast < 2 || communication.Diagnostics.Received <= 0 || collective.QueryCollectiveStatistics().SignalsBroadcast < 2 || state.Diagnostics.ErrorCount != 0)
            {
                throw new System.InvalidOperationException("Communication Lab validation failed.");
            }

            Debug.Log("Communication Lab validation completed: 36000 ticks, broadcasts " + communication.Diagnostics.Broadcast + ", received " + communication.Diagnostics.Received + ", expired " + communication.Diagnostics.Expired + ", collective signals " + collective.QueryCollectiveStatistics().SignalsBroadcast + ".");
        }
    }
}
