# SERVER-B-065 - World Map Overlay Guardrails Correction Report

Date: 2026-07-14  
Role: Server-B targeted correction  
Boundary: local only, no endpoint, HTTP, SQL activation, staging, remote access or Unity  
Report location: local staging path because the contractual reports directory is outside the writable workspace

## Executive Result

The four SERVER-064 QA blockers are closed locally.

`WorldMapChunkReadinessContract.FinalizeReadinessOverlays` is now the single final composition boundary for provider overlays. It replaces the overlay envelope, recalculates `EstimatedPayloadBytes` with the canonical private formula, validates all local non-live/authority/flight rules, and derives stable typed errors. `WorldMapChunkQueryService` consumes this finalized response and returns `Rejected` before `Success` or `NotModified` whenever finalization produces an error.

The final targeted matrix passes 15/15, all shared contracts pass 74/74, the complete Release suite passes 165/0/6, and the typed ops verifier passes offline with its local empty-source NuGet configuration. No WorldMap test is skipped and no verifier/test-data residue remains.

## QA Blockers Closed

### B1 - Stale final payload estimate

Before correction, the provider envelope replaced canonical overlays after guardrails were calculated. The response could therefore contain more overlays than its estimate represented.

Correction:

- the service no longer assigns `canonical with { Overlays = overlays }`;
- it calls `WorldMapChunkReadinessContract.FinalizeReadinessOverlays`;
- finalization invokes the existing private `EstimatePayloadBytes` formula;
- the final guardrail is written back with the real provider counts;
- any final estimate above `PayloadBudgetBytes` adds `PayloadBudgetExceeded`;
- the service returns `Rejected` with null response/cache metadata.

No payload formula was copied into `WorldMapChunkQueryService`.

Evidence:

| Provider payload | Final estimate | Budget | Result |
|---|---:|---:|---|
| 2 hives, 3 resources, 2 flights, 25 chunks | `16896` | `98304` | `Success` with estimate `16896` |
| 1 hive, 1000 resources, 1 flight, 25 chunks | `271488` | `98304` | `Rejected / PayloadBudgetExceeded` |

Repeated finalization of the oversized envelope keeps one derived payload error and the same `271488` estimate.

### B2 - Missing non-live and authority enforcement

Finalization now rejects every requested violation:

- envelope `PaintedIntoBackground=true`;
- envelope `Live=true`;
- envelope `ServerAuthoritative=true`;
- any hive `Live=true` or `ServerAuthoritative=true`;
- any resource `Live=true` or `ServerAuthoritative=true`;
- any flight `Live=true` or `ServerAuthoritative=true`;
- any flight `AirOnly=false`;
- any flight `RoadGraphUsed=true`.

The successful response test also proves all envelope/entity live and authority flags false, overlays separate, flights air-only and road graph disabled.

### R2 - Incorrect overlay error taxonomy

`WorldMapChunkErrorCode.OverlayContractViolation = 8` was appended without changing values `0..7`.

- shape, separation, live, authority and flight-rule violations use `OverlayContractViolation`;
- only final byte-budget overflow uses `PayloadBudgetExceeded`.

The canonical guardrail error catalog now contains nine stable names. Both JSON examples were regenerated from the real DTO so the new name is represented without changing spatial/cache values.

### R1 - Shared-instance multi-world evidence

The concurrency test now creates exactly:

- one `WorldMapChunkQueryService` instance;
- one shared identity provider containing two `(WorldId, GameServerId)` states;
- one shared deterministic overlay provider.

Four reads execute concurrently through that same object graph, two per world. Assertions verify world IDs, server IDs, invalidation scopes and distinct manifests. A crossed `(worldA, serverB)` request through the same service/provider is rejected as `UnknownWorld` with null response/cache metadata.

## Canonical Finalization API

The public API is:

```text
WorldMapChunkReadinessContract.FinalizeReadinessOverlays(response, overlays)
```

It performs these steps in order:

1. Calculate the final estimate from chunk and actual overlay counts.
2. Replace `Guardrails.EstimatedPayloadBytes`.
3. Remove prior finalization-derived overlay/payload errors for deterministic repeated use.
4. Add `OverlayContractViolation` when any overlay contract rule is broken.
5. Add `PayloadBudgetExceeded` when the final estimate exceeds the budget.
6. Return the response with final overlays, guardrails and typed errors.

The query service returns `Rejected(response.Errors)` when the final errors list is non-empty. Existing canonical errors for world geometry remain handled before provider access.

## Result Metadata Evidence

### Success

- `State = Success`;
- `Response` non-null;
- result ETag equals `Response.Cache.ETag`;
- result manifest hash equals `Response.Cache.ManifestHash`;
- result invalidation key equals `Response.Cache.InvalidationKey`;
- `Errors` empty;
- final provider payload estimate visible in the response.

### NotModified

- `State = NotModified`;
- `Response = null`;
- exact ETag, manifest hash and invalidation key retained;
- `Errors` empty;
- exact ordinal `IfNoneMatch` comparison retained;
- no HTTP 304 or adapter claim.

### Rejected

- `State = Rejected`;
- `Response = null`;
- ETag, manifest hash and invalidation key all null;
- exactly the expected typed error in every negative provider scenario.

## Preserved Spatial and Cache Contract

The correction does not change chunk generation, hash input or ETag input.

