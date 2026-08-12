# World Map P7/P8 Owner Proof Storyboard

Date locale: 2026-07-15
Responsable: reviewer Demo P7 Bee Kingdom

## Objet

Ce storyboard relie:

- la demo owner P7 deja soutenue par le rapport et le recu;
- la future preuve owner P8, uniquement apres validation responsive et
  completion du manifeste.

Cette revue est documentaire. Aucun Unity, PNG ou APK n'a ete ouvert, produit
ou modifie.

## Gates

READY_FOR_OWNER_P7_DEMO=YES
READY_FOR_OWNER_P7_DEMO_QA_CLOSED=YES
READY_FOR_OWNER_P8_PROOF_WHEN_VALIDATED=YES
P8_VALIDATED_NOW=NO

Semantique: le premier gate autorise le parcours owner P7 local. Le second dit
que ce storyboard est pret a piloter la collecte P8 quand elle aura ete
validee; il ne declare ni preuve responsive actuelle ni P8 termine. Le gate
`READY_FOR_OWNER_P7_DEMO_QA_CLOSED` est ferme car le verdict read-only
`QA_P7_REREVIEW=PASS` est publie.

## Manifeste des sources et sorties

| ID | Artefact | Chemin ou etat | Usage |
| --- | --- | --- | --- |
| SRC-P7-01 | Rapport P7 | C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave1\SpawnInspectorIntegration_Report.md | Verdict et faits |
| SRC-P7-02 | Recu P7 detaille | C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapRuntimeEntitiesWave1\SpawnInspectorProof\SpawnInspectorProofReceipt.md | Valeurs observees et gates |
| SRC-P7-03 | Cloture evidence QA | C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave1\SpawnInspector_QAClosure_Report.md | B01-B06 fermes cote producteur |
| SRC-P7-04 | Verdict QA precedent | C:\projets\beekingdomgame-master\Docs\QARelay\SpawnInspector_P7_QA_Verdict.md | Ancien verdict `QA_P7=FAIL`, leve par rereview |
| SRC-P7-05 | Verdict QA rereview | C:\projets\beekingdomgame-master\Docs\QARelay\SpawnInspector_P7_QA_ReReview.md | `QA_P7_REREVIEW=PASS`; `READY_FOR_P8_REGRESSION_EXECUTION=YES` |
| SRC-P7-06 | Parcours owner | C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave1\Owner_5Minute_SpawnInspectorDemo.md | Cadence 5 minutes |
| OUT-P7-01 | Plan relay P7 | C:\projets\beekingdomgame-master\Docs\DemoRelay\WorldMapSpawnInspector_5MinuteOwnerDemoPlan.md | Plan consolide |
| OUT-P7P8-01 | Storyboard | C:\projets\beekingdomgame-master\Docs\DemoRelay\WorldMap_P7_P8_OwnerProofStoryboard.md | Relay P7 vers P8 |
| QA-P7-RR | Verdict QA rereview | C:\projets\beekingdomgame-master\Docs\QARelay\SpawnInspector_P7_QA_ReReview.md | Gate owner QA ferme |
| MAN-P7 | Manifeste de captures P7 | NON_REFERENCE_DANS_LES_SOURCES_AUTORISEES | Ne pas inventer |
| MAN-P8 | Manifeste owner proof P8 | A_PRODUIRE_ET_VALIDER | Gate P8 courant non valide |
| RSP-P8 | Preuve responsive | NON_DOCUMENTEE_DANS_LES_SOURCES_AUTORISEES | Validation P8 requise |

## Donnees fixes a afficher

| Donnee | Valeur relayable | Portee |
| --- | --- | --- |
| Version | spawn_v1 | Preview locale |
| Seed A | 738921 | Preview locale |
| Seed A, passage 1 | f17362b9 | Hash A1 publie |
| Seed A, passage 2 | f17362b9 | Hash A2 publie apres centre-voisin-centre |
| Seed B | 918337 / 7b8adab4 | Seed et hash publies |
| Version alternative | spawn_v2_proof / ab507cde | Variation versionnee |
| Compteurs centre 25x25 | 25 / 2 / 11 / 7 | chunks / hives / resources / threats |
| Densest 25x25 | 25 / 22 / 50 / 19 | Sous limites 25/25/75/25 |
| Densest 50x50 logique | 25 / 14 / 40 / 14 | Aucun terrain 50x50 cree |
| Exclusion hits | 0 / 0 / 0 / 25 | BearDen / water / cliff / event |
| Exclusions forcees | 4/4 PASS, acceptes=0 | BearDen/eau/falaise/event reserve |
| Negatifs | 8/8 PASS | P7-NEG-001 a P7-NEG-008 |
| Budgets densite | PASS | Centre, bords, coins, densest et 50x50 logique |
| Overlay | OFF par defaut | Rapport et recu |
| Serveur | false | Local/demo |
| Officiel | false | Local/demo |
| Gain officiel | false | Local/demo |
| Remote calls | 0 | Local/demo |

