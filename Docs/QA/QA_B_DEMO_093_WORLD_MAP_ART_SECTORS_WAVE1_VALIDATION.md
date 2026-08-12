# QA-B - DEMO-093 World Map Art Sectors Wave 1 - Validation

Date: 2026-07-13  
Role: QA-B, prevalidation independante pour QA-A  
Mode: lecture seule, hors Unity  
Gate officiel: non ferme par QA-B

## 1. Verdict executif

Le lot DEMO-093 satisfait les controles techniques et visuels demandes pour un pilote artistique local 3 x 3:

- 9/9 secteurs PNG lisibles, RGB, 512 x 512 et conformes aux hashes du manifeste;
- reconstruction 1536 x 1536 exacte, sans trou et identique pixel par pixel au master, a la planche contact et a la preuve independante `00`;
- 12/12 coutures recalculees, sans ligne artificielle ni rupture naturelle observee;
- aucun texte, UI, ruche peinte, batiment joueur, ville moderne, route, rail ou pont routier observe dans les neuf secteurs;
- marqueurs, annotations, grille et zones d'evitement limites a des copies QA distinctes des neuf secteurs;
- 12 emplacements coherents en tant que candidats visuels uniquement;
- bords opposes non periodiques;
- claims limites a un pilote local 3 x 3, non live, non runtime et non HD final.

Le lot est recevable avec reserves de perimetre. Ces reserves n'invalident pas le pilote, mais interdisent de le presenter comme art final HD, carte MMO complete ou placements runtime valides.

## 2. Perimetre audite

Paquet de preuve:

- `C:\projets\beekingdom\prompt_demo\rapports\DEMO-093_WorldMapArtSectorsWave1\DEMO-093_Report.md`
- les 10 PNG de preuve `00` a `09`;
- `DEMO-093_IndependentVerification.json`;
- `independent_verify.py`, lu comme element de tracabilite mais non execute par QA-B.

Sources artistiques:

- `C:\projets\beekingdom\worldmap_art_wave4\UIB_SectorWave1\manifest.json`
- `verification.json`;
- les neuf `sector_*.png`;
- master, planche contact, grille QA et source generee;
- rapport source UI-B.

QA-B n'a pas lance Unity, n'a execute aucun script producteur et n'a modifie aucun asset source. Les calculs independants ont charge les PNG en memoire sans extraction ni reecriture.

## 3. Matrice d'acceptation

| Critere | Verification independante QA-B | Statut |
|---|---|---|
| 9/9 PNG 512 x 512 | 9 fichiers PNG RGB lisibles, dimensions identiques | PASS |
| Hashes | 9/9 egaux au manifeste, 9 hashes distincts | PASS |
| Reconstruction | 1536 x 1536, 0 pixel different contre master/contact/preuve 00 | PASS |
| Douze coutures | 12/12 recalculees et inspectees | PASS |
| Continuites naturelles | eau, relief, foret, fleurs, cristaux et lumiere coherents | PASS |
| Contenu interdit | aucun element interdit observe a l'echelle 1:1 | PASS |
| Copies QA | annotations absentes des neuf secteurs et du master propre | PASS |
| 12 emplacements | 12 IDs et coordonnees uniques, tous dans le bon secteur | PASS COMME CANDIDATS VISUELS |
| Bords non periodiques | bandes opposees non egales, deltas eleves et hashes uniques | PASS |
| Claims | pilote 3 x 3/local/non-live/non-HD, sans claim runtime | PASS |

## 4. Integrite des neuf secteurs

Ordre contractuel recoupe:

```text
NW | N | NE
W  | C | E
SW | S | SE
```

| Secteur | Fichier | Format | Dimensions | Hash manifeste | Position grille |
|---|---|---|---:|---|---|
| NW | `sector_NW.png` | PNG RGB | 512 x 512 | MATCH | MATCH |
| N | `sector_N.png` | PNG RGB | 512 x 512 | MATCH | MATCH |
| NE | `sector_NE.png` | PNG RGB | 512 x 512 | MATCH | MATCH |
| W | `sector_W.png` | PNG RGB | 512 x 512 | MATCH | MATCH |
| C | `sector_C.png` | PNG RGB | 512 x 512 | MATCH | MATCH |
| E | `sector_E.png` | PNG RGB | 512 x 512 | MATCH | MATCH |
| SW | `sector_SW.png` | PNG RGB | 512 x 512 | MATCH | MATCH |
| S | `sector_S.png` | PNG RGB | 512 x 512 | MATCH | MATCH |
| SE | `sector_SE.png` | PNG RGB | 512 x 512 | MATCH | MATCH |

Controles complementaires:

- 9 IDs uniques;
- 9 fichiers uniques;
- 9 coordonnees de grille uniques;
- 9 hashes de fichiers distincts;
- relations de voisinage N/E/S/W coherentes;
- hashes courants des 16 sources enregistrees identiques aux hashes `after` du paquet de preuve;
- reference `carte.png` presente et hash courant conforme a `verification.json`.

