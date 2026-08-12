# SERVER-B-067 - World Map Local Overlay Snapshot Revision Governance Wave 4 Report

Date: 2026-07-14  
Role: Server-B local preparation  
Boundary: BeeKingdom.Shared WorldMap, SharedContracts tests and world-map ops documentation only  
Report location: local fallback path because the contractual reports directory is outside the writable workspace

## Executive Result

SERVER-B-067 is complete locally.

`LocalWorldMapOverlaySnapshotProvider` adds explicit, in-memory revision governance for immutable overlay snapshots scoped by the exact `(WorldId, GameServerId)` pair. Each scope has an independent state, lock, revision sequence and bounded history. A successful changed publication allocates exactly `latest + 1` in that scope, builds the complete hive/resource/flight snapshot off to the side, and replaces the visible state with one atomic reference write.

The provider implements the existing `IWorldMapChunkOverlayProvider` read boundary and a new transport-neutral `IWorldMapOverlaySnapshotGovernance` API. It is not registered in a server host, dependency injection, endpoint, controller or background service.

Final validation passes:

- targeted SERVER-B-067 provider matrix: `10/10`, no failure, no skip and no compiler warning;
- all `SharedContractsTests`: `89/89`, no failure or skip;
- complete Release suite: `180` passed, `0` failed, `6` existing SQL opt-in skips, `186` total;
- independent offline chunk-wire verifier: exit code `0`, `PASS`;
- no WorldMap test skipped;
- no verifier build output or disposable data residue.

This PASS is local preparation only. It does not establish an official provider, persistence, endpoint, HTTP behavior, realtime synchronization, staging activation or live authority.

## Gate Closed

QA-A validated SERVER-B-066 and identified the remaining future boundary: the canonical overlay hash protected cache correctness, but no component governed revision allocation, concurrent writers, immutable publication or retained history.

SERVER-B-067 closes that local boundary by providing:

- fixed, explicitly constructed world/server scopes;
- strictly monotonic numeric revision per scope;
- deterministic wire revision formatting;
- typed publish/read/history results;
- semantic `NoChange` detection;
- optional compare-and-swap by revision and/or overlay hash;
- atomic complete-snapshot replacement;
- bounded immutable history;
- direct reuse of SERVER-B-065/066 finalization.

No existing query, cache or JSON response contract was weakened or duplicated.

## Public Local Contracts

### Governance boundary

```text
IWorldMapOverlaySnapshotGovernance
  PublishAsync(request, cancellationToken)
  ReadLatestAsync(scope, cancellationToken)
  ReadHistoryAsync(scope, cancellationToken)
```

The implementation is `LocalWorldMapOverlaySnapshotProvider`, which also implements `IWorldMapChunkOverlayProvider` so the unchanged SERVER-064 query service can consume the latest local snapshot.

### Scope

`WorldMapOverlayScope` contains exactly:

```text
WorldId
GameServerId
```

The complete allowed scope set is supplied to the provider constructor. Empty IDs and duplicate scopes are rejected. No scope can be created implicitly during publication, so a crossed `(worldA, serverB)` pair returns `ScopeNotFound` and cannot read or mutate either registered scope.

### Snapshot content

`WorldMapOverlaySnapshotContent` contains complete collections of:

- `WorldHiveOverlay`;
- `WorldResourceOverlay`;
- `WorldFlightOverlay`;
- envelope separation/live/authority flags.

The caller does not assign a committed revision or trusted hash. Caller-owned collections are copied into read-only collections before validation. A published `WorldMapOverlaySnapshot` exposes scope, numeric revision and the final canonical `WorldMapChunkOverlayEnvelope`.

### Stable publication states

| State | Meaning | Snapshot returned |
|---|---|---|
| `Published` | changed valid content atomically committed | committed snapshot |
| `NoChange` | canonical semantic content already equals latest | unchanged latest snapshot |
| `RejectedConflict` | changed content used a stale expected revision/hash | current latest when present |
| `RejectedContract` | content, identifiers or 065/066 finalization invalid | null |
| `ScopeNotFound` | exact scope was not configured | null |

Typed direct reads distinguish `Found`, `SnapshotNotFound` and `ScopeNotFound`.

## Per-Scope Monotonic Revision

There is no provider-wide revision counter. Each private `ScopeState` derives the next revision from its own latest immutable snapshot while holding only that scope's lock.

Numeric progression:

```text
no snapshot -> 1 -> 2 -> 3 -> ...
```

Deterministic wire formatting:

```text
overlay-snapshot-00000000000000000001
overlay-snapshot-00000000000000000002
```

The zero-padded 20-digit format is stable and preserves numeric ordering over all positive `long` revisions. Revision exhaustion is represented as a typed `RevisionExhausted` contract rejection rather than overflow.

