# Alliance Platform Architecture (M041-CL)

Statut : socle serveur-autoritaire construit et testé (421/421 tests serveur
verts). Fenêtre Unity existante et client Unity NON branchés cette session —
voir `Docs/AI/Missions/M041-CL-Alliance-Platform-Core.md` pour le détail
complet et le verdict final honnête.

## 1. Alliance comme entité stratégique

Une Alliance n'est **pas** un `PlayerId` avec une liste de membres. C'est un
Aggregate Root autonome (`AllianceEntity`, `Server/src/BeeKingdom.Alliance/Models/AllianceModels.cs`)
avec sa propre identité (`AllianceId`), son cycle de vie (`AllianceStatus`),
son leadership, et des références en avant vers des sous-domaines qui
n'existent pas encore pleinement (diplomatie, guerre, page web) sans que
l'aggregate central doive être refondu quand ces sous-domaines s'activent.

Les joueurs appartiennent à une Alliance via `AllianceMembership` — une table
séparée, pas une liste embarquée dans l'Alliance elle-même.

## 2. Modèle Aggregate

`AllianceEntity` (record immutable) :
`AllianceId, Name, Tag, Description, Language, EmblemKey, JoinMode, Status,
CreatedAtUtc, CreatedByPlayerId, LeaderPlayerId, MemberCount, MaxMembers,
PublicSlug, ChatConversationId?, Revision, DisbandedAtUtc?`

`Revision` est incrémentée à chaque mutation et vérifiée en optimistic
concurrency (`UpdateProfile` refuse un `ExpectedRevision` périmé avec
`revision_conflict`).

## 3. Membership

`AllianceMembership` : `AllianceId, PlayerId, Role, JoinedAtUtc,
InvitedByPlayerId?, ApplicationId?, LastRoleChangedAtUtc, RemovedAtUtc?,
Revision`.

**Invariant serveur-autoritaire** : un joueur appartient à AU PLUS une
Alliance active à la fois. Vérifié via
`IAllianceRepository.GetActiveMembershipForPlayer(playerId)` avant toute
création/join/acceptation — jamais fait confiance côté client.

## 4. Rôles / permissions

Rôles Alpha : `Member`, `Officer`, `Leader` (enum `AllianceRole`).

Toutes les capacités passent par `AlliancePermissionPolicy` (classe statique
centralisée, `Models/AllianceModels.cs`) — jamais de `if (role == ...)`
dispersé dans le service :
`CanInvite, CanApproveApplication, CanRejectApplication, CanKickMember,
CanPromote, CanDemote, CanEditProfile, CanManageDiplomacy, CanDeclareWar,
CanAcceptPeace, CanTransferLeadership, CanDissolve, CanKickTarget`.

`CanKickTarget` encode en plus la hiérarchie (un Officer ne peut pas kicker un
autre Officer, seul le Leader peut).

## 5. Cycle de vie du join

`AllianceJoinMode` : `Open, Application, InviteOnly`.

- **Open** : `AllianceService.JoinOpen` — atomique, vérifie capacité en
  relisant le compte réel de membres actifs (pas un compteur caché
  potentiellement périmé), race-safe via le verrou du repository.
- **Application** : `SubmitApplication` → `AcceptApplication`/`RejectApplication`
  (permission `CanApproveApplication`/`CanRejectApplication`). Idempotent via
  reçu `(PlayerId, ClientRequestId) → ApplicationId`.
- **InviteOnly** : `CreateInvitation` (permission `CanInvite`) →
  `AcceptInvitation`/`DeclineInvitation`/`RevokeInvitation`.

Toutes les mutations de join vérifient la capacité ET l'invariant
un-joueur-une-alliance au moment de l'acceptation, pas seulement à la
soumission (empêche qu'un joueur accepte deux invitations concurrentes de
deux alliances différentes).

## 6. Communication (chat)

Aucun second système de chat créé. `AllianceEntity.ChatConversationId`
référence la conversation réelle du système `BeeKingdom.Chat` existant
(`ChatChannelType.Alliance`, clé d'audience `"alliance:{allianceId:N}"`,
`LocalChatAudienceResolver`). Le champ existe dans le modèle mais n'est pas
encore peuplé automatiquement à la création — la liaison chat/alliance
authentique (au lieu du rôle client-déclaré actuel dans
`LocalChatAudienceResolver`) reste un gap identifié en section 21.

## 7. Activity Feed — domaine central

