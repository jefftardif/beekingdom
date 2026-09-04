# M051C-CL — Alliance Research Player-Facing UI + Localization Fix

Fixes Stage 1's raw-technical-ID presentation failure. No production
donation, no SQL migration, no gameplay/backend behavior change.

## 1. Root cause, proven

The card already called `BeeLocalization.Text(tech.DisplayNameKey,
tech.TechnologyId)` since M051 - the architecture was always correct
(stable `TechnologyId` + a separate presentation key). The bug: **the
localization keys were never registered in
`Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`**.
`BeeLocalization.Text(key, fallback)` returns `fallback` whenever `key`
isn't found in any locale's catalog - and the fallback I passed in M051 was
`tech.TechnologyId` itself, so every lookup silently missed and rendered
the raw ID.

Confirmed by comparing against the exact same, already-working convention
personal Research already uses for its own techs
(`research.<id>.title`/`.summary`/`.result`, all present in the same JSON
file) - Alliance Research's `alliance.research.<id>.name`/`.desc` simply
had no matching entries yet. Fixed by adding them, not by reformatting the
ID client-side (`Replace("_"," ")` or similar was never used and is
explicitly what the mission forbade).

## 2. Localization entries added

`strings.fr-CA.json` gained (all under the existing catalog mechanism, no
second localization system):

- 9 × `alliance.research.<id>.name` (the real French names from this
  mission's own list, e.g. `prosperity_shared_reserves_i` → "Réserves
  partagées I").
- 9 × `alliance.research.<id>.desc` - one concise "why donate" sentence
  per technology, in the branch's own theme (production / storage / troop
  power).
- 3 × `alliance.research.branch.*` (PROSPÉRITÉ / COOPÉRATION / DÉFENSE
  ROYALE).
- 4 × `alliance.research.state.*` (DISPONIBLE / VERROUILLÉE / EN
  PROGRESSION / TERMINÉE).
- 1 × `alliance.research.locked.requires` = `"Requiert : {0}"`.
- 3 × `alliance.research.bonus.*_percent` = `"+{0} % <Production de
  ressources|Capacité de stockage|Puissance des troupes>"` - a template,
  not a baked-in number (see section 3).

`TechnologyId` itself is untouched everywhere - still the sole stable
identity used by the donate call, idempotency keys, and prerequisite
matching.

## 3. Real effect values - minimal read-contract extension

The old contract only carried a generic `BonusSummaryKey` (a bonus
*category*, no magnitude) - there was nowhere for the UI to read an actual
number, so any effect text would have had to be hardcoded and could drift
from the real catalog. Extended (read-only, additive, no gameplay change):

- Server: `AllianceTechnologyReadModel` gained `ProductionBp`, `CapacityBp`,
  `CombatPowerBp` - populated directly from
  `AllianceResearchCatalog.TechnologyDefinition`'s own existing fields
  (`AllianceResearchService.BuildSnapshot`, one line changed).
- Client: `RemoteAllianceTechnology` and `AllianceTechnologyRowModel`
  mirror the same three fields.
- UI: `AllianceTechnologyEffectText` picks whichever of the three is
  non-zero (exactly one per Alpha technology) and formats `"+{0} %"` via
  `string.Format` against the localized template, with the number itself
  always `bp / 100` read live from the server - never a client constant.

## 4. Branch grouping

`DrawAllianceResearchTab` now inserts a branch header
(`alliance.research.branch.<branch>`) whenever the server's own
`Technologies` list transitions to a new `Branch` value - the server
already returns the catalog in stable declaration order (Prospérité →
Coopération → Défense Royale), so this needed no new ordering data, just a
client-side "branch changed since the last row" check. No graph/tree
widget - a grouped scrollable list, per the mission's own explicit scope
limit.

## 5. Card redesign

Each card now shows, top to bottom: name + state badge, description,
`EFFET  +X % <category>`, progress bar + `current / required`, then one of
three mutually-exclusive bottom rows:

- **Locked**: `VERROUILLÉE` badge + `Requiert : <real prerequisite
  name(s)>` (resolved from the same snapshot's own rows - never a second
  catalog lookup) - **no Donate button drawn at all**, not merely disabled.
- **Available / En progression**: donation cost + a real `DONNER` button,
  enabled only when `Available` and no other donation is in flight.
- **Completed**: `TERMINÉE` badge only - no cost, no button; the `EFFET`
  line above already communicates the bonus the Alliance now owns.