## 5. Reconstruction exacte

QA-B a reconstruit l'image en memoire directement depuis les neuf secteurs.

Resultats:

- dimensions: 1536 x 1536;
- surface couverte: 9 blocs de 512 x 512, sans trou ni chevauchement;
- comparaison au master `atlas_master_1536.png`: 0 pixel different;
- comparaison a `contact_sheet_3x3.png`: 0 pixel different;
- comparaison a `00_DEMO-093_Reconstruction_From9Sectors.png`: 0 pixel different;
- hash fichier du master egal a celui de la planche contact;
- hashes master et contact egaux aux valeurs du manifeste;
- source generee: 1254 x 1254, conforme a son hash et a ses dimensions declares;
- master, contact et grille QA: 1536 x 1536, hashes et dimensions conformes.

Resultat: reconstruction bit-a-bit confirmee.

## 6. Douze coutures et continuites

Mesure: ecart absolu moyen RGB entre les deux colonnes ou lignes qui se touchent. QA-B a recalcule les valeurs sans reutiliser les resultats JSON.

| Couture | Orientation | Moyenne RGB | Maximum canal | Ratio contre mediane locale | Observation |
|---|---|---:|---:|---:|---|
| NW\|N | verticale | 12.467 | 92 | 0.937 | eau et foret continues |
| N\|NE | verticale | 20.797 | 117 | 0.983 | massif montagneux continu |
| W\|C | verticale | 11.064 | 121 | 0.882 | prairie et lisiere continues |
| C\|E | verticale | 14.538 | 110 | 0.905 | riviere et relief continus |
| SW\|S | verticale | 14.761 | 106 | 1.036 | fleurs et vegetation continues |
| S\|SE | verticale | 13.037 | 80 | 0.890 | fleurs, foret et relief continus |
| NW/W | horizontale | 8.721 | 83 | 1.017 | lac et rive continus |
| N/C | horizontale | 12.793 | 110 | 0.895 | eau et prairie continues |
| NE/E | horizontale | 12.961 | 98 | 1.069 | relief et clairiere continus |
| W/SW | horizontale | 13.158 | 89 | 0.952 | foret et riviere continues |
| C/S | horizontale | 15.842 | 83 | 0.996 | prairie fleurie continue |
| E/SE | horizontale | 15.128 | 97 | 0.993 | cristaux, roche et eau continus |

Le ratio compare chaque couture a la mediane des differences entre pixels voisins dans une bande locale autour de la meme frontiere. Maximum observe: `1.069`; aucune couture ne depasse `1.5`. Les frontieres contractuelles ne sont donc pas des anomalies colorimetriques locales.

Inspection visuelle:

- aucune ligne droite de grille sur le master ou la reconstruction propre;
- aucune bande vide, changement soudain de lumiere ou brouillard opaque de masquage;
- les rivieres et lacs se prolongent sans cassure artificielle;
- les cretes montagneuses conservent orientation, matiere et eclairage;
- les transitions foret, prairie et fleurs restent irregulieres et naturelles;
- la geologie cristalline se poursuit de E vers SE;
- les douze crops de `03_DEMO-093_All12InternalSeams.png` sont coherents avec l'atlas;
- les zooms N|NE et C/S ne montrent aucun trait de raccord ajoute.

Resultat: 12/12 coutures acceptees pour le pilote.

## 7. Contenu interdit

Les neuf secteurs ont ete inspectes individuellement a leur resolution native, puis dans l'atlas complet.

| Element interdit | Constat |
|---|---|
| Texte, label, badge ou watermark | aucun observe |
| UI ou grille | aucune dans les neuf secteurs ou le master propre |
| Ruche ou batiment joueur peint | aucun observe |
| Ville ou objet moderne | aucun observe |
| Route ou chemin terrestre structure | aucun observe |
| Rail | aucun observe |
| Pont routier | aucun observe |

Les bandes minerales claires de E/SE ont ete controlees: elles sont irregulieres, rocheuses, interrompues par cristaux et reliefs, sans largeur constante, bordure amenagee ni infrastructure. Elles se lisent comme sol geologique expose, pas comme route peinte.

Cette conclusion est une inspection visuelle humaine du lot courant. Elle ne vaut pas detecteur semantique automatique pour de futures vagues.

## 8. Isolation des overlays QA

Assets propres confirmes:

- les neuf `sector_*.png`;
- `atlas_master_1536.png`;
- `contact_sheet_3x3.png`;
- reconstruction brute `00_DEMO-093_Reconstruction_From9Sectors.png`.

Copies ou planches QA distinctes:

