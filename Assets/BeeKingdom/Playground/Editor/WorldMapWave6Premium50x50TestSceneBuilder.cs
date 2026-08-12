using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave6Premium50x50TestSceneBuilder
    {
        [MenuItem("Bee Kingdom/World Map/Rebuild Wave6 Premium 50x50 Terrain Test Scene")]
        public static void RebuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = WorldMapWave6Premium50x50TestBootstrap.SceneName;

            GameObject bootstrapObject = new GameObject("Wave6 Premium 50x50 Terrain Test Runtime");
            bootstrapObject.AddComponent<WorldMapWave6Premium50x50TestBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.016f, 0.012f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            if (!EditorSceneManager.SaveScene(scene, WorldMapWave6Premium50x50TestBootstrap.ScenePath))
            {
                throw new InvalidOperationException("Impossible de sauvegarder la scene Wave6 50x50 terrain test.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Wave6 50x50 Terrain Test] Scene rebuilt: " + WorldMapWave6Premium50x50TestBootstrap.ScenePath);
        }

        [MenuItem("Bee Kingdom/World Map/Open Wave6 Premium 50x50 Terrain Test Scene")]
        public static void OpenScene()
        {
            EditorSceneManager.OpenScene(WorldMapWave6Premium50x50TestBootstrap.ScenePath, OpenSceneMode.Single);
        }

        public static void RebuildAndValidate()
        {
            RebuildScene();
            ValidateScene();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void ValidateScene()
        {
            Scene scene = EditorSceneManager.OpenScene(WorldMapWave6Premium50x50TestBootstrap.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new InvalidOperationException("La scene Wave6 50x50 terrain test ne peut pas etre ouverte.");
            if (UnityEngine.Object.FindFirstObjectByType<WorldMapWave6Premium50x50TestBootstrap>() == null)
            {
                throw new InvalidOperationException("Le bootstrap Wave6 50x50 terrain test est absent.");
            }

            if (UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>() != null)
            {
                throw new InvalidOperationException("Le bootstrap MMO ne doit pas etre present dans la scene terrain test.");
            }

            Debug.Log("[Wave6 50x50 Terrain Test] Scene validation PASS.");
        }
    }
}
