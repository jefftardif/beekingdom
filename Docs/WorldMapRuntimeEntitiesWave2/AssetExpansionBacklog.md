# WorldMap Runtime Entities Wave2 - Asset Expansion Backlog

Date de cadrage: 2026-07-15  
Responsabilite: UI / art planning, documentation seulement  
Perimetre de cette mission: aucun Unity, PNG, APK, serveur, publication ou changement terrain.

## 1. Sources et baseline autoritaire

Sources relues:

- Manifestes premium `H1`, `H2`, `H3`, `R1`, `R2`, `R3`, `M1`.
- Manifestes agreges `manifest_wave1_premium_all_lots.json`, `manifest_wave5_readability.json` et `lab_placeholder_exchange_manifest.json`.
- `PremiumWave1_FinalLocalReview.md`.
- `EntityMatrix.md`.
- `HiveRuntimeProgressionIntegration_Report.md`.

Baseline acceptee:

- 59 sprites premium Wave1, tous en 512 x 512 avec transparence.
- 24 ruches: 4 neutres pre-classe et 20 ruches de classe.
- 21 ressources: 7 types x 3 richesses.
- 14 creatures: 2 par tier T1 a T7.
- Lisibilite Wave5 declaree `PASS` a 100 %, 50 % et 25 %.
- Progression de ruche, changement de classe et overlays de faction runtime declares `PASS`.
- Wave5, master terrain et BearDen restent intouches.

La validation Wave1 est reutilisee. Aucun asset valide ne doit etre refait sans echec documente a un gate Wave2.

## 2. Decisions de production verrouillees

1. **Player et ennemi sont des usages runtime, pas deux peintures de faction.** Un seul sprite de corps, faction-neutre, sert aux deux etats. Le marqueur player/enemy reste une surcouche UI/runtime separee. Aucun rouge, bleu, blason, fanion, medaillon, anneau ou autre code de faction n'est peint dans le sprite.
2. **Les niveaux 1 a 9 ont chacun une ruche neutre exacte.** Les assets Wave1 des niveaux 1, 4, 7 et 9 restent canoniques. Wave2 complete uniquement 2, 3, 5, 6 et 8.
3. **La classe commence au niveau 10.** Les classes canoniques sont `royal_guard`, `striker`, `nurturer`, `scout` et `alchemist`.
4. **Les jalons de classe restent 10, 20, 35 et 50.** La resolution intermediaire reste deterministe: 10-19 vers L10, 20-34 vers L20, 35-49 vers L35 et 50+ vers L50.
5. **R1-R3 sont des richesses, pas des factions.** Correspondance canonique: R1=`poor`, R2=`medium`, R3=`rich`.
6. **Solo/raid est une propriete d'encounter.** Elle n'est pas peinte dans les creatures et ne cree pas de doublon raster. T1-T5 couvrent solo, elite, groupe et escouade; T6 couvre mini-raid; T7 couvre raid boss.
7. **Les landmarks sont des objets runtime autonomes.** Ils n'incluent ni sol, ni route, ni texte, ni UI, ni anneau de placement, et ne remplacent pas BearDen.

## 3. Inventaire, trous et quantites

| Famille | Cible verrouillee | Existant Wave1 | Trou raster | Action Wave2 | Priorite |
| --- | ---: | ---: | ---: | --- | --- |
| Ruches neutres L1-L9 | 9 | 4 | **5** | Produire L2, L3, L5, L6, L8 | P0 |
| Ruches de classe L10/L20/L35/L50 | 5 classes x 4 = 20 | 20 | 0 | Reutiliser et certifier | P1 QA |
| Usages player/enemy des ruches | 29 sprites x 2 = 58 cas | 24 sprites x 2 = 48 cas | **0 doublon** | Etendre la matrice de test avec les 5 nouveaux corps | P0 UI/QA |
| Ressources R1-R3 | 7 types x 3 = 21 | 21 | 0 | Reutiliser et certifier | P1 QA |
| Bestiaire T1-T7 | 2 par tier = 14 | 14 | 0 | Reutiliser et certifier solo/raid | P1 QA |
| Landmarks futurs | 8 | 0 | **8** | Produire apres P0, sans placement terrain | P2 |

Comptage des sprites de gameplay:

- Baseline existante acceptee: **59**.
- Nouveau coeur Wave2 P0: **5**.
- Total apres P0: **64**.
- Extension landmarks P2: **8**.
- Total cible apres P2: **72**.
- Backlog raster neuf total: **13** = 5 P0 + 8 P2.
- Les manifestes, planches de contact et captures QA ne sont pas comptes comme sprites de gameplay.
- Aucun sprite `_player` ou `_enemy` n'est ajoute: les 58 cas sont des combinaisons de rendu, pas 58 fichiers.

