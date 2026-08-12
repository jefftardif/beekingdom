# LivingHive — soin autoritaire du couvain

Contrat fermé `living-hive-brood-vitality-v1`. Les routes authentifiées sont :

- `GET /game/v1/hives/{hiveId}/brood/vitality` (snapshot avec `playerId`, `hiveId`, `contractVersion`, `serverTimeUtc`, `globalRevision`, `vitality` nullable) ;
- `POST /game/v1/hives/{hiveId}/brood/vitality/care/start?type=feeding|stabilization` ;
- `POST /game/v1/hives/{hiveId}/brood/vitality/care/{operationId}/complete`.

Les corps de mutation sont `{ expectedRevision, idempotencyKey }`. Le serveur dérive
les coûts, durées, horloge UTC, identité et gains : `feeding` débite 300 miel,
dure 12 s et ajoute 22 nutrition ; `stabilization` débite 45 cire, dure 13 s et
ajoute 7 stabilité. Aucun gain avant l'échéance et les valeurs restent bornées à
0..100.

Les reçus dédiés `BroodCareReceipts` sont persistés dans `PlayerHiveState`, limités
à 128 avec éviction déterministe. Le hash inclut la commande et la révision ; une
reprise avec même clé/charge renvoie exactement le reçu original, même après
disparition de l'opération, tandis qu'une charge différente renvoie
`game.idempotency_conflict` sans mutation. Les états sont validés par le migrateur
(UTC, portée joueur/ruche, révisions, hash hexadécimal, bornes).

Fichiers modifiés/ajoutés :

- `Server/src/BeeKingdom.HiveOperations/BroodVitalityCareService.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/BroodVitalityCareTests.cs`
- `Server/tests/BeeKingdom.Tests/BroodVitalityEndpointTests.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveOperationServiceTests.cs`

Preuves finales exécutées avec découverte réelle après revue croisée Architecte :
tests ciblés BroodVitality 9/9 ; suite HiveOperations 69/69 ; tests HTTP
BroodVitality 4/4 ; suite serveur net10 338 réussis, 0 échec, 8 SQL ignorés ;
build Release serveur 0 erreur, 1 avertissement de conflit
Microsoft.Data.SqlClient déjà existant. Les tests ajoutés après la première
remise couvrent notamment la complétion HTTP après avance de l'horloge, le rejeu
exact du reçu final, l'absence de double débit/gain, l'isolation joueur, le fait
Ronde, le bornage 128 et les gardes du migrateur. Journaux :

- `Artifacts/BroodCareServerHiveOperationsFinal.log`
- `Artifacts/BroodCareServerNet10Final.log`
- `Artifacts/BroodCareServerBuildFinal.log`

`BroodVitality:Enabled=false` reste la configuration par défaut et Production.
SQL natif, staging, activation économique, candidat et déploiement restent fermés.
Unity/Assets/chat n'ont pas été modifiés. Aucun processus `dotnet`/`testhost` ne
reste actif en fin de validation.
