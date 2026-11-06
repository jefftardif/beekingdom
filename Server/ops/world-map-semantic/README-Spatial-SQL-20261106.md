# BeeKingdom - WorldMap spatial SQL: preproduction runbook (2026-11-06)

STATUS: CODE READY FOR ISOLATED SQL QA. LIVE GAMEPLAY NOT ACTIVATED.

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

IMPORTANT: This is NOT proof the full stored procedure works. It has not run on a disposable SQL database. The remote SYSTEM agent is not sysadmin on either SQL Server Express instance, and no BeeKingdomSpatialSandbox_* database exists. Do not bypass these privileges or test against the real BeeKingdom database.

## How an authorized SQL administrator can perform the isolated QA

Create a disposable, dedicated database named BeeKingdomSpatialSandbox_20261106. Never use a preexisting user database. From the directory holding the scripts, in that database only, run in this order:

1. sqlcmd -S .\SQLEXPRESS -E -C -b -d BeeKingdomSpatialSandbox_20261106 -i 100_worldmap_spatial_reservations.staging.sql
2. sqlcmd -S .\SQLEXPRESS -E -C -b -d BeeKingdomSpatialSandbox_20261106 -i 101_worldmap_spatial_atomic_commit.staging.sql
3. sqlcmd -S .\SQLEXPRESS -E -C -b -d BeeKingdomSpatialSandbox_20261106 -i 102_worldmap_spatial_sandbox_contract_test.sql

EXPECTED, NOT YET OBSERVED: BK_SPATIAL_SQL_SANDBOX_PASS, 2 reservations, revision 3. The contract tests successful placement, replay without duplication, collision rejection, unauthorized ownership replacement and stale revision rejection. Follow with actual parallel end-to-end stored-procedure tests before release.

## Required before activating the production game

- Independent human or authoritative review of WorldMap Wave6 terrain and shorelines, version-pinned against source tile SHA-256. The nine pilot tiles are still candidates, not approved gameplay data.
- An authoritative footprint catalogue on the SERVER including the rendered artwork, labels, controls and margin at the smallest supported zoom. Implement and verify IWorldMapReviewedFootprintVerifier.
- An authenticated server-side route and/or trusted system service that applies the gateway to initial generation, player movement, NPC movement and resource respawn. Client data cannot grant server occupancy.
- Disposable database SQL QA and tested rollback. Apply any additive SQL migration in production only after backup and exact compatibility checks; never auto-seed WritesEnabled=1.
- Correct 22 estimated overlapping pairs and 18 unresolved historical positions through certified world-coordinate changes, not on-screen shuffling.
- Full runtime Unity/Android visual and hitbox validation; recheck HUD/Chat regressions.
- Production gate must be READY with genuine runtime proof, not simply flipped booleans.

No live IIS, production SQL, existing game coordinates or main branch were modified by this prototype.