`AllianceActivityEvent` (`Models/AllianceActivity.cs`) : structure typée,
JAMAIS de phrase pré-localisée stockée —
`Type, ActorPlayerId?, TargetPlayerId?, RelatedAllianceId?, RelatedEntityId?,
Visibility, Payload{EntityKey,EntityName,Level,Result,Extra}, Sequence,
OccurredAtUtc`. Le client (Unity ou futur Web) localise à partir de ces
champs structurés.

Types Alpha réellement émis : `AllianceCreated, MemberJoined, MemberLeft,
MemberKicked, MemberPromoted, MemberDemoted, LeadershipTransferred,
ProfileUpdated, AllianceDiplomacyChanged, AllianceWarDeclared`.

Types réservés (enum existe, rien ne les émet encore) :
`PlayerBuildingUpgraded, PlayerResearchCompleted, PlayerAttackStarted/Won/Lost,
CreatureDefeated, GatheringCompleted, AllianceWarEnded,
AllianceTerritoryCaptured, AllianceBuildingUpgraded, AllianceTechnologyCompleted`.

`AllianceActivityVisibility` : `Public, MembersOnly, OfficersOnly,
SystemPrivate`. Le filtrage se fait dans le repository, pas seulement dans
l'appelant — `ListPublicForAlliance` ne retourne JAMAIS rien au-dessus de
`Public`, quel que soit ce que demande l'appelant.

Pagination : curseur `Sequence` (monotone par alliance, indépendant de
`OccurredAtUtc`), tri décroissant, stable même sous ajouts concurrents.

## 8. Ingestion d'activité joueur (fondation, pas branchée)

`IAllianceActivityPublisher` (`Activity/IAllianceActivityPublisher.cs`) : la
seule méthode `PublishForPlayerAsync(playerId, type, payload, dedupeKey)`
résout l'alliance active du joueur et publie un événement idempotent
(`AppendIdempotent`, dédoublonné par `(AllianceId, Type, dedupeKey)`). No-op
silencieux si le joueur n'a pas d'alliance.

**Rien n'appelle cette interface aujourd'hui** — aucun événement
`BuildingUpgradeCompleted`/`ResearchCompleted`/`AttackResolved` n'existe
encore côté jeu pour la déclencher. C'est le point d'intégration prévu pour
un futur chantier, volontairement non câblé cette nuit (hors scope explicite
de la mission).

## 9. Profil public Web

`AlliancePublicProfile` (`Models/AllianceContracts.cs`) — contrat exposé par
`GET /alliance/v1/alliances/{id}` et `GET /alliance/v1/alliances/by-slug/{slug}` :
`AllianceId, Name, Tag, Description, Language, EmblemKey, MemberCount,
MaxMembers, Leader{PlayerId,DisplayName}, Status, CreatedAtUtc, JoinMode,
PublicSlug, Diplomacy{AllyCount,NonAggressionPactCount,HostileCount,
ActiveWarCount}?`.

**N'expose jamais** : invitations en attente, candidatures, notes internes,
actions réservées aux officiers — le type C# lui-même n'a tout simplement pas
ces champs (`AllianceServiceTests.PublicProfile_DoesNotLeakPrivateFields`
documente ce contrat).

## 10. Route Web (`beekingdomgame.com/alliance/{slug}`)

`PublicSlug` est un identifiant de **navigation**, pas d'autorité —
l'autorité reste `AllianceId`. `BuildUniqueSlug` (dans `AllianceService`)
slugifie le nom à la création et gère les collisions par suffixe numérique.
`GetBySlug`/`GET /alliance/v1/alliances/by-slug/{slug}` fait la résolution
slug → AllianceId. Renommer l'Alliance plus tard changera le slug affiché
sans jamais casser l'identité serveur (`PublicSlug` n'est pas recalculé
automatiquement au renommage aujourd'hui — `UpdateProfile` ne le régénère
pas ; à faire explicitement si un futur "renommer l'alliance" est demandé).

**Aucun dépôt Web n'existe dans ce repo** (confirmé par inventaire complet).
Cette section documente le contrat que ce futur dépôt consommera, pas un
site fonctionnel.

## 11. Diplomatie

`AllianceDiplomaticRelation` (`Models/AllianceDiplomacy.cs`) :
`RelationId, AllianceIdA, AllianceIdB, RelationType, Status, CreatedAtUtc,
UpdatedAtUtc, InitiatedByAllianceId, Revision`.

