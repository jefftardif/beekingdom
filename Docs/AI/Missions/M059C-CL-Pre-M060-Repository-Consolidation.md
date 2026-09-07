# M059C-CL — Consolidation / versioning avant M060

Date : 2026-09-07
Agent : Claude Code
Objectif unique : mettre proprement sous contrôle de version le travail déjà réalisé
et validé (M057/M058/M059/correctif SQL), sans perdre, écraser, réécrire ou embarquer
de travail non lié. Aucun push, aucun déploiement, aucune migration production.
`LivingHive.unity` non touchée.

Périmètre étendu en cours de route par un addendum CEO : une régression Chat Royal
réelle ("Discuter" → "Chat serveur indisponible") a été diagnostiquée et corrigée
avant de clore cette mission — voir section 8.

---

## 1. État Git initial

Checkout principal (`C:\projets\beekingdomgame-master`, branche `main` — c'est là que
tourne l'Éditeur Unity connecté au MCP, **pas** le worktree de cette session, qui est
resté figé à un commit antérieur sans le travail M059/chantier en cours ; voir la note
technique laissée à la fin de la mission M057 précédente).

Avant toute modification de cette mission, `git status` montrait un arbre de travail
avec :
- 18 fichiers modifiés non commités, mêlant plusieurs missions indépendantes dans les
  mêmes fichiers (confirmé uniquement par lecture des hunks, jamais par nom de fichier
  seul — voir section 3) ;
- ~20 fichiers non suivis : nouveaux tests/rapports M059, rapports d'**autres**
  missions (M047, M048, M049, M049B, M049C, M051B, M052B, M053B, M054B — aucun lien
  avec cette consolidation), et des artefacts de résultats de tests serveur (`.trx`).

## 2. Commit M057 Construction/Progress/Audio déjà présent

`b9f4ff16` — `feat(hive): close M057 - alliance close-path fix, cyan construction UX,
upgrade SFX`. Déjà CLOSED/PASS selon son propre rapport
(`M057-CL-Construction-Progress-Visual-Audio-Closeout.md`, déjà commité dans ce même
commit). Rien de ce périmètre n'a été recommité.

**Collision de numérotation confirmée et non corrigée rétroactivement**, comme
demandé : il existe DEUX rapports "M057-CL" distincts et sans rapport l'un avec
l'autre —
`M057-CL-Alliance-Help-Catalog-Drift-Fix.md` (le correctif SQL, commité dans
`fb7aa9c9`) et `M057-CL-Construction-Progress-Visual-Audio-Closeout.md` (le chantier
visuel/audio, commité dans `b9f4ff16`). Les deux fichiers restent tels quels.

## 3. Changements M058 retrouvés

**Déjà entièrement commités**, dans `fb7aa9c9` : `AllianceScreenOverlayLeakTests.cs`
(5 tests) + le hunk correspondant de `HiveViewProductUiPresenter.cs`
(`ReleaseAllianceScreenOverlays` comme point de sortie unique du périmètre Alliance) +
`M058-CL-Alliance-Profile-World-Input-Leak-Fix.md`. Rien à committer ici.

**Régression découverte pendant cette consolidation** (pas une nouvelle mission — une
correction) : le commit M057 `b9f4ff16` avait modifié
`ClosePremiumScreensInPriorityOrderCore` pour faire sortir tout le menu Alliance dès
la 1ʳᵉ fermeture d'un profil de membre, afin de satisfaire deux assertions de
`SandboxLivingHiveUiStabilizationTests`. Cela cassait silencieusement
`AllianceScreenOverlayLeakTests.AllianceBackStackStillClosesProfileFirstThenTheScreen`
(M058-CL, exercé via le vrai chemin `OpenAllianceOverlayForExternalHost`) : la pile de
retour validée (1ᵉʳ retour → Centre d'Alliance, monde toujours bloqué à raison ; 2ᵉ
retour → sortie réelle) est un comportement **intentionnel** protégé depuis M043O-CL.

