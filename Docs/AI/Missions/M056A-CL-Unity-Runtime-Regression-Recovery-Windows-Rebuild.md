# M056A-CL — Unity Runtime Regression Recovery + Windows Rebuild

Statut : **PARTIEL — 4 defauts corriges et verifies, 3 defauts diagnostiqués avec preuve
mais NON corriges (decision CEO requise). Build Windows NON produite, volontairement.**

Date : 2026-09-06
Agent : Claude Code (seul agent sur le projet depuis le 2026-07-24)

---

## 0. RESUME EXECUTIF — LA REPONSE A LA QUESTION CENTRALE

Le CEO demande : *pourquoi une fonctionnalite Chat/Groupes/UI qui marchait est-elle revenue a
un vieil etat ?*

**Reponse concrete : pour le Chat Royal, elle n'est jamais revenue en arriere — elle n'a
jamais existe.** Ce n'est pas une regression. C'est une maquette qui n'a jamais ete branchee.

L'ecran CHAT ROYAL de `HiveViewProductUiPresenter.cs` est une maquette IMGUI 100 % locale :
ses conversations sont construites en dur par `BuildChatConversations()` (12 conversations
codees en dur, dont `pv-alex`, `pv-marie`, `pv-clara`), ses messages par `BuildChatMessages()`,
et un simulateur (`chatSimulationRng`, seed `20260806`) **injecte de faux messages toutes les
~16 secondes** pour donner l'illusion d'une activite. Aucun de ces chemins ne touche le
serveur.

En parallele, le VRAI backend de communication existe bel et bien et fonctionne
(`ServerChatProvider`, `SignalRChatRealtimeTransport`, `LivingHiveChatController`,
`Server/src/BeeKingdom.Chat/`). Mais il n'est branche que sur **deux** surfaces, et le Chat
Royal n'en fait pas partie :

1. le tiroir de chat d'alliance du QG (`DrawAllianceChatRealBody`, M043B-CL) ;
2. le pont vers le Canvas uGUI `LivingHiveMenu` (`LivingHiveChatBridgeBootstrap`).

**Les bugs 2, 3, 4 et 5 ont donc une seule et meme cause partagee** : ils sont tous les quatre
des symptomes du fait que l'ecran Chat Royal est une maquette non connectee. Ce ne sont pas
quatre bugs independants, et ce ne sont pas des regressions.

Preuve de provenance : l'historique git est **enracine sur un commit BASELINE**
(`4e88f68c BASELINE: recover latest LivingHive production state`, 71 commits au total).
`git log -S "BuildChatConversations"` ne retourne que ce commit unique : les donnees en dur
sont presentes depuis la racine de l'historique disponible. Il n'existe, dans tout l'historique
accessible, **aucune version de ce fichier ou le Chat Royal aurait ete branche au serveur puis
debranche**. Il n'y a donc rien a "recuperer" ; il y a une fonctionnalite a construire.

Les bugs 1, 6 et 7 sont, eux, de vrais defauts runtime independants, sans rapport avec le chat.
Ils sont corriges.

---

## 1. ETAT INITIAL

- Branche : `main`
- HEAD au demarrage : `197fcbc094e0da36dc302fd8062dd40089010088`
- Remote : `origin` → `https://github.com/jefftardif/beekingdom.git`
- Unity : 6000.5.3f1, scene ouverte `Environment2D5D_HiveMap_Test` (buildIndex 0), non dirty
- `ProjectSettings/EditorBuildSettings.asset` : HiveMap officielle a l'index 0, LivingHive desactivee — conforme, non modifie
- Arbre de travail : nombreuses modifications non commitees heritees des missions precedentes
  (M047 a M056B). **Toutes preservees.** Aucune operation git destructive n'a ete effectuee.
  Aucun commit, aucun push, aucun deploiement serveur.

---

## 2. MATRICE DE REGRESSION

