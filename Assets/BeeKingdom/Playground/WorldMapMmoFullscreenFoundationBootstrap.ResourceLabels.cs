using UnityEngine;

namespace BeeKingdom.Playground
{
    public sealed partial class WorldMapMmoFullscreenFoundationBootstrap
    {
        private Font resourceLabelFont;
        private bool resourceLabelFontLoaded;
        private GUIStyle resourceNameStyle, resourceCountStyle;
        private GUIStyle hiveNameStyle, hiveDetailStyle;
        private int resourceLabelFontSize;
        private readonly System.Collections.Generic.List<Rect> drawnMapLabelPanels = new System.Collections.Generic.List<Rect>();

        private readonly System.Collections.Generic.Dictionary<Texture2D, ResourceLabelBoundsCache> resourceLabelBounds =
            new System.Collections.Generic.Dictionary<Texture2D, ResourceLabelBoundsCache>();
        private readonly GUIContent resourceLabelNameContent = new GUIContent();
        private readonly GUIContent resourceLabelCountContent = new GUIContent();

        private struct ResourceLabelBoundsCache
        {
            public uint UpdateCount;
            public Rect Bounds;
        }

        // Only selected/collecting art is read, once per texture revision. This local
        // presentation cache does not participate in world placement or hit masks.
        private Rect ResourceLabelInkBounds(Texture2D texture)
        {
            if (texture == null) return new Rect(0f, 0f, 1f, 1f);
            if (resourceLabelBounds.TryGetValue(texture, out ResourceLabelBoundsCache cached)
                && cached.UpdateCount == texture.updateCount) return cached.Bounds;

            Rect bounds = new Rect(0f, 0f, 1f, 1f);
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = null;
            Texture2D readable = null;
            try
            {
                Color32[] pixels;
                if (texture.isReadable) pixels = texture.GetPixels32();
                else
                {
                    temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(texture, temporary);
                    RenderTexture.active = temporary;
                    readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                    readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0, false);
                    pixels = readable.GetPixels32();
                }
                int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                    {
                        if (pixels[y * texture.width + x].a < 16) continue;
                        minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                    }
                // Unity pixels start bottom-left; IMGUI uses top-left coordinates.
                if (maxX >= minX && maxY >= minY)
                    bounds = new Rect((float)minX / texture.width, 1f - (float)(maxY + 1) / texture.height,
                        (float)(maxX - minX + 1) / texture.width, (float)(maxY - minY + 1) / texture.height);
            }
            catch (UnityException)
            {
                // Retain a usable label if this platform cannot read the texture.
            }
            finally
            {
                RenderTexture.active = previous;
                if (temporary != null) RenderTexture.ReleaseTemporary(temporary);
                if (readable != null) Destroy(readable);
            }
            // Bound the cache across streamed texture lifetimes.
            if (resourceLabelBounds.Count >= 32) resourceLabelBounds.Clear();
            resourceLabelBounds[texture] = new ResourceLabelBoundsCache { UpdateCount = texture.updateCount, Bounds = bounds };
            return bounds;
        }

        private Rect DrawPremiumResourceLabel(WorldResourceNode resource, Vector2 point, float size, Texture2D texture, string quantity, float opacity = 1f, float trailingHeight = 0f, Rect? reservedPanel = null)
        {
            return DrawPremiumMapLabel(ResourceLabel(resource.Kind), quantity, point, size, texture, opacity, trailingHeight, false, reservedPanel);
        }

