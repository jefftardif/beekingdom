using BeeKingdom.Colony;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class SandboxPlaygroundSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Sandbox Playground Scene")]
        public static void RebuildSandboxPlaygroundScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SandboxPlayground";
            new GameObject("Sandbox Playground").AddComponent<SandboxPlaygroundBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 8f, -11f);
            cameraObject.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
            SandboxPlaygroundBootstrap.EnsureRenderableCamera(camera);

            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/LivingHive.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/ConstructionDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/PopulationDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/AIObservationLab.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/LogisticsDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/CommunicationLab.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/WorldSimulation.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/SeasonWeatherDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/CombatDefenseDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MultiplayerSynchronization.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/BenchmarkSuite.unity", true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateSandboxPlaygroundScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new System.InvalidOperationException("Sandbox Playground scene could not be opened.");
            SandboxPlaygroundBootstrap bootstrap = Object.FindFirstObjectByType<SandboxPlaygroundBootstrap>();
            if (bootstrap == null) throw new System.InvalidOperationException("Sandbox Playground scene does not contain SandboxPlaygroundBootstrap.");
            if (!bootstrap.isActiveAndEnabled) throw new System.InvalidOperationException("Sandbox Playground bootstrap is not active.");
            Camera camera = Camera.main;
            if (camera == null) throw new System.InvalidOperationException("Sandbox Playground scene does not contain a Main Camera.");
            SandboxPlaygroundBootstrap.EnsureRenderableCamera(camera);
            if (camera.GetComponent<UniversalAdditionalCameraData>() == null) throw new System.InvalidOperationException("Sandbox Playground camera is missing URP additional camera data.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            ColonySandboxManager manager = new ColonySandboxManager();
            SandboxSession session = manager.CreateSandbox(new SandboxDefinition("validation-sandbox", new[] { SandboxMode.FreeBuild, SandboxMode.DebugMode }, null));
            if (session == null || !manager.StartSandbox(session.Definition.SandboxId) || !manager.PauseSandbox(session.Definition.SandboxId) || !manager.ResumeSandbox(session.Definition.SandboxId) || !manager.ResetSandbox(session.Definition.SandboxId))
            {
                throw new System.InvalidOperationException("Sandbox framework validation failed.");
            }

            if (EditorBuildSettings.scenes.Length < 12) throw new System.InvalidOperationException("Not all demo scenes are registered in build settings.");
            Debug.Log("Sandbox Playground validation completed: scenes " + EditorBuildSettings.scenes.Length + ", sandbox state " + manager.QuerySandbox(session.Definition.SandboxId).State + ". DEMO-012 monitor ready.");
        }

        public static void ValidateSandboxPremiumRuntimeIntegration()
        {
            SandboxPremiumRuntimeIntegrationTests.ValidatePremiumRuntimeIntegration();
        }

        public static void CaptureBee620PlayerGameView()
        {
            SandboxBee620PlayerGameViewCapture.CaptureBee620PlayerGameView();
        }
    }
}
