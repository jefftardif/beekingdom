# ARCH-237 - Directive utilisateur / contours organiques de frontiere de cire

Date : 2026-07-12

## Reference utilisateur

L'utilisateur a fourni une capture annotee comparant :

- contour jaune : forme produite par Builder-A;
- contour bleu pale : forme dessinee a main levee par l'utilisateur dans Paint.

Conclusion Architecte : le contour jaune reste trop technique, trop anguleux et trop eloigne de la vraie frontiere visuelle. Le contour bleu pale illustre la direction attendue : suivre la cire et les irregularites naturelles de la zone.

## Correction de vision

La demande n'est pas simplement :

> ajouter plus de points a un polygone.

La demande est :

> dessiner un contour organique qui epouse la frontiere de cire du building, comme si le joueur selectionnait reellement la zone naturelle de la ruche.

## Definition acceptable

Un contour acceptable :

- suit la bordure de cire visible;
- inclut les creux, bosses et courbes organiques;
- reste lisible sans cacher l'asset;
- ne coupe pas a travers l'interieur du building;
- ne ressemble pas a une enveloppe technique;
- ne ressemble pas a un cercle, hexagone ou polygone generique;
- peut etre une spline ou une polyline dense;
- peut etre lisse visuellement, mais doit respecter les irregularites de l'image;
- reste separe de la hitbox tactile invisible.

## Definition refusee

Un contour refuse :

- contour simplifie en 8-12 segments anguleux;
- bordure qui traverse la cire ou coupe les details internes;
- halo large qui masque l'image;
- forme mathematique uniforme;
- contour approximatif uniquement centre sur le building;
- preuve overlay sans capture runtime native.

## Priorite des zones

Passer en priorite sur les zones visibles et jugeables :

1. Entrepot / reserve situee en haut-gauche de la capture utilisateur;
2. Transformation / zone centrale basse entouree en jaune;
3. Administration;
4. Reserve miel;
5. Nurserie;
6. Caserne;
7. Recherche;
8. Genetique.

## Directive Builder-A

Builder-A doit remplacer les contours simplistes par des contours organiques calibres, proches de l'exemple bleu pale.

Le runtime peut conserver une hitbox tactile confortable, mais le contour visible doit etre beaucoup plus proche de la frontiere de cire.

## Directive Builder-B

Builder-B doit continuer la preuve native AFTER, mais ne doit pas faire passer la preuve finale tant que Builder-A n'a pas livre les nouveaux contours organiques.

Builder-B peut preparer l'outil de capture et le protocole, puis relancer les captures apres livraison Builder-A.

## Gate qualite

DEMO-079 ne peut pas passer a QA tant que :

- les contours visibles ne suivent pas les frontieres organiques;
- les captures AFTER natives existent;
- les crops permettent de comparer l'alignement a 2x ou 3x;
- le rapport indique clairement que l'ancien contour jaune est remplace par un contour organique proche de la reference utilisateur.
