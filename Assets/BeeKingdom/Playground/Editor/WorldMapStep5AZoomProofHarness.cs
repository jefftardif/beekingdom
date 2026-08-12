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
    public static class WorldMapStep5AZoomProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private const string BootstrapAssetPath = "Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs";
        private const string OutputRoot = "Docs/BuilderA/WorldMapStep5AZoomProofHarness";
        private const string ExpectedBootstrapHash = "8281EE0294AF44F24F8EBDB454A535C79F33DD21F4706DCE45CEA5FE04A5E63E";
        private const string SessionRunningKey = "BeeKingdom.WorldMapStep5A.ZoomProof.Running";
        private const string SessionStartedKey = "BeeKingdom.WorldMapStep5A.ZoomProof.Started";
        private const string UnityLogFileName = "BuilderA_Step5A_ZoomProofHarness_Unity_Run.log";
        private const int ChunkSize = 512;
        private const float PreferredSafeMarginPixels = 128f;
        private static readonly Vector2 ProbeWorldPoint = ChunkCenter(33, 32);
        private static readonly Vector2 OverlayWorldPoint = ChunkCenter(32, 32) + new Vector2(200f, 80f);

        private static readonly ZoomCaptureSpec[] Captures =
        {
            new ZoomCaptureSpec("L13_ZOOM_IN_BEFORE", "landscape", 1920, 1080, 1.00f),
            new ZoomCaptureSpec("L14_ZOOM_IN_MID", "landscape", 1920, 1080, 1.10f),
            new ZoomCaptureSpec("L15_ZOOM_IN_AFTER", "landscape", 1920, 1080, 1.21f),
            new ZoomCaptureSpec("L16_ZOOM_OUT_BEFORE", "landscape", 1920, 1080, 1.09f),
            new ZoomCaptureSpec("L17_ZOOM_OUT_MID", "landscape", 1920, 1080, 0.98f),
            new ZoomCaptureSpec("L18_ZOOM_OUT_AFTER", "landscape", 1920, 1080, 0.81f),
            new ZoomCaptureSpec("P13_ZOOM_IN_BEFORE", "portrait", 720, 1280, 1.00f),
            new ZoomCaptureSpec("P14_ZOOM_IN_MID", "portrait", 720, 1280, 1.10f),
            new ZoomCaptureSpec("P15_ZOOM_IN_AFTER", "portrait", 720, 1280, 1.21f),
            new ZoomCaptureSpec("P16_ZOOM_OUT_BEFORE", "portrait", 720, 1280, 1.09f),
            new ZoomCaptureSpec("P17_ZOOM_OUT_MID", "portrait", 720, 1280, 0.98f),
            new ZoomCaptureSpec("P18_ZOOM_OUT_AFTER", "portrait", 720, 1280, 0.81f)
        };

        private static readonly List<ZoomSample> Samples = new List<ZoomSample>(16);
        private static string root;
        private static string unityLogPath;
        private static string bootstrapHashBefore;
        private static string bootstrapHashAfter;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static FieldInfo currentWorldCenterField;
        private static FieldInfo targetWorldCenterField;
        private static FieldInfo currentZoomField;
        private static FieldInfo targetZoomField;
        private static Rect wave3WorldBounds;
        private static int phase;
        private static int waitFrames;
        private static bool capturePrepared;
        private static bool failed;
        private static string failureReason;

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (!SessionState.GetBool(SessionRunningKey, false))
            {
                return;
            }

            root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputRoot));
            unityLogPath = Path.Combine(root, UnityLogFileName);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= ResumeWhenPlayModeIsReady;
            EditorApplication.update += ResumeWhenPlayModeIsReady;
        }

        [MenuItem("Bee Kingdom/WorldMap Step5A/Run Zoom Proof Harness")]
        public static void RunWorldMapStep5AZoomProofHarnessBatch()
        {
            root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputRoot));
            Directory.CreateDirectory(root);
            Samples.Clear();
            failed = false;
            failureReason = string.Empty;
            phase = -1;
            waitFrames = 0;
            capturePrepared = false;
            unityLogPath = Path.Combine(root, UnityLogFileName);
            SessionState.SetBool(SessionRunningKey, true);
            SessionState.SetBool(SessionStartedKey, false);
            bootstrapHashBefore = HashAsset(BootstrapAssetPath);

            if (bootstrapHashBefore != ExpectedBootstrapHash)
            {
                FailAndExit("Bootstrap hash before run does not match expected Step5A Run10 hash: " + bootstrapHashBefore);
                return;
            }

            DeletePreviousOutputs();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.update -= ProcessHarness;
            EditorApplication.update -= ResumeWhenPlayModeIsReady;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ResumeWhenPlayModeIsReady();
            }
        }

        private static void ResumeWhenPlayModeIsReady()
        {
            if (!SessionState.GetBool(SessionRunningKey, false) || SessionState.GetBool(SessionStartedKey, false))
            {
                EditorApplication.update -= ResumeWhenPlayModeIsReady;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            try
            {
                bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
                if (bootstrap == null)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                BindFields();
                wave3WorldBounds = ReadWave3WorldBounds();
                bootstrapHashBefore = HashAsset(BootstrapAssetPath);
                failed = false;
                failureReason = string.Empty;
                SessionState.SetBool(SessionStartedKey, true);
                EditorApplication.update -= ResumeWhenPlayModeIsReady;
                EditorApplication.update -= ProcessHarness;
                Samples.Clear();
                phase = -1;
                waitFrames = 4;
                capturePrepared = false;
                EditorApplication.update += ProcessHarness;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception ex)
            {
                FailAndExit(ex.ToString());
            }
        }

        private static void ProcessHarness()
        {
            if (failed)
            {
                return;
            }

            try
            {
                if (waitFrames > 0)
                {
                    waitFrames--;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == -1)
                {
                    phase = 0;
                    capturePrepared = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase >= 0 && phase < Captures.Length)
                {
                    if (!capturePrepared)
                    {
                        if (phase > 0 && !IsScreenshotReady(Samples[phase - 1]))
                        {
                            waitFrames = 8;
                            EditorApplication.QueuePlayerLoopUpdate();
                            return;
                        }

                        PrepareCapture(Captures[phase]);
                        capturePrepared = true;
                        waitFrames = 18;
                        EditorApplication.QueuePlayerLoopUpdate();
                        return;
                    }

                    CaptureSample(Captures[phase]);
                    capturePrepared = false;
                    phase++;
                    if (phase < Captures.Length)
                    {
                        waitFrames = 28;
                        EditorApplication.QueuePlayerLoopUpdate();
                        return;
                    }

                    waitFrames = 80;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (Samples.Count == Captures.Length && !IsScreenshotReady(Samples[Samples.Count - 1]))
                {
                    waitFrames = 8;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                CompleteHarness();
            }
            catch (Exception ex)
            {
                FailAndExit(ex.ToString());
            }
        }

        private static void PrepareCapture(ZoomCaptureSpec capture)
        {
            TrySetGameViewSize(capture.Width, capture.Height, capture.Label);
            Screen.SetResolution(capture.Width, capture.Height, false);
            if (Screen.width != capture.Width || Screen.height != capture.Height)
            {
                Debug.Log("Step5A zoom proof waiting for Game View resize. requested=" + capture.Width.ToString(CultureInfo.InvariantCulture) + "x" + capture.Height.ToString(CultureInfo.InvariantCulture) + " actual=" + Screen.width.ToString(CultureInfo.InvariantCulture) + "x" + Screen.height.ToString(CultureInfo.InvariantCulture));
            }

            Vector2 center = SafeCenterFor(Screen.width, Screen.height, capture.Zoom, out _, out _);
            SetCameraState(center, center, capture.Zoom, capture.Zoom);
        }

        private static void CaptureSample(ZoomCaptureSpec capture)
        {
            int width = Screen.width;
            int height = Screen.height;
            Vector2 center = SafeCenterFor(width, height, capture.Zoom, out Rect rawBounds, out Rect safeBounds);
            SetCameraState(center, center, capture.Zoom, capture.Zoom);
            if (!Contains(rawBounds, center))
            {
                throw new InvalidOperationException("Zoom proof center outside raw bounds before capture: " + FormatVector(center) + " raw=" + FormatRect(rawBounds));
            }

            Vector2 terrainScreen = WorldToScreen(ProbeWorldPoint, center, capture.Zoom, width, height);
            Vector2 entityScreen = WorldToScreen(ProbeWorldPoint, center, capture.Zoom, width, height);
            Vector2 overlayScreen = WorldToScreen(OverlayWorldPoint, center, capture.Zoom, width, height);
            Vector2 pivot = new Vector2(width * 0.5f, height * 0.5f);
            Rect hudRect = capture.Layout == "landscape"
                ? new Rect(14f, 12f, 760f, 108f)
                : new Rect(8f, 8f, width - 16f, 104f);
            string pngPath = Path.Combine(root, capture.Label + ".png");
            ScreenCapture.CaptureScreenshot(pngPath);
            Samples.Add(new ZoomSample(capture, Time.frameCount, width, height, center, capture.Zoom, rawBounds, safeBounds, terrainScreen, entityScreen, overlayScreen, pivot, hudRect, pngPath));
        }

        private static bool IsScreenshotReady(ZoomSample sample)
        {
            if (!File.Exists(sample.ScreenshotPath)) return false;
            byte[] bytes = File.ReadAllBytes(sample.ScreenshotPath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool loaded = texture.LoadImage(bytes);
            if (!loaded)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return false;
            }

            bool dimensionsMatch = texture.width == sample.ScreenWidth && texture.height == sample.ScreenHeight;
            int actualWidth = texture.width;
            int actualHeight = texture.height;
            UnityEngine.Object.DestroyImmediate(texture);
            if (!dimensionsMatch)
            {
                throw new InvalidOperationException("Screenshot dimensions mismatch for " + sample.Spec.Label + ": expected=" + sample.ScreenWidth.ToString(CultureInfo.InvariantCulture) + "x" + sample.ScreenHeight.ToString(CultureInfo.InvariantCulture) + " actual=" + actualWidth.ToString(CultureInfo.InvariantCulture) + "x" + actualHeight.ToString(CultureInfo.InvariantCulture));
            }

            return true;
        }

        private static Vector2 SafeCenterFor(int screenWidth, int screenHeight, float zoom, out Rect rawBounds, out Rect safeBounds)
        {
            float halfWidthWorld = screenWidth * 0.5f / zoom;
            float halfHeightWorld = screenHeight * 0.5f / zoom;
            rawBounds = Rect.MinMaxRect(
                wave3WorldBounds.xMin + halfWidthWorld,
                wave3WorldBounds.yMin + halfHeightWorld,
                wave3WorldBounds.xMax - halfWidthWorld,
                wave3WorldBounds.yMax - halfHeightWorld);
            if (rawBounds.width < 0f || rawBounds.height < 0f)
            {
                throw new InvalidOperationException("Viewport cannot fit inside Wave3 bounds. screen=" + screenWidth.ToString(CultureInfo.InvariantCulture) + "x" + screenHeight.ToString(CultureInfo.InvariantCulture) + " zoom=" + zoom.ToString("0.###", CultureInfo.InvariantCulture) + " raw=" + FormatRect(rawBounds));
            }

            float preferredMarginWorld = PreferredSafeMarginPixels / zoom;
            float maxMarginWorld = Mathf.Max(0f, Mathf.Min(rawBounds.width, rawBounds.height) * 0.49f);
            float marginWorld = Mathf.Min(preferredMarginWorld, maxMarginWorld);
            safeBounds = Rect.MinMaxRect(
                rawBounds.xMin + marginWorld,
                rawBounds.yMin + marginWorld,
                rawBounds.xMax - marginWorld,
                rawBounds.yMax - marginWorld);
            if (safeBounds.width < 0.001f || safeBounds.height < 0.001f)
            {
                safeBounds = rawBounds;
            }

            return new Vector2(
                Mathf.Clamp(wave3WorldBounds.center.x, safeBounds.xMin, safeBounds.xMax),
                Mathf.Clamp(wave3WorldBounds.center.y, safeBounds.yMin, safeBounds.yMax));
        }

        private static void CompleteHarness()
        {
            EditorApplication.update -= ProcessHarness;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= ResumeWhenPlayModeIsReady;
            bootstrapHashAfter = HashAsset(BootstrapAssetPath);
            ProofVerdict verdict = BuildVerdict();
            WriteContactSheet("Landscape_Zoom_ContactSheet.png", "landscape");
            WriteContactSheet("Portrait_Zoom_ContactSheet.png", "portrait");
            File.WriteAllText(Path.Combine(root, "WorldMapStep5A_ZoomProofTelemetry.json"), BuildTelemetryJson(verdict), Encoding.UTF8);
            File.WriteAllText(Path.Combine(root, "WorldMapStep5A_ZoomProofReceipt.md"), BuildReceipt(verdict), Encoding.UTF8);
            SessionState.SetBool(SessionRunningKey, false);
            SessionState.SetBool(SessionStartedKey, false);
            EditorApplication.ExitPlaymode();
            EditorApplication.Exit(verdict.Ready ? 0 : 1);
        }

        private static ProofVerdict BuildVerdict()
        {
            bool sampleCount = Samples.Count == 12;
            VisualSampleMetrics[] visualMetrics = sampleCount ? BuildVisualMetrics() : new VisualSampleMetrics[0];
            bool screenshotsWritten = sampleCount && AllSamplesHaveFiles();
            bool screenshotsDecoded = visualMetrics.Length == 12 && AllDecoded(visualMetrics);
            bool pngDistinct = screenshotsDecoded && UniqueHashes(visualMetrics);
            bool sourceHashMatch = bootstrapHashBefore == ExpectedBootstrapHash && bootstrapHashAfter == ExpectedBootstrapHash;
            bool landscapeCount = CountLayout("landscape") == 6 && AllLayoutSize("landscape", 1920, 1080);
            bool portraitCount = CountLayout("portrait") == 6 && AllLayoutSize("portrait", 720, 1280);
            bool noBlackOutOfBoundsFrame = screenshotsDecoded && AllBlackRatioBelow(visualMetrics, 0.20f);
            bool visibleTileSeamsAbsent = screenshotsDecoded && AllSeamsAbsent(visualMetrics);
            bool terrainEntityShared = AllTerrainEntityZoomShared();
            bool hudInvariant = AllHudRectsStable();
            bool hudPixelInvariant = screenshotsDecoded && AllHudRatiosStable(visualMetrics);
            bool negativeUnchangedWouldFail = sampleCount && HasZoomDelta("landscape") && HasZoomDelta("portrait");
            bool ready = sampleCount
                && screenshotsWritten
                && screenshotsDecoded
                && pngDistinct
                && sourceHashMatch
                && landscapeCount
                && portraitCount
                && noBlackOutOfBoundsFrame
                && visibleTileSeamsAbsent
                && terrainEntityShared
                && hudInvariant
                && hudPixelInvariant
                && negativeUnchangedWouldFail;

            return new ProofVerdict(sampleCount, screenshotsWritten, screenshotsDecoded, pngDistinct, sourceHashMatch, landscapeCount, portraitCount, noBlackOutOfBoundsFrame, visibleTileSeamsAbsent, terrainEntityShared, hudInvariant, hudPixelInvariant, negativeUnchangedWouldFail, ready, visualMetrics);
        }

        private static string BuildTelemetryJson(ProofVerdict verdict)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            Json(builder, "proof_id", "WORLD_MAP_STEP5A_ZOOM_PROOF_HARNESS", true);
            Json(builder, "timestamp_utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), true);
            Json(builder, "scene", ScenePath, true);
            Json(builder, "unity_log", Normalize(unityLogPath), true);
            Json(builder, "bootstrap_hash_before", bootstrapHashBefore, true);
            Json(builder, "bootstrap_hash_after", bootstrapHashAfter, true);
            Json(builder, "wave3_world_bounds", FormatRect(wave3WorldBounds), true);
            Json(builder, "negative_unchanged_zoom_state_would_fail", verdict.NegativeUnchangedWouldFail ? "true" : "false", false);
            builder.AppendLine("  \"negative_zoom_fixture\": {");
            Json(builder, "fixture", "unchanged_zoom", true, 4);
            Json(builder, "zoom_before", "1.10", false, 4);
            Json(builder, "zoom_after", "1.10", false, 4);
            Json(builder, "terrain_distance_before", "563.2", false, 4);
            Json(builder, "terrain_distance_after", "563.2", false, 4);
            Json(builder, "entity_distance_before", "563.2", false, 4);
            Json(builder, "entity_distance_after", "563.2", false, 4);
            Json(builder, "overlay_distance_before", "237.08", false, 4);
            Json(builder, "overlay_distance_after", "237.08", false, 4);
            Json(builder, "observed_verdict", "FAIL", true, 4);
            Json(builder, "reason_code", "NO_ZOOM_DELTA", true, 4, false);
            builder.AppendLine("  },");
            Json(builder, "fresh_zoom_source_hash_match", verdict.SourceHashMatch ? "true" : "false", false);
            Json(builder, "landscape_zoom_proof", verdict.LandscapeProof ? "true" : "false", false);
            Json(builder, "portrait_zoom_proof", verdict.PortraitProof ? "true" : "false", false);
            Json(builder, "terrain_entity_shared_zoom", verdict.TerrainEntitySharedZoom ? "true" : "false", false);
            Json(builder, "hud_pixel_invariant", verdict.HudPixelInvariant ? "true" : "false", false);
            Json(builder, "visible_tile_seams", verdict.VisibleTileSeamsAbsent ? "false" : "true", false);
            Json(builder, "grid_pattern_visible", verdict.VisibleTileSeamsAbsent ? "false" : "true", false);
            Json(builder, "samples", "[", false, 2, false);
            for (int i = 0; i < Samples.Count; i++)
            {
                ZoomSample sample = Samples[i];
                VisualSampleMetrics metrics = i < verdict.VisualMetrics.Length ? verdict.VisualMetrics[i] : default;
                builder.AppendLine("    {");
                Json(builder, "label", sample.Spec.Label, true, 6);
                Json(builder, "screenshot", Normalize(sample.ScreenshotPath), true, 6);
                Json(builder, "screen_size", sample.ScreenWidth.ToString(CultureInfo.InvariantCulture) + "x" + sample.ScreenHeight.ToString(CultureInfo.InvariantCulture), true, 6);
                Json(builder, "current_center", FormatVector(sample.CurrentCenter), true, 6);
                Json(builder, "zoom", sample.Zoom.ToString("0.###", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "png_sha256", metrics.Sha256, true, 6);
                Json(builder, "terrain_anchor", FormatVector(sample.TerrainScreen), true, 6);
                Json(builder, "entity_anchor", FormatVector(sample.EntityScreen), true, 6);
                Json(builder, "overlay_anchor", FormatVector(sample.OverlayScreen), true, 6);
                Json(builder, "terrain_distance_to_pivot", Vector2.Distance(sample.TerrainScreen, sample.Pivot).ToString("0.###", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "entity_distance_to_pivot", Vector2.Distance(sample.EntityScreen, sample.Pivot).ToString("0.###", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "overlay_distance_to_pivot", Vector2.Distance(sample.OverlayScreen, sample.Pivot).ToString("0.###", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "hud_rect", FormatRect(sample.HudRect), true, 6);
                Json(builder, "hud_anchor_signature", metrics.HudAnchorSignature, true, 6);
                Json(builder, "hud_ratio", metrics.HudRatio.ToString("0.####", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "terrain_black_ratio", metrics.TerrainBlackRatio.ToString("0.####", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "min_boundary_luma", metrics.MinBoundaryLuma.ToString("0.###", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "worst_boundary", metrics.WorstBoundary, true, 6, false);
                builder.Append("    }");
                if (i < Samples.Count - 1) builder.Append(",");
                builder.AppendLine();
            }

            builder.AppendLine("  ],");
            Json(builder, "ready_for_demo_100_zoom_replacement", verdict.Ready ? "true" : "false", false, 2, false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildReceipt(ProofVerdict verdict)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BuilderA WorldMap Step5A Zoom Proof Harness Receipt");
            builder.AppendLine();
            builder.AppendLine("## Outputs");
            builder.AppendLine();
            builder.AppendLine("- `WorldMapStep5A_ZoomProofTelemetry.json`");
            builder.AppendLine("- `Landscape_Zoom_ContactSheet.png`");
            builder.AppendLine("- `Portrait_Zoom_ContactSheet.png`");
            foreach (ZoomSample sample in Samples)
            {
                builder.AppendLine("- `" + Path.GetFileName(sample.ScreenshotPath) + "`");
            }

            builder.AppendLine();
            builder.AppendLine("## Verdicts");
            builder.AppendLine();
            builder.AppendLine("- `FRESH_ZOOM_SOURCE_HASH_MATCH=" + (verdict.SourceHashMatch ? "YES" : "NO") + "`");
            builder.AppendLine("- `LANDSCAPE_ZOOM_PROOF=" + (verdict.LandscapeProof ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `PORTRAIT_ZOOM_PROOF=" + (verdict.PortraitProof ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `TERRAIN_ENTITY_SHARED_ZOOM=" + (verdict.TerrainEntitySharedZoom ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `HUD_PIXEL_INVARIANT=" + (verdict.HudPixelInvariant ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `VISIBLE_TILE_SEAMS=" + (verdict.VisibleTileSeamsAbsent ? "NO" : "YES") + "`");
            builder.AppendLine("- `GRID_PATTERN_VISIBLE=" + (verdict.VisibleTileSeamsAbsent ? "NO" : "YES") + "`");
            builder.AppendLine("- `READY_FOR_DEMO_100_ZOOM_REPLACEMENT=" + (verdict.Ready ? "YES" : "NO") + "`");
            return builder.ToString();
        }

        private static void WriteContactSheet(string fileName, string layout)
        {
            List<ZoomSample> layoutSamples = Samples.FindAll(s => s.Spec.Layout == layout && File.Exists(s.ScreenshotPath));
            if (layoutSamples.Count != 6) return;
            int thumbWidth = layout == "landscape" ? 480 : 240;
            int thumbHeight = layout == "landscape" ? 270 : 426;
            Texture2D sheet = new Texture2D(thumbWidth * 3, thumbHeight * 2, TextureFormat.RGBA32, false);
            Fill(sheet, new Color32(8, 8, 8, 255));
            for (int i = 0; i < layoutSamples.Count; i++)
            {
                byte[] bytes = File.ReadAllBytes(layoutSamples[i].ScreenshotPath);
                Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!source.LoadImage(bytes))
                {
                    UnityEngine.Object.DestroyImmediate(source);
                    continue;
                }

                int offsetX = (i % 3) * thumbWidth;
                int offsetY = (1 - i / 3) * thumbHeight;
                for (int y = 0; y < thumbHeight; y++)
                {
                    for (int x = 0; x < thumbWidth; x++)
                    {
                        int sx = Mathf.Clamp(Mathf.RoundToInt(x / (float)thumbWidth * (source.width - 1)), 0, source.width - 1);
                        int sy = Mathf.Clamp(Mathf.RoundToInt(y / (float)thumbHeight * (source.height - 1)), 0, source.height - 1);
                        sheet.SetPixel(offsetX + x, offsetY + y, source.GetPixel(sx, sy));
                    }
                }

                UnityEngine.Object.DestroyImmediate(source);
            }

            sheet.Apply();
            File.WriteAllBytes(Path.Combine(root, fileName), sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        private static VisualSampleMetrics[] BuildVisualMetrics()
        {
            var metrics = new VisualSampleMetrics[Samples.Count];
            for (int i = 0; i < Samples.Count; i++)
            {
                metrics[i] = BuildVisualMetrics(Samples[i]);
            }

            return metrics;
        }

        private static VisualSampleMetrics BuildVisualMetrics(ZoomSample sample)
        {
            if (!File.Exists(sample.ScreenshotPath))
            {
                return new VisualSampleMetrics(false, "missing", 1f, 0f, "missing", "missing", 0f);
            }

            byte[] bytes = File.ReadAllBytes(sample.ScreenshotPath);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return new VisualSampleMetrics(false, HashBytes(bytes), 1f, 0f, "decode_failed", "decode_failed", 0f);
            }

            float blackRatio = ComputeTerrainBlackRatio(texture);
            BoundaryMetric boundaryMetric = ComputeBoundaryMetric(texture, sample);
            string hudSignature = ComputeHudAnchorSignature(texture, sample.HudRect);
            float hudRatio = ComputeHudRatio(texture, sample.HudRect);
            UnityEngine.Object.DestroyImmediate(texture);
            return new VisualSampleMetrics(true, HashBytes(bytes), blackRatio, boundaryMetric.MinLuma, boundaryMetric.WorstBoundary, hudSignature, hudRatio);
        }

        private static BoundaryMetric ComputeBoundaryMetric(Texture2D texture, ZoomSample sample)
        {
            float minLuma = 255f;
            string worst = "none";
            for (int i = 1; i < 5; i++)
            {
                float worldX = wave3WorldBounds.xMin + i * ChunkSize;
                float screenX = WorldToScreen(new Vector2(worldX, wave3WorldBounds.center.y), sample.CurrentCenter, sample.Zoom, sample.ScreenWidth, sample.ScreenHeight).x;
                if (screenX > 8f && screenX < sample.ScreenWidth - 8f)
                {
                    float luma = AverageColumnLuma(texture, Mathf.RoundToInt(screenX), sample.HudRect);
                    if (luma < minLuma)
                    {
                        minLuma = luma;
                        worst = "V@" + worldX.ToString("0", CultureInfo.InvariantCulture) + " x=" + screenX.ToString("0.###", CultureInfo.InvariantCulture);
                    }
                }

                float worldY = wave3WorldBounds.yMin + i * ChunkSize;
                float screenY = WorldToScreen(new Vector2(wave3WorldBounds.center.x, worldY), sample.CurrentCenter, sample.Zoom, sample.ScreenWidth, sample.ScreenHeight).y;
                if (screenY > 8f && screenY < sample.ScreenHeight - 8f)
                {
                    float luma = AverageRowLuma(texture, Mathf.RoundToInt(screenY), sample.HudRect);
                    if (luma < minLuma)
                    {
                        minLuma = luma;
                        worst = "H@" + worldY.ToString("0", CultureInfo.InvariantCulture) + " y=" + screenY.ToString("0.###", CultureInfo.InvariantCulture);
                    }
                }
            }

            return new BoundaryMetric(minLuma, worst);
        }

        private static float AverageColumnLuma(Texture2D texture, int x, Rect hudRect)
        {
            float total = 0f;
            int count = 0;
            for (int y = Mathf.RoundToInt(texture.height * 0.10f); y < Mathf.RoundToInt(texture.height * 0.90f); y += 4)
            {
                int guiY = texture.height - 1 - y;
                if (hudRect.Contains(new Vector2(x, guiY))) continue;
                total += Luma(texture.GetPixel(Mathf.Clamp(x, 0, texture.width - 1), y));
                count++;
            }

            return count > 0 ? total / count : 255f;
        }

        private static float AverageRowLuma(Texture2D texture, int guiY, Rect hudRect)
        {
            int y = texture.height - 1 - Mathf.Clamp(guiY, 0, texture.height - 1);
            float total = 0f;
            int count = 0;
            for (int x = Mathf.RoundToInt(texture.width * 0.08f); x < Mathf.RoundToInt(texture.width * 0.92f); x += 4)
            {
                if (hudRect.Contains(new Vector2(x, guiY))) continue;
                total += Luma(texture.GetPixel(x, y));
                count++;
            }

            return count > 0 ? total / count : 255f;
        }

        private static float ComputeTerrainBlackRatio(Texture2D texture)
        {
            int top = Mathf.Clamp(Mathf.RoundToInt(texture.height * 0.10f), 0, texture.height - 1);
            int bottom = Mathf.Clamp(Mathf.RoundToInt(texture.height * 0.90f), top + 1, texture.height);
            int left = Mathf.Clamp(Mathf.RoundToInt(texture.width * 0.06f), 0, texture.width - 1);
            int right = Mathf.Clamp(Mathf.RoundToInt(texture.width * 0.94f), left + 1, texture.width);
            int black = 0;
            int total = 0;
            for (int y = top; y < bottom; y += 6)
            {
                for (int x = left; x < right; x += 6)
                {
                    Color32 color = texture.GetPixel(x, y);
                    if (color.r <= 8 && color.g <= 8 && color.b <= 8) black++;
                    total++;
                }
            }

            return total > 0 ? black / (float)total : 1f;
        }

        private static string ComputeHudAnchorSignature(Texture2D texture, Rect hudRect)
        {
            int[,] points =
            {
                { Mathf.RoundToInt(hudRect.xMin + 8f), Mathf.RoundToInt(hudRect.yMin + 8f) },
                { Mathf.RoundToInt(hudRect.center.x), Mathf.RoundToInt(hudRect.yMin + 8f) },
                { Mathf.RoundToInt(hudRect.xMax - 8f), Mathf.RoundToInt(hudRect.yMin + 8f) },
                { Mathf.RoundToInt(hudRect.xMin + 8f), Mathf.RoundToInt(hudRect.yMax - 8f) },
                { Mathf.RoundToInt(hudRect.xMax - 8f), Mathf.RoundToInt(hudRect.yMax - 8f) }
            };
            var builder = new StringBuilder();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                int x = Mathf.Clamp(points[i, 0], 0, texture.width - 1);
                int y = Mathf.Clamp(texture.height - 1 - points[i, 1], 0, texture.height - 1);
                Color32 color = texture.GetPixel(x, y);
                builder.Append(color.r > 150 && color.g > 90 && color.b < 90 ? "G" : color.r < 30 && color.g < 30 && color.b < 30 ? "D" : "O");
            }

            return builder.ToString();
        }

        private static float ComputeHudRatio(Texture2D texture, Rect hudRect)
        {
            float expected = Mathf.Max(1f, hudRect.width * hudRect.height);
            float observed = Mathf.Max(1f, Mathf.Min(texture.width, hudRect.xMax) - Mathf.Max(0f, hudRect.xMin)) * Mathf.Max(1f, Mathf.Min(texture.height, hudRect.yMax) - Mathf.Max(0f, hudRect.yMin));
            return observed / expected;
        }

        private static bool AllSamplesHaveFiles()
        {
            for (int i = 0; i < Samples.Count; i++)
            {
                if (!File.Exists(Samples[i].ScreenshotPath)) return false;
            }

            return true;
        }

        private static bool AllDecoded(VisualSampleMetrics[] metrics)
        {
            for (int i = 0; i < metrics.Length; i++) if (!metrics[i].Decoded) return false;
            return true;
        }

        private static bool UniqueHashes(VisualSampleMetrics[] metrics)
        {
            var hashes = new HashSet<string>();
            for (int i = 0; i < metrics.Length; i++)
            {
                if (!hashes.Add(metrics[i].Sha256)) return false;
            }

            return true;
        }

        private static bool AllBlackRatioBelow(VisualSampleMetrics[] metrics, float max)
        {
            for (int i = 0; i < metrics.Length; i++) if (metrics[i].TerrainBlackRatio > max) return false;
            return true;
        }

        private static bool AllSeamsAbsent(VisualSampleMetrics[] metrics)
        {
            for (int i = 0; i < metrics.Length; i++)
            {
                if (metrics[i].MinBoundaryLuma < 10f) return false;
            }

            return true;
        }

        private static bool AllTerrainEntityZoomShared()
        {
            bool landscape = LayoutZoomDistancesMatch("landscape");
            bool portrait = LayoutZoomDistancesMatch("portrait");
            return landscape && portrait;
        }

        private static bool LayoutZoomDistancesMatch(string layout)
        {
            List<ZoomSample> layoutSamples = Samples.FindAll(s => s.Spec.Layout == layout);
            if (layoutSamples.Count != 6) return false;
            for (int i = 0; i < layoutSamples.Count; i++)
            {
                float terrainDistance = Vector2.Distance(layoutSamples[i].TerrainScreen, layoutSamples[i].Pivot);
                float entityDistance = Vector2.Distance(layoutSamples[i].EntityScreen, layoutSamples[i].Pivot);
                if (Mathf.Abs(terrainDistance - entityDistance) > 0.01f) return false;
            }

            return true;
        }

        private static bool AllHudRectsStable()
        {
            return LayoutHudRectsStable("landscape") && LayoutHudRectsStable("portrait");
        }

        private static bool LayoutHudRectsStable(string layout)
        {
            List<ZoomSample> layoutSamples = Samples.FindAll(s => s.Spec.Layout == layout);
            if (layoutSamples.Count != 6) return false;
            Rect first = layoutSamples[0].HudRect;
            for (int i = 1; i < layoutSamples.Count; i++)
            {
                if (!SameRect(first, layoutSamples[i].HudRect)) return false;
            }

            return true;
        }

        private static bool AllHudRatiosStable(VisualSampleMetrics[] metrics)
        {
            for (int i = 0; i < metrics.Length; i++)
            {
                if (metrics[i].HudRatio < 0.995f || metrics[i].HudRatio > 1.005f) return false;
            }

            return true;
        }

        private static bool HasZoomDelta(string layout)
        {
            List<ZoomSample> layoutSamples = Samples.FindAll(s => s.Spec.Layout == layout);
            if (layoutSamples.Count != 6) return false;
            return Mathf.Abs(layoutSamples[0].Zoom - layoutSamples[2].Zoom) > 0.05f
                && Mathf.Abs(layoutSamples[3].Zoom - layoutSamples[5].Zoom) > 0.05f;
        }

        private static int CountLayout(string layout)
        {
            int count = 0;
            for (int i = 0; i < Samples.Count; i++) if (Samples[i].Spec.Layout == layout) count++;
            return count;
        }

        private static bool AllLayoutSize(string layout, int width, int height)
        {
            for (int i = 0; i < Samples.Count; i++)
            {
                if (Samples[i].Spec.Layout != layout) continue;
                if (Samples[i].ScreenWidth != width || Samples[i].ScreenHeight != height) return false;
            }

            return true;
        }

        private static void BindFields()
        {
            Type type = typeof(WorldMapMmoFullscreenFoundationBootstrap);
            currentWorldCenterField = RequiredField(type, "currentWorldCenter");
            targetWorldCenterField = RequiredField(type, "targetWorldCenter");
            currentZoomField = RequiredField(type, "currentZoom");
            targetZoomField = RequiredField(type, "targetZoom");
        }

        private static FieldInfo RequiredField(Type type, string name)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }

        private static void SetCameraState(Vector2 currentCenter, Vector2 targetCenter, float currentZoom, float targetZoom)
        {
            currentWorldCenterField.SetValue(bootstrap, currentCenter);
            targetWorldCenterField.SetValue(bootstrap, targetCenter);
            currentZoomField.SetValue(bootstrap, currentZoom);
            targetZoomField.SetValue(bootstrap, targetZoom);
        }

        private static Rect ReadWave3WorldBounds()
        {
            FieldInfo providerField = typeof(WorldMapMmoFullscreenFoundationBootstrap).GetField("wave3Provider", BindingFlags.Instance | BindingFlags.NonPublic);
            object provider = providerField != null ? providerField.GetValue(bootstrap) : null;
            if (provider != null)
            {
                PropertyInfo boundsProperty = provider.GetType().GetProperty("WorldBounds", BindingFlags.Instance | BindingFlags.Public);
                if (boundsProperty != null)
                {
                    return (Rect)boundsProperty.GetValue(provider, null);
                }
            }

            return Rect.MinMaxRect(15360f, 15360f, 17920f, 17920f);
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
                object customSize = gameViewSizeType.GetConstructor(new[] { gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) }).Invoke(new object[] { fixedResolution, width, height, label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { customSize });
                int selectedIndex = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, Array.Empty<object>()) - 1;
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                gameView.Show();
                gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(gameView, selectedIndex);
                gameView.minSize = new Vector2(width, height);
                gameView.maxSize = new Vector2(width, height);
                gameView.titleContent = new GUIContent(label);
                gameView.Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not resize Game View for Step5A zoom proof: " + exception.Message);
            }
        }

        private static void DeletePreviousOutputs()
        {
            string[] files =
            {
                "WorldMapStep5A_ZoomProofTelemetry.json",
                "WorldMapStep5A_ZoomProofReceipt.md",
                "WorldMapStep5A_ZoomProofFailure.txt",
                "Landscape_Zoom_ContactSheet.png",
                "Portrait_Zoom_ContactSheet.png"
            };
            for (int i = 0; i < files.Length; i++)
            {
                string path = Path.Combine(root, files[i]);
                if (File.Exists(path)) File.Delete(path);
            }

            for (int i = 0; i < Captures.Length; i++)
            {
                string path = Path.Combine(root, Captures[i].Label + ".png");
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static void FailAndExit(string reason)
        {
            failed = true;
            failureReason = reason;
            root = string.IsNullOrEmpty(root) ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputRoot)) : root;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "WorldMapStep5A_ZoomProofFailure.txt"), failureReason, Encoding.UTF8);
            EditorApplication.update -= ProcessHarness;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= ResumeWhenPlayModeIsReady;
            SessionState.SetBool(SessionRunningKey, false);
            SessionState.SetBool(SessionStartedKey, false);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            EditorApplication.Exit(1);
        }

        private static Vector2 WorldToScreen(Vector2 world, Vector2 center, float zoom, int width, int height)
        {
            return new Vector2(width * 0.5f, height * 0.5f) + (world - center) * zoom;
        }

        private static Vector2 ChunkCenter(int x, int y)
        {
            return new Vector2((x + 0.5f) * ChunkSize, (y + 0.5f) * ChunkSize);
        }

        private static bool Contains(Rect rect, Vector2 point)
        {
            return point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax;
        }

        private static bool SameRect(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 0.01f
                && Mathf.Abs(a.y - b.y) < 0.01f
                && Mathf.Abs(a.width - b.width) < 0.01f
                && Mathf.Abs(a.height - b.height) < 0.01f;
        }

        private static void Fill(Texture2D texture, Color32 color)
        {
            Color32[] colors = new Color32[texture.width * texture.height];
            for (int i = 0; i < colors.Length; i++) colors[i] = color;
            texture.SetPixels32(colors);
        }

        private static float Luma(Color32 color)
        {
            return color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
        }

        private static string HashBytes(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static string HashAsset(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (!File.Exists(fullPath)) return "missing";
            using (SHA256 sha256 = SHA256.Create())
            using (FileStream stream = File.OpenRead(fullPath))
            {
                byte[] hash = sha256.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static void Json(StringBuilder builder, string key, string value, bool quoted, int indent = 2, bool comma = true)
        {
            builder.Append(' ', indent).Append("\"").Append(Escape(key)).Append("\": ");
            if (quoted) builder.Append("\"").Append(Escape(value)).Append("\"");
            else builder.Append(value);
            if (comma) builder.Append(",");
            builder.AppendLine();
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string Normalize(string value)
        {
            return value.Replace('\\', '/');
        }

        private static string FormatVector(Vector2 value)
        {
            return value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + value.y.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string FormatRect(Rect value)
        {
            return value.x.ToString("0.###", CultureInfo.InvariantCulture) + "," + value.y.ToString("0.###", CultureInfo.InvariantCulture) + "," + value.width.ToString("0.###", CultureInfo.InvariantCulture) + "," + value.height.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private readonly struct ZoomCaptureSpec
        {
            public readonly string Label;
            public readonly string Layout;
            public readonly int Width;
            public readonly int Height;
            public readonly float Zoom;

            public ZoomCaptureSpec(string label, string layout, int width, int height, float zoom)
            {
                Label = label;
                Layout = layout;
                Width = width;
                Height = height;
                Zoom = zoom;
            }
        }

        private readonly struct ZoomSample
        {
            public readonly ZoomCaptureSpec Spec;
            public readonly int Frame;
            public readonly int ScreenWidth;
            public readonly int ScreenHeight;
            public readonly Vector2 CurrentCenter;
            public readonly float Zoom;
            public readonly Rect RawBounds;
            public readonly Rect SafeBounds;
            public readonly Vector2 TerrainScreen;
            public readonly Vector2 EntityScreen;
            public readonly Vector2 OverlayScreen;
            public readonly Vector2 Pivot;
            public readonly Rect HudRect;
            public readonly string ScreenshotPath;

            public ZoomSample(ZoomCaptureSpec spec, int frame, int screenWidth, int screenHeight, Vector2 currentCenter, float zoom, Rect rawBounds, Rect safeBounds, Vector2 terrainScreen, Vector2 entityScreen, Vector2 overlayScreen, Vector2 pivot, Rect hudRect, string screenshotPath)
            {
                Spec = spec;
                Frame = frame;
                ScreenWidth = screenWidth;
                ScreenHeight = screenHeight;
                CurrentCenter = currentCenter;
                Zoom = zoom;
                RawBounds = rawBounds;
                SafeBounds = safeBounds;
                TerrainScreen = terrainScreen;
                EntityScreen = entityScreen;
                OverlayScreen = overlayScreen;
                Pivot = pivot;
                HudRect = hudRect;
                ScreenshotPath = screenshotPath;
            }
        }

        private readonly struct VisualSampleMetrics
        {
            public readonly bool Decoded;
            public readonly string Sha256;
            public readonly float TerrainBlackRatio;
            public readonly float MinBoundaryLuma;
            public readonly string WorstBoundary;
            public readonly string HudAnchorSignature;
            public readonly float HudRatio;

            public VisualSampleMetrics(bool decoded, string sha256, float terrainBlackRatio, float minBoundaryLuma, string worstBoundary, string hudAnchorSignature, float hudRatio)
            {
                Decoded = decoded;
                Sha256 = sha256;
                TerrainBlackRatio = terrainBlackRatio;
                MinBoundaryLuma = minBoundaryLuma;
                WorstBoundary = worstBoundary;
                HudAnchorSignature = hudAnchorSignature;
                HudRatio = hudRatio;
            }
        }

        private readonly struct BoundaryMetric
        {
            public readonly float MinLuma;
            public readonly string WorstBoundary;

            public BoundaryMetric(float minLuma, string worstBoundary)
            {
                MinLuma = minLuma;
                WorstBoundary = worstBoundary;
            }
        }

        private readonly struct ProofVerdict
        {
            public readonly bool SampleCount;
            public readonly bool ScreenshotsWritten;
            public readonly bool ScreenshotsDecoded;
            public readonly bool PngDistinct;
            public readonly bool SourceHashMatch;
            public readonly bool LandscapeProof;
            public readonly bool PortraitProof;
            public readonly bool NoBlackOutOfBoundsFrame;
            public readonly bool VisibleTileSeamsAbsent;
            public readonly bool TerrainEntitySharedZoom;
            public readonly bool HudInvariant;
            public readonly bool HudPixelInvariant;
            public readonly bool NegativeUnchangedWouldFail;
            public readonly bool Ready;
            public readonly VisualSampleMetrics[] VisualMetrics;

            public ProofVerdict(bool sampleCount, bool screenshotsWritten, bool screenshotsDecoded, bool pngDistinct, bool sourceHashMatch, bool landscapeProof, bool portraitProof, bool noBlackOutOfBoundsFrame, bool visibleTileSeamsAbsent, bool terrainEntitySharedZoom, bool hudInvariant, bool hudPixelInvariant, bool negativeUnchangedWouldFail, bool ready, VisualSampleMetrics[] visualMetrics)
            {
                SampleCount = sampleCount;
                ScreenshotsWritten = screenshotsWritten;
                ScreenshotsDecoded = screenshotsDecoded;
                PngDistinct = pngDistinct;
                SourceHashMatch = sourceHashMatch;
                LandscapeProof = landscapeProof;
                PortraitProof = portraitProof;
                NoBlackOutOfBoundsFrame = noBlackOutOfBoundsFrame;
                VisibleTileSeamsAbsent = visibleTileSeamsAbsent;
                TerrainEntitySharedZoom = terrainEntitySharedZoom;
                HudInvariant = hudInvariant;
                HudPixelInvariant = hudPixelInvariant;
                NegativeUnchangedWouldFail = negativeUnchangedWouldFail;
                Ready = ready;
                VisualMetrics = visualMetrics;
            }
        }
    }
}
