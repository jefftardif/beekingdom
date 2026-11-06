using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Playground
{
    /// <summary>
    /// One screen-space allocation contract for map artwork, captions and hits.
    /// This is ONLY a visual projection. It never changes server-owned world
    /// coordinates, leases, spawning or economic state.
    /// </summary>
    public static class WorldMapVisualSeparation
    {
        public sealed class Claim
        {
            public string Key;
            public Vector2 Anchor;
            public float ArtWidth, ArtHeight;
            public float ArtTopFraction = .58f;
            // Alpha ink offset relative to the original draw anchor.
            public Vector2 ArtOffset;
            public Vector2 CaptionSize;
            public bool CaptionAbove;
            public bool FixedCaptionOffset;
            public Vector2 CaptionOffset;
            public float ExtraTop, ExtraBottom;
            public int Priority;
            public bool Pinned;
        }

        public sealed class Slot
        {
            public string Key;
            public Vector2 Anchor;
            public Vector2 Point;
            public Rect Art;
            public Rect Caption;
            public Rect Extra;
            public bool HasCaption, HasExtra;
            public bool Relocated => Vector2.Distance(Anchor, Point) > .5f;
            public bool Contains(Vector2 mouse)
            {
                return Art.Contains(mouse)
                    || (HasCaption && Caption.Contains(mouse))
                    || (HasExtra && Extra.Contains(mouse));
            }
        }

        public sealed class Layout
        {
            public readonly Dictionary<string, Slot> Slots =
                new Dictionary<string, Slot>(StringComparer.Ordinal);
            public readonly List<string> Unresolved = new List<string>();
            public bool IsClear => Unresolved.Count == 0;
            public bool TryGet(string key, out Slot slot) => Slots.TryGetValue(key, out slot);
            public bool TryHit(Vector2 point, out string key)
            {
                foreach (var pair in Slots)
                    if (pair.Value.Contains(point)) { key = pair.Key; return true; }
                key = null;
                return false;
            }
        }

        private static Slot Make(Claim c, Vector2 point)
        {
            Rect art = new Rect(point.x + c.ArtOffset.x - c.ArtWidth * .5f,
                point.y + c.ArtOffset.y - c.ArtHeight * c.ArtTopFraction,
                c.ArtWidth, c.ArtHeight);
            bool labelled = c.CaptionSize.x > 0 && c.CaptionSize.y > 0;
            Rect caption = !labelled ? new Rect() : c.FixedCaptionOffset
                ? new Rect(point.x + c.CaptionOffset.x, point.y + c.CaptionOffset.y,
                    c.CaptionSize.x, c.CaptionSize.y)
                : c.CaptionAbove
                    ? new Rect(point.x + c.ArtOffset.x - c.CaptionSize.x * .5f,
                        art.yMin - c.CaptionSize.y - 4f, c.CaptionSize.x,c.CaptionSize.y)
                    : new Rect(point.x + c.ArtOffset.x - c.CaptionSize.x * .5f,
                        art.yMax + 4f, c.CaptionSize.x,c.CaptionSize.y);
            bool extra = c.ExtraTop > 0 || c.ExtraBottom > 0;
            Rect ornament = !extra ? new Rect() : new Rect(
                point.x - Math.Max(c.ArtWidth, 76f) * .5f,
                art.yMin - c.ExtraTop,
                Math.Max(c.ArtWidth, 76f),
                art.height + c.ExtraTop + c.ExtraBottom);
            return new Slot { Key = c.Key, Anchor = c.Anchor, Point = point,
                Art = art, Caption = caption, Extra = ornament,
                HasCaption = labelled, HasExtra = extra };
        }

        private static IEnumerable<Rect> Parts(Slot s)
        {
            yield return s.Art;
            if (s.HasCaption) yield return s.Caption;
            if (s.HasExtra) yield return s.Extra;
        }

        public static bool Conflicts(Slot a, Slot b, float gap)
        {
            foreach (Rect x in Parts(a))
                foreach (Rect y in Parts(b))
                {
                    var wide = new Rect(x.xMin - gap * .5f, x.yMin - gap * .5f,
                        x.width + gap, x.height + gap);
                    if (wide.Overlaps(y)) return true;
                }
            return false;
        }

        private static bool Fits(Slot slot, Rect viewport)
        {
            foreach (var r in Parts(slot))
                if (r.xMin < viewport.xMin || r.yMin < viewport.yMin
                    || r.xMax > viewport.xMax || r.yMax > viewport.yMax)
                    return false;
            return true;
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in value) { hash ^= c; hash *= 16777619; }
                return hash;
            }
        }

        public static Layout Solve(IReadOnlyList<Claim> claims, Rect viewport, float gap = 10f)
        {
            if (claims == null || viewport.width <= 0 || viewport.height <= 0 ||
                float.IsNaN(gap) || gap < 0) throw new ArgumentException("layout_input_invalid");
            var result = new Layout();
            var accepted = new List<Slot>();
            var ordered = new List<Claim>(claims);
            ordered.Sort((a,b) => {
                int byPriority = a.Priority.CompareTo(b.Priority);
                return byPriority != 0 ? byPriority : string.CompareOrdinal(a.Key,b.Key);
            });
            foreach (var c in ordered)
            {
                if (c == null || string.IsNullOrEmpty(c.Key) ||
                    result.Slots.ContainsKey(c.Key) ||
                    c.ArtWidth <= 0 || c.ArtHeight <= 0 ||
                    float.IsNaN(c.Anchor.x) || float.IsNaN(c.Anchor.y))
                    throw new ArgumentException("layout_claim_invalid");
                var slot = Make(c, c.Anchor);
                bool clear = Fits(slot, viewport) && !accepted.Exists(s => Conflicts(s,slot,gap));
                if (!clear && !c.Pinned)
                {
                    float step = Mathf.Max(24f, Mathf.Min(70f, Mathf.Max(c.ArtWidth,c.ArtHeight) * .5f));
                    // Deterministic candidate rings, independent of chunk iteration order.
                    float angleShift = (StableHash(c.Key) % 16) * (Mathf.PI / 8f);
                    for (int ring = 1;ring <= 22 && !clear;ring++)
                        for (int i = 0;i < 16 && !clear;i++)
                        {
                            float angle = i * (Mathf.PI / 8f) + angleShift;
                            Vector2 p = c.Anchor + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (step*ring);
                            Slot candidate = Make(c,p);
                            if (!Fits(candidate,viewport) || accepted.Exists(s => Conflicts(s,candidate,gap)))
                                continue;
                            slot = candidate;clear = true;
                        }
                }
                if (!clear && !c.Pinned)
                {
                    // Radial candidates can miss thin corridors between large
                    // labels. Search the complete viewport as a bounded,
                    // deterministic last resort; never discard an entity.
                    float bestDistance = float.MaxValue;
                    Slot bestSlot = null;
                    for (float y = viewport.yMin + 14f; y <= viewport.yMax - 14f; y += 28f)
                    for (float x = viewport.xMin + 14f; x <= viewport.xMax - 14f; x += 28f)
                    {
                        Vector2 candidatePoint = new Vector2(x, y);
                        float distance = (candidatePoint - c.Anchor).sqrMagnitude;
                        if (distance >= bestDistance) continue;
                        Slot candidateSlot = Make(c, candidatePoint);
                        if (!Fits(candidateSlot, viewport) ||
                            accepted.Exists(other => Conflicts(other, candidateSlot, gap)))
                            continue;
                        bestSlot = candidateSlot;
                        bestDistance = distance;
                    }
                    if (bestSlot != null) { slot = bestSlot; clear = true; }
                }
                if (!clear) result.Unresolved.Add(c.Key);
                result.Slots.Add(c.Key,slot);
                accepted.Add(slot);
            }
            return result;
        }
    }
}
