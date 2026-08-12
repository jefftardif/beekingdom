# WorldMap Spawn Distribution - QA Matrix P7

Date locale: 2026-07-15

Role: QA-Relay read-only, preparation documentaire pour futur inspecteur/generateur local de spawns.

## Portee et exclusions

- Portee: matrice QA pour valider un futur inspecteur/generateur local de spawns deterministes World Map.
- Exclusions strictes: aucun Unity, PNG, APK, scene, master terrain, BearDen source, serveur, remote, DNS/TLS/SQL, donnees reelles, gain officiel ou persistence officielle.
- Le 25x25 Wave5 reste le terrain visible de reference.
- Le 50x50 reste une grille logique/reprojection. Aucun terrain 50x50 ne doit etre genere.
- Tout resultat local doit rester `server=false`, `official=false`, `official_gain=false`.
- L'overlay diagnostic doit etre off par defaut.

## Sources locales consultees

- `Docs/Recovery/BeeKingdom_Relay_Progress.md`
- `Docs/QARelay/WorldMapScenarioDataLayer_QA_Matrix.md`
- `Docs/BuilderCRelay/WorldMapScenarioDataLayer_TechnicalContract.md`
- `Docs/WorldMapRuntimeEntitiesWave1/WorldMap50x50Readiness_ConsolidatedDemoReport.md`
- `Docs/WorldMapRuntimeEntitiesWave1/AutomatedRegression_Report.md`

## Seuils PASS/FAIL P7

| Domaine | PASS | FAIL |
| --- | --- | --- |
| Determinisme seed/version | Meme `world_id`, `server_id`, `season_id`, `spawn_seed_version`, `exclusion_version` et grille donnent memes IDs, positions, tiers, richesses et flags | ID ou position change sans changement d'entree |
| Variation seed | Seed differente donne distribution differente, tout en gardant les memes budgets actifs et exclusions | Meme distribution avec seed differente, ou budgets depasses |
| Fenetre active | <= 25 chunks actifs; coins/bords clamps correctement | Plus de 25 chunks, fuite hors grille, chargement catalogue complet en scene |
| Densite entites | <= 25 ruches, <= 75 ressources, <= 25 menaces dans la fenetre active | Un plafond est depasse |
| Exclusions | 0 entite dans BearDen, eau, falaise, evenement reserve ou volume exclu versionne | Entite spawn dans une zone exclue ou exclusion non auditee |
| Chevauchement | 0 chevauchement critique bloquant selection/lecture; chevauchements mineurs listes | Entites superposees de facon non selectionnable ou illisible |
| Combat tier | T1-T4 solo; T5-T7 raid | T7 solo accepte, T5-T7 sans raid, T1-T4 forces raid sans regle |
| Richesse ressource | R1/R2/R3 lisibles par symbole et libelle, pas seulement couleur | Richesse ambigue ou depend uniquement de couleur |
| Bords/coins 25x25 | NW/NE/SW/SE et bords produisent coords dans grille, sans spawn hors monde | Coord hors bornes, clamp incorrect, trous critiques |
| Reprojection 50x50 | Position derivee de coord normalisee; chunk 0..49; local 0..1; aucun terrain 50x50 | Mapping par pixel terrain, terrain/PNG/atlas cree |
| Diagnostic overlay | Off par defaut, activable seulement localement, ne change pas les spawns | On par defaut ou modifie resultats |
| Autorite | Aucun serveur, aucune recompense officielle, aucun etat officiel | `server=true`, `official_gain=true`, remote ou persistence officielle |

## Matrice QA actionnable

