# M042-CL — Alliance Platform Integration

Mission de nuit, sans supervision interactive (Jeff endormi, brief envoyé
avant de dormir). Reprend le socle serveur M041 (Alliance Platform Core) et
devait le connecter au jeu réel sur 9 fronts (fenêtre Unity, chat, persistance
durable, ingestion d'activité réelle, contrat web, intégrité
diplomatie/guerre, tests Unity/serveur).

**Verdict honnête d'entrée de jeu** : cette session a livré un sous-ensemble
complet, testé et solide (Parts 3, 4, et la portion serveur de Part 9), mais
**n'a pas eu le temps de toucher Part 1 (fenêtre Unity), Part 2 (activité
Unity), Part 5 (ingestion gameplay réelle), Part 6 (contrat web), Part 7
(tests diplomatie/guerre) ni Part 8 (tests Unity)**. Rien de ce qui suit
n'est fabriqué ou approximé : chaque section dit précisément ce qui existe,
ce qui est testé, et ce qui manque encore.

## 1. Executive verdict

Le chat d'alliance (création, synchronisation de membres, autorité serveur
sur les rôles) et la persistance durable (JSON sur disque, un fichier par
agrégat, résistant à un redémarrage réel) sont **construits, câblés dans le
serveur réel (`Program.cs`), et testés** : 434/442 tests serveur verts (8
ignorés, comme avant), 0 échec, aucune régression sur les 35 tests M041.//
La fenêtre Alliance Center dans Unity affiche **toujours** des données
fictives ("ALLIANCE PRIME"/"NEX") — `AllianceClient.cs` existe (créé en
M041) mais n'est **toujours pas instancié** dans
`MobileAccountSessionRuntimeBootstrap.cs`, et
`HiveViewProductUiPresenter.DrawAllianceHeadquartersScreen` n'a subi aucune
modification cette session. Rien côté ingestion d'activité gameplay réelle
n'a été branché non plus. Le CEO ne verra pas encore la vraie ruche demain
en ouvrant Alliance Center — voir section 23 pour le blocage réel et section
24 pour la suite exacte à faire.

## 2. M041 baseline (rappel, vérifié au démarrage)

`git status` en début de session a confirmé la présence de tous les fichiers
M041 non commités (aucun revert nécessaire). Le service `AllianceService`,
les 4 repositories InMemory, les 25 endpoints REST, `AllianceClient.cs`
(Unity, non branché) et les 35 tests M041 étaient tous présents et
fonctionnels tels que rapportés dans `M041-CL-Alliance-Platform-Core.md`.

## 3. Unity Alliance Center wiring (Part 1)

**NON COMMENCÉ.** `HiveViewProductUiPresenter.DrawAllianceHeadquartersScreen`
n'a reçu aucune modification cette session. `BuildAllianceMemberRoster()` et
les valeurs "ALLIANCE PRIME"/"NEX" sont toujours en place.
`MobileAccountSessionRuntimeBootstrap.cs` n'instancie toujours pas
`AllianceClient`. Sous-parties 1A à 1H : aucune faite.

## 4. NO_ALLIANCE flow (Part 1B)

Non fait — dépend de 1A.

## 5. Create/Search/Join (Part 1C/1D)

Non fait — dépend de 1A.

## 6. Applications/Invitations (Part 1G)

Non fait — dépend de 1A.

## 7. Members/Roles (Part 1F)

Non fait — dépend de 1A.

## 8. Activity UI (Part 2)

Non fait.

## 9. Alliance chat creation (Part 3A)

