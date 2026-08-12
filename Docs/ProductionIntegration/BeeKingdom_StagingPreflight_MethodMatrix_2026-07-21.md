# Préflight staging — matrice de méthodes

Le préflight `Server/tools/Test-ChatStagingPreflight.ps1` vérifie désormais, en plus de TLS/SNI, `/chat/v1/capabilities` sans bearer et sans redirection:

- `GET /capabilities` est la seule négociation acceptée;
- `POST`, `PUT` et `DELETE /capabilities` doivent être refusés directement (4xx), sans redirection ni session;
- les routes métier restent testées sans bearer et doivent répondre 401 directement;
- aucun corps, cookie, token ou URL complète n’est journalisé par le préflight.

Le script est syntaxiquement valide. Aucun hôte staging autorisé n’étant configuré dans la VM, aucun appel externe n’a été effectué.
