using UnityEngine;

namespace BeeKingdom.Tutorial
{
    // UI-only skin using ornamental crops of the approved October 16 mockup.
    // Text, portraits, actions, buildings and FTUE state stay live game content.
    internal static class TutorialPremiumSkin
    {
        private static Texture2D _atlas, _panelFill, _goldFill;
        private static Font _titleFont;
        private static GUIStyle _primaryStyle, _secondaryStyle, _badgeStyle;
        private static int _buttonSize;
        private static bool _loaded;

        internal static Font TitleFont
        {
            get
            {
                EnsureAssets();
                return _titleFont;
            }
        }

        private static void EnsureAssets()
        {
            if (!_loaded)
            {
                _atlas = Resources.Load<Texture2D>("TutorialPremium/royal-dialogue-atlas");
                _titleFont = Resources.Load<Font>("TutorialPremium/Cinzel-Regular");
                _loaded = true;
            }
            if (_panelFill == null)
                _panelFill = Gradient("BK tutorial obsidian", new Color(0.027f, 0.034f, 0.045f),
                    new Color(0.059f, 0.073f, 0.091f));
            if (_goldFill == null)
                _goldFill = Gradient("BK tutorial gold", new Color(0.46f, 0.28f, 0.045f),
                    new Color(0.96f, 0.73f, 0.21f));
        }

