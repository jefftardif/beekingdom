# Mandat parallele VM - Communication

**Date de coordination:** 2026-07-21  
**Agent:** `Communication`  
**Copie de travail:** `C:\projets\beekingdomgame-master`

## Instruction

Tu es `Communication`, responsable exclusif du chat, de la messagerie, du temps
reel et de la traduction a la demande de Bee Kingdom.

Le present fichier est ton mandat executable et se suffit a lui-meme. Si le fichier
suivant est deja disponible dans la copie locale, utilise-le comme specification
detaillee complementaire sans bloquer s'il est absent:

`Docs/WorldMapCommunication/Communication_Agent_ParallelProduction_Goal.md`

Lis egalement:

1. `AGENTS.md`;
2. `Docs/WorldMapCommunication/ChatMessaging_CommunicationRelaunch_Status_2026-07-16.md`;
3. `Docs/WorldMapCommunication/ChatMessaging_ProductionReadiness_Checkpoint.md`;
4. `Docs/WorldMapCommunication/ChatMessaging_ProductionReadiness_Runbook.md`;
5. `Docs/WorldMapCommunication/ChatMessaging_ServerPhase3_Report.md`;
6. `Docs/Product/BeeKingdom_Localization.md`.

## Mission exclusive

Livrer la premiere tranche du pont Unity de production vers le serveur chat:

- contrat distant asynchrone et testable;
- transport REST injectable;
- authentification par abstraction de session;
- lecture des capacites, conversations et messages;
- envoi idempotent avec `ClientRequestId`;
- lecture, moderation et reprise reseau;
- reconciliation SignalR et REST/polling;
- contrats de traduction a la demande;
- retour permanent au texte original;
- tests de doublons, sequences, reconnexion et erreurs.

Conserve `LocalChatProvider` pour le mode local. Ne bloque jamais le thread Unity
avec une operation reseau.

## Fichiers attribues

Tu peux travailler dans:

- `Server/src/BeeKingdom.Chat/`;
- les tests serveur `Chat*` et `SignalRChat*`;
- `Assets/BeeKingdom/Gameplay/Communication/`;
- les tests Unity de communication dedies;
- `Docs/WorldMapCommunication/`;
- de nouveaux fichiers chat isoles dans ces dossiers.

## Fichiers interdits pendant le travail parallele

Ne modifie pas:

- les scenes, bootstraps, panneaux, menus ou navigations LivingHive;
- les autres dossiers `Assets/BeeKingdom/Playground/`;
- `Server/src/BeeKingdom.Server/Program.cs`;
- `Server/src/BeeKingdom.Database/` et ses migrations;
- les fichiers de persistance generale de `Integrateur`;
- les catalogues de localisation partages;
- les configurations et scripts de deploiement production;
- `Docs/Product/`, `Docs/Demos/`, `Docs/ProductionIntegration/`;
- `Docs/AgentCoordination/`;
- `AGENTS.md`;
- les scripts de synchronisation.

`Architecte` possede LivingHive. `Integrateur` possede les fichiers serveur
centraux et la base de donnees. Si tu as besoin d'un endpoint, d'une migration ou
d'une cle de localisation partagee, produis une note de handoff sans modifier le
fichier central.

## Protections et production

- Ne modifie jamais la carte 50x50, ses images ou l'image de base LivingHive.
- L'original d'un message demeure la donnee officielle et moderee.
- Une traduction ne cree jamais un second message.
- Aucun fournisseur externe n'est appele dans les tests.
- Aucun secret n'entre dans le depot.
- Ne change pas `Chat:Enabled` ou `Chat:RealtimeEnabled` en production.
- Ne deploie rien sur `chat.dravii.com` ou `104.129.128.136`.
- Reconcile les anciens rapports contradictoires avant toute affirmation de statut.

## Synchronisation

L'indisponibilite de `Z:` dans le bac a sable est attendue. Travaille uniquement
dans `C:` et ne bloque pas pour cette seule raison.

Pendant que les trois agents travaillent:

- ne lance pas `Synchroniser-BeeKingdom.cmd`;
- ne remappe pas `Z:`;
- n'ecris jamais directement dans `Z:`;
- ne tente pas de relacher le bac a sable.

La synchronisation finale sera realisee manuellement apres verification.

## Fin de tranche

Avant de t'arreter:

1. compiler les modules concernes;
2. executer les tests serveur et Unity de communication;
3. produire un rapport dans `Docs/WorldMapCommunication/`;
4. fournir la liste exacte des fichiers crees et modifies;
5. remettre a `Integrateur` les changements centraux requis;
6. remettre a `Architecte` le contrat de branchement UI futur;
7. ne pas synchroniser.

Poursuis de facon autonome tant que la tranche reste dans ce perimetre exclusif.
