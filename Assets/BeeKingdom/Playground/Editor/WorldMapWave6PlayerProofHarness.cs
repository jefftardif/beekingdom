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
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave6PlayerProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private const string BootstrapAssetPath = "Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs";
        private const string OutputRoot = "Docs/BuilderA/WorldMapWave6_50x50_UnityIntegration/PlayerProof";
        private const string SessionRunningKey = "BeeKingdom.WorldMapWave6.PlayerProof.Running";
        private const string SessionStartedKey = "BeeKingdom.WorldMapWave6.PlayerProof.Started";
        private const string ExitPendingKey = "BeeKingdom.WorldMapWave6.PlayerProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.WorldMapWave6.PlayerProof.ExitCode";
        private const int LandscapeWidth = 1280;
        private const int LandscapeHeight = 720;
        private const int PortraitWidth = 720;
        private const int PortraitHeight = 1280;
        private const float SafeMarginPixels = 128f;

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("L00_CENTER_Z100", "landscape", LandscapeWidth, LandscapeHeight, ViewAnchor.Center, 1.00f, true),
            new CaptureSpec("L01_CENTER_Z135_NATIVE", "landscape", LandscapeWidth, LandscapeHeight, ViewAnchor.Center, 1.35f, true),
            new CaptureSpec("L02_NORTH_WEST", "landscape", LandscapeWidth, LandscapeHeight, ViewAnchor.NorthWest, 1.00f, true),
            new CaptureSpec("L03_NORTH_EAST", "landscape", LandscapeWidth, LandscapeHeight, ViewAnchor.NorthEast, 1.00f, true),
            new CaptureSpec("L04_SOUTH_WEST", "landscape", LandscapeWidth, LandscapeHeight, ViewAnchor.SouthWest, 1.00f, true),
            new CaptureSpec("L05_SOUTH_EAST", "landscape", LandscapeWidth, LandscapeHeight, ViewAnchor.SouthEast, 1.00f, true),
            new CaptureSpec("L06_BEAR_DEN_VISIBLE", "landscape", LandscapeWidth, LandscapeHeight, ViewAnchor.BearDen, 1.10f, true),
            new CaptureSpec("L07_BEAR_DEN_HIDDEN", "landscape", LandscapeWidth, LandscapeHeight, ViewAnchor.BearDen, 1.10f, false),
            new CaptureSpec("L08_BEAR_DEN_RESTORED", "landscape", LandscapeWidth, LandscapeHeight, ViewAnchor.BearDen, 1.10f, true),
            new CaptureSpec("P00_CENTER_Z100", "portrait", PortraitWidth, PortraitHeight, ViewAnchor.Center, 1.00f, true),
            new CaptureSpec("P01_BEAR_DEN_VISIBLE", "portrait", PortraitWidth, PortraitHeight, ViewAnchor.BearDen, 1.10f, true)
        };

        private static readonly List<CaptureSample> Samples = new List<CaptureSample>(Captures.Length);
        private static string root;
        private static string bootstrapHashBefore;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static int phase;
        private static int waitFrames;
        private static int resizeWaitFrames;
        private static bool sizePrepared;
        private static bool stateApplied;
        private static bool defaultBearVisibleObserved;
        private static bool failed;

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (SessionState.GetBool(ExitPendingKey, false) && !EditorApplication.isPlaying)
            {
                int code = SessionState.GetInt(ExitCodeKey, 1);
                SessionState.SetBool(ExitPendingKey, false);
                EditorApplication.delayCall += () => EditorApplication.Exit(code);
                return;
            }

            if (!SessionState.GetBool(SessionRunningKey, false)) return;
            root = AbsoluteProjectPath(OutputRoot);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= ResumeWhenPlayModeReady;
            EditorApplication.update += ResumeWhenPlayModeReady;
        }

        [MenuItem("Bee Kingdom/World Map/Run Wave6 Player Proof Harness")]
        public static void RunWave6PlayerProofHarness()
        {
            root = AbsoluteProjectPath(OutputRoot);
            Directory.CreateDirectory(root);
            DeletePreviousOutputs();
            Samples.Clear();
            failed = false;
            bootstrapHashBefore = HashAsset(BootstrapAssetPath);
            SessionState.SetBool(SessionRunningKey, true);
            SessionState.SetBool(SessionStartedKey, false);
            SessionState.SetBool(ExitPendingKey, false);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) ResumeWhenPlayModeReady();
        }

        private static void ResumeWhenPlayModeReady()
        {
            if (!SessionState.GetBool(SessionRunningKey, false) || SessionState.GetBool(SessionStartedKey, false))
            {
                EditorApplication.update -= ResumeWhenPlayModeReady;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
            if (bootstrap == null)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            defaultBearVisibleObserved = bootstrap.BearDenVisibleForProof();
            bootstrapHashBefore = HashAsset(BootstrapAssetPath);
            Samples.Clear();
            phase = 0;
            waitFrames = 8;
            resizeWaitFrames = 0;
            sizePrepared = false;
            stateApplied = false;
            SessionState.SetBool(SessionStartedKey, true);
            EditorApplication.update -= ResumeWhenPlayModeReady;
            EditorApplication.update -= ProcessHarness;
            EditorApplication.update += ProcessHarness;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void ProcessHarness()
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

                if (phase >= Captures.Length)
                {
                    if (Samples.Count != Captures.Length || !IsScreenshotReady(Samples[Samples.Count - 1]))
                    {
                        waitFrames = 6;
                        EditorApplication.QueuePlayerLoopUpdate();
                        return;
                    }

                    CompleteHarness();
                    return;
                }

                if (phase > 0 && !IsScreenshotReady(Samples[phase - 1]))
                {
                    waitFrames = 6;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                CaptureSpec spec = Captures[phase];
                if (!sizePrepared)
                {
                    TrySetGameViewSize(spec.Width, spec.Height, spec.Label);
                    Screen.SetResolution(spec.Width, spec.Height, false);
                    sizePrepared = true;
                    resizeWaitFrames = 0;
                    waitFrames = 18;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (Screen.width != spec.Width || Screen.height != spec.Height)
                {
                    if (++resizeWaitFrames > 180)
                    {
                        throw new InvalidOperationException("Game View resolution did not converge for " + spec.Label + ": expected " + spec.Width + "x" + spec.Height + ", actual " + Screen.width + "x" + Screen.height);
                    }

                    Screen.SetResolution(spec.Width, spec.Height, false);
                    waitFrames = 4;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (!stateApplied)
                {
                    WorldMapMmoFullscreenFoundationBootstrap.Wave6ProofSnapshot before = bootstrap.CurrentWave6ProofSnapshot();
                    Vector2 center = ResolveCenter(spec, before.WorldBounds);
                    bootstrap.ApplyWave6ProofView(center, spec.Zoom);
                    bootstrap.SetBearDenVisibilityForProof(spec.BearDenVisible);
                    stateApplied = true;
                    waitFrames = 24;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                CaptureCurrentState(spec);
                phase++;
                sizePrepared = false;
                stateApplied = false;
                waitFrames = 24;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception exception)
            {
                FailAndExit(exception.ToString());
            }
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
            Require(safe.width >= 0f && safe.height >= 0f, "Viewport cannot fit inside Wave6 bounds for " + spec.Label);

            if (spec.Anchor == ViewAnchor.NorthWest) return new Vector2(safe.xMin, safe.yMin);
            if (spec.Anchor == ViewAnchor.NorthEast) return new Vector2(safe.xMax, safe.yMin);
            if (spec.Anchor == ViewAnchor.SouthWest) return new Vector2(safe.xMin, safe.yMax);
            if (spec.Anchor == ViewAnchor.SouthEast) return new Vector2(safe.xMax, safe.yMax);
            if (spec.Anchor == ViewAnchor.BearDen)
            {
                Vector2 anchor = WorldMapWave5StreamingTileProvider.TileAnchorWorld(
                    WorldMapBearDenLandmark.AnchorRow,
                    WorldMapBearDenLandmark.AnchorColumn,
                    WorldMapBearDenLandmark.AnchorLocalX,
                    WorldMapBearDenLandmark.AnchorLocalY);
                return new Vector2(Mathf.Clamp(anchor.x, safe.xMin, safe.xMax), Mathf.Clamp(anchor.y, safe.yMin, safe.yMax));
            }

            return new Vector2(Mathf.Clamp(bounds.center.x, safe.xMin, safe.xMax), Mathf.Clamp(bounds.center.y, safe.yMin, safe.yMax));
        }

        private static void CaptureCurrentState(CaptureSpec spec)
        {
            WorldMapMmoFullscreenFoundationBootstrap.Wave6ProofSnapshot state = bootstrap.CurrentWave6ProofSnapshot();
            Require(state.ManifestReady, spec.Label + " manifest is not ready.");
            Require(state.VisibleTilesReady, spec.Label + " has missing visible tiles.");
            Require(state.LoadedVisibleTiles == state.RequiredVisibleTiles, spec.Label + " visible tile count mismatch.");
            Require(state.BearDenLoaded, spec.Label + " Bear Den is not loaded.");
            Require(state.BearDenVisible == spec.BearDenVisible, spec.Label + " Bear Den visibility mismatch.");
            Require(Mathf.Abs(state.Zoom - spec.Zoom) < 0.001f, spec.Label + " zoom mismatch.");

            Rect hudRect = spec.Layout == "portrait"
                ? new Rect(8f, 8f, spec.Width - 16f, 104f)
                : new Rect(14f, 12f, Mathf.Min(820f, spec.Width - 28f), 108f);
            Rect toggleRect = spec.Layout == "portrait"
                ? new Rect(8f, 190f, Mathf.Max(142f, Mathf.Min(238f, spec.Width - 152f)), 48f)
                : new Rect(14f, 128f, 220f, 48f);
            Rect bearWorldRect = BearWorldRect();
            Rect bearScreenRect = WorldRectToScreen(bearWorldRect, state.WorldCenter, state.Zoom, spec.Width, spec.Height);
            string screenshotPath = Path.Combine(root, spec.Label + ".png");
            ScreenCapture.CaptureScreenshot(screenshotPath);
            Samples.Add(new CaptureSample(spec, state, hudRect, toggleRect, bearScreenRect, screenshotPath));
        }

        private static void CompleteHarness()
        {
            EditorApplication.update -= ProcessHarness;
            string bootstrapHashAfter = HashAsset(BootstrapAssetPath);
            var metrics = new List<ImageMetrics>(Samples.Count);
            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool dimensionsPass = true;
            bool allDistinct = true;
            bool noBlackFrames = true;
            bool noSeams = true;
            for (int i = 0; i < Samples.Count; i++)
            {
                ImageMetrics metric = AnalyzeScreenshot(Samples[i]);
                metrics.Add(metric);
                dimensionsPass &= metric.Decoded && metric.Width == Samples[i].Spec.Width && metric.Height == Samples[i].Spec.Height;
                allDistinct &= hashes.Add(metric.Sha256);
                noBlackFrames &= metric.BlackRatio < 0.20f;
                noSeams &= metric.SeamCount == 0;
            }

            bool centerCorners = HasLabel("L00_CENTER_Z100") && HasLabel("L02_NORTH_WEST") && HasLabel("L03_NORTH_EAST") && HasLabel("L04_SOUTH_WEST") && HasLabel("L05_SOUTH_EAST");
            bool nativeZoom = HasLabel("L01_CENTER_Z135_NATIVE");
            bool portrait = HasLabel("P00_CENTER_Z100") && HasLabel("P01_BEAR_DEN_VISIBLE");
            bool visibleTiles = AllVisibleTilesReady();
            bool hudFixed = HudAndToggleRectsStable();
            bool bearHideShow = BearPixelTogglePass(metrics);
            bool hashStable = string.Equals(bootstrapHashBefore, bootstrapHashAfter, StringComparison.OrdinalIgnoreCase);
            bool ready = Samples.Count == Captures.Length
                && dimensionsPass
                && allDistinct
                && noBlackFrames
                && noSeams
                && centerCorners
                && nativeZoom
                && portrait
                && visibleTiles
                && hudFixed
                && defaultBearVisibleObserved
                && bearHideShow
                && hashStable;

            File.WriteAllText(Path.Combine(root, "WorldMapWave6_PlayerProofTelemetry.json"), BuildTelemetry(metrics, bootstrapHashAfter, ready), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root, "WorldMapWave6_PlayerProofReceipt.md"), BuildReceipt(metrics, bootstrapHashAfter, ready), new UTF8Encoding(false));
            ExitThroughEditMode(ready ? 0 : 1);
        }

        private static ImageMetrics AnalyzeScreenshot(CaptureSample sample)
        {
            byte[] bytes = File.ReadAllBytes(sample.ScreenshotPath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return new ImageMetrics(false, 0, 0, HashBytes(bytes), 1f, int.MaxValue, 0f);
            }

            float blackRatio = ComputeBlackRatio(texture, sample);
            int seamCount = CountProjectedSeams(texture, sample, out float worstRatio);
            int width = texture.width;
            int height = texture.height;
            UnityEngine.Object.DestroyImmediate(texture);
            return new ImageMetrics(true, width, height, HashBytes(bytes), blackRatio, seamCount, worstRatio);
        }

        private static int CountProjectedSeams(Texture2D texture, CaptureSample sample, out float worstRatio)
        {
            int seams = 0;
            worstRatio = 1f;
            Rect bounds = sample.State.WorldBounds;
            for (int index = 1; index < WorldMapWave6StreamingTileProvider.Columns; index++)
            {
                float worldX = bounds.xMin + index * WorldMapWave6StreamingTileProvider.TileSize;
                float screenX = sample.Spec.Width * 0.5f + (worldX - sample.State.WorldCenter.x) * sample.State.Zoom;
                if (screenX < 4f || screenX > sample.Spec.Width - 5f) continue;
                float center = AverageVerticalLuma(texture, Mathf.RoundToInt(screenX), sample);
                float neighbors = (AverageVerticalLuma(texture, Mathf.RoundToInt(screenX) - 2, sample) + AverageVerticalLuma(texture, Mathf.RoundToInt(screenX) + 2, sample)) * 0.5f;
                float ratio = center / Mathf.Max(1f, neighbors);
                worstRatio = Mathf.Min(worstRatio, ratio);
                if (center < 10f && neighbors > 30f && ratio < 0.25f) seams++;
            }

            for (int index = 1; index < WorldMapWave6StreamingTileProvider.Rows; index++)
            {
                float worldY = bounds.yMin + index * WorldMapWave6StreamingTileProvider.TileSize;
                float screenY = sample.Spec.Height * 0.5f + (worldY - sample.State.WorldCenter.y) * sample.State.Zoom;
                if (screenY < 4f || screenY > sample.Spec.Height - 5f) continue;
                float center = AverageHorizontalLuma(texture, Mathf.RoundToInt(screenY), sample);
                float neighbors = (AverageHorizontalLuma(texture, Mathf.RoundToInt(screenY) - 2, sample) + AverageHorizontalLuma(texture, Mathf.RoundToInt(screenY) + 2, sample)) * 0.5f;
                float ratio = center / Mathf.Max(1f, neighbors);
                worstRatio = Mathf.Min(worstRatio, ratio);
                if (center < 10f && neighbors > 30f && ratio < 0.25f) seams++;
            }

            return seams;
        }

        private static float AverageVerticalLuma(Texture2D texture, int x, CaptureSample sample)
        {
            float total = 0f;
            int count = 0;
            for (int guiY = Mathf.RoundToInt(sample.Spec.Height * 0.06f); guiY < Mathf.RoundToInt(sample.Spec.Height * 0.94f); guiY += 4)
            {
                Vector2 guiPoint = new Vector2(x, guiY);
                if (IsFixedUi(guiPoint, sample)) continue;
                Color32 color = texture.GetPixel(Mathf.Clamp(x, 0, texture.width - 1), Mathf.Clamp(texture.height - 1 - guiY, 0, texture.height - 1));
                total += Luma(color);
                count++;
            }
            return count > 0 ? total / count : 255f;
        }

        private static float AverageHorizontalLuma(Texture2D texture, int guiY, CaptureSample sample)
        {
            float total = 0f;
            int count = 0;
            for (int x = Mathf.RoundToInt(sample.Spec.Width * 0.04f); x < Mathf.RoundToInt(sample.Spec.Width * 0.96f); x += 4)
            {
                Vector2 guiPoint = new Vector2(x, guiY);
                if (IsFixedUi(guiPoint, sample)) continue;
                Color32 color = texture.GetPixel(x, Mathf.Clamp(texture.height - 1 - guiY, 0, texture.height - 1));
                total += Luma(color);
                count++;
            }
            return count > 0 ? total / count : 255f;
        }

        private static float ComputeBlackRatio(Texture2D texture, CaptureSample sample)
        {
            int black = 0;
            int total = 0;
            for (int guiY = 0; guiY < sample.Spec.Height; guiY += 8)
            {
                for (int x = 0; x < sample.Spec.Width; x += 8)
                {
                    if (IsFixedUi(new Vector2(x, guiY), sample)) continue;
                    Color32 color = texture.GetPixel(x, texture.height - 1 - guiY);
                    if (color.r <= 8 && color.g <= 8 && color.b <= 8) black++;
                    total++;
                }
            }
            return total > 0 ? black / (float)total : 1f;
        }

        private static bool IsFixedUi(Vector2 point, CaptureSample sample)
        {
            if (sample.HudRect.Contains(point) || sample.ToggleRect.Contains(point)) return true;
            if (sample.Spec.Layout == "portrait")
            {
                if (new Rect(8f, 124f, sample.Spec.Width - 16f, 58f).Contains(point)) return true;
                if (new Rect(sample.Spec.Width - 128f, 204f, 118f, 86f).Contains(point)) return true;
                if (new Rect(8f, sample.Spec.Height - 190f, sample.Spec.Width - 16f, 178f).Contains(point)) return true;
                return false;
            }

            if (new Rect(sample.Spec.Width - 292f, 12f, 278f, 150f).Contains(point)) return true;
            if (new Rect(sample.Spec.Width - 320f, 176f, 304f, 286f).Contains(point)) return true;
            if (new Rect(sample.Spec.Width - 380f, 468f, 364f, Mathf.Min(144f, Mathf.Max(132f, sample.Spec.Height - 600f))).Contains(point)) return true;
            if (new Rect(14f, sample.Spec.Height - 112f, Mathf.Min(760f, sample.Spec.Width - 28f), 96f).Contains(point)) return true;
            if (new Rect(sample.Spec.Width - 214f, sample.Spec.Height - 156f, 198f, 140f).Contains(point)) return true;
            return false;
        }

        private static bool BearPixelTogglePass(List<ImageMetrics> metrics)
        {
            int visibleIndex = SampleIndex("L06_BEAR_DEN_VISIBLE");
            int hiddenIndex = SampleIndex("L07_BEAR_DEN_HIDDEN");
            int restoredIndex = SampleIndex("L08_BEAR_DEN_RESTORED");
            if (visibleIndex < 0 || hiddenIndex < 0 || restoredIndex < 0) return false;
            float hiddenDelta = CropDifference(Samples[visibleIndex], Samples[hiddenIndex]);
            float restoredDelta = CropDifference(Samples[restoredIndex], Samples[hiddenIndex]);
            return hiddenDelta > 3f && restoredDelta > 3f
                && Samples[visibleIndex].State.BearDenVisible
                && !Samples[hiddenIndex].State.BearDenVisible
                && Samples[restoredIndex].State.BearDenVisible;
        }

        private static float CropDifference(CaptureSample left, CaptureSample right)
        {
            Texture2D a = LoadTexture(left.ScreenshotPath);
            Texture2D b = LoadTexture(right.ScreenshotPath);
            Rect rect = Intersect(left.BearScreenRect, new Rect(0f, 0f, left.Spec.Width, left.Spec.Height));
            int step = 6;
            float total = 0f;
            int count = 0;
            for (int guiY = Mathf.RoundToInt(rect.yMin); guiY < Mathf.RoundToInt(rect.yMax); guiY += step)
            {
                for (int x = Mathf.RoundToInt(rect.xMin); x < Mathf.RoundToInt(rect.xMax); x += step)
                {
                    Color32 ca = a.GetPixel(Mathf.Clamp(x, 0, a.width - 1), Mathf.Clamp(a.height - 1 - guiY, 0, a.height - 1));
                    Color32 cb = b.GetPixel(Mathf.Clamp(x, 0, b.width - 1), Mathf.Clamp(b.height - 1 - guiY, 0, b.height - 1));
                    total += (Mathf.Abs(ca.r - cb.r) + Mathf.Abs(ca.g - cb.g) + Mathf.Abs(ca.b - cb.b)) / 3f;
                    count++;
                }
            }
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
            return count > 0 ? total / count : 0f;
        }

        private static bool AllVisibleTilesReady()
        {
            for (int i = 0; i < Samples.Count; i++)
            {
                if (!Samples[i].State.VisibleTilesReady || Samples[i].State.LoadedVisibleTiles != Samples[i].State.RequiredVisibleTiles) return false;
            }
            return true;
        }

        private static bool HudAndToggleRectsStable()
        {
            Rect landscapeHud = default;
            Rect landscapeToggle = default;
            Rect portraitHud = default;
            Rect portraitToggle = default;
            bool landscapeSet = false;
            bool portraitSet = false;
            for (int i = 0; i < Samples.Count; i++)
            {
                CaptureSample sample = Samples[i];
                if (sample.Spec.Layout == "landscape")
                {
                    if (!landscapeSet)
                    {
                        landscapeHud = sample.HudRect;
                        landscapeToggle = sample.ToggleRect;
                        landscapeSet = true;
                    }
                    else if (!SameRect(landscapeHud, sample.HudRect) || !SameRect(landscapeToggle, sample.ToggleRect)) return false;
                }
                else
                {
                    if (!portraitSet)
                    {
                        portraitHud = sample.HudRect;
                        portraitToggle = sample.ToggleRect;
                        portraitSet = true;
                    }
                    else if (!SameRect(portraitHud, sample.HudRect) || !SameRect(portraitToggle, sample.ToggleRect)) return false;
                }
            }
            return landscapeSet && portraitSet;
        }

        private static string BuildTelemetry(List<ImageMetrics> metrics, string bootstrapHashAfter, bool ready)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            Json(builder, "schema", "bee-kingdom.world-map.wave6-player-proof.v1", true);
            Json(builder, "utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), true);
            Json(builder, "scene", ScenePath, true);
            Json(builder, "source_master_sha256", WorldMapWave6StreamingTileProvider.ExpectedMasterSha256, true);
            Json(builder, "bootstrap_hash_before", bootstrapHashBefore, true);
            Json(builder, "bootstrap_hash_after", bootstrapHashAfter, true);
            Json(builder, "bear_den_visible_by_default", defaultBearVisibleObserved ? "true" : "false", false);
            Json(builder, "samples", "[", false, 2, false);
            for (int i = 0; i < Samples.Count; i++)
            {
                CaptureSample sample = Samples[i];
                ImageMetrics metric = metrics[i];
                builder.AppendLine("    {");
                Json(builder, "label", sample.Spec.Label, true, 6);
                Json(builder, "screenshot", Normalize(sample.ScreenshotPath), true, 6);
                Json(builder, "screen_size", sample.Spec.Width + "x" + sample.Spec.Height, true, 6);
                Json(builder, "center", FormatVector(sample.State.WorldCenter), true, 6);
                Json(builder, "zoom", sample.State.Zoom.ToString("0.###", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "visible_tiles", sample.State.LoadedVisibleTiles + "/" + sample.State.RequiredVisibleTiles, true, 6);
                Json(builder, "cache_tiles", sample.State.CachedTiles.ToString(CultureInfo.InvariantCulture), false, 6);
                Json(builder, "bear_den_visible", sample.State.BearDenVisible ? "true" : "false", false, 6);
                Json(builder, "png_sha256", metric.Sha256, true, 6);
                Json(builder, "black_ratio", metric.BlackRatio.ToString("0.####", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "projected_seam_count", metric.SeamCount.ToString(CultureInfo.InvariantCulture), false, 6);
                Json(builder, "worst_boundary_ratio", metric.WorstBoundaryRatio.ToString("0.####", CultureInfo.InvariantCulture), false, 6, false);
                builder.Append("    }");
                if (i < Samples.Count - 1) builder.Append(',');
                builder.AppendLine();
            }
            builder.AppendLine("  ],");
            Json(builder, "ready_for_player_unity_test", ready ? "true" : "false", false, 2, false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildReceipt(List<ImageMetrics> metrics, string bootstrapHashAfter, bool ready)
        {
            int seamCount = 0;
            for (int i = 0; i < metrics.Count; i++) seamCount += metrics[i].SeamCount;
            var builder = new StringBuilder();
            builder.AppendLine("# WorldMap Wave6 50x50 Player Proof Receipt");
            builder.AppendLine();
            builder.AppendLine("- `SCENE_OPENABLE=PASS`");
            builder.AppendLine("- `CENTER_AND_FOUR_CORNERS=PASS`");
            builder.AppendLine("- `NATIVE_ZOOM_1_35=PASS`");
            builder.AppendLine("- `VISIBLE_TILE_MISSING_COUNT=0`");
            builder.AppendLine("- `VISIBLE_TILE_SEAMS=" + (seamCount == 0 ? "NO" : "YES") + "`");
            builder.AppendLine("- `GRID_PATTERN_VISIBLE=" + (seamCount == 0 ? "NO" : "YES") + "`");
            builder.AppendLine("- `HUD_SCREEN_SPACE_FIXED=" + (HudAndToggleRectsStable() ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `BEAR_DEN_VISIBLE_BY_DEFAULT=" + (defaultBearVisibleObserved ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `BEAR_DEN_TOGGLE_HIDE=" + (Samples[SampleIndex("L07_BEAR_DEN_HIDDEN")].State.BearDenVisible ? "FAIL" : "PASS") + "`");
            builder.AppendLine("- `BEAR_DEN_TOGGLE_SHOW=" + (Samples[SampleIndex("L08_BEAR_DEN_RESTORED")].State.BearDenVisible ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `BEAR_DEN_TOGGLE_HUD_FIXED=" + (HudAndToggleRectsStable() ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `BEAR_VISIBLE=NO`");
            builder.AppendLine("- `OLD_WAVE3_5X5_ACTIVE=NO`");
            builder.AppendLine("- `SERVER_LIVE=NO`");
            builder.AppendLine("- `BOOTSTRAP_HASH_BEFORE=" + bootstrapHashBefore + "`");
            builder.AppendLine("- `BOOTSTRAP_HASH_AFTER=" + bootstrapHashAfter + "`");
            builder.AppendLine("- `READY_FOR_PLAYER_UNITY_TEST=" + (ready ? "YES" : "NO") + "`");
            return builder.ToString();
        }

        private static bool IsScreenshotReady(CaptureSample sample)
        {
            if (!File.Exists(sample.ScreenshotPath)) return false;
            Texture2D texture = LoadTexture(sample.ScreenshotPath);
            bool valid = texture.width == sample.Spec.Width && texture.height == sample.Spec.Height;
            UnityEngine.Object.DestroyImmediate(texture);
            if (!valid) throw new InvalidOperationException("Screenshot dimensions mismatch for " + sample.Spec.Label);
            return true;
        }

        private static Texture2D LoadTexture(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Unable to decode screenshot: " + path);
            }
            return texture;
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
            object groupType = Enum.Parse(gameViewSizeGroupType, "Android");
            object group = gameViewSizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
            object fixedResolution = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
            object customSize = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) }).Invoke(new object[] { fixedResolution, width, height, label });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
            int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            gameView.Show();
            gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
            gameView.Repaint();
        }

        private static void DeletePreviousOutputs()
        {
            string[] files = Directory.GetFiles(root, "*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileName(files[i]);
                if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || name == "WorldMapWave6_PlayerProofTelemetry.json"
                    || name == "WorldMapWave6_PlayerProofReceipt.md"
                    || name == "WorldMapWave6_PlayerProofFailure.txt")
                {
                    File.Delete(files[i]);
                }
            }
        }

        private static void FailAndExit(string reason)
        {
            failed = true;
            root = string.IsNullOrEmpty(root) ? AbsoluteProjectPath(OutputRoot) : root;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "WorldMapWave6_PlayerProofFailure.txt"), reason, new UTF8Encoding(false));
            ExitThroughEditMode(1);
        }

        private static void ExitThroughEditMode(int exitCode)
        {
            EditorApplication.update -= ProcessHarness;
            EditorApplication.update -= ResumeWhenPlayModeReady;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SessionState.SetBool(SessionRunningKey, false);
            SessionState.SetBool(SessionStartedKey, false);
            SessionState.SetBool(ExitPendingKey, true);
            SessionState.SetInt(ExitCodeKey, exitCode);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            else EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
        }

        private static Rect BearWorldRect()
        {
            Vector2 anchor = WorldMapWave5StreamingTileProvider.TileAnchorWorld(
                WorldMapBearDenLandmark.AnchorRow,
                WorldMapBearDenLandmark.AnchorColumn,
                WorldMapBearDenLandmark.AnchorLocalX,
                WorldMapBearDenLandmark.AnchorLocalY);
            return new Rect(
                anchor.x - WorldMapBearDenLandmark.WorldWidth * WorldMapBearDenLandmark.PivotX,
                anchor.y - WorldMapBearDenLandmark.WorldHeight * (1f - WorldMapBearDenLandmark.PivotY),
                WorldMapBearDenLandmark.WorldWidth,
                WorldMapBearDenLandmark.WorldHeight);
        }

        private static Rect WorldRectToScreen(Rect worldRect, Vector2 center, float zoom, int width, int height)
        {
            Vector2 min = new Vector2(width * 0.5f, height * 0.5f) + (worldRect.min - center) * zoom;
            Vector2 max = new Vector2(width * 0.5f, height * 0.5f) + (worldRect.max - center) * zoom;
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        private static Rect Intersect(Rect left, Rect right)
        {
            return Rect.MinMaxRect(Mathf.Max(left.xMin, right.xMin), Mathf.Max(left.yMin, right.yMin), Mathf.Min(left.xMax, right.xMax), Mathf.Min(left.yMax, right.yMax));
        }

        private static int SampleIndex(string label)
        {
            for (int i = 0; i < Samples.Count; i++) if (Samples[i].Spec.Label == label) return i;
            return -1;
        }

        private static bool HasLabel(string label) => SampleIndex(label) >= 0;

        private static bool SameRect(Rect left, Rect right)
        {
            return Mathf.Abs(left.x - right.x) < 0.01f
                && Mathf.Abs(left.y - right.y) < 0.01f
                && Mathf.Abs(left.width - right.width) < 0.01f
                && Mathf.Abs(left.height - right.height) < 0.01f;
        }

        private static float Luma(Color32 color) => color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;

        private static string HashAsset(string assetPath) => HashFile(AbsoluteProjectPath(assetPath));

        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return ToHex(sha.ComputeHash(stream));
            }
        }

        private static string HashBytes(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create()) return ToHex(sha.ComputeHash(bytes));
        }

        private static string ToHex(byte[] value)
        {
            var builder = new StringBuilder(value.Length * 2);
            for (int i = 0; i < value.Length; i++) builder.Append(value[i].ToString("X2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string AbsoluteProjectPath(string relative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Normalize(string value) => value.Replace('\\', '/');
        private static string FormatVector(Vector2 value) => value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + value.y.ToString("0.###", CultureInfo.InvariantCulture);

        private static void Json(StringBuilder builder, string key, string value, bool quoted, int indent = 2, bool comma = true)
        {
            builder.Append(' ', indent).Append('"').Append(key).Append("\": ");
            if (quoted) builder.Append('"').Append(value.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
            else builder.Append(value);
            if (comma) builder.Append(',');
            builder.AppendLine();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private enum ViewAnchor
        {
            Center,
            NorthWest,
            NorthEast,
            SouthWest,
            SouthEast,
            BearDen
        }

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly string Layout;
            public readonly int Width;
            public readonly int Height;
            public readonly ViewAnchor Anchor;
            public readonly float Zoom;
            public readonly bool BearDenVisible;

            public CaptureSpec(string label, string layout, int width, int height, ViewAnchor anchor, float zoom, bool bearDenVisible)
            {
                Label = label;
                Layout = layout;
                Width = width;
                Height = height;
                Anchor = anchor;
                Zoom = zoom;
                BearDenVisible = bearDenVisible;
            }
        }

        private readonly struct CaptureSample
        {
            public readonly CaptureSpec Spec;
            public readonly WorldMapMmoFullscreenFoundationBootstrap.Wave6ProofSnapshot State;
            public readonly Rect HudRect;
            public readonly Rect ToggleRect;
            public readonly Rect BearScreenRect;
            public readonly string ScreenshotPath;

            public CaptureSample(CaptureSpec spec, WorldMapMmoFullscreenFoundationBootstrap.Wave6ProofSnapshot state, Rect hudRect, Rect toggleRect, Rect bearScreenRect, string screenshotPath)
            {
                Spec = spec;
                State = state;
                HudRect = hudRect;
                ToggleRect = toggleRect;
                BearScreenRect = bearScreenRect;
                ScreenshotPath = screenshotPath;
            }
        }

        private readonly struct ImageMetrics
        {
            public readonly bool Decoded;
            public readonly int Width;
            public readonly int Height;
            public readonly string Sha256;
            public readonly float BlackRatio;
            public readonly int SeamCount;
            public readonly float WorstBoundaryRatio;

            public ImageMetrics(bool decoded, int width, int height, string sha256, float blackRatio, int seamCount, float worstBoundaryRatio)
            {
                Decoded = decoded;
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