## 4. Inventaire Wave1 a reutiliser

### 4.1 Ruches

| Lot | Assets existants | Quantite | Etat |
| --- | --- | ---: | --- |
| H1 | `hive_neutral_l1`, `hive_neutral_l4`, `hive_neutral_l7`, `hive_neutral_l9` | 4 | Accepte; interpolation a completer |
| H2 | `hive_{royal_guard,striker,nurturer,scout,alchemist}_l10` | 5 | Complet |
| H3 L20 | `hive_{royal_guard,striker,nurturer,scout,alchemist}_l20` | 5 | Complet |
| H3 L35 | `hive_{royal_guard,striker,nurturer,scout,alchemist}_l35` | 5 | Complet |
| H3 L50 | `hive_{royal_guard,striker,nurturer,scout,alchemist}_l50` | 5 | Complet |

Apres P0, la bibliotheque contient 29 corps de ruche uniques: 9 neutres et 20 de classe. Chaque corps doit etre utilisable avec les etats player et enemy sans modification des pixels du corps.

### 4.2 Ressources

Les sept types existants sont `nectar`, `pollen`, `water`, `wax`, `honey`, `royal_jelly` et `propolis`. Chacun possede les trois suffixes canoniques:

- R1: `resource_{type}_poor`.
- R2: `resource_{type}_medium`.
- R3: `resource_{type}_rich`.

Les 21 combinaisons existent. Il n'y a aucun besoin de nouveau raster ressource en Wave2. Un remplacement n'est autorise qu'en cas d'echec de lisibilite ou d'alpha, sans augmenter la quantite cible.

### 4.3 Bestiaire

| Tier | Assets existants | Bande d'encounter | Signature de silhouette | Trou |
| --- | --- | --- | --- | ---: |
| T1 | `beast_t1_aphid_thief`, `beast_t1_red_mite` | Solo nuisance | Ovale/antennes contre masse acarienne compacte | 0 |
| T2 | `beast_t2_cutter_ant`, `beast_t2_shield_beetle` | Solo ou petit groupe | Mandibules segmentees contre carapace-bouclier | 0 |
| T3 | `beast_t3_jumping_spider`, `beast_t3_robber_fly` | Solo elite | Pattes hautes contre axe volant | 0 |
| T4 | `beast_t4_mantis_predator`, `beast_t4_centipede_runner` | Elite ou pack | Bras-faucilles contre corps long rasant | 0 |
| T5 | `beast_t5_hornet_brigand`, `beast_t5_stag_beetle_raider` | Escouade | Ailes/abdomen contre bois et carapace lourde | 0 |
| T6 | `beast_t6_root_scorpion`, `beast_t6_armored_tarantula` | Mini-raid | Pinces/queue contre ancrage radial blinde | 0 |
| T7 | `beast_t7_ancient_hornet_queen`, `beast_t7_titan_stag_beetle` | Raid boss | Boss aerien couronne contre titan terrestre | 0 |

Les 14 sprites sont complets. Aucun ours n'entre dans cette matrice. Les versions solo, elite, pack, mini-raid et raid reutilisent les corps ci-dessus; niveau, jauge, cible et FX restent des couches runtime.

## 5. Backlog P0 - completion des ruches L1-L9

Lot recommande: `H4` (completion pre-classe, sans modifier H1).  
Quantite: **5 nouveaux sprites de gameplay**.

Les dimensions de bbox ci-dessous sont des cibles d'occupation, avec une tolerance artistique de +/- 12 px. Elles interpolent les bboxes Wave1 observees et ne remplacent pas le test visuel.

| Rang | ID canonique | Entre les references | Delta de silhouette obligatoire | Bbox alpha cible W x H |
| ---: | --- | --- | --- | ---: |
| 1 | `hive_neutral_l2` | L1 -> L4 | Ajouter une seconde masse superieure; entree encore unique | 309 x 342 |
| 2 | `hive_neutral_l3` | L2 -> L4 | Premiere epaule alveolaire visible; contour distinct de L2 | 309 x 350 |
| 3 | `hive_neutral_l5` | L4 -> L7 | Corps plus vertical et premier col structure; pas de motif de classe | 310 x 367 |
| 4 | `hive_neutral_l6` | L5 -> L7 | Seconde sortie ou couronne de cire; hauteur clairement superieure | 311 x 376 |
| 5 | `hive_neutral_l8` | L7 -> L9 | Couronne plus large et masse laterale pre-L9; aucune fortification de classe | 345 x 396 |

