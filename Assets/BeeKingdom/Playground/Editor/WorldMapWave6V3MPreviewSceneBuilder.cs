using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave6V3MPreviewSceneBuilder
    {
        public const string CanonicalScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        public const string PreviewScenePath = "Assets/Scenes/WorldMapWave6V3MPreview.unity";
        private const string ReceiptPath = "Docs/BuilderA/WorldMapWave6_50x50_V3MPreview/WorldMapWave6_V3MPreviewSceneBuildReceipt.md";

        [MenuItem("Bee Kingdom/World Map/Build Wave6 V3M Preview Scene")]
        public static void BuildPreviewScene()
        {
            Directory.CreateDirectory(AbsoluteProjectPath(Path.GetDirectoryName(ReceiptPath)));
            try
            {
                EditorSceneManager.OpenScene(CanonicalScenePath);
                WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
                Require(bootstrap != null, "WorldMap bootstrap is missing from canonical scene.");
                bootstrap.SetV3MPreviewRuntimePackageForPlayMode(true);
                EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
                Require(EditorSceneManager.SaveScene(bootstrap.gameObject.scene, PreviewScenePath), "V3M preview scene could not be saved.");
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
            builder.AppendLine("# WorldMap Wave6 V3M Preview Scene Build");
            builder.AppendLine();
            builder.AppendLine("STATUS=" + status);
            builder.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("canonical_scene=" + CanonicalScenePath);
            builder.AppendLine("preview_scene=" + PreviewScenePath);
            builder.AppendLine("resource_root=" + WorldMapWave6StreamingTileProvider.V3MPreviewResourceRoot);
            builder.AppendLine("source_master_sha256=" + WorldMapWave6StreamingTileProvider.V3MPreviewExpectedMasterSha256);
            builder.AppendLine("VISUAL_FINAL_HD=NO");
            builder.AppendLine("READY_FOR_CANONICAL_SWAP=NO");
            builder.AppendLine("READY_FOR_UNITY_HANDOFF=NO");
            builder.AppendLine("MASTER_25600_AUTHORIZED=NO");
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
