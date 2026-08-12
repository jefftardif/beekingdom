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
    public static class SandboxBee500PlayModeEvidenceCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-042_BEE500_PlayModeEvidence";
        private const string ScreenshotPath = OutputDirectory + "/DEMO-042_BEE-500_PlayMode.png";
        private const string ManifestPath = OutputDirectory + "/DEMO-042_BEE-500_VisualEvidenceManifest.md";
        private const string StateRequested = "BeeKingdom.Playground.Bee500PlayModeEvidence.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee500PlayModeEvidence.Frames";

        static SandboxBee500PlayModeEvidenceCapture()
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

        [MenuItem("Bee Kingdom/Playground/Capture BEE-500 Play Mode Evidence")]
        public static void CaptureBee500PlayModeEvidence()
        {
            Directory.CreateDirectory(OutputDirectory);
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
                SessionState.SetInt(StateFrames, 0);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.update += OnPlayModeUpdate;
            }
        }

        private static void OnPlayModeUpdate()
        {
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);
            if (frames < 12)
            {
                return;
            }

            EditorApplication.update -= OnPlayModeUpdate;

            try
            {
                CaptureEvidence();
                SessionState.SetBool(StateRequested, false);
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-042 BEE-500 Play Mode visual evidence captured: " + ScreenshotPath);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                Debug.LogError("DEMO-042 BEE-500 Play Mode visual evidence capture failed: " + exception);
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
                throw new InvalidOperationException("BEE-500 Play Mode evidence requires a Main Camera.");
            }

            Directory.CreateDirectory(OutputDirectory);
            GameObject overlay = CreateCaptureOverlay(camera);
            Texture2D texture = RenderCamera(camera, 1280, 720);
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(ScreenshotPath, bytes);

            FrameAnalysis analysis = Analyze(texture);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(overlay);

            NonBlankFrameCheck check = analysis.IsNonBlank ? NonBlankFrameCheck.NonBlank : NonBlankFrameCheck.Blank;
            var evidence = new DemoPlayModeEvidenceCapture("demo-042-bee-500-play-mode", new[]
            {
                new VisualEvidenceFrame(
                    "bee-500-sandbox-play-mode",
                    "SandboxPlayground",
                    "BEE-500 immediate visual proof with preview limits",
                    check,
                    new EvidenceLimitNotice("Demo read-only: preuve visuelle immediate BEE-500, pas QA finale, pas production readiness, pas serveur live.", true),
                    EvidencePrivacyStatus.Safe,
                    "Presence visuelle Play Mode du jalon BEE-500",
                    productionClaim: false)
            });

            DemoEvidenceDiagnostics diagnostics = evidence.Evaluate();
            PlayableProductMilestoneGate gate = new PlayableProductMilestoneGate(
                "demo-042-bee-500-playable-product-gate",
                BuildGateRows(),
                new[] { new PlayableSliceReserve("visual-proof-immediate", "Preuve Play Mode immediate produite ; validation QA finale et production readiness restent exclues.") },
                Bee501BlockerStatus.BlockedUntilArchitectValidation);

            File.WriteAllText(ManifestPath, BuildManifest(analysis, diagnostics, gate), Encoding.UTF8);
            if (diagnostics.Verdict != PlayModeCaptureVerdict.EvidenceReady || gate.Verdict != PlayableProductMilestoneVerdict.ReadyWithPreviewReserves)
            {
                throw new InvalidOperationException("BEE-500 visual evidence is not acceptable. Evidence verdict: " + diagnostics.Verdict + ", gate verdict: " + gate.Verdict);
            }
        }

        private static IReadOnlyList<PlayableSliceGateRow> BuildGateRows()
        {
            string[] domains =
            {
                "premiere-minute", "hub", "ruche", "boucle-action", "feedbacks", "progression", "monde", "alliance",
                "communication", "evenements", "commerce", "conflit", "armee", "server-authority", "demo-qa"
            };

            var rows = new List<PlayableSliceGateRow>();
            for (int i = 0; i < domains.Length; i++)
            {
                rows.Add(new PlayableSliceGateRow(
                    domains[i],
                    "DEMO-012 Play Mode visual proof references " + domains[i],
                    "No live claim for " + domains[i],
                    PlayableSliceGateStatus.PreviewReserve));
            }

            return rows;
        }

        private static GameObject CreateCaptureOverlay(Camera camera)
        {
            GameObject root = new GameObject("BEE-500 Visual Proof Overlay");
            root.transform.position = camera.transform.position + camera.transform.forward * 5.2f + camera.transform.up * 1.25f - camera.transform.right * 1.9f;
            root.transform.rotation = camera.transform.rotation;

            TextMesh title = CreateText(root.transform, "BEE-500 Play Mode Proof", Vector3.zero, 0.04f, Color.white);
            title.fontStyle = FontStyle.Bold;
            CreateText(root.transform, "DEMO-012 read-only | BEE-481..500 detectes | BEE-501 bloquee", new Vector3(0f, -0.15f, 0f), 0.027f, new Color(1f, 0.82f, 0.22f));
            CreateText(root.transform, "No live map, server, trade, PvP, reward or production readiness", new Vector3(0f, -0.25f, 0f), 0.025f, new Color(0.7f, 0.95f, 1f));
            return root;
        }

        private static TextMesh CreateText(Transform parent, string text, Vector3 localPosition, float size, Color color)
        {
            GameObject textObject = new GameObject("BEE-500 Evidence Text");
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
                return new FrameAnalysis(false, 0, 0d, 0d);
            }

            Color32 first = pixels[0];
            int different = 0;
            int bright = 0;
            int sampled = 0;
            int step = Math.Max(1, pixels.Length / 5000);
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
            return new FrameAnalysis(nonBlank, sampled, variationRatio, visibleRatio);
        }

        private static string BuildManifest(FrameAnalysis analysis, DemoEvidenceDiagnostics diagnostics, PlayableProductMilestoneGate gate)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-042 - BEE-500 Play Mode Visual Evidence");
            builder.AppendLine();
            builder.AppendLine("Date : 2026-07-09");
            builder.AppendLine();
            builder.AppendLine("## Resultat");
            builder.AppendLine();
            builder.AppendLine("- Capture : `" + ScreenshotPath + "`");
            builder.AppendLine("- Verdict preuve visuelle : `" + diagnostics.Verdict + "`");
            builder.AppendLine("- Verdict gate BEE-500 : `" + gate.Verdict + "`");
            builder.AppendLine("- Statut BEE-501 : `" + gate.Bee501Status + "`");
            builder.AppendLine("- Image non vide : `" + analysis.IsNonBlank + "`");
            builder.AppendLine("- Pixels echantillonnes : `" + analysis.SampledPixels + "`");
            builder.AppendLine("- Ratio variation : `" + analysis.VariationRatio.ToString("0.0000") + "`");
            builder.AppendLine("- Ratio visible : `" + analysis.VisibleRatio.ToString("0.0000") + "`");
            builder.AppendLine();
            builder.AppendLine("## Couverture BEE-500");
            builder.AppendLine();
            builder.AppendLine("- Premiere minute, hub, ruche, boucle action, feedbacks et progression : references dans gate preview.");
            builder.AppendLine("- Monde, alliance, communication, evenements, commerce, conflit et armee : references dans gate preview.");
            builder.AppendLine("- Autorite serveur et Demo/QA : visibles comme limites, sans backend cree.");
            builder.AppendLine("- BEE-501 : bloquee jusqu'a validation architecte.");
            builder.AppendLine();
            builder.AppendLine("## Limites");
            builder.AppendLine();
            builder.AppendLine("- Preuve visuelle Play Mode immediate uniquement.");
            builder.AppendLine("- Ne vaut pas QA finale.");
            builder.AppendLine("- Ne vaut pas production readiness.");
            builder.AppendLine("- Ne valide aucun serveur live.");
            builder.AppendLine("- Ne cree aucune carte Monde live, commerce, PvP, progression officielle ou reward.");
            return builder.ToString();
        }

        private readonly struct FrameAnalysis
        {
            public FrameAnalysis(bool isNonBlank, int sampledPixels, double variationRatio, double visibleRatio)
            {
                IsNonBlank = isNonBlank;
                SampledPixels = sampledPixels;
                VariationRatio = variationRatio;
                VisibleRatio = visibleRatio;
            }

            public bool IsNonBlank { get; }
            public int SampledPixels { get; }
            public double VariationRatio { get; }
            public double VisibleRatio { get; }
        }
    }
}