**FAIT ET TESTÉ.** `AllianceService.CreateAlliance` crée maintenant, via
`ChatManager.CreateConversation`, une vraie conversation
`ChatChannelType.Alliance` (audience `alliance:{allianceId:N}`) et sauvegarde
son `ChatConversationId` réel sur l'`AllianceEntity`. Idempotent via le même
`ClientRequestId` que la création d'alliance (préfixé `alliance-chat-`) — une
retry après un vrai crash/reconnect ne crée jamais une deuxième conversation
(`CreateAlliance_RetryDoesNotCreateASecondChatConversation`, vert). Best-effort
: toute exception côté chat est avalée pour ne jamais bloquer la création
d'alliance elle-même (`CreateOrLinkAllianceChat`).

## 10. Chat membership authority (Part 3B/3C)

**FAIT ET TESTÉ.** Synchronisation réelle des participants de chat sur
Join/AcceptApplication/AcceptInvitation (ajout), Leave/Kick (retrait),
Promote/Demote/TransferLeadership (changement de rôle), Dissolve (retrait de
tous les membres — l'équivalent le plus proche d'une archive, `BeeKingdom.Chat`
n'ayant pas de flag d'archive dédié aujourd'hui).

Sécurité (Part 3C, exigence explicite du brief) : `LocalChatAudienceResolver`
ne fait plus confiance au rôle d'alliance déclaré par le client
(`RequesterAllianceRole`) pour les canaux Alliance/Leaders — le rôle réel est
résolu côté serveur via la nouvelle interface `IAllianceMembershipResolver`
(vit dans `BeeKingdom.Chat.Audience` pour éviter une référence circulaire
Chat→Alliance ; implémentée par `BeeKingdom.Alliance.Integration.
AllianceMembershipResolver`, qui interroge `IAllianceRepository.
GetActiveMembership`). `ChatServiceCollectionExtensions` enregistre un
`NullAllianceMembershipResolver` (fail-closed, refuse tout) par défaut afin
que `BeeKingdom.Chat` reste compilable et fonctionnel seul ;
`AllianceServiceCollectionExtensions` enregistre ensuite la vraie
implémentation, qui gagne car appelée après dans `Program.cs`.

Tests dédiés, tous verts : `NonMemberIsDeniedAllianceChannel`,
`RealMemberIsAllowedAllianceChannelWithServerRole`,
`ClientDeclaredRoleIsIgnoredCompletely`, `KickedMemberLosesChatAccess`,
`LeaderRoleComesFromServerMembershipForLeadersChannel`,
`AllianceAnnouncementsRequireOfficerOrLeaderFromServerMembership`
(`ChatAudienceResolverTests.cs`) + 5 tests bout-en-bout avec un vrai
`ChatService`/`AllianceService` construits à la main
(`AllianceChatIntegrationTests.cs`), dont un test qui prouve qu'un membre
exclu perd réellement l'accès en lecture au canal
(`EndToEnd_MemberCanSendMessageAfterJoin_KickedMemberIsRejectedByRealAudienceResolver`).

Deux tests HTTP bout-en-bout préexistants (`ChatMessagingEndpointTests.cs`)
testaient l'ancien comportement insécure (rôle déclaré par le client accepté
tel quel) — ils ont été réécrits pour créer une vraie alliance et une vraie
adhésion via les endpoints réels `/alliance/v1/*` au lieu de déclarer un rôle
fictif (`AllianceAndLeadersChannelsRequireAllianceRoles`,
`AllianceAnnouncementRequiresLeaderRoleAndFanOutParticipants`) — c'est le
changement de comportement correct et attendu explicitement demandé par la
Part 3C du brief, pas une régression.

## 11. Durable persistence (Part 4)

**FAIT ET TESTÉ.** Un repository `DurableJsonAlliance*Repository` par
sous-domaine (core/membres/candidatures/invitations, activité, diplomatie,
guerre), même pattern que `DurableJsonHiveStateRepository` : un fichier JSON
par agrégat, écriture atomique (fichier temporaire + `File.Move`), chaque
repository enveloppe une instance `InMemoryAlliance*Repository` privée pour
toute la logique métier (déjà testée) et ajoute uniquement l'écriture sur
disque + le rechargement au démarrage. Nouvelle surface `Dump*`/`Restore*`
strictement interne (même assembly, jamais exposée via les interfaces
publiques `IAlliance*Repository`, jamais utilisée par `AllianceService`).

