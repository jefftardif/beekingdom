# M015-CX FINAL LIVINGHIVE PLAYER-CAPABILITY GAP MAP

## Executive Conclusion

LivingHive is no longer blocked by the recent building-window inventory. Production, Nursery, Research, Construction/Upgrade, Barrack, Champion Hall, Royal Palace/Colony Overview, Alliance entry, Settings, resource HUD and the justified future-building status windows are now either accessible from HiveMap or honestly represented there.

The remaining functional gap is non-building player capability.

I count **6 meaningful player systems** that still prevent LivingHive from being declared functionally obsolete unless each receives a HiveMap answer:

1. Guided tutorial / chapter onboarding.
2. Army operations beyond the Barrack window.
3. Daily activities / missions / rewards progression.
4. Account inventory/economy surfaces: Stock, Reward Ledger, VIP, Speed-ups, Bag.
5. Strategic path / class / queen-profile identity.
6. Communication-adjacent social surfaces: full chat, mail/courier, friends.

Of those, **4 are P0** for functional retirement because current code already contains server-backed or materially current player flows with no equivalent HiveMap-native route: tutorial, army operations, daily/reward progression, and account inventory/economy surfaces. The other 2 are P1 unless product decides they are required in the retirement build.

Important distinction: this is not a demand to migrate every IMGUI screen. It is a demand that HiveMap provide the same meaningful player capability, preferably by reusing existing server-backed controllers and replacing fake menu panels before relying on LivingHive as the only usable access path.

## Capability Matrix

| Player System | LivingHive Capability | HiveMap Capability | Migration Status | Authority | Priority |
|---|---|---|---|---|---|
| Guided tutorial / chapters | Large chapter-driven guided flow in `HiveViewProductUiPresenter`, with opening, brood, worker, upgrade, defense, readiness, world-transition and foraging steps | No equivalent HiveMap-owned onboarding found; `SkipGuidedTutorialForExternalHost` exists as a bridge | Missing as a HiveMap player capability | Mostly legacy local/current tutorial state, product-authored flow; not server-authoritative | P0 |
| Army operations suite | Army menu, formation/readiness, official doctrine recruitment, official squad reservation, perimeter sortie, combat patrol, rewards/claim/recall flows | Barrack training window exists in HiveMap; Defense building is correctly only future/status; no full Army entry found | Partial | Server-backed for doctrine, squad reservation, perimeter sortie, combat patrol; some preview/local composition UI remains legacy | P0 |
| Daily activities / missions / events | Missions center, daily round, milestone event, bestiary/event entry points, mission widget | Bottom `Activities` panel is hardcoded fake rows: "Evenement Special", "Defi Hebdomadaire", "Quete Journaliere"; no controller-backed HiveMap activities surface found | Fake/placeholder | Daily round and milestone event are server-backed; mission center/catalog appears current client/legacy presentation | P0 |
| Stock / Bag | Stock panel reads official resource balances, completed research and active engagements; Bag-like access exists in monolith | Bottom `Bag` panel is hardcoded fake values: Nectar 120, Pollen 80, Cire 45, etc. | Fake/placeholder | Server-backed via `HiveStockSnapshotClient`; protected cache read-only fallback | P0 |
| Reward Ledger | Reward ledger panel lists rewards/events and pending reward count | No HiveMap surface found outside monolith bridges | Missing as HiveMap route | Server-backed read via `HiveRewardLedgerClient` | P0 |
| VIP | VIP client and monolith panel exist | No HiveMap VIP surface found; Bank deliberately does not own VIP | Missing as HiveMap route | Server-backed client exists, building ownership undecided | P1 |
| Speed-ups | Speed-up overlay usable from Construction/Barrack and controller wired | HiveMap can open speed-up overlay through Construction/Barrack bridges | Partial but acceptable for current attached timers | Server-backed when `HiveSpeedUpClient` is configured; local inventory also exists in tests/legacy | P1 |
| Strategic path / class | Strategic path preview/trial/details, official action and server-backed controller | No HiveMap route found; Queen profile panel is local preview only | Missing as HiveMap route | Server-backed via `StrategicPathClient`; legacy presentation still in monolith | P1 |
| Queen/profile identity | Header queen button opens a local preview panel with level/progress text only | HiveMap header exists but uses preview data, not full identity/progression | Fake/placeholder | Current client preview; product decision needed for official profile source | P1 |
| Communication / chat | Full chat screen with channels, conversations, messages, composer, emoji; server chat runtime support | HiveMap Communication toggles the monolith communication overlay. Server-backed mini-chat bridge publishes real messages and sends through `LivingHiveChatRuntime`, but the full UI remains monolith/local | Partial | Server-backed for runtime chat messages; broader chat UI still legacy/local presentation | P1 |
| Mail / Courier | Courier screen with tabs/list/reader/toasts | HiveMap communication overlay can switch to mail through monolith bridge; no HiveMap-native mail route | Partial/missing | Current client/legacy presentation; no dedicated server mail client found in inspected networking list | P1 |
| Friends | Friends screen with tabs/actions/search/toasts | No HiveMap menu route found except monolith proof/internal routes | Missing or hidden | Legacy local/client presentation; server authority not found | P2 / Product decision |
| Bestiary Codex | Bestiary codex overlay with server-backed read controller | No HiveMap route found; WorldMap has local/proof bestiary interaction but no official HiveMap codex entry | Missing as HiveMap route | Server-backed read via `BestiaryCodexClient`; WorldMap bestiary gameplay appears local/proof for some interactions | P2 |
| Milestone Event | Overlay with objectives, reward and claim action | No HiveMap route found except monolith mission/more paths | Missing as HiveMap route | Server-backed read/claim via `HiveMilestoneEventClient` | P0/P1, depending whether folded into Activities wave |
| World map access | Bottom Carte loads the canonical `WorldMapWave6Wave5Method12288Preview.unity` scene | HiveMap route exists | Already complete enough for retirement | Scene/navigation dependency, not LivingHive | Complete |
| Royal Palace / Colony Overview | Level, level cap role, upgrade state/action, colony overview | HiveMap fullscreen modal accepted after M013 | Complete enough; monolith bridge remains temporary | Upgrade server-backed when controller configured; colony overview current client overlay | Complete |
| Bank / Infirmary / Genetics / Academy / Defense buildings | Future/status only in LivingHive, no detail panels/controllers | HiveMap status windows accepted/justified | Already complete as honest placeholders | Future only as buildings; related systems are separate | Complete |

