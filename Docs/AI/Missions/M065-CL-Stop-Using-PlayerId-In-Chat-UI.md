# M065-CL — Stop Using PlayerId In Chat UI

## Retest CEO : toujours KO malgré le correctif précédent

Cause réelle, différente de ce qui était supposé : `chatPlayerPicker` (et
son cache de noms) est **recréé à zéro** par
`MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession()`
- une méthode appelée par plusieurs bootstraps HiveMap sans rapport avec le
chat, potentiellement plusieurs fois par session. Chaque appel efface le nom
qu'on venait d'apprendre via "Nouvelle discussion", que ce soit juste après
la recherche ou plus tard en rouvrant la conversation.

## Correctif

Le nom appris (`entry.DisplayName`) est maintenant stocké dans un
dictionnaire stable au niveau de l'écran Chat Royal
(`chatKnownDisplayNames`), jamais recréé par ce mécanisme sans rapport.
`ChatDirectoryDisplayName` - déjà utilisé par les 3 emplacements demandés -
le consulte en premier.

## Vérifié dans le code (pas de test lancé)

Titre de conversation, liste DISCUSSIONS et auteur des messages reçus lisent
tous `conv.Title` / `ChatDirectoryDisplayName(...)`, jamais le PlayerId brut.

- Fichiers : `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Backend, envoi, connexion (`MobileAccountSessionRuntimeBootstrap.cs` non
  touché), layout : non touchés.
- Compilation vérifiée propre. Aucun test, aucun audit, aucun refactor.

Commit local uniquement, aucun push.

READY FOR CEO RETEST
