# Bee Kingdom - Jalon creation durable de conversations

Date: 2026-07-21  
Agent: `Communication`

## Livraison

La creation d'une conversation distante possede maintenant sa propre outbox
versionnee. Avant le premier appel reseau, le client conserve:

- type de canal;
- serveur et monde;
- audience et titre;
- participants normalises;
- `ClientRequestId` stable;
- nombre de tentatives.

Les participants vides et doublons sont retires, puis les identifiants sont tries
ordinalement. Une reconstruction du provider reprend donc exactement le meme
payload canonique, independamment de l'ordre initial de l'interface.

Politique:

- succes: journal acquitte et supprime;
- 401, 429, 5xx ou perte reseau: journal conserve;
- 403, 409 ou reponse definitive invalide: journal retire;
- meme `ClientRequestId` avec payload different: rejet avant reseau;
- corruption/version inconnue: valeur preservee et erreur explicite.

`RetryPendingConversationsAsync` permet la reprise apres redemarrage. Le stockage
reel reste injecte par `IChatStringStore`; aucun stockage non protege n'est impose.

## Verification

- 34 tests Communication executes;
- 34 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux tests couvrent redemarrage, normalisation des participants, collision
locale, 409 definitif, 429 conservable et round-trip du journal schema v1.

## Handoff Integrateur

Le recu SQL de creation doit calculer son hash sur la meme forme canonique:
participants distincts et tries, chaines normalisees selon une politique
documentee. La retention du recu doit depasser la fenetre maximale de reprise du
client. Un conflit de payload doit rester un 409 stable et sans creation partielle.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_DurableConversationCreationMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Gameplay/Communication/VersionedChatPendingSendStore.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, secret, drapeau de production ou synchronisation n'a ete
ajoute ou active.
