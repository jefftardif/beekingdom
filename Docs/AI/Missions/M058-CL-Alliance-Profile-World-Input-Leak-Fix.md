# M058-CL — Fuite de drapeaux d'input du perimetre Alliance (camera HiveMap bloquee a jamais)

Date : 2026-09-07
Agent : Claude Code
Scene de test : `Environment2D5D_HiveMap_Test` (jamais `LivingHive.unity`)
Statut : correctif livre, prouve par instrumentation runtime + 5 tests EditMode neufs.
Rien n'a ete commite, pousse ni deploye.

---

## 1. Le defaut

Symptome rapporte : apres etre passe par le profil d'alliance, la camera HiveMap
(pan + zoom) ne repond plus du tout, **definitivement**, alors que les batiments et
les menus restent parfaitement cliquables. Aucun ecran n'est visible : rien a fermer.

C'est la meme famille que le bug 6 de M056A-CL (drapeau d'overlay statique jamais
remis a zero), mais **sans changement de scene** — le hook
`HiveMapRuntimeBootstrapInitializer.OnSceneLoaded` ajoute par M056A ne pouvait donc
pas aider ici.

## 2. Diagnostic exact (prouve, pas suppose)

Deux gardes distinctes coexistent dans `HiveViewProductUiPresenter` :

| Garde | Ce qu'elle protege | Consommateur reel |
|---|---|---|
| `PremiumUiBlocksWorldInput()` | la **camera** (pan/zoom) | `BuildingPerspectiveCamera` (`Environment2D5D/Scripts`) |
| `AllianceOverlayOpenForExternalHost` (= `activeHiveMenu == Alliance`) | batiments, menus, occlusion | `HiveMapOverlayInputGateBootstrap`, `HiveMapQueueSidebarBootstrap`, `HiveMapUiOcclusion` |

C'est cette asymetrie qui produit le symptome « camera morte, batiments vivants ».

L'ecran Centre d'Alliance possede **trois overlays qui lui sont propres** :
profil de membre, panneau d'action rapide, tiroir de chat. Ils sont **lus** par
`PremiumUiBlocksWorldInput()`, mais ils ne sont **dessines** que depuis
`DrawAllianceHeadquartersScreen`, qui lui-meme ne tourne que tant que
`activeHiveMenu == HiveMenuMode.Alliance` (verifie : les 3 fonctions de dessin
n'ont aucun autre site d'appel dans tout le fichier).

**Consequence : toute sortie du menu Alliance qui ne nettoyait pas ces trois
drapeaux les rendait orphelins** — vrais, donc bloquants pour la camera, mais plus
rien a l'ecran pour les refermer. Etat sans issue.

Deux chemins de sortie reels etaient dans ce cas :

1. `CloseAllianceOverlayForExternalHost()` — le chemin de fermeture du pont
   HiveMap. Il remettait `activeMainMenuId` et `activeHiveMenu` a zero, mais pas les
   trois overlays internes.
2. `ActivateHiveMenu(autre_menu)` — le joueur ouvre un autre ecran (Recherche,
   Armee…) par-dessus un profil de membre ouvert. Meme resultat.

### Instrumentation runtime (avant correctif)

Sonde par reflexion sur les statiques, dans la scene `Environment2D5D_HiveMap_Test` :

```
A1 ouverture QG (clic batiment)  profileOpen=False  activeHiveMenu=Alliance  WORLD_BLOCKED=True   overlayDessine=True
A2 profil de membre ouvert       profileOpen=True   activeHiveMenu=Alliance  WORLD_BLOCKED=True   overlayDessine=True
A3 1er retour                    profileOpen=False  activeHiveMenu=Alliance  WORLD_BLOCKED=True   overlayDessine=True
A4 2e retour                     profileOpen=False  activeHiveMenu=Hive      WORLD_BLOCKED=False  overlayDessine=False
B  fermeture par le pont externe profileOpen=TRUE   activeHiveMenu=Hive      WORLD_BLOCKED=TRUE   overlayDessine=FALSE   <-- BUG
C  detour par la Recherche       profileOpen=TRUE   activeHiveMenu=Hive      WORLD_BLOCKED=TRUE   overlayDessine=FALSE   <-- BUG
```

Les lignes B et C sont l'etat sans issue : monde bloque, aucun ecran dessine.

