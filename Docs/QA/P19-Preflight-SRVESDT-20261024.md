# BeeKingdom — Contrôle P19 sur SRVESDT (24 octobre 2026)

## Résultat

**BLOQUÉ — déploiement P19 non autorisé.** Prévalidation exécutée directement sur SRVESDT avec `Verify-P19ReadOnly.ps1`. Le script n'effectue que des lectures HTTP, disque et SQL; il a enregistré le rapport local `C:\BeeKingdom\staging\pollination-P19-readiness\p19-readiness.json`.

| Contrôle | Résultat |
|---|---|
| Pool IIS BeeKingdomApi | Démarré |
| `/health` | HTTP 200 |
| Binaires Server/HiveOperations | SHA-256 identiques aux artefacts P18 vérifiés |
| Sauvegarde des binaires P18 | Présente |
| Routes florales et population non authentifiées | HTTP 401 (conforme) |
| Expéditions et projets désactivés | HTTP 503 (conforme) |
| `CivilPopulation.Enabled` / `ReadOnlyPilot` | true / true |
| `CivilExpeditions.Enabled` | false |
| `FloralAllianceProjects.Enabled` | false |
| Lecture SQL production par l'identité Valoraxo Remote | **Non autorisée** |
| Preuve de restauration sauvegarde SQL BeeKingdom | **Non vérifiée** |

### Blocages retournés automatiquement

- `SQL_READ_NOT_AUTHORIZED_FOR_CURRENT_IDENTITY`
- `SQL_PRODUCTION_BACKUP_RESTORE_NOT_VERIFIED`

Le diagnostic officiel `/ops/readiness` est protégé (HTTP 401 sans l'authentification d'administration requise). Le fournisseur SQL effectif de l'application doit être vérifié par un opérateur autorisé : la valeur du fichier JSON versionné ne suffit pas à en déduire la valeur d'exécution IIS.

## Vérifications complémentaires

- Une requête SQL **SELECT uniquement**, dans `Server/ops/pollination-production-preflight/P19-DatabaseReadOnly.sql`, vérifie 7 tables et 2 index spécifiques aux migrations 096–098. Exécution sur la base QA isolée `BeeKingdomPollinationQA`, avec adaptation de la garde de nom de base : **9 contrôles sur 9 réussis**, 0 mission échue.
- Deux scénarios d'intégration SQL sur la branche unifiée : `SQL_QA_PASS` (transactions, concurrence, rollbacks, index d'échéances) et `P4_SQL_QA_PASS` (2 membres, anti-solo, expérience d'alliance idempotente). Les deux ont confirmé leur nettoyage final.
- Suite complète métier sur la branche unifiée : **328/328 tests réussis**.
- Tests Unity Editor sur la reconnaissance d'alliance : **11/11 réussis** lors de la reprise précédente. L'interface a été vérifiée visuellement avec **[BKT] Alliance Test**.
- La suite API générale lancée lors de cette reprise n'avait pas encore produit son bilan final au moment de ce rapport. Ne pas l'annoncer comme réussie sans preuve.

## Garde de mise en production

Les scripts placés dans `C:\BeeKingdom\staging\pollination-P19-readiness` sont des diagnostics, **pas une activation**. Aucun drapeau, exécutable, table, sauvegarde ou donnée joueur n'a été changé pendant le contrôle.

Pour passer à P19 : un opérateur SQL autorisé doit vérifier les objets et la file d'échéances en production, valider une sauvegarde SQL récupérable, consulter les diagnostics ops authentifiés, puis démontrer le règlement automatique et la recette multijoueur avant toute bascule des drapeaux. Ne pas utiliser les identifiants d'autres services ni contourner les permissions SQL.
