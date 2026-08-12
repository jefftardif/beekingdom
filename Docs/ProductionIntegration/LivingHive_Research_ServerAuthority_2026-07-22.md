# LivingHive — recherche officielle serveur

## Contrat

Version `living-hive-research-v1`.

- `GET /game/v1/hives/{hiveId}/research`
- `POST /game/v1/hives/{hiveId}/research/{researchId}/start`
- `POST /game/v1/hives/{hiveId}/research/{operationId}/complete`

Les snapshots camelCase contiennent `playerId`, `hiveId`, `contractVersion`, `catalogVersion`, `revision`, `serverTimeUtc`, `balances`, `completed`, `offers` et `activeOperation`. Les requêtes ne contiennent que `expectedRevision` et `idempotencyKey`; les réponses de mutation contiennent `receipt` et `snapshot`.

Le catalogue de test reprend `foraging_routes_i` et `tempered_combs_i` avec des coûts/durée/effets explicitement factices. Le catalogue live reste vide et `LivingHiveResearch:Enabled=false` par défaut et en Production.

## Autorité et effets

La complétion persistante de recherche est lue par `HiveOfflineProductionService`: les bps stockés dans `Research.Completed` sont validés (0..10000) puis appliqués exactement comme `1 + bps/10000` au taux miel et `capacity + floor(capacity*bps/10000)` à la capacité pending cire. Le calcul est serveur, borné et déterministe; le mobile ne calcule aucun bonus. Les DTO HTTP projettent désormais `effects:{honeyProductionBonusBps,waxCapacityBonusBps}`. Désactiver l’offre n’efface pas `Research.Completed`.

## Fichiers

- `Server/src/BeeKingdom.HiveOperations/LivingHiveResearchOptions.cs`
- `Server/src/BeeKingdom.HiveOperations/LivingHiveResearchContracts.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOfflineProductionService.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.Tests/LivingHiveResearchEndpointTests.cs`

## Validation

- Build Release : 0 erreur, 2 avertissements existants.
- Suite serveur net10.0 : **325 réussis, 0 échec, 8 ignorés, 333 total**.
- Suite HTTP recherche : **4 réussis, 0 échec, 0 ignoré, 4 total**; fermeture, authentification, GET avec `effects` structurés et catalogue activé vide sans mutation.

La suite complète couvre le noyau de recherche historique. La preuve HTTP dédiée confirme désormais que le catalogue est injecté par configuration et qu’un catalogue activé mais vide n’expose aucune offre et ne mute pas l’état. Les parcours HTTP start/complete/rejeu/effets persistés restent à compléter avant ratification staging; les routes restent fermées.

## Portes restantes

Catalogue économique officiel, tests HTTP activés, reconstruction JSON/SQL dédiée, staging TLS/IIS et activation restent ouverts. Aucun candidat, transfert, activation ou déploiement n’a été effectué.
