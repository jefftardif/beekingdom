# Bee Kingdom - Chat et messagerie locale

**Statut :** specification locale, sans deploiement backend  
**Version :** 0.1  
**Perimetre :** `Docs/WorldMapCommunication/` uniquement  
**Date :** 2026-07-15

## 1. Intention et limites

Cette specification definit le contrat stable d'une voie **Chat/Messagerie locale** pour Bee Kingdom. Elle permet de prototyper l'experience et les transitions d'etat sans connexion reseau, sans donnees reelles et sans modifier Unity, les scenes, les PNG, l'APK, le DNS, le TLS, SQL ou un serveur public.

Le client depend d'une interface `IChatProvider`. Le provider local respecte les memes formes de donnees et d'evenements que le futur provider permanent, mais toutes les donnees sont des fixtures deterministes et ephemeres ou stockees dans le bac local explicitement reserve au prototype.

### Principes non negociables

- Un message est identifie par une cle idempotente fournie par le client; une reconnexion ne doit jamais le dupliquer.
- Les droits sont verifies par le provider avant l'ajout dans la file d'envoi, puis seront verifies par le backend permanent.
- Les compteurs non lus sont des curseurs de lecture par utilisateur et conversation, jamais une simple longueur d'une liste UI.
- Les messages supprimes ou masques gardent leur identite et leur trace d'etat; ils ne sont pas re-ecrits comme s'ils n'avaient jamais existe.
- Le provider local ne promet aucune securite, moderation globale ou livraison multi-appareils reelle.

## 2. Quatre canaux

Les quatre canaux ont chacun un `channelType` immuable.

| `channelType` | Audience | Livraison locale | Persistance logique | Droit d'ecriture par defaut |
|---|---|---|---|---|
| `alliance` | Membres de la meme alliance | Instantanee dans la simulation | Historique de conversation | Membre actif de l'alliance |
| `server` | Joueurs du meme serveur de jeu | Instantanee dans la simulation | Historique public du serveur | Joueur connecte et non suspendu |
| `private` | Deux participants ou petit groupe prive | File hors ligne puis livraison a la reconnexion simulee | Conversation privee et boite de reception | Participant autorise |
| `leaders` | Dirigeants de l'alliance | Instantanee dans la simulation | Historique restreint | Role `leader` ou `officer` |

### Regles d'audience

- `alliance` et `leaders` sont des conversations singleton par alliance dans le contexte local (`alliance:{allianceId}` et `leaders:{allianceId}`).
- `server` est une conversation singleton par serveur (`server:{serverId}`); les messages ne sont pas des messages prives meme si une mention cible un joueur.
- `private` possede un `conversationId` stable. Pour un dialogue a deux, la paire d'identifiants triee forme la cle de deduplication; un groupe conserve un identifiant de creation distinct.
- Un participant retire perd la lecture et l'ecriture futures, mais les messages historiques restent soumis a la retention et aux regles de moderation.
- Aucun canal ne permet l'envoi de monnaie, de recompense, de commande de jeu ou de gain officiel.

## 3. Modele de donnees persistant

Les types ci-dessous sont des contrats JSON conceptuels. Les dates sont ISO-8601 UTC; les identifiants sont des chaines opaques. Les champs marques optionnels ne doivent pas etre inventes par l'UI.

### 3.1 `MessageRecord`

```json
{
  "messageId": "msg_local_000001",
  "conversationId": "alliance:alliance_demo",
  "channelType": "alliance",
  "sender": { "playerId": "player_queen", "displayName": "Queen" },
  "recipientIds": [],
  "body": "Rendez-vous a la porte nord !",
  "contentParts": [
    { "kind": "text", "text": "Rendez-vous a la porte nord !" }
  ],
  "mentions": [],
  "emoji": [],
  "replyToMessageId": null,
  "clientCreatedAt": "2026-07-15T14:00:00Z",
  "acceptedAt": "2026-07-15T14:00:00Z",
  "sequence": 12,
  "clientRequestId": "send_player_queen_000012",
  "state": "accepted",
  "moderation": { "status": "clear", "reasonCode": null },
  "editedAt": null,
  "deletedAt": null,
  "schemaVersion": 1
}
```

Champs et invariants :

