# Bee Kingdom — Server Authority Audit

Date : 2026-08-09  
Scope : `Assets/`, `Server/src/`, `Server/tests/`  
Objectif : identifier la source de vérité réelle avant une version jouable persistante.

## Verdict global

Le projet possède une tranche serveur officielle, mais elle n'est pas encore la source de vérité globale du produit.

- Le client contient trois couches concurrentes : legacy `Assets/_Project/Scripts`, preview `Assets/BeeKingdom` et clients REST officiels.
- La configuration production actuelle utilise `Persistence:InMemory` et garde la plupart des fonctionnalités officielles désactivées.
- Le serveur SpeedUp existe maintenant dans le dépôt, mais reste désactivé par défaut et n'est pas déployé sur `chat.dravii.com`.
- Les systèmes non couverts par une API officielle doivent rester explicitement en preview; ils ne doivent pas fusionner leurs `PlayerPrefs` dans l'état serveur.

Références de configuration : `Server/src/BeeKingdom.Server/appsettings.Production.json:23-169`, `Server/src/BeeKingdom.Server/Program.cs:295-326`.

## Matrice d'autorité


## Persistence et reconnect

L'état officiel est un aggregate `PlayerHiveState` persisté par `DurableJsonHiveStateRepository` ou `SqlHiveStateRepository`. Les tables SQL de queues/receipts ne sont pas la source active : l'aggregate JSON reste utilisé.

Les clients officiels ne sont pas homogènes : Chat possède cache/outbox et replay, tandis que plusieurs clients Construction/Research/Combat/Gathering ne possèdent qu'un cache de lecture ou aucun outbox. Une mutation ambiguë peut donc rester inconnue du client après déconnexion.

## Migrations réalisées pendant cet audit

- Initialisation explicite de `PlayerHiveState.SpeedUps` dans le nouvel état serveur.
- Validation persistante des quantités SpeedUp dans `HiveStateMigrator`.
- Contrat `SpeedUpOptions` et catalogue serveur.
- `SpeedUpInventoryService` avec transaction repository, révision, idempotence et handlers Construction/Research/Training/Healing/Manufacturing/Universal.
- Routes `GET /game/v1/hives/{hiveId}/speedups`, `POST /game/v1/hives/{hiveId}/speedups/apply` et façade catégorie.
- Réponse serveur contenant inventaire, timers, rewards et events, prête pour le settlement ultérieur.
- Tests service d'application, clamp timer, idempotence et appels concurrents : `SpeedUpInventoryServiceTests` 3/3.

## Migrations réalisées après cet audit (Sprint-027)

- Ledger Rewards serveur implémenté : `RewardLedgerState` (entries, events, markers de settlement, receipts) persistant dans `PlayerHiveState`, normalisé et validé par `HiveStateMigrator` (bornes : 512 entries, 64 events, 512 markers, 256 receipts).
- `RewardLedgerService` : octroi idempotent (revision + receipt), recensement des complétions de files (`queue_completed`, une fois par OperationId via `SettledOperationIds`), lecture avec settlement, snapshot `Rewards`/`Events` effectif.
- Les octrois écrivent la récompense claimable (`Rewards`) ET l'entrée de ledger dans la même mutation atomique ; la réclamation existante (`HiveOperationService.ClaimRewardAsync`) synchronise l'entrée (Claimed/CreditedAmount/ClaimedAtUtc) et append `reward_claimed`.
- Les collections `Rewards`/`Events` de la réponse SpeedUp sont désormais remplies depuis le ledger (fini les points d'extension vides).
- Routes : `GET /game/v1/hives/{hiveId}/rewards` (jeu, gaté par `RewardLedger.Enabled`) et `POST /admin/v1/players/{playerId}/hives/{hiveId}/rewards/grant` (admin, gaté par `AdminSupport`).
- Configuration `RewardLedger` ajoutée aux appsettings (désactivée par défaut, cohérent avec `SpeedUps.Enabled=false`).
- Tests : `RewardLedgerServiceTests` 7/7 (octroi, replay idempotent, revision conflict, réclamation synchronisée, settlement unique, snapshot SpeedUp, rejet invalid).

## Écarts restant volontairement bloqués

- `SpeedUps.Enabled` reste `false` par défaut : aucun faux état live n'est activé.
- Le ledger Rewards est désormais effectif, mais reste désactivé par défaut (`RewardLedger.Enabled=false`) tant que le branchement client et le déploiement ne sont pas réalisés.
- Le client Unity n'appelle pas encore ces routes : ce sera le branchement du sprint suivant.
- Healing et Manufacturing réutilisent des modèles de timer existants, mais leurs contrats métier/récompenses spécifiques doivent encore être validés par le backend produit.
- Le déploiement SQL et `chat.dravii.com` n'a pas été effectué dans ce workspace.

## Risques bloquants avant production

1. Production configurée avec `Persistence:InMemory`.
2. Route production, backup et rollback non prouvés.
3. Comptes/session/token désactivés dans `appsettings.Production.json`.
4. Inventaire générique non centralisé (les récompenses le sont désormais via le ledger).
5. Quêtes, achievements, alliance, boutique et mail sans API serveur.
6. Échecs serveur encore présents : `BeeKingdom.HiveOperations.Tests` passe 178/178; la suite `BeeKingdom.Tests` exécutée le 2026-08-09 passe 377/377 (8 tests SQL ignorés par design). Un échec flaky observé une fois sur `ChatMessagingEndpointTests.ChatRequestBodyLimitAcceptsExactBytesAndRejectsTheNextByte` (binding de corps Chat à 65 536 octets), non reproductible en isolation ni sur deux exécutions complètes consécutives, sans lien avec la mutation SpeedUp/ledger.

## Conclusion

Le serveur possède désormais une fondation SpeedUp testable, mais l'audit ne permet pas de qualifier BeeKingdom de jeu entièrement server-authoritative. Toute fonctionnalité non listée comme serveur dans la matrice doit rester preview-only et ne doit pas être présentée comme progression persistante officielle.
