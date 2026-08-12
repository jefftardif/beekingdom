# Bee Kingdom Wave6 50x50 - Unity Packaging Readiness For V3R

Status: Unity path inspected while V3R source proof is pending.

Existing runtime evidence:
- Unity version: `6000.5.3f1`
- Runtime provider: `C:\projets\beekingdomgame-master\Assets\BeeKingdom\Playground\WorldMapWave6StreamingTileProvider.cs`
- Runtime roots already supported:
  - `WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v1`
  - `WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3d_preview`
  - `WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3e_candidate`
  - `WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_v3m_preview`
- V3M preview scene builder: `C:\projets\beekingdomgame-master\Assets\BeeKingdom\Playground\Editor\WorldMapWave6V3MPreviewSceneBuilder.cs`

Important packaging finding:
- The legacy Python packager `C:\projets\beekingdomgame-master\tools\world-map-wave6-unity-integration\build_wave6_runtime_bundle.py` is frozen to the old v1 master and expected hashes.
- It must not be reused directly for V3R unless cloned/adapted into a versioned candidate packager with V3R source paths and hashes.

Candidate path when V3R passes:
1. Create a versioned runtime root, for example:
   `Assets\BeeKingdom\Playground\Resources\WorldMapWave6Runtime\UIB_ImmenseContinuousMaster50x50_v3r_candidate`
2. Produce exactly `2500` runtime PNGs named `R00C00_g2.png` through `R49C49_g2.png`.
3. Preserve contract:
   - source tile size `512`
   - runtime tile size `516`
   - gutter `2`
   - rows `50`
   - columns `50`
   - origin chunk `7,7`
   - resources root matching the new v3r candidate folder
4. Write `runtime_manifest.json` and `runtime_validation.json`.
5. Add a provider constant and expected V3R source hash only after source QA PASS.
6. Add a V3R candidate scene builder and Play Mode proof harness by following the V3M/V3E pattern.

Closed gates:
- V3R source PASS: pending
- 2500 tiles: not created
- Unity candidate root: not created
- Play Mode proof: not run
- READY_FOR_UNITY_HANDOFF=NO

This note is preparation only. It does not authorize a tile package or Unity handoff.
