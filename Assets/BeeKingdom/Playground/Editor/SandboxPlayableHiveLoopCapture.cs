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
    public static class SandboxPlayableHiveLoopCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-064_PlayableHiveLoop";
        private const string ManifestPath = OutputDirectory + "/PlayableHiveLoop_Manifest.md";
        private const string ReportPath = "C:/projets/beekingdom/prompts_codex/rapports/BuilderA_PlayableHiveLoop_Report.md";
        private const string StateRequested = "BeeKingdom.Playground.PlayableHiveLoop.Requested";
        private const string StateFrames = "BeeKingdom.Playground.PlayableHiveLoop.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.PlayableHiveLoop.Captured";
        private const string StateIndex = "BeeKingdom.Playground.PlayableHiveLoop.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string HotspotId;
            public readonly string LoopState;
            public readonly Vector2 Pan;
            public readonly float Zoom;

            public CaptureSpec(string label, string fileName, int width, int height, string hotspotId, string loopState, Vector2 pan, float zoom)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                HotspotId = hotspotId;
                LoopState = loopState;
                Pan = pan;
                Zoom = zoom;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Tablette paysage - ruche jouable locale", "PlayableHive_TabletLandscape_1920x1200.png", 1920, 1200, "honey_storage", "resources_tick", Vector2.zero, 1.05f),
            new CaptureSpec("Desktop Game View - etat initial preview", "PlayableHive_Initial_1280x720.png", 1280, 720, "honey_storage", "idle", Vector2.zero, 1.08f),
            new CaptureSpec("Desktop Game View - ressources en hausse", "PlayableHive_ResourcesTick_1280x720.png", 1280, 720, "honey_storage", "resources_tick", Vector2.zero, 1.08f),
            new CaptureSpec("Desktop Game View - amelioration en cours", "PlayableHive_UpgradeRunning_1280x720.png", 1280, 720, "honey_storage", "upgrade_running", new Vector2(24f, -8f), 1.12f),
            new CaptureSpec("Desktop Game View - amelioration terminee", "PlayableHive_UpgradeDone_1280x720.png", 1280, 720, "honey_storage", "upgrade_done", new Vector2(24f, -8f), 1.12f),
            new CaptureSpec("Desktop Game View - entrainement en cours", "PlayableHive_TrainingRunning_1280x720.png", 1280, 720, "guard_post", "training_running", new Vector2(-18f, 10f), 1.12f),
            new CaptureSpec("Desktop Game View - entrainement termine", "PlayableHive_TrainingDone_1280x720.png", 1280, 720, "guard_post", "training_done", new Vector2(-18f, 10f), 1.12f),
            new CaptureSpec("Telephone portrait - ruche jouable locale", "PlayableHive_PhonePortrait_390x844.png", 390, 844, "guard_post", "training_running", new Vector2(-160f, 48f), 1.16f)
        };

        static SandboxPlayableHiveLoopCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture Playable Hive Loop")]
        public static void CapturePlayableHiveLoop()
        {
            Directory.CreateDirectory(OutputDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            DeleteIfExists(ReportPath);
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
            if (frames < 70) return;

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
                    if (frames < 160) return;
                    throw new InvalidOperationException("Playable Hive Loop screenshot was not written: " + path);
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
                File.WriteAllText(ReportPath, BuildReport(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("Playable Hive Loop captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("Playable Hive Loop capture failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SetRuntimeBridgeModeForProof(RuntimeBridgePlayerMode.ServerPreparation);
            HiveViewProductUiPresenter.SetProductionReducedMotionForProof(false);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(capture.Zoom);
            HiveViewProductUiPresenter.TriggerProductionFeedbackPulseForProof(capture.HotspotId);
            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState(capture.LoopState);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Playable Hive Loop Manifest");
            builder.AppendLine();
            builder.AppendLine("## Statut");
            builder.AppendLine();
            builder.AppendLine("- Vue: `SandboxPlayground` / Ruche joueur");
            builder.AppendLine("- Surface carte monde prioritaire: `false`");
            builder.AppendLine("- Simulation locale de demonstration: `true`");
            builder.AppendLine("- Progression serveur officielle: `false`");
            builder.AppendLine("- Sauvegarde active: `false`");
            builder.AppendLine("- Debug overlay visible: `" + HiveViewProductUiPresenter.PlayerViewDebugOverlayVisibleForProof() + "`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## Etat runtime");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.PlayableHiveLoopStateForProof()) builder.AppendLine("- " + row);
            builder.AppendLine();
            builder.AppendLine("## Gestes ruche");
            builder.AppendLine();
            foreach (string row in HiveViewProductUiPresenter.ReferenceHiveGestureTelemetryForProof()) builder.AppendLine("- " + row);
            return builder.ToString();
        }

        private static string BuildReport()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Builder-A Playable Hive Loop Report");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("* Completed with recommendations");
            builder.AppendLine();
            builder.AppendLine("## Resume");
            builder.AppendLine();
            builder.AppendLine("La priorite Builder-A a ete recentree sur la Ruche. Une boucle jouable minimale locale a ete ajoutee: ressources dynamiques, selection de zone, amelioration preview, entrainement preview et feedback immediat.");
            builder.AppendLine();
            builder.AppendLine("## Rapport bouton par bouton");
            builder.AppendLine();
            builder.AppendLine("* Ameliorer: affiche cout, duree, etat en amelioration, progression et niveau final en simulation locale.");
            builder.AppendLine("* Entrainer Soldats: affiche cout, progression et compteur Soldats preview.");
            builder.AppendLine("* Entrainer Gardiennes: affiche cout, progression et compteur Gardiennes preview.");
            builder.AppendLine("* Entrainer Eclaireuses: affiche cout, progression et compteur Eclaireuses preview.");
            builder.AppendLine("* Navigation Ruche/Monde: conserve la Ruche comme priorite; Monde reste non-live et isole.");
            builder.AppendLine();
            builder.AppendLine("## Fichiers crees");
            builder.AppendLine();
            builder.AppendLine("* C:/projets/beekingdomgame-master/Assets/BeeKingdom/Playground/Editor/SandboxPlayableHiveLoopCapture.cs");
            builder.AppendLine();
            builder.AppendLine("## Fichiers modifies");
            builder.AppendLine();
            builder.AppendLine("* C:/projets/beekingdomgame-master/Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs");
            builder.AppendLine();
            builder.AppendLine("## Limites non-live");
            builder.AppendLine();
            builder.AppendLine("* Aucune sauvegarde serveur.");
            builder.AppendLine("* Aucune economie officielle.");
            builder.AppendLine("* Aucune armee serveur, combat, alliance, chat ou classement live.");
            builder.AppendLine("* Les ressources, niveaux et troupes sont une simulation locale de demonstration.");
            builder.AppendLine();
            builder.AppendLine("## Preuves");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("* " + capture.Label + ": `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## Ready for next brick");
            builder.AppendLine();
            builder.AppendLine("YES, cote Ruche preview locale uniquement.");
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

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
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
                Debug.LogWarning("Unable to force Playable Hive Loop Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }
    }
}
