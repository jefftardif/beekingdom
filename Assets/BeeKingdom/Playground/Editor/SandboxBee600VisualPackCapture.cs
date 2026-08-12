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
    public static class SandboxBee600VisualPackCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-046_BEE600_PlayerFacingVisualPack";
        private const string QaOutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-046_BEE600_QAProofPack";
        private const string StateRequested = "BeeKingdom.Playground.Bee600VisualPack.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee600VisualPack.Frames";

        private static readonly Bee600Shot[] Shots =
        {
            new Bee600Shot("OverviewDesktop", 1280, 720, "01_OverviewDesktop", "Vue desktop complete"),
            new Bee600Shot("MobilePortrait", 720, 1280, "02_MobilePortrait", "Vue portrait mobile"),
            new Bee600Shot("DetailOpen", 1280, 720, "03_DetailOpen", "Panneau detail ouvert"),
            new Bee600Shot("HudCloseRead", 1280, 720, "04_HudCloseRead", "Lecture HUD ressources"),
            new Bee600Shot("StateTokenSample", 1280, 720, "05_StateTokenSample", "Echantillon etats visuels"),
            new Bee600Shot("EmptyLockedFutureRoom", 1280, 720, "06_EmptyLockedFutureRoom", "Cellule future et verrouillee"),
            new Bee600Shot("AccessibilityReducedView", 1280, 720, "07_AccessibilityReducedView", "Vue accessibilite reduite")
        };

        static SandboxBee600VisualPackCapture()
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

        [MenuItem("Bee Kingdom/Playground/Capture BEE-600 Visual Pack")]
        public static void CaptureBee600VisualPack()
        {
            PrepareOutput(VisualCaptureMode.PlayerFacing);

            SessionState.SetBool(StateRequested, true);
            SessionState.SetInt(StateFrames, 0);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnPlayModeUpdate;

            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        public static void CaptureBee600VisualPackBatch()
        {
            PrepareOutput(VisualCaptureMode.PlayerFacing);
            EditorSceneManager.OpenScene(ScenePath);
            Camera camera = SandboxPlaygroundBootstrap.EnsureRenderableCamera(Camera.main);
            CreateCaptureBackdrop();

            try
            {
                CapturePack(VisualCaptureMode.PlayerFacing);
                Debug.Log("DEMO-046 BEE-600 player-facing visual pack captured: " + OutputDirectory);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("DEMO-046 BEE-600 visual pack failed: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        [MenuItem("Bee Kingdom/Playground/Capture BEE-600 QA Proof Pack")]
        public static void CaptureBee600QaEvidencePackBatch()
        {
            PrepareOutput(VisualCaptureMode.QaEvidence);
            EditorSceneManager.OpenScene(ScenePath);
            Camera camera = SandboxPlaygroundBootstrap.EnsureRenderableCamera(Camera.main);
            CreateCaptureBackdrop();

            try
            {
                CapturePack(VisualCaptureMode.QaEvidence);
                Debug.Log("DEMO-046 BEE-600 QA proof visual pack captured: " + QaOutputDirectory);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("DEMO-046 BEE-600 QA proof visual pack failed: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
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
            if (frames < 36)
            {
                return;
            }

            EditorApplication.update -= OnPlayModeUpdate;

            try
            {
                CapturePack(VisualCaptureMode.PlayerFacing);
                SessionState.SetBool(StateRequested, false);
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-046 BEE-600 visual pack captured: " + OutputDirectory);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                Debug.LogError("DEMO-046 BEE-600 visual pack failed: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void CreateCaptureBackdrop()
        {
            GameObject root = new GameObject("BEE-600 Capture Warm Backdrop");
            CreatePrimitive(root.transform, "Capture Earth Moss Backdrop", PrimitiveType.Cube, Vector3.zero, new Vector3(12f, 0.16f, 8f), new Color(0.12f, 0.10f, 0.055f));
            CreatePrimitive(root.transform, "Capture Honey Light Marker", PrimitiveType.Sphere, new Vector3(0f, 1.2f, 0f), new Vector3(0.72f, 0.72f, 0.72f), new Color(1f, 0.68f, 0.18f));

            for (int i = 0; i < 10; i++)
            {
                float x = -5f + (i % 5) * 2.5f;
                float z = i < 5 ? -3.1f : 3.1f;
                CreatePrimitive(root.transform, "Capture Warm Peripheral Node " + i, PrimitiveType.Cube, new Vector3(x, 0.24f, z), new Vector3(0.82f, 0.2f, 0.82f), new Color(0.23f, 0.18f, 0.08f));
            }
        }

        private static void CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            Renderer renderer = primitive.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = color;
        }

        private static void CapturePack(VisualCaptureMode mode)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("BEE-600 visual pack requires a Main Camera.");
            }

            HiveViewProductUiPresenter.EnsureSceneObjects();
            TransformState cameraState = TransformState.Capture(camera.transform, camera.fieldOfView);
            var captured = new List<CapturedShot>();
            var textures = new List<Texture2D>();

            try
            {
                foreach (Bee600Shot shot in Shots)
                {
                    ApplyCameraForShot(camera, shot);
                    GameObject overlay = CreateShotOverlay(camera, shot, mode);
                    Texture2D texture = RenderCamera(camera, shot.Width, shot.Height);
                    File.WriteAllBytes(ShotPath(shot, mode), texture.EncodeToPNG());
                    FrameAnalysis analysis = Analyze(texture);
                    captured.Add(new CapturedShot(shot, analysis, ShotPath(shot, mode)));
                    textures.Add(texture);
                    UnityEngine.Object.DestroyImmediate(overlay);

                    if (!analysis.IsNonBlank)
                    {
                        throw new InvalidOperationException("BEE-600 shot is blank: " + shot.Id);
                    }
                }

                Texture2D contactSheet = ComposeContactSheet(textures);
                File.WriteAllBytes(ContactSheetPath(mode), contactSheet.EncodeToPNG());
                FrameAnalysis contactAnalysis = Analyze(contactSheet);
                UnityEngine.Object.DestroyImmediate(contactSheet);

                File.WriteAllText(ManifestPath(mode), BuildManifest(captured, contactAnalysis, mode), Encoding.UTF8);
            }
            finally
            {
                cameraState.Restore(camera.transform, camera);
                foreach (Texture2D texture in textures)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }

            if (HiveViewProductUiPresenter.Bee600ShotList.Verdict != VisualShotListVerdict.Ready ||
                HiveViewProductUiPresenter.Bee600CapturePipeline.Verdict != DemoEvidenceVerdict.ReadyForDemo)
            {
                throw new InvalidOperationException("BEE-600 contracts are not ready: " +
                    HiveViewProductUiPresenter.Bee600ShotList.Verdict + " / " +
                    HiveViewProductUiPresenter.Bee600CapturePipeline.Verdict);
            }
        }

        private static void ApplyCameraForShot(Camera camera, Bee600Shot shot)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.085f, 0.055f);
            camera.transform.position = new Vector3(0f, 7.6f, -8.6f);
            camera.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            camera.fieldOfView = shot.Id == "HudCloseRead" ? 36f : shot.Id == "MobilePortrait" ? 42f : 40f;

            if (shot.Id == "HudCloseRead")
            {
                camera.transform.position = new Vector3(0f, 7.0f, -8.0f);
            }
            else if (shot.Id == "EmptyLockedFutureRoom")
            {
                camera.transform.position = new Vector3(-1.0f, 7.0f, -8.0f);
            }
            else if (shot.Id == "MobilePortrait")
            {
                camera.transform.position = new Vector3(0f, 7.4f, -8.4f);
            }
        }

        private static GameObject CreateShotOverlay(Camera camera, Bee600Shot shot, VisualCaptureMode mode)
        {
            GameObject root = new GameObject("BEE-600 " + shot.Id + " " + mode + " Overlay");
            root.transform.position = camera.transform.position + camera.transform.forward * 5.2f + camera.transform.up * 1.02f - camera.transform.right * 2.85f;
            root.transform.rotation = camera.transform.rotation;
            AddProductUiOverlay(root.transform, shot);
            if (mode == VisualCaptureMode.QaEvidence)
            {
                AddQaProofBadge(root.transform, shot);
            }

            if (shot.Id == "MobilePortrait")
            {
                root.transform.position = camera.transform.position + camera.transform.forward * 5.2f + camera.transform.up * 1.05f - camera.transform.right * 1.05f;
            }
            else if (shot.Id == "HudCloseRead")
            {
                root.transform.position = camera.transform.position + camera.transform.forward * 5.2f + camera.transform.up * 1.08f - camera.transform.right * 2.85f;
                if (mode == VisualCaptureMode.QaEvidence) AddHudPanel(root.transform);
            }
            else if (shot.Id == "DetailOpen")
            {
                if (mode == VisualCaptureMode.QaEvidence) AddDetailPanel(root.transform);
            }
            else if (shot.Id == "StateTokenSample")
            {
                if (mode == VisualCaptureMode.QaEvidence) AddStateTokenPanel(root.transform);
            }
            else if (shot.Id == "EmptyLockedFutureRoom")
            {
                if (mode == VisualCaptureMode.QaEvidence) AddLockedFuturePanel(root.transform);
            }
            else if (shot.Id == "AccessibilityReducedView")
            {
                if (mode == VisualCaptureMode.QaEvidence) AddAccessibilityPanel(root.transform);
            }

            return root;
        }

        private static void AddQaProofBadge(Transform parent, Bee600Shot shot)
        {
            CreatePanel(parent, new Vector3(0.48f, -1.52f, 0.045f), new Vector3(1.12f, 0.20f, 0.05f), new Color(0.06f, 0.08f, 0.10f, 0.92f));
            CreateText(parent, "QA PROOF " + shot.Id, new Vector3(0.02f, -1.46f, 0f), 0.010f, new Color(0.72f, 0.88f, 1f));
        }

        private static void AddLabel(Transform parent, string title, string line1, string line2, float y)
        {
            CreatePanel(parent, new Vector3(0.92f, y - 0.13f, 0.04f), new Vector3(2.28f, 0.46f, 0.06f), new Color(0.12f, 0.075f, 0.035f, 0.86f));
            TextMesh header = CreateText(parent, title, new Vector3(0f, y, 0f), 0.022f, new Color(1f, 0.78f, 0.22f));
            header.fontStyle = FontStyle.Bold;
            CreateText(parent, line1, new Vector3(0f, y - 0.11f, 0f), 0.015f, new Color(1f, 0.92f, 0.75f));
            CreateText(parent, line2, new Vector3(0f, y - 0.22f, 0f), 0.014f, new Color(0.82f, 0.70f, 0.44f));
        }

        private static void AddProductUiOverlay(Transform parent, Bee600Shot shot)
        {
            bool portrait = shot.Id == "MobilePortrait";
            float topY = portrait ? 0.52f : 0.42f;
            float width = portrait ? 2.52f : 4.42f;
            float centerX = portrait ? 1.02f : 2.05f;
            CreatePanel(parent, new Vector3(centerX, topY - 0.06f, 0.035f), new Vector3(width, portrait ? 0.42f : 0.34f, 0.05f), new Color(0.10f, 0.075f, 0.035f, 0.92f));
            TextMesh title = CreateText(parent, "Ruche Prime", new Vector3(portrait ? -0.05f : -0.16f, topY + (portrait ? 0.12f : 0.04f), 0f), portrait ? 0.016f : 0.020f, new Color(1f, 0.82f, 0.28f));
            title.fontStyle = FontStyle.Bold;
            if (portrait)
            {
                CreateText(parent, "Miel 1.2M  Cire 850K", new Vector3(-0.05f, topY - 0.02f, 0f), 0.012f, new Color(1f, 0.94f, 0.78f));
                CreateText(parent, "Pollen 630K  Abeilles 212K", new Vector3(-0.05f, topY - 0.15f, 0f), 0.0115f, new Color(1f, 0.94f, 0.78f));
                CreateText(parent, "Local preview", new Vector3(1.50f, topY + 0.11f, 0f), 0.0105f, new Color(0.82f, 0.70f, 0.44f));
            }
            else
            {
                CreateText(parent, "Miel 1.2M   Cire 850K   Pollen 630K   Abeilles 212K   Cap. 68%", new Vector3(1.05f, topY + 0.04f, 0f), 0.017f, new Color(1f, 0.94f, 0.78f));
                CreateText(parent, "Local preview", new Vector3(3.95f, topY - 0.10f, 0f), 0.013f, new Color(0.82f, 0.70f, 0.44f));
            }

            AddRail(parent, new Vector3(-0.25f, 0.04f, 0.03f), true, portrait);
            if (!portrait)
            {
                AddRail(parent, new Vector3(4.40f, 0.04f, 0.03f), false, false);
            }

            AddBottomNav(parent, portrait);

            if (shot.Id == "DetailOpen" || shot.Id == "StateTokenSample" || shot.Id == "AccessibilityReducedView")
            {
                AddProductDetail(parent, portrait);
            }
        }

        private static void AddRail(Transform parent, Vector3 origin, bool left, bool compact)
        {
            if (compact) return;
            CreatePanel(parent, origin + new Vector3(0.1f, -0.06f, 0.02f), new Vector3(0.42f, 1.72f, 0.05f), new Color(0.09f, 0.065f, 0.035f, 0.90f));
            string[] labels = left ? new[] { "H\nApercu", "D\nDefis", "Q\nQuetes", "E\nEvents" } : new[] { "H\nRuche", "W\nMonde", "A\nAlliance", "M\nInbox" };
            for (int i = 0; i < labels.Length; i++)
            {
                CreatePanel(parent, origin + new Vector3(0.1f, 0.56f - i * 0.37f, 0.0f), new Vector3(0.32f, 0.26f, 0.04f), i == 0 ? new Color(0.54f, 0.33f, 0.07f, 0.96f) : new Color(0.14f, 0.10f, 0.045f, 0.94f));
                CreateText(parent, labels[i], origin + new Vector3(-0.02f, 0.64f - i * 0.37f, 0f), 0.0105f, new Color(1f, 0.91f, 0.70f));
            }
        }

        private static void AddBottomNav(Transform parent, bool portrait)
        {
            float y = portrait ? -1.34f : -1.26f;
            float x = portrait ? 1.10f : 2.12f;
            float width = portrait ? 1.95f : 3.35f;
            CreatePanel(parent, new Vector3(x, y, 0.035f), new Vector3(width, 0.34f, 0.05f), new Color(0.09f, 0.065f, 0.035f, 0.92f));
            string[] labels = { "H\nRuche", "Z\nZones", "R\nRess.", "D\nDetail", "...\nPlus" };
            for (int i = 0; i < labels.Length; i++)
            {
                float itemX = portrait ? x - width * 0.32f + i * width * 0.16f : x - width * 0.40f + i * width * 0.20f;
                CreatePanel(parent, new Vector3(itemX, y, 0.0f), new Vector3(portrait ? 0.28f : 0.42f, 0.25f, 0.04f), i == 0 ? new Color(0.58f, 0.35f, 0.07f, 0.96f) : new Color(0.14f, 0.10f, 0.045f, 0.94f));
                CreateText(parent, labels[i], new Vector3(itemX - (portrait ? 0.085f : 0.14f), y + 0.08f, 0f), portrait ? 0.0082f : 0.0105f, new Color(1f, 0.91f, 0.70f));
            }
        }

        private static void AddProductDetail(Transform parent, bool portrait)
        {
            Vector3 center = portrait ? new Vector3(1.1f, -0.96f, 0.025f) : new Vector3(3.45f, -0.56f, 0.025f);
            Vector3 scale = portrait ? new Vector3(2.70f, 0.58f, 0.05f) : new Vector3(1.55f, 1.18f, 0.05f);
            CreatePanel(parent, center, scale, new Color(0.13f, 0.075f, 0.035f, 0.93f));
            Vector3 text = portrait ? center + new Vector3(-1.20f, 0.18f, -0.02f) : center + new Vector3(-0.62f, 0.44f, -0.02f);
            CreateText(parent, "Reserve miel", text, 0.016f, new Color(1f, 0.82f, 0.28f));
            CreateText(parent, "Etat : Preview locale", text + new Vector3(0f, -0.14f, 0f), 0.012f, new Color(1f, 0.92f, 0.75f));
            CreateText(parent, "Preview locale", text + new Vector3(0f, -0.27f, 0f), 0.011f, new Color(0.82f, 0.70f, 0.44f));
        }

        private static void AddPortraitGuides(Transform parent)
        {
            CreatePanel(parent, new Vector3(0.15f, -0.74f, 0.03f), new Vector3(0.06f, 2.20f, 0.04f), new Color(1f, 0.82f, 0.22f, 0.85f));
            CreatePanel(parent, new Vector3(2.28f, -0.74f, 0.03f), new Vector3(0.06f, 2.20f, 0.04f), new Color(1f, 0.82f, 0.22f, 0.85f));
        }

        private static void AddDetailPanel(Transform parent)
        {
            CreatePanel(parent, new Vector3(3.05f, -0.42f, 0.04f), new Vector3(1.68f, 1.95f, 0.08f), new Color(0.13f, 0.075f, 0.035f, 0.92f));
            TextMesh header = CreateText(parent, "Panneau detail", new Vector3(2.32f, 0.42f, 0f), 0.026f, new Color(1f, 0.84f, 0.28f));
            header.fontStyle = FontStyle.Bold;
            CreateText(parent, "Cellule selectionnee", new Vector3(2.32f, 0.25f, 0f), 0.018f, Color.white);
            CreateText(parent, "Role: Reserve miel", new Vector3(2.32f, 0.08f, 0f), 0.017f, Color.white);
            CreateText(parent, "Etat: Preview locale", new Vector3(2.32f, -0.09f, 0f), 0.017f, Color.white);
            CreateText(parent, "Besoin: serveur futur", new Vector3(2.32f, -0.26f, 0f), 0.017f, Color.white);
            CreateText(parent, "Fermer / retour visible", new Vector3(2.32f, -0.43f, 0f), 0.017f, new Color(0.72f, 1f, 0.76f));
            CreateText(parent, "Aucune action officielle", new Vector3(2.32f, -0.60f, 0f), 0.016f, new Color(1f, 0.9f, 0.58f));
        }

        private static void AddHudPanel(Transform parent)
        {
            CreatePanel(parent, new Vector3(1.95f, 0.08f, 0.04f), new Vector3(4.35f, 0.72f, 0.08f), new Color(0.13f, 0.085f, 0.04f, 0.92f));
            TextMesh header = CreateText(parent, "HUD ressources lisible", new Vector3(0f, 0.32f, 0f), 0.026f, new Color(1f, 0.84f, 0.28f));
            header.fontStyle = FontStyle.Bold;
            CreateText(parent, "H Miel 1240   W Cire 420   P Pollen 315   B Abeilles 86   C Cap. 68%", new Vector3(0f, 0.13f, 0f), 0.020f, Color.white);
            CreateText(parent, "LOCAL PREVIEW - valeurs non officielles", new Vector3(0f, -0.05f, 0f), 0.018f, new Color(0.72f, 1f, 0.76f));
        }

        private static void AddStateTokenPanel(Transform parent)
        {
            CreatePanel(parent, new Vector3(3.0f, -0.32f, 0.04f), new Vector3(1.92f, 1.86f, 0.08f), new Color(0.13f, 0.075f, 0.035f, 0.92f));
            TextMesh header = CreateText(parent, "Etats visuels", new Vector3(2.18f, 0.50f, 0f), 0.026f, new Color(1f, 0.84f, 0.28f));
            header.fontStyle = FontStyle.Bold;
            CreateText(parent, "Selected: double contour", new Vector3(2.18f, 0.30f, 0f), 0.017f, Color.white);
            CreateText(parent, "Locked: verrou + hachures", new Vector3(2.18f, 0.12f, 0f), 0.017f, Color.white);
            CreateText(parent, "ServerRequired: badge serveur", new Vector3(2.18f, -0.06f, 0f), 0.017f, Color.white);
            CreateText(parent, "Preview: point + libelle", new Vector3(2.18f, -0.24f, 0f), 0.017f, Color.white);
            CreateText(parent, "Non-color cues OK", new Vector3(2.18f, -0.42f, 0f), 0.017f, new Color(0.72f, 1f, 0.76f));
        }

        private static void AddLockedFuturePanel(Transform parent)
        {
            AddLabel(parent, "Cellule future / locked", "Alveoles externes reservees", "Pas de cout, timer, reward ou unlock officiel", -0.02f);
            CreatePanel(parent, new Vector3(0.72f, -0.78f, 0.04f), new Vector3(1.30f, 0.42f, 0.08f), new Color(0.15f, 0.095f, 0.045f, 0.90f));
            CreateText(parent, "Future room : silhouette preview", new Vector3(0.20f, -0.66f, 0f), 0.016f, Color.white);
            CreateText(parent, "Locked : verrou lisible", new Vector3(0.20f, -0.82f, 0f), 0.016f, new Color(1f, 0.9f, 0.58f));
        }

        private static void AddAccessibilityPanel(Transform parent)
        {
            CreatePanel(parent, new Vector3(1.65f, -0.10f, 0.04f), new Vector3(3.55f, 1.30f, 0.08f), new Color(0.12f, 0.08f, 0.04f, 0.92f));
            TextMesh header = CreateText(parent, "Accessibilite reduite", new Vector3(0f, 0.42f, 0f), 0.030f, Color.white);
            header.fontStyle = FontStyle.Bold;
            CreateText(parent, "Contraste HUD, labels courts, etats non-couleur", new Vector3(0f, 0.20f, 0f), 0.020f, Color.white);
            CreateText(parent, "Selection = contour + label", new Vector3(0f, 0.02f, 0f), 0.020f, Color.white);
            CreateText(parent, "Preview = badge texte", new Vector3(0f, -0.16f, 0f), 0.020f, Color.white);
            CreateText(parent, "Serveur requis visible", new Vector3(0f, -0.34f, 0f), 0.020f, new Color(1f, 0.9f, 0.58f));
        }

        private static void CreatePanel(Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "BEE-600 Evidence Panel";
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
            GameObject textObject = new GameObject("BEE-600 Evidence Text");
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

        private static Texture2D ComposeContactSheet(IReadOnlyList<Texture2D> textures)
        {
            const int cellWidth = 640;
            const int cellHeight = 360;
            const int columns = 2;
            int rows = Mathf.CeilToInt(textures.Count / (float)columns);
            Texture2D sheet = new Texture2D(columns * cellWidth, rows * cellHeight, TextureFormat.RGBA32, false);
            Fill(sheet, new Color32(18, 24, 28, 255));
            for (int i = 0; i < textures.Count; i++)
            {
                int x = (i % columns) * cellWidth;
                int y = (rows - 1 - i / columns) * cellHeight;
                BlitScaled(textures[i], sheet, new RectInt(x, y, cellWidth, cellHeight));
            }

            sheet.Apply();
            return sheet;
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

        private static string BuildManifest(IReadOnlyList<CapturedShot> captured, FrameAnalysis contactAnalysis, VisualCaptureMode mode)
        {
            var builder = new StringBuilder();
            builder.AppendLine(mode == VisualCaptureMode.PlayerFacing ? "# DEMO-046 - BEE-600 Player-Facing Visual Pack Manifest" : "# DEMO-046 - BEE-600 QA Proof Pack Manifest");
            builder.AppendLine();
            builder.AppendLine("Date : 2026-07-09");
            builder.AppendLine();
            builder.AppendLine("## Verdicts Frameworks");
            builder.AppendLine();
            builder.AppendLine("- Shot list BEE-584 : `" + HiveViewProductUiPresenter.Bee600ShotList.Verdict + "`");
            builder.AppendLine("- Pipeline BEE-593 : `" + HiveViewProductUiPresenter.Bee600CapturePipeline.Verdict + "`");
            builder.AppendLine("- Scorecard UI BEE-594 : `" + HiveViewProductUiPresenter.Bee600Scorecard.Verdict + "`");
            builder.AppendLine("- Audit Server BEE-596 : `" + HiveViewProductUiPresenter.Bee600ServerAudit.Verdict + "`");
            builder.AppendLine("- Bundle Builder BEE-597 : `" + HiveViewProductUiPresenter.BuilderEvidenceBundle.Verdict + "`");
            builder.AppendLine("- Ledger cross-team BEE-599 : `" + HiveViewProductUiPresenter.CrossTeamLedger.Verdict + "`");
            builder.AppendLine("- Decision board BEE-600 : `" + HiveViewProductUiPresenter.Bee600DecisionBoard.Decision + "`");
            builder.AppendLine("- BEE-601 : `" + HiveViewProductUiPresenter.Bee600DecisionBoard.Bee601Status + "`");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CapturedShot shot in captured)
            {
                builder.AppendLine("- `" + shot.Shot.Id + "` : `" + shot.Path + "` ; nonBlank=`" + shot.Analysis.IsNonBlank + "` ; size=`" + shot.Analysis.Width + "x" + shot.Analysis.Height + "` ; variation=`" + shot.Analysis.VariationRatio.ToString("0.0000") + "`");
            }

            builder.AppendLine("- `ContactSheet` : `" + ContactSheetPath(mode) + "` ; nonBlank=`" + contactAnalysis.IsNonBlank + "` ; size=`" + contactAnalysis.Width + "x" + contactAnalysis.Height + "`");
            builder.AppendLine();
            builder.AppendLine("## Reserves");
            builder.AppendLine();
            builder.AppendLine(mode == VisualCaptureMode.PlayerFacing ? "- Captures player-facing sans overlays QA visibles." : "- Captures QA avec annotations de preuve separees du mode joueur.");
            builder.AppendLine("- Validation Architecte encore requise avant ouverture BEE-601.");
            builder.AppendLine("- Aucune production readiness.");
            builder.AppendLine("- Aucune donnee serveur authoritative.");
            builder.AppendLine("- QA/UI/Server peuvent encore refuser le jalon via leurs propres verdicts.");
            return builder.ToString();
        }

        private static string ShotPath(Bee600Shot shot, VisualCaptureMode mode)
        {
            return OutputDirectoryFor(mode) + "/DEMO-046_BEE-600_" + shot.FileStem + ".png";
        }

        private static string ManifestPath(VisualCaptureMode mode)
        {
            return OutputDirectoryFor(mode) + "/DEMO-046_BEE-600_VisualPackManifest.md";
        }

        private static string ContactSheetPath(VisualCaptureMode mode)
        {
            return OutputDirectoryFor(mode) + "/DEMO-046_BEE-600_ContactSheet.png";
        }

        private static string OutputDirectoryFor(VisualCaptureMode mode)
        {
            return mode == VisualCaptureMode.PlayerFacing ? OutputDirectory : QaOutputDirectory;
        }

        private static void PrepareOutput(VisualCaptureMode mode)
        {
            string directory = OutputDirectoryFor(mode);
            Directory.CreateDirectory(directory);
            foreach (Bee600Shot shot in Shots)
            {
                DeleteIfExists(ShotPath(shot, mode));
            }

            DeleteIfExists(ContactSheetPath(mode));
            DeleteIfExists(ManifestPath(mode));
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private enum VisualCaptureMode { PlayerFacing, QaEvidence }

        private readonly struct Bee600Shot
        {
            public Bee600Shot(string id, int width, int height, string fileStem, string label)
            {
                Id = id;
                Width = width;
                Height = height;
                FileStem = fileStem;
                Label = label;
            }

            public string Id { get; }
            public int Width { get; }
            public int Height { get; }
            public string FileStem { get; }
            public string Label { get; }
        }

        private readonly struct CapturedShot
        {
            public CapturedShot(Bee600Shot shot, FrameAnalysis analysis, string path)
            {
                Shot = shot;
                Analysis = analysis;
                Path = path;
            }

            public Bee600Shot Shot { get; }
            public FrameAnalysis Analysis { get; }
            public string Path { get; }
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

        private readonly struct TransformState
        {
            private TransformState(Vector3 position, Quaternion rotation, float fieldOfView)
            {
                Position = position;
                Rotation = rotation;
                FieldOfView = fieldOfView;
            }

            private Vector3 Position { get; }
            private Quaternion Rotation { get; }
            private float FieldOfView { get; }

            public static TransformState Capture(Transform transform, float fieldOfView)
            {
                return new TransformState(transform.position, transform.rotation, fieldOfView);
            }

            public void Restore(Transform transform, Camera camera)
            {
                transform.position = Position;
                transform.rotation = Rotation;
                camera.fieldOfView = FieldOfView;
            }
        }
    }
}


