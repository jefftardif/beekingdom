# Handoff Unity World Map Wave 3

## Statut

Ce dossier est un support d'integration uniquement. Il ne contient aucune integration Unity, aucune copie de PNG sous `Assets` et aucune validation runtime/live.

L'execution par Builder-A est conditionnee a deux gates:

1. validation Builder-C du bundle runtime a gouttieres;
2. autorisation QA de l'integration Unity.

## Livrables

- `WorldMapWave3_RuntimeTileUnityHandoff.manifest.json`: contrat machine-readable des 25 tuiles runtime;
- `WorldMapWave3_SourceDestinationInventory.csv`: inventaire exact source vers destination future;
- `WorldMapWave3_UnityIntegrationProcedure.md`: import, mapping, chargement, anti-coutures et rollback;
- `WorldMapWave3_BuilderASelfChecks.md`: matrice de controles apres integration;
- `WorldMapWave3_HandoffValidation.json`: resultat des controles locaux du handoff;
- `generate_handoff_manifest.py`: regeneration deterministe depuis le bundle `run1`;
- `verify_handoff.py`: verification des hashes, UV, dimensions et sources Unity en lecture seule;
- `BuilderB_WorldMapWave3UnityIntegrationHandoff_Report.md`: rapport fallback en francais.

## Regeneration

Depuis la racine du projet:

```powershell
& 'C:\Users\Utilisateur\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'Docs\BuilderB\WorldMapWave3UnityIntegrationHandoff\generate_handoff_manifest.py'

& 'C:\Users\Utilisateur\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'Docs\BuilderB\WorldMapWave3UnityIntegrationHandoff\verify_handoff.py'
```

Le generateur refuse un master, une version, un ordre, un nombre de tuiles, une dimension ou un hash non conforme. Il n'ecrit que dans ce dossier de documentation.

## Non-claims

- handoff prepare, pas integre;
- aucune scene ou ressource Unity modifiee;
- aucune validation Android ou device realisee ici;
- aucun monde immense/live livre;
- aucun serveur officiel;
- aucun deplacement terrestre: les vols restent aeriens et independants du decor.
