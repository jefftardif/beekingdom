# M082-CL — Chat Royal, refonte premium de la barre d'action

Refonte complète de la barre "Nouvelle discussion / Nouveau groupe /
Recherche / Favoris / Paramètres" :

- **Icônes officielles** intégrées (`nouvelle_discussion.png`,
  `nouveau_groupe.png`, `nouvelle_recherche.png`, `favori.png`,
  `settings.png`), remplaçant les icônes procédurales génériques.
- **Police en gras nettement plus grande** (13-15pt au lieu de 10-11pt).
- **"Simili rectangle" ouvragé retiré** : le rendu `DrawPremiumPanel`
  (grain + contour) est remplacé par un vrai bouton plat arrondi
  (`DrawFlatRoundedRect`, même technique fiable que M080-CL), avec une
  bordure dorée nette pour l'état actif au lieu du contour flou précédent.
- Barre agrandie (54-56px → 74-82px) pour laisser respirer les icônes.

Non touché : backend, données, autres écrans.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre, icônes confirmées importées.

Commit local uniquement, aucun push.
