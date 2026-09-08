# M065-CL — Stop Using PlayerId In Chat UI

## Retest CEO : toujours KO malgré le correctif précédent

Cause réelle, différente de ce qui était supposé : `chatPlayerPicker` (et
son cache de noms) est **recréé à zéro** par
`MobileAccountSessionRuntimeBootstrap.TryConfigureGameplayForActiveSession()`
- une méthode appelée par plusieurs bootstraps HiveMap sans rapport avec le
chat, potentiellement plusieurs fois par session. Chaque appel efface le nom
qu'on venait d'apprendre via "Nouvelle discussion", que ce soit juste après
la recherche ou plus tard en rouvrant la conversation.

## Correctif (affichage du nom)

Le nom appris (`entry.DisplayName`) est maintenant stocké dans un
dictionnaire stable au niveau de l'écran Chat Royal
(`chatKnownDisplayNames`), jamais recréé par ce mécanisme sans rapport.
`ChatDirectoryDisplayName` - déjà utilisé par les 3 emplacements demandés -
le consulte en premier.

## Nettoyage complet de la base de production, puis deux vrais bugs serveur découverts

Après un nettoyage complet (à la demande du CEO) des tables chat en
production, "Discuter" a cessé de créer la moindre conversation. Deux bugs
serveur réels et distincts, confirmés par les logs `unhandled-exceptions.log`
du serveur :

1. **Création de conversation privée toujours 400 muet** : le client n'a
   jamais rempli `GameServerId`/`WorldId` sur la requête de création (requis,
   non-nullable côté serveur) - masqué tout ce temps parce que le tiroir de
   chat Alliance (seule preuve fonctionnelle antérieure) n'a jamais créé de
   conversation depuis zéro, et parce que les conversations privées de test
   existaient déjà en base (simple retrouvaille idempotente, jamais une
   vraie création). Corrigé en réutilisant exactement le même scope déjà
   utilisé par `CreateGroupAsync` (`ConfigureGroupScope`), avec le même
   garde-fou de refus propre si le scope n'est pas encore connu.
2. **`GET /chat/v1/invitations` plantait (500)** : `Invalid column name
   'InviterAcknowledged'` - la colonne existe dans le script de migration
   094 mais celui-ci est protégé par "si la table n'existe pas déjà" ; la
   table existait déjà en production avant l'ajout de cette colonne au
   script, donc elle n'a jamais été appliquée. Corrigé par une nouvelle
   migration additive (095) qui ajoute la colonne/l'index manquants,
   appliquée manuellement par le CEO en attendant le prochain déploiement
   automatisé.

## Vérifié dans le code (pas de test automatisé lancé)

Titre de conversation, liste DISCUSSIONS et auteur des messages reçus lisent
tous `conv.Title` / `ChatDirectoryDisplayName(...)`, jamais le PlayerId brut.

- Fichiers : `HiveViewProductUiPresenter.ChatRoyal.cs`,
  `LivingHiveChatController.cs`, `ServerChatProvider.cs` (traces
  temporaires `[M065-CL RUNTIME]` laissées en place, à retirer après
  confirmation CEO), `DatabaseCatalog.cs` +
  `095_chat_group_invite_acknowledged_column.sql` (+`.rollback.sql`).
- Envoi, connexion (`MobileAccountSessionRuntimeBootstrap.cs` non touché),
  layout : non touchés.
- Compilation vérifiée propre côté client Unity ET côté serveur
  (`dotnet build` sur `BeeKingdom.Database` et `BeeKingdom.Chat`).

Commit local uniquement, aucun push. Migration 095 déjà appliquée
manuellement par le CEO sur la base de production.

READY FOR CEO RETEST
