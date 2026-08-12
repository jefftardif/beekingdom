# Contrat serveur de traduction du chat

Date: 2026-07-21

## Resultat

Le serveur expose maintenant le contrat authentifie `POST /chat/v1/messages/{messageId}/translations` attendu par le pont Unity. Cette livraison est locale uniquement: `Chat:Enabled` et `Chat:RealtimeEnabled` ne sont pas actives, aucun fournisseur externe n'est configure, aucune migration n'est appliquee et aucun deploiement n'est effectue.

## Autorite et moderation

- Le bearer token existant determine le `PlayerId`; le corps ne peut pas choisir le lecteur.
- Le message original est relu depuis `IChatRepository` et n'est jamais envoye par le client.
- Le lecteur doit etre participant actif avec `CanRead=true`.
- Le texte original dans `ChatMessages.Body` reste la seule donnee officielle et moderee.
- La traduction est un cache derive; elle ne modifie ni message, ni sequence, ni moderation, ni inbox.
- `MessageId` du chemin et du corps doivent correspondre.

## Validation et limites

- locale cible au format `en-US`/`fr-CA`;
- version de modele egale a la version configuree par le serveur;
- taille originale bornee par `Chat:TranslationMaxCharacters` (1000 par defaut);
- resultat borne a deux fois cette taille;
- limite par joueur de `Chat:TranslationsPerMinutePerPlayer` (10 par defaut);
- une entree deja en cache ne consomme pas une nouvelle traduction;
- reponse 401 sans session, 403 sans lecture, 404 sans message, 400 pour contrat invalide, 429 sur limite et 503 sans fournisseur.

## Fournisseur

`IChatTranslationProvider` separe le domaine de tout fournisseur externe. L'enregistrement par defaut est `UnavailableChatTranslationProvider`, qui bloque explicitement la traduction. `DelegateChatTranslationProvider` sert uniquement aux tests. Aucun endpoint, jeton, modele cloud ou secret n'est ajoute au depot.

## Persistance et idempotence

`IChatTranslationRepository` utilise la cle unique `(MessageId, TargetLocale, ModelVersion)`. L'adaptateur memoire est verrouille. L'adaptateur SQL effectue un `INSERT` sous transaction `Serializable` avec `UPDLOCK,HOLDLOCK`, puis relit la valeur gagnante. Deux requetes concurrentes convergent donc vers la meme traduction persistee.

La migration `061_chat_translations.sql` cree `ChatMessageTranslations` avec cle primaire composite et cle etrangere vers `ChatMessages`. Le rollback supprime uniquement cette table derivee. Aucun original n'est supprime ou transforme par le rollback.

## Contrat Unity stable

Requete:

```json
{
  "messageId": "00000000-0000-0000-0000-000000000000",
  "targetLocale": "en-US",
  "modelVersion": "translation-disabled-v1"
}
```

Reponse completee:

```json
{
  "messageId": "00000000-0000-0000-0000-000000000000",
  "sourceLocale": "fr-CA",
  "targetLocale": "en-US",
  "modelVersion": "translation-disabled-v1",
  "translatedText": "...",
  "status": "completed"
}
```

Le cache Unity `MessageId + TargetLocale + ModelVersion` correspond exactement a la contrainte serveur.

## Validation

Un projet isole compile les sources Chat actuelles contre les dependances serveur construites, puisque les `.csproj` historiques manquent dans cette copie. Resultat: 4/4 tests reussis, couvrant cache/idempotence, autorisation de lecture, taille maximale et limite de debit. Le endpoint central est ajoute mais la solution serveur complete ne peut pas etre reconstruite avant restauration de ses `.csproj` historiques.

## Fichiers crees

- `Server/src/BeeKingdom.Chat/Translations/ChatTranslationContracts.cs`
- `Server/src/BeeKingdom.Chat/Translations/ChatTranslationProviders.cs`
- `Server/src/BeeKingdom.Chat/Translations/ChatTranslationRateLimiter.cs`
- `Server/src/BeeKingdom.Chat/Translations/InMemoryChatTranslationRepository.cs`
- `Server/src/BeeKingdom.Chat/Translations/SqlChatTranslationRepository.cs`
- `Server/src/BeeKingdom.Chat/Translations/ChatTranslationService.cs`
- `Server/src/BeeKingdom.Database/Scripts/061_chat_translations.sql`
- `Server/src/BeeKingdom.Database/Scripts/061_chat_translations.rollback.sql`
- `Server/tests/BeeKingdom.Tests/ChatTranslationServiceTests.cs`
- `Server/tests/BeeKingdom.ChatTranslation.Tests/BeeKingdom.ChatTranslation.Tests.csproj`
- `Docs/ProductionIntegration/ChatTranslation_ServerContract_2026-07-21.md`

## Fichiers modifies

- `Server/src/BeeKingdom.Chat/Configuration/ChatOptions.cs`
- `Server/src/BeeKingdom.Chat/DependencyInjection/ChatServiceCollectionExtensions.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Database/DatabaseCatalog.cs`
- `Server/src/BeeKingdom.Database/DatabaseRollbackCatalog.cs`

