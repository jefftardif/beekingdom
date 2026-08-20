# AI Engineering Mission Index

| Mission | Owner | Purpose | Status |
|---|---|---|---|
| M001 | CL | Runtime baseline establishment | CLOSED |
| M002 | OC | Restore production persistence guardrail | CLOSED |
| M003 | CL | LivingHive → HiveMap migration inventory | CLOSED |
| M004 | OC | HiveMap migration provenance analysis | CLOSED |
| M005 | CX | HiveMap decoupling strategy | CLOSED |
| M006 | CL | HiveMap feature implementation wave | IN PROGRESS |
| M007 | OC | AI mission history consolidation | CLOSED |

---

## Report Locations

All reports are in `Docs/AI/Missions/`:

- `M001-CL-Runtime-Baseline.md` — PARTIALLY RECONSTRUCTED
- `M002-OC-Production-Guardrails.md` — From actual execution (commit `7b59f47`)
- `M003-CL-LivingHive-Migration-Inventory.md` — PARTIALLY RECONSTRUCTED
- `M004-OC-HiveMap-Migration-Provenance.md` — From actual execution
- `M005-CX-HiveMap-Decoupling-Strategy.md` — Preserved from CX
- `M006-CL-...` — In progress (CL)
- `M007-OC-AI-Mission-History-Consolidation.md` — This consolidation

---

## Key Findings Summary

**M002:** Restored `Persistence.Provider = "InMemory"` in `appsettings.Production.json` (commit `7b59f47`). Two production guardrail tests + full suite (385 passed) now pass.

**M004:** HiveMap bootstraps are thin adapters to monolith `ForExternalHost` methods. Only `LivingHiveMenuCanvas` and `LivingHiveResearchWindow` are full uGUI ports. **PROTECT HIVEMAP** for 11 features including Barrack (server-authoritative `79f6660`), Research, Navigation Rail, zoom-derived badges.

**M005:** LivingHive.unity (scene) can be retired before HiveViewProductUiPresenter.cs (code). Strangler pattern recommended.

**Rule Established:** If a feature was ported to HiveMap and modified afterward, the HiveMap version is authoritative and must never be overwritten by LivingHive.