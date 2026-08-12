# Frontière REST des read-models game — 2026-07-22

Les réponses GET sous `/game/v1` portent désormais `Cache-Control: private, no-store` et `Pragma: no-cache`; elles ne sont donc pas partageables par un proxy/CDN. Le cache protégé reste exclusivement applicatif côté appareil.

Les routes activées en test renvoient 401 avec `{code:"game.session_required",message:"game.error.session_required"}` lorsque le bearer est absent. Aucun PlayerId ni corps brut n'est exposé. Les drapeaux de production restent fermés.

Preuves : `GameReadModelSecurityTests` 2/2 sous net10.0 avec `DOTNET_ROLL_FORWARD=Major`. La suite complète n'a pas été relancée pour cette passe ciblée; le dernier état complet ratifié reste 272 réussis, 7 ignorés SQL, 0 échec. Build Release du serveur : 0 erreur, avertissement préexistant Microsoft.Data.SqlClient.

Fichiers :
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.Tests/GameReadModelSecurityTests.cs`
- `Docs/ProductionIntegration/GameReadModel_AuthCacheBoundary_2026-07-22.md`

`ChatEnabled=false`, `RealtimeEnabled=false`, `DeploymentAuthorized=false`; aucun candidat, déploiement ou activation.