Cause racine des deux tests fautifs : `OpenAllianceMemberProfileForProof` (un
raccourci de harnais) positionne `activeMainMenuId="Alliance"` — un état qu'**aucun
chemin d'ouverture réel ne produit jamais**, déjà documenté comme infidélité connue
dans le rapport M058-CL lui-même. Les deux tests encodaient cette bizarrerie de
harnais plutôt que le vrai comportement en deux temps.

**Corrigé et commité séparément** (`cff44b0c`) : `ClosePremiumScreensInPriorityOrderCore`
restauré à sa forme M058-CL d'origine, et les deux tests corrigés pour attendre la
fermeture en deux temps réelle. `AllianceScreenOverlayLeakTests` 5/5,
`SandboxLivingHiveUiStabilizationTests` 22/22.

## 4. Changements M059 retrouvés (non commités, hors chantier progress/SFX déjà dans M057)

Isolés hunk par hunk (jamais par nom de fichier — voir méthode section 5) :
- Vue Colonie serveur-first + bandeau de provenance (`HiveViewProductUiPresenter.cs`,
  8 hunks distincts vérifiés un par un) ;
- isolation du cache d'aperçu local par compte authentifié
  (`LocalPreviewHiveProgress.cs`) ;
- purge du cycle de vie compte/cache au (dé)branchement de session
  (`MobileAccountSessionRuntimeBootstrap.cs`) ;
- gate d'input manquant pour la fenêtre "Amélioration en cours"
  (`HiveMapOverlayInputGateBootstrap.cs`, **un seul hunk sur deux** — voir section 5) ;
- tests d'isolation (`ColonyViewAccountIsolationTests.cs`, 10 tests) ;
- 3 clés de localisation propres à ce travail.

**Commité séparément** (`e0e2f88d`) — `fix(hive): isolate colony state and improve
upgrade UX (M059-CL)`, accompagné de `Docs/AI/Missions/M059-CL-New-Player-Feedback-
Account-Isolation-Upgrade-UX.md`.

## 5. Changement DatabaseCatalog retrouvé