        private void EnsurePremiumMapLabelStyles()
        {
            if (!resourceLabelFontLoaded)
            {
                resourceLabelFont = Resources.Load<Font>("TutorialPremium/Cinzel-Regular");
                resourceLabelFontLoaded = true;
            }
            // Screen-space type remains legible when world zoom changes.
            int titleSize = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(Screen.width, Screen.height) / 72f), 14, 18);
            if (resourceNameStyle == null || titleSize != resourceLabelFontSize)
            {
                resourceLabelFontSize = titleSize;
                resourceNameStyle = new GUIStyle(GUI.skin.label) { font = resourceLabelFont, fontSize = titleSize,
                    alignment = TextAnchor.MiddleCenter, wordWrap = true, padding = new RectOffset(),
                    normal = { textColor = new Color(0.94f, 0.78f, 0.45f, 1f) } };
                resourceCountStyle = new GUIStyle(GUI.skin.label) { font = resourceLabelFont, fontSize = titleSize - 2,
                    alignment = TextAnchor.MiddleCenter, padding = new RectOffset(),
                    normal = { textColor = new Color(0.84f, 0.85f, 0.79f, 1f) } };
            }
            if (hiveNameStyle == null || hiveNameStyle.fontSize != resourceLabelFontSize)
            {
                hiveNameStyle = new GUIStyle(resourceNameStyle) { richText = false };
                hiveDetailStyle = new GUIStyle(resourceCountStyle) { richText = false, wordWrap = true };
            }
        }

        // The solver and renderer use identical font metrics: label bounds are
        // part of the collision reservation and the click target.
        private Vector2 MeasurePremiumMapLabel(string name, string quantity, bool hiveCaption)
        {
            EnsurePremiumMapLabelStyles();
            GUIStyle title = hiveCaption ? hiveNameStyle : resourceNameStyle;
            GUIStyle count = hiveCaption ? hiveDetailStyle : resourceCountStyle;
            resourceLabelNameContent.text = name;
            resourceLabelCountContent.text = quantity;
            float maxWidth = Mathf.Min(230f, Mathf.Max(1f, Screen.width - 16f));
            float w = Mathf.Min(maxWidth, Mathf.Max(100f,
                Mathf.Max(title.CalcSize(resourceLabelNameContent).x,
                    count.CalcSize(resourceLabelCountContent).x) + 20f));
            float nameHeight = title.CalcHeight(resourceLabelNameContent, w - 16f);
            float countHeight = hiveCaption
                ? count.CalcHeight(resourceLabelCountContent, w - 16f)
                : resourceLabelFontSize + 3f;
            if (hiveCaption)
            {
                float heightBudget = Mathf.Max(32f, Screen.height - 24f);
                nameHeight = Mathf.Min(nameHeight, heightBudget * .45f);
                countHeight = Mathf.Min(countHeight, heightBudget * .55f - 8f);
            }
            return new Vector2(w, nameHeight + countHeight + 8f);
        }

        private Rect DrawPremiumMapLabel(string name, string quantity, Vector2 point, float size, Texture2D texture, float opacity = 1f, float trailingHeight = 0f, bool hiveCaption = false, Rect? reservedPanel = null)
        {
            EnsurePremiumMapLabelStyles();
            int titleSize = resourceLabelFontSize;
            Rect ink = ResourceLabelInkBounds(texture);
            float width = size, height = size;
            if (texture != null && texture.width > 0 && texture.height > 0)
            {
                float fit = size / Mathf.Max(texture.width, texture.height);
                width = texture.width * fit;
                height = texture.height * fit;
            }
            float left = point.x - width * 0.5f;
            float top = point.y - size * 0.58f + (size - height) * 0.5f;
            float center = left + (ink.x + ink.width * 0.5f) * width;
            float bottom = top + ink.yMax * height;
            if (texture == null)
            {
                center = point.x;
                top = point.y - size * 0.36f;
                bottom = point.y + size * 0.36f;
                ink = new Rect(0f, 0f, 1f, 1f);
            }
            // Keep server-provided player/alliance names literal and wrap long names.
            GUIStyle nameStyle = hiveCaption ? hiveNameStyle : resourceNameStyle;
            GUIStyle countStyle = hiveCaption ? hiveDetailStyle : resourceCountStyle;
            resourceLabelNameContent.text = name;
            resourceLabelCountContent.text = quantity;
            float maxWidth = Mathf.Min(230f, Mathf.Max(1f, Screen.width - 16f));
            float panelWidth = Mathf.Min(maxWidth, Mathf.Max(100f,
                Mathf.Max(nameStyle.CalcSize(resourceLabelNameContent).x,
                    countStyle.CalcSize(resourceLabelCountContent).x) + 20f));
            float nameHeight = nameStyle.CalcHeight(resourceLabelNameContent, panelWidth - 16f);
            float countHeight = hiveCaption ? countStyle.CalcHeight(resourceLabelCountContent, panelWidth - 16f) : titleSize + 3f;
            if (hiveCaption)
            {
                float heightBudget = Mathf.Max(32f, Screen.height - 24f);
                nameHeight = Mathf.Min(nameHeight, heightBudget * 0.45f);
                countHeight = Mathf.Min(countHeight, heightBudget * 0.55f - 8f);
            }
            float panelHeight = nameHeight + countHeight + 8f;
            float x = Mathf.Clamp(center - panelWidth * 0.5f, 8f, Mathf.Max(8f, Screen.width - panelWidth - 8f));
            float y = bottom + 4f;
            // Flip above the visible art at the lower screen edge, retaining attachment.
            if (y + panelHeight + trailingHeight > Screen.height - 8f) y = top + ink.y * height - panelHeight - trailingHeight - 4f;
            y = Mathf.Max(8f, y);
            Rect visibleArt = texture != null
                ? new Rect(left + ink.x * width, top + ink.y * height, ink.width * width, ink.height * height)
                : new Rect(point.x - size * 0.36f, top, size * 0.72f, size * 0.72f);
            Rect placement = reservedPanel ?? PlaceResourceLabelAroundHiveCaptions(
                new Rect(x, y, panelWidth, panelHeight), visibleArt, trailingHeight, hiveCaption);
            x = placement.x;
            y = placement.y;
            Color previous = GUI.color;
            DrawResourceLabelPlaque(new Rect(x, y, panelWidth, panelHeight), opacity * previous.a);
            GUI.color = new Color(1f, 1f, 1f, opacity * previous.a);
            GUI.Label(new Rect(x + 8f, y + 3f, panelWidth - 16f, nameHeight), name, nameStyle);
            GUI.Label(new Rect(x + 8f, y + 3f + nameHeight, panelWidth - 16f, countHeight), quantity, countStyle);
            GUI.color = previous;
            drawnMapLabelPanels.Add(new Rect(x, y, panelWidth, panelHeight + trailingHeight));
            return new Rect(x, y, panelWidth, panelHeight);
        }

        private Rect PlaceResourceLabelAroundHiveCaptions(Rect preferred, Rect visibleArt, float trailingHeight, bool ignoreLegacyHiveCaptions = false)
        {
            if (IsResourceLabelPlacementClear(preferred, trailingHeight, ignoreLegacyHiveCaptions)) return preferred;

            // Keep the callout adjacent to its own visible art. Do not move a world
            // entity or paint over the neighbouring hive's later-drawn caption.
            Rect above = new Rect(preferred.x, visibleArt.yMin - preferred.height - trailingHeight - 4f,
                preferred.width, preferred.height);
            if (IsResourceLabelPlacementClear(above, trailingHeight, ignoreLegacyHiveCaptions)) return above;

            float sideY = Mathf.Clamp(visibleArt.center.y - (preferred.height + trailingHeight) * 0.5f,
                8f, Mathf.Max(8f, Screen.height - preferred.height - trailingHeight - 8f));
            Rect leftSide = new Rect(visibleArt.xMin - preferred.width - 4f, sideY, preferred.width, preferred.height);
            if (IsResourceLabelPlacementClear(leftSide, trailingHeight, ignoreLegacyHiveCaptions)) return leftSide;
            Rect rightSide = new Rect(visibleArt.xMax + 4f, sideY, preferred.width, preferred.height);
            if (IsResourceLabelPlacementClear(rightSide, trailingHeight, ignoreLegacyHiveCaptions)) return rightSide;

            // Secondary deterministic caption-only search. The WORLD sprite
            // anchor never moves when the camera zoom or projection changes.
            for(int radius=55;radius<=275;radius+=55)
                for(int step=0;step<16;step++)
                {
                    float angle=step*Mathf.PI/8f;
                    var at=new Rect(preferred.x+Mathf.Cos(angle)*radius,
                        preferred.y+Mathf.Sin(angle)*radius,preferred.width,preferred.height);
                    if(IsResourceLabelPlacementClear(at,trailingHeight,ignoreLegacyHiveCaptions))
                        return at;
                }
            // Preserve the text and record collision diagnostics rather than
            // silently hiding the entity when the viewport has no free caption.
            return preferred;
        }

        private bool IsResourceLabelPlacementClear(Rect panel, float trailingHeight, bool ignoreLegacyHiveCaptions = false)
        {
            Rect occupied = new Rect(panel.x - 3f, panel.y - 3f, panel.width + 6f,
                panel.height + trailingHeight + 6f);
            if (occupied.xMin < 0f || occupied.yMin < 0f
                || occupied.xMax > Screen.width || occupied.yMax > Screen.height) return false;
            for (int i = 0; i < drawnMapLabelPanels.Count; i++)
                if (occupied.Overlaps(drawnMapLabelPanels[i])) return false;
            foreach(var hit in drawnMapHits)
                if(occupied.Overlaps(hit.Rectangle))return false;
            // All drawn and future artwork is reserved, including hives that
            // render after the bestiary. A caption never moves its sprite.
            // Actual panels are registered as drawn. Hive panels now avoid earlier
            // resource/bestiary panels; obsolete black-caption rectangles no longer exist.
            return true;
        }

        private static void DrawResourceLabelPlaque(Rect panel, float opacity)
        {
            Color previous = GUI.color;
            GUI.color = Color.white;
            // A restrained bronze rim and moss-brown inset echo the map palette.
            // Rounded shader edges avoid an extra asset and retain screen-space type.
            Rect shadow = new Rect(panel.x, panel.y + 1f, panel.width, panel.height);
            GUI.DrawTexture(shadow, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f,
                new Color(0.06f, 0.045f, 0.025f, 0.18f * opacity), 0f, 5f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f,
                new Color(0.67f, 0.51f, 0.27f, 0.64f * opacity), 0f, 5f);
            Rect inset = new Rect(panel.x + 1f, panel.y + 1f, panel.width - 2f, panel.height - 2f);
            GUI.DrawTexture(inset, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f,
                new Color(0.13f, 0.15f, 0.10f, 0.87f * opacity), 0f, 4f);
            GUI.color = previous;
        }
    }
}
