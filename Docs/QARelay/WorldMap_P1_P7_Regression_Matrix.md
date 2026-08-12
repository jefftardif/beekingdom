# World Map P1-P7 - Regression Matrix

Date locale: 2026-07-15

Objet: consolider la baseline P1-P6 et la contre-validation P7 a partir des quatre sources documentaires autorisees.

## Legende

| Statut | Sens |
| --- | --- |
| PASS DOCUMENTAIRE | Statut concordant dans les sources, sans reexecution independante |
| PARTIEL | Une partie du critere est prouvee, mais la couverture obligatoire est incomplete |
| NON DEMONTRE | Declaration presente sans donnees suffisantes pour reproduire la conclusion |
| NON EXECUTE / NON RECU | Aucun resultat du test obligatoire dans les sources |
| FAIL GATE | Une exigence bloquante de la gate n'est pas satisfaite par les preuves |

## Baseline de regression P1-P7

| Phase | Baseline declaree dans le relais | Signal P7 | Statut QA | Risque residuel |
| --- | --- | --- | --- | --- |
| P1 | `WORLD_MAP_50X50_READINESS_P1: PASS` | P1-P6 preserves; regression P1-P6 PASS | PASS DOCUMENTAIRE | Rapport/recu P1 non relus dans ce mandat |
| P2 | `P2_MAP_READING_TOOLS: PASS` | Regression P1-P6 PASS | PASS DOCUMENTAIRE | Rapport/recu P2 non relus |
| P3 | `P3_INTERACTION_POLISH: PASS` | Regression P1-P6 PASS | PASS DOCUMENTAIRE | Rapport/recu P3 non relus |
| P4 | `P4_AUTOMATED_REGRESSION: PASS` | Regression P1-P6 PASS | PASS DOCUMENTAIRE | Resultats detailles P4 non relus |
| P5 | `P5_DEMO_PACKAGE: PASS` | Regression P1-P6 PASS | PASS DOCUMENTAIRE | Package owner non relu |
| P6 | `P6_RUNTIME_SCENARIO_DATA_LAYER: PASS` | Regression P1-P6 PASS | PASS DOCUMENTAIRE | Rapport/recu P6 non relus |
| P7 | `P7_SPAWN_INSPECTOR: PASS` | Rapport et recu P7 disponibles | **FAIL GATE** | Preuves obligatoires P7 incompletes, notamment negatifs, exclusions et scenarios |

Conclusion baseline: aucune regression P1-P6 n'est declaree par les sources. Cette baseline documentaire ne suffit pas a ouvrir P8 tant que P7 n'a pas une gate QA PASS.

## Matrice des exigences P7

| IDs matrice | Domaine | Resultat recu | Contre-validation QA |
| --- | --- | --- | --- |
| P7-SEED-001 | Meme seed/version | PASS declare, hash A unique | NON DEMONTRE: paire A1/A2 absente |
| P7-SEED-002 | Stabilite IDs apres camera | Stabilite de format d'ID decrite dans le rapport | NON DEMONTRE: aucun aller-retour centre/voisin/centre recu |
| P7-SEED-003 | Variation seed | A `01b78336` != B `fef6f1b4`; changement declare PASS | PARTIEL: comptes et conservation des budgets de B absents |
| P7-SEED-004 | Changement de version seed | Aucun resultat | NON EXECUTE / NON RECU |
| P7-BUD-001 | Fenetre centre | Tuple `25/2/11/7` sous plafonds | PARTIEL: la fenetre n'est pas identifiee explicitement comme centre |
| P7-BUD-002 | Bords N/S/E/W | Aucun resultat par bord | NON EXECUTE / NON RECU |
| P7-BUD-003 | Coins NW/NE/SW/SE | Aucun resultat par coin | NON EXECUTE / NON RECU |
| P7-BUD-004 | 50x50 logique | Aucun resultat de fenetre logique | NON EXECUTE / NON RECU |
| P7-EXCL-001 | BearDen | 0 hit, PASS global declare | NON DEMONTRE: candidat force et rejet absents |
| P7-EXCL-002 | Eau | 0 hit, PASS global declare | NON DEMONTRE: candidat force et rejet absents |
| P7-EXCL-003 | Falaise | 0 hit, PASS global declare | NON DEMONTRE: candidat force et rejet absents |
| P7-EXCL-004 | Evenement reserve | 25 hits | PARTIEL: aucun motif de rejet ni compte d'entites acceptees |
| P7-EXCL-005 | Exclusions apres reprojection | Aucun resultat | NON EXECUTE / NON RECU |
| P7-OVER-001/002 | Chevauchement et selection proche | Aucun resultat | NON EXECUTE / NON RECU |
| P7-CMB-001/002/003 | T1-T4 solo, T5-T7 raid, autorite combat | Couverture T1-T7 declaree | NON DEMONTRE: decisions solo/raid et refus T7 non recus |
| P7-RES-001/002/003/004 | R1/R2/R3 et lecture sans couleur | Couverture R1-R3 declaree | NON DEMONTRE: aucun resultat texte/symbole/sans couleur |
| P7-REPR-001/002 | Normalisation et 50x50 | Aucun resultat | NON EXECUTE / NON RECU |
| P7-DIAG-001 | Overlay OFF par defaut | OFF dans rapport et recu | PASS DOCUMENTAIRE |
| P7-DIAG-002 | Overlay local sans mutation | Aucun comparatif de distribution | NON EXECUTE / NON RECU |
| P7-AUTH-001 | Aucun serveur/remote | `server=false`; remote exclu par declaration de perimetre | PARTIEL: aucune trace d'absence effective d'appel remote |
| P7-AUTH-002 | Aucun gain/etat officiel | `official_gain=false` | PARTIEL: `official=false` et le negatif associe sont absents |

