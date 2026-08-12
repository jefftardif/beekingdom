# Chat — reçus de mutation corrélés (2026-07-21)

## Résultat

Une réponse HTTP réussie n’acquitte plus une opération persistante uniquement parce que son JSON est désérialisable. Le reçu doit correspondre exactement à la mutation en attente.

### Message

Le reçu d’envoi doit contenir :

- un `messageId` non vide ;
- le même `conversationId` ;
- le même `clientRequestId` ;
- le même corps original ;
- une séquence strictement positive ;
- `serverSequence` égal à la séquence du message ;
- un `senderPlayerId` valide.

### Création de conversation

Le résultat doit retourner le même `clientRequestId`, une conversation valide, une entrée inbox présente et le même `conversationId` dans les deux objets. `lastSequence` ne peut pas être négatif.

### Signalement

Le résultat doit retourner un `reportId` et un statut non vides, ainsi que le même `messageId` et le même `clientRequestId`.

Les DTO Unity transportent donc désormais `clientRequestId` sur le résultat de création, et `messageId` plus `clientRequestId` sur le résultat de signalement.

## Conservation de la persistance

Un reçu réussi mais incohérent produit `InvalidResponse` avec un code local précis :

- `message_receipt_mismatch` ;
- `conversation_receipt_mismatch` ;
- `moderation_receipt_mismatch`.

L’entrée persistante est conservée et aucun cache local n’est muté. Seuls un refus client HTTP terminal explicite (4xx) ou `Forbidden` suit la politique terminale existante. Une réponse 2xx malformée n’est jamais assimilée à un acquittement.

## Validation

- reçu de message associé à une autre conversation : rejet et envoi conservé ;
- création retournant un autre `clientRequestId` : rejet et création conservée ;
- signalement retournant un autre message : rejet et signalement conservé ;
- parcours valides existants conservés.

Suite isolée Communication : **106/106 tests réussis**, compilation sans erreur ni avertissement.

## Directive serveur

Étendre les contrats camelCase :

- création : `conversation`, `inbox`, `clientRequestId` ;
- signalement : `reportId`, `messageId`, `clientRequestId`, `status` ;
- envoi : conserver `message.clientRequestId`, `message.conversationId`, `message.body`, `message.sequence` et `serverSequence` concordants.

Les valeurs doivent provenir du même commit/reçu idempotent, y compris lors d’une déduplication après coupure. Les tests HTTP doivent injecter volontairement chaque discordance et vérifier qu’aucun reçu d’une autre opération, d’un autre joueur ou d’une autre conversation ne peut acquitter la file cliente.

Le candidat précédent ne satisfait pas ce nouveau contrat tant que ses DTO et tests ne sont pas étendus. Le prochain candidat doit le révoquer et rester `DeploymentAuthorized=false` jusqu’aux portes SQL, .NET 8, TLS/IIS et Android staging. Aucun transfert, déploiement, activation ni synchronisation n’est autorisé ici.
