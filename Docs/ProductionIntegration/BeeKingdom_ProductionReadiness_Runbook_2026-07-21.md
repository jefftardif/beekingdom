# Runbook de préparation production — Bee Kingdom

## Séquence locale obligatoire

1. Vérifier la configuration fail-closed:
   `powershell -File Server/tools/Test-ProductionConfiguration.ps1`
2. Vérifier le candidat et son manifeste, puis le smoke Production loopback:
   `powershell -File Server/tools/Test-CandidateLocalPreflight.ps1 -CandidatePath Server/artifacts/candidates/<candidat> -RunSmoke`
3. Exécuter les tests serveur ciblés et la suite complète via:
   `powershell -File Server/tools/New-ProductionCandidateLocal.ps1`

Le résultat attendu est `Healthy`, `ChatEnabled=false`, `RealtimeEnabled=false`, `PreparationOnly` et `DeploymentAuthorized=false`.

## Porte staging explicite

Après attribution d’un hôte staging autorisé, exécuter uniquement:

`powershell -File Server/tools/Test-ChatStagingPreflight.ps1 -BaseUrl https://<hote>/chat/v1`

Le préflight vérifie TLS/SNI, chaîne et durée du certificat, absence de redirection, cache capabilities non stockable, bornes annoncées, méthodes HTTP et 401 sur les routes métier sans bearer.

## Portes non franchies dans la VM

- aucune chaîne SQL réelle ni test SQL jetable;
- aucun hôte staging/TLS autorisé;
- aucun fournisseur de traduction externe;
- aucune activation publique, synchronisation ou transfert.

Les secrets, bearer, chaînes de connexion et clés d’administration doivent être injectés hors dépôt uniquement lors d’une tranche explicitement autorisée.

État VM vérifié le 2026-07-21: seuls les runtimes .NET 10.0.10 sont installés et aucun service SQL LocalDB/SQL Server attendu n’est présent; les tests SQL et l’exécution .NET 8 restent donc explicitement en attente d’un environnement autorisé.