The monotonic test publishes independently in two scopes and proves both scopes progress `1, 2` rather than consuming a shared sequence.

## Semantic NoChange

Candidate content is detached and passed through `WorldMapChunkReadinessContract.FinalizeReadinessOverlays` with a fixed private semantic-comparison revision. This reuses the exact SERVER-B-066 ordering and canonical hash procedure while ignoring governance revision number.

Consequences:

- semantically equal lists in opposite input order compare equal;
- `NoChange` does not allocate a revision;
- latest numeric revision, wire revision and overlay hash remain identical;
- querying the same chunk window before and after `NoChange` produces identical response bytes and combined ETag.

`NoChange` is checked before CAS because it performs no mutation. An identical stale retry remains idempotent; stale changed content is still rejected as a conflict.

## Compare-And-Swap

`WorldMapOverlayPublishRequest` accepts independently optional:

- `ExpectedRevision`;
- `ExpectedOverlayHash`.

For changed content, every supplied expectation must match the current latest snapshot. Initial publication may use expected revision `0` to mean no current snapshot.

Concurrent evidence starts two writers from revision `1` with the same expected revision/hash and different complete contents:

```text
Published          = 1
RejectedConflict  = 1
Committed revision = 2
```

The losing result exposes the winning revision `2`. Subsequent requests using only stale revision `1` or only the stale revision-1 hash also return `RejectedConflict`; latest remains revision `2`.

No changed writer silently overwrites a concurrent winner.

## Atomic Publication And Reads

Publication sequence:

1. Check cancellation and resolve exact scope.
2. Detach caller collections.
3. Validate IDs and canonical semantic content outside the scope lock.
4. Enter the scope lock and atomically re-read latest.
5. Resolve `NoChange` or CAS conflict.
6. Allocate the next per-scope numeric revision.
7. Finalize the revisioned envelope through SERVER-B-065/066.
8. Build the complete immutable snapshot and next history without mutating visible state.
9. Check cancellation immediately before commit.
10. publish `latest + history` with one `Volatile.Write`.

Readers use one `Volatile.Read`. They therefore observe either the complete previous state or complete new state; hive, resource and flight lists cannot come from different revisions.

Stress evidence uses one shared provider instance:

| Scope | Baseline | Concurrent changed writers | Final revision |
|---|---:|---:|---:|
| A | 1 | 12 | 13 |
| B | 1 | 5 | 6 |

Four concurrent readers perform `200` reads each while those writers run. Every one of the `800` reads validates a shared content tag across hive, resource and flight values. All 17 changed writers return `Published`; scope A receives revisions `2..13`, scope B receives `2..6`, proving independent counters on the same instance.

## Cancellation And Pre-Commit Exceptions

Cancellation is checked before work, again after entering the scope lock and immediately before the atomic state replacement.

Evidence:

- baseline revision `1` is published;
- a pre-cancelled revision-2 request throws `OperationCanceledException`;
- latest remains revision/hash `1`;
- a synthetic collection-enumeration exception occurs while detaching the next request;
- latest again remains the exact revision/hash `1`.

The provider does not maintain a separately incremented counter. Exceptions during validation, finalization or history construction therefore cannot leave a consumed or partially visible revision.

## Immutable Bounded History

`WorldMapOverlaySnapshotOptions.HistoryCapacity`:

- default: `2`;
- minimum: `2`;
- maximum: `128`;
- order: oldest-to-newest among retained snapshots.

A capacity below `2` is rejected at construction. With capacity `2` and four publications, history is deterministically:

```text
[3, 4]
```

Revisions `1` and `2` are removed before the new immutable state is committed. Retention cannot grow without the configured bound.

The immutability test publishes caller-owned arrays, mutates the original hive array afterward, and proves latest still contains the original value. Exposed snapshot collections report `IsReadOnly=true` and reject element replacement with `NotSupportedException`.

## Dynamic SERVER-B-066 ETag Invalidation

One provider and one unchanged query service publish/query these snapshots against the same background manifest:

| Revision | Semantic change | Expected result |
|---:|---|---|
| 1 | baseline | first combined ETag |
| 2 | flight destination X + 1 | new overlay hash and ETag |
| 3 | resource node respawn ID | new overlay hash and ETag |
| 4 | hive power-band evolution | new overlay hash and ETag |

Evidence:

- all four queries retain one background manifest hash;
- all four overlay hashes are distinct;
- all four combined ETags are distinct;
- revision-3 ETag supplied after revision 4 returns `Success`;
- exact revision-4 ETag returns transport-neutral `NotModified` with a null response.

The query service and combined ETag implementation were not changed in this wave.

## SERVER-B-065/066 Guardrails Preserved

