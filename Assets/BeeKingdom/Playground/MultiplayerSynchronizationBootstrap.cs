using System;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class MultiplayerSynchronizationBootstrap : MonoBehaviour
    {
        private Camera sceneCamera;
        private bool showPackets = true;
        private bool showEntities = true;
        private bool showTicks = true;
        private bool showDiffs = true;
        private bool showNetwork = true;
        private bool showServices = true;
        private bool showDiagnostics = true;
        private bool showPerformance = true;
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
            CreatePrimitive("Unity Client", PrimitiveType.Cube, new Vector3(-6f, 0.6f, 0f), new Vector3(2.5f, 1.2f, 2.5f), new Color(0.25f, 0.55f, 0.9f));
            CreatePrimitive("Gateway Service", PrimitiveType.Cube, new Vector3(-2f, 0.6f, 0f), new Vector3(2.5f, 1.2f, 2.5f), new Color(0.35f, 0.45f, 0.62f));
            CreatePrimitive("Simulation Service", PrimitiveType.Cube, new Vector3(2f, 0.6f, 0f), new Vector3(2.5f, 1.2f, 2.5f), new Color(0.72f, 0.45f, 0.24f));
            CreatePrimitive("World / Colony Services", PrimitiveType.Cube, new Vector3(6f, 0.6f, 0f), new Vector3(2.5f, 1.2f, 2.5f), new Color(0.3f, 0.65f, 0.35f));
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
            if (Input.GetKeyDown(KeyCode.F1)) showPackets = !showPackets;
            if (Input.GetKeyDown(KeyCode.F2)) showEntities = !showEntities;
            if (Input.GetKeyDown(KeyCode.F3)) showTicks = !showTicks;
            if (Input.GetKeyDown(KeyCode.F4)) showDiffs = !showDiffs;
            if (Input.GetKeyDown(KeyCode.F5)) showNetwork = !showNetwork;
            if (Input.GetKeyDown(KeyCode.F6)) showServices = !showServices;
            if (Input.GetKeyDown(KeyCode.F7)) showDiagnostics = !showDiagnostics;
            if (Input.GetKeyDown(KeyCode.F8)) showPerformance = !showPerformance;
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
            GUI.Box(new Rect(12, 12, 450, 500), "Multiplayer Synchronization");
            GUILayout.BeginArea(new Rect(24, 40, 426, 460));
            GUILayout.Label("Backend Authoritative status: unavailable in this Unity project");
            GUILayout.Label("F1 Packets " + Toggle(showPackets) + " F2 Entities " + Toggle(showEntities) + " F3 Ticks " + Toggle(showTicks));
            GUILayout.Label("F4 Diffs " + Toggle(showDiffs) + " F5 Network " + Toggle(showNetwork) + " F6 Services " + Toggle(showServices));
            if (showNetwork)
            {
                GUILayout.Label("Connection: not connected | ping: not exposed | latency: not exposed");
                GUILayout.Label("Packets received/sent: no runtime networking framework available");
            }
            if (showTicks)
            {
                GUILayout.Label("Server tick/client tick/lag: no server runtime available");
            }
            if (showEntities)
            {
                GUILayout.Label("Synchronized entities: 0 | updates/sec: 0");
            }
            if (showServices)
            {
                GUILayout.Label("Detected specs: Gateway, Colony, Simulation, protocol/auth/account docs");
                GUILayout.Label("Detected Unity runtime: Networking assembly marker only");
            }
            if (showDiagnostics)
            {
                GUILayout.Label("No client-side simulation fallback was created.");
                GUILayout.Label("Scene exists to document readiness until server/runtime sync APIs are available.");
                ColonyIntegrationDemoDiagnostics.DrawSceneItems("MultiplayerSynchronization", 3);
            }
            if (showPerformance)
            {
                GUILayout.Label("FPS: " + fps.ToString("0") + " | render-only scene");
            }
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(new Vector3(-4.8f, 1.2f, 0f), new Vector3(-3.2f, 1.2f, 0f));
            Gizmos.DrawLine(new Vector3(-0.8f, 1.2f, 0f), new Vector3(0.8f, 1.2f, 0f));
            Gizmos.DrawLine(new Vector3(3.2f, 1.2f, 0f), new Vector3(4.8f, 1.2f, 0f));
        }

        private static string Toggle(bool value) => value ? "on" : "off";
    }
}
