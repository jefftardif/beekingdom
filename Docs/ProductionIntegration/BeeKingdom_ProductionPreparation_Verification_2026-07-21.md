# Vérification de préparation production — 2026-07-21

## Portée

Contrôle local uniquement, sans synchronisation, déploiement ni activation du
chat. Le périmètre reste `Server/` et `Docs/ProductionIntegration/`.

## Preuves exécutées

| Contrôle | Résultat |
|---|---|
| Prévalidation du candidat `BeeKingdom.Server.20260721T201425Z` | Réussie |
| Smoke du binaire candidat en mode Production local | Healthy (`127.0.0.1:5127`) |
| Fichiers du manifeste | 54 |
| Écarts de hachage | 0 |
| Configuration chat | `ChatEnabled=false`, `RealtimeEnabled=false` |
| Autorisation de déploiement | `false` |
| Configuration production fail-closed | Réussie |
| Suite serveur `BeeKingdom.Tests` (cible locale net10.0 de compatibilité) | 250 réussis, 0 échec, 7 SQL ignorés |
| Fournisseur de persistance local | `InMemory` (SQL externe requis pour staging) |
| Protocole | `chat-v1` |
| Rétention annoncée | 30 jours |

Le candidat courant demeure `local-validation-only`. Les portes SQL jetable,
.NET 8, TLS/SNI/IIS et Android staging ne sont pas levées par cette vérification;
aucune conclusion de disponibilité publique ne doit en être déduite.

## Recontrôle environnemental

Le recontrôle local ne détecte toujours aucun service SQL Server/LocalDB et la
VM expose uniquement les runtimes .NET 10.0.10. Les tests SQL et l'exécution
native net8 restent donc explicitement différés vers l'environnement prévu.

La cible native `net8.0` compile, mais son exécution n'est pas disponible sur
cette VM faute de runtime .NET 8. La suite a donc été exécutée sur la cible
`net10.0` explicitement prévue par le projet, avec les sept tests SQL externes
ignorés conformément au garde-fou local.

## Fichiers contrôlés

- `Server/artifacts/candidates/CANDIDATE-STATUS.json`
- `Server/tools/Test-CandidateLocalPreflight.ps1`
- `Server/tools/Test-ProductionConfiguration.ps1`
- `Server/artifacts/candidates/BeeKingdom.Server.20260721T201425Z/manifest.json`

## Décision

Aucun changement runtime ni reconstruction de candidat n'est nécessaire pour ce
contrôle. Le candidat reste conservé localement, sans transfert ni activation.
