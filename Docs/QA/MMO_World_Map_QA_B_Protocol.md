# QA-B Protocol - Future MMO World Map

Date: 2026-07-11

## Status

Preparation only.

QA-B prepares the validation protocol and refusal matrix for the future MMO world map. QA-B does not close official gates, does not declare production readiness, and does not replace QA-A final verdicts.

## Scope

This protocol covers:

- visual readability of the world map;
- refusal criteria for false live claims, unreadable territories, confusing hives, misleading minimap, and broken pan/zoom;
- tablet landscape and phone portrait checks;
- selection checks for player hive, allied hive, neutral hive, hostile hive, resource, and territory;
- server-first checks proving that official world data is never shown without server authority.

Out of scope:

- no official QA gate closure;
- no production release decision;
- no live MMO claim;
- no official server acceptance;
- no gameplay balance verdict;
- no economy, PvP, alliance, ranking, matchmaking, chat, or persistence validation.

## Required Evidence For QA-A

| Evidence | Minimum content | QA-B status |
| --- | --- | --- |
| Device screenshots | Tablet landscape and phone portrait for each map state | Prepared requirement only |
| Interaction capture | Pan, zoom, selection, deselection, minimap tap/drag | Prepared requirement only |
| Server trace | Request/response or test harness log proving source of each official datum | Prepared requirement only |
| Offline/no-server capture | Map shell without official data and with correct non-live state | Prepared requirement only |
| Debug overlay | Entity ids, ownership state, territory bounds, viewport/minimap projection | Optional, recommended |
| Regression notes | Known blockers, drift, unclear labels, false claims | Prepared requirement only |

## Visual Validation Criteria

| ID | Area | Acceptance target for QA-A review | Evidence to collect | Refusal trigger |
| --- | --- | --- | --- | --- |
| MAP-VIS-001 | First read | Player understands this is a world map, not the internal hive screen, within 3 seconds | Tablet and phone first-frame screenshots | Screen reads as hive interior, generic menu, or diagnostic surface |
| MAP-VIS-002 | Ownership language | Player, allied, neutral, and hostile ownership states are visually distinct without relying on color alone | Screenshot with all states visible | Any two ownership states can be confused in normal viewing |
| MAP-VIS-003 | Territory boundaries | Territory borders remain readable at default zoom and do not hide hive/resource markers | Default zoom screenshot with overlay | Boundaries disappear, merge, or cover important markers |
| MAP-VIS-004 | Hive markers | Hive icons have stable shape, team state, and selection affordance | Capture of multiple hives at same zoom | Hives look like resources, buttons, or decorative art only |
| MAP-VIS-005 | Resource markers | Resource nodes are visually lower priority than hives but still tappable and recognizable | Mixed hive/resource capture | Resource marker can be mistaken for hive ownership or live reward |
| MAP-VIS-006 | Label hierarchy | Labels do not collide with hives, territory borders, minimap, HUD, or safe areas | Screens at min/default/max zoom | Text overlap, clipped labels, unreadable labels |
| MAP-VIS-007 | Zoom continuity | Marker size, label density, and territory opacity scale smoothly across zoom levels | Video from min to max zoom | Pop-in, jitter, disappearing targets, or unreadable clutter |
| MAP-VIS-008 | Selection clarity | Selected object is clearly highlighted and detail panel matches the selected object | Selection screenshot for each object class | Highlight and detail panel disagree or selection is ambiguous |
| MAP-VIS-009 | Non-live disclosure | Preview/offline states cannot be read as live MMO activity | Offline/no-server capture | Copy, badge, animation, or CTA implies live alliance, live PvP, live economy, or synced players |
| MAP-VIS-010 | Accessibility floor | Critical states remain distinguishable in grayscale and reduced-motion mode | Grayscale/reduced-motion captures | State depends only on hue or continuous motion |

## Refusal Criteria

QA-B flags the build for QA-A refusal review if any item below is observed.

