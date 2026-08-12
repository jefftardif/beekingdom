# World Map Chunk Contract Specification

## Purpose

Prepare local server-side contracts for future delivery of a large continuous MMO world map without claiming that a live Bee Kingdom world map server exists.

## Request Shape

- `worldId`: logical world identifier.
- `gameServerId`: future owner/server scope.
- `centerChunkX`, `centerChunkY`: requested center chunk.
- `radius`: Wave 1 maximum `2`, yielding up to `5x5` chunks.
- `seed`: deterministic art seed.
- `artisticRevision`: immutable art revision label.
- `ifNoneMatch`: future cache validator.
- `sinceRevision`: future delta anchor.
- `deltaPageToken`: future pagination anchor.
- `contractVersion`: canonical `major.minor.patch` string; negotiation is passive in this wave.

`worldId` and `gameServerId` are lowercase 32-character GUID strings without separators. Unknown JSON members, integer enum values and non-canonical identifiers or versions are rejected.

## Response Shape

- `chunks`: background chunk descriptors only.
- `cache`: ETag, manifest hash, revision and invalidation metadata.
- `overlays`: hives, resources and flights separated from background.
- `pagination`: deterministic ordering, page size, delta token and snapshot revision.
- `guardrails`: response budget, limits and error codes.
- `nonClaims`: flags proving this remains local/readiness/non-live.
- `errors`: complete typed error list; empty for the two successful examples.
- `preparatoryFeatures`: explicit passive-state flags for cache validation, delta input, version negotiation and future error handling.
- `contractVersion`: current response contract version.

The checked-in full-window and edge-clipped files contain every chunk descriptor and every overlay record. They are generated from `WorldMapChunkWindowResponse`, not maintained as summaries.

## Visual Continuity Requirements

The client must perceive a continuous world. The background chunks cannot encode hives, resources, flights, roads, debug grids or any chunk-boundary language. Artistic continuity is a UI/visual gate, while this server contract preserves the data separation required for that gate.

## Flight Rules

Flights are airborne bee movements:

- distance is future server-calculated from coordinates;
- origin/destination are stable world coordinates;
- `airOnly` must be true;
- `roadGraphUsed` must be false;
- attack, reinforcement, transport, gather and return remain future/readiness-only.

## Cache and Versioning

- ETag is deterministic for `(worldId, gameServerId, center, radius, manifestHash)`.
- Manifest hash is deterministic for `(seed, artisticRevision, chunk list)`.
- `artisticRevision` controls cache invalidation.
- `snapshotRevision` is read-only and non-live in Wave 1.
- `nextPageToken` and `sinceRevisionApplied` are present as explicit nulls in the examples.
- Changing the seed changes the manifest hash and ETag deterministically.
- Changing the artistic revision changes the manifest hash, ETag and invalidation key deterministically.

## Passive Inputs and Future Errors

- `ifNoneMatch` is carried by the request shape but is not evaluated; no `304` behavior exists.
- `deltaPageToken` is carried by the request shape but is not consumed or validated.
- request `contractVersion` is represented but no negotiation or compatibility selection runs.
- only radius and bounds validation are active locally; the remaining stable error names are a future catalog and do not imply world lookup, authentication or persistence.

## Guardrails

- Wave 1 radius must be `0..2`.
- Maximum window size is `25` chunks.
- Default payload budget is `98304` bytes.
- Unknown worlds/chunks remain future error codes; no database lookup is active.

## Non-Live Boundary

This contract does not create:

- public endpoint;
- SQL migration;
- SQL reads/writes;
- official player map state;
- live overlays;
- official authority;
- Unity integration;
- staging or production publish.

## SERVER-064 Query Service Boundary

`IWorldMapChunkQueryService` is the local application-service boundary for future adapters. It accepts a `WorldMapChunkRequest` and returns a typed `WorldMapChunkQueryResult`.

The service is transport-neutral:

- no HTTP status codes;
- no ASP.NET dependency;
- no endpoint path;
- no controller/minimal API registration;
- no SQL dependency;
- no remote calls.

Result states:

- `Success`: includes the canonical `WorldMapChunkWindowResponse`.
- `NotModified`: returned when `IfNoneMatch` exactly matches the current ETag; `Response` is null.
- `Rejected`: includes typed `WorldMapChunkContractError` entries.

Providers:

- `IWorldMapChunkIdentityProvider` supplies read-only world/server identity, bounds, seed and artistic revision.
- `IWorldMapChunkOverlayProvider` supplies read-only overlays.

