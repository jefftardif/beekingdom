using System;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class BenchmarkSuiteBootstrap : MonoBehaviour
    {
        private readonly string[] scenarios =
        {
            "100 bees",
            "500 bees",
            "1,000 bees",
            "5,000 bees",
            "10,000 bees",
            "multiple colonies",
            "construction intensive",
            "AI intensive",
            "communication intensive",
            "complete simulation"
        };

        private Camera sceneCamera;
        private bool showSimulation = true;
        private bool showAI = true;
        private bool showConstruction = true;
        private bool showLogistics = true;
        private bool showCommunication = true;
        private bool showNetwork = true;
        private bool showProfiler = true;
        private bool showReport = true;
        private float fps;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsTimer;

        private void Awake()
        {
            sceneCamera = Camera.main;
        }

        private void Start()
        {
            BuildScenePrimitives();
        }

        private void Update()
        {
            UpdateFps();
            HandleDebugKeys();
            MoveCamera();
        }

        private void BuildScenePrimitives()
        {
            CreatePrimitive("Benchmark Board", PrimitiveType.Cube, Vector3.zero, new Vector3(10f, 0.25f, 6f), new Color(0.18f, 0.24f, 0.28f));
            for (int i = 0; i < scenarios.Length; i++)
            {
                CreatePrimitive("Benchmark " + (i + 1), PrimitiveType.Cube, new Vector3(-4.5f + (i % 5) * 2.25f, 0.35f, -2f + (i / 5) * 4f), new Vector3(1.2f, 0.4f, 1.2f), new Color(0.35f, 0.62f, 0.82f));
            }
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
            if (Input.GetKeyDown(KeyCode.F1)) showSimulation = !showSimulation;
            if (Input.GetKeyDown(KeyCode.F2)) showAI = !showAI;
            if (Input.GetKeyDown(KeyCode.F3)) showConstruction = !showConstruction;
            if (Input.GetKeyDown(KeyCode.F4)) showLogistics = !showLogistics;
            if (Input.GetKeyDown(KeyCode.F5)) showCommunication = !showCommunication;
            if (Input.GetKeyDown(KeyCode.F6)) showNetwork = !showNetwork;
            if (Input.GetKeyDown(KeyCode.F7)) showProfiler = !showProfiler;
            if (Input.GetKeyDown(KeyCode.F8)) showReport = !showReport;
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
            GUI.Box(new Rect(12, 12, 480, 650), "Performance Benchmark Suite");
            GUILayout.BeginArea(new Rect(24, 40, 456, 610));
            GUILayout.Label("FPS: " + fps.ToString("0") + " | exports: Docs/Benchmarks");
            GUILayout.Label("F1 Simulation " + Toggle(showSimulation) + " F2 AI " + Toggle(showAI) + " F3 Construction " + Toggle(showConstruction));
            GUILayout.Label("F4 Logistics " + Toggle(showLogistics) + " F5 Communication " + Toggle(showCommunication) + " F6 Network " + Toggle(showNetwork));
            if (showReport)
            {
                for (int i = 0; i < scenarios.Length; i++) GUILayout.Label((i + 1) + ". " + scenarios[i]);
            }
            if (showNetwork) GUILayout.Label("Network benchmarks unavailable: no runtime networking framework.");
            if (showProfiler) GUILayout.Label("Profiler metrics available in exported benchmark files after validation.");
            if (showReport) ColonyIntegrationDemoDiagnostics.DrawSceneItems("BenchmarkSuite", 5);
            GUILayout.EndArea();
        }

        private static string Toggle(bool value) => value ? "on" : "off";
    }
}
