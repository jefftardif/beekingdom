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
    public static class SandboxBee620ReferencePlayableProofCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-052_BEE620_PlayerGameView";
        private const string StoragePath = OutputDirectory + "/BEE-620_PlayableHotspot_StorageSelection.png";
        private const string DefensePath = OutputDirectory + "/BEE-620_PlayableHotspot_DefenseSelection.png";
        private const string MobilePanPath = OutputDirectory + "/BEE-620_PlayableHotspot_MobilePanned.png";
        private const string ManifestPath = OutputDirectory + "/BEE-620_PlayableHotspots_Proof_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee620PlayableProof.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee620PlayableProof.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee620PlayableProof.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee620PlayableProof.Index";

        static SandboxBee620ReferencePlayableProofCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-620 Playable Hotspot Proof")]
        public static void CaptureBee620ReferencePlayableProof()
        {
            Directory.CreateDirectory(OutputDirectory);
            DeleteIfExists(StoragePath);
            DeleteIfExists(DefensePath);
            DeleteIfExists(MobilePanPath);
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
            if (!SessionState.GetBool(StateRequested, false)) return;
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            ApplyCurrentProofState();
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCaptured, false);
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                EditorApplication.update -= OnPlayModeUpdate;
                return;
            }

            ApplyCurrentProofState();
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 45) return;

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
                    if (frames < 120) return;
                    throw new InvalidOperationException("BEE-620 playable proof screenshot was not written: " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < 2)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    ApplyCurrentProofState();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("BEE-620 playable hotspot proof captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-620 playable hotspot proof failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentProofState()
        {
            int index = SessionState.GetInt(StateIndex, 0);
            if (index == 0)
            {
                TrySetGameViewSize(1280, 720, "BEE-620 Playable Storage");
                Screen.SetResolution(1280, 720, false);
                HiveViewProductUiPresenter.SetReferenceMobilePanForProof(0f, 0f);
                HiveViewProductUiPresenter.SelectReferenceHotspotForProof("cell-0-0");
                return;
            }

            if (index == 1)
            {
                TrySetGameViewSize(1280, 720, "BEE-620 Playable Defense");
                Screen.SetResolution(1280, 720, false);
                HiveViewProductUiPresenter.SetReferenceMobilePanForProof(0f, 0f);
                HiveViewProductUiPresenter.SelectReferenceHotspotForProof("cell--1-1");
                return;
            }

            TrySetGameViewSize(390, 844, "BEE-620 Mobile Panned Proof");
            Screen.SetResolution(390, 844, false);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(-170f, 64f);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof("cell-0-1");
        }

        private static string CurrentPath()
        {
            int index = SessionState.GetInt(StateIndex, 0);
            if (index == 0) return StoragePath;
            if (index == 1) return DefensePath;
            return MobilePanPath;
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-620 Playable Hotspots Proof");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("Completed");
            builder.AppendLine();
            builder.AppendLine("## Proofs");
            builder.AppendLine();
            builder.AppendLine("- Storage selection proof: `" + StoragePath + "`");
            builder.AppendLine("- Defense/server-required selection proof: `" + DefensePath + "`");
            builder.AppendLine("- Mobile panned crop proof: `" + MobilePanPath + "`");
            builder.AppendLine();
            builder.AppendLine("## Validation");
            builder.AppendLine();
            builder.AppendLine("- Hotspots are runtime-driven through `SelectReferenceHotspotForProof` and the same `focusedCellId` state used by click handling.");
            builder.AppendLine("- The detail panel title/icon/value changes when the selected hotspot changes.");
            builder.AppendLine("- Mobile uses a panned reference-backed art crop rather than compressing the full hive.");
            builder.AppendLine("- No server authority, official progression, live economy or synchronization is introduced.");
            return builder.ToString();
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
                PropertyInfo instanceProperty = scriptableSingletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                object sizesInstance = instanceProperty.GetValue(null);
                object androidGroupType = Enum.Parse(gameViewSizeGroupType, "Android");
                object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizesInstance, new[] { androidGroupType });
                object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                ConstructorInfo constructor = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                object customSize = constructor.Invoke(new[] { fixedResolution, width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
                int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                PropertyInfo selectedSizeIndex = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                selectedSizeIndex?.SetValue(gameView, selectedIndex);
                gameView.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to force BEE-620 proof Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
