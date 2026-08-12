# Hive Visual Contours Import Contract

Status: waiting for UI-B authored visual source.

Unity runtime path:

`Assets/BeeKingdom/Playground/Resources/BeeKingdom/HiveVisualContours.json`

Runtime loader:

`HiveVisualContourImportRuntime`

Schema:

`bee-hive-visual-contours-v1`

Coordinate space:

`normalized_0_1_reference_hive_art`

Recommended authoring pipeline:

1. Open the premium hive image in Inkscape.
2. Draw each visible wax-boundary contour manually over the image.
3. Name each SVG path with the matching zone id.
4. Convert SVG paths to normalized JSON points.
5. Put the exported file at the Unity runtime path above.

Required zone ids:

- `honey_storage`
- `administration_core`
- `nursery_cluster`
- `guard_post`
- `research_node`
- `genetics_garden`
- `warehouse_cells`
- `wax_workshop`

JSON example:

```json
{
  "schema": "bee-hive-visual-contours-v1",
  "coordinateSpace": "normalized_0_1_reference_hive_art",
  "sourceImage": "hive-ui-target.png",
  "zones": [
    {
      "id": "wax_workshop",
      "label": "Transformation",
      "svgPathName": "Transformation",
      "points": [
        { "x": 0.3266, "y": 0.5303 },
        { "x": 0.3400, "y": 0.5165 },
        { "x": 0.3540, "y": 0.5058 }
      ]
    }
  ]
}
```

Important runtime rule:

If this import file is missing or has no valid zones, Unity keeps only the invisible tactile hitboxes. It must not draw the old coded contour as a final visual contour.
