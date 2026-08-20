# M014-OC PLACEHOLDER CAPABILITY VERIFICATION RESULT

## Executive Conclusion

**No placeholder/status window has incorrectly discarded useful LivingHive functionality.**

All five buildings (Infirmary, Genetics, Academy, Defense, Bank) were already marked **"future"** in LivingHive with no building-specific detail panels, no server-backed controllers, and no actionable gameplay beyond a "Voir X" button with "Fonctionnalite a venir." The HiveMap status windows honestly reflect this reality.

**The Royal Palace error does not apply here.** Royal Palace was unique: it had an `active` catalog state, a `DrawAdministrationCoreDetail` panel with level display, upgrade button, colony overview, and server-supported upgrade (`administration_core`). None of the five audited buildings had equivalent functionality.

---

## 1. Infirmary

### LivingHive Capability
| Aspect | Finding |
|--------|---------|
| Catalog state | `future` |
| Hotspot action | `Voir soins` (See care) |
| Disclosure | `Fonctionnalite a venir` (Feature coming soon) |
| Detail panel | **None** — no `DrawInfirmaryDetail` or equivalent |
| Bee agents | Garden scout placed at hotspot (visual only) |

### State/Controller Source
- **Client:** Only `ReferenceHiveHotspot` entry with `future` state
- **Server:** **No Infirmary building controller exists**
  - `BroodVitalityCare` (healing) exists but is **Brood care**, not Infirmary building-specific
  - SpeedUp category `healing` targets `BroodVitalityCare` operations, not Infirmary building
  - No Infirmary-specific upgrade contracts beyond `infirmary_grove` in building list

### Classification
**FUTURE ONLY** — Code/documentation exists, but the feature is intentionally not current gameplay. The "healing" system (`BroodVitalityCare`) is a separate Brood care system, not an Infirmary building capability.

### Current HiveMap Behavior
`HiveMapUnsupportedBuildingBootstrap` shows honest status:
> "Les soins officiels ne sont pas encore exposes par un controleur Infirmary. Les soigneuses restent donc en attente de fonctionnalite."

Preserves upgrade path via Construction (one tap deeper).

### Verdict: **PLACEHOLDER JUSTIFIED**

No useful Infirmary-specific capability was discarded. The "healing" system (`BroodVitalityCare`) is a separate Brood care system, not an Infirmary building capability. No server-backed Infirmary controller exists.

---

## 2. Genetics

### LivingHive Capability
| Aspect | Finding |
|--------|---------|
| Catalog state | `future` |
| Hotspot action | `Etudier genetique` (Study genetics) |
| Disclosure | `Fonctionnalite a venir` |
| Detail panel | **None** — no `DrawGeneticsDetail` or equivalent |
| Bee agents | Drone placed at hotspot (visual only) |

### State/Controller Source
- **Client:** Only `ReferenceHiveHotspot` entry with `future` state; bee genetics system (`GeneticsId` on bees) exists but is **not building-specific**
- **Server:** **No Genetics building controller exists**
  - Bee genetics system (`GeneticsId` on `BeeLifecycleBee`) exists but is a **bee property**, not a building capability
  - `ResearchGeneticsActivationBoundary` / `ResearchGeneticsPreviewChoice` are **Research-domain**, not Genetics building
  - No Genetics building upgrade contracts beyond `genetics_garden` in building list
  - No Genetics-specific server controller or client

### Classification
**FUTURE ONLY** — Bee genetics system exists but is a bee-level property, not a Genetics building capability. Research/Genetics boundary frameworks exist but are Research-domain, not a Genetics building controller.

### Current HiveMap Behavior
`HiveMapUnsupportedBuildingBootstrap` shows honest status:
> "La genetique officielle reste une capacite future : les choix de mutation/progression ne sont pas encore server-backed."

### Verdict: **PLACEHOLDER JUSTIFIED**

No Genetics building-specific capability was discarded. Bee genetics is a bee property; Research/Genetics activation boundaries are Research-domain frameworks. No server-backed Genetics building controller exists.

---

