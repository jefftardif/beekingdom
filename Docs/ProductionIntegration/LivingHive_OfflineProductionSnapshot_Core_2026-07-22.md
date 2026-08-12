# LivingHive — snapshot de production hors ligne

Le noyau `HiveOfflineProductionSnapshotFactory` est une projection strictement read-only d'un état serveur et d'un catalogue serveur. Il expose joueur/ruche/monde/serveur, révision, `ServerUtc`, marqueur `ProductionAsOfUtc`, durée reconnue bornée à sept jours, version de catalogue et entrées triées par bâtiment (ressource, quantité en attente, capacité, taux horaire).

L'heure future est ramenée à `ServerUtc`, un recul d'horloge produit une durée nulle et le pending est plafonné à la capacité. Les identités, dates UTC, clés, doublons et valeurs négatives sont rejetés. La lecture ne crédite aucun stock; la collecte manuelle reste l'unique mutation. Aucun champ client n'est accepté.

`HiveOfflineProduction:Enabled=false` par défaut et en Production; aucune route HTTP n'est ouverte tant que session/auth/transport ne sont pas raccordés.

Fichiers : `HiveOfflineProductionSnapshot.cs`, `Program.cs`, les deux `appsettings`, `HiveOfflineProductionSnapshotTests.cs`, ce rapport.

La factory impose aussi une taille maximale de catalogue de **64** entrées. Les tests couvrent explicitement les gardes d'identité/révision/UTC/durée/catalogue, ordre déterministe à deux bâtiments, plafonnement de durée et pending, futur/recul d'horloge et absence de mutation logique de l'état. Le modèle durable ne persiste pas encore le marqueur/pending par bâtiment : ce noyau ne constitue donc pas une production serveur complète. Suite HiveOperations **35/35**; build Release **0 erreur**, avertissement SqlClient préexistant; runtime avec `DOTNET_ROLL_FORWARD=Major`; aucun candidat, déploiement, Assets ou chat modifié.
