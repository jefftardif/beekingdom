using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave6V3DPreviewSceneBuilder
    {
        public const string CanonicalScenePath = "Assets/Scenes/WorldMapMmoFullscreenFoundation.unity";
        public const string PreviewScenePath = "Assets/Scenes/WorldMapWave6V3DPreview.unity";
        private const string ReceiptPath = "Docs/BuilderA/WorldMapWave6_50x50_V3DPreview/WorldMapWave6_V3DPreviewSceneBuildReceipt.md";

        [MenuItem("Bee Kingdom/World Map/Build Wave6 V3D Preview Scene")]
        public static void BuildPreviewScene()
        {
            Directory.CreateDirectory(AbsoluteProjectPath(Path.GetDirectoryName(ReceiptPath)));
            try
            {
                EditorSceneManager.OpenScene(CanonicalScenePath);
                WorldMapMmoFullscreenFoundationBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>();
                Require(bootstrap != null, "WorldMap bootstrap is missing from canonical scene.");
                bootstrap.SetV3DPreviewRuntimePackageForPlayMode(true);
                EditorUtility.SetDirty(bootstrap);
                EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
                Require(EditorSceneManager.SaveScene(bootstrap.gameObject.scene, PreviewScenePath), "Preview scene could not be saved.");

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
            builder.AppendLine("# WorldMap Wave6 V3D Preview Scene Build");
            builder.AppendLine();
            builder.AppendLine("STATUS=" + status);
            builder.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("canonical_scene=" + CanonicalScenePath);
            builder.AppendLine("preview_scene=" + PreviewScenePath);
            builder.AppendLine("resource_root=" + WorldMapWave6StreamingTileProvider.V3DPreviewResourceRoot);
            builder.AppendLine("source_master_sha256=" + WorldMapWave6StreamingTileProvider.V3DPreviewExpectedMasterSha256);
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
