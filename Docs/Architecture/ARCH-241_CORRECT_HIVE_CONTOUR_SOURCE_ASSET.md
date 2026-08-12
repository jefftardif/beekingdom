# ARCH-241 - Correction source officielle pour contours de ruche

Date : 2026-07-12

## Decision

Les images precedemment preparees dans `contours_inkscape` etaient incorrectes et ont ete supprimees.

La source correcte pour les contours de la ruche est maintenant :

`C:\projets\beekingdomgame-master\Assets\BeeKingdom\Playground\Resources\PremiumBeeReference\hive-ui-target.png`

Copie de travail Inkscape :

`C:\projets\beekingdom\contours_inkscape_correct\hive-ui-target_FOR_CONTOURS.png`

Dimensions : 1672 x 941.

## Regle

Aucune validation contour ne doit utiliser :

- `HiveBackground.png`
- `HiveBackground_UnityReady_2048x3072.png`
- les anciennes copies `contours_inkscape`
- les contours generes automatiquement sans tracage visuel manuel

## Gate actuel

Demo/QA restent bloques tant qu'un fichier SVG trace sur la bonne image n'existe pas :

`C:\projets\beekingdom\contours_inkscape_correct\HiveContours.svg`

Les paths doivent etre nommes par zone et suivre la cire visible.
