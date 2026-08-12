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
    public static class SandboxLivingHiveStrategicPathCapture
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";
        private const string OutputDirectory = "C:/projets/beekingdomgame-master/Docs/Product/Evidence/LivingHiveStrategicPath";
        private const string ManifestPath = OutputDirectory + "/LivingHiveStrategicPath_CaptureManifest.md";
        private const string Requested = "BeeKingdom.StrategicPath.Capture.Requested";
        private const string Frames = "BeeKingdom.StrategicPath.Capture.Frames";
        private const string Captured = "BeeKingdom.StrategicPath.Capture.Captured";
        private const string Index = "BeeKingdom.StrategicPath.Capture.Index";
        private const string ConfiguredIndex = "BeeKingdom.StrategicPath.Capture.ConfiguredIndex";

        private readonly struct CaptureSpec
        {
            public CaptureSpec(string label, string fileName, int width, int height, bool portrait, bool trial, bool doctrine, bool formation, bool recruitment, bool composition = false)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Portrait = portrait;
                Trial = trial;
                Doctrine = doctrine;
                Formation = formation;
                Recruitment = recruitment;
                Composition = composition;
            }

            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly bool Portrait;
            public readonly bool Trial;
            public readonly bool Doctrine;
            public readonly bool Formation;
            public readonly bool Recruitment;
            public readonly bool Composition;
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Voie Nourriciere FR", "LivingHive_StrategicPath_Nurturer_FR_390x844.png", 390, 844, true, false, false, false, false),
            new CaptureSpec("Essai Nourriciere FR", "LivingHive_StrategicTrial_Nurturer_FR_390x844.png", 390, 844, true, true, false, false, false),
            new CaptureSpec("Doctrine Gardiennes FR", "LivingHive_CombatDoctrine_GuardiansVsDarters_FR_390x844.png", 390, 844, true, false, true, false, false),
            new CaptureSpec("Preparation Gardiennes FR", "LivingHive_FormationReadiness_GuardiansVsDarters_FR_390x844.png", 390, 844, true, false, false, true, false),
            new CaptureSpec("Recrutement Voltigeuses FR", "LivingHive_DoctrineRecruitment_Wingrunners_FR_390x844.png", 390, 844, true, false, false, false, true),
            new CaptureSpec("Composition mixte FR", "LivingHive_SquadComposition_Mixed_FR_390x844.png", 390, 844, true, false, false, false, false, true),
            new CaptureSpec("Scout path EN", "LivingHive_StrategicPath_Scout_EN_1600x900.png", 1600, 900, false, false, false, false, false),
            new CaptureSpec("Scout trial EN", "LivingHive_StrategicTrial_Scout_EN_1600x900.png", 1600, 900, false, true, false, false, false),
            new CaptureSpec("Wingrunners doctrine EN", "LivingHive_CombatDoctrine_WingrunnersVsGuardians_EN_1600x900.png", 1600, 900, false, false, true, false, false),
            new CaptureSpec("Guardian readiness EN", "LivingHive_FormationReadiness_GuardiansVsWingrunners_EN_1600x900.png", 1600, 900, false, false, false, true, false),
            new CaptureSpec("Darters recruitment EN", "LivingHive_DoctrineRecruitment_Darters_EN_1600x900.png", 1600, 900, false, false, false, false, true),
            new CaptureSpec("Mixed squad composition EN", "LivingHive_SquadComposition_Mixed_EN_1600x900.png", 1600, 900, false, false, false, false, false, true)
        };

        static SandboxLivingHiveStrategicPathCapture()
        {
            if (SessionState.GetBool(Requested, false)) Subscribe();
        }

        [MenuItem("Bee Kingdom/Playground/Capture LivingHive Strategic Path Proofs")]
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
                    throw new InvalidOperationException("Strategic path screenshot was not written: " + path);
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

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                Finish(0, "LivingHive strategic path proofs captured.");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                Finish(1, "LivingHive strategic path capture failed.");
            }
        }

        private static void ConfigureAndApplyCurrentState()
        {
            int index = Mathf.Clamp(SessionState.GetInt(Index, 0), 0, Captures.Length - 1);
            if (SessionState.GetInt(ConfiguredIndex, -1) == index) return;
            CaptureSpec spec = Current();
            TrySetGameViewSize(spec.Width, spec.Height, spec.Label);
            Screen.SetResolution(spec.Width, spec.Height, false);
            if (spec.Composition) HiveViewProductUiPresenter.PrepareSquadCompositionCaptureForProof(spec.Portrait);
            else if (spec.Recruitment) HiveViewProductUiPresenter.PrepareDoctrineRecruitmentCaptureForProof(spec.Portrait);
            else if (spec.Formation) HiveViewProductUiPresenter.PrepareFormationReadinessCaptureForProof(spec.Portrait);
            else if (spec.Doctrine) HiveViewProductUiPresenter.PrepareCombatDoctrineCaptureForProof(spec.Portrait);
            else if (spec.Trial) HiveViewProductUiPresenter.PrepareStrategicPathTrialCaptureForProof(spec.Portrait);
            else HiveViewProductUiPresenter.PrepareStrategicPathCaptureForProof(spec.Portrait);
            SessionState.SetInt(ConfiguredIndex, index);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# LivingHive Strategic Path - manifeste de captures");
            builder.AppendLine();
            builder.AppendLine("- Scene: `Assets/Scenes/LivingHive.unity`");
            builder.AppendLine("- Portrait: voie Nourriciere, francais, 390x844");
            builder.AppendLine("- Paysage: voie Scout, anglais, 1600x900");
            builder.AppendLine("- Essais tactiques: Nourriciere FR et Scout EN, reponse identitaire selectionnee");
            builder.AppendLine("- Doctrine de combat: Gardiennes > Lanceuses en FR; Wingrunners > Guardians en EN");
            builder.AppendLine("- Preparation d'escouade: composition mixte locale bornee a 12; controles -/+ et suggestion doctrinale; Soldats/Eclaireuses hors doctrine");
            builder.AppendLine("- Familles tactiques: `guardians,wingrunners,darters`; catalogue `phase4-combat-v1`");
            builder.AppendLine("- Cycle: `guardians>darters>wingrunners>guardians`; aucun coefficient ni victoire garantie");
            builder.AppendLine("- Cinq voies: `royal_guard,striker,nurturer,scout,alchemist`");
            builder.AppendLine("- Neutral selectable: `false`; unlock level: `10`");
            builder.AppendLine("- Tap: apercu en memoire seulement");
            builder.AppendLine("- Selection officielle et bonus: `false`; serveur requis");
            builder.AppendLine("- Essai: etat volatile en memoire, aucune persistance serveur ni mutation de gameplay");
            builder.AppendLine("- Doctrine: etat volatile en memoire, aucun combat simule ni mutation de gameplay");
            builder.AppendLine("- Preparation: brouillon volatile; aucune reservation locale, composition officielle, marche ou combat; serveur requis");
            builder.AppendLine("- Reservation officielle: contrat prepare et feature fermee; bouton mobile desactive tant que session/transport ne sont pas raccordes");
            builder.AppendLine("- Recrutement: file locale de Caserne, roster doctrinal separe; aucun effectif legacy converti");
            builder.AppendLine("- Appareil: rendu, langue et option inspectee seulement");
            builder.AppendLine("- Terrain 50x50, image de ruche et scenes modifies: `false`");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                string path = PathFor(capture);
                Vector2Int dimensions = ReadPngDimensions(path);
                builder.AppendLine("- `" + capture.FileName + "`: `" + dimensions.x + "x" + dimensions.y + "`, locale `" + (capture.Portrait ? "fr-CA" : "en-US") + "`, SHA-256 `" + Sha256(path) + "`");
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
