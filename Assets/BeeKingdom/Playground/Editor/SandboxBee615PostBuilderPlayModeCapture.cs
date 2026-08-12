using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeeKingdom.Playground.Editor
{
    [InitializeOnLoad]
    public static class SandboxBee615PostBuilderPlayModeCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string PlayerDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-051_BEE601_620_BEE615_PlayerCleanPack";
        private const string QaDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-051_BEE601_620_BEE615_QARuntimeProofPack";
        private const string StateRequested = "BeeKingdom.Playground.Bee615PostBuilder.Requested";
        private const string StatePhase = "BeeKingdom.Playground.Bee615PostBuilder.Phase";
        private const string StateIndex = "BeeKingdom.Playground.Bee615PostBuilder.Index";
        private const string StateFrames = "BeeKingdom.Playground.Bee615PostBuilder.Frames";
        private const string StateCapturePending = "BeeKingdom.Playground.Bee615PostBuilder.CapturePending";

        private static readonly Bee615Shot[] Shots =
        {
            new Bee615Shot("premium_overview", 1280, 720, "01_premium_overview", Bee615ShotKind.Overview),
            new Bee615Shot("icon_sheet_50", 1280, 720, "02_icon_sheet_50", Bee615ShotKind.IconSheet),
            new Bee615Shot("zone_landmarks", 1280, 720, "03_zone_landmarks", Bee615ShotKind.ZoneLandmarks),
            new Bee615Shot("hud_zoom", 1280, 720, "04_hud_zoom", Bee615ShotKind.Hud),
            new Bee615Shot("panel_open", 1280, 720, "05_panel_open", Bee615ShotKind.Panel),
            new Bee615Shot("state_tokens", 1280, 720, "06_state_tokens", Bee615ShotKind.States),
            new Bee615Shot("responsive_matrix", 1280, 720, "07_responsive_matrix", Bee615ShotKind.Responsive),
            new Bee615Shot("phone_portrait", 720, 1280, "08_phone_portrait", Bee615ShotKind.Portrait),
            new Bee615Shot("non_claim_badges", 1280, 720, "09_non_claim_badges", Bee615ShotKind.NonClaims),
            new Bee615Shot("fallback_manifest", 1280, 720, "10_fallback_manifest", Bee615ShotKind.Fallback)
        };

        static SandboxBee615PostBuilderPlayModeCapture()
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

        [MenuItem("Bee Kingdom/Playground/Capture BEE-615 Post-Builder Play Mode Packs")]
        public static void CaptureBee615PostBuilderPlayModePacks()
        {
            PrepareDirectory(PlayerDirectory);
            PrepareDirectory(QaDirectory);
            SessionState.SetBool(StateRequested, true);
            SessionState.SetString(StatePhase, "boot");
            SessionState.SetInt(StateIndex, 0);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCapturePending, false);

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
                SessionState.SetString(StatePhase, "player");
                SessionState.SetInt(StateIndex, 0);
                SessionState.SetInt(StateFrames, 0);
                SessionState.SetBool(StateCapturePending, false);
                EditorApplication.update -= OnPlayModeUpdate;
                EditorApplication.update += OnPlayModeUpdate;
            }
        }

        private static void OnPlayModeUpdate()
        {
            if (!SessionState.GetBool(StateRequested, false))
            {
                return;
            }

            try
            {
                string phase = SessionState.GetString(StatePhase, "player");
                if (phase == "player")
                {
                    CapturePlayerGameViewStep();
                    return;
                }

                if (phase == "qa")
                {
                    int frames = SessionState.GetInt(StateFrames, 0) + 1;
                    SessionState.SetInt(StateFrames, frames);
                    if (frames < 24)
                    {
                        return;
                    }

                    CaptureQaPack();
                    Finish();
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                EditorApplication.update -= OnPlayModeUpdate;
                Debug.LogError("DEMO-051 BEE-615 post-Builder Play Mode capture failed: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void CapturePlayerGameViewStep()
        {
            int index = SessionState.GetInt(StateIndex, 0);
            if (index >= Shots.Length)
            {
                ComposePack(PlayerDirectory, "BEE-601_620_BEE-615_PlayerClean_ContactSheet.png");
                File.WriteAllText(PlayerDirectory + "/BEE-601_620_BEE-615_PlayerClean_Manifest.md", BuildPlayerManifest(), Encoding.UTF8);
                SessionState.SetString(StatePhase, "qa");
                SessionState.SetInt(StateFrames, 0);
                SessionState.SetBool(StateCapturePending, false);
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("Main Camera missing for player clean Game View capture.");
            }

            ValidateRuntimePremiumHive();
            Bee615Shot shot = Shots[index];
            int frames = SessionState.GetInt(StateFrames, 0) + 1;
            SessionState.SetInt(StateFrames, frames);

            if (frames == 1)
            {
                ApplyCamera(camera, shot);
                Screen.SetResolution(shot.Width, shot.Height, false);
                return;
            }

            if (frames < 12)
            {
                return;
            }

            string path = ShotPath(PlayerDirectory, shot);
            if (!SessionState.GetBool(StateCapturePending, false))
            {
                DeleteIfExists(path);
                SandboxGameViewScreenshotWriter.Request(path);
                SessionState.SetBool(StateCapturePending, true);
                return;
            }

            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                if (frames < 90)
                {
                    return;
                }

                throw new InvalidOperationException("Player clean Game View screenshot was not written: " + path);
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            byte[] bytes = File.ReadAllBytes(path);
            texture.LoadImage(bytes);
            FrameAnalysis analysis = Analyze(texture);
            UnityEngine.Object.DestroyImmediate(texture);
            if (!analysis.IsNonBlank)
            {
                throw new InvalidOperationException("Player clean Game View shot is blank: " + shot.Id);
            }

            SessionState.SetInt(StateIndex, index + 1);
            SessionState.SetInt(StateFrames, 0);
            SessionState.SetBool(StateCapturePending, false);
        }

        private static void CaptureQaPack()
        {
            RuntimeEvidence runtime = ValidateRuntimePremiumHive();
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("Main Camera missing for QA runtime capture.");
            }

            var captured = new List<CapturedShot>();
            var textures = new List<Texture2D>();
            for (int i = 0; i < Shots.Length; i++)
            {
                Bee615Shot shot = Shots[i];
                ApplyCamera(camera, shot);
                GameObject overlay = CreateQaOverlay(camera, shot, runtime);
                Texture2D texture = RenderCamera(camera, shot.Width, shot.Height);
                string path = ShotPath(QaDirectory, shot);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                FrameAnalysis analysis = Analyze(texture);
                if (!analysis.IsNonBlank)
                {
                    throw new InvalidOperationException("QA runtime shot is blank: " + shot.Id);
                }

                captured.Add(new CapturedShot(shot, path, analysis));
                textures.Add(texture);
                UnityEngine.Object.DestroyImmediate(overlay);
            }

            Texture2D contactSheet = ComposeContactSheet(textures);
            string contactPath = QaDirectory + "/BEE-601_620_BEE-615_QARuntime_ContactSheet.png";
            File.WriteAllBytes(contactPath, contactSheet.EncodeToPNG());
            FrameAnalysis contactAnalysis = Analyze(contactSheet);
            UnityEngine.Object.DestroyImmediate(contactSheet);

            File.WriteAllText(QaDirectory + "/BEE-601_620_BEE-615_QARuntime_Manifest.md", BuildQaManifest(runtime, captured, contactPath, contactAnalysis), Encoding.UTF8);
            foreach (Texture2D texture in textures)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void Finish()
        {
            SessionState.SetBool(StateRequested, false);
            EditorApplication.update -= OnPlayModeUpdate;
            EditorApplication.ExitPlaymode();
            Debug.Log("DEMO-051 BEE-615 post-Builder Play Mode packs captured: " + PlayerDirectory + " | " + QaDirectory);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static RuntimeEvidence ValidateRuntimePremiumHive()
        {
            GameObject root = GameObject.Find(HiveViewProductUiPresenter.RootName);
            Ensure(root != null, "Premium runtime root is absent in normal Play Mode.");
            Ensure(FindObject<SandboxPlaygroundBootstrap>() != null, "SandboxPlaygroundBootstrap is absent in normal Play Mode.");

            int markers = root.GetComponentsInChildren<HiveViewCellMarker>(true).Length;
            int children = root.GetComponentsInChildren<Transform>(true).Length;
            bool waxRim = ContainsChild(root.transform, "Hive Hex Outer Wax Rim");
            bool waxWall = ContainsChild(root.transform, "Hive Hex Left Wax Wall");
            bool honeySpecular = ContainsChild(root.transform, "Hive Zone Landmark Honey Specular");
            bool queenCore = ContainsChild(root.transform, "Hive Queen Core Product Marker");

            Ensure(markers >= 15, "Premium runtime cell markers are missing in Play Mode.");
            Ensure(children > 20, "Premium runtime root does not contain enough visible children.");
            Ensure(waxRim, "Premium wax rims are missing in Play Mode.");
            Ensure(waxWall, "Premium wax walls are missing in Play Mode.");
            Ensure(honeySpecular, "Premium honey highlights are missing in Play Mode.");
            Ensure(queenCore, "Premium queen core marker is missing in Play Mode.");

            return new RuntimeEvidence(markers, children, waxRim, waxWall, honeySpecular, queenCore);
        }

        private static T FindObject<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        private static bool ContainsChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name.StartsWith(name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Ensure(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void ApplyCamera(Camera camera, Bee615Shot shot)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.065f, 0.05f);
            camera.orthographic = false;
            camera.fieldOfView = shot.Kind == Bee615ShotKind.Hud ? 34f : shot.Kind == Bee615ShotKind.Portrait ? 56f : 42f;
            camera.transform.position = new Vector3(0f, 7.9f, -9.4f);
            camera.transform.rotation = Quaternion.Euler(42f, 0f, 0f);

            if (shot.Kind == Bee615ShotKind.Portrait)
            {
                camera.transform.position = new Vector3(0f, 9.4f, -11.8f);
                camera.transform.rotation = Quaternion.Euler(43f, 0f, 0f);
            }
            else if (shot.Kind == Bee615ShotKind.Hud)
            {
                camera.transform.position = new Vector3(0f, 7.2f, -8.1f);
            }
            else if (shot.Kind == Bee615ShotKind.Panel)
            {
                camera.transform.position = new Vector3(1.25f, 7.8f, -8.6f);
            }
        }

        private static GameObject CreateQaOverlay(Camera camera, Bee615Shot shot, RuntimeEvidence runtime)
        {
            GameObject root = new GameObject("BEE-615 QA Runtime Overlay " + shot.Id);
            Transform t = root.transform;
            t.position = camera.transform.position + camera.transform.forward * 5.2f;
            t.rotation = camera.transform.rotation;
            AddPanel(t, new Vector2(-2.20f, 1.46f), new Vector2(2.35f, 0.54f), new Color(0.11f, 0.075f, 0.032f, 0.96f));
            AddText(t, "QA RUNTIME PROOF", new Vector2(-3.28f, 1.62f), 0.042f, new Color(1f, 0.80f, 0.20f));
            AddText(t, shot.Id + " / SandboxPlayground", new Vector2(-3.28f, 1.44f), 0.028f, new Color(0.96f, 0.88f, 0.68f));
            AddPanel(t, new Vector2(2.00f, 0.36f), new Vector2(2.35f, 2.10f), new Color(0.075f, 0.060f, 0.035f, 0.88f));
            AddText(t, "runtime_verified", new Vector2(0.98f, 1.14f), 0.038f, new Color(1f, 0.82f, 0.26f));
            AddText(t, "- root: " + HiveViewProductUiPresenter.RootName, new Vector2(0.98f, 0.88f), 0.026f, new Color(0.94f, 0.86f, 0.68f));
            AddText(t, "- cells: " + runtime.CellMarkers, new Vector2(0.98f, 0.66f), 0.026f, new Color(0.94f, 0.86f, 0.68f));
            AddText(t, "- transforms: " + runtime.Children, new Vector2(0.98f, 0.44f), 0.026f, new Color(0.94f, 0.86f, 0.68f));
            AddText(t, "- BEE-621 blocked", new Vector2(0.98f, 0.22f), 0.026f, new Color(0.94f, 0.86f, 0.68f));
            return root;
        }

        private static void AddPanel(Transform parent, Vector2 center, Vector2 size, Color color)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BEE-615 QA Overlay Panel";
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(center.x, center.y, 0f);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            Renderer renderer = quad.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = OverlayMaterial(color);
            }
        }

        private static void AddText(Transform parent, string text, Vector2 position, float size, Color color)
        {
            GameObject obj = new GameObject("BEE-615 QA Text " + text);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(position.x, position.y, -0.02f);
            TextMesh mesh = obj.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.UpperLeft;
            mesh.alignment = TextAlignment.Left;
            mesh.color = color;
        }

        private static Material OverlayMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = color;
            return material;
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

        private static void ComposePack(string directory, string contactName)
        {
            var textures = new List<Texture2D>();
            try
            {
                for (int i = 0; i < Shots.Length; i++)
                {
                    byte[] bytes = File.ReadAllBytes(ShotPath(directory, Shots[i]));
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texture.LoadImage(bytes);
                    textures.Add(texture);
                }

                Texture2D contact = ComposeContactSheet(textures);
                File.WriteAllBytes(directory + "/" + contactName, contact.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(contact);
            }
            finally
            {
                foreach (Texture2D texture in textures)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
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
                return new FrameAnalysis(false, texture.width, texture.height, 0d);
            }

            Color32 first = pixels[0];
            int different = 0;
            int sampled = 0;
            int step = Math.Max(1, pixels.Length / 9000);
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                int delta = Math.Abs(pixel.r - first.r) + Math.Abs(pixel.g - first.g) + Math.Abs(pixel.b - first.b);
                if (delta > 12)
                {
                    different++;
                }

                sampled++;
            }

            double variationRatio = sampled == 0 ? 0d : (double)different / sampled;
            return new FrameAnalysis(variationRatio > 0.01d, texture.width, texture.height, variationRatio);
        }

        private static string BuildPlayerManifest()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-051 - BEE-615 Player Clean Pack");
            builder.AppendLine();
            builder.AppendLine("Date : 2026-07-10");
            builder.AppendLine();
            builder.AppendLine("## Usage");
            builder.AppendLine();
            builder.AppendLine("- Pack destine a UI/QA scorecard.");
            builder.AppendLine("- Captures Play Mode normal depuis la Game View complete, sans overlay diagnostic ajoute par l'outil de capture.");
            builder.AppendLine("- Les overlays de scorecard sont interdits dans ce pack ; la preuve runtime est separee dans le pack QA.");
            builder.AppendLine("- Scene source : `Assets/Scenes/SandboxPlayground.unity`.");
            builder.AppendLine("- BEE-621 reste bloquee.");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            for (int i = 0; i < Shots.Length; i++)
            {
                builder.AppendLine("- `" + Shots[i].Id + "` : `" + ShotPath(PlayerDirectory, Shots[i]) + "`.");
            }

            builder.AppendLine("- `contact_sheet` : `" + PlayerDirectory + "/BEE-601_620_BEE-615_PlayerClean_ContactSheet.png`.");
            return builder.ToString();
        }

        private static string BuildQaManifest(RuntimeEvidence runtime, IReadOnlyList<CapturedShot> captured, string contactPath, FrameAnalysis contactAnalysis)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-051 - BEE-615 QA Runtime Proof Pack");
            builder.AppendLine();
            builder.AppendLine("Date : 2026-07-10");
            builder.AppendLine();
            builder.AppendLine("## Runtime");
            builder.AppendLine();
            builder.AppendLine("- Scene : `Assets/Scenes/SandboxPlayground.unity`.");
            builder.AppendLine("- Mode : Play Mode normal via `SandboxPlaygroundBootstrap.Start`.");
            builder.AppendLine("- Root : `" + HiveViewProductUiPresenter.RootName + "`.");
            builder.AppendLine("- Cell markers : `" + runtime.CellMarkers + "`.");
            builder.AppendLine("- Transforms : `" + runtime.Children + "`.");
            builder.AppendLine("- Wax rims : `" + runtime.WaxRim + "`.");
            builder.AppendLine("- Wax walls : `" + runtime.WaxWall + "`.");
            builder.AppendLine("- Honey specular landmark : `" + runtime.HoneySpecular + "`.");
            builder.AppendLine("- Queen core marker : `" + runtime.QueenCore + "`.");
            builder.AppendLine();
            builder.AppendLine("## Captures QA");
            builder.AppendLine();
            for (int i = 0; i < captured.Count; i++)
            {
                CapturedShot shot = captured[i];
                builder.AppendLine("- `" + shot.Shot.Id + "` : `" + shot.Path + "` ; nonBlank=`" + shot.Analysis.IsNonBlank + "` ; size=`" + shot.Analysis.Width + "x" + shot.Analysis.Height + "` ; variation=`" + shot.Analysis.VariationRatio.ToString("0.0000") + "`.");
            }

            builder.AppendLine("- `contact_sheet` : `" + contactPath + "` ; nonBlank=`" + contactAnalysis.IsNonBlank + "`.");
            builder.AppendLine();
            builder.AppendLine("## Reserves");
            builder.AppendLine();
            builder.AppendLine("- Les captures QA peuvent contenir des overlays diagnostic.");
            builder.AppendLine("- Les captures scorecard propres sont separees dans le pack PlayerClean.");
            builder.AppendLine("- Assets encore proceduraux, UI non finale.");
            builder.AppendLine("- BEE-621 reste bloquee.");
            return builder.ToString();
        }

        private static string ShotPath(string directory, Bee615Shot shot)
        {
            return directory + "/BEE-601_620_BEE-615_" + shot.FileStem + ".png";
        }

        private static void PrepareDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
            foreach (string file in Directory.GetFiles(directory))
            {
                File.Delete(file);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private enum Bee615ShotKind { Overview, IconSheet, ZoneLandmarks, Hud, Panel, States, Responsive, Portrait, NonClaims, Fallback }

        private readonly struct Bee615Shot
        {
            public Bee615Shot(string id, int width, int height, string fileStem, Bee615ShotKind kind)
            {
                Id = id;
                Width = width;
                Height = height;
                FileStem = fileStem;
                Kind = kind;
            }

            public string Id { get; }
            public int Width { get; }
            public int Height { get; }
            public string FileStem { get; }
            public Bee615ShotKind Kind { get; }
        }

        private readonly struct RuntimeEvidence
        {
            public RuntimeEvidence(int cellMarkers, int children, bool waxRim, bool waxWall, bool honeySpecular, bool queenCore)
            {
                CellMarkers = cellMarkers;
                Children = children;
                WaxRim = waxRim;
                WaxWall = waxWall;
                HoneySpecular = honeySpecular;
                QueenCore = queenCore;
            }

            public int CellMarkers { get; }
            public int Children { get; }
            public bool WaxRim { get; }
            public bool WaxWall { get; }
            public bool HoneySpecular { get; }
            public bool QueenCore { get; }
        }

        private readonly struct CapturedShot
        {
            public CapturedShot(Bee615Shot shot, string path, FrameAnalysis analysis)
            {
                Shot = shot;
                Path = path;
                Analysis = analysis;
            }

            public Bee615Shot Shot { get; }
            public string Path { get; }
            public FrameAnalysis Analysis { get; }
        }

        private readonly struct FrameAnalysis
        {
            public FrameAnalysis(bool isNonBlank, int width, int height, double variationRatio)
            {
                IsNonBlank = isNonBlank;
                Width = width;
                Height = height;
                VariationRatio = variationRatio;
            }

            public bool IsNonBlank { get; }
            public int Width { get; }
            public int Height { get; }
            public double VariationRatio { get; }
        }
    }
}