## Matrice des tests negatifs

| ID | Injection requise | Preuve disponible | Statut QA |
| --- | --- | --- | --- |
| P7-NEG-001 | Incoherence meme seed/version | Aucune injection ni diagnostic de rejet | NON EXECUTE / NON RECU |
| P7-NEG-002 | Depassement 25/75/25 | Aucun depassement force ni `DensityBudgetExceeded` | NON EXECUTE / NON RECU |
| P7-NEG-003 | Candidat dans BearDen | BearDen a 0 hit | NON DEMONTRE |
| P7-NEG-004 | Candidat eau/falaise/event | Eau/falaise a 0 hit; event a 25 hits sans decision detaillee | NON DEMONTRE |
| P7-NEG-005 | T7 en solo | Aucun refus recu | NON EXECUTE / NON RECU |
| P7-NEG-006 | Coordonnee normalisee hors bornes | Aucun rejet/clamp recu | NON EXECUTE / NON RECU |
| P7-NEG-007 | Overlay ON par defaut | Seul le default OFF nominal est recu | NON EXECUTE / NON RECU |
| P7-NEG-008 | `official_gain=true` en local | Seul `official_gain=false` nominal est recu | NON EXECUTE / NON RECU |

## Couverture des scenarios obligatoires

| Scenario | Statut | Motif |
| --- | --- | --- |
| Centre 25x25 | PARTIEL | Un tuple sous budget, sans identification explicite ni detail fonctionnel |
| Bords N/S/E/W | NON EXECUTE / NON RECU | Aucun clamp ou resultat par bord |
| Coins NW/NE/SW/SE | NON EXECUTE / NON RECU | Aucune fenetre reduite ni coordonnee recue |
| Densite max | NON EXECUTE / NON RECU | Aucun maximum multi-fenetres |
| BearDen | NON DEMONTRE | Zero hit ne prouve pas le cas negatif impose |
| Eau/falaise/event | PARTIEL | Event touche 25 fois; eau/falaise non exercees; aucun compte accepte |
| 50x50 logique | NON EXECUTE / NON RECU | Reprojection, chunks 0..49 et local 0..1 absents |

## Autorite et exclusions de livraison

| Controle | Etat |
| --- | --- |
| `server=false` | Recu |
| `official_gain=false` | Recu |
| `official=false` | Non recu |
| Remote absent | Declare dans le rapport, non trace par une preuve d'execution |
| Unity/PNG/APK pendant cette revue QA | Non consultes et non executes |
| Terrain 50x50/master terrain/BearDen source | Exclus de cette revue et declares preserves par le rapport |

## Gates

`P1_P6_BASELINE_DOCUMENTARY=PASS`

`P7_REQUIRED_NEGATIVE_TESTS=NOT_RUN`

`P7_REQUIRED_SCENARIOS=NOT_PROVEN`

`P7_SERVER_AND_OFFICIAL_AUTHORITY=PARTIAL`

`QA_P7=FAIL`

`READY_FOR_P8_REGRESSION_EXECUTION=NO`