Deterministic local providers exist for tests only. They must not be used to claim server authority or live gameplay state.

Future adapter handoff:

1. Resolve authenticated player/session only in an adapter, not in this service.
2. Build `WorldMapChunkRequest`.
3. Call `IWorldMapChunkQueryService`.
4. Translate `Success`, `NotModified` and `Rejected` to the chosen transport.
5. Preserve all non-claims until a separate live/server-authority gate.

## SERVER-B-065 Final Overlay Composition

The provider response is not assigned directly to the canonical response. It is passed to `WorldMapChunkReadinessContract.FinalizeReadinessOverlays`, which is the single composition boundary for this wave.

Finalization recalculates the estimated payload with the same private formula used by initial contract construction:

```text
2048 + (chunkCount * 512) + (hiveCount * 256) + (resourceCount * 256) + (flightCount * 384)
```

The formula is not duplicated in `WorldMapChunkQueryService`.

Final validation rejects:

- payload estimate greater than `98304` bytes as `PayloadBudgetExceeded`;
- `PaintedIntoBackground=true` as `OverlayContractViolation`;
- `Live=true` or `ServerAuthoritative=true` on the envelope as `OverlayContractViolation`;
- `Live=true` or `ServerAuthoritative=true` on any hive, resource or flight as `OverlayContractViolation`;
- `AirOnly=false` or `RoadGraphUsed=true` on any flight as `OverlayContractViolation`.

`OverlayContractViolation` is appended as stable enum value `8`; all earlier error values retain their existing numeric values. The canonical examples therefore expose nine guardrail error-code names.

On any finalization error, the transport-neutral result is `Rejected`, its response and cache metadata are null, and its typed errors identify overlay contract violations separately from payload overflow.

## SERVER-B-066 Overlay Revision and Hash

The final wire overlay envelope adds two scalar fields:

- `overlayRevision`: a non-empty deterministic revision supplied by the overlay provider;
- `overlayHash`: a canonical lowercase SHA-256 calculated by the finalizer.

Canonical hash procedure:

1. Sort hives by marker ID and deterministic tie-break fields.
2. Sort resources by node ID and deterministic tie-break fields.
3. Sort flights by flight ID and deterministic tie-break fields.
4. Preserve the provider `overlayRevision`.
5. Set `overlayHash` to the empty string.
6. Serialize the actual `WorldMapChunkOverlayEnvelope` using `WorldMapChunkJson.CreateOptions()`.
7. SHA-256 hash the UTF-8 wire bytes and write the lowercase result to `overlayHash`.

Any provider-supplied hash is replaced. An empty/whitespace revision is rejected as `OverlayContractViolation`.

### Combined local validator

The ETag is SHA-256 over:

```text
worldId|gameServerId|centerChunkX|centerChunkY|radius|manifestHash|overlayRevision|overlayHash
```

`manifestHash` continues to represent background chunks. `overlayRevision` and `overlayHash` represent the final provider envelope. The delta token is regenerated from the combined ETag.

An exact combined ETag match returns the transport-neutral `NotModified` state. A stale ETag caused by a flight move, resource respawn, hive evolution, revision bump or hash-only collision returns `Success` with the new response. No HTTP status, endpoint or live cache behavior exists.

### Collision and ordering properties

- Equal semantic overlay sets in different provider list orders canonicalize to identical wire bytes, hash and ETag.
- Equal revision strings with different wire content produce different hashes and ETags.
- World/server scope remains part of the ETag even if two worlds have identical overlay wire content.
- All SERVER-B-065 payload and non-live guardrails remain mandatory after hash composition.

## SERVER-B-067 Local Snapshot Revision Governance

### Scope and construction

`LocalWorldMapOverlaySnapshotProvider` is a local in-memory implementation of:

- `IWorldMapOverlaySnapshotGovernance` for typed publish/latest/history operations;
- `IWorldMapChunkOverlayProvider` for reuse by the transport-neutral SERVER-064 query service.

The constructor receives the complete allowed set of `WorldMapOverlayScope` values. A scope is the exact value pair `(WorldId, GameServerId)`. Empty identifiers and duplicate scopes are rejected at construction. The provider is not discovered, registered or activated by any server host.

Each scope has its own lock and immutable visible state. The provider has no shared revision counter. Numeric revision starts at `1` independently in every scope, then increases by exactly one for each `Published` result.

The wire revision is deterministic:

