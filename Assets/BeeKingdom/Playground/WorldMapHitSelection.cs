using System;
using System.Collections.Generic;

namespace BeeKingdom.Playground
{
    internal enum WorldMapHitKind { None, Hive, Resource, Bestiary, PointOfInterest }

    internal struct WorldMapHitCandidate
    {
        internal WorldMapHitKind Kind;
        internal string Id;
        internal float Distance;
        internal bool WithinCommittedBounds;
        internal WorldMapHitCandidate(WorldMapHitKind kind, string id, float distance, bool withinCommittedBounds=false)
        { Kind = kind; Id = id; Distance = distance; WithinCommittedBounds=withinCommittedBounds; }
    }

    internal static class WorldMapHitSelection
    {
        internal static int ChooseIndex(IReadOnlyList<WorldMapHitCandidate> candidates, float zoom,
            WorldMapHitKind previousKind, string previousId)
        {
            if (float.IsNaN(zoom) || float.IsInfinity(zoom) || zoom <= 0f) return -1;
            float closest = float.MaxValue;
            int first = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                WorldMapHitCandidate c = candidates[i];
                if (Eligible(c, zoom) && c.Distance < closest) { closest = c.Distance; first = i; }
            }
            if (first < 0) return -1;
            int previous = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                WorldMapHitCandidate c = candidates[i];
                if (c.Kind == previousKind && c.Id == previousId && Eligible(c, zoom)
                    && Math.Abs(c.Distance - closest) <= 0.01f) { previous = i; break; }
            }
            if (previous < 0) return first;
            for (int step = 1; step <= candidates.Count; step++)
            {
                int index = (previous + step) % candidates.Count;
                WorldMapHitCandidate c = candidates[index];
                if (Eligible(c, zoom) && Math.Abs(c.Distance - closest) <= 0.01f) return index;
            }
            return first;
        }

        private static bool Eligible(WorldMapHitCandidate c, float zoom)
        {
            float radius = c.Kind == WorldMapHitKind.Hive ? 58f : c.Kind == WorldMapHitKind.Resource ? 48f
                : c.Kind == WorldMapHitKind.Bestiary ? 62f : c.Kind == WorldMapHitKind.PointOfInterest ? 66f : 0f;
            return radius > 0f && !float.IsNaN(c.Distance) && !float.IsInfinity(c.Distance) && c.Distance >= 0f
                && (c.WithinCommittedBounds || c.Distance <= Math.Max(12f, radius * zoom));
        }
    }
}
