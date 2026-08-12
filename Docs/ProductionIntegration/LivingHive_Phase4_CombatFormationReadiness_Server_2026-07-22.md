# Phase 4 — disponibilité de formation, tranche serveur

Audit : `PlayerHiveState` persiste des bâtiments et opérations, mais aucun roster d’effectifs classé par doctrine. `HiveProgressionSnapshot` accepte éventuellement un dictionnaire de troupes fourni à la factory, sans source durable actuelle dans `PlayerHiveState`; il ne peut donc pas servir de preuve d’effectifs officiels.

Contrat de lecture préparé : `GET /game/v1/hives/{hiveId}/combat/formation-readiness`. La réponse porte `contractVersion`, `doctrineCatalogVersion`, `playerId`, `hiveId`, `revision`, `availabilityStatus`, `families` et `unclassifiedLegacyRoles`. Tant que le roster serveur n’existe pas, `availabilityStatus=not_recorded`, `families` est vide (jamais des zéros synthétiques) et les rôles historiques `Soldats`, `Gardiennes`, `Eclaireuses` restent explicitement non classifiés. Aucune conversion implicite n’est faite; la seule correspondance Gardiennes → guardians reste une décision future lorsque des effectifs autoritaires seront persistés.

La route est fermée par `CombatFormationReadiness:Enabled=false` par défaut et en Production. Le drapeau faux renvoie `503 game.unavailable` avant authentification ou lecture. Une fois activée, elle exige le bearer et l’appartenance à la ruche (`401 game.session_required`, `400 game.invalid_request`, `404 game.not_found`). Elle ne mute aucun état et n’expose aucun coefficient, dégât, puissance ou victoire.

Fichiers : `CombatFormationReadiness.cs`, `CombatFormationReadinessTests.cs`, `Program.cs`, `appsettings.json`, `appsettings.Production.json`, ce rapport.

Preuves : suite HiveOperations **40/40**; build Release serveur **0 erreur**, avertissement SqlClient MSB3277 préexistant. Exécution avec `DOTNET_ROLL_FORWARD=Major` faute de runtime .NET 8 natif. Les tests HTTP WebApplicationFactory, SQL et staging restent à ouvrir. Aucun candidat, transfert, activation ou déploiement; `DeploymentAuthorized=false` conservé.
