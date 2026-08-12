# Chat — reprises de polling strictement transitoires (2026-07-21)

## Résultat

Le polling ne répète plus indistinctement toutes les erreurs. Les seules catégories réessayées sont :

- panne de transport ;
- état hors ligne ;
- limitation de débit.

Les erreurs d’authentification, autorisation, incompatibilité, réponse invalide, opération locale invalide ou stockage local échouent immédiatement. Une réponse structurellement fautive n’est donc pas téléchargée plusieurs fois et ne peut pas provoquer une boucle inutile.

## Délais

- délai de base configurable de 0 à 30 secondes ;
- `Retry-After` serveur utilisé lorsqu’il dépasse le délai de base ;
- `Retry-After` plafonné à 300 secondes côté client ;
- nombre de tentatives toujours borné de 1 à 8 ;
- annulation vérifiée avant chaque tentative et pendant chaque attente.

Après épuisement, une erreur distante typée (`RateLimited`, `Transport`, etc.) est retournée telle quelle plutôt que masquée dans une exception générique. Une exception réseau brute conserve le comportement historique d’épuisement borné.

## Validation

- réponse de message invalide : une requête, aucune attente, `InvalidResponse` préservée ;
- 429 avec `Retry-After: 600` : deux tentatives, une attente plafonnée à 300 secondes, `RateLimited` préservée ;
- perte réseau brute : reprises existantes puis récupération ;
- limite de tentatives réseau toujours respectée ;
- délai de base supérieur à 30 secondes refusé.

Suite isolée Communication : **115/115 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

Les réponses transitoires doivent utiliser les statuts et en-têtes contractuels : 429 avec `Retry-After`, 503 avec `Retry-After` lorsque pertinent. Les 4xx structurels et erreurs d’autorisation ne doivent jamais suggérer une reprise automatique. `Retry-After` doit être une valeur entière bornée et cohérente entre application, IIS et proxy.

Les tests HTTP doivent vérifier 429/503 avec en-tête valide, absent, négatif, excessif et malformé; 400/401/403/404/409 sans boucle; coupure réseau; annulation pendant attente; et absence de rafale synchronisée. Le serveur peut ajouter une légère gigue documentée côté client à l’avenir, mais ne doit jamais dépendre d’une reprise infinie.

Le candidat `183655Z` ne couvre pas ce nouveau jalon. Son successeur doit intégrer les tests, révoquer l’ancien courant et rester `DeploymentAuthorized=false` jusqu’aux validations SQL jetable, .NET 8, TLS/IIS et Android staging.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
