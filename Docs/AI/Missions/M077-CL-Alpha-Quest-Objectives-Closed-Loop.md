# M077-CL — Alpha Quest & Objectives Closed Loop

Date : 2026-09-09

## Infrastructure existante trouvée

Aucun système de quêtes/objectifs générique et réutilisable n'existait. Le
plus proche, `HiveMilestoneEventService`/`HiveMilestoneEventPanelController`
("Défi de la ruche"), est un évènement **ponctuel, à fenêtre de temps limitée,
réclamation unique agrégée sur 5 objectifs fixes codés en dur** — un excellent
patron d'architecture à copier, mais pas un moteur à étendre littéralement
(pas de chaîne ordonnée, jamais de réclamation individuelle, expire après 30
jours). `RewardLedgerService.GrantAsync` (mécanisme de récompense idempotent)
existe mais est dormant (seul appelant réel : un endpoint admin). Le "Centre
des missions" (`MissionCatalog`, onglets Quotidien/Hebdo/Succès) et le
"Journal de la ruche" (onglet "Acte I") sont **entièrement locaux/simulés**,
non branchés au serveur — explicitement documentés dans le code comme des
maquettes en attente d'un vrai système. Le FTUE (`TutorialGameplayNotifier`)
expose déjà des évènements réels (`UpgradeCompleted`, `ResearchStarted`,
`TrainingStarted`) mais aucun évènement de complétion pour recherche/
entraînement, et rien pour la World Map.

**Point clé découvert en cours de route** : la vraie scène officielle testée
par le CEO (`Environment2D5D_HiveMap_Test`) utilise un Canvas uGUI
(`LivingHiveMenuCanvas`) totalement séparé de l'IMGUI historique
(`HiveViewProductUiPresenter`). Le seul endroit où ce Canvas expose déjà du
contenu de type "Ronde quotidienne / Évènement jalon" est la modale
**Activités** (`HiveMapActivitiesBootstrap.cs`) — c'est donc là, et non dans
la barre IMGUI historique, que la nouvelle chaîne d'objectifs devait
apparaître pour être réellement accessible au CEO.

## Ce qui a été réutilisé

- Le patron exact de `HiveMilestoneEventService` côté serveur (lecture
  dérivée de l'état déjà persisté par d'autres systèmes, aucune nouvelle
  instrumentation dans ces systèmes, idempotence par clé + revision
  optimiste, mutation atomique via `IHiveStateRepository`).
