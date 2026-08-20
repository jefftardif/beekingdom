# M001-CL RUNTIME BASELINE

**Project:** BeeKingdom
**Mission:** M001
**Owner:** CL
**Status:** CLOSED
**Historical record:** YES
**Date:** ~2026-08-18

---

## HISTORICAL REPORT — PARTIALLY RECONSTRUCTED

**Notice:** The exact original M001 report is not available in the repository. This document has been reconstructed from Git history, test results, and subsequent mission context (M002, M004). Some details may be incomplete.

---

## CONTEXT (Reconstructed)

CL established the current runtime baseline for the BeeKingdom project. This involved verifying the Unity client and .NET server can build and run, and identifying any regressions in the test suite.

## OBJECTIVE (Reconstructed)

1. Verify Unity compilation (0 CS errors)
2. Verify server test suite baseline
3. Identify failing tests and their root causes
4. Document the current state of HiveMap vs LivingHive scenes

## FINDINGS (Reconstructed from Evidence)

### Compilation
- Unity: **0 CS errors** (verified via batchmode build)
- Server: **Builds successfully**

### Server Test Suite Baseline
Two tests were found failing:

1. `AuthenticationProductionBoundaryTests.Production_keeps_account_creation_and_token_issuance_closed`
2. `DatabaseMigrationTests.ProductionConfigurationRemainsInMemoryAndContainsNoSqlConnectionValue`

### Root Cause Identified
The failing tests were traced to `Server/src/BeeKingdom.Server/appsettings.Production.json` where `Persistence.Provider` was set to `"SqlServer"` instead of the expected `"InMemory"`. This change appears to have been introduced in commit `7f3fc18` ("HiveMap + LivingHive + Auth + Production bootstrap (2026-08-19)").

### HiveMap vs LivingHive State
- **LivingHive scene:** `LivingHive.unity` — legacy monolith entry point with `LivingHiveDemoBootstrap`
- **HiveMap scene:** `Environment2D5D_HiveMap_Test.unity` — new 2.5D scene with `HiveMap*Bootstrap` adapters
- **LivingHiveMenu:** Fully ported to uGUI in `BeeKingdom.LivingHiveMenu` package (Canvas, Header, ResearchWindow, etc.)
- **HiveMap Bootstraps:** 11 adapter files created in `7f3fc18` translating 3D building clicks to monolith `ForExternalHost` methods

## ACTIONS TAKEN (Reconstructed)

CL documented the failing tests and traced the regression to the Production configuration. This enabled M002 to execute the targeted fix.

## VALIDATION (Reconstructed)

After M002 fix (`7b59f47`):
- Both previously failing tests: ✅ Passed
- Full server suite: **385 passed, 8 skipped** (SQL integration tests)

## LIMITATIONS OF RECONSTRUCTION

The following could not be verified from available evidence:
- Exact date of M001 execution
- Complete list of tests run during M001 (only the two failing ones are known from M002 context)
- Any additional runtime observations made by CL
- Whether M001 included any Unity scene validation beyond compilation
- Exact commands/scripts used by CL for baseline verification

## RELATED COMMITS

- `4e88f68` — BASELINE: recover latest LivingHive production state (pre-M001)
- `7f3fc18` — Introduced the SqlServer Provider regression (HiveMap bootstrap commit)
- `0e2af83` — HiveMap sidecar + context + overview fix
- `7b59f47` (M002) — Restored production persistence guardrail (InMemory Provider)

---

*This report was reconstructed on 2026-08-20 by OC as part of M007 mission history consolidation.*