# M002-OC PRODUCTION GUARDRAILS

**Project:** BeeKingdom
**Mission:** M002
**Owner:** OC
**Status:** CLOSED
**Historical record:** YES
**Date:** 2026-08-20

---

## CONTEXT

During M001, CL established the current runtime baseline and discovered two failing server tests:

* `AuthenticationProductionBoundaryTests.Production_keeps_account_creation_and_token_issuance_closed`
* `DatabaseMigrationTests.ProductionConfigurationRemainsInMemoryAndContainsNoSqlConnectionValue`

CL traced the regression to:

`Server/src/BeeKingdom.Server/appsettings.Production.json`

The committed Production configuration currently declares:

`Provider = "SqlServer"`

while the project's existing production safety tests expect the committed baseline configuration to remain `InMemory` and contain no SQL connection value.

This appears to have been introduced unintentionally in commit `7f3fc18`.

## OBJECTIVE

Restore the intended committed Production configuration and return the server test suite to its expected passing state.

This is a narrowly scoped corrective mission.

## INSTRUCTIONS

1. Inspect the two failing tests and the current Production configuration.

2. Confirm that the failures are caused by the unintended `SqlServer` provider change.

3. Restore the intended safe committed configuration.

Do NOT weaken or delete the tests merely to make them pass.

Do NOT introduce credentials or connection strings.

Do NOT change unrelated feature flags.

Do NOT refactor configuration architecture.

4. Run the two previously failing tests first.

5. If both pass, run the relevant full server test suite necessary to verify that the correction caused no regression.

6. Check Git diff carefully.

The final diff must contain only changes required for this correction.

7. Do not commit unless explicitly instructed.

## ACCEPTANCE CRITERIA

* Both previously failing Production guardrail tests pass.
* No SQL credentials or connection values are committed.
* Production safety behavior expected by the existing tests is restored.
* Relevant server tests pass.
* No unrelated files are modified.

## REPORT

Return:

### M002-OC RESULT

**Root Cause**

**Changes**

**Files Modified**

**Validation**
Include exact test counts/results.

**Git Diff Summary**

**Remaining Issues**

**Recommendation**

Then stop and wait for GPT orchestration.

---

## EXECUTION LOG

### Root Cause
Commit `7f3fc18` ("HiveMap + LivingHive + Auth + Production bootstrap (2026-08-19)") unintentionally changed `appsettings.Production.json` line 26 from `"Provider": "InMemory"` to `"Provider": "SqlServer"`. This violated the production safety guardrails that the committed configuration must remain in-memory with no SQL connection values.

### Changes
Restored `Persistence.Provider` to `"InMemory"` in `Server/src/BeeKingdom.Server/appsettings.Production.json`.

### Files Modified
- `Server/src/BeeKingdom.Server/appsettings.Production.json` (single line)

### Validation
| Test | Result |
|---|---|
| `AuthenticationProductionBoundaryTests.Production_keeps_account_creation_and_token_issuance_closed` | ✅ Passed (573 ms) |
| `DatabaseMigrationTests.ProductionConfigurationRemainsInMemoryAndContainsNoSqlConnectionValue` | ✅ Passed (1 ms) |
| **Full server test suite** | ✅ **385 passed, 8 skipped** (SQL integration tests requiring `BEE_SQL_INTEGRATION_CONNECTION_STRING`) |

### Git Diff Summary
```diff
-    "Provider": "SqlServer"
+    "Provider": "InMemory"
```

### Remaining Issues
None. The production guardrail tests pass, no credentials or connection strings are committed, and the full test suite passes without regression.

### Recommendation
Ready for commit. The correction is minimal, targeted, and restores the intended safe baseline.

---

## COMMIT RECORD

**Commit:** `7b59f47`
**Message:** `Restore production persistence guardrail`
**Files:** 1 changed (`Server/src/BeeKingdom.Server/appsettings.Production.json`)

**Working Tree Status After Commit:** Clean — only untracked file was `Server/tests/BeeKingdom.Tests/TestResults/results.trx` (test run artifact).