- Le patron exact de `HiveMilestoneEventClient`/`HiveMilestoneEventPanelController`
  côté client (session gate, transport authentifié, machine d'états simple).
- L'emplacement UI réel et déjà fonctionnel : la modale **Activités**
  (`HiveMapActivitiesBootstrap.DrawMilestoneEventSection`), à laquelle une
  nouvelle section a été ajoutée plutôt que de construire un nouvel écran.
- La convention `PlayerHiveState` (un sous-modèle nullable dédié par
  fonctionnalité) et la mécanique de migration JSON (aucune migration SQL
  requise, juste bump de `ModelVersion`).

## Ce qui a été ajouté

**Serveur** (`Server/src/BeeKingdom.HiveOperations/QuestChainService.cs`,
nouveau fichier) : `QuestChainState`/`QuestChainObjectiveReadModel`/
`QuestChainSnapshot`, endpoints `GET /quest-chain`, `POST /quest-chain/{key}/claim`,
`POST /quest-chain/world-map-visited`. 5 objectifs, **réclamation
individuelle par objectif** (contrairement à l'Évènement jalon), jamais
d'expiration :

| Clé | Titre | Détection | Récompense |
|---|---|---|---|
| `q1_building_upgrade` | Royaume en croissance | `BuildingLevels.Max() >= 2` | 200 miel |
| `q2_troop_recruit` | Préparer la garde | `DoctrineRoster.Counts.Sum() > 0` | 150 cire |
| `q3_research_complete` | Le savoir du royaume | `Research.Completed.Count > 0` | 150 pollen |
| `q4_world_map_visit` | Explorer le royaume | auto-déclaré client (seul sans signal serveur) | 100 miel |
| `q5_world_resource_collect` | Richesses sauvages | `WorldResourceCollection.NodeReadyAtUtc.Count > 0` | 100 cire |

Quatre objectifs sur cinq sont vérifiés en relisant l'état déjà persisté par
Construction/Caserne/Recherche/Collecte World Map — **aucune de ces
fonctionnalités n'a été modifiée**. Le cinquième (ouvrir la World Map) n'a
aucun signal serveur naturel ; il est auto-déclaré par le client via un
endpoint dédié, idempotent par nature.

`HiveOperationModels.cs` : ajout de `QuestChainState? QuestChain` à
`PlayerHiveState`. `HiveStateMigrator.cs` : `ModelVersion` 10 → 11 + bloc de
validation. Pas de migration SQL (confirmé : tout vit dans le blob JSON
existant).

**Client Unity** : `Assets/BeeKingdom/Networking/QuestChainClient.cs` et
`Assets/BeeKingdom/Playground/QuestChainPresentation.cs` (nouveaux, miroirs
structurels de `HiveMilestoneEventClient`/`Presentation`). Wiring dans
`MobileAccountSessionRuntimeBootstrap.cs` (construction, refresh initial,
teardown — même emplacement que chaque contrôleur existant). Nouvelle
section "OBJECTIFS DU ROYAUME" dans `HiveMapActivitiesBootstrap.cs` (bouton
Réclamer par ligne, au lieu d'un bouton global). Auto-déclaration de
`q4_world_map_visit` ajoutée en une ligne dans `WorldMapMmoFullscreenFoundationBootstrap.Awake()`.
Nudge de synchronisation du Sac après chaque réclamation (réutilise le
`NotifyStockMightHaveChanged()` déjà ajouté en M076).

Un bouton "Objectifs" a aussi été ajouté à l'ancienne barre IMGUI
(`HiveViewProductUiPresenter.DrawBottomRail`/portrait/menu "Plus") par souci
de parité avec le "Défi" existant au même endroit — mais l'intégration qui
compte réellement pour le CEO est celle de la modale Activités ci-dessus.

**FTUE** : non touché. Aucun fichier `Assets/BeeKingdom/Tutorial/*` modifié.
La chaîne détecte les mêmes actions réelles que le FTUE (amélioration de
bâtiment, etc.) mais en relisant l'état serveur, jamais via
`TutorialGameplayNotifier` — zéro dépendance croisée dans les deux sens.

## Persistance

Serveur autoritaire pour la détection (4/5 objectifs), l'octroi, la
réclamation et l'anti-double-réclamation (`ClaimedObjectiveKeys` + reçu
d'idempotence, revision optimiste, mutation atomique SQL/JSON transactionnelle
— même repository que Combat Patrol/Milestone Event). Reconnexion : l'état
vit dans la même ligne `HivePlayerStates` que tout le reste, donc rechargée
telle quelle — aucune remise à zéro possible côté client.

## Récompenses

Miel/cire/pollen uniquement (200/150/150/100/100), aucune Gelée Royale,
aucun Speed Up — conforme à la demande. Crédit direct sur `state.Resources`
(même pattern qu'Évènement jalon), plafonné par la capacité de stockage.

## Tests

`Server/tests/BeeKingdom.HiveOperations.Tests/QuestChainServiceTests.cs`
(nouveau, 7 tests ciblés) : état par défaut sans objectif complété ;
détection correcte par objectif à partir d'un état pré-rempli ;
auto-déclaration World Map idempotente ; échec de réclamation si objectif
incomplet ; réclamation réussie + anti-double-réclamation ; rejeu de la même
clé d'idempotence sans double crédit ; réclamer un objectif n'affecte pas les
autres. **7/7 réussis.** Suite `HiveMilestoneEventServiceTests` (4/4)
re-exécutée sans régression après le changement partagé de
`HiveOperationModels.cs`/`HiveStateMigrator.cs`. Build complet du solveur
serveur (`dotnet build BeeKingdom.Server.slnx`) : 0 erreur. Compilation
Unity (`assets-refresh`) : 0 erreur après chaque étape.

## Validation runtime effectuée

Play Mode depuis la vraie scène d'entrée (`Environment2D5D_HiveMap_Test` →
démo locale) : ouverture de la modale Activités sans exception, nouvelle
section "Objectifs du royaume" présente et sans erreur en état
`NotConfigured` (comportement identique à Ronde quotidienne/Défi dans ce même
environnement, qui n'a jamais de session authentifiée réelle). Auto-
déclaration `q4_world_map_visit` insérée dans `WorldMapMmoFullscreenFoundationBootstrap.Awake()`,
vérifiée sans exception au chargement de la World Map.

## Ce qui nécessite un retest CEO

Cet environnement de développement n'a pas de compte authentifié réel — tout
ce qui suit n'a **pas** pu être vérifié de bout en bout ici :

1. Que les 5 objectifs passent réellement à "complété" après l'action
   correspondante avec un vrai compte (améliorer un bâtiment, entraîner des
   troupes, terminer une recherche, ouvrir la World Map, collecter une
   ressource).
2. Que chaque réclamation crédite bien miel/cire/pollen une seule fois et
   affiche "Réclamé" ensuite.
3. Que la progression et les réclamations survivent à un retour Ruche →
   World Map → Ruche, puis à une reconnexion complète.

Test court suggéré : compte réel → ouvrir Activités (bouton "Activités" du
menu du bas) → constater les 5 objectifs → accomplir une action → Rafraîchir
→ Réclamer → fermer le jeu → rouvrir → confirmer l'état conservé.

## Commit

Commits locaux uniquement, aucun push.

## READY FOR CEO QUEST CLOSED LOOP RETEST