- `qa_seam_grid_3x3.png`: grille de couture explicitement QA;
- `02_DEMO-093_FeatureContinuity.png`: reperes de lecture;
- `03`, `04`, `05`: crops et marqueurs de coutures;
- `06_DEMO-093_RuntimePlacementCandidates.png`: 12 reperes candidats;
- `07_DEMO-093_TemporaryMarkerReadability.png`: une ruche, deux ressources et un vol temporaires;
- `08_DEMO-093_AvoidWaterReliefZones.png`: contours eau/relief indicatifs;
- `09_DEMO-093_NonPeriodicOuterEdges.png`: comparaison annotee des bords.

Les marqueurs temporaires n'apparaissent pas dans les hashes des secteurs ni dans le master propre. Les zones d'evitement existent uniquement dans la copie QA `08` et dans le JSON comme annotations illustratives; elles ne sont ni collisions, ni masques gameplay, ni donnees serveur.

Reserve d'integration: un importeur runtime devra utiliser une allowlist des neuf secteurs et du manifeste. Il ne devra pas importer `qa_seam_grid_3x3.png` ou les PNG de preuve comme art de jeu.

## 9. Douze emplacements candidats

Verification structurelle:

- 12 entrees;
- 12 IDs uniques;
- 12 coordonnees uniques;
- 12/12 dans les bornes 1536 x 1536;
- 12/12 etiquettes de secteur conformes aux coordonnees;
- repartition sur 8 secteurs;
- les quatre exemples de marqueurs utilisent uniquement des points candidats.

Verification semantique:

- les points visent des clairieres, plateaux ou lisieres visuellement exploitables;
- certains restent proches d'eau, de foret, de relief ou de cristaux;
- aucune collision, pente, empreinte de modele, distance de securite, densite gameplay ou regle server-first n'est fournie.

Conclusion: les 12 positions sont acceptees comme candidats visuels de composition uniquement. Aucun placement runtime n'est valide par DEMO-093.

## 10. Bords non periodiques

QA-B a compare les bandes opposees de 32 pixels du master reconstruit:

- haut contre bas: non identiques, delta moyen `40.383`;
- gauche contre droite: non identiques, delta moyen `58.095`;
- 9/9 hashes de secteurs distincts;
- reliefs, eau et vegetation externes differents visuellement.

Conclusion: le pilote n'est pas periodique. Ses bords ne doivent pas etre boucles, repetes ou juxtaposes arbitrairement. Une extension devra etre generee avec contexte de bord.

## 11. Audit des claims

Le rapport DEMO-093 et le JSON de preuve maintiennent les distinctions requises:

| Claim | Etat |
|---|---|
| Pilote artistique 3 x 3 | revendique et prouve |
| Verification locale hors Unity | revendique et coherente |
| Carte monde immense terminee | explicitement non revendiquee |
| Integration runtime | explicitement non validee |
| Placements gameplay/server-first | explicitement non valides |
| Serveur ou donnees live | explicitement non revendiques |
| Art final haute definition | explicitement non prouve |
| Secteurs hors du lot | aucun claim |

Le champ `status = production-pilot` du manifeste doit etre lu avec `worldClaim`, qui le limite explicitement au pilote 3 x 3. Il ne constitue pas un claim d'art final de production.

## 12. Reserves QA-B

### R1 - Resolution source

Le master source de 1254 x 1254 a ete agrandi par facteur `1.22488` vers 1536 x 1536. Le lot est adapte au controle des coutures, de la composition et de la lisibilite initiale, pas a une validation d'art final HD ou de zoom proche de production.

### R2 - Placement runtime

Les 12 emplacements et les zones d'evitement restent des indications visuelles. Collision, pente, echelle des ruches/ressources, regles territoriales et autorite serveur restent a valider dans une vague runtime server-first.

### R3 - Discipline des copies QA

La grille QA reside dans le dossier artistique source et les autres annotations dans le paquet Demo-B. Le handoff doit conserver une allowlist stricte pour eviter toute ingestion accidentelle d'une planche annotee.

### R4 - Extension du monde

Les bords externes ne sont pas raccordables a eux-memes. Une vague suivante doit produire des voisins avec contexte; aucun claim de monde au-dela du 3 x 3 n'est permis.

Ces quatre reserves sont explicites dans les livrables et ne constituent pas une contradiction ou un manque de preuve pour le pilote demande.

## 13. Risques bloquants

- P0: aucun observe.
- P1: aucun observe.
- Preuve absente: aucune pour les criteres DEMO-093 demandes.
- Claim trompeur: aucun observe, sous reserve de maintenir le vocabulaire pilote/local/non-live/non-HD.

## 14. Note de livraison

Le chemin demande `C:\projets\beekingdom\QA\QA_B_DEMO_093_WORLD_MAP_ART_SECTORS_WAVE1_VALIDATION.md` est hors du perimetre d'ecriture accorde a cette session. La copie QA-B est donc produite dans le workspace autorise, sans modification des sources DEMO-093.

QA-A conserve seul le verdict officiel et la fermeture des gates.

QA_B_DEMO_093_WORLD_MAP_ART_SECTORS_WAVE1 = PASS_WITH_RESERVES
