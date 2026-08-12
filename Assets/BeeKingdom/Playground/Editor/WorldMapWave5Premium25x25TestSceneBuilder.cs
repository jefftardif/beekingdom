using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class WorldMapWave5Premium25x25TestSceneBuilder
    {
        [MenuItem("Bee Kingdom/World Map/Rebuild Wave5 Premium 25x25 Test Scene")]
        public static void RebuildScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = WorldMapWave5Premium25x25TestBootstrap.SceneName;

            GameObject bootstrapObject = new GameObject("Wave5 Premium 25x25 Test Runtime");
            bootstrapObject.AddComponent<WorldMapWave5Premium25x25TestBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.016f, 0.012f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            if (!EditorSceneManager.SaveScene(scene, WorldMapWave5Premium25x25TestBootstrap.ScenePath))
            {
                throw new InvalidOperationException("Impossible de sauvegarder la scene Wave5 premium de test.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Wave5 Premium Test] Scene rebuilt: " + WorldMapWave5Premium25x25TestBootstrap.ScenePath);
        }

        [MenuItem("Bee Kingdom/World Map/Open Wave5 Premium 25x25 Test Scene")]
        public static void OpenScene()
        {
            EditorSceneManager.OpenScene(WorldMapWave5Premium25x25TestBootstrap.ScenePath, OpenSceneMode.Single);
        }

        public static void RebuildAndValidate()
        {
            RebuildScene();
            ValidateScene();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static void ValidateScene()
        {
            Scene scene = EditorSceneManager.OpenScene(WorldMapWave5Premium25x25TestBootstrap.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new InvalidOperationException("La scene Wave5 premium de test ne peut pas etre ouverte.");
            if (UnityEngine.Object.FindFirstObjectByType<WorldMapWave5Premium25x25TestBootstrap>() == null)
            {
                throw new InvalidOperationException("Le bootstrap Wave5 premium de test est absent.");
            }

            if (UnityEngine.Object.FindFirstObjectByType<WorldMapMmoFullscreenFoundationBootstrap>() != null)
            {
                throw new InvalidOperationException("Le bootstrap canonique Wave6 ne doit pas etre present dans la scene Wave5 de test.");
            }

            Debug.Log("[Wave5 Premium Test] Scene validation PASS.");
        }
    }
}
