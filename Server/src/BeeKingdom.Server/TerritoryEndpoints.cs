using BeeKingdom.Alliance.Models;
using BeeKingdom.Alliance.Repositories;
using BeeKingdom.Alliance.Territories;
using BeeKingdom.Authentication;
using BeeKingdom.Authentication.Models;
using BeeKingdom.Shared.ValueObjects;
using System.Text.Json;

namespace BeeKingdom.Server;

public sealed class TerritoryReadOptions
{
    public bool Enabled { get; set; }
    public bool UseCandidateRegistry { get; set; }
    public string WorldId { get; set; } = "";
    public string SurfaceId { get; set; } = "";
    public string CoordinateUnit { get; set; } = "";
    public string Directory { get; set; } = "";
}

// A real world-membership provider must be registered by the deployment adapter.
// The existing account token does not contain a world assignment. Never infer it
// from the requested world, the default server world, or alliance membership alone.
public interface ITerritoryWorldAccess
{
    bool CanReadWorld(PlayerId player, string worldId);
}

public sealed record TerritoryReadResponse(string ContractVersion, Guid PlayerId, Guid AllianceId, string WorldId,
    string SurfaceId, long MembershipRevision, DateTimeOffset ServerTimeUtc, TerritoryMapView View);

public static class TerritoryEndpoints
{
    public const string Version = "bk-alliance-territory-v1-candidate";
    public static void Map(WebApplication app, Func<HttpContext, AuthenticationManager, TokenValidationResult> authenticate)
    {
        app.MapGet("/alliance/v1/territory/mine", (HttpContext context,
            AuthenticationManager authentication, IAllianceRepository alliances,
            string? worldId, string? surfaceId, long? minimumRevision, Guid? minimumAllianceId) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var options = app.Configuration.GetSection("AllianceTerritoryRead").Get<TerritoryReadOptions>() ?? new();
            return Read(authenticate(context,authentication),options,
                context.RequestServices.GetService<ITerritoryWorldAccess>(),alliances,
                worldId,surfaceId,minimumRevision ?? 0,TimeProvider.System,minimumAllianceId,
                context.RequestServices.GetService<DurableTerritoryWorldRegistry>());
        });
    }

    public static IResult Read(TokenValidationResult auth, TerritoryReadOptions options,
        ITerritoryWorldAccess? worlds, IAllianceRepository alliances,
        string? worldId, string? surfaceId, long minimumRevision, TimeProvider clock, Guid? minimumAllianceId = null,
        DurableTerritoryWorldRegistry? candidateRegistry = null)
    {
        IResult Error(int status,string code) => Results.Json(new {code},statusCode:status);
        if (!auth.IsValid || auth.PlayerId.Value == Guid.Empty) return Error(401,"territory.session_required");
        if (!options.Enabled) return Error(503,"territory.read_disabled");
        if (string.IsNullOrWhiteSpace(options.WorldId) || string.IsNullOrWhiteSpace(options.SurfaceId)
            || string.IsNullOrWhiteSpace(options.CoordinateUnit) || string.IsNullOrWhiteSpace(options.Directory))
            return Error(503,"territory.configuration_unavailable");
        if (minimumRevision < 0 || string.IsNullOrWhiteSpace(worldId) || worldId.Length > 128
            || string.IsNullOrWhiteSpace(surfaceId) || surfaceId.Length > 128)
            return Error(400,"territory.invalid_request");
        if (worldId != options.WorldId || surfaceId != options.SurfaceId)
            return Error(403,"territory.world_scope_denied");
        // Explicit isolated-authority mode only. It never merges registry membership
        // with unsynchronized legacy repository membership or falls back on failure.
        if (options.UseCandidateRegistry)
        {
            if (candidateRegistry is null) return Error(503,"territory.registry_unavailable");
            try
            {
                var read = candidateRegistry.ReadForSession(auth.PlayerId.Value, options.CoordinateUnit, minimumRevision, minimumAllianceId);
                if (read is null) return Error(403,"territory.membership_required");
                if (read.WorldId != worldId || read.SurfaceId != surfaceId) return Error(403,"territory.world_scope_denied");
                return Results.Ok(new TerritoryReadResponse(Version,auth.PlayerId.Value,read.AllianceId,worldId,surfaceId,
                    read.MembershipRevision,clock.GetUtcNow(),read.View));
            }
            catch (IOException) { return Error(503,"territory.storage_unavailable"); }
            catch (UnauthorizedAccessException) { return Error(503,"territory.storage_unavailable"); }
            catch (JsonException) { return Error(503,"territory.storage_invalid"); }
        }
        if (worlds is null) return Error(503,"territory.world_membership_unavailable");
        if (!worlds.CanReadWorld(auth.PlayerId,worldId)) return Error(403,"territory.world_scope_denied");
        var membership = alliances.GetActiveMembershipForPlayer(auth.PlayerId);
        if (membership is null || membership.RemovedAtUtc is not null) return Error(403,"territory.membership_required");
        var alliance = alliances.Get(membership.AllianceId);
        if (alliance is null || alliance.Status != AllianceStatus.Active) return Error(403,"territory.alliance_inactive");
        try
        {
            var store = new DurableTerritoryAuthorityStore(options.Directory,clock);
            long floor = minimumAllianceId is null || minimumAllianceId == membership.AllianceId.Value ? minimumRevision : 0;
            var view = new TerritoryMapReader(store).Read(new(membership.AllianceId.Value,worldId,surfaceId,floor),options.CoordinateUnit);
            // Recheck after I/O; never return a former alliance's outline on a known switch.
            var current = alliances.GetActiveMembershipForPlayer(auth.PlayerId);
            if (current is null || current.RemovedAtUtc is not null || current.AllianceId != membership.AllianceId
                || current.Revision != membership.Revision || !worlds.CanReadWorld(auth.PlayerId,worldId)
                || alliances.Get(membership.AllianceId)?.Status != AllianceStatus.Active)
                return Error(403,"territory.membership_changed");
            return Results.Ok(new TerritoryReadResponse(Version,auth.PlayerId.Value,membership.AllianceId.Value,worldId,surfaceId,
                membership.Revision,clock.GetUtcNow(),view));
        }
        catch (IOException) { return Error(503,"territory.storage_unavailable"); }
        catch (UnauthorizedAccessException) { return Error(503,"territory.storage_unavailable"); }
        catch (JsonException) { return Error(503,"territory.storage_invalid"); }
        catch (ArgumentException) { return Error(503,"territory.storage_invalid"); }
    }
}
