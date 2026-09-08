# M091-CL — Chat Royal, boutons du composer vraiment premium

Les boutons 😊 et ➤ (envoyer) utilisaient encore le rendu "panneau
premium" (grain + coins décoratifs), qui débordait mal à cette petite
taille ("simili rectangle" signalé par le CEO).

Remplacés par le même bouton plat arrondi (`DrawFlatRoundedRect`) que
partout ailleurs dans Chat Royal depuis M080, avec bordure dorée nette
quand le panneau d'émoticônes est ouvert.

Non touché : envoi, backend, autres écrans.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
