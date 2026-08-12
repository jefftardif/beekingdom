# QA-B - DEMO-096 World Map Runtime Continuity Step4B

## Etat du document

`PREPARATION_ACTIVE - AUCUN VERDICT EMIS`

Date d'ouverture QA-B : 2026-07-13/14 (America/Toronto)  
Role : protocole et validation independante en lecture seule  
Gate officiel : reserve a QA-A  
Produit Unity : aucune modification par QA-B

Le paquet DEMO-096 n'est pas complet au moment de cette ouverture. Le rapport, le manifeste et les captures sont absents. Seul un log Unity provisoire est present. Les marqueurs de verdict sont volontairement retenus jusqu'a livraison complete de Demo-A.

## Sources de cadrage lues

1. `Architect_WorldMapContinuousVisualGate.md`
   - SHA-256 : `F79478A1AA0203B1BE55D4147E870B38F6E31AC2BFA5239E518705036323A27E`
2. `Architect_Step4BProofHandoffDecision.md`
   - SHA-256 : `5DD19026088F3753AFF84541E75203271D6E01D02B041023703BD08B714B2733`
3. `BuilderA_WorldMapRuntimeTileSeamCorrectionStep4B_Report.md`
   - SHA-256 : `81AF6C8BFE010804A863EE14BE776675703C23211EFB53D1F0A8022D6E144651`
4. `QA_DEMO_095_WORLD_MAP_WAVE4_INTEGRATION_STEP4A_VALIDATION.md`
   - SHA-256 : `049B82DE93DE45D3EC9C595C0E33C0F477E3A062136E02B62A753F6C576FE686`

## Gate applicable

La carte finale doit etre percue comme une surface continue, jamais comme une juxtaposition d'images. Les controles numeriques et les self-checks sont necessaires mais ne remplacent pas l'inspection perceptuelle originale.

Le gate echoue si un observateur peut identifier une limite de fichier ou de chunk, meme si les dimensions, hashes et tests automatiques passent.

## Etat provisoire du paquet DEMO-096

Chemin attendu :

