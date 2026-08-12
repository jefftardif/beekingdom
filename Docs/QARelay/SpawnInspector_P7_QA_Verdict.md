# Spawn Inspector P7 - QA Verdict

Date locale: 2026-07-15

Role: QA P7 Bee Kingdom, contre-validation documentaire stricte.

## Decision

`Verdict=FAIL`

`QA_P7=FAIL`

`READY_FOR_P8_REGRESSION_EXECUTION=NO`

Le rapport et le recu attestent un resultat local encourageant, mais ils ne couvrent pas les preuves obligatoires de la matrice P7. Ce verdict porte sur la suffisance des preuves disponibles; il ne conclut pas que l'implementation est defectueuse.

## Perimetre de la revue

Sources exclusivement lues:

- `Docs/QARelay/WorldMapSpawnDistribution_QA_Matrix.md`
- `Docs/WorldMapRuntimeEntitiesWave1/SpawnInspectorIntegration_Report.md`
- `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/SpawnInspectorProof/SpawnInspectorProofReceipt.md`
- `Docs/Recovery/BeeKingdom_Relay_Progress.md`

Aucun ancien thread, Unity, scene, PNG, APK, log, serveur, remote ou autre fichier n'a ete consulte ou execute. La revue est donc documentaire et limitee aux quatre sources autorisees.

## Contre-validation

| Domaine | Preuve disponible | Evaluation QA |
| --- | --- | --- |
| Determinisme | Le rapport et le recu declarent `Same seed/version deterministic: PASS`; un seul hash A (`01b78336`) est fourni | **NON DEMONTRE**: absence des deux hashes A1/A2 et d'un comparatif des IDs, positions, tiers, richesses et flags |
| Variation | Hash A `01b78336`, hash B `fef6f1b4`, et changement de distribution declare PASS | **PARTIEL**: variation de hash demontree, mais aucun compte/budget propre a la seed B ni preuve `different_seed_budgets_preserved` |
| Budgets | `25/2/11/7` chunks/ruches/ressources/menaces, sous les plafonds `25/25/75/25` | **PARTIEL**: un echantillon respecte les plafonds; centre, N/S/E/W, quatre coins, densite max et 50x50 logique ne sont pas recus |
| Exclusions | Hits BearDen/eau/falaise/event `0/0/0/25`; `Exclusion zones: PASS` declare | **NON DEMONTRE**: zero hit ne prouve pas l'injection de candidats BearDen/eau/falaise; aucun compte d'entites acceptees dans les exclusions, motif de rejet ou controle apres reprojection |
| Negatifs | Aucun resultat individualise pour `P7-NEG-001` a `P7-NEG-008` | **NON EXECUTE / NON RECU** |
| Chevauchements | Aucun nombre de chevauchements critiques ni resultat de selection proche | **NON DEMONTRE** |
| Combat T1-T7 | Le rapport indique que T1-T7 sont couverts dans la preuve | **NON DEMONTRE**: aucun resultat T1-T4 solo, T5-T7 raid ou refus T7 solo dans le recu |
| Richesse R1/R2/R3 | Le rapport indique que R1/R2/R3 sont couverts dans la preuve | **NON DEMONTRE**: aucune validation texte/symbole ni lecture sans couleur dans le recu |
| Bords, coins et 50x50 | Aucun resultat par bord/coin et aucune coordonnee normalisee/chunk/local | **NON EXECUTE / NON RECU** |
| Overlay diagnostic | Default `OFF` dans le rapport et le recu | **PARTIEL**: default OFF demontre; activation locale sans mutation de distribution non demontree |
| P1-P6 | Le relais declare P1 a P6 PASS; rapport et recu declarent `P1-P6 regression: PASS` et `P1_P6_REGRESSION=NO` | **PASS DOCUMENTAIRE**: `NO` est interprete comme aucune regression detectee; aucun re-run independant n'a ete autorise |
| Serveur et officiel | `server=false`, `official_gain=false`, `SERVER_OR_OFFICIAL_GAIN=NO`; absence de remote declaree dans le perimetre | **PARTIEL**: serveur et gain officiel sont attestes faux, mais `official=false` n'est pas recu et aucune preuve d'absence d'appel remote ou du negatif `official_gain=true` n'est fournie |

## Defauts bloquants

| ID | Defaut | Impact gate |
| --- | --- | --- |
| P7-QA-B01 | Le recu n'instancie pas le format obligatoire: versions seed/exclusion/grille, hashes A1/A2, resultats par fenetre, maxima, entites acceptees dans les exclusions, chevauchements, tiers, richesse et reprojection manquent | Determinisme et couverture P7 non auditables |
| P7-QA-B02 | Aucun des huit tests negatifs requis n'est rapporte individuellement | Gate P7 impossible a fermer |
| P7-QA-B03 | Les exclusions BearDen/eau/falaise ont zero hit sans preuve qu'un candidat interdit a ete soumis; l'evenement a 25 hits sans motifs ni compte accepte | Exclusions obligatoires non validees |
| P7-QA-B04 | Un seul tuple de densite est fourni; aucune couverture centre/bords/coins/dense/50x50 logique | Budgets globaux, clamps et reprojection non valides |
| P7-QA-B05 | Chevauchements, acces combat, lisibilite R1/R2/R3 et invariance de l'overlay ne figurent pas dans le recu | Exigences fonctionnelles P7 non validees |
| P7-QA-B06 | `official=false` et l'absence effective de remote ne sont pas traces dans le recu; le negatif d'autorite n'est pas execute | Autorite locale incomplete |

## Interpretation des gates amont

- `READY_FOR_P7_VALIDATION=YES` dans la matrice signifie que la specification est prete a etre validee, pas que P7 a passe.
- `READY_FOR_OWNER_SPAWN_INSPECTOR_TEST=YES` ouvre un test owner; il ne remplace pas la gate QA P7.
- `P1_P6_REGRESSION=NO` est coherent avec les mentions textuelles `P1-P6 regression: PASS`: aucune regression P1-P6 n'est declaree.
- Les declarations PASS du rapport et du recu ne compensent pas les champs et scenarios obligatoires absents.

## Conditions de nouvelle revue

Une nouvelle preuve doit au minimum fournir:

- les deux executions A1/A2, leurs hashes et le comparatif IDs/positions/tiers/richesses/flags;
- une execution B avec distribution differente et budgets preserves;
- les resultats centre, N/S/E/W, NW/NE/SW/SE, densite max et 50x50 logique;
- des candidats forces dans BearDen, eau, falaise et evenement reserve, avec motifs de rejet et `accepted_entities_inside_exclusions=0`;
- les huit resultats `P7-NEG-001` a `P7-NEG-008`;
- les nombres de chevauchements, les gates T1-T4/T5-T7, la lisibilite R1/R2/R3 et la reprojection;
- la preuve que l'overlay active ne change pas la distribution;
- `server=false`, `official=false`, `official_gain=false` et l'absence de remote.

## Gates finales

`QA_P7=FAIL`

`READY_FOR_P8_REGRESSION_EXECUTION=NO`
