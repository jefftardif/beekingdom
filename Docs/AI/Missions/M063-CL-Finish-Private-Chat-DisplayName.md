# M063-CL — Finish Private Chat DisplayName Fix

Codex (M060→M062-CX) avait déjà résolu la majorité du problème et laissé le
correctif final non commité, avec son propre rapport `M063-CX-Private-Chat-
DisplayName.md` déjà rédigé. Vérifié : le code compile sans erreur, réutilise
bien le cache `ChatPlayerPickerController.ResolveDisplayName` déjà alimenté par
la recherche de "Nouvelle discussion", et couvre les 3 emplacements demandés
(liste des conversations, titre de la conversation, auteur au-dessus des
messages reçus) via `ChatPrivateDisplayName`/`ChatDirectoryDisplayName`,
partagés par les trois endroits.

Rien à ajouter ni corriger : commité tel quel.

- Fichiers : `ChatPlayerPickerController.cs`, `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Backend, envoi, connexion, layout : non touchés.
- Compilation vérifiée propre. Aucun test lancé (consigne explicite).
- Limite déjà documentée par Codex : un ID jamais croisé par une recherche
  reste non résolu (aucun endpoint serveur de résolution par ID).

Commit local uniquement, aucun push.

READY FOR CEO RETEST
