# M021 REAL MARCH COMPOSITION + CHAMPION + RETURN STATE RESULT

## Existing March Architecture

All rendering lives in `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs`,
inside `DrawCombatPatrolMarch()` (own-player only; no shared/synchronized world state exists yet
for other players' marches). It is pure IMGUI (`OnGUI`, `GUI.DrawTexture`/`DrawCircle`/`DrawBezier`)
redrawn every frame — there are no GameObjects, no `Instantiate`/`Destroy`, and therefore no pooling
concern to begin with; every "unit" is just a draw call computed from current state.

Before this mission, the method drew exactly **one** generic bee sprite (`DrawCombatMarchBee`) per
active encounter and per return trip, regardless of how many troops were actually committed. The
outbound path already tracked one `Vector2` map target per `EncounterId`
(`combatPatrolTargetWorldCoordByEncounterId`) and a separate return-trip animation
(`combatPatrolReturnTrips`, keyed by `EncounterId`) that starts when an encounter silently
disappears from `ActiveEncounters` (auto-claimed elsewhere). The route curve, its Bezier control
point, and the halo/core/filament/spark rendering (`DrawStyledMarchPath`, `MarchPalette`,
`CombatMarchPalette`/`RaidMarchPalette`) were left completely untouched by this mission — only what
gets drawn *at* the marker position changed.

## Composition Source

The real committed composition comes straight from the server-authoritative encounter data already
being read for the patrol panel: `RemoteCombatPatrolActiveEncounter.CommittedTroops`
(`Dictionary<string, long>`, keyed by `"guardians"|"wingrunners"|"darters"`) — never from the
composer's UI draft values (`Model.DraftGuardians` etc.), which only exist before a march is
launched. This is the same field the existing patrol panel already lists per family, so no new
server endpoint or client model was needed.

## Proportional Rendering Algorithm

`ComputeMarchVisualSample(IReadOnlyDictionary<string,long> composition)` (new, static, no
allocations beyond the returned list) converts a real composition into a bounded, proportional
`List<(string Family, int Count)>`:

1. Sum the three families' committed counts; bail out (empty sample) if the total is zero or the
   input is null.
2. Pick a cap via `MarchVisualCapForTotal(total)`: `<=8` total troops shows the exact count (1-5),
   `<=60` caps at 6, `<=400` caps at 9, larger armies cap at 13 (stays inside the 8-15 sprite budget
   given in the brief, biased toward the readable end since 3 families can already crowd a small
   formation).
3. Every family with `count > 0` is guaranteed at least 1 sprite (reserved up front) — this is an
   unconditional rule, verified with a `1 guardian / 1000 wingrunners` case (see Validation) which
   still shows the lone guardian instead of being rounded away.
4. The remaining cap budget is distributed proportionally to composition share using the largest-
   remainder apportionment method (floor each family's proportional share, then hand out leftover
   slots to the families with the largest fractional remainder) — this is what keeps ratios
   meaningful instead of naive rounding.

Verified in a live Editor call (`script-execute`, not just static review):

| Input | Output | Notes |
|---|---|---|
| 100 guardians / 50 wingrunners | 6 / 3 (total 9) | 2:1 ratio preserved |
| 1000 / 1000 | 7 / 6 (total 13) | ~1:1 preserved, capped for a large army |
| 500 / 250 / 250 | 6 / 4 / 3 (total 13) | ~2:1:1 preserved |
| 3 guardians only | 3 (total 3) | small squad shows exact count, no padding |
| 1 guardian / 1000 wingrunners | 1 / 12 (total 13) | rare family never disappears |

The exact cap numbers differ from the brief's own worked examples (which all land on 12) — the brief
explicitly allows this ("exact algorithm may differ, but proportions must remain visually
meaningful"). The computed sample is cached per `EncounterId` in
`combatPatrolOutboundVisualSampleCache` (invalidated when the encounter disappears) so it is
computed once per march, not once per frame.

## Formation Layout

`MarchFormationOffset(index, total, baseSpread)` places each visible unit using a phyllotaxis
("sunflower") layout — golden-angle rotation (`137.50776°`) times `sqrt((index+0.5)/total)` for
radius — which produces an organic, compact cluster rather than a line or a grid, and is fully
deterministic per index (no per-frame jitter/flicker). The vertical component is flattened
(`×0.55`) to read as a loose swarm on the map's near-top-down perspective. `DrawMarchFormation`
adds the offset to the existing Bezier marker position — **the underlying path calculation, march
speed, and Bezier control point were not touched.** Each unit also gets its own wing-flap phase
(`index * 1.7`) instead of sharing one synchronized flap, per the brief's "different wing animation
phase" requirement.

## Guardian Integration

Guardians reuse the existing, unmodified `CombatMarchBeeBody`/`CombatMarchBeeWings` sprite pair with
no tint (`MarchFamilyTint("guardians") == Color.white`) — visually identical to the pre-mission
single-bee march, just now potentially repeated several times in formation. This satisfies "Guardian
representation may be fully visual."

## Missing Troop Assets

Confirmed by direct search of every `Resources/` folder in the project: **no per-family sprite
exists** for Wingrunners/Voltigeuses or Darters/Lanceuses (nor a generic non-Guardian troop sprite).
The only combat-march sprite pair in the project is the Guardian one listed above. Per the brief's
explicit instruction ("do NOT invent temporary unrelated sprites... implement the renderer
architecture so missing troop visuals can be plugged in later"), the architecture is built so a real
per-family sprite just needs to be dropped in:

- `MarchFamilyTint(family)` is the single seam to touch — swap the tint-based differentiation for a
  distinct texture lookup (e.g. `CombatMarchBeeBodyResource` → a `{family}` resource path convention)
  once real Wingrunner/Darter art exists, without touching the formation/sampling/return logic at
  all.
- Until then, all three families render the same Guardian body/wing sprite pair, differentiated only
  by a subtle tint (light blue for Wingrunners, light green for Darters) so a mixed army doesn't
  read as an undifferentiated blob while real art is pending.

**Action item for Jeff / art:** commission or generate Wingrunner and Darter march sprites (body +
wings, matching the existing Guardian pair's style/size) and drop them into
`Resources/WorldMapWave6Runtime/CombatMarch/`.

## Champion Data Source

`ChampionBeeProgressState.AssignedBeeIds` (server, roster-wide — `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`)
and its client mirror `RemoteChampionBeeSnapshot.AssignedBeeIds`
(`Assets/BeeKingdom/Networking/HiveChampionBeeClient.cs`) are the only real champion-assignment data
in the project. **This assignment is global to the hive, not tied to any specific march/encounter —
confirmed by inspecting both `CombatPatrolActiveEncounter` (server) and its client mirror
`RemoteCombatPatrolActiveEncounter`: neither has a champion field.** The only per-encounter champion
link that exists anywhere is `CombatPatrolClaimReceipt.ContributingChampionBeeIds`, but that is only
populated *after* a claim resolves combat — never available during the outbound march or for a
return trip whose receipt was not retained (see "Combat Result Data Available" below).

`ChampionBeeCatalog` (`Assets/BeeKingdom/Playground/ChampionBeeCatalog.cs`, client-side mirror of
the server catalog) resolves a bee id to its `ChampionBeeRole` (`Guardians|Wingrunners|Darters|
Civilian`). Real portrait art already exists and is Resources-loadable:
`Resources/PremiumBeeReference/ChampionBees/{striga,zephyra,ambra,nectaria,aurelia}.png` (already
used by the Champion Hall screen via the same `Resources.Load` path convention).

## Champion March Integration

`HiveViewProductUiPresenter.PeekAssignedChampionBeeIdsForWorldMap()` (new, one-line accessor) simply
exposes the presenter's existing `officialAssignedChampionBeeIds` cache — the same data already
loaded once per session for the Champion Hall screen, so **no new network call was added**.
`ResolveMarchLeaderChampionId()` (WorldMap file) walks that list and returns the first assigned bee
whose `Role != Civilian` (Nectaria/Aurelia are production-only champions and must never appear
leading a military march). This is resolved once per `OnGUI` frame and applied identically to every
currently visible march, both outbound and return.

This is the "smallest clean propagation" allowed by the brief given the real constraint above: since
champion assignment has no per-march server link at all, showing the actual currently-assigned
combat champion (real data, already contributing to this exact combat's power calculation via
`ChampionCombatContribution`) is the closest honest representation without inventing a new
champion-to-march linking system, which the brief explicitly forbids building in this mission.
**Known limitation, documented rather than hidden:** if two marches are active simultaneously with a
combat-relevant champion assigned, both currently show the same champion leading — because,
server-side, that is literally true today (the same assignment contributes to every concurrent
combat's power calculation). A real "this champion escorted this specific march" concept would
require a genuine server schema addition (a champion field on `CombatPatrolActiveEncounter`,
populated at launch time) — explicitly out of scope per the brief ("do NOT create a new
Champion-selection system in this mission").

## Combat Result Data Available

`CombatPatrolClaimReceipt` (server) / `RemoteCombatPatrolClaimReceipt` (client) already expose
`PermanentLosses` and `WoundedLosses` (both `Dictionary<string,long>` per family) on every claim —
but only the **manual** claim path (`CombatPatrolPresentation.ClaimCoreAsync`) was actually keeping
that receipt (as `Model.Debrief`). The **automatic** claim path that drives the WorldMap's "troops
come home on their own" flow (`AutoClaimFinishedEncountersAsync`, which is what fires for the vast
majority of marches since the player is rarely staring at the patrol panel exactly when a timer
hits zero) discarded `response.ClaimReceipt` entirely.

**Fix applied (client-only, no combat formula touched):** `CombatPatrolPanelController` now keeps a
small bounded cache (`recentClaimReceipts`, capped at 16 entries, FIFO eviction) of the last claim
receipts by `EncounterId`, populated from **both** the manual and automatic claim paths via a new
`RememberClaimReceipt` helper. This does not change what the server computes or returns — it only
stops the client from throwing away data the server already sent back on every single claim.
`HiveViewProductUiPresenter.TryGetCombatPatrolClaimReceiptForWorldMap(encounterId, out receipt)`
exposes it to the WorldMap renderer.

## Return Composition

When a return trip starts (`DrawCombatPatrolMarch`, the loop over `lastKnownCombatPatrolEncounterIds`
that detects a disappeared encounter), the WorldMap now:

1. Looks up the pre-battle composition it already tracks per `EncounterId`
   (`combatPatrolCommittedTroopsByEncounterId`, populated every frame an encounter is seen active).
2. Calls `TryGetCombatPatrolClaimReceiptForWorldMap(encounterId, ...)`. If a receipt was retained
   (now the common case — see above), survivors are computed as
   `max(0, committed[family] - PermanentLosses[family])` per family. **Wounded troops are counted as
   returning** — they physically come home and only then enter recovery
   (`CombatPatrolResolution.Recovering`/`ComputeRecoveryDuration`), so excluding them from the return
   visual would misrepresent the game's own model. Only permanent losses are subtracted.
3. If no receipt is available (edge case — e.g. a receipt evicted from the 16-entry cache by a burst
   of other claims before the return animation started), it falls back to the raw pre-battle
   composition rather than fabricating a loss number.
4. The resulting composition is run through the same `ComputeMarchVisualSample` used for the
   outbound march and stored once inside the `CombatPatrolReturnTrip` struct (`VisualSample`,
   `LeaderChampionId`) — computed once per return trip, not per frame.

A family with zero survivors is simply absent from `ComputeMarchVisualSample`'s output (the "must
disappear" rule from the brief) — no extra code needed, it falls out of the same sampling algorithm
used outbound.

## Wounded/Survivor Semantics

No new "wounded" visual state was added — per the brief, that is explicitly optional/out of scope
unless trivial and model-backed, and there is no existing visual language in this codebase for an
injured bee sprite. The minimum requirement (return formation size/composition reflects
surviving/returning troops, wounded included since they do physically return) is met as described
above. No blood/gore, no new animation states.

## Performance

The renderer was already zero-GameObject IMGUI before this mission (no pooling architecture existed
because none was needed). The two allocation-risk points introduced by this mission are both cached
rather than recomputed per frame:

- Outbound composition sampling: cached per `EncounterId` in `combatPatrolOutboundVisualSampleCache`,
  computed once, purged alongside the existing `combatPatrolTargetWorldCoordByEncounterId`/
  `combatPatrolCommittedTroopsByEncounterId` cleanup (stale-encounter loop and finished-return-trip
  loop, both already existed for the coordinate map).
- Return composition sampling: computed once at return-trip creation, stored directly in the
  (now slightly larger) `CombatPatrolReturnTrip` readonly struct — no dictionary lookup needed while
  the trip animates.

`ComputeMarchVisualSample` itself allocates a small `List`/two small `Dictionary`s bounded to at most
3 entries (one per combat family) — trivial, and only runs once per march/return-trip, not per frame,
per formation-member, or per OnGUI call. No LINQ was used anywhere in the new code specifically to
avoid per-call closures/enumerator allocations in a method that could in theory run every frame
(`ComputeMarchVisualSample` avoids it on principle even though it's cached, since a future caller
might not cache it).

## Files Changed

- `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs` — all rendering work:
  new fields (`combatPatrolCommittedTroopsByEncounterId`, `combatPatrolOutboundVisualSampleCache`),
  `CombatPatrolReturnTrip` struct extended with `VisualSample`/`LeaderChampionId`,
  `DrawCombatPatrolMarch` updated to track composition and drive formations, new methods
  `ComputeMarchVisualSample`, `MarchVisualCapForTotal`, `MarchFamilyTint`, `MarchFormationOffset`,
  `ResolveMarchLeaderChampionId`, `DrawChampionMarchUnit`, `DrawMarchFormation`,
  `DrawTroopMarchUnit` (generalizes the old `DrawCombatMarchBee`, kept as a thin fallback wrapper).
- `Assets/BeeKingdom/Playground/CombatPatrolPresentation.cs` — `ICombatPatrolPanelController` gains
  `TryGetRecentClaimReceipt`; `CombatPatrolPanelController` gains the bounded receipt cache and
  `RememberClaimReceipt`, called from both `ClaimCoreAsync` and `AutoClaimFinishedEncountersAsync`;
  `UnavailableCombatPatrolPanelController` gets a no-op stub.
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` — two new internal accessors:
  `PeekAssignedChampionBeeIdsForWorldMap()` and `TryGetCombatPatrolClaimReceiptForWorldMap(...)`.

No server-side files were touched. No combat resolution, damage, casualty, reward, or balancing code
was touched anywhere.

## Tests

No automated tests exist for this IMGUI rendering file (consistent with the rest of the WorldMap
rendering code, which has no unit test coverage). Verification was done via live `script-execute`
calls against the running Editor session:

- `ComputeMarchVisualSample` invoked directly via reflection with 5 compositions (see the table in
  "Proportional Rendering Algorithm") — all outputs correct: proportions preserved, rare families
  never dropped, small squads show exact counts, cap respected.
- Confirmed 0 compile errors and 0 new warnings after each edit (`assets-refresh` +
  `console-get-logs`, `Warning`/`Error` filters).
- Confirmed 0 runtime exceptions from `DrawCombatPatrolMarch`/`DrawMarchFormation` while the
  WorldMap `OnGUI` was actively running in the Editor (console checked immediately after each edit).

## Validation

Automated/tool-verified in this session:
- Unity compile: 0 errors (confirmed after every edit).
- `ComputeMarchVisualSample` proportional output correctness (table above).
- No exceptions raised by the live WorldMap `OnGUI` loop with the new code active.

**Manually verified in Play Mode by Jeff (2026-08-26):** confirmed working with a Guardians-only
march first, then again after adding real Wingrunner and Darter body sprites (see below) and
testing a mix of all 3 troop types in the same march — "j'ai testé avec les 3 types d'abeilles et
ca fonctionne".

**Real art added during validation (not part of the original architecture-only mission, but
directly closes the "Missing Troop Assets" gap below):** Jeff supplied real Wingrunner
(`voltigeuse.png`, background removed by regenerating with a transparent background) and Darter
(`lanceuse.png`, already transparent) body sprites, both reusing the existing wing sprite pair as
instructed. Both are now real Resources-loadable assets:
`Resources/WorldMapWave6Runtime/CombatMarch/CombatMarchBeeBody_Wingrunners.png` and
`..._Darters.png`. `MarchFamilyBodyResource(family)` now returns the real per-family resource path
for all 3 combat families instead of falling back to a tinted Guardian sprite — `MarchFamilyTint`
is kept only as a safety net if a dedicated texture fails to load at runtime. This means the
"Remaining Visual Asset Needs" item about missing Wingrunner/Darter sprites is now resolved.

## CEO Manual Validation Required

Per the brief's own validation checklist, these need an actual Play Mode pass by Jeff:

1. Dispatch a Guardian-only squad → confirm multiple Guardian visuals appear, proportional to count.
2. Dispatch a mixed squad (Guardians + Wingrunners, and/or Darters if roster allows) → confirm
   visible proportions look right and the tint difference (light blue/light green) reads clearly
   enough at map zoom, or looks too subtle/needs adjustment.
3. Confirm a zero-count family truly never appears.
4. Confirm the formation still follows the red attack curve correctly (it reuses the exact same
   Bezier marker position as before — no path math changed — but worth eyeballing).
5. Confirm collection stays yellow and raid stays violet (neither palette was touched).
6. Confirm the outbound march still takes the same real-world duration as before (marchProgress
   logic untouched).
7. Let a patrol resolve and auto-claim, then watch the return trip — confirm it now shows a
   plausible survivor count (fewer troops than went out, when losses occurred) rather than the
   full outbound composition.
8. Confirm a champion (if one is assigned via the Champion Hall, combat role) appears as a
   golden-haloed portrait leading the formation, on both outbound and return.
9. Confirm no duplicate/orphan sprites linger after an encounter fully resolves and its return trip
   finishes (should self-clean given the existing `finishedTrips` cleanup, now also clearing the two
   new caches).
10. Sanity-check performance impact is imperceptible even with 2-3 marches active simultaneously.

## Remaining Visual Asset Needs

- ~~Real Wingrunner and Darter march sprites~~ — **done**, added and validated during this mission
  (see above). All 3 combat families now render their real body art.
- No champion-specific "leading a march" pose exists — the existing static portrait art is reused
  as a small halo-framed icon. A dedicated small in-formation champion sprite (rather than a
  portrait crop) would read better at map scale but was not fabricated per the brief's rules.
- No per-family formation size/scale differentiation exists yet (all troop sprites render at the
  same size) — left as a documented hook (`DrawTroopMarchUnit`'s tint parameter sits right next to
  where a scale parameter could go) rather than invented without a design call from Jeff on whether
  families should visually differ in size.

## Confidence

High confidence in the composition/proportional-sampling logic (verified live, matches every rule in
the brief including the "rare family never disappears" edge case) and in the architectural honesty
around Champion/return-composition data (nothing invented, every limitation traced to a real,
named, missing server-side or asset-side piece rather than papered over). Medium confidence on the
formation's visual quality at actual map zoom/scale and on whether the placeholder tint
differentiation reads well enough in practice — both are exactly what item 2 of the CEO validation
list above is for, and this session could not run Play Mode itself to pre-judge it.
