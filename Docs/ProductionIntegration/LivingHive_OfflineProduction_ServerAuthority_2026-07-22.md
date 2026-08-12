# LivingHive — autorité serveur de production hors absence

## Objet et frontière

Cette tranche fournit la base serveur de production manuelle après absence. Le serveur est l’unique autorité pour l’horloge UTC, les taux, capacités, pending, soldes, révisions et reçus. L’appareil peut seulement conserver un cache de lecture protégé et préparer une demande de collecte; il ne fournit aucun montant, taux, capacité, durée ou heure faisant autorité.

La fonctionnalité reste fermée par défaut et en Production (`HiveOfflineProduction:Enabled=false`). Aucun candidat, déploiement ou activation n’a été réalisé.

## Contrat HTTP

`GET /game/v1/hives/{hiveId}/offline-production` exige un bearer valide et renvoie une enveloppe `OfflineProductionReadSnapshot` camelCase. `POST /game/v1/hives/{hiveId}/offline-production/{buildingKey}/collect` accepte uniquement `{expectedProductionRevision,idempotencyKey}`.

Exemple GET:

```json
{"playerId":"11111111-1111-1111-1111-111111111111","hiveId":"22222222-2222-2222-2222-222222222222","contractVersion":"living-hive-offline-production-v1","catalogVersion":"test-v1","productionRevision":0,"serverTimeUtc":"2026-07-22T12:00:00+00:00","productionAsOfUtc":"2026-07-22T12:00:00+00:00","maxRecognizedDuration":"02:00:00","lines":[{"buildingKey":"honey_storage","resourceKey":"honey","pendingAmount":20,"hourlyRate":10,"capacity":1000000000,"collectableWholeUnits":20},{"buildingKey":"wax_workshop","resourceKey":"wax","pendingAmount":10,"hourlyRate":5,"capacity":1000000000,"collectableWholeUnits":10},{"buildingKey":"warehouse_cells","resourceKey":"pollen","pendingAmount":16,"hourlyRate":8,"capacity":1000000000,"collectableWholeUnits":16}],"balances":{"honey":{"amount":11,"capacity":100},"wax":{"amount":12,"capacity":100},"pollen":{"amount":13,"capacity":100}}}
```

Exemple requête de collecte:

```json
{"expectedProductionRevision":0,"idempotencyKey":"collect-1"}
```

Exemple succès (et réponse de rejeu identique):

```json
{"receipt":{"playerId":"11111111-1111-1111-1111-111111111111","hiveId":"22222222-2222-2222-2222-222222222222","idempotencyKey":"collect-1","buildingKey":"honey_storage","resourceKey":"honey","creditedAmount":1,"remainingPending":0.5,"productionRevision":1,"serverTimeUtc":"2026-07-22T12:00:00+00:00","resultingBalance":{"amount":12,"capacity":100}},"snapshot":{"playerId":"11111111-1111-1111-1111-111111111111","hiveId":"22222222-2222-2222-2222-222222222222","contractVersion":"living-hive-offline-production-v1","catalogVersion":"test-v1","productionRevision":1,"serverTimeUtc":"2026-07-22T12:00:00+00:00","productionAsOfUtc":"2026-07-22T12:00:00+00:00","maxRecognizedDuration":"02:00:00","lines":[{"buildingKey":"honey_storage","resourceKey":"honey","pendingAmount":0.5,"hourlyRate":10,"capacity":1000000000,"collectableWholeUnits":0},{"buildingKey":"wax_workshop","resourceKey":"wax","pendingAmount":0,"hourlyRate":5,"capacity":1000000000,"collectableWholeUnits":0},{"buildingKey":"warehouse_cells","resourceKey":"pollen","pendingAmount":0,"hourlyRate":8,"capacity":1000000000,"collectableWholeUnits":0}],"balances":{"honey":{"amount":12,"capacity":100},"wax":{"amount":12,"capacity":100},"pollen":{"amount":13,"capacity":100}}}}
```

Exemple erreur:

```json
{"code":"game.resource_capacity_full","message":"game.error.conflict","retryAfterSeconds":null}
```

Codes: `game.session_required` (401), `game.invalid_request` (400), `game.production_conflict`, `game.idempotency_conflict`, `game.resource_capacity_full` et `game.production_not_ready` (409), `game.unavailable` (503). Les réponses authentifiées de lecture portent `Cache-Control: private, no-store` et `Pragma: no-cache`.

## Règles et persistance

L’accrual est atomique, plafonné à la durée configurée (au plus 7 jours), conserve les fractions décimales et ne change pas `productionRevision`. Une collecte crédite `min(floor(pending), capacité restante)`, incrémente la révision une fois et persiste un reçu dédié. Le même couple clé/charge rejoue exactement la réponse persistée, même après reconstruction; une charge différente sous la même clé est un conflit sans mutation. Les reçus sont bornés à 512 avec éviction déterministe du plus ancien.

L’état est dans `PlayerHiveState.StateJson`; `HiveStateMigrator` migre vers `ModelVersion=10`. Aucune colonne SQL dédiée n’est ajoutée. Le catalogue (`test-v1` dans les tests) est injecté et validé; aucune économie live officielle n’est choisie dans cette tranche.

## Preuves

- Suite complète `BeeKingdom.Tests` net10.0 avec `DOTNET_ROLL_FORWARD=Major`: **315 réussis, 0 échec, 8 ignorés, 323 total**.
- `HiveOfflineProductionServiceTests`: **25/25**.
- `HiveOfflineProductionEndpointTests`: **8/8**.
- SQL opt-in `SqlHiveStateRepositoryRoundTripsOfflineProductionReceiptAndReplay`: **0 réussi, 0 échec, 1 ignoré**, chaîne `BEE_SQL_INTEGRATION_CONNECTION_STRING` absente.
- Build Release serveur: **0 erreur, 1 avertissement MSB3277** (conflit de versions Microsoft.Data.SqlClient 5/6 existant).

## Fichiers de la tranche

- `Server/src/BeeKingdom.HiveOperations/HiveOfflineProductionSnapshot.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOfflineProductionService.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.Tests/HiveOfflineProductionServiceTests.cs`
- `Server/tests/BeeKingdom.Tests/HiveOfflineProductionEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/SqlServerOptInIntegrationTests.cs`
- `Docs/ProductionIntegration/LivingHive_OfflineProduction_ServerAuthority_2026-07-22.md`

## Portes restantes

Les valeurs économiques finales doivent être décidées et injectées par configuration auditée. La preuve SQL jetable reste conditionnée à un LocalDB autorisé; le staging TLS/IIS et le raccordement du client mobile authentifié restent à effectuer. Les drapeaux et `DeploymentAuthorized` demeurent faux.