| ID | Refusal area | Refuse when |
| --- | --- | --- |
| MAP-REF-001 | False live claim | The map claims, implies, or visually simulates official live MMO state without confirmed server authority |
| MAP-REF-002 | Territories unreadable | Territory borders, ownership fills, or labels cannot be interpreted at default zoom on tablet landscape or phone portrait |
| MAP-REF-003 | Hives confusing | Player, allied, neutral, and hostile hives are not distinguishable, or a hive can be mistaken for resource/decoration |
| MAP-REF-004 | Misleading minimap | Minimap viewport, markers, scale, north/orientation, or selected target disagrees with the main map |
| MAP-REF-005 | Broken pan/zoom | Pan or zoom loses the map, traps the camera, jumps unexpectedly, ignores bounds, or breaks selection hit tests |
| MAP-REF-006 | Official data without server | Any official account, hive, alliance, territory, resource, PvP, ranking, event, or progression data appears while server authority is unavailable |
| MAP-REF-007 | Stale data as live | Cached/demo/fixture data is presented as current server truth |
| MAP-REF-008 | Selection mismatch | Detail panel, highlight, minimap, and server id do not refer to the same selected object |
| MAP-REF-009 | Unsafe touch targets | Primary tap targets are too small or overlap on phone portrait |
| MAP-REF-010 | Non-claim missing | Offline, demo, preview, or unavailable states omit clear non-live/non-official boundaries |

## Device Matrix

| ID | Device mode | Viewport target | Required checks | Expected result for QA-A review |
| --- | --- | --- | --- | --- |
| MAP-DEV-001 | Tablet landscape | 16:10 and 4:3 safe-area variants | Default zoom, pan edges, minimap placement, detail panel, selection halo | No UI overlap; map remains readable with thumb-accessible controls |
| MAP-DEV-002 | Tablet landscape | Low and high DPI | Marker clarity, label scale, touch target projection | Icons and territory edges remain crisp and tappable |
| MAP-DEV-003 | Phone portrait | Tall safe-area device | Default map composition, bottom/top UI, minimap, selected detail panel | Main map is usable without hiding selected object or action context |
| MAP-DEV-004 | Phone portrait | Narrow device | Dense hive/resource cluster, label collisions, selection changes | No critical target falls below minimum tap size or behind UI |
| MAP-DEV-005 | Orientation transition | Landscape to portrait and portrait to landscape | Preserve selected object, camera bounds, zoom level policy, minimap sync | No selection loss unless explicitly designed and documented |
| MAP-DEV-006 | Reduced motion | Tablet and phone | Disable continuous world activity illusions and keep state visible | Map stays readable; no false live activity remains |

## Pan, Zoom, And Minimap Tests

| ID | Action | Steps | Expected result for QA-A review | Refusal trigger |
| --- | --- | --- | --- | --- |
| MAP-NAV-001 | Pan bounds | Drag to each edge and corner at default zoom | Camera clamps cleanly to world bounds | Blank space, lost map, or infinite drift |
| MAP-NAV-002 | Pinch zoom | Zoom from minimum to maximum and back | Zoom centers predictably and preserves interaction targets | Jumps, inverted zoom, jitter, or non-deterministic scale |
| MAP-NAV-003 | Selection during pan | Select hive, pan away, pan back | Selection state remains consistent or clears by explicit rule | Detail panel shows stale object after target changed/disappeared |
| MAP-NAV-004 | Selection during zoom | Select each object type, zoom in/out | Highlight remains attached to selected object | Highlight drifts from object or hitbox changes incorrectly |
| MAP-NAV-005 | Minimap viewport | Compare minimap rectangle to main map camera | Minimap viewport matches main map position and scale | Minimap suggests a different location or scale |
| MAP-NAV-006 | Minimap target jump | Tap/drag minimap to move camera if supported | Main camera moves to the correct world location | Tap lands on wrong region or bypasses pan bounds |
| MAP-NAV-007 | Orientation indicator | Rotate/pan/zoom with minimap visible | Orientation and north/anchor policy stays explicit and stable | Minimap orientation contradicts main map |

