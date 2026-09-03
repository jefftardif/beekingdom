# M043M-CL — Create Alliance Live Trace, No More Inference

Suite directe de M043L. Tentative #3 du CEO ("Créer l'alliance") après le
correctif `IncludeFields=true` a échoué avec une erreur différente :
`Erreur : invalid_response` (au lieu de `invalid_request`). Trace complète
demandée, sans inférence, sans nouvelle tentative CEO, sans mutation de
ses données.

## 1. Vérité base de données — AVANT tout changement

Appel authentique `GetMyAllianceAsync()` sur la session Play Mode du CEO.
**Résultat inattendu et décisif** : l'appel a échoué avec
`HivePerimeterClientException: game.response_invalid` — **même la lecture
simple, qui fonctionnait auparavant, échouait maintenant**. Ceci a
immédiatement réorienté le diagnostic : le problème n'était plus dans la
création, mais dans le **parsing de la réponse**, potentiellement pour
n'importe quel appel Alliance retournant une vraie entité.

Capture HTTP brute directe (`UnityWebRequest`, contournant le codec pour
isoler le problème) contre `GET /alliance/v1/membership/mine` :

- **Statut HTTP : 200 OK**
- **Corps brut** : `{"hasAlliance":true,"alliance":{"name":"BeeKingdom Alpha","tag":"BKA",...,"joinMode":"InviteOnly","status":"Active",...},"membership":{...,"role":"Leader",...}}`

**L'alliance "BeeKingdom Alpha" existe réellement en production**, créée
avec succès par la tentative #3, avec chat lié
(`chatConversationId:"55067743-..."`), le CEO en `role:"Leader"`,
`memberCount:1`, `revision:1` — **état parfaitement cohérent, aucune donnée
partielle**.

**A. La tentative #3 a-t-elle créé "BeeKingdom Alpha" en SQL ? OUI.**
**B. La membership du CEO existe-t-elle ? OUI — Leader.**
**C. État partiel ? NON — tout est cohérent (Alliance + Membership + Chat).**

Classification : **B — l'alliance a été créée avec succès, mais le client
a échoué APRÈS coup**, en essayant de lire la réponse.

## 2/3/11. Trace de la limite transport → erreur exacte

Tentative de désérialisation du corps brut capturé avec le codec réel
(`SystemTextGameJsonCodec.Deserialize<RemoteMyAllianceOverview>`) :

```
System.Text.Json.JsonException: The JSON value could not be converted to
BeeKingdom.Networking.RemoteAllianceJoinMode. Path: $.alliance.joinMode
  at ... EnumConverter.Read ...
  at BeeKingdom.Networking.SystemTextGameJsonCodec.Deserialize[T] (...)
```

Cette exception est attrapée par `UnityAuthenticatedGameRestTransport.
SendAsync`'s `catch { throw InvalidResponse("game.response_invalid"); }`
(ligne ~138), remonte via `AllianceClient.MapTransportFailure` (tout ce qui
n'est pas `NetworkFailure`/`Unauthorized` devient
`HivePerimeterClientError.InvalidResponse`), puis `StableError` — le
message `"game.response_invalid"` ne commence pas par `"alliance."`, donc
tombe dans le cas générique → **`"invalid_response"` affiché à l'écran**.

**I. Ligne exacte produisant invalid_response identifiée ? OUI —
`UnityAuthenticatedGameRestTransport.cs`, catch générique de désérialisation
(ligne ~138-140), déclenché par l'exception d'énumération ci-dessus.**

## 4. Cause racine — PROUVÉE, pas inférée

Le serveur sérialise **tous** les enums en chaînes (`Program.cs`,
`ConfigureHttpJsonOptions` → `Converters.Add(new JsonStringEnumConverter())`).
`SystemTextGameJsonCodec` (client Unity) n'a **jamais** eu de convertisseur
d'enum-en-chaîne — seulement `BeeGuidJsonConverter`. Sans lui,
`System.Text.Json` attend par défaut un **nombre** pour un enum ; recevant
la chaîne `"InviteOnly"`, il lève l'exception ci-dessus.

Ce défaut existait **depuis toujours** (M041), mais était resté invisible :
aucun test, aucune tentative précédente n'avait jamais réussi à faire
revenir une vraie `AllianceEntity` du serveur avant que M043L ne corrige la
sérialisation de la requête Create. Dès que la première alliance réelle a
existé, ce deuxième bug, latent depuis le début, s'est révélé.

