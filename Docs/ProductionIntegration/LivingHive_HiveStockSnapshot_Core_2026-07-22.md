# LivingHive — snapshot autoritaire des stocks (22 juillet 2026)

Le noyau local expose `HiveStockSnapshotFactory`, qui projette exclusivement `PlayerHiveState` vers un snapshot player/ruche cloisonné : révision, miel/cire/pollen avec montant et capacité, recherches terminées et engagements actifs (opérations existantes et recherche). Population/capacité de population restent explicitement `null`, car aucun agrégat serveur actuel ne les représente honnêtement.

Aucune mutation, récompense ou valeur client n'intervient. `HiveStockSnapshot:Enabled=false` est ajouté par défaut et en Production. Aucune route HTTP n'est ouverte dans cette tranche : l'authentification/session et le contrat de lecture doivent encore être raccordés avant exposition.

Fichiers modifiés/créés :

- `Server/src/BeeKingdom.HiveOperations/HiveStockSnapshot.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/HiveStockSnapshotTests.cs`
- ce rapport

Preuves locales : `HiveStockSnapshotTests` **1/1**; suite HiveOperations **27/27**, 0 échec. Build Release serveur : 0 erreur, avertissement SqlClient préexistant. Runtime local utilisé avec `DOTNET_ROLL_FORWARD=Major`; aucun processus Unity/dotnet/testhost ne reste actif. Les portes HTTP WebApplicationFactory, SQL, TLS/IIS et Android staging restent ouvertes et aucun candidat/déploiement n'a été produit.
