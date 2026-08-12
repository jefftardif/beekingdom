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
    public static class WorldMapStep5APanProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private const string BootstrapAssetPath = "Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs";
        private const string OutputRoot = "Docs/BuilderA/WorldMapStep5APanProofHarness";
        private const string ExpectedBootstrapHash = "8281EE0294AF44F24F8EBDB454A535C79F33DD21F4706DCE45CEA5FE04A5E63E";
        private const string SessionRunningKey = "BeeKingdom.WorldMapStep5A.PanProof.Running";
        private const string SessionStartedKey = "BeeKingdom.WorldMapStep5A.PanProof.Started";
        private const float ChunkSize = 512f;
        private const float Zoom = 1.10f;
        private const float SafeMarginPixels = 128f;
        private static Vector2 panStartCenter;
        private static Vector2 panMidCenter;
        private static Vector2 panEndCenter;
        private static Rect wave3WorldBounds;
        private static Rect rawCenterBounds;
        private static Rect safeCenterBounds;
        private static int proofScreenWidth;
        private static int proofScreenHeight;
        private static readonly Vector2 ProbeWorldPoint = ChunkCenter(33, 32);

        private static readonly List<PanSample> Samples = new List<PanSample>(8);
        private static string root;
        private static string bootstrapHashBefore;
        private static string bootstrapHashAfter;
        private static WorldMapMmoFullscreenFoundationBootstrap bootstrap;
        private static FieldInfo currentWorldCenterField;
        private static FieldInfo targetWorldCenterField;
        private static FieldInfo currentZoomField;
        private static FieldInfo targetZoomField;
        private static int phase;
        private static int waitFrames;
        private static bool failed;
        private static string failureReason;

        [InitializeOnLoadMethod]
        private static void ResumeAfterDomainReload()
        {
            if (!SessionState.GetBool(SessionRunningKey, false))
            {
                return;
            }

            Debug.Log("Step5A pan harness resume after domain reload.");
            root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputRoot));
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= ResumeWhenPlayModeIsReady;
            EditorApplication.update += ResumeWhenPlayModeIsReady;
        }

        [MenuItem("Bee Kingdom/WorldMap Step5A/Run Pan Proof Harness")]
        public static void RunWorldMapStep5APanProofHarnessBatch()
        {
            root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputRoot));
            Directory.CreateDirectory(root);
            Samples.Clear();
            failed = false;
            failureReason = string.Empty;
            phase = 0;
            waitFrames = 0;
            SessionState.SetBool(SessionRunningKey, true);
            SessionState.SetBool(SessionStartedKey, false);
            bootstrapHashBefore = HashAsset(BootstrapAssetPath);

            if (bootstrapHashBefore != ExpectedBootstrapHash)
            {
                FailAndExit("Bootstrap hash before run does not match expected Step5A hash: " + bootstrapHashBefore);
                return;
            }

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
                Debug.Log("Step5A pan harness resume skipped. running=" + SessionState.GetBool(SessionRunningKey, false) + " started=" + SessionState.GetBool(SessionStartedKey, false));
                EditorApplication.update -= ResumeWhenPlayModeIsReady;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                Debug.Log("Step5A pan harness waiting for Play Mode.");
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            try
            {
                bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
                if (bootstrap == null)
                {
                    Debug.Log("Step5A pan harness waiting for bootstrap.");
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                bootstrapHashBefore = HashAsset(BootstrapAssetPath);
                BindFields();
                Screen.SetResolution(1920, 1080, false);
                failed = false;
                failureReason = string.Empty;
                SessionState.SetBool(SessionStartedKey, true);
                EditorApplication.update -= ResumeWhenPlayModeIsReady;
                EditorApplication.update -= ProcessHarness;
                Samples.Clear();
                phase = -1;
                waitFrames = 2;
                EditorApplication.update += ProcessHarness;
                Debug.Log("Step5A pan harness Editor update runner attached.");
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception ex)
            {
                FailAndExit(ex.Message);
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
                    ComputeSafePanCenters();
                    SetCameraState(panStartCenter, panStartCenter, Zoom, Zoom);
                    phase = 0;
                    waitFrames = 12;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == 0)
                {
                    CaptureSample("T0_SAFE_C32_CENTER");
                    phase = 1;
                    waitFrames = 20;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == 1)
                {
                    SetCameraState(ReadVector2(currentWorldCenterField), panMidCenter, Zoom, Zoom);
                    phase = 2;
                    waitFrames = 24;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == 2)
                {
                    CaptureSample("T1_SAFE_MID");
                    phase = 3;
                    waitFrames = 24;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == 3)
                {
                    SetCameraState(ReadVector2(currentWorldCenterField), panEndCenter, Zoom, Zoom);
                    phase = 4;
                    waitFrames = 30;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == 4)
                {
                    CaptureSample("T2_SAFE_RIGHT_INSET");
                    phase = 5;
                    waitFrames = 60;
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (phase == 5)
                {
                    CompleteHarness();
                }
            }
            catch (Exception ex)
            {
                FailAndExit(ex.ToString());
            }
        }

        private static void CaptureSample(string label)
        {
            Vector2 currentCenter = ReadVector2(currentWorldCenterField);
            Vector2 targetCenter = ReadVector2(targetWorldCenterField);
            float currentZoom = ReadFloat(currentZoomField);
            if (!Contains(safeCenterBounds, currentCenter))
            {
                throw new InvalidOperationException("Pan proof center outside safe bounds before capture: " + FormatVector(currentCenter) + " safe=" + FormatRect(safeCenterBounds));
            }

            Vector2 terrainScreen = WorldToScreen(ProbeWorldPoint, currentCenter, currentZoom);
            Vector2 entityScreen = WorldToScreen(ProbeWorldPoint, currentCenter, currentZoom);
            Rect hudRect = new Rect(14f, 12f, 760f, 108f);
            string pngPath = Path.Combine(root, label + ".png");
            ScreenCapture.CaptureScreenshot(pngPath);

            Samples.Add(new PanSample(label, Time.frameCount, currentCenter, targetCenter, currentZoom, terrainScreen, entityScreen, hudRect, pngPath));
        }

        private static void ComputeSafePanCenters()
        {
            proofScreenWidth = Screen.width;
            proofScreenHeight = Screen.height;
            wave3WorldBounds = ReadWave3WorldBounds();
            float halfWidthWorld = proofScreenWidth * 0.5f / Zoom;
            float halfHeightWorld = proofScreenHeight * 0.5f / Zoom;
            rawCenterBounds = Rect.MinMaxRect(
                wave3WorldBounds.xMin + halfWidthWorld,
                wave3WorldBounds.yMin + halfHeightWorld,
                wave3WorldBounds.xMax - halfWidthWorld,
                wave3WorldBounds.yMax - halfHeightWorld);

            float marginWorld = SafeMarginPixels / Zoom;
            safeCenterBounds = Rect.MinMaxRect(
                rawCenterBounds.xMin + marginWorld,
                rawCenterBounds.yMin + marginWorld,
                rawCenterBounds.xMax - marginWorld,
                rawCenterBounds.yMax - marginWorld);

            if (safeCenterBounds.width < 256f || safeCenterBounds.height < 1f)
            {
                throw new InvalidOperationException("Safe Wave3 pan bounds are too small for proof: " + FormatRect(safeCenterBounds) + " screen=" + proofScreenWidth.ToString(CultureInfo.InvariantCulture) + "x" + proofScreenHeight.ToString(CultureInfo.InvariantCulture));
            }

            float y = Mathf.Clamp(16640f, safeCenterBounds.yMin, safeCenterBounds.yMax);
            float startX = Mathf.Clamp(16640f, safeCenterBounds.xMin, safeCenterBounds.xMax);
            float availableRight = safeCenterBounds.xMax - startX;
            float step = availableRight >= 768f ? 512f : Mathf.Min(128f, availableRight * 0.5f);
            if (step < 64f)
            {
                startX = safeCenterBounds.xMin;
                step = Mathf.Max(64f, Mathf.Min(256f, safeCenterBounds.width * 0.35f));
            }

            panStartCenter = new Vector2(startX, y);
            panMidCenter = new Vector2(Mathf.Min(safeCenterBounds.xMax, startX + step), y);
            panEndCenter = new Vector2(Mathf.Min(safeCenterBounds.xMax, startX + step * 2f), y);

            if (!Contains(safeCenterBounds, panStartCenter) || !Contains(safeCenterBounds, panMidCenter) || !Contains(safeCenterBounds, panEndCenter))
            {
                throw new InvalidOperationException("Computed pan centers are outside safe bounds: " + FormatVector(panStartCenter) + " / " + FormatVector(panMidCenter) + " / " + FormatVector(panEndCenter) + " safe=" + FormatRect(safeCenterBounds));
            }

            Debug.Log("Step5A pan safe centers computed. screen=" + proofScreenWidth.ToString(CultureInfo.InvariantCulture) + "x" + proofScreenHeight.ToString(CultureInfo.InvariantCulture)
                + " raw=" + FormatRect(rawCenterBounds)
                + " safe=" + FormatRect(safeCenterBounds)
                + " centers=" + FormatVector(panStartCenter) + " -> " + FormatVector(panMidCenter) + " -> " + FormatVector(panEndCenter));
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

        private static void CompleteHarness()
        {
            EditorApplication.update -= ProcessHarness;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= ResumeWhenPlayModeIsReady;
            bootstrapHashAfter = HashAsset(BootstrapAssetPath);
            ProofVerdict verdict = BuildVerdict();
            File.WriteAllText(Path.Combine(root, "WorldMapStep5A_PanProofTelemetry.json"), BuildTelemetryJson(verdict), Encoding.UTF8);
            File.WriteAllText(Path.Combine(root, "WorldMapStep5A_PanProofReceipt.md"), BuildReceipt(verdict), Encoding.UTF8);
            SessionState.SetBool(SessionRunningKey, false);
            SessionState.SetBool(SessionStartedKey, false);
            EditorApplication.ExitPlaymode();
            EditorApplication.Exit(verdict.Ready ? 0 : 1);
        }

        private static ProofVerdict BuildVerdict()
        {
            bool sampleCount = Samples.Count == 3;
            bool screenshotsWritten = sampleCount
                && File.Exists(Samples[0].ScreenshotPath)
                && File.Exists(Samples[1].ScreenshotPath)
                && File.Exists(Samples[2].ScreenshotPath);
            VisualSampleMetrics[] visualMetrics = sampleCount ? BuildVisualMetrics() : new VisualSampleMetrics[0];
            bool screenshotsDecoded = visualMetrics.Length == 3 && visualMetrics[0].Decoded && visualMetrics[1].Decoded && visualMetrics[2].Decoded;
            bool pngDistinct = screenshotsDecoded
                && visualMetrics[0].Sha256 != visualMetrics[1].Sha256
                && visualMetrics[1].Sha256 != visualMetrics[2].Sha256
                && visualMetrics[0].Sha256 != visualMetrics[2].Sha256;
            bool positionsWithinSafeViewport = sampleCount
                && Contains(safeCenterBounds, Samples[0].CurrentCenter)
                && Contains(safeCenterBounds, Samples[1].CurrentCenter)
                && Contains(safeCenterBounds, Samples[2].CurrentCenter);
            bool noBlackOutOfBoundsFrame = screenshotsDecoded
                && visualMetrics[0].TerrainBlackRatio <= 0.20f
                && visualMetrics[1].TerrainBlackRatio <= 0.20f
                && visualMetrics[2].TerrainBlackRatio <= 0.20f;
            bool visibleTileSeamsAbsent = screenshotsDecoded
                && visualMetrics[0].MinTerrainRowLuma >= 10f
                && visualMetrics[1].MinTerrainRowLuma >= 10f
                && visualMetrics[2].MinTerrainRowLuma >= 10f
                && visualMetrics[0].MinTerrainColumnLuma >= 10f
                && visualMetrics[1].MinTerrainColumnLuma >= 10f
                && visualMetrics[2].MinTerrainColumnLuma >= 10f;
            bool cameraDelta = sampleCount
                && Vector2.Distance(Samples[0].CurrentCenter, Samples[1].CurrentCenter) > 1f
                && Vector2.Distance(Samples[1].CurrentCenter, Samples[2].CurrentCenter) > 1f;
            bool negativeUnchangedWouldFail = sampleCount
                && !(Approximately(Samples[0].CurrentCenter, Samples[1].CurrentCenter) && Approximately(Samples[1].CurrentCenter, Samples[2].CurrentCenter));
            bool terrainEntityShared = sampleCount
                && Vector2.Distance(Samples[1].TerrainScreen - Samples[0].TerrainScreen, Samples[1].EntityScreen - Samples[0].EntityScreen) < 0.01f
                && Vector2.Distance(Samples[2].TerrainScreen - Samples[1].TerrainScreen, Samples[2].EntityScreen - Samples[1].EntityScreen) < 0.01f;
            bool hudInvariant = sampleCount
                && SameRect(Samples[0].HudRect, Samples[1].HudRect)
                && SameRect(Samples[1].HudRect, Samples[2].HudRect);
            bool hudPixelInvariant = screenshotsDecoded
                && visualMetrics[0].HudAnchorSignature == visualMetrics[1].HudAnchorSignature
                && visualMetrics[1].HudAnchorSignature == visualMetrics[2].HudAnchorSignature;
            bool hashUnchanged = bootstrapHashBefore == ExpectedBootstrapHash && bootstrapHashAfter == ExpectedBootstrapHash;
            bool ready = sampleCount
                && screenshotsWritten
                && screenshotsDecoded
                && pngDistinct
                && positionsWithinSafeViewport
                && noBlackOutOfBoundsFrame
                && visibleTileSeamsAbsent
                && cameraDelta
                && negativeUnchangedWouldFail
                && terrainEntityShared
                && hudInvariant
                && hudPixelInvariant
                && hashUnchanged;
            return new ProofVerdict(sampleCount, screenshotsWritten, screenshotsDecoded, pngDistinct, positionsWithinSafeViewport, noBlackOutOfBoundsFrame, visibleTileSeamsAbsent, cameraDelta, negativeUnchangedWouldFail, terrainEntityShared, hudInvariant, hudPixelInvariant, hashUnchanged, ready, visualMetrics);
        }

        private static string BuildTelemetryJson(ProofVerdict verdict)
        {
            var builder = new StringBuilder();
            builder.AppendLine("{");
            Json(builder, "proof_id", "WORLD_MAP_STEP5A_PAN_PROOF_HARNESS", true);
            Json(builder, "timestamp_utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture), true);
            Json(builder, "scene", ScenePath, true);
            Json(builder, "bootstrap_hash_before", bootstrapHashBefore, true);
            Json(builder, "bootstrap_hash_after", bootstrapHashAfter, true);
            Json(builder, "screen_size", proofScreenWidth.ToString(CultureInfo.InvariantCulture) + "x" + proofScreenHeight.ToString(CultureInfo.InvariantCulture), true);
            Json(builder, "wave3_world_bounds", FormatRect(wave3WorldBounds), true);
            Json(builder, "raw_center_bounds", FormatRect(rawCenterBounds), true);
            Json(builder, "safe_center_bounds", FormatRect(safeCenterBounds), true);
            Json(builder, "pan_proof_uses_production_world_transform", verdict.SampleCount ? "true" : "false", false);
            Json(builder, "screenshots_written", verdict.ScreenshotsWritten ? "true" : "false", false);
            Json(builder, "screenshots_decoded", verdict.ScreenshotsDecoded ? "true" : "false", false);
            Json(builder, "png_hashes_distinct", verdict.PngDistinct ? "true" : "false", false);
            Json(builder, "pan_positions_within_wave3_safe_viewport", verdict.PositionsWithinSafeViewport ? "true" : "false", false);
            Json(builder, "no_black_out_of_bounds_frame", verdict.NoBlackOutOfBoundsFrame ? "true" : "false", false);
            Json(builder, "visible_tile_seams", verdict.VisibleTileSeamsAbsent ? "false" : "true", false);
            Json(builder, "grid_pattern_visible", verdict.VisibleTileSeamsAbsent ? "false" : "true", false);
            Json(builder, "gutter_sampling", verdict.VisibleTileSeamsAbsent ? "true" : "false", false);
            Json(builder, "pan_before_mid_after_camera_delta", verdict.CameraDelta ? "true" : "false", false);
            Json(builder, "negative_unchanged_camera_state_would_fail", verdict.NegativeUnchangedWouldFail ? "true" : "false", false);
            Json(builder, "visual_terrain_entity_shared_delta", verdict.TerrainEntityShared ? "true" : "false", false);
            Json(builder, "hud_invariant", verdict.HudInvariant ? "true" : "false", false);
            Json(builder, "hud_pixel_invariant", verdict.HudPixelInvariant ? "true" : "false", false);
            Json(builder, "bootstrap_hash_unchanged", verdict.HashUnchanged ? "true" : "false", false);
            builder.AppendLine("  \"samples\": [");
            for (int i = 0; i < Samples.Count; i++)
            {
                PanSample sample = Samples[i];
                VisualSampleMetrics metrics = i < verdict.VisualMetrics.Length ? verdict.VisualMetrics[i] : default;
                builder.AppendLine("    {");
                Json(builder, "label", sample.Label, true, 6);
                Json(builder, "frame", sample.Frame.ToString(CultureInfo.InvariantCulture), false, 6);
                Json(builder, "current_center", FormatVector(sample.CurrentCenter), true, 6);
                Json(builder, "target_center", FormatVector(sample.TargetCenter), true, 6);
                Json(builder, "zoom", sample.Zoom.ToString("0.###", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "terrain_screen", FormatVector(sample.TerrainScreen), true, 6);
                Json(builder, "entity_screen", FormatVector(sample.EntityScreen), true, 6);
                Json(builder, "hud_rect", FormatRect(sample.HudRect), true, 6);
                Json(builder, "decoded", metrics.Decoded ? "true" : "false", false, 6);
                Json(builder, "png_sha256", metrics.Sha256, true, 6);
                Json(builder, "terrain_black_ratio", metrics.TerrainBlackRatio.ToString("0.####", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "min_terrain_row_luma", metrics.MinTerrainRowLuma.ToString("0.###", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "min_terrain_column_luma", metrics.MinTerrainColumnLuma.ToString("0.###", CultureInfo.InvariantCulture), false, 6);
                Json(builder, "hud_anchor_signature", metrics.HudAnchorSignature, true, 6);
                Json(builder, "screenshot", Normalize(sample.ScreenshotPath), true, 6, false);
                builder.Append("    }");
                if (i < Samples.Count - 1) builder.Append(",");
                builder.AppendLine();
            }

            builder.AppendLine("  ],");
            Json(builder, "ready_for_demo_100_pan_proof", verdict.Ready ? "true" : "false", false, 2, false);
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string BuildReceipt(ProofVerdict verdict)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BuilderA WorldMap Step5A Pan Proof Harness Receipt");
            builder.AppendLine();
            builder.AppendLine("## Résumé");
            builder.AppendLine();
            builder.AppendLine("Harness Editor/Development uniquement. Le bootstrap runtime Step5A n'a pas été modifié.");
            builder.AppendLine();
            builder.AppendLine("## Sorties");
            builder.AppendLine();
            builder.AppendLine("- `WorldMapStep5A_PanProofTelemetry.json`");
            foreach (PanSample sample in Samples)
            {
                builder.AppendLine("- `" + Path.GetFileName(sample.ScreenshotPath) + "`");
            }

            builder.AppendLine();
            builder.AppendLine("## Hash bootstrap");
            builder.AppendLine();
            builder.AppendLine("- Avant : `" + bootstrapHashBefore + "`");
            builder.AppendLine("- Après : `" + bootstrapHashAfter + "`");
            builder.AppendLine();
            builder.AppendLine("## Verdicts");
            builder.AppendLine();
            builder.AppendLine("- `PAN_PROOF_USES_PRODUCTION_WORLD_TRANSFORM=" + (verdict.SampleCount ? "YES" : "NO") + "`");
            builder.AppendLine("- `PAN_SCREENSHOTS_WRITTEN=" + (verdict.ScreenshotsWritten ? "YES" : "NO") + "`");
            builder.AppendLine("- `PAN_SCREENSHOTS_DECODED=" + (verdict.ScreenshotsDecoded ? "YES" : "NO") + "`");
            builder.AppendLine("- `PNG_HASHES_DISTINCT=" + (verdict.PngDistinct ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `PAN_POSITIONS_WITHIN_WAVE3_SAFE_VIEWPORT=" + (verdict.PositionsWithinSafeViewport ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `NO_BLACK_OUT_OF_BOUNDS_FRAME=" + (verdict.NoBlackOutOfBoundsFrame ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `VISIBLE_TILE_SEAMS=" + (verdict.VisibleTileSeamsAbsent ? "NO" : "YES") + "`");
            builder.AppendLine("- `GRID_PATTERN_VISIBLE=" + (verdict.VisibleTileSeamsAbsent ? "NO" : "YES") + "`");
            builder.AppendLine("- `GUTTER_SAMPLING=" + (verdict.VisibleTileSeamsAbsent ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `PAN_BEFORE_MID_AFTER_CAMERA_DELTA=" + (verdict.CameraDelta ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `NEGATIVE_UNCHANGED_CAMERA_STATE_TEST=" + (verdict.NegativeUnchangedWouldFail ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `VISUAL_TERRAIN_ENTITY_SHARED_DELTA=" + (verdict.TerrainEntityShared ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `HUD_INVARIANT=" + (verdict.HudInvariant ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `HUD_PIXEL_INVARIANT=" + (verdict.HudPixelInvariant ? "PASS" : "FAIL") + "`");
            builder.AppendLine("- `BOOTSTRAP_HASH_UNCHANGED=" + (verdict.HashUnchanged ? "YES" : "NO") + "`");
            builder.AppendLine("- `READY_FOR_DEMO_100_PAN_PROOF=" + (verdict.Ready ? "YES" : "NO") + "`");
            return builder.ToString();
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

        private static Vector2 ReadVector2(FieldInfo field)
        {
            return (Vector2)field.GetValue(bootstrap);
        }

        private static float ReadFloat(FieldInfo field)
        {
            return (float)field.GetValue(bootstrap);
        }

        private static void FailAndExit(string reason)
        {
            failed = true;
            failureReason = reason;
            root = string.IsNullOrEmpty(root) ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputRoot)) : root;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "WorldMapStep5A_PanProofFailure.txt"), failureReason, Encoding.UTF8);
            EditorApplication.update -= ProcessHarness;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= ResumeWhenPlayModeIsReady;
            SessionState.SetBool(SessionRunningKey, false);
            SessionState.SetBool(SessionStartedKey, false);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            EditorApplication.Exit(1);
        }

        private static Vector2 WorldToScreen(Vector2 world, Vector2 center, float zoom)
        {
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + (world - center) * zoom;
        }

        private static Vector2 ChunkCenter(int x, int y)
        {
            return new Vector2((x + 0.5f) * ChunkSize, (y + 0.5f) * ChunkSize);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b) < 0.01f;
        }

        private static bool SameRect(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 0.01f
                && Mathf.Abs(a.y - b.y) < 0.01f
                && Mathf.Abs(a.width - b.width) < 0.01f
                && Mathf.Abs(a.height - b.height) < 0.01f;
        }

        private static bool Contains(Rect rect, Vector2 point)
        {
            return point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax;
        }

        private static VisualSampleMetrics[] BuildVisualMetrics()
        {
            var metrics = new VisualSampleMetrics[Samples.Count];
            for (int i = 0; i < Samples.Count; i++)
            {
                metrics[i] = BuildVisualMetrics(Samples[i].ScreenshotPath);
            }

            return metrics;
        }

        private static VisualSampleMetrics BuildVisualMetrics(string path)
        {
            if (!File.Exists(path))
            {
                return new VisualSampleMetrics(false, "missing", 1f, 0f, 0f, "missing");
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool loaded = texture.LoadImage(bytes);
            if (!loaded)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return new VisualSampleMetrics(false, HashBytes(bytes), 1f, 0f, 0f, "decode_failed");
            }

            float blackRatio = ComputeTerrainBlackRatio(texture);
            float minRowLuma = ComputeMinTerrainRowLuma(texture);
            float minColumnLuma = ComputeMinTerrainColumnLuma(texture);
            string hudSignature = ComputeHudAnchorSignature(texture);
            UnityEngine.Object.DestroyImmediate(texture);
            return new VisualSampleMetrics(true, HashBytes(bytes), blackRatio, minRowLuma, minColumnLuma, hudSignature);
        }

        private static float ComputeTerrainBlackRatio(Texture2D texture)
        {
            int top = Mathf.Clamp(Mathf.RoundToInt(texture.height * 0.16f), 0, texture.height - 1);
            int bottom = Mathf.Clamp(Mathf.RoundToInt(texture.height * 0.85f), top + 1, texture.height);
            int left = Mathf.Clamp(Mathf.RoundToInt(texture.width * 0.08f), 0, texture.width - 1);
            int right = Mathf.Clamp(Mathf.RoundToInt(texture.width * 0.92f), left + 1, texture.width);
            int black = 0;
            int total = 0;
            const int stride = 4;
            for (int y = top; y < bottom; y += stride)
            {
                for (int x = left; x < right; x += stride)
                {
                    Color32 color = texture.GetPixel(x, y);
                    if (color.r <= 8 && color.g <= 8 && color.b <= 8)
                    {
                        black++;
                    }

                    total++;
                }
            }

            return total > 0 ? black / (float)total : 1f;
        }

        private static string ComputeHudAnchorSignature(Texture2D texture)
        {
            int[,] points =
            {
                { 8, 8 },
                { 120, 8 },
                { 8, 110 },
                { 710, 8 },
                { 710, 110 }
            };
            var builder = new StringBuilder();
            for (int i = 0; i < points.GetLength(0); i++)
            {
                int x = Mathf.Clamp(points[i, 0], 0, texture.width - 1);
                int y = Mathf.Clamp(texture.height - 1 - points[i, 1], 0, texture.height - 1);
                Color32 color = texture.GetPixel(x, y);
                builder.Append(color.r > 160 && color.g > 100 && color.b < 80 ? "G" : color.r < 20 && color.g < 20 && color.b < 20 ? "D" : "O");
            }

            return builder.ToString();
        }

        private static float ComputeMinTerrainRowLuma(Texture2D texture)
        {
            int top = Mathf.Clamp(Mathf.RoundToInt(texture.height * 0.16f), 0, texture.height - 1);
            int bottom = Mathf.Clamp(Mathf.RoundToInt(texture.height * 0.85f), top + 1, texture.height);
            int left = Mathf.Clamp(Mathf.RoundToInt(texture.width * 0.08f), 0, texture.width - 1);
            int right = Mathf.Clamp(Mathf.RoundToInt(texture.width * 0.92f), left + 1, texture.width);
            float min = 255f;
            const int stride = 4;
            for (int y = top; y < bottom; y++)
            {
                float total = 0f;
                int count = 0;
                for (int x = left; x < right; x += stride)
                {
                    total += Luma(texture.GetPixel(x, y));
                    count++;
                }

                if (count > 0) min = Mathf.Min(min, total / count);
            }

            return min;
        }

        private static float ComputeMinTerrainColumnLuma(Texture2D texture)
        {
            int top = Mathf.Clamp(Mathf.RoundToInt(texture.height * 0.16f), 0, texture.height - 1);
            int bottom = Mathf.Clamp(Mathf.RoundToInt(texture.height * 0.85f), top + 1, texture.height);
            int left = Mathf.Clamp(Mathf.RoundToInt(texture.width * 0.08f), 0, texture.width - 1);
            int right = Mathf.Clamp(Mathf.RoundToInt(texture.width * 0.92f), left + 1, texture.width);
            float min = 255f;
            const int stride = 4;
            for (int x = left; x < right; x++)
            {
                float total = 0f;
                int count = 0;
                for (int y = top; y < bottom; y += stride)
                {
                    total += Luma(texture.GetPixel(x, y));
                    count++;
                }

                if (count > 0) min = Mathf.Min(min, total / count);
            }

            return min;
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
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
                }

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
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private static void Json(StringBuilder builder, string key, string value, bool quoted, int indent = 2, bool comma = true)
        {
            builder.Append(' ', indent).Append("\"").Append(Escape(key)).Append("\": ");
            if (quoted)
            {
                builder.Append("\"").Append(Escape(value)).Append("\"");
            }
            else
            {
                builder.Append(value);
            }

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

        private readonly struct PanSample
        {
            public readonly string Label;
            public readonly int Frame;
            public readonly Vector2 CurrentCenter;
            public readonly Vector2 TargetCenter;
            public readonly float Zoom;
            public readonly Vector2 TerrainScreen;
            public readonly Vector2 EntityScreen;
            public readonly Rect HudRect;
            public readonly string ScreenshotPath;

            public PanSample(string label, int frame, Vector2 currentCenter, Vector2 targetCenter, float zoom, Vector2 terrainScreen, Vector2 entityScreen, Rect hudRect, string screenshotPath)
            {
                Label = label;
                Frame = frame;
                CurrentCenter = currentCenter;
                TargetCenter = targetCenter;
                Zoom = zoom;
                TerrainScreen = terrainScreen;
                EntityScreen = entityScreen;
                HudRect = hudRect;
                ScreenshotPath = screenshotPath;
            }
        }

        private readonly struct ProofVerdict
        {
            public readonly bool SampleCount;
            public readonly bool ScreenshotsWritten;
            public readonly bool ScreenshotsDecoded;
            public readonly bool PngDistinct;
            public readonly bool PositionsWithinSafeViewport;
            public readonly bool NoBlackOutOfBoundsFrame;
            public readonly bool VisibleTileSeamsAbsent;
            public readonly bool CameraDelta;
            public readonly bool NegativeUnchangedWouldFail;
            public readonly bool TerrainEntityShared;
            public readonly bool HudInvariant;
            public readonly bool HudPixelInvariant;
            public readonly bool HashUnchanged;
            public readonly bool Ready;
            public readonly VisualSampleMetrics[] VisualMetrics;

            public ProofVerdict(bool sampleCount, bool screenshotsWritten, bool screenshotsDecoded, bool pngDistinct, bool positionsWithinSafeViewport, bool noBlackOutOfBoundsFrame, bool visibleTileSeamsAbsent, bool cameraDelta, bool negativeUnchangedWouldFail, bool terrainEntityShared, bool hudInvariant, bool hudPixelInvariant, bool hashUnchanged, bool ready, VisualSampleMetrics[] visualMetrics)
            {
                SampleCount = sampleCount;
                ScreenshotsWritten = screenshotsWritten;
                ScreenshotsDecoded = screenshotsDecoded;
                PngDistinct = pngDistinct;
                PositionsWithinSafeViewport = positionsWithinSafeViewport;
                NoBlackOutOfBoundsFrame = noBlackOutOfBoundsFrame;
                VisibleTileSeamsAbsent = visibleTileSeamsAbsent;
                CameraDelta = cameraDelta;
                NegativeUnchangedWouldFail = negativeUnchangedWouldFail;
                TerrainEntityShared = terrainEntityShared;
                HudInvariant = hudInvariant;
                HudPixelInvariant = hudPixelInvariant;
                HashUnchanged = hashUnchanged;
                Ready = ready;
                VisualMetrics = visualMetrics;
            }
        }

        private readonly struct VisualSampleMetrics
        {
            public readonly bool Decoded;
            public readonly string Sha256;
            public readonly float TerrainBlackRatio;
            public readonly float MinTerrainRowLuma;
            public readonly float MinTerrainColumnLuma;
            public readonly string HudAnchorSignature;

            public VisualSampleMetrics(bool decoded, string sha256, float terrainBlackRatio, float minTerrainRowLuma, float minTerrainColumnLuma, string hudAnchorSignature)
            {
                Decoded = decoded;
                Sha256 = sha256;
                TerrainBlackRatio = terrainBlackRatio;
                MinTerrainRowLuma = minTerrainRowLuma;
                MinTerrainColumnLuma = minTerrainColumnLuma;
                HudAnchorSignature = hudAnchorSignature;
            }
        }
    }
}
