# Phase 4 — roster doctrinal et formation officielle

Le modèle ajoute `DoctrineRosterState` nullable à `PlayerHiveState` et fait passer `CurrentModelVersion` à **7**. La migration v6→v7 ne seed rien : les états historiques restent sans roster (`not_recorded`), tandis qu’un roster présent est validé et conservé. Les clés doivent être canoniques, les comptes sont bornés à 1 000 000 000, les reçus à 4096, et la révision, les clés/hash d’idempotence et l’opération UTC sont cohérentes; le lot actif doit correspondre exactement au catalogue. Aucun Soldat, Gardienne ou Eclaireuse n’est importé automatiquement.

Le catalogue serveur `phase4-combat-v1` contient exactement : guardians (lot 4, 680 miel + 180 pollen, 14 s), wingrunners (lot 6, 420 miel + 260 pollen, 14 s), darters (lot 8, 500 miel + 120 pollen, 14 s). Une seule opération doctrinale est active par ruche. Le démarrage vérifie l’appartenance, `guard_post >= 1`, la révision, les ressources et la file, puis débite atomiquement et fixe une échéance UTC. La réclamation avant échéance est refusée; après échéance, le lot est incrémenté une seule fois. Les reçus et conflits utilisent des clés dédiées `game.*`.

Routes fermées :

- `GET /game/v1/hives/{hiveId}/combat/recruitment`
- `POST /game/v1/hives/{hiveId}/combat/recruitment/start`
- `POST /game/v1/hives/{hiveId}/combat/recruitment/{operationId}/claim`

`CombatRecruitment:Enabled=false` est défini par défaut et en Production; le drapeau court-circuite avant authentification, lecture ou mutation avec `503 game.unavailable`. Aucune puissance, victoire, dégât ou composition de combat n’est calculée.

La projection formation-readiness retourne désormais `not_recorded` avec dictionnaire vide si le roster est absent, et `recorded` avec exactement les trois familles et leurs comptes persistés si le roster existe. Fichiers : `HiveOperationModels.cs`, `HiveStateMigrator.cs`, `CombatFormationReadiness.cs`, `CombatRecruitmentService.cs`, `CombatRecruitmentOptions.cs`, `Program.cs`, les deux `appsettings`, `CombatRecruitmentTests.cs`, `HiveOperationServiceTests.cs`, ce rapport.

Preuves : tests HiveOperations **44/44**; build Release serveur **0 erreur**, avertissement SqlClient préexistant. Runtime local avec `DOTNET_ROLL_FORWARD=Major`; .NET 8 natif, HTTP WebApplicationFactory, SQL et staging restent des portes. Aucun candidat, transfert, activation ou déploiement; `DeploymentAuthorized=false`.
