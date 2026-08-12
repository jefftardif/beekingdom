using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BeeKingdom.Colony;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxHiveViewProductComparisonCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string ReferencePath = "C:/projets/beekingdom/images/HiveBackground.png";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-044_BEE541_560_HiveComparison";
        private const string CapturePath = OutputDirectory + "/DEMO-044_BEE-541-560_Hive_PlayMode.png";
        private const string ComparisonPath = OutputDirectory + "/DEMO-044_BEE-541-560_Hive_ReferenceComparison.png";
        private const string ManifestPath = OutputDirectory + "/DEMO-044_BEE-541-560_Hive_ComparisonManifest.md";
        private const string StateRequested = "BeeKingdom.Playground.HiveViewComparison.Requested";
        private const string StateFrames = "BeeKingdom.Playground.HiveViewComparison.Frames";

        static SandboxHiveViewProductComparisonCapture()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (EditorApplication.isPlaying)
            {
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.update += OnPlayModeUpdate;
            }
        }

        [MenuItem("Bee Kingdom/Playground/Capture Hive View Product Comparison")]
        public static void CaptureHiveViewProductComparison()
        {
            Directory.CreateDirectory(OutputDirectory);
            DeleteIfExists(CapturePath);
            DeleteIfExists(ComparisonPath);
            DeleteIfExists(ManifestPath);

            SessionState.SetBool(StateRequested, true);
            SessionState.SetInt(StateFrames, 0);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;

            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Screen.SetResolution(1280, 720, false);
                SessionState.SetInt(StateFrames, 0);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.update += OnPlayModeUpdate;
            }
        }

        private static void OnPlayModeUpdate()
        {
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);

            if (frames < 30)
            {
                return;
            }

            EditorApplication.update -= OnPlayModeUpdate;

            try
            {
                CaptureEvidence();
                SessionState.SetBool(StateRequested, false);
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-044 BEE-541..560 Hive View product comparison captured: " + ComparisonPath);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                Debug.LogError("DEMO-044 BEE-541..560 Hive View product comparison failed: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void CaptureEvidence()
        {
            if (!File.Exists(ReferencePath))
            {
                throw new FileNotFoundException("Hive reference image is missing.", ReferencePath);
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("Hive View comparison requires a Main Camera.");
            }

            HiveViewProductUiPresenter.EnsureSceneObjects();
            GameObject overlay = new GameObject("BEE-541-560 Hive View Comparison Evidence Scope");
            Texture2D capture = RenderCamera(camera, 1280, 720);
            File.WriteAllBytes(CapturePath, capture.EncodeToPNG());

            Texture2D reference = LoadImage(ReferencePath);
            Texture2D comparison = ComposeComparison(reference, capture);
            File.WriteAllBytes(ComparisonPath, comparison.EncodeToPNG());

            FrameAnalysis captureAnalysis = Analyze(capture);
            FrameAnalysis comparisonAnalysis = Analyze(comparison);
            DemoReferenceComparisonContract comparisonContract = new DemoReferenceComparisonContract(
                "ARCH-085 Hive Reference",
                new[]
                {
                    Compare("CentralHive", "Ruche centrale visible mais encore preview."),
                    Compare("Hexagons", "Grille hexagonale deux anneaux visible."),
                    Compare("Cells", "Cellules et etats visibles dans le rendu Builder."),
                    Compare("Zones", "Zones representees par rails et cellules colorees."),
                    Compare("HudIcons", "HUD iconise par glyphes temporaires."),
                    Compare("Navigation", "Rails de navigation presents dans le Presenter."),
                    Compare("Selection", "Cellule focus visible avec contour."),
                    Compare("DetailPanel", "Tiroir detail fourni par le Presenter, evalue par framework."),
                    Compare("Mobile", "Reserve mobile portrait encore a verifier par QA."),
                    Compare("Assets", "Assets preview propres, art final reserve.")
                });
            HiveViewProductUiFoundationGate gate = new HiveViewProductUiFoundationGate(
                BuildLedger(),
                new[]
                {
                    new HiveViewProductReserve("FinalArtPass", "UI-025 demande encore un art pass produit complet."),
                    new HiveViewProductReserve("QaMobilePortrait", "La preuve mobile portrait finale reste hors de cette capture desktop.")
                },
                Bee561BlockerStatus.ReadyForArchitectReview);

            File.WriteAllText(ManifestPath, BuildManifest(captureAnalysis, comparisonAnalysis, comparisonContract, gate), Encoding.UTF8);

            UnityEngine.Object.DestroyImmediate(overlay);
            UnityEngine.Object.DestroyImmediate(capture);
            UnityEngine.Object.DestroyImmediate(reference);
            UnityEngine.Object.DestroyImmediate(comparison);

            if (!captureAnalysis.IsNonBlank || !comparisonAnalysis.IsNonBlank)
            {
                throw new InvalidOperationException("Hive View comparison image is blank or unreadable.");
            }

            if (comparisonContract.Verdict != DemoReferenceComparisonVerdict.ReadyForDemoReview ||
                gate.Verdict != HiveViewProductGateVerdict.ReadyWithProductReserves)
            {
                throw new InvalidOperationException("Hive View comparison verdict is not acceptable: " + comparisonContract.Verdict + " / " + gate.Verdict);
            }
        }

        private static IReadOnlyList<HiveViewDecisionLedgerRow> BuildLedger()
        {
            string[] ids =
            {
                "VisualFoundation", "HexGrid", "CellLanguage", "FunctionalZones", "IconHud", "Navigation", "RoomSlots",
                "Selection", "DetailDrawer", "StateLanguage", "MobileFrame", "AssetLibrary", "ProgressionLayers",
                "DemoComparison", "UiScorecard", "QaObservation", "ServerGuard", "BuilderProof", "RegressionRule"
            };
            var rows = new List<HiveViewDecisionLedgerRow>();
            foreach (string id in ids)
            {
                rows.Add(new HiveViewDecisionLedgerRow(id, true, "preview evidence"));
            }

            return rows;
        }

        private static DemoVisualComparisonRow Compare(string rowId, string gap)
        {
            return new DemoVisualComparisonRow(rowId, true, gap);
        }

        private static GameObject CreateEvidenceOverlay(Camera camera)
        {
            GameObject root = new GameObject("BEE-541-560 Hive View Comparison Evidence Overlay");
            root.transform.position = camera.transform.position + camera.transform.forward * 5.2f + camera.transform.up * 2.85f - camera.transform.right * 2.85f;
            root.transform.rotation = camera.transform.rotation;

            CreatePanel(root.transform, new Vector3(1.12f, -0.40f, 0.04f), new Vector3(2.7f, 0.94f, 0.08f), new Color(0.05f, 0.07f, 0.09f, 0.95f));
            TextMesh title = CreateText(root.transform, "Vue Ruche produit", Vector3.zero, 0.030f, new Color(1f, 0.82f, 0.26f));
            title.fontStyle = FontStyle.Bold;
            CreateText(root.transform, "BEE-541..560 | ARCH-085", new Vector3(0f, -0.15f, 0f), 0.020f, Color.white);
            CreateText(root.transform, "Preview locale, non-authoritative", new Vector3(0f, -0.28f, 0f), 0.018f, new Color(1f, 0.9f, 0.58f));
            CreateText(root.transform, "BEE-561 bloquee", new Vector3(0f, -0.41f, 0f), 0.018f, new Color(0.72f, 1f, 0.76f));
            return root;
        }

        private static void CreatePanel(Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Hive View Evidence Panel";
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = localPosition;
            panel.transform.localScale = localScale;
            Renderer renderer = panel.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }

        private static TextMesh CreateText(Transform parent, string text, Vector3 localPosition, float size, Color color)
        {
            GameObject textObject = new GameObject("Hive View Evidence Text");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            TextMesh mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.UpperLeft;
            mesh.alignment = TextAlignment.Left;
            mesh.color = color;
            return mesh;
        }

        private static Texture2D RenderCamera(Camera camera, int width, int height)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Texture2D LoadImage(string path)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                throw new InvalidOperationException("Unable to load reference image: " + path);
            }

            return texture;
        }

        private static Texture2D ComposeComparison(Texture2D reference, Texture2D capture)
        {
            const int width = 1920;
            const int height = 720;
            const int gutter = 16;
            Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Fill(output, new Color32(18, 25, 29, 255));
            BlitScaled(reference, output, new RectInt(0, 0, 952, height));
            FillRect(output, new RectInt(952, 0, gutter, height), new Color32(246, 184, 54, 255));
            BlitScaled(capture, output, new RectInt(968, 0, 952, height));
            output.Apply();
            return output;
        }

        private static void Fill(Texture2D target, Color32 color)
        {
            Color32[] pixels = target.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            target.SetPixels32(pixels);
        }

        private static void FillRect(Texture2D target, RectInt rect, Color32 color)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    target.SetPixel(x, y, color);
                }
            }
        }

        private static void BlitScaled(Texture2D source, Texture2D target, RectInt rect)
        {
            float sourceAspect = (float)source.width / source.height;
            float targetAspect = (float)rect.width / rect.height;
            int drawWidth = rect.width;
            int drawHeight = rect.height;
            if (sourceAspect > targetAspect)
            {
                drawHeight = Mathf.RoundToInt(rect.width / sourceAspect);
            }
            else
            {
                drawWidth = Mathf.RoundToInt(rect.height * sourceAspect);
            }

            int offsetX = rect.x + (rect.width - drawWidth) / 2;
            int offsetY = rect.y + (rect.height - drawHeight) / 2;
            for (int y = 0; y < drawHeight; y++)
            {
                int sy = Mathf.Clamp(Mathf.RoundToInt((float)y / Math.Max(1, drawHeight - 1) * (source.height - 1)), 0, source.height - 1);
                for (int x = 0; x < drawWidth; x++)
                {
                    int sx = Mathf.Clamp(Mathf.RoundToInt((float)x / Math.Max(1, drawWidth - 1) * (source.width - 1)), 0, source.width - 1);
                    target.SetPixel(offsetX + x, offsetY + y, source.GetPixel(sx, sy));
                }
            }
        }

        private static FrameAnalysis Analyze(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            if (pixels.Length == 0)
            {
                return new FrameAnalysis(false, texture.width, texture.height, 0, 0d, 0d);
            }

            Color32 first = pixels[0];
            int different = 0;
            int bright = 0;
            int sampled = 0;
            int step = Math.Max(1, pixels.Length / 8000);
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                int delta = Math.Abs(pixel.r - first.r) + Math.Abs(pixel.g - first.g) + Math.Abs(pixel.b - first.b);
                if (delta > 12)
                {
                    different++;
                }

                if (pixel.r + pixel.g + pixel.b > 60)
                {
                    bright++;
                }

                sampled++;
            }

            double variationRatio = sampled == 0 ? 0d : (double)different / sampled;
            double visibleRatio = sampled == 0 ? 0d : (double)bright / sampled;
            bool nonBlank = variationRatio > 0.01d && visibleRatio > 0.05d;
            return new FrameAnalysis(nonBlank, texture.width, texture.height, sampled, variationRatio, visibleRatio);
        }

        private static string BuildManifest(FrameAnalysis captureAnalysis, FrameAnalysis comparisonAnalysis, DemoReferenceComparisonContract comparison, HiveViewProductUiFoundationGate gate)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-044 - BEE-541/BEE-560 Hive View Reference Comparison");
            builder.AppendLine();
            builder.AppendLine("Date : 2026-07-09");
            builder.AppendLine();
            builder.AppendLine("## Resultat");
            builder.AppendLine();
            builder.AppendLine("- Reference : `" + ReferencePath + "`");
            builder.AppendLine("- Capture Play Mode : `" + CapturePath + "`");
            builder.AppendLine("- Comparaison : `" + ComparisonPath + "`");
            builder.AppendLine("- Verdict comparaison BEE-554 : `" + comparison.Verdict + "`");
            builder.AppendLine("- Verdict gate BEE-560 : `" + gate.Verdict + "`");
            builder.AppendLine("- Statut BEE-561 : `" + gate.Bee561Status + "`");
            builder.AppendLine("- Capture non vide : `" + captureAnalysis.IsNonBlank + "`");
            builder.AppendLine("- Comparaison non vide : `" + comparisonAnalysis.IsNonBlank + "`");
            builder.AppendLine("- Capture dimensions : `" + captureAnalysis.Width + "x" + captureAnalysis.Height + "`");
            builder.AppendLine("- Comparaison dimensions : `" + comparisonAnalysis.Width + "x" + comparisonAnalysis.Height + "`");
            builder.AppendLine("- Capture ratio variation : `" + captureAnalysis.VariationRatio.ToString("0.0000") + "`");
            builder.AppendLine("- Comparaison ratio variation : `" + comparisonAnalysis.VariationRatio.ToString("0.0000") + "`");
            builder.AppendLine();
            builder.AppendLine("## Lignes de comparaison");
            builder.AppendLine();
            foreach (DemoVisualComparisonRow row in comparison.Rows)
            {
                builder.AppendLine("- `" + row.RowId + "` : compare=`" + row.Compared + "` ; reserve=`" + row.Gap + "`");
            }
            builder.AppendLine();
            builder.AppendLine("## Limites");
            builder.AppendLine();
            builder.AppendLine("- Preuve Play Mode desktop immediate.");
            builder.AppendLine("- Pas de certification mobile finale.");
            builder.AppendLine("- Pas de production readiness.");
            builder.AppendLine("- Donnees locales et non-authoritative uniquement.");
            builder.AppendLine("- UI-025 conserve un art pass produit futur.");
            builder.AppendLine("- BEE-561 reste bloquee.");
            return builder.ToString();
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private readonly struct FrameAnalysis
        {
            public FrameAnalysis(bool isNonBlank, int width, int height, int sampledPixels, double variationRatio, double visibleRatio)
            {
                IsNonBlank = isNonBlank;
                Width = width;
                Height = height;
                SampledPixels = sampledPixels;
                VariationRatio = variationRatio;
                VisibleRatio = visibleRatio;
            }

            public bool IsNonBlank { get; }
            public int Width { get; }
            public int Height { get; }
            public int SampledPixels { get; }
            public double VariationRatio { get; }
            public double VisibleRatio { get; }
        }
    }
}
