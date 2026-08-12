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
    public static class WorldMapWave6V3DPreviewPlayModeProofHarness
    {
        private const string ScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        private const string OutputRoot = "Docs/BuilderA/WorldMapWave6_50x50_V3DPreview/PlayerProof";
        private const string SessionRunningKey = "BeeKingdom.WorldMapWave6.V3DPreview.PlayerProof.Running";
        private const string ExitPendingKey = "BeeKingdom.WorldMapWave6.V3DPreview.PlayerProof.ExitPending";
        private const string ExitCodeKey = "BeeKingdom.WorldMapWave6.V3DPreview.PlayerProof.ExitCode";
        private static readonly List<string> Evidence = new List<string>();

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
            EditorApplication.update -= RunWhenReady;
            EditorApplication.update += RunWhenReady;
        }

        [MenuItem("Bee Kingdom/World Map/Run Wave6 V3D Preview Play Proof")]
        public static void RunWave6V3DPreviewPlayProof()
        {
            Directory.CreateDirectory(AbsoluteProjectPath(OutputRoot));
            Evidence.Clear();
            SessionState.SetBool(SessionRunningKey, true);
            SessionState.SetBool(ExitPendingKey, false);
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.update -= RunWhenReady;
            EditorApplication.update += RunWhenReady;
            EditorApplication.EnterPlaymode();
        }

        private static void RunWhenReady()
        {
            if (!SessionState.GetBool(SessionRunningKey, false)) return;
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
                Evidence.Clear();
                Evidence.Add("entered_play_mode:true");
                bool packageReady = bootstrap.ApplyWave6RuntimePackageForProof(
                    WorldMapWave6StreamingTileProvider.V3DPreviewResourceRoot,
                    WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256,
                    "V3D preview");
                Require(packageReady, "V3D preview package did not report all visible tiles ready at initial view.");
                Evidence.Add("v3d_preview_package_applied_in_play_mode:true");

                WorldMapMmoFullscreenFoundationBootstrap.Wave6ProofSnapshot initial = bootstrap.CurrentWave6ProofSnapshot();
                Rect bounds = initial.WorldBounds;
                ValidateView(bootstrap, "CENTER_Z100", bounds.center, 1.00f);
                ValidateView(bootstrap, "CENTER_Z135", bounds.center, 1.35f);
                ValidateView(bootstrap, "NORTH_WEST", SafeCorner(bounds, false, false), 1.00f);
                ValidateView(bootstrap, "NORTH_EAST", SafeCorner(bounds, true, false), 1.00f);
                ValidateView(bootstrap, "SOUTH_WEST", SafeCorner(bounds, false, true), 1.00f);
                ValidateView(bootstrap, "SOUTH_EAST", SafeCorner(bounds, true, true), 1.00f);

                Evidence.Add("READY_FOR_CANONICAL_SWAP=NO");
                Evidence.Add("READY_FOR_UNITY_HANDOFF=NO");
                Evidence.Add("MASTER_25600_AUTHORIZED=NO");
                WriteReceipt("PASS", null);
                Exit(0);
            }
            catch (Exception exception)
            {
                WriteReceipt("FAIL", exception);
                Exit(1);
            }
        }

        private static void ValidateView(WorldMapMmoFullscreenFoundationBootstrap bootstrap, string label, Vector2 center, float zoom)
        {
            bootstrap.ApplyWave6ProofView(center, zoom);
            WorldMapMmoFullscreenFoundationBootstrap.Wave6ProofSnapshot state = bootstrap.CurrentWave6ProofSnapshot();
            Require(state.ManifestReady, label + " manifest not ready.");
            Require(state.VisibleTilesReady, label + " visible tiles not ready.");
            Require(state.LoadedVisibleTiles == state.RequiredVisibleTiles, label + " visible tile count mismatch.");
            Require(state.CachedTiles <= WorldMapWave6StreamingTileProvider.CacheCapacity, label + " cache exceeded.");
            Evidence.Add(label.ToLowerInvariant() + "_visible_tiles:" + state.LoadedVisibleTiles.ToString(CultureInfo.InvariantCulture) + "/" + state.RequiredVisibleTiles.ToString(CultureInfo.InvariantCulture));
            Evidence.Add(label.ToLowerInvariant() + "_cache_tiles:" + state.CachedTiles.ToString(CultureInfo.InvariantCulture));
        }

        private static Vector2 SafeCorner(Rect bounds, bool right, bool bottom)
        {
            const float width = 1280f;
            const float height = 720f;
            const float margin = 128f;
            return new Vector2(
                right ? bounds.xMax - width * 0.5f - margin : bounds.xMin + width * 0.5f + margin,
                bottom ? bounds.yMax - height * 0.5f - margin : bounds.yMin + height * 0.5f + margin);
        }

        private static void WriteReceipt(string status, Exception exception)
        {
            string receipt = AbsoluteProjectPath(OutputRoot + "/WorldMapWave6_V3DPreview_PlayModeProofReceipt.md");
            var builder = new StringBuilder();
            builder.AppendLine("# WorldMap Wave6 V3D Preview Play Mode Proof");
            builder.AppendLine();
            builder.AppendLine("STATUS=" + status);
            builder.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("resource_root=" + WorldMapWave6StreamingTileProvider.V3DPreviewResourceRoot);
            builder.AppendLine("source_master_sha256=" + WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256);
            for (int i = 0; i < Evidence.Count; i++) builder.AppendLine(Evidence[i]);
            if (exception != null)
            {
                builder.AppendLine("exception=" + exception.GetType().FullName);
                builder.AppendLine("message=" + exception.Message);
            }

            File.WriteAllText(receipt, builder.ToString(), new UTF8Encoding(false));
        }

        private static void Exit(int code)
        {
            SessionState.SetBool(SessionRunningKey, false);
            SessionState.SetInt(ExitCodeKey, code);
            SessionState.SetBool(ExitPendingKey, true);
            EditorApplication.update -= RunWhenReady;
            EditorApplication.ExitPlaymode();
        }

        private static string AbsoluteProjectPath(string projectRelativePath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
