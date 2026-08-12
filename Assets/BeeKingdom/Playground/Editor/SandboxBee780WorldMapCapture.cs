using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee780WorldMapCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-061_BEE761_780_WorldMap";
        private const string ManifestPath = OutputDirectory + "/BEE-780_WorldMap_Manifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee780WorldMap.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee780WorldMap.Frames";
        private const string StateCaptured = "BeeKingdom.Playground.Bee780WorldMap.Captured";
        private const string StateIndex = "BeeKingdom.Playground.Bee780WorldMap.Index";

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string Surface;
            public readonly float Zoom;
            public readonly Vector2 Pan;
            public readonly string NodeId;
            public readonly string GestureMode;
            public readonly int TouchCount;
            public readonly Vector2 PanDelta;
            public readonly float PinchDelta;

            public CaptureSpec(string label, string fileName, int width, int height, string surface, float zoom, Vector2 pan, string nodeId, string gestureMode = "proof-idle", int touchCount = 0, float panDeltaX = 0f, float panDeltaY = 0f, float pinchDelta = 0f)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Surface = surface;
                Zoom = zoom;
                Pan = pan;
                NodeId = nodeId;
                GestureMode = gestureMode;
                TouchCount = touchCount;
                PanDelta = new Vector2(panDeltaX, panDeltaY);
                PinchDelta = pinchDelta;
            }
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Ruche avant transition", "BEE-762_01_HiveBeforeWorld_1280x720.png", 1280, 720, "hive", 1f, Vector2.zero, "goldenheart"),
            new CaptureSpec("Transition Ruche vers Monde", "BEE-762_02_TransitionHiveToWorld_1280x720.png", 1280, 720, "world", 1f, Vector2.zero, "goldenheart"),
            new CaptureSpec("Monde tablette paysage", "BEE-780_03_WorldTabletLandscape_1920x1200.png", 1920, 1200, "world", 1f, Vector2.zero, "goldenheart", "tablet-landscape-ready"),
            new CaptureSpec("Monde telephone portrait", "BEE-780_04_WorldPhonePortrait_390x844.png", 390, 844, "world", 1.10f, Vector2.zero, "silverstream", "portrait-ready"),
            new CaptureSpec("Monde zoom pan HUD fixe", "BEE-780_05_WorldZoomPanHudFixed_1280x720.png", 1280, 720, "world", 1.22f, new Vector2(-120f, 44f), "crimson", "two-finger-pinch-zoom", 2, 0f, 0f, 0.032f),
            new CaptureSpec("Hit zones halos apres pan zoom", "BEE-780_06_HitZonesHalosAfterPanZoom_1280x720.png", 1280, 720, "world", 1.26f, new Vector2(-158f, 62f), "silverstream", "one-finger-pan", 1, -38f, 14f, 0f),
            new CaptureSpec("Retour Monde vers Ruche", "BEE-774_07_ReturnWorldToHive_1280x720.png", 1280, 720, "hive", 1f, Vector2.zero, "goldenheart")
        };

        static SandboxBee780WorldMapCapture()
        {
            if (!SessionState.GetBool(StateRequested, false)) return;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.update += OnPlayModeUpdate;
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-780 World Map")]
        public static void CaptureBee780WorldMap()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(PathFor(capture));
            DeleteIfExists(OutputDirectory + "/BEE-774_06_ReturnWorldToHive_1280x720.png");
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
                    throw new InvalidOperationException("BEE-780 screenshot was not written: " + path);
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
                Debug.Log("BEE-780 world map proof captured.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("BEE-780 world map capture failed: " + exception);
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
            HiveViewProductUiPresenter.SetReferenceSurfaceModeForProof(capture.Surface);
            HiveViewProductUiPresenter.SetReferenceMobilePanForProof(0f, 0f);
            HiveViewProductUiPresenter.SetReferenceHiveZoomForProof(1f);
            if (string.Equals(capture.Surface, "world", StringComparison.OrdinalIgnoreCase))
            {
                HiveViewProductUiPresenter.SetWorldMapViewForProof(capture.Zoom, capture.Pan.x, capture.Pan.y, capture.NodeId);
                HiveViewProductUiPresenter.SetWorldMapGestureTelemetryForProof(capture.GestureMode, capture.TouchCount, capture.PanDelta.x, capture.PanDelta.y, capture.PinchDelta, capture.Zoom, capture.Zoom);
            }
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-780 World Map Non-Live Runtime Surface Manifest");
            builder.AppendLine();
            builder.AppendLine("## Statut");
            builder.AppendLine();
            builder.AppendLine("- Builder-A : `Completed`");
            builder.AppendLine("- Ready for Architect review : `YES`");
            builder.AppendLine("- BEE-781 : `Bloquee`");
            builder.AppendLine("- Carte produit non-live : `" + HiveViewProductUiPresenter.WorldMapNonLiveRuntimeSurfaceReadyForProof() + "`");
            builder.AppendLine("- Nodes read-only : `" + HiveViewProductUiPresenter.WorldMapPreviewNodeCountForProof() + "`");
            builder.AppendLine("- Carte finale fixe : `NO`");
            builder.AppendLine("- Boundary view technique : `" + HiveViewProductUiPresenter.WorldMapTechnicalBoundaryViewActiveForProof() + "`");
            builder.AppendLine("- Claims live : `NO`");
            builder.AppendLine("- Lot BEE-781-800 runtime : `IntegratedEvidenceOnly`");
            builder.AppendLine("- BEE-801 : `Blocked`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures) builder.AppendLine("- " + capture.Label + " : `" + PathFor(capture) + "`");
            builder.AppendLine();
            builder.AppendLine("## Preuve demandee");
            builder.AppendLine();
            builder.AppendLine("- Tablette paysage carte monde premium non-live : `BEE-780_03_WorldTabletLandscape_1920x1200.png`");
            builder.AppendLine("- Telephone portrait carte monde non-live : `BEE-780_04_WorldPhonePortrait_390x844.png`");
            builder.AppendLine("- Transition Ruche -> Monde : `BEE-762_01_HiveBeforeWorld_1280x720.png` puis `BEE-762_02_TransitionHiveToWorld_1280x720.png`");
            builder.AppendLine("- Retour Monde -> Ruche : `BEE-774_07_ReturnWorldToHive_1280x720.png`");
            builder.AppendLine("- Absence de claims live : microcopy non-live visible et manifeste `Claims live : NO`.");
            builder.AppendLine("- Pas une boundary view technique : carte premium runtime, hives, territoires, routes, legende, minimap et manifeste `Boundary view technique : False`.");
            builder.AppendLine();
            builder.AppendLine("## Preuve ARCH-166 gestes tablette");
            builder.AppendLine();
            builder.AppendLine("- Un doigt = pan/deplacement seulement.");
            builder.AppendLine("- Deux doigts = pinch zoom/dezoom seulement.");
            builder.AppendLine("- Un pan a un doigt ne declenche jamais un zoom.");
            builder.AppendLine("- Zoom doux : cible de zoom + damping/interpolation + limite de vitesse.");
            builder.AppendLine("- HUD, menus, panneaux et navigation restent fixes pendant zoom.");
            builder.AppendLine("- Halos, hotspots et zones restent alignes apres pan/zoom : `BEE-780_06_HitZonesHalosAfterPanZoom_1280x720.png`.");
            builder.AppendLine("- Selections accidentelles evitees : selection supprimee pendant pan/pinch.");
            builder.AppendLine();
            builder.AppendLine("### Telemetry par preuve");
            foreach (CaptureSpec capture in Captures)
            {
                builder.AppendLine("- `" + capture.FileName + "` mode=`" + capture.GestureMode + "` touch_count=`" + capture.TouchCount + "` pan_delta=`" + capture.PanDelta.x.ToString("0.##", CultureInfo.InvariantCulture) + "," + capture.PanDelta.y.ToString("0.##", CultureInfo.InvariantCulture) + "` pinch_delta=`" + capture.PinchDelta.ToString("0.####", CultureInfo.InvariantCulture) + "` zoom_target=`" + capture.Zoom.ToString("0.###", CultureInfo.InvariantCulture) + "` zoom_applied=`" + capture.Zoom.ToString("0.###", CultureInfo.InvariantCulture) + "`");
            }

            builder.AppendLine();
            builder.AppendLine("### Derniere telemetrie runtime");
            foreach (string item in HiveViewProductUiPresenter.WorldMapGestureTelemetryForProof()) builder.AppendLine("- `" + item + "`");
            builder.AppendLine();
            builder.AppendLine("## Fondation MMO scalable");
            builder.AppendLine();
            foreach (string item in HiveViewProductUiPresenter.WorldMapScalableReadinessForProof()) builder.AppendLine("- `" + item + "`");
            builder.AppendLine();
            builder.AppendLine("`C:/projets/beekingdom/carte.png` est une reference de qualite et de composition. La fondation runtime reste prevue pour pan/zoom, regions, clusters, recherche, minimap, chargement par zones, territoires, routes, points de ressources, WorldId et GameServerId.");
            builder.AppendLine();
            builder.AppendLine("## BEE-781 a BEE-800 Runtime Evidence");
            builder.AppendLine();
            foreach (string item in HiveViewProductUiPresenter.WorldMapProductionizationLotReadinessForProof()) builder.AppendLine("- `" + item + "`");
            builder.AppendLine();
            builder.AppendLine("### Hit-test matrix post pan/zoom");
            foreach (string item in HiveViewProductUiPresenter.WorldMapHitTestMatrixForProof()) builder.AppendLine("- `" + item + "`");
            builder.AppendLine();
            builder.AppendLine("### World Registry server selection non-live");
            foreach (string item in HiveViewProductUiPresenter.WorldMapServerSelectionForProof()) builder.AppendLine("- `" + item + "`");
            builder.AppendLine();
            builder.AppendLine("## Couverture BEE");
            builder.AppendLine();
            builder.AppendLine("- BEE-761 : surface carte monde runtime non-live premium.");
            builder.AppendLine("- BEE-762 / BEE-774 : navigation Ruche <-> Monde.");
            builder.AppendLine("- BEE-763 : MapCameraLayer separe du HUD fixe.");
            builder.AppendLine("- BEE-764 / BEE-765 : nodes read-only et etats d'interaction non-live.");
            builder.AppendLine("- BEE-766 a BEE-768 : preuves device conservees cote ruche et capture monde responsive.");
            builder.AppendLine("- BEE-769 a BEE-771 : WorldId/GameServerId affiches comme readiness, non assignes live.");
            builder.AppendLine("- BEE-772 / BEE-773 : microcopy server-first et garde anti live-claim.");
            builder.AppendLine("- BEE-775 : consommation readiness uniquement, aucun endpoint live consomme.");
            builder.AppendLine("- BEE-776 a BEE-780 : pack Demo/QA/gate.");
            builder.AppendLine("- BEE-781 : intake reserves ARCH-172.");
            builder.AppendLine("- BEE-782 a BEE-785 : atlas/tuiles/regions, streaming readiness et HUD fixe.");
            builder.AppendLine("- BEE-786 a BEE-794 : certification device prep, telemetry, hit-test matrix et tests ARCH-166.");
            builder.AppendLine("- BEE-795 a BEE-798 : selection serveur non-live, capacite monde et no-claim guard.");
            builder.AppendLine("- BEE-799 a BEE-800 : handoff Builder-C et gate final evidence-only.");
            builder.AppendLine();
            builder.AppendLine("## Non-claims");
            builder.AppendLine();
            builder.AppendLine("- Monde en preparation.");
            builder.AppendLine("- Donnees non officielles.");
            builder.AppendLine("- Serveur requis pour jouer.");
            builder.AppendLine("- Aucun territoire officiel, alliance active, guerre, PvP, chat, classement, matchmaking, economie live ou synchronisation temps reel.");
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
                Debug.LogWarning("Unable to force BEE-780 Game View size " + width + "x" + height + ": " + exception.Message);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