### Correction apportee au diagnostic de la mission precedente

Le rapport `RAP-OPTIONNEL-COMMUNICATIONS_01` concluait que « fermer le profil ne
libere qu'UN des TROIS drapeaux ». **Cette conclusion etait mesuree a travers le
harnais de test, pas a travers le chemin reel, et elle est inexacte.**

- `OpenAllianceMemberProfileForProof` positionne `activeMainMenuId = "Alliance"` —
  valeur qu'**aucun chemin d'ouverture reel ne met jamais** (ligne A1 ci-dessus :
  `activeMainMenuId=''`). C'est un artefact du harnais.
- Le vrai bouton retour (ligne A3/A4) est, lui, **parfaitement equilibre** : le 1er
  retour referme le profil et revient au Centre d'Alliance — qui reste affiche, donc
  le monde reste bloque **a juste titre** (comportement voulu, protege par M043O-CL) ;
  le 2e retour quitte l'ecran et rend la camera.

Le vrai defaut n'etait donc pas « un drapeau sur trois mal libere a la fermeture du
profil », mais « les trois overlais internes survivent a la sortie du perimetre
Alliance par un chemin autre que le bouton retour ». Le symptome decrit par le CEO
correspond a B/C, pas a A.

## 3. Correctif

Point d'entree unique `ReleaseAllianceScreenOverlays()` (le corps de l'ancien
`CloseAllianceMemberProfile()`, qui devient un simple alias conservant sa semantique
« ne referme que le profil »). Toutes les sorties du perimetre Alliance y passent
desormais, au lieu de nettoyer des drapeaux chacune de leur cote :

- fermeture par le pont HiveMap ;
- changement de menu par `ActivateHiveMenu` alors qu'on etait dans Alliance ;
- branche Alliance de la fermeture prioritaire (Echap / retour Android) ;
- fermeture par bascule du bouton Alliance de la barre du bas.

**Filet non contournable, en plus du point d'entree unique.** Comme l'invariant est
demontrable (ces trois overlays ne peuvent etre visibles que si
`activeHiveMenu == Alliance`), les deux predicats de blocage
(`PremiumUiBlocksWorldInput` et `ShouldBlockUnderlyingHiveChromeInput`) ne lisent
plus les trois drapeaux bruts mais un helper `AllianceScreenOverlayOpen()` qui les
conditionne au mode Alliance. Les gardes demandent maintenant « un overlay Alliance
est-il reellement atteignable a l'ecran », et non plus « ce booleen est-il encore
vrai ». Il reste plusieurs affectations directes `activeHiveMenu = Hive` ailleurs
dans le fichier (barre du bas, bascule de surface, chat) qui contournent le point
d'entree unique : le filet les rend inoffensives — c'est prouve par le scenario E
ci-dessous.

## 4. Preuves

### Sonde runtime apres correctif (meme scene, meme methode)

```
B  fermeture par le pont externe  profileOpen=False  WORLD_BLOCKED=False  overlayDessine=False   (etait True/True)
C  detour par la Recherche        profileOpen=False  WORLD_BLOCKED=False  overlayDessine=False   (etait True/True)
A3 1er retour                     profileOpen=False  WORLD_BLOCKED=True   overlayDessine=True    (inchange, voulu)
A4 2e retour                      profileOpen=False  WORLD_BLOCKED=False  overlayDessine=False   (inchange)
E  affectation brute de contournement : profileOpen=True mais WORLD_BLOCKED=False  <-- le filet tient
```

### Tests EditMode

- **Neuf** : `Assets/BeeKingdom/Playground/Editor/AllianceScreenOverlayLeakTests.cs`
  — **5/5 verts**. Couvre les deux chemins de fuite, la non-regression de la pile de
  retour M043O-CL, une sequence entrelacee de plusieurs ecrans premium, et la
  liberation de `GUIUtility.hotControl` / `keyboardControl`.
- **Non-regression** : `HiveMapSceneReentryInputTests` (les tests du bug 6 de M056A)
  **6/6 verts**.
- `SandboxLivingHiveUiStabilizationTests` : **20/22, exactement comme avant mon
  correctif** — aucune regression introduite, les 2 echecs preexistants subsistent
  (voir section 5).
- Compilation Unity : `assets-refresh` vert, 0 erreur console.

## 5. Limites honnetes

