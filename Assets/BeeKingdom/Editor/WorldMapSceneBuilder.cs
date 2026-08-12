using System.IO;
using BeeKingdom.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Editor
{
    // Construit la scene de demonstration de la fondation de la Carte du Monde :
    // camera orthographique (controleur + entree + overlay debug) et runtime de la
    // carte (WorldManager + source de contenu neutre). Regenerable a tout moment.
    public static class WorldMapSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/WorldMapFoundation.unity";

        [MenuItem("BeeKingdom/World Map/Build Foundation Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WorldMapFoundation";

            Camera camera = BuildCamera();
            BuildRuntime();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.Log("[WorldMap] Scene foundation construite : " + ScenePath);
            }
            else
            {
                Debug.LogError("[WorldMap] Echec de sauvegarde de la scene foundation.");
            }
        }

        // Point d'entree CLI : -executeMethod BeeKingdom.Editor.WorldMapSceneBuilder.Execute
        public static void Execute()
        {
            Build();
            EditorApplication.Exit(0);
        }

        private static Camera BuildCamera()
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 64f;
            camera.backgroundColor = new Color(0.02f, 0.035f, 0.025f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.nearClipPlane = -50f;
            camera.farClipPlane = 200f;
            go.AddComponent<AudioListener>();
            go.AddComponent<WorldCameraController>();
            go.AddComponent<UnityWorldInputSource>();
            go.AddComponent<WorldDebugOverlay>();
            go.transform.position = new Vector3(32f, 32f, -10f);
            return camera;
        }

        private static void BuildRuntime()
        {
            GameObject go = new GameObject("WorldMapRuntime");
            go.AddComponent<WorldManager>();
            go.AddComponent<NeutralTerrainContentSource>();
        }
    }
}
