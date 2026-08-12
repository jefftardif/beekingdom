# Builder-B Handoff - Hive Clickable Zones

## Status

Material ready for Builder-A review and future integration. Pass 2 extends the draft from 7 generic zones to the 14 official hive zones. This is preparation only: no official gameplay feature, scene change, Android build change, runtime presenter change, server connection, or validated microcopy was modified.

## Scope

Target asset:

`Assets/Art/UI/Backgrounds/HiveBackground_UnityReady_2048x3072.png`

The asset is portrait `2048x3072`. All proposed points use normalized asset coordinates so the same data can be projected into tablet landscape, phone portrait, and future cropped UI containers.

## Proposed JSON Format

Use `asset-normalized` points, origin top-left, values from `0.0` to `1.0`.

Recommended fields:

- `schema`: versioned contract, currently `bee-kingdom.hive-click-zones.v1`.
- `asset`: source asset id, path, pixel size, optional content frame.
- `coordinateSystem`: origin, ranges, winding, pixel conversion rule.
- `hitTestPolicy`: primary method, fallback method, touch target expansion, tolerance, overlap resolution.
- `zones[]`: stable id, display key, type, priority, runtime shape recommendation, sample quality, polygon points.

Prepared sample:

`Docs/BuilderB/hive_click_zones_v001.sample.json`

## Sample Normalized Zones

The sample JSON includes draft points for the 14 official zones:

- `nursery_cluster` - Nurserie
- `honey_storage` - Reserve miel
- `guard_post` - Caserne
- `defense_growth` - Defense
- `genetics_garden` - Genetique
- `research_node` - Recherche
- `warehouse_cells` - Entrepot
- `wax_workshop` - Transformation
- `infirmary_grove` - Infirmerie
- `academy_canopy` - Academie
- `hive_bank` - Banque
- `administration_core` - Administration
- `archives_honeyfall` - Archives
- `alliance_future_hall` - Centre alliance

These are deliberately marked `draft-from-visual-reference`. They are good enough to validate the data shape and overlay workflow, not final enough to bind to official gameplay.

## Technical Recommendation

Recommended final approach:

1. Use alpha masks as the authoritative hit-test surface for premium hive zones.
2. Keep fine polygons as editable source data and fallback hit-test data.
3. Generate or maintain a contour overlay for review only.
4. Store all authored data in normalized asset coordinates.
5. Expand hit targets at runtime by device class without moving the visible boundary.

Why this combination:

- Fine polygons alone are workable but become fragile around painted wax borders and uneven silhouettes.
- Alpha masks can match the premium art boundary pixel-perfectly and are easier to validate visually.
- Polygons remain useful for editing, debugging, approximate physics raycasts, and platforms where mask lookup is not desired.
- Normalized points survive resolution changes, safe-area layout, and aspect-ratio projection.

Recommended mask convention:

- One grayscale or alpha PNG per clickable zone, same pixel dimensions as the source asset.
- White or alpha `>= 0.5` means clickable.
- Black or alpha `< 0.5` means not clickable.
- Optional contour PNG can be generated from mask edges for QA screenshots.

Future mask file schema:

```text
Docs/BuilderB/Masks/HiveZones/v001/
  mask_hive_zone_01_nursery_cluster_2048x3072.png
  mask_hive_zone_02_honey_storage_2048x3072.png
  mask_hive_zone_03_guard_post_2048x3072.png
  mask_hive_zone_04_defense_growth_2048x3072.png
  mask_hive_zone_05_genetics_garden_2048x3072.png
  mask_hive_zone_06_research_node_2048x3072.png
  mask_hive_zone_07_warehouse_cells_2048x3072.png
  mask_hive_zone_08_wax_workshop_2048x3072.png
  mask_hive_zone_09_infirmary_grove_2048x3072.png
  mask_hive_zone_10_academy_canopy_2048x3072.png
  mask_hive_zone_11_hive_bank_2048x3072.png
  mask_hive_zone_12_administration_core_2048x3072.png
  mask_hive_zone_13_archives_honeyfall_2048x3072.png
  mask_hive_zone_14_alliance_future_hall_2048x3072.png
```

Optional generated QA overlays should use a separate path:

```text
Docs/BuilderB/Masks/HiveZones/v001/qa_contours/
  contour_hive_zone_01_nursery_cluster_2048x3072.png
```

## Validation Method

Prepared visual checker:

`Docs/BuilderB/hive_click_zone_overlay_viewer.html`

Open it in a browser from the workspace. It loads `hive_click_zones_v001.sample.json` and overlays the JSON points on the premium hive asset. If a browser blocks local JSON loading from `file://`, use the file selector in the viewer to choose the JSON manually.

The viewer includes a selection preview: click any polygon to show a brighter boundary/halo and inspect the zone id, official name, runtime shape recommendation and future mask path.

Validation pass for Builder-A or QA after integration:

1. Render the premium hive asset in the target UI container.
2. Convert each normalized point to rendered screen coordinates after aspect-fit/crop calculation.
3. Draw zone fill at 15-25% opacity, contour at high contrast, and numbered vertices.
4. Capture tablet landscape and phone portrait screenshots.
5. Compare overlay boundary against visible wax/room borders.
6. Reject zones with visible drift greater than `3 px` at source scale or with overlap ambiguity on adjacent rooms.
7. Run tap probes at each vertex, edge midpoint, centroid, and just outside each edge.

## Responsive Projection Notes

The data should be interpreted against the source asset, then projected through the actual image transform:

- Aspect fit: keep full asset visible; black bars or empty UI space are outside hit-test.
- Aspect fill/crop: subtract crop offsets before normalized lookup.
- Safe areas: apply safe-area transform after asset projection, not inside the JSON.

Formula for aspect-fit display:

```text
renderScale = min(containerWidth / assetWidth, containerHeight / assetHeight)
renderWidth = assetWidth * renderScale
renderHeight = assetHeight * renderScale
offsetX = (containerWidth - renderWidth) / 2
offsetY = (containerHeight - renderHeight) / 2
screenX = offsetX + normalizedX * renderWidth
screenY = offsetY + normalizedY * renderHeight
```

Formula for pointer lookup:

```text
assetX = (screenX - offsetX) / renderWidth
assetY = (screenY - offsetY) / renderHeight
inside = 0 <= assetX <= 1 && 0 <= assetY <= 1
```

## Integration Handoff For Builder-A

Builder-A can later integrate this by:

- choosing the official source asset and confirming whether aspect fit or aspect fill is used;
- replacing draft polygons with traced final contours or generated masks;
- loading the JSON into a read-only click-zone registry;
- mapping stable zone ids to approved runtime actions only after Architect approval;
- adding the overlay as an Editor/debug-only view;
- adding tap-probe tests for portrait and landscape projections.

Builder-B recommendation: do not make circles the final hit zones. Use alpha masks for the final premium experience, backed by fine polygons for traceability and fallback.

## Non-Claims

This handoff does not declare the feature complete in the game. It does not validate Demo or QA. It only prepares material for Builder-A integration.
