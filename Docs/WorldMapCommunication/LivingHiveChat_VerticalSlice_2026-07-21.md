# LivingHive Chat — tranche verticale mobile

Date : 2026-07-21  
Responsable : Communication  
État : **ratifié par validation Unity globale**, non activé en production

## Résultat produit

Le point d’entrée Communication est intégré à la Ruche vivante sans modifier la carte 50x50, la scène canonique ni l’image de ruche :

- portrait : `Plus -> Communication`, cible tactile de 50 px, sans seconde barre de chat ;
- paysage : barre Communication de 44 px au-dessus du rail ;
- panneau responsive borné entre le HUD et le rail, avec commandes tactiles d’au moins 44 px ;
- fermeture par `X`, Escape/Back ou changement de surface sans déconnexion du service ;
- brouillon conservé lors d’une fermeture accidentelle et effaçable explicitement au changement de session ;
- conversations, messages, badges et états exclusivement issus du fournisseur réel ;
- historique de session borné, envoi optimiste, outbox persistante, reprise, non-lus, reçus de lecture et traduction/original ;
- événement temps réel réconcilié sans action utilisateur, avec polling borné et annulable lorsque le temps réel est indisponible ;
- réouverture sans seconde connexion temps réel ni seconde boucle de polling.

Il ne subsiste aucun message, conversation, badge ou statut de chat fictif dans le présentateur.

## Frontière appareil / serveur

### Appareil mobile

- rendu responsive, défilement et sélection de canal ;
- brouillon strictement borné à 4 000 caractères ;
- état optimiste de l’envoi ;
- cache récent persistant, versionné, protégé, partitionné par joueur et borné à 100 messages confirmés ;
- outbox locale persistante, protégée et partitionnée par joueur ;
- rejet des réponses tardives appartenant à une ancienne session ;
- réconciliation avec l’état autoritaire du serveur.

Le cache récent est restauré hors ligne puis remplacé ou réconcilié par l’état autoritaire du serveur. Une corruption du blob chiffré provoque une quarantaine vérifiée et bornée à deux slots; la source courante n’est supprimée qu’après relecture réussie de la quarantaine. Les envois optimistes ou encore en attente ne sont jamais enregistrés dans ce cache.

### Serveur autoritaire

- authentification et identité de l’expéditeur ;
- appartenance aux conversations et canaux ;
- ordre monotone et historique persistant ;
- modération, reçus idempotents et curseurs de lecture par joueur ;
- compteurs non-lus ;
- traduction après autorisation de lecture ;
- contrats de reprise après abandon ou changement de session.

La fermeture visuelle de l’overlay n’est jamais un logout et ne doit pas provoquer de déconnexion côté serveur.

## Composition et état honnête

`LivingHiveChatBootstrap` fournit une composition injectable à partir de l’URL de base, de la session authentifiée, du stockage partitionné, du protecteur de données et du transport temps réel. Un logout ou changement de joueur annule, ferme et purge l’ancien contrôleur avant toute reconfiguration.

Le shell mobile de production ne fournit pas encore ce branchement. Tant qu’il n’appelle pas `LivingHiveChatBootstrap.ActivateAsync`, l’interface affiche honnêtement `NotConfigured`; aucun compte, jeton ou statut fictif n’est embarqué.

## Contrat transport attendu

Les DTO JSON serveur consommés par Unity doivent exposer en camelCase :

- conversation : `conversationId`, `title`, `channelType`, `lastSequence`, `readCursorSequence`, `unreadCount`, `mentionCount` ;
- message : `messageId`, `conversationId`, `clientRequestId`, `senderPlayerId`, `senderDisplayName`, `channelType`, `body`, `sequence`, `acceptedAtUtc`.

Les listes de conversations doivent rester filtrées par les appartenances du joueur authentifié. Les reçus, curseurs et compteurs doivent rester cloisonnés par joueur et conversation.

## Fichiers Communication

Créés :

- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatController.cs`
- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatController.cs.meta`
- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatBootstrap.cs`
- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatBootstrap.cs.meta`
- `Assets/BeeKingdom/Gameplay/Communication/VersionedChatRecentCache.cs`
- `Assets/BeeKingdom/Gameplay/Communication/VersionedChatRecentCache.cs.meta`
- `Assets/BeeKingdom/Playground/Editor/LivingHiveChatLayoutTests.cs`
- `Assets/BeeKingdom/Playground/Editor/LivingHiveChatLayoutTests.cs.meta`
- `Assets/BeeKingdom/Playground/Editor/LivingHiveChatCaptureHarness.cs`
- `Assets/BeeKingdom/Playground/Editor/LivingHiveChatCaptureHarness.cs.meta`