## Storyboard P7 - 5 minutes

### P7-00 - Ouverture locale

Temps: 0:00-0:35

- Cadre: World Map Wave5 preservee, panneau SPAWN INSPECTEUR visible.
- Action: montrer LOCAL - APERCU NON OFFICIEL.
- Preuve: overlay diagnostic OFF par defaut.
- Carton oral: server=false; official_gain=false.
- Stop: ne pas continuer si l'overlay demarre ON ou si le badge local manque.

### P7-01 - Seed A, premier passage

Temps: 0:35-1:25

- Action: regenerer l'apercu local avec Seed A et spawn_v1.
- Affichage attendu: hash f17362b9.
- Affichage attendu: 25 chunks, 2 ruches, 11 ressources, 7 menaces.
- Detail utile: ID preview, famille, type/tier, chunk et coordonnee normalisee.
- Limite: ce sont des donnees de preview locale, jamais des donnees reelles.

### P7-02 - Seed A, second passage

Temps: 1:25-2:10

- Action: regenerer sans changer seed, version ou contexte.
- Affichage attendu: hash f17362b9 a nouveau.
- Comparaison attendue: memes IDs, positions, tiers et richesses.
- Appui documentaire: A1/A2 count, IDs, positions, tiers, richness et flags
  PASS dans le recu detaille.
- Stop: tout ecart A1/A2 annule la demonstration deterministe.

### P7-03 - Seed B

Temps: 2:10-2:55

- Action: changer uniquement la seed et regenerer.
- Affichage attendu: seed 918337, hash 7b8adab4.
- Comparaison attendue: 7b8adab4 est different de f17362b9.
- Appui documentaire: Different seed distribution changed: PASS.
- Controle: Density budgets reste PASS.

### P7-04 - Exclusions

Temps: 2:55-3:45

- Action: activer l'overlay pour inspection.
- Montrer: BearDen, water, cliff et event sous forme diagnostique.
- Valeurs a relayer exactement: 0 / 0 / 0 / 25.
- Verdict a relayer: Forced exclusions PASS, `accepted_entities_inside_exclusions=0`.
- Discipline: event=25 reste un Exclusion hit; les sources ne le nomment pas
  violation.
- Controle visuel: aucun overlay ne doit etre presente comme peinture terrain.

### P7-05 - Budgets et couverture

Temps: 3:45-4:25

- Montrer: 25 / 2 / 11 / 7.
- Verdict: Density budgets: PASS.
- Couverture relayable: centre, N/S/E/W, NW/NE/SW/SE, densest, 50x50 logique.
- Couverture interaction: T1-T4 solo, T5-T7 raid, T7 solo refuse.
- Couverture lisibilite: R1/R2/R3 distincts sans couleur.

### P7-06 - Responsive, limite de preuve

Temps: 4:25-4:45

- Dire clairement: responsive non documente dans les sources P7 autorisees.
- Ne montrer aucun artefact non manifeste.
- Relayer vers les plans P8-01 et P8-02 ci-dessous.

### P7-07 - Fermeture

Temps: 4:45-5:00

- Action: couper l'overlay diagnostic.
- Etat final attendu: OFF.
- Carton final: LOCAL - APERCU NON OFFICIEL.
- Carton final: server=false; official_gain=false.
- Verdict: READY_FOR_OWNER_P7_DEMO=YES.
- Verdict QA owner: READY_FOR_OWNER_P7_DEMO_QA_CLOSED=YES.
- Ne pas dire que P8 est valide.

## Storyboard P8 - preuve a valider

### P8-00 - Entree de manifeste

- Creer une entree de manifeste avant chaque artefact de preuve.
- Attribuer un ID stable et un chemin reel.
- Ne jamais inscrire un chemin prevu comme s'il existait deja.
- Lier chaque artefact a un resultat de validation PASS ou FAIL.

### P8-01 - Vue large responsive

- Enregistrer la largeur et la hauteur reelles du viewport.
- Montrer le panneau, le badge, les compteurs et l'action locale.
- Verifier absence de chevauchement, texte coupe, controle hors ecran ou carte
  masquee de facon incoherente.
- Enregistrer l'overlay OFF a l'ouverture.
- Enregistrer A1, A2 et B, avec trois lignes de manifeste distinctes.

### P8-02 - Vue etroite responsive

- Enregistrer la largeur et la hauteur reelles du viewport.
- Verifier que les controles restent visibles, lisibles et utilisables.
- Verifier que le contenu se replie sans collision.
- Verifier que le badge local et les compteurs gardent leur sens.
- Repeter au minimum le controle A/A/B ou documenter explicitement le partage
  de preuve deterministe avec P8-01.
