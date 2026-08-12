# ARCH-235 - Validation des entrees DEMO-079 / contours pixel-perfect

Date : 2026-07-12

## Contexte

Priorite produit active : les contours des buildings de la ruche doivent suivre les frontieres visuelles des zones au pixel pres. Les halos circulaires, les polygones grossiers et les overlays generiques ne sont plus acceptables pour la selection principale.

Cette validation couvre les entrees produites pour la vague BEE-1001 a BEE-1020.

## Livrables valides

### Builder-A

Rapport :
`C:\projets\beekingdom\prompts_codex\rapports\BuilderA_BEE1001_1007_1010_PixelPerfectContourRuntime_Report.md`

Verdict :
`READY_FOR_DEMO_079_PIXEL_CONTOUR_RUNTIME = YES`

Validation Architecte :
- Runtime de contours calibrables ajoute.
- 14 zones de ruche inventoriees.
- Separation contour visible / hitbox tactile invisible introduite.
- Selection par hitbox conservee pour le confort tactile.
- Rendu du contour branche sur le repere de l'asset ruche afin de rester aligne avec pan/zoom.
- Aucune carte monde, aucun BEE-881, aucun serveur officiel live.

Reserve :
- Les points de contour restent calibrables; la precision finale doit etre confirmee par preuve visuelle et QA.

### Builder-B

Rapport :
`C:\projets\beekingdom\prompts_codex\rapports\BuilderB_BEE1009_1011_1015_1016_1018_1020_Report.md`

Verdict :
`READY_FOR_DEMO_079_PIXEL_CONTOUR_PROOF_GUARDS = YES`

Validation Architecte :
- Garde anti-halo generique documentee.
- Plan de screenshots avant/apres fourni.
- Versioning des donnees de contours fourni.
- Handoff QA fourni.
- Aucune carte monde, aucun BEE-881, aucune preuve appareil physique fermee abusivement.

### UI-B

Rapport :
`C:\projets\beekingdom\prompt_ui\rapports\UI-B-074_PIXEL_PERFECT_CONTOUR_VISUAL_DIRECTION.md`

Verdict :
`UI_B_074_READY_FOR_DEMO_QA_SUPPORT = YES`

Validation Architecte :
- Direction visuelle claire : contour dessine pour la ruche, pas pose par-dessus elle.
- Le contour doit suivre la cire, rester fin, lisible, premium et ne pas masquer l'asset.
- La hitbox ne doit jamais etre visible.

## Preuves source disponibles pour DEMO

Dossier :
`C:\projets\beekingdom\prompt_demo\rapports\DEMO-079_BEE1001_1020_Source\`

Fichiers attendus disponibles :
- `DEMO-079_BEE1001_1007_1010_PixelContourRuntime_Manifest.md`
- `DEMO-079_BEE1001_1007_1010_PixelContourRuntime_Summary.json`
- `DEMO-079_BeforeAfterContourScreenshotPlan.md`
- `DEMO-079_ContourDataVersioningTemplate.json`
- `DEMO-079_PixelContourProofGuardsManifest.json`
- `DEMO-079_PixelContour_QA_Handoff.md`

## Decision Architecte

Les entrees Builder-A, Builder-B et UI-B sont suffisantes pour lancer DEMO-079.

DEMO-A doit maintenant produire une preuve visuelle comparative qui repond a une seule question produit :

Est-ce que les contours de selection suivent maintenant les zones de ruche de facon suffisamment precise pour remplacer les anciens halos/polygones grossiers?

## Tache DEMO-A

DEMO-A doit creer :
`C:\projets\beekingdom\prompt_demo\rapports\DEMO-079_BEE1001_1020\DEMO-079_Report.md`

Avec le verdict final obligatoire :
`READY_FOR_QA_079 = YES` ou `NO`

La preuve doit inclure :
- avant/apres contour historique vs contour calibrable;
- au moins les zones P0 : Reserve miel, Administration, Nurserie, Caserne, Recherche, Genetique;
- preuve que le contour visible suit la zone et que la hitbox reste invisible;
- preuve pan/zoom : le contour reste aligne avec l'asset ruche;
- preuve qu'aucun halo circulaire generique ne remplace le contour;
- mention explicite que la preuve reste locale/demo, sans serveur officiel live;
- mention explicite que la carte monde et BEE-881 ne sont pas dans le perimetre.

## Tache QA-A suivante

QA-A attend DEMO-079.

Si `READY_FOR_QA_079 = YES`, QA-A devra valider :
- precision visuelle des contours;
- absence de halo/polygone grossier;
- lisibilite en paysage tablette et portrait telephone;
- maintien du confort tactile;
- non-regression ruche jouable produit.
