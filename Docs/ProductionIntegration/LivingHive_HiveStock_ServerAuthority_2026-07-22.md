# LivingHive — snapshot Sac & stocks serveur

Contrat `living-hive-stock-v1`, lecture authentifiée `GET /game/v1/hives/{hiveId}/hive-stock`.
Le snapshot contient `playerId`, `hiveId`, `contractVersion`, `catalogVersion`, `revision`, `serverTimeUtc`, les trois ressources `honey/wax/pollen` avec `amount/capacity`, `population/populationCapacity` null faute d’agrégat autoritaire, les recherches terminées et les engagements actifs bornés.

`HiveStockSnapshot:Enabled=false` et `CatalogVersion=""` restent fermés par défaut et en Production. Un état invalide renvoie `503 game.unavailable`; absence de session `401 game.session_required`; ruche absente `404 game.hive_not_found`. Les réponses authentifiées restent privées et sans cache partagé via la politique existante.

Quand `HiveDailyRound:Enabled=true`, une lecture réussie marque `SnapshotRead` de façon idempotente; aucune ressource n’est créditée et aucune route de claim n’est modifiée. Avec le flag quotidien fermé, aucune ronde n’est créée.

Fichiers : `Server/src/BeeKingdom.HiveOperations/HiveStockSnapshot.cs`, `Server/src/BeeKingdom.Server/Program.cs`, `Server/src/BeeKingdom.Server/appsettings.json`, `Server/src/BeeKingdom.Server/appsettings.Production.json`, `Server/tests/BeeKingdom.HiveOperations.Tests/HiveStockSnapshotTests.cs`, `Server/tests/BeeKingdom.Tests/HiveStockEndpointTests.cs`.

Validation locale : build Release serveur **0 erreur** sans nouvel avertissement de la tranche. Noyau snapshot ciblé : **2/2**; tests HTTP : **3/3** (fermeture, authentification, succès activé + relecture non mutante). Replay indépendant : suite complète BeeKingdom.Tests net10.0 **328 réussis, 0 échec, 8 ignorés SQL**, TRX `Artifacts/TestResults/HiveStock_ServerFullFinal.trx`. DailyRound activé/projection après mutation, SQL jetable et staging TLS restent explicitement ouverts; aucun candidat, transfert, activation ou déploiement effectué.
