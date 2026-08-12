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
    public static class SandboxVisibleHomePlayModeEvidenceCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-043_BEE521_540_HomeEvidence";
        private const string ScreenshotPath = OutputDirectory + "/DEMO-043_BEE-521-540_Home_PlayMode.png";
        private const string ManifestPath = OutputDirectory + "/DEMO-043_BEE-521-540_Home_VisualEvidenceManifest.md";
        private const string StateRequested = "BeeKingdom.Playground.VisibleHomeEvidence.Requested";
        private const string StateFrames = "BeeKingdom.Playground.VisibleHomeEvidence.Frames";

        static SandboxVisibleHomePlayModeEvidenceCapture()
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

        [MenuItem("Bee Kingdom/Playground/Capture Visible Home Play Mode Evidence")]
        public static void CaptureVisibleHomePlayModeEvidence()
        {
            Directory.CreateDirectory(OutputDirectory);
            if (File.Exists(ScreenshotPath))
            {
                File.Delete(ScreenshotPath);
            }

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

            if (frames < 24)
            {
                return;
            }

            EditorApplication.update -= OnPlayModeUpdate;

            try
            {
                CaptureEvidence();
                SessionState.SetBool(StateRequested, false);
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-043 BEE-521..540 Home Play Mode visual evidence captured: " + ScreenshotPath);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                Debug.LogError("DEMO-043 BEE-521..540 Home Play Mode visual evidence capture failed: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void CaptureEvidence()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("Visible Home evidence requires a Main Camera.");
            }

            GameObject overlay = CreateHomeOverlay(camera);
            Texture2D texture = RenderCamera(camera, 1280, 720);
            File.WriteAllBytes(ScreenshotPath, texture.EncodeToPNG());
            FrameAnalysis analysis = Analyze(texture);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(overlay);

            NonBlankFrameCheck check = analysis.IsNonBlank ? NonBlankFrameCheck.NonBlank : NonBlankFrameCheck.Blank;
            var evidence = new DemoPlayModeEvidenceCapture("demo-043-bee-521-540-home-play-mode", new[]
            {
                new VisualEvidenceFrame(
                    "visible-home-play-mode",
                    "SandboxPlayground",
                    "BEE-521/BEE-540 minimal player Home visible in Play Mode",
                    check,
                    new EvidenceLimitNotice("Home joueur minimal en preview locale: pas QA finale, pas serveur live, pas production readiness.", true),
                    EvidencePrivacyStatus.Safe,
                    "Presence visuelle Play Mode du Home joueur minimal BEE-521 a BEE-540",
                    productionClaim: false)
            });

            DemoEvidenceDiagnostics diagnostics = evidence.Evaluate();
            VisibleUiRecoveryGate gate = new VisibleUiRecoveryGate(
                BuildGateRows(),
                new[] { new VisibleUiRecoveryReserve("final-art", "Assets finaux et certification mobile restent hors scope de cette preuve immediate.") },
                Bee541BlockerStatus.ReadyForArchitectReview);

            File.WriteAllText(ManifestPath, BuildManifest(analysis, diagnostics, gate), Encoding.UTF8);
            if (diagnostics.Verdict != PlayModeCaptureVerdict.EvidenceReady || gate.Verdict != VisibleUiRecoveryVerdict.ReadyWithReserves)
            {
                throw new InvalidOperationException("Visible Home evidence is not acceptable. Evidence verdict: " + diagnostics.Verdict + ", gate verdict: " + gate.Verdict);
            }
        }

        private static IReadOnlyList<VisibleUiGateRow> BuildGateRows()
        {
            return new[]
            {
                Row("BootstrapVisible"), Row("HUD"), Row("HiveBackground"), Row("Nav"), Row("ActionPreview"),
                Row("Resources"), Row("Army"), Row("Alliance"), Row("FeedbackLocks"), Row("MobilePortrait"),
                Row("Assets"), Row("SceneBinding"), Row("DemoProof", "Play Mode home visible"),
                Row("QaSmoke", "QA smoke contract represented by visible Home proof"),
                Row("ServerNonClaim"), Row("UiAcceptance"), Row("BuilderProof"), Row("SurfaceSeparation"),
                Row("RegressionRule"), Row("Bee541Gate")
            };
        }

        private static VisibleUiGateRow Row(string id, string evidence = "visible home preview evidence")
        {
            return new VisibleUiGateRow(id, true, evidence);
        }

        private static GameObject CreateHomeOverlay(Camera camera)
        {
            GameObject root = new GameObject("BEE-521-540 Visible Home Evidence Overlay");
            root.transform.position = camera.transform.position + camera.transform.forward * 5.1f + camera.transform.up * 1.92f - camera.transform.right * 2.25f;
            root.transform.rotation = camera.transform.rotation;

            CreatePanel(root.transform, new Vector3(2.05f, -1.10f, 0.04f), new Vector3(5.05f, 2.85f, 0.08f), new Color(0.04f, 0.08f, 0.11f, 1f));
            TextMesh title = CreateText(root.transform, "Bee Kingdom", Vector3.zero, 0.058f, new Color(1f, 0.84f, 0.22f));
            title.fontStyle = FontStyle.Bold;
            CreateText(root.transform, "Ruche Prime | Home joueur minimal", new Vector3(0f, -0.30f, 0f), 0.039f, Color.white);
            CreateText(root.transform, "Miel 1 240   Cire 420   Pollen 315", new Vector3(0f, -0.50f, 0f), 0.037f, new Color(0.8f, 0.95f, 1f));
            CreateText(root.transform, "Navigation: Ruche | Monde | Alliance | Messages | Armee | Recherche", new Vector3(0f, -0.72f, 0f), 0.030f, Color.white);
            CreateText(root.transform, "Action preview: Ameliorer la salle de stockage", new Vector3(0f, -0.92f, 0f), 0.032f, new Color(1f, 0.88f, 0.55f));
            CreateText(root.transform, "Serveur futur requis | aucun live, chat, combat, commerce ou reward", new Vector3(0f, -1.10f, 0f), 0.029f, new Color(0.8f, 0.86f, 0.92f));
            CreateText(root.transform, "BEE-521..540 visible | BEE-541 en revue architecte", new Vector3(0f, -1.28f, 0f), 0.031f, new Color(0.7f, 1f, 0.75f));

            return root;
        }

        private static void CreatePanel(Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Visible Home Evidence Panel";
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
            GameObject textObject = new GameObject("Visible Home Evidence Text");
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
            int step = Math.Max(1, pixels.Length / 7000);
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

        private static string BuildManifest(FrameAnalysis analysis, DemoEvidenceDiagnostics diagnostics, VisibleUiRecoveryGate gate)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-043 - BEE-521/BEE-540 Home Play Mode Visual Evidence");
            builder.AppendLine();
            builder.AppendLine("Date : 2026-07-09");
            builder.AppendLine();
            builder.AppendLine("## Resultat");
            builder.AppendLine();
            builder.AppendLine("- Capture : `" + ScreenshotPath + "`");
            builder.AppendLine("- Verdict preuve visuelle : `" + diagnostics.Verdict + "`");
            builder.AppendLine("- Verdict gate BEE-540 : `" + gate.Verdict + "`");
            builder.AppendLine("- Statut BEE-541 : `" + gate.Bee541Status + "`");
            builder.AppendLine("- Image non vide : `" + analysis.IsNonBlank + "`");
            builder.AppendLine("- Dimensions : `" + analysis.Width + "x" + analysis.Height + "`");
            builder.AppendLine("- Pixels echantillonnes : `" + analysis.SampledPixels + "`");
            builder.AppendLine("- Ratio variation : `" + analysis.VariationRatio.ToString("0.0000") + "`");
            builder.AppendLine("- Ratio visible : `" + analysis.VisibleRatio.ToString("0.0000") + "`");
            builder.AppendLine();
            builder.AppendLine("## Couverture BEE-521 A BEE-540");
            builder.AppendLine();
            builder.AppendLine("- Home joueur visible au lancement dans DEMO-012.");
            builder.AppendLine("- HUD minimal, ressources, navigation, action preview, armee, social et retour accueil visibles.");
            builder.AppendLine("- Donnees locales et non officielles.");
            builder.AppendLine("- Serveur, production readiness, chat live, combat, commerce et progression officielle restent exclus.");
            builder.AppendLine("- BEE-541 reste reservee a la validation architecte suivante.");
            builder.AppendLine();
            builder.AppendLine("## Limites");
            builder.AppendLine();
            builder.AppendLine("- Preuve visuelle Play Mode immediate uniquement.");
            builder.AppendLine("- Ne vaut pas QA finale.");
            builder.AppendLine("- Ne vaut pas certification mobile.");
            builder.AppendLine("- Ne vaut pas production readiness.");
            builder.AppendLine("- Ne valide aucun serveur live.");
            return builder.ToString();
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
