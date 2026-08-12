# Builder-C V3E Reduced Package Precheck

Date: 2026-07-16
Scope: Bee Kingdom Wave6 V3E reduced candidate package only.
Source package: `C:\projets\beekingdomgame-master\artifacts\UIB_ImmenseContinuousMaster50x50_premium_v4_full_production_staging\production_v3e_reduced_candidate_package`

No Unity launch, no scene/APK changes, and no source image edits were performed.

## Verdict

V3E_REDUCED_PACKAGE_VERDICT=PASS

Builder-C reduced precheck passes for the package surface only. The candidate package is present, the receipt JSON is readable and internally aligned, 8/8 crop records are present and marked passing, proof and comparison sheets are present, declared SHA256 values match the checked package files, and all future production/handoff gates remain closed.

This is not final QA, not Builder-C final approval, not native 25600 authorization, and not Unity handoff.

## Files verified

- Candidate 8192: `v3e_reduced_candidate_8192.png` - 8192x8192 - SHA256 `978C79C66792040F3FDE79077BE8506041FD993E695599EDCD693F2FFB60CDE3`
- Review 4096: `v3e_reduced_candidate_review_4096.png` - 4096x4096 - SHA256 `029F6C1BE332183D75864117C006422455E2DA5B1B12596C2C1E218844072D31`
- Soft review 4096: `v3e_reduced_candidate_soft_review_4096.png` - 4096x4096 - SHA256 `741DD6021C95CD68160118000354658578B7BA0483AE5A34AA197F2A301B45AD`
- Proof sheet: `proof\v3e_reduced_candidate_proof_sheet.png` - 4096x3072 - SHA256 `C3E17C3B9A89A746779766EF616F55529A5B6C093DA7A74FF037ABF48D1AF8DA`
- Thread2 comparison: `proof\v3e_vs_thread2_reference_comparison.png` - 4096x2048 - SHA256 `226DD465F88DC3705176AEE5FBB37CAC07B9E34662558856B7A1CF6CC76BA97F`
- Receipt: `V3E_REDUCED_CANDIDATE_RECEIPT.json`
- Checkpoint: `V3E_REDUCED_CANDIDATE_CHECKPOINT.md`

## Crop evidence

Receipt declares `crops_pass=8/8`; disk inventory confirms exactly eight 1024x1024 crop PNGs under `crops`.

- `northwest_coast_forest_1024.png` - PASS - SHA256 `3E340A6E9B1EBB01C0417FAEF46EEFEE36A9EF120B7FABFF97DE167756A4E41B`
- `north_mountain_lakes_1024.png` - PASS - SHA256 `0B2417B8ED5F372A8076D0CE51DE5068A1B923F0BBF08277EC7B885EF6C0E7C1`
- `northeast_mountains_1024.png` - PASS - SHA256 `E82699B460C2144490E35EE55929B4CD566586ADFED138562607AA66624E43F6`
- `west_coast_transition_1024.png` - PASS - SHA256 `76C5F33A5E8358F6C4A6FA059F6362B6CA0CB8C8D7F3CF71E61EEB7A8E71879A`
- `center_meadow_hydrology_1024.png` - PASS - SHA256 `0EA568F289D901C930F5471845A08ABD029C062AA44973BFD57B589B8C6925C1`
- `east_water_forest_edge_1024.png` - PASS - SHA256 `05AFEFBF7CEB82BD563ED9E8A31D39EB40AC54EB2AF79A624A2B7D2A1613DFAA`
- `southwest_wetland_forest_1024.png` - PASS - SHA256 `F5BDDEC174102145F1423B7F0815CD430237324189848D3AE24BA1E503EFC562`
- `southeast_bay_ridge_1024.png` - PASS - SHA256 `F30B0473470A23381A60C1782700A0006B9DF5567FBF94DC520DF7D45FF5F159`

## Gate status

- `V3E_REDUCED_CANDIDATE_PACKAGE_CREATED=YES`
- `V3E_REDUCED_CROPS_PASS=8/8`
- `MASTER_25600_AUTHORIZED=NO`
- `READY_FOR_FULL_25600_PRODUCTION=NO`
- `READY_FOR_QA_BUILDERC=NO`
- `READY_FOR_UNITY_HANDOFF=NO`
- Builder-C final gate: NO
- Handoff gate: NO

## Integration / production notes

- PASS applies only to package completeness and reduced-candidate evidence.
- Native 25600 production remains unauthorized.
- Final QA, Builder-C final, Unity handoff, APK validation, scene integration, tile-set generation, and runtime checks remain out of scope and blocked.
- Thread2 comparison is present as reference comparison evidence only, not as an authorization to promote or splice sources.
