# World Map Step5A - Unity Import Payload

## Statut

Le payload precache est disponible hors Unity:

`C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_UnityImportPayload_staging\`

Il contient exactement 25 PNG runtime `516x516 RGB`, de `R0C0_g2.png` a `R4C4_g2.png`, ainsi que leurs manifests, inventaire et preflight.

Payload ID:

`step5a-uib-wave3-continuous-v1-f458571e4e2de481`

Digest de l'arbre verrouille, hors fichier de preflight:

`377ca038ad9364cd194d49319f5ddd45136b43ae613d6a542b6548948d21b823`

## Validation

```powershell
& 'C:\Users\Utilisateur\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' `
  'Docs\BuilderB\WorldMapStep5AImportPayload\build_step5a_import_payload.py' verify
```

La verification est en lecture seule. Le resultat attendu est `status = PASS`, `checks_failed = 0`.

Le mode `build` refuse de s'executer si le dossier payload existe deja. Toute evolution doit recevoir un nouveau dossier/version; le lot actuel ne doit pas etre modifie en place.

## Fichiers utiles pour Builder-A

- `payload.lock.json`: contrat complet et hashes 25/25;
- `preflight.result.json`: resultat machine-readable PASS;
- `source-to-future-destination.csv`: copie future exacte sans aucune copie deja effectuee;
- `source.handoff.unity.json`: manifest Unity de handoff a adapter/copier plus tard;
- `tiles/`: 25 PNG runtime precaches.

## Non-claims

- aucune copie sous `Assets`;
- aucun import Unity;
- aucune scene ou `.meta` creee;
- aucun `ProjectSettings` modifie;
- aucun Repeat ou remplissage modulo 64x64;
- aucune carte live ou serveur live.
