# Hive Zone Alpha Masks - v001

This folder is reserved for future Builder-A or Art-produced alpha masks.

No mask in this folder is currently authoritative. The JSON paths are a proposed file schema only.

## Source

- Source asset: `Assets/Art/UI/Backgrounds/HiveBackground_UnityReady_2048x3072.png`
- Mask size: `2048x3072`
- Format: 8-bit PNG alpha or grayscale
- Clickable threshold: alpha or luma `>= 0.5`

## Naming

```text
mask_hive_zone_{officialOrder}_{id}_2048x3072.png
```

Example:

```text
mask_hive_zone_01_nursery_cluster_2048x3072.png
```

QA contour overlays, if generated later, should live in:

```text
qa_contours/contour_hive_zone_{officialOrder}_{id}_2048x3072.png
```
