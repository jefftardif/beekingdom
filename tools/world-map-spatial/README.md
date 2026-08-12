# World Map Spatial Placement Wave 1

Offline support kernel only.

Do not run Unity for this tool. Do not write under `Assets/`, `Packages/`, or `ProjectSettings/`.

## Run

```powershell
& "C:\Users\Utilisateur\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe" `
  "C:\projets\beekingdomgame-master\tools\world-map-spatial\world_map_spatial_wave1.py" `
  --output "C:\projets\beekingdom\worldmap_spatial_wave4" `
  --seed 738921 `
  --players 1500 `
  --resources 10000 `
  --flights 300
```

Optional:

```text
--suitability-map <png>
```

When supplied, the suitability map is sampled as a proxy filter for visibly unsuitable deep-blue/dark terrain. Without it, only reserved zones, world bounds, sector load, alliance cap and collision margins are enforced.

## Outputs

- `run1/snapshot.json`
- `run1/validation.json`
- `run1/integration_contract.md`
- `run2/snapshot.json`
- `determinism_compare.json`
- `seed_alt/snapshot.json`
- `summary.json`

## Non-Claims

- No live server.
- No Unity runtime integration.
- No route or ground pathfinding.
- Flights are direct aerial trajectories with integer world coordinates.