Choix explicite et documenté pour SQL Server (exigence 4B du brief) :
`AddBeeKingdomAlliance` lance une `InvalidOperationException` claire et
actionnable si `Persistence:Provider=SqlServer` est configuré, plutôt que de
retomber silencieusement sur DurableJson sous un nom qui prétend SQL. Aucune
migration SQL n'a été commencée — voir section 20.

## 12. Restart validation (Part 4D)

**FAIT ET TESTÉ.** `AllianceDurablePersistenceTests.cs` (2 tests, tous verts) :
crée une alliance complète (membre, candidature, invitation, relation
diplomatique proposée, guerre, événements d'activité), reconstruit un
DEUXIÈME `AllianceService` à partir de repositories fraîchement instanciés
au même chemin disque (simulation réelle d'un redémarrage serveur), puis
vérifie que profil/membres/candidature/invitation/activité/relation
diplomatique/guerre ont tous survécu — et continue de muter après le
"redémarrage" pour prouver que l'état n'est pas juste lisible mais
réellement réutilisable. Un second test vérifie que les reçus d'idempotence
survivent aussi (une retry après redémarrage ne duplique rien).

## 13. Gameplay activity ingestion (Part 5)

**NON COMMENCÉ.** `IAllianceActivityPublisher.PublishForPlayerAsync(...)`
existe (M041) mais n'est appelé nulle part dans le code de gameplay réel.
Aucun des 4 points d'ingestion (Building Upgrade, Research, Combat,
Gathering) n'a été localisé ni câblé cette session.

## 14. Building Upgrade activity (Part 5A)

Non fait.

## 15. Research activity (Part 5B)

Non fait.

## 16. Combat activity (Part 5C)

Non fait.

## 17. Gathering activity (Part 5D)

Non fait.

## 18. Web contract readiness (Part 6)

**PARTIELLEMENT FAIT (M041, non ré-approfondi cette session).** Le vrai
dépôt Web existe à `C:\Users\Utilisateur\source\repos\beekingdom-web`
(confirmé lors de M041/M042, Jeff avait raison de corriger — il n'est
simplement pas dans ce dépôt Unity/serveur). Inspection en lecture seule
faite : aucune route `/alliance/[name]` ni contrat Alliance existant côté
Web à préserver — les endpoints M041/M042 sont le point de départ réel pour
cette future intégration. Aucune vérification supplémentaire de la forme
exacte attendue par le Web n'a été refaite cette session faute de temps.

## 19. Diplomacy/War lifecycle integrity (Part 7)

**NON COMMENCÉ.** Aucun des 4 tests demandés (dissolve→diplomatie
nettoyée, dissolve→guerre gérée de façon déterministe, départ de membre
n'affecte pas la guerre, transfert de leadership garde la guerre sur
l'alliance) n'a été écrit cette session.

## 20. Security

Le changement de sécurité réel de cette session est la Part 3C : le chat
d'alliance n'accepte plus jamais un rôle déclaré par le client — c'est
désormais vérifié par des tests explicites nommés d'après les scénarios
d'attaque du brief (non-membre refusé, membre exclu perd l'accès, rôle
toujours résolu côté serveur). Aucun autre changement de surface de sécurité
n'a été fait cette session (Part 1/5 non commencées, donc aucune nouvelle
surface d'attaque introduite par elles).

## 21. Tests

Suite serveur complète : **434 réussis / 0 échec / 8 ignorés / 442 total**
(`dotnet test` depuis `Server/tests/BeeKingdom.Tests`, ~1m36s). Aucun test
Unity n'a été exécuté cette session (Part 1/8 non commencées, rien de neuf à
tester côté Unity).