Every candidate and every revisioned commit goes through `FinalizeReadinessOverlays`. The provider does not copy its hash, sorting or payload formulas.

Provider publication rejects all tested violations:

1. envelope `PaintedIntoBackground=true`;
2. envelope `ServerAuthoritative=true`;
3. envelope `Live=true`;
4. hive authoritative;
5. hive live;
6. resource authoritative;
7. resource live;
8. flight authoritative;
9. flight live;
10. flight `AirOnly=false`;
11. flight `RoadGraphUsed=true`;
12. 1000-resource final payload over budget;
13. duplicate hive marker ID;
14. duplicate resource node ID;
15. duplicate flight ID.

The over-budget candidate is validated against the canonical maximum 25-chunk window and maps to governance `PayloadBudgetExceeded`. All live/authority/separation/flight violations map to governance `OverlayContractViolation`. Duplicate IDs have dedicated stable governance codes and do not alter `WorldMapChunkErrorCode`.

After all 15 invalid initial publications, the scope remains `SnapshotNotFound`; no revision was consumed.

## Existing Wire Contract Unchanged

SERVER-B-067 adds a local governance API but does not change `WorldMapChunkWindowResponse`, `WorldMapChunkOverlayEnvelope`, JSON converters, cache fields or error catalog. The examples were intentionally not regenerated.

Final file evidence remains byte-identical to SERVER-B-066:

| Evidence | Full window | Edge window |
|---|---|---|
| Chunks | `25` | `9` |
| Overlay revision | `overlay-readiness-001` | `overlay-readiness-001` |
| Overlay hash | `3b959d7e6403e3a8d0b9e4815224419805b99e241188ed49baa5f01fddf9ae67` | `4b37971dfde47f8ba1130dd0dadb4eca7cd8709cc89b6c924720b546e84d80f3` |
| Combined ETag | `W/"ac2b6a99deb6456e95fed31fd40e0417c87bd88eed336644419abc4fbef92d72"` | `W/"06948970e15cf1d8bdb8246318ded665b7c20b0375770b4ea442a08dfe689aa1"` |
| File SHA-256 | `30EE42C3C87C97583656E31C74B66BFD5CCD7F195A9BE9A4BD11C99F653ABF1D` | `A72D36663FA9AD2FE70BE7B1359F19F3D16C1D0FDA6C67E86D564BA925CA5002` |
| File size | `11699` bytes | `6431` bytes |

## Exact SERVER-B-067 Tests

Final targeted Release run: `10` passed, `0` failed, `0` skipped.

1. `LocalWorldMapOverlaySnapshotProviderPublishesStrictlyMonotonicRevisionsPerScope`
2. `LocalWorldMapOverlaySnapshotProviderReturnsNoChangeForCanonicalSemanticMatch`
3. `LocalWorldMapOverlaySnapshotProviderInvalidatesCombinedEtagForDynamicPublications`
4. `LocalWorldMapOverlaySnapshotProviderRejectsConcurrentStaleCompareAndSwapWriter`
5. `LocalWorldMapOverlaySnapshotProviderKeepsCrossedScopesIsolated`
6. `LocalWorldMapOverlaySnapshotProviderPublishesAtomicConcurrentSnapshotsAcrossTwoScopes`
7. `LocalWorldMapOverlaySnapshotProviderPurgesBoundedHistoryDeterministically`
8. `LocalWorldMapOverlaySnapshotProviderHonorsCancellationAndExceptionBeforeCommit`
9. `LocalWorldMapOverlaySnapshotProviderRejectsInvalidGuardrailsBudgetAndDuplicateIds`
10. `LocalWorldMapOverlaySnapshotProviderDetachesPublishedCollections`

The targeted tests execute again as part of both SharedContracts and the complete Release suite, giving three successful runs on the final implementation state.

## Shared Contracts And Release Suite

Final `SharedContractsTests`:

```text
Total:   89
Passed:  89
Failed:  0
Skipped: 0
```

Final complete Release suite:

```text
Total:   186
Passed:  180
Failed:  0
Skipped: 6
```

The six skips are exactly the existing SQL opt-in scenarios outside this wave:

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

No SQL scenario was represented as executed. No WorldMap, overlay, query-service or governance test was skipped.

## Offline Ops Verifier

The final verifier uses the repository-local `NuGet.Config` with an empty source list and requires no user profile, network feed or authorization.

```text
ExitCode = 0
WORLD_MAP_CHUNK_JSON_CONTRACT_VERIFICATION = PASS
FullWindowChunks = 25
FullWindowOverlayHash = 3b959d7e6403e3a8d0b9e4815224419805b99e241188ed49baa5f01fddf9ae67
FullWindowETag = W/"ac2b6a99deb6456e95fed31fd40e0417c87bd88eed336644419abc4fbef92d72"
EdgeWindowChunks = 9
EdgeWindowOverlayHash = 4b37971dfde47f8ba1130dd0dadb4eca7cd8709cc89b6c924720b546e84d80f3
EdgeWindowETag = W/"06948970e15cf1d8bdb8246318ded665b7c20b0375770b4ea442a08dfe689aa1"
Post-verifier bin/obj residue = 0
```