**Déjà entièrement commité**, dans `fb7aa9c9` (message : "sync the M045 catalog
drift"). Aucun diff restant sur `Server/src/BeeKingdom.Database/DatabaseCatalog.cs` -
confirmé (`git diff --stat` vide sur ce fichier avant toute action de cette mission).
Aucune nouvelle migration créée. Test re-vérifié vert cette mission (section 7).

## 6. Changements non liés volontairement laissés hors commits

Vérifiés un par un, jamais devinés par nom de fichier :

| Fichier | Contenu réel (lu, pas supposé) |
|---|---|
| `HiveMapOverlayInputGateBootstrap.cs` (hunk restant) | `PlayerSummaryOverlayOpenForExternalHost`/`PlayerProfileOverlayOpenForExternalHost` — écran Profil Joueur, mission distincte |
| `HiveMapQueueSidebarBootstrap.cs` | idem, blocage sidebar pour Profil Joueur |
| `HiveMapResourceHudBootstrap.cs` | `OnEnable`/`OnGUI` pour `DrawPlayerIdentityOverlaysForExternalHost` — Profil Joueur |
| `HiveMapUiOcclusion.cs` | rects d'occlusion pour les panneaux Profil Joueur |
| `SandboxLivingHiveUiStabilizationTests.cs` (hunks restants, hors la correction section 3) | tests `PlayerSummaryOpensProfileAndBlocksUnderlyingHiveInput` etc. — Profil Joueur |
| `LivingHiveMenuCanvas.cs` | police/chat, sans rapport |
| `Assets/Fonts/Resources/Cinzel-Regular SDF.asset` | régénération de police TMP, sans rapport |
| `strings.en-US.json` / `strings.fr-CA.json` (hunk restant) | `building_upgrade.progress.levels`/`.remaining` — appartiennent au chantier M057 déjà fermé (`b9f4ff16`), pas à cette consolidation ; laissées pour le propriétaire de ce commit |
| `Docs/Claude/Claude_Continuation.md` | dirty avant cette mission, contenu d'une autre session, non touché |
| `Assets/BeeKingdom/Playground/Resources/PremiumBeeIcons/research_ready.png.meta` | méta orpheline (icône Recherche, M049B), sans lien |
| `Docs/AI/Missions/M047*.md`, `M048*.md`, `M049*.md`, `M049B*.md`, `M049C*.md`, `M051B*.md`, `M052B*.md`, `M053B*.md`, `M054B*.md` | rapports d'**autres** missions déjà passées, aucun rapport avec M057/058/059/SQL |
| `Server/tests/**/TestResults/*.trx` | artefacts de résultats de tests, jamais commités |

**Aucun de ces fichiers n'a été modifié, ajouté, retiré ou même relu en détail au-delà
de ce qui était nécessaire pour l'attribution.** Ils restent exactement dans l'état où
cette mission les a trouvés.

## 7. Tests exécutés + résultats exacts

### Unity EditMode

| Suite | Résultat |
|---|---|
| `ColonyViewAccountIsolationTests` | 10/10 |
| `HiveMapUpgradeProgressWiringTests` | 4/4 |
| `AllianceScreenOverlayLeakTests` | 5/5 (après correction section 3 ; 4/5 avant, régression bloquée et diagnostiquée avant tout commit) |
| `HiveMapSceneReentryInputTests` | 6/6 |
| `BuildingInteractionControllerClickPriorityTests` | 10/10 |
| `SandboxLivingHiveUiStabilizationTests` | 22/22 |
| `BuildingUpgradeFrameworkTests` | 3/3 |
| `GameEventBusTests` | 2/2 |
| `SandboxLivingHiveManualCollectionTests` | 55/55 |
| `ServerChatProviderTests` (7 tests ciblés — voir M059D-CL) | 7/7 |

Compilation Unity vérifiée propre (0 erreur) après chaque étape. L'Éditeur Unity est
devenu non-répondant à trois reprises pendant cette mission (deux fois pile en tentant
`ServerChatProviderTests` en classe complète, une fois lors d'une passe précédente) ;
l'utilisateur l'a redémarré lui-même à chaque fois. Après chaque redémarrage, la
compilation et les tests déjà passés ont été rejoués intégralement plutôt que
supposés toujours valides.

### Serveur .NET

`dotnet test Server/tests/BeeKingdom.Tests --filter
"FullyQualifiedName~DatabaseMigrationTests.CatalogSqlMatchesCheckedInScriptFiles"` →
**1/1 réussi**. Aucune connexion production utilisée ni nécessaire.

## 8. Régression Chat Royal — diagnostiquée et corrigée (addendum CEO)

Voir le rapport dédié : `Docs/AI/Missions/M059D-CL-Chat-Royal-Private-Conversation-
Recovery.md`. Résumé :

1. **Bail de capacités jamais renégocié** — `ServerChatProvider.InvalidateCapabilities()`
   ne remettait jamais `ConnectionState` à `Offline`, donc `LivingHiveChatController.
   OpenAsync()` sautait indéfiniment la reconnexion après l'expiration du bail (5 min
   par défaut). Prouvé en direct dans la session Play Mode réelle du CEO
   (`Status=Error/invalid_conversation_cursor` puis `capability_lease_expired`).
   Non lié à M056A/M058/M059 (diffs relus intégralement, aucun ne touche ce code).
2. **"Discuter" n'appelait aucun endpoint** — `ChatStartPrivateConversation` changeait
   seulement d'onglet sans jamais créer/retrouver de conversation avec le joueur tapé.
   Complété via `CreatePrivateConversationAsync`, même patron que `CreateGroupAsync`
   déjà existant, même endpoint déjà utilisé par les autres canaux.

Commité séparément (`f64380d2`) — `fix(chat): recover the Chat Royal capability-lease
reconnect and wire private conversation creation (M059D-CL)`. 7 tests EditMode verts
(2 nouveaux + 5 non-régression ciblés — la classe complète n'a pas pu tourner en un
seul appel, voir M059D-CL section 4.2).

**Non résolu / à confirmer par le CEO** : cliquer Discuter sur un vrai joueur en Play
Mode et confirmer que la conversation s'ouvre réellement. C'est la condition de sortie
explicite posée par le CEO — non remplie tant que ce test manuel n'a pas été refait.

## 9. Commits créés

| # | Hash | Message | Fichiers |
|---|---|---|---|
| 1 | `cff44b0c` | `fix(hive): restore M058's two-step Alliance back-stack (corrects M057 b9f4ff16)` | `HiveViewProductUiPresenter.cs` (1 hunk), `SandboxLivingHiveUiStabilizationTests.cs` (2 tests) |
| 2 | `e0e2f88d` | `fix(hive): isolate colony state and improve upgrade UX (M059-CL)` | `HiveViewProductUiPresenter.cs` (8 hunks restants), `LocalPreviewHiveProgress.cs`, `MobileAccountSessionRuntimeBootstrap.cs`, `HiveMapOverlayInputGateBootstrap.cs` (1 hunk), `ColonyViewAccountIsolationTests.cs`+`.meta`, `strings.en-US.json`+`strings.fr-CA.json` (3 clés), `Docs/AI/Missions/M059-CL-New-Player-Feedback-Account-Isolation-Upgrade-UX.md` |
| 3 | `f64380d2` | `fix(chat): recover the Chat Royal capability-lease reconnect and wire private conversation creation (M059D-CL)` | `ServerChatProvider.cs`, `LivingHiveChatController.cs`, `HiveViewProductUiPresenter.ChatRoyal.cs`, `ServerChatProviderTests.cs`, `Docs/AI/Missions/M059D-CL-Chat-Royal-Private-Conversation-Recovery.md` |

Méthode de staging pour les fichiers mêlant plusieurs missions
(`HiveViewProductUiPresenter.cs`, `HiveMapOverlayInputGateBootstrap.cs`,
`strings.*.json`) : diff hunk par hunk, patch construit à la main pour chaque hunk
attribué, appliqué à l'index via `git apply --cached`. Jamais un `git add` du fichier
entier quand plusieurs missions y coexistaient.

## 10. Fichiers restant modifiés/non suivis après consolidation

Exactement la liste de la section 6 — inchangée, non commitée, préservée. Confirmé
par `git status` après le 3ᵉ commit : aucun fichier de cette liste n'apparaît comme
staged, aucun n'a été touché.

## 11. Ambiguïtés restantes

1. Les 2 clés de localisation `building_upgrade.progress.levels`/`.remaining`
   (section 6) appartiennent au chantier M057 déjà fermé et commité (`b9f4ff16`) mais
   n'ont jamais été ajoutées aux fichiers de traduction — un vrai manque, mais hors du
   périmètre de CETTE consolidation (qui ne recommite pas ce qui est déjà commité).
   `BeeLocalization.Text` retombe sur son fallback français codé en dur, donc aucune
   régression fonctionnelle ; juste un avertissement console. À corriger par le
   propriétaire du chantier M057.
2. La suite complète `ServerChatProviderTests` (80+ tests) n'a jamais pu tourner en un
   seul appel — l'exécuteur de tests Unity s'est bloqué dessus deux fois de suite, y
   compris après redémarrage complet de l'Éditeur. Les tests ciblés couvrent le
   correctif et les chemins voisins, mais une preuve de non-régression totale sur
   cette classe reste à faire en batchmode CLI hors session interactive.
3. Le worktree de cette session (`C:\projets\beekingdomgame-master\.claude\worktrees\
   gracious-heyrovsky-0c7cee`) reste figé à un commit antérieur à tout ce travail — les
   trois commits de cette mission vivent uniquement dans le checkout principal
   (`C:\projets\beekingdomgame-master`, branche `main`), qui est l'endroit réel où
   tourne l'Éditeur Unity connecté au MCP. Point déjà signalé en fin de mission M057 ;
   à garder en tête pour toute session future qui commencerait dans ce worktree.

## 12. Confirmations explicites

- **Aucun push.** Les trois commits (`cff44b0c`, `e0e2f88d`, `f64380d2`) vivent
  uniquement en local sur `main`, non synchronisés avec un quelconque remote.
- **Aucun déploiement.**
- **Aucune migration production.** Le correctif SQL (`fb7aa9c9`) était déjà en place
  avant cette mission ; aucune nouvelle migration n'a été créée ; aucune connexion à
  un environnement de production n'a été utilisée (le test `dotnet test` tourne
  entièrement en mémoire/local).
