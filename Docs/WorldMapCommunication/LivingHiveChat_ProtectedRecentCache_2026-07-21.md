# LivingHive Chat — cache récent protégé

Date : 2026-07-21  
Responsable : Communication  
État : **preuve autonome ratifiée**, intégration Unity globale inchangée

## Résultat

Le client Communication possède maintenant un cache récent persistant pour afficher hors ligne les derniers messages confirmés, puis se réconcilier avec l’état autoritaire du serveur à la reconnexion.

- schéma versionné `v1` ;
- stockage protégé par `IChatDataProtector` et partitionné par joueur ;
- maximum de 100 conversations et 100 messages confirmés par défaut ;
- la conversation sélectionnée est toujours conservée, même au-delà des 100 premières ;
- seuls les messages confirmés de cette conversation sont inclus ;
- aucune outbox, aucun message optimiste et aucun faux état ne sont pris pour vérité serveur ;
- taille sérialisée strictement bornée ;
- restauration hors ligne, puis remplacement/réconciliation lors du retour serveur.

## Quarantaine répétable et bornée

Une donnée protégée illisible est copiée dans un slot temporaire, relue et comparée avant toute suppression de la source. Le slot courant de quarantaine est ensuite remplacé; son prédécesseur est conservé dans un second slot. La rotation reste donc bornée à deux quarantaines plus un slot temporaire transitoire.

Si l’écriture ou la relecture de la nouvelle quarantaine échoue, le blob source courant demeure intact. Deux corruptions successives sont prises en charge sans laisser la source empoisonnée après une quarantaine réussie.

## Frontière appareil / serveur

L’appareil conserve uniquement un aperçu récent protégé, le brouillon, l’état optimiste et l’outbox durable. Le serveur reste seul responsable de l’authentification, des appartenances, de l’ordre, des corps persistants, de la modération, des reçus, des non-lus et de la traduction autorisée. Au retour en ligne, le cache local ne peut ni imposer un message, ni un compteur, ni un curseur de lecture au serveur.

Le logout vide l’état volatil du contrôleur. Le cache persistant demeure dans la partition protégée du joueur afin de permettre sa propre reprise; un autre joueur utilise une partition distincte.

## Fichiers exacts

Créés :

- `Assets/BeeKingdom/Gameplay/Communication/VersionedChatRecentCache.cs`
- `Assets/BeeKingdom/Gameplay/Communication/VersionedChatRecentCache.cs.meta`

Modifiés :

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatClientFactory.cs`
- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatBootstrap.cs`
- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatController.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`
- `Docs/WorldMapCommunication/LivingHiveChat_VerticalSlice_2026-07-21.md`

Aucun fichier du présentateur partagé, du terrain, de la scène canonique ou de l’image LivingHive n’a été modifié dans cette tranche.

## Preuve reproductible

Commande exécutée dans le harnais autonome existant :

`dotnet test CommunicationCompile.csproj --no-restore -v:minimal --logger "trx;LogFileName=LivingHiveChatRecentCacheFinal.trx"`

Résultat :

- compilation `CommunicationCompile.dll` réussie ;
- 138 tests exécutés, 138 réussis, 0 échec, 0 erreur, 0 ignoré ;
- TRX : `C:\Users\tardi\.codex\visualizations\2026\07\21\019f855a-7f5a-70e2-a104-e633cd421a43\TestResults\LivingHiveChatRecentCacheFinal.trx` ;
- fin de passe : Unity=0, dotnet=0, testhost=0.

Les tests couvrent notamment deux corruptions successives, la conservation de la source lorsque la quarantaine ne peut pas être vérifiée, la sélection située après la limite de 100 conversations, le bornage, le cloisonnement joueur, la restauration hors ligne et la réconciliation serveur.

La tentative Unity ciblée antérieure avait compilé sans `error CS`, mais le Test Runner était resté bloqué et n’avait produit aucun XML. Elle reste explicitement non ratifiée et n’est pas comptée dans la preuve ci-dessus. Aucun nouveau batch Unity n’a été lancé.

## Contrepartie serveur préparée

L’Intégrateur a ajouté les garanties autoritaires dans `Server/src/BeeKingdom.Chat/ChatService.cs` et les contrats associés dans `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`. Le détail est conservé dans `Docs/ProductionIntegration/LivingHiveChat_ProtectedRecentCache_ServerAuthoritativeReplay_2026-07-21.md` :

- curseur de lecture borné par la dernière séquence persistée ;
- compteurs recalculés depuis les messages serveur autorisés ;
- appartenance vérifiée avant lecture ;
- reprise strictement ordonnée avec `Sequence > afterSequence` ;
- curseurs de conversations opaques et liés au joueur ;
- reçus toujours cloisonnés par joueur et `ClientRequestId`.

La compilation serveur aboutit, mais le testhost disponible sous .NET 10 n’a découvert aucun test pour les projets ciblant .NET 8. Ces changements serveur restent donc **non ratifiés et non promouvables** jusqu’à une exécution réelle sous le runtime .NET 8 requis. Aucun candidat n’a été remplacé et `DeploymentAuthorized=false` demeure l’autorité.

## Portes restantes

- branchement réel au cycle d’authentification du shell mobile ;
- validation staging de la reprise sur historique serveur réel ;
- activation Chat/Realtime, SQL, .NET 8 natif, TLS/SNI/IIS et Android staging.

Aucun secret, déploiement, transfert, activation ou synchronisation n’a été effectué.
