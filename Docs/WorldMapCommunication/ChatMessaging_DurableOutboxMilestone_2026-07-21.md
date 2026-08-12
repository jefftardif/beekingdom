# Bee Kingdom - Jalon outbox durable et reconciliation Inbox

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Les envois distants utilisent maintenant un journal transactionnel avant reseau:

- l'entree est ecrite avant le premier POST;
- `ClientRequestId`, corps, conversation et date client restent identiques apres
  une interruption ou un redemarrage;
- le compteur de tentatives est versionne;
- une confirmation serveur supprime l'entree;
- 401, 429 et erreurs transitoires conservent l'entree;
- 403 et reponse definitive invalide la retirent;
- reutiliser un `ClientRequestId` avec un autre payload est refuse avant reseau;
- `RetryPendingAsync` reprend les envois dans une nouvelle instance du provider.

`VersionedChatPendingSendStore` serialise un journal schema v1 sur un
`IChatStringStore` injecte. Aucun choix de stockage non protege n'est impose au
runtime. Une corruption ou une version inconnue produit une erreur explicite et
la valeur originale est preservee pour recuperation; elle n'est jamais effacee
silencieusement.

## Reconciliation du handoff Integrateur

Integrateur a valide le serveur local avec 10/10 tests et signale que `inbox` est
un objet `ChatInboxEntry`, pas une chaine. Le codec Unity mappe maintenant cet
objet vers `RemoteInboxEntry`, avec conversation, curseur, non-lus, mentions,
muet et archive. Le client conserve aussi `lastSequence` de la conversation.

## Verification

- 18 tests Communication executes;
- 18 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Nouveaux scenarios: reprise apres reconstruction du provider, conservation de la
date originale, acquittement et suppression, collision de cle, session expiree,
round-trip du journal v1, corruption preservee et objet Inbox camelCase.

Le serveur local annonce 10/10 tests pour traduction, idempotence, contrat JSON et
pagination. Ses migrations 061/062 restent preparees mais non appliquees; aucun
deploiement public n'est revendique.

## Handoff Integrateur

Avant production, verifier que les recus serveur de `ClientRequestId` survivent a
un redemarrage SQL, qu'un payload different renvoie un conflit definitif, et que
la retention des recus couvre au moins la fenetre maximale de reprise du client.
Documenter aussi la politique de purge sans supprimer un recu encore necessaire a
la deduplication.

## Fichiers du jalon

Crees:

- `Assets/BeeKingdom/Gameplay/Communication/VersionedChatPendingSendStore.cs`
- `Assets/BeeKingdom/Gameplay/Communication/VersionedChatPendingSendStore.cs.meta`
- `Docs/WorldMapCommunication/ChatMessaging_DurableOutboxMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ChatConversationSynchronizer.cs`
- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucune scene, interface LivingHive, carte, image ou configuration publique n'a ete
modifiee. Aucune synchronisation ni activation n'a ete lancee.