| ID | Priorite | Zone | Test | Entree | PASS attendu | FAIL attendu |
| --- | --- | --- | --- | --- | --- | --- |
| P7-SEED-001 | P1 | Determinisme | Lancer deux generations avec meme seed et meme versions | Seed A, versions identiques | Hash distribution identique, memes IDs/positions/tiers/richesses | Difference non expliquee |
| P7-SEED-002 | P1 | Stabilite IDs | Regenerer apres changement camera puis retour | Centre -> voisin -> centre | IDs des entites revenues inchanges | IDs lies aux objets runtime ou a l'ordre d'affichage |
| P7-SEED-003 | P1 | Variation seed | Lancer seed A puis seed B | Memes versions, seed differente | Distribution differente, budgets identiques | Distribution identique ou budget depasse |
| P7-SEED-004 | P1 | Version seed | Changer `spawn_seed_version` | Meme seed, version differente | Hash/version change explicitement, budget respecte | Changement silencieux sans version ou ID instable non audite |
| P7-BUD-001 | P1 | Densite centre | Inspecter fenetre centre 25x25 | Centre Wave5 | Chunks <= 25; hives/resources/threats <= 25/75/25 | Depassement de plafond |
| P7-BUD-002 | P1 | Densite bords | Inspecter N/S/E/W | Bords 25x25 | Fenetre clamp, aucun hors grille, budgets respectes | Spawn hors monde ou fuite de chunks |
| P7-BUD-003 | P1 | Densite coins | Inspecter NW/NE/SW/SE | Coins 25x25 | Coins valides, fenetre reduite, budgets respectes | Coord negative, coord > limite, trou critique |
| P7-BUD-004 | P1 | Densite 50x50 | Rejouer centre, coins et fenetre dense logique | Grille 50x50 logique | 2500 coords max, active chunks <= 25, pas de terrain 50x50 | Catalogue instancie en scene ou terrain cree |
| P7-EXCL-001 | P1 | BearDen | Tester spawn autour/inside BearDen | Volume BearDen versionne | 0 entite dedans, rejets listes | Entite dans BearDen |
| P7-EXCL-002 | P1 | Eau | Tester candidats eau | Volumes eau versionnes | Candidats rejetes ou deplaces audites | Ressource/ruche/menace sur eau interdite |
| P7-EXCL-003 | P1 | Falaise | Tester candidats falaise | Volumes falaise versionnes | 0 entite en zone interdite | Spawn sur falaise interdite |
| P7-EXCL-004 | P1 | Evenement reserve | Tester zones reservees | Volumes event reserve | 0 entite non-event, rejection reason | Spawn normal dans event reserve |
| P7-EXCL-005 | P1 | Reprojection exclusions | Reprojeter 25x25 -> 50x50 puis revalider | Coord normalisees | Exclusions reappliquees apres reprojection | Entite validee avant reprojection mais exclue apres |
| P7-OVER-001 | P2 | Chevauchement | Calculer distance/hitbox entre entites | Fenetre dense | 0 chevauchement critique; mineurs listes | Selection impossible ou pile illisible |
| P7-OVER-002 | P2 | Priorite proche | Tester selection proche en densite | Fenetre dense | Proche selectionnable, labels non prioritaires reduits | Mauvaise cible ou labels masquent carte |
| P7-CMB-001 | P1 | Tier combat solo | Verifier T1-T4 | Bestiaire T1..T4 | `combat_access=solo` ou equivalent local | T1-T4 bloques sans raison |
| P7-CMB-002 | P1 | Tier combat raid | Verifier T5-T7 | Bestiaire T5..T7 | `combat_access=raid`, solo refuse | T7 solo accepte |
| P7-CMB-003 | P1 | Autorite combat | Lancer preview combat | Solo/raid local | `server=false`, `official_gain=false`, no reward official | Gain officiel ou serveur requis pour preview |
| P7-RES-001 | P2 | Richesse R1 | Inspecter R1 | Ressource pauvre | Libelle/symbole R1 visible | R1 indistinct |
| P7-RES-002 | P2 | Richesse R2 | Inspecter R2 | Ressource moyenne | Libelle/symbole R2 visible | R2 indistinct |
| P7-RES-003 | P2 | Richesse R3 | Inspecter R3 | Ressource riche | Libelle/symbole R3 visible | R3 indistinct |
| P7-RES-004 | P2 | Lisibilite couleur | Simuler lecture sans couleur | R1/R2/R3 | Richesse lisible par texte/symbole | Couleur seule porte l'info |
| P7-REPR-001 | P1 | Normalisation | Verifier coords persistables | Entites spawn | `world_coord_normalized` dans [0,1], version presente | Coord ecran ou version absente |
| P7-REPR-002 | P1 | Reprojection 50x50 | Convertir normalized vers chunk/local 50x50 | Grille 50x50 | Chunk 0..49, local 0..1 | Chunk hors bornes |
| P7-DIAG-001 | P2 | Overlay diagnostic | Lancer inspecteur par defaut | Defaults | Overlay off, aucune mutation spawn | Overlay on par defaut |
| P7-DIAG-002 | P2 | Overlay local | Activer overlay local | Mode QA local | Affiche seed/hash/exclusions/budgets, ne change rien | Overlay modifie distribution |
| P7-AUTH-001 | P1 | Aucun serveur | Inspecter flags/run | Tout scenario | `server=false`, remote absent | Appel serveur/remote |
| P7-AUTH-002 | P1 | Aucun gain officiel | Collecte/combat preview | Ressource/bestiary | `official_gain=false`, `official=false` | Loot/progression officielle |

## Tests negatifs requis

| ID | Cas negatif | PASS attendu | FAIL |
| --- | --- | --- | --- |
| P7-NEG-001 | Meme seed/version donne deux resultats differents | Rejet du build ou diagnostic FAIL determinisme | Variation acceptee |
| P7-NEG-002 | Seed differente depasse 25/75/25 | Diagnostic `DensityBudgetExceeded` ou equivalent | Distribution acceptee |
| P7-NEG-003 | Candidat dans BearDen | Rejet avec `ExclusionVolumeHit` | Spawn accepte |
| P7-NEG-004 | Candidat dans eau/falaise/event reserve | Rejet ou deplacement audite | Spawn silencieux |
| P7-NEG-005 | T7 lance en solo | Refus et suggestion raid | Solo accepte |
| P7-NEG-006 | Coord normalized hors [0,1] apres reprojection | Rejet/clamp audite | Spawn hors grille |
| P7-NEG-007 | Overlay diagnostic on par defaut | Gate FAIL | Etat accepte |
| P7-NEG-008 | `official_gain=true` en local | Gate FAIL | Gain accepte |