- Enregistrer l'overlay OFF a la fermeture.

### P8-03 - Exclusions et budgets

- Manifester les valeurs BearDen / water / cliff / event.
- Conserver 0 / 0 / 0 / 25 comme reference P7, sans promettre que P8 sera
  identique avant observation.
- Enregistrer le verdict Exclusion zones.
- Enregistrer les compteurs chunks / hives / resources / threats.
- Enregistrer Density budgets et la source des seuils si ceux-ci sont affiches.

### P8-04 - Limites locales

- Enregistrer server=false.
- Enregistrer official_gain=false.
- Montrer LOCAL - APERCU NON OFFICIEL.
- Confirmer absence de serveur, remote, device, APK et donnee reelle.
- Confirmer qu'aucun terrain 50x50, PNG terrain, master terrain ou BearDen
  source n'a ete introduit.

### P8-05 - Cloture et validation

- Verifier overlay OFF a l'ouverture et a la fermeture.
- Verifier que toutes les entrees du manifeste ont un chemin reel.
- Verifier que chaque preuve a un viewport, une seed/version, un hash, des
  compteurs, des exclusions, les deux flags locaux et un resultat.
- Faire signer ou enregistrer la validation responsive.
- Seulement ensuite annoncer la preuve owner P8.

## Schema minimal du manifeste P8

| Champ | Exigence |
| --- | --- |
| proof_id | Unique et stable |
| artifact_path | Chemin reel, jamais previsionnel |
| viewport | Largeur x hauteur observees |
| seed_pass | A1, A2, B ou N/A |
| version | spawn_v1 ou valeur observee |
| distribution_hash | Valeur observee |
| counts | chunks / hives / resources / threats |
| exclusions | BearDen / water / cliff / event |
| density_budgets | PASS ou FAIL, avec source du seuil |
| overlay_open | OFF requis |
| overlay_close | OFF requis |
| server | false requis |
| official_gain | false requis |
| responsive_result | PASS ou FAIL |
| validator | Identite ou role du validateur |
| validated_at | Horodatage de validation |

## Criteres responsive P8

PASS exige:

- au moins une vue large et une vue etroite, dimensions reelles manifestees;
- aucun chevauchement incoherent;
- aucun texte essentiel coupe;
- aucun controle requis hors ecran sans acces;
- panneau, badge, compteurs et regeneration locale utilisables;
- carte encore lisible;
- overlay OFF a l'ouverture et a la fermeture;
- aucune divergence non expliquee dans A/A/B;
- manifeste complet et valide.

Les dimensions cibles ne sont pas inventees ici: elles doivent venir de la
validation P8.

## Exclusions de la preuve

- Pas de serveur ou remote.
- Pas de gain officiel.
- Pas de donnee reelle.
- Pas d'APK ou de device.
- Pas de terrain 50x50.
- Pas de PNG terrain ou master terrain.
- Pas de modification de la source BearDen.
- Pas de regression P1-P6 acceptee.
- Pas de claim responsive sans validation.
- Pas de claim de manifeste complet sans chemins reels.

## Matrice de decision

| Gate | Etat | Motif |
| --- | --- | --- |
| Determinisme A/A | PASS P7 detaille | A1/A2 f17362b9, IDs/positions/tiers/richesses/flags PASS |
| Variation B | PASS P7 detaille | B 7b8adab4 different de A et budgets preserves |
| Exclusions | PASS P7 detaille | Forces 4/4 PASS, acceptes=0 |
| Negatifs | PASS P7 detaille | P7-NEG-001 a P7-NEG-008 PASS |
| Fenetres | PASS P7 detaille | Centre, bords, coins, densest, 50x50 logique |
| Budgets | PASS P7 detaille | Max 25/22/50/19 sous limites 25/25/75/25 |
| Overlay OFF par defaut | PASS P7 declare | Rapport et recu |
| Overlay OFF final | Action de demo requise | A manifester en P8 |
| Autorite locale | PASS P7 detaille | server=false, official=false, official_gain=false, remote_calls=0 |
| QA P7 rereview | PASS | `QA_P7_REREVIEW=PASS`; `READY_FOR_P8_REGRESSION_EXECUTION=YES` |
| Responsive | NON VALIDE | Absent des sources autorisees |
| Manifeste de captures | NON VALIDE | Non reference dans les sources autorisees |
| Limites local/demo | PASS P7 declare | server=false, official_gain=false |

## Verdict final

READY_FOR_OWNER_P7_DEMO=YES
READY_FOR_OWNER_P7_DEMO_QA_CLOSED=YES
READY_FOR_OWNER_P8_PROOF_WHEN_VALIDATED=YES
P8_VALIDATED_NOW=NO
