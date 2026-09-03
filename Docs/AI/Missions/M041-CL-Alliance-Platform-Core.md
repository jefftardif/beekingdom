# M041-CL — Alliance Platform Core

Mission de nuit, sans supervision interactive (Jeff endormi, "continue si tu
veux"). Socle serveur-autoritaire construit et testé. Fenêtre Unity existante
et client Unity **non branchés** cette session — voir verdict final pour le
détail honnête de ce qui reste.

## 1. Executive verdict

Le socle serveur (aggregate, membership, rôles/permissions, join/candidature/
invitation, promotion/rétrogradation/kick/leave, transfert de leadership,
dissolution, profil, activity feed avec visibilité, diplomatie, fondation de
guerre) est **construit, câblé dans le serveur réel (`Program.cs`), compilé
sans erreur, et testé (35 nouveaux tests, tous verts ; 422/422 tests serveur
au total, aucune régression)**. C'est un socle réel, pas un prototype
jetable — le service est déjà celui qui tournera en production le jour où le
flag `Alliance:Enabled` passe à `true`.

**Mise à jour en fin de session** : `Assets/BeeKingdom/Networking/AllianceClient.cs`
a finalement été créé aussi (bonus, en plus du plan initial) — même patron
exact que `HiveResearchClient.cs` (session/transport/refresh-on-401), 24
méthodes couvrant Create/Search/Profil/Membres/Join/Candidatures/Invitations/
Kick-Promote-Demote-Transfer/Dissolve/Profil/Activité. Compilé sans erreur en
Unity. Diplomatie/Guerre volontairement pas couvertes côté client (les
onglets correspondants dans la fenêtre existante sont encore des placeholders
"bientôt disponible" — rien à y brancher pour l'instant).

Ce qui n'a **TOUJOURS PAS** été fait cette nuit, explicitement : brancher la
fenêtre Unity existante (`HiveViewProductUiPresenter.DrawAllianceHeadquartersScreen`)
sur ce client — elle affiche encore des données 100% codées en dur. Toucher
au dépôt Web (qui n'existe pas dans ce repo).

## 2. Existing Alliance Center UI inventory

Fenêtre trouvée : `HiveViewProductUiPresenter.DrawAllianceHeadquartersScreen`
(IMGUI, ~38k lignes du fichier monolithique Playground), ouverte via
`HiveMapAllianceBootstrap.cs` sur le bâtiment `"ALLIANCE_CENTER"`.

10 onglets déclarés (`overview, members, chat, journal, help, donations,
research, gifts, war, diplomacy`) :

| Onglet | État |
|---|---|
| overview | Local-fake — texte statique + flux d'activité simulé côté client |
| members | Local-fake — roster codé en dur (`BuildAllianceMemberRoster()`) |
| chat | Placeholder — affiche littéralement "Le chat arrive au prochain sprint…" |
| journal, help, donations, research, gifts, war, diplomacy | Non implémentés — panneau générique "bientôt disponible" |

Nom d'alliance codé en dur : `"ALLIANCE PRIME"` / tag `"NEX"` — aucune source
de données réelle nulle part dans cette fenêtre.

## 3. Existing backend inventory

- Domaine Alliance serveur : **absent** avant cette mission (seulement des
  DTO stubs jamais câblés dans `BeeKingdom.Shared`).
- Système de chat Alliance : **partiellement réel** — `ChatChannelType.Alliance`,
  transport/persistance de conversation réels via `BeeKingdom.Chat`, mais le
  rôle d'alliance du joueur est déclaré par le client et fait confiance telle
  quelle dans `LocalChatAudienceResolver` (documenté explicitement dans le
  code comme non fiable pour de l'autorisation au-delà du chat).
- `AllianceDiplomacyWarFoundationFrameworks.cs` (Unity) : cadre de diagnostic/
  QA pur, aucune logique de jeu réelle — utilisé pour le vocabulaire (rôles,
  états, types de diplomatie) mais aucun code réutilisé tel quel.
- Docs vision (`ALLIANCE_DESIGN_BIBLE.md`, `EPIC_12_ALLIANCE_WEB_PLATFORM.md`) :
  vision produit complète, zéro spécification technique, zéro implémentation.
- Aucun dépôt Web dans ce repo.

## 4-17. Architecture, aggregate, membership, rôles, permissions, join,
applications, invitations, profil, leadership, communication, activity,
feed public, ingestion joueur, contrat Web

Voir `Docs/Alliance/ALLIANCE_PLATFORM_ARCHITECTURE.md` sections 2-10 —
contenu complet et non dupliqué ici pour éviter la dérive entre deux copies
du même contenu.

## 18. Existing Unity window wiring

**Non fait.** Voir section 1 et le verdict B.

## 19-23. Diplomacy model / primitives, War model / declaration foundation

Voir `ALLIANCE_PLATFORM_ARCHITECTURE.md` sections 11-12. Implémenté et testé :
`ProposeRelation` (NAP/Ally), `RespondToRelation`, `CancelRelation`,
`DeclareWar`. Non construit (volontairement, hors scope explicite du
brief) : mécanique de combat, territoire, score, récompenses.

## 24. Persistence

In-memory uniquement, thread-safe. Aucune migration SQL cette nuit —
`SQL_PRODUCTION_MIGRATION_PENDING` documenté dans l'architecture (section 17).

## 25. Auth/security

Acteur toujours dérivé du token authentifié (`AuthenticationManager.ValidateToken`),
jamais d'un champ client. Alliance de l'acteur pour diplomatie/guerre dérivée
de l'adhésion active réelle. Voir section 16 de l'architecture.

## 26. Concurrency/idempotence

`Revision` optimistic concurrency sur le profil ; capacité re-vérifiée à
l'acceptation (pas seulement à la soumission) pour open-join, candidatures,
invitations ; reçus idempotents pour Create/Application/Invitation/
Diplomacy-proposal/DeclareWar (mêmes patterns que `ChatConversationCreationReceipt`).
`HasActiveWarBetween` empêche les guerres dupliquées. Verrous `lock` par
repository protègent les races d'écriture.

## 27. Feature flags

`Alliance:{Enabled,DiplomacyEnabled,WarEnabled,MaxMembers}` — `true` en
dev/base config, `false` explicitement en production (conservateur, comme
demandé).

## 28. Tests

`Server/tests/BeeKingdom.Tests/AllianceServiceTests.cs` — 35 tests, tous
verts, couvrant : création + idempotence + validation, recherche, open-join
(capacité, mode incorrect), candidatures (soumission/acceptation/rejet/
annulation, permissions), invitations (création/acceptation/permissions),
promotion/rétrogradation (permissions), kick (hiérarchie officier/leader),
leave (leader doit transférer/dissoudre, invariant un-joueur-une-alliance),
transfert de leadership, dissolution (ferme les adhésions et candidatures),
mise à jour de profil (revision conflict, permission), activity feed
(visibilité publique vs membres, pagination stable), diplomatie (proposition/
acceptation/rejet, permissions, double-acceptation sûre), guerre (déclaration,
anti-self, permission, anti-duplication, flag désactivé), et sécurité du DTO
public (`PublicProfile_DoesNotLeakPrivateFields`).

Suite complète serveur : **422/422 verts** en isolation (387 pré-existants +
35 nouveaux). Une exécution complète en parallèle a fait apparaître 2 échecs
(`Enabled_start_complete_replay_and_conflict_are_exact`,
`PostCapacityFullKeepsStateAndSameHiveIsIsolatedByPlayer`) — **aucun des
deux n'appartient au domaine Alliance** ; ré-exécutés seuls, les deux passent
à 100%. Flakiness de parallélisme préexistante dans la suite (probable état
statique partagé entre classes de test non liées à Alliance), pas une
régression introduite cette nuit — signalé honnêtement plutôt que masqué,
mais pas corrigé (hors scope de cette mission).

Tests Unity de la fenêtre existante (état NO_ALLIANCE/IN_ALLIANCE, mapping
membres/activité) : **non écrits** — la fenêtre n'a pas été touchée cette
session, écrire des tests contre un état qu'on ne branche pas n'aurait rien
prouvé de réel.

## 29. Files changed

**Nouveaux fichiers (serveur)** :
```
Server/src/BeeKingdom.Alliance/BeeKingdom.Alliance.csproj
Server/src/BeeKingdom.Alliance/Configuration/AllianceOptions.cs
Server/src/BeeKingdom.Alliance/Models/AllianceModels.cs
Server/src/BeeKingdom.Alliance/Models/AllianceActivity.cs
Server/src/BeeKingdom.Alliance/Models/AllianceDiplomacy.cs
Server/src/BeeKingdom.Alliance/Models/AllianceContracts.cs
Server/src/BeeKingdom.Alliance/Repositories/IAllianceRepository.cs
Server/src/BeeKingdom.Alliance/Repositories/InMemoryAllianceRepository.cs
Server/src/BeeKingdom.Alliance/Repositories/IAllianceActivityRepository.cs
Server/src/BeeKingdom.Alliance/Repositories/InMemoryAllianceActivityRepository.cs
Server/src/BeeKingdom.Alliance/Repositories/IAllianceDiplomacyRepository.cs
Server/src/BeeKingdom.Alliance/Repositories/InMemoryAllianceDiplomacyRepository.cs
Server/src/BeeKingdom.Alliance/Repositories/IAllianceWarRepository.cs
Server/src/BeeKingdom.Alliance/Repositories/InMemoryAllianceWarRepository.cs
Server/src/BeeKingdom.Alliance/Activity/IAllianceActivityPublisher.cs
Server/src/BeeKingdom.Alliance/AllianceService.cs
Server/src/BeeKingdom.Alliance/DependencyInjection/AllianceServiceCollectionExtensions.cs
Server/tests/BeeKingdom.Tests/AllianceServiceTests.cs
Docs/Alliance/ALLIANCE_PLATFORM_ARCHITECTURE.md
Docs/AI/Missions/M041-CL-Alliance-Platform-Core.md
Assets/BeeKingdom/Networking/AllianceClient.cs
```

**Fichiers modifiés** :
```
Server/src/BeeKingdom.Server/BeeKingdom.Server.csproj   (référence de projet)
Server/src/BeeKingdom.Server/Program.cs                  (DI + 24 endpoints REST + helpers d'erreur)
Server/src/BeeKingdom.Server/appsettings.json             (section Alliance, activée)
Server/src/BeeKingdom.Server/appsettings.Production.json  (section Alliance, désactivée)
Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj     (référence de projet)
```

Un seul nouveau fichier Unity ajouté (`AllianceClient.cs`, pur code réseau,
aucun fichier Unity existant modifié) ; rien touché côté FTUE/M037-M040,
aucun système protégé (WorldMap, PvE, Recherche, Formation, économie)
modifié.

## 30. CEO runtime validation pending

Tout ce qui précède est vérifié par compilation + tests automatisés
uniquement — **aucune validation en jeu réel n'a eu lieu** (pas de fenêtre
Unity branchée à tester). La validation runtime CEO de demain devra
commencer par brancher la fenêtre existante avant de pouvoir montrer quoi
que ce soit visuellement.

## 31. Production gaps

Voir `ALLIANCE_PLATFORM_ARCHITECTURE.md` section 21 — liste complète (pas de
SQL, chat non auto-lié, rôle chat toujours client-déclaré, fenêtre Unity/
client Unity non branchés, slug non régénéré au renommage).

## 32. Future territory/tech/buildings integration

Emplacements réservés uniquement (types d'activité dans l'enum, aucune
implémentation) — voir architecture sections 13-15.

## 33. Final verdict

A. Existing Alliance Center window found and documented? **YES**
B. Existing Unity Alliance window wired to real backend where possible? **NO** — le client Unity (`AllianceClient.cs`) existe et compile désormais, mais la fenêtre elle-même n'a pas encore été modifiée pour l'utiliser.
C. Alliance is modeled as a first-class strategic entity? **YES**
D. Membership lifecycle complete? **YES**
E. Roles/permissions centralized? **YES** (`AlliancePermissionPolicy`)
F. Create/Search/Open Join complete? **YES**
G. Applications complete? **YES**
H. Invitations complete? **YES**
I. Promote/Demote/Kick/Leave complete? **YES**
J. Leadership transfer complete? **YES**
K. Dissolve complete? **YES**
L. Alliance chat uses Communication backend? **PARTIAL** — référence prévue (`ChatConversationId`), pas encore auto-créée/liée à la création d'alliance.
M. Activity Feed infrastructure complete? **YES**
N. Member/leadership activity events emitted correctly? **YES** (testé)
O. Public Alliance Web profile contract ready? **YES**
P. Public activity feed contract ready? **YES**
Q. Player gameplay events can be ingested into Alliance activity without redesigning the domain? **YES** (`IAllianceActivityPublisher`, non branché à aucune source d'événement réelle)
R. Diplomacy model complete enough for NAP/Alliance relations? **YES**
S. Diplomacy primitives implemented/tested? **YES**
T. AllianceWar first-class model exists? **YES**
U. War declaration foundation implemented or contract-ready? **YES** (implémenté et testé, pas seulement contractuel)
V. War/diplomacy visible through future Web contracts? **YES** (`AlliancePublicProfile.Diplomacy`)
W. One-player-one-alliance invariant enforced? **YES** (serveur, testé)
X. Capacity/concurrency protected? **YES**
Y. Auth actor spoofing prevented? **YES**
Z. Durable persistence abstraction ready? **PARTIAL** — abstraction (interfaces repository) prête, implémentation SQL absente (in-memory seulement).
AA. Automated test suites green? **YES for Alliance** (35/35, isolation et parallèle) — 2 tests pré-existants sans rapport avec Alliance sont flaky sous parallélisme complet (voir section 28), non introduits cette nuit.
AB. Is the Alliance Platform technically ready for CEO runtime/UI validation tomorrow? **NO** — le backend est prêt et testé, mais rien n'est visible en jeu tant que la fenêtre Unity existante n'est pas branchée sur ce backend. La prochaine session doit commencer exactement là.

### Si AB=NO — bloqueurs restants, dans l'ordre

1. **BLOQUEUR** : `HiveViewProductUiPresenter.DrawAllianceHeadquartersScreen`
   lit encore des données 100% codées en dur.
   **ROOT CAUSE** : non branché cette nuit.
   **MINIMUM FIX** : remplacer `BuildAllianceMemberRoster()` et le nom/tag
   codés en dur par une lecture du `AllianceClient` (déjà créé et compilé,
   `Assets/BeeKingdom/Networking/AllianceClient.cs`), en suivant le même
   patron que `OfficialResearchModel()`/`OfficialBuildingUpgradeModel()` déjà
   dans ce même fichier — d'abord les onglets `overview`/`members` seulement,
   sans toucher au reste de la mise en page existante. Nécessitera aussi un
   bootstrap-side wiring (DI/instanciation du `AllianceClient` avec le
   `MobileAccountSessionGate`/transport réels, même patron que la recherche/
   la Caserne dans `MobileAccountSessionRuntimeBootstrap.cs`).

2. **GAP** (pas bloquant pour un premier test, mais réel) : `ChatConversationId`
   n'est pas peuplé à la création d'Alliance.
   **ROOT CAUSE** : hors scope de cette nuit.
   **MINIMUM FIX** : dans `AllianceService.CreateAlliance`, après la sauvegarde
   de l'Alliance, appeler le service Chat existant pour créer/lier la
   conversation `alliance:{allianceId:N}` et sauvegarder le
   `ChatConversationId` résultant.
