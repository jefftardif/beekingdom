# SERVER-B-066 - World Map Overlay Revision Cache Validator Wave 3 Report

Date: 2026-07-14  
Role: Server-B local preparation  
Boundary: local only, transport-neutral and non-live; no endpoint, HTTP binding, SQL, staging, remote access or Unity  
Report location: local fallback path because the contractual reports directory is outside the writable workspace

## Executive Result

SERVER-B-066 is complete locally.

The final overlay envelope now carries a required, deterministic `OverlayRevision` and a canonical `OverlayHash`. `WorldMapChunkReadinessContract.FinalizeReadinessOverlays` remains the single composition boundary: it canonicalizes the provider overlay order, replaces any provider-supplied hash with a SHA-256 of the real wire envelope, recalculates the final payload estimate, applies every SERVER-B-065 guardrail, and derives a combined cache validator from the background manifest plus overlay identity.

`WorldMapChunkQueryService` already finalized overlays before evaluating `IfNoneMatch`; no query-service source change was required in this wave. An exact ordinal match of the new combined ETag returns the transport-neutral `NotModified` result. A stale validator caused by a flight move, resource respawn, hive evolution, revision change or same-revision content collision returns `Success` with the new response.

Final validation passes:

- targeted query-service matrix: `20/20`, no skip;
- all `SharedContractsTests`: `79/79`, no skip;
- complete Release suite: `170` passed, `0` failed, `6` existing SQL opt-in skips, `176` total;
- independent offline ops verifier: exit code `0`, contract verification `PASS`;
- no WorldMap test skipped and no ops verifier or disposable test-data residue.

This is a local preparation contract. It is not an official real-time overlay flow, HTTP cache behavior, staging activation or live server claim.

## Final Overlay Wire Contract

`WorldMapChunkOverlayEnvelope` adds two required wire fields:

- `overlayRevision`: non-empty deterministic revision supplied by the overlay provider;
- `overlayHash`: lowercase 64-character SHA-256 calculated by canonical finalization.

Both fields are marked `JsonRequired`, so a wire payload that omits either field fails deterministic deserialization instead of silently acquiring a default value. A provider revision containing only whitespace reaches finalization and is rejected with the stable `OverlayContractViolation` error. Provider hash input is never trusted.

The non-success `WorldMapChunkOverlayEnvelope.Empty` value also has an explicit preparatory revision and a canonical 64-character hash, preserving a complete typed JSON shape for rejected response round trips.

## Canonical Hash Procedure

`WorldMapChunkReadinessContract.FinalizeReadinessOverlays` performs these overlay identity steps:

1. Sort hives by `HiveMarkerId`, followed by deterministic wire-field tie breakers.
2. Sort resources by `ResourceNodeId`, followed by deterministic wire-field tie breakers.
3. Sort flights by `FlightId`, followed by deterministic wire-field tie breakers.
4. Preserve the provider `OverlayRevision`.
5. Replace `OverlayHash` with the empty string.
6. Serialize the actual `WorldMapChunkOverlayEnvelope` through `WorldMapChunkJson.CreateOptions()`.
7. SHA-256 hash those UTF-8 wire bytes.
8. Write the lowercase hash back to the final envelope.

Consequences proven by tests:

- equal background and equal semantic overlays produce identical response bytes, overlay hash and ETag;
- provider list order does not change final bytes, hash or ETag;
- any stale or fabricated provider hash is replaced;
- equal revision strings with different overlay wire content still produce different hashes and ETags;
- a mutation of one hive field invalidates the combined validator even when the provider revision collides.

The hash deliberately covers the final non-live envelope, including envelope flags, hive/resource/flight values and `OverlayRevision`. It does not hash itself because `OverlayHash` is set to `""` during canonical serialization.

## Combined Cache Validator

The background `ManifestHash` remains the identity of the 25/9 chunk background window. The response ETag is now SHA-256 over this exact input:

```text
worldId|gameServerId|centerChunkX|centerChunkY|radius|manifestHash|overlayRevision|overlayHash
```

The resulting lowercase digest keeps the existing local weak-ETag representation:

```text
W/"<64 lowercase hexadecimal characters>"
```

The pagination `DeltaToken` is regenerated from that combined ETag. `WorldId` and `GameServerId` remain direct inputs, so two scopes cannot share a validator merely because their background and overlay bytes happen to match.

`IfNoneMatch` behavior remains transport-neutral:

