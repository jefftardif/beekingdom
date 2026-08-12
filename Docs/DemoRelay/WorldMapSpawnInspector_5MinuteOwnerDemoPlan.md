# World Map Spawn Inspector - 5 Minute Owner Demo Plan

Date locale: 2026-07-15
Revue: Demo P7 Bee Kingdom

## Verdict

READY_FOR_OWNER_P7_DEMO=YES
READY_FOR_OWNER_P7_DEMO_QA_CLOSED=YES
READY_FOR_OWNER_P8_PROOF_WHEN_VALIDATED=YES
P8_VALIDATED_NOW=NO

Le gate P7 reste positif pour une demo owner locale fondee sur le rapport
d'integration, le recu P7 detaille, le rapport de cloture des preuves QA et le
parcours owner. Le gate de cloture QA owner est maintenant `YES`: le verdict
read-only `QA_P7_REREVIEW=PASS` est publie dans
`Docs/QARelay/SpawnInspector_P7_QA_ReReview.md`. Le gate P8 est strictement
conditionnel: le storyboard est pret a recevoir la preuve P8, mais aucune
validation responsive ni aucun manifeste de captures P8 n'est fourni dans les
sources autorisees.

## Perimetre de cette revue

- Documentation seulement.
- Aucun lancement ou changement Unity.
- Aucun PNG lu, produit ou modifie.
- Aucun APK lu, produit ou modifie.
- Aucun serveur, remote, device ou donnee reelle.
- Aucun ancien thread.
- Aucune preuve hors des documents locaux listes ci-dessous.

## Chemins P7 reels

- P7_SPAWN_INSPECTOR_REPORT:
  C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave1\SpawnInspectorIntegration_Report.md
- P7_SPAWN_INSPECTOR_RECEIPT:
  C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapRuntimeEntitiesWave1\SpawnInspectorProof\SpawnInspectorProofReceipt.md
- P7_QA_EVIDENCE_CLOSURE_REPORT:
  C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave1\SpawnInspector_QAClosure_Report.md
- P7_QA_REREVIEW_VERDICT:
  C:\projets\beekingdomgame-master\Docs\QARelay\SpawnInspector_P7_QA_ReReview.md
- P7_OWNER_5_MINUTE_DEMO:
  C:\projets\beekingdomgame-master\Docs\WorldMapRuntimeEntitiesWave1\Owner_5Minute_SpawnInspectorDemo.md
- P7_DEMO_RELAY_PLAN:
  C:\projets\beekingdomgame-master\Docs\DemoRelay\WorldMapSpawnInspector_5MinuteOwnerDemoPlan.md
- P7_PROOF_DIRECTORY_DECLARED_BY_RECEIPT_PATH:
  C:\projets\beekingdomgame-master\Docs\BuilderA\WorldMapRuntimeEntitiesWave1\SpawnInspectorProof
- P7_CAPTURE_MANIFEST:
  NOT_REFERENCED_IN_AUTHORIZED_P7_SOURCES

Le dossier de preuve est derive du chemin reel du recu. Cette revue ne pretend
pas qu'il contient d'autres artefacts. Aucun chemin de capture ou de manifeste
visuel n'est invente.

## Manifeste documentaire P7

| Element | Source | Statut de revue |
| --- | --- | --- |
| Integration | SpawnInspectorIntegration_Report.md | PASS declare |
| Recu | SpawnInspectorProofReceipt.md | PASS declare |
| Cloture evidence QA | SpawnInspector_QAClosure_Report.md | P7-QA-B01..B06 fermes cote producteur |
| Verdict QA rereview | SpawnInspector_P7_QA_ReReview.md | `QA_P7_REREVIEW=PASS`; gate owner QA ferme |
| Parcours owner | Owner_5Minute_SpawnInspectorDemo.md | Disponible |
| Seed A/A/B | Rapport QA closure + recu detaille | A1/A2/B hashes explicites |
| Exclusions | Rapport QA closure + recu detaille | Forces BearDen/eau/falaise/event PASS |
| Budgets densite | Rapport QA closure + recu detaille | Centre, bords, coins, densest, 50x50 logique PASS |
| Negatifs | Rapport QA closure + recu detaille | P7-NEG-001..008 PASS |
| Overlay diagnostic | Rapport + recu | OFF par defaut |
| Responsive | Non documente | A valider en P8 |
| Manifeste de captures | Non reference | A produire et valider en P8 |