Modifiés :

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.fr-CA.json`
- `Assets/_Project/Data/Localization/Resources/Localization/strings.en-US.json`

## Preuves acquises

- suite contractuelle Communication : 131/131 réussie, 0 échec ;
- alignement DTO serveur : `ChatTransportContractTests` 18/18 réussis ;
- contrat serveur documenté dans `Docs/ProductionIntegration/ChatMessaging_ServerDtoAlignment_2026-07-21.md` ;
- candidat serveur local courant : `BeeKingdom.Server.20260721T225554Z`, 55 fichiers, build Release réussi, 253 tests réussis et 7 SQL ignorés, smoke `Healthy` sur loopback ;
- candidat fermé : `ChatEnabled=false`, `RealtimeEnabled=false`, `DeploymentAuthorized=false` ;
- catalogues `fr-CA` et `en-US` : JSON valides, 477 entrées chacun, 0 doublon ;
- recherche des anciens contenus fictifs : 0 occurrence ;
- rectangles testés à 390x844 et 1600x900, avec non-recouvrement HUD/rail et consommation du clic sous l’overlay.
- cache récent protégé : harnais NUnit autonome 138/138 réussis, 0 échec, TRX `LivingHiveChatRecentCacheFinal.trx`.

## Preuves Unity globales ratifiées

État final : **signal vert global indépendant**.

- `Artifacts/LivingHiveChatFinalF8.log` : sortie 0, marqueur F8 présent, 0 `error CS` ;
- `LivingHiveChat_LayoutTests.xml` : 3/3 `Passed` ;
- capture : sortie 0, 0 erreur CS, 0 test en échec, marqueur `LivingHive Communication proofs captured.` ;
- portrait natif `390x844` : SHA-256 `4ee4953cb95e4ca442ab2684cc237a09583c2228f6d7f3654d358aefa252d26a` ;
- paysage natif `1600x900` : SHA-256 `56f84893570ee0d98d85345701c5c4d6b5e5dc136593f835193844dfb177cfa9` ;
- inspection native : overlay lisible et borné, `X`, saisie, `Envoyer` et rail sans collision ;
- état visible honnête : `NotConfigured`, aucun faux message, conversation, badge ou statut ;
- manifeste conforme ;
- scène canonique : 7 776 octets, timestamp inchangé `2026-07-17 17:11:05` ;
- fin de validation : aucun processus Unity ou dotnet résiduel.

### Historique des refus conservé

- première capture portrait : refusée par le harnais, dimensions réelles `390x823` au lieu de `390x844` ;
- cause : la première version dimensionnait la fenêtre Game View et la barre d’outils réduisait la surface rendue ;
- deuxième capture portrait : refusée par le harnais, dimensions réelles `3840x2160` au lieu de `390x844` ;
- cause : la taille fixe avait été ajoutée au groupe Android alors que le batch utilisait la cible active Standalone, faisant sélectionner une option 4K du mauvais groupe ;
- aucun recadrage, redimensionnement ou contournement postérieur n’a été appliqué ;
- ces deux sorties ont été supprimées et n’ont jamais été comptées comme preuves.

Le harnais sélectionne maintenant une `FixedResolution` exacte dans le groupe correspondant à `EditorUserBuildSettings.activeBuildTarget`, via le helper interne `GameViewSizes.GetGroupType` lorsqu’il est disponible et un repli explicite fermé Standalone/Android. Il ne change jamais la cible de build et ne configure la taille qu’une fois lors de chaque changement de spécification. `ValidateDimensions` reste stricte.

## Portes ouvertes

- branchement réel au cycle de session/authentification du shell mobile ;
- validation des DTO enrichis sur le serveur et en staging ;
- SQL jetable, runtime .NET 8 natif, TLS/SNI/IIS et Android staging ;
- activation Chat/Realtime et autorisation de déploiement.

Aucun secret, déploiement, transfert, activation ou synchronisation n’a été effectué.
