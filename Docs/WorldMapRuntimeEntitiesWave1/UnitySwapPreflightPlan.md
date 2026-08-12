# WorldMap Runtime Entities Wave1 - Unity Swap Preflight Plan

## Statut

PREPARED_NOT_APPLIED

## Condition d'entree obligatoire

Ne rien appliquer tant que le proprietaire n'a pas choisi:

`APPROVE_FOR_UNITY_PLACEHOLDER_SWAP`

## Manifeste source

`C:\projets\beekingdomgame-master\artifacts\WorldMapRuntimeEntitiesWave1\premium\lab_placeholder_exchange_manifest.json`

## Portee autorisee apres approbation

- Laboratoire local uniquement.
- Remplacement visuel des placeholders `PLAYER_TEST_HIVE` et `ENEMY_TEST_HIVE`.
- Aucun changement gameplay officiel.
- Aucun serveur.
- Aucun APK.
- Aucune tuile Wave5.
- Aucun BearDen.

## Etapes proposees apres approbation

1. Copier les PNG approuves sous un dossier `Assets/BeeKingdom/Playground/Resources/WorldMapRuntimeEntitiesWave1/`.
2. Ajouter un petit chargeur runtime local optionnel pour lire les PNG via `Resources.Load`.
3. Brancher uniquement le rendu du `WorldMapLocalLabRuntime`, avec fallback sur les formes IMGUI actuelles si les PNG manquent.
4. Compiler Unity.
5. Relancer le harness Mission 1 et verifier que Reset/Collecte/Combat restent PASS.
6. Verifier Wave5/BearDen non modifies.

## Refus explicites

- Pas de publication.
- Pas de live data.
- Pas de DNS/TLS/SQL.
- Pas de persistance officielle.
- Pas de reconstruction APK.

