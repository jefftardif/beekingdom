# M083-CL — Chat Royal, curseur texte orphelin

## Diagnostic (hypothèse la plus probable, non confirmée en Play Mode)

L'icône décrite ("I" flottant à gauche du losange, dans l'en-tête de
conversation) correspond à un artefact classique d'IMGUI Unity : un champ de
texte (composer, recherche) fermé alors qu'il avait encore le focus clavier
laisse `GUIUtility.keyboardControl` pointer vers un ID de contrôle qui n'est
plus dessiné - le curseur clignotant peut alors se raccrocher au premier
contrôle qui réutilise cet ID à l'affichage suivant, ailleurs à l'écran.

Ce n'est **pas** un élément du monde 3D qui traverse - `chatScreenOpen` est
déjà dans `IsAnyOverlayBlocking()`, donc la barre de construction en espace
monde ne peut pas se dessiner par-dessus.

## Correctif

`ReleaseGuiInputCapture()` (déjà utilisé ailleurs, ex.
`CloseActiveMainMenuPanel`, pour exactement cette famille de bug) appelé à
la fermeture de l'écran Chat (`CloseChatScreen`) et de ses sous-modaux
(`CloseChatRoyalOverlays`).

**À confirmer par le CEO** : cette hypothèse n'a pas pu être vérifiée en
Play Mode de mon côté (pas d'accès à une capture IMGUI en direct). Si le
curseur orphelin persiste après ce correctif, il faudra une capture plus
large montrant l'icône en contexte pour continuer le diagnostic.

- Fichiers : `HiveViewProductUiPresenter.cs`, `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Compilation vérifiée propre.

Commit local uniquement, aucun push.