## P0 - Must Migrate Before LivingHive Functional Retirement

### 1. Guided Tutorial / Chapter Onboarding

LivingHive still contains a large tutorial state machine and chapter flow inside the monolith. It is not just presentation: it coordinates objectives, gated clicks, local resource effects, choices, timers, defense/readiness beats, world transition and proof hooks.

HiveMap can become the only scene without preserving every legacy tutorial detail, but it cannot claim functional replacement while first-run onboarding only lives in the LivingHive monolith.

Recommended retirement requirement:

- HiveMap owns the first-run entry point.
- Tutorial state no longer requires `LivingHive.unity`.
- Any retained tutorial bridge is explicitly temporary and blocks input/modal presentation correctly.
- Product decides which legacy tutorial chapters remain canonical, which are rebuilt, and which are obsolete.

### 2. Army Operations Beyond Barrack

HiveMap Barrack covers troop training and claim behavior. That does not cover the broader army suite already present in code:

- `HiveDoctrineRecruitmentClient` / `HiveDoctrineRecruitmentPanelController`.
- `HiveSquadReservationPanelController`.
- `HivePerimeterSortieClient` / `HivePerimeterSortiePanelController`.
- `CombatPatrolClient` / `CombatPatrolPanelController`.
- Formation/readiness and squad composition presentation.

M014 correctly says Defense is not the owner of this functionality. That means the capability should not be stuffed into the Defense building just to close a gap. It needs an Army entry point in HiveMap, probably as a global/player system or a dedicated combat surface.

### 3. Activities / Daily / Missions / Rewards Progression

The HiveMap bottom `Activities` panel is currently fake static text. In contrast, the codebase has:

