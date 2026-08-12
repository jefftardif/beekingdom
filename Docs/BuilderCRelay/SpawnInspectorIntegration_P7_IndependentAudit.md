# Spawn Inspector Integration P7 - Independent Audit

Date locale: 2026-07-15

## Portee

Audit technique independant Builder-C Relay contre:

- `Docs/BuilderCRelay/WorldMapSpawnDistribution_TechnicalContract.md`
- `Docs/WorldMapRuntimeEntitiesWave1/SpawnInspectorIntegration_Report.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/SpawnInspectorProof/SpawnInspectorProofReceipt.md`

Contraintes respectees:

- Aucun fichier Unity modifie.
- Aucun PNG modifie.
- Aucun APK modifie.
- Aucun terrain, scene, master terrain, BearDen source, serveur, remote ou donnee reelle modifie.

## Synthese preuves lues

Rapport P7 principal:

- Scene cible: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`.
- Portee: inspecteur/generateur local deterministe des spawns World Map.
- Compilation Unity batchmode: PASS, zero erreur.
- Play Mode P7: PASS.
- Panneau `SPAWN INSPECTEUR`.
- Badge `LOCAL - APERCU NON OFFICIEL`.
- Overlay diagnostic OFF par defaut.
- Seed local editable et `spawn_v1`.
- Generation preview locale sans mutation terrain ni serveur.
- IDs preview stables: `preview:{world}:{grid}:family:chunk:slot:version:seed`.
- Ressources R1/R2/R3 et menaces T1-T7 couvertes.
- Exclusions diagnostiques: BearDen, eau, falaise, evenement reserve.
- `server=false`.
- `official_gain=false`.
- Regression P1-P6: PASS.

Recu P7:

- Play Mode: PASS.
- Seed A hash: `01b78336`.
- Seed B hash: `fef6f1b4`.
- Active chunks / hives / resources / threats: `25/2/11/7`.
- Exclusion hits BearDen/water/cliff/event: `0/0/0/25`.
- Same seed/version deterministic: PASS.
- Different seed distribution changed: PASS.
- Exclusion zones: PASS.
- Density budgets: PASS.
- Spawn inspector UI: PASS.
- Diagnostic overlay default: OFF.
- `server=false`.
- `official_gain=false`.
- P1-P6 regression: PASS.

## Audit deterministic seeds and IDs

Contrat attendu:

- Meme contexte versionne => memes candidats, memes ids, memes rejets.
- Seed differente => hash/distribution differents, budgets conserves.
- IDs preview stables et non officiels.

Preuve observee:

- Rapport: meme seed/version => meme hash, memes IDs, positions, tiers/richesses.
- Rapport: seed differente => distribution differente sous memes budgets.
- Rapport: IDs preview stables.
- Recu: Seed A hash `01b78336`, Seed B hash `fef6f1b4`.
- Recu: `DETERMINISTIC_SPAWN=PASS`.
- Recu: `SEED_VARIATION=PASS`.

Verdict:

- `P7_DETERMINISTIC_SEEDS_IDS=PASS`

## Audit R1-R3 and T1-T7 coverage

Contrat attendu:

- Ressources R1/R2/R3 inspectables.
- Bestiaire T1-T7 inspectable.
- T1-T4 restent duel/solo preview.
- T5-T7 restent raid preview.
- Aucun tier ne cree de loot officiel.

Preuve observee:

- Rapport: ressources R1/R2/R3 couvertes dans la preuve.
- Rapport: menaces T1-T7 couvertes dans la preuve.
- Rapport/recu: `official_gain=false`.
- Rapport/recu: `server=false`.

Verdict:

- `P7_R1_R3_T1_T7_COVERAGE=PASS`

Note:

- Le recu confirme la couverture R/T via le rapport principal, mais ne liste pas chaque entite/tier individuellement.

## Audit caps and density budgets

Contrat attendu:

- Fenetre active <= 25 chunks.
- Hives <= 25.
- Resources <= 75.
- Bestiary/threats <= 25.
- Le catalogue complet ne doit pas etre instancie en scene.

Preuve observee:

- Recu: active chunks / hives / resources / threats = `25/2/11/7`.
- Recu: `DENSITY_BUDGETS=PASS`.
- Rapport: aucun terrain 50x50.
- Rapport: Wave5 25x25, 625 tuiles preserves.

Verdict:

- `P7_CAPS_DENSITY_BUDGETS=PASS`

## Audit exclusion volumes

Contrat attendu:

- Exclusions BearDen/eau/falaise/evenement auditees.
- Les rejets doivent etre inspectables.
- Aucun spawn ne doit transformer un volume reserve en etat officiel.

Preuve observee:

- Rapport: exclusions diagnostiques BearDen, eau, falaise, evenement reserve.
- Recu: exclusion hits BearDen/water/cliff/event = `0/0/0/25`.
- Recu: `EXCLUSION_ZONES=PASS`.

Verdict:

- `P7_EXCLUSION_VOLUMES=PASS`

Note:

- Le recu prouve le gate global et les compteurs d'exclusion. Il ne detaille pas chaque candidat rejete.

## Audit distances minimales

Contrat attendu:

- Distances minimales entre entites evaluees de facon deterministe.
- Les collisions/doublons critiques doivent etre evites ou listes.

Preuve observee:

- Rapport: detail selection et distribution locale inspectable.
- Recu: density budgets PASS.
- Aucune ligne explicite ne detaille `hive-hive`, `hive-resource`, `resource-resource`, `bestiary-bestiary` ou les seuils de distance.

Verdict:

- `P7_MIN_DISTANCE_RULES=PASS_WITH_NOTES`

Note:

- Aucun signal de depassement n'apparait dans le rapport/recu. Pour un PASS strict futur, le recu devrait reporter un gate explicite de distance ou de chevauchement.

## Audit streaming, pooling, allocations

Contrat attendu:

- Pas de chargement du catalogue complet en scene.
- Pas d'allocation excessive pendant pan/zoom.
- Pools/scratch buffers privilegies.
- Pas de terrain 50x50.

Preuve observee:

- Rapport: aucun terrain 50x50, PNG terrain, master terrain, APK, serveur, remote, donnee reelle.
- Rapport: P1-P6 preserves.
- Recu: P1-P6 regression PASS.
- Recu: active window bornee a 25 chunks.

Verdict:

- `P7_STREAMING_POOLING_ALLOCATIONS=PASS_WITH_NOTES`

Note:

- Le recu ne donne pas de mesure d'allocations ou de cache. Il prouve l'absence de regression P1-P6 et le respect des budgets de densite.

## Audit reprojection 25x25 -> 50x50

Contrat attendu:

- La distribution doit rester compatible avec `world_coord_normalized`.
- Toute reprojection 25x25 -> 50x50 doit revalider exclusions et bornes.

Preuve observee:

- Rapport: detail selection affiche chunk et coordonnee normalisee.
- Rapport: aucun terrain 50x50.
- Aucune ligne explicite de recu ne prouve une reprojection 25x25 -> 50x50 P7.

Verdict:

- `P7_REPROJECTION_COMPATIBILITY=PASS_WITH_NOTES`

Note:

- P6 a deja prouve la reprojection scenario data. Pour P7 strict, ajouter un gate de spawn inspector dedie a la reprojection 50x50 logique.

## Audit local inspection UI

Contrat attendu:

- Interface d'inspection locale read-only.
- Affichage seed/hash/exclusions/budgets.
- Overlay diagnostic OFF par defaut.
- L'inspection ne modifie pas la distribution.

Preuve observee:

- Rapport: panneau `SPAWN INSPECTEUR`.
- Rapport: badge `LOCAL - APERCU NON OFFICIEL`.
- Rapport: overlay diagnostic OFF par defaut.
- Rapport: detail selection id, famille, type/tier, chunk, coordonnee normalisee.
- Recu: `SPAWN_INSPECTOR_UI=PASS`.
- Recu: `DIAGNOSTIC_OVERLAY_DEFAULT=OFF`.

Verdict:

- `P7_LOCAL_INSPECTION_UI=PASS`

## Audit no official client authority

Contrat attendu:

- Toute sortie locale porte `official=false`.
- Aucun gain officiel.
- Aucun serveur/remote.
- Aucun combat/loot/progression officiel cote client.

Preuve observee:

- Rapport: badge local non officiel.
- Rapport: bouton `Regenerer apercu local - Jamais officiel`.
- Rapport: generation preview locale sans serveur.
- Rapport/recu: `server=false`.
- Rapport/recu: `official_gain=false`.
- Gate: `SERVER_OR_OFFICIAL_GAIN=NO`.

Verdict:

- `P7_NO_CLIENT_OFFICIAL_AUTHORITY=PASS`

## Reserves techniques

Reserves non bloquantes:

- L'audit est documentaire et independant; il ne relance pas Unity.
- Le recu P7 ne detaille pas les distances minimales par paire.
- Le recu P7 ne detaille pas de mesure d'allocation/cache.
- Le recu P7 ne contient pas de gate explicite de reprojection 25x25 -> 50x50 pour le spawn inspector, meme si P6 a deja couvert la reprojection data.

## Gates

- `P7_DETERMINISTIC_SEEDS_IDS=PASS`
- `P7_R1_R3_T1_T7_COVERAGE=PASS`
- `P7_CAPS_DENSITY_BUDGETS=PASS`
- `P7_EXCLUSION_VOLUMES=PASS`
- `P7_MIN_DISTANCE_RULES=PASS_WITH_NOTES`
- `P7_STREAMING_POOLING_ALLOCATIONS=PASS_WITH_NOTES`
- `P7_REPROJECTION_COMPATIBILITY=PASS_WITH_NOTES`
- `P7_LOCAL_INSPECTION_UI=PASS`
- `P7_NO_CLIENT_OFFICIAL_AUTHORITY=PASS`

BUILDER_C_P7_AUDIT=PASS_WITH_NOTES

READY_FOR_NEXT=YES

Le `PASS_WITH_NOTES` autorise la suite: les gates critiques P7 sont PASS et l'autorite officielle client reste interdite. Les notes demandent seulement des receipts futurs plus granulaires sur distances, allocations/cache et reprojection P7 dediee.