- **`LivingHive.unity` non touchée.** Aucun fichier de cette scène, ni aucune
  référence à son ancien flux, n'a été modifié. Toute vérification runtime a eu lieu
  exclusivement dans `Environment2D5D_HiveMap_Test`.

---

## 13. Addendum — échec du retest CEO, cause réelle différente de celle corrigée

Le CEO a refait le test après `f64380d2` : échec identique (`Chat serveur
indisponible`). Sonde en lecture seule sur le snapshot déjà en mémoire (aucun nouvel
appel réseau déclenché) :

```
IsConfigured=True  Status=Error  ErrorCode=invalid_conversation_cursor
```

**Ce n'est PAS le bail de capacités déjà corrigé** (`capability_lease_expired`) — le
correctif de la section 8/`f64380d2` reste réel et nécessaire, mais ne couvre pas ce
que le CEO rencontre. La vraie cause, `invalid_conversation_cursor`, se produit dès
le premier chargement de la liste de conversations sur une session fraîche. Détail
complet, hypothèses écartées par lecture statique du code, et diagnostic temporaire
ajouté (non commité, `ServerChatProvider.cs`) : voir
`Docs/AI/Missions/M059D-CL-Chat-Royal-Private-Conversation-Recovery.md`, section 9.

**Incident de session** : une tentative de capturer la cause en appelant directement
le provider réseau depuis un script de diagnostic a gelé l'Éditeur Unity (nécessitant
un redémarrage manuel) — cause identifiée et retenue en mémoire durable
(`feedback_unity_mcp_script_execute_hangs.md`) pour ne plus la reproduire.

