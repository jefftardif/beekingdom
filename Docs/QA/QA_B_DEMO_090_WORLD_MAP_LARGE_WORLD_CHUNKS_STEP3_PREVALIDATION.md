# QA-B - DEMO-090 World Map Large World Chunks Step 3 - Prevalidation

Date: 2026-07-13

## Statut et mandat

Prevalidation independante QA-B preparee pour QA-A. Ce document ne ferme aucun gate officiel et ne remplace pas le verdict QA-A.

- Aucun fichier producteur n'a ete corrige ou modifie.
- Aucun claim serveur live, placement officiel, collecte officielle ou economie persistante n'est accorde.
- Perimetre valide ici: scene/lab Unity local/demo Step 3 et preuves associees.
- Destination demandee: `C:\projets\beekingdom\QA\QA_B_DEMO_090_WORLD_MAP_LARGE_WORLD_CHUNKS_STEP3_PREVALIDATION.md`.
- La session QA-B actuelle ne peut ecrire que dans le workspace; cette copie est donc preparee pour relais vers la destination demandee.

## Sources inspectees

- `DEMO-090_Report.md`.
- Les 15 PNG `DEMO090_01` a `DEMO090_15` en resolution originale.
- `DEMO-090_CaptureManifest.md`.
- `DEMO-090_DeterminismProof.json`.
- `DEMO-090_UnityCapture.log`.
- `DEMO-090_UnityCompile.log`.
- `BuilderB_WorldMapLargeWorldChunksStep3_Report.md`.
- `Assets/BeeKingdom/Playground/WorldMapLargeWorldChunksStep3Manifest.json`.
- Code runtime `WorldMapMmoFullscreenFoundationBootstrap.cs` et outil Editor de capture.
- Log Builder-B `Logs/worldmap_step3_large_chunks_validate_final.log`.

## Conclusion de prevalidation

Aucun critere de blocage demande n'est observe:

- pas de contradiction materielle entre rapport, manifestes, code, logs et captures;
- aucune preuve obligatoire absente dans le perimetre Step 3;
- aucune erreur de compilation Unity;
- aucune route terrestre ou dependance pathfinding au reseau peint;
- aucun claim live/officiel trompeur non contrebalance: les mentions local/demo et non-claims sont explicites dans l'interface, les manifestes et les rapports.

La prevalidation QA-B est donc prete pour examen par QA-A, avec reserves non bloquantes documentees plus bas.

## Integrite du bundle

| Controle | Resultat QA-B |
|---|---|
| Nombre de PNG | 15/15 |
| Entrees PNG dans le manifeste | 15/15 |
| SHA-256 recalcules concordants | 15/15 |
| Dimensions PNG concordantes | 15/15 |
| Hashes PNG uniques | 15/15 |
| Distribution reelle | 11 captures en 1600x874; 4 captures en 2400x1474 |
| JSON determinisme lisible | Oui |
| Snapshot 1 identique au snapshot 2 | Oui |
| SHA-256 des snapshots recalcules | Deux concordances |

Les dimensions reelles different des presets demandes, mais elles sont mesurees, declarees et coherentes avec les fichiers. Aucun redimensionnement externe non declare n'est detecte.

## Matrice des controles fonctionnels