| # | Defaut | Reproduit | Nature | Statut |
|---|--------|-----------|--------|--------|
| 1 | Grille jaune | Verifie par lecture de code | Vrai bug, deja corrige en M056B | **CORRIGE + trou supplementaire trouve et bouche** |
| 2 | Chat d'alliance HS | Diagnostique | **Pas une regression** — maquette non branchee | **NON CORRIGE — decision CEO** |
| 3 | Conversations en dur | **Prouve** (code + simulateur) | **Pas une regression** — jamais branche | **NON CORRIGE — decision CEO** |
| 4 | "Nouvelle discussion" HS | **Prouve** | Mixte : maquette + vrai defaut d'affichage | **PARTIEL — bouton corrige** |
| 5 | Creation de groupes HS | **Prouve** | **Pas une regression** — jamais implemente cote Unity | **NON CORRIGE — decision CEO** |
| 6 | Camera morte apres WorldMap | **Prouve par test automatise** | Vrai bug de cycle de vie | **CORRIGE + test** |
| 7 | Fleche FTUE orpheline | **Prouve** | Vrai bug de cycle de vie | **CORRIGE + test** |

### BUG 1 — Grille jaune de debug

La correction M056B est **reelle et correctement placee** : verification faite directement dans
`Assets/Experiments/Environment2D5D/Scripts/DebugHotkeyGuard.cs` et
`FrontalBackdrop.cs` (la garde `DebugHotkeyGuard.Blocked` precede bien la lecture de `xKey`).
La garde couvre le focus IMGUI (`GUIUtility.keyboardControl`), le focus uGUI/TMP, le type de
build et les fenetres Premium. Rien a refaire.

**Mais j'ai trouve un trou de la meme famille, non couvert par M056B :**
`Assets/Experiments/Environment2D5D/Scripts/BuildingPremiumController.cs` lisait
`Keyboard.current` (touches Q et E) **sans aucune garde**. Consequence reelle : taper un mot
contenant un "q" ou un "e" dans le compositeur de chat (« Que », « Merci ») deformait
silencieusement la hauteur du batiment selectionne — exactement le meme mecanisme que la
grille jaune, avec un symptome different. **Corrige** en appliquant la meme garde.

Audit complet effectue : apres correction, toutes les lectures `Keyboard.current` du runtime
(`FrontalBackdrop`, `AnchorMarkerUI`, `AnchorValidation`, `BuildingPerspectiveCamera`,
`BuildingPremiumController`) passent par `DebugHotkeyGuard`. Plus aucune lecture brute.

> Reserve honnete : la confirmation « en tapant reellement dans un composeur focus en session
> live » n'a pas pu etre faite (voir section 7 — Unity s'est arrete en cours de mission). La
> preuve reste au niveau du code, qui est cette fois sans ambiguite.

### BUGS 2/3/4/5 — Chat Royal (cause partagee unique)

Voir section 0 et section 4. Chemins exacts :

- Donnees en dur : `HiveViewProductUiPresenter.cs`, `BuildChatConversations()` (l. 35407) et
  `BuildChatMessages()` (l. 35426), affectees a `chatConversations` / `chatMessagesByConversation`
  en l. 35391-35392.
- Simulateur de faux trafic : `chatSimulationRng` (l. 1365), declenche l. 36286-36313.
- « Nouvelle discussion » : l. 35837-35844. Ne fait que basculer sur le canal prive et ouvrir
  un champ de recherche qui **filtre la liste locale en dur** — il n'interroge aucun annuaire
  de joueurs. Le toast dit lui-meme « (placeholder) ».
- « Creation de groupes » : l. 35845, un simple toast « La creation de groupes arrive a un
  prochain sprint. » Il n'existe **aucune** implementation Unity de creation de groupe a
  restaurer : la recherche dans tout `Assets/` ne trouve ni ecran, ni controleur, ni appel
  reseau de creation de groupe.
- Chat d'alliance : le vrai chemin existe (`DrawAllianceChatRealBody`, l. 33314) mais il est
  dans **l'ecran QG d'alliance**, pas dans le Chat Royal. Les onglets « alliance » du Chat
  Royal (`alliance-general`, `alliance-strategy`, `alliance-recruitment`) sont des maquettes.
  C'est tres probablement ce que le testeur a essaye.

