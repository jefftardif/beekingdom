# Bee Kingdom - ChatMessaging Unity Client/Server Contract

**Date :** 2026-07-16  
**Statut :** contrat client Unity, serveur local/staging seulement  
**Live :** non active  
**Transport :** REST JSON + SignalR prepare  
**Autorite :** serveur uniquement quand `Chat:Enabled=true`

## 1. Gates

Production reste fermee par defaut :

```json
{
  "Chat": {
    "Enabled": false,
    "RealtimeEnabled": false
  }
}
```

Un client Unity ne doit pas afficher le chat live comme disponible tant que :

- `GET /runtime/chat-readiness` ne retourne pas `enabled=true`;
- `GET /chat/v1/capabilities` ne retourne pas `server=true`;
- la session joueur n'a pas un access token valide;
- le serveur n'a pas valide DNS/TLS/firewall et staging.

## 2. Authentification

Tous les endpoints mutables et historiques utilisent :

```http
Authorization: Bearer <accessToken>
```

Erreurs :

- `401` : token absent, invalide, expire ou revoque;
- `403` : joueur authentifie mais non autorise pour le canal/conversation;
- `503` : `chat_disabled`;
- `409` : conflit d'idempotence;
- `400` : payload invalide.

## 3. Enums JSON

Les reponses exposent les enums en chaine.

`channelType` :

- `Alliance`
- `Server`
- `Private`
- `Leaders`

`state` :

- `Queued`
- `Accepted`
- `Delivered`
- `Failed`
- `Hidden`
- `Deleted`
- `Expired`

`moderationStatus` :

- `Clear`
- `Pending`
- `Blocked`
- `Masked`
- `Review`

Le serveur accepte encore les valeurs numeriques .NET pour compatibilite de tests, mais Unity doit envoyer les chaines.

## 4. Readiness

`GET /runtime/chat-readiness`

Reponse :

```json
{
  "status": "PreparationOnly",
  "enabled": false,
  "realtimeEnabled": false,
  "persistentSqlSchemaPrepared": true,
  "liveDeploymentAllowed": false,
  "blockers": ["Chat__Enabled is false; REST mutations are gated."]
}
```

## 5. Capabilities

`GET /chat/v1/capabilities`

Reponse :

```json
{
  "provider": "server",
  "server": false,
  "officialGain": false,
  "protocolVersion": "chat-v1",
  "channels": ["Alliance", "Server", "Private", "Leaders"],
  "emojis": true,
  "mentions": true,
  "offlineDelivery": true,
  "readCursors": true,
  "moderationReports": true,
  "realtime": false,
  "limits": {
    "bodyMaxCharacters": 500,
    "messagesPerMinutePerPlayer": 50,
    "messagesPerTenSecondsPerConversation": 10,
    "privateConversationCreatesPerHour": 10,
    "maxPrivateRecipients": 20
  }
}
```

## 6. Conversations

`POST /chat/v1/conversations`

Private :

```json
{
  "channelType": "Private",
  "gameServerId": "00000000-0000-0000-0000-000000000001",
  "worldId": "00000000-0000-0000-0000-000000000101",
  "audienceKey": null,
  "title": "Queen, Scout",
  "participantIds": ["11111111-1111-1111-1111-111111111111"],
  "clientRequestId": "create_private_001"
}
```

Server/global :

```json
{
  "channelType": "Server",
  "gameServerId": "00000000-0000-0000-0000-000000000001",
  "worldId": "00000000-0000-0000-0000-000000000101",
  "audienceKey": null,
  "title": "Global",
  "participantIds": [],
  "clientRequestId": "create_server_global_001"
}
```

Alliance :

```json
{
  "channelType": "Alliance",
  "gameServerId": "00000000-0000-0000-0000-000000000001",
  "worldId": "00000000-0000-0000-0000-000000000101",
  "audienceKey": "alliance:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "title": "Alliance",
  "participantIds": ["11111111-1111-1111-1111-111111111111"],
  "clientRequestId": "create_alliance_001",
  "requesterAllianceRole": "member"
}
```

Leaders :

```json
{
  "channelType": "Leaders",
  "gameServerId": "00000000-0000-0000-0000-000000000001",
  "worldId": "00000000-0000-0000-0000-000000000101",
  "audienceKey": "leaders:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "title": "Leaders",
  "participantIds": [],
  "clientRequestId": "create_leaders_001",
  "requesterAllianceRole": "leader"
}
```

Regles Phase 2 :

