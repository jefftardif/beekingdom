# ARCH-190 - QA-068 gate avance et dispatch BEE-832/BEE-833

Date: 2026-07-12
Responsable: Architect
Priorite: Ruche jouable produit, pas carte monde

## Rapports lus

- `C:\projets\beekingdom\QA\QA_DEMO_068_BEE828_835_VALIDATION.md`
- `C:\projets\beekingdom\prompt_demo\rapports\DEMO-068_BEE828_835\DEMO-068_Report.md`
- `C:\projets\beekingdom\prompts_codex\rapports\BuilderA_BEE828_831_Report.md`
- `C:\projets\beekingdom\prompts_codex\rapports\BuilderB_BEE836_839_Report.md`
- `C:\projets\beekingdom\prompts_codex\rapports\BuilderC_BEE834_835_Report.md`
- `C:\projets\beekingdom\prompts_codex\BEE-832_Right_Panel_Density_Product_Polish_Framework.md`
- `C:\projets\beekingdom\prompts_codex\BEE-833_Disabled_Reason_Readability_Placement_Framework.md`
- `C:\projets\beekingdom\prompts_codex\BEE-840_Playable_Hive_Product_Advance_Gate_Before_World_Map_Framework.md`

## Decision Architecte

QA-068 donne `PASS_WITH_RESERVES`. Le gate avance.

Les reserves ne bloquent pas l'etape suivante parce qu'elles concernent principalement la preuve tactile physique, le polish portrait final et la distinction support/runtime. BEE-828 a BEE-831 sont acceptables pour avancer vers la finition produit du panneau droit.

## Travail a lancer maintenant

### Builder-A

Implementer BEE-832 et BEE-833 dans la ruche jouable locale.

Objectif: rendre le panneau droit moins dense et placer les raisons de blocage dans le flux de lecture normal.

Builder-A ne doit pas relancer la carte monde, ne doit pas creer de serveur officiel, ne doit pas declarer de sauvegarde/economie/armee officielle.

### Builder-C

Preparer tests, matrices et preuves automatisees pour BEE-832/BEE-833, sans modifier le runtime principal.

### Builder-B

Preparer le support de gate BEE-840: checklist de fermeture de vague avant carte monde, lignes de manifeste DEMO/QA et non-claims.

## Ordre

- Builder-A, Builder-B et Builder-C peuvent travailler en parallele.
- Demo-A attend Builder-A.
- QA-A attend Demo-A.
- Planner reste en attente tant que BEE-840 n'est pas validee; BEE-841 demeure bloquee.

## Garde-fous

- Priorite absolue: ruche jouable produit.
- Ne pas relancer carte monde.
- Ne pas faire de claim serveur officiel/live/save/economie/armee persistante.
- Garder le statut local preview visible.
- Toute preuve doit distinguer runtime implemente, support documentaire et serveur futur.