- `messageId` est attribue par le provider; `clientRequestId` est unique par tentative d'envoi et permet l'idempotence.
- `body` est la forme texte canonique, sans HTML. `contentParts` ne contient que des elements connus (`text`, `emoji`, `mention`) et preserve l'ordre d'affichage.
- `recipientIds` est vide pour `alliance`, `server` et `leaders`; il est obligatoire pour `private`.
- `sequence` est monotone par conversation dans le provider qui accepte le message; il peut manquer dans une enveloppe hors ligne avant acceptation.
- `state` vaut `queued`, `accepted`, `delivered`, `failed`, `hidden`, `deleted` ou `expired`.
- `moderation.status` vaut `clear`, `pending`, `blocked`, `masked` ou `review`.
- Une edition ne remplace jamais `clientCreatedAt`; elle renseigne `editedAt` et incremente la version du record.

### 3.2 `Conversation`

```json
{
  "conversationId": "private:conv_0007",
  "channelType": "private",
  "title": "Queen, Scout",
  "participantIds": ["player_queen", "player_scout"],
  "createdBy": "player_queen",
  "createdAt": "2026-07-15T13:55:00Z",
  "lastMessageId": "msg_local_000010",
  "lastActivityAt": "2026-07-15T14:01:00Z",
  "archivedFor": [],
  "mutedFor": [],
  "retentionPolicy": "private_standard",
  "schemaVersion": 1
}
```

Une conversation est le conteneur de tri, de curseur, de permissions et de retention. Son titre est une projection locale; les identites faisant autorite resteront celles du service de compte du backend.

### 3.3 Boite de reception et non-lus

`InboxEntry` est une projection par utilisateur :

```json
{
  "userId": "player_queen",
  "conversationId": "private:conv_0007",
  "lastMessageId": "msg_local_000010",
  "lastActivityAt": "2026-07-15T14:01:00Z",
  "unreadCount": 1,
  "mentionCount": 0,
  "isMuted": false,
  "isArchived": false,
  "readCursor": 9
}
```

Regles :

- `unreadCount` compte les messages acceptes dont `sequence > readCursor`, filtres par appartenance et non masques; il ne compte pas les messages emis par l'utilisateur lui-meme.
- `mentionCount` est un sous-compteur de notifications non lues et est remis a zero seulement lorsqu'un message cible a ete vu.
- Ouvrir une conversation met le curseur au dernier message effectivement rendu, pas au dernier message recu en arriere-plan.
- `markConversationRead(conversationId, sequence)` est monotone; une valeur plus basse est ignoree.
- La badge globale est la somme des projections actives, avec un plafond d'affichage a `99+`; la valeur numerique complete reste accessible au lecteur d'ecran.
- Une conversation mutee continue de progresser et conserve ses non-lus; elle supprime uniquement les notifications sonores/visuelles de premier plan.

## 4. Permissions et transitions

### Roles

`member` est le role de base. `officer` et `leader` sont des roles d'alliance; `moderator` est une capacite de moderation independante du role d'alliance; `system` est reserve aux messages de service locaux/backend.

| Action | Member | Officer | Leader | Moderator |
|---|---:|---:|---:|---:|
| Lire `alliance` / `server` | oui | oui | oui | oui |
| Ecrire `alliance` / `server` | oui | oui | oui | oui |
| Lire/ecrire `leaders` | non | oui | oui | selon affectation |
| Creer un `private` | oui | oui | oui | oui |
| Bloquer un participant prive | soi-meme | soi-meme | soi-meme | oui, moderation |
| Masquer/supprimer pour tous | non | selon politique | oui | oui |
| Suspendre l'envoi | non | non | selon politique | oui |
| Exporter l'historique | non | non | non par defaut | non par defaut |

Les controles de permission sont appliques a l'ouverture, a la composition, a la mise en file et a l'acceptation. Une interface desactivee n'est pas une autorisation; toute erreur `forbidden` doit rester gerable si un role change pendant la session.

### Cycle d'un message

1. L'UI valide la taille, le contenu et la permission locale.
2. `sendMessage` cree un record `queued` avec `clientRequestId`.
3. Le provider applique moderation et anti-spam.
4. Un message accepte devient `accepted`, recoit `messageId` et `sequence`, puis est ajoute aux projections.
5. Un destinataire peut produire `delivered` lorsqu'il a recu le record; `read` est un curseur, pas un etat global du message.
6. Un refus devient `failed` avec un `reasonCode` stable; aucun retry automatique n'est effectue pour `blocked`, `forbidden` ou `rate_limited`.