- exact ordinal combined ETag match: `NotModified`, null response body, exact ETag/manifest/invalidation metadata, no errors;
- old background-only or stale combined ETag: `Success` with the current response;
- final overlay contract or budget violation: `Rejected` before any validator match is considered.

There is no HTTP adapter, HTTP 304 result or live cache policy in this package.

## Dynamic Invalidation Evidence

The same background manifest was queried with four deterministic overlay snapshots:

| Snapshot | Single semantic change | Revision | Result against prior ETag |
|---|---|---|---|
| Baseline | none | `overlay-readiness-001` | `Success` |
| Flight movement | destination X incremented by one | `overlay-readiness-002` | new hash and ETag |
| Resource respawn | resource node ID replaced | `overlay-readiness-003` | new hash and ETag |
| Hive evolution | power band replaced | `overlay-readiness-004` | new hash and ETag |

All four snapshots retain the same background manifest. The three changed snapshots each produce a distinct `OverlayHash` and distinct combined ETag.

A separate mutable-provider scenario moves a flight while keeping the same service instance:

1. query baseline and capture ETag A;
2. advance the overlay revision and flight destination;
3. query with ETag A and receive `Success` plus ETag B;
4. query the unchanged new snapshot with ETag B and receive `NotModified`.

The revision-collision scenario keeps the exact same revision, mutates only `Hive.PowerBand`, and presents the same stale provider hash. Canonical finalization replaces that hash, produces a different final hash and ETag, and returns `Success` rather than obsolete `NotModified`.

## SERVER-B-065 Guardrails Preserved

Overlay hash/cache composition does not bypass or duplicate SERVER-B-065 rules. The same canonical finalizer still recalculates `EstimatedPayloadBytes` after real overlays are composed and rejects the final response when it exceeds `PayloadBudgetBytes`.

Preserved evidence:

| Provider payload | Final estimate | Budget | Result |
|---|---:|---:|---|
| 2 hives, 3 resources, 2 flights, 25 chunks | `16896` | `98304` | `Success` |
| 1 hive, 1000 resources, 1 flight, 25 chunks | `271488` | `98304` | `Rejected / PayloadBudgetExceeded` |

The dedicated `OverlayContractViolation = 8` remains distinct from `PayloadBudgetExceeded` and rejects:

- blank/whitespace `OverlayRevision`;
- envelope `PaintedIntoBackground=true`;
- envelope `Live=true` or `ServerAuthoritative=true`;
- hive `Live=true` or `ServerAuthoritative=true`;
- resource `Live=true` or `ServerAuthoritative=true`;
- flight `Live=true` or `ServerAuthoritative=true`;
- flight `AirOnly=false`;
- flight `RoadGraphUsed=true`.

Successful payloads remain separate from background, non-live, non-authoritative, air-only and free of road-graph claims.

## Spatial and Scope Invariants

SERVER-B-066 does not change chunk geometry or background manifest calculation:

- full centered radius-2 window: `25` chunks;
- clipped corner radius-2 window: `9` chunks;
- stable ordering: Y then X;
- chunk origin and dimensions unchanged;
- seed/artistic revision still invalidate the background manifest;
- overlays remain separate from painted background chunks.

The shared-instance concurrency test still uses one `WorldMapChunkQueryService`, one identity provider containing at least two world/server scopes, and one shared overlay provider. Concurrent reads through that same object graph retain their expected `WorldId`, `GameServerId`, manifest, validator and invalidation scope. A crossed world/server pair is rejected and no state leaks between worlds.

Cancellation is checked before provider access, after identity resolution and after overlay resolution. Deterministic repeated reads preserve bytes, overlay identity and ETag.

## Regenerated Real JSON Evidence

Both examples were regenerated from the real DTO and Bee Kingdom JSON configuration.