## Faits P7 relayables

### Seed A/A/B

- Seed A: `738921`.
- Seed A, passage 1: hash `f17362b9`.
- Seed A, passage 2 apres centre-voisin-centre: hash `f17362b9`.
- A1/A2: compte PASS, IDs PASS, positions PASS, tiers PASS, richesses PASS,
  flags PASS.
- Seed B: `918337`, hash `7b8adab4`.
- Seed B: `25/2/9/3`, budgets preserves PASS.
- Different seed distribution changed: PASS.
- Version alternative `spawn_v2_proof`: hash `ab507cde`, variation versionnee
  PASS.

Le recu detaille publie maintenant A1 et A2 separement. Le manifeste P8 devra
quand meme enregistrer A1, A2 et B comme artefacts distincts si une preuve
visuelle responsive est produite.

### Exclusions

- Ordre du compteur: BearDen / water / cliff / event.
- Exclusion hits observes dans la cloture: 0 / 0 / 0 / 25.
- Candidats forces BearDen/eau/falaise/evenement reserve: soumis 1 chacun,
  rejetes 1 chacun, acceptes 0.
- Motifs: `ExclusionVolumeHit:BearDen`, `ExclusionVolumeHit:water`,
  `ExclusionVolumeHit:cliff`, `ExclusionVolumeHit:reserved_event`.
- `accepted_entities_inside_exclusions=0`.
- Forced exclusions: PASS.
- Les exclusions sont diagnostiques: contours ou hachures, jamais peinture du
  terrain.

Le nombre event=25 est relaye exactement comme un compteur Exclusion hits. Les
sources ne le nomment pas violation; la demo ne doit pas le requalifier.

### Budgets

- Centre 25x25: active chunks / hives / resources / threats = 25 / 2 / 11 / 7.
- Densest 25x25: 25 / 22 / 50 / 19.
- Densest 50x50 logique: 25 / 14 / 40 / 14.
- Maxima observes: chunks=25, ruches=22, ressources=50, menaces=19.
- Limites: chunks<=25, ruches<=25, ressources<=75, menaces<=25.
- Density budgets: PASS.
- Couverture fenetres: centre, N/S/E/W, NW/NE/SW/SE et densest sur 25x25 et
  50x50 logique: PASS.
- 50x50: 2500 coordonnees logiques, cache chunks `25 -> 25`, aucun terrain
  cree.

### Interface et limites

- Panneau: SPAWN INSPECTEUR.
- Badge: LOCAL - APERCU NON OFFICIEL.
- Version locale: spawn_v1.
- Action: Regenerer apercu local - Jamais officiel.
- Diagnostic overlay default: OFF.
- Overlay OFF/ON: `f17362b9` / `f17362b9`, distribution inchangee.
- Negative tests: P7-NEG-001 a P7-NEG-008, 8/8 PASS.
- Chevauchements critiques: 0; proximites mineures: 8; selection proche PASS.
- T1-T4 solo PASS; T5-T7 raid PASS; T7 solo refuse PASS.
- `[R1] pauvre`, `[R2] moyen`, `[R3] riche`: lisibles sans couleur, PASS.
- server=false.
- official=false.
- official_gain=false.
- remote_calls=0.
- P1-P6 regression: PASS.

## Parcours owner - 5 minutes

### 0:00-0:35 - Cadre local et overlay OFF

1. Ouvrir le parcours owner sur la scene declaree.
2. Montrer SPAWN INSPECTEUR et le badge local.
3. Montrer que l'overlay diagnostic est OFF par defaut.
4. Dire: demo locale, aucun serveur, aucun gain officiel.

Gate oral: server=false et official_gain=false.

### 0:35-1:25 - Seed A, passage 1

