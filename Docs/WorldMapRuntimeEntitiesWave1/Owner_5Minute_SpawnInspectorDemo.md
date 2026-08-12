# Bee Kingdom - Owner 5 Minute Spawn Inspector Demo

Date locale: 2026-07-15

## Cadre

- Demo locale uniquement.
- Aucun serveur, remote, APK, gain officiel, donnee reelle ou terrain 50x50.
- Scene: `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity`
- Panneau: `SPAWN INSPECTEUR`

## Parcours 5 minutes

### 0:00-0:45 - Ouvrir l'inspecteur

- Montrer `SPAWN INSPECTEUR`.
- Verifier le badge `LOCAL - APERCU NON OFFICIEL`.
- Confirmer que l'overlay diagnostic est OFF par defaut.
- Activer l'overlay seulement pour inspection locale.

### 0:45-1:45 - Seed A deterministe

- Entrer ou conserver le seed local.
- Cliquer `Regenerer apercu local - Jamais officiel`.
- Montrer les compteurs: chunks, ruches, ressources, menaces.
- Montrer le detail d'une entite: id, famille, type/tier, chunk, coordonnee normalisee.

### 1:45-2:40 - Meme seed, meme distribution

- Regenerer avec le meme seed/version.
- Comparer le hash: il reste identique.
- Expliquer que les IDs et positions sont stables pour le meme contexte versionne.

### 2:40-3:30 - Seed B

- Changer la seed.
- Regenerer.
- Montrer que le hash change mais que les budgets restent valides.

### 3:30-4:20 - Exclusions

- Activer l'affichage d'exclusions.
- Montrer que ce sont des contours/diagnostics, jamais une peinture terrain.
- BearDen reste separe et preserve.

### 4:20-5:00 - Verdict

- Montrer le recu P7.
- Rappeler:
  - `server=false`
  - `official_gain=false`
  - `DIAGNOSTIC_OVERLAY_DEFAULT=OFF`
  - `P1_P6_REGRESSION=NO`

## Preuves

- Rapport: `C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave1\SpawnInspectorIntegration_Report.md`
- Recu: `C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapRuntimeEntitiesWave1\SpawnInspectorProof\SpawnInspectorProofReceipt.md`

## Verdict

READY_FOR_OWNER_SPAWN_INSPECTOR_TEST=YES