**Seule partie corrigee (defaut d'affichage reel et independant) :** le bouton « Effacer » de
la barre de recherche faisait 46 px de large, trop etroit pour le mot ; IMGUI le tronquait a
l'ecran en « Efface… » — ce qui correspond exactement au rapport du testeur (« half-visible
Efface... »). Largeur portee a 64 px, champ de saisie raccourci d'autant.

### BUG 6 — Camera morte apres aller-retour WorldMap

**Cause exacte, prouvee par test automatise.**

`HiveViewProductUiPresenter` est une **classe statique**. Sa vingtaine de booleens « ecran
ouvert » survivent aux changements de scene (les statics ne se vident qu'au domain reload).
Ces drapeaux sont lus par trois predicats differents, et **un seul garde la camera** :

| Predicat | Ce qu'il garde |
|---|---|
| `PremiumUiBlocksWorldInput()` (l. 10733) | la **camera** (pan + zoom) |
| `HiveMapOverlayInputGateBootstrap.IsAnyOverlayBlocking()` | batiments + menus |

Plusieurs drapeaux figurent dans le **premier** et pas dans le second : `combatPatrolOverlayOpen`
(ouvert par le bouton ATTAQUER de la WorldMap), `communicationPanelOpen`, `missionsCenterOpen`,
`resourceInventoryOpen`, `bestiaryCodexOverlayOpen`, etc.

C'est **exactement** l'asymetrie du rapport : camera morte, batiments et menus vivants.

Aggravation : la HiveMap ne redessine jamais ces overlays (apres login, `Draw()` n'est plus
appele dans cette scene), donc **plus rien ne pouvait les refermer** — l'etat etait
irrecuperable sans redemarrer le processus.

`ResetPremiumScreensForProof()` (l. 39939) remettait deja tous ces drapeaux a zero… mais
n'etait appele **que depuis les tests**, jamais au runtime.

**Correction :** branchement sur le seam de re-initialisation par chargement de scene deja
prevu par le projet, `HiveMapRuntimeBootstrapInitializer.OnSceneLoaded`. On y remet aussi
`GUIUtility.keyboardControl = 0` : cet etat IMGUI est global et survit lui aussi a
`LoadScene`, et il constitue le **second** verrou de la camera via
`DebugHotkeyGuard.TextInputHasFocus` (un champ de saisie encore focus dans la scene precedente
— auth, recherche WorldMap — bloquerait la camera de la meme maniere).

### BUG 7 — Fleche jaune FTUE orpheline

**Cause exacte, prouvee.** `TutorialDialoguePresenter.Hide()` ne remet a zero que **son
propre** etat. Il n'a aucune reference vers la fleche ni vers le bloqueur d'input : les deux
presenters sont des composants freres ajoutes dynamiquement, sans couplage.

Sur toute etape `Require*`, le dialogue est affiche **sans callback de continuation**. Fermer
la bulle ne completait donc jamais l'etape, et rien n'appelait `_arrow.Hide()`. Restaient
orphelins :

- la fleche jaune (`TutorialArrowPresenter._visible`) ;
- l'anneau de pulsation / surbrillance ;
- **le bloqueur d'input plein ecran** (`FtueBlocker`, alpha 0.01, `raycastTarget`,
  `sortingOrder 9000`) — porte par un objet `DontDestroyOnLoad`, donc **survivant aux
  changements de scene**. C'est le pire des trois : invisible, il avale l'input en silence.

Second defaut independant trouve au passage : le bouton « Passer » (QA, Editor) appelait
`Hide()` **avant** `_onContinue?.Invoke()`, or `Hide()` met `_onContinue` a null — l'appel
etait donc **toujours** un no-op et l'etape ne se completait jamais, meme sur les etapes
`Highlight*`. Le bouton « Suite » etait ecrit correctement (capture du callback avant `Hide()`),
pas « Passer ».

**Correction :** point d'entree unique `FtueTutorialBootstrap.DismissPresentation()` (retire
fleche + dialogue + bloqueur), cable vers un hook `TutorialDialoguePresenter.DismissRequested`,
invoque quand la bulle est fermee sans completer l'etape. Ordonnancement de « Passer » corrige.

