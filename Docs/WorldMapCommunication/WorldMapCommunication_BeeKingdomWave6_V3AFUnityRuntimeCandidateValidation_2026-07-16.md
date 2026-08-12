# Bee Kingdom Wave6 - V3AF Unity Runtime Candidate Validation

Created: 2026-07-16T18:39:00

## Status

- V3AD local native 4096: FAIL perceptuel, blocked. Do not use for Unity.
- V3D/V3E/V3H 8192 crops: provisional visual PASS as candidate material, clearly better than V3AD and rejected legacy 25600.
- V3E candidate runtime bundle in Unity Resources: offline structural PASS.
- Play Mode proof: pending because Unity is currently locked by active Unity processes.

## Evidence

- V3AE perceptual audit receipt: `C:\projets\beekingdomgame-master\artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging\production_v3ae_existing_hd_source_audit\V3AE_EXISTING_HD_SOURCE_AUDIT_RECEIPT.json`
- V3AF Unity runtime candidate receipt: `C:\projets\beekingdomgame-master\artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging\production_v3af_unity_runtime_candidate_validation\V3AF_UNITY_RUNTIME_CANDIDATE_VALIDATION_RECEIPT.json`
- Unity runtime bundle: `C:\projets\beekingdomgame-master\Assets\BeeKingdom\Playground\Resources\WorldMapWave6Runtime\UIB_ImmenseContinuousMaster50x50_v3e_candidate`

## V3AF Gates

- `V3AF_MANIFEST_SCHEMA_PASS=YES`
- `V3AF_MASTER_SHA_MATCH_PROVIDER=YES`
- `V3AF_TILE_COUNT_2500=YES`
- `V3AF_TILE_FILES_2500_PRESENT=YES`
- `V3AF_TILE_DIMENSIONS_516=YES`
- `V3AF_RUNTIME_VALIDATION_PASS=YES`
- `V3AF_OFFLINE_UNITY_RUNTIME_CANDIDATE_PASS=YES`
- `V3AF_PLAY_MODE_VERIFIED=NO`
- `READY_FOR_QA_BUILDERC=NO`
- `READY_FOR_UNITY_HANDOFF=NO`

## Build Verification

`dotnet build beekingdomgame-master.slnx --no-restore` succeeded with 0 warnings and 0 errors.

## Next Required Action

Run Unity Play Mode proof with the V3E candidate runtime bundle enabled. Do not canonical-swap or Unity-handoff until Play Mode proof and independent QA/Builder-C pass.