| Evidence | Full window | Edge-clipped window |
|---|---|---|
| Chunk count | `25` | `9` |
| Background manifest | `a6e46a84bc24cb94111c09a1a3ea44aced10323575f6a2dad47497b477b55fa1` | `d9038abfee2eb1150d4e04986fa3a8e8cf879811398c04d102ba80c5f85c754a` |
| Overlay revision | `overlay-readiness-001` | `overlay-readiness-001` |
| Overlay hash | `3b959d7e6403e3a8d0b9e4815224419805b99e241188ed49baa5f01fddf9ae67` | `4b37971dfde47f8ba1130dd0dadb4eca7cd8709cc89b6c924720b546e84d80f3` |
| Combined ETag | `W/"ac2b6a99deb6456e95fed31fd40e0417c87bd88eed336644419abc4fbef92d72"` | `W/"06948970e15cf1d8bdb8246318ded665b7c20b0375770b4ea442a08dfe689aa1"` |
| JSON SHA-256 | `30EE42C3C87C97583656E31C74B66BFD5CCD7F195A9BE9A4BD11C99F653ABF1D` | `A72D36663FA9AD2FE70BE7B1359F19F3D16C1D0FDA6C67E86D564BA925CA5002` |
| File size | `11699` bytes | `6431` bytes |

The examples include all chunk, overlay, cache, pagination, guardrail, error, preparatory-feature and non-claim fields. The delta token follows the new combined ETag.

## Exact Targeted Tests

Final Release targeted query-service run: `20` passed, `0` failed, `0` skipped.

1. `WorldMapChunkQueryServiceCanonicalizesOverlaySerializationOrder`
2. `WorldMapChunkQueryServiceChangesManifestHashEtagAndInvalidationWhenSeedOrRevisionChanges`
3. `WorldMapChunkQueryServiceHonorsCancellation`
4. `WorldMapChunkQueryServiceInvalidatesCombinedEtagForDynamicOverlayChanges`
5. `WorldMapChunkQueryServiceKeepsCacheBytesHashAndEtagDeterministic`
6. `WorldMapChunkQueryServicePreservesEdgeClippingAndPayloadGuardrails`
7. `WorldMapChunkQueryServiceRecalculatesSuccessfulProviderOverlayPayload`
8. `WorldMapChunkQueryServiceRejectsEmptyOverlayRevision`
9. `WorldMapChunkQueryServiceRejectsLiveOrAuthoritativeOverlayEntities`
10. `WorldMapChunkQueryServiceRejectsLiveOrAuthoritativeOverlayEnvelope`
11. `WorldMapChunkQueryServiceRejectsNonAirborneFlightProviderPayload`
12. `WorldMapChunkQueryServiceRejectsOversizedFinalOverlayPayload`
13. `WorldMapChunkQueryServiceRejectsPaintedOverlayProviderPayload`
14. `WorldMapChunkQueryServiceRejectsRoadGraphFlightProviderPayload`
15. `WorldMapChunkQueryServiceRejectsWorldServerMismatchAndBadRevision`
16. `WorldMapChunkQueryServiceReturnsNotModifiedWithoutBodyWhenIfNoneMatchHits`
17. `WorldMapChunkQueryServiceReturnsSuccessForStaleCombinedEtagThenNotModifiedForExactEtag`
18. `WorldMapChunkQueryServiceReturnsSuccessWithCanonicalResponse`
19. `WorldMapChunkQueryServiceSupportsConcurrentReadsWithoutCrossWorldLeakage`
20. `WorldMapChunkQueryServiceUsesOverlayHashToSurviveRevisionCollision`

No WorldMap test is ignored.

## Shared Contracts and Complete Release Suite

Final `SharedContractsTests` run with Release and `--no-restore`:

```text
Total:   79
Passed:  79
Failed:  0
Skipped: 0
```

Final complete Release run with `--no-restore`:

```text
Total:   176
Passed:  170
Failed:  0
Skipped: 6
```

The six skips are exactly the existing SQL opt-in scenarios, outside this wave:

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

No SQL scenario was represented as executed by SERVER-B-066.

## Independent Offline Ops Verifier

The final verifier run used `Server/ops/world-map-chunk-contract/NuGet.Config`, whose package-source list is empty. Restore resolved only local/project assets; no user NuGet profile, feed network or authorization was required. The verifier deserialized both wire files and independently recalculated canonical overlay hashes and combined ETags.

```text
ExitCode = 0
WORLD_MAP_CHUNK_JSON_CONTRACT_VERIFICATION = PASS
FullWindowChunks = 25
FullWindowOverlayRevision = overlay-readiness-001
FullWindowOverlayHash = 3b959d7e6403e3a8d0b9e4815224419805b99e241188ed49baa5f01fddf9ae67
FullWindowETag = W/"ac2b6a99deb6456e95fed31fd40e0417c87bd88eed336644419abc4fbef92d72"
EdgeWindowChunks = 9
EdgeWindowOverlayRevision = overlay-readiness-001
EdgeWindowOverlayHash = 4b37971dfde47f8ba1130dd0dadb4eca7cd8709cc89b6c924720b546e84d80f3
EdgeWindowETag = W/"06948970e15cf1d8bdb8246318ded665b7c20b0375770b4ea442a08dfe689aa1"
Post-verifier bin/obj residue = 0
```