**La progression n'est volontairement pas touchee** : `FtueProgress` (ChapterId, CurrentStepId,
CompletedSteps, Revision) reste intact, le tutoriel reste reprenable et la presentation sera
reconstruite par `UpdateVisuals` a la prochaine etape. Fermer la presentation n'est pas annuler
le tutoriel — la distinction demandee par le CEO est respectee.

---

## 3. CAUSES RACINES PARTAGEES

- **Bugs 2, 3, 4, 5 → une seule cause :** l'ecran Chat Royal est une maquette IMGUI locale
  jamais connectee au backend de communication reel. Ce ne sont pas quatre bugs, c'est une
  fonctionnalite absente vue sous quatre angles.
- **Bugs 6 et 7 → une cause de meme *forme*, mais independante :** de l'etat de presentation
  qui survit a son proprietaire. Bug 6 : des statics de classe qui survivent au changement de
  scene. Bug 7 : un presenter qui nettoie son etat sans nettoyer ses freres, sur un objet
  `DontDestroyOnLoad`. Dans les deux cas, le nettoyage existait deja quelque part mais n'etait
  jamais appele sur le chemin reel.
- **Bug 1 → independant :** lecture clavier brute ignorant le focus de saisie.

Aucun « renderer Chat legacy reactive » n'a ete trouve. L'hypothese a haute valeur proposee
par le CEO en Phase B est donc **infirmee** : il n'y a pas deux implementations de Chat Royal
dont la mauvaise serait selectionnee. Il n'y en a qu'une, et elle est fausse depuis l'origine.

---

## 4. ARCHITECTURE CHAT — CE QUI FAIT AUTORITE