## 5. Moderation et anti-spam

La moderation locale est une simulation deterministe de la politique future; elle ne constitue pas une protection de production.

### Pipeline

1. Normaliser Unicode, espaces et casse pour la detection; conserver le texte original uniquement dans `body` si le record est accepte.
2. Refuser les contenus vides, les controles, les payloads trop longs et les mentions de joueurs inexistants.
3. Appliquer une liste de termes de fixture et des marqueurs d'URL de test. Le resultat doit etre `blocked` ou `masked`, jamais une suppression silencieuse.
4. Enregistrer `moderation.status`, `reasonCode`, `checkedAt` et le `policyVersion` dans le journal local de test.
5. L'action utilisateur `reportMessage` cree un `ModerationReport` local sans envoyer de donnees hors du workspace.

### Limites initiales du provider local

- `body` : 500 caracteres Unicode apres normalisation; 50 messages par minute et 10 par 10 secondes par auteur et conversation.
- `private` : 10 nouvelles conversations par heure et 20 destinataires maximum par groupe.
- Doublon exact meme auteur/conversation/contenu dans une fenetre de 30 secondes : `duplicate_suppressed`.
- Repetition de plus de 3 messages tres proches dans 20 secondes : `rate_limited`.
- Un message hors ligne reste dans `queued` jusqu'a expiration de la file; l'expiration par defaut est de 24 heures.
- Les limites sont des constantes de provider, exposees dans le manifeste local pour rendre les tests reproductibles.

Les limites finales, la liste de termes, l'appel a un service de moderation, les appels utilisateur et la conservation des journaux devront etre valides par le backend et la politique de confiance/surete avant production.

## 6. Retention et suppression

La retention s'applique par `retentionPolicy` et est executable par le provider local uniquement dans ses donnees de fixture.

| Politique | Messages | Metadonnees de conversation | Comportement apres expiration |
|---|---:|---:|---|
| `alliance_standard` | 30 jours | 90 jours | Masquer le contenu, conserver un tombstone minimal |
| `server_standard` | 7 jours | 30 jours | Masquer le contenu, conserver `expired` |
| `private_standard` | 90 jours | 180 jours | Masquer le contenu aux projections, conserver le curseur |
| `leaders_restricted` | 180 jours | 365 jours | Conserver tombstone et audit de moderation |

La suppression demandee par l'utilisateur est un evenement d'etat (`deleted`), horodate et attribue. Elle n'autorise pas l'UI a effacer une preuve de moderation ni a reutiliser le `messageId`. Aucune retention de donnees reelles n'est active dans cette livraison.

## 7. Reconnexion et fonctionnement hors ligne

### File locale

Chaque envoi hors ligne est place dans une `OutboxEntry` avec `clientRequestId`, `conversationId`, hash du payload, nombre de tentatives, `createdAt`, `nextAttemptAt` et `lastErrorCode`. L'UI affiche `queued` sans promettre `delivered`.

Au retour de connexion simule :

1. Le provider rejoue les tentatives par ordre de creation, sans depasser les limites.
2. Un `clientRequestId` deja accepte retourne le record existant (`deduplicated: true`).
3. `syncConversation(conversationId, afterSequence)` demande les records apres le curseur local; une absence de sequence declenche un resync borne, pas une concatenation aveugle.
4. Les messages entrants sont merges par `messageId`; un conflit de version garde le record au `schemaVersion` le plus recent et emet `message.updated`.
5. Les erreurs transitoires (`offline`, `timeout`, `temporarily_unavailable`) utilisent un backoff borne; les erreurs definitives restent visibles dans l'outbox.

Le passage de `offline` a `online`, ainsi que la disponibilite du provider, sont des etats observables par l'UI. La reconnexion ne change jamais les permissions ni le canal d'un message.

## 8. Contrat temps reel

Toutes les notifications utilisent l'enveloppe suivante :

```json
{
  "eventId": "evt_local_000012",
  "eventType": "message.created",
  "occurredAt": "2026-07-15T14:01:00Z",
  "conversationId": "alliance:alliance_demo",
  "sequence": 12,
  "actorId": "player_queen",
  "payload": {},
  "provider": "local",
  "schemaVersion": 1
}
```

