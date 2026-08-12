# ARCH-236 - Correction DEMO-079 / preuve native AFTER obligatoire

Date : 2026-07-12

## Constat

DEMO-079 a ete officialisee avec le verdict :

`READY_FOR_QA_079 = NO`

Rapport :
`C:\projets\beekingdom\prompt_demo\rapports\DEMO-079_BEE1001_1020\DEMO-079_Report.md`

Le runtime des contours pixel-perfect est present et les garde-fous Builder/UI sont valides, mais la preuve visuelle reste insuffisante pour QA.

## Cause du blocage

DEMO-A n'a pas pu produire de vraies captures Play Mode natives `AFTER_*`.

Les overlays comparatifs produits montrent l'intention et les donnees de calibration, mais ils ne prouvent pas que le joueur voit reellement le contour pixel-perfect dans Unity pendant l'execution.

Pour un gate pixel-perfect, une preuve overlay ne suffit pas.

## Decision Architecte

Le blocage doit etre corrige par une passe de tooling de preuve, pas par une nouvelle vague gameplay.

Objectif : rendre la capture native DEMO-079 fiable et reproductible.

## Tache corrective immediate

### Role assigne

Builder-B

### Identifiant de travail

BEE-1021 - Native Play Mode AFTER capture for DEMO-079

### Objectif

Produire une methode fiable qui genere de vraies captures Play Mode natives de la ruche avec le contour calibrable affiche, sans carte monde, sans BEE-881, sans serveur officiel/live.

### Livrables attendus

Builder-B doit produire :

`C:\projets\beekingdom\prompts_codex\rapports\BuilderB_BEE1021_NATIVE_AFTER_CAPTURE_Report.md`

Avec verdict :

`READY_FOR_DEMO_079_NATIVE_AFTER_CAPTURE = YES` ou `NO`

Et, si possible, les captures sources dans :

`C:\projets\beekingdom\prompt_demo\rapports\DEMO-079_BEE1001_1020_Source\NativeAfter\`

## Critere de succes

La solution est acceptable seulement si elle produit au minimum :

- `AFTER_ReserveMiel.png`
- `AFTER_Administration.png`
- `AFTER_Nurserie.png`
- `AFTER_Caserne.png`
- `AFTER_Recherche.png`
- `AFTER_Genetique.png`
- `AFTER_PanZoom_Alignment.png`
- un manifeste listant les conditions de capture

Ces images doivent provenir d'une capture Unity/Play Mode ou d'un rendu runtime equivalent, pas d'un overlay externe dessine apres coup.

## Prochaine etape

Si Builder-B livre `READY_FOR_DEMO_079_NATIVE_AFTER_CAPTURE = YES`, Demo-A devra relancer DEMO-079 et produire un nouveau verdict.

QA-A reste en attente.
