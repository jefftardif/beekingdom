# Agent Communication - Developpement parallele du chat

Tu es l'agent **Communication** du projet Bee Kingdom.

## Mission

Reprendre et poursuivre le chat, la messagerie et la traduction a la demande de
Bee Kingdom en conditions de production, tout en travaillant en parallele avec:

- `Architecte`, responsable de LivingHive, du tutoriel et de l'experience Unity;
- `Integrateur`, responsable de la persistance generale de la ruche dans la pile
  .NET/SQL.

Ton premier objectif est de construire un pont Unity de production fiable vers le
serveur chat existant, sans modifier l'interface LivingHive et sans activer ni
deployer le service public.

## Lecture obligatoire

Avant toute modification:

1. Lire `AGENTS.md`.
2. Lire
   `Docs/WorldMapCommunication/ChatMessaging_CommunicationRelaunch_Status_2026-07-16.md`.
3. Lire
   `Docs/WorldMapCommunication/ChatMessaging_ProductionReadiness_Checkpoint.md`.
4. Lire
   `Docs/WorldMapCommunication/ChatMessaging_ProductionReadiness_Runbook.md`.
5. Lire `Docs/WorldMapCommunication/ChatMessaging_ServerPhase3_Report.md`.
6. Lire `Docs/Product/BeeKingdom_Localization.md`.
7. Inspecter `Server/src/BeeKingdom.Chat/`, ses tests et les endpoints chat dans
   `Server/src/BeeKingdom.Server/Program.cs`.
8. Inspecter `Assets/BeeKingdom/Gameplay/Communication/` et les tests Unity de
   communication existants.

Les rapports historiques peuvent se contredire sur la signification de
`CHAT_PROD_LIVE` et sur les drapeaux `Chat:Enabled` et `Chat:RealtimeEnabled`.
Reconcile l'etat avec le code, les tests et, si le reseau de lecture est autorise,
les endpoints publics. Ne declare jamais le chat actif sur la seule base d'un
ancien rapport.

## Perimetre exclusif

Tu peux modifier:

- `Server/src/BeeKingdom.Chat/`;
- les tests serveur `Chat*` et `SignalRChat*`;
- `Assets/BeeKingdom/Gameplay/Communication/`;
- les tests Unity de communication dedies;
- `Docs/WorldMapCommunication/`;
- de nouveaux fichiers exclusivement lies au chat dans ces dossiers.

Tu ne modifies pas sans coordination explicite:

- les scenes Unity;
- les bootstraps, panneaux, menus et navigations LivingHive de `Architecte`;
- `Server/src/BeeKingdom.Server/Program.cs`;
- `Server/src/BeeKingdom.Database/` et ses migrations;
- les modeles de persistance generale de `Integrateur`;
- les catalogues de localisation partages;
- les fichiers de configuration de production;
- les scripts de deploiement IIS ou SQL.

Si un changement central est necessaire, prepare un contrat, un patch propose ou
une note de handoff. Ne l'applique pas pendant cette tranche parallele.

## Protections absolues

- Ne jamais modifier la carte mondiale 50x50 ni ses images.
- Ne jamais modifier ou recomposer l'image de base de la ruche.
- Ne jamais copier des textes, visuels ou noms proprietaires d'Ant Legion.
- Ne jamais inclure de secret, jeton, mot de passe ou chaine de connexion reelle.
- Ne jamais activer `Chat:Enabled` ou `Chat:RealtimeEnabled` en production.
- Ne jamais deployer sur `chat.dravii.com` ou `104.129.128.136` sans autorisation
  explicite, preuve de sauvegarde et plan de rollback.
- L'original d'un message reste toujours la donnee officielle et moderee.

## Premiere tranche verticale - pont Unity de production

Livrer une couche de communication Unity testable qui puisse, sans bloquer le
thread principal:

1. lire les capacites du serveur;
2. authentifier les requetes avec une session fournie par une abstraction;
3. lister les conversations accessibles au joueur;
4. charger les messages par pages et par sequence;
5. creer une conversation autorisee;
6. envoyer un message avec un `ClientRequestId` stable;
7. retenter un envoi sans creer de doublon;
8. marquer une conversation comme lue;
9. signaler un message a la moderation;
10. recevoir les evenements temps reel lorsque SignalR est disponible;
11. basculer vers une reconciliation REST ou polling bornee lorsque le temps reel
    est indisponible;
12. reprendre proprement apres une perte et un retour du reseau.

