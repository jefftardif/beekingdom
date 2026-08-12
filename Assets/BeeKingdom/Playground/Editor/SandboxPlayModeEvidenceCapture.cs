using System;
using System.IO;
using System.Text;
using BeeKingdom.Colony;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxPlayModeEvidenceCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-031_PlayModeEvidence";
        private const string ScreenshotPath = OutputDirectory + "/DEMO-031_BEE-429_PlayMode.png";
        private const string ManifestPath = OutputDirectory + "/DEMO-031_BEE-429_VisualEvidenceManifest.md";
        private const string StateRequested = "BeeKingdom.Playground.PlayModeEvidence.Requested";
        private const string StateFrames = "BeeKingdom.Playground.PlayModeEvidence.Frames";

        static SandboxPlayModeEvidenceCapture()
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

        [MenuItem("Bee Kingdom/Playground/Capture Sandbox Play Mode Evidence")]
        public static void CaptureSandboxPlayModeEvidence()
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
                Debug.Log("DEMO-031 Play Mode visual evidence captured: " + ScreenshotPath);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                Debug.LogError("DEMO-031 Play Mode visual evidence capture failed: " + exception);
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
                throw new InvalidOperationException("Sandbox Play Mode evidence requires a Main Camera.");
            }

            Directory.CreateDirectory(OutputDirectory);
            Texture2D texture = RenderCamera(camera, 1280, 720);
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(ScreenshotPath, bytes);

            FrameAnalysis analysis = Analyze(texture);
            UnityEngine.Object.DestroyImmediate(texture);

            NonBlankFrameCheck check = analysis.IsNonBlank ? NonBlankFrameCheck.NonBlank : NonBlankFrameCheck.Blank;
            var evidence = new DemoPlayModeEvidenceCapture("demo-031-bee-429-play-mode", new[]
            {
                new VisualEvidenceFrame(
                    "sandbox-play-mode-hub",
                    "SandboxPlayground",
                    "BEE-429/BEE-430 Play Mode visual reserve closure",
                    check,
                    new EvidenceLimitNotice("Demo read-only: preuve visuelle non vide, pas QA finale, pas release, pas serveur live.", true),
                    EvidencePrivacyStatus.Safe,
                    "Presence visuelle Play Mode de DEMO-012")
            });

            DemoEvidenceDiagnostics diagnostics = evidence.Evaluate();
            var gate = new PlayerOnboardingDemoEvidenceClosureGate(
                "demo-031-bee-430-closure",
                new[]
                {
                    Matrix("BEE-422", "Sandbox hub", "Capture Play Mode non vide", "Repere visible et fallback", "Aucun ecran vide", "Aucun serveur requis"),
                    Matrix("BEE-423", "Onboarding", "Surface listee dans diagnostic", "Parcours preview", "Sortie home visible", "Progression future serveur"),
                    Matrix("BEE-424", "Profil ruche", "Surface listee dans diagnostic", "Profil exemple", "Pas de donnees personnelles", "Identite future serveur"),
                    Matrix("BEE-425", "Styles", "Surface listee dans diagnostic", "Comparaison preview", "Pas de bonus officiel", "Choix futur serveur"),
                    Matrix("BEE-426", "Objectifs", "Surface listee dans diagnostic", "Pile compacte", "Pas de reward", "Completion future serveur"),
                    Matrix("BEE-427", "Allies", "Surface listee dans diagnostic", "Invitation bloquee", "Pas de joueur reel", "Social graph futur serveur"),
                    Matrix("BEE-428", "Intentions", "Surface listee dans diagnostic", "Intentions read-only", "Pas d'effet runtime", "Economie et monde futurs serveur"),
                    Matrix("BEE-429", "Evidence", "PNG et manifeste presents", "Limite visible", "Preuve non vide", "Pas de telemetry production"),
                    Matrix("BEE-430", "Closure gate", "Reserve Play Mode fermee", "BEE-431 bloquee", "Pas de release claim", "Validation architecte requise")
                },
                new PlayerSurfaceLimitAudit(productionClaim: false, visualRegressionRisk: !analysis.IsNonBlank),
                new ServerDependencyVisibilityAudit(visible: true),
                new Bee431BlockerStatus(false, Bee431BlockerStatusMessage()));

            OnboardingClosureDiagnostics closure = gate.Evaluate();
            File.WriteAllText(ManifestPath, BuildManifest(analysis, diagnostics, closure), Encoding.UTF8);
            if (diagnostics.Verdict != PlayModeCaptureVerdict.EvidenceReady || closure.Verdict != DemoEvidenceReadinessVerdict.ReadyForArchitectValidation)
            {
                throw new InvalidOperationException("Play Mode evidence reserve is not closed. Evidence verdict: " + diagnostics.Verdict + ", closure verdict: " + closure.Verdict);
            }
        }

        private static OnboardingLotCoverageMatrix Matrix(string beeId, string surface, string evidence, string uiNeed, string qaCheck, string serverBoundary)
        {
            return new OnboardingLotCoverageMatrix(beeId, surface, evidence, uiNeed, qaCheck, serverBoundary, DemoEvidenceReadinessVerdict.ReadyForArchitectValidation);
        }

        private static string Bee431BlockerStatusMessage()
        {
            return "BEE-431 reste bloquee jusqu'a validation architecte.";
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
                return new FrameAnalysis(false, 0, 0, 0, 0);
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
            return new FrameAnalysis(nonBlank, sampled, different, variationRatio, visibleRatio);
        }

        private static string BuildManifest(FrameAnalysis analysis, DemoEvidenceDiagnostics diagnostics, OnboardingClosureDiagnostics closure)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-031 - BEE-429/BEE-430 Play Mode Visual Evidence");
            builder.AppendLine();
            builder.AppendLine("Date : 2026-07-09");
            builder.AppendLine();
            builder.AppendLine("## Resultat");
            builder.AppendLine();
            builder.AppendLine("- Capture : `" + ScreenshotPath + "`");
            builder.AppendLine("- Verdict BEE-429 : `" + diagnostics.Verdict + "`");
            builder.AppendLine("- Verdict BEE-430 : `" + closure.Verdict + "`");
            builder.AppendLine("- Image non vide : `" + analysis.IsNonBlank + "`");
            builder.AppendLine("- Pixels echantillonnes : `" + analysis.SampledPixels + "`");
            builder.AppendLine("- Ratio variation : `" + analysis.VariationRatio.ToString("0.0000") + "`");
            builder.AppendLine("- Ratio visible : `" + analysis.VisibleRatio.ToString("0.0000") + "`");
            builder.AppendLine();
            builder.AppendLine("## Limites");
            builder.AppendLine();
            builder.AppendLine("- Preuve visuelle Play Mode uniquement.");
            builder.AppendLine("- Ne vaut pas QA finale.");
            builder.AppendLine("- Ne vaut pas UI production.");
            builder.AppendLine("- Ne vaut pas release.");
            builder.AppendLine("- Ne valide aucun serveur live.");
            builder.AppendLine("- BEE-431 reste bloquee jusqu'a validation architecte.");
            return builder.ToString();
        }

        private readonly struct FrameAnalysis
        {
            public FrameAnalysis(bool isNonBlank, int sampledPixels, int differentPixels, double variationRatio, double visibleRatio)
            {
                IsNonBlank = isNonBlank;
                SampledPixels = sampledPixels;
                DifferentPixels = differentPixels;
                VariationRatio = variationRatio;
                VisibleRatio = visibleRatio;
            }

            public bool IsNonBlank { get; }
            public int SampledPixels { get; }
            public int DifferentPixels { get; }
            public double VariationRatio { get; }
            public double VisibleRatio { get; }
        }
    }
}
