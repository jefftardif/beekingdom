using System;
using System.Collections.Generic;
using BeeKingdom.Audio;
using BeeKingdom.Colony;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground
{
    public sealed class SandboxPlaygroundBootstrap : MonoBehaviour
    {
        private readonly string[] demoScenes =
        {
            "LivingHive",
            "ConstructionDemo",
            "PopulationDemo",
            "AIObservationLab",
            "LogisticsDemo",
            "CommunicationLab",
            "WorldSimulation",
            "SeasonWeatherDemo",
            "CombatDefenseDemo",
            "MultiplayerSynchronization",
            "BenchmarkSuite"
        };

        private ColonySandboxManager sandboxManager;
        private SandboxSession session;
        private Camera sceneCamera;
        private bool showSimulation = true;
        private bool showAI = true;
        private bool showConstruction = true;
        private bool showPopulation = true;
        private bool showResources = true;
        private bool showCommunication = true;
        private bool showCombat = true;
        private bool showWorld = true;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;
        private string fallbackMessage = "Sandbox Playground actif";
        private string diagnosticsMessage = "Diagnostics disponibles";
        private string demo012MonitorMessage = "DEMO-012 monitor: initialisation";
        private string visibleHomeMessage = "Accueil joueur visible";

        public SandboxSession Session => session;

        private void Awake()
        {
            sceneCamera = EnsureRenderableCamera(Camera.main);
            sandboxManager = new ColonySandboxManager();
        }

        private void Start()
        {
            MusicManager.EnsureInstance().Play(MusicTrack.Hive);
            try
            {
                BuildScenePrimitives();
                HiveViewProductUiPresenter.EnsureSceneObjects();
            }
            catch (Exception exception)
            {
                fallbackMessage = "Sandbox Playground actif - repere minimal";
                Debug.LogError("Sandbox Playground primitive build failed: " + exception.Message);
                CreatePrimitive("Sandbox Visual Fallback", PrimitiveType.Cube, Vector3.zero, new Vector3(4f, 0.4f, 4f), new Color(0.25f, 0.55f, 0.65f));
            }

            try
            {
                session = sandboxManager.CreateSandbox(new SandboxDefinition("official-playground", new[] { SandboxMode.FreeBuild, SandboxMode.DebugMode, SandboxMode.PerformanceBenchmark, SandboxMode.ReplayValidation }, new Dictionary<string, double> { { "timeScale", 1d } }));
                sandboxManager.StartSandbox(session.Definition.SandboxId);
            }
            catch (Exception exception)
            {
                fallbackMessage = "Sandbox Playground actif - session indisponible";
                Debug.LogError("Sandbox Playground session initialization failed: " + exception.Message);
            }
        }

        private void Update()
        {
            UpdateFps();
            HandleDebugKeys();
            HiveViewProductUiPresenter.HandlePointer(sceneCamera);
            MoveCamera();
        }

        private void BuildScenePrimitives()
        {
            CreatePrimitive("Sandbox Earth Moss Backdrop", PrimitiveType.Cube, Vector3.zero, new Vector3(12f, 0.16f, 8f), new Color(0.12f, 0.10f, 0.055f));
            CreatePrimitive("Sandbox Honey Light Marker", PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), new Vector3(0.72f, 0.72f, 0.72f), new Color(1f, 0.68f, 0.18f));
            for (int i = 0; i < demoScenes.Length; i++)
            {
                CreatePrimitive("Distant Preview Node " + demoScenes[i], PrimitiveType.Cube, new Vector3(-5f + (i % 6) * 2f, 0.24f, -2f + (i / 6) * 4f), new Vector3(0.82f, 0.2f, 0.82f), new Color(0.23f, 0.18f, 0.08f));
            }
        }

        public static Camera EnsureRenderableCamera(Camera camera)
        {
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.transform.position = new Vector3(0f, 8f, -11f);
                cameraObject.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
            }

            camera.enabled = true;
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.065f, 0.05f);
            camera.fieldOfView = 55f;
            if (camera.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            return camera;
        }

        private static void CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
        }

        private void HandleDebugKeys()
        {
            if (session != null && Input.GetKeyDown(KeyCode.Space)) sandboxManager.PauseSandbox(session.Definition.SandboxId);
            if (session != null && Input.GetKeyDown(KeyCode.Return)) sandboxManager.ResumeSandbox(session.Definition.SandboxId);
            if (session != null && Input.GetKeyDown(KeyCode.Backspace)) sandboxManager.ResetSandbox(session.Definition.SandboxId);
            if (Input.GetKeyDown(KeyCode.F1)) showSimulation = !showSimulation;
            if (Input.GetKeyDown(KeyCode.F2)) showAI = !showAI;
            if (Input.GetKeyDown(KeyCode.F3)) showConstruction = !showConstruction;
            if (Input.GetKeyDown(KeyCode.F4)) showPopulation = !showPopulation;
            if (Input.GetKeyDown(KeyCode.F5)) showResources = !showResources;
            if (Input.GetKeyDown(KeyCode.F6)) showCommunication = !showCommunication;
            if (Input.GetKeyDown(KeyCode.F7)) showCombat = !showCombat;
            if (Input.GetKeyDown(KeyCode.F8)) showWorld = !showWorld;
            for (int i = 0; i < demoScenes.Length && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SceneManager.LoadScene(demoScenes[i]);
            }
            if (Input.GetKeyDown(KeyCode.Alpha0)) SceneManager.LoadScene("BenchmarkSuite");
        }

        private void MoveCamera()
        {
            if (sceneCamera == null) return;
            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            sceneCamera.transform.position += sceneCamera.transform.TransformDirection(input) * 8f * Time.deltaTime;
            if (Input.GetMouseButton(1))
            {
                sceneCamera.transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * 4f, Space.World);
                sceneCamera.transform.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * 4f, Space.Self);
            }
            float scroll = Input.mouseScrollDelta.y;
            if (Math.Abs(scroll) > 0.01f) sceneCamera.transform.position += sceneCamera.transform.forward * scroll * 3f;
        }

        private void UpdateFps()
        {
            float delta = Time.unscaledDeltaTime;
            fpsAccumulator += delta > 0f ? 1f / delta : 0f;
            fpsFrames++;
            fpsTimer += delta;
            if (fpsTimer >= 0.5f)
            {
                fps = fpsAccumulator / Math.Max(1, fpsFrames);
                fpsAccumulator = 0f;
                fpsFrames = 0;
                fpsTimer = 0f;
            }
        }

        private void OnGUI()
        {
            HiveViewProductUiPresenter.Draw(fps, Screen.width < 900);
            DrawProductStatus();
        }

        private void DrawProductStatus()
        {
            DrawDemo012MonitorHidden();
            DrawDiagnosticsSummaryHidden();
        }

        private void DrawDemo012MonitorHidden()
        {
            bool cameraReady = sceneCamera != null && sceneCamera.isActiveAndEnabled && sceneCamera.CompareTag("MainCamera");
            bool urpReady = cameraReady && sceneCamera.GetComponent<UniversalAdditionalCameraData>() != null;
            bool fallbackReady = !string.IsNullOrWhiteSpace(fallbackMessage);
            demo012MonitorMessage = cameraReady && urpReady && fallbackReady ? "DEMO-012 monitor: visible" : "DEMO-012 monitor: attention requise";
            visibleHomeMessage = VisiblePlayerHomeUiPresenter.BootstrapContract.VisibleState == VisibleHomeUiState.VisibleOnLaunch ? "Accueil joueur et vue Ruche visibles au lancement" : "Accueil joueur a verifier";
        }

        private void DrawDiagnosticsSummaryHidden()
        {
            try
            {
                diagnosticsMessage = ColonyIntegrationDemoDiagnostics.AvailableCount > 0 ? "Diagnostics disponibles" : "Diagnostics a verifier";
            }
            catch (Exception exception)
            {
                diagnosticsMessage = "Diagnostics indisponibles";
                Debug.LogError("Sandbox Playground diagnostics failed: " + exception.Message);
            }
        }

        private void DrawDemo012Monitor()
        {
            bool cameraReady = sceneCamera != null && sceneCamera.isActiveAndEnabled && sceneCamera.CompareTag("MainCamera");
            bool urpReady = cameraReady && sceneCamera.GetComponent<UniversalAdditionalCameraData>() != null;
            bool sessionReady = session != null && sandboxManager != null;
            bool fallbackReady = !string.IsNullOrWhiteSpace(fallbackMessage);
            bool diagnosticsReady = diagnosticsMessage == "Diagnostics disponibles";
            demo012MonitorMessage = cameraReady && urpReady && fallbackReady
                ? "DEMO-012 monitor: visible"
                : "DEMO-012 monitor: attention requise";
            visibleHomeMessage = VisiblePlayerHomeUiPresenter.BootstrapContract.VisibleState == VisibleHomeUiState.VisibleOnLaunch
                ? "Accueil joueur et vue Ruche visibles au lancement"
                : "Accueil joueur a verifier";

            GUILayout.Space(6);
            GUILayout.Label(demo012MonitorMessage);
            GUILayout.Label("Camera " + Status(cameraReady) + " | URP " + Status(urpReady) + " | Bootstrap " + Status(fallbackReady) + " | Session " + Status(sessionReady) + " | Diagnostics " + Status(diagnosticsReady));
            GUILayout.Label("BEE-422..560: prompts officiels relus; frameworks detectes, surfaces Demo read-only.");
        }

        private void DrawDiagnosticsSummary()
        {
            try
            {
                ColonyIntegrationDemoDiagnostics.DrawSummary("SandboxPlayground", 10);
                diagnosticsMessage = "Diagnostics disponibles";
            }
            catch (Exception exception)
            {
                diagnosticsMessage = "Diagnostics indisponibles";
                GUILayout.Space(6);
                GUILayout.Label("Diagnostics indisponibles. La demonstration reste active en mode secours.");
                GUILayout.Label("Limites: Demo read-only, pas d'UI production finale, pas de serveur social ou militaire.");
                Debug.LogError("Sandbox Playground diagnostics failed: " + exception.Message);
            }
        }

        private static string Toggle(bool value) => value ? "on" : "off";

        private static string Status(bool value) => value ? "OK" : "WAIT";
    }
}
