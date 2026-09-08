# M081-CL — Chat Royal, avatar du joueur sur ses propres messages

Les messages reçus avaient un avatar rond avec initiales à gauche, mais les
messages du joueur (bulle dorée) n'en avaient aucun. Ajout d'un avatar rond
symétrique à droite de sa propre bulle, mêmes initiales/texture que
l'interlocuteur (`DrawChatAvatar`). Largeur de bulle et calcul de hauteur de
ligne (`ChatMessageBaseHeight`) ajustés pour réserver la même place que côté
interlocuteur.

Non touché : backend, données, autres écrans.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
