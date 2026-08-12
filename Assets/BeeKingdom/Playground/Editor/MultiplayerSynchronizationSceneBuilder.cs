using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class MultiplayerSynchronizationSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MultiplayerSynchronization.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Multiplayer Synchronization Scene")]
        public static void RebuildMultiplayerSynchronizationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MultiplayerSynchronization";
            new GameObject("Multiplayer Synchronization").AddComponent<MultiplayerSynchronizationBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 7f, -12f);
            cameraObject.transform.rotation = Quaternion.Euler(35f, 0f, 0f);

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
                new EditorBuildSettingsScene("Assets/Scenes/CombatDefenseDemo.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateMultiplayerSynchronizationScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("Multiplayer Synchronization scene could not be opened.");
            if (Object.FindFirstObjectByType<MultiplayerSynchronizationBootstrap>() == null) throw new System.InvalidOperationException("Multiplayer Synchronization scene does not contain MultiplayerSynchronizationBootstrap.");
            Debug.Log("Multiplayer Synchronization validation completed: scene compiled, runtime networking unavailable, server connection not attempted.");
        }
    }
}