Evenements obligatoires :

| Evenement | Payload minimal | Effet UI |
|---|---|---|
| `conversation.created` | `Conversation` | Ajoute/actualise l'entree de boite |
| `message.queued` | `messageId`, `clientRequestId` | Affiche l'envoi en attente |
| `message.created` | `MessageRecord` accepte | Ajoute le message et incremente les non-lus si necessaire |
| `message.delivered` | `messageId`, `recipientId` | Affiche l'etat de livraison prive |
| `message.updated` | `messageId`, `version`, champs modifies | Re-render sans deplacer le curseur |
| `message.moderated` | `messageId`, `status`, `reasonCode` | Masque, remplace ou signale le contenu |
| `message.deleted` | `messageId`, `deletedAt`, `actorId` | Affiche le tombstone |
| `inbox.updated` | `conversationId`, compteurs | Recalcule badges et notifications |
| `presence.changed` | `playerId`, `presence` | Met a jour l'etat optionnel des participants |
| `sync.completed` | `conversationId`, `fromSequence`, `toSequence` | Termine le chargement incremental |
| `provider.status.changed` | `status`, `server` | Met a jour la banniere de connexion |

Les consommateurs doivent traiter les evenements de facon idempotente et tolerer un evenement inconnu avec `schemaVersion` superieure en l'ignorant avec telemetrie locale. L'ordre est garanti par `sequence` a l'interieur d'une conversation, pas entre canaux.

## 9. Faux provider local

Le provider de reference s'instancie avec la configuration suivante; ces valeurs doivent rester explicitement visibles dans les fixtures :

```json
{
  "provider": "local",
  "server": false,
  "official_gain": false,
  "networkTransport": "none",
  "fixtureSeed": "bee-kingdom-chat-demo-v1",
  "currentPlayerId": "player_queen",
  "latencyMs": { "min": 80, "max": 180 },
  "reconnectMode": "deterministic"
}
```

Comportement attendu :

- `server=false` interdit tout appel reseau et toute promesse de synchronisation reelle.
- `official_gain=false` interdit de transformer un message, une mention ou une notification en gain, ressource, progression ou evenement officiel.
- Les fixtures contiennent au moins un exemple par canal, un message hors ligne, un message masque, une mention et un etat vide.
- La latence est simulee par une horloge injectable; les tests ne doivent pas dependre du temps reel.
- Le stockage local est replaceable et limite au bac de prototype; aucune donnee personnelle ou identite de production ne doit y entrer.

Interface minimale :

```text
IChatProvider
  getCapabilities() -> ChatCapabilities
  listConversations(userId, filter) -> Page<ConversationSummary>
  getMessages(conversationId, cursor, limit) -> MessagePage
  createConversation(input) -> Conversation
  sendMessage(input) -> SendResult
  retryMessage(clientRequestId) -> SendResult
  markConversationRead(conversationId, sequence) -> ReadCursor
  setMuted(conversationId, muted) -> InboxEntry
  reportMessage(messageId, category) -> ModerationReport
  subscribe(listener) -> Unsubscribe
  getConnectionState() -> ConnectionState
```

`ChatCapabilities` doit declarer les quatre canaux, la prise en charge des emojis/mentions, les permissions connues, les limites locales et `server=false` / `official_gain=false`. Le client ne doit pas deduire ces informations du type concret du provider.

## 10. Contrats UI

L'UI consomme des view models et ne modifie jamais directement les records persistants.

### Emojis

- Le composeur propose un jeu d'emojis local versionne (`emojiCatalogVersion`) et insere un `contentPart` `emoji` avec `shortcode`, `unicode` et `alt`.
- Le rendu affiche l'emoji si connu, sinon le shortcode en texte; il ne doit pas casser la largeur ni la hauteur d'une bulle.
- Les emojis ne contournent pas la limite de caracteres; chaque shortcode est normalise avant envoi.
- Un picker ferme avec `Escape`, annonce l'emoji par `alt` au lecteur d'ecran et n'emet aucun message avant validation par l'utilisateur.

### Mentions

