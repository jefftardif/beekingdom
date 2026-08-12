# Phase 4 — voie stratégique, tranche serveur locale

Le noyau persistant et ses routes fermées ajoutent cinq IDs canoniques : `royal_guard`, `striker`, `nurturer`, `scout`, `alchemist` (catalogue `phase4-v1`). Le choix exige une identité Bearer, l’appartenance joueur/ruche, un niveau de bâtiment serveur >=10, une révision attendue et une clé d’idempotence. La mutation est atomique dans `PlayerHiveState`, verrouillée après le premier choix, isolée par joueur/ruche et rejouable avec la même charge. Une clé réutilisée avec une charge différente renvoie `game.idempotency_conflict` (409). Le snapshot expose catalogue/version, sélection éventuelle, révision et `updatedAtUtc` UTC.

Routes préparées mais fermées :

- `GET /game/v1/hives/{hiveId}/strategic-path`
- `POST /game/v1/hives/{hiveId}/strategic-path` avec `{pathId, expectedRevision, idempotencyKey}`

Le contrôle `StrategicPath:Enabled` est évalué avant authentification et avant lecture/mutation : la configuration absente ou fausse retourne `503 game.unavailable`. Les routes réutilisent uniquement les helpers/enveloppes `game.*` existants; les erreurs d’entrée sont `game.invalid_request`, l’absence de session `game.session_required`, et les conflits sont des codes `game.*` dédiés. Aucun crédit économique n’est effectué.

Le modèle actuel persiste l’état au niveau joueur/ruche (`PlayerHiveState`). Une intégration à un profil de compte distinct, si elle devient nécessaire, devra être versionnée séparément; elle n’est pas prétendue ici.

Fichiers créés ou modifiés :

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/StrategicPathService.cs`
- `Server/src/BeeKingdom.HiveOperations/StrategicPathOptions.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/StrategicPathTests.cs`
- ce rapport

Preuves locales : suite `BeeKingdom.HiveOperations.Tests` **36/36**; build Release de `BeeKingdom.Server` **0 erreur** (un avertissement SqlClient MSB3277 préexistant). Exécution avec `DOTNET_ROLL_FORWARD=Major`, car seul le runtime .NET 10 x64 est installé dans la VM; aucune exécution sous runtime .NET 8 natif n’est revendiquée. Les tests HTTP WebApplicationFactory, SQL jetable, TLS/IIS et Android staging restent des portes ouvertes avant toute exposition.

`StrategicPath:Enabled=false` demeure la valeur par défaut et Production. `ChatEnabled=false`, `RealtimeEnabled=false` et `DeploymentAuthorized=false` restent inchangés. Aucun candidat, transfert, activation ou déploiement n’a été effectué.
