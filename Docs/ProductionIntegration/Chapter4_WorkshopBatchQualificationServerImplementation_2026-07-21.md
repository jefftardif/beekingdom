# Chapitre 4 — qualification serveur du lot témoin

Date : 2026-07-21  
Statut : implémenté localement, fermé par défaut, non promouvable.

## Contrat livré

`POST /game/v1/hives/{hiveId}/workshop/batch-qualification` exige une session Bearer valide, un identifiant de ruche valide, `expectedRevision`, `answer` et `idempotencyKey`. Le `PlayerId` provient exclusivement du Bearer; le repository est indexé joueur/ruche et ne permet donc pas de réutiliser un reçu sur une autre identité ou ruche.

L’état persistant `WorkshopBatchQualificationState` contient la spécialisation relue côté serveur, la quantité collectée autoritaire, l’étape et sa révision. La commande relit ces préconditions dans la transaction. `production` attend `heat`; `storage` attend `load`. Une mauvaise réponse renvoie 200 `game.tutorial_answer_incorrect`, conserve l’étape et la révision (seul le reçu d’idempotence est enregistré). Une bonne réponse avance une seule fois vers `chapter4.upgrade_application_ready` et incrémente la révision. Un rejeu identique renvoie le même code, étapes, révisions et horodatage; une charge différente sous la même clé renvoie 409 `game.idempotency_conflict`.

Aucun coût, solde, minuterie ou opération économique n’est touché. Le flag `WorkshopBatchQualification:Enabled` est absent/false par défaut et renvoie 503 `game.unavailable` avant toute lecture; il reste fermé en Production.

## Fichiers exacts

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`
- `Server/src/BeeKingdom.HiveOperations/WorkshopBatchQualificationOptions.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/WorkshopBatchQualificationTests.cs`
- `Server/tests/BeeKingdom.Tests/GameWorkshopBatchQualificationEndpointTests.cs`

## Preuves

- HiveOperations ciblé : **24/24 réussis**, runtime cible net8.0 exécuté avec `DOTNET_ROLL_FORWARD=Major` (SDK 10.0.302; runtime .NET 8 natif indisponible localement). Les preuves couvrent les branches Rendement/Stockage, mauvaise réponse sans progression, révision obsolète, état invalide, rejeu d’erreur et conflit de charge.
- Build Release `BeeKingdom.Server.csproj` : **0 erreur**, 1 avertissement préexistant de conflit Microsoft.Data.SqlClient 5/6.
- Smoke local direct sur l’artefact Release/Debug avec `DOTNET_ROLL_FORWARD=Major` : `POST /game/v1/hives/{id}/workshop/batch-qualification` a renvoyé **503** et `game.unavailable`, conformément au flag fermé; processus arrêté après la requête.
- Validation de syntaxe JSON des deux fichiers de configuration : réussie.
- Tests HTTP WebApplicationFactory : compilation réussie, mais **0 test découvert** dans cet environnement. `dotnet test --list-tests` et une seconde tentative avec `--test-adapter-path` vers le package NUnit local donnent le même résultat; l’adaptateur/testhost net8 n’est pas exploitable avec le runtime de repli. Ils ne sont pas présentés comme exécutés. À rerun sous runtime .NET 8 natif avant ratification.

La validation HTTP n’est pas nécessaire pour progresser sur ce jalon local; la route reste fermée et le smoke 503 est déjà vérifié.

## Portes restantes

### Diagnostic runtime du 2026-07-22

Inventaire local effectué avec `dotnet --list-runtimes`, `dotnet --list-sdks` et recherche des dépendances workspace : aucun runtime `Microsoft.NETCore.App 8.x` ni `Microsoft.AspNetCore.App 8.x` x64 n’est présent. Seuls SDK/runtime 10.0.302/10.0.10 sont installés sous `C:\Program Files\dotnet`; le bundle workspace ne fournit pas de `dotnet.exe`. Les tests HTTP WebApplicationFactory n’ont donc pas été relancés sous .NET 8 natif et aucun défaut serveur supplémentaire n’est inféré.

SQL jetable/reconstruction et concurrence n’ont pas été exécutés; le flag reste fermé. TLS/SNI/IIS, shell mobile/auth réel, Android staging et tout candidat/déploiement restent inchangés. `DeploymentAuthorized=false`, `Chat/Realtime=false`.

Le mode opératoire transmissible est décrit dans `Docs/ProductionIntegration/Chapter4_WorkshopBatchQualification_Preflight.md`.

Le manifeste `Server/artifacts/candidates/CANDIDATE-STATUS.json` référence actuellement `BeeKingdom.Server.20260721T225554Z` en `local-validation-only`; ce candidat antérieur ne doit pas être présenté comme intégrant cette tranche. Aucun nouveau candidat n’a été construit.

Fichiers modifiés dans ce jalon : `HiveOperationService.cs`, `Program.cs`, `WorkshopBatchQualificationTests.cs` et le présent rapport. La définition de modèle/options et les configurations fermées restent inchangées depuis la tranche précédente.
