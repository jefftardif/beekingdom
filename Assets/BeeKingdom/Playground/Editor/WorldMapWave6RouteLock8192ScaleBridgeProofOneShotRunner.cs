using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class WorldMapWave6RouteLock8192ScaleBridgeProofOneShotRunner
    {
        private const string RequestFilePath = "Temp/RunWave6RouteLock8192ScaleBridgeProof.request";
        private const string ScenePath = "Assets/Scenes/WorldMapWave6RouteLock8192ScaleBridgeProofPreview.unity";
        private const string OutputRoot = "Docs/BuilderA/WorldMapWave6_50x50_RouteLock8192ScaleBridgeProofPreview/UnityLocalPlayProof";
        private const string RunningKey = "BeeKingdom.WorldMapWave6.RouteLock8192ScaleBridgeProof.LocalProof.Running";
        private const string PhaseKey = "BeeKingdom.WorldMapWave6.RouteLock8192ScaleBridgeProof.LocalProof.Phase";
        private const string CaptureFrameKey = "BeeKingdom.WorldMapWave6.RouteLock8192ScaleBridgeProof.LocalProof.CaptureFrame";
        private static readonly List<string> Evidence = new List<string>();

        private static readonly CaptureSpec[] Captures =
        {
            new CaptureSpec("ROUTE_LOCK_8192_CENTER_C24_R24_Z058", 24, 24, 0.58f),
            new CaptureSpec("ROUTE_LOCK_8192_NORTH_WEST_C16_R16_Z058", 16, 16, 0.58f),
            new CaptureSpec("ROUTE_LOCK_8192_SOUTH_EAST_C40_R40_Z058", 40, 40, 0.58f),
            new CaptureSpec("ROUTE_LOCK_8192_EAST_EDGE_C48_R52_Z058", 52, 48, 0.58f)
        };

        static WorldMapWave6RouteLock8192ScaleBridgeProofOneShotRunner()
        {
            EditorApplication.update -= WatchRequestAndRun;
            EditorApplication.update += WatchRequestAndRun;
        }

        [MenuItem("Bee Kingdom/World Map/Run Wave6 Route-Lock 8192 Local Play Proof")]
        public static void RunFromMenu()
        {
            StartProof();
        }

        private static void WatchRequestAndRun()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                RunWhenReady();
                return;
            }

            if (!File.Exists(RequestFilePath))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            try
            {
                File.Delete(RequestFilePath);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"Could not clear Wave6 route-lock 8192 proof request: {exception.Message}");
                return;
            }

            StartProof();
        }

        private static void StartProof()
        {
            Directory.CreateDirectory(AbsoluteProjectPath(OutputRoot));
            Evidence.Clear();
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetInt(CaptureFrameKey, 0);
            SessionState.SetBool(RunningKey, true);
            foreach (CaptureSpec capture in Captures) DeleteIfExists(CapturePath(capture));
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.update -= RunWhenReady;
            EditorApplication.update += RunWhenReady;
            EditorApplication.EnterPlaymode();
        }

        private static void RunWhenReady()
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
            if (bootstrap == null)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }

            try
            {
                if (Evidence.Count == 0)
                {
                    Evidence.Add("entered_play_mode:true");
                    Evidence.Add("preview_scene=" + ScenePath);
                    Evidence.Add("uses_route_lock_8192_scale_bridge_proof_runtime_package:" + bootstrap.UsesRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode().ToString().ToLowerInvariant());
                    Require(bootstrap.UsesRouteLock8192ScaleBridgeProofRuntimePackageForPlayMode(), "Route-lock 8192 proof scene did not enable the expected runtime package flag.");
                }

                int phase = SessionState.GetInt(PhaseKey, 0);
                if (phase >= Captures.Length)
                {
                    Evidence.Add("VISUAL_FINAL_50X50=NO");
                    Evidence.Add("ROUTE_LOCK_8192_SCALE_BRIDGE_PROOF_UNITY_AUDIT_ONLY=YES");
                    Evidence.Add("READY_FOR_FULL_50X50_TILE_BUILD=NO");
                    Evidence.Add("READY_FOR_QA_BUILDERC=NO");
                    Evidence.Add("READY_FOR_UNITY_HANDOFF=NO");
                    Evidence.Add("MASTER_25600_AUTHORIZED=NO");
                    WriteReceipt("PASS", null);
                    Finish();
                    return;
                }

                CaptureSpec spec = Captures[phase];
                int captureFrame = SessionState.GetInt(CaptureFrameKey, 0);
                if (captureFrame == 0)
                {
                    ValidateView(bootstrap, spec);
                    SessionState.SetInt(CaptureFrameKey, Time.frameCount + 3);
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                if (Time.frameCount < captureFrame)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                string screenshotPath = CapturePath(spec);
                if (!File.Exists(screenshotPath))
                {
                    ScreenCapture.CaptureScreenshot(screenshotPath);
                    Evidence.Add(spec.Label.ToLowerInvariant() + "_screenshot:" + screenshotPath);
                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                SessionState.SetInt(PhaseKey, phase + 1);
                SessionState.SetInt(CaptureFrameKey, 0);
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception exception)
            {
                WriteReceipt("FAIL", exception);
                Finish();
            }
        }

        private static void ValidateView(WorldMapMmoFullscreenFoundationBootstrap bootstrap, CaptureSpec spec)
        {
            Vector2 center = WorldMapWave6StreamingTileProvider.TileAnchorWorld(spec.Row, spec.Column, 256f, 256f);
            bootstrap.ApplyWave6ProofView(center, spec.Zoom);
            WorldMapMmoFullscreenFoundationBootstrap.Wave6ProofSnapshot state = bootstrap.CurrentWave6ProofSnapshot();
            Require(state.ManifestReady, spec.Label + " manifest not ready.");
            Require(state.VisibleTilesReady, spec.Label + " visible tiles not ready.");
            Require(state.LoadedVisibleTiles == state.RequiredVisibleTiles, spec.Label + " visible tile count mismatch.");
            Require(state.CachedTiles <= WorldMapWave6StreamingTileProvider.CacheCapacity, spec.Label + " cache exceeded.");
            Require(string.Equals(bootstrap.CurrentWave6MasterSha256ForProof(), WorldMapWave6StreamingTileProvider.RouteLock8192ScaleBridgeProofExpectedMasterSha256, StringComparison.OrdinalIgnoreCase), "Route-lock 8192 proof scene loaded the wrong SHA-256.");
            Evidence.Add(spec.Label.ToLowerInvariant() + "_center_tile:R" + spec.Row.ToString("00", CultureInfo.InvariantCulture) + "C" + spec.Column.ToString("00", CultureInfo.InvariantCulture));
            Evidence.Add(spec.Label.ToLowerInvariant() + "_zoom:" + spec.Zoom.ToString("0.00", CultureInfo.InvariantCulture));
            Evidence.Add(spec.Label.ToLowerInvariant() + "_visible_tiles:" + state.LoadedVisibleTiles.ToString(CultureInfo.InvariantCulture) + "/" + state.RequiredVisibleTiles.ToString(CultureInfo.InvariantCulture));
            Evidence.Add(spec.Label.ToLowerInvariant() + "_cache_tiles:" + state.CachedTiles.ToString(CultureInfo.InvariantCulture));
            Evidence.Add(spec.Label.ToLowerInvariant() + "_loaded_master_sha256:" + bootstrap.CurrentWave6MasterSha256ForProof());
        }

        private static void WriteReceipt(string status, Exception exception)
        {
            string receipt = AbsoluteProjectPath(OutputRoot + "/WorldMapWave6_RouteLock8192ScaleBridgeProof_LocalPlayModeProofReceipt.md");
            var builder = new StringBuilder();
            builder.AppendLine("# WorldMap Wave6 Route-Lock 8192 Scale-Bridge Proof - Local Play Mode Proof");
            builder.AppendLine();
            builder.AppendLine("STATUS=" + status);
            builder.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("resource_root=" + WorldMapWave6StreamingTileProvider.RouteLock8192ScaleBridgeProofResourceRoot);
            builder.AppendLine("source_master_sha256=" + WorldMapWave6StreamingTileProvider.RouteLock8192ScaleBridgeProofExpectedMasterSha256);
            foreach (string item in Evidence) builder.AppendLine(item);
            if (exception != null)
            {
                builder.AppendLine("exception=" + exception.GetType().FullName);
                builder.AppendLine("message=" + exception.Message);
            }

            File.WriteAllText(receipt, builder.ToString(), new UTF8Encoding(false));
        }

        private static void Finish()
        {
            SessionState.SetBool(RunningKey, false);
            SessionState.SetInt(PhaseKey, 0);
            SessionState.SetInt(CaptureFrameKey, 0);
            EditorApplication.update -= RunWhenReady;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        private static string CapturePath(CaptureSpec capture)
        {
            return AbsoluteProjectPath(OutputRoot + "/" + capture.Label + ".png");
        }

        private static string AbsoluteProjectPath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private readonly struct CaptureSpec
        {
            public readonly string Label;
            public readonly int Row;
            public readonly int Column;
            public readonly float Zoom;

            public CaptureSpec(string label, int row, int column, float zoom)
            {
                Label = label;
                Row = row;
                Column = column;
                Zoom = zoom;
            }
        }
    }
}