| Exigence | Preuve inspectee | Resultat QA-B |
|---|---|---|
| Carte plein ecran sans trou | 15 PNG, dont zoom 0.64x, 0.72x, 0.82x, 0.96x et 1.45x | CONFORME: aucune bande noire ou zone de chargement vide residuelle. |
| 25 chunks actifs | HUD, grille debug, manifeste et rayon runtime 2 | CONFORME: `5x5 / 25`, avant et apres pan. |
| Grille ON/OFF | PNG 02 contre PNG 01/03 et HUD | CONFORME: limites/IDs visibles ON, absents OFF; chunk courant cyan en debug. |
| Pan C32_32 vers C35_32 | PNG 04 et 05, coordonnees monde et HUD | CONFORME: trois frontieres, centre et chunk mis a jour, 25 actifs conserves, aucune zone vide. |
| Zoom | PNG 06/07 et 13/14 | CONFORME: contenu carte transforme; HUD, action, journal, legende et minimap gardent leurs ancres ecran. |
| Determinisme | JSON, manifeste, recalcul QA-B | CONFORME: deux snapshots identiques et deux hashes verifies pour C35_32. |
| Ruches et roles | PNG 08 | CONFORME: debutante, intermediaire, avancee, capitale; JOUEUR, ALLIEE et NEUTRE visibles. |
| Ressources | PNG 09 | CONFORME: Nectar, Pollen, Cire, Propolis et Gelee royale demo visibles. |
| Selection apres chunk | PNG 10, panneau et code `canCollect` | CONFORME: ruche et ressource C35_32 actives; `Collecter` est rendu et la condition d'activation est vraie a l'etat Idle. |
| VOL-42 inter-chunks | PNG 11/12, manifeste et outil de capture | CONFORME: meme ID et memes ancres; C35_32 vers C36_32; progression 14% vers 28%; arc maintenu. |
| Journal fixe | PNG 11 a 14, rect ecran fixe dans le code | CONFORME: ID, source, destination, etat, progression et gain local/demo restent lisibles. |
| Trajectoires aeriennes | PNG 07, 09, 11, 12, 14, 15 et code Bezier | CONFORME: arcs et essaims aeriens visibles, recalcules depuis les coordonnees monde. |
| Routes peintes ignorees | Manifestes, code, HUD et captures | CONFORME: `ground_routes_used:false`, aucun graphe/pathfinding routier; les routes de l'image restent decoratives. |
| Tablette paysage | PNG 15 | CONFORME AVEC RESERVE: cadrage plein, panneaux contenus et texte lisible; capture Editor 2400x1474 pour preset demande 1920x1200. |
| Compilation Unity | Logs Capture, Compile et validation Builder-B | CONFORME: Tundra success; compilation finale et validation batch terminent avec code 0. |
| Ruche interieure intacte | Rapports, manifestes, perimetre des fichiers et dates des principaux fichiers ruche | CONFORME SUR PREUVES FOURNIES: seuls les fichiers monde/capture sont associes a la vague; scene et bootstraps de ruche interieure sont anterieurs. |
| Non-claims | HUD, panneau action, manifestes, JSON et rapports | CONFORME: local/demo explicite; serveur, placement, collecte et economie persistante officiels restent faux. |

## Inspection des 15 captures

| PNG | Observation QA-B |
|---|---|
| 01 Overview | Carte dominante, 25 actifs, C32_32, grille OFF, panneaux fixes; aucun trou. |
| 02 DebugGridOn | Grille 5x5 materialisee, IDs secteurs/chunks et chunk courant cyan; aucun trou. |
| 03 ProductGridOff | Meme etat produit sans overlay debug; distinction ON/OFF nette. |
| 04 PanStart | Depart C32_32, 25 actifs et coordonnees de depart coherentes. |
| 05 PanArrival | Arrivee C35_32, cache accru mais actifs maintenus a 25; carte chargee et selections nouvelles. |
| 06 ZoomOut | Zoom 0.64x sans bande noire; overlays fixes et selection lisible. |
| 07 ZoomIn | Zoom 1.45x; vol aerien, essaim, journal et panneaux restent visibles. |
| 08 HiveLevelsRoles | Quatre niveaux et trois relations couverts; certaines entites de bord sont partiellement masquees par les panneaux, sans invalider la preuve. |
| 09 ResourceFamilies | Les cinq familles sont lisibles; preview aerienne clairement etiquetee, aucune route au sol. |
| 10 PostChunkSelection | Ruche et Gelee royale demo selectionnees; bouton `Collecter` present; journal vide coherent avec Idle. |
| 11 VOL42 Before | C35_32, VOL-42 En vol a 14%, arc et journal concordants. |
| 12 VOL42 After | C36_32, meme VOL-42 a 28%, source/destination conservees et arc encore visible. |
| 13 FixedPanels ZoomOut | Panneaux aux memes positions ecran a 0.64x; journal VOL-42 lisible. |
| 14 FixedPanels ZoomIn | Panneaux aux memes positions ecran a 1.45x; seule la carte change d'echelle. |
| 15 TabletLandscape | Carte pleine et lisible; HUD/action/journal/legende/minimap contenus; non-claims visibles. |

## Compilation et logs

### Capture

- Unity 6000.2.10f1.
- `Tundra build success` observe.
- Message final de production des captures DEMO-090 observe.
- Les 15 fichiers existent et leurs hashes correspondent au manifeste.
- Warnings non bloquants: validation initiale Licensing Client puis connexion, token de mise a jour indisponible, API Android `PlayerSettings` obsolete et avertissement de camera URP dans la Game View.
- Aucun `error CS`, `Scripts have compiler errors`, `Compilation failed` ou `Build failed`.

