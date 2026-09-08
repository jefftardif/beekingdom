# M077-CL — Chat Royal, surbrillance dorée des cartes sélectionnées

Le canal et la discussion sélectionnés n'avaient qu'un contour subtil.
Ajout d'un halo doré derrière la carte (texture `honey-glow-pool`, déjà
utilisée ailleurs dans l'interface premium, ex. `DrawSplashLogo`) + bordure
pleinement opaque, pour le même effet de surbrillance que la référence.

S'applique aux deux listes (`DrawChatChannelsPane`, `DrawChatConversationsPane`)
via un helper partagé `DrawSelectedRowGlow`.

Non touché : backend, données, autres écrans.

- Fichier : `HiveViewProductUiPresenter.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
