# M065-CL — Stop Using PlayerId In Chat UI

Cause du résidu : la liste de résultats de "Nouvelle discussion" (où vit
`entry.DisplayName`) ne survit pas à la fermeture du sélecteur - le cache de
noms déjà ajouté (`ChatPlayerPickerController.displayNames`) n'était alimenté
que par une recherche, jamais par le clic "Discuter" lui-même.

Correctif : au clic "Discuter", le nom déjà connu (`entry.DisplayName`) est
maintenant enregistré dans ce même répertoire (`RememberDisplayName`), donc
`ChatDirectoryDisplayName` - déjà utilisé par les 3 emplacements - le résout
de façon fiable.

Vérifié dans le code (pas de test lancé) : titre de conversation, liste
DISCUSSIONS et auteur des messages reçus lisent tous `conv.Title` /
`ChatDirectoryDisplayName(...)`, jamais le PlayerId brut.

- Fichiers : `ChatPlayerPickerController.cs`, `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Backend, envoi, connexion, layout : non touchés.
- Compilation vérifiée propre. Aucun test, aucun audit, aucun refactor.

Commit local uniquement, aucun push.

READY FOR CEO RETEST
