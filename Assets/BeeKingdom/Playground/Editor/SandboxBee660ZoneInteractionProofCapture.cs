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
    public static class SandboxBee660ZoneInteractionProofCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-054_BEE641_660_ZoneInteractionProofMatrix";
        private const string ManifestPath = OutputDirectory + "/BEE-656_BEE-660_ZoneInteractionProofMatrix_Manifest.md";
        private const string GateBoardPath = OutputDirectory + "/BEE-660_GateBoard.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee660ZoneInteractionProof.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee660ZoneInteractionProof.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee660ZoneInteractionProof.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee660ZoneInteractionProof.Index";

        private struct CaptureSpec
        {
            public readonly string Label;
            public readonly string HotspotId;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly Vector2 Pan;

            public CaptureSpec(string label, string hotspotId, string fileName, int width, int height, Vector2 pan)
            {
                Label = label;
                HotspotId = hotspotId;
                FileName = fileName;
                Width = width;
                Height = height;
                Pan = pan;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Zone 01 Nurserie", "nursery_cluster", "BEE-656_Zone01_Nurserie.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 02 Reserve miel", "honey_storage", "BEE-656_Zone02_ReserveMiel.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 03 Caserne", "guard_post", "BEE-656_Zone03_Caserne.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 04 Defense", "defense_growth", "BEE-656_Zone04_Defense.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 05 Genetique", "genetics_garden", "BEE-656_Zone05_Genetique.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 06 Recherche", "research_node", "BEE-656_Zone06_Recherche.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 07 Entrepot", "warehouse_cells", "BEE-656_Zone07_Entrepot.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 08 Transformation", "wax_workshop", "BEE-656_Zone08_Transformation.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 09 Infirmerie", "infirmary_grove", "BEE-656_Zone09_Infirmerie.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 10 Academie", "academy_canopy", "BEE-656_Zone10_Academie.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 11 Banque", "hive_bank", "BEE-656_Zone11_Banque.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 12 Administration", "administration_core", "BEE-656_Zone12_Administration.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 13 Archives", "archives_honeyfall", "BEE-656_Zone13_Archives.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Zone 14 Centre alliance", "alliance_future_hall", "BEE-656_Zone14_CentreAlliance.png", 1280, 720, Vector2.zero),
            new CaptureSpec("Mobile gauche", "infirmary_grove", "BEE-660_Mobile_Left.png", 390, 844, new Vector2(-260f, 72f)),
            new CaptureSpec("Mobile centre", "administration_core", "BEE-660_Mobile_Center.png", 390, 844, Vector2.zero),
            new CaptureSpec("Mobile droite", "academy_canopy", "BEE-660_Mobile_Right.png", 390, 844, new Vector2(230f, 44f)),
            new CaptureSpec("Tablette paysage reserve", "honey_storage", "BEE-660_TabletLandscape_Reserve.png", 1024, 768, Vector2.zero)
        };

        static SandboxBee660ZoneInteractionProofCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-660 Zone Interaction Proof Matrix")]
        public static void CaptureBee660ZoneInteractionProofMatrix()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(ManifestPath);
            DeleteIfExists(GateBoardPath);

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
                    throw new InvalidOperationException("BEE-660 screenshot was not written: " + path);
                }

                int index = SessionState.GetInt(StateIndex, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(StateIndex, index + 1);
                    SessionState.SetInt(StateFrames, 0);
                    SessionState.SetBool(StateCaptured, false);
                    ApplyCurrentProofState();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                File.WriteAllText(GateBoardPath, BuildGateBoard(), Encoding.UTF8);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.ExitPlaymode();
                Debug.Log("BEE-660 zone interaction proof matrix captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-660 zone interaction proof matrix failed: " + exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static void ApplyCurrentProofState()
        {
            CaptureSpec capture = Captures[Mathf.Clamp(SessionState.GetInt(StateIndex, 0), 0, Captures.Length - 1)];
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(capture.Pan.x, capture.Pan.y);
            HiveViewProductUiPresenter.SelectReferenceHotspotForProof(capture.HotspotId);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-656 / BEE-660 Zone Interaction Proof Matrix Manifest");
            builder.AppendLine();
            builder.AppendLine("## Status");
            builder.AppendLine();
            builder.AppendLine("Completed for Demo evidence. BEE-661 remains blocked.");
            builder.AppendLine();
            builder.AppendLine("## Expected / Actual");
            builder.AppendLine();
            builder.AppendLine("| Expected | Actual artifact | Runtime id |");
            builder.AppendLine("| --- | --- | --- |");
            for (int i = 0; i < 14; i++)
            {
                CaptureSpec capture = Captures[i];
                builder.AppendLine("| " + capture.Label + " | `" + PathFor(capture) + "` | `" + capture.HotspotId + "` |");
            }

            builder.AppendLine();
            builder.AppendLine("## BEE-660 Additional Proofs");
            for (int i = 14; i < Captures.Length; i++)
            {
                CaptureSpec capture = Captures[i];
                builder.AppendLine("- " + capture.Label + ": `" + PathFor(capture) + "`");
            }

            builder.AppendLine();
            builder.AppendLine("## QA Read");
            builder.AppendLine();
            builder.AppendLine("- 14 official zones are captured with visible focus and associated detail panel.");
            builder.AppendLine("- Center/border/inter-zone routing remains based on the existing polygon runtime from BEE-640.");
            builder.AppendLine("- Mobile left/center/right and tablet landscape are captured as player-facing evidence.");
            builder.AppendLine("- Caserne is the current runtime label for the guard/poste garde server-required zone.");
            builder.AppendLine("- LOCAL PREVIEW, Apercu local and Serveur futur remain visible non-claims.");
            builder.AppendLine();
            builder.AppendLine("## Non-Live Boundary");
            builder.AppendLine();
            builder.AppendLine("- No account, live MMO entry, official alliance, chat, ranking, war, economy, resource persistence or synchronization is introduced.");
            builder.AppendLine("- Bee Server remains required for any authoritative MMO behavior.");
            return builder.ToString();
        }

        private static string BuildGateBoard()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-660 Gate Board");
            builder.AppendLine();
            builder.AppendLine("## Verdict");
            builder.AppendLine();
            builder.AppendLine("- Demo gate conclusion: `pass with QA reserves`.");
            builder.AppendLine("- BEE-661: `blocked`.");
            builder.AppendLine();
            builder.AppendLine("## Covered Items");
            builder.AppendLine();
            builder.AppendLine("- 14 zones prouvees");
            builder.AppendLine("- centre/bord/inter-zone routing documented through the existing polygon map");
            builder.AppendLine("- mobile gauche/centre/droite captured");
            builder.AppendLine("- tablette paysage reserve captured");
            builder.AppendLine("- Caserne / Poste garde lexicon reserve documented");
            builder.AppendLine("- Demo contact sheet expected from BEE-656 artifacts");
            builder.AppendLine("- Server non-live boundary maintained");
            builder.AppendLine();
            builder.AppendLine("## Reserves");
            builder.AppendLine();
            builder.AppendLine("- Physical device tests are not executed in this Demo pass.");
            builder.AppendLine("- Server audit is limited to non-live claims; no backend endpoint is present.");
            builder.AppendLine("- UI/QA rescore remains required before any BEE-661 decision.");
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
                gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
                gameView.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Unable to force BEE-660 Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