## Scope, Secrets and Residues

Final static and filesystem checks:

```text
HTTP/endpoint/controller binding references in product/ops scope = 0
SQL client/context references in product/ops scope = 0
Unity references in product/ops scope = 0
Remote target references in product/ops scope = 0
Secret candidates in source/test/ops scope = 0
Ops verifier bin/obj residue = 0
Disposable .mdf/.ldf/.bak/.tmp residue under Server/tests and Server/ops = 0
Production Persistence.Provider = InMemory
```

The only broad scan match was the existing negative assembly assertion that verifies `BeeKingdom.Shared` does not reference `UnityEngine`; it is test evidence, not a dependency.

No endpoint, route, controller, HTTP binding, HTTP 304 implementation, SQL activation, staging action, remote write/read, secret, real player data or Unity change was made. No production appsettings file was edited.

The workspace contains an empty `.git` directory rather than usable repository metadata, so an authoritative `git diff` cannot be produced. Scope was controlled through the explicit file inventory below, file hashes, static scans and test compilation. No file outside the authorized Shared WorldMap, Shared contract tests and world-map ops folder was edited in this wave.

## Files Changed in SERVER-B-066

- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkContracts.cs`
- `Server/tests/BeeKingdom.Tests/SharedContractsTests.cs`
- `Server/ops/world-map-chunk-contract/Program.cs`
- `Server/ops/world-map-chunk-contract/example-window-5x5.json`
- `Server/ops/world-map-chunk-contract/example-edge-window.json`
- `Server/ops/world-map-chunk-contract/README.md`
- `Server/ops/world-map-chunk-contract/world-map-chunk-contract-spec.md`
- `Server/ops/world-map-chunk-contract/SERVER-B-066 - World Map Overlay Revision Cache Validator Wave 3 Report.md`

Final implementation/evidence hashes before adding this report:

| File | SHA-256 |
|---|---|
| `WorldMapChunkContracts.cs` | `46D664CD3CC234D4BB9729098288F4C769ACB1B1974F7CAB17EBA8B4DF44134B` |
| `SharedContractsTests.cs` | `48F5DC33F91CA3FFE72CC19A32D84C7AC3B4AA1636C6F32464B22EA830D3AA97` |
| `Program.cs` | `6D6B6DF5D1F42A9B0AC4D2DC951E54CDE9A7223052922F594FF54502B119A735` |
| `README.md` | `40D9F4C6960EAF599F8C61F0B924214E83B6741CB14DAD40566E88D383935927` |
| `world-map-chunk-contract-spec.md` | `9A08FE1A2B8933D7E7936A664E46A68EE700A6DFA20FDB6B6EB017C2712A5F06` |

The following relevant files were not changed in SERVER-B-066:

- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkQueryService.cs` (already finalizes before validator evaluation);
- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkJson.cs`;
- verifier `.csproj`, local `NuGet.Config` and verifier PowerShell script;
- historical SERVER-B-063, SERVER-064 and SERVER-B-065 reports;
- every endpoint/server host, persistence, SQL, deployment, production configuration and Unity file.

## Remaining Future Boundaries

This package intentionally does not define:

- provider storage, transactions or official revision allocation;
- polling cadence, push synchronization or real-time delivery;
- HTTP validators, headers, status codes or endpoint routing;
- live authoritative hive, resource or flight state;
- SQL schema, staging rollout or production migration;
- alliance, territory or war-data consistency behavior.

A future server wave must assign monotonic or otherwise deterministic overlay revisions per `(WorldId, GameServerId)` and snapshot, then prove persistence/concurrency semantics before any live claim. The canonical wire hash protects cache correctness against accidental revision reuse, but it is not a replacement for authoritative revision governance.

SERVER_B_066_OVERLAY_REVISION_CACHE_VALIDATOR = PASS
DYNAMIC_OVERLAY_ETAG_INVALIDATION = PASS
SERVER_B_065_GUARDRAILS_PRESERVED = YES
READY_FOR_QA_WORLD_MAP_OVERLAY_REVISION = YES
