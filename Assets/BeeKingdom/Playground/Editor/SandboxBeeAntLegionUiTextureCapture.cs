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
    public static class SandboxBeeAntLegionUiTextureCapture
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";
        private const string OutputDirectory = "Artifacts/HiveUiAntLegionTexture";
        private const string ManifestPath = OutputDirectory + "/HiveUiAntLegionTextureManifest.md";
        private const string StateRequested = "BeeKingdom.Playground.AntLegionTextureUi.Requested";
        private const string StateFrames = "BeeKingdom.Playground.AntLegionTextureUi.Frames";
        private const string StateIndex = "BeeKingdom.Playground.AntLegionTextureUi.Index";
        private const string StateCaptured = "BeeKingdom.Playground.AntLegionTextureUi.Captured";
        private const string StateExitWhenDone = "BeeKingdom.Playground.AntLegionTextureUi.ExitWhenDone";
        private static double captureReadyAt;
        private static double screenshotRequestedAt;

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly string OpenMenu;

            public CaptureSpec(string label, string fileName, string openMenu)
            {
                Label = label;
                FileName = fileName;
                OpenMenu = openMenu;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("HUD complet ruche, sans panneau batiment", "HiveUi_AntLegionTexture_BaseHud_1920x1080.png", string.Empty),
            new CaptureSpec("Menu profil au-dessus des files d'attente", "HiveUi_AntLegionTexture_PlayerMenu_1920x1080.png", "player"),
            new CaptureSpec("Menu VIP premium ouvert", "HiveUi_AntLegionTexture_VipMenu_1920x1080.png", "vip"),
            new CaptureSpec("Menu puissance premium ouvert", "HiveUi_AntLegionTexture_PowerMenu_1920x1080.png", "power")
        };

        static SandboxBeeAntLegionUiTextureCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            captureReadyAt = EditorApplication.timeSinceStartup + 3.5d;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture Ant Legion Texture UI Proof")]
        public static void CaptureAntLegionTextureUiProof()
        {
            SessionState.SetBool(StateExitWhenDone, false);
            StartCapture();
        }

        public static void CaptureForBatch()
        {
            SessionState.SetBool(StateExitWhenDone, true);
            StartCapture();
        }

        public static void CaptureForAutomatedEditor()
        {
            SessionState.SetBool(StateExitWhenDone, true);
            StartCapture();
        }

        private static void StartCapture()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(OutputDirectory + "/HiveUi_AntLegionTexture_BaseHud_1280x720.png");
            DeleteIfExists(OutputDirectory + "/HiveUi_AntLegionTexture_VipMenu_1280x720.png");
            DeleteIfExists(OutputDirectory + "/HiveUi_AntLegionTexture_PowerMenu_1280x720.png");
            DeleteIfExists(ManifestPath);

            SessionState.SetBool(StateRequested, true);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetInt(StateIndex, 0);
            SessionState.SetBool(StateCaptured, false);
            captureReadyAt = EditorApplication.timeSinceStartup + 3.5d;
            screenshotRequestedAt = 0d;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;

            PlaygroundPlayModeStartScene.UseLivingHiveOnPlay();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            ApplyCurrentState();
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCaptured, false);
            captureReadyAt = EditorApplication.timeSinceStartup + 3.5d;
            screenshotRequestedAt = 0d;
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            if (!Application.isPlaying || EditorApplication.timeSinceStartup < captureReadyAt) return;

            ApplyCurrentState();
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 80) return;

            try
            {
                string path = CurrentPath();
                if (!SessionState.GetBool(StateCaptured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(StateCaptured, true);
                    screenshotRequestedAt = EditorApplication.timeSinceStartup;
                    return;
                }

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (EditorApplication.timeSinceStartup - screenshotRequestedAt < 4d) return;
                    throw new InvalidOperationException("Screenshot was not written: " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    captureReadyAt = EditorApplication.timeSinceStartup + 1.2d;
                    screenshotRequestedAt = 0d;
                    ApplyCurrentState();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("Ant Legion texture UI proof captured in " + OutputDirectory);
                if (SessionState.GetBool(StateExitWhenDone, false)) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("Ant Legion texture UI proof failed: " + exception);
                if (SessionState.GetBool(StateExitWhenDone, false)) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            Screen.SetResolution(1920, 1080, false);
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(true);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(1.05f);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(0f, 0f);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.ResetAntLegionHudForProof();
            SetStaticBool("referenceHotspotSelected", false);
            SetStaticBool("detailPanelClosed", true);
            SetStaticBool("playerMenuOpen", capture.OpenMenu == "player");
            SetStaticBool("vipMenuOpen", capture.OpenMenu == "vip");
            SetStaticBool("powerMenuOpen", capture.OpenMenu == "power");
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Hive UI Ant Legion Texture Proof");
            builder.AppendLine();
            builder.AppendLine("- Scene: `" + ScenePath + "`");
            builder.AppendLine("- Resolution: `1920x1080` (Full HD)");
            builder.AppendLine("- Building detail panel visible by default: `false`");
            builder.AppendLine("- Orange square frame pass: `replaced with darker textured panels, fine gold accents, corner caps`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            return builder.ToString();
        }

        private static void SetStaticBool(string fieldName, bool value)
        {
            FieldInfo field = typeof(HiveViewProductUiPresenter).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null) field.SetValue(null, value);
        }

        private static string CurrentPath()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            return PathFor(capture);
        }

        private static string PathFor(CaptureSpec capture)
        {
            return OutputDirectory + "/" + capture.FileName;
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
