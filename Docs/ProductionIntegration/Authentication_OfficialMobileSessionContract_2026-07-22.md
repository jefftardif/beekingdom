# Contrat de session officielle mobile — 2026-07-22

## Audit et décision

Les routes réelles sont `GET /runtime/account-session-readiness`, `POST /accounts`, `POST /auth/login`, `POST /auth/refresh`, `POST /auth/validate` et `POST /auth/logout`. L’ancien chemin `/auth/token/refresh` n’existe pas et n’est pas ajouté : le client doit utiliser `/auth/refresh`.

La readiness est publique, en camelCase, sans bearer et sans secret. Elle expose notamment `accountCreationAllowed`, `sessionCreationAllowed`, `tokenIssuanceAllowed`, `officialPersistenceClaimAllowed`, `claims` et `blockers`; `secretsAllowedInResponse=false`. En Production les portes restent toutes fermées.

## Contrat HTTP stable

`POST /auth/login` reçoit `email`, `password`, `clientVersion`, `ipAddress`, `deviceIdentifier`, `region`. Une réussite 200 renvoie l’enveloppe camelCase `succeeded`, `playerId`, `accountId`, `session` et `tokens`; `session` contient `sessionId`, `playerId`, `loginUtc`, `expirationUtc` et l’état de révocation; `tokens` contient les deux jetons et `accessTokenExpiresUtc`/`refreshTokenExpiresUtc`. Les dates sont UTC. Le serveur dérive toutes les identités du compte et de la session; aucune identité envoyée par l’appareil n’est autoritaire.

`POST /auth/refresh` reçoit uniquement `refreshToken`. Une réussite 200 renvoie le nouveau couple de jetons. Le refresh précédent est révoqué avant émission; son rejeu renvoie 401 `auth.session_required`. Un refresh vide/malformé renvoie 400 `auth.invalid_request`.

`POST /auth/logout` utilise exclusivement le bearer `Authorization`. Le corps et tout `sessionId` déclaré sont ignorés. Une réussite 200 renvoie `{revoked:true}` et révoque la session correspondant au bearer; bearer absent, invalide ou déjà révoqué : 401 `auth.session_required`.

Catalogue d’erreurs : 400 `auth.invalid_request`, 401 `auth.invalid_credentials`/`auth.session_required`, 409 `auth.session_limit`, 429 `auth.rate_limited` pour compte temporairement verrouillé, 503 `auth.unavailable` lorsque les portes Production sont fermées. Aucun message brut, jeton ou identifiant sensible n’est renvoyé dans ces erreurs.

## Frontière appareil/serveur

L’appareil lit d’abord la readiness, puis conserve uniquement les jetons reçus et leurs échéances UTC pour la session courante. Il ne choisit ni `PlayerId`, ni `SessionId`, ni durée, ni statut; il renouvelle une seule fois sur 401 avec le refresh courant et supprime localement les jetons après logout. Le serveur reste propriétaire de l’identité, des sessions, de la rotation one-time, de la révocation et des expirations.

## Preuves locales

- `OfficialAuthenticationEndpointTests` : 6/6 réussis sous `net10.0` avec `DOTNET_ROLL_FORWARD=Major`.
- Couverture : readiness publique sans secrets; login et dates UTC; rotation et rejeu du refresh; deux joueurs isolés; logout bearer-only et révocation; erreurs 400/401/409/429. La fermeture Production 503 est couverte par `AuthenticationProductionBoundaryTests` dans la suite complète.
- Suite serveur complète net10.0 (`DOTNET_ROLL_FORWARD=Major`, `-p:EnableNet10TestTarget=true`) : 272 réussis, 7 ignorés, 0 échec (279 total). Les 7 ignorés sont les scénarios SQL externes; aucune connexion réelle ni donnée réelle n’a été utilisée.
- Build Release `BeeKingdom.Server.csproj` : 0 erreur, 1 avertissement préexistant de conflit Microsoft.Data.SqlClient.

## Correction du chemin de renouvellement

Le descripteur partagé et les tests utilisent désormais exclusivement `POST /auth/refresh`, marqué `ImplementedNow=true` et décrit comme une rotation one-time liée au `PlayerId`/`SessionId` serveur. L'ancien chemin `/auth/token/refresh` n'est pas un endpoint planifié : il est conservé uniquement dans `HttpEndpointTests` comme preuve négative explicite (404).

La route de login ignore désormais `ipAddress` fourni dans le JSON. Elle persiste uniquement `HttpContext.Connection.RemoteIpAddress` (ou la sentinelle bornée `unknown` si absente), sans faire confiance aux en-têtes forwarded par défaut et sans renvoyer l'adresse dans le DTO. `OfficialAuthenticationEndpointTests` vérifie ce cloisonnement avec une fausse adresse JSON.

- Sous-ensemble `SharedContractsTests|HttpEndpointTests` net10.0 : 116/116 réussis.
- Suite serveur complète net10.0 : 272 réussis, 7 ignorés SQL, 0 échec (279 total).
- Build Release : 0 erreur, 1 avertissement préexistant Microsoft.Data.SqlClient.
- Correctif IP : `OfficialAuthenticationEndpointTests` 7/7 réussis.

## Fichiers

- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Authentication/Models/AuthenticationModels.cs`
- `Server/src/BeeKingdom.Authentication/Tokens/AuthenticationTokenManager.cs`
- `Server/tests/BeeKingdom.Tests/OfficialAuthenticationEndpointTests.cs`
- `Server/src/BeeKingdom.Shared/Auth/OfficialAuthFoundationContracts.cs`
- `Server/tests/BeeKingdom.Tests/SharedContractsTests.cs`
- `Server/tests/BeeKingdom.Tests/HttpEndpointTests.cs`
- `Docs/ProductionIntegration/Authentication_OfficialMobileSessionContract_2026-07-22.md`

`AccountCreationAllowed=false`, `SessionCreationAllowed=false`, `TokenIssuanceAllowed=false`, Chat/Realtime et `DeploymentAuthorized` restent fermés. Aucun compte réel, secret, candidat, transfert ou déploiement n’a été utilisé.
