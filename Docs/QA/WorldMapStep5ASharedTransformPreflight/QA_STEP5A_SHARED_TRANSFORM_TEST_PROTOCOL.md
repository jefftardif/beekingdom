# QA Step5A - Shared Terrain/Entities Transform Test Protocol

Date de preparation : 2026-07-14  
Role : QA-A  
Statut : protocole pret, validation produit en HOLD  
Execution Unity pendant le preflight : interdite

## 1. Autorite du gate

Le constat direct du proprietaire est autoritatif pour le defaut Step4D : le terrain reste statique pendant que les entites reagissent au pan/zoom. Le verdict positif Step4D est revoque pour la continuite runtime.

Les anciennes captures Step4D peuvent uniquement servir d'historique. Elles ne peuvent pas etre reutilisees comme preuve de transformation partagee, meme si leurs hashes, UV ou etats finaux sont coherents.

Step5A ne pourra etre valide qu'apres handoff Builder-A, dans la scene canonique et en Play Mode normal. Une scene temporaire, un outil de capture qui construit son propre rendu, des etats forces sans interaction ou une serie de PNG isoles ne suffisent pas.

La reference UI-A `Docs/UIA/WorldMapStep5ALandmarkMotionReference/` est normative pour les mesures Step5A. Ses 13 landmarks, trois paires de pan et trois pivots de zoom doivent apparaitre dans le paquet de preuve. Les anneaux, numeros, croix et planches annotees sont des outils QA uniquement et ne doivent jamais etre rendus par le runtime joueur.

## 2. Portee

Le gate couvre exclusivement :

- le meme calcul monde-vers-ecran pour terrain, ruches, ressources, selections et vols ;
- un HUD, des panneaux et une minimap fixes a l'ecran ;
- l'integration runtime du pilote artistique Wave3 borne a 25 tuiles ;
- le pan, le zoom, la selection et les vols aeriens en paysage et portrait ;
- l'absence de repetition, grille, couture, trou ou claim de monde 64x64 deja produit.

Hors scope : serveur live, persistence officielle, economie officielle, monde immense termine, Android physique et modification de la ruche interieure.

## 3. Preconditions avant execution QA

QA ne lance pas Unity tant que les elements suivants ne sont pas remis :

1. rapport Builder-A Step5A avec inventaire exact des fichiers modifies ;
2. confirmation que le test utilise `Assets/Scenes/WorldMapMmoFullscreenFoundation.unity` en Play Mode normal ;
3. preuve que le chemin utilisateur ouvre cette scene, sans scene temporaire de capture ;
4. inventaire Wave3 de 25 tuiles uniques et hashes conformes au handoff ;
5. rapport Demo-A avec captures brutes et, si possible, video ou strip temporel ;
6. manifeste de preuve derive du template QA fourni avec ce protocole ;
7. les quatre sources UI-A obligatoires, avec hashes conformes et matrice `13 landmarks / 3 pans / 3 pivots` ;
8. labels de portee `local/demo`, sans claim serveur/live ou monde 64x64 artistique complet.

Si un element 1 a 7 manque, le handoff est incomplet et Unity ne doit pas etre utilise pour rendre un verdict final.

## 4. Configurations obligatoires

Deux executions independantes sont requises :

| Profil | Resolution de preuve | Zooms statiques | Interaction centrale |
|---|---:|---|---|
| Tablette/paysage | `1920x1080` | `0.85`, `1.10`, `1.35` | pan a `1.10`, zoom `1.10 -> 1.35 -> 1.10` |
| Telephone/portrait | `720x1280` | `0.85`, `1.10`, `1.35` | pan a `1.10`, zoom `1.10 -> 1.35 -> 1.10` |

Ces zooms restent la regression responsive. Les trois pivots canoniques UI-A sont en plus executes a `0.75`, `1.00` et `1.50`; les trois pans utilisent leurs zooms et deltas UI-A exacts.

Un appareil physique n'est pas requis pour ce gate local, mais une preuve appareil ne doit jamais etre revendiquee a partir d'une Game View Editor.

## 5. Reperes a suivre

### Reference UI-A obligatoire

Sources de verite :

- `Docs/UIA/WorldMapStep5ALandmarkMotionReference/UIA_WorldMapStep5A_LandmarkMotionReference_Report.md` ;
- `Docs/UIA/WorldMapStep5ALandmarkMotionReference/UIA_WorldMapStep5A_MasterLandmarks_Annotated.png` ;
- `Docs/UIA/WorldMapStep5ALandmarkMotionReference/UIA_WorldMapStep5A_PanZoomReference.png` ;
- `Docs/UIA/WorldMapStep5ALandmarkMotionReference/UIA_WorldMapStep5A_Landmarks.json`.

