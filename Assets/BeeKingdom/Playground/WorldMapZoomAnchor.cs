using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// Local camera projection for the WorldMap. Neither this class nor its
    /// callers mutate an entity's world or server-authoritative coordinates.
    /// </summary>
    public static class WorldMapZoomAnchor
    {
        public static Vector2 ScreenPoint(Vector2 world, Vector2 center,
            float zoom, Vector2 viewport)
        {
            return viewport * .5f + (world - center) * zoom;
        }

        public static Vector2 WorldPoint(Vector2 screen, Vector2 center,
            float zoom, Vector2 viewport)
        {
            return center + (screen - viewport * .5f) / Mathf.Max(.01f, zoom);
        }

        public static Vector2 CenterForCursor(Vector2 worldAtCursor,
            Vector2 cursor, float nextZoom, Vector2 viewport)
        {
            return worldAtCursor - (cursor - viewport * .5f) /
                Mathf.Max(.01f, nextZoom);
        }

        public static void Step(ref Vector2 currentCenter, ref float currentZoom,
            Vector2 targetCenter, float targetZoom, float damping, float deltaTime)
        {
            float weight = 1f - Mathf.Exp(-Mathf.Max(0f, damping) *
                Mathf.Max(0f, deltaTime));
            float nextZoom = Mathf.Lerp(currentZoom, targetZoom, weight);
            // Screen(world) = screenCenter + world*zoom - center*zoom.
            // Interpolating center*zoom with the same easing as zoom itself
            // keeps a world point under its screen cursor throughout the fade.
            Vector2 projectedOffset = Vector2.Lerp(currentCenter * currentZoom,
                targetCenter * targetZoom, weight);
            currentCenter = projectedOffset / Mathf.Max(.01f, nextZoom);
            currentZoom = nextZoom;
        }
    }
}