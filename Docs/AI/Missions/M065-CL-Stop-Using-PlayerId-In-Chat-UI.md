# M065-CL — Stop Using PlayerId In Chat UI

## Historique de cette session (résumé court)

1. Nom affiché = PlayerId brut → cause : le cache de noms se faisait effacer
   par un mécanisme de reconfiguration sans rapport avec le chat, déclenché
   plusieurs fois par session. Corrigé en stockant le nom appris dans un
   dictionnaire stable au niveau de l'écran (`chatKnownDisplayNames`).
2. Nettoyage complet de la base de production (demande CEO) → a exposé deux
   vrais bugs serveur jamais vus avant (masqués par la réutilisation
   idempotente de conversations déjà existantes) :
   - `GameServerId`/`WorldId` jamais remplis à la création de conversation
     privée → corrigé en réutilisant le même scope que `CreateGroupAsync`.
   - Colonne SQL manquante (`InviterAcknowledged`) → migration additive 095,
     déjà appliquée par le CEO.
3. **Toujours 400 après ces deux correctifs** → cause finale, vérifiée
   empiriquement (pas une supposition) : le désérialiseur JSON du serveur
   (.NET 8 / `System.Text.Json`) **rejette le format d'ID sans tirets**
   (`Guid.ToString("N")`) et n'accepte que le format avec tirets (`"D"`).
   Le client envoyait ce format pour l'ID du participant depuis le tout
   début, pour la création de conversation privée ET pour les invitations
   de groupe. Corrigé aux 3 endroits concernés
   (`entry.PlayerId.ToString("D")`).

## Vérifié dans le code (pas de test automatisé lancé)

Titre de conversation, liste DISCUSSIONS et auteur des messages reçus lisent
tous le nom résolu, jamais le PlayerId brut, une fois la conversation créée
avec succès.

- Fichiers touchés au total : `HiveViewProductUiPresenter.ChatRoyal.cs`,
  `LivingHiveChatController.cs`, `ServerChatProvider.cs` (traces
  temporaires `[M065-CL RUNTIME]` à retirer après confirmation CEO),
  `DatabaseCatalog.cs` + `095_chat_group_invite_acknowledged_column.sql`
  (+`.rollback.sql`).
- Envoi, connexion, layout : non touchés.
- Compilation vérifiée propre côté client Unity ET côté serveur.
- Format `Guid.ToString("N")` vs `"D"` vérifié empiriquement avec un test
  `System.Text.Json` autonome (pas une supposition sur le comportement du
  framework).

Commit local uniquement, aucun push. Migration 095 déjà appliquée
manuellement par le CEO sur la base de production.

READY FOR CEO RETEST
