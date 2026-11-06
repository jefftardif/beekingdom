# BeeKingdom - WorldMap spatial SQL: preproduction runbook (2026-11-06)

STATUS: ISOLATED SQL EXECUTION AND CONCURRENT C# INTEGRATION PASS. LIVE GAMEPLAY NOT ACTIVATED.

These three SQL scripts are intentionally NOT registered in automatic DatabaseCatalog.Migrations.

- 100_worldmap_spatial_reservations.staging.sql: world/surface revision, reservations, idempotency receipts. WritesEnabled defaults to 0, and no world rows are created.
- 101_worldmap_spatial_atomic_commit.staging.sql: server-only TVP and atomic commit stored procedure. No GRANT EXECUTE issued.
- 102_worldmap_spatial_sandbox_contract_test.sql: executable checks on a disposable database. Refuses all DB names without the BeeKingdomSpatialSandbox_ prefix.

The C# SqlWorldMapSpatialCommitGateway is compiled into BeeKingdom.HiveOperations and shares the reviewed SemanticTerrain/SharedPlacementPlanner logic from BeeKingdom.Shared.

## Admission and transaction guards

1. Server must authenticate actor and validate game-server/world/surface IDs, reviewed terrain revision and SHA-256, and an independently reviewed full icon + permanent caption footprint. Never trust client-provided bounds.
2. ReviewedSemanticPlacementPlanner must admit the entire footprint, including the separation margin, only in certified Land/River/Lake pixels. Unknown fails closed.
3. SQL uses SERIALIZABLE and a per-world sp_getapplock exclusive transaction lock. It validates expected revision, ownership for moves, geometric overlap with all existing objects and within the submitted batch, and idempotency receipts.
4. Entire batch is inserted/updated and its receipt written in one transaction. Rejects must leave no partial claim.
5. Both activation flags default to OFF: C# constructor enabledByServer=false and database WritesEnabled=0. No API endpoint is connected to the new gateway yet.

## Checks actually executed

- BeeKingdom.HiveOperations build .NET 8: PASS, no warnings or errors.
- NUnit WorldMapSpatialCommitGatewayTests and ReviewedSemanticPlacementServerTests: 16/16 PASS.
- Static SQL contract unit tests: 7/7 PASS.
- T-SQL syntax on SRVESDT using SET PARSEONLY ON: all 3 scripts parse, with zero DDL applied.
- Actual competing SQL sessions in tempdb: first sp_getapplock returned 0; conflicting attempt returned -1.
- Production API /health Healthy; world-map-readiness remains PreparationOnly, readOnly=true and nonLive=true.
- Production activation preflight: BLOCKED, as expected.

The full T-SQL procedure HAS NOW been run and passed against a separate
user-owned SQL Server 2025 LocalDB instance on PCJEFF, not against the
production game database. SRVESDT remote SYSTEM still lacks privileges
to create a disposable database; do not bypass that restriction.

## How an authorized SQL administrator can perform the isolated QA

Create a disposable, dedicated database named BeeKingdomSpatialSandbox_20261106. Never use a preexisting user database. From the directory holding the scripts, in that database only, run in this order:

1. sqlcmd -S .\SQLEXPRESS -E -C -b -d BeeKingdomSpatialSandbox_20261106 -i 100_worldmap_spatial_reservations.staging.sql
2. sqlcmd -S .\SQLEXPRESS -E -C -b -d BeeKingdomSpatialSandbox_20261106 -i 101_worldmap_spatial_atomic_commit.staging.sql
3. sqlcmd -S .\SQLEXPRESS -E -C -b -d BeeKingdomSpatialSandbox_20261106 -i 102_worldmap_spatial_sandbox_contract_test.sql

OBSERVED on PCJEFF LocalDB: BK_SPATIAL_SQL_SANDBOX_PASS,
2 reservations, revision 3. The contract tested successful placement,
idempotent replay, collision rejection, unauthorized ownership
replacement, and stale revision rejection.

OBSERVED in the opt-in NUnit integration test: two simultaneous C#
gateway calls from different actors using the same world revision resulted
in exactly ONE committed reservation, ONE idempotency receipt,
a revision of 2, and one rejected stale caller. Exact winner replay and
subsequent conflicting placement preserved this state.

## Required before activating the production game

- Independent human or authoritative review of WorldMap Wave6 terrain and shorelines, version-pinned against source tile SHA-256. The nine pilot tiles are still candidates, not approved gameplay data.
- An authoritative footprint catalogue on the SERVER including the rendered artwork, labels, controls and margin at the smallest supported zoom. Implement and verify IWorldMapReviewedFootprintVerifier.
- An authenticated server-side route and/or trusted system service that applies the gateway to initial generation, player movement, NPC movement and resource respawn. Client data cannot grant server occupancy.
- Disposable SQL QA is now PASS; repeat against the exact candidate production build and validate rollback before any additive production migration. Never auto-seed WritesEnabled=1.
- Correct 22 estimated overlapping pairs and 18 unresolved historical positions through certified world-coordinate changes, not on-screen shuffling.
- Full runtime Unity/Android visual and hitbox validation; recheck HUD/Chat regressions.
- Production gate must be READY with genuine runtime proof, not simply flipped booleans.

No live IIS, production SQL, existing game coordinates or main branch were modified by this prototype.

## PCJEFF LocalDB end-to-end evidence — 6 November 2026

Created a new **isolated** per-user LocalDB instance:
`(localdb)\BKSpatialQA20261106` (SQL Server 2025 17.0.4025.3).
Within this instance only, created the disposable database
`BeeKingdomSpatialSandbox_20261106`.

Using the Windows ODBC sqlcmd executable (not the Go sqlcmd build,
which did not discover the named LocalDB instance), executed scripts
100, 101 and 102 with `-b`, all **exit 0**. Actual SQL result:
`BK_SPATIAL_SQL_SANDBOX_PASS`, two reservations, occupation revision 3.
All intended rejections left the world unchanged.

Added `Server/tests/BeeKingdom.Tests/WorldMapSpatialLocalDbIntegrationTests.cs`
with two independent C# requests executed concurrently against the same
isolated SQL database. Result `Passed`, 1/1. Combined targeted NUnit
test suite: **17/17 PASS** (16 unit tests + 1 SQL integration).
Log/artifact:
`Server/tests/BeeKingdom.Tests/TestResults/BK-Spatial-Combined-QA-20261106.trx`.

The integration fixture FAILS CLOSED unless **both** of these
environment variables are deliberately provided:
- `BK_ENABLE_DISPOSABLE_SPATIAL_QA=1`
- `BK_SPATIAL_SANDBOX_CONNECTION` with a DataSource starting
  `(localdb)\BKSpatialQA`, database starting
  `BeeKingdomSpatialSandbox_` and integrated security.

The isolated QA database was backed up on PCJEFF with SQL Server
`BACKUP DATABASE ... WITH CHECKSUM` and verified using
`RESTORE VERIFYONLY ... WITH CHECKSUM` (**PASS**).
Local QA backup:
`C:\BeeKingdom\qa\BK-Spatial-Sandbox-Backup-20261106.bak`
(4,771,840 bytes, SHA-256
`414409E3A00C5DE8CABA9535E14CD6CE969D3C4AA8E9AB886461D802811B1D2D`).

**This does not authorize production placement.** The approved map terrain,
actual per-icon/per-label footprint catalogue, authenticated and trusted
server route, review of historical conflicts and Unity Android validation
remain outstanding. No database production schema was migrated, no
gameplay endpoint activated, and no game entity relocated.