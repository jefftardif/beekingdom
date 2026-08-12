using System;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldMapWave6AssetImporter : AssetPostprocessor
    {
        private const string TerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v1/";
        private const string V3DPreviewTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3d_preview/";
        private const string V3ECandidateTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3e_candidate/";
        private const string V3MPreviewTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3m_preview/";
        private const string V3VCandidateTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3v_candidate/";
        private const string V3OReducedAuditPreviewTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3o_reduced_audit_preview/";
        private const string SupportCenterNativeAuditPreviewTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_support_center_native_audit_preview/";
        private const string V2INativeAuditPreviewTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2i_native_audit_preview/";
        private const string V2IRepairAuditPreviewTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2i_repair_audit_preview/";
        private const string V2ISelectedHdLocalRepairReviewTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2i_selected_hd_local_repair_review/";
        private const string V2OPerimeterAuditPreviewTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v2o_perimeter_audit_preview/";
        private const string RouteLockCoherentProofTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_route_lock_coherent_proof/";
        private const string RouteLock8192ScaleBridgeProofTerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_route_lock_8192_scale_bridge_proof/";

        private void OnPreprocessTexture()
        {
            if (!IsWave6TerrainTexture(assetPath))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.None;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 1;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(AndroidSettings());
        }

        private static bool IsWave6TerrainTexture(string path)
        {
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return path.StartsWith(TerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(V3DPreviewTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(V3ECandidateTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(V3MPreviewTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(V3VCandidateTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(V3OReducedAuditPreviewTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(SupportCenterNativeAuditPreviewTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(V2INativeAuditPreviewTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(V2IRepairAuditPreviewTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(V2ISelectedHdLocalRepairReviewTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(V2OPerimeterAuditPreviewTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(RouteLockCoherentProofTerrainRoot, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(RouteLock8192ScaleBridgeProofTerrainRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static TextureImporterPlatformSettings AndroidSettings()
        {
            return new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 1024,
                resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                format = TextureImporterFormat.ASTC_6x6,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
                crunchedCompression = false,
                allowsAlphaSplitting = false
            };
        }
    }
}
