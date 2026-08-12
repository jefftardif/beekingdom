# Bee Kingdom - Jalon renouvellement de session chat

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Le client accepte maintenant une source de session renouvelable sans connaitre ni
persister le mecanisme de connexion. Apres un 401 authentifie:

1. il demande un seul renouvellement;
2. il repete une seule fois la requete avec le nouveau jeton;
3. un second 401 arrete le parcours et expose `AuthenticationRequired`;
4. aucune boucle de renouvellement n'est possible;
5. une annulation pendant le renouvellement empeche la seconde requete.

Le meme principe est applique a la connexion temps reel. Les autres erreurs de
connexion temps reel basculent vers polling, mais un echec d'authentification
reste explicite.

Le jeton n'entre ni dans l'outbox, ni dans le cache de traduction, ni dans les
messages. Seul le transport recoit la valeur courante pour l'en-tete Bearer.

`/chat/v1/capabilities` est maintenant traite comme endpoint public: aucune
session n'est demandee et aucun en-tete Bearer n'est ajoute.

## Verification

- 27 tests Communication executes;
- 27 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux scenarios couvrent capacites publiques, 401 puis succes, deux 401
consecutifs, un seul renouvellement, et annulation avant la seconde requete.

## Handoff Integrateur

Le serveur doit garantir que 401 signifie uniquement jeton absent, invalide,
revoque ou expire. Une requete rejetee 401 ne doit produire aucun effet metier,
afin que sa repetition apres renouvellement reste sure. Les endpoints publics de
capacites doivent rester utilisables sans session et ne jamais divulguer de
configuration sensible.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_SessionRefreshMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun secret, deploiement, drapeau de production ou synchronisation n'a ete
ajoute ou active.