`AllianceRelationType` : `Neutral (implicite, jamais stocké), NonAggressionPact,
Ally, Hostile, War`. `Neutral` = absence de ligne, pas une ligne explicite
(évite O(n²) lignes par défaut entre toutes les paires d'alliances).

Stockage canonique par paire non ordonnée
(`InMemoryAllianceDiplomacyRepository.CanonicalKey`, tri par Guid) — une
seule ligne par relation quel que soit qui l'a proposée.

Primitives implémentées et testées : `ProposeRelation` (NAP ou Ally),
`RespondToRelation` (accept/reject), `CancelRelation`. Permission
`CanManageDiplomacy` (Leader uniquement Alpha). Idempotent via reçu de
proposition.

## 12. Fondation de guerre

`AllianceWar` (`Models/AllianceDiplomacy.cs`) : relation ENTRE DEUX
ALLIANCES (`AttackerAllianceId ↔ DefenderAllianceId`), jamais "joueur A
attaque joueur B" — les futurs rapports de combat individuels référenceront
`WarId`.

`DeclareWar` implémenté et testé : vérifie permission (`CanDeclareWar`,
Leader), rejette self-guerre, rejette alliance dissoute, rejette guerre
active dupliquée (`IAllianceWarRepository.HasActiveWarBetween`), idempotent
via reçu. Déclarer une guerre met aussi à jour la relation diplomatique vers
`War`/`Active`.

**Volontairement non construit** : mécanique de combat, capture de
territoire, score de guerre, récompenses, ralliement, résolution PvP — hors
scope explicite.

## 13. Territoire futur

Aucun modèle construit. `AllianceStrategicState` (concept mentionné dans le
brief) n'a pas d'implémentation séparée cette session — `AlliancePublicProfile`
et le futur territoire s'accrocheront à `AllianceId` sans changement de
l'aggregate central.

## 14. Bâtiments Alliance futurs

Hors scope. Emplacement prévu : `AllianceActivityType.AllianceBuildingUpgraded`
existe déjà dans l'enum pour l'ingestion future.

## 15. Technologie Alliance future

Hors scope. `AllianceActivityType.AllianceTechnologyCompleted` réservé de la
même façon.

## 16. Sécurité

- L'acteur (`PlayerId`) vient TOUJOURS de `AuthenticationManager.ValidateToken`
  (`TokenValidationResult.PlayerId`), jamais d'un champ du corps de requête —
  aucun endpoint Alliance n'accepte un `actorPlayerId` en paramètre.
- L'alliance de l'acteur pour les actions de diplomatie/guerre est dérivée de
  son adhésion active réelle (`RequireAllianceIdForPlayer`), jamais transmise
  par le client.
- `AllianceService` ne fait confiance à aucune donnée cliente pour les
  vérifications de permission — toujours re-résolu depuis
  `IAllianceRepository.GetActiveMembership`.

## 17. Persistance

**Aucune implémentation SQL cette nuit** —
`SQL_PRODUCTION_MIGRATION_PENDING`. Seules les implémentations in-memory
existent (`InMemoryAllianceRepository`, `InMemoryAllianceActivityRepository`,
`InMemoryAllianceDiplomacyRepository`, `InMemoryAllianceWarRepository`),
toutes thread-safe (`lock`), toutes enregistrées inconditionnellement par
`AddBeeKingdomAlliance` (pas de branchement `PersistenceOptions.UsesSqlServer`
comme pour Chat — un config SQL en production ne doit jamais croire à tort
qu'une persistance durable existe pour l'Alliance). Les données Alliance ne
survivent PAS à un redémarrage du serveur tant que ceci n'est pas comblé.

## 18. Concurrence

- `Revision` optimistic concurrency sur `AllianceEntity` (profil).
- Slot de capacité re-vérifié au moment de l'acceptation (open join,
  candidature, invitation), pas seulement à la soumission.
- Verrou `lock` par repository in-memory couvre les races d'écriture
  concurrentes (double invite-accept, double application-accept,
  dissolve-vs-join non testés explicitement en environnement multi-thread
  réel mais protégés par construction : chaque étape relit l'état sous le
  même verrou avant mutation).
- `HasActiveWarBetween` empêche une déclaration de guerre dupliquée.

## 19. Fenêtre Unity Alliance existante — inventaire (non branchée cette session)