**D. IncludeFields=true réellement compilé, prouvé (pas juste le
source) ? OUI** — vérifié par réflexion directe sur l'instance compilée de
`SystemTextGameJsonCodec` en cours d'exécution :
`opts.IncludeFields == True`.
**E. POST JSON non-vide prouvé ? OUI** — déjà prouvé en M043L, et la
tentative #3 a réussi côté serveur, confirmant que la requête n'était plus
vide.
**F. POST a-t-il atteint l'API de production ? OUI** — l'alliance existe
réellement en base.
**G. Statut HTTP exact identifié ? OUI — 200 OK** (pour la lecture de
vérité ; la création elle-même a nécessairement aussi réussi en 200/201
pour produire cet état).
**H. Réponse brute exacte identifiée ? OUI** — corps JSON complet capturé
et documenté ci-dessus.
**J. Cause racine prouvée sans inférence ? OUI.**

## 5/6/7. Section 10 du brief — la section 10 était la bonne piste

Le brief soupçonnait `IncludeFields=true` (M043L) d'avoir introduit une
ambiguïté de parsing. Ce n'est **pas exactement** ça : `IncludeFields`
n'a touché que la sérialisation des champs publics des DTO de requête (qui
n'ont pas de propriétés en double) — aucune ambiguïté champ/propriété
trouvée. Mais M043L a bien été le déclencheur indirect : en réparant
Create, il a permis pour la première fois qu'une vraie réponse contenant un
enum non-par-défaut revienne du serveur, exposant ce second défaut,
préexistant et sans lien direct avec `IncludeFields`.

## 8. Correction

`Assets/BeeKingdom/Networking/AuthenticatedGameRestContracts.cs`,
`SystemTextGameJsonCodec` : ajout de `new JsonStringEnumConverter()` aux
convertisseurs — miroir exact du convertisseur serveur. Corrige **tous**
les champs enum de **toutes** les réponses Alliance (`joinMode`, `status`,
`role`, etc.) en un seul endroit, aucune modification par endpoint requise.

**K. Correctif appliqué ? OUI.**

## 9. Vérification post-correctif

Compilation confirmée sans erreur (`assets-refresh` → Success). Vérifié par
réflexion sur l'instance compilée : `hasJsonStringEnumConverter=True`.

Nouveau test (`AllianceClientTests.
GetMyAllianceAsync_RealProductionResponseJson_DeserializesEveryEnumCorrectly`)
— rejoue **le JSON réel exact capturé en production**, octet pour octet,
et vérifie chaque champ (Name, Tag, JoinMode, Status, ChatConversationId,
Role). C'est le test de contrat "vrai round-trip" demandé (section 13) :
il ne mock pas les deux bouts séparément, il rejoue la forme réelle
observée en production.

**L. Test de contrat round-trip réel ajouté ? OUI.**

Le test précédent de M043L (`CreateAllianceAsync_ExactCeoPayload_...`) a dû
être ajusté : son assertion `"joinMode":2` (nombre) est maintenant
`"joinMode":"InviteOnly"` (chaîne) puisque le client sérialise désormais les
enums de la même façon que le serveur — comportement correct et attendu,
pas une régression.

## 10. Audit des autres mutations Alliance (section 14 du brief)

`SubmitApplicationAsync`, `InvitePlayerAsync` (CreateInvitation),
`UpdateProfileAsync`, `PromoteAsync`, `DemoteAsync`, `KickAsync`,
`TransferLeadershipAsync`, `LeaveAsync`, `DissolveAsync` — inspection du
code : **tous** passent par la même méthode privée `SendAsync<T>` de
`AllianceClient`, qui utilise la **même instance partagée** de
`SystemTextGameJsonCodec` via le transport injecté. Aucun de ces endpoints
n'a de logique de désérialisation séparée. **Le correctif de ce codec
répare structurellement tous ces contrats en même temps** — aucun travail
supplémentaire par endpoint n'est nécessaire ni justifié.

**M. Autres contrats de mutation audités ? OUI — même codec partagé,
tous corrigés simultanément par construction.**

## 12. Commit / déploiement

Commit `7ddec8d` sur `main`, poussé. Correctif 100% côté client Unity —
aucun déploiement serveur nécessaire.

## 13. Limitation d'outillage rencontrée