## 3. Academy

### LivingHive Capability
| Aspect | Finding |
|--------|---------|
| Catalog state | `future` |
| Hotspot action | `Voir academie` |
| Disclosure | `Fonctionnalite a venir` |
| Detail panel | **None** — no `DrawAcademyDetail` or equivalent |
| Bee agents | None specific to Academy |

### State/Controller Source
- **Client:** Only `ReferenceHiveHotspot` entry with `future` state
- **Server:** **No Academy building controller exists**
  - Research system exists (`ResearchNode`, `HiveResearchClient`, `LivingHiveResearchWindow`) but is **separate** — ResearchNode is its own building
  - No Academy-specific training/formation controller
  - No Academy upgrade contracts beyond `academy_canopy` in building list
  - M009/M013 explicitly notes: "La Recherche officielle reste portee par son propre noeud et sa fenetre HiveMap dediee; aucune formation Academie separee n'est server-backed aujourd'hui"

### Classification
**FUTURE ONLY** — Research is a separate building/system (ResearchNode). No Academy-specific server controller exists.

### Current HiveMap Behavior
`HiveMapUnsupportedBuildingBootstrap` shows honest status:
> "L'Academie est presente comme batiment futur. La Recherche officielle reste portee par son propre noeud et sa fenetre HiveMap dediee; aucune formation Academie separee n'est server-backed aujourd'hui."

### Verdict: **PLACEHOLDER JUSTIFIED**

Research is a separate building (ResearchNode) with its own HiveMap window (`LivingHiveResearchWindow`). No Academy-specific server controller exists. The status message correctly clarifies the relationship.

---

## 4. Defense

### LivingHive Capability
| Aspect | Finding |
|--------|---------|
| Catalog state | `future` |
| Hotspot action | `Voir defense` |
| Disclosure | `Fonctionnalite a venir` |
| Detail panel | **None** — no `DrawDefenseDetail` or equivalent |
| Bee agents | Guard bee placed at hotspot (visual only) |

### State/Controller Source
- **Client:** Only `ReferenceHiveHotspot` entry with `future` state; Guard bee visual at hotspot
- **Server:** **No Defense building controller exists**
  - Combat systems exist (`CombatRecruitmentService`, `CombatPatrolService`, `CombatSquadReservationService`, `HivePerimeterSortieService`) but are **systems**, not a Defense building controller
  - `CombatRecruitmentService` uses Nursery level for capacity, not Defense building
  - `HivePerimeterSortie` is a system, not a Defense building action
  - No Defense building upgrade contracts beyond `defense_growth` in building list
  - M013/M009 status: "Les systemes combat/perimetre existants vivent dans les parcours Armee et serveur, mais ne sont pas encore une action officielle de ce batiment"

### Classification
**CURRENT CLIENT FEATURE (Systems) / FUTURE ONLY (Building)** — Combat/perimeter systems exist and are current gameplay, but they are **systems**, not a Defense building controller. The Defense building itself has no server-backed controller.

### Current HiveMap Behavior
`HiveMapUnsupportedBuildingBootstrap` shows honest status:
> "La Defense reste une zone future. Les systemes combat/perimetre existants vivent dans les parcours Armee et serveur, mais ne sont pas encore une action officielle de ce batiment."

### Verdict: **PLACEHOLDER JUSTIFIED**

Combat/perimeter systems exist and are current gameplay, but they are **not owned by the Defense building**. No Defense building controller exists. The status message correctly distinguishes systems from building.

---

## 5. Bank

### LivingHive Capability
| Aspect | Finding |
|--------|---------|
| Catalog state | `future` |
| Hotspot action | `Voir banque` |
| Disclosure | `Fonctionnalite a venir` |
| Detail panel | **None** — no `DrawBankDetail` or equivalent |
| Bee agents | None specific to Bank |

