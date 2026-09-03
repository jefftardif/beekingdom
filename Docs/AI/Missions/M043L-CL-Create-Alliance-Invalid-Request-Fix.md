# M043L-CL — Create Alliance invalid_request Fix

Suite directe de M043K (validé humainement — l'erreur générique
"invalid_response" a disparu, remplacée par un vrai code serveur). Nouvel
échec observé au premier vrai clic "Créer l'alliance" du CEO :
`Erreur : invalid_request`. Diagnostic complet par preuve directe, sans
jamais déclencher l'action Create moi-même ni modifier les données du CEO.

## 1. Vérité base de données — AVANT tout changement

Appel authentique `GetMyAllianceAsync()` sur la session Play Mode du CEO
(lecture pure, aucune donnée créée) : `HasAlliance=False`,
`Alliance=null`, `Membership=null`. Confirmé après la tentative échouée.

**A. Le CEO n'a toujours aucune alliance : OUI.**
**B. Aucune donnée partielle persistée : OUI** — aucun CreateReceipt,
aucune Membership orpheline, rien à nettoyer avant de laisser réessayer.

## 2. Capture du DTO exact soumis par le CEO

Les champs statiques du formulaire Unity (`allianceCreateNameInput`, etc.)
persistent entre les frames — capturés par réflexion directement sur la
session Play Mode réelle, sans reconstruction manuelle :

| Champ | Valeur exacte capturée |
|---|---|
| Name | `BeeKingdom Alpha` (16 car.) |
| Tag | `BKA` (3 car.) |
| Description | `Alliance officielle de test Alpha BeeKingdom` (44 car.) |
| Language | `fr-CA` (défaut statique — aucun sélecteur visible dans le formulaire) |
| JoinMode | `InviteOnly` |
| ClientRequestId | généré par `SessionAllianceCenterMutationKeySource` (`mobile-alliance-create-<32 hex>`) |

**C. DTO exact capturé : OUI.**

## 3. Validation serveur — chaque condition passée en revue

`AllianceService.CreateAlliance` (seules 4 conditions produisent
exactement `alliance.invalid_request`) :
- `ClientRequestId` vide ou > 128 caractères
- `Name` hors bornes `[NameMinLength=3, NameMaxLength=32]`
- `Tag` hors bornes `[TagMinLength=2, TagMaxLength=5]`
- `Description` > `DescriptionMaxLength=500`

Confirmé qu'aucune configuration (`appsettings.json`,
`appsettings.Production.json`, variables d'environnement du pool IIS)
ne modifie ces bornes par défaut — les valeurs du CEO les respectent
toutes largement.

**Test de reproduction ajouté** (`AllianceServiceTests.
CreateAlliance_AcceptsRealCeoPayloadUnderProductionDefaultOptions`) :
appelle le vrai code serveur (`AllianceService.CreateAlliance`, options par
défaut identiques à la production) avec exactement ce payload. **Résultat :
succès** — la logique de validation serveur, isolée, accepte ce payload
sans problème. Ceci a définitivement écarté l'hypothèse "payload invalide
selon les règles métier".

**E. Toutes les conditions de invalid_request énumérées : OUI.**

## 4. Langue — investiguée et écartée

Aucun sélecteur de langue visible dans le formulaire Create actuel — le
champ statique `allianceCreateLanguageInput` reste toujours à sa valeur
par défaut `"fr-CA"` (code canonique déjà valide, déjà le défaut serveur
documenté dans `AllianceEntity.Language`). `AllianceService.CreateAlliance`
n'applique d'ailleurs **aucune validation de format** sur `Language` — la
valeur du CEO n'a jamais pu être la cause.

**G. Cause "Language" prouvée/écartée : ÉCARTÉE — OUI (prouvé).** Aucun
changement produit nécessaire pour l'instant ; un vrai sélecteur de langue
reste une amélioration future légitime mais hors du périmètre de ce bug.

## 5. Cause racine réelle — PROUVÉE

Avec la logique métier serveur innocentée (section 3) et aucune exception
serveur journalisée (`unhandled-exceptions.log` non modifié depuis
14h39 UTC, confirmé par `Get-Item`/`LastWriteTime` — la tentative de
Create du CEO n'a laissé AUCUNE trace, alors qu'un rejet contrôlé
(`ArgumentException` → 400) ne passe justement jamais par le middleware
qui écrit ce fichier), la cause devait être **avant même que le serveur
ne voie une requête valide**.

Reproduction hors-réseau (test `AllianceClientTests.
CreateAllianceAsync_ExactCeoPayload_ReachesTransportAndSerializesCorrectly`,
transport simulé, zéro risque) avec le DTO exact du CEO : le JSON réellement
sérialisé et envoyé était **`{}`** — un objet vide.

**Cause exacte** : `CreateAllianceWireRequest` (et
`SubmitApplicationWireRequest`, `CreateInvitationWireRequest`,
`UpdateProfileWireRequest`) déclarent leurs données comme **champs publics**
(`public string Name, Tag, ...;`), pas comme propriétés
(`{ get; set; }`). `System.Text.Json` **ignore les champs par défaut** —
`SystemTextGameJsonCodec` ne définissait jamais `IncludeFields = true`.
Résultat : chacun de ces corps de requête POST se sérialisait
silencieusement en `{}`, envoyant `ClientRequestId=null` (entre autres) au
serveur, qui rejetait légitimement avec `alliance.invalid_request` dès la
toute première vérification — un refus **normal et correct** du serveur
face à une requête qui, de son point de vue, était réellement vide.

