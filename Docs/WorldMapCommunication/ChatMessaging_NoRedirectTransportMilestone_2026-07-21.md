# Chat — transport sans redirection automatique (2026-07-21)

## Résultat

`UnityWebRequestChatRestTransport` fixe désormais explicitement `redirectLimit` à zéro sur chaque requête.

Le client ne suit donc automatiquement aucune réponse HTTP 3xx. Une redirection est remise au fournisseur comme une réponse non réussie et ne peut pas transférer implicitement :

- le jeton `Authorization: Bearer` vers une autre origine ;
- un corps de message ou de modération vers une autre route ;
- une requête capabilities vers une page de connexion ou une façade différente.

La destination demeure exclusivement celle validée par `ChatEndpointUrl` sous la route canonique `/chat/v1`.

## Validation

- Politique centrale testée : limite de redirection égale à zéro.
- Suite isolée Communication : **91/91 tests réussis**.
- Compilation du harnais : aucune erreur ni aucun avertissement.

## Directive serveur et staging

Les endpoints `/chat/v1` doivent répondre directement, sans 301, 302, 303, 307 ou 308. Le préflight et les tests HTTP doivent vérifier au minimum capabilities, conversations, messages, lecture, signalement et traduction. Le nom HTTPS final, le chemin et le schéma doivent être corrects dès la première requête; aucune réécriture corrective visible par le client n’est acceptable.

Le prochain candidat local doit incorporer ces contrôles et rester `DeploymentAuthorized=false`. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé par ce jalon.