## Scope, Host And Residue Controls

Final checks:

```text
Provider/governance references under Server/src = declarations in the new Shared WorldMap file only
Host or dependency-injection registration = 0
HTTP endpoint/controller/route references in new product file = 0
SQL client/context references in new product file = 0
Hosted/background-service references in new product file = 0
Unity references in new product file = 0
Remote target references in new product file = 0
Secret candidates in source/test/ops scope = 0
Ops verifier bin/obj residue = 0
.trx/.mdf/.ldf/.bak/.db/.sqlite/.tmp residue under Server/tests and Server/ops = 0
Production Persistence.Provider = InMemory
```

`Server/src/BeeKingdom.Server/appsettings.Production.json` remains SHA-256 `1A4D10DDB163B9F78B7F5E957A054FE46C0B58116535AC4D8B0233EEB5B4D098`, the exact QA-066 baseline hash. It was read for control only and not modified.

No endpoint, HTTP 304 implementation, controller, host registration, SQL activation, appsettings edit, staging operation, remote access, secret, real player data or Unity action occurred.

The workspace contains an empty `.git` directory rather than usable repository metadata, so no authoritative `git diff` is available. Scope was controlled by explicit file inventory, hashes, static reference scans and compilation.

## Files Changed In SERVER-B-067

- `Server/src/BeeKingdom.Shared/WorldMap/LocalWorldMapOverlaySnapshotProvider.cs` (new)
- `Server/tests/BeeKingdom.Tests/SharedContractsTests.cs`
- `Server/ops/world-map-chunk-contract/README.md`
- `Server/ops/world-map-chunk-contract/world-map-chunk-contract-spec.md`
- `Server/ops/world-map-chunk-contract/SERVER-B-067 - World Map Local Overlay Snapshot Revision Governance Wave 4 Report.md` (new)

Final implementation/evidence hashes before adding this report:

| File | SHA-256 | Bytes |
|---|---|---:|
| `LocalWorldMapOverlaySnapshotProvider.cs` | `68AF89BBD00AD539D717964C674DE85BC9ECBF42B78A196AC062D33F07985A01` | `21525` |
| `SharedContractsTests.cs` | `73972471ECDCBA8B2278765D9F061447DC3F8564FE64F94AC46D09B7539B1B9D` | `162011` |
| `README.md` | `0B363C903FD43F5E39929F6890196CB9F2CDF0EF54EFF3589BFC31C021E9C1C0` | `9726` |
| `world-map-chunk-contract-spec.md` | `E5A52CF3BF0B408D789DB5E87E8CBDAB880643315F8D82244E922A05359B9337` | `14551` |

Relevant files confirmed unchanged:

- `WorldMapChunkContracts.cs`: SHA-256 `46D664CD3CC234D4BB9729098288F4C769ACB1B1974F7CAB17EBA8B4DF44134B`;
- `WorldMapChunkQueryService.cs`: SHA-256 `EE2966A6648809C3059E0000F7664B5D618C14941C988B3648B5E368BF2D57E9`;
- `WorldMapChunkJson.cs`;
- both JSON examples;
- ops verifier source/project/script and local NuGet configuration;
- all host, endpoint, persistence, SQL, deployment, appsettings and Unity files.

## Remaining Future Boundaries

This local provider intentionally does not supply:

- process restart durability or distributed coordination;
- SQL schema, transactions, backup or restore;
- a multi-process/distributed CAS implementation;
- an official source of hive, resource or flight state;
- endpoint routing, HTTP cache headers or status 304;
- polling, push, websocket or realtime synchronization;
- alliance, territory or war-data consistency;
- staging/live activation or production registration.

A future provider decision must replace or wrap this local process-memory implementation with persistent/distributed revision governance and prove recovery semantics. SERVER-B-067 must not be registered as an official provider on the strength of this local PASS.

SERVER_B_067_LOCAL_OVERLAY_SNAPSHOT_GOVERNANCE = PASS
MONOTONIC_SCOPE_REVISION = PASS
ATOMIC_CONCURRENT_SNAPSHOT_PUBLICATION = PASS
SERVER_B_065_066_GUARDRAILS_PRESERVED = YES
NO_HOST_OR_LIVE_ACTIVATION = YES
READY_FOR_QA_WORLD_MAP_OVERLAY_SNAPSHOT_GOVERNANCE = YES
