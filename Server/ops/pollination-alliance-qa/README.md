# BeeKingdom P4 — SQL local QA for collective floral projects

**NEVER run against SRVESDT or any production database.** This test writes and cleans synthetic alliances, hives, missions, and ecological aggregates in its configured SQL database.

## Local prerequisites

SQL Server LocalDB 2025 named BKPollinationQA with database BeeKingdomPollinationQA, and these additive scripts applied (in order):

- Server/src/BeeKingdom.Database/Scripts/070_hive_operations.sql
- Server/src/BeeKingdom.Database/Scripts/090_alliance_platform.sql
- Server/src/BeeKingdom.Database/Scripts/096_floral_site_states.sql
- Server/src/BeeKingdom.Database/Scripts/097_floral_civil_contributions.sql
- Server/src/BeeKingdom.Database/Scripts/098_floral_alliance_projects.sql

Run from the repository root:

    dotnet run --project Server/ops/pollination-alliance-qa/PollinationAllianceQa.csproj

The default connection is integrated Windows authentication to the local QA database only. Override BEEKINGDOM_POLLINATION_QA_SQL solely for another **disposable** test database. Never store credentials or run against production.

## Tested transitions

1. A leader creates a project. Idempotency replay returns that same project.
2. Ordinary member creation is forbidden; outsider project reads are forbidden.
3. The alliance of a civilian expedition is captured at departure.
4. A single player can exceed the project goal but cannot fulfill the requirement for two distinct qualified members.
5. The second member delivers real limited pollen and causes exactly one project completion and level gain.
6. A duplicate SQL mission delivery aborts the whole transaction and leaves the site, hive, civilians and project unchanged.
7. Retried or reopened sessions cannot duplicate points or XP.
8. Changing alliance mid-flight cannot award the new alliance's project with old pollen, though the personal harvest settles normally.
9. All newly generated QA rows are deleted at the end.

This is backend SQL proof, not a Unity visual test or live-API deployment test. All feature flags remain OFF by default.