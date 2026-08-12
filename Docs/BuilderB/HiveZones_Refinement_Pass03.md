# Builder-B Hive Zones - Refinement Pass 03

## Status

Preparation only. No main scene, official runtime presenter, gameplay logic, approved asset, Android build or server connection was modified.

## Current Material

- JSON: `Docs/BuilderB/hive_click_zones_v001.sample.json`
- Viewer: `Docs/BuilderB/hive_click_zone_overlay_viewer.html`
- Future mask schema: `Docs/BuilderB/Masks/HiveZones/v001/README.md`

The JSON keeps the 14 official zone ids and labels:

1. Nurserie
2. Reserve miel
3. Caserne
4. Defense
5. Genetique
6. Recherche
7. Entrepot
8. Transformation
9. Infirmerie
10. Academie
11. Banque
12. Administration
13. Archives
14. Centre alliance

## Pass 03 Changes

- The hive viewer now includes a normalized coordinate probe.
- The JSON now records a refinement pass marker.
- Mask and contour naming are explicit, but no mask is authoritative yet.

Coordinate probe behavior:

```text
asset-normalized x = pointerX / renderedAssetWidth
asset-normalized y = pointerY / renderedAssetHeight
pixelX = round(x * 2048)
pixelY = round(y * 3072)
```

## Refinement Workflow For Builder-A

1. Open the isolated viewer.
2. Use the coordinate probe to trace visible room boundaries.
3. Replace draft polygon vertices zone by zone.
4. Keep polygons clockwise where practical.
5. Export final contours as QA overlays only.
6. Generate alpha masks from final contour or hand-painted masks.
7. Compare mask edge to visual wax border at source scale.

## Acceptance Criteria For Future Integration

- Every zone has a stable id, official display label and non-overlapping priority.
- Every final zone has either a reviewed fine polygon or an alpha mask.
- Alpha masks use the same `2048x3072` source dimensions as the premium hive asset.
- Selection halo follows the visible room/territory edge, not a circle or bounding rectangle.
- Phone portrait and tablet landscape projection preserve the same normalized hit area.

## Non-Claims

This pass does not make the hive zones functional in the game. It only prepares better authoring and validation material for Builder-A.
