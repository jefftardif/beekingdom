# M066-CL — Chat Royal : Nouveau groupe fonctionnel

## Diagnostic

La création de groupe (`CreateGroupAsync`) réutilise déjà entièrement les
mécanismes du chat privé (scope `GameServerId`/`WorldId`, format d'ID `"D"`,
résolveur de nom serveur `senderDisplayNameResolver` déjà validé pour le
titre ET les membres) - tous corrigés en M065. Un seul bug restait,
symétrique à celui déjà corrigé pour "Nouvelle discussion" : après création,
l'écran basculait vers l'onglet Groupes de façon synchrone, choisissant le
PREMIER groupe déjà connu au lieu d'attendre le vrai groupe fraîchement créé
(fire-and-forget non synchronisé).

## Correctif

`ChatCreateGroupFromSelection` attend maintenant la création réelle et
resynchronise `chatSelectedConversation` sur l'identifiant retourné -
exactement le même correctif déjà appliqué et validé pour la conversation
privée.

## Vérifié dans le code (pas de test lancé)

- Titre de groupe : vient directement du serveur (`request.Title`), toujours
  rempli à la création, pas de résolution nécessaire.
- Noms des membres : `ChatGroupOperations.DisplayName` côté serveur utilise
  déjà le même résolveur validé - jamais le PlayerId brut.
- Envoi/réception de message : réutilise `ChatSendCurrentToServer` /
  `LivingHiveChatRuntime.SendAsync`, générique à tout type de conversation,
  déjà fonctionnel.
- Recherche de joueurs pour le groupe : même sélecteur (`chatPlayerPicker`)
  que "Nouvelle discussion".

Aucun système Chat parallèle créé. Chat privé non touché.

- Fichier : `HiveViewProductUiPresenter.ChatRoyal.cs`.
- Compilation vérifiée propre. Aucun test, aucun refactor.

Commit local uniquement, aucun push.

READY FOR CEO GROUP RUNTIME RETEST
