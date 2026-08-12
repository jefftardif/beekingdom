# Phase 4 — catalogue de doctrine de combat

Le serveur prépare le catalogue versionné `phase4-combat-v1` avec exactement trois familles : `guardians`, `wingrunners`, `darters`. Le cycle autoritaire est `guardians > darters > wingrunners > guardians`; une famille ne domine jamais elle-même. Aucun coefficient, dégât, récompense, sélection ou effet économique n’est exposé.

Lecture HTTP préparée : `GET /game/v1/combat/doctrine`. Elle est protégée par `CombatDoctrine:Enabled`, faux par défaut et dans `appsettings.Production.json`. Lorsque le drapeau est faux, elle retourne `503 game.unavailable` avant authentification et avant toute lecture. Lorsqu’elle est activée, elle exige le bearer via les helpers existants et retourne `401 game.session_required` si absent.

Fichiers :

- `Server/src/BeeKingdom.HiveOperations/CombatDoctrine.cs`
- `Server/src/BeeKingdom.HiveOperations/CombatDoctrineOptions.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/CombatDoctrineTests.cs`
- ce rapport

Preuves : tests noyau HiveOperations **38/38**; build Release serveur **0 erreur**, avec l’avertissement SqlClient MSB3277 préexistant. Exécution locale avec `DOTNET_ROLL_FORWARD=Major` (runtime .NET 8 natif absent). Les tests HTTP WebApplicationFactory, SQL, TLS/IIS et Android staging restent à réaliser avant exposition. Aucun candidat, transfert, activation ou déploiement; `DeploymentAuthorized=false` demeure inchangé.
