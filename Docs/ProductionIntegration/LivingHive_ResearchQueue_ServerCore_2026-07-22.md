# LivingHive — noyau serveur de recherche (22 juillet 2026)

## Périmètre

Cette tranche ajoute uniquement le noyau `Server/` pour deux recherches de ruche. Le drapeau `LivingHiveResearch:Enabled` est `false` par défaut et dans `appsettings.Production.json`; aucune route HTTP n'est activée, aucun candidat ni déploiement n'est produit. Aucun fichier `Assets/`, chat, scène ou image n'a été modifié.

## Contrat autoritaire

Les identifiants acceptés sont `foraging_routes_i` (240 miel, 90 pollen, 16 secondes, +200 points de base de production miel) et `tempered_combs_i` (180 miel, 120 pollen, 16 secondes, +500 points de base de capacité cire). L'état additif `HiveResearchState` conserve les recherches terminées et au plus une `ResearchOperation` active avec identifiant, dates UTC et révision. Les soldes sont débités dans la même transaction que l'état actif. Une recherche terminée ne peut pas être relancée; une seconde recherche pendant une opération active renvoie `research_busy`.

`StartResearchAsync` et `CompleteResearchAsync` exigent joueur/ruche, `expectedRevision` et `idempotencyKey`. Le reçu est cloisonné par état joueur/ruche. Une même clé et charge rejoue le résultat et la révision; une charge différente renvoie `idempotency_conflict`. La complétion avant `endsAtUtc` renvoie `research_not_ready`; aucun effet n'est appliqué avant l'heure serveur. Les effets sont des données autoritaires de la complétion et aucune valeur client n'est acceptée.

## Fichiers exacts

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveOperationService.cs`
- `Server/src/BeeKingdom.HiveOperations/LivingHiveResearchOptions.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/LivingHiveResearchTests.cs`
- ce rapport

## Preuves locales

- Tests ciblés `LivingHiveResearchTests` : **2/2**, 0 échec.
- Suite `BeeKingdom.HiveOperations.Tests` : **26/26**, 0 échec.
- Runtime utilisé : `DOTNET_ROLL_FORWARD=Major` (la VM ne fournit pas le runtime .NET 8 natif); aucune installation ni accès externe.
- Aucun processus Unity, `dotnet` ou `testhost` n'était actif avant les tests. Les processus de test se sont terminés normalement.

La couverture HTTP/WebApplicationFactory, SQL jetable et staging TLS/IIS/Android restent des portes ultérieures; elles ne sont pas prétendues couvertes par cette tranche. Le drapeau demeure fermé et `DeploymentAuthorized=false` inchangé.
