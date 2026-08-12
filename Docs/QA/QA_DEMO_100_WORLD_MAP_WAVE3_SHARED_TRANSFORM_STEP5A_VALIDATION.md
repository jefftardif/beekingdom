# QA-A - DEMO-100 WorldMap Wave3 Shared Transform Step5A

Date : 2026-07-14  
Mode : contre-validation locale, independante et en lecture seule hors fichier QA  
Perimetre : paquet DEMO-100 reconstruit, Run10, Zoom12, medias finaux et integrite produit

## Verdict

`PASS`

Le bug utilisateur de terrain statique est ferme. Le terrain, les ruches, les ressources, les selections et les vols partagent le pan et le zoom. Le HUD, les panneaux, le journal et la minimap restent fixes.

La couture noire historique de `L14_zoom_in_mid.png` est egalement fermee. Les douze zooms actifs proviennent de la source Run10 `8281...E63E`, sont uniques et ne montrent aucune couture ou grille a taille native. L'ancienne serie `DB5B...BF1` reste uniquement dans `historical_rejected` et ne participe a aucun gate d'acceptation.

Ce PASS valide exclusivement la preuve Unity locale/demo Step5A. Il ne valide ni appareil physique, ni APK, ni serveur live, ni economie ou collecte officielle.

## Sources controlees

- Paquet autoritatif : `C:/projets/beekingdomgame-master/DEMO_Evidence_Staging/DEMO-100_WorldMapWave3SharedTransformStep5A/official_20260714_144919Z/`.
- Miroir Demo-A : `C:/projets/beekingdom/prompt_demo/rapports/DEMO-100_WorldMapWave3SharedTransformStep5A/`.
- `DEMO-100_EvidenceManifest.json`.
- `manifests/DEMO-100_FinalArtifactIndex.json`.
- `final/DEMO-100_SHA256SUMS.txt`.
- `manifests/DEMO-100_FinalMediaInventory.json`.
- `manifests/DEMO-100_FinalMeasurements.json`.
- `final/DEMO-100_Report.md`, `DEMO-100_Final_State.json` et `DEMO-100_QA-A_Handoff.md`.
- Douze PNG Zoom12 actifs, quatre strips zoom, trois PNG Run10, strip Run10 et contact sheet finale.
- Rapport Builder-C SHA-256 `17BFF8CB69F4CBE86B607335D601CFED4ED225F2DD7FF640834158E45B2AC149`.
- Validation Builder-C SHA-256 `C45891DB36A3E5BDC260CB0371587F3DACEC13FC29B5D6C366739BCEAA60BE6D`.

## Provenance et comptage final

Le paquet s'est stabilise apres ajout du recu de publication Demo-A. Le comptage final est coherent :

| Controle | Resultat QA |
|---|---|
| Fichiers physiques autoritatifs | `214` |
| Entrees d'index | `212` |
| Exclusions explicites de l'index | index lui-meme et fichier SHA |
| Lignes `SHA256SUMS` | `213` |
| Auto-exclusion SHA | fichier `SHA256SUMS` lui-meme |
| Fichiers manquants | `0` |
| Hashes divergents | `0` |
| Tailles divergentes | `0` |
| Entrees non listees ou fantomes | `0` |
| Recu de publication miroir | `43/43`, hashes et tailles conformes |

La formulation `index 212 / SHA 213` est donc exacte sur l'etat publie. Les fichiers coeur du miroir, notamment rapport, handoff, etat et liste SHA, sont byte-identiques aux fichiers autoritatifs.

## Replays independants

### Gate strict positif

- Code retour : `0`.
- Validateur : `DEMO100_RUN10_FRESH_ZOOM12_STRICT`.
- Controles : `261/261`.
- Echecs : `0`.
- Medias reels decodes : `45`.
- Verdict : `PASS`.

### Regression adversariale

Le faux paquet reutilisant un fichier texte, des metriques nulles et des rapports absents est correctement refuse :

- code interne : `2` ;
- `pass:false` ;
- medias decodes : `0` ;
- faux medias rejetes : oui ;
- metriques nulles rejetees : oui ;
- rapports Builder absents rejetes : oui ;
- wrapper de regression : `PASS`, code externe `0`.

Le recu canonique compte `74` echecs. Le replay QA isole en chemin temporaire en compte `75`; l'unique controle supplementaire est `run_root_persistent`, attendu puisque la copie QA est volontairement sous `Temp`. Ce delta environnemental ne contribue pas seul au PASS du negatif et ne constitue pas une reserve produit.

## Inspection visuelle a 100 pour cent

### Pan Run10

| Media | Resultat QA |
|---|---|
| `T0_SAFE_C32_CENTER.png` | PASS |
| `T1_SAFE_MID.png` | PASS |
| `T2_SAFE_RIGHT_INSET.png` | PASS |
| Strip avant/milieu/apres | PASS |
| Contact sheet finale | PASS |

Constats :

- pan terrain evident sur les reliefs, cours d'eau et vegetations ;
- centres monde `16640 -> 17152 -> 17476.36` ;
- deltas terrain/entites `-563.2 px`, puis `-356.8 px` ;
- erreur terrain/entites `0 px` ;
- HUD et panneaux ancres au meme rectangle ecran ;
- `11` frontieres projetees, `0` couture bloquante et `0` bande sombre ;
- aucune grille, repetition ou sortie noire de la region pilote ;
- constat proprietaire favorable recoupe par les medias et les mesures.

