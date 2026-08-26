using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BeeKingdom.LivingHiveMenu
{
    // Portage du langage visuel "premium panel" du monolithe HiveViewProductUiPresenter.cs
    // vers des Sprite uGUI, pour le rail inférieur de Environment2D5D_SpatialV3 uniquement.
    //
    // Fonctions/couleurs source (citées, jamais référencées — ce fichier ne dépend PAS du
    // monolithe) : DrawPremiumPanel, DrawPanelCorner, DrawPremiumHeaderBand, DrawMenuIcon,
    // DrawBottomRailOrnament, DrawRailDivider, DrawMenuBadge, CreatePremiumTexture (ids
    // panel-outer-grain / etched-line / amber-veil / icon-socket / selected-glow / soft-shadow /
    // progress-fill), CreateHighDefinitionMenuIconTexture (hex badge + hive-nav / world / quests
    // / inbox / alliance / fallback générique — les seules icônes utilisées par le rail).
    //
    // Différence d'approche assumée : le monolithe redessine ces couches en IMGUI à chaque
    // frame (blending GPU). Ici, chaque "look" composite est cuit UNE FOIS dans une texture
    // mise en cache (mêmes formules de couleur/géométrie, compositing manuel "source-over"),
    // puis exposé en Sprite pour UnityEngine.UI.Image — adapté à uGUI, visuellement fidèle.
    public static class LivingHiveMenuVisuals
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();

        // --- Couleurs copiées de DrawBottomRail / DrawIconButton / DrawPremiumPanel ---
        public static readonly Color RailFill = new Color(0.020f, 0.018f, 0.014f, 0.84f);
        public static readonly Color RailBorder = new Color(0.86f, 0.58f, 0.16f, 0.68f);
        public static readonly Color ButtonNormalFill = new Color(0.035f, 0.030f, 0.023f, 0.78f);
        public static readonly Color ButtonNormalBorder = new Color(0.55f, 0.40f, 0.18f, 0.52f);
        public static readonly Color ButtonActiveFill = new Color(0.060f, 0.050f, 0.034f, 0.94f);
        public static readonly Color ButtonActiveBorder = new Color(1f, 0.69f, 0.15f, 0.82f);

        // Copiées de RefreshRailHighlights-equivalent du monolithe (labelStyle dans DrawIconButton).
        public static readonly Color LabelActiveColor = new Color(1f, 0.9f, 0.3f, 1f);
        public static readonly Color LabelInactiveColor = new Color(0.92f, 0.84f, 0.66f, 1f);

        // Copiées de DrawMenuIcon.
        public static readonly Color IconTintActive = Color.white;
        public static readonly Color IconTintInactive = new Color(1f, 0.86f, 0.48f, 0.96f);
        public static readonly Color SocketTintActive = Color.white;
        public static readonly Color SocketTintInactive = new Color(1f, 0.92f, 0.64f, 0.94f);
        public static readonly Color GlowTintActive = new Color(1f, 0.78f, 0.15f, 0.42f);
        public static readonly Color GlowTintInactive = new Color(0f, 0f, 0f, 0.34f);

        // ==================== API publique (Sprites prêts pour Image.sprite) ====================

        public static Sprite RailBackdropSprite() => GetPanelSprite("rail", RailFill, RailBorder);
        public static Sprite ButtonNormalSprite() => GetPanelSprite("btn-normal", ButtonNormalFill, ButtonNormalBorder);
        public static Sprite ButtonActiveSprite() => GetPanelSprite("btn-active", ButtonActiveFill, ButtonActiveBorder);

        public static Sprite RailOrnamentSprite() => GetSimpleSprite("rail-ornament", CreateRailOrnamentTexture);
        public static Sprite DividerSprite() => GetSimpleSprite("divider", CreateDividerTexture);
        public static Sprite HeaderBandSprite() => GetSimpleSprite("header-band", CreateHeaderBandTexture);
        public static Sprite ProgressFillSprite() => GetSimpleSprite("progress-fill", CreateProgressFillTexture);
        public static Sprite GlowActiveSprite() => GetSimpleSprite("glow-active", CreateSelectedGlowTexture);
        public static Sprite GlowInactiveSprite() => GetSimpleSprite("glow-inactive", CreateSoftShadowTexture);
        public static Sprite IconSocketSprite() => GetSimpleSprite("icon-socket", CreateIconSocketTexture);

        public static Sprite BadgeSprite(bool urgent) =>
            GetSimpleSprite(urgent ? "badge-urgent" : "badge-info", () => CreateBadgeTexture(urgent));

        // Neutral white radial falloff, unlike CreateSelectedGlowTexture/CreateSoftShadowTexture
        // above which bake fixed gold/cyan/black colors into the texture itself - this one is
        // meant to be tinted via Image.color (same "draw white, tint per-caller" contract as the
        // monolith's own IMGUI "selected-glow" texture, see GetPremiumTexture usage in
        // HiveViewProductUiPresenter). Used behind the header's resource/queen icons so each one
        // gets its own accent-colored halo instead of one fixed color for all of them.
        public static Sprite SoftRadialGlowSprite() => GetSimpleSprite("soft-radial-glow", CreateSoftRadialGlowTexture);

        private static Texture2D CreateSoftRadialGlowTexture()
        {
            const int size = 128;
            Texture2D tex = NewTransparentTexture(size, size);
            const float center = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float alpha = Mathf.Clamp01(Mathf.Pow(1f - d, 2.2f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        // Ids resolved to real art (Resources.Load succeeded) rather than the procedural
        // hex-badge fallback - the header resource chips use this to skip their accent tint
        // (ResourceAccent) on real painted art, same distinction the monolith makes via its
        // own OfficialIconKeys/PremiumIconKeys (see GetIconTexture/DrawGameIcon).
        private static readonly HashSet<string> RealArtIconKeys = new HashSet<string>();

        public static bool IconIsRealArt(string iconId)
        {
            string key = string.IsNullOrEmpty(iconId) ? "future" : iconId;
            // Force the lookup (and RealArtIconKeys population) if this id hasn't been
            // resolved yet this session.
            IconSprite(iconId);
            return RealArtIconKeys.Contains(key);
        }

        public static Sprite IconSprite(string iconId)
        {
            if (string.Equals(iconId, "world", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(iconId, "map", StringComparison.OrdinalIgnoreCase))
            {
                string iconKey = "icon-world-map";
                return GetSimpleSprite(iconKey, () => LoadWorldMapIcon());
            }

            string key = "icon-" + (string.IsNullOrEmpty(iconId) ? "future" : iconId);
            return GetSimpleSprite(key, () => LoadOrCreateIconTexture(iconId));
        }

        // Scoped to the header's resource ids only (not the rail's nav icons - "quests",
        // "hive-nav", etc. - which also happen to have same-named files in PremiumBeeIcons
        // but were never in scope for this fix and already look correct procedural).
        private static readonly HashSet<string> ResourceArtIconIds = new HashSet<string>
        {
            "honey", "wax", "pollen", "bees", "capacity", "royalJelly"
        };

        // Same real-art-first, procedural-fallback strategy as the monolith's own
        // GetIconTexture (HiveViewProductUiPresenter.cs) - "PremiumBeeIcons" is a shared
        // Resources folder (Assets/BeeKingdom/Playground/Resources/PremiumBeeIcons), so it
        // loads the exact same honey/wax/pollen/bees/capacity art Jeff paints there,
        // regardless of which assembly calls Resources.Load.
        private static Texture2D LoadOrCreateIconTexture(string iconId)
        {
            string rawKey = string.IsNullOrEmpty(iconId) ? "future" : iconId;
            if (ResourceArtIconIds.Contains(rawKey))
            {
                Texture2D texture = Resources.Load<Texture2D>("PremiumBeeIcons/" + rawKey);
                if (texture != null)
                {
                    RealArtIconKeys.Add(rawKey);
                    return texture;
                }
            }
            return CreateIconTexture(iconId);
        }

        private static Texture2D LoadWorldMapIcon()
        {
            // Loaded from a Resources folder (not a raw Assets/ file path) so it also resolves
            // in a built player - the Assets/ source folder does not exist outside the Editor.
            return Resources.Load<Texture2D>("world-map");
        }

        // ==================== Cache / fabrique de Sprite ====================

        private static Sprite GetPanelSprite(string key, Color fill, Color border)
        {
            string cacheKey = "panel-" + key;
            if (SpriteCache.TryGetValue(cacheKey, out Sprite cached)) return cached;
            Texture2D tex = CreatePremiumPanelTexture(fill, border);
            const float b = 28f;
            Sprite sprite = Sprite.Create(
                tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            sprite.name = cacheKey;
            SpriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Sprite GetSimpleSprite(string key, Func<Texture2D> factory)
        {
            if (SpriteCache.TryGetValue(key, out Sprite cached)) return cached;
            Texture2D tex = factory();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = key;
            SpriteCache[key] = sprite;
            return sprite;
        }

        public static void ClearCacheForProof()
        {
            SpriteCache.Clear();
            TextureCache.Clear();
        }

        // ==================== Génération : panneau premium fusionné ====================
        // Fusion de DrawPremiumPanel + CreatePremiumTexture("panel-outer-grain") + etched-line
        // (bordures) + DrawPanelCorner (coins) + amber-veil (voile), en une seule passe de pixels
        // au lieu de plusieurs textures superposées au runtime. Formules copiées à l'identique.

        private static Texture2D CreatePremiumPanelTexture(Color fill, Color border)
        {
            const int size = 192;
            Texture2D tex = NewTransparentTexture(size, size);
            Color refinedFill = Color.Lerp(fill, new Color(0.014f, 0.012f, 0.009f, fill.a), 0.58f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (float)(size - 1);
                    float ny = y / (float)(size - 1);
                    float edge = 1f - Mathf.Clamp01(Mathf.Min(Mathf.Min(nx, 1f - nx), Mathf.Min(ny, 1f - ny)) * 9f);
                    float wave = Mathf.Sin(x * 0.15f + y * 0.04f) * 0.055f + Mathf.Cos(x * 0.037f - y * 0.19f) * 0.040f;
                    float cell = Mathf.Sin((x + y) * 0.075f) * Mathf.Cos((x - y) * 0.052f) * 0.055f;
                    float fleck = ((x * 53 + y * 29 + (x * y) % 41) % 101) / 101f;
                    Color darkWax = new Color(0.025f, 0.018f, 0.011f, 0.96f);
                    Color warmWax = new Color(0.22f, 0.115f, 0.030f, 0.96f);
                    Color grain = Color.Lerp(darkWax, warmWax, Mathf.Clamp01(0.28f + wave + cell + fleck * 0.09f + edge * 0.16f));
                    grain.a = Mathf.Lerp(0.72f, 0.98f, Mathf.Clamp01(edge * 0.85f + 0.20f));

                    Color pixel = grain;
                    if (x >= 4 && x < size - 4 && y >= 4 && y < size - 4)
                    {
                        pixel = BlendOver(pixel, refinedFill);
                    }
                    tex.SetPixel(x, y, pixel);
                }
            }

            Color edgeLine = new Color(border.r, border.g, border.b, Mathf.Clamp01(border.a * 0.30f));
            for (int x = 7; x < size - 7; x++)
            {
                BlendPixel(tex, x, 1, edgeLine);
                BlendPixel(tex, x, size - 2, edgeLine);
            }
            for (int y = 7; y < size - 7; y++)
            {
                BlendPixel(tex, 1, y, edgeLine);
                BlendPixel(tex, size - 2, y, edgeLine);
            }

            Color cornerColor = new Color(1f, 0.82f, 0.34f, Mathf.Clamp01(border.a * 0.50f));
            const int cap = 14;
            DrawCorner(tex, 3, 3, cap, false, false, cornerColor);
            DrawCorner(tex, size - cap - 3, 3, cap, true, false, cornerColor);
            DrawCorner(tex, 3, size - cap - 3, cap, false, true, cornerColor);
            DrawCorner(tex, size - cap - 3, size - cap - 3, cap, true, true, cornerColor);

            Color veil = new Color(1f, 0.60f, 0.12f, Mathf.Clamp01(border.a * 0.10f));
            int veilHeight = Mathf.Max(2, (int)(size * 0.07f));
            for (int y = 5; y < 5 + veilHeight; y++)
            {
                for (int x = 10; x < size - 10; x++) BlendPixel(tex, x, y, veil);
            }

            tex.Apply();
            return tex;
        }

        // Miroir de DrawPanelCorner (deux segments perpendiculaires + point central).
        private static void DrawCorner(Texture2D tex, float x, float y, float cap, bool right, bool bottom, Color color)
        {
            float x0 = right ? x + cap - 2f : x;
            float y0 = bottom ? y + cap - 2f : y;
            float x1 = right ? x : x + cap - 2f;
            float y1 = bottom ? y : y + cap - 2f;
            DrawSolidLine(tex, Mathf.Min(x0, x1), y0, Mathf.Abs(x1 - x0), 2f, color);
            DrawSolidLine(tex, x0, Mathf.Min(y0, y1), 2f, Mathf.Abs(y1 - y0), color);
            DrawSolidLine(tex, x + cap * 0.5f - 1.5f, y + cap * 0.5f - 1.5f, 3f, 3f, color);
        }

        private static void DrawSolidLine(Texture2D tex, float x, float y, float w, float h, Color color)
        {
            int x0 = Mathf.RoundToInt(x);
            int y0 = Mathf.RoundToInt(y);
            int x1 = Mathf.Max(x0 + 1, Mathf.RoundToInt(x + w));
            int y1 = Mathf.Max(y0 + 1, Mathf.RoundToInt(y + h));
            for (int py = y0; py < y1; py++)
                for (int px = x0; px < x1; px++)
                    BlendPixel(tex, px, py, color);
        }

        // ==================== Bandes/ornements simples (copiées presque à l'identique) ====================

        // Miroir de DrawBottomRailOrnament : voile ambré, ombre douce, ligne gravée — bande fine
        // étirée horizontalement sur toute la largeur du rail (Image simple, non 9-slice).
        private static Texture2D CreateRailOrnamentTexture()
        {
            const int w = 256, h = 32;
            Texture2D tex = NewTransparentTexture(w, h);
            for (int x = 0; x < w; x++)
            {
                BlendPixel(tex, x, h - 9, new Color(1f, 0.64f, 0.16f, 0.10f)); // amber-veil, proche du haut du rail
                BlendPixel(tex, x, 4, new Color(0f, 0f, 0f, 0.24f));           // soft-shadow, proche du bas
                BlendPixel(tex, x, h - 14, new Color(1f, 0.80f, 0.28f, 0.18f)); // etched-line
            }
            tex.Apply();
            return tex;
        }

        // Miroir de DrawRailDivider : liseré gravé + ombre, entre deux boutons.
        private static Texture2D CreateDividerTexture()
        {
            const int w = 4, h = 64;
            Texture2D tex = NewTransparentTexture(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) BlendPixel(tex, x, y, new Color(1f, 0.76f, 0.22f, 0.16f));
                BlendPixel(tex, 2, y, new Color(0f, 0f, 0f, 0.26f));
            }
            tex.Apply();
            return tex;
        }

        // Miroir de DrawPremiumHeaderBand (bandeau doré au-dessus d'un bouton actif).
        private static Texture2D CreateHeaderBandTexture()
        {
            const int w = 192, h = 24;
            Texture2D tex = NewTransparentTexture(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = new Color(0.090f, 0.064f, 0.034f, 0.52f);
                    if (y < h * 0.34f) c = BlendOver(c, new Color(1f, 0.66f, 0.16f, 0.12f));
                    BlendPixel(tex, x, y, c);
                }
                if (y == h - 3) for (int x = 0; x < w; x++) BlendPixel(tex, x, y, new Color(1f, 0.86f, 0.34f, 0.28f));
                if (y == 1) for (int x = 0; x < w; x++) BlendPixel(tex, x, y, new Color(0f, 0f, 0f, 0.32f));
            }
            tex.Apply();
            return tex;
        }

        // Miroir de CreatePremiumTexture("progress-fill") : dégradé vertical orange -> jaune.
        private static Texture2D CreateProgressFillTexture()
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < size; y++)
            {
                Color c = Color.Lerp(new Color(1f, 0.50f, 0.02f, 1f), new Color(1f, 0.92f, 0.18f, 1f), y / (float)size);
                for (int x = 0; x < size; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        // Miroir de CreatePremiumTexture("selected-glow") : glow hexagonal derrière l'icône active.
        private static Texture2D CreateSelectedGlowTexture()
        {
            const int size = 192;
            Texture2D tex = NewTransparentTexture(size, size);
            FillHex(tex, 96, 96, 90, new Color(1f, 0.80f, 0.08f, 0.22f));
            StrokeHex(tex, 96, 96, 84, new Color(1f, 0.94f, 0.18f, 0.86f), 10);
            StrokeHex(tex, 96, 96, 70, new Color(0.20f, 0.96f, 1f, 0.80f), 5);
            tex.Apply();
            return tex;
        }

        // Miroir de CreatePremiumTexture("soft-shadow") : ombre douce derrière l'icône inactive.
        private static Texture2D CreateSoftShadowTexture()
        {
            const int size = 192;
            Texture2D tex = NewTransparentTexture(size, size);
            FillEllipse(tex, 96, 96, 78, 48, new Color(0f, 0f, 0f, 0.55f));
            tex.Apply();
            return tex;
        }

        // Miroir de CreatePremiumTexture("icon-socket") : cadre hexagonal derrière chaque icône.
        private static Texture2D CreateIconSocketTexture()
        {
            const int size = 192;
            Texture2D tex = NewTransparentTexture(size, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size * 0.5f) / (size * 0.5f);
                    float dy = (y - size * 0.5f) / (size * 0.5f);
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float grain = Mathf.Sin(x * 0.17f + y * 0.06f) * 0.04f + Mathf.Cos(x * 0.05f - y * 0.21f) * 0.035f;
                    float alpha = Mathf.Clamp01(1.12f - d * 1.18f);
                    Color c = Color.Lerp(new Color(0.020f, 0.016f, 0.011f, alpha), new Color(0.24f, 0.12f, 0.028f, alpha), Mathf.Clamp01(0.30f + grain));
                    tex.SetPixel(x, y, c);
                }
            }
            StrokeHex(tex, 96, 98, 78, new Color(0.08f, 0.035f, 0.008f, 0.98f), 16);
            StrokeHex(tex, 96, 96, 70, new Color(1f, 0.72f, 0.16f, 0.92f), 7);
            StrokeHex(tex, 96, 96, 56, new Color(1f, 0.88f, 0.34f, 0.42f), 3);
            FillCircle(tex, 72, 62, 9, new Color(1f, 0.86f, 0.36f, 0.44f));
            DrawLine(tex, 58, 58, 134, 42, new Color(1f, 0.84f, 0.28f, 0.22f), 4);
            FillCircle(tex, 124, 130, 8, new Color(0f, 0f, 0f, 0.24f));
            tex.Apply();
            return tex;
        }

        // Miroir de CreatePremiumTexture("red-badge") / le cas "bleu" utilisé par DrawMenuBadge
        // pour les pastilles non urgentes. Portée mais non câblée sur le rail (voir rapport :
        // aucune source de données réelle de notifications dans ce port).
        private static Texture2D CreateBadgeTexture(bool urgent)
        {
            const int size = 96;
            Texture2D tex = NewTransparentTexture(size, size);
            if (urgent)
            {
                FillCircle(tex, 48, 48, 35, new Color(0.82f, 0.02f, 0.02f, 1f));
                StrokeCircle(tex, 48, 48, 36, new Color(1f, 0.78f, 0.42f, 1f), 4);
            }
            else
            {
                FillCircle(tex, 48, 48, 35, new Color(0.10f, 0.38f, 0.66f, 0.94f));
                StrokeCircle(tex, 48, 48, 36, new Color(0.55f, 0.86f, 1f, 0.72f), 4);
            }
            tex.Apply();
            return tex;
        }

        // ==================== Icônes (miroir de CreateHighDefinitionMenuIconTexture) ====================
        // Seules les 5 formes réellement utilisées par le rail + le fallback générique du
        // monolithe (utilisé par lui-même pour "queen"/"inventory"/"messages"/"more"/"preview",
        // qui n'ont pas de case dédiée dans le monolithe non plus).

        private static Texture2D CreateIconTexture(string iconId)
        {
            const int size = 256;
            Texture2D tex = NewTransparentTexture(size, size);

            Color dark = new Color(0.12f, 0.055f, 0.012f, 0.98f);
            Color gold = new Color(1f, 0.70f, 0.14f, 1f);
            Color light = new Color(1f, 0.96f, 0.56f, 1f);
            Color blue = new Color(0.40f, 0.78f, 1f, 1f);

            StrokeHex(tex, 128, 130, 104, dark, 15);
            StrokeHex(tex, 128, 128, 96, gold, 9);
            StrokeHex(tex, 128, 128, 78, new Color(1f, 0.86f, 0.28f, 0.40f), 4);

            switch (iconId)
            {
                case "hive":
                case "hive-nav":
                    FillHex(tex, 128, 136, 54, new Color(0.96f, 0.54f, 0.07f, 1f));
                    StrokeHex(tex, 128, 136, 62, light, 9);
                    FillHex(tex, 128, 136, 25, dark);
                    FillCircle(tex, 128, 86, 31, gold);
                    StrokeCircle(tex, 128, 86, 34, light, 6);
                    DrawLine(tex, 82, 132, 174, 108, new Color(0.38f, 0.16f, 0.02f, 0.65f), 8);
                    break;
                case "quests":
                case "detail":
                    FillRect(tex, 76, 70, 104, 132, new Color(0.72f, 0.40f, 0.10f, 1f));
                    FillRect(tex, 91, 84, 74, 100, new Color(0.18f, 0.095f, 0.030f, 1f));
                    StrokeRect(tex, 76, 70, 104, 132, light);
                    DrawLine(tex, 102, 112, 154, 112, gold, 8);
                    DrawLine(tex, 102, 140, 154, 140, gold, 8);
                    DrawLine(tex, 102, 168, 140, 168, gold, 8);
                    break;
                case "world":
                case "map":
                    FillCircle(tex, 128, 130, 58, new Color(0.10f, 0.38f, 0.58f, 1f));
                    StrokeCircle(tex, 128, 130, 65, blue, 9);
                    DrawLine(tex, 64, 130, 192, 130, light, 5);
                    DrawLine(tex, 128, 66, 128, 194, light, 5);
                    StrokeCircle(tex, 128, 130, 31, new Color(0.64f, 0.92f, 1f, 0.95f), 5);
                    break;
                case "alliance":
                    Color violet = new Color(0.48f, 0.30f, 0.92f, 1f);
                    FillCircle(tex, 92, 132, 32, blue);
                    FillCircle(tex, 164, 132, 32, violet);
                    FillCircle(tex, 128, 96, 30, gold);
                    StrokeCircle(tex, 92, 132, 36, light, 6);
                    StrokeCircle(tex, 164, 132, 36, light, 6);
                    StrokeCircle(tex, 128, 96, 34, light, 6);
                    DrawLine(tex, 112, 122, 144, 122, light, 8);
                    break;
                case "inbox":
                    FillRect(tex, 66, 88, 124, 90, new Color(0.14f, 0.18f, 0.20f, 1f));
                    StrokeRect(tex, 66, 88, 124, 90, light);
                    DrawLine(tex, 66, 88, 128, 142, gold, 9);
                    DrawLine(tex, 190, 88, 128, 142, gold, 9);
                    DrawLine(tex, 82, 170, 174, 170, blue, 7);
                    break;
                case "queen":
                    // Couronne (langage hexagonal premium, touche bleue) : bandeau + pointes.
                    FillHex(tex, 128, 140, 64, new Color(0.16f, 0.10f, 0.05f, 0.98f));
                    FillCircle(tex, 128, 128, 30, gold);
                    StrokeCircle(tex, 128, 128, 33, new Color(0.40f, 0.78f, 1f, 0.9f), 7);
                    DrawLine(tex, 88, 104, 168, 104, new Color(0.40f, 0.78f, 1f, 0.9f), 7);
                    DrawLine(tex, 88, 104, 128, 66, light, 8);
                    DrawLine(tex, 128, 66, 168, 104, light, 8);
                    DrawLine(tex, 88, 104, 128, 66, new Color(1f, 0.96f, 0.56f, 1f), 4);
                    DrawLine(tex, 128, 66, 168, 104, new Color(1f, 0.96f, 0.56f, 1f), 4);
                    break;
                case "honey":
                    // Goutte de miel : hex extérieur + bulle, accents ambre.
                    FillCircle(tex, 128, 132, 56, dark);
                    StrokeCircle(tex, 128, 132, 62, new Color(1f, 0.70f, 0.14f, 0.9f), 9);
                    FillCircle(tex, 128, 150, 38, new Color(0.96f, 0.60f, 0.08f, 1f));
                    StrokeCircle(tex, 128, 96, 20, light, 7);
                    break;
                case "wax":
                    // Bloc de cire : cellules hex empilées, tons chaude cire.
                    StrokeHex(tex, 128, 116, 44, new Color(1f, 0.82f, 0.28f, 0.95f), 8);
                    StrokeHex(tex, 96, 158, 22, new Color(0.96f, 0.74f, 0.22f, 0.9f), 6);
                    StrokeHex(tex, 160, 158, 22, new Color(0.96f, 0.74f, 0.22f, 0.9f), 6);
                    FillHex(tex, 128, 150, 20, new Color(1f, 0.70f, 0.14f, 1f));
                    break;
                case "pollen":
                    // Grains de pollen groupés (accents verts/pollen).
                    FillCircle(tex, 100, 100, 24, new Color(0.78f, 0.90f, 0.32f, 1f));
                    FillCircle(tex, 156, 112, 24, new Color(0.86f, 0.94f, 0.42f, 1f));
                    FillCircle(tex, 122, 152, 24, new Color(0.72f, 0.84f, 0.28f, 1f));
                    StrokeCircle(tex, 100, 100, 28, light, 6);
                    StrokeCircle(tex, 156, 112, 28, light, 6);
                    StrokeCircle(tex, 122, 152, 28, light, 6);
                    break;
                case "bees":
                    // Abeille stylisée : corps bleuté + rayures, hex socket.
                    StrokeHex(tex, 128, 128, 66, dark, 12);
                    StrokeHex(tex, 128, 128, 90, new Color(0.56f, 0.82f, 1f, 0.8f), 6);
                    FillEllipseH(tex, 128, 142, 30, 24, new Color(0.40f, 0.78f, 1f, 1f));
                    DrawLine(tex, 98, 136, 158, 136, new Color(0.05f, 0.05f, 0.06f, 1f), 8);
                    DrawLine(tex, 98, 148, 158, 148, new Color(0.05f, 0.05f, 0.06f, 1f), 8);
                    FillCircle(tex, 128, 118, 20, gold);
                    break;
                case "capacity":
                    // Caisse / capacité : cadres superposés, teinte neutre réserve.
                    StrokeRect(tex, 88, 92, 80, 72, new Color(0.84f, 0.76f, 0.60f, 0.95f));
                    DrawLine(tex, 88, 92, 168, 92, new Color(0.40f, 0.78f, 1f, 0.6f), 4);
                    FillRect(tex, 108, 148, 40, 10, blue);
                    DrawLine(tex, 104, 92, 128, 62, light, 6);
                    DrawLine(tex, 152, 92, 128, 62, light, 6);
                    break;
                case "royalJelly":
                    // Pot de gelee royale (repli procedural avant le vrai artwork peint) :
                    // bol dore + goutte credeuse + accent violet, meme langage que "honey"
                    // mais teinte distincte (monnaie premium, pas une ressource en vrac).
                    FillCircle(tex, 128, 150, 46, new Color(0.62f, 0.42f, 0.85f, 1f));
                    StrokeCircle(tex, 128, 150, 52, gold, 8);
                    FillCircle(tex, 128, 118, 30, new Color(0.98f, 0.92f, 0.70f, 1f));
                    StrokeCircle(tex, 128, 118, 34, light, 5);
                    break;
                case "shop":
                    // Vitrine / boutique : écrin hexagonal + enseigne, accents assortis.
                    StrokeHex(tex, 128, 140, 62, dark, 12);
                    StrokeHex(tex, 128, 128, 84, blue, 5);
                    FillRect(tex, 92, 128, 72, 40, new Color(0.12f, 0.055f, 0.012f, 1f));
                    StrokeRect(tex, 92, 128, 72, 40, light);
                    FillCircle(tex, 128, 120, 14, gold);
                    StrokeCircle(tex, 128, 120, 17, light, 4);
                    DrawLine(tex, 102, 168, 154, 168, new Color(0.40f, 0.78f, 1f, 0.85f), 6);
                    break;
                default:
                    // Fallback générique du monolithe (aussi utilisé par lui pour queen/inventory/
                    // messages/more/preview — ces IDs n'ont pas de forme dédiée dans le monolithe).
                    StrokeCircle(tex, 128, 128, 56, blue, 10);
                    DrawLine(tex, 128, 86, 128, 132, gold, 12);
                    DrawLine(tex, 128, 132, 162, 154, gold, 12);
                    break;
            }

            tex.Apply();
            return tex;
        }

        // ==================== Primitives pixel (copiées de HiveViewProductUiPresenter.cs) ====================

        private static Texture2D NewTransparentTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, Color.clear);
            return tex;
        }

        // Compositing "source-over" manuel : équivalent du blending GPU qu'IMGUI obtient
        // gratuitement en empilant plusieurs GUI.DrawTexture successifs.
        private static Color BlendOver(Color dst, Color src)
        {
            float a = src.a + dst.a * (1f - src.a);
            if (a <= 0.0001f) return new Color(0, 0, 0, 0);
            float r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / a;
            float g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / a;
            float b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / a;
            return new Color(r, g, b, a);
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) return;
            tex.SetPixel(x, y, BlendOver(tex.GetPixel(x, y), color));
        }

        private static void SetIconPixel(Texture2D tex, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) return;
            tex.SetPixel(x, y, color);
        }

        private static void FillRect(Texture2D tex, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
                for (int px = x; px < x + width; px++)
                    SetIconPixel(tex, px, py, color);
        }

        private static void StrokeRect(Texture2D tex, int x, int y, int width, int height, Color color)
        {
            DrawLine(tex, x, y, x + width, y, color, 3);
            DrawLine(tex, x + width, y, x + width, y + height, color, 3);
            DrawLine(tex, x + width, y + height, x, y + height, color, 3);
            DrawLine(tex, x, y + height, x, y, color, 3);
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            int r2 = radius * radius;
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r2) SetIconPixel(tex, x, y, color);
                }
        }

        private static void FillEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color color)
        {
            for (int y = cy - ry; y <= cy + ry; y++)
                for (int x = cx - rx; x <= cx + rx; x++)
                {
                    float dx = (x - cx) / (float)rx, dy = (y - cy) / (float)ry;
                    if (dx * dx + dy * dy <= 1f) SetIconPixel(tex, x, y, color);
                }
        }

        private static void FillEllipseH(Texture2D tex, int cx, int cy, int rx, int ry, Color color)
        {
            FillEllipse(tex, cx, cy, rx, ry, color);
        }

        private static void StrokeCircle(Texture2D tex, int cx, int cy, int radius, Color color, int thickness)
        {
            int outer = radius * radius;
            int innerRadius = Mathf.Max(1, radius - thickness);
            int inner = innerRadius * innerRadius;
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    int dx = x - cx, dy = y - cy;
                    int distance = dx * dx + dy * dy;
                    if (distance <= outer && distance >= inner) SetIconPixel(tex, x, y, color);
                }
        }

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                FillCircle(tex, x0, y0, Mathf.Max(1, thickness / 2), color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private static float Edge(int x1, int y1, int x2, int y2, int x, int y)
        {
            return (x - x1) * (y2 - y1) - (y - y1) * (x2 - x1);
        }

        private static void FillTriangle(Texture2D tex, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            int minX = Mathf.Min(x1, Mathf.Min(x2, x3));
            int maxX = Mathf.Max(x1, Mathf.Max(x2, x3));
            int minY = Mathf.Min(y1, Mathf.Min(y2, y3));
            int maxY = Mathf.Max(y1, Mathf.Max(y2, y3));
            float area = Edge(x1, y1, x2, y2, x3, y3);
            if (Mathf.Abs(area) < 0.01f) return;
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Edge(x2, y2, x3, y3, x, y);
                    float w1 = Edge(x3, y3, x1, y1, x, y);
                    float w2 = Edge(x1, y1, x2, y2, x, y);
                    if ((w0 >= 0f && w1 >= 0f && w2 >= 0f) || (w0 <= 0f && w1 <= 0f && w2 <= 0f))
                        SetIconPixel(tex, x, y, color);
                }
        }

        private static void FillHex(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            Vector2Int[] points = HexPoints(cx, cy, radius);
            for (int i = 1; i < points.Length - 1; i++)
                FillTriangle(tex, points[0].x, points[0].y, points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color);
            FillTriangle(tex, cx, cy, points[0].x, points[0].y, points[points.Length - 1].x, points[points.Length - 1].y, color);
            for (int i = 0; i < points.Length - 1; i++)
                FillTriangle(tex, cx, cy, points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color);
        }

        private static void StrokeHex(Texture2D tex, int cx, int cy, int radius, Color color, int thickness)
        {
            Vector2Int[] points = HexPoints(cx, cy, radius);
            for (int i = 0; i < points.Length; i++)
            {
                Vector2Int a = points[i];
                Vector2Int b = points[(i + 1) % points.Length];
                DrawLine(tex, a.x, a.y, b.x, b.y, color, thickness);
            }
        }

        private static Vector2Int[] HexPoints(int cx, int cy, int radius)
        {
            Vector2Int[] points = new Vector2Int[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i + 30f);
                points[i] = new Vector2Int(cx + Mathf.RoundToInt(Mathf.Cos(angle) * radius), cy + Mathf.RoundToInt(Mathf.Sin(angle) * radius));
            }
            return points;
        }
    }
}
