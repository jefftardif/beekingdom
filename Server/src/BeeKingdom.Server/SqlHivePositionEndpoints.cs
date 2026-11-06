using BeeKingdom.Authentication;
using BeeKingdom.Authentication.Models;
using Microsoft.Data.SqlClient;

namespace BeeKingdom.TerritorySqlCandidate;

// Explicit test/candidate registration. No production Program registration or mutation endpoint.
public static class SqlHivePositionEndpoints
{
    public static void Map(WebApplication app, SqlHivePositionStore store, string world, string surface,
        Func<HttpContext, AuthenticationManager, TokenValidationResult> authenticate)
    {
        app.MapGet("/game/v1/world/hives/mine/{hiveId:guid}",
            (HttpContext context, AuthenticationManager manager, Guid hiveId, string? worldId, string? surfaceId) =>
        {
            context.Response.Headers.CacheControl = "private, no-store";
            context.Response.Headers.Pragma = "no-cache";
            var auth = authenticate(context, manager);
            if (!auth.IsValid || auth.PlayerId.Value == Guid.Empty) return Results.Unauthorized();
            if (worldId != world || surfaceId != surface) return Results.Json(new { code = "hive_world_scope_denied" }, statusCode: 403);
            try
            {
                var position = store.ReadCanonicalOwn(auth.PlayerId.Value, hiveId);
                return position is null
                    ? Results.Json(new { code = "hive_position_not_reconciled" }, statusCode: 409)
                    : Results.Ok(position);
            }
            catch (InvalidOperationException error) when (error.Message is "canonical_hive_not_owned" or "canonical_colony_scope_denied" or "canonical_hive_world_assignment_required")
            { return Results.Json(new { code = error.Message }, statusCode: 403); }
            catch (InvalidDataException)
            { return Results.Json(new { code = "hive_identity_inconsistent" }, statusCode: 503); }
            catch (InvalidOperationException)
            { return Results.Json(new { code = "hive_position_authority_unavailable" }, statusCode: 503); }
            catch (SqlException)
            { return Results.Json(new { code = "hive_position_storage_unavailable" }, statusCode: 503); }
        });
    }
}
