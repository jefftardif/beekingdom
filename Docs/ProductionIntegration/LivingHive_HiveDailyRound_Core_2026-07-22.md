# LivingHive — HiveDailyRound autoritaire (22 juillet 2026)

Le noyau persistant ajoute une ronde UTC par joueur/ruche. Les trois jalons sont enregistrés par méthodes serveur dédiées : collecte d'une opération effectivement `Collected`, lancement d'une opération non collectée dont `StartedAtUtc` appartient au jour UTC courant (bâtiment ou recherche), et lecture explicite du snapshot autoritaire. Une opération ancienne/inexistante est refusée; aucune donnée client ne peut positionner ces indicateurs.

Lorsque les trois faits appartiennent au même jour UTC, `ClaimDailyRoundAsync` vérifie l'identité/ruche via le repository, `expectedRevision` et `idempotencyKey`, puis crédite atomiquement 120 miel + 60 pollen au plus une fois. Même clé/charge rejoue le reçu; charge contradictoire renvoie `idempotency_conflict`. Les capacités sont vérifiées avant crédit.

Le flag `HiveDailyRound:Enabled` est false par défaut et en Production. Aucun endpoint HTTP n'est exposé : le raccordement session/snapshot reste une porte ultérieure.

Fichiers : `HiveOperationModels.cs`, `HiveOperationService.cs`, `HiveDailyRoundOptions.cs`, `Program.cs`, les deux `appsettings`, `HiveDailyRoundTests.cs`, ce rapport.

Preuves : suite HiveOperations **28/28**; le test couvre aussi le rejet d'une opération collectée comme preuve de lancement; build Release **0 erreur**, avertissement SqlClient préexistant. Le hash de reçu inclut désormais le jour UTC. Runtime local avec `DOTNET_ROLL_FORWARD=Major`; aucun processus Unity/dotnet/testhost après exécution. Aucun candidat, déploiement ou synchronisation.
