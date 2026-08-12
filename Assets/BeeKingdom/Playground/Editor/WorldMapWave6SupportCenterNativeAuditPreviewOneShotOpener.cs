using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class WorldMapWave6SupportCenterNativeAuditPreviewOneShotOpener
    {
        private const string RequestFilePath = "Temp/OpenWave6SupportCenterNativeAuditPreview.request";
        private const string ScenePath = "Assets/Scenes/WorldMapWave6SupportCenterNativeAuditPreview.unity";

        static WorldMapWave6SupportCenterNativeAuditPreviewOneShotOpener()
        {
            EditorApplication.delayCall += TryOpenRequestedScene;
        }

        private static void TryOpenRequestedScene()
        {
            if (!File.Exists(RequestFilePath))
            {
                return;
            }

            try
            {
                File.Delete(RequestFilePath);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"Could not clear Wave6 support-center scene request: {exception.Message}");
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Wave6 support-center scene request detected while Play Mode is active. Stop Play Mode and re-create the request file.");
                return;
            }

            if (SceneManager.GetActiveScene().path == ScenePath)
            {
                PlaygroundPlayModeStartScene.UseWave6SupportCenterNativeAuditPreviewOnPlay();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("Wave6 support-center scene request was cancelled because current scene changes were not saved.");
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlaygroundPlayModeStartScene.UseWave6SupportCenterNativeAuditPreviewOnPlay();
            Debug.Log("Opened Wave6 support-center native audit preview scene from one-shot request.");
        }
    }
}
