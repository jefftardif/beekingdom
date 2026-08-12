# ARCH-180 - BEE-821 To BEE-840 Validation And Dispatch

Date: 2026-07-12
Status: validated and dispatched

## Sources Reviewed

- `C:/projets/beekingdom/prompts_codex/rapports/BEE-821_Report.md`
- `C:/projets/beekingdom/prompts_codex/rapports/BEE-840_Report.md`
- `C:/projets/beekingdom/prompt_ui/rapports/UI-065_HIVE_DEVICE_POLISH_AND_PORTRAIT_FINALIZATION.md`
- `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE821_840_RiskPrep_Report.md`
- `C:/projets/beekingdomgame-master/Docs/BuilderB/BEE821_840_AutomatedChecks_InputBoundaries_Support.md`
- `C:/projets/beekingdomgame-master/Docs/BuilderC/BEE821_840_DeviceTouch_Automation_RegressionMatrix.md`
- `C:/projets/beekingdom/prompt_server/rapports/SERVER-034 - Hive Loop Authoritative Catalog Prep Report.md`

## Planner Validation

BEE-821 through BEE-840 are accepted as the next playable Hive product wave.

The lot is coherent with ARCH-179:

- playable Hive remains the priority;
- no major world-map expansion;
- no live/server/save/economy/official army claim;
- UI, Demo, QA and Server impacts are present;
- BEE-841 remains blocked.

## Execution Strategy

The wave should not be executed as one large uncontrolled runtime change.

First runtime tranche:

- BEE-821: playable Hive product intake;
- BEE-822: real-device / robust touch proof protocol;
- BEE-823: phone portrait polish;
- BEE-824: tablet landscape polish;
- BEE-825: rapid tap upgrade automation;
- BEE-826: rapid tap training automation;
- BEE-827: deterministic checks for cost, queue, troops and level.

This tranche keeps the work focused on the remaining QA reserves from DEMO-066.

Second tranche after Demo/QA:

- BEE-828 through BEE-835: buttons, disabled states, resource feedback, upgrade/training clarity, panel polish and gesture automation.

Server can progress independently on non-live catalog implementation:

- SERVER-035: code-first non-live Hive Loop catalogs and unit tests.

## Dispatch

- Builder-A: implement BEE-821 through BEE-827 only.
- Server-A: start SERVER-035 non-live catalog implementation.
- Demo-A: hold until Builder-A returns `READY_FOR_DEMO_067`.
- QA-A: hold until Demo-A returns `READY_FOR_QA_067`.
- Builder-B and Builder-C: hold; their support docs are available for Builder-A.
- UI-A: hold; UI-065 is available for Builder-A.

## Quality Gate

Builder-A must return:

- runtime report;
- compile status;
- test status;
- proof bundle source for DEMO-067;
- clear non-claims.

No BEE-828 runtime work should begin before DEMO-067 / QA validation of BEE-821 through BEE-827.

