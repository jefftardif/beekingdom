using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxMobileAccountSessionCapture
    {
        private const string ScenePath = "Assets/Scenes/LivingHive.unity";
        private const string OutputDirectory = "C:/projets/beekingdomgame-master/Docs/Product/Evidence/MobileAccountSession";
        private const string ManifestPath = OutputDirectory + "/MobileAccountSession_CaptureManifest.md";
        private const string Requested = "BeeKingdom.MobileAccountSession.Capture.Requested";
        private const string Frames = "BeeKingdom.MobileAccountSession.Capture.Frames";
        private const string Captured = "BeeKingdom.MobileAccountSession.Capture.Captured";
        private const string Index = "BeeKingdom.MobileAccountSession.Capture.Index";
        private const string ConfiguredIndex = "BeeKingdom.MobileAccountSession.Capture.ConfiguredIndex";

        private readonly struct CaptureSpec
        {
            public CaptureSpec(string label, string fileName, int width, int height, string locale, string state)
            {
                Label = label;
                FileName = fileName;
                Width = width;
                Height = height;
                Locale = locale;
                State = state;
            }

            public readonly string Label;
            public readonly string FileName;
            public readonly int Width;
            public readonly int Height;
            public readonly string Locale;
            public readonly string State;
        }

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("Session mobile non configuree FR", "MobileAccountSession_NotConfigured_FR_390x844.png", 390, 844, "fr-CA", "not_configured"),
            new CaptureSpec("Formulaire session mobile apercu QA FR", "MobileAccountSession_ReadyFormQA_FR_390x844.png", 390, 844, "fr-CA", "ready_form_qa"),
            new CaptureSpec("Mobile session not configured EN", "MobileAccountSession_NotConfigured_EN_1600x900.png", 1600, 900, "en-US", "not_configured"),
            new CaptureSpec("Authenticated account local preview QA EN", "MobileAccountSession_AuthenticatedPreviewQA_EN_1600x900.png", 1600, 900, "en-US", "authenticated_preview_qa")
        };

        static SandboxMobileAccountSessionCapture()
        {
            if (SessionState.GetBool(Requested, false)) Subscribe();
        }

        [MenuItem("Bee Kingdom/Playground/Capture Mobile Account Session Proofs")]
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
            ConfigureCurrent();
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
            ConfigureCurrent();
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
                    throw new InvalidOperationException("Mobile session screenshot was not written: " + path);
                }

                ValidateDimensions(path, spec.Width, spec.Height);
                int index = SessionState.GetInt(Index, 0);
                if (index < Captures.Length - 1)
                {
                    SessionState.SetInt(Index, index + 1);
                    SessionState.SetInt(ConfiguredIndex, -1);
                    SessionState.SetInt(Frames, 0);
                    SessionState.SetBool(Captured, false);
                    ConfigureCurrent();
                    return;
                }

                File.WriteAllText(ManifestPath, BuildManifest(), Encoding.UTF8);
                Finish(0, "Mobile account session proofs captured.");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                Finish(1, "Mobile account session capture failed.");
            }
        }

        private static void ConfigureCurrent()
        {
            int index = Mathf.Clamp(SessionState.GetInt(Index, 0), 0, Captures.Length - 1);
            if (SessionState.GetInt(ConfiguredIndex, -1) == index) return;
            CaptureSpec spec = Current();
            TrySetGameViewSize(spec.Width, spec.Height, spec.Label);
            Screen.SetResolution(spec.Width, spec.Height, false);
            HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
            HiveViewProductUiPresenter.SetLocaleForRuntime(spec.Locale);
            HiveViewProductUiPresenter.SetSplashAuthGateForProof("login");

            if (!string.Equals(spec.State, "not_configured", StringComparison.Ordinal))
            {
                var clock = new FixedClock(new DateTimeOffset(2026, 7, 22, 19, 0, 0, TimeSpan.Zero));
                var transport = new EvidenceTransport(clock.UtcNow);
                var client = new MobileAccountSessionClient(
                    HiveViewProductUiPresenter.AccountSessionGateForRuntime(),
                    transport,
                    new MemoryProtectedStore(),
                    clock);
                HiveViewProductUiPresenter.ConfigureMobileAccountSessionForRuntime(
                    client,
                    (email, password) => new MobileAccountLoginRequest(email, password, "qa", "qa-installation", "qa"));
                client.InitializeAsync().GetAwaiter().GetResult();
                if (string.Equals(spec.State, "authenticated_preview_qa", StringComparison.Ordinal))
                {
                    client.LoginAsync(new MobileAccountLoginRequest(
                        "qa-layout@bee.test",
                        "qa-only",
                        "qa",
                        "qa-installation",
                        "qa")).GetAwaiter().GetResult();
                }
                HiveViewProductUiPresenter.SetMobileAccountQaPreviewForProof(true);
                HiveViewProductUiPresenter.RefreshMobileAccountSessionMessageForRuntime();
            }

            SessionState.SetInt(ConfiguredIndex, index);
        }

        private static string BuildManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Mobile Account Session - manifeste de captures");
            builder.AppendLine();
            builder.AppendLine("- Scene: `Assets/Scenes/LivingHive.unity`");
            builder.AppendLine("- Etats `not_configured`: configuration runtime absente, aucun formulaire, identifiant, jeton ou statut live invente");
            builder.AppendLine("- Etats `*_qa`: transport et coffre strictement en memoire dans le harnais de capture, aucun appel serveur, aucune persistence, marques comme apercu QA");
            builder.AppendLine("- Mot de passe affiche ou inscrit au manifeste: `false`");
            builder.AppendLine("- Jeton affiche ou inscrit au manifeste: `false`");
            builder.AppendLine("- Production: opt-in par ressource runtime absente; portes serveur fermees; aucun compte reel");
            builder.AppendLine("- Appareil production cible: access token memoire, refresh Android Keystore, identifiant installation aleatoire non autoritaire");
            builder.AppendLine("- Serveur: identite, session, expiration, rotation, revocation et autorite de jeu");
            builder.AppendLine("- Terrain 50x50, image de ruche et scenes modifies: `false`");
            builder.AppendLine();
            foreach (CaptureSpec capture in Captures)
            {
                string path = PathFor(capture);
                Vector2Int dimensions = ReadPngDimensions(path);
                builder.AppendLine("- `" + capture.FileName + "`: `" + dimensions.x + "x" + dimensions.y + "`, locale `" + capture.Locale + "`, etat `" + capture.State + "`, SHA-256 `" + Sha256(path) + "`");
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
            HiveViewProductUiPresenter.ResetMobileAccountSessionForProof();
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

        private sealed class FixedClock : IMobileAccountSessionClock
        {
            public FixedClock(DateTimeOffset utcNow) { UtcNow = utcNow; }
            public DateTimeOffset UtcNow { get; }
        }

        private sealed class MemoryProtectedStore : IProtectedRefreshTokenStore
        {
            private ProtectedRefreshTokenRecord value;
            public bool IsProtectionAvailable => true;
            public Task SaveAsync(ProtectedRefreshTokenRecord record, CancellationToken cancellationToken) { value = record; return Task.CompletedTask; }
            public Task<ProtectedRefreshTokenRecord> LoadAsync(CancellationToken cancellationToken) => Task.FromResult(value);
            public Task DeleteAsync(CancellationToken cancellationToken) { value = null; return Task.CompletedTask; }
        }

        private sealed class EvidenceTransport : IMobileAccountSessionRestTransport
        {
            private readonly DateTimeOffset now;
            private readonly Guid player = Guid.Parse("11111111-1111-1111-1111-111111111111");
            private readonly Guid account = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            public EvidenceTransport(DateTimeOffset now) { this.now = now; }

            public Task<RemoteAccountSessionReadiness> ReadReadinessAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(new RemoteAccountSessionReadiness
                {
                    ServerTimeUtc = now,
                    AccountCreationAllowed = true,
                    SessionCreationAllowed = true,
                    TokenIssuanceAllowed = true,
                    SecretsAllowedInResponse = false,
                    Claims = new RemoteAccountSessionReadinessClaims
                    {
                        LiveAccounts = true,
                        LiveSessions = true,
                        GameplayAuthorityGranted = false
                    },
                    Blockers = new System.Collections.Generic.List<string>()
                });
            }

            public Task<RemoteMobileLoginResult> LoginAsync(MobileAccountLoginRequest request, CancellationToken cancellationToken)
            {
                const string sessionId = "qa-session";
                return Task.FromResult(new RemoteMobileLoginResult
                {
                    Succeeded = true,
                    PlayerId = player,
                    AccountId = account,
                    Session = new RemoteMobileAuthenticationSession
                    {
                        SessionId = sessionId,
                        PlayerId = player,
                        AccountId = account,
                        LoginUtc = now,
                        ExpirationUtc = now.AddDays(14),
                        IsRevoked = false
                    },
                    Tokens = new RemoteMobileTokenPair
                    {
                        AccessToken = "qa-access-memory-only",
                        RefreshToken = "qa-refresh-memory-only",
                        AccessTokenExpiresUtc = now.AddMinutes(15),
                        RefreshTokenExpiresUtc = now.AddDays(14),
                        PlayerId = player,
                        SessionId = sessionId
                    }
                });
            }

            public Task<RemoteMobileLoginResult> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
            public Task<RemoteMobileTokenPair> RefreshAsync(string refreshToken, CancellationToken cancellationToken) => throw new NotSupportedException();
            public Task LogoutAsync(string bearerAccessToken, CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
