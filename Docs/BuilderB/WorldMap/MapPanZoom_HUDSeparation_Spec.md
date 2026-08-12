# Builder-B World Map Pan/Zoom Spec

## Status

Preparation only. No runtime map, scene, server flow, account state, official MMO data, or gameplay action was connected.

## Core Rule

The map camera and the HUD must be different layers.

```text
WorldMapRoot
  MapCameraLayer
    MapImage
    Territories
    AllianceTerritories
    Routes
    ResourceFields
    Wonders
    HostileNests
    NeutralZones
    PointsOfInterest
    SelectionHalo
  HudFixedLayer
    TopResourceBar
    LeftOverview
    Search
    Legend
    BottomNavigation
    Minimap
    DetailPanel
```

Only `MapCameraLayer` pans and zooms.

## Coordinate Conversion

Source asset:

`C:/projets/beekingdom/carte.png`, `1536x1024`

Normalized coordinate:

```text
mapX = pixelX / 1536
mapY = pixelY / 1024
```

Pointer to map coordinate after pan/zoom:

```text
localX = pointerScreenX - viewportLeft
localY = pointerScreenY - viewportTop
mapX = (localX - cameraOffsetX) / cameraScale / viewportWidth
mapY = (localY - cameraOffsetY) / cameraScale / viewportHeight
```

## Camera Policy

- Default zoom frames the player hive and nearby routes.
- Minimum zoom shows the map art without exposing blank space.
- Maximum zoom remains capped at `3.2x` until tile or high-resolution map art exists.
- Panning clamps to map bounds.
- Selection survives pan/zoom.
- Empty tap clears selection unless Builder-A later defines a persistent selection mode.

## HUD Policy

- HUD reserved rectangles in the JSON are blockers for map interaction.
- HUD must not inherit map scale.
- Detail panel content must match the selected object id.
- Minimap uses a projection of the main map camera; it must not become a second source of truth.
- Layer visibility is a debug/review concern only; hidden layers should not alter source data.
- HUD hit testing wins over map hit testing inside reserved HUD regions.

## Recommended Hit-Test Flow

```text
1. Check fixed HUD rectangles first.
2. If pointer is inside HUD, route to HUD or ignore map selection.
3. Convert pointer to normalized map coordinate using the inverse camera transform.
4. Evaluate map objects by priority.
5. Select the first matching object and display a screen-space halo.
6. Keep selected object id stable while panning/zooming.
```

The current Builder-B prototype keeps this as a visual/review model, not production behavior.

## Prototype Support

`world_map_overlay_viewer.html` now includes:

- wheel/button zoom;
- drag pan;
- selection halo;
- HUD reserve overlay toggle;
- per-layer visibility toggles;
- normalized coordinate probe that accounts for camera transform.

## Builder-A Integration Notes

- Keep JSON loading read-only at first.
- Keep all official state server-authoritative.
- Treat local hives, territories, resources and hostile nests as placeholders until a server contract exists.
- Keep visual hit tests deterministic with this priority order:

```text
hive > hostileNest > wonder > pointOfInterest > resourceField > hostileZone > route > allianceTerritory > neutralZone > territory
```

This spec prepares the map work; it does not activate an official MMO map.
