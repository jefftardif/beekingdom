# Chat — isolation des données volatiles de session (2026-07-21)

## Résultat

La séparation compte-partition couvre maintenant aussi les données conservées uniquement en mémoire par `ServerChatProvider`.

Une traduction déjà mise en cache n’est jamais retournée avant validation de la session courante. Les événements temps réel et l’inspection des files persistantes valident également la session avant de lire ou fusionner des données.

Lors d’une déconnexion normale ou de la détection d’un compte différent, le fournisseur purge immédiatement :

- les messages fusionnés REST/temps réel ;
- les reçus indexés par `ClientRequestId` ;
- les traductions terminées en cache ;
- les dernières séquences confirmées.

Les journaux persistants ne sont pas supprimés : ils demeurent chiffrés et attribués à leur partition afin que le compte d’origine puisse les reprendre après reconnexion.

## Frontière de session

- cache traduction : session validée avant toute réponse locale ;
- événement temps réel : session validée avant fusion ;
- statut et drainage des files : identité validée avant lecture du journal ;
- renouvellement 401 vers un autre joueur : cache purgé, seconde requête bloquée, opération persistante conservée ;
- déconnexion : capacités et données volatiles invalidées ensemble.

## Validation

- traduction de `p1` mise en cache puis session changée vers `p2` : `LocalAccountMismatch`, aucun nouveau HTTP, aucune traduction de `p1` retournée ;
- données temps réel et séquence de `p1` purgées dès la détection de `p2` ;
- déconnexion puis nouvelle traduction du même message : nouvel appel serveur requis, preuve que le cache a été vidé ;
- journaux et comportements normaux préexistants conservés ;
- suite isolée Communication : **122/122 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur et staging

Le serveur doit vérifier l’autorisation du joueur avant toute consultation de cache partagé, notamment pour les traductions. La validation Android doit prouver qu’après déconnexion de A, aucun message, texte traduit, séquence, compteur ou reçu de A ne reste visible pendant la session B, tout en permettant à A de reprendre ses propres opérations persistantes lors de son retour.

Le candidat serveur courant communiqué par l’Intégrateur est `BeeKingdom.Server.20260721T201425Z`, `DeploymentAuthorized=false`. Les portes SQL jetable, .NET 8 natif, TLS/SNI/IIS et Android staging restent ouvertes.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