### Zoom12 paysage

Les six images `L13..L18` et les deux strips paysage ont ete inspectes a leur resolution native `1920x1080`.

- Zoom in : `1.00 -> 1.10 -> 1.21`.
- Zoom out : `1.09 -> 0.98 -> 0.81`.
- Terrain et entites changent d'echelle ensemble.
- HUD, panneau d'action, journal, legende et minimap restent fixes.
- Aucune couture noire, bande sombre ou grille visible.

Fermeture precise de l'ancien blocker :

- ancien L14 rejete : SHA-256 `ABC16972D89DCF1981C986DF24D9B40A7BF786FFEBB45F5EE0ABAFA42CF7CC1B` ;
- nouveau L14 accepte : SHA-256 `154007BC354032A63EED94B9F891462C56A6D6539FD456FF4B937FD3B9F17842` ;
- frontiere `V@16896` : position predite `1241.6`, ligne mesuree `1240` ;
- luminance ligne `105.4259`, voisinage `106.6311`, ratio `0.988697` ;
- fraction sombre coherente `0` ;
- verdict frontiere : PASS.

### Zoom12 portrait

Les six images `P13..P18` et les deux strips portrait ont ete inspectes a leur resolution native `720x1280`.

- Zoom in : `1.00 -> 1.10 -> 1.21`.
- Zoom out : `1.09 -> 0.98 -> 0.81`.
- Terrain, ruches, ressources, selection et arc de vol suivent la meme echelle.
- HUD, journal, minimap et panneau d'action restent fixes et lisibles.
- Aucune couture, bande sombre ou grille visible.

### Gate de frontieres Zoom12

- `12/12` PNG decodables et uniques.
- `62` frontieres projetees controlees.
- `0` couture bloquante.
- `0` bande sombre inattendue.
- Erreur relative maximale du transform partage : `0.00000335`.
- Derive HUD maximale : `0 px`.
- Fixture sans delta zoom correctement rejetee avec `NO_ZOOM_DELTA`.

## Monde, bornes et vols

| Critere | Resultat QA |
|---|---|
| Source acceptee unique | `8281EE0294AF44F24F8EBDB454A535C79F33DD21F4706DCE45CEA5FE04A5E63E` |
| Ancienne source DB5B acceptee | NO |
| Fenetre art active | `5x5` |
| Tuiles actives | `25`, hashes uniques |
| Region monde Wave3 | `15360,15360,2560,2560` |
| Positions Run10 dans la zone sure | PASS |
| Camera hors art ou frame noire | NO |
| Gutters / voisins / Clamp | `40/40`, `80/80`, `20/20` |
| Repeat ou modulo terrain | NO |
| Vols | arcs aeriens en coordonnees monde |
| Graphe ou route terrestre | NO |

## Integrite produit

- Inventaire avant fusion Zoom12 : `58` fichiers.
- Inventaire apres fusion Zoom12 : `58` fichiers.
- Fichier ajoute, retire ou modifie entre les deux : `0`.
- Recalcul contre l'etat produit courant : `0` fichier manquant, `0` hash divergent, `0` taille divergente.
- Source runtime actuelle : meme hash `8281...E63E`.
- Aucun changement Unity ou produit effectue par QA.

## Non-claims et reserves de portee

- Preuve `local_demo` uniquement.
- `PHYSICAL_DEVICE_PROOF = PENDING`.
- Aucun APK Android produit, installe ou valide dans ce gate.
- Aucun serveur officiel/live ou endpoint officiel valide.
- Aucune collecte, economie, persistance ou population MMO officielle valide.
- Aucun monde immense termine revendique; la camera reste bornee au pilote artistique `5x5`.

Ces limites ne bloquent pas le PASS Step5A. Elles restent des gates distincts pour les vagues Android/device et serveur futures.

## Decision finale

Le probleme signale par le proprietaire est visuellement et metrologiquement ferme. Le terrain n'est plus statique pendant le pan/zoom, les entites partagent son transform, et la couture L14 historique n'apparait pas dans les douze zooms frais. Aucun blocker Step5A ne reste ouvert.

`QA_DEMO_100_WORLD_MAP_WAVE3_SHARED_TRANSFORM_STEP5A = PASS`

`STATIC_TERRAIN_PAN_REGRESSION = CLOSED`

`RUN10_PAN_SEAMS = CLOSED`

`FRESH_ZOOM12_PROOF = PASS`

`ACCEPTED_LANDSCAPE_ZOOM_SEAM = CLOSED`

`VISIBLE_TILE_SEAMS = NO`

`GRID_PATTERN_VISIBLE = NO`

`PHYSICAL_DEVICE_PROOF = PENDING`

`ANDROID_DEVELOPMENT_APK_PRODUCED_OR_VALIDATED = NO`

`READY_FOR_ARCHITECT_ANDROID_DEVELOPMENT_APK_ASSIGNMENT = YES`

`LOCAL_DEMO_ONLY_NO_LIVE_ACTIVATION = YES`
