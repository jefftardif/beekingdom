# Outil de préflight local du candidat

`Server/tools/Test-CandidateLocalPreflight.ps1` vérifie un candidat sous `Server/artifacts/candidates` sans écrire dans celui-ci:

- chaque hash SHA-256 du manifeste;
- absence de PDB et de configuration Development;
- `ChatEnabled=false`, `RealtimeEnabled=false`, persistance `InMemory`;
- smoke Production loopback optionnel via `-RunSmoke`.

Exécution vérifiée sur `BeeKingdom.Server.20260721T195742Z`: 54 fichiers, 0 divergence, smoke `Healthy`, `DeploymentAuthorized=false`.