Regles P0:

- La progression doit etre visible par contour, masse, hauteur et nombre de volumes; jamais par simple recoloration ou simple mise a l'echelle.
- Le pied et le centre visuel restent stables pour eviter un saut lors d'un changement de niveau.
- L8 doit annoncer L9 sans reprendre les pointes, vigies, plaques, nurseries ou accessoires propres aux classes.
- Les niveaux 1 a 9 doivent rester lisibles comme une meme famille neutre.
- Le sprite de chaque niveau est identique pour player et enemy; seule la surcouche runtime change.

Sorties attendues lors de la future production, hors mission actuelle:

- 5 PNG de gameplay.
- `manifest_H4.json` avec ID, chemin, taille, `alpha_bbox`, SHA-256 et contraintes.
- Une planche H1+H4 ordonnee L1 a L9.
- Une planche Wave5 a 100 %, 50 % et 25 %.

## 6. Backlog P1 - certification sans nouvel asset

P1 ne commande aucun raster neuf. Il certifie les 20 ruches de classe, 21 ressources et 14 creatures deja valides.

| Pack | Quantite reutilisee | Verification Wave2 | Sortie en cas de PASS | Sortie en cas de FAIL |
| --- | ---: | --- | --- | --- |
| Classes H2/H3 | 20 | Classe lisible a niveau egal; progression 10/20/35/50 lisible | Conserver sans retouche | Remplacer uniquement l'asset fautif |
| Ressources R1-R3 | 21 | Type et richesse lisibles sans UI | Conserver sans retouche | Retouche 1:1, aucun nouveau type |
| Bestiaire M1 | 14 | Deux silhouettes distinctes par tier; escalade solo vers raid | Conserver sans retouche | Retouche 1:1, aucun ours |

Une retouche remplace un asset dans le compte existant. Elle ne cree pas une variante supplementaire et ne change pas la cible de 72 sprites.

## 7. Backlog P2 - landmarks futurs

Quantite: **8 nouveaux sprites de gameplay**.  
Etat: production artistique future; placement, collision et comportement gameplay restent hors de ce document.

| Rang | ID canonique | Fonction visuelle | Silhouette imposee | Echelle carte vs ruche L50 |
| ---: | --- | --- | --- | ---: |
| 1 | `landmark_queen_tree` | Capitale / repere majeur | Grand Y vegetal, couronne et rayon suspendu | 1.50x |
| 2 | `landmark_wax_cathedral` | Culture / progression | Trois fleches de cire, centre dominant | 1.35x |
| 3 | `landmark_sunflower_observatory` | Exploration | Tige haute et fleur inclinee en disque | 1.30x |
| 4 | `landmark_propolis_forge` | Artisanat | Masse basse large, enclume organique et deux cheminees | 1.20x |
| 5 | `landmark_royal_jelly_spring` | Soin / rarete | Vasque nacree en eventail, profil bas | 1.15x |
| 6 | `landmark_ancient_comb_ruin` | Lore / ruine | Arche alveolaire brisee en C asymetrique | 1.25x |
| 7 | `landmark_pollen_exchange` | Commerce | Auvents et paniers etages, aucun panneau texte | 1.15x |
| 8 | `landmark_sting_arena` | Combat | Palissades crochues asymetriques, aucun anneau UI | 1.30x |

Regles P2:

- Chaque landmark reste sur un canvas 512 x 512 pour conserver le contrat premium actuel.
- L'echelle du tableau est une echelle de rendu runtime, pas un agrandissement du canvas ni une ombre peinte.
- Un landmark doit etre reconnaissable par son contour a 25 %, meme en niveaux de gris.
- Aucun morceau de sol, sentier, riviere, cercle de selection, nom ou icone de fonction n'est integre.
- BearDen reste un repere externe preserve; aucun des huit assets ne le remplace ou ne l'imite.

## 8. Regles visuelles communes

### 8.1 Perspective et lumiere

- Perspective isometrique 3/4 compatible avec la baseline BearDen et Wave5.
- Key light haut gauche.
- Ombres internes courtes; aucune ombre projetee lourde ou plaque d'ombre de terrain.
- Materiaux organiques premium: cire, propolis, fibres vegetales, carapace et liquides lisibles sans bruit fin excessif.
- Aucun texte, UI, route, coordonnee, badge ou terrain dans les corps.

