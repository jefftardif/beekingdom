using BeeKingdom.Alliance.Territories;
using BeeKingdom.Authentication;
using BeeKingdom.Authentication.Models;

namespace BeeKingdom.Server;

public sealed record TerritoryFoundationRequest(string RequestId, decimal X, decimal Y);
public sealed record TerritoryConstructionWorkRequest(Guid BuildingId, string Source, string ReceiptId);

// Candidate handler, deliberately NOT registered in Program/production DI.
// Deployment must explicitly select an authoritative registry and coordinate every
// legacy spatial/member writer before mapping this route. No client actor/world/role.
public static class TerritoryFoundationEndpoints
{
    public static IResult Credit(TokenValidationResult authentication, DurableTerritoryWorldRegistry? registry,
        TerritoryConstructionWorkRequest? request, Func<string, string, VerifiedTerritoryWork?>? verifiedServerReceipts)
    {
        if (!authentication.IsValid || authentication.PlayerId.Value == Guid.Empty)
            return Results.Json(new { code = "construction.session_required" }, statusCode: 401);
        if (registry is null) return Results.Json(new { code = "construction.registry_unavailable" }, statusCode: 503);
        if (request is null) return Results.BadRequest(new { code = "construction.request_required" });
        try
        {
            var result = registry.CreditVerifiedWork(authentication.PlayerId.Value, request.BuildingId,
                request.Source, request.ReceiptId, verifiedServerReceipts);
            return Results.Json(result, statusCode: result.Applied || result.Replayed ? 200 : 409);
        }
        catch (IOException) { return Results.Json(new { code = "construction.storage_unavailable" }, statusCode: 503); }
        catch (UnauthorizedAccessException) { return Results.Json(new { code = "construction.storage_unavailable" }, statusCode: 503); }
        catch (System.Text.Json.JsonException) { return Results.Json(new { code = "construction.storage_invalid" }, statusCode: 503); }
    }

    public static void Map(WebApplication app, Func<HttpContext, AuthenticationManager, TokenValidationResult> authenticate)
    {
        app.MapPost("/alliance/v1/territory/foundation", (HttpContext context,
            AuthenticationManager authentication, TerritoryFoundationRequest request) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Found(authenticate(context, authentication),
                context.RequestServices.GetService<DurableTerritoryWorldRegistry>(), request);
        });
    }

    public static IResult Found(TokenValidationResult authentication, DurableTerritoryWorldRegistry? registry,
        TerritoryFoundationRequest? request)
    {
        if (!authentication.IsValid || authentication.PlayerId.Value == Guid.Empty)
            return Results.Json(new { code = "foundation.session_required" }, statusCode: 401);
        if (registry is null)
            return Results.Json(new { code = "foundation.registry_unavailable" }, statusCode: 503);
        if (request is null) return Results.BadRequest(new { code = "foundation.request_required" });
        try
        {
            var result = registry.Found(authentication.PlayerId.Value, request.RequestId, new(request.X, request.Y));
            return Results.Json(result, statusCode: result.Created ? 201 : result.Replayed ? 200 : 409);
        }
        catch (IOException) { return Results.Json(new { code = "foundation.storage_unavailable" }, statusCode: 503); }
        catch (UnauthorizedAccessException) { return Results.Json(new { code = "foundation.storage_unavailable" }, statusCode: 503); }
        catch (System.Text.Json.JsonException) { return Results.Json(new { code = "foundation.storage_invalid" }, statusCode: 503); }
    }
}
