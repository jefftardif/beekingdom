# SERVER-063 World Map Chunk JSON Contract Correction

Status: local-only readiness contract.

This folder documents and mechanically verifies the future chunk delivery contract for the Bee Kingdom world map. It does not define a live endpoint, connect Unity, activate SQL, or publish anything to staging or production.

## Scope

- Windowed chunk delivery around `(worldId, chunkX, chunkY)`.
- Wave 1 viewport window: radius `2`, maximum `5x5` chunks.
- Stable integer world coordinates.
- Artistic seed, artistic revision and manifest hash.
- Cache metadata, ETag and invalidation key.
- Hives, resources and flights as overlays only.
- Air-only flight trajectories; no road graph.
- Deterministic ordering, delta token and snapshot revision.
- Payload budget guardrails and stable error codes.
- Explicit non-claims for official endpoint, persistence, player data, progression, authority, Unity, SQL and staging/production.
- Canonical JSON generated from `WorldMapChunkWindowResponse` with `WorldMapChunkJson`.
- Strict typed deserialization with unknown JSON members rejected.

## Non-Claims

- No live endpoint.
- No official persistence.
- No official player data.
- No official progression.
- No active server authority.
- No Unity integration.
- No SQL-backed world map.
- No staging or production action.

## Files

- `world-map-chunk-contract-spec.md`: contract specification.
- `example-window-5x5.json`: complete canonical DTO response for a full `5x5` window.
- `example-edge-window.json`: complete canonical DTO response for a clipped `3x3` world-edge window.
- `Program.cs`: local generator and exhaustive typed verifier.
- `BeeKingdom.WorldMapChunkContractVerifier.csproj`: package-free local verifier project.
- `NuGet.Config`: clears package feeds; the verifier has no external package dependency.
- `Test-WorldMapChunkContract.ps1`: restores offline, builds and runs the typed verifier.

## Reproducible Commands

From the repository root:

```powershell
dotnet run --project Server/ops/world-map-chunk-contract/BeeKingdom.WorldMapChunkContractVerifier.csproj --configuration Release -- generate Server/ops/world-map-chunk-contract
& .\Server\ops\world-map-chunk-contract\Test-WorldMapChunkContract.ps1
```

`generate` writes both JSON examples from the real response DTO and immediately verifies them. `verify` never writes the examples.

## Canonical Wire Scalars

- `worldId`: lowercase 32-character GUID in `N` format.
- `gameServerId`: lowercase 32-character GUID in `N` format.
- `contractVersion`: canonical non-negative `major.minor.patch` string.
- enums: lowercase camel-case names; integer enum values are rejected.
- null response fields are explicit, including `nextPageToken` and `sinceRevisionApplied`.

## Passive Preparatory Features

`ifNoneMatch`, `deltaPageToken`, version negotiation and future-only error codes remain passive/preparatory. They are represented and documented, but no endpoint evaluates cache validators, consumes delta page tokens, negotiates a version, or performs future database-backed error resolution.

## SERVER-064 Wave 2 Query Service Boundary

SERVER-064 adds a transport-neutral local query service in `BeeKingdom.Shared.WorldMap`:

- `IWorldMapChunkQueryService`
- `IWorldMapChunkIdentityProvider`
- `IWorldMapChunkOverlayProvider`
- `WorldMapChunkQueryResult`

The service returns typed states only:

- `Success`: canonical `WorldMapChunkWindowResponse`;
- `NotModified`: matching ETag, no response body;
- `Rejected`: typed `WorldMapChunkContractError` values.

This is not an HTTP endpoint and is not registered in ASP.NET. A future adapter may translate these states into HTTP, polling, CLI or another transport only after a separate gate.

Local deterministic providers are test helpers for readiness only. They do not claim authority, persistence, live player data, Unity integration, SQL, staging or production availability.

## SERVER-B-065 Overlay Finalization Guardrails

Provider overlays are finalized only through `WorldMapChunkReadinessContract.FinalizeReadinessOverlays`. This canonical operation:

- replaces the overlay envelope;
- recalculates `EstimatedPayloadBytes` from the final hive, resource and flight counts;
- rejects a final payload above `PayloadBudgetBytes` with `PayloadBudgetExceeded`;
- rejects painted overlays, non-air flights and road-graph flights with `OverlayContractViolation`;
- rejects `Live` or `ServerAuthoritative` on the envelope, every hive, every resource and every flight with `OverlayContractViolation`;
- replaces its own derived errors when invoked repeatedly, so finalization remains deterministic.

