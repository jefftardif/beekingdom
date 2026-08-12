# Chat et messagerie — capabilities avant tout accès distant

Date : 2026-07-21  
Responsable : Communication

## Résultat

Le mode strict de la fabrique distante exige maintenant une négociation réussie avant toute opération authentifiée, en lecture comme en écriture. La garde couvre les messages, créations, lectures, signalements, drainage, listes de conversations, réconciliation/polling et traduction.

Avant négociation, chaque chemin retourne `capability_negotiation_required` avec HTTP 0. Aucun journal n'est consulté ou modifié, aucune session n'est demandée et aucun appel métier n'atteint le transport. `/chat/v1/capabilities` demeure le seul appel distant autorisé dans cet état et reste sans bearer.

Après une réponse active et compatible, l'acquisition de session commence seulement au premier accès authentifié. Le client utilise alors les limites, fonctionnalités et la rétention effective négociées.

## Validation

- Compilation isolée Communication : réussie, sans erreur ni avertissement.
- Suite ciblée : 81/81 réussie.
- Huit chemins stricts refusés avant négociation.
- Zéro appel REST, zéro acquisition de session et quatre journaux vides après ces refus.
- La négociation capabilities elle-même n'acquiert aucune session.
- Le premier envoi après négociation réussit et acquiert exactement une session.
- Aucun déploiement, activation ni synchronisation effectué.

## Directive d'intégration

Le serveur et les proxies doivent garantir que `/chat/v1/capabilities` est la seule route chat publique sans bearer et qu'elle ne redirige pas vers une page de connexion. Toutes les autres routes REST et temps réel restent authentifiées. En staging, capturer le trafic d'un démarrage à froid : première requête chat = capabilities sans Authorization; aucune autre requête avant validation; première mutation après validation = bearer attendu. Tester aussi capabilities désactivées/incompatibles : aucune session, aucun drainage, aucune traduction et aucun websocket. Le prochain candidat local doit inclure `idempotencyReceiptRetentionDays` et ces tests avant de remplacer le candidat courant révoqué; `DeploymentAuthorized=false` demeure obligatoire.