### Compilation finale

- `Tundra build success` observe.
- `Exiting batchmode successfully` observe.
- `return code 0` explicite dans le log.
- Le `Curl error 42` apparait pendant l'arret batch apres le succes et ne change pas le code final.

### Validation Builder-B

- Le log reference existe.
- `Tundra build success` observe.
- Validation de la scene monde terminee.
- `return code 0` explicite.

## Determinisme et VOL-42

Le JSON de determinisme a ete parse et ses valeurs ont ete recalculees:

- deux initialisations runtime locales independantes;
- meme seed et meme chunk C35_32;
- snapshots tries identiques;
- les deux SHA-256 fournis correspondent au recalcul QA-B;
- `official_server_authority:false` explicite.

Pour VOL-42, la formule runtime de progression produit bien 14% au timer 1.10 et 28% au timer 2.10. L'outil appelle d'abord l'action runtime `StartLocalCollectionFlight`, conserve ensuite la meme reference objet, prepare les timers pour la stabilite des captures et verifie la conservation des ancres monde. Cette preparation Editor est honnetement declaree.

## Ruche interieure et perimetre

Les rapports Builder-B et Demo-A ne se contredisent pas:

- Builder-B a modifie le bootstrap carte monde et son scene builder avant la capture;
- Demo-A declare ne pas avoir remodifie ce bootstrap ni la scene pendant sa passe de preuves;
- les fichiers recents du perimetre sont limites au monde Step 3 et a l'outil de capture;
- les principaux fichiers de scene/bootstrap de ruche interieure ont des dates anterieures a la vague.

Le dossier `.git` local ne contient pas de metadata exploitable, donc aucun diff Git historique independant n'est disponible. La preuve d'integrite de la ruche repose sur le perimetre declare et les fichiers observes; cette limite n'est pas contradictoire et ne bloque pas la passe locale/demo.

## Non-claims verifies

- `server_live:false`.
- `official_placement:false`.
- `official_collection:false`.
- `persistent_economy:false`.
- `official_server_authority:false`.
- `ground_routes_used:false`.
- `painted_roads_ignored:true`.
- `inner_hive_touched:false`.
- `official_world_map_final_art:false`.

Le titre visuel mentionne une carte mondiale MMO, mais le meme ecran affiche `local/demo`, `placement client non officiel`, `Routes au sol: NON` et les non-claims. Aucun claim live trompeur n'est retenu.

## Reserves non bloquantes pour QA-A

### Editor

- Les etats pan, zoom, grille, selection et timers sont prepares par reflection dans un outil Editor.
- Les captures prouvent le rendu et l'etat runtime, pas une video continue d'entrees manuelles.
- Le preset et le PNG reel different avec le DPI/Game View Windows; les deux dimensions sont declarees.

### Tactile

- Le code contient pan mono-touch et pinch bi-touch.
- Aucun appareil physique ni geste tactile enregistre n'est fourni.
- La selection tactile par tap n'est pas prouvee dans ce bundle.

### UI

- L'IMGUI reste dense et de niveau lab.
- Certains labels/entites de bord passent sous les panneaux fixes ou se chevauchent visuellement.
- La minimap logique est tres petite a l'echelle 64x64 et demande un polish produit.
- Le bouton actif conserve un style visuel gris peu demonstratif; son activation est etablie par l'etat et le code.

### Tuiles temporaires

- `carte.png` est repete/echantillonne par chunk comme proxy.
- Les raccords et repetitions sont visibles.
- La preuve valide le modele logique de chunks, pas l'art final ni des tuiles artistiques uniques.

### Portee

- Le determinisme couvre un snapshot cible C35_32, pas l'ensemble des 4096 chunks.
- Les relations, placements, collectes, recompenses et vols restent client/local demo.
- Aucun serveur, combat, guerre, territoire live, economie persistante ou autorite officielle n'est valide.

## Handoff QA-A

QA-A peut reexaminer le bundle avec les reserves ci-dessus. QA-B ne ferme aucun gate officiel et ne prononce aucune validation live.

QA_B_DEMO_090_PREVALIDATION = READY_FOR_QA_A
