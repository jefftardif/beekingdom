# World Map Tiles Pipeline

Offline tooling only. Do not open Unity and do not write under `Assets/`.

## Build Tiles

```powershell
& "C:\Users\Utilisateur\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe" `
  "C:\projets\beekingdomgame-master\tools\world-map-tiles\world_map_tile_pipeline.py" `
  --source "C:\projets\beekingdom\carte.png" `
  --output "C:\projets\beekingdom\worldmap_tiles_wave4\run1" `
  --tile-size 512 `
  --world-id "BK-WORLD-WAVE4-PROOF" `
  --atlas-id "official-carte-proxy" `
  --version "wave4-toolproof" `
  --origin-x 0 `
  --origin-y 0 `
  --edge-mode actual
```

## Compare Two Runs

```powershell
& "C:\Users\Utilisateur\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe" `
  "C:\projets\beekingdomgame-master\tools\world-map-tiles\compare_tile_runs.py" `
  --run-a "C:\projets\beekingdom\worldmap_tiles_wave4\run1" `
  --run-b "C:\projets\beekingdom\worldmap_tiles_wave4\run2" `
  --output "C:\projets\beekingdom\worldmap_tiles_wave4\wave4_determinism_compare.json"
```

## Scope

- Visual tiles only.
- No route or pathfinding data.
- No server claims.
- `carte.png` is read-only input and remains a proxy source, not the final enormous MMO world.
