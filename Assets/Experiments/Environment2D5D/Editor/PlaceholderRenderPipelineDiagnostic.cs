#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    public static class PlaceholderRenderPipelineDiagnostic
    {
        private const string ScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";
        private const string PlaceholderPath = "Assets/BeeKingdom/Art/Buildings/BUILDING_PLACEHOLDER_001.png";

        [MenuItem("BeeKingdom/DEBUG/Run Placeholder Render Diagnostic")]
        public static void RunPlaceholderRenderDiagnostic()
        {
            if (!EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single).IsValid())
            {
                Debug.LogError("[PLACEHOLDER_DIAG] Failed to open scene.");
                return;
            }

            var camGo = GameObject.Find("PrototypeCamera");
            if (camGo == null)
            {
                Debug.LogError("[PLACEHOLDER_DIAG] PrototypeCamera not found.");
                return;
            }

            var cam = camGo.GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("[PLACEHOLDER_DIAG] Camera component missing on PrototypeCamera.");
                return;
            }

            var sprite = LoadLargestSprite();
            if (sprite == null)
            {
                Debug.LogError("[PLACEHOLDER_DIAG] Placeholder sprite not found.");
                return;
            }

            var root = GameObject.Find("__TEST_BUILDING_PLACEHOLDERS__") ?? new GameObject("__TEST_BUILDING_PLACEHOLDERS__");
            var placeholder = new GameObject("__DIAG_PLACEHOLDER__");
            placeholder.transform.SetParent(root.transform, false);
            placeholder.transform.position = new Vector3(0f, 0f, 29.95f);
            placeholder.transform.rotation = Quaternion.identity;
            placeholder.transform.localScale = new Vector3(0.27f, 0.27f, 0.27f);

            var sr = placeholder.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.enabled = true;
            sr.color = Color.white;
            sr.sortingLayerID = 0;
            sr.sortingOrder = 1000;

            var litShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            var unlitShader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (litShader != null)
            {
                sr.sharedMaterial = new Material(litShader);
            }

            bool aVisible = false;
            bool bVisible = false;
            bool cVisible = false;
            bool cSameTexture = false;

            // TEST A: unlit sprite at original depth.
            if (unlitShader != null)
            {
                sr.sharedMaterial = new Material(unlitShader);
            }

            aVisible = DetectRendererVisibility(cam, sr);

            // TEST B: move to z=29.0 (only if still invisible).
            if (!aVisible)
            {
                placeholder.transform.position = new Vector3(placeholder.transform.position.x, placeholder.transform.position.y, 29.0f);
                bVisible = DetectRendererVisibility(cam, sr);
            }

            // TEST C: quad mesh using exact same texture.
            if (!aVisible && !bVisible)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "__DIAG_QUAD__";
                quad.transform.SetParent(root.transform, false);
                quad.transform.position = new Vector3(2.5f, 0f, 29.0f);
                quad.transform.rotation = Quaternion.identity;
                quad.transform.localScale = new Vector3(4f, 2.8f, 1f);

                var mr = quad.GetComponent<MeshRenderer>();
                var matShader = Shader.Find("Unlit/Texture") ?? Shader.Find("Universal Render Pipeline/Unlit");
                var tex = sprite.texture;
                if (mr != null && matShader != null && tex != null)
                {
                    var mat = new Material(matShader);
                    if (mat.HasProperty("_BaseMap"))
                    {
                        mat.SetTexture("_BaseMap", tex);
                    }
                    if (mat.HasProperty("_MainTex"))
                    {
                        mat.SetTexture("_MainTex", tex);
                    }
                    mr.sharedMaterial = mat;
                    cSameTexture = true;
                }

                cVisible = mr != null && DetectRendererVisibility(cam, mr);

                UnityEngine.Object.DestroyImmediate(quad);
            }

            Debug.Log("[PLACEHOLDER_DIAG] SelectedSprite=" + sprite.name
                      + " Rect=" + sprite.rect.width.ToString(CultureInfo.InvariantCulture)
                      + "x" + sprite.rect.height.ToString(CultureInfo.InvariantCulture)
                      + " Texture=" + sprite.texture.width.ToString(CultureInfo.InvariantCulture)
                      + "x" + sprite.texture.height.ToString(CultureInfo.InvariantCulture));
            Debug.Log("[PLACEHOLDER_DIAG] A=" + (aVisible ? "visible" : "invisible")
                      + " B=" + (bVisible ? "visible" : "invisible")
                      + " C=" + (cVisible ? "visible" : "invisible")
                      + " C_sameTexture=" + cSameTexture);

            UnityEngine.Object.DestroyImmediate(placeholder);
        }

        private static Sprite LoadLargestSprite()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(PlaceholderPath)
                .OfType<Sprite>()
                .ToArray();
            if (sprites.Length == 0)
            {
                return null;
            }

            return sprites.OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();
        }

        private static bool DetectRendererVisibility(Camera cam, Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            bool previous = renderer.enabled;
            renderer.enabled = false;
            var baseline = Capture(cam);

            renderer.enabled = true;
            var withRenderer = Capture(cam);
            renderer.enabled = previous;

            if (baseline == null || withRenderer == null)
            {
                return false;
            }

            var bounds = renderer.bounds;
            var center = cam.WorldToScreenPoint(bounds.center);
            if (center.z <= 0f)
            {
                return false;
            }

            int width = baseline.GetLength(0);
            int height = baseline.GetLength(1);
            int cx = Mathf.Clamp(Mathf.RoundToInt(center.x), 2, width - 3);
            int cy = Mathf.Clamp(Mathf.RoundToInt(center.y), 2, height - 3);

            int changed = 0;
            const int threshold = 12;
            for (int y = cy - 2; y <= cy + 2; y++)
            {
                for (int x = cx - 2; x <= cx + 2; x++)
                {
                    var a = baseline[x, y];
                    var b = withRenderer[x, y];
                    int diff = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                    if (diff > threshold)
                    {
                        changed++;
                    }
                }
            }

            return changed >= 3;
        }

        private static Color32[,] Capture(Camera cam)
        {
            const int w = 512;
            const int h = 512;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prevRt = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            RenderTexture.active = rt;
            cam.Render();

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            var pixels = tex.GetPixels32();

            var result = new Color32[w, h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    result[x, y] = pixels[y * w + x];
                }
            }

            cam.targetTexture = prevRt;
            RenderTexture.active = prevActive;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
            return result;
        }
    }
}
#endif
