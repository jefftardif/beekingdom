# M043S-CL — Real Alliance Invitation: Click Has No Visible Result

## ADDENDUM (même session, après le rapport initial) — vraie cause racine trouvée et corrigée

Le correctif de retour visuel ci-dessous a permis au CEO de voir, pour la
première fois, le VRAI résultat de son clic : une erreur (`invalid_response`,
puis `game.rejected` une fois le message brut journalisé). Investigation
poussée jusqu'à la cause exacte, prouvée en direct par reflection (appel
réel `InvitePlayerForProofAsync`, avec l'accord explicite du CEO pour ce
test) :

**`CreateInvitationRequest.InvitedPlayerId` est typé `PlayerId` (record
struct sans `[JsonConverter]` propre, par design). Le client envoie ce
champ comme un GUID brut (`"invitedPlayerId":"<guid>"`), mais
`System.Text.Json` attend par défaut une forme objet
(`{"invitedPlayerId":{"value":"<guid>"}}`) pour ce type. La désérialisation
du corps de requête échouait donc À L'INTÉRIEUR du binding ASP.NET, AVANT
même d'atteindre `ExecuteAlliance`/`AllianceService.CreateInvitation` — le
client recevait la forme d'erreur générique .NET, pas
`AllianceErrorEnvelope`, d'où le repli `"game.rejected"`.**

**Preuve définitive : la table `dbo.AllianceInvitations` était
complètement vide (aucune ligne, aucun filtre) malgré plusieurs vraies
tentatives du CEO — cette route n'avait jamais fonctionné une seule fois
depuis sa création.**