| Evidence | Full | Edge |
|---|---:|---:|
| Chunks | 25 | 9 |
| Order | Y then X | Y then X |
| Manifest | `a6e46a84bc24cb94111c09a1a3ea44aced10323575f6a2dad47497b477b55fa1` | `d9038abfee2eb1150d4e04986fa3a8e8cf879811398c04d102ba80c5f85c754a` |
| ETag | `W/"9e17913cd519f0a06f111055b78c5aae3e7fc3119ac89d5f82490fed7d6c8151"` | `W/"3073255be6df2ae53ea1dd68da925c78a8bf705b7cf26e5bec51e330722e9a24"` |

The regenerated JSON file hashes changed only because the guardrail error catalog gained `OverlayContractViolation`:

- full example SHA-256: `4c876e095572072b213eac81a4a2e49773a4ae1fc43b88fada093e5477363421`;
- edge example SHA-256: `8057cd362dc24fda1122b137f1877d2590983dcd01a0e56c32943b81e6f65dd2`.

## Exact Targeted Tests

Release command with `--no-restore`: 15 passed, 0 failed, 0 skipped.

1. `WorldMapChunkQueryServiceChangesManifestHashEtagAndInvalidationWhenSeedOrRevisionChanges`
2. `WorldMapChunkQueryServiceHonorsCancellation`
3. `WorldMapChunkQueryServiceKeepsCacheBytesHashAndEtagDeterministic`
4. `WorldMapChunkQueryServicePreservesEdgeClippingAndPayloadGuardrails`
5. `WorldMapChunkQueryServiceRecalculatesSuccessfulProviderOverlayPayload`
6. `WorldMapChunkQueryServiceRejectsLiveOrAuthoritativeOverlayEntities`
7. `WorldMapChunkQueryServiceRejectsLiveOrAuthoritativeOverlayEnvelope`
8. `WorldMapChunkQueryServiceRejectsNonAirborneFlightProviderPayload`
9. `WorldMapChunkQueryServiceRejectsOversizedFinalOverlayPayload`
10. `WorldMapChunkQueryServiceRejectsPaintedOverlayProviderPayload`
11. `WorldMapChunkQueryServiceRejectsRoadGraphFlightProviderPayload`
12. `WorldMapChunkQueryServiceRejectsWorldServerMismatchAndBadRevision`
13. `WorldMapChunkQueryServiceReturnsNotModifiedWithoutBodyWhenIfNoneMatchHits`
14. `WorldMapChunkQueryServiceReturnsSuccessWithCanonicalResponse`
15. `WorldMapChunkQueryServiceSupportsConcurrentReadsWithoutCrossWorldLeakage`

## Shared Contracts and Release Suite

`SharedContractsTests`:

```text
Passed:  74
Failed:  0
Skipped: 0
```

Complete Release suite:

```text
Total:   171
Passed:  165
Failed:  0
Skipped: 6
```

The six skips remain exactly the existing SQL opt-in scenarios:

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

No WorldMap, JSON or query-service test was skipped.

## Offline Ops Verifier

The final verifier run used `Server/ops/world-map-chunk-contract/NuGet.Config` with an empty package-source list. No user NuGet profile or network feed was required.

```text
ExitCode = 0
WORLD_MAP_CHUNK_JSON_CONTRACT_VERIFICATION = PASS
FullWindowChunks = 25
EdgeWindowChunks = 9
Post-verifier bin/obj residue = 0
```

## Scope, Secrets and Residues

Final checks:

```text
Live binding references = 0
Forbidden implementation references = 0
Secret candidates = 0
Ops residue = 0
Server test-data residue = 0
Production Persistence.Provider = InMemory
```

No endpoint, route, controller, HTTP binding, SQL client, SQL activation, staging action, remote access, secret, real player data or Unity dependency was added or used.

The following evidence/tooling files remained unchanged from SERVER-064:

- `WorldMapChunkJson.cs`;
- verifier project and `Program.cs`;
- local `NuGet.Config`;
- verifier PowerShell script;
- historical SERVER-B-063 and SERVER-064 reports.

## Files Changed

- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkContracts.cs`
- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkQueryService.cs`
- `Server/tests/BeeKingdom.Tests/SharedContractsTests.cs`
- `Server/ops/world-map-chunk-contract/example-window-5x5.json`
- `Server/ops/world-map-chunk-contract/example-edge-window.json`
- `Server/ops/world-map-chunk-contract/README.md`
- `Server/ops/world-map-chunk-contract/world-map-chunk-contract-spec.md`
- `Server/ops/world-map-chunk-contract/SERVER-B-065 - World Map Overlay Guardrails Correction Report.md`

## Remaining Future Limit

The ETag remains the canonical background-manifest validator. Dynamic live overlays must gain a separate revision or later validator design before any live `NotModified` behavior. Current providers are deterministic, local and non-live, so this does not block SERVER-B-065.

SERVER_B_065_WORLD_MAP_OVERLAY_GUARDRAILS = PASS
FINAL_OVERLAY_PAYLOAD_BUDGET_ENFORCED = YES
LOCAL_NONLIVE_OVERLAY_FLAGS_ENFORCED = YES
SHARED_INSTANCE_MULTI_WORLD_ISOLATION = PASS
READY_FOR_QA_WORLD_MAP_OVERLAY_GUARDRAILS = YES
