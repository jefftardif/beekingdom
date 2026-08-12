# Production hors absence — fondation durable

Cette passe ajoute la forme persistante `HiveOfflineProductionState` (marqueur UTC, pending décimal par bâtiment, révision et reçus), une migration de modèle 9 vers 10 et un service atomique d'accrual borné à 7 jours. Le catalogue est désormais exclusivement injecté par `HiveOfflineProductionOptions` et sa version; aucune valeur économique n'est codée en dur. Les anciens états restent `null` sans seed. Le flag `HiveOfflineProduction:Enabled` demeure faux par défaut et en Production.

La route GET/collecte et la preuve HTTP d'idempotence durable ne sont pas encore exposées dans cette passe; elles restent bloquées jusqu'à l'ajout du DTO, de la collecte atomique et des tests de reprise demandés. Aucun montant client, horloge client ou pending client n'est accepté.

Build Release serveur : 0 erreur, avertissements existants (conflit Microsoft.Data.SqlClient et avertissement nullable Program.cs). Tests HTTP/core dédiés non ajoutés dans cette passe; la porte SQL et les preuves de collecte idempotente restent explicitement ouvertes avant toute activation.

Fichiers :
- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOfflineProductionService.cs`
- `Docs/ProductionIntegration/HiveOfflineProduction_DurableFoundation_2026-07-22.md`

Pas de candidat, déploiement, synchronisation ou modification Assets/Unity/chat.
