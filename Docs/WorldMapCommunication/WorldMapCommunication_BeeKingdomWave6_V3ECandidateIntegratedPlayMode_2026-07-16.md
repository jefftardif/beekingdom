# Bee Kingdom Wave6 50x50 - V3E Candidate Integrated In Unity

Timestamp: 2026-07-16 local

## Verdict

`V3E_CANDIDATE_UNITY_PLAY_MODE=PASS`

The V3E candidate is now installed as a separate Unity runtime package and has passed static and Play Mode validation in a dedicated scene.

This is a candidate integration, not a canonical swap or final Unity handoff.

## Image / QA Evidence

- Reduced candidate package:
  `C:\projets\beekingdomgame-master\artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging\production_v3e_reduced_candidate_package`
- Reduced crops: `8/8`
- QA reduced perceptual precheck:
  `C:\projets\beekingdomgame-master\Docs\QARelay\WorldMapImmenseWave6_50x50\QA_V3E_Reduced_Perceptual_Precheck.md`
- Builder-C reduced precheck:
  `C:\projets\beekingdomgame-master\Docs\BuilderCRelay\WorldMapImmenseWave6_50x50\BuilderC_V3E_Reduced_Precheck.md`

Known visual risks remain: stipple/noise/emboss, repeated local water/vegetation micro-texture, and lower micro-detail cleanliness than Wave5 premium.

## Tile Package Evidence

- Fullsize tile package:
  `C:\projets\beekingdomgame-master\artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging\production_v3e_fullsize_tile_package`
- Tile count: `2500`
- Runtime tile dimensions: `516x516` with `2px` gutter.
- Neighbor pairs checked: `4900`
- Neighbor validation: `PASS`
- Max gutter delta: `1`
- Contact sheet:
  `C:\projets\beekingdomgame-master\artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging\production_v3e_fullsize_tile_package\proof\v3e_fullsize_tile_contact_sheet.png`

## Unity Evidence

- Resource root:
  `Assets\BeeKingdom\Playground\Resources\WorldMapWave6Runtime\UIB_ImmenseContinuousMaster50x50_v3e_candidate`
- Resource install receipt:
  `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapWave6_50x50_V3ECandidate\WorldMapWave6_V3ECandidate_ResourceInstallReceipt.txt`
- Static validation receipt:
  `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapWave6_50x50_V3ECandidate\WorldMapWave6_V3ECandidate_StaticValidation.txt`
- Candidate scene:
  `Assets\Scenes\WorldMapWave6V3ECandidate.unity`
- Scene build receipt:
  `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapWave6_50x50_V3ECandidate\WorldMapWave6_V3ECandidateSceneBuildReceipt.md`
- Play Mode receipt:
  `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapWave6_50x50_V3ECandidate\PreviewScenePlayProof\WorldMapWave6_V3ECandidateScene_PlayModeProofReceipt.md`

Play Mode evidence includes:

- `entered_play_mode:true`
- `uses_v3e_candidate_runtime_package:true`
- `loaded_master_sha256:978C79C66792040F3FDE79077BE8506041FD993E695599EDCD693F2FFB60CDE3`
- `initial_visible_tiles:4/4`
- `center_z100_visible_tiles:4/4`
- `north_west_visible_tiles:6/6`
- `south_east_visible_tiles:6/6`

## Gates

- `V3E_CANDIDATE_UNITY_PLAY_MODE=PASS`
- `READY_FOR_CANONICAL_SWAP=NO`
- `READY_FOR_UNITY_HANDOFF=NO`
- `MASTER_25600_AUTHORIZED=NO`
- `MONOLITHIC_25600_WRITTEN=NO`

## Next Required Step

Run a final visual decision pass on the V3E Unity candidate. If the remaining micro-texture risk is accepted or corrected, the next bounded step is canonical swap planning. If not, V3E stays as a playable candidate scene only.
