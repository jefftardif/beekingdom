# Bee Kingdom - Jalon erreurs structurees du chat

Date: 2026-07-21  
Agent: `Communication`

## Livraison

Le transport conserve maintenant le corps HTTP brut uniquement pour un decodeur
d'erreur injecte. Il ne tente plus de deserialiser une reponse 4xx/5xx comme un
DTO metier. `RemoteChatTransportException` expose:

- categorie cliente;
- statut HTTP;
- code serveur stable;
- delai `Retry-After` facultatif.

Le message brut du serveur n'est jamais repris dans le texte d'exception affiche.
L'interface peut donc localiser un code stable sans exposer un detail technique ou
un contenu non controle.

Politique appliquee:

- 409: conflit definitif, entree outbox retiree;
- 429: limite temporaire, entree conservee, `Retry-After` propage;
- 503/5xx: indisponibilite temporaire, entree conservee;
- corps absent ou malforme: categorie et statut conserves, code serveur nul;
- absence de reponse HTTP: erreur reseau distincte.

Le header `Retry-After` a priorite sur la valeur du corps JSON.

## Verification

- 30 tests Communication executes;
- 30 reussis;
- 0 echec;
- compilation: 0 erreur, 0 avertissement.

Les nouveaux scenarios couvrent 409 avec code stable, 429 avec header et corps,
preservation de l'outbox, 503 et corps d'erreur malforme non expose.

## Contrat propose au serveur

```json
{
  "code": "chat.rate_limited",
  "message": "diagnostic facultatif non affiche directement",
  "retryAfterSeconds": 12
}
```

Les codes doivent etre versionnes, sans corps de message, identifiant joueur,
jeton ou secret. Le header HTTP `Retry-After` demeure l'autorite pour 429/503.

## Fichiers du jalon

Crees:

- `Docs/WorldMapCommunication/ChatMessaging_StructuredErrorsMilestone_2026-07-21.md`

Modifies:

- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/UnityWebRequestChatRestTransport.cs`
- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs`
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs`

Aucun deploiement, secret, drapeau de production ou synchronisation n'a ete
ajoute ou active.
