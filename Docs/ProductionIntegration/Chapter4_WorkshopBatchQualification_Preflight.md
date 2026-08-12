# Préflight de validation — qualification du lot témoin

Ce préflight est local/staging uniquement. Il ne doit pas activer la fonctionnalité en Production.

## Conditions

1. Runtime et SDK .NET 8 natifs installés (ne pas utiliser `DOTNET_ROLL_FORWARD=Major` comme preuve finale).
2. SQL jetable/reconstruction disponible, sans données réelles.
3. `WorkshopBatchQualification:Enabled=false` en Production; l’activation de test doit être limitée à la fabrique WebApplicationFactory ou à un environnement isolé.

## Vérifications minimales

- `dotnet test Server/tests/BeeKingdom.HiveOperations.Tests/BeeKingdom.HiveOperations.Tests.csproj` : tests métier découverts et verts.
- `dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj --filter FullyQualifiedName~GameWorkshopBatchQualificationEndpointTests` : tests HTTP découverts et verts.
- Vérifier 503 fermé avant lecture repository, 401 sans Bearer, 400 identifiants/charge invalides.
- Avec une ruche de test appartenant au joueur A : mauvaise réponse 200 sans progression, bonne réponse 200 avec une seule révision supplémentaire, rejeu identique strictement corrélé, clé contradictoire 409.
- Joueur B et autre ruche : aucun état ni reçu visible.
- Comparer avant/après ressources, opérations, minuteries et progression économique : aucune mutation économique.

## Promotion

Ne promouvoir qu’après preuves SQL, shell authentifié mobile, TLS/SNI/IIS et Android staging. Maintenir `DeploymentAuthorized=false`, `Chat/Realtime=false` et ne jamais synchroniser automatiquement.
