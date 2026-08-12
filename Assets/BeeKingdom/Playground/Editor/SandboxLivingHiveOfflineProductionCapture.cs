using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxLivingHiveOfflineProductionCapture
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";
        private const string OutputDirectory = "C:/projets/beekingdomgame-master/Docs/Product/Evidence/LivingHiveOfflineProduction";
        private const string ManifestPath = OutputDirectory + "/LivingHiveOfflineProduction_CaptureManifest.md";
        private const string Requested = "BeeKingdom.OfflineProduction.Capture.Requested";
        private const string Frames = "BeeKingdom.OfflineProduction.Capture.Frames";
        private const string Captured = "BeeKingdom.OfflineProduction.Capture.Captured";
        private const string Index = "BeeKingdom.OfflineProduction.Capture.Index";
        private const string ConfiguredIndex = "BeeKingdom.OfflineProduction.Capture.ConfiguredIndex";

        private readonly struct CaptureSpec
        {
            public CaptureSpec(string label, string fileName, int width, int height, bool portrait, string locale, string buildingKey)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Portrait = portrait;
                Locale = locale;
                BuildingKey = buildingKey;
            }

            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly bool Portrait;
            public readonly string Locale;
            public readonly string BuildingKey;
        }

        private sealed class HonestNotConfiguredController : IHiveOfflineProductionPanelController
        {
            private readonly HiveOfflineProductionScreenModel model = HiveOfflineProductionPresentation.NotConfigured();
            public HiveOfflineProductionScreenModel Model => model;
            public bool IsConfigured => true;
            public bool IsBusy => false;
            public void Refresh() { }
            public void Collect(string buildingKey) { }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Production officielle non configurée FR", "LivingHive_OfflineProduction_NotConfigured_FR_390x844.png", 390, 844, true, "fr-CA", "honey_storage"),
            new CaptureSpec("Official production not configured EN", "LivingHive_OfflineProduction_NotConfigured_EN_1600x900.png", 1600, 900, false, "en-US", "wax_workshop")
        };

        static SandboxLivingHiveOfflineProductionCapture()
        {
            if (SessionState.GetBool(Requested, false)) Subscribe();
        }

        [MenuItem("Bee Kingdom/Playground/Capture LivingHive Offline Production Proofs")]
        public static void CaptureAndExit()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            SessionState.SetBool(Requested, true);
            SessionState.SetBool(Captured, false);
            SessionState.SetInt(Frames, 0);
            SessionState.SetInt(Index, 0);
            SessionState.SetInt(ConfiguredIndex, -1);
            Subscribe();
            PlaygroundPlayModeStartScene.UseLivingHiveOnPlay();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void Subscribe()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Requested, false) || state != PlayModeStateChange.EnteredPlayMode) return;
            ConfigureAndApplyCurrentState();
            SessionState.SetInt(Frames, 0);
            SessionState.SetBool(Captured, false);
        }

        private static void OnUpdate()
        {
            if (!SessionState.GetBool(Requested, false))
            {
                EditorApplication.update -= OnUpdate;
                return;
            }
            if (!EditorApplication.isPlaying) return;

            ConfigureAndApplyCurrentState();
            int frames = SessionState.GetInt(Frames, 0) + 1;
            SessionState.SetInt(Frames, frames);
            if (frames < 80) return;

            try
            {
                CaptureSpec spec = Current();
                string path = PathFor(spec);
                if (!SessionState.GetBool(Captured, false))
                {
                    ScreenCapture.CaptureScreenshot(path);
                    SessionState.SetBool(Captured, true);
                    return;
                }

                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    if (frames < 180) return;
                    throw new InvalidOperationException("Offline production screenshot was not written: " + path);
                }

                ValidateDimensions(path, spec.Width, spec.Height);
                int index = SessionState.GetInt(Index, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(Index, index + 1);
                    SessionState.SetInt(ConfiguredIndex, -1);
                    SessionState.SetInt(Frames, 0);
                    SessionState.SetBool(Captured, false);
                    ConfigureAndApplyCurrentState();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), new UTF8Encoding(false));
                Finish(0, "LivingHive offline production proofs captured.");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                Finish(1, "LivingHive offline production capture failed.");
            }
        }

        private static void ConfigureAndApplyCurrentState()
        {
            int index = Mathf.Clamp(SessionState.GetInt(Index, 0), 0, Captures.Length - 1);
            if (SessionState.GetInt(ConfiguredIndex, -1) == index) return;
            CaptureSpec spec = Current();
            TrySetGameViewSize(spec.Width, spec.Height, spec.Label);
            Screen.SetResolution(spec.Width, spec.Height, false);

            HiveViewProductUiPresenter.SetPlayableHiveLoopProofState("idle");
            HiveViewProductUiPresenter.SetLocaleForRuntime(spec.Locale);
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof("hive");
            HiveViewProductUiPresenter.SetMobileComfortPreferencesForProof(false, false, false);
            HiveViewProductUiPresenter.ConfigureOfflineProductionControllerForRuntime(new HonestNotConfiguredController());
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(spec.BuildingKey);
            SessionState.SetInt(ConfiguredIndex, index);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# LivingHive Offline Production - manifeste de captures");
            builder.AppendLine();
            builder.AppendLine("- Scène: `Assets/Scenes/LivingHive.unity`");
            builder.AppendLine("- État présenté: production officielle non configurée, sans taux, stock, succès ou reçu simulé");
            builder.AppendLine("- Nature de la preuve: état UI injectable de mise en page; ni session live ni données QA chiffrées");
            builder.AppendLine("- Appareil: rendu et préférences seulement; aucune ressource créditée localement");
            builder.AppendLine("- Serveur: autorité future sur UTC, catalogue, pending, capacités, révision et collecte idempotente");
            builder.AppendLine("- Collecte disponible dans ces preuves: `false`");
            builder.AppendLine("- Terrain 50x50, image de ruche et scènes modifiés: `false`");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                string path = PathFor(capture);
                Vector2Int dimensions = ReadPngDimensions(path);
                builder.AppendLine("- `" + capture.FileName + "`: `" + dimensions.x + "x" + dimensions.y + "`, locale `" + capture.Locale + "`, SHA-256 `" + Sha256(path) + "`");
            }
            return builder.ToString();
        }

        private static void ValidateDimensions(string path, int width, int height)
        {
            Vector2Int actual = ReadPngDimensions(path);
            if (actual.x != width || actual.y != height)
                throw new InvalidOperationException("Unexpected dimensions " + actual.x + "x" + actual.y + " for " + path);
        }

        private static Vector2Int ReadPngDimensions(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            var dimensions = new Vector2Int(texture.width, texture.height);
            UnityEngine.Object.DestroyImmediate(texture);
            return dimensions;
        }

        private static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static CaptureSpec Current() => Captures[Mathf.Clamp(SessionState.GetInt(Index, 0), 0, Captures.Length - 1)];
        private static string PathFor(CaptureSpec capture) => OutputDirectory + "/" + capture.FileName;
        private static void DeleteIfExists(string path) { if (File.Exists(path)) File.Delete(path); }

        private static void Finish(int exitCode, string message)
        {
            HiveViewProductUiPresenter.ResetOfflineProductionControllerForRuntime();
            SessionState.SetBool(Requested, false);
            EditorApplication.update -= OnUpdate;
            Debug.Log(message);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
        }

        private static void TrySetGameViewSize(int width, int height, string label)
        {
            Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            Type gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            Type gameViewSizeGroupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
            Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
            object sizes = singletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            object groupType = ResolveActiveGroupType(gameViewSizesType, gameViewSizeGroupType, sizes);
            object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
            object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
            object customSize = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) })
                .Invoke(new[] { fixedResolution, width, height, label });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
            int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            gameView.Show();
            gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
            gameView.Repaint();
        }

        private static object ResolveActiveGroupType(Type gameViewSizesType, Type gameViewSizeGroupType, object sizes)
        {
            BuildTarget activeTarget = EditorUserBuildSettings.activeBuildTarget;
            foreach (MethodInfo method in gameViewSizesType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "GetGroupType" || parameters.Length != 1 || parameters[0].ParameterType != typeof(BuildTarget)) continue;
                object resolved = method.Invoke(method.IsStatic ? null : sizes, new object[] { activeTarget });
                if (resolved != null && resolved.GetType() == gameViewSizeGroupType) return resolved;
            }

            string fallback;
            switch (activeTarget)
            {
                case BuildTarget.Android: fallback = "Android"; break;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64: fallback = "Standalone"; break;
                default: throw new NotSupportedException("No safe Game View size group mapping for " + activeTarget + ".");
            }
            return Enum.Parse(gameViewSizeGroupType, fallback);
        }
    }
}
