# Chat — frontière d’authentification imposée par le transport (2026-07-21)

## Résultat

La séparation entre découverte publique et opérations authentifiées n’est plus seulement une convention du fournisseur. Le transport Unity la vérifie immédiatement avant toute création de requête.

### Capabilities

`/chat/v1/capabilities` doit être exactement :

- méthode `GET` ;
- aucun corps ;
- aucun jeton Bearer ;
- `BypassCache=true`.

### Routes métier

Toute autre route `/chat/v1` doit :

- porter un Bearer valide ;
- ne jamais activer `BypassCache` ;
- utiliser uniquement `GET` sans corps ou `POST` avec corps.

Les autres méthodes, GET avec corps, POST sans corps, route métier sans session, capabilities avec Bearer et contournement de cache hors capabilities sont refusés localement avant réseau.

## Défense en profondeur

Cette vérification complète :

- la négociation capabilities-first du fournisseur ;
- la validation stricte du jeton ;
- l’absence de redirection ;
- les bornes de cible, requête et réponse.

Une future erreur de composition ne peut donc pas faire fuiter un jeton sur la route publique ni appeler une route métier anonymement.

## Validation

- capabilities publique correcte acceptée ;
- GET et POST métier authentifiés acceptés ;
- capabilities avec Bearer ou sans contournement de cache refusée ;
- route métier anonyme ou avec contournement de cache refusée ;
- DELETE, GET avec corps et POST sans corps refusés.

Suite isolée Communication : **113/113 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

Le serveur, IIS et le proxy doivent refléter la même matrice. `/capabilities` est l’unique route chat publique et ne doit jamais déclencher de session, redirection ou cookie d’authentification. Toutes les autres routes REST et la connexion temps réel exigent une identité valide et une autorisation de lecture ou mutation adaptée.

Les tests HTTP doivent parcourir chaque combinaison méthode/route/Bearer/corps/cache et vérifier qu’aucun middleware ne transforme un refus en redirection HTML. Les logs ne doivent contenir ni Bearer, ni cookie, ni corps, ni URL complète avec paramètres.

Le candidat courant ne couvre pas ce nouveau garde-fou tant que ces tests ne sont pas ajoutés. Son successeur doit aussi intégrer l’alignement SQL demandé au jalon précédent, révoquer l’ancien courant et rester `DeploymentAuthorized=false`.

Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
