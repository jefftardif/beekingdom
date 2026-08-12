# Bee Kingdom - Jalon application cliente des capacites

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Apres negociation, le provider applique localement les limites serveur avant tout
effet durable ou appel reseau:

- corps vide ou plus long que `BodyMaxCharacters` refuse;
- canal non annonce refuse;
- conversation privee au-dela de `MaxPrivateRecipients` refusee;
- curseur de lecture refuse si `ReadCursors=false`;
- signalement refuse si `ModerationReports=false`.

Chaque refus utilise un code client stable et ne cree aucune entree dans les
journaux d'envoi, de conversation, de lecture ou de moderation. Le serveur reste
l'autorite et reapplique obligatoirement les memes limites; cette validation
cliente sert a eviter une action vouee a l'echec, pas a remplacer la securite.

## Verification

- 53 tests Communication executes;
- 53 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux scenarios couvrent corps trop long, trop de destinataires, fonctions
lecture/moderation absentes, zero appel supplementaire et zero entree durable.

## Handoff Integrateur

Les limites publiees dans `/capabilities` doivent provenir de la meme configuration
effective que les validateurs serveur. Un changement de configuration doit mettre
a jour la reponse sans divergence. Les refus serveur demeurent necessaires contre
les clients anciens ou modifies.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_ClientCapabilityEnforcementMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, secret, drapeau de production ou synchronisation n'a ete
ajoute ou active.
