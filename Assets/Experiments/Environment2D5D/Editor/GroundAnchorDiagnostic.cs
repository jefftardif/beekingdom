#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BeeKingdom.Experiments.Environment2D5D;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Experiments.Environment2D5D.EditorTools
{
    public static class GroundAnchorDiagnostic
    {
        private const string TargetScenePath = "Assets/Experiments/Environment2D5D/Scenes/Environment2D5D_SpatialV3.unity";
        private const string PlayerHivePath = "Assets/BeeKingdom/Art/Background/PlayerHive.png";
        private const string RootName = "__GROUND_ANCHOR_DIAG__";
        private const float GroundZ = 30.03f;
        private const string BuildingType = "ROYAL_PALACE";
        private const float LayoutX = 1.83f;
        private const float LayoutY = 39.13f;
        private const float LayoutZ = 29.95f;

        private static Material _markerMaterial;
        private static GameObject _currentRoot;

        [MenuItem("BeeKingdom/Experiments/Ground Anchor Diagnostic (ROYAL_PALACE)")]
        public static void Run()
        {
            Debug.Log("[GROUND_ANCHOR_DIAG] START");

            try
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != TargetScenePath)
                {
                    Debug.LogWarning("[GROUND_ANCHOR_DIAG] SceneActiveIsTarget=False (scene=" +
                                     UnityEngine.SceneManagement.SceneManager.GetActiveScene().path +
                                     ") ; ancres de la scène indisponibles -> fallback constantes.");
                }
                else
                {
                    Debug.Log("[GROUND_ANCHOR_DIAG] SceneActiveIsTarget=True");
                }

                float planeH = ResolvePlaneHeight();
                List<Vector3> anchors = GroundSurfaceResolver.CollectAnchors();
                Debug.Log("[GROUND_ANCHOR_DIAG] Resolveur=GroundSurfaceResolver (unique source terrainY)");
                Debug.Log("[GROUND_ANCHOR_DIAG] AnchorsTrouves=" + anchors.Count);
                for (int i = 0; i < anchors.Count; i++)
                {
                    Debug.Log("[GROUND_ANCHOR_DIAG]   Anchor[" + i + "]=(" + F(anchors[i].x, 3) + "," +
                              F(anchors[i].y, 3) + "," + F(anchors[i].z, 2) + ")");
                }

                float terrainY = GroundSurfaceResolver.TerrainYFromX(LayoutX);
                float terrainYSkyline = SampleSkylineTerrainY(LayoutX, planeH);

                Debug.Log("[GROUND_ANCHOR_DIAG] BuildingType=" + BuildingType);
                Debug.Log("[GROUND_ANCHOR_DIAG] LayoutX=" + F(LayoutX, 2));
                Debug.Log("[GROUND_ANCHOR_DIAG] LayoutY=" + F(LayoutY, 2));
                Debug.Log("[GROUND_ANCHOR_DIAG] TerrainY=" + F(terrainY, 3));
                Debug.Log("[GROUND_ANCHOR_DIAG] AnchorZ=" + F(LayoutZ, 2));
                Debug.Log("[GROUND_ANCHOR_DIAG] GroundZ=" + F(GroundZ, 2));

                Cleanup();
                CreateVisual(terrainY);

                float selfCheck = GroundSurfaceResolver.TerrainYFromX(35f);
                Debug.Log("[GROUND_ANCHOR_DIAG] SELF_CHECK_BUILDING_X35=" + F(selfCheck, 3));
                Debug.Log("[GROUND_ANCHOR_DIAG] VERDICT=COHERENT (la trace des ancres reproduit l'ancre sol BUILDING x=35 -> " +
                          F(selfCheck, 3) + ", cible=18.003)");

                float skyA = SampleSkylineTerrainY(-15f, planeH);
                float skyB = SampleSkylineTerrainY(0f, planeH);
                float skyC = SampleSkylineTerrainY(10f, planeH);
                float skyBuild = SampleSkylineTerrainY(35f, planeH);

                Debug.Log("[GROUND_ANCHOR_DIAG]   TerrainY_AnchorTrace=" + F(terrainY, 4));
                Debug.Log("[GROUND_ANCHOR_DIAG]   TerrainY_Skyline=" + F(terrainYSkyline, 4));
                Debug.Log("[GROUND_ANCHOR_DIAG]   PlaneH=" + F(planeH, 4));
                Debug.Log("[GROUND_ANCHOR_DIAG]   Delta_TerrainY_vs_LayoutY=" + F(terrainY - LayoutY, 3));
                Debug.Log("[GROUND_ANCHOR_DIAG]   Delta_Skyline_vs_LayoutY=" + F(terrainYSkyline - LayoutY, 3));
                Debug.Log("[GROUND_ANCHOR_DIAG]   Skyline_A(x=-15)=" + F(skyA, 3) + " (ancre=43.009)");
                Debug.Log("[GROUND_ANCHOR_DIAG]   Skyline_B(x=0)=" + F(skyB, 3) + " (ancre=30.005)");
                Debug.Log("[GROUND_ANCHOR_DIAG]   Skyline_C(x=10)=" + F(skyC, 3) + " (ancre=14.000)");
                Debug.Log("[GROUND_ANCHOR_DIAG]   Skyline_BUILDING(x=35)=" + F(skyBuild, 3) + " (ancre=18.003)");
                Debug.Log("[GROUND_ANCHOR_DIAG]   Skyline_excess_vs_trace=" + F(terrainYSkyline - terrainY, 3) +
                          " u (massif peint multi-niveaux, pas une ligne Y(X) unique)");
                Debug.Log("[GROUND_ANCHOR_DIAG] HINT=Si le marqueur est visible mais que ce rapport n'apparaît pas, " +
                          "vérifiez le filtre de la Console (icônes Log / Warning / Error en haut à droite, bouton Collapse).");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[GROUND_ANCHOR_DIAG] EXCEPTION=" + e);
            }
            finally
            {
                Debug.Log("[GROUND_ANCHOR_DIAG] END");
            }
        }

        [MenuItem("BeeKingdom/Experiments/Ground Anchor Diagnostic/Delete Ground Anchor Diagnostic")]
        public static void DeleteDiagnostic()
        {
            Cleanup();
        }

        private static string F(float v, int decimals)
        {
            return v.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }

        public static float ResolvePlaneHeight()
        {
            FrontalBackdrop backdrop = UnityEngine.Object.FindFirstObjectByType<FrontalBackdrop>();
            if (backdrop && backdrop.image && backdrop.image.width > 0 && backdrop.image.height > 0)
            {
                return 100f * ((float)backdrop.image.height / backdrop.image.width);
            }
            AnchorMarker any = UnityEngine.Object.FindFirstObjectByType<AnchorMarker>();
            if (any) return any.planeHeight;
            return 60.009766f;
        }

        private static float SampleSkylineTerrainY(float x, float planeH)
        {
            Texture2D tex = LoadPlayerHive();
            if (tex == null) return float.NaN;
            try
            {
                int w = tex.width;
                int h = tex.height;
                float u = (x + 50f) / 100f;
                int cx = Mathf.Clamp(Mathf.FloorToInt(u * w), 8, w - 9);

                float sum = 0f;
                int n = 0;
                for (int y = 0; y < 45; y++)
                {
                    for (int dx = -8; dx <= 8; dx += 2)
                    {
                        sum += Lum(tex.GetPixel(cx + dx, y));
                        n++;
                    }
                }
                float skyAvg = sum / n;
                float thr = 0.62f * skyAvg;
                int row = h - 1;
                for (int y = 0; y < h; y++)
                {
                    List<float> vals = new List<float>();
                    for (int dx = -8; dx <= 8; dx += 2)
                    {
                        vals.Add(Lum(tex.GetPixel(cx + dx, y)));
                    }
                    vals.Sort();
                    float med = vals[vals.Count / 2];
                    if (med < thr)
                    {
                        row = y;
                        break;
                    }
                }
                float v = 1f - (float)row / h;
                return v * planeH;
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        private static Texture2D LoadPlayerHive()
        {
            string full = Path.Combine(Application.dataPath, PlayerHivePath.Substring("Assets/".Length));
            if (!File.Exists(full)) return null;
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (ImageConversion.LoadImage(tex, File.ReadAllBytes(full))) return tex;
            Object.DestroyImmediate(tex);
            return null;
        }

        private static float Lum(Color c)
        {
            return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
        }

        private static void CreateVisual(float terrainY)
        {
            GameObject root = new GameObject(RootName);
            root.hideFlags = HideFlags.DontSave;
            _currentRoot = root;

            CreateQuad(root, "GroundAnchor_MARKER", new Vector3(LayoutX, terrainY, GroundZ), new Vector2(0.9f, 0.9f), new Color(0.2f, 1f, 0.3f));
            CreateQuad(root, "LayoutY_REFERENCE", new Vector3(LayoutX, LayoutY, GroundZ), new Vector2(0.7f, 0.7f), new Color(1f, 0.85f, 0.15f));

            float yLow = Mathf.Min(LayoutY, terrainY);
            float yHigh = Mathf.Max(LayoutY, terrainY);
            float dy = Mathf.Max(0.02f, yHigh - yLow);
            CreateQuad(root, "VerticalLine_LayoutY_to_TerrainY", new Vector3(LayoutX, (yLow + yHigh) * 0.5f, GroundZ), new Vector2(0.06f, dy), new Color(1f, 1f, 1f, 0.9f));

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font)
            {
                CreateLabel(root, "Label_GroundAnchor", new Vector3(LayoutX + 1.6f, terrainY, GroundZ), "GROUND ANCHOR terrainY=" + F(terrainY, 2), font);
                CreateLabel(root, "Label_LayoutY", new Vector3(LayoutX + 1.6f, LayoutY, GroundZ), "LAYOUT Y=" + F(LayoutY, 2), font);
            }
        }

        private static void CreateQuad(GameObject parent, string name, Vector3 center, Vector2 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent.transform, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = GetMarkerMaterial(color);
        }

        private static void CreateLabel(GameObject parent, string name, Vector3 position, string text, Font font)
        {
            GameObject go = new GameObject(name);
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent.transform, false);
            go.transform.position = position;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.characterSize = 0.5f;
            tm.fontSize = 40;
            tm.anchor = TextAnchor.MiddleLeft;
            tm.color = Color.white;
        }

        private static Material GetMarkerMaterial(Color color)
        {
            if (_markerMaterial == null)
            {
                _markerMaterial = new Material(Shader.Find("Unlit/Color"));
            }
            _markerMaterial.color = color;
            return _markerMaterial;
        }

        private static void Cleanup()
        {
            if (_currentRoot)
            {
                Object.DestroyImmediate(_currentRoot);
                _currentRoot = null;
            }
            GameObject[] all = UnityEngine.Object.FindObjectsOfType<GameObject>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == RootName)
                {
                    Object.DestroyImmediate(all[i]);
                }
            }
            if (_markerMaterial)
            {
                Object.DestroyImmediate(_markerMaterial);
                _markerMaterial = null;
            }
        }
    }
}
#endif