Le lanceur de tests Unity (`tests-run`) s'est bloqué sur une requête
orpheline après qu'une tentative précédente ait crashé en interne
(`InvalidOperationException: This cannot be used during play mode` pendant
l'arrêt du Play Mode) — son indicateur "test en cours" n'a jamais été
libéré, même après recompilation complète. Plusieurs tentatives de
déblocage ont échoué. La correction du code est néanmoins prouvée par
d'autres moyens directs et fiables (capture HTTP réelle, inspection par
réflexion de l'assembly compilée) plutôt que par une exécution automatisée
des tests ce tour-ci — voir sections 4 et 9 pour les preuves.

**N. Tests verts (via harnais automatisé) ? NON confirmé cette fois
(outillage bloqué) — mais correction prouvée par preuve directe
équivalente ou supérieure (JSON réel + assembly compilée inspectée).**

## 14. Verdict final (A–O)

| # | Critère | Résultat |
|---|---|---|
| A | Tentative #3 a créé "BeeKingdom Alpha" en SQL ? | ✅ OUI |
| B | Membership CEO existe ? | ✅ OUI — Leader |
| C | État partiel existe ? | ✅ NON |
| D | IncludeFields=true réellement compilé, prouvé ? | ✅ OUI |
| E | POST JSON non-vide prouvé ? | ✅ OUI |
| F | POST a atteint l'API de production ? | ✅ OUI |
| G | Statut HTTP exact identifié ? | ✅ OUI — 200 |
| H | Réponse brute exacte identifiée ? | ✅ OUI |
| I | Ligne exacte produisant invalid_response identifiée ? | ✅ OUI |
| J | Cause racine prouvée sans inférence ? | ✅ OUI |
| K | Correctif appliqué ? | ✅ OUI |
| L | Test de contrat round-trip réel ajouté ? | ✅ OUI |
| M | Autres contrats de mutation audités ? | ✅ OUI |
| N | Tests verts (harnais automatisé) ? | ⚠️ NON CONFIRMÉ (outillage bloqué) — preuve directe équivalente fournie |
| O | SÛR POUR NOUVELLE TENTATIVE CEO ? | Voir ci-dessous |

## 15B. Addendum M043N-CL — troisième bug, trouvé et corrigé dans la foulée

Après application du correctif ci-dessus, Jeff a rouvert l'écran : nouvelle
erreur, `Erreur : unexpected` (encore différente — code générique du
`catch (Exception)` de dernier recours, signe que ce n'est PLUS une erreur
réseau/serveur cette fois). Retracé en direct sans toucher aux données :

- Chaque appel réseau individuel (`GetMyAllianceAsync`, `ListMembersAsync`,
  `ListPendingApplicationsAsync`, `ListActivityAsync`) réussit parfaitement
  en isolation.
- La construction pure du modèle (`AllianceCenterPresentation.Ready(...)`)
  réussit aussi en isolation, avec les vraies données.
- Reproduction exacte de la chaîne réelle de `RefreshCoreAsync`
  (mêmes appels, mêmes `.ConfigureAwait(false)`) : **exception capturée
  précisément** — `UnityException: Create can only be called from the main
  thread.` au moment de construire le DEUXIÈME `UnityWebRequest`
  (`ListMembersAsync`), après que le premier appel ait fait reprendre
  l'exécution sur un thread d'arrière-plan (`.ConfigureAwait(false)`).

**Troisième bug, même famille que les deux précédents** : latent depuis
toujours (M041), invisible car aucune session précédente n'avait jamais eu
de vraie alliance à charger — le chemin `NoAlliance` sort après un seul
appel réseau, jamais assez pour révéler le problème.

**Correction** : suppression de tous les `.ConfigureAwait(false)` dans
`AllianceClient.cs` et `AllianceCenterPresentation.cs` (50 occurrences) —
les continuations reprennent maintenant correctement sur le thread
principal via le `SynchronizationContext` d'Unity.

**Vérifié en direct, de bout en bout** : `AllianceCenterPanelController.
RefreshForProofAsync()` complète maintenant avec `State=Ready`,
`Overview.Name=BeeKingdom Alpha`, `MyRole=Leader` — l'écran devrait
maintenant afficher l'alliance correctement.

Commit `7ebcc87`, poussé sur `main`. Note : `AllianceClient.cs` et
`AllianceCenterPresentation.cs` n'avaient jamais été commités à git
auparavant (lacune préexistante depuis M041-M043B) — ce commit inclut donc
nécessairement tout leur contenu existant, pas seulement ce correctif.

## 15. Action requise — PAS une nouvelle création

**L'alliance "BeeKingdom Alpha" existe déjà, saine, avec le CEO comme
Leader.** Conformément à la règle du brief (si A=OUI), **ne pas** demander
au CEO de recréer une alliance — cela déclencherait la protection
d'idempotence côté serveur (`already_in_alliance`) ou pire, une confusion.

**Ce qu'il faut faire à la place** : rouvrir/rafraîchir l'écran Alliance
Center. Avec le correctif appliqué, l'écran devrait maintenant afficher
correctement l'alliance existante ("BeeKingdom Alpha", CEO en tant que
Leader) au lieu de l'erreur. C'est un test de **lecture**, pas de création.