Les 13 reperes naturels doivent etre identifies dans le master et retrouves pendant la traverssee runtime :

| ID | Repere | Master px | Tuile |
|---|---|---:|---|
| `L01` | sommet neige nord-ouest | `(226,96)` | `R0C0` |
| `L02` | fourche du lac alpin | `(671,175)` | `R0C1` |
| `L03` | couronne rocheuse nord | `(1462,74)` | `R0C2` |
| `L04` | double sommet nord-est | `(2348,247)` | `R0C4` |
| `L05` | cascade en escalier | `(807,664)` | `R1C1` |
| `L06` | clairiere florale centrale | `(1294,682)` | `R1C2` |
| `L07` | selle rocheuse est | `(1891,577)` | `R1C3` |
| `L08` | bassin et ile de riviere | `(865,1192)` | `R2C1` |
| `L09` | pic isole de prairie | `(1424,950)` | `R1C2` |
| `L10` | double bassin d'automne | `(182,1816)` | `R3C0` |
| `L11` | lac du croissant cristallin | `(2248,1304)` | `R2C4` |
| `L12` | contact crete-marais | `(1884,1801)` | `R3C3` |
| `L13` | delta marecageux sud-est | `(1696,2228)` | `R4C3` |

Scenarios de pan canoniques :

| ID | Landmarks | Zoom | Delta camera monde | Delta ecran attendu terrain et entites |
|---|---|---:|---:|---:|
| `PH01` | `L05 -> L06` | `1.00` | `(+300,0)` | `(-300,0) px` |
| `PH02` | `L10 -> L12` | `0.60` | `(+220,0)` | `(-132,0) px` |
| `PV01` | `L05 -> L08` | `1.00` | `(0,+180)` | `(0,-180) px` |

Pivots de zoom canoniques :

| ID | Landmark | Pivot master | Facteurs |
|---|---|---:|---|
| `Z01` | `L05` | `(807,664)` | `0.75 / 1.00 / 1.50` |
| `Z02` | `L09` | `(1424,950)` | `0.75 / 1.00 / 1.50` |
| `Z03` | `L11` | `(2248,1304)` | `0.75 / 1.00 / 1.50` |

Les images annotees servent uniquement a localiser et mesurer. Les captures runtime autoritatives restent propres : aucun anneau UI-A, numero `Lxx`, croix, grille, coordonnee ou marqueur invente ne doit etre visible dans la vue joueur. Les annotations de mesure sont appliquees seulement sur une copie derivee QA, jamais dans Unity.

### Reperes runtime complementaires

Avant le geste, identifier dans la meme image :

- `T0` : un detail de terrain unique, net et non atmospherique ;
- `H0` : une ruche et son halo/selection ;
- `R0` : une ressource selectionnable ;
- `F0-A/F0-B` : source et destination d'un vol visible ;
- `HUD-L/HUD-R` : deux coins stables du HUD ou des panneaux fixes.

Les details de terrain doivent etre choisis parmi les landmarks UI-A et ne doivent pas etre un nuage, une particule, une ombre animee ou une zone uniforme. Les coordonnees ecran de ces reperes sont consignees avant, au milieu et apres le geste.

Tolerances normatives UI-A pour les six scenarios canoniques :

```text
Erreur delta/pivot capture deterministe non re-echantillonnee <= 2 px
Erreur delta/pivot capture physique compressee <= 3 px
Translation HUD <= 1 px
Ratio de taille HUD compris entre 0.995 et 1.005
```

La tolerance historique `max(5 px, 0.75 % du cote court)` reste seulement un indicateur exploratoire pour des entites secondaires. Elle ne peut pas faire passer `PH01`, `PH02`, `PV01`, `Z01`, `Z02` ou `Z03`.

## 6. Gate pan terrain/entites

### Action

Executer `PH01`, `PH02` et `PV01` avec leurs landmarks, zooms et deltas UI-A. En plus, depuis zoom `1.10`, effectuer le drag utilisateur du smoke test couvrant au moins `25 %` de la dimension utile. Conserver les memes reperes visibles avant, pendant et apres.

### PASS obligatoire

1. Le detail terrain `T0` se deplace d'au moins :

```text
Dmin = max(96 pixels, 12 % du plus petit cote de la resolution)
```