- `Alliance` exige `requesterAllianceRole` = `member`, `officer` ou `leader`;
- `Leaders` exige `requesterAllianceRole` = `officer` ou `leader`;
- `Private` est visible/ecrivable seulement par les participants;
- `Server` est visible/ecrivable par le joueur authentifie sur le serveur/monde;
- l'autorite alliance definitive reste a brancher sur le futur module alliance live.

## 7. Messages

`POST /chat/v1/conversations/{conversationId}/messages`

```json
{
  "clientRequestId": "send_player_queen_000012",
  "body": "Rendez-vous a la porte nord !",
  "contentParts": [
    { "kind": "text", "text": "Rendez-vous a la porte nord !" }
  ],
  "mentions": [
    { "playerId": "11111111-1111-1111-1111-111111111111", "label": "Scout" }
  ],
  "emoji": [
    { "shortcode": ":bee:", "unicode": "bee", "alt": "bee" }
  ],
  "replyToMessageId": null,
  "clientCreatedAt": "2026-07-16T14:00:00Z"
}
```

Reponse :

```json
{
  "message": {
    "messageId": "22222222-2222-2222-2222-222222222222",
    "conversationId": "33333333-3333-3333-3333-333333333333",
    "channelType": "Private",
    "body": "Rendez-vous a la porte nord !",
    "sequence": 1,
    "clientRequestId": "send_player_queen_000012",
    "state": "Accepted",
    "moderationStatus": "Clear",
    "schemaVersion": 1
  },
  "deduplicated": false,
  "serverSequence": 1
}
```

Idempotence :

- `clientRequestId` doit etre stable pour une tentative;
- si la meme requete revient identique, le serveur retourne le message existant avec `deduplicated=true`;
- si le meme `clientRequestId` revient avec payload different, le serveur retourne `409 idempotency_conflict`.

## 8. Historique et curseurs

`GET /chat/v1/conversations/{conversationId}/messages?afterSequence=0&limit=50`

Reponse :

```json
{
  "items": [],
  "nextAfterSequence": null
}
```

`POST /chat/v1/conversations/{conversationId}/read`

```json
{ "sequence": 12 }
```

Le curseur de lecture est monotone. Unity ne doit jamais baisser un curseur local deja confirme.

## 9. Annonces dirigeants

`POST /chat/v1/alliances/{allianceId}/announcements`

```json
{
  "gameServerId": "00000000-0000-0000-0000-000000000001",
  "worldId": "00000000-0000-0000-0000-000000000101",
  "body": "Defense au centre ce soir.",
  "memberPlayerIds": ["11111111-1111-1111-1111-111111111111"],
  "clientRequestId": "announcement_001",
  "requesterAllianceRole": "leader"
}
```

`requesterAllianceRole` doit etre `officer` ou `leader`.

## 10. Moderation report

`POST /chat/v1/messages/{messageId}/report`

```json
{ "category": "spam" }
```

Reponse :

```json
{
  "reportId": "44444444-4444-4444-4444-444444444444",
  "messageId": "22222222-2222-2222-2222-222222222222",
  "category": "spam",
  "status": "open"
}
```

## 11. Reconnect et temps reel

Hub prepare :

```text
/chat/v1/realtime
```

Le hub refuse la connexion tant que `Chat:RealtimeEnabled=false`.

Enveloppe d'evenement :

```json
{
  "eventId": "evt_...",
  "eventType": "message.created",
  "occurredAt": "2026-07-16T14:01:00Z",
  "conversationId": "33333333-3333-3333-3333-333333333333",
  "sequence": 1,
  "actorId": { "value": "00000000-0000-0000-0000-000000000001" },
  "payload": {},
  "provider": "server",
  "schemaVersion": 1
}
```

Reconnect Unity :

1. Conserver `conversationId` et dernier `sequence` rendu.
2. Reconnecter le hub si disponible.
3. Appeler `GET /messages?afterSequence=<lastRenderedSequence>`.
4. Merger par `messageId`.
5. Appeler `/read` seulement apres rendu effectif.

## 12. Limites Phase 2

- SQL repository est implemente, mais les tests SQL reels sont opt-in LocalDB.
- Source d'autorite alliance definitive non branchee; Phase 2 utilise `requesterAllianceRole` comme garde contractuel local/staging.
- Dispatch SignalR effectif reste noop tant que `Chat:RealtimeEnabled=false`.
- Aucun changement Unity n'est inclus dans ce contrat.
