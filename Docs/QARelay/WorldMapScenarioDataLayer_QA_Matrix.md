# WorldMap Scenario/Data Layer - QA Matrix P6

Date locale: 2026-07-15

Role: QA-Relay read-only, matrice documentaire uniquement.

## Portee et exclusions

- Portee: validation data/scenarios locale pour World Map runtime 25x25 visible + catalogue logique 50x50.
- Exclusions: aucun Unity, PNG, APK, scene, BearDen source, master terrain, serveur, remote, DNS/TLS/SQL, donnees reelles, gain officiel ou persistance officielle.
- Le 50x50 reste une reprojection/catalogue logique. Aucun terrain 50x50 ne doit etre genere.
- Les interactions restent demo/locales avec `server=false`, `official_gain=false` et aucun etat officiel.

## Sources locales consultees

- `Docs/Recovery/BeeKingdom_Relay_Progress.md`
- `Docs/QARelay/WorldMap50x50Readiness_QA_Matrix.md`
- `Docs/BuilderCRelay/WorldMap50x50_RuntimePerformanceContract.md`
- `Docs/WorldMapRuntimeEntitiesWave1/WorldMap50x50Readiness_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/MapReadingTools_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/InteractionPolish_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/AutomatedRegression_Report.md`
- `Docs/WorldMapRuntimeEntitiesWave1/WorldMap50x50Readiness_ConsolidatedDemoReport.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/InteractionPolishProof/InteractionPolishProofReceipt.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/AutomatedRegressionProof/AutomatedRegressionProofReceipt.md`

## Seuils PASS/FAIL P6

| Domaine | PASS | FAIL |
| --- | --- | --- |
| `entity_id` | Unique dans chaque snapshot, stable entre deux runs meme seed/version, non vide, prefixe par famille ou type lisible | ID duplique, ID vide, ID change sans changement de seed/version, ID derive d'une position ecran |
| Coordonnees normalisees | Chaque entite a `world_grid_version`, `chunk_id`, `local_x01`, `local_y01`, `world_coord_normalized` dans [0,1] | Coord hors bornes, version absente, coord ecran source de verite, dependance aux pixels terrain |
| Reprojection 25x25 -> 50x50 | Reprojection depuis normalise 0..1 vers grille 50x50, chunk clamp correct, sans terrain 50x50 | Generation PNG/terrain 50x50, mapping par pixel terrain, perte d'identite logique |
| Serialization locale | Version explicite, schema lisible, reset deterministic restaure defaults, champs inconnus ignores ou refuses avec erreur claire | Version absente, migration silencieuse destructive, reset non deterministe |
| Provider local | Snapshot porte `server=false`, `official=false` ou equivalent, interactions portent `official_gain=false` | Appel serveur/remote, gain officiel, persistence officielle, libelle trompeur |
| Presets scenario | Collecte R3, Duel, Raid T7 disponibles comme presets locaux reproductibles | Preset absent, non reproductible, modifie un etat officiel |
| Deux ruches test | Edition player/enemy locale, classes/niveaux valides, reset deterministic | Classe invalide acceptee, reset partiel, ruche deplacee sans ancre versionnee |
| Filtres/Proche/legende | Ruches/Ressources/Menaces/BearDen filtrables, Proche coherent, legende R1-R3/T1-T7 | Terrain masque par defaut, mauvais filtre, legende ambigue |
| BearDen | Visible/cache/restaure, separe des entites runtime | BearDen remplace/modifie ou confondu avec bestiaire |
| Pan/zoom | HUD fixe, selection et labels coherents, aucune fuite d'objets actifs | HUD deplace/occlusif, selection fantome, allocations ou objets actifs non bornes |
| Budgets 50x50 | Catalogue 2500, active chunks centre/NW/SE/densite 25/9/9/25, hives/resources/bestiary <= 25/75/25, cache terrain <= 96, stress alloc <= 2 000 000 B | Tout depassement, cache pollue, stress actif par defaut |
| Regression P1-P5 | P1/P2/P3/P4/P5 restent PASS, Wave5 regression NO, BearDen regression NO | Regression sur readiness, filtres, interactions, regression auto ou package demo |

