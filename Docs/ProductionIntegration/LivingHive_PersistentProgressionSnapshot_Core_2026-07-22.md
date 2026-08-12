# LivingHive — snapshot de progression persistante

## Audit

`PlayerHiveState.BuildingLevels` conserve déjà plusieurs bâtiments ensemble et sa `Revision` fournit une révision bâtiment/état. Les contrats HiveLoop readiness (`HiveBuildingReadinessRecord`, `HiveTroopCountReadinessRecord`, queues et `PlayableHiveLoopReadinessResponse`) sont explicitement read-only/non-live et ne constituent pas une autorité persistante raccordée. Ils prouvent toutefois la forme attendue pour un catalogue, une révision d'armée et des effectifs.

## Noyau livré

`HiveProgressionSnapshotFactory` projette un état serveur vers un snapshot cloisonné par joueur, ruche, monde et serveur, avec `BuildingRevision`, `ArmyRevision`, version de catalogue, niveaux complets et effectifs complets. Les niveaux/effectifs négatifs, scope vide et révision d'armée négative sont rejetés; aucun dictionnaire provenant d'une autre partition n'est fusionné. Les effectifs restent fournis uniquement par un agrégat serveur futur, jamais par le client.

`HiveProgressionSnapshot:Enabled=false` est défini par défaut et en Production; aucune route HTTP n'est ouverte avant raccordement authentification/appartenance/transport.

Fichiers : `HiveProgressionSnapshot.cs`, `Program.cs`, les deux `appsettings`, `HiveProgressionSnapshotTests.cs`, ce rapport.

Validation renforcée : état nul, PlayerId/HiveId vides, révision négative, clés vides, effectifs négatifs et catalogue vide sont rejetés; les copies restent défensives. Suite HiveOperations **32/32**; build Release serveur **0 erreur**, avertissement SqlClient préexistant. Aucun candidat, déploiement, Assets ou chat modifié.
