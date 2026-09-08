# M065-CL — Stop Using PlayerId In Chat UI

## Résultat final : confirmé en production par le CEO

Chaîne complète de bugs trouvés et corrigés dans cette mission, du plus
superficiel au plus profond :

1. **Cache de nom effacé sans arrêt** : `chatPlayerPicker` (où un nom appris
   via "Nouvelle discussion" était mis en cache) est recréé à zéro par un
   mécanisme de reconfiguration sans rapport avec le chat, déclenché
   plusieurs fois par session. Corrigé en stockant le nom appris dans un
   dictionnaire stable au niveau de l'écran (`chatKnownDisplayNames`).
2. **Nettoyage complet de la base de production** (demande CEO) a exposé
   deux bugs serveur jamais vus avant (masqués par la réutilisation
   idempotente de conversations déjà existantes) :
   - `GameServerId`/`WorldId` jamais remplis à la création de conversation
     privée → corrigé en réutilisant le même scope que `CreateGroupAsync`.
   - Colonne SQL manquante (`InviterAcknowledged`) → migration additive 095,
     appliquée en production.
3. **Format d'ID rejeté par le serveur** : vérifié empiriquement (test
   `System.Text.Json` autonome), le serveur .NET 8 n'accepte que le format
   de GUID avec tirets (`"D"`), pas le format sans tirets (`"N"`) que le
   client envoyait depuis toujours pour les participants/invités. Corrigé
   aux 3 endroits concernés.
4. **Titre de conversation vide après reconnexion** : le résolveur de nom
   serveur (`ChatService.ConversationForViewer`) fonctionnait déjà
   correctement - c'est la combinaison des 3 correctifs précédents qui
   manquait pour qu'il ait quoi que ce soit à résoudre. Déployé en
   production (build + migration), confirmé par le CEO : le nom "bob"
   s'affiche maintenant correctement et survit à une reconnexion.

## Nettoyage final

Toutes les traces temporaires `[M059D RUNTIME]`/`[M065-CL RUNTIME]`
(client et serveur) ont été retirées une fois le succès confirmé - seule la
logique de correctif reste (gestion cohérente du statut à l'annulation, le
verrou `openInFlight` contre les ouvertures en double, etc.).

## Fichiers touchés (cumulatif sur toute la mission)

- Client : `HiveViewProductUiPresenter.ChatRoyal.cs`, `HiveViewProductUiPresenter.cs`,
  `ChatPlayerPickerController.cs`, `LivingHiveChatController.cs`, `ServerChatProvider.cs`.
- Serveur : `LivingHiveChatController.cs` (Guid format), `DatabaseCatalog.cs` +
  `095_chat_group_invite_acknowledged_column.sql` (+`.rollback.sql`).
- Envoi, connexion (`MobileAccountSessionRuntimeBootstrap.cs` non touché),
  layout : non touchés.

Compilation vérifiée propre côté client Unity et côté serveur à chaque
étape. Commits locaux uniquement, aucun push. Déploiement serveur fait
manuellement en production (build + IIS + migration SQL), confirmé
fonctionnel en direct.

**TERMINÉ.**