State label is derived purely from already-authoritative fields already in
the contract (`Locked`/`Completed`/`CurrentProgress > 0`) - no new server
state, no semantic change, exactly the mission's own allowance ("this can
be presentation derived from authoritative progress").

Contribution header and donation cost presentation/values: unchanged from
M051, per the mission's explicit "preserve" instructions.

## 6. What was NOT touched

`RequiredProgress`, donation progress-per-donation, donation resource
costs, prerequisites, completion logic, bonus magnitudes, contribution
math, idempotency, persistence, SQL, the `AllianceResearch` feature flag,
Alliance Help/Chat/membership/roles/Activity, Player Profile, the
Research/Construction pulses, personal Research, WorldMap, FTUE. No SQL
migration was needed or created - this was a read-contract field addition
(new columns in a DTO, not a schema change) plus localization/presentation
work only.

## 7. Live production state

Not touched. No donation was made; `Donate` was never called this
mission. Alliance Test `[BKT]` research remains exactly as M051B left it
(0 progress, 0 contributions) - confirmed unaffected since nothing in this
mission's diff touches `AllianceResearchService.DonateAsync`,
`AllianceResearchBonusResolver`, or any repository write path.

## 8. Compile and tests

Server: `dotnet build` - 0 errors. `dotnet test --filter
AllianceResearchServiceTests` - **14/14 green** (13 from M051 + 1 new:
`GetSnapshot_ExposesRealCatalogBonusMagnitudes_NotJustAGenericSummaryKey`,
proving the read contract now carries the real
`ProductionBp`/`CapacityBp`/`CombatPowerBp` values, item 5 of the required
test list).

Unity: compile verified clean via `assets-refresh`. Editor was confirmed
out of Play Mode this time (`Application.isPlaying=false`, read-only
probe) - ran the focused suite: **`AllianceResearchClientTests`, 4/4
green** (2 existing M051 tests unaffected + 1 updated GET-snapshot
assertion + 1 new round-trip test for the bonus magnitude fields, item 5's
client-side half). No broad suite was run, per instruction.

Items 1-4, 6-13 from the mission's list (name≠id, all 9 names/descriptions
present, branch grouping, locked-hides-Donate, prerequisite shown,
state transitions, contribution header, costs unchanged) are UI-rendering
behavior inside `HiveViewProductUiPresenter.cs`/`AllianceCenterPresentation.cs`
(default `Assembly-CSharp`, no `.asmdef`) - the same standing
cross-assembly constraint documented in every prior Alliance Center
mission this session (`BeeKingdom.Tests.asmdef` cannot reach these types
directly). They are proven by direct source inspection above (sections
1-5) and are exactly what your Stage 1 visual retest will confirm live.
Item 14 (no mutation during refresh) is provable by inspection: `RefreshResearch()`
only ever calls the `GET` endpoint (`AllianceClient.GetAllianceResearchAsync`)
- `DonateToResearch`/`DonateAsync` were not touched or called this mission.

---

## Final checklist

| # | Question | Answer |
|---|---|---|
| A | Exact source of raw-ID rendering proven? | YES — missing `strings.fr-CA.json` catalog entries, not an architecture defect |
| B | Technical TechnologyIds preserved unchanged? | YES |
| C | All 9 player-facing names implemented? | YES |
| D | All 9 descriptions implemented? | YES |
| E | Real effect values displayed? | YES — read contract extended with real bp magnitudes, formatted client-side from server truth |
| F | Three branches visibly grouped? | YES |
| G | Locked technologies hide/disable Donate appropriately? | YES — no button drawn at all, not just disabled |
| H | Real prerequisite shown for locked technologies? | YES — resolved from the same snapshot's own rows |
| I | Available/In Progress/Completed states presented correctly? | YES |
| J | Contribution header preserved? | YES — unchanged |
| K | Donation costs unchanged? | YES |
| L | Backend gameplay unchanged? | YES — only a read-only DTO field addition |
| M | No SQL migration? | YES — none needed, none created |
| N | Alliance Test production state untouched? | YES — `Donate` never called this mission |
| O | Unity compile green? | YES |
| P | Focused tests green? | YES — 14/14 server, 4/4 Unity |
| Q | READY FOR CEO VISUAL RETEST? | YES |

READY FOR CEO STAGE 1 RETEST — OPEN ALLIANCE CENTER → RECHERCHES. DO NOT DONATE.
