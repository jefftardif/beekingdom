# ARCH-231 - DEMO-078 Inputs Validation And Dispatch

Date: 2026-07-12

## Decision

Architecte valide les intrants DEMO-078 pour la vague BEE-981 a BEE-1000.

DEMO-078 peut demarrer.

## Intrants valides

- Builder-A: etats T0-T8 et captures fraiches.
  - Rapport: `C:/projets/beekingdom/prompts_codex/rapports/BuilderA_BEE984_992_T0_T8_ScreenshotStates_Report.md`
  - Statut: `READY_FOR_DEMO_078_T0_T8_SCREENSHOT_STATES = YES`
- Builder-B: bundle visuel, manifest, contact sheet/handoff.
  - Rapport: `C:/projets/beekingdom/prompts_codex/rapports/BuilderB_BEE981_983_993_994_998_1000_VisualBundle_Report.md`
  - Statut: `READY_FOR_DEMO_078_VISUAL_BUNDLE = YES`
- UI-B: criteres image/cropping/lisibilite.
  - Rapport: `C:/projets/beekingdom/prompt_ui/rapports/UI-B-073_BEE995_IMAGE_QUALITY_CROPPING_SUPPORT.md`
  - Statut: `UI_B_073_READY_FOR_DEMO_QA_SUPPORT = YES`
- Server-A: guard visuel non-claim serveur.
  - Rapport: `C:/projets/beekingdom/prompt_server/rapports/SERVER-049 - BEE997 Server Live Claim Visual Guard Report.md`
  - Statut: `SERVER_049_READY_FOR_DEMO_QA_SUPPORT = YES`

## Preuves visuelles detectees

Le dossier source contient des PNG reels:

- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO078_T0_SessionStart.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO078_T1_ActionConfirmation.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO078_T2_DisabledState.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO078_T3_RefusalRecovery.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO078_T4_UpgradeCompletion.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO078_T5_TrainingCompletion.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO078_T6_LocalArmyInspection.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO078_T7_GestureUiFixed.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO078_T8_NonClaimsScopeLock.png`
- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000_Source/DEMO-078_T0_T8_ContactSheet.png`

## Attention Demo-A

Builder-B mentionne aussi des images historiques `SourceDEMO0xx`. DEMO-078 doit officialiser en priorite les captures fraiches `DEMO078_T*.png`.

Si une contact sheet est composee d'images historiques, elle doit etre declaree comme historique/support, pas comme preuve fraiche.

## Objectif DEMO-078

Prouver visuellement:

- T0 session start/resources;
- T1 action confirmation;
- T2 disabled state;
- T3 refusal/recovery;
- T4 upgrade completion;
- T5 training completion;
- T6 local army inspection;
- T7 gesture UI fixed;
- T8 non-claims scope lock;
- contact sheet image si utilisable;
- manifest visuel coherent.

## Reserves obligatoires

DEMO-078 ne doit pas fermer:

- `PHYSICAL_DEVICE_PROOF = PENDING`;
- APK install/launch reel;
- phone portrait physique;
- tablet landscape physique;
- gestes tactiles physiques;
- preuve device reelle.

## Interdictions

- Ne pas relancer carte monde.
- Ne pas creer/debloquer BEE-881.
- Ne pas creer exploration/alliance/guerre/map MMO.
- Ne pas pretendre serveur officiel/live.
- Ne pas pretendre endpoint/save/economie/armee persistante officielle.

## Livrables Demo attendus

Demo-A doit produire:

- `C:/projets/beekingdom/prompt_demo/rapports/DEMO-078_BEE981_1000/DEMO-078_Report.md`
- artefacts machine-readable si possible;
- liste explicite images fraiches, historiques/support et manquantes;
- verdict final `READY_FOR_QA_078 = YES` ou `NO`.

## Chaine suivante

Si `READY_FOR_QA_078 = YES`, lancer QA-A.

Si DEMO-078 bloque, relancer uniquement le role responsable de la preuve manquante.
