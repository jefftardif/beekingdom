# ARCH-240 - Arret des contours devines en code / pipeline visuel obligatoire

Date : 2026-07-12

## Decision Architecte

Les passes actuelles sur les contours de zones ne sont pas acceptables.

Le probleme n'est pas que le jeu ne peut pas afficher un contour. Le probleme est que l'equipe tente de deviner une forme artistique a partir de coordonnees codees a la main.

Cette methode est stoppee.

## Nouvelle regle

A partir de maintenant, les contours des buildings de la ruche ne doivent plus etre inventes directement dans le code.

Ils doivent etre dessines visuellement sur l'image de la ruche avec un outil de dessin vectoriel ou de masque, puis importes dans Unity.

## Outil recommande

Outil auteur recommande : Inkscape.

Raison :
- gratuit;
- fonctionne sur Windows;
- permet de placer l'image de la ruche en fond;
- permet de tracer des courbes Bezier propres autour des zones;
- exporte en SVG;
- permet de nommer chaque path par zone;
- ne force aucune dependance runtime Unity si les paths sont convertis en JSON/polyline.

Alternative acceptable : Figma, Illustrator, Affinity Designer, Krita/GIMP avec masque, ou tout outil capable de produire des chemins vectoriels ou masques propres.

## Pipeline obligatoire

1. Charger l'image premium de la ruche comme image de reference.
2. Dessiner manuellement chaque contour au-dessus de la vraie bordure de cire.
3. Nommer chaque contour par zone :
   - ReserveMiel
   - Administration
   - Nurserie
   - Caserne
   - Recherche
   - Genetique
   - Entrepot
   - Transformation
4. Exporter les contours en SVG ou JSON.
5. Convertir les paths en points normalises dans le repere de l'image.
6. Unity affiche ces points comme contour visible.
7. Unity conserve une hitbox tactile separee, invisible et plus confortable.

## Ce qui est interdit

- Continuer a ajuster des points a l'aveugle dans le code.
- Produire des captures qui prouvent seulement que le code sait dessiner une ligne.
- Declarer READY si la forme ne suit pas visuellement la cire.
- Confondre hitbox tactile et contour visible.
- Utiliser une enveloppe mathematique autour du building.

## Definition de succes

Un contour est acceptable si une personne non technique peut regarder la capture et dire :

> Le contour suit naturellement le bord de la zone.

Il n'est pas acceptable si la reaction est :

> C'est encore une forme approximative dessinee par un programme.

## Assignation

UI-B doit produire ou preparer les contours sources visuels.

Builder-A doit retirer la logique de contours devines et supporter l'import des paths.

Builder-B doit fournir le validateur/importeur et les captures natives apres integration.

Demo-A et QA-A restent bloques tant que le pipeline visuel n'a pas livre de vrais contours.

## Statut

Les validations precedentes BEE-1021/BEE-1022 sont retrogradees en preuves techniques partielles.

Elles ne prouvent pas encore que la demande produit est satisfaite.
