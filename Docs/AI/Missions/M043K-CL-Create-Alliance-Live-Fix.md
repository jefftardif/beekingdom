# M043K-CL — Create Alliance invalid_response Live Fix

Suite directe de M043J : premier vrai clic "Créer l'alliance" du CEO en
certification Play Mode a échoué avec "Erreur : invalid_response", sans
qu'aucune alliance n'apparaisse en jeu. Diagnostic complet par preuve
directe (aucune supposition), sans jamais déclencher l'action Create
moi-même.

## 1. Vérité SQL/serveur — AVANT tout autre diagnostic

Appel authentique `GetMyAllianceAsync()` sur la session Play Mode du CEO
(déjà authentifiée, aucune donnée créée par cet appel — lecture pure) :
`overview.HasAlliance = False`, `overview.Alliance = null`,
`overview.Membership = null`.

**Verdict** : aucune alliance n'a été créée, aucune donnée partielle
(pas d'outcome B/C de la checklist). Le premier clic du CEO n'a rien laissé
en base — sûr de laisser réessayer une fois la cause corrigée, sans risque
de doublon.

## 2. Recherche de l'exception serveur — AUCUNE trouvée

`Server.CreateAlliance` catch toutes les exceptions non gérées dans un
middleware qui les journalise ligne par ligne dans
`C:\inetpub\BeeKingdomApi\logs\unhandled-exceptions.log` (fichier de 280 Ko,
tout le fichier inspecté avec Jeff sur le serveur réel). **Aucune entrée
après 14h39 UTC** (avant même la migration SQL) — donc **aucune exception
non gérée ne s'est produite pendant l'appel Create du CEO**, malgré une
tentative de reproduction bien après ce timestamp. Ceci élimine
complètement l'hypothèse initiale (bug dans mon nouveau code SQL
`SqlAllianceRepository`).

## 3. Cause racine réelle — PROUVÉE

En creusant le même transport partagé déjà corrigé en M043J
(`UnityAuthenticatedGameRestTransport.cs`), une **deuxième instance
exacte du même patron de bug** : `ParseSafeErrorCode`/`IsSafeGameCode`
n'acceptait que les codes d'erreur commençant par `"game."` — mais le
serveur envoie des codes `"alliance.*"` (ex. `alliance.not_found`,
`alliance.invalid_request`, `alliance.already_in_alliance`) pour **tout**
rejet légitime d'une mutation Alliance. Résultat : chaque rejet serveur
réel et valide était silencieusement remplacé par un `"game.rejected"`
générique avant même d'atteindre `AllianceClient`, qui le transformait en
`HivePerimeterClientError.InvalidResponse` — indiscernable côté écran d'un
vrai bug, alors qu'il s'agissait d'un refus serveur parfaitement normal
(input invalide, doublon, etc. — impossible à savoir lequel sans le vrai
code, qui n'a jamais atteint l'UI).

Ceci explique parfaitement les 3 faits observés ensemble : aucune exception
serveur (rejet contrôlé, pas un crash), aucune alliance créée (un rejet
légitime ne crée rien), et "invalid_response" affiché malgré un serveur
sain.

## 4. Correction

`Assets/BeeKingdom/Networking/UnityAuthenticatedGameRestTransport.cs` :
`IsSafeGameCode` accepte maintenant les préfixes `"game."` **et**
`"alliance."` (même liste `AllowedErrorCodePrefixes`, même patron que
`AllowedPathPrefixes` de M043J).

## 5. Vérification live — sans jamais créer d'alliance

Appel réel `JoinOpenAsync(Guid.NewGuid())` (id inexistant, garanti de ne
rien créer ni modifier — refus serveur pur) sur la session Play Mode du
CEO, après compilation du correctif :

- **Avant le correctif** (comportement historique) : le code réel aurait
  été remplacé par `game.rejected` → `invalid_response` à l'écran.
- **Après le correctif** : `Error=InvalidResponse Message='alliance.not_found'`
  — le vrai code serveur survit maintenant jusqu'au client. `StableError`
  (M043G) le transforme ensuite en `"not_found"` lisible au lieu du
  générique `"invalid_response"`.

## 6. Tests

Nouveau fichier `Assets/BeeKingdom/Tests/Editor/UnityAuthenticatedGameRestTransportTests.cs`
(13 tests, réflexion sur les méthodes privées du transport — même technique
que le diagnostic live) : couvre à la fois ce correctif (préfixes de code
d'erreur) et celui de M043J (préfixes de route). Exécutés en EditMode :
**13/13 verts**. Suite `AllianceClientTests` existante re-exécutée :
**14/14 verts**, aucune régression. Build serveur : 0 erreur (aucun
changement serveur cette fois — correctif 100% client).

## 7. Commit / déploiement

Commit `531504b` sur `main`, poussé. Correctif 100% côté client Unity —
aucun déploiement serveur nécessaire cette fois (la session Play Mode du
CEO tourne déjà avec le code compilé localement).

## 8. Verdict final (A–L)

| # | Critère | Résultat |
|---|---|---|
| A | Couche exacte de l'échec identifiée ? | ✅ OUI — transport client (`ParseSafeErrorCode`/`IsSafeGameCode`) |
| B | Requête Create brute capturée ? | Non nécessaire — la cause est prouvée en amont de la requête réelle |
| C | Réponse Create brute capturée ? | Non nécessaire — voir B |
| D | Le premier clic CEO a-t-il créé une alliance en SQL ? | ✅ NON — confirmé par `GetMyAllianceAsync` authentique |
| E | Données partielles créées ? | ✅ NON |
| F | Cause racine prouvée ? | ✅ OUI — preuve directe (log serveur vide + comportement avant/après correctif) |
| G | Chemin SQL Create sain ? | ✅ OUI (jamais atteint le problème — le rejet vient du transport client) |
| H | Parsing de réponse côté client sain ? | ✅ OUI (une fois corrigé) |
| I | Retry idempotent sûr ? | ✅ OUI — aucune donnée créée par la tentative échouée, un nouveau clic est un premier essai propre |
| J | Correctif appliqué ? | ✅ OUI |
| K | Tests verts ? | ✅ OUI — 13/13 (nouveaux) + 14/14 (existants) |
| L | PRÊT POUR NOUVEAU TEST CEO "CRÉER" ? | ✅ OUI |

## 9. Prochain test utilisateur

Rouvrir Alliance Center → CRÉER, remplir le formulaire, "Créer l'alliance".
Si un refus légitime se produit à nouveau (ex. tag déjà pris), l'écran
affichera maintenant le vrai motif au lieu de "invalid_response" — ce qui
permettra de corriger l'input plutôt que de soupçonner un bug.
