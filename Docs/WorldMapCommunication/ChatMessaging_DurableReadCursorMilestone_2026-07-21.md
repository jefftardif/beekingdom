# Bee Kingdom - Jalon curseurs de lecture durables

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Les lectures sont maintenant journalisees par conversation avant le POST serveur.
Le journal conserve uniquement la sequence maximale et fusionne toutes les
lectures plus anciennes.

L'acquittement utilise une suppression conditionnelle: il retire le curseur
uniquement si la valeur encore stockee est inferieure ou egale a la sequence
acquittee. Une lecture plus recente apparue pendant la requete reste donc dans le
journal et sera envoyee ensuite.

Politique:

- succes: suppression jusqu'a la sequence acquittee;
- 401, 429, 5xx, annulation ou perte reseau: maximum conserve;
- 403/404: curseur de la conversation retire;
- redemarrage: reprise de chaque maximum en attente;
- sequence plus faible: aucune regression;
- corruption/version inconnue: valeur preservee et erreur explicite.

`VersionedChatPendingReadStore` fournit le schema v1 sur un `IChatStringStore`
injecte. Il ne conserve aucun corps de message.

## Verification

- 42 tests Communication executes;
- 42 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux scenarios couvrent perte reseau puis redemarrage, fusion 7 vers 9,
refus de regression 10 vers 4, acquittement 5 concurrent avec lecture 8, et
round-trip du journal versionne.

## Handoff Integrateur

Le serveur doit appliquer `ReadCursorSequence = max(valeur_actuelle, sequence)`
dans une transaction atomique. Un retry ou une arrivee hors ordre ne doit jamais
faire regresser le curseur, les non-lus ou les mentions. La reponse doit representer
la valeur effective apres fusion, pas seulement la valeur demandee.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_DurableReadCursorMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Gameplay/Communication/VersionedChatPendingSendStore.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, secret, drapeau de production ou synchronisation n'a ete
ajoute ou active.
