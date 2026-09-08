# M078-CL — Chat Royal, correction des halos invisibles

## Cause

Le halo de M077-CL débordait volontairement au-delà du rectangle de la
carte - mais les listes CANAUX/DISCUSSIONS sont dessinées dans un
`GUI.BeginScrollView` dont le clipping ne laisse rien passer hors du
rectangle de contenu exact. Le halo était donc entièrement invisible
(confirmé par le CEO).

## Correctif

- Nouvelle texture `selection-ring-glow` : alpha fort près du bord, qui
  retombe vite vers le centre (l'inverse de `honey-glow-pool`, centrée).
- Le halo remplit maintenant exactement le rectangle de la carte, jamais
  au-delà.
- Le panneau premium de la carte sélectionnée est dessiné légèrement
  rétréci (4px de chaque côté) par-dessus, laissant apparaître un anneau
  doré visible tout autour sans jamais sortir des bornes de la carte.

Non touché : backend, données, autres écrans, zone de clic (reste la
carte entière).

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
