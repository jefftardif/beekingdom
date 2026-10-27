using UnityEngine;

namespace BeeKingdom.Playground
{
    internal static class WorldMapHudLayout
    {
        internal static Rect Pollination(float width, float height)
        {
            float w = Mathf.Min(216f, Mathf.Max(1f, width - 24f));
            // Coordinates end at y=24; the bear-den control ends at y=60.
            return new Rect((width - w) * 0.5f, 68f, w, 36f);
        }

        internal static Rect ChatButton(float width, float height)
        {
            bool portrait = width < 700f || height > width * 1.15f;
            float bottom = portrait ? height - 262f : width < 800f ? height - 164f : height - 12f;
            return new Rect(12f, Mathf.Max(108f, bottom - 44f), Mathf.Min(190f, Mathf.Max(1f, width - (portrait ? 152f : 24f))), 44f);
        }

        internal static Rect MarchCounter(float width, float height)
        {
            Rect chat = ChatButton(width, height);
            return new Rect(chat.x, chat.y - 28f, chat.width, 20f);
        }

        internal static Rect ChatPanel(float width, float height)
        {
            float bottom = Mathf.Max(MarchCounter(width, height).y - 8f, Mathf.Min(height - 8f, 246f));
            float w = Mathf.Min(520f, Mathf.Max(1f, width - 24f));
            float h = Mathf.Min(360f, Mathf.Max(1f, bottom - 8f));
            return new Rect(12f, bottom - h, w, h);
        }

        internal static Rect RelativeToSafeChat(Rect rect)
        {
            Rect chat = ChatButton(Screen.width, Screen.height);
            Rect safeChat = WithinSafeArea(chat);
            rect.position += safeChat.position - chat.position;
            return WithinSafeArea(rect);
        }

        internal static Rect WithinSafeArea(Rect rect)
        {
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, Screen.width, Screen.height);
            safe.y = Screen.height - safe.yMax;
            rect.width = Mathf.Min(rect.width, Mathf.Max(1f, safe.width - 16f));
            rect.height = Mathf.Min(rect.height, Mathf.Max(1f, safe.height - 16f));
            rect.x = Mathf.Clamp(rect.x, safe.xMin + 8f, Mathf.Max(safe.xMin + 8f, safe.xMax - rect.width - 8f));
            rect.y = Mathf.Clamp(rect.y, safe.yMin + 8f, Mathf.Max(safe.yMin + 8f, safe.yMax - rect.height - 8f));
            return rect;
        }
    }
}