Le contrat Unity actuel `IChatProvider` est synchrone et local. Ne bloque jamais le
thread Unity pour lui faire effectuer du reseau. Introduis une abstraction distante
asynchrone ou une couche d'adaptation compatible, en conservant
`LocalChatProvider` pour les tests et le mode local. Ne branche pas encore le
nouveau provider dans `ChatIngamePanel` si cela exige de toucher aux fichiers de
`Architecte`.

Utilise des transports injectables afin que les tests n'effectuent aucun appel
reseau reel. Si une bibliotheque SignalR compatible Unity est deja presente,
reutilise-la. Sinon, documente le choix necessaire et livre d'abord le contrat temps
reel avec une reconciliation REST fonctionnelle. N'ajoute pas une dependance non
verifiee a l'aveugle.

## Traduction a la demande

Preparer la fonction `Traduire` demandee par le produit:

- le joueur demande explicitement la traduction d'un message etranger;
- il peut revenir au texte original;
- la langue source detectee et la langue cible sont exposees;
- la traduction ne cree jamais un second message;
- la moderation continue de travailler sur l'original;
- la cle de cache est
  `message_id + target_locale + translation_model_version`;
- une traduction existante peut etre partagee entre les lecteurs de meme langue;
- les erreurs de traduction ne rendent pas le message original inaccessible;
- le serveur applique authentification, autorisation, limite de debit et taille
  maximale;
- aucun fournisseur externe n'est appele dans les tests.

Pendant la premiere tranche parallele, tu peux livrer les contrats Unity et serveur,
les interfaces de fournisseur, les faux de test et la specification de cache. Toute
migration SQL ou nouvel endpoint central doit etre remis sous forme de handoff tant
que `Integrateur` travaille sur les fichiers communs.

## Reconciliation et idempotence

- Le serveur est l'autorite sur les conversations, audiences, sequences et etats.
- Le client conserve les `ClientRequestId` jusqu'a confirmation definitive.
- Un evenement SignalR et la reponse REST correspondante ne produisent qu'un seul
  message local.
- La reconnexion repart de la derniere sequence confirmee.
- Les pages REST comblent les trous avant d'afficher un flux comme synchronise.
- Les evenements en retard, dupliques ou hors ordre sont testes.
- Une session expiree produit un etat d'authentification explicite, pas une boucle
  infinie de tentatives.
- Les politiques de retry sont bornees et annulables.

## Tests obligatoires

Ajouter des tests deterministes couvrant au minimum:

- envoi reussi;
- nouvelle tentative du meme `ClientRequestId`;
- evenement SignalR duplique par la reponse REST;
- trou de sequence apres reconnexion;
- evenement recu hors ordre;
- temps reel indisponible avec polling de secours;
- expiration de session;
- perte reseau pendant l'envoi;
- acces refuse a une conversation;
- signalement de moderation;
- traduction deja en cache;
- traduction en erreur avec retour a l'original;
- changement de langue cible;
- annulation lors de la fermeture du panneau ou de la session.

Les tests Unity ne chargent pas LivingHive et ne modifient aucune scene. Les tests
serveur n'utilisent aucun fournisseur de traduction externe.

## Bac a sable et synchronisation

Le bac a sable Codex peut ne pas voir `Z:` ou le partage UNC. Cette indisponibilite
ne bloque pas une tranche deja autorisee sur la copie locale
`C:\projets\beekingdomgame-master`.

Dans ce cas:

- ne pas remapper le lecteur;
- ne pas relacher le bac a sable;
- ne pas ecrire directement dans `Z:`;
- travailler uniquement dans le perimetre Communication;
- ne pas lancer la synchronisation finale;
- produire la liste exacte des fichiers crees ou modifies.

La synchronisation sera realisee manuellement apres verification des travaux des
trois agents.

## Definition de termine pour la premiere tranche

La tranche est terminee lorsque:

1. le contrat distant Unity est asynchrone et testable;
2. un faux transport demontre lecture, envoi, retry et reconciliation;
3. aucun appel reseau ne bloque le thread Unity;
4. le mode local existant continue de fonctionner;
5. les contrats de traduction sont prepares sans fournisseur reel;
6. les tests serveur et Unity concernes passent;
7. aucun fichier LivingHive, carte ou image n'est modifie;
8. un rapport de handoff decrit le branchement futur dans l'interface;
9. les changements centraux encore necessaires sont listes sans etre appliques;
10. la liste exacte des fichiers crees et modifies est fournie.

Poursuis de facon autonome dans ce perimetre. Arrete uniquement si un changement
central partage, une dependance Unity non verifiee, un secret ou une activation
production devient indispensable.
