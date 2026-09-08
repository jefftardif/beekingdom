# M088-CL — Chat Royal, refonte de la fenêtre Paramètres

Les "onglets" (couleur d'accent, règle d'invitation) utilisaient le rendu
"panneau premium" (grain + coins décoratifs), qui débordait mal sur des
rectangles étroits ("les onglets sortent mal").

Remplacés par le même bouton plat arrondi (`DrawFlatRoundedRect`) déjà
utilisé partout ailleurs dans Chat Royal (M080/M082/M086), avec une bordure
dorée nette pour l'état actif. Police nettement plus grande sur tout
l'écran (titres, options, texte d'aide, titre de fenêtre, bouton fermer).

Non touché : backend, logique de préférence (accent local, règle
d'invitation côté serveur), autres écrans.

- Fichier : `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