The query service returns `Rejected` with no response/cache metadata when finalization produces an error. Successful and `NotModified` results retain the canonical background hash and ETag. Dynamic overlay revisioning remains a separate future cache-design gate.

Concurrency evidence now uses one `WorldMapChunkQueryService`, one shared identity provider resolving two world/server scopes and one shared overlay provider. The providers remain local test evidence only.

## SERVER-B-066 Overlay Revision and Combined Cache Validator

Every successful final overlay envelope now exposes:

- `overlayRevision`: deterministic, provider-supplied and non-empty;
- `overlayHash`: lowercase SHA-256 recalculated by the canonical finalizer.

The provider hash is never trusted. `FinalizeReadinessOverlays` sorts hives, resources and flights by their stable identifiers and deterministic tie-break fields, replaces `overlayHash` with an empty string, serializes that real envelope with `WorldMapChunkJson`, and hashes the resulting UTF-8 wire bytes. The calculated hash is then written into the final envelope.

The local ETag input is:

```text
worldId | gameServerId | centerX | centerY | radius | backgroundManifestHash | overlayRevision | overlayHash
```

The pagination delta token follows this combined ETag. The background manifest remains a background-only identity, while the overlay revision/hash identify the final dynamic envelope.

Rules:

- identical background and overlays produce identical bytes, overlay hash and ETag;
- a flight movement, resource respawn or hive evolution changes provider revision, overlay hash and ETag;
- an accidental revision collision is still detected because different wire content produces a different overlay hash and ETag;
- `NotModified` is returned only for an exact ordinal match of the combined ETag;
- a blank provider revision is an `OverlayContractViolation`;
- all SERVER-B-065 budget, non-live, authority, separation and flight guardrails still run before `Success` or `NotModified`.

This is a local preparation contract. It is not an HTTP 304 flow, realtime stream, live world provider or official server cache.

## SERVER-B-067 Local Overlay Snapshot Governance

`LocalWorldMapOverlaySnapshotProvider` is an explicitly constructed, in-memory preparation provider. It implements both the existing `IWorldMapChunkOverlayProvider` read boundary and the new transport-neutral `IWorldMapOverlaySnapshotGovernance` boundary. It is not registered in the server host or dependency injection.

Scopes are fixed at construction as exact `(WorldId, GameServerId)` pairs. Each scope owns an independent state, lock and revision sequence; there is no global revision counter. A successful publication starts at numeric revision `1` and uses this deterministic wire revision:

```text
overlay-snapshot-<20 digit zero-padded per-scope revision>
```

Publication states are stable:

- `Published`: a complete immutable snapshot was atomically committed;
- `NoChange`: canonical semantic content already matches latest, so revision/hash remain unchanged;
- `RejectedConflict`: optional expected revision or hash does not match latest;
- `RejectedContract`: identifiers, 065/066 flags, flight rules or payload budget are invalid;
- `ScopeNotFound`: the exact world/server pair was not registered locally.

The caller publishes complete hive, resource and flight collections without supplying a revision or trusted hash. The provider detaches caller-owned collections, rejects blank or duplicate identifiers per entity category, and invokes `FinalizeReadinessOverlays` with a fixed semantic comparison revision. Equal semantic content in a different list order therefore returns `NoChange`.

For changed content, compare-and-swap is checked against the latest numeric revision and/or canonical overlay hash. A changed stale writer receives `RejectedConflict`; an identical stale retry may return `NoChange` because it performs no commit. The provider then allocates the next per-scope revision, invokes the same 065/066 finalizer again, constructs the full immutable snapshot and replaces visible scope state in one atomic write.

Readers observe either the complete previous snapshot or the complete latest snapshot. Cancellation is checked before any commit. No revision counter is advanced separately, so cancellation or an exception before the atomic state replacement leaves no partial revision.

History capacity is configurable from `2` through `128`, defaults to `2`, and retains snapshots oldest-to-newest after deterministic removal of the oldest entry. This guarantees at least latest plus previous without unbounded retention.

The existing chunk response wire did not change in SERVER-B-067. The SERVER-B-066 examples and verifier inputs are intentionally not regenerated; their file bytes, overlay hashes and combined ETags remain the previous validated values.

This provider is local process memory only. It provides no HTTP endpoint, realtime stream, official authority, persistence, SQL, staging, production activation, Unity integration or player data.