- `HiveDailyRoundClient` and `HiveDailyRoundPanelController`, with read/claim/retry.
- `HiveMilestoneEventClient` and `HiveMilestoneEventPanelController`, with read/claim.
- `MissionCatalog` and a monolith Missions Center / widget.

This is a player-facing progression and reward loop. It should be one of the earliest replacements because the current HiveMap surface looks functional but is not authoritative.

### 4. Account Inventory / Economy Surfaces

The HiveMap `Bag` panel is fake static text. Current code contains real account-backed inventory/economy surfaces:

- `HiveStockSnapshotClient` / `HiveStockPanelController`.
- `HiveRewardLedgerClient` / `HiveRewardLedgerPanelController`.
- `HiveVipClient`.
- `HiveSpeedUpClient` / `HiveSpeedUpPanelController`.

M013/M014 correctly decided not to attach these to Bank. That does not remove the need for player access. The safe path is a global Inventory/Account/Economy surface that reuses those controllers directly.

## P1 - Important Next Systems

### Strategic Path / Class / Profile

Strategic path is server-backed through `StrategicPathClient` and has monolith presentation. HiveMap currently has only a Queen profile preview panel with hardcoded progress text. This should not block the earliest scene-deprecation milestone if product accepts it as non-critical, but it blocks any claim that HiveMap fully replaces LivingHive player progression identity.

### Communication, Mail and Friends

Chat is partially recovered: `LivingHiveChatBridgeBootstrap` wires real server-backed chat messages and sending into HiveMap, and the Communication button opens the monolith communication overlay. That is a valid temporary bridge.

The broader communication suite remains uneven:

- full chat UI remains monolith presentation;
- mail/courier is reachable only through the monolith communication overlay;
- friends appears to be legacy/client presentation with proof hooks and no inspected server client.

This can be P1 if "server chat minimum" is accepted for retirement. It becomes P0 if the retirement definition requires mail/friends/full chat parity.

## P2 - Can Follow Later

- Bestiary Codex as a standalone HiveMap route, unless product makes it part of the Activities wave.
- Friends, if not part of the MVP social contract.
- VIP, if product has not decided where premium/account status should live.
- Shop, because the HiveMap header shop panel explicitly says content will be added later.
- Help and Support rows under More, because the menu spec only lists entries and currently routes only Settings.

## Rebuild Required

These should be rebuilt as HiveMap-owned player surfaces rather than extracted wholesale from IMGUI:

- Activities hub: daily round, milestone events, mission center and reward entry points.
- Army hub: doctrine recruitment, squad reservation, perimeter sortie, combat patrol and formation/readiness.
- Inventory/Account hub: stock, reward ledger, speed-ups and possibly VIP.
- Tutorial runtime: at minimum a HiveMap first-run guided path.

The rebuild should reuse existing clients/controllers/presentation models where available, not copy `DrawMissionsCenterScreen`, `DrawArmyMenuPanel`, `DrawStrategicPathPreviewPanel`, `DrawFriendsScreen` or similar monolith layout code as-is.

## Already Complete

These are complete enough for `LivingHive.unity` retirement, though some still use temporary monolith bridges:

- HiveMap entry/splash gate.
- Resource HUD.
- Manual production tap/collect and production info/forecast.
- Nursery brood vitality/feed/stabilize.
- Research fullscreen modal.
- Construction/Upgrade and Queue Sidebar.
- Barrack training and ready-claim feedback.
- Champion Hall current read-only supported subset.
- Royal Palace / Colony Overview.
- Alliance building entry to existing overlay.
- Settings overlay.
- World map scene transition.
- Bank, Infirmary, Genetics, Academy and Defense as honest future/status building windows.

## Obsolete / Do Not Migrate

Do not migrate these merely to eliminate code:

- Fake HiveMap Activities rows as gameplay.
- Fake HiveMap Bag resource values.
- Queen profile preview level/progress as authoritative state.
- Bank loans, taxes, investments, prestige, currency exchange or other invented bank mechanics.
- Defense-owned combat actions without a product/API decision.
- Academy-owned research/training actions without a product/API decision.
- Infirmary-owned Brood care unless product explicitly moves Brood care under Infirmary.
- Genetics-building mutation actions unless a real Genetics building controller exists.
- WorldMap local/proof bestiary rewards as official Hive capability.
- Monolith proof/test helpers and capture-only methods.

