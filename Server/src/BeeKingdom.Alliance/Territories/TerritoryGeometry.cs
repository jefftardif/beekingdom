using System;
using System.Collections.Generic;

namespace BeeKingdom.Alliance.Territories;

public readonly record struct TerritoryPoint(decimal X, decimal Y);
public readonly record struct TerritoryMapBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY);
public sealed record TerritoryGeometryResult(bool Valid, string Code, decimal? Area);

/// <summary>
/// Candidate technical geometry domain: one simple ring, no holes, at most 1024
/// vertices, coordinates of magnitude at most 1,000,000 with up to six decimals.
/// This is not a map size or gameplay grid. Never round incoming coordinates.
/// Int128 determinants preserve exact predicates within this explicit domain.
/// </summary>
public static class TerritoryGeometry
{
    public const int MaximumVertices = 1024;
    private const decimal Scale = 1000000m;
    private const decimal Magnitude = 1000000m;

    public static TerritoryGeometryResult Validate(IReadOnlyList<TerritoryPoint>? ring, TerritoryMapBounds bounds)
    {
        TerritoryGeometryResult Reject(string code) => new(false, code, null);
        if (ring is null) return Reject("geometry_missing");
        if (ring.Count == 0) return Reject("geometry_empty");
        if (!Supported(new(bounds.MinX, bounds.MinY)) || !Supported(new(bounds.MaxX, bounds.MaxY))
            || bounds.MinX >= bounds.MaxX || bounds.MinY >= bounds.MaxY)
            return Reject("map_bounds_unsupported");
        if (ring.Count > MaximumVertices + 1) return Reject("vertex_budget_exceeded");
        int count = ring.Count;
        if (count > 1 && ring[0] == ring[count - 1]) count--;
        if (count < 3) return Reject("too_few_vertices");
        if (count > MaximumVertices) return Reject("vertex_budget_exceeded");
        for (int i = 0; i < count; i++)
        {
            if (!Supported(ring[i])) return Reject("coordinate_domain_unsupported");
            if (ring[i].X < bounds.MinX || ring[i].X > bounds.MaxX
                || ring[i].Y < bounds.MinY || ring[i].Y > bounds.MaxY)
                return Reject("outside_map_bounds");
            for (int j = 0; j < i; j++)
                if (ring[i] == ring[j]) return Reject("duplicate_vertex");
        }
        Int128 areaTwice = 0;
        for (int i = 1; i < count - 1; i++) areaTwice += Cross(ring[0], ring[i], ring[i + 1]);
        if (areaTwice == 0) return Reject("zero_area");
        for (int i = 0; i < count; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % count]; var c = ring[(i + 2) % count];
            if (Cross(a, b, c) == 0 && OnSegment(a, c, b)) return Reject("adjacent_edges_overlap");
            for (int j = i + 1; j < count; j++)
            {
                if (j == i + 1 || (i == 0 && j == count - 1)) continue;
                if (Intersects(a, b, ring[j], ring[(j + 1) % count])) return Reject("self_intersection");
            }
        }
        return new(true, "valid_simple_ring", (decimal)Int128.Abs(areaTwice) / (2m * Scale * Scale));
    }

    // Caller supplies a validated ring. Inclusion is geometric only: it grants no
    // access and never privatizes a public floral site. Boundary points count inside.
    public static bool ContainsValidated(IReadOnlyList<TerritoryPoint> ring, TerritoryPoint point)
    {
        if (!Supported(point)) return false;
        int count = ring.Count;
        if (count > 1 && ring[0] == ring[count - 1]) count--;
        int winding = 0;
        for (int i = 0; i < count; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % count]; var cross = Cross(a, b, point);
            if (cross == 0 && OnSegment(a, point, b)) return true;
            if (a.Y <= point.Y && b.Y > point.Y && cross > 0) winding++;
            else if (a.Y > point.Y && b.Y <= point.Y && cross < 0) winding--;
        }
        return winding != 0;
    }

    private static bool Supported(TerritoryPoint point) =>
        Math.Abs(point.X) <= Magnitude && Math.Abs(point.Y) <= Magnitude
        && decimal.Truncate(point.X * Scale) == point.X * Scale
        && decimal.Truncate(point.Y * Scale) == point.Y * Scale;
    private static Int128 Cross(TerritoryPoint a, TerritoryPoint b, TerritoryPoint c)
    {
        long ax = (long)(a.X * Scale), ay = (long)(a.Y * Scale);
        return (Int128)((long)(b.X * Scale) - ax) * ((long)(c.Y * Scale) - ay)
            - (Int128)((long)(b.Y * Scale) - ay) * ((long)(c.X * Scale) - ax);
    }
    private static bool OnSegment(TerritoryPoint a, TerritoryPoint p, TerritoryPoint b) =>
        p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X)
        && p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);
    private static bool Intersects(TerritoryPoint a, TerritoryPoint b, TerritoryPoint c, TerritoryPoint d)
    {
        var x = Cross(a, b, c); var y = Cross(a, b, d);
        var z = Cross(c, d, a); var w = Cross(c, d, b);
        if ((x == 0 && OnSegment(a, c, b)) || (y == 0 && OnSegment(a, d, b))
            || (z == 0 && OnSegment(c, a, d)) || (w == 0 && OnSegment(c, b, d))) return true;
        return ((x < 0 && y > 0) || (x > 0 && y < 0)) && ((z < 0 && w > 0) || (z > 0 && w < 0));
    }
}