## Matrice QA actionnable

| ID | Priorite | Zone | Scenario/Test | Donnees d'entree | PASS attendu | FAIL attendu |
| --- | --- | --- | --- | --- | --- | --- |
| P6-DATA-001 | P1 | Entity ID | Generer deux snapshots identiques meme seed/version | Snapshot A/B local | Meme set d'`entity_id`, aucun doublon | ID manquant, duplique ou instable |
| P6-DATA-002 | P1 | Entity ID | Changer seulement le centre de fenetre puis revenir | Centre -> voisin -> centre | Les IDs revenus au centre restent identiques | IDs regen aleatoires ou lies aux objets Unity |
| P6-DATA-003 | P1 | Coordonnees | Verifier chaque entite visible | Hives/resources/bestiary actifs | `chunk_id`, `local_x01/y01`, normalized [0,1], version presents | Coord hors bornes ou version absente |
| P6-DATA-004 | P1 | Reprojection | Reprojeter anchors 25x25 vers grille logique 50x50 | Coord normalisee, grid version source/cible | Chunk cible 0..49, local 0..1, aucun terrain genere | PNG/terrain cree ou coord par pixel |
| P6-DATA-005 | P1 | Provider local | Lire un snapshot local | Provider demo | `server=false`, `official=false`, `official_gain=false` dans interactions | Serveur/remote/officiel present |
| P6-DATA-006 | P1 | Serialization | Sauver/recharger snapshot local versionne | Donnees demo | Meme contenu logique, version explicite | Perte ID/coord, version absente |
| P6-DATA-007 | P1 | Regression P1-P5 | Relire verdicts consolides | P1-P5 reports/receipts | P1-P5 PASS, Wave5/BearDen regression NO | Toute regression P1-P5 |
| P6-SCEN-001 | P2 | Preset Collecte R3 | Lancer preset Collecte R3 | Ressource R3 ex. `[R3] 129/129` | Selection, quantite, collecte, epuisement, respawn demo; no official gain | Quantite negative, collecte officielle, respawn non deterministe |
| P6-SCEN-002 | P2 | Preset Duel | Lancer duel local tier bas | Cible solo T1-T4 | Mode solo/local, resultat deterministe, `server=false` | Resultat officiel ou cible raid traitee comme duel |
| P6-SCEN-003 | P2 | Preset Raid T7 | Lancer raid T7 local | Cible T7 | Mode raid_local, required/available/result visibles, `official_gain=false server=false` | T7 lance en solo ou loot officiel |
| P6-SCEN-004 | P2 | Deux ruches test | Editer PLAYER_TEST_HIVE et ENEMY_TEST_HIVE | Niveau/classe/faction valides | Rendu applique sans deplacement, overlays distincts | Classe/faction fusionnee dans sprite ou position perdue |
| P6-SCEN-005 | P2 | Reset deterministic | Modifier ruches puis reset | Deux ruches test | Defaults identiques a chaque reset | Reset partiel ou valeurs aleatoires |
| P6-MAP-001 | P2 | Filtres | Toggle Ruches/Ressources/Menaces/BearDen | Carte centre | Terrain non masque, overlays seuls changes | Terrain masque ou mauvais overlay |
| P6-MAP-002 | P2 | Proche | Selectionner noeud proche du centre | Centre actuel | Noeud coherent et dans fenetre active | Noeud hors fenetre ou mauvais type |
| P6-MAP-003 | P2 | Legende | Verifier R1/R2/R3 et T1..T7 | HUD lecture | Richesse/tier explicites, symboles accessibles | Legende absente ou couleur seule |
| P6-MAP-004 | P2 | BearDen | Basculer visible/cache/restaure | BearDen local | Etat restaure, pas remplace | BearDen source modifie ou associe a bestiaire |
| P6-MAP-005 | P2 | Pan/zoom | Pan centre/bords + zoom court | Centre, NW, SE | HUD fixe, pas de selection fantome, budgets inchanges | HUD bouge, objets actifs fuient |
| P6-BUD-001 | P1 | 50x50 centre | Stress centre logique | Grille 50x50 | 2500 catalog, 25 chunks actifs max | Plus de 25 chunks ou terrain cree |
| P6-BUD-002 | P1 | 50x50 bords | Stress NW/SE | C00_00, C49_49 | 9 chunks actifs observes/valides | Fuite bords ou coord hors grille |
| P6-BUD-003 | P1 | 50x50 densite | Fenetre la plus dense | Scan logique 2500 | Hives/resources/bestiary <= 25/75/25 | Depassement d'un plafond |
| P6-BUD-004 | P1 | Cache/allocation | Comparer avant/apres stress | Cache terrain/chunk, alloc | Cache stable, terrain cache <= 96, alloc <= 2 000 000 B | Cache pollue ou alloc > seuil |
| P6-NEG-001 | P1 | Negatif ID duplique | Injecter/charger deux entites meme `entity_id` | Snapshot local invalide | Rejet explicite, erreur lisible, aucun spawn double | Acceptation silencieuse |
| P6-NEG-002 | P1 | Negatif coord hors bornes | Entite normalized <0 ou >1 | Snapshot local invalide | Rejet ou clamp audite, pas de spawn hors monde | Spawn hors grille ou crash |
| P6-NEG-003 | P1 | Negatif classe invalide | Ruche classe inconnue | Edition ruche | Refus + message; reset conserve defaults | Classe inconnue acceptee |
| P6-NEG-004 | P1 | Negatif quantite negative | Ressource remaining < 0 | Preset Collecte R3 invalide | Refus/clamp a 0 audite, collecte bloquee | Quantite negative affichee/collectee |
| P6-NEG-005 | P1 | Negatif T7 solo | Tenter T7 en solo | Cible T7 | Refus solo, route vers raid_local | T7 solo accepte |

