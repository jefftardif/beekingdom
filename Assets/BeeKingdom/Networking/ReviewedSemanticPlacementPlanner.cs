using System;
using System.Collections.Generic;
using System.Linq;

namespace BeeKingdom.MapPlacement
{
    /// <summary>
    /// Preview-stage bridge between the existing, fingerprinted and reviewed
    /// raster semantic terrain and the deterministic shared footprint planner.
    ///
    /// This pure planner does not persist placements or grant player ownership.
    /// Server authority must perform version/identity checks and commit atomically.
    /// In particular, the old unreviewed Wave6 coordinate mask is NEVER a source
    /// of terrain approval here.
    /// </summary>
    public static class ReviewedSemanticPlacementPlanner
    {
        private static PlacementPlan Denied(string code, string entity = null)
        {
            return new PlacementPlan(code, Array.Empty<MapPlacementSnapshot>(), entity);
        }

        public static PlacementPlan Build(SemanticTerrain terrain,
            TerrainPlacementCoverage scope,
            IReadOnlyList<MapPlacementSnapshot> existing,
            IReadOnlyList<PlacementRequest> requests,
            double gap, long occupationRevision,
            IReadOnlyCollection<string> historicalPinned = null)
        {
            if (terrain == null || scope == null || !scope.VerifiedGeometry ||
                !scope.WorldBounds.Valid || scope.Revision != terrain.Revision ||
                string.IsNullOrWhiteSpace(terrain.ContentSha256))
                return Denied("reviewed_semantic_terrain_missing_or_revision_mismatch");

            if (existing == null || requests == null ||
                !MapPlacementRules.Finite(gap) || gap < 0 ||
                occupationRevision <= 0 || requests.Count + existing.Count > 10000)
                return Denied("invalid_input");

            if (string.IsNullOrWhiteSpace(scope.WorldId) ||
                string.IsNullOrWhiteSpace(scope.SurfaceId) ||
                string.IsNullOrWhiteSpace(scope.CoordinateUnit) ||
                scope.EntityTerrainFamilies == null)
                return Denied("terrain_scope_unknown");

            // The semantic raster is the ONLY admission authority here. The
            // broad rectangles below are a compatibility envelope for the
            // existing deterministic overlap planner, not permission to place
            // something outside certified Land/River/Lake pixels.
            var allowed = new Dictionary<string, IReadOnlyList<PlacementRect>>(
                StringComparer.Ordinal)
            {
                { "Land", new[] { scope.WorldBounds } },
                { "Water", new[] { scope.WorldBounds } }
            };
            var families = new Dictionary<string, string>(StringComparer.Ordinal);
            var certified = new List<PlacementRequest>(requests.Count);

            foreach (PlacementRequest request in requests)
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Kind) ||
                    string.IsNullOrWhiteSpace(request.EntityId) ||
                    request.Candidates == null || request.Candidates.Count > 256)
                    return Denied("request_invalid_or_duplicate");

                string identity = request.Kind + ":" + request.EntityId;
                if (!scope.EntityTerrainFamilies.TryGetValue(identity, out string family) ||
                    (family != "Land" && family != "Water"))
                    return Denied("entity_terrain_family_not_certified", request.EntityId);

                families[identity] = family;
                bool water = family == "Water";
                var candidates = new List<MapPlacementSnapshot>();
                foreach (MapPlacementSnapshot candidate in request.Candidates)
                {
                    if (MapPlacementRules.Validate(candidate, request.Kind,
                        request.EntityId, scope.WorldId, scope.SurfaceId,
                        scope.CoordinateUnit) != "verified" ||
                        candidate.TerrainRevision != terrain.Revision ||
                        candidate.OccupationRevision != occupationRevision)
                        continue;

                    PlacementRect footprint = MapPlacementRules.Bounds(candidate);
                    if (!scope.WorldBounds.Contains(footprint.Expand(gap)))
                        continue;

                    // Every footprint pixel, including the safety margin,
                    // must belong to a reviewed semantic class. Unknown,
                    // unavailable neighbour tiles, shores and unsupported
                    // families all fail closed.
                    if (!terrain.AllowsNewFootprint(footprint, gap, water))
                        continue;
                    candidates.Add(MapPlacementRules.Copy(candidate));
                }

                certified.Add(new PlacementRequest
                {
                    Kind = request.Kind,
                    EntityId = request.EntityId,
                    Candidates = candidates.AsReadOnly()
                });
            }

            // Existing reservations cannot be repositioned or erased by this
            // planner. Their overlaps remain auditable; a conflict aborts the
            // entire transaction rather than silently dropping an entity.
            if (existing.Any(x => x == null)) return Denied("fixed_occupation_invalid");

            foreach (MapPlacementSnapshot fixedPlacement in existing)
            {
                if (string.IsNullOrWhiteSpace(fixedPlacement.Kind) ||
                    string.IsNullOrWhiteSpace(fixedPlacement.EntityId))
                    return Denied("fixed_identity_missing");

                string identity = fixedPlacement.Kind + ":" + fixedPlacement.EntityId;
                if (!scope.EntityTerrainFamilies.TryGetValue(identity, out string family) ||
                    (family != "Land" && family != "Water"))
                    return Denied("fixed_terrain_family_unknown", fixedPlacement.EntityId);
                families[identity] = family;

                // Existing placements are NOT implicitly certified because the
                // caller supplies a rectangle. They must pass the same reviewed
                // raster as new objects, unless the server explicitly marks
                // their identity as historical (retain in place, never re-grant
                // a new entitlement or infer land/water from old coordinates).
                bool historical = historicalPinned != null &&
                    historicalPinned.Contains(identity);
                if (!historical)
                {
                    if (MapPlacementRules.Validate(fixedPlacement, fixedPlacement.Kind,
                        fixedPlacement.EntityId, scope.WorldId, scope.SurfaceId,
                        scope.CoordinateUnit) != "verified" ||
                        fixedPlacement.TerrainRevision != terrain.Revision ||
                        fixedPlacement.OccupationRevision != occupationRevision ||
                        !terrain.AllowsNewFootprint(
                            MapPlacementRules.Bounds(fixedPlacement), gap,
                            family == "Water"))
                        return Denied("fixed_semantic_terrain_unverified",
                            fixedPlacement.EntityId);
                }
            }

            var certifiedScope = new TerrainPlacementCoverage
            {
                WorldId = scope.WorldId,
                SurfaceId = scope.SurfaceId,
                CoordinateUnit = scope.CoordinateUnit,
                Revision = scope.Revision,
                WorldBounds = scope.WorldBounds,
                VerifiedGeometry = true,
                AllowedRegions = allowed,
                EntityTerrainFamilies = families
            };

            // Do not enable packRegionEdges: it invents coordinates without a
            // second raster admission pass. Only reviewed candidate coordinates
            // from above are eligible.
            return SharedPlacementPlanner.Build(
                certifiedScope, existing, certified, gap, occupationRevision,
                historicalPinned, packRegionEdges: false);
        }
    }
}