## Gates avant activation

1. restaurer et compiler tous les `.csproj` historiques;
2. choisir et auditer un fournisseur de traduction;
3. injecter ses secrets uniquement depuis la configuration externe;
4. tester la migration 061 sur une base ephemere/staging;
5. ajouter metriques et journaux structures sans texte original;
6. obtenir une autorisation distincte avant activation ou deploiement.

Aucun changement n'est requis dans les contrats Unity livres par Communication.

## Validation transport et synchronisation

La politique JSON HTTP est maintenant explicitement fixee a `JsonNamingPolicy.CamelCase`, y compris les cles de dictionnaire. Les enums restent serialises comme chaines.

Le wire message conserve les noms publies par le codec Unity: `messageId`, `conversationId`, `senderPlayerId`, `body`, `clientCreatedAtUtc`, `acceptedAtUtc`, `sequence` et `clientRequestId`. Une projection dediee convertit le value object serveur `PlayerId` en GUID JSON; sans cette projection, System.Text.Json aurait expose un objet imbrique incompatible.

Les pages conservent `items` et `nextAfterSequence`. Les resultats d'envoi conservent `message`, `deduplicated` et `serverSequence`. Les conversations sont projetees avec `conversationId`, `title` et `lastSequence` reel, calcule depuis la sequence maximale persistee.

### Idempotence

- Envoi: le recu existant `(PlayerId, ConversationId, ClientRequestId)` conserve le hash du payload et le `MessageId`; un retry strictement identique retourne `deduplicated=true`, un payload different retourne 409.
- Creation: la migration 062 ajoute un recu persistant `(PlayerId, ClientRequestId)` avec hash du payload et `ConversationId`. Un retry retrouve la conversation; un payload different retourne 409.
- Les retries idempotents connus sont traites avant la limite de debit et ne consomment pas un nouveau quota.

### Sequences et reprise

Les sequences sont allouees de facon monotone par conversation. SQL utilise une transaction serialisable et une ligne `ChatConversationSequences` verrouillee. `afterSequence=N` effectue une lecture stricte `Sequence > N`, triee ascendante et bornee. `nextAfterSequence` vaut la derniere sequence effectivement retournee seulement lorsque la page est pleine. Une page repetee est identique et une page suivante ne saute aucune sequence non retournee.

Le serveur ne cree aucune session durable pour le polling: une deconnexion client ne laisse donc ni curseur serveur temporaire, ni timer, ni ressource de synchronisation. La reconnexion repart du dernier `afterSequence` confirme par le client. Les doublons ou evenements temps reel hors ordre sont resolus par le merge client; la source REST reste ordonnee et idempotente.

### Statuts

- 401: bearer absent, invalide, revoque ou expire uniquement;
- 403: participation absente, retiree ou sans droit de lecture/ecriture;
- 429: limite joueur/conversation ou traduction;
- 503: chat desactive ou fournisseur de traduction indisponible;
- 409: conflit d'idempotence;
- les erreurs reseau avant le serveur ne sont pas transformees en 401/403.

Les limites de messages utilisent maintenant les valeurs deja publiees dans `ChatLimits`: par joueur/minute et par conversation/10 secondes.

### Ecart client signale

`UnityChatJsonCodec.WireCreateResult.inbox` est declare comme `string`, tandis que le serveur conserve l'objet `ChatInboxEntry` historique et que `RemoteCreateConversationResult.Inbox` est un `object`. Aucun changement silencieux n'a ete applique. Communication doit choisir un DTO filaire inbox explicite ou ignorer ce champ; la creation et son `conversation` restent compatibles.

### Tests

La suite isolee compile les sources Chat actuelles et execute 10/10 tests. Elle couvre traduction/cache/lecture/taille/debit, contrat JSON de traduction, creation idempotente et conflit, sequences monotones, trou apres reconnexion, pages repetees, pagination sans saut, forme camelCase et retry d'envoi gratuit sous limite.

Fichiers supplementaires crees:

- `Server/src/BeeKingdom.Chat/Models/ChatTransportDtos.cs`
- `Server/src/BeeKingdom.Database/Scripts/062_chat_creation_idempotency.sql`
- `Server/src/BeeKingdom.Database/Scripts/062_chat_creation_idempotency.rollback.sql`
- `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`

Fichiers supplementaires modifies:

- `Server/src/BeeKingdom.Chat/Models/ChatRecords.cs`
- `Server/src/BeeKingdom.Chat/Repositories/IChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Repositories/InMemoryChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Repositories/SqlChatRepository.cs`
- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/ChatManager.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Database/DatabaseCatalog.cs`
- `Server/src/BeeKingdom.Database/DatabaseRollbackCatalog.cs`
- `Server/tests/BeeKingdom.ChatTranslation.Tests/BeeKingdom.ChatTranslation.Tests.csproj`

Les migrations 061/062 restent non appliquees. Aucun drapeau, deploiement ou synchronisation n'a ete active.
