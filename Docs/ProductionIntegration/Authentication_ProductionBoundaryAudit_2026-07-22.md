# Audit de frontière compte/session — 2026-07-22

## Routes réelles

- `POST /accounts` : création de compte (fermé en Production par `AccountCreationAllowed=false`).
- `POST /auth/login` : authentification et émission initiale (fermé en Production si `SessionCreationAllowed` ou `TokenIssuanceAllowed` est faux).
- `POST /auth/refresh` : rotation de jeton (fermé en Production si `TokenIssuanceAllowed=false`).
- `POST /auth/validate` : validation d’un jeton fourni; ne crée aucun jeton.
- `POST /auth/logout` : révocation d’une session fournie; ne crée aucun jeton.
- `GET /runtime/account-session-readiness` : lecture publique minimale de l’état de préparation, sans secret, identifiant de compte/session ou jeton.

Les durées configurées sont 15 minutes pour l’access token et 14 jours pour le refresh token. Elles ne constituent pas une activation : les portes de Production restent fermées.

## Correction appliquée

Avant cette passe, login/refresh/accounts appelaient directement les services même lorsque la readiness Production déclarait l’émission fermée. Les trois routes retournent maintenant `503 {code: "auth.unavailable", message: "auth.unavailable"}` avant tout service métier lorsque l’environnement est Production et que la porte correspondante est false. Le comportement Development reste disponible pour les tests locaux.

## Preuves

- Smoke direct de l’artefact Release, `ASPNETCORE_ENVIRONMENT=Production`, via .NET 10 avec roll-forward de l’assembly net8 :
  - `/auth/login` → 503 `auth.unavailable`.
  - `/auth/refresh` → 503 `auth.unavailable`.
  - `/accounts` → 503 `auth.unavailable`.
- Build Release `BeeKingdom.Tests.csproj` : 0 erreur; avertissement préexistant Microsoft.Data.SqlClient 5/6.
- Test NUnit ajouté `AuthenticationProductionBoundaryTests.cs` : compilation réussie, mais testhost local ne découvre toujours aucun test NUnit; il n’est donc pas compté comme exécuté.
- Le test existant `AccountSessionReadinessReportsPreparedButNotLiveState` fige déjà `accountCreationAllowed=false`, `sessionCreationAllowed=false`, `tokenIssuanceAllowed=false`, claims live false et absence de secrets.

## Mise à jour de statut

Ce rapport est l’audit initial de frontière Production. Ses nombres de découverte de tests sont historiques et sont remplacés par `Authentication_OfficialMobileSessionContract_2026-07-22.md`, qui couvre désormais le contrat login/refresh/logout/readiness et les erreurs structurées avec 6/6 tests ciblés et 272/279 dans la suite serveur.

## Fichiers exacts

- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.Tests/AuthenticationProductionBoundaryTests.cs`
- `Docs/ProductionIntegration/Authentication_ProductionBoundaryAudit_2026-07-22.md`

Les fichiers `Assets/`, le chat, Unity, staging, les candidats et le déploiement n’ont pas été touchés.
