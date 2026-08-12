# Chat — échéance REST bornée (2026-07-21)

## Résultat

Chaque requête REST Unity possède maintenant une échéance explicite.

- valeur par défaut : 30 secondes ;
- configuration par `RemoteChatClientOptions.RequestTimeout` ;
- valeur strictement positive et limitée à 120 secondes ;
- conversion vers les secondes entières de Unity par arrondi supérieur ;
- rejet immédiat d’une configuration nulle, négative ou excessive.

La propriété `UnityWebRequest.timeout` est appliquée à chaque requête en même temps que la politique sans redirection. L’annulation fournie par l’appelant reste active et peut interrompre plus tôt. Un dépassement réseau demeure une erreur de transport : les opérations persistantes ne sont pas acquittées et pourront être reprises selon les règles idempotentes existantes.

## Validation

- défaut 30 secondes ;
- minimum effectif 1 seconde ;
- arrondi de 1,5 seconde vers 2 secondes ;
- maximum 120 secondes ;
- refus de zéro, valeur négative et 121 secondes ;
- refus des configurations invalides par la composition complète du client.

Suite isolée Communication : **97/97 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

Les délais serveur/proxy doivent être cohérents avec cette fenêtre. Une mutation acceptée avant une coupure doit conserver son reçu idempotent afin que la reprise du client ne crée aucun doublon. Les tests HTTP doivent simuler : réponse avant 30 secondes, absence de réponse, coupure après commit avant réception, reprise avec le même `clientRequestId`, 503 avec `Retry-After`, et absence d’acquittement local lors d’un timeout.

Le serveur ne doit pas maintenir inutilement une requête abandonnée; le jeton d’annulation doit se propager jusqu’aux opérations non commitées. Un commit déjà durable ne doit jamais être annulé logiquement après coup.

Le prochain candidat reste `DeploymentAuthorized=false` jusqu’aux portes SQL, HTTP .NET 8, TLS et Android. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
