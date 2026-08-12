# World Map Wave3 - Android Test APK Handoff

Ce dossier prepare un futur APK Android Development local. Il ne construit aucun APK et ne modifie aucun fichier Unity.

## Etat courant

`BUILD_ALLOWED_NOW = NO`

Les trois gates suivants doivent etre `PASS` strict avant toute compilation:

1. QA runtime Step4D;
2. QA art/bundle Wave3;
3. QA integration Unity Wave3.

`PASS_WITH_RESERVES`, une validation Builder ou une preuve technique ne remplacent pas ces trois PASS QA.

## Livrables

- `WorldMapWave3_AndroidDevelopmentApk_Procedure.md`: procedure de build et smoke test;
- `WorldMapWave3_AndroidBuild_GoNoGoMatrix.md`: gate de decision;
- `WorldMapWave3_AndroidBuildHandoff.manifest.json`: contrat machine-readable;
- `Test-WorldMapWave3AndroidPreflight.ps1`: audit en lecture seule, sans appel Unity/build;
- `BuilderB_WorldMapWave3AndroidTestApkHandoff_Report.md`: rapport fallback.

## Sortie future reservee

`C:\projets\beekingdomgame-master\Builds\Android\BeeKingdom_WorldMapWave3_uib-wave3-continuous-v1_Development_001.apk`

Cette sortie n'existe pas au moment du handoff. Si elle existe avant le futur build, le build est `NO-GO`: il est interdit de l'effacer ou de l'ecraser. Une revision de handoff doit alors reserver `_002`.

## Non-claims

- aucune compilation executee;
- aucun APK Wave3 produit;
- aucune installation appareil executee;
- aucune preuve physique fermee;
- aucun serveur ou reseau live;
- signature release et secrets interdits;
- build local/demo Development seulement.
