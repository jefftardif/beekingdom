# Chat — composition canonique de l’adresse serveur (2026-07-21)

## Résultat

Le client Communication accepte désormais indifféremment :

- l’origine HTTPS, par exemple `https://chat.example.test` ;
- la racine publique canonique, par exemple `https://chat.example.test/chat/v1`.

Dans les deux cas, une route comme `/chat/v1/conversations/...` produit une seule occurrence de `/chat/v1`. Cela corrige le risque de requêtes envoyées vers `/chat/v1/chat/v1/...` avec la configuration staging documentée.

## Garde-fous

- HTTPS obligatoire hors développement loopback explicitement autorisé.
- Chemin de base vide ou exactement `/chat/v1`.
- Refus des identifiants intégrés, paramètres de requête et fragments dans l’adresse de base.
- Refus des routes sortant de l’espace canonique `/chat/v1`.
- Aucune redirection, réécriture serveur ou correction implicite n’est requise.

## Validation

- Compilation du périmètre Communication : aucune erreur ni aucun avertissement.
- Suite isolée : **90/90 tests réussis**.
- Cas vérifiés : origine avec ou sans barre finale, racine `/chat/v1` avec ou sans barre finale, absence de double préfixe et refus des formes ambiguës.

## Porte de production

Le candidat serveur local `BeeKingdom.Server.20260721T170747Z` reste `DeploymentAuthorized=false`. Il doit être révoqué et remplacé par un candidat reconstruit après alignement/validation de ce contrat. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