**Preuve capturée et cause racine confirmée** au retest suivant. Console, Play Mode
réel : `cursorIsNull=False | cursorLength=0` — le curseur reçu est une **chaîne vide
`""`**, pas `null`. Cause exacte : `UnityEngine.JsonUtility` (le backend JSON de
production, jamais exercé par les tests EditMode existants qui substituent tous
`SystemTextJsonBackend`) désérialise un `"nextCursor":null` JSON en chaîne C# vide,
jamais en `null` — limite documentée de ce parseur, pas un défaut serveur
(`ChatService.ListConversations` ne renvoie jamais que soit un curseur non-vide, soit
un littéral `null`). Le contrat "`null`/chaîne vide = pas de page suivante" existait
déjà et était appliqué correctement par `LoadAllConversationsAsync`, mais pas par
`ValidateConversationPage` (`!= null` au lieu de `IsNullOrWhiteSpace`), atteint plus
tôt dans l'appel et qui explosait donc en premier.

**Correctif appliqué, purement client, aucun contrat serveur touché** : normalisation
`null`/vide→`null` au point unique de désérialisation (`UnityChatJsonCodec.Map`) +
garde-fou symétrique dans `ValidateConversationPage`. Diagnostic temporaire retiré.
2 tests de régression neufs reproduisant la cause exacte, 9/9 tests EditMode ciblés
verts. Détail complet : `Docs/AI/Missions/M059D-CL-Chat-Royal-Private-Conversation-
Recovery.md`, section 10.

**Incident de session** (avant la capture de preuve ci-dessus) : une tentative de
capturer la cause en appelant directement le provider réseau depuis un script de
diagnostic a gelé l'Éditeur Unity (nécessitant un redémarrage manuel) — cause
identifiée et retenue en mémoire durable (`feedback_unity_mcp_script_execute_hangs.md`)
pour ne plus la reproduire ; le diagnostic final a été obtenu uniquement en lisant le
snapshot déjà en mémoire et via un vrai clic CEO en Play Mode, jamais par un nouvel
appel réseau déclenché depuis un script.

---

## Verdict

**NOT READY FOR M060 — Chat Royal private conversation runtime regression (correctif appliqué, retest CEO requis)**

Le correctif du bail de capacités (`f64380d2`) et le correctif du curseur de
pagination vide (non encore commité au moment de la rédaction de cette section — voir
M059D-CL section 10.3) sont tous deux en place, compilés et testés. La seule
condition de sortie reste celle posée explicitement par le CEO, non encore
remplie : un nouveau clic **Discuter** en Play Mode réel, sur un vrai joueur, doit
ouvrir effectivement la conversation.
