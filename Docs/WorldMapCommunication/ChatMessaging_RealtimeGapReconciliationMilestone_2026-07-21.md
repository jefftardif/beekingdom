# Bee Kingdom - Jalon reconciliation temps reel

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Le provider maintient maintenant une sequence confirmee par conversation. Les
evenements temps reel sont serialises par une porte asynchrone afin que deux
callbacks concurrents ne puissent pas avancer le curseur dans le desordre.

Lorsqu'un evenement revele un trou:

1. le client conserve la derniere sequence contigue confirmee;
2. il demande les messages REST apres cette sequence;
3. il fusionne la page ordonnee;
4. il fusionne ensuite l'evenement temps reel;
5. il n'avance la confirmation que sur une suite entierement contigue.

Un doublon remplace la meme entree de sequence et ne produit aucun second message.
Un evenement hors ordre reste visible dans le tampon mais non confirme jusqu'a
l'arrivee ou la lecture REST de la sequence manquante.

## Verification

- 23 tests Communication executes;
- 23 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux tests couvrent un evenement 3 recu avant 1 et 2, le comblement REST
avant confirmation, ainsi que 2 puis 1 hors ordre sans avancement premature.
Ils garantissent aussi qu'une panne Unity sans reponse HTTP (`status 0`) reste une
erreur reseau et ne supprime jamais l'entree durable de l'outbox.

## Handoff Integrateur

Le serveur doit conserver la lecture stricte `Sequence > afterSequence`, l'ordre
ascendant et des pages repetables. Les evenements SignalR doivent porter la meme
sequence et le meme `messageId` que la representation REST. Toute divergence doit
etre traitee comme une rupture de contrat avant activation.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_RealtimeGapReconciliationMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, aucune activation et aucune synchronisation n'ont ete faits.
