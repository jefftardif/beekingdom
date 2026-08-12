using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBeeAntLegionUiTextureAutorun
    {
        private const string RequestPath = "Artifacts/HiveUiAntLegionTexture/request-capture.txt";
        private const string ConsumedPath = "Artifacts/HiveUiAntLegionTexture/request-capture.consumed.txt";
        private const string AutorunRevision = "living-hive-textured-ui-2026-07-19";

        static SandboxBeeAntLegionUiTextureAutorun()
        {
            EditorApplication.delayCall -= TryConsumeRequest;
            EditorApplication.delayCall += TryConsumeRequest;
            EditorApplication.update -= PollForRequest;
            EditorApplication.update += PollForRequest;
        }

        private static void PollForRequest()
        {
            if (!File.Exists(RequestPath)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (Application.isPlaying) EditorApplication.ExitPlaymode();
                return;
            }

            TryConsumeRequest();
        }

        private static void TryConsumeRequest()
        {
            try
            {
                if (!File.Exists(RequestPath)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(ConsumedPath) ?? "Artifacts/HiveUiAntLegionTexture");
                File.WriteAllText(ConsumedPath, "Consumed at " + DateTime.UtcNow.ToString("O") + " by " + AutorunRevision);
                File.Delete(RequestPath);
                SandboxBeeAntLegionUiTextureCapture.CaptureAntLegionTextureUiProof();
            }
            catch (Exception exception)
            {
                Debug.LogError("Ant Legion texture UI autorun failed: " + exception);
            }
        }
    }
}
