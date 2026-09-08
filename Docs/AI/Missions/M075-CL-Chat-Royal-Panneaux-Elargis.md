# M075-CL — Chat Royal, panneaux CANAUX/DISCUSSIONS élargis

Deux correctifs :

1. **Largeur minimale des panneaux augmentée** : CANAUX (150→230px min) et
   DISCUSSIONS (210→280px min), pour laisser assez d'espace au texte et aux
   badges.
2. **Bug de rognage trouvé en creusant le premier problème** : les deux
   listes ne réservaient jamais la largeur de la barre de défilement
   verticale dans leurs cartes - dès que le contenu dépassait la hauteur
   visible, la barre chevauchait/rognait le bord droit des cartes (le badge
   non-lus notamment, visible sur la capture). Corrigé en réservant 18px,
   même motif déjà utilisé pour le panneau Membres.

Non touché : backend, données, autres écrans.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
