# M043Q-CL — Alliance Center Real Chat UI Integration

Objectif : afficher dans l'onglet "Chat" d'Alliance Center la vraie
conversation d'alliance (`Alliance.ChatConversationId`), en réutilisant
entièrement le backend/client Communication existant (M042/M043B) — aucun
second système de chat.

## 1. Vérité serveur — PROUVÉE, participant réel présent

Preuve directe (pas d'inférence) : appel `GET
/chat/v1/conversations/{ChatConversationId}/messages` sur la conversation
réelle de l'Alliance du CEO, avec le vrai jeton du CEO extrait de sa session
Play Mode live → **`200 {"items":[],"nextAfterSequence":null}`**. Un `200`
avec liste vide est la preuve que `ChatService.RequireRead` (qui exige une
ligne `ChatConversationParticipant` réelle, stockée, `RemovedAtUtc=null`,
`CanRead=true`) accepte déjà le CEO sur cette conversation — le participant
existe bel et bien, créé par `AllianceService.CreateOrLinkAllianceChat` au
moment de la création de l'alliance (le membership est sauvegardé **avant**
l'appel chat, donc `IAllianceMembershipResolver` — bien câblé en DI après le
résolveur `Null` par défaut de Chat — voit le Leader et autorise
`BuildParticipants`). Zéro message est un résultat valide (personne n'a
encore écrit).

**A. Server truth prouvée ? OUI — participant réel confirmé par lecture
directe, pas par inférence.**

## 2. Cause réelle du symptôme — pas une question d'autorisation

Le même appel `GET /chat/v1/conversations` (liste "mes conversations") ne
retournait PAS cette conversation dans les captures précédentes. `ListConversations`
et `RequireRead` interrogent tous les deux la même jointure
`ChatConversationParticipants` — donc ce n'était jamais un problème
d'autorisation serveur. Le vrai coupable, côté client : `LivingHiveChatController.
SelectConversationAsync` refuse tout id absent de la liste chargée par
`OpenAsync()` → `LoadAllConversationsAsync`, et cet appel peut ne pas
(encore) avoir fait remonter la conversation Alliance au moment où l'écran
Alliance Center tente de l'ouvrir (pagination/temps de l'abonnement
temps réel). Le drawer de chat existant (`DrawAllianceChatRealBody`,
M043B-CL) appelait directement `SelectAsync(conversationKey)` — exactement
le point qui échouait.

**B. Cause racine du symptôme identifiée avec preuve ? OUI.**

## 3. Correctif — réutilise le transport existant, n'ajoute rien de neuf

- [LivingHiveChatController.cs](../../../Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatController.cs) :
  nouvelle méthode `SelectKnownConversationAsync(conversationId, title,
  channelType, ct)` — ouvre un id de conversation déjà connu du serveur même
  s'il n'est pas encore dans la liste agrégée locale, en réutilisant
  `RefreshSelectedAsync` (donc `ReconcileFullyAsync`) tel quel. Façade
  statique `LivingHiveChatRuntime.SelectKnownAsync` ajoutée à l'identique du
  reste de la classe.
- [HiveViewProductUiPresenter.cs](../../../Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs) :
  - `EnsureAllianceChatOpenedAndSelected` appelle maintenant `SelectKnownAsync`
    au lieu de `SelectAsync`, et lit le `Snapshot.Status` après coup pour
    distinguer un vrai refus serveur (`AuthenticationRequired`/`Error` →
    "not_a_member") d'un succès — le comportement de refus d'accès
    kické/jamais-membre reste intact, juste détecté différemment.
  - L'onglet "Chat" d'Alliance Center (`allianceOverviewTab == "chat"`)
    routait vers `DrawAllianceComingSoon` — il appelle maintenant
    `DrawAllianceChatRealBody`, le même corps déjà réel utilisé par le tiroir
    latéral (même correctif, même trajet réseau, aucune deuxième
    implémentation).

**C. Second système de chat créé ? NON. D. Business logic forkée ? NON —
`ReconcileFullyAsync`/`SendAsync` inchangés, appelés tels quels.**

## 4. Bug distinct découvert et corrigé : nom d'expéditeur jamais résolu

En traçant `SenderDisplayNameSnapshot` (ouvert par M043P section 4, laissé
"PARTIEL") : `ChatService.SendMessageAsync` fixait
`$"player:{playerId:N}"` **sans condition**, jamais le vrai nom — donc même
un message envoyé par un joueur avec un vrai `DisplayName` onboardé se
serait affiché comme un identifiant tronqué dans le chat, indépendamment de
tout ce qui touche Alliance.

Corrigé avec le même pattern d'inversion de dépendance que
`IAllianceMembershipResolver` (M042) : nouveau seam
[IChatSenderDisplayNameResolver](../../../Server/src/BeeKingdom.Chat/Audience/IChatSenderDisplayNameResolver.cs)
dans `BeeKingdom.Chat` (défaut `Null...` → comportement inchangé si jamais
câblé), implémentation réelle
[PlayerDirectoryChatSenderDisplayNameResolver](../../../Server/src/BeeKingdom.Accounts/PlayerDirectoryChatSenderDisplayNameResolver.cs)
dans `BeeKingdom.Accounts` (enveloppe `IPlayerDirectoryService.GetByPlayerId`
— exactement la même source authoritative que M043P), enregistrée en DI
après le défaut de Chat (`Program.cs` appelle déjà `AddBeeKingdomChat` avant
`AddBeeKingdomAccounts`, donc le résolveur réel gagne). Repli exact sur
l'ancien comportement (`"player:{id}"`) si le résolveur ne trouve rien.

**E. Gap "Communication identity PARTIAL" de M043P fermé ? OUI.**

## 5. Sécurité de l'appartenance — héritée, pas réinventée

`SelectKnownConversationAsync` ne contourne aucune autorisation : elle
appelle exactement les mêmes méthodes réseau
(`ReconcileFullyAsync`/`SendAsync`) qui, côté serveur, passent toujours par
`ChatService.RequireRead`/`RequireWrite` (ligne `ChatConversationParticipant`
réelle). Un non-membre ou un membre exclu reçoit toujours un refus serveur
réel (`UnauthorizedAccessException` → `RemoteChatError` → `Snapshot.Status`
= `AuthenticationRequired`/`Error`, détecté et affiché comme
"not_a_member"). Test déjà existant et toujours vert :
`EndToEnd_MemberCanSendMessageAfterJoin_KickedMemberIsRejectedByRealAudienceResolver`.

**F. Accès membres/non-membres/exclus vérifié ? OUI (par test existant,
inchangé par ce correctif).**

## 6. États d'écran

Déjà couverts par `DrawAllianceChatRealBody` (M043B-CL), maintenant
atteignable depuis l'onglet : "Chat indisponible" (pas de
ChatConversationId), "Session de chat non prête…" (runtime pas encore
configuré), "Connexion au chat de l'alliance…" (ouverture en cours), "Accès
au chat de l'alliance perdu" (refus serveur réel), liste de messages vide
(zéro message = état valide, pas d'erreur), composer + bouton Envoyer.

**G. États Loading/Empty/Ready/Error couverts ? OUI (repris, pas
reconstruits).**

## 7. Tests

`Server/tests/BeeKingdom.Tests/AllianceChatIntegrationTests.cs` — 2
nouveaux tests :

- `SendMessage_UsesResolvedDisplayNameWhenResolverIsWired` — le résolveur
  injecté fournit un nom, le message stocké porte ce nom exact.
- `SendMessage_FallsBackToPlayerIdWhenResolverHasNoName` — repli exact sur
  l'ancien format si le résolveur ne connaît pas le joueur.

Suite serveur complète : **462/462 verts** (hors `Claim_returns_typed_receipt_and_replay_conflict`,
échec isolé confirmé préexistant/instable — repasse au vert seul, sans
rapport avec ce correctif ; 8 tests SQL ignorés comme d'habitude, base locale
non disponible). Build serveur complet : 0 erreur. Compilation Unity : 0
erreur (`assets-refresh` confirmé propre après chaque changement).

**H. Tests verts ? OUI — 462/462 (+ 2 nouveaux), 1 échec préexistant confirmé
non lié.**

## 8. Vérification en direct (lecture seule, aucune mutation)

Après le correctif, session Play Mode CEO relancée pour recharger les
assemblies : `LivingHiveChatRuntime.IsConfigured` reste `false` tant que
l'écran Communication/Alliance Chat n'a pas été ouvert au moins une fois par
le joueur (comportement normal, inchangé — le runtime se configure au
premier accès, pas au démarrage). Aucune action Play Mode n'a été effectuée
au nom du CEO au-delà de cette vérification en lecture seule
(`GET /chat/v1/conversations/{id}/messages`, jamais d'envoi).

**I. Aucune action Play Mode CEO effectuée par CL ? OUI — seule preuve
serveur en lecture seule collectée directement.**
**J. Aucune mutation de données ? OUI.**
**K. Premier message envoyé par CL ? NON — réservé au CEO, comme demandé.**

## 9. Hors périmètre (non touché)

- Traduction, réactions/emoji, mentions, modération : inchangés.
- Le tiroir latéral de chat (`allianceChatDrawerOpen`, bouton action bar
  "chat") bénéficie du même correctif de fond (`SelectKnownAsync`) mais son
  UI n'a pas été modifiée.
- Aucun changement au terrain, à `LivingHive.unity`, ni au module
  Communication gelé au-delà de ce seam minimal et additif.

## 10. Verdict final (A–K)

| # | Critère | Résultat |
|---|---|---|
| A | Server truth prouvée (pas d'inférence) ? | ✅ OUI |
| B | Cause racine identifiée avec preuve ? | ✅ OUI |
| C | Second système de chat créé ? | ✅ NON |
| D | Business logic forkée ? | ✅ NON |
| E | Gap DisplayName Communication (M043P) fermé ? | ✅ OUI |
| F | Accès membres/non-membres/exclus vérifié ? | ✅ OUI |
| G | États Loading/Empty/Ready/Error couverts ? | ✅ OUI |
| H | Tests verts ? | ✅ OUI (462/462 + 2 nouveaux) |
| I | Aucune action Play Mode CEO par CL ? | ✅ OUI |
| J | Aucune mutation de données ? | ✅ OUI |
| K | Premier message envoyé par CL ? | ✅ NON — réservé au CEO |

## 11. Prochain test utilisateur

Aucun déploiement effectué (changement client Unity + un seam serveur
additif non encore poussé). Pour certifier : ouvrir Alliance Center → onglet
"Chat" en Play Mode connecté à la production — attendu : plus de "À VENIR",
composer visible, "Aucun message pour le moment" si vide, et le premier
message envoyé par le CEO doit apparaître avec son vrai nom (pas
`player:xxxxxxxx`). Correctif serveur (résolveur de nom) nécessite un
déploiement API avant que le nom réel apparaisse sur un NOUVEAU message —
les messages déjà stockés (aucun ici, conversation vide) resteraient sur
leur ancien snapshot.
