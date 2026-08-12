# Bee Kingdom — Server Sync Audit

Date : 2026-08-09  
Scope : `Assets/BeeKingdom`, `Server/src/`, `Server/tests/`  
Objectif : qualifier, système par système, ce qui est réellement synchronisé avec le serveur officiel et ce qui reste preview local.

## Verdict global

Le serveur est l'autorité pour les timers, quantités, récompenses, événements et idempotence. Le client Unity est désormais branché sur les routes SpeedUp/ledger via `HiveSpeedUpClient` / `HiveRewardLedgerClient` et le bloc « Serveur officiel » de `HiveViewProductUiPresenter` (décision : panneaux officiels + démo conservée en PREVIEW tant que le serveur ne peut pas octroyer d'items SpeedUp). Rien de ce qui n'a pas d'API officielle ne doit être présenté comme progression persistante.

## Matrice de synchronisation

| Système client | API officielle | Source de vérité | Statut |
| --- | --- | --- | --- |
| Building upgrade (start/complete) | `POST /game/v1/hives/{hiveId}/building-upgrades/...` | Serveur (`BuildingUpgradeContracts`) | Serveur |
| Research (start/complete) | `POST /game/v1/hives/{hiveId}/research/...` | Serveur (`HiveOperationService.ResearchCatalog`) | Serveur |
| Daily round (claim) | `POST /game/v1/hives/{hiveId}/daily-round/claim` | Serveur | Serveur |
| Offline production (collect) | `POST /game/v1/hives/{hiveId}/offline-production/...` | Serveur | Serveur |
| Milestone event | `GET/POST /game/v1/hives/{hiveId}/milestone-event...` | Serveur | Serveur (disabled par défaut) |
| SpeedUps (inventaire/timers) | `GET/POST /game/v1/hives/{hiveId}/speedups...` | Serveur (`SpeedUpInventoryService`) | Serveur ; `HiveSpeedUpClient` + bloc « Serveur officiel » branchés (application idempotente) |
| Rewards/Events (ledger) | `GET /game/v1/hives/{hiveId}/rewards` | Serveur (`RewardLedgerService`) | Serveur ; `HiveRewardLedgerClient` + bloc récompenses branchés (lecture seule) |
| VIP, Champion Bees, Troop tiers | routes `/game/v1/hives/{hiveId}/vip` etc. | Serveur | Serveur |
| Combat (doctrine, recruitment, patrol, sortie) | routes `/game/v1/...` | Serveur | Serveur |
| Inventaire démo SpeedUp (client) | aucune | Client (`PlayerPrefs`) | **PREVIEW** — conservé tant que le serveur ne peut pas octroyer d'items ; `HiveViewProductUiPresenter.EnsureSpeedUpDemoInventorySeeded` à supprimer lors du branchement complet |
| Quêtes, achievements, alliance, boutique, mail | aucune | Client | **PREVIEW** — jamais de fusion PlayerPrefs dans l'état serveur |

## Ledger Rewards (nouveau)

- `RewardLedgerState` : entries (source, ressource, montant, credited, claimed, notification), events (key, target, at), markers de settlement par OperationId, receipts idempotence. Bornes : 512/64/512/256, validées par `HiveStateMigrator`.
- Octroi : `GrantAsync` — atomic, idempotent, revision check ; écrit la récompense claimable + l'entrée de ledger + l'événement `reward_granted` dans la même mutation.
- Réclamation : `HiveOperationService.ClaimRewardAsync` — crédit capacité-borné, synchronise l'entrée (Claimed, CreditedAmount, ClaimedAtUtc) et append `reward_claimed`.
- Settlement : `RewardLedgerService.Settle` — recense chaque file passée `AwaitingCollection` exactement une fois (`queue_completed`), appelé sur les lectures SpeedUp et ledger.
- Exposé : `SpeedUpReadSnapshot.Rewards` (clés claimables) et `.Events` (`event:target`) ; route `GET /game/v1/hives/{hiveId}/rewards` ; route admin de grant.
- Gates : `RewardLedger.Enabled=false` et `SpeedUps.Enabled=false` par défaut ; aucune route dev exposée.

## Tests

- `RewardLedgerServiceTests` 7/7 : octroi + entries/events, replay idempotent, revision conflict, réclamation synchronisée, settlement unique, snapshot SpeedUp rempli, rejet invalid.
- `BeeKingdom.HiveOperations.Tests` : 178/178.
- `BeeKingdom.Tests` : 377/377 (8 tests SQL ignorés par design). Un flake observé une fois sur le test de limite de corps Chat (65 536 octets), non reproductible en isolation ni sur deux runs complets consécutifs, sans lien avec le ledger.

## Bloqueurs avant activation

1. ~~Brancher le client Unity aux routes SpeedUp/ledger~~ — fait (bootstrap + presenter, décision « officiel + démo conservée ») ; `EnsureSpeedUpDemoInventorySeeded` reste PREVIEW à supprimer lors du branchement complet (octroi d'items via le serveur).
2. ~~Construire l'artefact serveur~~ — fait : `Server/artifacts/BeeKingdom.Server/sprint-027-2026-08-10` (0 erreur, 1 warning préexistant `Program.cs:229`). Validation locale complète bloquée : aucun SQL Server local (la suite xUnit couvre la logique ; smoke test HTTP à faire au déploiement).
3. Fournir l'accès de déploiement `chat.dravii.com` (aucun accès fourni à ce jour) pour activer `SpeedUps.Enabled` et `RewardLedger.Enabled` en production.

## Note environnement (Unity EditMode)

La suite complète de tests EditMode (6000.5.3f1, batchmode) se fige de façon déterministe (~20-25 min dans le run, CPU→0, log figé) après `MobileAccountSessionClientTests`. Diagnostic : les fixtures concernées passent en isolation (`MobileAccountSessionClientTests` 25/25, `MultiAgentCoordinationFrameworkTests` 4/4) ; aucun test ne référence le code du Sprint-027 (grep = 0 hit) ; rejoué **sans** le paquet `com.ivanmurzak.unity.mcp` (manifest temporairement modifié puis restauré) : gel identique → ni le Sprint-027, ni le paquet MCP n'en sont la cause. Hang préexistant/environnemental, lié à l'ordre de la suite. La suite xUnit serveur est verte (178/178 + 377/377). À ré-investiguer séparément (ordre des classes, upgrade Unity 6000.5, run par tranches via `-testFilter`).
