using System;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    public sealed class WorldMapWave5AssetImporter : AssetPostprocessor
    {
        private const string TerrainRoot = "Assets/BeeKingdom/Playground/Resources/WorldMapWave5Runtime/UIB_ImmenseContinuousMaster25x25_v1/";
        private const string BearDenPath = "Assets/BeeKingdom/Playground/Resources/WorldMapWave5Runtime/Landmarks/BearDen/bear_den_dormant_v1.png";

        private void OnPreprocessTexture()
        {
            if (!assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            if (assetPath.StartsWith(TerrainRoot, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureTerrain(importer);
            }
            else if (string.Equals(assetPath, BearDenPath, StringComparison.OrdinalIgnoreCase))
            {
                ConfigureBearDen(importer);
            }
        }

        private static void ConfigureTerrain(TextureImporter importer)
        {
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
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(AndroidSettings(false));
        }

        private static void ConfigureBearDen(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.None;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 1;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(AndroidSettings(true));
        }

        private static TextureImporterPlatformSettings AndroidSettings(bool hasAlpha)
        {
            return new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 2048,
                resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
                format = TextureImporterFormat.ASTC_6x6,
                textureCompression = TextureImporterCompression.Compressed,
                compressionQuality = 50,
                crunchedCompression = false,
                allowsAlphaSplitting = hasAlpha
            };
        }
    }
}
