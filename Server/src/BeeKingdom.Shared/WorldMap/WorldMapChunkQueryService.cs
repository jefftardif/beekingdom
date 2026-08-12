using BeeKingdom.Shared.ValueObjects;
using BeeKingdom.Shared.Versioning;

namespace BeeKingdom.Shared.WorldMap;

public interface IWorldMapChunkQueryService
{
    ValueTask<WorldMapChunkQueryResult> QueryAsync(WorldMapChunkRequest request, CancellationToken cancellationToken = default);
}

public interface IWorldMapChunkIdentityProvider
{
    ValueTask<WorldMapChunkWorldState?> GetWorldStateAsync(WorldId worldId, GameServerId gameServerId, CancellationToken cancellationToken = default);
}

public interface IWorldMapChunkOverlayProvider
{
    ValueTask<WorldMapChunkOverlayEnvelope> GetOverlaysAsync(WorldMapChunkOverlayQuery query, CancellationToken cancellationToken = default);
}

public sealed class WorldMapChunkQueryService(
    IWorldMapChunkIdentityProvider identityProvider,
    IWorldMapChunkOverlayProvider overlayProvider) : IWorldMapChunkQueryService
{
    public async ValueTask<WorldMapChunkQueryResult> QueryAsync(WorldMapChunkRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WorldMapChunkWorldState? state = await identityProvider.GetWorldStateAsync(request.WorldId, request.GameServerId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        if (state is null)
        {
            return WorldMapChunkQueryResult.Rejected(WorldMapChunkErrorCode.UnknownWorld, "World/server identity is not registered for local readiness.");
        }

        if (state.WorldId != request.WorldId || state.GameServerId != request.GameServerId)
        {
            return WorldMapChunkQueryResult.Rejected(WorldMapChunkErrorCode.UnknownWorld, "World/server identity mismatch.");
        }

        if (!string.Equals(request.Seed, state.Seed, StringComparison.Ordinal) ||
            !string.Equals(request.ArtisticRevision, state.ArtisticRevision, StringComparison.Ordinal))
        {
            return WorldMapChunkQueryResult.Rejected(WorldMapChunkErrorCode.ManifestRevisionMismatch, "Seed or artistic revision does not match the local readiness world state.");
        }

        WorldMapChunkRequest canonicalRequest = request with
        {
            Seed = state.Seed,
            ArtisticRevision = state.ArtisticRevision
        };

        WorldMapChunkWindowResponse canonical = WorldMapChunkReadinessContract.CreateReadinessWindow(
            canonicalRequest,
            state.WorldMinChunkX,
            state.WorldMaxChunkX,
            state.WorldMinChunkY,
            state.WorldMaxChunkY);

        if (canonical.Errors.Count > 0)
        {
            return WorldMapChunkQueryResult.Rejected(canonical.Errors);
        }

        WorldMapChunkOverlayEnvelope overlays = await overlayProvider.GetOverlaysAsync(
            new WorldMapChunkOverlayQuery(
                request.WorldId,
                request.GameServerId,
                request.CenterChunkX,
                request.CenterChunkY,
                request.Radius,
                state.Seed,
                state.ArtisticRevision,
                canonical.Chunks,
                ContractVersion.Current),
            cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        WorldMapChunkWindowResponse response = WorldMapChunkReadinessContract.FinalizeReadinessOverlays(canonical, overlays);
        if (response.Errors.Count > 0)
        {
            return WorldMapChunkQueryResult.Rejected(response.Errors);
        }

        if (string.Equals(request.IfNoneMatch, response.Cache.ETag, StringComparison.Ordinal))
        {
            return WorldMapChunkQueryResult.NotModified(response.Cache.ETag, response.Cache.ManifestHash, response.Cache.InvalidationKey);
        }

        return WorldMapChunkQueryResult.Success(response);
    }
}

public sealed record WorldMapChunkWorldState(
    WorldId WorldId,
    GameServerId GameServerId,
    int WorldMinChunkX,
    int WorldMaxChunkX,
    int WorldMinChunkY,
    int WorldMaxChunkY,
    string Seed,
    string ArtisticRevision,
    bool ReadOnly,
    bool NonLive,
    ContractVersion ContractVersion);

public sealed record WorldMapChunkOverlayQuery(
    WorldId WorldId,
    GameServerId GameServerId,
    int CenterChunkX,
    int CenterChunkY,
    int Radius,
    string Seed,
    string ArtisticRevision,
    IReadOnlyList<WorldMapChunkDescriptor> Chunks,
    ContractVersion ContractVersion);

public sealed record WorldMapChunkQueryResult(
    WorldMapChunkQueryResultState State,
    WorldMapChunkWindowResponse? Response,
    string? ETag,
    string? ManifestHash,
    string? InvalidationKey,
    IReadOnlyList<WorldMapChunkContractError> Errors)
{
    public static WorldMapChunkQueryResult Success(WorldMapChunkWindowResponse response)
    {
        return new WorldMapChunkQueryResult(
            WorldMapChunkQueryResultState.Success,
            response,
            response.Cache.ETag,
            response.Cache.ManifestHash,
            response.Cache.InvalidationKey,
            []);
    }

    public static WorldMapChunkQueryResult NotModified(string etag, string manifestHash, string invalidationKey)
    {
        return new WorldMapChunkQueryResult(
            WorldMapChunkQueryResultState.NotModified,
            Response: null,
            etag,
            manifestHash,
            invalidationKey,
            []);
    }

    public static WorldMapChunkQueryResult Rejected(WorldMapChunkErrorCode code, string message)
    {
        return Rejected([new WorldMapChunkContractError(code, message)]);
    }

    public static WorldMapChunkQueryResult Rejected(IReadOnlyList<WorldMapChunkContractError> errors)
    {
        return new WorldMapChunkQueryResult(
            WorldMapChunkQueryResultState.Rejected,
            Response: null,
            ETag: null,
            ManifestHash: null,
            InvalidationKey: null,
            errors);
    }
}

public enum WorldMapChunkQueryResultState
{
    Success = 0,
    NotModified = 1,
    Rejected = 2
}

public sealed class DeterministicLocalWorldMapChunkIdentityProvider(WorldMapChunkWorldState state) : IWorldMapChunkIdentityProvider
{
    public ValueTask<WorldMapChunkWorldState?> GetWorldStateAsync(WorldId worldId, GameServerId gameServerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (state.WorldId == worldId && state.GameServerId == gameServerId)
        {
            return ValueTask.FromResult<WorldMapChunkWorldState?>(state);
        }

        return ValueTask.FromResult<WorldMapChunkWorldState?>(null);
    }
}

public sealed class DeterministicLocalWorldMapChunkOverlayProvider : IWorldMapChunkOverlayProvider
{
    public ValueTask<WorldMapChunkOverlayEnvelope> GetOverlaysAsync(WorldMapChunkOverlayQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        WorldMapChunkWindowResponse canonical = WorldMapChunkReadinessContract.CreateReadinessWindow(
            new WorldMapChunkRequest(
                query.WorldId,
                query.GameServerId,
                query.CenterChunkX,
                query.CenterChunkY,
                query.Radius,
                query.Seed,
                query.ArtisticRevision,
                IfNoneMatch: null,
                SinceRevision: null,
                DeltaPageToken: null,
                query.ContractVersion));

        return ValueTask.FromResult(canonical.Overlays);
    }
}
