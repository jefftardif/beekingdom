using System.Collections;
using System.IO;
using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed class SandboxGameViewScreenshotWriter : MonoBehaviour
    {
        private string path;

        public static void Request(string outputPath)
        {
            GameObject obj = new GameObject("Sandbox Game View Screenshot Writer");
            DontDestroyOnLoad(obj);
            SandboxGameViewScreenshotWriter writer = obj.AddComponent<SandboxGameViewScreenshotWriter>();
            writer.path = outputPath;
        }

        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
            try
            {
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Destroy(texture);
                Destroy(gameObject);
            }
        }
    }
}
