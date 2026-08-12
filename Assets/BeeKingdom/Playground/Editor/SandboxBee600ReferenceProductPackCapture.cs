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
    public static class SandboxBee600ReferenceProductPackCapture
    {
        private const string ScenePath = "Assets/Scenes/SandboxPlayground.unity";
        private const string OutputDirectory = "C:/projets/beekingdom/prompt_demo/rapports/DEMO-051_BEE601_620_BEE615_PremiumRework";
        private const string ManifestPath = OutputDirectory + "/BEE-601_620_BEE-615_Manifest.md";
        private const string ContactSheetPath = OutputDirectory + "/BEE-601_620_BEE-615_11_contact_sheet.png";

        private static readonly ProductShot[] Shots =
        {
            new ProductShot("premium_overview", 1280, 720, "01_premium_overview", ProductShotKind.Overview),
            new ProductShot("icon_sheet_50", 1280, 720, "02_icon_sheet_50", ProductShotKind.IconSheet),
            new ProductShot("zone_landmarks", 1280, 720, "03_zone_landmarks", ProductShotKind.ZoneLandmarks),
            new ProductShot("hud_zoom", 1280, 720, "04_hud_zoom", ProductShotKind.Hud),
            new ProductShot("panel_open", 1280, 720, "05_panel_open", ProductShotKind.Detail),
            new ProductShot("state_tokens", 1280, 720, "06_state_tokens", ProductShotKind.States),
            new ProductShot("responsive_matrix", 1280, 720, "07_responsive_matrix", ProductShotKind.ResponsiveMatrix),
            new ProductShot("phone_portrait", 720, 1280, "08_phone_portrait", ProductShotKind.Mobile),
            new ProductShot("non_claim_badges", 1280, 720, "09_non_claim_badges", ProductShotKind.NonClaimBadges),
            new ProductShot("fallback_manifest", 1280, 720, "10_fallback_manifest", ProductShotKind.FallbackManifest)
        };

        [MenuItem("Bee Kingdom/Playground/Capture BEE-600 Product Reference Pack")]
        public static void CaptureBee600ProductReferencePack()
        {
            Directory.CreateDirectory(OutputDirectory);
            DeleteIfExists(ManifestPath);
            DeleteIfExists(ContactSheetPath);
            foreach (ProductShot shot in Shots)
            {
                DeleteIfExists(ShotPath(shot));
            }

            EditorSceneManager.OpenScene(ScenePath);
            Camera camera = SandboxPlaygroundBootstrap.EnsureRenderableCamera(Camera.main);
            var captured = new List<CapturedShot>();
            var textures = new List<Texture2D>();

            try
            {
                foreach (ProductShot shot in Shots)
                {
                    GameObject root = BuildShotScene(camera, shot);
                    Texture2D texture = RenderCamera(camera, shot.Width, shot.Height);
                    File.WriteAllBytes(ShotPath(shot), texture.EncodeToPNG());
                    FrameAnalysis analysis = Analyze(texture);
                    if (!analysis.IsNonBlank)
                    {
                        throw new InvalidOperationException("BEE-600 product shot is blank: " + shot.Id);
                    }

                    captured.Add(new CapturedShot(shot, ShotPath(shot), analysis));
                    textures.Add(texture);
                    UnityEngine.Object.DestroyImmediate(root);
                }

                Texture2D contactSheet = ComposeContactSheet(textures);
                File.WriteAllBytes(ContactSheetPath, contactSheet.EncodeToPNG());
                FrameAnalysis contactAnalysis = Analyze(contactSheet);
                UnityEngine.Object.DestroyImmediate(contactSheet);

                File.WriteAllText(ManifestPath, BuildManifest(captured, contactAnalysis), Encoding.UTF8);
                Debug.Log("DEMO-051 BEE-601_620 BEE-615 premium rework pack captured: " + OutputDirectory);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("DEMO-047 BEE-600 product reference pack failed: " + exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
            finally
            {
                foreach (Texture2D texture in textures)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        private static GameObject BuildShotScene(Camera camera, ProductShot shot)
        {
            bool portrait = shot.Kind == ProductShotKind.Mobile;
            ConfigureCamera(camera, portrait);

            GameObject root = new GameObject("DEMO-047 BEE-600 Product Reference " + shot.Id);
            float worldHalfHeight = camera.orthographicSize;
            float worldHalfWidth = worldHalfHeight * shot.Width / shot.Height;

            AddRect(root.transform, "Moss Ground", Vector3.zero, new Vector2(worldHalfWidth * 2.4f, worldHalfHeight * 2.4f), new Color(0.11f, 0.18f, 0.13f), 0f);
            AddSoftCircle(root.transform, "Warm Backlight", new Vector3(0f, 0.35f, 0.02f), 4.8f, new Color(0.64f, 0.40f, 0.10f, 0.92f));
            AddDecor(root.transform, worldHalfWidth, worldHalfHeight, portrait);

            if (shot.Kind == ProductShotKind.IconSheet)
            {
                AddIconSheet(parent: root.transform, rect: new Rect(-6.4f, -3.45f, 12.8f, 6.7f));
                AddShotBadge(root.transform, shot, new Rect(-6.95f, 3.04f, 3.15f, 0.78f));
                return root;
            }

            if (shot.Kind == ProductShotKind.ResponsiveMatrix)
            {
                AddResponsiveMatrix(root.transform, new Rect(-6.55f, -3.35f, 13.1f, 6.5f));
                AddShotBadge(root.transform, shot, new Rect(-6.95f, 3.04f, 3.15f, 0.78f));
                return root;
            }

            if (shot.Kind == ProductShotKind.FallbackManifest)
            {
                AddFallbackManifest(root.transform, new Rect(-6.55f, -3.35f, 13.1f, 6.5f));
                AddShotBadge(root.transform, shot, new Rect(-6.95f, 3.04f, 3.15f, 0.78f));
                return root;
            }

            if (shot.Kind == ProductShotKind.NonClaimBadges)
            {
                AddNonClaimBadgeBoard(root.transform, new Rect(-6.55f, -3.35f, 13.1f, 6.5f));
                AddShotBadge(root.transform, shot, new Rect(-6.95f, 3.04f, 3.15f, 0.78f));
                return root;
            }

            Vector2 boardCenter = portrait ? new Vector2(0f, -0.20f) : new Vector2(-0.30f, -0.08f);
            float radius = portrait ? 0.62f : 0.74f;
            AddHiveBoard(root.transform, boardCenter, radius, shot.Kind);

            if (shot.Kind == ProductShotKind.ZoneLandmarks)
            {
                AddZoneRecognitionStrip(root.transform, new Rect(3.88f, -3.02f, 3.15f, 5.78f));
            }

            if (shot.Kind == ProductShotKind.Hud)
            {
                AddHud(root.transform, new Rect(-4.6f, 2.95f, 9.2f, 1.0f), true, true);
            }
            else
            {
                AddHud(root.transform, portrait ? new Rect(-3.45f, 6.00f, 6.90f, 1.18f) : new Rect(-4.25f, 3.1f, 8.5f, 0.95f), portrait, false);
            }

            AddNavigation(root.transform, portrait, shot.Kind);

            if (portrait)
            {
                AddDetailPanel(root.transform, new Rect(-3.20f, -5.82f, 6.40f, 1.92f), true);
            }
            else if (shot.Kind == ProductShotKind.Detail)
            {
                AddDetailPanel(root.transform, new Rect(4.10f, -1.55f, 3.0f, 4.1f), true);
            }
            else if (!portrait && shot.Kind != ProductShotKind.Hud && shot.Kind != ProductShotKind.ZoneLandmarks)
            {
                AddDetailPanel(root.transform, new Rect(4.35f, -1.30f, 2.55f, 3.55f), false);
            }

            if (shot.Kind == ProductShotKind.States)
            {
                AddStateLegend(root.transform, new Rect(-6.95f, -3.55f, 3.2f, 2.05f), true);
            }
            else if (shot.Kind == ProductShotKind.FutureLocked)
            {
                AddFutureLockedCallout(root.transform, new Rect(-6.95f, -3.4f, 3.55f, 1.8f));
            }
            else if (shot.Kind == ProductShotKind.Accessibility)
            {
                AddAccessibilityCallout(root.transform, new Rect(-6.95f, -3.4f, 4.25f, 2.25f));
            }
            else if (!portrait)
            {
                AddStateLegend(root.transform, new Rect(-6.95f, -3.25f, 2.85f, 1.75f), false);
            }

            if (!portrait)
            {
                AddShotBadge(root.transform, shot, new Rect(-6.95f, 3.04f, 3.15f, 0.78f));
            }
            return root;
        }

        private static void ConfigureCamera(Camera camera, bool portrait)
        {
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = portrait ? 7.6f : 4.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.10f, 0.14f, 0.13f);
            camera.fieldOfView = 55f;
        }

        private static void AddHiveBoard(Transform parent, Vector2 center, float radius, ProductShotKind kind)
        {
            AddSoftCircle(parent, "Board Shadow", new Vector3(center.x, center.y - radius * 0.34f, 0.05f), radius * 7.9f, new Color(0.10f, 0.055f, 0.020f, 0.90f));
            AddSoftCircle(parent, "Hive Ambient Glow", new Vector3(center.x, center.y + radius * 0.15f, 0.08f), radius * 6.5f, new Color(0.55f, 0.31f, 0.06f, 0.78f));
            AddSoftCircle(parent, "Honey Crown Aura", new Vector3(center.x, center.y + radius * 1.05f, 0.09f), radius * 3.1f, new Color(1f, 0.58f, 0.06f, 0.34f));

            var cells = new List<CellVisual>();
            for (int q = -3; q <= 3; q++)
            {
                int r1 = Math.Max(-3, -q - 3);
                int r2 = Math.Min(3, -q + 3);
                for (int r = r1; r <= r2; r++)
                {
                    if (Math.Abs(q) == 3 && Math.Abs(r) == 3) continue;
                    Vector2 pos = Axial(center, radius, q, r);
                    string id = q + ":" + r;
                    CellState state = ResolveState(q, r, kind);
                    IconKind icon = ResolveCellIcon(q, r, state);
                    cells.Add(new CellVisual(id, pos, state, icon));
                }
            }

            foreach (CellVisual cell in cells)
            {
                AddHex(parent, "Cell Deep Shadow " + cell.Id, cell.Position + new Vector2(radius * 0.08f, -radius * 0.28f), radius * 1.16f, new Color(0.10f, 0.045f, 0.016f, 0.96f), 0.16f);
                AddIsoWall(parent, "Left Wall " + cell.Id, cell.Position, radius * 1.10f, new Color(0.42f, 0.20f, 0.045f), -1, 0.19f);
                AddIsoWall(parent, "Right Wall " + cell.Id, cell.Position, radius * 1.10f, new Color(0.62f, 0.31f, 0.055f), 1, 0.20f);
                AddHex(parent, "Outer Wax Wall " + cell.Id, cell.Position + new Vector2(0f, -radius * 0.08f), radius * 1.10f, new Color(0.98f, 0.60f, 0.08f), 0.22f);
                AddHex(parent, "Inner Caramel Wall " + cell.Id, cell.Position + new Vector2(0f, -radius * 0.02f), radius * 1.02f, new Color(0.54f, 0.27f, 0.055f), 0.24f);
            }

            foreach (CellVisual cell in cells)
            {
                AddHex(parent, "Honey Floor " + cell.Id, cell.Position, radius * 0.84f, CellColor(cell.State, kind), 0.28f);
                AddHex(parent, "Wax Lip Highlight " + cell.Id, cell.Position + new Vector2(-radius * 0.10f, radius * 0.10f), radius * 0.48f, CellHighlightColor(cell.State), 0.34f);
                AddWaxTexture(parent, cell.Position, radius, cell.Id, cell.State);
                if (cell.State == CellState.Selected)
                {
                    AddHex(parent, "Selected Glow Outer " + cell.Id, cell.Position, radius * 1.32f, new Color(1f, 0.92f, 0.18f, 0.72f), 0.21f);
                    AddHex(parent, "Selected White Rim " + cell.Id, cell.Position, radius * 1.12f, new Color(1f, 0.96f, 0.46f), 0.31f);
                    AddHex(parent, "Selected Inner " + cell.Id, cell.Position, radius * 0.88f, CellColor(cell.State, kind), 0.32f);
                }

                if (cell.State == CellState.Locked || cell.State == CellState.Future || cell.State == CellState.Server)
                {
                    AddStateMark(parent, cell.Position, radius, cell.State);
                }

                AddPremiumIcon(parent, "Landmark " + cell.Id, cell.Position + new Vector2(0f, radius * 0.03f), radius * LandmarkSize(cell.Icon), cell.Icon, cell.State, 0.46f);
            }

            AddSoftCircle(parent, "Queen Back Glow", new Vector3(center.x, center.y + radius * 0.10f, 0.44f), radius * 0.86f, new Color(1f, 0.76f, 0.05f, 0.70f));
            AddPremiumIcon(parent, "Queen Crown Landmark", center + new Vector2(0f, radius * 0.06f), radius * 1.28f, IconKind.Queen, CellState.Selected, 0.52f);
        }

        private static Vector2 Axial(Vector2 center, float radius, int q, int r)
        {
            float x = radius * Mathf.Sqrt(3f) * (q + r * 0.5f);
            float y = radius * 1.50f * r;
            return center + new Vector2(x, y);
        }

        private static CellState ResolveState(int q, int r, ProductShotKind kind)
        {
            if (q == 0 && r == 0) return CellState.Selected;
            if (q == 1 && r == -1) return CellState.Occupied;
            if (q == -1 && r == 0) return CellState.Occupied;
            if (q == -2 && r == 1) return CellState.Server;
            if (q == 2 && r == -2) return CellState.Future;
            if (Math.Abs(q) == 3 || Math.Abs(r) == 3 || Math.Abs(q + r) == 3) return CellState.Locked;
            if (kind == ProductShotKind.FutureLocked && (q == 2 || r == -2)) return CellState.Future;
            return CellState.Empty;
        }

        private static IconKind ResolveCellIcon(int q, int r, CellState state)
        {
            if (q == 0 && r == 0) return IconKind.Queen;
            if (q > 0 && r <= 0 && state != CellState.Locked) return IconKind.Nursery;
            if (q <= -1 && r == 0) return IconKind.HoneyVault;
            if (q < 0 && r > 0) return IconKind.Defense;
            if (q < 0 && r < 1) return IconKind.Research;
            if (r > 0) return IconKind.WaxPress;
            if (state == CellState.Server) return IconKind.ServerRequired;
            if (state == CellState.Future) return IconKind.FutureRoom;
            if (state == CellState.Locked) return IconKind.Locked;
            return IconKind.Alliance;
        }

        private static Color CellColor(CellState state, ProductShotKind kind)
        {
            if (kind == ProductShotKind.Accessibility)
            {
                switch (state)
                {
                    case CellState.Selected: return new Color(1f, 0.95f, 0.18f);
                    case CellState.Locked: return new Color(0.13f, 0.13f, 0.13f);
                    case CellState.Server: return new Color(0.30f, 0.55f, 0.88f);
                    case CellState.Future: return new Color(0.62f, 0.62f, 0.62f);
                    case CellState.Occupied: return new Color(0.88f, 0.55f, 0.18f);
                    default: return new Color(0.62f, 0.38f, 0.14f);
                }
            }

            switch (state)
            {
                case CellState.Selected: return new Color(1f, 0.84f, 0.14f);
                case CellState.Locked: return new Color(0.31f, 0.20f, 0.12f);
                case CellState.Server: return new Color(0.45f, 0.56f, 0.72f);
                case CellState.Future: return new Color(0.52f, 0.42f, 0.25f);
                case CellState.Occupied: return new Color(0.78f, 0.43f, 0.10f);
                default: return new Color(0.68f, 0.40f, 0.11f);
            }
        }

        private static Color CellHighlightColor(CellState state)
        {
            switch (state)
            {
                case CellState.Selected: return new Color(1f, 0.96f, 0.42f);
                case CellState.Locked: return new Color(0.58f, 0.40f, 0.20f);
                case CellState.Server: return new Color(0.72f, 0.86f, 0.96f);
                case CellState.Future: return new Color(0.78f, 0.66f, 0.38f);
                default: return new Color(1f, 0.68f, 0.22f);
            }
        }

        private static void AddStateMark(Transform parent, Vector2 pos, float radius, CellState state)
        {
            IconKind icon = state == CellState.Server ? IconKind.ServerRequired : state == CellState.Future ? IconKind.FutureRoom : IconKind.Locked;
            AddPremiumIcon(parent, "State Token " + state, pos + new Vector2(radius * 0.42f, radius * 0.30f), radius * 0.46f, icon, state, 0.62f);
        }

        private static void AddHud(Transform parent, Rect rect, bool compact, bool closeRead)
        {
            AddRect(parent, "HUD Panel Shadow", RectCenter(new Rect(rect.x + 0.08f, rect.y - 0.08f, rect.width, rect.height), 0.58f), new Vector2(rect.width, rect.height), new Color(0.055f, 0.030f, 0.012f, 0.80f), 0.58f);
            AddRect(parent, "HUD Panel Bronze", RectCenter(rect, 0.60f), new Vector2(rect.width, rect.height), new Color(0.17f, 0.105f, 0.040f, 0.98f), 0.60f);
            AddRect(parent, "HUD Panel Wax Highlight", RectCenter(new Rect(rect.x + 0.06f, rect.y + rect.height * 0.55f, rect.width - 0.12f, rect.height * 0.36f), 0.64f), new Vector2(rect.width - 0.12f, rect.height * 0.36f), new Color(0.36f, 0.22f, 0.065f, 0.62f), 0.64f);
            AddPremiumIcon(parent, "HUD Hive Crest", new Vector2(rect.x + 0.28f, rect.y + rect.height - 0.30f), compact ? 0.28f : 0.34f, IconKind.Queen, CellState.Selected, 0.86f);
            AddText(parent, compact ? "Ruche Prime" : "Bee Kingdom - Ruche Prime", new Vector3(rect.x + (compact ? 0.58f : 0.66f), rect.y + rect.height - 0.20f, 0.86f), compact ? 0.13f : 0.17f, new Color(1f, 0.86f, 0.26f), TextAnchor.UpperLeft);

            string[] chips = compact ? new[] { "1.2M", "420K", "315K", "86", "68%" } : new[] { "1.24M", "420K", "315K", "86", "68%" };
            IconKind[] icons = { IconKind.HoneyDrop, IconKind.WaxBlock, IconKind.Pollen, IconKind.Bee, IconKind.Capacity };
            float chipW = (rect.width - 0.36f) / chips.Length;
            for (int i = 0; i < chips.Length; i++)
            {
                Rect chip = new Rect(rect.x + 0.18f + i * chipW, rect.y + 0.12f, chipW - 0.08f, closeRead ? 0.46f : compact ? 0.42f : 0.36f);
                AddRect(parent, "HUD Chip Shadow " + i, RectCenter(new Rect(chip.x + 0.035f, chip.y - 0.035f, chip.width, chip.height), 0.69f), new Vector2(chip.width, chip.height), new Color(0.05f, 0.025f, 0.01f, 0.60f), 0.69f);
                AddRect(parent, "HUD Chip Wax " + i, RectCenter(chip, 0.72f), new Vector2(chip.width, chip.height), new Color(0.38f, 0.235f, 0.085f), 0.72f);
                AddRect(parent, "HUD Chip Shine " + i, RectCenter(new Rect(chip.x + 0.03f, chip.y + chip.height * 0.55f, chip.width - 0.06f, chip.height * 0.30f), 0.78f), new Vector2(chip.width - 0.06f, chip.height * 0.30f), new Color(0.82f, 0.48f, 0.12f, 0.45f), 0.78f);
                AddPremiumIcon(parent, "HUD Icon " + i, new Vector2(chip.x + chip.width * 0.18f, chip.y + chip.height * 0.50f), closeRead ? 0.24f : compact ? 0.18f : 0.19f, icons[i], CellState.Occupied, 0.90f);
                AddText(parent, chips[i], new Vector3(chip.x + chip.width * 0.36f, chip.y + chip.height * 0.73f, 0.93f), closeRead ? 0.15f : compact ? 0.092f : 0.105f, new Color(1f, 0.96f, 0.80f), TextAnchor.UpperLeft);
            }

            Rect badge = new Rect(rect.x + rect.width - (compact ? 1.48f : 1.84f), rect.y + rect.height - 0.46f, compact ? 1.24f : 1.60f, 0.28f);
            AddRect(parent, "HUD Preview Badge", RectCenter(badge, 0.92f), new Vector2(badge.width, badge.height), new Color(0.15f, 0.24f, 0.25f, 0.94f), 0.92f);
            AddPremiumIcon(parent, "HUD Preview Badge Icon", new Vector2(badge.x + 0.16f, badge.y + badge.height * 0.5f), 0.13f, IconKind.Preview, CellState.Server, 0.98f);
            AddText(parent, "LOCAL PREVIEW", new Vector3(badge.x + 0.30f, badge.y + 0.19f, 1.0f), closeRead ? 0.10f : compact ? 0.062f : 0.075f, new Color(0.76f, 0.92f, 1f), TextAnchor.UpperLeft);
        }

        private static void AddNavigation(Transform parent, bool portrait, ProductShotKind kind)
        {
            if (portrait)
            {
                Rect rail = new Rect(-3.30f, -6.95f, 6.60f, 0.82f);
                AddRect(parent, "Portrait Bottom Rail", RectCenter(rail, 0.65f), new Vector2(rail.width, rail.height), new Color(0.18f, 0.11f, 0.055f, 0.96f), 0.65f);
                string[] items = { "Home", "Zones", "Ress.", "Detail", "Plus" };
                IconKind[] icons = { IconKind.Hive, IconKind.Nursery, IconKind.HoneyDrop, IconKind.Inspect, IconKind.More };
                for (int i = 0; i < items.Length; i++)
                {
                    Rect item = new Rect(rail.x + 0.16f + i * 1.26f, rail.y + 0.10f, 1.03f, 0.62f);
                    AddRect(parent, "Portrait Nav Item " + i, RectCenter(item, 0.73f), new Vector2(item.width, item.height), i == 0 ? new Color(0.72f, 0.42f, 0.08f) : new Color(0.28f, 0.18f, 0.08f), 0.73f);
                    AddPremiumIcon(parent, "Portrait Nav Icon " + i, new Vector2(item.x + item.width * 0.50f, item.y + item.height * 0.62f), 0.18f, icons[i], i == 0 ? CellState.Selected : CellState.Occupied, 0.90f);
                    AddText(parent, items[i], new Vector3(item.x + 0.20f, item.y + 0.16f, 0.92f), 0.068f, new Color(1f, 0.92f, 0.72f), TextAnchor.UpperLeft);
                }
                return;
            }

            Rect bottom = new Rect(-2.45f, -3.78f, 4.9f, 0.54f);
            AddRect(parent, "Bottom Rail", RectCenter(bottom, 0.65f), new Vector2(bottom.width, bottom.height), new Color(0.18f, 0.11f, 0.055f, 0.96f), 0.65f);
            string[] labels = { "Ruche", "Zones", "Ressources", "Detail", "Preview" };
            IconKind[] bottomIcons = { IconKind.Hive, IconKind.Nursery, IconKind.HoneyDrop, IconKind.Inspect, IconKind.Preview };
            for (int i = 0; i < labels.Length; i++)
            {
                Rect item = new Rect(bottom.x + 0.10f + i * 0.93f, bottom.y + 0.08f, 0.78f, 0.38f);
                AddRect(parent, "Bottom Nav Item " + i, RectCenter(item, 0.73f), new Vector2(item.width, item.height), i == 0 ? new Color(0.70f, 0.40f, 0.08f) : new Color(0.28f, 0.18f, 0.08f), 0.73f);
                AddPremiumIcon(parent, "Bottom Nav Icon " + i, new Vector2(item.x + 0.16f, item.y + 0.20f), 0.13f, bottomIcons[i], i == 0 ? CellState.Selected : CellState.Occupied, 0.88f);
                AddText(parent, labels[i], new Vector3(item.x + 0.30f, item.y + 0.24f, 0.90f), 0.064f, new Color(1f, 0.92f, 0.72f), TextAnchor.UpperLeft);
            }

            Rect side = new Rect(-7.0f, -0.95f, 1.18f, 2.75f);
            AddRect(parent, "Zone Rail", RectCenter(side, 0.65f), new Vector2(side.width, side.height), new Color(0.18f, 0.11f, 0.055f, 0.94f), 0.65f);
            IconKind[] zoneIcons = { IconKind.HoneyVault, IconKind.Nursery, IconKind.Defense, IconKind.Research };
            for (int i = 0; i < zoneIcons.Length; i++)
            {
                Vector2 p = new Vector2(side.x + 0.38f, side.y + side.height - 0.42f - i * 0.56f);
                AddPremiumIcon(parent, "Side Zone Icon " + i, p, 0.22f, zoneIcons[i], CellState.Occupied, 0.86f);
            }
        }

        private static void AddDetailPanel(Transform parent, Rect rect, bool emphasized)
        {
            AddRect(parent, "Detail Panel Shadow", RectCenter(new Rect(rect.x + 0.07f, rect.y - 0.07f, rect.width, rect.height), 0.66f), new Vector2(rect.width, rect.height), new Color(0.055f, 0.028f, 0.010f, 0.80f), 0.66f);
            AddRect(parent, "Detail Panel Bronze", RectCenter(rect, 0.68f), new Vector2(rect.width, rect.height), new Color(0.18f, 0.10f, 0.045f, 0.98f), 0.68f);
            AddRect(parent, "Detail Panel Header Shine", RectCenter(new Rect(rect.x + 0.04f, rect.y + rect.height - 0.64f, rect.width - 0.08f, 0.56f), 0.72f), new Vector2(rect.width - 0.08f, 0.56f), new Color(0.50f, 0.30f, 0.085f, 0.76f), 0.72f);
            AddPremiumIcon(parent, "Detail Queen Icon", new Vector2(rect.x + 0.40f, rect.y + rect.height - 0.34f), emphasized ? 0.34f : 0.25f, IconKind.Queen, CellState.Selected, 0.90f);
            AddText(parent, "Chambre Reine", new Vector3(rect.x + 0.78f, rect.y + rect.height - 0.21f, 0.92f), emphasized ? 0.18f : 0.12f, new Color(1f, 0.86f, 0.28f), TextAnchor.UpperLeft);
            AddPremiumIcon(parent, "Detail Close Icon", new Vector2(rect.x + rect.width - 0.28f, rect.y + rect.height - 0.30f), 0.18f, IconKind.Close, CellState.Occupied, 0.94f);
            Rect stateBadge = new Rect(rect.x + 0.22f, rect.y + rect.height - 1.05f, emphasized ? 1.32f : 0.92f, 0.34f);
            AddRect(parent, "Detail State Badge", RectCenter(stateBadge, 0.82f), new Vector2(stateBadge.width, stateBadge.height), new Color(0.12f, 0.25f, 0.22f, 0.94f), 0.82f);
            AddPremiumIcon(parent, "Detail Preview Token", new Vector2(stateBadge.x + 0.18f, stateBadge.y + stateBadge.height * 0.5f), 0.15f, IconKind.Preview, CellState.Server, 0.94f);
            AddText(parent, "Preview local", new Vector3(stateBadge.x + 0.36f, stateBadge.y + 0.23f, 0.94f), emphasized ? 0.09f : 0.064f, new Color(0.76f, 0.96f, 1f), TextAnchor.UpperLeft);
            Rect serverBadge = new Rect(rect.x + stateBadge.width + 0.36f, rect.y + rect.height - 1.05f, emphasized ? 1.42f : 0.98f, 0.34f);
            AddRect(parent, "Detail Server Badge", RectCenter(serverBadge, 0.82f), new Vector2(serverBadge.width, serverBadge.height), new Color(0.30f, 0.18f, 0.08f, 0.94f), 0.82f);
            AddPremiumIcon(parent, "Detail Server Token", new Vector2(serverBadge.x + 0.18f, serverBadge.y + serverBadge.height * 0.5f), 0.15f, IconKind.ServerRequired, CellState.Server, 0.94f);
            AddText(parent, "Serveur futur", new Vector3(serverBadge.x + 0.36f, serverBadge.y + 0.23f, 0.94f), emphasized ? 0.09f : 0.064f, new Color(1f, 0.90f, 0.64f), TextAnchor.UpperLeft);

            float cardY = rect.y + rect.height - 1.64f;
            AddRect(parent, "Detail Info Card", new Vector3(rect.x + rect.width * 0.50f, cardY, 0.78f), new Vector2(rect.width - 0.42f, emphasized ? 0.72f : 0.50f), new Color(0.28f, 0.16f, 0.060f, 0.92f), 0.78f);
            AddText(parent, "Coeur de ruche, lecture locale seulement", new Vector3(rect.x + 0.28f, cardY + (emphasized ? 0.18f : 0.13f), 0.94f), emphasized ? 0.105f : 0.072f, Color.white, TextAnchor.UpperLeft);
            AddText(parent, "Aucun achat, construction, reward ou synchro", new Vector3(rect.x + 0.28f, rect.y + 0.28f, 0.94f), emphasized ? 0.092f : 0.064f, new Color(1f, 0.90f, 0.55f), TextAnchor.UpperLeft);
        }

        private static void AddStateLegend(Transform parent, Rect rect, bool emphasized)
        {
            AddRect(parent, "State Legend", RectCenter(rect, 0.68f), new Vector2(rect.width, rect.height), new Color(0.20f, 0.12f, 0.060f, 0.96f), 0.68f);
            AddText(parent, "Etats visuels", new Vector3(rect.x + 0.16f, rect.y + rect.height - 0.26f, 0.80f), emphasized ? 0.16f : 0.12f, new Color(1f, 0.84f, 0.25f), TextAnchor.UpperLeft);
            IconKind[] icons = { IconKind.Selected, IconKind.Locked, IconKind.ServerRequired, IconKind.FutureRoom };
            CellState[] states = { CellState.Selected, CellState.Locked, CellState.Server, CellState.Future };
            string[] copy = { "halo selection", "verrou premium", "serveur futur", "salle reservee" };
            for (int i = 0; i < icons.Length; i++)
            {
                float y = rect.y + rect.height - 0.64f - i * (emphasized ? 0.34f : 0.28f);
                AddPremiumIcon(parent, "State Legend Icon " + i, new Vector2(rect.x + 0.34f, y), emphasized ? 0.19f : 0.14f, icons[i], states[i], 0.86f);
                AddText(parent, copy[i], new Vector3(rect.x + 0.62f, y + 0.07f, 0.88f), emphasized ? 0.096f : 0.070f, Color.white, TextAnchor.UpperLeft);
            }
        }

        private static void AddFutureLockedCallout(Transform parent, Rect rect)
        {
            AddRect(parent, "Future Locked Callout", RectCenter(rect, 0.68f), new Vector2(rect.width, rect.height), new Color(0.20f, 0.12f, 0.060f, 0.96f), 0.68f);
            AddText(parent, "Cellules futures / locked", new Vector3(rect.x + 0.18f, rect.y + rect.height - 0.30f, 0.80f), 0.16f, new Color(1f, 0.84f, 0.25f), TextAnchor.UpperLeft);
            AddText(parent, "Silhouette future visible", new Vector3(rect.x + 0.18f, rect.y + rect.height - 0.70f, 0.80f), 0.12f, Color.white, TextAnchor.UpperLeft);
            AddText(parent, "Verrou sans couleur seule", new Vector3(rect.x + 0.18f, rect.y + rect.height - 1.02f, 0.80f), 0.12f, Color.white, TextAnchor.UpperLeft);
            AddText(parent, "Aucun cout, timer, reward", new Vector3(rect.x + 0.18f, rect.y + 0.24f, 0.80f), 0.12f, new Color(1f, 0.90f, 0.55f), TextAnchor.UpperLeft);
        }

        private static void AddAccessibilityCallout(Transform parent, Rect rect)
        {
            AddRect(parent, "Accessibility Callout", RectCenter(rect, 0.68f), new Vector2(rect.width, rect.height), new Color(0.20f, 0.12f, 0.060f, 0.96f), 0.68f);
            AddText(parent, "Vue accessibilite", new Vector3(rect.x + 0.20f, rect.y + rect.height - 0.34f, 0.82f), 0.18f, Color.white, TextAnchor.UpperLeft);
            AddText(parent, "Contraste eleve", new Vector3(rect.x + 0.20f, rect.y + rect.height - 0.76f, 0.82f), 0.14f, Color.white, TextAnchor.UpperLeft);
            AddText(parent, "Labels + icones + contours", new Vector3(rect.x + 0.20f, rect.y + rect.height - 1.10f, 0.82f), 0.14f, Color.white, TextAnchor.UpperLeft);
            AddText(parent, "Etats non dependants couleur", new Vector3(rect.x + 0.20f, rect.y + rect.height - 1.44f, 0.82f), 0.14f, Color.white, TextAnchor.UpperLeft);
            AddText(parent, "Preview serveur visible", new Vector3(rect.x + 0.20f, rect.y + 0.26f, 0.82f), 0.14f, new Color(1f, 0.94f, 0.55f), TextAnchor.UpperLeft);
        }

        private static void AddShotBadge(Transform parent, ProductShot shot, Rect rect)
        {
            AddRect(parent, "Shot Badge", RectCenter(rect, 0.90f), new Vector2(rect.width, rect.height), new Color(0.18f, 0.11f, 0.055f, 0.82f), 0.90f);
            AddText(parent, "BEE-601-620 / " + shot.Id, new Vector3(rect.x + 0.14f, rect.y + rect.height - 0.19f, 0.98f), 0.12f, new Color(1f, 0.84f, 0.25f), TextAnchor.UpperLeft);
            AddText(parent, "Ruche premium preview locale - aucune action officielle", new Vector3(rect.x + 0.14f, rect.y + 0.16f, 0.98f), 0.075f, Color.white, TextAnchor.UpperLeft);
        }

        private static void AddDecor(Transform parent, float worldHalfWidth, float worldHalfHeight, bool portrait)
        {
            AddSoftCircle(parent, "Shrub Left", new Vector3(-worldHalfWidth + 0.9f, worldHalfHeight - 1.0f, 0.04f), 0.35f, new Color(0.18f, 0.34f, 0.18f));
            AddSoftCircle(parent, "Shrub Right", new Vector3(worldHalfWidth - 1.1f, -worldHalfHeight + 1.15f, 0.04f), 0.32f, new Color(0.19f, 0.35f, 0.18f));
            AddSoftCircle(parent, "Honey Light A", new Vector3(worldHalfWidth - 1.4f, worldHalfHeight - 1.0f, 0.04f), 0.16f, new Color(1f, 0.62f, 0.10f));
            if (!portrait)
            {
                AddSoftCircle(parent, "Honey Light B", new Vector3(worldHalfWidth - 2.0f, worldHalfHeight - 0.7f, 0.04f), 0.12f, new Color(1f, 0.78f, 0.18f));
            }
        }

        private static void AddIconSheet(Transform parent, Rect rect)
        {
            AddRect(parent, "Icon Sheet Panel", RectCenter(rect, 0.62f), new Vector2(rect.width, rect.height), new Color(0.20f, 0.12f, 0.06f, 0.96f), 0.62f);
            AddText(parent, "Sheet premium 50+ icones", new Vector3(rect.x + 0.22f, rect.y + rect.height - 0.35f, 0.82f), 0.18f, new Color(1f, 0.84f, 0.25f), TextAnchor.UpperLeft);
            AddText(parent, "Resources | Zones | Buildings | States | Navigation | Social | World | Feedback | Non-claims | Empty", new Vector3(rect.x + 0.22f, rect.y + rect.height - 0.72f, 0.82f), 0.095f, new Color(1f, 0.92f, 0.72f), TextAnchor.UpperLeft);

            string[] categories = { "RES", "ZONE", "BLDG", "STAT", "NAV", "SOC", "WRLD", "FDBK", "NC", "EMPTY" };
            string[] names = {
                "Honey", "Wax", "Pollen", "Bees", "Cap",
                "Nursery", "Reserve", "Defense", "Research", "WaxPress",
                "Queen", "Store", "Barrack", "Academy", "Archive",
                "Selected", "Locked", "Preview", "Server", "Alert",
                "Hive", "World", "Alliance", "Inbox", "More",
                "Officer", "Recruit", "Diplomat", "Pact", "Shield",
                "Explore", "Event", "Trade", "Route", "Fog",
                "Press", "Pulse", "Inspect", "Back", "Close",
                "Local", "Future", "NoLive", "NoSync", "ServerReq",
                "Empty", "FutureRoom", "Disabled", "Help", "Reserve" };
            IconKind[] icons = {
                IconKind.HoneyDrop, IconKind.WaxBlock, IconKind.Pollen, IconKind.Bee, IconKind.Capacity,
                IconKind.Nursery, IconKind.HoneyVault, IconKind.Defense, IconKind.Research, IconKind.WaxPress,
                IconKind.Queen, IconKind.HoneyVault, IconKind.Defense, IconKind.Research, IconKind.Archive,
                IconKind.Selected, IconKind.Locked, IconKind.Preview, IconKind.ServerRequired, IconKind.Alert,
                IconKind.Hive, IconKind.World, IconKind.Alliance, IconKind.Inbox, IconKind.More,
                IconKind.Officer, IconKind.Bee, IconKind.Diplomat, IconKind.Alliance, IconKind.Defense,
                IconKind.Explore, IconKind.Event, IconKind.Trade, IconKind.Route, IconKind.Fog,
                IconKind.Press, IconKind.Pulse, IconKind.Inspect, IconKind.Back, IconKind.Close,
                IconKind.Preview, IconKind.FutureRoom, IconKind.NoLive, IconKind.NoSync, IconKind.ServerRequired,
                IconKind.Empty, IconKind.FutureRoom, IconKind.Disabled, IconKind.Help, IconKind.Reserve };
            CellState[] variants = { CellState.Occupied, CellState.Selected, CellState.Locked, CellState.Server, CellState.Future };

            const int columns = 10;
            float cellW = (rect.width - 0.56f) / columns;
            float startY = rect.y + rect.height - 1.18f;
            for (int i = 0; i < names.Length; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Rect cell = new Rect(rect.x + 0.28f + col * cellW, startY - row * 1.02f - 0.82f, cellW - 0.08f, 0.88f);
                Color panel = row % 2 == 0 ? new Color(0.30f, 0.19f, 0.08f, 0.96f) : new Color(0.24f, 0.15f, 0.065f, 0.96f);
                AddRect(parent, "Icon Sheet Cell " + i, RectCenter(cell, 0.72f), new Vector2(cell.width, cell.height), panel, 0.72f);
                AddPremiumIcon(parent, "Icon Sheet Glyph " + i, new Vector2(cell.x + cell.width * 0.50f, cell.y + 0.55f), 0.42f, icons[i], variants[i % variants.Length], 0.88f);
                AddText(parent, categories[Math.Min(categories.Length - 1, i / 5)], new Vector3(cell.x + 0.06f, cell.y + 0.78f, 0.86f), 0.055f, new Color(0.76f, 0.86f, 1f), TextAnchor.UpperLeft);
                AddText(parent, names[i], new Vector3(cell.x + 0.06f, cell.y + 0.20f, 0.86f), 0.060f, new Color(1f, 0.92f, 0.72f), TextAnchor.UpperLeft);
            }
        }

        private static Color IconCategoryColor(int index)
        {
            Color[] colors =
            {
                new Color(1f, 0.70f, 0.16f), new Color(0.96f, 0.82f, 0.44f), new Color(0.76f, 0.48f, 0.18f),
                new Color(0.74f, 0.86f, 0.96f), new Color(0.90f, 0.58f, 0.18f), new Color(0.52f, 0.72f, 0.95f),
                new Color(0.48f, 0.64f, 0.34f), new Color(1f, 0.85f, 0.30f), new Color(0.70f, 0.82f, 0.95f),
                new Color(0.62f, 0.55f, 0.46f)
            };
            return colors[(index / 5) % colors.Length];
        }

        private static void AddResponsiveMatrix(Transform parent, Rect rect)
        {
            AddRect(parent, "Responsive Matrix Panel", RectCenter(rect, 0.62f), new Vector2(rect.width, rect.height), new Color(0.20f, 0.12f, 0.06f, 0.96f), 0.62f);
            AddText(parent, "Preuve responsive visuelle + mesures", new Vector3(rect.x + 0.22f, rect.y + rect.height - 0.36f, 0.84f), 0.18f, new Color(1f, 0.84f, 0.25f), TextAnchor.UpperLeft);
            string[] devices = { "Desktop 16:9", "Tablet 4:3", "Phone 9:16" };
            Vector2[] sizes = { new Vector2(3.2f, 1.8f), new Vector2(2.35f, 1.75f), new Vector2(1.28f, 2.28f) };
            Vector2[] positions =
            {
                new Vector2(rect.x + 2.10f, rect.y + 4.25f),
                new Vector2(rect.x + 6.15f, rect.y + 4.25f),
                new Vector2(rect.x + 10.10f, rect.y + 4.00f)
            };

            for (int i = 0; i < devices.Length; i++)
            {
                AddDeviceProof(parent, devices[i], positions[i], sizes[i], i == 2);
            }

            string[] measurements = { "HUD 100% visible", "Rail cible 48px", "Panel <= 30%", "Ruche lisible", "Badge local", "No official claim" };
            for (int i = 0; i < measurements.Length; i++)
            {
                Rect row = new Rect(rect.x + 0.40f + (i % 3) * 4.15f, rect.y + 1.05f - (i / 3) * 0.52f, 3.65f, 0.38f);
                AddRect(parent, "Responsive Measure " + i, RectCenter(row, 0.82f), new Vector2(row.width, row.height), new Color(0.29f, 0.18f, 0.075f, 0.94f), 0.82f);
                AddPremiumIcon(parent, "Responsive Check Icon " + i, new Vector2(row.x + 0.20f, row.y + row.height * 0.50f), 0.15f, IconKind.Selected, CellState.Selected, 0.92f);
                AddText(parent, measurements[i], new Vector3(row.x + 0.42f, row.y + 0.24f, 0.94f), 0.072f, Color.white, TextAnchor.UpperLeft);
            }
            AddText(parent, "Reserve: captures simulees Unity, appareil reel a faire cote QA.", new Vector3(rect.x + 0.34f, rect.y + 0.28f, 0.84f), 0.085f, new Color(0.74f, 0.88f, 1f), TextAnchor.UpperLeft);
        }

        private static void AddDeviceProof(Transform parent, string label, Vector2 center, Vector2 size, bool phone)
        {
            AddRect(parent, "Device Frame " + label, new Vector3(center.x, center.y, 0.72f), size + new Vector2(0.18f, 0.18f), new Color(0.06f, 0.04f, 0.025f, 0.98f), 0.72f);
            AddRect(parent, "Device Screen " + label, new Vector3(center.x, center.y, 0.76f), size, new Color(0.16f, 0.20f, 0.13f, 0.98f), 0.76f);
            AddRect(parent, "Device HUD " + label, new Vector3(center.x, center.y + size.y * 0.38f, 0.84f), new Vector2(size.x * 0.82f, size.y * 0.12f), new Color(0.34f, 0.20f, 0.07f), 0.84f);
            AddHex(parent, "Device Hive " + label, new Vector2(center.x, center.y + (phone ? size.y * 0.03f : -size.y * 0.04f)), size.x * (phone ? 0.18f : 0.13f), new Color(1f, 0.64f, 0.10f), 0.88f);
            AddHex(parent, "Device Hive B " + label, new Vector2(center.x - size.x * 0.13f, center.y - size.y * 0.05f), size.x * 0.10f, new Color(0.78f, 0.40f, 0.07f), 0.89f);
            AddHex(parent, "Device Hive C " + label, new Vector2(center.x + size.x * 0.13f, center.y - size.y * 0.05f), size.x * 0.10f, new Color(0.88f, 0.50f, 0.08f), 0.90f);
            AddRect(parent, "Device Rail " + label, new Vector3(center.x, center.y - size.y * 0.39f, 0.84f), new Vector2(size.x * 0.74f, size.y * 0.11f), new Color(0.24f, 0.14f, 0.06f), 0.84f);
            AddText(parent, label, new Vector3(center.x - size.x * 0.44f, center.y - size.y * 0.62f, 0.92f), 0.085f, new Color(1f, 0.90f, 0.62f), TextAnchor.UpperLeft);
        }

        private static void AddFallbackManifest(Transform parent, Rect rect)
        {
            AddRect(parent, "Fallback Manifest Panel", RectCenter(rect, 0.62f), new Vector2(rect.width, rect.height), new Color(0.20f, 0.12f, 0.06f, 0.96f), 0.62f);
            AddText(parent, "Manifest assets premium / fallbacks declares", new Vector3(rect.x + 0.22f, rect.y + rect.height - 0.36f, 0.84f), 0.18f, new Color(1f, 0.84f, 0.25f), TextAnchor.UpperLeft);
            string[] rows = { "IconFallbacks", "ProceduralCells", "PlaceholderPanels", "TemporaryTextures", "MissingVariants", "LowFidelityBadges" };
            string[] status = { "Accepted preview", "Reduced by landmarks", "Replaced chrome", "Accepted preview", "Reserved", "Replaced badges" };
            for (int i = 0; i < rows.Length; i++)
            {
                float y = rect.y + rect.height - 1.10f - i * 0.82f;
                AddRect(parent, "Fallback Row " + i, new Vector3(rect.x + rect.width * 0.5f, y - 0.20f, 0.72f), new Vector2(rect.width - 0.44f, 0.62f), i % 2 == 0 ? new Color(0.30f, 0.19f, 0.08f) : new Color(0.24f, 0.15f, 0.065f), 0.72f);
                AddText(parent, rows[i], new Vector3(rect.x + 0.34f, y, 0.84f), 0.12f, Color.white, TextAnchor.UpperLeft);
                AddText(parent, status[i], new Vector3(rect.x + 4.45f, y, 0.84f), 0.11f, i == 4 ? new Color(1f, 0.78f, 0.28f) : new Color(0.70f, 0.92f, 0.70f), TextAnchor.UpperLeft);
            }
            AddText(parent, "Non-claim : aucune construction, achat, reward, synchro ou progression officielle.", new Vector3(rect.x + 0.34f, rect.y + 0.42f, 0.84f), 0.10f, new Color(0.74f, 0.88f, 1f), TextAnchor.UpperLeft);
        }

        private static void AddZoneRecognitionStrip(Transform parent, Rect rect)
        {
            AddRect(parent, "Zone Recognition Panel", RectCenter(rect, 0.66f), new Vector2(rect.width, rect.height), new Color(0.18f, 0.095f, 0.038f, 0.95f), 0.66f);
            AddText(parent, "Zones sans labels", new Vector3(rect.x + 0.22f, rect.y + rect.height - 0.36f, 0.86f), 0.15f, new Color(1f, 0.84f, 0.25f), TextAnchor.UpperLeft);
            IconKind[] icons = { IconKind.Nursery, IconKind.HoneyVault, IconKind.Defense, IconKind.Research, IconKind.WaxPress, IconKind.Alliance };
            CellState[] states = { CellState.Occupied, CellState.Occupied, CellState.Locked, CellState.Server, CellState.Occupied, CellState.Future };
            for (int i = 0; i < icons.Length; i++)
            {
                float y = rect.y + rect.height - 0.96f - i * 0.82f;
                AddRect(parent, "Zone Strip Cell " + i, new Vector3(rect.x + rect.width * 0.50f, y - 0.18f, 0.76f), new Vector2(rect.width - 0.42f, 0.58f), new Color(0.30f, 0.17f, 0.065f, 0.92f), 0.76f);
                AddPremiumIcon(parent, "Zone Strip Icon " + i, new Vector2(rect.x + 0.58f, y - 0.02f), 0.44f, icons[i], states[i], 0.95f);
                AddHex(parent, "Zone Strip Token " + i, new Vector2(rect.x + rect.width - 0.42f, y - 0.02f), 0.16f, StateTokenColor(states[i]), 0.94f);
            }
            AddText(parent, "Lecture attendue par silhouette, pas par texte.", new Vector3(rect.x + 0.22f, rect.y + 0.28f, 0.86f), 0.075f, new Color(0.78f, 0.94f, 1f), TextAnchor.UpperLeft);
        }

        private static void AddNonClaimBadgeBoard(Transform parent, Rect rect)
        {
            AddRect(parent, "Non Claim Panel", RectCenter(rect, 0.62f), new Vector2(rect.width, rect.height), new Color(0.18f, 0.095f, 0.038f, 0.96f), 0.62f);
            AddText(parent, "Badges preview premium - aucune promesse officielle", new Vector3(rect.x + 0.28f, rect.y + rect.height - 0.42f, 0.86f), 0.18f, new Color(1f, 0.84f, 0.25f), TextAnchor.UpperLeft);
            string[] copy = { "Valeurs locales", "Aucun achat", "Aucune construction", "Aucun reward", "Pas de synchro", "Serveur futur" };
            IconKind[] icons = { IconKind.Preview, IconKind.NoLive, IconKind.WaxPress, IconKind.Reserve, IconKind.NoSync, IconKind.ServerRequired };
            for (int i = 0; i < copy.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                Rect badge = new Rect(rect.x + 0.55f + col * 4.05f, rect.y + rect.height - 1.70f - row * 1.58f, 3.35f, 1.06f);
                AddRect(parent, "Non Claim Badge Shadow " + i, RectCenter(new Rect(badge.x + 0.05f, badge.y - 0.05f, badge.width, badge.height), 0.74f), new Vector2(badge.width, badge.height), new Color(0.05f, 0.025f, 0.010f, 0.72f), 0.74f);
                AddRect(parent, "Non Claim Badge " + i, RectCenter(badge, 0.78f), new Vector2(badge.width, badge.height), new Color(0.24f, 0.15f, 0.060f, 0.96f), 0.78f);
                AddPremiumIcon(parent, "Non Claim Icon " + i, new Vector2(badge.x + 0.46f, badge.y + badge.height * 0.55f), 0.46f, icons[i], i == 5 ? CellState.Server : CellState.Occupied, 0.94f);
                AddText(parent, copy[i], new Vector3(badge.x + 0.92f, badge.y + 0.68f, 0.96f), 0.105f, Color.white, TextAnchor.UpperLeft);
                AddText(parent, "preview locale", new Vector3(badge.x + 0.92f, badge.y + 0.34f, 0.96f), 0.075f, new Color(0.78f, 0.94f, 1f), TextAnchor.UpperLeft);
            }
            AddText(parent, "Ces badges sont decoratifs et informatifs: aucune economie live, progression, achat ou sync serveur.", new Vector3(rect.x + 0.36f, rect.y + 0.46f, 0.86f), 0.092f, new Color(1f, 0.90f, 0.58f), TextAnchor.UpperLeft);
        }

        private static Color StateTokenColor(CellState state)
        {
            switch (state)
            {
                case CellState.Selected: return new Color(1f, 0.96f, 0.22f);
                case CellState.Locked: return new Color(0.10f, 0.07f, 0.04f);
                case CellState.Server: return new Color(0.52f, 0.82f, 1f);
                case CellState.Future: return new Color(0.82f, 0.70f, 0.45f);
                default: return new Color(1f, 0.70f, 0.16f);
            }
        }

        private static void AddWaxTexture(Transform parent, Vector2 center, float radius, string id, CellState state)
        {
            Color vein = state == CellState.Locked ? new Color(0.20f, 0.13f, 0.07f, 0.88f) : new Color(1f, 0.64f, 0.16f, 0.58f);
            AddRect(parent, "Wax Vein A " + id, new Vector3(center.x - radius * 0.18f, center.y + radius * 0.20f, 0.36f), new Vector2(radius * 0.46f, radius * 0.045f), vein, 0.36f);
            AddRect(parent, "Wax Vein B " + id, new Vector3(center.x + radius * 0.18f, center.y - radius * 0.10f, 0.37f), new Vector2(radius * 0.38f, radius * 0.040f), vein, 0.37f);
            AddSoftCircle(parent, "Honey Specular " + id, new Vector3(center.x - radius * 0.26f, center.y + radius * 0.28f, 0.38f), radius * 0.055f, new Color(1f, 0.92f, 0.42f, 0.76f));
            AddSoftCircle(parent, "Honey Grain " + id, new Vector3(center.x + radius * 0.30f, center.y + radius * 0.05f, 0.39f), radius * 0.035f, new Color(0.55f, 0.26f, 0.04f, 0.55f));
        }

        private static float LandmarkSize(IconKind icon)
        {
            switch (icon)
            {
                case IconKind.Queen: return 0.72f;
                case IconKind.HoneyVault:
                case IconKind.Defense:
                case IconKind.Research:
                case IconKind.WaxPress:
                case IconKind.Nursery: return 0.58f;
                case IconKind.Locked:
                case IconKind.ServerRequired:
                case IconKind.FutureRoom: return 0.50f;
                default: return 0.46f;
            }
        }

        private static void AddPremiumIcon(Transform parent, string name, Vector2 center, float size, IconKind icon, CellState variant, float z)
        {
            Color shadow = new Color(0.05f, 0.025f, 0.010f, 0.70f);
            Color rim = variant == CellState.Locked ? new Color(0.28f, 0.19f, 0.11f) : variant == CellState.Server ? new Color(0.58f, 0.78f, 0.92f) : new Color(1f, 0.72f, 0.16f);
            Color fill = IconFill(icon, variant);
            AddSoftCircle(parent, name + " Shadow", new Vector3(center.x + size * 0.05f, center.y - size * 0.07f, z), size * 0.58f, shadow);
            AddSoftCircle(parent, name + " Rim", new Vector3(center.x, center.y, z + 0.01f), size * 0.50f, rim);
            AddSoftCircle(parent, name + " Fill", new Vector3(center.x, center.y, z + 0.02f), size * 0.40f, fill);

            switch (icon)
            {
                case IconKind.HoneyDrop:
                    AddSoftCircle(parent, name + " Drop Top", new Vector3(center.x, center.y + size * 0.08f, z + 0.08f), size * 0.18f, new Color(1f, 0.86f, 0.20f));
                    AddTriangle(parent, name + " Drop Tip", center + new Vector2(0f, -size * 0.10f), size * 0.25f, new Color(1f, 0.64f, 0.08f), z + 0.09f, 180f);
                    break;
                case IconKind.WaxBlock:
                    AddHex(parent, name + " Wax Hex", center, size * 0.25f, new Color(1f, 0.74f, 0.18f), z + 0.09f);
                    AddRect(parent, name + " Wax Shine", new Vector3(center.x - size * 0.03f, center.y + size * 0.08f, z + 0.10f), new Vector2(size * 0.25f, size * 0.04f), new Color(1f, 0.95f, 0.42f), z + 0.10f);
                    break;
                case IconKind.Pollen:
                    AddSoftCircle(parent, name + " Pollen A", new Vector3(center.x - size * 0.11f, center.y, z + 0.09f), size * 0.12f, new Color(1f, 0.92f, 0.36f));
                    AddSoftCircle(parent, name + " Pollen B", new Vector3(center.x + size * 0.10f, center.y + size * 0.03f, z + 0.10f), size * 0.11f, new Color(0.96f, 0.74f, 0.18f));
                    AddSoftCircle(parent, name + " Pollen C", new Vector3(center.x, center.y - size * 0.12f, z + 0.11f), size * 0.10f, new Color(0.76f, 0.88f, 0.30f));
                    break;
                case IconKind.Bee:
                    AddSoftCircle(parent, name + " Bee Body", new Vector3(center.x, center.y, z + 0.09f), size * 0.15f, new Color(0.13f, 0.08f, 0.03f));
                    AddRect(parent, name + " Bee Stripe", new Vector3(center.x, center.y, z + 0.10f), new Vector2(size * 0.06f, size * 0.26f), new Color(1f, 0.78f, 0.12f), z + 0.10f);
                    AddSoftCircle(parent, name + " Bee Wing L", new Vector3(center.x - size * 0.13f, center.y + size * 0.10f, z + 0.08f), size * 0.11f, new Color(0.78f, 0.92f, 1f, 0.75f));
                    AddSoftCircle(parent, name + " Bee Wing R", new Vector3(center.x + size * 0.13f, center.y + size * 0.10f, z + 0.08f), size * 0.11f, new Color(0.78f, 0.92f, 1f, 0.75f));
                    break;
                case IconKind.Capacity:
                    AddRect(parent, name + " Gauge Back", new Vector3(center.x, center.y - size * 0.02f, z + 0.09f), new Vector2(size * 0.34f, size * 0.12f), new Color(0.18f, 0.10f, 0.04f), z + 0.09f);
                    AddRect(parent, name + " Gauge Fill", new Vector3(center.x - size * 0.04f, center.y - size * 0.02f, z + 0.10f), new Vector2(size * 0.22f, size * 0.08f), new Color(0.76f, 0.95f, 0.34f), z + 0.10f);
                    break;
                case IconKind.Nursery:
                    AddSoftCircle(parent, name + " Egg A", new Vector3(center.x - size * 0.11f, center.y - size * 0.02f, z + 0.09f), size * 0.12f, new Color(1f, 0.90f, 0.58f));
                    AddSoftCircle(parent, name + " Egg B", new Vector3(center.x + size * 0.08f, center.y + size * 0.02f, z + 0.10f), size * 0.15f, new Color(1f, 0.82f, 0.36f));
                    AddSoftCircle(parent, name + " Larva Glow", new Vector3(center.x, center.y - size * 0.13f, z + 0.11f), size * 0.08f, new Color(0.82f, 0.96f, 0.52f));
                    break;
                case IconKind.HoneyVault:
                    AddRect(parent, name + " Jar Body", new Vector3(center.x, center.y - size * 0.03f, z + 0.09f), new Vector2(size * 0.28f, size * 0.30f), new Color(0.74f, 0.38f, 0.08f), z + 0.09f);
                    AddRect(parent, name + " Jar Honey", new Vector3(center.x, center.y - size * 0.07f, z + 0.10f), new Vector2(size * 0.22f, size * 0.13f), new Color(1f, 0.72f, 0.10f), z + 0.10f);
                    AddRect(parent, name + " Jar Lid", new Vector3(center.x, center.y + size * 0.16f, z + 0.11f), new Vector2(size * 0.34f, size * 0.08f), new Color(1f, 0.82f, 0.22f), z + 0.11f);
                    break;
                case IconKind.Defense:
                    AddTriangle(parent, name + " Shield", center + new Vector2(0f, -size * 0.02f), size * 0.38f, new Color(0.70f, 0.43f, 0.16f), z + 0.09f, 180f);
                    AddTriangle(parent, name + " Stinger", center + new Vector2(size * 0.13f, size * 0.05f), size * 0.22f, new Color(1f, 0.86f, 0.28f), z + 0.10f, -45f);
                    break;
                case IconKind.Research:
                    AddRect(parent, name + " Flask Neck", new Vector3(center.x, center.y + size * 0.12f, z + 0.09f), new Vector2(size * 0.10f, size * 0.22f), new Color(0.72f, 0.96f, 0.90f), z + 0.09f);
                    AddTriangle(parent, name + " Flask Bowl", center + new Vector2(0f, -size * 0.08f), size * 0.34f, new Color(0.48f, 0.86f, 0.70f), z + 0.10f, 0f);
                    AddSoftCircle(parent, name + " Crystal", new Vector3(center.x + size * 0.16f, center.y + size * 0.10f, z + 0.11f), size * 0.08f, new Color(0.90f, 1f, 0.58f));
                    break;
                case IconKind.WaxPress:
                    AddRect(parent, name + " Press Top", new Vector3(center.x, center.y + size * 0.12f, z + 0.09f), new Vector2(size * 0.36f, size * 0.10f), new Color(0.42f, 0.22f, 0.08f), z + 0.09f);
                    AddRect(parent, name + " Press Base", new Vector3(center.x, center.y - size * 0.10f, z + 0.10f), new Vector2(size * 0.34f, size * 0.14f), new Color(0.95f, 0.58f, 0.12f), z + 0.10f);
                    AddSoftCircle(parent, name + " Wax Drop", new Vector3(center.x + size * 0.18f, center.y - size * 0.18f, z + 0.11f), size * 0.07f, new Color(1f, 0.84f, 0.20f));
                    break;
                case IconKind.Queen:
                    AddTriangle(parent, name + " Crown A", center + new Vector2(-size * 0.12f, size * 0.05f), size * 0.18f, new Color(1f, 0.84f, 0.16f), z + 0.10f, 0f);
                    AddTriangle(parent, name + " Crown B", center + new Vector2(0f, size * 0.11f), size * 0.22f, new Color(1f, 0.94f, 0.32f), z + 0.11f, 0f);
                    AddTriangle(parent, name + " Crown C", center + new Vector2(size * 0.12f, size * 0.05f), size * 0.18f, new Color(1f, 0.84f, 0.16f), z + 0.10f, 0f);
                    AddRect(parent, name + " Crown Band", new Vector3(center.x, center.y - size * 0.10f, z + 0.12f), new Vector2(size * 0.42f, size * 0.12f), new Color(0.42f, 0.16f, 0.04f), z + 0.12f);
                    break;
                case IconKind.Locked:
                    AddRect(parent, name + " Lock Body", new Vector3(center.x, center.y - size * 0.04f, z + 0.09f), new Vector2(size * 0.30f, size * 0.22f), new Color(0.09f, 0.06f, 0.035f), z + 0.09f);
                    AddSoftCircle(parent, name + " Lock Loop", new Vector3(center.x, center.y + size * 0.13f, z + 0.10f), size * 0.14f, new Color(0.18f, 0.12f, 0.07f));
                    break;
                case IconKind.ServerRequired:
                    AddHex(parent, name + " Server Badge", center, size * 0.22f, new Color(0.72f, 0.90f, 1f), z + 0.09f);
                    AddRect(parent, name + " Server Slash", new Vector3(center.x, center.y, z + 0.10f), new Vector2(size * 0.32f, size * 0.05f), new Color(0.20f, 0.30f, 0.36f), z + 0.10f);
                    break;
                case IconKind.FutureRoom:
                    AddHex(parent, name + " Future Hex", center, size * 0.24f, new Color(0.82f, 0.70f, 0.45f, 0.72f), z + 0.09f);
                    AddSoftCircle(parent, name + " Future Dot", new Vector3(center.x, center.y, z + 0.10f), size * 0.07f, new Color(1f, 0.92f, 0.46f));
                    break;
                case IconKind.Selected:
                    AddHex(parent, name + " Selected Hex", center, size * 0.28f, new Color(1f, 0.95f, 0.22f), z + 0.09f);
                    AddSoftCircle(parent, name + " Selected Dot", new Vector3(center.x, center.y, z + 0.10f), size * 0.08f, new Color(0.35f, 0.16f, 0.04f));
                    break;
                case IconKind.Close:
                    AddRect(parent, name + " Close A", new Vector3(center.x, center.y, z + 0.10f), new Vector2(size * 0.34f, size * 0.06f), new Color(1f, 0.90f, 0.62f), z + 0.10f);
                    AddRect(parent, name + " Close B", new Vector3(center.x, center.y, z + 0.11f), new Vector2(size * 0.06f, size * 0.34f), new Color(1f, 0.90f, 0.62f), z + 0.11f);
                    break;
                case IconKind.More:
                    AddSoftCircle(parent, name + " More A", new Vector3(center.x - size * 0.13f, center.y, z + 0.09f), size * 0.045f, Color.white);
                    AddSoftCircle(parent, name + " More B", new Vector3(center.x, center.y, z + 0.10f), size * 0.045f, Color.white);
                    AddSoftCircle(parent, name + " More C", new Vector3(center.x + size * 0.13f, center.y, z + 0.11f), size * 0.045f, Color.white);
                    break;
                case IconKind.Hive:
                    AddHex(parent, name + " Hive Mark A", center + new Vector2(-size * 0.08f, 0f), size * 0.16f, new Color(1f, 0.82f, 0.20f), z + 0.09f);
                    AddHex(parent, name + " Hive Mark B", center + new Vector2(size * 0.10f, size * 0.02f), size * 0.16f, new Color(0.96f, 0.56f, 0.08f), z + 0.10f);
                    break;
                case IconKind.World:
                    AddSoftCircle(parent, name + " World", new Vector3(center.x, center.y, z + 0.09f), size * 0.18f, new Color(0.30f, 0.62f, 0.34f));
                    AddRect(parent, name + " World Meridian", new Vector3(center.x, center.y, z + 0.10f), new Vector2(size * 0.05f, size * 0.34f), new Color(1f, 0.86f, 0.26f), z + 0.10f);
                    break;
                case IconKind.Alliance:
                    AddTriangle(parent, name + " Alliance Banner", center + new Vector2(0f, size * 0.02f), size * 0.32f, new Color(0.95f, 0.68f, 0.14f), z + 0.09f, 180f);
                    AddSoftCircle(parent, name + " Alliance Gem", new Vector3(center.x, center.y + size * 0.02f, z + 0.10f), size * 0.07f, new Color(0.52f, 0.82f, 1f));
                    break;
                case IconKind.Inbox:
                    AddRect(parent, name + " Inbox Box", new Vector3(center.x, center.y - size * 0.03f, z + 0.09f), new Vector2(size * 0.34f, size * 0.22f), new Color(0.52f, 0.82f, 1f), z + 0.09f);
                    AddTriangle(parent, name + " Inbox Flap", center + new Vector2(0f, size * 0.04f), size * 0.24f, new Color(0.18f, 0.30f, 0.38f), z + 0.10f, 180f);
                    break;
                case IconKind.Archive:
                    AddRect(parent, name + " Archive", new Vector3(center.x, center.y, z + 0.09f), new Vector2(size * 0.30f, size * 0.28f), new Color(1f, 0.72f, 0.12f), z + 0.09f);
                    AddRect(parent, name + " Archive Lid", new Vector3(center.x, center.y + size * 0.17f, z + 0.10f), new Vector2(size * 0.38f, size * 0.08f), new Color(0.38f, 0.20f, 0.07f), z + 0.10f);
                    break;
                case IconKind.Alert:
                    AddTriangle(parent, name + " Alert Tri", center, size * 0.34f, new Color(1f, 0.86f, 0.16f), z + 0.09f, 0f);
                    AddRect(parent, name + " Alert Mark", new Vector3(center.x, center.y - size * 0.02f, z + 0.10f), new Vector2(size * 0.04f, size * 0.18f), new Color(0.24f, 0.10f, 0.04f), z + 0.10f);
                    break;
                case IconKind.Officer:
                    AddTriangle(parent, name + " Officer Crest", center + new Vector2(0f, size * 0.02f), size * 0.30f, new Color(1f, 0.82f, 0.20f), z + 0.09f, 0f);
                    AddRect(parent, name + " Officer Band", new Vector3(center.x, center.y - size * 0.14f, z + 0.10f), new Vector2(size * 0.32f, size * 0.07f), new Color(0.32f, 0.16f, 0.05f), z + 0.10f);
                    break;
                case IconKind.Diplomat:
                    AddSoftCircle(parent, name + " Diplomat Seal", new Vector3(center.x - size * 0.08f, center.y, z + 0.09f), size * 0.12f, new Color(0.72f, 0.90f, 1f));
                    AddSoftCircle(parent, name + " Diplomat Seal B", new Vector3(center.x + size * 0.08f, center.y, z + 0.10f), size * 0.12f, new Color(1f, 0.82f, 0.20f));
                    break;
                case IconKind.Explore:
                    AddSoftCircle(parent, name + " Compass", new Vector3(center.x, center.y, z + 0.09f), size * 0.17f, new Color(1f, 0.76f, 0.16f));
                    AddTriangle(parent, name + " Compass Needle", center + new Vector2(size * 0.02f, size * 0.02f), size * 0.22f, new Color(0.20f, 0.10f, 0.04f), z + 0.10f, -25f);
                    break;
                case IconKind.Event:
                    AddSoftCircle(parent, name + " Event Sun", new Vector3(center.x, center.y, z + 0.09f), size * 0.17f, new Color(1f, 0.82f, 0.20f));
                    AddSoftCircle(parent, name + " Event Center", new Vector3(center.x, center.y, z + 0.10f), size * 0.07f, new Color(0.32f, 0.16f, 0.04f));
                    break;
                case IconKind.Trade:
                    AddRect(parent, name + " Trade A", new Vector3(center.x - size * 0.06f, center.y + size * 0.06f, z + 0.09f), new Vector2(size * 0.28f, size * 0.06f), new Color(1f, 0.82f, 0.20f), z + 0.09f);
                    AddRect(parent, name + " Trade B", new Vector3(center.x + size * 0.06f, center.y - size * 0.06f, z + 0.10f), new Vector2(size * 0.28f, size * 0.06f), new Color(0.52f, 0.82f, 1f), z + 0.10f);
                    break;
                case IconKind.Route:
                    AddSoftCircle(parent, name + " Route A", new Vector3(center.x - size * 0.14f, center.y - size * 0.06f, z + 0.09f), size * 0.055f, new Color(1f, 0.82f, 0.20f));
                    AddSoftCircle(parent, name + " Route B", new Vector3(center.x + size * 0.14f, center.y + size * 0.06f, z + 0.10f), size * 0.055f, new Color(1f, 0.82f, 0.20f));
                    AddRect(parent, name + " Route Link", new Vector3(center.x, center.y, z + 0.11f), new Vector2(size * 0.28f, size * 0.045f), new Color(0.52f, 0.82f, 1f), z + 0.11f);
                    break;
                case IconKind.Fog:
                    AddSoftCircle(parent, name + " Fog A", new Vector3(center.x - size * 0.09f, center.y, z + 0.09f), size * 0.10f, new Color(0.78f, 0.86f, 0.80f, 0.78f));
                    AddSoftCircle(parent, name + " Fog B", new Vector3(center.x + size * 0.07f, center.y + size * 0.04f, z + 0.10f), size * 0.12f, new Color(0.62f, 0.72f, 0.68f, 0.78f));
                    break;
                case IconKind.Press:
                    AddRect(parent, name + " Press Finger", new Vector3(center.x, center.y + size * 0.04f, z + 0.09f), new Vector2(size * 0.12f, size * 0.28f), new Color(1f, 0.78f, 0.22f), z + 0.09f);
                    AddSoftCircle(parent, name + " Press Ripple", new Vector3(center.x, center.y - size * 0.13f, z + 0.10f), size * 0.13f, new Color(0.52f, 0.82f, 1f, 0.72f));
                    break;
                case IconKind.Pulse:
                    AddSoftCircle(parent, name + " Pulse Outer", new Vector3(center.x, center.y, z + 0.09f), size * 0.20f, new Color(1f, 0.82f, 0.20f, 0.55f));
                    AddSoftCircle(parent, name + " Pulse Inner", new Vector3(center.x, center.y, z + 0.10f), size * 0.09f, new Color(1f, 0.92f, 0.42f));
                    break;
                case IconKind.Inspect:
                    AddSoftCircle(parent, name + " Inspect Lens", new Vector3(center.x - size * 0.04f, center.y + size * 0.04f, z + 0.09f), size * 0.13f, new Color(0.72f, 0.90f, 1f));
                    AddRect(parent, name + " Inspect Handle", new Vector3(center.x + size * 0.11f, center.y - size * 0.12f, z + 0.10f), new Vector2(size * 0.18f, size * 0.05f), new Color(1f, 0.82f, 0.20f), z + 0.10f);
                    break;
                case IconKind.Back:
                    AddTriangle(parent, name + " Back Arrow", center + new Vector2(-size * 0.04f, 0f), size * 0.28f, new Color(1f, 0.82f, 0.20f), z + 0.09f, 90f);
                    AddRect(parent, name + " Back Tail", new Vector3(center.x + size * 0.08f, center.y, z + 0.10f), new Vector2(size * 0.22f, size * 0.06f), new Color(1f, 0.82f, 0.20f), z + 0.10f);
                    break;
                case IconKind.NoLive:
                case IconKind.NoSync:
                case IconKind.Disabled:
                    AddHex(parent, name + " Blocked Hex", center, size * 0.22f, new Color(0.52f, 0.62f, 0.64f), z + 0.09f);
                    AddRect(parent, name + " Blocked Slash", new Vector3(center.x, center.y, z + 0.10f), new Vector2(size * 0.42f, size * 0.055f), new Color(0.08f, 0.05f, 0.03f), z + 0.10f);
                    break;
                case IconKind.Empty:
                case IconKind.Reserve:
                    AddHex(parent, name + " Empty Outline", center, size * 0.23f, new Color(0.82f, 0.65f, 0.34f, 0.70f), z + 0.09f);
                    AddSoftCircle(parent, name + " Empty Center", new Vector3(center.x, center.y, z + 0.10f), size * 0.06f, new Color(0.18f, 0.10f, 0.04f));
                    break;
                case IconKind.Help:
                    AddSoftCircle(parent, name + " Help Dot", new Vector3(center.x, center.y - size * 0.12f, z + 0.09f), size * 0.04f, Color.white);
                    AddText(parent, "?", new Vector3(center.x - size * 0.07f, center.y + size * 0.11f, z + 0.10f), size * 0.42f, Color.white, TextAnchor.MiddleCenter);
                    break;
                default:
                    AddHex(parent, name + " Generic Mark", center, size * 0.22f, new Color(1f, 0.86f, 0.26f), z + 0.09f);
                    break;
            }

            if (variant == CellState.Locked)
            {
                AddRect(parent, name + " Disabled Stripe", new Vector3(center.x, center.y, z + 0.20f), new Vector2(size * 0.78f, size * 0.05f), new Color(0.05f, 0.035f, 0.02f, 0.86f), z + 0.20f);
            }
        }

        private static Color IconFill(IconKind icon, CellState variant)
        {
            if (variant == CellState.Locked) return new Color(0.22f, 0.16f, 0.10f);
            if (variant == CellState.Server) return new Color(0.22f, 0.36f, 0.42f);
            switch (icon)
            {
                case IconKind.Research: return new Color(0.25f, 0.45f, 0.34f);
                case IconKind.Defense: return new Color(0.36f, 0.19f, 0.08f);
                case IconKind.Alliance:
                case IconKind.World: return new Color(0.20f, 0.30f, 0.16f);
                case IconKind.ServerRequired: return new Color(0.20f, 0.34f, 0.42f);
                default: return new Color(0.44f, 0.24f, 0.07f);
            }
        }

        private static void AddTriangle(Transform parent, string name, Vector2 center, float size, Color color, float z, float rotationDegrees)
        {
            GameObject triangle = new GameObject(name);
            triangle.transform.SetParent(parent, false);
            triangle.transform.localPosition = new Vector3(center.x, center.y, -z);
            triangle.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            MeshFilter filter = triangle.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(0f, size * 0.50f, 0f),
                new Vector3(-size * 0.46f, -size * 0.34f, 0f),
                new Vector3(size * 0.46f, -size * 0.34f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = triangle.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = NewMaterial(color);
        }

        private static void AddIsoWall(Transform parent, string name, Vector2 center, float radius, Color color, int side, float z)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = new Vector3(center.x, center.y, -z);
            MeshFilter filter = wall.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh { name = name + " Mesh" };
            float yScale = 0.62f;
            float drop = radius * 0.30f;
            Vector3 a = new Vector3(side * radius * 0.86f, radius * 0.50f * yScale, 0f);
            Vector3 b = new Vector3(side * radius * 0.86f, -radius * 0.50f * yScale, 0f);
            Vector3 c = new Vector3(side * radius * 0.70f, -radius * 0.50f * yScale - drop, 0f);
            Vector3 d = new Vector3(side * radius * 0.70f, radius * 0.50f * yScale - drop, 0f);
            mesh.vertices = new[] { a, b, c, d };
            mesh.triangles = side > 0 ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = wall.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = NewMaterial(color);
        }

        private static void AddRect(Transform parent, string name, Vector3 center, Vector2 size, Color color, float z)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(center.x, center.y, -z);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            SetMaterial(quad, color);
        }

        private static void AddSoftCircle(Transform parent, string name, Vector3 center, float radius, Color color)
        {
            GameObject circle = new GameObject(name);
            circle.transform.SetParent(parent, false);
            circle.transform.localPosition = new Vector3(center.x, center.y, -center.z);
            MeshFilter filter = circle.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateCircleMesh(radius, 36);
            MeshRenderer renderer = circle.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = NewMaterial(color);
        }

        private static void AddHex(Transform parent, string name, Vector2 center, float radius, Color color, float z)
        {
            GameObject hex = new GameObject(name);
            hex.transform.SetParent(parent, false);
            hex.transform.localPosition = new Vector3(center.x, center.y, -z);
            MeshFilter filter = hex.AddComponent<MeshFilter>();
            filter.sharedMesh = CreateHexMesh(radius);
            MeshRenderer renderer = hex.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = NewMaterial(color);
        }

        private static void AddText(Transform parent, string text, Vector3 position, float size, Color color, TextAnchor anchor)
        {
            GameObject obj = new GameObject("Text " + text);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = new Vector3(position.x, position.y, -position.z);
            TextMesh mesh = obj.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 48;
            mesh.characterSize = size * 0.34f;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Left;
            mesh.color = color;
        }

        private static Mesh CreateHexMesh(float radius)
        {
            Vector3[] vertices = new Vector3[7];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i + 30f);
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.68f, 0f);
            }

            int[] triangles = { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 5, 4, 0, 6, 5, 0, 1, 6 };
            Mesh mesh = new Mesh { name = "Product Hex" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateCircleMesh(float radius, int segments)
        {
            Vector3[] vertices = new Vector3[segments + 1];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }

            int[] triangles = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i == segments - 1 ? 1 : i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            Mesh mesh = new Mesh { name = "Product Circle" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void SetMaterial(GameObject obj, Color color)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = NewMaterial(color);
        }

        private static Material NewMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = color;
            return material;
        }

        private static Vector3 RectCenter(Rect rect, float z)
        {
            return new Vector3(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f, z);
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
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            target.SetPixels32(pixels);
        }

        private static void BlitScaled(Texture2D source, Texture2D target, RectInt rect)
        {
            float sourceAspect = (float)source.width / source.height;
            float targetAspect = (float)rect.width / rect.height;
            int drawWidth = rect.width;
            int drawHeight = rect.height;
            if (sourceAspect > targetAspect) drawHeight = Mathf.RoundToInt(rect.width / sourceAspect);
            else drawWidth = Mathf.RoundToInt(rect.height * sourceAspect);

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
            if (pixels.Length == 0) return new FrameAnalysis(false, texture.width, texture.height, 0, 0d, 0d);

            Color32 first = pixels[0];
            int different = 0;
            int bright = 0;
            int sampled = 0;
            int step = Math.Max(1, pixels.Length / 9000);
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                int delta = Math.Abs(pixel.r - first.r) + Math.Abs(pixel.g - first.g) + Math.Abs(pixel.b - first.b);
                if (delta > 12) different++;
                if (pixel.r + pixel.g + pixel.b > 60) bright++;
                sampled++;
            }

            double variationRatio = sampled == 0 ? 0d : (double)different / sampled;
            double visibleRatio = sampled == 0 ? 0d : (double)bright / sampled;
            return new FrameAnalysis(variationRatio > 0.01d && visibleRatio > 0.05d, texture.width, texture.height, sampled, variationRatio, visibleRatio);
        }

        private static string BuildManifest(IReadOnlyList<CapturedShot> captured, FrameAnalysis contactAnalysis)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# DEMO-051 - BEE-601_620 / BEE-615 Premium Rework Manifest");
            builder.AppendLine();
            builder.AppendLine("Date : 2026-07-10");
            builder.AppendLine();
            builder.AppendLine("## Intention");
            builder.AppendLine();
            builder.AppendLine("Pack de reprise majeure selon UI-032, QA BEE-616 et ARCH-119. Objectif: ruche MMO 2026 premium, mobile-first, avec preuves visibles et non-claims conserves.");
            builder.AppendLine();
            builder.AppendLine("## Reprise UI-032 / QA BEE-616 / ARCH-119");
            builder.AppendLine();
            builder.AppendLine("- Ruche cire/miel avec couches isometriques, profondeur, ombres, glow et texture procedurale.");
            builder.AppendLine("- Iconographie 50+ renforcee par silhouettes composees lisibles en 48x48.");
            builder.AppendLine("- Zones critiques visibles sans labels principaux: nurserie, reserve miel, defense, recherche, transformation, alliance.");
            builder.AppendLine("- HUD, panneaux, mobile portrait, tokens visuels et non-claims refaits en composants premium preview.");
            builder.AppendLine("- BEE-621 reste bloquee jusqu'a validation UI/QA/Architecte.");
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

            builder.AppendLine("- `ContactSheet` : `" + ContactSheetPath + "` ; nonBlank=`" + contactAnalysis.IsNonBlank + "` ; size=`" + contactAnalysis.Width + "x" + contactAnalysis.Height + "`");
            builder.AppendLine();
            builder.AppendLine("## Reserves");
            builder.AppendLine();
            builder.AppendLine("- Couche visuelle de demonstration transitoire, non framework.");
            builder.AppendLine("- Aucune production readiness.");
            builder.AppendLine("- Aucune donnee serveur authoritative.");
            builder.AppendLine("- Validation UI/QA/Architecte requise avant ouverture BEE-621.");
            return builder.ToString();
        }

        private static string ShotPath(ProductShot shot)
        {
            return OutputDirectory + "/BEE-601_620_BEE-615_" + shot.FileStem + ".png";
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private enum ProductShotKind { Overview, Mobile, Detail, Hud, States, FutureLocked, Accessibility, IconSheet, ResponsiveMatrix, FallbackManifest, ZoneLandmarks, NonClaimBadges }
        private enum CellState { Empty, Occupied, Selected, Locked, Server, Future }
        private enum IconKind
        {
            HoneyDrop, WaxBlock, Pollen, Bee, Capacity, Nursery, HoneyVault, Defense, Research, WaxPress,
            Queen, Archive, Selected, Locked, Preview, ServerRequired, Alert, Hive, World, Alliance, Inbox,
            More, Officer, Diplomat, Explore, Event, Trade, Route, Fog, Press, Pulse, Inspect, Back, Close,
            NoLive, NoSync, Empty, Disabled, Help, Reserve, FutureRoom
        }

        private readonly struct ProductShot
        {
            public ProductShot(string id, int width, int height, string fileStem, ProductShotKind kind)
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
            public ProductShotKind Kind { get; }
        }

        private readonly struct CellVisual
        {
            public CellVisual(string id, Vector2 position, CellState state, IconKind icon)
            {
                Id = id;
                Position = position;
                State = state;
                Icon = icon;
            }

            public string Id { get; }
            public Vector2 Position { get; }
            public CellState State { get; }
            public IconKind Icon { get; }
        }

        private readonly struct CapturedShot
        {
            public CapturedShot(ProductShot shot, string path, FrameAnalysis analysis)
            {
                Shot = shot;
                Path = path;
                Analysis = analysis;
            }

            public ProductShot Shot { get; }
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
    }
}
