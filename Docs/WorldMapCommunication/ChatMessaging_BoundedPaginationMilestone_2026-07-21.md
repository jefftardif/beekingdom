# Bee Kingdom - Jalon pagination bornee du chat

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Le provider charge maintenant conversations et messages sur plusieurs pages avec
une politique explicite:

- taille de page de 1 a 100;
- maximum de 1 a 100 pages par operation;
- annulation propagee entre chaque page;
- curseurs encodes dans l'URL;
- doublons de conversations fusionnes par identifiant;
- messages fusionnes par sequence;
- curseur repete, cyclique ou sans progression refuse;
- limite atteinte exposee par `IsComplete=false`, jamais presentee comme une
  synchronisation terminee.

`RemoteConversationLoadResult` et `RemoteReconciliationResult` exposent le nombre
de pages, la prochaine position et l'etat complet/incomplet. Le polling normal
utilise desormais cette reconciliation multi-pages bornee.

## Verification

- 46 tests Communication executes;
- 46 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux scenarios couvrent doublon entre deux pages de conversations,
curseur cyclique, deux pages de messages contigues et limite d'une page avec
resultat explicitement incomplet.

## Handoff Integrateur

Le serveur doit garantir des curseurs opaques stables, strictement progressifs et
lies aux filtres/audience de la requete. `nextAfterSequence` doit etre superieur au
curseur fourni et correspondre a la derniere sequence effectivement retournee.
Une page repetee avec le meme curseur doit etre identique tant que l'autorisation
ne change pas.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_BoundedPaginationMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, secret, drapeau de production ou synchronisation n'a ete
ajoute ou active.
