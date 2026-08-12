# Remplacement de liaison de session — audit serveur

Les routes serveur revalident l'Authorization bearer à chaque requête via `AuthenticateGameRequest`/la validation d'authentification; le PlayerId est extrait du jeton et non d'un `StoragePartitionId` ou d'un champ client. Les repositories, reçus, curseurs et traductions sont indexés par ce PlayerId authentifié. Un jeton d'un autre joueur ne peut donc pas lire la partition du premier ni produire d'effet sous celle-ci.

La course « ancienne connexion après remplacement » reste une preuve transport/staging à exécuter avec deux connexions réelles : l'ancien bearer doit échouer avant lecture/effet, tandis qu'un renouvellement du même PlayerId est accepté. Aucun état de connexion serveur n'a été modifié dans cette tranche.

Preuve d'environnement : `dotnet --list-runtimes` ne fournit que .NET/ASP.NET **10.0.10**; aucun .NET 8 natif n'est disponible. La découverte .NET 8 native et la suite complète ne peuvent donc pas être ratifiées ici sans contournement. Aucun processus Unity/dotnet/testhost ne tournait au contrôle.

Fichiers modifiés : uniquement ce rapport. `ChatEnabled=false`, `RealtimeEnabled=false`, `DeploymentAuthorized=false`; aucun candidat, transfert ou déploiement.