## Product Decisions Required

1. What is the minimum first-run tutorial required before `LivingHive.unity` can be deprecated?
2. Should Army be a global menu entry, a building entry, or a contextual flow from Barrack/Defense?
3. Should Activities own Daily Round, Milestone Event, Mission Center and Reward Ledger, or should Rewards live under Bag/Inventory?
4. Where should Stock, Reward Ledger, Speed-ups and VIP live: Bag, Account, Shop, or separate surfaces?
5. Is server-backed mini-chat enough for scene retirement, or must full chat/mail/friends parity ship first?
6. Is Strategic Path/Class required for retirement, or can it follow after the scene is deprecated?

## Fake / Placeholder HiveMap Surfaces

These are the highest-risk fake surfaces because they look like real player systems:

- `Activities`: static fake events/challenges/quests.
- `Bag`: static fake resources and capacity.
- `QueenProfile`: preview level/progress only.
- `Shop`: explicit trial/access shell.
- `More/Aide/Support`: visible entries with no inspected functional route.

These are not the same as the accepted building status windows. The building status windows are honest "future" surfaces after code verification. Activities/Bag/QueenProfile are more dangerous because they mimic active player data.

## Recommended Migration Waves

### Wave A - Replace Fake Global Menu Surfaces

- Replace `Activities` static content with a controller-backed hub for Daily Round and Milestone Event.
- Replace `Bag` static content with `HiveStockPanelController` and entry points for Reward Ledger/Speed-ups.
- Label any unavailable subfeature honestly.

### Wave B - Army Hub

- Add a HiveMap-owned Army entry point.
- Reuse `HiveDoctrineRecruitmentPanelController`, `HiveSquadReservationPanelController`, `HivePerimeterSortiePanelController` and `CombatPatrolPanelController`.
- Keep Defense as future/status until a Defense building controller exists.

### Wave C - Tutorial Retirement Path

- Decide the canonical HiveMap first-run flow.
- Port only the tutorial beats required for the current product.
- Avoid carrying old proof/local preview branches unless they still teach active gameplay.

### Wave D - Account/Profile/Social

- Promote Strategic Path/Class/Profile if product requires identity progression before retirement.
- Decide whether Communication keeps the temporary monolith overlay or receives a HiveMap-native full screen.
- Keep mini-chat server bridge if it continues to satisfy the minimum social requirement.

### Wave E - Cleanup After Scene Deprecation

- Remove scene-only dependencies.
- Leave targeted `*ForExternalHost` adapters only where they still expose current controllers safely.
- Defer monolith retirement until no player runtime depends on `HiveViewProductUiPresenter`.

## Functional Retirement Checklist

Declare `LivingHive.unity = FUNCTIONALLY OBSOLETE / DEPRECATED` only when:

- Production player navigation never loads `LivingHive.unity`.
- HiveMap covers all already-migrated building windows and accepted status/future buildings.
- No remaining current player capability is accessible only by opening LivingHive.
- Fake `Activities`, `Bag` and `QueenProfile` surfaces are either replaced with real data or explicitly relabelled as unavailable/preview.
- A HiveMap route exists for the accepted minimum of: tutorial, army, activities/rewards, inventory/account economy, communication.
- Temporary monolith bridges used by HiveMap are modal/input-safe and scene-independent.
- Server-backed controllers are configured outside the LivingHive scene.
- QA can complete a normal player loop from HiveMap: enter hive, collect, manage production, manage brood, research, build/upgrade, train/claim troops, inspect Royal Palace/Colony Overview, open accepted social/account/progression surfaces, and go to WorldMap.

This milestone does **not** require `HiveViewProductUiPresenter.cs` retirement.

## Confidence

MEDIUM-HIGH.

The building-window side is high confidence because M006-M014 already validated it, and M014 specifically verified the accepted placeholders. The non-building map is medium-high because direct code inspection found the relevant clients/controllers and several HiveMap fake/partial surfaces, but I did not perform Play Mode validation and did not exhaustively audit every monolith branch inside the 40k+ line presenter.