Corrigé, au point de rupture exact et rien d'autre :
`Server/src/BeeKingdom.Alliance/Models/AllianceContracts.cs` —
`[property: JsonConverter(typeof(PlayerIdJsonConverter))]` ajouté
uniquement sur `CreateInvitationRequest.InvitedPlayerId`.
`Server/src/BeeKingdom.Shared/ValueObjects/Identifiers.cs` — nouveau
`PlayerIdJsonConverter` (lit/écrit un GUID brut). **Ne touche pas** au type
`PlayerId` lui-même (une première tentative l'avait fait — a cassé 19 tests
non liés à cause d'un changement de forme de sérialisation ailleurs dans le
code : hachage d'idempotence Chat, fixtures de test, etc. — annulée).

Nouveau test : `CreateInvitationRequest_DeserializesTheExactWireShapeAllianceClientSends`
(désérialise le JSON exact que le client envoie avec les mêmes options que
`ConfigureHttpJsonOptions`). Suite complète : 482 tests, 473-482 verts selon
exécution (1 échec isolé confirmé préexistant/instable, sans lien). Build :
0 erreur.

**Déployé (autorisation explicite du CEO reçue), confirmé sain** : API
`Healthy`, Alliance Test [BKT] et membership Leader du CEO intacts après
déploiement.

**Vérification finale — succès réel, prouvé de bout en bout :** avec
l'accord explicite du CEO (donné en direct après plusieurs échecs de clic
manuel dus à un problème d'UI distinct — voir ci-dessous), une invitation
réelle a été créée pour Stara via le même chemin de code que le bouton
"Inviter" (`InvitePlayerCoreAsync`), et confirmée en base par le CEO :

```
InvitationId    : 0b7afca1-abf1-4a3f-b2f0-f8f37b1390f7
AllianceId      : 5feafc8c-365b-43ea-a5a7-0818419f9261 (Alliance Test [BKT])
InvitedPlayerId : 77510147-cc80-4922-9bde-aa8a296cdd68 (Stara)
InvitedByPlayerId : da420f03-f0cf-4cb6-8328-297f83af34a7 (CEO)
Status          : Pending
CreatedAtUtc    : 2026-09-03 12:41:40 UTC
RespondedAtUtc  : NULL
```

**Bug distinct découvert, non résolu ce soir :** les clics manuels du CEO
sur le bouton "Inviter" ne déclenchaient toujours pas la méthode
(`busy=False`, `Model.State=Ready` confirmés sains au moment du clic,
aucun log, aucun changement visuel) - un problème UI/détection de clic
séparé du bug serveur corrigé ici. Non investigué en profondeur (hors
temps de cette session) ; signalé pour une prochaine session.

---


Le CEO a cliqué UNE fois "Stara → Inviter" dans Alliance Center. Rien de
visible n'a changé : modal ouvert, Stara toujours listée, bouton toujours
"Inviter", aucune confirmation, aucune erreur.

## 1. Vérité serveur — AUCUNE invitation créée

Requête SQL en lecture seule fournie par le CEO sur
`dbo.AllianceInvitations` filtrée sur `AllianceId = BKT` et
`InvitedPlayerId = Stara` : **aucune ligne retournée**.

**Classification : le clic n'a jamais abouti à une invitation persistée.**
Impossible de distinguer avec certitude "B — rejetée par le serveur" de
"C — jamais parvenue au serveur" sans preuve supplémentaire : le code
côté client, avant ce correctif, **n'écrivait aucun log en cas d'échec**
(voir section 3) — un rejet serveur réel et un clic qui n'a jamais déclenché
la requête étaient rigoureusement indiscernables du point de vue du CEO
comme du mien. Reproduire l'action pour trancher aurait créé une seconde
tentative d'invitation, explicitement interdit par la mission.

**A. Did CEO's first click create an invitation? NON.**
**B. InvitationId identified? N/A — aucune invitation n'existe.**
**C. No duplicate invitation exists? OUI (par construction — zéro
invitation, donc zéro doublon).**

## 2. Cause racine de l'absence de retour visuel — prouvée par lecture de code

`AllianceCenterPanelController.InvitePlayerCoreAsync` (avant correctif)
appelait bien `client.CreateInvitationAsync(...)`, mais :

- **jetait le résultat** (jamais assigné, jamais lu) ;
- ne rafraîchissait que `Model` (le survol général de l'alliance,
  `RefreshCoreAsync(true)`) — jamais `invitePlayerSearchResults`, la liste
  affichée par le modal ;
- en cas d'exception, `Model` passait bien à l'état `Error`, mais le corps
  du modal `DrawAllianceInvitePlayerBody` ne lit jamais `Model.State` — il
  ne consulte que `InvitePlayerSearchResults`/`InviteSearchStatus`/`IsBusy` ;
- **aucun `Debug.Log*` nulle part dans ce chemin** — succès ou échec, rien
  n'était journalisé.

Donc : que le clic ait réussi, échoué avec une vraie erreur serveur, ou
n'ait jamais atteint le réseau, le résultat visible pour le CEO aurait été
**identique dans les trois cas** : rien ne change. C'est la cause prouvée,
pas supposée, de "aucune confirmation, aucune erreur visible".

**E. Root cause of missing UI feedback proven? OUI.**

## 3. Correctif appliqué

`Assets/BeeKingdom/Playground/AllianceCenterPresentation.cs` :

- Nouveau `InvitationRowStatus` (`Eligible`/`Sending`/`Sent`/
  `AlreadyPending`/`Error`) porté par `PlayerSearchResultModel` (état
  mutable par ligne, pas un magasin parallèle — la ligne existante de la
  liste de résultats de recherche déjà réelle).
- `InvitePlayerCoreAsync` : marque la ligne `Sending` avant l'appel,
  `Sent` sur succès (et rafraîchit l'aperçu comme avant), `AlreadyPending`
  si le serveur répond `already_invited`/`target_already_in_alliance`
  (précision du vrai code d'erreur serveur), `Error` sinon — **et journalise
  désormais systématiquement** (`Debug.LogWarning`) le code d'erreur exact
  ou le type d'exception, corrigeant le silence total constaté en section 1.

`Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` :

- La ligne du joueur dans le modal reflète maintenant l'état réel :
  "Envoi…" (désactivé) pendant l'appel, "Envoyée" (désactivé) après succès,
  "Déjà invité" (désactivé) si une invitation était déjà en attente,
  "Réessayer" (actif) en cas d'erreur — plus jamais "Inviter" actionnable
  après un envoi réussi.

**F. Invitation UI state corrected? OUI.**

## 4. Contrat de requête — vérifié, inchangé depuis M043L

`CreateInvitationWireRequest` utilise des champs publics C# (`InvitedPlayerId`,
`ClientRequestId`), et le codec partagé a `IncludeFields = true` depuis
M043L — donc le JSON envoyé n'est plus vide, comme déjà prouvé pour
Create Alliance. Rien à changer ici.

**D. Exact request/result traced? OUI — le contrat de requête est correct
et inchangé ; ce qui manquait était en aval de l'appel réseau, pas dans sa
construction.**

## 5. Idempotence serveur — déjà correcte, maintenant testée explicitement

`AllianceService.CreateInvitation` a déjà deux protections indépendantes,
lues directement dans le code :

- Idempotence par `ClientRequestId` (`GetInvitationReceipt`) : un rejeu du
  même clic (même clé) retourne l'invitation existante, n'en crée jamais
  une seconde.
- Garde métier (`GetPendingInvitation(allianceId, invitedPlayerId)`) :
  toute tentative avec une clé *différente* pour un joueur déjà invité
  échoue avec `already_invited`, sans jamais créer de second enregistrement.

Deux nouveaux tests dans `AllianceServiceTests.cs` couvrent exactement le
scénario Stara (second clic avec une clé différente → `already_invited`,
zéro doublon ; rejeu avec la même clé → même `InvitationId`, zéro
doublon).

**H. Server idempotency/duplicate protection verified? OUI.**

## 6. Survie à la réouverture du modal — non implémentée, honnêtement

Le modal ne "sait" pas encore, à la réouverture, qu'un joueur a déjà une
invitation en attente **avant** que le CEO ne retente de l'inviter — l'état
`AlreadyPending` n'apparaît qu'*en réaction* à une tentative (le serveur le
révèle à ce moment-là). Une détection *proactive* dès l'affichage des
résultats de recherche nécessiterait une nouvelle route serveur ("liste des
invitations sortantes en attente pour cette alliance") — **elle n'existe
pas aujourd'hui**, et un commentaire déjà présent dans
`AllianceService.Dissolve` documente explicitement ce choix ("pas
d'index `ListPendingInvitationsForAlliance`, seulement `...ForPlayer`,
ajouter ça juste pour ce cas rare n'en valait pas la peine"). Ajouter cette
route aurait été un vrai nouveau périmètre serveur, hors de ce que cette
mission demandait de corriger — non fait, signalé ici pour décision future,
sans magasin parallèle inventé pour contourner.

**G. Pending invitation survives modal reopen? NON — limite connue,
justifiée, pas corrigée ce soir.**

## 7. Côté Stara — vérifié sans impersonation

`Invitation_CreateAcceptFlow` (test serveur déjà existant, toujours vert)
prouve que `ListMyInvitations(invitee)` — la même méthode que
`GET /alliance/v1/invitations/mine` — retournerait bien l'invitation à
l'invité une fois créée. Aucune connexion Stara effectuée, comme demandé.

**I. Stara incoming invitation exists server-side? N/A — aucune invitation
n'a été créée pour Stara (section 1) ; le mécanisme lui-même est prouvé
fonctionnel par le test existant pour tout invité réel.**

## 8. Bug distinct trouvé en chemin (hors périmètre, signalé séparément)

`Program.cs` enregistre la route d'acceptation d'invitation avec des
antislashs (`\alliance\v1\invitations\{id}/accept`) au lieu de slashs —
cette route ne peut jamais matcher une vraie requête HTTP. Repéré en
traçant les routes voisines, **non corrigé ce soir** (hors périmètre
explicite de cette mission — accepter une invitation, pas en créer une) ;
signalé comme tâche séparée.

## 9. Tests

`Server/tests/BeeKingdom.Tests/AllianceServiceTests.cs` — 2 nouveaux tests :

- `Invitation_SecondCallForSameStillPendingTarget_ThrowsAlreadyInvited_NoDuplicateRowCreated`
- `Invitation_SameClientRequestIdReplayed_IsIdempotent_ReturnsSameInvitationNoDuplicate`

Suite serveur complète : 481 tests, 470-478 verts selon l'exécution (2-3
échecs isolés confirmés préexistants/instables sous exécution parallèle
complète — `AllianceAndLeadersChannelsRequireAllianceRoles`,
`Qualification_returns_incorrect_then_advances_and_replays` — tous deux
repassent au vert exécutés seuls, aucun lien avec ce correctif), 8 ignorés
(SQL, préexistant). Suite ciblée `AllianceServiceTests` seule : **48/48
verts**. Build serveur complet : 0 erreur.

Aucun nouveau test Unity automatisé : `AllianceCenterPanelController` vit
dans l'assembly par défaut `Assembly-CSharp` (`Assets/BeeKingdom/Playground/`
n'a pas de `.asmdef` propre), et `BeeKingdom.Tests.asmdef` ne peut
structurellement pas le référencer — limitation déjà documentée dans
`AllianceClientTests.cs` (commentaire M043-CL) pour la même raison. Ce
n'est pas un raccourci pris ce soir : c'est une contrainte d'architecture
d'assemblies déjà actée dans ce projet, hors périmètre de cette mission à
lever. Vérifié par compilation Unity (0 erreur) + relecture de code.

**J. Tests green? OUI (côté serveur, ciblé et complet hors instabilité
préexistante confirmée non liée).**

## 10. Déploiement

**Aucun changement au code serveur de production** — `AllianceService.
CreateInvitation` est inchangé ; seuls des tests ont été ajoutés
(`AllianceServiceTests.cs`, jamais exécuté en production). Le correctif
réel est entièrement côté Unity
(`AllianceCenterPresentation.cs`, `HiveViewProductUiPresenter.cs`),
compilé localement, **non commité/poussé**, comme pour chaque changement
Unity de cette session.

**K. Server deployment required? NON.**

## 11. Verdict final (A–L)

| # | Critère | Résultat |
|---|---|---|
| A | Did CEO's first click create an invitation? | ❌ NON |
| B | InvitationId identified? | N/A |
| C | No duplicate invitation exists? | ✅ OUI |
| D | Exact request/result traced? | ✅ OUI |
| E | Root cause of missing UI feedback proven? | ✅ OUI |
| F | Invitation UI state corrected? | ✅ OUI |
| G | Pending invitation survives modal reopen? | ❌ NON (limite connue, justifiée) |
| H | Server idempotency/duplicate protection verified? | ✅ OUI |
| I | Stara incoming invitation exists server-side? | N/A (aucune créée) |
| J | Tests green? | ✅ OUI |
| K | Server deployment required? | ❌ NON |
| L | NEXT HUMAN ACTION | **CEO INVITE RETRY** |

## 12. Prochain test utilisateur

Rouvrir Alliance Center → Inviter → rechercher "St" → cliquer "Inviter" sur
Stara **une fois**. Attendu : le bouton passe à "Envoi…" puis "Envoyée"
(désactivé) — plus de silence. Si une erreur survient cette fois, elle
sera visible ("Réessayer") et journalisée côté Unity (`[AllianceInvite] ...`
dans la console), ce qui permettra un diagnostic réel au lieu d'un
mystère. Ne pas confirmer la persistance de l'invitation en base tant que
le retour visuel n'est pas positif.
