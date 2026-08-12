# Pont de cycle de session — audit serveur

## Vérifications

Les routes métier appellent `AuthenticateGameRequest`/l'authentification chat et prennent le joueur exclusivement depuis le bearer validé; aucun `playerId` de corps n'est utilisé pour les opérations chat. Les repositories et reçus sont indexés par joueur/ruche, et les conversations/curseurs/traductions relisent ce joueur authentifié. Le refresh produit une session du même joueur attesté par le jeton; aucun changement de compte silencieux n'est accepté par la couche de validation.

La matrice A retardée → logout → B → retour A est donc une exigence de staging à exécuter avec deux jetons réels : la réponse tardive A doit être ignorée côté client et une reprise A avec la même clé doit relire le reçu sans second effet. Elle n'est pas simulée ici et aucune route ou événement n'a été ajouté.

## Preuve d'exécution

- `dotnet --list-runtimes` : uniquement .NET/ASP.NET **10.0.10**; aucun runtime .NET 8 natif installé.
- Commande ciblée HTTP avec `DOTNET_ROLL_FORWARD=Major` : build réussi, mais **0 test découvert** dans `BeeKingdom.Tests`.
- Suite complète .NET 8 native, SQL jetable, TLS/IIS et Android staging : non ratifiées faute de runtime/environnements requis.

Fichiers modifiés : uniquement ce rapport. Aucun candidat n'a été créé ou promu; `ChatEnabled=false`, `RealtimeEnabled=false` et `DeploymentAuthorized=false` sont conservés. Aucun secret, transfert ou activation.
