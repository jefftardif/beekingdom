# M079-CL — Chat Royal, fond des bulles de messages

Les bulles utilisaient le rendu "panneau premium" ouvragé (grain, contour
or, coins décorés) - trop chargé pour de vrais messages, et ne
correspondait pas à la référence (fonds plats et arrondis).

Remplacé par une texture de rectangle arrondi générique (masque de
distance, réutilisable pour les deux couleurs) :
- messages reçus : gris/bleu sombre plat
- mes messages : doré/miel plat

Non touché : disposition du texte (auteur/heure), backend, autres écrans.
Les halos de sélection (canaux/discussions) restent en attente de
validation, non retouchés dans cette passe.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