**F. Champ fautif exact prouvé : OUI — aucun champ en particulier n'était
"invalide", c'est le corps entier de la requête qui n'a jamais été
transmis.**

## 6. JoinMode

Enums `RemoteAllianceJoinMode` (Unity) et `AllianceJoinMode` (serveur)
identiques valeur par valeur (`Open=0, Application=1, InviteOnly=2`), et le
serveur utilise `JsonStringEnumConverter()` avec `AllowIntegerValues=true`
(défaut), donc accepte aussi bien un nombre qu'une chaîne — aucun problème
de ce côté, une fois le corps de requête réellement transmis.

**H. Mapping JoinMode vérifié : OUI — correct des deux côtés.**

## 7. ClientRequestId

Généré par `SessionAllianceCenterMutationKeySource.Create("create")` :
`"mobile-alliance-" + operation + "-" + Guid.NewGuid("N")` — non-vide,
stable pour la durée de vie du call (un seul GUID par clic), respecte les
bornes serveur. Le mécanisme d'idempotence serveur
(`AllianceService.GetCreateReceipt`) est intact et inchangé — un nouveau
clic générera une nouvelle clé, et comme aucune alliance n'a été créée par
la tentative précédente (section 1), un nouvel essai est un premier essai
propre, pas un doublon.

**I. ClientRequestId valide/idempotent : OUI.**

## 8. Correction

`Assets/BeeKingdom/Networking/AuthenticatedGameRestContracts.cs`,
`SystemTextGameJsonCodec` : ajout de `IncludeFields = true` aux
`JsonSerializerOptions`. Correction unique, minimale, qui répare
simultanément les 4 DTO basés sur des champs (Create, SubmitApplication,
CreateInvitation, UpdateProfile) sans toucher à la validation serveur ni
à aucun autre contrat.

**J. Correctif appliqué : OUI.**

## 9. Tests

- Nouveau test serveur `AllianceServiceTests.
  CreateAlliance_AcceptsRealCeoPayloadUnderProductionDefaultOptions` —
  payload exact du CEO contre la vraie logique serveur, options par défaut.
  **Vert.**
- Nouveau test client `AllianceClientTests.
  CreateAllianceAsync_ExactCeoPayload_ReachesTransportAndSerializesCorrectly`
  — capture le JSON réellement sérialisé, vérifie chaque champ présent et
  correct. **Vert** (échouait avant le correctif avec `{}`).
- Suite `AllianceClientTests` complète : **15/15 verts**.
- Suite `UnityAuthenticatedGameRestTransportTests` (M043J/M043K) :
  **13/13 verts**, aucune régression du correctif précédent.
- Suite `PlayerDirectoryClientTests` : **7/7 verts**, aucune régression
  du changement de codec partagé.
- Build serveur : 0 erreur (aucun changement serveur cette fois).

**K. Test de contrat exact ajouté : OUI. L. Tests Unity verts : OUI.
M. Tests serveur verts : OUI** (aucun changement serveur, suite Alliance
déjà verte confirmée en M043H/M043K).

## 10. Sécurité SQL

Confirmé en section 1 : la tentative rejetée n'a rien écrit — ni Alliance,
ni Membership, ni Activity, ni Chat, ni receipt. Le serveur rejette avant
toute mutation (la validation a lieu avant le premier `repository.Save`).

## 11. Commit / déploiement

Commit `be49fe1` sur `main`, poussé. Correctif 100% côté client Unity —
aucun déploiement serveur nécessaire.

## 12. Verdict final (A–N)

| # | Critère | Résultat |
|---|---|---|
| A | CEO toujours sans alliance ? | ✅ OUI |
| B | Aucune donnée partielle persistée ? | ✅ OUI |
| C | DTO Create exact capturé ? | ✅ OUI |
| D | JSON sérialisé exact capturé ? | ✅ OUI — `{}` (avant), corrigé (après) |
| E | Chaque condition invalid_request énumérée ? | ✅ OUI |
| F | Champ fautif exact prouvé ? | ✅ OUI — corps entier vide, pas un champ isolé |
| G | Cause "Language" prouvée/écartée ? | ✅ OUI — écartée |
| H | Mapping JoinMode vérifié ? | ✅ OUI |
| I | ClientRequestId valide/idempotent ? | ✅ OUI |
| J | Correctif appliqué ? | ✅ OUI |
| K | Test de contrat exact ajouté ? | ✅ OUI |
| L | Tests Unity verts ? | ✅ OUI |
| M | Tests serveur verts ? | ✅ OUI |
| N | PRÊT POUR NOUVEAU TEST CEO "CRÉER" #3 ? | ✅ OUI |

## 13. Portée de l'impact (au-delà de Create)

Ce bug touchait **tous** les appels utilisant un DTO à champs publics, pas
seulement Create : `SubmitApplicationAsync`, `InvitePlayerAsync`
(`CreateInvitationWireRequest`), `UpdateProfileAsync`. Tous auraient
échoué de la même façon avant ce correctif ; tous sont réparés par le même
changement de codec, sans travail supplémentaire.
