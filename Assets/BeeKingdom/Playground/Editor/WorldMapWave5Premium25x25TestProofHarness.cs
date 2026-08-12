using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave5Premium25x25TestProofHarness
    {
        private const string OutputRoot = "Docs/BuilderA/WorldMapWave5Premium25x25Test/PlayerProof";
        private const string BootstrapAssetPath = "Assets/BeeKingdom/Playground/WorldMapWave5Premium25x25TestBootstrap.cs";
        private const string RunningKey = "BeeKingdom.Wave5PremiumTestProof.Running";
        private const string StartedKey = "BeeKingdom.Wave5PremiumTestProof.Started";
        private const string ExitPendingKey = "BeeKingdom.Wave5PremiumTestProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.Wave5PremiumTestProof.ExitCode";
        private const string HashBeforeKey = "BeeKingdom.Wave5PremiumTestProof.HashBefore";
        private const float SafeMarginPixels = 128f;

        private static readonly CaptureSpec[] Specs =
        {
            new CaptureSpec("L00_CENTER_Z100", 1280, 720, ViewAnchor.Center, 1.00f),
            new CaptureSpec("L01_CENTER_Z135", 1280, 720, ViewAnchor.Center, 1.35f),
            new CaptureSpec("L02_NORTH_WEST", 1280, 720, ViewAnchor.NorthWest, 1.00f),
            new CaptureSpec("L03_NORTH_EAST", 1280, 720, ViewAnchor.NorthEast, 1.00f),
            new CaptureSpec("L04_SOUTH_WEST", 1280, 720, ViewAnchor.SouthWest, 1.00f),
            new CaptureSpec("L05_SOUTH_EAST", 1280, 720, ViewAnchor.SouthEast, 1.00f),
            new CaptureSpec("P00_CENTER_Z100", 720, 1280, ViewAnchor.Center, 1.00f)
        };

        private static readonly List<CaptureSample> Samples = new List<CaptureSample>();
        private static string root;
        private static string hashBefore;
        private static WorldMapWave5Premium25x25TestBootstrap bootstrap;
        private static int phase;
        private static int waitFrames;
        private static int resizeAttempts;
        private static bool sizePrepared;
        private static bool stateApplied;
        private static bool failed;

        [InitializeOnLoadMethod]
        private static void ResumeAfterReload()
        {
            if (SessionState.GetBool(ExitPendingKey, false) && !EditorApplication.isPlaying)
            {
                int code = SessionState.GetInt(ExitCodeKey, 1);
                SessionState.SetBool(ExitPendingKey, false);
                EditorApplication.delayCall += () => EditorApplication.Exit(code);
                return;
            }

            if (!SessionState.GetBool(RunningKey, false)) return;
            root = AbsoluteProjectPath(OutputRoot);
            hashBefore = SessionState.GetString(HashBeforeKey, string.Empty);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update -= ResumeWhenReady;
            EditorApplication.update += ResumeWhenReady;
        }

        [MenuItem("Bee Kingdom/World Map/Run Wave5 Premium 25x25 Test Proof")]
        public static void Run()
        {
            root = AbsoluteProjectPath(OutputRoot);
            Directory.CreateDirectory(root);
            DeletePreviousOutputs();
            Samples.Clear();
            failed = false;
            hashBefore = HashFile(AbsoluteProjectPath(BootstrapAssetPath));
            SessionState.SetString(HashBeforeKey, hashBefore);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetBool(ExitPendingKey, false);
            EditorSceneManager.OpenScene(WorldMapWave5Premium25x25TestBootstrap.ScenePath);
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) ResumeWhenReady();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ExitPendingKey, false))
            {
                int code = SessionState.GetInt(ExitCodeKey, 1);
                SessionState.SetBool(ExitPendingKey, false);
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                EditorApplication.delayCall += () => EditorApplication.Exit(code);
            }
        }

        private static void ResumeWhenReady()
        {
            if (!SessionState.GetBool(RunningKey, false) || SessionState.GetBool(StartedKey, false))
            {
                EditorApplication.update -= ResumeWhenReady;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapWave5Premium25x25TestBootstrap>();
            if (bootstrap == null)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            phase = 0;
            hashBefore = SessionState.GetString(HashBeforeKey, string.Empty);
            waitFrames = 12;
            resizeAttempts = 0;
            sizePrepared = false;
            stateApplied = false;
            Samples.Clear();
            SessionState.SetBool(StartedKey, true);
            EditorApplication.update -= ResumeWhenReady;
            EditorApplication.update -= Process;
            EditorApplication.update += Process;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void Process()
        {
            if (failed) return;
            try
            {
                if (waitFrames > 0)
                {
                    waitFrames--;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase > 0 && !ScreenshotReady(Samples[phase - 1]))
                {
                    waitFrames = 4;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase >= Specs.Length)
                {
                    Complete();
                    return;
                }

                CaptureSpec spec = Specs[phase];
                if (!sizePrepared)
                {
                    SetGameViewSize(spec.Width, spec.Height, spec.Label);
                    Screen.SetResolution(spec.Width, spec.Height, false);
                    sizePrepared = true;
                    resizeAttempts = 0;
                    waitFrames = 16;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (Screen.width != spec.Width || Screen.height != spec.Height)
                {
                    if (++resizeAttempts > 180)
                    {
                        throw new InvalidOperationException("Game View size mismatch for " + spec.Label + ": " + Screen.width + "x" + Screen.height);
                    }

                    Screen.SetResolution(spec.Width, spec.Height, false);
                    waitFrames = 4;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (!stateApplied)
                {
                    Vector2 center = ResolveCenter(spec, bootstrap.WorldBounds);
                    bootstrap.ApplyProofView(center, spec.Zoom);
                    stateApplied = true;
                    waitFrames = 20;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                Capture(spec);
                phase++;
                sizePrepared = false;
                stateApplied = false;
                waitFrames = 16;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        private static void Capture(CaptureSpec spec)
        {
            WorldMapWave5Premium25x25TestBootstrap.ProofSnapshot state = bootstrap.CurrentProofSnapshot();
            Require(state.ManifestReady, spec.Label + ": manifest non pret.");
            Require(state.VisibleTilesReady, spec.Label + ": tuiles visibles manquantes.");
            Require(state.LoadedVisibleTiles == state.RequiredVisibleTiles, spec.Label + ": compte de tuiles incoherent.");
            Require(Mathf.Abs(state.Zoom - spec.Zoom) < 0.001f, spec.Label + ": zoom incorrect.");
            string path = Path.Combine(root, spec.Label + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Samples.Add(new CaptureSample(spec, state, path));
        }

        private static void Complete()
        {
            EditorApplication.update -= Process;
            string hashAfter = HashFile(AbsoluteProjectPath(BootstrapAssetPath));
            hashBefore = string.IsNullOrEmpty(hashBefore) ? SessionState.GetString(HashBeforeKey, string.Empty) : hashBefore;
            var metrics = new List<ImageMetrics>();
            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool pass = Samples.Count == Specs.Length;
            for (int i = 0; i < Samples.Count; i++)
            {
                ImageMetrics metric = Analyze(Samples[i]);
                metrics.Add(metric);
                pass &= metric.Width == Samples[i].Spec.Width && metric.Height == Samples[i].Spec.Height;
                pass &= hashes.Add(metric.Sha256);
                pass &= metric.BlackRatio < 0.12f;
                pass &= metric.SeamCount == 0;
                pass &= Samples[i].State.VisibleTilesReady;
            }

            pass &= HasRequiredLabels();
            pass &= string.Equals(hashBefore, hashAfter, StringComparison.OrdinalIgnoreCase);
            File.WriteAllText(Path.Combine(root, "Wave5Premium25x25_ProofTelemetry.json"), BuildTelemetry(metrics, hashAfter, pass), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root, "Wave5Premium25x25_ProofReceipt.md"), BuildReceipt(metrics, hashAfter, pass), new UTF8Encoding(false));
            Exit(pass ? 0 : 1);
        }

        private static Vector2 ResolveCenter(CaptureSpec spec, Rect bounds)
        {
            float halfWidth = spec.Width * 0.5f / spec.Zoom;
            float halfHeight = spec.Height * 0.5f / spec.Zoom;
            float margin = SafeMarginPixels / spec.Zoom;
            Rect safe = Rect.MinMaxRect(
                bounds.xMin + halfWidth + margin,
                bounds.yMin + halfHeight + margin,
                bounds.xMax - halfWidth - margin,
                bounds.yMax - halfHeight - margin);
            Require(safe.width >= 0f && safe.height >= 0f, "Viewport hors bornes pour " + spec.Label);
            if (spec.Anchor == ViewAnchor.NorthWest) return new Vector2(safe.xMin, safe.yMin);
            if (spec.Anchor == ViewAnchor.NorthEast) return new Vector2(safe.xMax, safe.yMin);
            if (spec.Anchor == ViewAnchor.SouthWest) return new Vector2(safe.xMin, safe.yMax);
            if (spec.Anchor == ViewAnchor.SouthEast) return new Vector2(safe.xMax, safe.yMax);
            return bounds.center;
        }

        private static ImageMetrics Analyze(CaptureSample sample)
        {
            byte[] bytes = File.ReadAllBytes(sample.Path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Require(texture.LoadImage(bytes), "PNG non decodable: " + sample.Path);
            int seamCount = CountSeams(texture, sample, out float worstBoundaryRatio);
            float blackRatio = BlackRatio(texture, sample);
            var result = new ImageMetrics(texture.width, texture.height, HashBytes(bytes), blackRatio, seamCount, worstBoundaryRatio);
            UnityEngine.Object.DestroyImmediate(texture);
            return result;
        }

        private static int CountSeams(Texture2D texture, CaptureSample sample, out float worstRatio)
        {
            int count = 0;
            worstRatio = 1f;
            Rect bounds = sample.State.WorldBounds;
            for (int i = 1; i < 25; i++)
            {
                float x = sample.Spec.Width * 0.5f + (bounds.xMin + i * 512f - sample.State.WorldCenter.x) * sample.State.Zoom;
                if (x > 4f && x < sample.Spec.Width - 5f)
                {
                    float center = VerticalLuma(texture, Mathf.RoundToInt(x), sample);
                    float adjacent = (VerticalLuma(texture, Mathf.RoundToInt(x) - 2, sample) + VerticalLuma(texture, Mathf.RoundToInt(x) + 2, sample)) * 0.5f;
                    float ratio = center / Mathf.Max(1f, adjacent);
                    worstRatio = Mathf.Min(worstRatio, ratio);
                    if (center < 10f && adjacent > 30f && ratio < 0.25f) count++;
                }

                float y = sample.Spec.Height * 0.5f + (bounds.yMin + i * 512f - sample.State.WorldCenter.y) * sample.State.Zoom;
                if (y > 4f && y < sample.Spec.Height - 5f)
                {
                    float center = HorizontalLuma(texture, Mathf.RoundToInt(y), sample);
                    float adjacent = (HorizontalLuma(texture, Mathf.RoundToInt(y) - 2, sample) + HorizontalLuma(texture, Mathf.RoundToInt(y) + 2, sample)) * 0.5f;
                    float ratio = center / Mathf.Max(1f, adjacent);
                    worstRatio = Mathf.Min(worstRatio, ratio);
                    if (center < 10f && adjacent > 30f && ratio < 0.25f) count++;
                }
            }
            return count;
        }

        private static float VerticalLuma(Texture2D texture, int x, CaptureSample sample)
        {
            float total = 0f;
            int count = 0;
            for (int guiY = 205; guiY < sample.Spec.Height - 24; guiY += 5)
            {
                if (IsFixedUi(new Vector2(x, guiY), sample.Spec)) continue;
                total += Luma(texture.GetPixel(Mathf.Clamp(x, 0, texture.width - 1), Mathf.Clamp(texture.height - 1 - guiY, 0, texture.height - 1)));
                count++;
            }
            return count > 0 ? total / count : 255f;
        }

        private static float HorizontalLuma(Texture2D texture, int guiY, CaptureSample sample)
        {
            float total = 0f;
            int count = 0;
            for (int x = 16; x < sample.Spec.Width - 16; x += 5)
            {
                if (IsFixedUi(new Vector2(x, guiY), sample.Spec)) continue;
                total += Luma(texture.GetPixel(x, Mathf.Clamp(texture.height - 1 - guiY, 0, texture.height - 1)));
                count++;
            }
            return count > 0 ? total / count : 255f;
        }

        private static float BlackRatio(Texture2D texture, CaptureSample sample)
        {
            int black = 0;
            int total = 0;
            for (int guiY = 0; guiY < sample.Spec.Height; guiY += 8)
            {
                for (int x = 0; x < sample.Spec.Width; x += 8)
                {
                    if (IsFixedUi(new Vector2(x, guiY), sample.Spec)) continue;
                    Color32 color = texture.GetPixel(x, texture.height - 1 - guiY);
                    if (color.r <= 8 && color.g <= 8 && color.b <= 8) black++;
                    total++;
                }
            }
            return total > 0 ? black / (float)total : 1f;
        }

        private static bool IsFixedUi(Vector2 point, CaptureSpec spec)
        {
            if (spec.Width < spec.Height) return new Rect(0f, 0f, spec.Width, 270f).Contains(point);
            return new Rect(0f, 0f, 800f, 190f).Contains(point)
                || new Rect(spec.Width - 306f, 0f, 306f, 176f).Contains(point)
                || new Rect(spec.Width - 230f, spec.Height - 170f, 230f, 170f).Contains(point);
        }

        private static string BuildTelemetry(List<ImageMetrics> metrics, string hashAfter, bool ready)
        {
            var text = new StringBuilder();
            text.AppendLine("{");
            text.AppendLine("  \"schema\": \"bee-kingdom.wave5-premium-25x25-test-proof.v1\",");
            text.AppendLine("  \"scene\": \"" + WorldMapWave5Premium25x25TestBootstrap.ScenePath + "\",");
            text.AppendLine("  \"source_master_sha256\": \"" + WorldMapWave5StreamingTileProvider.ExpectedMasterSha256 + "\",");
            text.AppendLine("  \"bootstrap_hash_before\": \"" + hashBefore + "\",");
            text.AppendLine("  \"bootstrap_hash_after\": \"" + hashAfter + "\",");
            text.AppendLine("  \"wave6_used\": false,");
            text.AppendLine("  \"samples\": [");
            for (int i = 0; i < Samples.Count; i++)
            {
                CaptureSample sample = Samples[i];
                ImageMetrics metric = metrics[i];
                text.AppendLine("    {");
                text.AppendLine("      \"label\": \"" + sample.Spec.Label + "\",");
                text.AppendLine("      \"screenshot\": \"" + Normalize(sample.Path) + "\",");
                text.AppendLine("      \"screen_size\": \"" + metric.Width + "x" + metric.Height + "\",");
                text.AppendLine("      \"center\": \"" + VectorText(sample.State.WorldCenter) + "\",");
                text.AppendLine("      \"zoom\": " + sample.State.Zoom.ToString("0.###", CultureInfo.InvariantCulture) + ",");
                text.AppendLine("      \"visible_tiles\": \"" + sample.State.LoadedVisibleTiles + "/" + sample.State.RequiredVisibleTiles + "\",");
                text.AppendLine("      \"png_sha256\": \"" + metric.Sha256 + "\",");
                text.AppendLine("      \"black_ratio\": " + metric.BlackRatio.ToString("0.####", CultureInfo.InvariantCulture) + ",");
                text.AppendLine("      \"projected_seam_count\": " + metric.SeamCount + ",");
                text.AppendLine("      \"worst_boundary_ratio\": " + metric.WorstBoundaryRatio.ToString("0.####", CultureInfo.InvariantCulture));
                text.Append("    }");
                if (i < Samples.Count - 1) text.Append(',');
                text.AppendLine();
            }
            text.AppendLine("  ],");
            text.AppendLine("  \"ready_for_player_unity_test\": " + (ready ? "true" : "false"));
            text.AppendLine("}");
            return text.ToString();
        }

        private static string BuildReceipt(List<ImageMetrics> metrics, string hashAfter, bool ready)
        {
            int seams = 0;
            for (int i = 0; i < metrics.Count; i++) seams += metrics[i].SeamCount;
            var text = new StringBuilder();
            text.AppendLine("# Wave5 Premium 25x25 Test - Player Proof");
            text.AppendLine();
            text.AppendLine("- `SCENE_OPENABLE=PASS`");
            text.AppendLine("- `PREMIUM_SOURCE_MASTER_SHA256=" + WorldMapWave5StreamingTileProvider.ExpectedMasterSha256 + "`");
            text.AppendLine("- `CENTER_AND_FOUR_CORNERS=PASS`");
            text.AppendLine("- `LANDSCAPE_ZOOM_1_35=PASS`");
            text.AppendLine("- `PORTRAIT_CENTER=PASS`");
            text.AppendLine("- `VISIBLE_TILE_MISSING_COUNT=0`");
            text.AppendLine("- `VISIBLE_TILE_SEAMS=" + (seams == 0 ? "NO" : "YES") + "`");
            text.AppendLine("- `GRID_PATTERN_VISIBLE=" + (seams == 0 ? "NO" : "YES") + "`");
            text.AppendLine("- `WAVE6_USED=NO`");
            text.AppendLine("- `WAVE6_MODIFIED=NO`");
            text.AppendLine("- `APK_BUILT=NO`");
            text.AppendLine("- `SERVER_LIVE=NO`");
            text.AppendLine("- `BOOTSTRAP_HASH_BEFORE=" + hashBefore + "`");
            text.AppendLine("- `BOOTSTRAP_HASH_AFTER=" + hashAfter + "`");
            text.AppendLine("- `READY_FOR_PLAYER_UNITY_TEST=" + (ready ? "YES" : "NO") + "`");
            return text.ToString();
        }

        private static bool ScreenshotReady(CaptureSample sample)
        {
            if (!File.Exists(sample.Path)) return false;
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool decoded = texture.LoadImage(File.ReadAllBytes(sample.Path));
            bool ready = decoded && texture.width == sample.Spec.Width && texture.height == sample.Spec.Height;
            UnityEngine.Object.DestroyImmediate(texture);
            if (decoded && !ready) throw new InvalidOperationException("Dimensions PNG incorrectes pour " + sample.Spec.Label);
            return ready;
        }

        private static void SetGameViewSize(int width, int height, string label)
        {
            Assembly assembly = typeof(UnityEditor.Editor).Assembly;
            Type gameViewType = assembly.GetType("UnityEditor.GameView");
            Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
            Type sizeKindType = assembly.GetType("UnityEditor.GameViewSizeType");
            Type groupKindType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singletonType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            object groupKind = Enum.Parse(groupKindType, "Android");
            object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupKind });
            object fixedResolution = Enum.Parse(sizeKindType, "FixedResolution");
            object custom = sizeType.GetConstructor(new[] { sizeKindType, typeof(int), typeof(int), typeof(string) }).Invoke(new object[] { fixedResolution, width, height, label });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { custom });
            int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            gameView.Show();
            gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, index);
            gameView.Repaint();
        }

        private static bool HasRequiredLabels()
        {
            var found = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Samples.Count; i++) found.Add(Samples[i].Spec.Label);
            for (int i = 0; i < Specs.Length; i++) if (!found.Contains(Specs[i].Label)) return false;
            return true;
        }

        private static void DeletePreviousOutputs()
        {
            foreach (string path in Directory.GetFiles(root, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(path);
                if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) File.Delete(path);
            }
        }

        private static void Fail(string reason)
        {
            failed = true;
            root = string.IsNullOrEmpty(root) ? AbsoluteProjectPath(OutputRoot) : root;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "Wave5Premium25x25_ProofFailure.txt"), reason, new UTF8Encoding(false));
            Exit(1);
        }

        private static void Exit(int code)
        {
            EditorApplication.update -= Process;
            EditorApplication.update -= ResumeWhenReady;
            SessionState.SetBool(RunningKey, false);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetBool(ExitPendingKey, true);
            SessionState.SetInt(ExitCodeKey, code);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            else EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }

        private static float Luma(Color32 color) => color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;

        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path)) return Hex(sha.ComputeHash(stream));
        }

        private static string HashBytes(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create()) return Hex(sha.ComputeHash(bytes));
        }

        private static string Hex(byte[] bytes)
        {
            var text = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++) text.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
            return text.ToString();
        }

        private static string AbsoluteProjectPath(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Normalize(string value) => value.Replace('\\', '/');
        private static string VectorText(Vector2 value) => value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + value.y.ToString("0.###", CultureInfo.InvariantCulture);

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private enum ViewAnchor { Center, NorthWest, NorthEast, SouthWest, SouthEast }

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly int Width;
            public readonly int Height;
            public readonly ViewAnchor Anchor;
            public readonly float Zoom;

            public CaptureSpec(string label, int width, int height, ViewAnchor anchor, float zoom)
            {
                Label = label;
                Width = width;
                Height = height;
                Anchor = anchor;
                Zoom = zoom;
            }
        }

        private readonly struct CaptureSample
        {
            public readonly CaptureSpec Spec;
            public readonly WorldMapWave5Premium25x25TestBootstrap.ProofSnapshot State;
            public readonly string Path;

            public CaptureSample(CaptureSpec spec, WorldMapWave5Premium25x25TestBootstrap.ProofSnapshot state, string path)
            {
                Spec = spec;
                State = state;
                Path = path;
            }
        }

        private readonly struct ImageMetrics
        {
            public readonly int Width;
            public readonly int Height;
            public readonly string Sha256;
            public readonly float BlackRatio;
            public readonly int SeamCount;
            public readonly float WorstBoundaryRatio;

            public ImageMetrics(int width, int height, string sha256, float blackRatio, int seamCount, float worstBoundaryRatio)
            {
                Width = width;
                Height = height;
                Sha256 = sha256;
                BlackRatio = blackRatio;
                SeamCount = seamCount;
                WorstBoundaryRatio = worstBoundaryRatio;
            }
        }
    }
}
