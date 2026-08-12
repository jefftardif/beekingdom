# ARCH-233 - Pixel Perfect Building Zone Contours Priority

Date: 2026-07-12

## Decision

Nouvelle priorite produit: les contours des buildings/zones de la ruche doivent etre alignes au pixel pres sur les vraies frontieres visuelles des zones.

Les polygones approximatifs, cercles, halos generiques ou contours qui coupent a travers les murs de cire ne sont plus acceptables comme solution finale.

## Probleme observe

La preuve actuelle montre un contour de selection qui ne suit pas exactement la frontiere de la zone:

- segments trop droits;
- sommets approximatifs;
- ligne qui traverse ou ignore certaines courbes de cire;
- impression de prototype au lieu d'interface premium.

## Exigence produit

Chaque building/zone importante doit avoir:

- une zone cliquable correspondant a la zone visuelle reelle;
- un contour visible qui suit la frontiere de cire;
- une selection claire sans masquer l'asset;
- une tolerance tactile utilisable sans deformer le contour visuel;
- une separation entre hitbox technique et outline visuel;
- une preuve screenshot avant/apres.

## Principe architectural

Deux couches distinctes sont requises:

1. **Contour visuel pixel-perfect**
   - suit le bord exact de la zone;
   - peut etre defini par points, spline, mask ou texture d'overlay;
   - ne doit pas etre simplifie en cercle.

2. **Hitbox interaction confortable**
   - peut etre legerement plus large pour le tactile;
   - ne doit pas changer l'apparence du contour;
   - doit rester coherente apres zoom/pan.

## Contraintes

- Ne pas relancer la carte monde.
- Ne pas debloquer BEE-881.
- Ne pas inventer une nouvelle UI globale.
- Ne pas remplacer l'image de ruche par un simple background non interactif.
- Ne pas casser les preuves DEMO-078 T0-T8.
- Ne pas fermer la preuve physique device.

## Equipes concernees

Builder-A: Oui

- implementation runtime des contours et hitboxes.

Builder-B: Oui

- manifests, preuves visuelles, comparaison avant/apres.

UI-B: Oui

- criteres visuels: epaisseur, glow, lisibilite, selection, hover/tap.

Demo-A: Oui

- preuve screenshot des contours alignes.

QA-A: Oui

- validation pixel alignment et non-regression zoom/pan.

Server-A: Non

- aucun impact serveur officiel.

## Definition de done

Une zone selectionnee est acceptable seulement si:

- le contour suit clairement la bordure reelle visible;
- aucun segment majeur ne traverse l'interieur du building;
- les coins suivent les angles/arrondis principaux;
- le contour reste aligne apres zoom/pan local;
- les menus restent fixes;
- la zone reste cliquable confortablement;
- une capture prouve l'alignement.

## Suite attendue

Planner doit creer une vague dediee apres le gate courant:

- inventaire des zones;
- format de donnees des contours;
- outil ou fichier de definition des polygones;
- separation contour visuel / hitbox tactile;
- integration runtime;
- validation Demo/QA par screenshot.