## Selection Test Matrix

| ID | Target | Steps | Expected result for QA-A review | Server-first guard |
| --- | --- | --- | --- | --- |
| MAP-SEL-001 | Player hive | Tap player hive at default zoom, min zoom, max zoom | Player hive highlight, player-owned styling, detail panel title/id, minimap focus all match | Official stats/actions load only from server response |
| MAP-SEL-002 | Allied hive | Tap allied hive from cluster and isolated view | Allied styling, alliance relation, non-hostile affordance, detail panel match | Alliance name/status absent or marked unavailable without server |
| MAP-SEL-003 | Neutral hive | Tap neutral hive | Neutral styling, no alliance/PvP claim, correct detail panel | No live diplomacy, scout, or invite state without server |
| MAP-SEL-004 | Hostile hive | Tap hostile hive | Hostile styling is clear but not over-stated; detail panel match | No live attack/rally/PvP readiness without server |
| MAP-SEL-005 | Resource node | Tap resource marker near hive and near territory border | Resource highlight and panel are distinct from hive selection | No official yield, timer, ownership, or collection state without server |
| MAP-SEL-006 | Territory | Tap territory fill/border away from markers | Territory highlight, owner/state label, boundary emphasis | No official claim/control/war state without server |
| MAP-SEL-007 | Empty map | Tap empty space after each selection | Selection clears or remains by documented rule | No stale panel showing official data for unselected object |
| MAP-SEL-008 | Overlap conflict | Tap dense cluster where hive, resource, and territory overlap | Priority rule is deterministic and visible in debug evidence | Wrong object selected or object priority changes by device |

## Server-First Protocol

Principle: the client may render a map shell, tutorial preview, placeholders, or explicitly labeled demo fixtures. It must not render official world data unless the server is authoritative for that datum.

| ID | Scenario | Steps | Expected result for QA-A review | Refusal trigger |
| --- | --- | --- | --- | --- |
| MAP-SRV-001 | No server connection | Launch map with server unavailable | Empty/placeholder/offline shell only; no official hive, territory, resource, alliance, PvP, ranking, or event data | Any official-looking data appears |
| MAP-SRV-002 | Auth missing | Open map without authenticated session | Access blocked or non-official preview state shown | Player hive or account-linked map state appears |
| MAP-SRV-003 | Partial server data | Server returns hives but no territories/resources | Only returned authoritative categories render as official; missing categories stay unavailable | Client fills missing official data from local guesses |
| MAP-SRV-004 | Stale cache | Previous server data exists, current server unavailable | Cached data is hidden, expired, or explicitly marked non-current per design | Cached data appears as live/current |
| MAP-SRV-005 | Demo fixture | Fixture/demo mode enabled | Fixture state is visibly labeled as demo/non-official | Fixture presented as live server state |
| MAP-SRV-006 | Server id audit | Select each official object | Detail panel exposes/logs stable server id/source in QA evidence | Selection has no traceable server source |
| MAP-SRV-007 | Authority downgrade | Server disconnects while map is open | Official actions disabled; live labels removed or marked unavailable | User can act on official state after authority loss |
| MAP-SRV-008 | Conflicting data | Server changes territory owner while selected | UI updates from server or shows resolving state, without inventing outcome | Client keeps old owner as truth without stale warning |

## QA-B Handoff Notes For QA-A

- QA-B can mark rows as `prepared`, `needs evidence`, `blocked by implementation`, or `candidate refusal`.
- Only QA-A can mark official pass/fail or close the gate.
- Every candidate refusal should include device, orientation, zoom level, selected object id, server state, screenshot/video path, and reproduction steps.
- Any live MMO wording must be reviewed against server authority evidence before QA-A verdict.
- This protocol should be rerun whenever map projection, marker art, minimap logic, server data contracts, selection priority, or responsive layout changes.

## Non-Claims

This document does not validate the future MMO world map. It does not approve a build, close a QA gate, certify server readiness, or claim live MMO functionality. It only prepares QA-B protocol material for QA-A review.