1. **Aucune preuve en Play Mode.** J'ai tente d'entrer en Play Mode pour observer la
   camera reelle : l'entree a coupe le pont MCP et l'editeur est reste bloque sur la
   fenetre modale « Recovering Scene Backups » (processus vivant et repondant, mais
   injoignable ; poll de ~5 minutes sans evolution). Je n'ai **pas** force la
   fermeture d'Unity, pour ne pas risquer le travail non commite d'autres sessions
   presentes dans l'arbre. **Unity a probablement besoin d'une intervention manuelle
   de Jeff pour revenir a l'etat normal.** Mes deux fichiers sont sur disque, rien
   n'est perdu. La preuve reste donc statique + instrumentation runtime en mode
   edition — solide (le garde-camera est litteralement
   `if (PremiumWorldInputBlockedForProof) return;`, et ce booleen est prouve faux),
   mais ce n'est pas un pan/zoom observe a l'ecran. **A refaire valider par le CEO.**
2. **Les 2 tests preexistants de `SandboxLivingHiveUiStabilizationTests` echouent
   toujours** (`WorldInputIsBlockedWhileAnyFullScreenIsOpen`,
   `ClosingAllianceProfileReleasesCapturedGuiControls`). Ils encodent une attente
   fausse : « un seul retour depuis un profil de membre doit rendre le monde ». C'est
   contraire a la pile de retour voulue et protegee par M043O-CL (le profil se referme
   **dans** le Centre d'Alliance, qui reste ouvert). Le remede est d'une ligne dans
   chacun : appeler `ClosePremiumScreensForProof()` une seconde fois avant d'exiger
   l'etat de repos. **Je n'y ai pas touche : ce fichier fait partie des modifications
   non commitees d'une autre session, explicitement hors de mon perimetre.** A
   confier a son proprietaire.
3. **La suite EditMode complete (1578 tests) n'a pas ete rejouee** — meme risque de
   depassement du delai MCP que M056A et RAP-OPTIONNEL-COMMUNICATIONS_01, et
   l'editeur est de toute facon indisponible depuis la tentative de Play Mode. La
   non-regression globale n'est pas prouvee ; a rejouer en batchmode CLI.
4. `OpenAllianceMemberProfileForProof` conserve son `activeMainMenuId = "Alliance"`
   infidele au chemin reel. Je ne l'ai pas corrige : cela ne change aucun resultat de
   test et cela aurait perturbe un harnais dont je n'ai pas le droit de toucher les
   tests.

## 6. Amelioration Quality of Life du sprint (regle du 2026-08-04)

La position de defilement du profil de membre etait une statique jamais reinitialisee :
apres avoir fait defiler une fiche longue, **tous** les membres ouverts ensuite
s'affichaient a mi-page au lieu de leur portrait. Chaque profil s'ouvre desormais en
haut. Une ligne, dans la meme zone que le correctif.

## 7. Fichiers touches

- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (modifie, +50/-8)
- `Assets/BeeKingdom/Playground/Editor/AllianceScreenOverlayLeakTests.cs` (nouveau)
- `Docs/AI/Missions/M058-CL-Alliance-Profile-World-Input-Leak-Fix.md` (ce rapport)
- `Docs/Claude/Claude_Continuation.md` (nouvelle entree en tete)

Aucun autre fichier de l'arbre de travail n'a ete modifie. La carte terrain 50x50 et
son package Resources n'ont pas ete approches ; Alliance Research, Royal Seals, Royal
Palace, l'economie/combat et le PvP n'ont pas ete touches.

## 8. Prochain test utilisateur

Dans `Environment2D5D_HiveMap_Test`, en Play Mode :

1. Entrer dans la ruche, cliquer le batiment **Centre d'Alliance**.
2. Ouvrir un **profil de membre**.
3. Fermer l'ecran d'alliance **sans repasser par le bouton retour du profil** (par
   le pont HiveMap, ou en ouvrant un autre batiment comme la Recherche).
4. Verifier que le **pan et le zoom de la camera repondent immediatement**.
   Avant ce correctif, la camera restait morte jusqu'au redemarrage de la scene.
5. Verifier aussi la pile de retour normale : depuis un profil, un 1er retour revient
   au Centre d'Alliance (camera encore bloquee, c'est voulu), un 2e retour rend la
   camera.