### State/Controller Source
- **Client:** Only `ReferenceHiveHotspot` entry with `future` state
- **Server:** **No Bank building controller exists**
  - `HiveStockSnapshotClient` (stock) — separate panel
  - `HiveRewardLedgerClient` (rewards) — separate panel
  - `HiveVipClient` (VIP) — separate client
  - `StrategicPathClient` — separate system
  - M013: "Server/client inspection found stock, reward ledger, VIP and other account-backed clients, but no bank-owned gameplay controller and no server-backed Bank action"
  - No Bank-specific upgrade contracts beyond `hive_bank` in building list

### Classification
**SAFE LEGACY CODE (Systems) / FUTURE ONLY (Building)** — Stock/Reward/VIP/StrategicPath systems exist and are current, but they are **separate systems/panels**, not a Bank building controller.

### Current HiveMap Behavior
`HiveMapUnsupportedBuildingBootstrap` shows honest status:
> "La Banque est presente comme batiment futur. Les stocks, recompenses et ressources officielles restent portes par leurs panneaux et clients dedies; aucune action bancaire separee n'est server-backed aujourd'hui."

### Verdict: **PLACEHOLDER JUSTIFIED**

Stock/Reward/VIP/StrategicPath are separate systems with their own panels/clients. No Bank building controller exists. M013 explicitly verified this.

---

## Capability Recovery Candidates

| Building | Missing Capability | Classification | Recovery Priority |
|----------|-------------------|----------------|-------------------|
| **None** | — | — | — |

**No genuine recovery candidates found.** All five buildings were already `future` in LivingHive with no detail panels, no server-backed controllers, and no actionable gameplay beyond "Voir X" buttons.

---

## Correctly Deferred Features

| Feature | Building | Why Correctly Deferred |
|---------|----------|------------------------|
| Brood care / healing | Infirmary | Separate `BroodVitalityCare` system (Brood domain), not Infirmary building |
| Bee genetics | Genetics | Bee property (`GeneticsId`), not building capability |
| Research | Academy | Separate `ResearchNode` building with own window |
| Combat/perimeter systems | Defense | Exist as systems (Army/Server), not Defense building |
| Stock/Reward/VIP/StrategicPath | Bank | Separate systems with own panels/clients |

---

## Royal Palace Lesson Applied

| Royal Palace (Mistake) | These 5 Buildings (Verified Correct) |
|------------------------|--------------------------------------|
| `active` catalog state | All 5: `future` catalog state |
| `DrawAdministrationCoreDetail` panel with level, upgrade, colony overview | **No detail panels exist** for any of the 5 |
| `administration_core` upgrade supported by `HiveBuildingUpgradeClient` | Only `infirmary_grove`, `genetics_garden`, `academy_canopy`, `defense_growth`, `hive_bank` in building list — no building-specific upgrade contracts |
| `active` catalog state, `active` role disclosure | All 5: `future` catalog state, `future` disclosure |
| Rich IMGUI detail block (`DrawAdministrationCoreDetail`) | **No detail methods exist** (`DrawInfirmaryDetail`, `DrawGeneticsDetail`, etc. do not exist) |

**Key distinction:** Royal Palace was the **only** building with an `active` state, a detail panel, and a server-supported upgrade. All five audited buildings were uniformly `future` with no detail panels and no server-backed controllers.

---

## Recommended Corrective Missions

**None required.** All five placeholders are correctly justified.

If product later decides to activate any building:
- Infirmary → Would need BroodVitalityCare integration or new Infirmary controller
- Genetics → Would need Genetics building controller (separate from bee genetics)
- Academy → Would need Academy training controller (separate from Research)
- Defense → Would need Defense building controller owning combat systems
- Bank → Would need Bank controller owning stock/reward/VIP entry points

Each would be a product/API decision, not a recovery of lost functionality.

---

## Confidence

**HIGH** — Exhaustive code search across client, server, and monolith found:
- Zero detail panel methods for any of the 5 buildings
- Zero server-backed building controllers for any of the 5
- All 5 uniformly `future` in catalog with only "Voir X" + "Fonctionnalite a venir"
- Server systems exist but are **separate domains**, not building-owned

---

*Report saved to: `Docs/AI/Missions/M014-OC-Placeholder-Capability-Verification.md`*