2. Le deplacement est evident a l'oeil nu. Une variation UV minuscule, un frisson de sampling ou un changement de texture sans translation perceptible echoue.
3. Les deltas terrain de `PH01`, `PH02` et `PV01` correspondent aux deltas attendus a `2 px` pres sur capture deterministe.
4. `H0`, `R0`, les deux extremites de `F0` et les landmarks suivis ont le meme delta ecran a `2 px` pres.
5. La position relative entite-terrain reste stable a `2 px` pres.
6. Le frame milieu montre une trajectoire continue, sans saut ni remplacement instantane du decor.
7. HUD, panneaux, navigation et minimap restent a `1 px` et dans le ratio `0.995..1.005`.

### BLOCKED immediat

- terrain sous `Dmin` pendant que les entites depassent `Dmin` ;
- signes ou directions de deplacement differents ;
- un des trois scenarios UI-A absent ou erreur delta entite-terrain superieure a `2 px` ;
- decor fixe et seules les entites mobiles ;
- HUD ou panneaux qui suivent la camera ;
- preuve uniquement statique sans observation interactive directe ni preuve temporelle continue.

## 7. Gate zoom et pivot commun

### Action

Executer `Z01/L05`, `Z02/L09` et `Z03/L11` a `0.75`, `1.00` et `1.50`, autour du pivot UI-A exact. Le smoke responsive `1.10 -> 1.35 -> 1.10` reste un controle complementaire, sans remplacer les trois pivots.

### PASS obligatoire

1. Les trois facteurs UI-A sont captures pour chacun des trois pivots.
2. Terrain, ruches, ressources, halos et vols changent d'echelle autour du meme pivot.
3. Le landmark reste a `2 px` maximum du pivot et les centres des entites suivent le meme pivot.
4. Le detail secondaire respecte le facteur attendu et terrain/entites donnent le meme ratio dans la tolerance UI-A.
5. Les extremites de l'arc restent sur leurs source/destination.
6. Le retour au facteur precedent restaure l'alignement a `2 px` pres.
7. Le HUD conserve position et taille a `1 px` et dans le ratio `0.995..1.005`.

### BLOCKED immediat

- zoom visible des entites sans zoom evident du terrain ;
- terrain et entites utilisent des pivots differents ;
- un pivot UI-A absent, ou halo, hit zone, ressource ou vol se decale de plus de `2 px` ;
- zoom de la carte declenche par une interaction sur un bouton ou panneau fixe ;
- HUD qui zoome, se translate ou change de taille au-dela de la tolerance.

## 8. Gate Wave3 5x5

### Integrite mecanique

Tous les points sont obligatoires :

- exactement 25 tuiles runtime uniques `R0C0_g2` a `R4C4_g2` ;
- dimensions `516x516 RGB`, interieur monde `512x512` ;
- 25 hashes conformes au manifeste de handoff ;
- mapping borne aux chunks `(30,30)` a `(34,34)`, avec `R2C2 -> (32,32)` ;
- UV interieures `2/516 .. 514/516` ;
- `Clamp`, bilinear, NPOT conserve, mipmaps off ;
- aucun modulo, `Repeat`, miroir, rotation, transposee ou etirement du 516 complet ;
- aucune texture manquante, dupliquee ou supplementaire utilisee pour simuler une extension.

### Integrite visuelle runtime

- les 40 frontieres internes doivent etre sans ligne, grille, overlap, trou ou changement brutal ;
- aucun motif ou biome ne doit se repeter a une position equivalente ;
- les gouttieres ne doivent jamais etre visibles comme contenu ;
- les preuves produit sont capturees avec grille/debug chunks desactives ;
- inspection requise aux zooms `0.85`, `1.10` et `1.35`, dans les deux orientations.

Une seule couture, grille ou repetition visible dans le rendu produit impose `BLOCKED`.

## 9. Gate camera bornee

Le pilote artistique est une region 5x5, pas un art 64x64.

### PASS obligatoire

1. La camera est contrainte pour que le viewport ne sorte pas de la region artistique utile.
2. Une tentative de pan au-dela de chacun des quatre bords arrete ou contraint la camera proprement.
3. Aucun bord ne wrappe vers le bord oppose.
4. Aucun motif du 5x5 n'est repete pour remplir le monde logique 64x64.
5. Le manifeste declare explicitement `art_region=5x5`, `modulo_used=false`, `world_64x64_art_complete=false`.

### BLOCKED immediat

- camera montrant un trou, une bande noire ou un fallback repete hors du 5x5 ;
- retour du bord gauche a droite, du haut en bas ou toute repetition modulo ;
- utilisation des 25 tuiles comme texture repetee pour revendiquer 64x64 ;
- texte ou rapport laissant croire que l'art du monde immense est termine.

## 10. Gate paysage et portrait

Dans chaque orientation :