## Criteres de contre-validation documentaire P6

Quand le rapport P6 principal apparait, QA-Relay doit verifier sans executer Unity:

- Le rapport cite un recu local P6 lisible et non un log massif.
- Le rapport confirme `READY_FOR_P6_VALIDATION=YES` ou explique chaque blocage.
- Les tests P6-DATA, P6-SCEN, P6-MAP, P6-BUD et P6-NEG sont tous couverts.
- Les resultats negatifs sont des PASS de refus controle, pas des crashes.
- Le rapport garde les exclusions: aucun Unity/PNG/APK/terrain/BearDen/source modifie par QA.
- Les flags `server=false`, `official_gain=false` et absence de remote/officiel sont explicitement prouves.
- Les budgets 50x50 et la regression P1-P5 restent sous seuils.

## Etat documentaire actuel

| Element | Statut |
| --- | --- |
| Base P1-P5 locale | PASS documentaire, d'apres rapport consolide |
| Matrice P6 data/scenarios | PRETE |
| Rapport P6 principal | NON TROUVE dans les rapports consultes |
| Contre-validation documentaire P6 | PENDING_REPORT |

## Gate

READY_FOR_P6_VALIDATION=YES

Raison: les seuils, tests positifs, tests negatifs, budgets 50x50 et criteres de regression P1-P5 sont definis et actionnables. Le verdict final P6 reste conditionne a l'apparition du rapport P6 principal et de son recu local.

## Template verdict P6

```text
WORLD_MAP_SCENARIO_DATA_LAYER_QA_VERDICT

Date locale:
Rapport P6 principal lu:
Recu P6 lu:

Exclusions confirmees:
- Unity/scene/PNG/APK:
- Wave5/master terrain/BearDen source:
- Serveur/remote/donnees reelles/gain officiel:

Synthese:
- Entity_id unicite/stabilite:
- Coordonnees normalisees/reprojection:
- Serialization/version locale:
- Provider local server=false official_gain=false:
- Presets Collecte R3/Duel/Raid T7:
- Deux ruches test/edit/reset:
- Filtres/Proche/legende/BearDen/pan-zoom:
- Budgets 50x50:
- Regression P1-P5:
- Tests negatifs:

Defauts bloquants:
-

Notes:
-

Verdict:
PASS / PASS_WITH_NOTES / FAIL / NOT_RUN

Gate:
READY_FOR_P6_VALIDATION=YES/NO
```
