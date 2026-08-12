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
    public static class CombatDefenseDemoSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/CombatDefenseDemo.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Combat Defense Demo Scene")]
        public static void RebuildCombatDefenseDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CombatDefenseDemo";
            new GameObject("Combat & Defense Simulation").AddComponent<CombatDefenseDemoBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 8f, -12f);
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
                new EditorBuildSettingsScene("Assets/Scenes/CommunicationLab.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/WorldSimulation.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/SeasonWeatherDemo.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateCombatDefenseDemoScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("Combat Defense Demo scene could not be opened.");
            if (Object.FindFirstObjectByType<CombatDefenseDemoBootstrap>() == null) throw new System.InvalidOperationException("Combat Defense Demo scene does not contain CombatDefenseDemoBootstrap.");

            PlayableHiveState colony = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), new StarterPopulationProfile(22, 4, 6, 8, 4, 100, 100, null), StarterResourceProfile.CreateDefault());
            ActivateQueen(colony);
            EmergencyResponseManager emergency = new EmergencyResponseManager();
            emergency.RegisterEmergencyType(new EmergencyPlan("predator-attack", EmergencyType.PredatorAttack, 0.35d));
            EmergencyIncident incident = emergency.DetectEmergency("predator-attack", 0.9d);
            if (incident == null || !emergency.ActivateEmergency(incident.IncidentId) || !emergency.EscalateEmergency(incident.IncidentId, 0.95d)) throw new System.InvalidOperationException("Emergency validation failed.");
            SwarmCommunicationManager communication = new SwarmCommunicationManager();
            communication.RegisterCommunicationChannel(new CommunicationChannel("alarm", CommunicationKind.EmergencySignal, 12d));
            CommunicationSignal signal = communication.BroadcastSignal("alarm", CommunicationSignalType.DangerDetected, incident.IncidentId, 0.5d, 1d, 0.02d, 40d, 1d);

            const double deltaSeconds = 0.1d;
            for (int i = 0; i < 36000; i++)
            {
                double totalSeconds = (i + 1) * deltaSeconds;
                SimulationExecutionContext context = new SimulationExecutionContext(new SimulationTimestamp(i + 1, totalSeconds), new SimulationCalendar(1, 0, (int)(totalSeconds / 60d), SimulationSeason.Spring), SimulationTickFrequency.TenHz, deltaSeconds, null);
                colony.Controller.Execute(context);
                colony.AIManager.Execute(context);
                communication.PropagateSignal(deltaSeconds);
                if (signal != null) communication.ReceiveSignal(signal.SignalId, 12d, 1d);
                communication.ExpireSignal();
            }
            emergency.ResolveEmergency(incident.IncidentId);

            if (emergency.Diagnostics.Detected <= 0 || emergency.Diagnostics.Activated <= 0 || emergency.Diagnostics.Resolved <= 0 || communication.Diagnostics.Broadcast <= 0 || colony.Diagnostics.ErrorCount != 0)
            {
                throw new System.InvalidOperationException("Combat Defense Demo validation failed.");
            }

            Debug.Log("Combat Defense Demo validation completed: 36000 ticks, emergencies " + emergency.Diagnostics.Detected + ", alarm broadcasts " + communication.Diagnostics.Broadcast + ", resolved " + emergency.Diagnostics.Resolved + ".");
        }

        private static void ActivateQueen(PlayableHiveState state)
        {
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, BeeKingdom.Hive.QueenState.ActiveQueen);
        }
    }
}
