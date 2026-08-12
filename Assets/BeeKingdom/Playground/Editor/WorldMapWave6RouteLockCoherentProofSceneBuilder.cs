using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave6RouteLockCoherentProofSceneBuilder
    {
        public const string CanonicalScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        public const string PreviewScenePath = "Assets/Scenes/WorldMapWave6RouteLockCoherentProofPreview.unity";
        private const string ReceiptPath = "Docs/BuilderA/WorldMapWave6_50x50_RouteLockCoherentProofPreview/WorldMapWave6_RouteLockCoherentProofPreviewSceneBuildReceipt.md";

        [MenuItem("Bee Kingdom/World Map/Build Wave6 Route-Lock Coherent Proof Preview Scene")]
        public static void BuildPreviewScene()
        {
            Directory.CreateDirectory(AbsoluteProjectPath(Path.GetDirectoryName(ReceiptPath)));
            try
            {
                EditorSceneManager.OpenScene(CanonicalScenePath);
                WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
                Require(bootstrap != null, "WorldMap bootstrap is missing from canonical scene.");
                bootstrap.SetRouteLockCoherentProofRuntimePackageForPlayMode(true);
                bootstrap.SetInitialAuditViewForPlayMode(true, 24, 24, 0.58f, "Route-lock proof - jonctions 50x50");
                EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
                Require(EditorSceneManager.SaveScene(bootstrap.gameObject.scene, PreviewScenePath), "Route-lock coherent proof preview scene could not be saved.");
                WriteReceipt("PASS", null);
            }
            catch (Exception exception)
            {
                WriteReceipt("FAIL", exception);
                throw;
            }
        }

        private static void WriteReceipt(string status, Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# WorldMap Wave6 Route-Lock Coherent Proof Preview Scene Build");
            builder.AppendLine();
            builder.AppendLine("STATUS=" + status);
            builder.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("canonical_scene=" + CanonicalScenePath);
            builder.AppendLine("preview_scene=" + PreviewScenePath);
            builder.AppendLine("resource_root=" + WorldMapWave6StreamingTileProvider.RouteLockCoherentProofResourceRoot);
            builder.AppendLine("source_master_sha256=" + WorldMapWave6StreamingTileProvider.RouteLockCoherentProofExpectedMasterSha256);
            builder.AppendLine("initial_audit_view=C24_24");
            builder.AppendLine("initial_audit_zoom=0.58");
            builder.AppendLine("UNITY_AUDIT_PREVIEW=YES");
            builder.AppendLine("ROUTE_LOCK_COHERENT_PROOF=YES");
            builder.AppendLine("FULL_50X50_TILE_PACKAGE=YES");
            builder.AppendLine("AUDIT_ONLY_FROM_4096_PROOF_SOURCE=YES");
            builder.AppendLine("READY_FOR_FULL_50X50_TILE_BUILD=NO");
            builder.AppendLine("READY_FOR_QA_BUILDERC=NO");
            builder.AppendLine("READY_FOR_CANONICAL_SWAP=NO");
            builder.AppendLine("READY_FOR_UNITY_HANDOFF=NO");
            builder.AppendLine("MASTER_25600_AUTHORIZED=NO");
            builder.AppendLine("PREMIUM_50X50_VALIDATED=NO");
            if (exception != null)
            {
                builder.AppendLine("exception=" + exception.GetType().FullName);
                builder.AppendLine("message=" + exception.Message);
            }

            File.WriteAllText(AbsoluteProjectPath(ReceiptPath), builder.ToString(), new UTF8Encoding(false));
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
