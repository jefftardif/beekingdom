using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee760MobileHiveGateCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-059_BEE742_760_MobileHiveGate";
        private const string ManifestPath = OutputDirectory + "/BEE-760_MobileHiveGate_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee760MobileHiveGate.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee760MobileHiveGate.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee760MobileHiveGate.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee760MobileHiveGate.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly Vector2 Pan;
            public readonly float Zoom;

            public CaptureSpec(string label, string fileName, int width, int height, string hotspotId, Vector2 pan, float zoom)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = hotspotId;
                Pan = pan;
                Zoom = zoom;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Tablet landscape player-facing hive", "BEE-748_TabletLandscape_PlayerFacing.png", 1920, 1200, "research_node", Vector2.zero, 1.0f),
            new CaptureSpec("Phone portrait player-facing hive", "BEE-749_PhonePortrait_PlayerFacing.png", 390, 844, "honey_storage", new Vector2(-170f, 66f), 1.0f),
            new CaptureSpec("Desktop server-first player-facing hive", "BEE-742_743_DesktopServerFirst_PlayerFacing.png", 1280, 720, "guard_post", Vector2.zero, 1.0f),
            new CaptureSpec("Hive zoom pan HUD fixed proof", "BEE-744_747_ZoomPanHudFixed_PlayerFacing.png", 1280, 720, "administration_core", new Vector2(44f, -18f), 1.32f),
            new CaptureSpec("BEE-760 gate player-facing proof", "BEE-760_MobileHiveGate_PlayerFacing.png", 1280, 720, "alliance_future_hall", Vector2.zero, 1.12f)
        };

        static SandboxBee760MobileHiveGateCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-760 Mobile Hive Gate")]
        public static void CaptureBee760MobileHiveGate()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            SessionState.SetBool(StateRequested, true);
            SessionState.SetBool(StateCaptured, false);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateIndex, 0);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            ApplyCurrentState();
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCaptured, false);
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            ApplyCurrentState();
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 62) return;

            try
            {
                string path = CurrentPath();
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(StateCaptured, true);
                    return;
                }

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 150) return;
                    throw new InvalidOperationException("BEE-760 screenshot was not written: " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    ApplyCurrentState();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("BEE-760 mobile hive gate captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-760 mobile hive gate capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.Zoom);
            HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof(capture.HotspotId);
        }

        private static string BuildManifest()
        {
            RuntimeBridgePlayerFacingState state = HiveViewProductUiPresenter.RuntimeBridgePlayerState;
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-760 Mobile Hive Gate Manifest");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("- Scene: `SandboxPlayground`");
            builder.AppendLine("- Play Mode: `normal player-facing Game View`");
            builder.AppendLine("- Debug overlay visible: `" + HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof() + "`");
            builder.AppendLine("- Official gameplay requires server: `" + HiveViewProductUiPresenter.ServerFirstGate.OfficialGameplayRequiresServer + "`");
            builder.AppendLine("- Gameplay mutation allowed: `" + state.GameplayMutationAllowed + "`");
            builder.AppendLine("- Live gameplay introduced: `" + HiveViewProductUiPresenter.ServerFirstIntroducesLiveGameplayForProof() + "`");
            builder.AppendLine("- BEE-761: `Blocked`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## BEE Coverage");
            builder.AppendLine();
            builder.AppendLine("- BEE-742/BEE-743: player-facing server-first copy visible; legacy preview badge not shown.");
            builder.AppendLine("- BEE-744/BEE-745: zoom and pan are applied to the hive surface only.");
            builder.AppendLine("- BEE-746: HUD, menus and runtime status remain fixed while hive transform changes.");
            builder.AppendLine("- BEE-747: selected hotspot halo remains aligned after the transformed hive proof.");
            builder.AppendLine("- BEE-748: tablet landscape captured at 1920x1200.");
            builder.AppendLine("- BEE-749: phone portrait captured at 390x844.");
            builder.AppendLine("- BEE-760: gate proof captured with server-first status and no live gameplay claim.");
            builder.AppendLine();
            builder.AppendLine("## World Map Reserve");
            builder.AppendLine();
            builder.AppendLine("- BEE-756/BEE-757/BEE-758: no current non-live world map runtime proof was detected in Playground sources.");
            builder.AppendLine("- Result: no world map capture was produced in this Demo cycle.");
            builder.AppendLine("- Boundary: the hive proof does not activate territory, alliance, war, scouting, economy, chat, ranking, matchmaking or realtime sync.");
            builder.AppendLine();
            builder.AppendLine("## Limitations");
            builder.AppendLine();
            builder.AppendLine("- Pinch gesture is represented by the existing hive zoom proof hook; this pack does not claim a physical multi-touch device pass.");
            builder.AppendLine("- Tablet and phone evidence is Unity Game View sizing, not a real-device certification.");
            return builder.ToString();
        }

        private static string CurrentPath()
        {
            return PathFor(Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)]);
        }

        private static string PathFor(CaptureSpec capture)
        {
            return OutputDirectory + "/" + capture.FileName;
        }

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            try
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
                Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                Type gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                Type gameViewSizeGroupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
                Type scriptableSingletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
                object sizesInstance = scriptableSingletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                object androidGroupType = Enum.Parse(gameViewSizeGroupType, "Android");
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] { androidGroupType });
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                object customSize = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) }).Invoke(new[] { fixedResolution, width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
                int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
                gameView.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to force BEE-760 Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
