# Bee Kingdom - Jalon negociation des capacites

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Avant toute session joueur ou connexion temps reel, le client lit maintenant les
capacites publiques et valide:

- protocole exact `chat-v1`;
- activation du serveur;
- presence de limites valides;
- fonctions lecture, moderation, livraison hors ligne, mentions et emojis;
- canaux annonces;
- disponibilite temps reel.

Comportement:

- serveur desactive: arret avant session, raison `server_disabled`;
- protocole incompatible: arret avant session, raison `protocol_incompatible`;
- limites invalides: arret avant session;
- temps reel non annonce ou transport absent: polling REST;
- temps reel annonce et transport disponible: tentative de connexion authentifiee.

Les capacites ne recoivent aucun header Bearer et ne contiennent aucune donnee
sensible. La reponse negociee reste accessible au futur panneau pour appliquer les
limites sans valeurs inventees.

## Verification

- 50 tests Communication executes;
- 50 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux scenarios couvrent mapping complet camelCase, limites, serveur
desactive sans session, polling annonce et protocole incompatible.

## Etat serveur consolide recu

`Docs/ProductionIntegration/ChatMessaging_ServerConsolidation_2026-07-21.md`
rapporte une compilation Release propre et 16/16 tests serveur. Les codes finaux
400/401/403/404/409/429/503, traduction, idempotence, egalite event/REST et curseur
monotone sont valides localement.

Restent explicitement ouverts avant staging/production:

- pagination opaque des conversations;
- transaction SQL atomique rapport + recu et concurrence;
- purge generale des recus et preuves sur base SQL jetable;
- rerun HTTP sous runtime .NET 8.

Ces points ne sont pas declares livres par ce jalon client.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_CapabilityNegotiationMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, secret, drapeau de production ou synchronisation n'a ete
ajoute ou active.
