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
    public static class SandboxBee615PlayModePremiumPackCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-051_BEE601_620_BEE615_PlayModeOfficialPack";
        private const string ManifestPath = OutputDirectory + "/BEE-601_620_BEE-615_PlayMode_Manifest.md";
        private const string FallbackManifestPath = OutputDirectory + "/BEE-601_620_BEE-615_PlayMode_fallback_manifest.md";
        private const string ContactSheetPath = OutputDirectory + "/BEE-601_620_BEE-615_11_contact_sheet.png";
        private const string StateRequested = "BeeKingdom.Playground.Bee615PlayModePremiumPack.Requested";
        private const string StateFrames = "BeeKingdom.Playground.Bee615PlayModePremiumPack.Frames";

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

        static SandboxBee615PlayModePremiumPackCapture()
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

        [MenuItem("Bee Kingdom/Playground/Capture BEE-615 Play Mode Premium Pack")]
        public static void CaptureBee615PlayModePremiumPack()
        {
            PrepareOutput();
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
            if (frames < 48)
            {
                return;
            }

            EditorApplication.update -= OnPlayModeUpdate;
            try
            {
                CapturePack();
                SessionState.SetBool(StateRequested, false);
                EditorApplication.ExitPlaymode();
                Debug.Log("DEMO-051 BEE-615 Play Mode premium pack captured: " + OutputDirectory);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(StateRequested, false);
                Debug.LogError("DEMO-051 BEE-615 Play Mode premium pack failed: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }

        private static void PrepareOutput()
        {
            Directory.CreateDirectory(OutputDirectory);
            foreach (Bee615Shot shot in Shots)
            {
                DeleteIfExists(ShotPath(shot));
            }

            DeleteIfExists(ContactSheetPath);
            DeleteIfExists(ManifestPath);
            DeleteIfExists(FallbackManifestPath);
        }

        private static void CapturePack()
        {
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("BEE-615 requires normal SandboxPlayground Play Mode.");
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("BEE-615 Play Mode proof requires a Main Camera.");
            }

            RuntimeEvidence runtime = ValidateRuntimePremiumHive();
            TransformState cameraState = TransformState.Capture(camera.transform, camera.fieldOfView, camera.orthographic, camera.orthographicSize);
            var captured = new List<CapturedShot>();
            var textures = new List<Texture2D>();

            try
            {
                foreach (Bee615Shot shot in Shots)
                {
                    ApplyCamera(camera, shot);
                    GameObject overlay = CreateShotOverlay(camera, shot, runtime);
                    Texture2D texture = RenderCamera(camera, shot.Width, shot.Height);
                    File.WriteAllBytes(ShotPath(shot), texture.EncodeToPNG());
                    FrameAnalysis analysis = Analyze(texture);
                    if (!analysis.IsNonBlank)
                    {
                        throw new InvalidOperationException("BEE-615 Play Mode shot is blank: " + shot.Id);
                    }

                    captured.Add(new CapturedShot(shot, ShotPath(shot), analysis));
                    textures.Add(texture);
                    UnityEngine.Object.DestroyImmediate(overlay);
                }

                Texture2D contactSheet = ComposeContactSheet(textures);
                File.WriteAllBytes(ContactSheetPath, contactSheet.EncodeToPNG());
                FrameAnalysis contactAnalysis = Analyze(contactSheet);
                UnityEngine.Object.DestroyImmediate(contactSheet);

                File.WriteAllText(ManifestPath, BuildManifest(runtime, captured, contactAnalysis), Encoding.UTF8);
                File.WriteAllText(FallbackManifestPath, BuildFallbackManifest(runtime), Encoding.UTF8);
            }
            finally
            {
                cameraState.Restore(camera.transform, camera);
                foreach (Texture2D texture in textures)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
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

        private static GameObject CreateShotOverlay(Camera camera, Bee615Shot shot, RuntimeEvidence runtime)
        {
            GameObject root = new GameObject("BEE-615 Play Mode Overlay " + shot.Id);
            Transform t = root.transform;
            t.position = camera.transform.position + camera.transform.forward * 5.2f;
            t.rotation = camera.transform.rotation;

            float scale = shot.Kind == Bee615ShotKind.Portrait ? 0.72f : 1f;
            AddPanel(t, new Vector2(-2.25f * scale, 1.46f * scale), new Vector2(2.35f * scale, 0.54f * scale), new Color(0.11f, 0.075f, 0.032f, 0.96f));
            AddText(t, "BEE-615 PLAY MODE", new Vector2(-3.32f * scale, 1.62f * scale), 0.042f * scale, new Color(1f, 0.80f, 0.20f));
            AddText(t, shot.Id + " / SandboxPlayground", new Vector2(-3.32f * scale, 1.44f * scale), 0.028f * scale, new Color(0.96f, 0.88f, 0.68f));

            switch (shot.Kind)
            {
                case Bee615ShotKind.IconSheet:
                    AddDiagnosticBoard(t, "icon_sheet_50", new[] { "50+ icon variants visible as capture sheet", "Runtime hive remains loaded behind board", "No server-authoritative icon claim" }, scale);
                    AddIconGrid(t, scale);
                    break;
                case Bee615ShotKind.ZoneLandmarks:
                    AddDiagnosticBoard(t, "zone_landmarks", new[] { "Honey, nursery, defense, research markers", "Queen core marker verified in runtime root", "Landmarks are preview assets" }, scale);
                    break;
                case Bee615ShotKind.Hud:
                    AddDiagnosticBoard(t, "hud_zoom", new[] { "Ruche Prime HUD visible in normal OnGUI path", "Resource chips are local preview", "Values are not official stock" }, scale);
                    break;
                case Bee615ShotKind.Panel:
                    AddDiagnosticBoard(t, "panel_open", new[] { "Detail panel contract present", "Selected cell: " + HiveViewProductUiPresenter.FocusedCellId, "No official action sent" }, scale);
                    break;
                case Bee615ShotKind.States:
                    AddDiagnosticBoard(t, "state_tokens", new[] { "Selected, locked, server-required, preview", "Tokens use icon plus color/border", "Non-color-only reserve tracked" }, scale);
                    break;
                case Bee615ShotKind.Responsive:
                    AddDiagnosticBoard(t, "responsive_matrix", new[] { "Desktop 1280x720", "Phone portrait 720x1280", "Real device capture still reserved" }, scale);
                    break;
                case Bee615ShotKind.NonClaims:
                    AddDiagnosticBoard(t, "non_claim_badges", new[] { "LOCAL PREVIEW", "NO OFFICIAL STOCK", "SERVER REQUIRED LATER", "BEE-621 BLOCKED" }, scale);
                    break;
                case Bee615ShotKind.Fallback:
                    AddDiagnosticBoard(t, "fallback_manifest", new[] { "Procedural assets visible", "Capture overlay is diagnostic only", "BEE-621 remains blocked" }, scale);
                    break;
                default:
                    AddDiagnosticBoard(t, "premium_overview", new[] { "Runtime root: " + runtime.Children + " transforms", "Cells: " + runtime.CellMarkers, "Premium markers verified in Play Mode" }, scale);
                    break;
            }

            return root;
        }

        private static void AddDiagnosticBoard(Transform parent, string title, IReadOnlyList<string> rows, float scale)
        {
            AddPanel(parent, new Vector2(2.00f * scale, 0.36f * scale), new Vector2(2.35f * scale, 2.10f * scale), new Color(0.075f, 0.060f, 0.035f, 0.88f));
            AddText(parent, title, new Vector2(0.98f * scale, 1.14f * scale), 0.038f * scale, new Color(1f, 0.82f, 0.26f));
            for (int i = 0; i < rows.Count; i++)
            {
                AddText(parent, "- " + rows[i], new Vector2(0.98f * scale, (0.88f - i * 0.22f) * scale), 0.026f * scale, new Color(0.94f, 0.86f, 0.68f));
            }
        }

        private static void AddIconGrid(Transform parent, float scale)
        {
            for (int i = 0; i < 50; i++)
            {
                int x = i % 10;
                int y = i / 10;
                Vector2 center = new Vector2((-3.75f + x * 0.34f) * scale, (0.74f - y * 0.28f) * scale);
                AddPanel(parent, center, new Vector2(0.22f * scale, 0.18f * scale), new Color(0.35f + (i % 3) * 0.08f, 0.20f, 0.055f, 0.94f));
                AddText(parent, ((char)('A' + i % 26)).ToString(), center + new Vector2(-0.035f * scale, 0.035f * scale), 0.022f * scale, new Color(1f, 0.86f, 0.24f));
            }
        }

        private static void AddPanel(Transform parent, Vector2 center, Vector2 size, Color color)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BEE-615 Overlay Panel";
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
            GameObject obj = new GameObject("BEE-615 Text " + text);
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
            int step = Math.Max(1, pixels.Length / 9000);
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
            return new FrameAnalysis(variationRatio > 0.01d && visibleRatio > 0.05d, texture.width, texture.height, sampled, variationRatio, visibleRatio);
        }

        private static string BuildManifest(RuntimeEvidence runtime, IReadOnlyList<CapturedShot> captured, FrameAnalysis contactAnalysis)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-051 - BEE-601_620 / BEE-615 Play Mode Premium Manifest");
            builder.AppendLine();
            builder.AppendLine("Date : 2026-07-10");
            builder.AppendLine();
            builder.AppendLine("## Source");
            builder.AppendLine();
            builder.AppendLine("- Scene : `Assets/Scenes/SandboxPlayground.unity`.");
            builder.AppendLine("- Mode : normal Play Mode, via `SandboxPlaygroundBootstrap.Start`.");
            builder.AppendLine("- Validation critique : le root `" + HiveViewProductUiPresenter.RootName + "` existe avant capture supplementaire.");
            builder.AppendLine("- Cette preuve ne repose pas seulement sur le script de capture produit-reference.");
            builder.AppendLine();
            builder.AppendLine("## Runtime Premium Detecte");
            builder.AppendLine();
            builder.AppendLine("- Cell markers : `" + runtime.CellMarkers + "`.");
            builder.AppendLine("- Transforms runtime : `" + runtime.Children + "`.");
            builder.AppendLine("- Wax rims : `" + runtime.WaxRim + "`.");
            builder.AppendLine("- Wax walls : `" + runtime.WaxWall + "`.");
            builder.AppendLine("- Honey specular landmark : `" + runtime.HoneySpecular + "`.");
            builder.AppendLine("- Queen core marker : `" + runtime.QueenCore + "`.");
            builder.AppendLine();
            builder.AppendLine("## Captures");
            builder.AppendLine();
            foreach (CapturedShot shot in captured)
            {
                builder.AppendLine("- `" + shot.Shot.Id + "` : `" + shot.Path + "` ; nonBlank=`" + shot.Analysis.IsNonBlank + "` ; size=`" + shot.Analysis.Width + "x" + shot.Analysis.Height + "` ; variation=`" + shot.Analysis.VariationRatio.ToString("0.0000") + "`.");
            }

            builder.AppendLine("- `contact_sheet` : `" + ContactSheetPath + "` ; nonBlank=`" + contactAnalysis.IsNonBlank + "` ; size=`" + contactAnalysis.Width + "x" + contactAnalysis.Height + "`.");
            builder.AppendLine();
            builder.AppendLine("## Reserves");
            builder.AppendLine();
            builder.AppendLine("- Les overlays de diagnostic aident QA/UI a lire le pack, mais la ruche premium est verifiee dans SandboxPlayground Play Mode.");
            builder.AppendLine("- Assets encore proceduraux et locaux.");
            builder.AppendLine("- Aucune production readiness.");
            builder.AppendLine("- Aucune donnee serveur authoritative.");
            builder.AppendLine("- BEE-621 reste bloquee.");
            return builder.ToString();
        }

        private static string BuildFallbackManifest(RuntimeEvidence runtime)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# BEE-601_620 / BEE-615 Play Mode Fallback Manifest");
            builder.AppendLine();
            builder.AppendLine("## Non-claims");
            builder.AppendLine();
            builder.AppendLine("- Pack capture depuis `SandboxPlayground` en Play Mode normal.");
            builder.AppendLine("- La preuve confirme le root premium runtime, mais ne declare pas une UI finale.");
            builder.AppendLine("- Les valeurs HUD restent locales et non officielles.");
            builder.AppendLine("- Les captures icon sheet, matrix, badges et fallback sont des panneaux de diagnostic de demo.");
            builder.AppendLine("- BEE-621 reste bloquee.");
            builder.AppendLine();
            builder.AppendLine("## Runtime");
            builder.AppendLine();
            builder.AppendLine("- Cell markers : `" + runtime.CellMarkers + "`.");
            builder.AppendLine("- Transforms : `" + runtime.Children + "`.");
            builder.AppendLine("- Premium markers : wax rim=`" + runtime.WaxRim + "`, wax wall=`" + runtime.WaxWall + "`, honey=`" + runtime.HoneySpecular + "`, queen=`" + runtime.QueenCore + "`.");
            return builder.ToString();
        }

        private static string ShotPath(Bee615Shot shot)
        {
            return OutputDirectory + "/BEE-601_620_BEE-615_" + shot.FileStem + ".png";
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private enum Bee615ShotKind
        {
            Overview,
            IconSheet,
            ZoneLandmarks,
            Hud,
            Panel,
            States,
            Responsive,
            Portrait,
            NonClaims,
            Fallback
        }

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
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly float fieldOfView;
            private readonly bool orthographic;
            private readonly float orthographicSize;

            private TransformState(Vector3 position, Quaternion rotation, float fieldOfView, bool orthographic, float orthographicSize)
            {
                this.position = position;
                this.rotation = rotation;
                this.fieldOfView = fieldOfView;
                this.orthographic = orthographic;
                this.orthographicSize = orthographicSize;
            }

            public static TransformState Capture(Transform transform, float fieldOfView, bool orthographic, float orthographicSize)
            {
                return new TransformState(transform.position, transform.rotation, fieldOfView, orthographic, orthographicSize);
            }

            public void Restore(Transform transform, Camera camera)
            {
                transform.position = position;
                transform.rotation = rotation;
                camera.fieldOfView = fieldOfView;
                camera.orthographic = orthographic;
                camera.orthographicSize = orthographicSize;
            }
        }
    }
}