`C:\projets\beekingdom\prompt_demo\rapports\DEMO-096_WorldMapRuntimeContinuityStep4B\`

| Element obligatoire | Etat a l'ouverture |
|---|---:|
| `DEMO-096_Report.md` | ABSENT |
| Manifeste de preuves | ABSENT |
| Captures paysage 0.85x / 1.10x / 1.35x | ABSENTES |
| Captures portrait 0.85x / 1.10x / 1.35x | ABSENTES |
| Sequence pan C32 -> C35 -> C36 | ABSENTE |
| Log Unity final | PROVISOIRE |

Le log provisoire `DEMO096_UnityValidation.log`, SHA-256 `3C2F4FB6876637F3377CA84612BC58944D8D15374DB9600999F3AF27F80951BF`, identifie Unity `6000.2.10f1`, la methode officielle et le bon projet, mais se termine actuellement par `return code 1` avant preuve de validation. Cette sortie n'est pas transformee en verdict tant que Demo-A n'a pas livre son paquet final. Si elle demeure la preuve finale, le verdict devra etre `BLOCKED` selon la decision Architecte.

## Prechecks statiques deja effectues

### Atlas UI-B Wave1

- atlas source : 1536x1536 ;
- atlas Unity : meme taille et meme contenu ;
- SHA-256 source et copie runtime : `533DAD1BBAA138FA12880D44BD5E4DA22F41F564524C87AE512D1F030E4154BD` ;
- hash conforme au manifeste UI-B Wave1 ;
- manifeste source et copie runtime identiques, SHA-256 `917F7101F4FADFE31BE78D120AE50A50848DBD825FDD4AB5E56A8FAB7010F069`.

### Absence du master artistique 5x5

Le master `UIB_SectorWave2_5x5` existe hors projet dans la zone artistique, mais aucun fichier `5x5`, `2560` ou `SectorWave2` n'est present sous `Assets`. Il n'est donc pas integre au runtime Step4B a cet instant.

### Strategie runtime

La lecture statique confirme :

- atlas charge par le manifest ;
- `TextureWrapMode.Repeat`, filtrage trilineaire et anisotropie 2 ;
- chemin atlas prioritaire vers `DrawContinuousAtlasSurface()` ;
- un seul `GUI.DrawTextureWithTexCoords` plein ecran pour l'art primaire ;
- retour immediat avant la boucle de rendu chunk par chunk quand l'atlas est disponible ;
- voile d'ambiance plein ecran unique ;
- grille debug conditionnelle seulement ;
- overlays vols, ressources, ruches et HUD dessines apres le fond ;
- rayon logique actif 2, soit 5x5 / 25 chunks ;
- vols en coordonnees monde et aucune route terrestre revendiquee.

Ces constats statiques ne ferment pas le gate visuel. Le mode `Repeat` rend notamment obligatoire une recherche attentive de repetition artistique pendant le pan C32 -> C36.

## Baseline Step4A

Les captures originales Step4A a `1.10x` montrent des lignes sombres fines mais nettes :

- paysage : plusieurs limites verticales et horizontales traversent le fond peint ;
- portrait : une limite verticale centrale et des limites horizontales restent perceptibles ;
- les lignes sont alignees sur les rectangles de chunks, pas sur des accidents naturels du relief.

L'amelioration Step4B ne sera reconnue que si ces lignes disparaissent reellement dans les vues correspondantes. Un simple changement de cadrage, d'overlay, de luminosite ou de zoom ne constitue pas une correction.

References baseline :

- `DEMO095_03_TabletLandscape_1920x1080_GameViewEditor.png`
- `DEMO095_04_PhonePortrait_GameViewEditor.png`

## Paquet minimal requis de Demo-A

### Documents

1. Rapport DEMO-096 final.
2. Manifeste listant chaque preuve, taille, SHA-256, zoom, format et centre/chunk.
3. Log Unity final complet avec code de sortie explicite.

### Captures statiques

Six captures originales minimum :

| Format | Zooms obligatoires |
|---|---|
| Paysage 1920x1080 | 0.85x, 1.10x, 1.35x |
| Portrait 720x1280 | 0.85x, 1.10x, 1.35x |

Chaque capture doit montrer la Game View finale sans grille debug et permettre d'inspecter toute la surface visible. Les dimensions du viewport doivent etre prouvees, meme si la capture inclut le chrome Editor.

### Pan dynamique

Une sequence ordonnee et horodatee doit couvrir :

1. centre/chunk `C32_32` ;
2. passage vers `C35_32` ;
3. fin sur `C36_32`.

Une video est preferee pour verifier les flashs. A defaut, une rafale suffisamment dense doit montrer les etats intermediaires, pas seulement trois poses finales.

## Protocole de validation apres livraison

### 1. Integrite et compilation

- recalculer chaque SHA-256 du manifeste ;
- verifier dimensions et format de chaque PNG/video ;
- confirmer Unity `6000.2.10f1` et la methode officielle ;
- rechercher `error CS`, exception, crash, marqueur de validation absent et code non nul ;
- exiger code Unity `0` et marqueur de validation termine.

Regle : toute erreur C#, exception bloquante ou sortie Unity non nulle produit `BLOCKED`.

### 2. Inspection perceptuelle par zoom

Pour chacune des six vues, inspecter a resolution originale puis a agrandissement controle :

- lignes verticales ou horizontales ;
- grille, damier, anneau ou carre central ;
- bande sombre ou claire ;
- trou ou pixel de fond ;
- chevauchement ou double assombrissement ;
- rupture de riviere, foret, relief, clairiere ou masse de couleur ;
- repetition evidente du meme atlas/chunk ;
- art masque par un overlay opaque ou une grille debug.

Regle : un seul defaut perceptible attribuable a une limite de tuile/chunk produit `BLOCKED`.

### 3. Controle quantitatif auxiliaire

- profiler l'energie des gradients par lignes et colonnes ;
- rechercher des pics rectilignes traversant une grande part du viewport ;
- comparer ces profils aux captures Step4A ;
- rechercher des regions identiques ou quasi identiques a periodicite de trois chunks ;
- confirmer qu'aucune bande noire/transparente n'apparait aux bords.

Ces mesures orientent l'inspection mais ne peuvent pas annuler un defaut vu a l'oeil.

### 4. Pan C32 -> C35 -> C36

- verifier le mouvement continu des landmarks naturels ;
- confirmer aucune frame noire, flash, bande, trou, overlap ou saut ;
- confirmer absence de repetition visible quand le deplacement depasse la largeur logique de l'atlas 3x3 ;
- verifier que les vols restent ancres au monde ;
- verifier que HUD, journal et minimap restent fixes ;
- verifier que la selection et les coordonnees changent de facon coherente ;
- confirmer 25 chunks actifs aux trois positions.

### 5. Overlays et interaction

- ruches et ressources restent des overlays nets et selectionnables ;
- selection joueur/allie/neutre/hostile/ressource si presente dans la preuve ;
- HUD, panneau d'action, journal, coordonnees et minimap restent lisibles ;
- aucune route peinte n'est interpretee comme chemin de deplacement ;
- trajectoires strictement aeriennes.

### 6. Comparaison Step4A

La preuve d'amelioration doit utiliser les vues `1.10x` paysage et portrait comme comparaison principale :

- meme type de surface et meme zone centrale autant que possible ;
- coutures Step4A identifiables dans la baseline ;
- absence de ces coutures en Step4B ;
- aucune nouvelle repetition ou bande introduite par le single-draw.

### 7. Claims et perimetre

Le rapport final doit confirmer :

- local/demo uniquement ;
- serveur live faux ;
- placements, collecte et economie non officiels ;
- pilote artistique 3x3 seulement ;
- monde logique 64x64 et fenetre active 5x5 ne signifient pas master artistique 5x5 ;
- aucun fichier produit modifie par QA-B.

## Logique de verdict a appliquer apres livraison

### PASS

Toutes les preuves sont presentes et hash-valides, Unity sort `0`, les six vues et le pan ne revelent aucune limite/repetition/flash, et les overlays/non-claims sont conformes.

### PASS_WITH_RESERVES

Reserve permise uniquement pour une limite non bloquante hors gate de continuite, clairement prouvee et sans ambiguite sur les coutures, le pan, la compilation ou les claims.

### BLOCKED

Au moins un des cas suivants :

- rapport, manifeste, vue obligatoire ou pan manquant ;
- hash/dimension incoherent ;
- erreur C# ou sortie Unity non nulle ;
- limite, grille, bande, trou, overlap, flash ou repetition visible ;
- master 5x5 integre ;
- atlas/hash UI-B incorrect ;
- overlays fusionnes au fond, HUD/pan/selection casses ou route terrestre ;
- claim live/officiel trompeur.

## Marqueurs

Les marqueurs finaux ne sont pas emis dans cette version de preparation. Ils seront ajoutes uniquement apres reception et inspection de la livraison DEMO-096 complete.