Tests flaky documentés en M041 (`Enabled_start_complete_replay_and_conflict_
are_exact`, `PostCapacityFullKeepsStateAndSameHiveIsIsolatedByPlayer`,
flaky uniquement en exécution parallèle) : non ré-observés dans cette
session (le run ci-dessus était vert de bout en bout), mais je n'ai pas
relancé spécifiquement en mode parallèle pour les provoquer — statut
inchangé par rapport à M041, ni confirmé ni infirmé activement cette fois.

## 22. Files changed (cette session, M042)

Serveur (nouveaux) : `Server/src/BeeKingdom.Alliance/Repositories/
DurableJsonFileIo.cs`, `DurableJsonAllianceRepository.cs`,
`DurableJsonAllianceActivityRepository.cs`,
`DurableJsonAllianceDiplomacyRepository.cs`,
`DurableJsonAllianceWarRepository.cs`,
`Server/src/BeeKingdom.Alliance/Integration/AllianceMembershipResolver.cs`,
`Server/src/BeeKingdom.Chat/Audience/IAllianceMembershipResolver.cs`,
`Server/tests/BeeKingdom.Tests/AllianceDurablePersistenceTests.cs`,
`Server/tests/BeeKingdom.Tests/AllianceChatIntegrationTests.cs`.

Serveur (modifiés) : `InMemoryAllianceRepository.cs`,
`InMemoryAllianceActivityRepository.cs`,
`InMemoryAllianceDiplomacyRepository.cs`, `InMemoryAllianceWarRepository.cs`
(surface Dump/Restore interne), `AllianceServiceCollectionExtensions.cs`,
`AllianceOptions.cs` (GameServerId/WorldId), `AllianceService.cs` (création/
synchronisation du chat), `LocalChatAudienceResolver.cs`,
`ChatServiceCollectionExtensions.cs`, `IChatRepository.cs`,
`InMemoryChatRepository.cs`, `SqlChatRepository.cs`,
`ChatAudienceResolverTests.cs` (réécrit), `ChatMessagingEndpointTests.cs`
(2 tests réécrits pour utiliser une vraie alliance).

Unity : aucun fichier touché cette session (Part 1/2/5/8 non commencées).

## 23. Remaining runtime validation

Aucune validation en Play Mode Unity n'a été faite cette session — rien côté
Unity n'a changé. Côté serveur, la validation faite est celle des tests
automatisés (section 21) ; aucun test manuel via HTTP réel en dehors des
tests `WebApplicationFactory` n'a été fait.

## 24. Production blockers

- La fenêtre Alliance Center affiche toujours des données fictives — bloque
  toute validation produit réelle par le CEO.
- Feature flags production restent `Alliance.Enabled=false`,
  `DiplomacyEnabled=false`, `WarEnabled=false`, comme demandé (non touchés).
- La persistance durable Alliance écrit sur disque local
  (`AppContext.BaseDirectory/data/alliances`) — cohérent avec le pattern
  Hive existant mais donc pas encore une solution multi-instance/scalée ; la
  section 17 de `ALLIANCE_PLATFORM_ARCHITECTURE.md` documente ce qu'une vraie
  migration SQL demanderait, non commencée (décision CEO/GPT requise, comme
  demandé).

## 25. Final verdict

Le socle chat + persistance durable construit cette session est réel,
câblé, et testé — pas un prototype. Mais la mission, dans son ensemble, est
loin d'être terminée : 6 des 9 parties du brief (1, 2, 5, 6 partiel, 7, 8)
n'ont pas été commencées. Le principe final du brief ("Demain le CEO devrait
pouvoir cliquer sur Alliance Center et arrêter de voir 'ALLIANCE PRIME'")
**n'est pas encore atteint** — c'est le blocage réel le plus important à
communiquer, pas un détail à minimiser.

### Verdict final (A–V)

