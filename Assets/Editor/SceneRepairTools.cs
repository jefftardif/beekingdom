using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class SceneRepairTools
{
    private const string BootScenePath = "Assets/_Project/Scenes/_Boot.unity";

    [MenuItem("Bee Kingdom/Fix URP Camera Data In Open Scene")]
    public static void FixUrpCameraDataInOpenScene()
    {
        FixUrpCameraDataInCurrentScene(saveScene: false);
    }

    public static void FixBootSceneUrpCameraData()
    {
        EditorSceneManager.OpenScene(BootScenePath);
        FixUrpCameraDataInCurrentScene(saveScene: true);
    }

    private static void FixUrpCameraDataInCurrentScene(bool saveScene)
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        int fixedCount = 0;

        foreach (Camera camera in cameras)
        {
            if (camera.GetComponent<UniversalAdditionalCameraData>() == null)
            {
                Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);

            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Added URP camera data to {fixedCount} camera(s) and saved {scene.path}.");
            }
            else
            {
                Debug.Log($"Added URP camera data to {fixedCount} camera(s). Save the scene to keep the fix.");
            }
        }
        else
        {
            Debug.Log("All cameras already have URP camera data.");
        }
    }
}
