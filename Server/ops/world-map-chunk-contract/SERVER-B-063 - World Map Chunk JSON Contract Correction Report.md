# SERVER-B-063 - World Map Chunk JSON Contract Correction Report

Date: 2026-07-14  
Owner: Server-B  
Execution boundary: local repository only  
Status: correction and validation complete

## Executive Result

The SERVER-062 JSON/wire blocker is closed locally. The two checked-in examples are complete canonical serializations of the real `WorldMapChunkWindowResponse` DTO, using the dedicated Bee Kingdom world-map JSON configuration. A package-free .NET verifier deserializes both examples into the real DTO and verifies their complete canonical form plus independent spatial, cache, hash, pagination, guardrail, overlay, error, passive-feature and non-claim invariants.

No endpoint was created. No SQL operation, staging action, production action, Unity change, remote access, secret, or real player data was used.

## Root Cause Closed

The previous examples were hand-maintained summaries rather than serializations of the runtime DTO. They represented chunks and overlays as summary objects or strings, omitted DTO fields, added non-contract fields, used placeholder hashes and cache values, and did not prove deterministic deserialization of `WorldId`, `GameServerId`, or `ContractVersion`. The previous PowerShell verifier only performed shallow untyped JSON checks.

## Correction

1. Added `WorldMapChunkJson.CreateOptions()` as the explicit future wire configuration derived from `BeeJson`.
2. Added strict converters for `WorldId`, `GameServerId`, and `ContractVersion` while preserving the domain value objects unchanged.
3. Added named enum serialization with integer enum values rejected.
4. Rejected unknown JSON members and emitted null response members explicitly.
5. Added `WorldMapChunkPreparatoryFeatures` to the response so passive behavior is wire-visible.
6. Generated both examples from the real runtime response DTO.
7. Replaced shallow PowerShell assertions with a typed .NET generator/verifier.
8. Added request/response round-trip, malformed wire value, artifact parity, invalid radius, seed change, artistic revision change, and deterministic invalidation tests.

The validated 5x5 construction, Y-then-X ordering, edge clipping, stable coordinates, overlay separation, and air-only flight invariants were not changed.

## Canonical Wire Rules

| Value | Canonical JSON representation |
|---|---|
| `WorldId` | Lowercase 32-character GUID, `N` format |
| `GameServerId` | Lowercase 32-character GUID, `N` format |
| `ContractVersion` | Non-negative canonical `major.minor.patch` string |
| Enums | Lower camel-case names; integer values rejected |
| Unknown members | Rejected during deserialization |
| Nullable response members | Emitted explicitly as JSON `null` |

## Generated Evidence

| Evidence | Full window | Edge-clipped window |
|---|---:|---:|
| Center | `(10,-4)` | `(0,0)` |
| World bounds | `-1024..1024` X/Y | `0..2` X/Y |
| Radius | `2` | `2` |
| Complete chunks | `25` | `9` |
| Order | Y then X | Y then X |
| Estimated payload | `15744` bytes | `7552` bytes |
| Manifest hash | `a6e46a84bc24cb94111c09a1a3ea44aced10323575f6a2dad47497b477b55fa1` | `d9038abfee2eb1150d4e04986fa3a8e8cf879811398c04d102ba80c5f85c754a` |
| ETag | `W/"9e17913cd519f0a06f111055b78c5aae3e7fc3119ac89d5f82490fed7d6c8151"` | `W/"3073255be6df2ae53ea1dd68da925c78a8bf705b7cf26e5bec51e330722e9a24"` |
| Invalidation key | `world:00000000000000000000000000000001:map:art-revision-readiness-001` | Same |

Both examples contain complete chunk arrays, complete hive/resource/flight overlay arrays, cache metadata, pagination including explicit nulls, guardrails and the full error-code catalog, an empty success error list, all non-claims, all passive-feature flags, and contract version `1.0.0`.

## Passive and Preparatory Semantics

- `IfNoneMatchPassive = true`: `ifNoneMatch` is represented but no cache-validator or HTTP 304 behavior executes.
- `DeltaPageTokenPassive = true`: `deltaPageToken` is represented but is not consumed or validated.
- `ContractVersionNegotiationPassive = true`: the request version is serializable, but no version negotiation executes.
- `FutureErrorCodesPassive = true`: only local radius and bounds validation are active; future world lookup, delta, manifest, and authentication errors remain catalog entries.

These flags are descriptive non-live readiness evidence, not feature activation.

## Verification Results

| Validation | Result |
|---|---|
| Typed ops verifier, final run | PASS |
| Full example typed deserialization and canonical comparison | PASS, 25 chunks |
| Edge example typed deserialization and canonical comparison | PASS, 9 chunks |
| Independent manifest hash and ETag recalculation | PASS |
| Origins, dimensions, Y/X order and clipping | PASS |
| Cache, invalidation and pagination | PASS |
| Complete overlays, separation and air-only flight | PASS |
| Guardrails, errors, passive features and non-claims | PASS |
| Repeated generation byte hashes | PASS, deterministic |
| Targeted `SharedContractsTests` Release run | PASS, 59 passed / 0 failed / 0 skipped |
| Complete .NET Release suite | PASS, 150 passed / 0 failed / 6 skipped / 156 total |
| Secret and forbidden-scope scan | PASS |
| Post-verifier residue check | PASS, no verifier `bin`/`obj`, database, backup, or temporary artifact |

## Exactly Skipped Tests

The complete Release run skipped these six existing SQL opt-in tests because no SQL integration connection was supplied. This JSON-only task did not activate or simulate them:

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

No world-map JSON test was skipped.

## Files Changed

- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkContracts.cs`
- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkJson.cs`
- `Server/tests/BeeKingdom.Tests/SharedContractsTests.cs`
- `Server/ops/world-map-chunk-contract/BeeKingdom.WorldMapChunkContractVerifier.csproj`
- `Server/ops/world-map-chunk-contract/NuGet.Config`
- `Server/ops/world-map-chunk-contract/Program.cs`
- `Server/ops/world-map-chunk-contract/Test-WorldMapChunkContract.ps1`
- `Server/ops/world-map-chunk-contract/example-window-5x5.json`
- `Server/ops/world-map-chunk-contract/example-edge-window.json`
- `Server/ops/world-map-chunk-contract/README.md`
- `Server/ops/world-map-chunk-contract/world-map-chunk-contract-spec.md`
- `Server/ops/world-map-chunk-contract/SERVER-B-063 - World Map Chunk JSON Contract Correction Report.md`

## Boundaries and Remaining Limits

- No live or preparatory HTTP endpoint exists.
- The dedicated JSON options are not registered into any live service pipeline.
- There is no SQL-backed map state, world lookup, active server authority, or real player data.
- There is no active HTTP caching, conditional response, delta paging, or version negotiation implementation.
- There is no Unity integration or visual continuity claim.
- No staging or production configuration was read or changed for execution.
- No remote host was contacted.
- The requested external report directory is outside this task's writable workspace. Nothing was published there; this local report is the canonical Server-B artifact.

READY_FOR_QA_WORLD_MAP_CHUNK_JSON_CONTRACT = YES