- La syntaxe d'entree est `@displayName`; le provider resout vers un `playerId` uniquement si le joueur est membre de la conversation.
- Le record persiste `mentions: [{ "playerId": "...", "label": "..." }]`, jamais la position UI seule.
- Une mention inconnue reste du texte non cliquable; elle ne declenche aucune notification.
- Une mention valide declenche `mentionCount` et une notification si le destinataire n'a pas mute la conversation.
- L'autocompletion est limitee aux participants/audience du canal et fonctionne au clavier comme au tactile.

### Notifications

Le contrat `ChatNotification` comprend `notificationId`, `kind` (`new_message`, `mention`, `delivery`, `moderation`, `provider`), `conversationId`, `messageId` optionnel, `priority`, `isRead`, `createdAt` et `deepLink`. L'UI :

- affiche au maximum une notification synthetisee par conversation dans la pile visible;
- differencie un message prive, une mention et une erreur d'envoi;
- respecte `isMuted` pour le son et la notification de premier plan, mais garde le badge non lu;
- n'affiche jamais de gain officiel ni de recompense associee au chat;
- remet `isRead` a vrai lorsqu'une destination a ete ouverte, sans avancer le curseur de conversation si le message n'a pas ete rendu.

### Etats de page et de composant

Chaque vue de conversation expose explicitement `loading`, `ready`, `empty`, `error`, `offline` et `forbidden`.

| Etat | Contrat visuel et action |
|---|---|
| `loading` | Skeleton stable; composer desactive; aucun badge temporaire ajoute |
| `empty` | Message neutre adapte au canal, bouton d'action contextuel; pas de fausse conversation |
| `ready` | Historique ordonne par `sequence`, composer selon permissions, curseur de lecture explicite |
| `offline` | Bandeau non bloquant, messages sortants en `queued`, action retry visible pour les echecs |
| `error` | Code utilisateur stable, action `retry`, preservation du dernier contenu valide |
| `forbidden` | Historique masque si necessaire, raison generique, aucune action d'envoi |

Les transitions doivent etre monotones pour le chargement (`loading -> ready|empty|error|forbidden`) et ne doivent pas effacer un historique deja rendu pendant un refresh. Les erreurs de permission et de moderation sont rendues sans exposer de details sensibles.

## 11. Handoff backend permanent (futur, non deploye)

Le futur service backend devra reprendre les contrats ci-dessus sans changer le comportement observable du client :

1. Remplacer `IChatProvider` par un adaptateur authentifie; conserver `messageId`, `clientRequestId`, `sequence`, `schemaVersion` et les codes d'erreur.
2. Faire autorite sur identites, appartenance aux alliances/serveurs, roles, blocages, moderation et retention; le client local ne sera jamais une source de verite.
3. Exposer des operations equivalentes a `listConversations`, `getMessages`, `sendMessage`, `markConversationRead`, `reportMessage` et un flux temps reel reprenant l'enveloppe d'evenement.
4. Garantir idempotence, pagination par curseur, reprise apres reconnexion et invalidation des permissions pendant une session.
5. Definir authentification, autorisation, chiffrement en transit, journalisation, moderation humaine/automatique, suppression conforme et observabilite avec l'equipe de production.
6. Faire une migration de fixtures vers un environnement de test isole, puis executer les tests de contrat et de charge avant toute activation.

Ce handoff est une liste de contrats et de decisions, **pas une implementation**. Aucun endpoint, serveur, DNS, TLS, base SQL ou donnees reelles n'est cree par ce livrable.

## 12. Criteres d'acceptation locaux

- Les quatre `channelType` sont disponibles et leurs audiences sont distinctes.
- Un message `queued` peut etre rejoue sans doublon apres une reconnexion simulee.
- Les projections de boite de reception produisent des non-lus par curseur, y compris pour les mentions.
- Les permissions refusent au minimum un membre sur `leaders` et un destinataire hors audience.
- Les cas `clear`, `masked`, `blocked`, `rate_limited` et `duplicate_suppressed` sont observables par code stable.
- Les evenements temps reel listés dans cette specification sont routables et idempotents.
- Les UI contracts couvrent emojis, mentions, notifications et `empty/loading/error/offline/forbidden`.
- La configuration de fixture indique exactement `server=false` et `official_gain=false`.
- Le handoff backend est documente sans aucun deploiement.

