# ARCH-216 - DEMO-075 Inputs Validation And Dispatch

Date: 2026-07-12

## Decision

Architecte valide les intrants DEMO-075 pour la vague BEE-921 a BEE-940.

DEMO-075 peut demarrer.

## Intrants valides

- Builder-A: BEE-925 a BEE-930, boucle quotidienne locale de ruche jouable.
  - Rapport: `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE925_930_DailyHiveLoop_Report.md`
  - Statut: `READY_FOR_DEMO_075_DAILY_HIVE_LOOP = YES`
- Builder-B: BEE-933, BEE-934, BEE-938, BEE-939, BEE-940, support evidence/readiness.
  - Rapport: `C:/projets/beekingdom/prompts_codex/rapports/BuilderB_BEE933_934_938_940_Report.md`
  - Statut: `READY_FOR_DEMO_075_EVIDENCE_SUPPORT = YES`
- Builder-C: BEE-921 a BEE-924, support APK/device gate.
  - Rapport: `C:/projets/beekingdom/prompts_codex/rapports/BuilderC_BEE921_924_APKDeviceGate_Report.md`
  - Statut: `READY_FOR_DEMO_075_DEVICE_APK_SUPPORT = YES`
- UI-B: UI-070, confort lecture/toucher pour tablette paysage et telephone portrait.
  - Rapport: `C:/projets/beekingdom/prompt_ui/rapports/UI-B-070_HIVE_APP_READINESS_COMFORT_SUPPORT.md`
  - Statut: `UI_B_070_READY_FOR_BUILDER_DEMO_QA_SUPPORT = YES`
- Server-A: SERVER-046, non-claim evidence et continuity.
  - Rapport: `C:/projets/beekingdom/prompt_server/rapports/SERVER-046 - Hive App Readiness Non Claim Evidence Carry Forward Report.md`
  - Statut: `SERVER_046_READY_FOR_DEMO_QA_SUPPORT = YES`

## Reserve obligatoire

Cette validation ne ferme pas la preuve physique sur telephone/tablette si DEMO-075 ne possede pas d'artefacts reels d'appareil.

DEMO-075 doit distinguer explicitement:

- simulation locale;
- preuve demo;
- build APK;
- preuve physique device;
- serveur officiel futur.

Si la preuve physique device manque, le verdict doit rester honnete: local/demo ready, physical device proof pending.

## Scope interdit

DEMO-075 ne doit pas relancer:

- carte monde;
- BEE-881;
- serveur officiel live;
- endpoint officiel;
- sauvegarde officielle;
- economie officielle;
- armee persistante officielle;
- claim production.

## Objectif DEMO-075

Prouver que la ruche jouable produit a progresse vers une experience quotidienne exploitable:

- ressources qui augmentent;
- collecte locale lisible;
- amelioration de batiment avec pending, completion, cout reserve et cout depense une seule fois;
- entrainement de troupes avec queue, arrivee et compteur d'armee locale;
- inspection d'armee locale;
- refus avec cause et recuperation;
- boutons fonctionnels/non muets;
- menus permanents fixes;
- tablette paysage et telephone portrait pris en compte;
- aucun zoom des menus;
- scope sans carte monde.

## Tache envoyee

Demo-A doit produire:

- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-075_BEE921_940/DEMO-075_Report.md`
- artefacts machine-readable si possible;
- liste explicite des preuves presentes/manquantes;
- verdict final `READY_FOR_QA_075 = YES` ou `NO`.

## Chaine suivante

Si `READY_FOR_QA_075 = YES`, lancer QA-A sur DEMO-075.

Si DEMO-075 bloque sur preuve manquante mais les corrections runtime sont presentes, relancer uniquement le role responsable de la preuve manquante.