### 8.2 Langage de silhouette

| Famille | Signature obligatoire |
| --- | --- |
| Neutre L1-L9 | Croissance continue; base vegetale et cire brute; aucune classe avant L10 |
| `royal_guard` | Epaules larges, symetrie, plaques et alveoles cerclees |
| `striker` | Angles, pointes de cire/propolis, sorties agressives |
| `nurturer` | Dome rond, volumes de nurserie et grandes alveoles |
| `scout` | Axe haut et mince, vigies, antennes et plateformes legeres |
| `alchemist` | Atelier asymetrique, fioles/retortes organiques; silhouette lisible sans couleur |
| Ressources | Type reconnaissable par forme; richesse par volume et nombre d'elements |
| Bestiaire | Deux plans corporels distincts par tier; masse croissante jusqu'au raid |
| Landmarks | Un profil unique par fonction; aucune dependance a un pictogramme |

### 8.3 Echelle, ancrage et alpha

Le contrat source reste 512 x 512 RGBA. L'`alpha_bbox` correspond aux pixels dont l'alpha est non nul.

| Famille | Baseline alpha cible | Occupation de reference | Regle |
| --- | ---: | --- | --- |
| Ruches neutres | Y=447-448 | Wave1: env. 309 x 334 en L1 a 380 x 405 en L9 | Croissance monotone; centre X=256 +/- 8 px |
| Ruches de classe | Y=448 | Wave1: env. 380 px de large aux premiers jalons, jusqu'a 418 px en L50 | Pied stable entre 10/20/35/50; aucun clipping |
| Ressources | Y=436-438 | Wave1: env. 319-357 px de large et 174-259 px de haut | R1 < R2 < R3 en masse percue, ancrage bas constant |
| Bestiaire | Y=443-446 | Wave1: env. 331-413 px de large et 315-421 px de haut | T6/T7 dominent T1-T5; pose toujours centree |
| Landmarks | Y=446-450 | Bbox max recommandee 464 x 432 | Marge minimale 16 px, echelle carte geree au runtime |

Regles alpha bloquantes:

- Fond entierement transparent, sans matte clair ou sombre.
- Aucun pixel visible ne touche le bord du canvas; marge minimale 10 px pour les familles existantes et 16 px pour les landmarks.
- Pas de frange RGB, halo, pixel alpha isole, brouillard coupe ou particule hors silhouette.
- Les coins sont alpha 0; les pixels sous la baseline sont alpha 0.
- Le contour antialiase doit rester propre sur fond clair, sombre et tuile Wave5.
- Le point d'ancrage bas-centre ne varie pas de plus de 4 px entre deux niveaux d'une meme famille.

### 8.4 Player / enemy sans faction peinte

- Les deux usages referencent le meme fichier et le meme hash de corps.
- Le statut est porte par la surcouche runtime existante; elle peut varier par forme et couleur pour ne pas dependre de la couleur seule.
- Les anneaux de selection, barres, noms, compteurs, niveaux et etats de combat restent des couches UI.
- Les noms de fichiers de corps n'emploient jamais `_player`, `_enemy`, une couleur ou un nom de faction.
- `ally` et `neutral`, deja supportes par l'integration Wave1, restent compatibles mais ne creent aucun raster dans ce backlog.

## 9. Regles de nommage et packaging

Regles generales:

- Minuscules ASCII, `snake_case`, extension `.png`.
- Un ID de manifeste est identique au nom de fichier sans extension.
- Aucun espace, tiret, suffixe de version, couleur de faction ou coordonnee de carte.
- Un asset remplace conserve son ID canonique; la tracabilite passe par SHA-256 et manifeste, pas par `_v2`.

Schemas canoniques:

```text
hive_neutral_l{1..9}.png
hive_{royal_guard|striker|nurturer|scout|alchemist}_l{10|20|35|50}.png
resource_{nectar|pollen|water|wax|honey|royal_jelly|propolis}_{poor|medium|rich}.png
beast_t{1..7}_{species_role}.png
landmark_{theme_function}.png
```

Packaging Wave2 recommande:

```text
premium/H4/                         # 5 ruches pre-classe manquantes
premium/LM1/                        # 8 landmarks futurs
premium/manifest_wave2_all_lots.json
premium/manifest_wave2_readability.json
```

Les chemins ci-dessus decrivent la future livraison d'assets; ils ne sont pas crees par cette mission documentaire.

## 10. Gates UI / art / QA

