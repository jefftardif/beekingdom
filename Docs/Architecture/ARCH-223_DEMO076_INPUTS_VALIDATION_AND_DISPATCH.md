# ARCH-223 - DEMO-076 Inputs Validation And Dispatch

Date: 2026-07-12

## Decision

Architecte valide les intrants DEMO-076 pour la vague BEE-941 a BEE-960.

DEMO-076 peut demarrer.

## Intrants valides

- Builder-A: coeur jouable produit BEE-945 a BEE-951.
  - Rapport: `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE945_951_PlayableHiveProductCore_Report.md`
  - Statut: `READY_FOR_DEMO_076_HIVE_PRODUCT_CORE = YES`
- Builder-B: confirmations, refus, evidence continuity, quick QA pack et gate BEE-960.
  - Rapport: `C:/projets/beekingdom/prompts_codex/rapports/BuilderB_BEE952_953_957_959_960_Report.md`
  - Statut: `READY_FOR_DEMO_076_ACTION_EVIDENCE = YES`
- UI-B: lisibilite telephone, menus tablette, matrice gestes.
  - Rapport: `C:/projets/beekingdom/prompt_ui/rapports/UI-B-071_BEE954_956_HIVE_READABILITY_GESTURE_COMFORT.md`
  - Statut: `UI_B_071_READY_FOR_DEMO_QA_SUPPORT = YES`
- Server-A: support non-claim BEE-958.
  - Rapport: `C:/projets/beekingdom/prompt_server/rapports/SERVER-047 - BEE958 Hive Product Non Claim Support Report.md`
  - Statut: `SERVER_047_READY_FOR_DEMO_QA_SUPPORT = YES`

## Objectif DEMO-076

Prouver que la ruche jouable est plus proche d'un produit quotidien:

- debut de session clair;
- collecte et croissance ressources intelligibles;
- capacite/overflow comprehensibles;
- choix upgrade lisible;
- completion upgrade avec reward;
- choix training lisible;
- completion training avec prochaine action;
- panneau armee locale utile;
- confirmations et disabled states non muets;
- recovery court apres refus;
- menus fixes;
- gestes separes;
- textes critiques lisibles;
- manifests coherents.

## Reserves obligatoires

DEMO-076 ne doit pas fermer:

- `PHYSICAL_DEVICE_PROOF = PENDING`;
- installation/lancement APK sur appareil;
- telephone portrait physique;
- tablette paysage physique;
- confort tactile physique.

## Interdictions

- Ne pas relancer carte monde.
- Ne pas creer/debloquer BEE-881.
- Ne pas creer exploration/alliance/guerre/map MMO.
- Ne pas pretendre serveur officiel/live.
- Ne pas pretendre endpoint/save/economie/armee persistante officielle.

## Livrables Demo attendus

Demo-A doit produire:

- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-076_BEE941_960/DEMO-076_Report.md`
- artefacts machine-readable si possible;
- liste explicite preuves presentes/manquantes;
- verdict final `READY_FOR_QA_076 = YES` ou `NO`.

## Chaine suivante

Si `READY_FOR_QA_076 = YES`, lancer QA-A.

Si DEMO-076 bloque, relancer uniquement le role responsable de la preuve manquante.
