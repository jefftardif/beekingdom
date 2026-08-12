# LivingHive — contrat local de reprise « retour à la ruche »

Le serveur fournit maintenant `HiveOperationResumeSummaryFactory`, projection en lecture seule de `PlayerHiveState`. Elle sépare les opérations actives et collectées, expose identifiant, type, destination, statut, résultat, révision de ruche et horodatages UTC. Les recherches actives/terminées sont également représentées; aucun identifiant ou statut client n'est utilisé.

Le contrat reste local : `HiveOperationResume:Enabled=false` par défaut et en Production, aucune route HTTP ni notification push. Population, navigation et récolte automatique ne sont pas ajoutées. Le futur raccordement devra appeler cette projection après authentification et appartenance joueur/ruche.

Fichiers : `HiveOperationResumeContract.cs`, `Program.cs`, les deux `appsettings`, `HiveOperationResumeSummaryTests.cs`, ce rapport.

Preuves : suite HiveOperations **29/29**, build Release **0 erreur** (avertissement SqlClient préexistant), runtime `DOTNET_ROLL_FORWARD=Major`, aucun processus Unity/dotnet/testhost résiduel. Aucun candidat, déploiement ou synchronisation.