        private static Texture2D Gradient(string name, Color bottom, Color top)
        {
            var texture = new Texture2D(1, 64, TextureFormat.RGBA32, false)
            {
                name = name, hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.Lerp(bottom, top, i / 63f);
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        internal static void DrawPanel(Rect panel, float scale)
        {
            EnsureAssets();
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.30f);
            GUI.DrawTexture(new Rect(panel.x - 5f * scale, panel.y + 8f * scale,
                panel.width + 10f * scale, panel.height + 6f * scale), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(panel, _panelFill);
            // Fully opaque, unlike GUI.Box: hive decorations cannot bleed through the body.
            Stroke(new Rect(panel.x + 3f * scale, panel.y + 3f * scale,
                panel.width - 6f * scale, panel.height - 6f * scale),
                new Color(0.82f, 0.60f, 0.20f), 2f * scale);
            Stroke(new Rect(panel.x + 10f * scale, panel.y + 10f * scale,
                panel.width - 20f * scale, panel.height - 20f * scale),
                new Color(0.40f, 0.29f, 0.10f), Mathf.Max(1f, scale));
            DrawHoneycomb(panel, scale);
            if (_atlas != null)
            {
                float corner = Mathf.Min(96f * scale, panel.height * 0.36f);
                float edge = corner / 96f;
                // Stretch only straight lengths. The corners and jewels retain their proportions.
                Slice(new Rect(panel.x + corner, panel.y, panel.width - 2f * corner, 14f * edge), 320, 0, 4, 14);
                Slice(new Rect(panel.x + corner, panel.yMax - 14f * edge, panel.width - 2f * corner, 14f * edge), 320, 0, 4, 14, false, true);
                Slice(new Rect(panel.xMax - 10f * edge, panel.y + corner, 10f * edge, panel.height - 2f * corner), 336, 0, 10, 4);
                Slice(new Rect(panel.x, panel.y + corner, 10f * edge, panel.height - 2f * corner), 336, 0, 10, 4, true);
                Slice(new Rect(panel.x, panel.y, corner, corner), 0, 0, 96, 96, true);
                Slice(new Rect(panel.xMax - corner, panel.y, corner, corner), 0, 0, 96, 96);
                Slice(new Rect(panel.x, panel.yMax - corner, corner, corner), 0, 0, 96, 96, true, true);
                Slice(new Rect(panel.xMax - corner, panel.yMax - corner, corner, corner), 0, 0, 96, 96, false, true);
                Slice(new Rect(panel.center.x - 74f * scale, panel.y - 31f * scale,
                    148f * scale, 80f * scale), 112, 0, 148, 80);
                Slice(new Rect(panel.center.x - 32f * scale, panel.yMax - 23f * scale,
                    64f * scale, 48f * scale), 272, 34, 64, 48);
            }
            GUI.color = previous;
        }

        private static void DrawHoneycomb(Rect panel, float scale)
        {
            if (_atlas == null) return;
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.42f);
            float size = 96f * scale;
            Slice(new Rect(panel.xMax - size - 18f * scale, panel.y + 14f * scale,
                size, size), 0, 160, 96, 96);
            GUI.color = previous;
        }

        internal static void DrawNameRule(Rect name, float titleWidth, float scale)
        {
            float x = name.x + titleWidth + 26f * scale;
            float right = name.xMax - 42f * scale;
            if (right <= x + 40f * scale) return;
            Color gold = new Color(0.72f, 0.51f, 0.15f, 0.65f);
            float center = (x + right) * 0.5f, y = name.center.y;
            Fill(new Rect(x, y, center - x - 14f * scale, Mathf.Max(1f, scale)), gold);
            Fill(new Rect(center + 14f * scale, y, right - center - 14f * scale, Mathf.Max(1f, scale)), gold);
            if (_atlas != null)
                Slice(new Rect(center - 13f * scale, y - 17f * scale, 26f * scale, 34f * scale), 272, 0, 26, 34);
        }

        internal static bool DrawButton(Rect rect, string label, bool primary, float scale)
        {
            EnsureAssets();
            Color previous = GUI.color;
            bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
            GUI.color = hover ? new Color(1.10f, 1.10f, 1.10f, 1f) : Color.white;
            if (_atlas != null)
            {
                float edge = Mathf.Min(22f * scale, rect.width * 0.18f);
                int sourceX = primary ? 112 : 224;
                Slice(new Rect(rect.x, rect.y, edge, rect.height), sourceX, 112, 22, 64);
                Slice(new Rect(rect.x + edge, rect.y, rect.width - 2f * edge, rect.height), sourceX + 22, 112, 4, 64);
                Slice(new Rect(rect.xMax - edge, rect.y, edge, rect.height), sourceX + 26, 112, 22, 64);
            }
            else
            {
                GUI.DrawTexture(rect, primary ? _goldFill : _panelFill);
                Stroke(rect, new Color(0.9f, 0.70f, 0.29f), Mathf.Max(1f, 2f * scale));
            }
            GUI.color = Color.white;
            int size = Mathf.Max(16, Mathf.RoundToInt(24f * scale));
            if (_primaryStyle == null || _buttonSize != size)
            {
                _buttonSize = size;
                _primaryStyle = ButtonStyle(new Color(0.07f, 0.045f, 0.01f), size);
                _secondaryStyle = ButtonStyle(new Color(1f, 0.89f, 0.66f), Mathf.Max(16, Mathf.RoundToInt(20f * scale)));
            }
            bool clicked = GUI.Button(rect, label, primary ? _primaryStyle : _secondaryStyle);
            GUI.color = previous;
            return clicked;
        }

        private static GUIStyle ButtonStyle(Color text, int size)
        {
            // A fresh style also avoids inherited high-DPI scaledBackgrounds covering the gold art.
            var style = new GUIStyle()
            {
                fontSize = size, font = TitleFont, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, border = new RectOffset(),
                padding = new RectOffset(6, 6, 2, 2), margin = new RectOffset()
            };
            GUIStyleState[] states = { style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused };
            foreach (GUIStyleState state in states)
            {
                state.background = null;
#if UNITY_EDITOR
                state.scaledBackgrounds = System.Array.Empty<Texture2D>();
#endif
                state.textColor = text;
            }
            return style;
        }

        internal static void DrawBadge(Rect rect, string letter, float scale)
        {
            EnsureAssets();
            GUI.DrawTexture(rect, _panelFill);
            Stroke(rect, new Color(0.83f, 0.63f, 0.25f), Mathf.Max(1f, scale));
            if (_badgeStyle == null)
                _badgeStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,
                    font = TitleFont, fontStyle = FontStyle.Bold };
            _badgeStyle.fontSize = Mathf.Max(20, Mathf.RoundToInt(28f * scale));
            _badgeStyle.normal.textColor = new Color(1f, 0.80f, 0.40f);
            GUI.Label(rect, letter, _badgeStyle);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Stroke(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static void Slice(Rect destination, int x, int y, int width, int height,
            bool flipX = false, bool flipY = false)
        {
            if (_atlas == null || destination.width <= 0f || destination.height <= 0f) return;
            Rect uv = new Rect(x / (float)_atlas.width, 1f - (y + height) / (float)_atlas.height,
                width / (float)_atlas.width, height / (float)_atlas.height);
            if (flipX) { uv.x += uv.width; uv.width = -uv.width; }
            if (flipY) { uv.y += uv.height; uv.height = -uv.height; }
            GUI.DrawTextureWithTexCoords(destination, _atlas, uv, true);
        }
    }
}