1. Conserver la version spawn_v1.
2. Regenerer l'apercu local avec Seed A.
3. Relever le hash `f17362b9`.
4. Montrer les compteurs 25 / 2 / 11 / 7.
5. Montrer un detail d'entite sans presenter la preview comme donnee reelle.

### 1:25-2:10 - Seed A, passage 2

1. Ne changer ni seed, ni version, ni contexte.
2. Regenerer une seconde fois.
3. Comparer au hash A du premier passage.
4. Attendre `f17362b9` et les memes IDs, positions, tiers, richesses et flags.

Stop demo si le hash ou la distribution differe.

### 2:10-2:55 - Seed B

1. Changer uniquement la seed.
2. Regenerer l'apercu local.
3. Relever le hash `7b8adab4`.
4. Montrer qu'il differe de `f17362b9`.
5. Confirmer Density budgets: PASS.

### 2:55-3:45 - Exclusions

1. Activer l'overlay uniquement pour cette inspection.
2. Montrer BearDen, water, cliff et event reserve comme diagnostics.
3. Relayer exactement 0 / 0 / 0 / 25.
4. Montrer Forced exclusions: PASS et `accepted_entities_inside_exclusions=0`.
5. Ne pas presenter event=25 comme une violation.

### 3:45-4:25 - Budgets et couverture

1. Montrer 25 chunks actifs, 2 ruches, 11 ressources et 7 menaces au centre.
2. Montrer Density budgets: PASS.
3. Rappeler que la cloture couvre centre, bords, coins, densest et 50x50
   logique.
4. Rappeler T1-T4 solo, T5-T7 raid, T7 solo refuse et R1/R2/R3 lisibles sans
   couleur.

### 4:25-4:45 - Frontiere responsive P8

1. Expliquer que le responsive n'est pas documente par le rapport ou le recu P7.
2. Ne pas le presenter comme PASS.
3. Pointer le storyboard P8 pour la validation large/etroite et le manifeste.

### 4:45-5:00 - Fermeture propre

1. Remettre l'overlay diagnostic sur OFF.
2. Repeter LOCAL - APERCU NON OFFICIEL.
3. Repeter server=false et official_gain=false.
4. Conclure sur le verdict P7 et la cloture QA owner, sans annoncer P8 comme
   valide.

## Responsive et manifeste P8 requis

Le proof P8 devra:

- enregistrer les largeurs de viewport reellement validees;
- montrer une vue large et une vue etroite sans chevauchement ni contenu coupe;
- garder le panneau, le badge, les compteurs et l'action locale utilisables;
- enregistrer A1, A2 et B comme trois entrees distinctes;
- enregistrer les exclusions et les budgets observes;
- montrer l'overlay OFF a l'ouverture et a la fermeture;
- fournir un manifeste avec identifiant, chemin, viewport, seed/version, hash,
  compteurs, exclusions, etat overlay, server, official_gain et resultat;
- etre valide avant toute affirmation de preuve owner P8.

## Limites local/demo

- Preview locale uniquement.
- Aucun serveur ou remote.
- Aucun gain officiel.
- Aucune donnee reelle.
- Aucun APK ou device.
- Aucun terrain 50x50, PNG terrain, master terrain ou modification BearDen.
- Wave5 25x25, 625 tuiles, BearDen et P1-P6 declares preserves.
- Les overlays d'exclusion sont diagnostiques et ne modifient pas le terrain.

## Conditions d'arret

Arreter ou degrader la demo si:

- A1 et A2 ne correspondent pas;
- B ne differe pas de A;
- Exclusion zones ou Density budgets n'est plus PASS;
- l'overlay n'est pas OFF au depart ou ne peut pas etre coupe a la fin;
- server ou official_gain devient vrai;
- le presenter affirme P8 valide sans preuve responsive/manifeste P8;
- le presenter tente de qualifier responsive ou manifeste comme valides sans
  nouvelle preuve.

## Gates finaux

READY_FOR_OWNER_P7_DEMO=YES
READY_FOR_OWNER_P7_DEMO_QA_CLOSED=YES
READY_FOR_OWNER_P8_PROOF_WHEN_VALIDATED=YES
P8_VALIDATED_NOW=NO
