using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using BeeKingdom.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxLivingHivePerimeterSortieCapture
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";
        private const string OutputDirectory = "C:/projets/beekingdomgame-master/Docs/Product/Evidence/LivingHivePerimeterSortie";
        private const string ManifestPath = OutputDirectory + "/LivingHivePerimeterSortie_CaptureManifest.md";
        private const string Requested = "BeeKingdom.PerimeterSortie.Capture.Requested";
        private const string Frames = "BeeKingdom.PerimeterSortie.Capture.Frames";
        private const string Captured = "BeeKingdom.PerimeterSortie.Capture.Captured";
        private const string Index = "BeeKingdom.PerimeterSortie.Capture.Index";
        private const string ConfiguredIndex = "BeeKingdom.PerimeterSortie.Capture.ConfiguredIndex";

        private readonly struct CaptureSpec
        {
            public CaptureSpec(string label, string fileName, int width, int height, bool portrait, string state)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Portrait = portrait;
                State = state;
            }

            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly bool Portrait;
            public readonly string State;
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Sorties non configurees FR", "LivingHive_PerimeterSortie_NotConfigured_FR_390x844.png", 390, 844, true, "not_configured"),
            new CaptureSpec("Sortie active apercu QA FR", "LivingHive_PerimeterSortie_ActiveQA_FR_390x844.png", 390, 844, true, "active_qa"),
            new CaptureSpec("Consultation hors ligne apercu QA FR", "LivingHive_PerimeterSortie_OfflineReadOnlyQA_FR_390x844.png", 390, 844, true, "offline_qa"),
            new CaptureSpec("Debrief partiel apercu QA FR", "LivingHive_PerimeterSortie_DebriefPartialQA_FR_390x844.png", 390, 844, true, "debrief_partial_qa"),
            new CaptureSpec("Perimeter sorties not configured EN", "LivingHive_PerimeterSortie_NotConfigured_EN_1600x900.png", 1600, 900, false, "not_configured"),
            new CaptureSpec("Perimeter sorties ready QA EN", "LivingHive_PerimeterSortie_ReadyQA_EN_1600x900.png", 1600, 900, false, "ready_qa"),
            new CaptureSpec("Offline read-only QA EN", "LivingHive_PerimeterSortie_OfflineReadOnlyQA_EN_1600x900.png", 1600, 900, false, "offline_qa"),
            new CaptureSpec("Full return debrief QA EN", "LivingHive_PerimeterSortie_DebriefFullQA_EN_1600x900.png", 1600, 900, false, "debrief_full_qa")
        };

        static SandboxLivingHivePerimeterSortieCapture()
        {
            if (SessionState.GetBool(Requested, false)) Subscribe();
        }

        [MenuItem("Bee Kingdom/Playground/Capture LivingHive Perimeter Sortie Proofs")]
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
                    throw new InvalidOperationException("Perimeter sortie screenshot was not written: " + path);
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
                Finish(0, "LivingHive perimeter sortie proofs captured.");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                Finish(1, "LivingHive perimeter sortie capture failed.");
            }
        }

        private static void ConfigureAndApplyCurrentState()
        {
            int index = Mathf.Clamp(SessionState.GetInt(Index, 0), 0, Captures.Length - 1);
            if (SessionState.GetInt(ConfiguredIndex, -1) == index) return;
            CaptureSpec spec = Current();
            TrySetGameViewSize(spec.Width, spec.Height, spec.Label);
            Screen.SetResolution(spec.Width, spec.Height, false);

            HiveViewProductUiPresenter.ResetPerimeterSortieControllerForProof();
            HiveViewProductUiPresenter.PrepareFormationReadinessCaptureForProof(spec.Portrait);
            if (!string.Equals(spec.State, "not_configured", StringComparison.Ordinal))
            {
                bool debrief = spec.State.StartsWith("debrief_", StringComparison.Ordinal);
                bool active = string.Equals(spec.State, "active_qa", StringComparison.Ordinal);
                RemoteHivePerimeterSnapshot snapshot = debrief
                    ? BuildQaClaimSnapshot(string.Equals(spec.State, "debrief_partial_qa", StringComparison.Ordinal))
                    : BuildQaSnapshot(active);
                bool offline = string.Equals(spec.State, "offline_qa", StringComparison.Ordinal);
                HivePerimeterSortieScreenModel model = HivePerimeterSortiePresentation.FromSnapshot(
                    snapshot,
                    includeClaimReceipt: !offline,
                    readOnlyOffline: offline,
                    cachedAtUtc: offline ? snapshot.ServerTimeUtc : default(DateTimeOffset));
                HiveViewProductUiPresenter.UsePerimeterSortieControllerForProof(new EvidenceController(model), true);
            }
            HiveViewProductUiPresenter.OpenPerimeterSortieForProof();
            SessionState.SetInt(ConfiguredIndex, index);
        }

        private static RemoteHivePerimeterSnapshot BuildQaSnapshot(bool active)
        {
            DateTimeOffset cycle = new DateTimeOffset(2026, 7, 22, 8, 0, 0, TimeSpan.Zero);
            DateTimeOffset serverTime = cycle.AddHours(4);
            const string reservationId = "qa-layout-reservation";
            var signals = new List<RemoteHivePerimeterSignal>
            {
                new RemoteHivePerimeterSignal
                {
                    SignalKey = "foraging_scout",
                    SignalInstanceId = "20260722T080000Z:foraging_scout",
                    HazardDoctrine = "wingrunners",
                    Duration = TimeSpan.FromSeconds(16),
                    MinimumSquad = 1,
                    HoneyReward = 40,
                    PollenReward = 20,
                    Completed = false,
                    CanLaunch = !active
                },
                new RemoteHivePerimeterSignal
                {
                    SignalKey = "brood_watch",
                    SignalInstanceId = "20260722T080000Z:brood_watch",
                    HazardDoctrine = "guardians",
                    Duration = TimeSpan.FromSeconds(20),
                    MinimumSquad = 2,
                    HoneyReward = 25,
                    PollenReward = 35,
                    Completed = false,
                    CanLaunch = !active
                }
            };
            var snapshot = new RemoteHivePerimeterSnapshot
            {
                ContractVersion = HivePerimeterSortieClient.SortieContractVersion,
                Revision = active ? 3 : 2,
                ServerTimeUtc = serverTime,
                CycleStartedAtUtc = cycle,
                CycleEndsAtUtc = cycle.AddHours(8),
                Reservation = new RemoteSquadReservationSnapshot
                {
                    ContractVersion = HivePerimeterSortieClient.ReservationContractVersion,
                    CatalogVersion = HivePerimeterSortieClient.RecruitmentCatalogVersion,
                    ReservationRevision = 2,
                    Capacity = HivePerimeterSortieClient.InitialCapacity,
                    ReservationId = reservationId,
                    Reserved = new Dictionary<string, long>
                    {
                        ["guardians"] = 3,
                        ["wingrunners"] = 6,
                        ["darters"] = 3
                    }
                },
                Signals = signals
            };
            if (active)
            {
                snapshot.Active = new RemoteHivePerimeterActiveSortie
                {
                    SortieId = Guid.Parse("84cf1542-7d54-4f71-a6aa-31a40f79f2d0"),
                    SignalKey = signals[0].SignalKey,
                    SignalInstanceId = signals[0].SignalInstanceId,
                    ReservationId = reservationId,
                    StartedAtUtc = serverTime.AddSeconds(-5),
                    EndsAtUtc = serverTime.AddSeconds(11),
                    Revision = snapshot.Revision
                };
            }
            return snapshot;
        }

        private static RemoteHivePerimeterSnapshot BuildQaClaimSnapshot(bool capacityLimited)
        {
            RemoteHivePerimeterSnapshot snapshot = BuildQaSnapshot(false);
            snapshot.Revision = 4;
            snapshot.Reservation.ReservationRevision = 3;
            snapshot.Reservation.ReservationId = null;
            snapshot.Reservation.Reserved = new Dictionary<string, long>
            {
                ["guardians"] = 0,
                ["wingrunners"] = 0,
                ["darters"] = 0
            };
            snapshot.Signals[0].Completed = true;
            snapshot.Signals[0].CanLaunch = false;
            snapshot.ClaimReceipt = new RemoteHivePerimeterClaimReceipt
            {
                SortieId = Guid.Parse("84cf1542-7d54-4f71-a6aa-31a40f79f2d0"),
                SignalKey = snapshot.Signals[0].SignalKey,
                SignalInstanceId = snapshot.Signals[0].SignalInstanceId,
                Revision = snapshot.Revision,
                ServerTimeUtc = snapshot.ServerTimeUtc,
                CreditedByResource = new Dictionary<string, long>
                {
                    ["honey"] = capacityLimited ? 10 : 40,
                    ["pollen"] = 20
                },
                ResultingBalances = new Dictionary<string, RemoteHiveResourceBalance>
                {
                    ["honey"] = new RemoteHiveResourceBalance { Amount = capacityLimited ? 130 : 640, Capacity = capacityLimited ? 130 : 1000 },
                    ["pollen"] = new RemoteHiveResourceBalance { Amount = 420, Capacity = 1000 }
                }
            };
            return snapshot;
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# LivingHive Perimeter Sortie - manifeste de captures");
            builder.AppendLine();
            builder.AppendLine("- Scene: `Assets/Scenes/LivingHive.unity`");
            builder.AppendLine("- Etats `not_configured`: controleur de production indisponible par defaut, aucun signal ni statut invente");
            builder.AppendLine("- Etats `active_qa`, `ready_qa` et `debrief_*_qa`: donnees synthetiques de mise en page, marquees `APERCU QA`, controleur sans effet et aucun appel serveur");
            builder.AppendLine("- Etat `offline_qa`: dernier GET synthetique marque `APERCU QA`, consultation seulement, toutes les actions serveur neutralisees");
            builder.AppendLine("- Debriefs QA: recus synthetiques destines uniquement a prouver le rendu plein/partiel; aucun credit local");
            builder.AppendLine("- Appareil: rendu, langue, navigation, selection et compte a rebours relatif seulement");
            builder.AppendLine("- Serveur: session, cycle, revision, reservation, heure, recompense et credit officiels");
            builder.AppendLine("- Mutation locale de ressource, reservation, sortie ou recompense: `false`");
            builder.AppendLine("- Terrain 50x50, image de ruche et scenes modifies: `false`");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                string path = PathFor(capture);
                Vector2Int dimensions = ReadPngDimensions(path);
                builder.AppendLine("- `" + capture.FileName + "`: `" + dimensions.x + "x" + dimensions.y + "`, locale `" + (capture.Portrait ? "fr-CA" : "en-US") + "`, etat `" + capture.State + "`, SHA-256 `" + Sha256(path) + "`");
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
            HiveViewProductUiPresenter.ResetPerimeterSortieControllerForProof();
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

        private sealed class EvidenceController : IHivePerimeterSortiePanelController
        {
            public EvidenceController(HivePerimeterSortieScreenModel model) { Model = model; }
            public HivePerimeterSortieScreenModel Model { get; }
            public bool IsConfigured => true;
            public bool IsBusy => false;
            public void Refresh() { }
            public void ReserveSquad(int guardians, int wingrunners, int darters) { }
            public void Launch(string signalKey) { }
            public void Claim() { }
            public void Recall() { }
            public void Retry() { }
            public void DismissDebrief() { }
        }
    }
}
