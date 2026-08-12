# LivingHive — file officielle de construction

## Mini-contrat livré

Contrat `living-hive-building-upgrade-v1`, fermé par `BuildingUpgrades:Enabled=false` par défaut et en Production. Les routes sont authentifiées et cloisonnées par `PlayerId` du bearer :

- `GET /game/v1/hives/{hiveId}/building-upgrades`
- `POST /game/v1/hives/{hiveId}/building-upgrades/{buildingKey}/start`
- `POST /game/v1/hives/{hiveId}/building-upgrades/{operationId}/complete`

Le snapshot camelCase contient `playerId`, `hiveId`, `contractVersion`, `catalogVersion`, `revision`, `serverTimeUtc`, `balances`, `buildingLevels`, `offers` et `activeOperation`. Les requêtes start/complete contiennent seulement `expectedRevision` et `idempotencyKey`. Les réponses de mutation contiennent `receipt` et `snapshot`.

Le catalogue initial fermé et injectable ne contient que `wax_workshop` niveau 1→2. Les tests utilisent une définition explicitement factice (20 miel + 60 pollen, une heure); elle n’est pas présentée comme économie live. Le serveur choisit toujours coût, durée et niveau suivant. Le débit et la création de l’opération sont atomiques; la complétion applique le niveau sans crédit artificiel. Une seule opération de construction est active à la fois.

Les codes d’erreur sont `game.session_required`, `game.invalid_request`, `game.revision_conflict`, `game.idempotency_conflict`, `game.construction_busy`, `game.insufficient_resources`, `game.not_ready`, `game.operation_not_found` et `game.already_completed`. Le flag fermé renvoie `503 game.unavailable` avant authentification ou lecture.

La file réutilise l’état durable `PlayerHiveState.Operations` avec `HiveOperationKind.BuildingUpgrade` et les reçus persistants existants; aucune colonne SQL et aucune migration de modèle supplémentaire ne sont nécessaires. Les repositories JSON et SQL sérialisent donc la même structure StateJson. Cette réutilisation est explicitement limitée à la file de construction et ne modifie pas les autres kinds.

## Preuves locales

- `BuildingUpgradeServiceTests`: **4/4**.
- `BuildingUpgradeEndpointTests`: **2/2**.
- Correctif d’interop : les offres sont filtrées après passage au niveau 2; les reçus de rejeu conservent clé, code, opération, niveaux, révision et horodatage; le conflit de révision est `game.revision_conflict`; `Running` et `AwaitingCollection` sont complétables après échéance; aucune collecte ne crédite de ressource.
- Après correction : service + HTTP ciblés **6/6**.
- Suite complète `BeeKingdom.Tests` net10.0 : **321 réussis, 0 échec, 8 ignorés, 329 total**.
- Build Release serveur : **0 erreur**; avertissements existants de référence `Microsoft.Data.SqlClient` et un avertissement nullable préexistant dans `Program.cs`.

Les scénarios couvrent fermeture, authentification, snapshot/catalogue, coût serveur, débit atomique, slot occupé, idempotence/rejeu, conflit de clé, complétion avant/après l’heure serveur et passage du niveau 1 au niveau 2.

## Fichiers exacts

- `Server/src/BeeKingdom.HiveOperations/BuildingUpgradeContracts.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.Tests/BuildingUpgradeServiceTests.cs`
- `Server/tests/BeeKingdom.Tests/BuildingUpgradeEndpointTests.cs`
- `Docs/ProductionIntegration/LivingHive_BuildingUpgrade_ServerAuthority_2026-07-22.md`

## Frontière appareil/serveur et portes ouvertes

Le bootstrap mobile est désormais raccordé à ce contrat, mais le mobile ne fournit ni coût, durée, niveau, horloge, solde ni résultat; il conserve seulement un brouillon et un cache de lecture. `DeploymentAuthorized=false` reste inchangé. Aucun Asset, module chat, candidat, transfert, staging ou déploiement n’a été touché. Le staging/auth réel, la décision économique finale, la preuve SQL jetable et l’activation restent ouverts.
