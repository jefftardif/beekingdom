# M089-CL — Chat Royal, barre de recherche premium

Refonte de la barre de recherche des DISCUSSIONS (activée depuis l'onglet
"Recherche") :

- Loupe premium (`loupe.png`, déjà utilisée pour la recherche de joueurs)
  à gauche du champ.
- Placeholder "Rechercher..." affiché quand le champ est vide et non focus.
- Contour doré arrondi autour du champ (`DrawFlatRoundedRect`).
- Nouveau bouton "Rechercher" ajouté devant "Effacer", les deux en style
  plat premium.
- Largeur du champ recalculée pour laisser la place aux deux boutons - il
  ne les chevauche plus.

Le filtrage réagit déjà en direct à chaque frappe (aucun changement côté
logique) ; "Rechercher" referme simplement le focus clavier.

Non touché : backend, autres écrans.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