```text
overlay-snapshot-00000000000000000001
overlay-snapshot-00000000000000000002
...
```

### Publication request and result

`WorldMapOverlayPublishRequest` contains:

- exact scope;
- complete `WorldMapOverlaySnapshotContent` with hives, resources, flights and envelope flags;
- optional `ExpectedRevision`;
- optional `ExpectedOverlayHash`.

The caller cannot assign the committed revision. Any hash present on a source envelope is discarded when it is converted to snapshot content.

`WorldMapOverlayPublicationState` values are stable:

| State | Meaning | Snapshot field |
|---|---|---|
| `Published` | changed valid content committed atomically | committed snapshot |
| `NoChange` | canonical semantic content equals latest | unchanged latest snapshot |
| `RejectedConflict` | changed content has stale expected revision/hash | current latest snapshot when present |
| `RejectedContract` | snapshot content or final guardrails invalid | null |
| `ScopeNotFound` | exact scope was not configured | null |

Semantic equality is calculated with a fixed internal comparison revision and the canonical SERVER-B-066 overlay hash procedure. It ignores provider list order and governance revision number while still covering every overlay field. `NoChange` is evaluated before CAS because no mutation occurs. A semantically identical retry is idempotent even if its expectation is stale; changed stale content is always `RejectedConflict`.

### Atomic commit sequence

1. Check cancellation and resolve the exact scope.
2. Detach all caller-owned lists into read-only collections.
3. Reject null collections, blank identifiers and duplicate IDs within hives, resources or flights.
4. Canonicalize and validate semantic content through `FinalizeReadinessOverlays`.
5. Enter the lock owned only by this scope and re-read latest state.
6. Return `NoChange` for equal semantic content.
7. Compare optional expected numeric revision and/or final overlay hash.
8. Allocate `latest + 1` in this scope only.
9. Format the deterministic wire revision and finalize again through the 065/066 boundary.
10. Build a complete immutable `WorldMapOverlaySnapshot` and next bounded history off to the side.
11. Check cancellation immediately before commit.
12. Replace `latest + history` with one atomic reference write.

There is no separately mutated counter. Any cancellation or exception before step 12 leaves latest revision, hash and history unchanged. Lock-free readers use one atomic state read, so they cannot combine hive, resource or flight lists from different revisions.

### Compare-and-swap

Expected revision and expected overlay hash are independently optional. When supplied for changed content, every supplied value must match latest. Two writers using the same base and different content therefore produce exactly one `Published` result and one `RejectedConflict`; no stale write silently overwrites the winner.

For initial publication, expected revision `0` represents no current snapshot. An expected hash can only match an existing snapshot.

### History and immutability

`WorldMapOverlaySnapshotOptions.HistoryCapacity` is restricted to `2..128` and defaults to `2`. History order is oldest-to-newest among retained entries. On overflow, exactly the oldest entries are removed before the new immutable state is committed.

Snapshots contain detached read-only hive, resource and flight collections. Mutating a caller-owned array after `Published` cannot mutate latest or history.

### Contract enforcement

Publication reuses the existing maximum 25-chunk readiness validation response, so the final payload limit remains `98304` bytes. `RejectedContract` maps finalizer errors without changing the chunk wire enum:

- final payload overflow maps to governance `PayloadBudgetExceeded`;
- painted, live, authoritative, non-air or road-graph overlays map to governance `OverlayContractViolation`;
- duplicates use dedicated hive/resource/flight governance error codes;
- blank IDs use `InvalidIdentifier`.

The existing `WorldMapChunkErrorCode` values and JSON guardrail catalog are unchanged.

### Query integration and non-claims

After a local snapshot is published, the existing query service obtains its envelope through `IWorldMapChunkOverlayProvider`. The SERVER-B-066 finalizer independently recomputes the same hash and combined ETag. `NoChange` preserves revision/hash/ETag; flight movement, resource respawn or hive evolution publishes a new revision and invalidates the ETag.

Typed governance reads distinguish `Found`, `SnapshotNotFound` and `ScopeNotFound`. The legacy overlay-provider interface has no typed absence result and returns the existing empty non-live envelope for an absent local snapshot; world/server validity remains the identity provider's responsibility.

SERVER-B-067 does not define an endpoint, HTTP behavior, realtime synchronization, persistence, SQL, official provider, server authority, staging/live activation, Unity integration or real player data. The two SERVER-B-066 JSON examples remain byte-identical because no existing wire DTO changed.