- la carte reste la surface principale exploitable ;
- HUD, panneaux, navigation, journal et minimap ne se chevauchent pas de facon incoherente ;
- ressources, ruches et vols restent visibles et lisibles ;
- une ruche puis une ressource peuvent etre selectionnees avant et apres pan/zoom ;
- halo, hit zone et panneau concernent toujours l'entite touchee ;
- aucun texte critique n'est coupe ;
- aucun format ne reutilise le layout de l'autre de maniere etiree ou cassee.

Un overlap bloquant, une zone tactile inaccessible, une carte coupee ou une selection desancree impose `BLOCKED`.

## 11. Gate vols air-only

Au moins un vol actif doit rester visible pendant pan et zoom :

- trajectoire directe, courbe ou en arc dans l'espace ;
- points A/B ancres aux memes positions monde que le terrain ;
- aucune dependance a une route, piste ou ligne peinte ;
- aucune rupture, teleportation ou disparition au changement de tuile ;
- labels `local/demo` et non officiels preserves.

Un vol qui suit une route terrestre, se detache de ses points ou saute pendant la transformation impose `BLOCKED`.

## 12. Test utilisateur reproductible en moins de 30 secondes

Ce smoke test est execute une fois par orientation, par un utilisateur qui ne connait pas les outils de capture.

| Temps | Action | Observation attendue |
|---:|---|---|
| `0-3 s` | Identifier un detail terrain, une ruche selectionnee et le HUD | trois reperes nets et visibles |
| `3-8 s` | Drag large de la carte | terrain et entites se deplacent ensemble; HUD fixe |
| `8-11 s` | Pause courte | aucun saut, grille ou couture |
| `11-17 s` | Zoom `1.10 -> 1.35` autour de la ruche | meme pivot terrain/entites; HUD fixe |
| `17-21 s` | Retour `1.35 -> 1.10` | alignement initial restaure |
| `21-25 s` | Selectionner une ressource puis la ruche | hit zones, halos et panneau alignes |
| `25-29 s` | Observer un vol actif | arc aerien ancre, aucune route au sol |

Le test echoue des que l'utilisateur peut garder le regard sur un detail terrain fixe tandis que les entites bougent. Il ne depend d'aucun label debug, hotkey Editor ou lecture de manifeste.

## 13. Paquet de preuve attendu apres handoff

Pour chaque orientation :

1. `T0_before.png`, `T1_mid.png`, `T2_after.png` provenant du meme geste ;
2. trois captures zoom : `Z0_110.png`, `Z1_135.png`, `Z2_return110.png` ;
3. video brute courte ou strip temporel, si l'outil le permet ;
4. a defaut de video, observation interactive directe QA obligatoire, jamais remplacee par les PNG ;
5. manifeste JSON complete avec mesures de reperes, camera, zoom, HUD et tuiles ;
6. SHA-256 de chaque artefact ;
7. aucune retouche, interpolation ou recomposition artistique.

Le bundle doit aussi fournir :

- la matrice UI-A complete `13 landmarks / PH01-PH02-PV01 / Z01-Z02-Z03`, avec mesures et hashes des quatre sources ;
- inventaire et hashes 25/25 ;
- preuve des quatre bornes camera ;
- planche de controle des 40 frontieres internes ;
- confirmation du chemin normal Splash/Development -> WorldMap ;
- non-claims explicites.

## 14. Regles de verdict

`PASS` exige tous les gates obligatoires en paysage et portrait, les 13 landmarks identifies, les trois paires de pan passees et les trois pivots de zoom passes.

`PASS_WITH_RESERVES` n'est autorise que pour une limite externe au comportement local, par exemple l'absence de preuve appareil physique explicitement hors scope. Il est interdit pour la transformation partagee, le HUD fixe, les 25 tuiles, les coutures, la repetition, les bornes camera, la selection ou les vols.

`BLOCKED` est obligatoire si un seul blocker explicite des sections 6 a 11 est observe ou si la preuve temporelle/directe manque.

Toute annotation UI-A visible dans le runtime joueur, toute source UI-A absente ou tout scenario canonique non mesure impose egalement `BLOCKED`.

## 15. Etat actuel

Ce document prepare le protocole. Il ne valide pas le produit et ne remplace pas le handoff Builder-A.

```text
STEP4D_SHARED_TRANSFORM_CONCLUSION = REVOKED
CURRENT_PRODUCT_GATE = BLOCKED_PENDING_BUILDER_A
UIA_STEP5A_LANDMARK_REFERENCE_REQUIRED = YES
UIA_ANNOTATIONS_ALLOWED_IN_RUNTIME = NO
UNITY_EXECUTED_BY_QA_PREFLIGHT = NO
STEP5A_PRODUCT_VALIDATED = NO
```
