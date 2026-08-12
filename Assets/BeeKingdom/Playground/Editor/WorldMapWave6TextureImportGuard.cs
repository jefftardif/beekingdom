using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldMapWave6TextureImportGuard : AssetPostprocessor
    {
        private const string RuntimeRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/";
        private const string V2IRepairAuditRoot = RuntimeRoot + "UIB_ImmenseContinuousMaster50x50_v2i_repair_audit_preview/";
        private const string V2ISelectedHdLocalRepairReviewRoot = RuntimeRoot + "UIB_ImmenseContinuousMaster50x50_v2i_selected_hd_local_repair_review/";
        private const string Wave5Method12288ExactCropRoot = RuntimeRoot + "UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview/";

        [MenuItem("Bee Kingdom/World Map/Reimport Wave6 V2I Repair Textures Uncompressed")]
        public static void ReimportV2IRepairAuditTextures()
        {
            AssetDatabase.ImportAsset(V2IRepairAuditRoot, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(V2ISelectedHdLocalRepairReviewRoot, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("Wave6 V2I repair audit textures reimported as uncompressed textures.");
        }

        [MenuItem("Bee Kingdom/World Map/Reimport Wave6 50x50 Exact Crop Textures")]
        public static void ReimportWave5Method12288ExactCropTextures()
        {
            AssetDatabase.ImportAsset(Wave5Method12288ExactCropRoot, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("Wave6 50x50 exact-crop textures reimported with terrain texture settings.");
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(V2IRepairAuditRoot, System.StringComparison.Ordinal)
                && !assetPath.StartsWith(V2ISelectedHdLocalRepairReviewRoot, System.StringComparison.Ordinal)
                && !assetPath.StartsWith(Wave5Method12288ExactCropRoot, System.StringComparison.Ordinal)) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.None;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }
    }
}