Trouvée et documentée intégralement — voir le rapport de mission section 2.
Résumé : `HiveMapAllianceBootstrap.cs` route les clics sur le bâtiment
`"ALLIANCE_CENTER"` vers `HiveViewProductUiPresenter.DrawAllianceHeadquartersScreen`
(IMGUI). 10 onglets déclarés (`overview, members, chat, journal, help,
donations, research, gifts, war, diplomacy`) ; seuls `overview` et `members`
ont du contenu, et ce contenu est **entièrement local-fake** (nom d'alliance
codé en dur `"ALLIANCE PRIME"`, roster de membres codé en dur, flux
d'activité simulé côté client). Les 8 autres onglets affichent un panneau
"bientôt disponible". Le tiroir de chat affiche littéralement le texte
"Le chat arrive au prochain sprint…".

**Câblage réel non fait cette session** — le socle serveur est prêt à
recevoir un `AllianceClient` Unity (voir section 20) mais la fenêtre elle-même
n'a pas été modifiée. Voir rapport de mission, verdict B = NO, avec
justification.

## 20. Contrats Web / route

Voir section 10. Endpoints REST exposés (`/alliance/v1/*`, voir
`Server/src/BeeKingdom.Server/Program.cs`) — c'est la même API que
consommerait un futur client Unity ET un futur site Web, sans duplication de
backend :

```
POST   /alliance/v1/alliances
GET    /alliance/v1/alliances/search
GET    /alliance/v1/alliances/{allianceId}
GET    /alliance/v1/alliances/by-slug/{slug}
GET    /alliance/v1/alliances/{allianceId}/activity/public
GET    /alliance/v1/alliances/{allianceId}/activity            (authentifié)
POST   /alliance/v1/alliances/{allianceId}/join
POST   /alliance/v1/alliances/{allianceId}/applications
POST   /alliance/v1/applications/{applicationId}/cancel
POST   /alliance/v1/applications/{applicationId}/accept
POST   /alliance/v1/applications/{applicationId}/reject
POST   /alliance/v1/alliances/{allianceId}/invitations
GET    /alliance/v1/invitations/mine
POST   /alliance/v1/invitations/{invitationId}/accept
POST   /alliance/v1/invitations/{invitationId}/decline
POST   /alliance/v1/invitations/{invitationId}/revoke
POST   /alliance/v1/membership/leave
POST   /alliance/v1/membership/{targetPlayerId}/kick
POST   /alliance/v1/membership/{targetPlayerId}/promote
POST   /alliance/v1/membership/{targetPlayerId}/demote
POST   /alliance/v1/membership/{targetPlayerId}/transfer-leadership
POST   /alliance/v1/alliances/dissolve
POST   /alliance/v1/alliances/profile
POST   /alliance/v1/diplomacy/{targetAllianceId}/propose
POST   /alliance/v1/diplomacy/{proposerAllianceId}/accept
POST   /alliance/v1/diplomacy/{proposerAllianceId}/reject
POST   /alliance/v1/diplomacy/{otherAllianceId}/cancel
POST   /alliance/v1/war/declare
```

Toutes les routes mutantes exigent `Authorization: Bearer <token>` (même
mécanisme que `/game/v1/*`). `alliance.session_required` si absent/invalide.

## 21. Gaps de production connus

1. **Aucune persistance SQL** (section 17).
2. **`ChatConversationId` non auto-peuplé** à la création — le lien Alliance
   ⟷ conversation de chat réelle reste à câbler explicitement.
3. **Rôle client-déclaré dans `LocalChatAudienceResolver`** (pré-existant,
   pas introduit cette session) — maintenant qu'un vrai modèle de membership
   serveur-autoritaire existe (`IAllianceRepository.GetActiveMembership`),
   c'est l'occasion de faire vérifier le rôle chat par le VRAI modèle
   Alliance au lieu de faire confiance au client. Non fait cette nuit (hors
   scope, mais noté comme amélioration naturelle immédiate).
4. **Fenêtre Unity non branchée** (section 19).
5. **`AllianceClient` Unity non créé** — le pattern à suivre
   (`HiveResearchClient.cs`) est documenté et prêt à répliquer.
6. **Renommage d'alliance ne régénère pas le slug** (section 10).
7. Flag `Alliance:Enabled=true` en dev/tests, `false` en production — à
   activer explicitement en production seulement après le câblage Unity/Web.

## 22. Feature flags

`Server/src/BeeKingdom.Server/appsettings.json` : `Alliance:{Enabled,
DiplomacyEnabled, WarEnabled, MaxMembers}` = `true/true/true/100`.
`appsettings.Production.json` : tous `false` (conservateur, conforme à la
consigne "Production default conservative: FALSE").

Ne pas confondre avec le flag pré-existant `LiveAllianceEnabled` (readiness
gate séparé, pour l'ownership/territoire "live", pas pour ce nouveau service
de domaine).