## Scenarios obligatoires

| Scenario | Couverture minimale | PASS |
| --- | --- | --- |
| Centre 25x25 | Fenetre centrale, densite normale | Determinisme, budgets, richesse et combat tier OK |
| Bord nord | Clamp Y min/max selon grille | Aucun hors bornes |
| Bord sud | Clamp Y min/max selon grille | Aucun hors bornes |
| Bord ouest | Clamp X min/max selon grille | Aucun hors bornes |
| Bord est | Clamp X min/max selon grille | Aucun hors bornes |
| Coins NW/NE/SW/SE | Fenetre reduite, coords valides | Aucun spawn hors monde |
| Densite max | Fenetre la plus dense | <= 25 ruches, <= 75 ressources, <= 25 menaces |
| BearDen | Volume visible/cache/restaure | 0 entite dedans |
| Eau/falaise/event | Volumes reserves versionnes | 0 entite non autorisee |
| 50x50 logique | Centre, coins, densite | Reprojection sans terrain 50x50 |

## Recu attendu du futur inspecteur

Le futur rapport principal P7 doit fournir un petit recu lisible avec:

```text
WORLD_MAP_SPAWN_DISTRIBUTION_P7_RECEIPT

seed_a:
seed_b:
spawn_seed_version:
exclusion_version:
world_grid_version:

same_seed_same_version_hash_a1:
same_seed_same_version_hash_a2:
same_seed_stable:

different_seed_hash_b:
different_seed_distribution_changed:
different_seed_budgets_preserved:

active_chunks_center/N/S/E/W/NW/NE/SW/SE/dense:
max_hives/resources/threats:

exclusion_hits_bear_den:
exclusion_hits_water:
exclusion_hits_cliff:
exclusion_hits_reserved_event:
accepted_entities_inside_exclusions:

critical_overlaps:
combat_t1_t4_solo:
combat_t5_t7_raid:
richness_r1_r2_r3_readable:
reprojection_50x50_pass:
diagnostic_overlay_default_off:
server:
official_gain:

READY_FOR_P7_VALIDATION=YES/NO
```

## Contre-validation P6

Statut au moment de cette matrice:

- Rapport P6 principal: NON TROUVE dans `Docs/WorldMapRuntimeEntitiesWave1`.
- Rapports/relais P6 trouves: `Docs/BuilderCRelay/WorldMapScenarioDataLayer_TechnicalContract.md`, `Docs/QARelay/WorldMapScenarioDataLayer_QA_Matrix.md`, `Docs/UIRelay/WorldMapScenarioLab_UI_Spec.md`, `Docs/DemoRelay/WorldMapScenarioLab_5MinuteOwnerDemoPlan.md`.
- Contre-validation documentaire P6: PENDING_MAIN_REPORT.

Si un rapport P6 principal apparait, publier avant toute cloture un fichier de contre-validation dans `Docs/QARelay` verifiant au minimum: couverture P6-DATA/P6-SCEN/P6-MAP/P6-BUD/P6-NEG, flags `server=false` et `official_gain=false`, budgets 50x50, regression P1-P5, et absence de modification Unity/PNG/APK/terrain/BearDen.

## Gate

READY_FOR_P7_VALIDATION=YES

Raison: la matrice P7 est complete et actionnable pour le futur inspecteur/generateur local de spawns. Le YES signifie readiness de validation documentaire/spec, pas PASS d'une implementation P7 deja executee.

## Template verdict P7

```text
WORLD_MAP_SPAWN_DISTRIBUTION_QA_VERDICT

Date locale:
Rapport P7 principal lu:
Recu P7 lu:

Exclusions confirmees:
- Unity/scene/PNG/APK:
- Wave5/master terrain/BearDen source:
- Serveur/remote/donnees reelles/gain officiel:

Resultats:
- Meme seed + meme version:
- Seed differente:
- Budgets 25/75/25:
- Exclusions BearDen/eau/falaise/event:
- Chevauchement critique:
- T1-T4 solo / T5-T7 raid:
- R1/R2/R3 lisible:
- Bords/coins 25x25:
- Reprojection 50x50:
- Overlay diagnostic off par defaut:
- server=false / official_gain=false:

Defauts bloquants:
-

Notes:
-

Verdict:
PASS / PASS_WITH_NOTES / FAIL / NOT_RUN

Gate:
READY_FOR_P7_VALIDATION=YES/NO
```
