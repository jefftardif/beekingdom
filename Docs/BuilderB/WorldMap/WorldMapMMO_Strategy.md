# Builder-B Handoff - MMO World Map

## Status

Preparation material only. No main scene, official runtime, server connection, gameplay logic, Android build, approved assets, or validated microcopy was modified.

Source reference:

`C:/projets/beekingdom/carte.png`

The image is `1536x1024`. The concept currently includes the world art plus HUD, legend, minimap, bottom navigation and resource bar in one flat bitmap. Builder-B data is normalized over the full image so the overlay can be validated against the supplied concept.

## Draft JSON Format

Prepared sample:

`Docs/BuilderB/WorldMap/world_map_zones_v001.sample.json`

Top-level groups:

- `hives`: player, allied, neutral and hostile hives with normalized point position and tap radius.
- `territories`: polygon boundaries with owner/state references.
- `allianceTerritories`: grouped alliance polygons that can span multiple hive territories.
- `routes`: multi-point flight paths/frontier paths.
- `resources`: point markers for pollen, nectar, royal jelly and propolis fields.
- `wonders`: point markers with larger selection radius.
- `hostileNests`: point markers for major hostile nests and frontier brood outposts.
- `hostileZones`: polygon danger areas.
- `neutralZones`: polygon areas without player/alliance/hostile ownership.
- `pointsOfInterest`: landmarks and future route nodes.
- `hudReservedRegions`: draft rectangles marking areas that should not belong to the pan/zoom map layer.

Coordinate rule:

```text
xNormalized = xPixels / 1536
yNormalized = yPixels / 1024
```

The current points are draft visual approximations. They are ready for overlay review, not final server-authoritative data.

## Prototype

Prepared isolated viewer:

`Docs/BuilderB/WorldMap/world_map_overlay_viewer.html`

Capabilities:

- loads `world_map_zones_v001.sample.json`;
- displays hives, territories, alliance territories, routes, resources, wonders, hostile nests, hostile zones, neutral zones and points of interest;
- supports local pan and wheel/button zoom;
- draws a selection halo for the clicked object;
- can show/hide HUD reserved regions.

If the browser blocks local JSON loading, use the file selector to open the JSON manually. If it blocks the external image path, open from a local browser context that permits `file:///C:/projets/beekingdom/carte.png`.

## Pan/Zoom Strategy

Builder-B recommendation: the map camera must be separate from the HUD.

Layer model for future Builder-A integration:

```text
WorldMapRoot
  MapCameraLayer
    MapArt
    TerritoryOverlay
    RouteOverlay
    EntityMarkers
    SelectionHalo
    DebugNormalizedPoints
  HudFixedLayer
    TopResourceBar
    LeftOverviewPanel
    Search
    Legend
    BottomNavigation
    Minimap
    DetailPanel
```

Rules:

- Pan and zoom only transform `MapCameraLayer`.
- HUD, legend, search, detail panel and minimap stay in screen space.
- Hit tests convert pointer screen position into map normalized coordinates after reversing camera transform.
- Marker visual size should use a hybrid scale: position follows map zoom, tap target remains at least 44 dp.
- Territory polygons and hostile areas can scale with the map, but selection strokes should use screen-space thickness.
- Minimap has its own projection model and should never be used as the authoritative world coordinate source.

Suggested camera limits:

- Min zoom: show full map content frame.
- Default zoom: frame player hive plus nearby routes.
- Max zoom: no more than `3.2x` until higher-resolution tiles exist.
- Clamp panning to world bounds with a small elastic margin only if UX requests it.

## Validation Method

1. Open the isolated viewer.
2. Toggle HUD reserved regions and confirm future interactive map content does not depend on HUD pixels.
3. Select each hive, territory, alliance territory, resource, wonder, hostile nest, route, hostile zone, neutral zone and point of interest.
4. Verify the selected halo refers to the same object shown in the detail panel.
5. Pan to every edge at min/default/max zoom.
6. Confirm no object becomes unreachable behind reserved HUD regions.
7. Compare with `Docs/QA/MMO_World_Map_QA_B_Protocol.md` before any Builder-A integration.

Refuse integration if:

- the map implies live MMO state without server authority;
- HUD elements pan with the map;
- selection changes between devices because overlap priority is undefined;
- a route, territory or hive cannot be selected at phone portrait tap sizes.

## Future Integration Notes For Builder-A

Builder-A can later:

- split the flat concept into map art and fixed HUD layers;
- replace draft polygons with final authored territory contours;
- decide whether alliance territories are computed from server ownership or stored as authored macro-regions;
- keep hostile nests distinct from hostile hives until combat/server language is approved;
- define which points of interest are decorative, navigational or future interactive targets;
- load the JSON into a read-only map registry;
- map client objects to server ids only after server authority is available;
- add a debug overlay for normalized coordinates and selection priority;
- connect minimap viewport math after the main map camera model is stable.

Do not treat this file as official world state. It is Builder-B preparation material for future integration.
