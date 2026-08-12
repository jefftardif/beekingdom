# Bee Kingdom - Jalon session de synchronisation chat

Date: 2026-07-21  
Agent: `Communication`

## Livraison

`ChatConversationSynchronizer` orchestre maintenant une session de conversation
sans dependre de l'interface LivingHive:

- connexion au demarrage;
- reconciliation initiale et polling a partir de la derniere sequence confirmee;
- remise a zero du compteur de reprise apres un succes;
- nombre borne de cycles de recuperation;
- propagation immediate d'une session expiree;
- annulation lors de la fermeture ou du changement de panneau;
- deconnexion garantie dans un bloc `finally`, meme apres erreur ou annulation.

La politique de session permet de configurer l'intervalle de polling et zero a
vingt cycles de recuperation. Elle s'ajoute aux tentatives bornees de chaque
requete, sans produire de boucle de retry infinie.

## Verification

- 12 tests executes;
- 12 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Le nouveau scenario ouvre une session, publie un instantane, simule la fermeture
du panneau, verifie l'annulation et confirme une seule deconnexion avec retour a
l'etat `Offline`.

## Handoff

Integrateur doit conserver des pages `afterSequence` idempotentes pendant les
reconnexions et ne jamais avancer un curseur au-dela d'un message non retourne.
Les sessions expirees doivent rester des 401 explicites; les erreurs transitoires
ne doivent pas etre transformees en 401/403.

Architecte pourra creer un `CancellationTokenSource` par panneau/conversation,
executer `RunAsync`, puis annuler le jeton avant de remplacer la conversation ou
fermer l'interface. Aucun changement d'interface n'est applique dans ce jalon.

## Fichiers du jalon

Crees:

- `Assets/BeeKingdom/Gameplay/Communication/ChatConversationSynchronizer.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ChatConversationSynchronizer.cs.meta`
- `Docs/WorldMapCommunication/ChatMessaging_SynchronizationSessionMilestone_2026-07-21.md`

Modifie:

- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, aucune activation et aucune synchronisation n'ont ete faits.