- A. Fenêtre Unity branchée sur le vrai backend : **NON**
- B. État NO_ALLIANCE réel (Create/Search/Invitations) : **NON**
- C. Création d'alliance réelle depuis Unity : **NON**
- D. Recherche réelle depuis Unity : **NON**
- E. Aperçu (Overview) réel, sans "ALLIANCE PRIME"/"NEX" : **NON**
- F. Roster de membres réel avec actions Promote/Demote/Kick/Transfer : **NON**
- G. Invitations/Candidatures réelles, recherche de joueur réutilisée : **NON**
- H. Leave/Dissolve réels : **NON**
- I. Onglet Activité/Journal réel, localisé côté client : **NON**
- J. Création réelle de la conversation de chat d'alliance : **OUI**
- K. Synchronisation réelle des membres du chat (join/leave/kick) : **OUI**
- L. Rôle de chat jamais dicté par le client, toujours résolu serveur : **OUI**
- M. Persistance durable pour toutes les sous-parties Alliance : **OUI**
- N. Validation par redémarrage réel (recréation de repository) : **OUI**
- O. Ingestion d'activité Building Upgrade réelle : **NON**
- P. Ingestion d'activité Research réelle : **NON**
- Q. Ingestion d'activité Combat réelle : **NON**
- R. Ingestion d'activité Gathering réelle : **NON**
- S. Contrats Web confirmés suffisants pour `/alliance/[name]` : **PARTIEL**
  (inspection faite en M041, non réapprofondie cette session)
- T. Tests d'intégrité diplomatie/guerre (dissolve, leave, transfert) : **NON**
- U. Tests Unity (NO_ALLIANCE, IN_ALLIANCE, mapping, permissions) : **NON**
- V. Le CEO peut ouvrir Alliance Center et voir le vrai système demain : **NON**

**Si V = NON — blocages réels uniquement :**
1. `AllianceClient` (existant depuis M041) n'est toujours pas instancié
   dans `MobileAccountSessionRuntimeBootstrap.cs` — rien côté Unity ne parle
   au vrai backend.
2. `HiveViewProductUiPresenter.DrawAllianceHeadquartersScreen` n'a reçu
   aucune modification — tous les onglets (Overview, Members, Journal,
   Invitations) affichent toujours les données codées en dur de M040 et
   antérieur.
3. Aucun point d'ingestion d'activité gameplay réelle (Building Upgrade,
   Research, Combat, Gathering) n'a été localisé ni câblé — le flux
   d'activité restera vide même une fois la fenêtre branchée, tant que ce
   travail n'est pas fait.

Aucun de ces trois blocages n'est un problème de conception ou de blocage
technique découvert — c'est simplement du travail non commencé faute de
temps dans cette session. Le socle serveur (chat + persistance) sur lequel
ce travail doit maintenant se brancher est prêt et testé.

## Prochaine session — reprise exacte

1. Part 1A : instancier `AllianceClient` dans
   `MobileAccountSessionRuntimeBootstrap.cs`, même patron exact que
   `HiveResearchClient` (`client.Gate, client, gameTransport`, pas de
   paramètre cache — voir constructeur `AllianceClient.cs:227`). Créer un
   `AllianceCenterPanelController`-style wrapper (mirroring
   `HiveResearchPanelController`) pour gérer l'état/refresh, puis
   `HiveViewProductUiPresenter.ConfigureAllianceControllerForRuntime(...)`.
2. Part 1E-1H : dans `DrawAllianceHeadquartersScreen`, remplacer
   `BuildAllianceMemberRoster()` et les valeurs "ALLIANCE PRIME"/"NEX" par
   les données réelles du controller.
3. Part 5 : localiser le point de complétion réel (pas Started) de Building
   Upgrade et Research en premier (ce sont les plus simples/déjà les mieux
   isolés dans le code existant), appeler
   `IAllianceActivityPublisher.PublishForPlayerAsync(...)` avec une
   dedupeKey stable, avant de s'attaquer à Combat/Gathering.
