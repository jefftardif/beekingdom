"""Static guardrails for SQL spatial preflight scripts; no DB mutations.

Passing here is NOT a replacement for disposable-db execution and parallel
transactions with actual SQL Server. This gate prevents accidental auto-release.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "Server" / "ops" / "world-map-semantic"
SCHEMA = (BASE / "100_worldmap_spatial_reservations.staging.sql").read_text("utf-8")
PROC = (BASE / "101_worldmap_spatial_atomic_commit.staging.sql").read_text("utf-8")
SANDBOX = (BASE / "102_worldmap_spatial_sandbox_contract_test.sql").read_text("utf-8")
CATALOG = (ROOT / "Server" / "src" / "BeeKingdom.Database" / "DatabaseCatalog.cs").read_text("utf-8")
GATEWAY = (ROOT / "Server" / "src" / "BeeKingdom.HiveOperations" /
           "SqlWorldMapSpatialCommitGateway.cs").read_text("utf-8")

def executable(sql):
    return "\n".join(line for line in sql.splitlines()
                     if not line.strip().startswith("--"))

class SqlSpatialContractTests(unittest.TestCase):
    def test_no_auto_production_migration(self):
        self.assertNotIn("WorldMapSpatialScopes", CATALOG)
        self.assertNotIn("WorldMapSpatialReservations", CATALOG)
        self.assertNotIn("100_worldmap_spatial", CATALOG)

    def test_sql_scope_defaults_to_disabled_and_no_seed(self):
        self.assertRegex(SCHEMA, r"WritesEnabled\s+bit\s+NOT NULL")
        self.assertRegex(SCHEMA, r"DF_WMSpatialScopes_WritesEnabled DEFAULT 0")
        self.assertNotRegex(executable(SCHEMA), r"(?i)INSERT\s+(INTO\s+)?dbo\.WorldMapSpatialScopes")

    def test_procedure_has_global_scope_lock_and_serializable_cas(self):
        self.assertIn("sp_getapplock",PROC)
        self.assertIn("@LockOwner='Transaction'",PROC)
        self.assertIn("SET XACT_ABORT ON",PROC)
        self.assertIn("BEGIN TRANSACTION",PROC)
        self.assertIn("COMMIT TRANSACTION",PROC)
        self.assertIn("WritesEnabled=1",PROC)
        self.assertIn("@ExpectedRevision",PROC)
        self.assertIn("TerrainContentSha256=@TerrainContentSha256",PROC)

    def test_procedure_protects_auth_idempotency_and_overlap(self):
        self.assertIn("old.OwnerPlayerId<>@ActorPlayerId",PROC)
        self.assertIn("dbo.WorldMapSpatialReceipts",PROC)
        self.assertIn("Idempotency key conflict.",PROC)
        self.assertIn("Spatial footprint occupied",PROC)
        self.assertIn("Spatial batch contains overlapping",PROC)
        self.assertIn("Spatial move not authorized",PROC)
        self.assertIn("dbo.WorldMapSpatialPlacementBatch READONLY",PROC)

    def test_staging_scripts_never_grant_execute_to_api(self):
        for source in (SCHEMA, PROC, SANDBOX):
            self.assertNotRegex(executable(source),r"(?i)\bGRANT\s+EXECUTE\b")

    def test_sandbox_can_never_target_production(self):
        self.assertIn("DB_NAME() NOT LIKE N'BeeKingdomSpatialSandbox[_]%'",SANDBOX)
        self.assertIn("Expected collision rejection",SANDBOX)
        self.assertIn("Expected unauthorized move rejection",SANDBOX)
        self.assertIn("Expected stale revision rejection",SANDBOX)
        self.assertIn("Idempotent replay",SANDBOX)

    def test_gateway_requires_server_footprints_and_is_off_by_default(self):
        self.assertIn("IWorldMapReviewedFootprintVerifier",GATEWAY)
        self.assertIn("bool enabledByServer = false",GATEWAY)
        self.assertIn("server_spatial_feature_disabled",GATEWAY)
        self.assertIn("Rounded",GATEWAY.replace("rounded","Rounded"))
        self.assertIn("ReviewedSemanticPlacementPlanner.Build",GATEWAY)

if __name__ == "__main__":
    unittest.main()