Tous les gates marques bloquants doivent etre `PASS` avant integration runtime. Un gate echoue produit une retouche ciblee; il n'autorise pas la regeneration globale des 59 assets Wave1.

| Gate | Responsable | Critere de PASS | Bloquant |
| --- | --- | --- | --- |
| G0 - Comptage | Art QA | 5 nouveaux H4 en P0; 8 landmarks en P2; aucun doublon faction; totaux 64 puis 72 | Oui |
| G1 - Manifeste | Tech art QA | ID/nom uniques, 512 x 512, RGBA, bbox et SHA-256 presents; compte parse = compte declare | Oui |
| G2 - Alpha | Art QA | Baselines et marges conformes; zero clipping, matte, halo ou terrain peint sur fonds clair/sombre/Wave5 | Oui |
| G3 - Silhouette | UI/art | Chaque asset reste identifiable a 100 %, 50 % et 25 %; controle couleur et niveaux de gris | Oui |
| G4 - Progression L1-L9 | UI/art | Planche ordonnee: chaque voisin est distinct; masse percue non decroissante; aucun signal de classe avant L10 | Oui P0 |
| G5 - Classes | UI/art | A jalon egal, les 5 classes sont distinguables par contour; dans une classe, 10/20/35/50 restent ordonnes | Oui |
| G6 - Player/enemy | UI QA | 58 rendus verifies: corps identique par paire, seule la surcouche change, statut lisible sans couleur seule | Oui |
| G7 - Stabilite UI | UI QA | Label, niveau, barre, selection et overlay ne masquent pas l'entree ni la signature; aucune variation de layout au changement de niveau | Oui |
| G8 - Ressources | UI/art | Matrice 7 x 3 complete; type lisible par forme; R1/R2/R3 ordonnes; aucun aspect bouton ou icone UI | Oui |
| G9 - Bestiaire | UI/art | Matrice 7 x 2 complete; deux silhouettes par tier; T6 mini-raid et T7 raid dominants; aucun ours | Oui |
| G10 - Landmarks | UI/art | 8 profils uniques a 25 %; aucun texte, route, sol, anneau ou imitation de BearDen | Oui P2 |
| G11 - Readabilite carte | QA | Planche sur plusieurs tuiles Wave5, sans collision incoherente avec routes, labels ou entites voisines | Oui |
| G12 - Regression de perimetre | QA | Aucun changement aux tuiles Wave5, master terrain, BearDen, APK, serveur ou donnees reelles | Oui |

Preuves minimales attendues par lot futur:

- Manifeste JSON parse avec compte exact.
- Planche de contact sur transparence.
- Planche Wave5 a 100 %, 50 % et 25 %.
- Audit alpha sur fond blanc, noir et damier.
- Planche de progression ou de tier en niveaux de gris.
- Pour les ruches, matrice player/enemy montrant que seul l'overlay change.

La capture batchmode absente du rapport Wave1 n'est pas un blocage pour commencer la production d'assets. Elle ne remplace toutefois pas les preuves visuelles Wave2 ci-dessus avant integration.

## 11. Ordre d'execution recommande

1. Produire et valider le lot H4 de 5 ruches.
2. Executer la certification P1 des 55 sprites reutilises: 20 classes, 21 ressources, 14 creatures.
3. Executer la matrice UI des 29 ruches x 2 etats, soit 58 cas.
4. Geler le total coeur a 64 sprites.
5. Produire les 8 landmarks P2 dans l'ordre du tableau, sans placement terrain.
6. Executer les gates landmarks et geler le total etendu a 72 sprites.

## 12. Definition of Done

Le coeur Wave2 est termine lorsque:

- les 5 trous L2/L3/L5/L6/L8 sont combles;
- les niveaux L1-L9 forment une progression continue;
- les 20 ruches de classe, 21 ressources et 14 creatures reutilisees ont repasse leurs gates;
- les 58 cas player/enemy utilisent 29 corps sans faction peinte;
- tous les gates P0/P1 sont `PASS` et les preuves sont archivees.

L'extension landmarks est terminee lorsque les 8 assets P2 passent G0-G3 et G10-G12, sans modification terrain.

## Verdict

Le socle Wave1 est valide, les trous sont identifies, les quantites et noms sont fixes, et aucun choix artistique bloquant ne subsiste pour lancer H4. Les landmarks sont suffisamment cadres pour entrer en production apres P0; leur integration gameplay reste une etape ulterieure.

`READY_FOR_WAVE2_ASSET_PRODUCTION=YES`