| Element | Chemin | Etat |
|---|---|---|
| Backend serveur | `Server/src/BeeKingdom.Chat/` | Reel, existant |
| Provider serveur Unity | `Gameplay/Communication/ServerChatProvider.cs` | Reel |
| Temps reel | `SignalRChatRealtimeTransport.cs` | Reel |
| Controleur | `LivingHiveChatController.cs` + `LivingHiveChatRuntime` | Reel |
| Activation | `MobileAccountSessionRuntimeBootstrap.ActivateChatForActiveSession` (l. 587) | Reel, sur session authentifiee |
| Surface reelle 1 | `DrawAllianceChatRealBody` (QG d'alliance) | Reel |
| Surface reelle 2 | `LivingHiveChatBridgeBootstrap` → Canvas uGUI | Reel |
| **Ecran CHAT ROYAL** | `HiveViewProductUiPresenter` | **Maquette locale, non branchee** |
| Fixtures de test | `LocalChatProvider` | Legitime, reserve aux tests — **non touche** |

`LivingHiveChatRuntime.IsConfigured` est le verrou : il n'est vrai qu'apres
`ReconfigureAsync`, appele uniquement depuis `LivingHiveChatBootstrap.ActivateAsync`, elle-meme
appelee a l'ouverture de session authentifiee. Le tiroir d'alliance affiche
« Session de chat non prete… » si ce verrou est faux, et « Chat indisponible pour cette
alliance. » si l'alliance n'a pas de `ChatConversationId`. **Ces deux messages permettent de
distinguer un probleme de session d'un probleme de donnees d'alliance** — a demander au CEO
lors du prochain test.

**Aucun backend parallele n'a ete cree. Aucun ChatV2. Aucune fixture de demo detruite.**

---

## 5. DECISION REQUISE DU CEO (bloquant pour les bugs 2, 3, 5)

Brancher le Chat Royal sur le backend reel n'est pas une « recuperation », c'est une
**construction de fonctionnalite** : liste de conversations serveur, annuaire/recherche de
joueurs, creation de conversation privee, creation de groupe (titre, selection de membres,
chef, transfert de direction). C'est un chantier de mission complete, explicitement hors du
perimetre « correctifs minimaux » de M056A.

Je n'ai **volontairement pas** supprime les conversations en dur : sans backend branche, cela
viderait completement l'ecran Chat Royal du CEO sans rien livrer en echange — strictement pire
qu'aujourd'hui. C'est un choix qui doit venir du CEO, pas de moi.

Trois options :

- **A** — Mission dediee « Chat Royal reel » : brancher l'ecran sur le backend existant
  (recommande, c'est la seule option qui satisfait le cahier des charges du CEO).
- **B** — Isoler proprement : masquer le Chat Royal du runtime joueur derriere un drapeau
  developpeur, et pointer le bouton Communication vers la seule surface reellement branchee.
  Rapide, honnete, mais retire une fonctionnalite visible.
- **C** — Statu quo assume : garder la maquette en le documentant comme non fonctionnel.

---

## 6. TESTS

Nouveaux tests cibles, **tous verts** :

- `Assets/BeeKingdom/Playground/Editor/HiveMapSceneReentryInputTests.cs` — **6/6**
  Le test parametre ne se contente pas de verifier le reset : il **prouve d'abord** que le
  drapeau bloque bien la camera (`PremiumWorldInputBlockedForProof == true`), puis que le reset
  la libere. C'est la confirmation empirique du mecanisme du bug 6, pas seulement de la
  correction.
- `Assets/BeeKingdom/Playground/Editor/FtuePresentationCleanupTests.cs` — **4/4**

Compilation propre verifiee apres chaque lot (`console-get-logs` filtre Error : vide), et
presence effective des correctifs dans les assemblies runtime confirmee par reflection en
Editor (`initializer=True resetSeam=True`, `ftueBootstrap=True DismissPresentation=True`).

**Limite a signaler :** la suite EditMode complete (1553 tests) **n'a pas pu etre executee
jusqu'au bout**. Deux tentatives ont depasse le delai d'inactivite MCP de 300 s ; a la seconde,
l'editeur Unity s'est arrete. Les fichiers sources sont intacts et l'arbre de travail est
preserve, mais **la non-regression globale n'est pas prouvee**. A refaire en batchmode CLI
(hors MCP) avant toute livraison.

---

## 7. BUILD WINDOWS — NON PRODUITE (volontaire)

Aucune build n'a ete generee, conformement a la consigne de la mission :
« Do NOT proceed to Windows rebuild if major Unity regressions remain. »

Sur les 7 defauts, **3 restent ouverts** (2, 3, 5) et la checklist de certification Phase D ne
peut pas passer : « no hard-coded/demo conversations », « real conversations load »,
« Alliance Chat works », « group creation UI works » sont tous en echec par construction.

Livrer une build maintenant ferait retester a Alex exactement les memes 4 defauts de chat, pour
rien. `WindowsInternalBuildTool.Version` est donc laisse a `0.1.1-alpha-internal-gridfix` et
**le paquet M056B reste le dernier paquet valide**.

Les correctifs des bugs 1, 4 (affichage), 6 et 7 sont prets a etre embarques des que le CEO
tranche la section 5.

---

## 8. FICHIERS MODIFIES PAR M056A

Modifies :
- `Assets/Experiments/Environment2D5D/Scripts/BuildingPremiumController.cs` (bug 1, trou de garde)
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (bug 4, bouton « Effacer »)
- `Assets/BeeKingdom/Playground/HiveMapRuntimeBootstrapInitializer.cs` (bug 6)
- `Assets/BeeKingdom/Tutorial/Runtime/FtueTutorialBootstrap.cs` (bug 7)
- `Assets/BeeKingdom/Tutorial/Runtime/TutorialDialoguePresenter.cs` (bug 7)
- `Assets/BeeKingdom/Tutorial/Runtime/TutorialArrowPresenter.cs` (bug 7, `IsVisible` pour les tests)

Crees :
- `Assets/BeeKingdom/Playground/Editor/HiveMapSceneReentryInputTests.cs`
- `Assets/BeeKingdom/Playground/Editor/FtuePresentationCleanupTests.cs`
- `Docs/AI/Missions/M056A-CL-Unity-Runtime-Regression-Recovery-Windows-Rebuild.md` (ce document)

Aucun commit, aucun push, aucun deploiement serveur. Aucune operation git destructive.
Aucun etat de production (joueur, alliance) n'a ete modifie. LivingHive n'a pas ete reactivee.
