using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class WorldMapWave6ExactCropValidationAutorun
    {
        private const string FlagPath = "Temp/RunWave6ExactCropUnityValidation.flag";

        static WorldMapWave6ExactCropValidationAutorun()
        {
            EditorApplication.delayCall += TryRunRequestedValidation;
        }

        private static void TryRunRequestedValidation()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string flagPath = Path.GetFullPath(Path.Combine(projectRoot, FlagPath));
            if (!File.Exists(flagPath)) return;

            try
            {
                File.Delete(flagPath);
                Debug.Log("[Wave6 50x50 Exact Crop] Autorun requested; reimporting exact-crop textures and validating runtime.");
                WorldMapWave6TextureImportGuard.ReimportWave5Method12288ExactCropTextures();
                WorldMapWave6ExactCropRuntimeValidator.ValidateExactCropRuntime();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }
    }
}
