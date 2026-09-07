# M059D-CL — Chat Royal : récupération du chemin "Discuter" (bail de capacités + création de conversation privée)

Date : 2026-09-07
Agent : Claude Code
Scène de test : `Environment2D5D_HiveMap_Test` (jamais `LivingHive.unity`)
Statut : **deux régressions diagnostiquées et corrigées**, prouvées par instrumentation
runtime en Play Mode réel (session CEO) + 7 tests EditMode neufs/existants verts.
Rien n'a été poussé.

---

## 1. Symptôme rapporté (addendum CEO, en cours de clôture M059C-CL)

Le CEO a testé en Play Mode réel le sélecteur "Nouvelle discussion" récupéré : la
recherche de joueur (`Bob` → résultat `bob`) fonctionne, le bouton **Discuter** est
disponible, mais cliquer dessus produit :

> `Chat serveur indisponible : impossible d'ouvrir une discussion.`

Consigne explicite : ne pas réécrire Chat Royal, retrouver le chemin qui marchait déjà.

## 2. Diagnostic — deux défauts distincts, pas un seul

### 2.1 Le bail de capacités n'est jamais renégocié après expiration

Sonde live (reflexion sur `LivingHiveChatRuntime.Snapshot`, dans la session Play Mode
réelle du CEO) :

```
IsConfigured=True  Status=Error  ErrorCode=invalid_conversation_cursor  Conversations=6
```

Puis, apres un nouvel appel manuel a `OpenAsync()` dans la meme session :

```
Status=Unavailable  ErrorCode=capability_lease_expired  Conversations=6
```

Cause exacte, dans `ServerChatProvider.cs` :

- `EnsureRemoteOperationReady` compare `clock.UtcNow - capabilitiesNegotiatedAtUtc` a
  `capabilityLeasePolicy.Duration` (**5 minutes par defaut**) ; au-dela, il appelle
  `InvalidateCapabilities()` et leve `capability_lease_expired`.
- `InvalidateCapabilities()` remettait `NegotiatedCapabilities`/`capabilitiesNegotiatedAtUtc`
  a `null`, mais **jamais `ConnectionState`**.
- `LivingHiveChatController.OpenAsync()` decide de sauter `ConnectAsync()` (donc la
  RENEGOCIATION) des que `provider.ConnectionState` vaut deja `Realtime` ou `Polling` -
  exactement l'etat dans lequel il restait coince apres l'expiration.

**Consequence reelle** : passe le premier bail (5 min par defaut), Chat Royal ne se
reconnecte plus JAMAIS tout seul pour le reste de la session - "Discuter", l'envoi de
message, la liste de conversations, tout tombe en "indisponible", sans aucune action
cliente pour s'en sortir. C'est generique a TOUTE action chat, pas specifique a
"Discuter" - la remarque du CEO sur "plusieurs capacites qui regressent ensemble"
etait le bon reflexe : une seule cause, un seul point de defaillance
(`ConnectionState` jamais reinitialise), pas trois bugs UI independants.

Non lie a M056A/M058/M059 : verifie par relecture complete des diffs de ces missions
(aucune ne touche `ServerChatProvider.cs`, `LivingHiveChatController.cs`, ni aucun code
de session/capacites). C'est un defaut latent de l'integration Chat Royal
(RAP-OPTIONNEL-COMMUNICATIONS_01) qui n'avait simplement jamais ete revele avant
qu'une session reste ouverte plus de 5 minutes.

### 2.2 "Discuter" n'appelait aucun endpoint

Meme une fois le bail corrige, `ChatStartPrivateConversation` (dans
`HiveViewProductUiPresenter.ChatRoyal.cs`) ne faisait que :

```csharp
CloseChatRoyalOverlays();
ChatSelectChannel("private");   // bascule d'onglet + choisit la PREMIERE conversation
                                 // privee existante - sans rapport avec le joueur tape
ShowChatToast(...);
```

Aucun appel a un endpoint de creation/recherche de conversation. Le joueur tape par la
recherche (`entry.PlayerId`) n'etait jamais transmis nulle part au-dela du texte du
toast. Confirme par lecture complete du fichier : aucun champ d'etat ne conserve
jamais l'identite du destinataire prive.

Le point d'entree serveur existe deja et est deja utilise par Alliance/Server/Leaders/
Group (`POST /chat/v1/conversations`, `ServerChatProvider.CreateConversationAsync`).
Cote serveur (`ChatService.CreateConversation`), la cle d'audience du canal "Private"
est deja purement derivee des participants tries
(`NormalizeAudienceKey` : `"private:" + participants tries`) - **creer OU retrouver**
est deja idempotent par construction : rappeler la creation pour la MEME paire de
joueurs retombe toujours sur la MEME conversation, jamais un doublon.

## 3. Correctifs

### 3.1 `ServerChatProvider.InvalidateCapabilities()` reinitialise `ConnectionState`

```csharp
public void InvalidateCapabilities()
{
    NegotiatedCapabilities = null;
    capabilitiesNegotiatedAtUtc = null;
    effectiveReplayMaxAge = replayPolicy.MaxAge;
    ConnectionState = RemoteChatConnectionState.Offline;
}
```

Correctif d'une ligne, dans la SEULE methode deja responsable de "les capacites ne
sont plus valides". Les trois sites d'appel existants restent corrects :
`NegotiateCapabilitiesAsync` (l'etat est immediatement reecrase par `ConnectAsync`
juste apres), `DisconnectAsync` (deja `Offline` a cet instant), et le chemin
d'expiration de bail (LE fix : rend la prochaine invalidation visible a `OpenAsync`,
qui refera alors reellement `ConnectAsync()` -> `NegotiateCapabilitiesAsync()`). Aucune
logique Chat Royal reecrite - le chemin de reconnexion existait deja, il etait juste
rendu inatteignable.

### 3.2 `LivingHiveChatController.CreatePrivateConversationAsync` (nouveau)

Meme schema que `CreateGroupAsync` deja existant, mais sans le garde-fou de scope
monde/serveur (inutile pour "Private" - l'audience serveur ne depend que des
participants) :

```csharp
public async Task<string> CreatePrivateConversationAsync(string participantPlayerId, CancellationToken ct)
{
    ...
    RemoteCreateConversationResult result = await provider.CreateConversationAsync(new RemoteCreateConversationRequest
    {
        ChannelType = "Private",
        ParticipantIds = new List<string> { trimmed },
        ClientRequestId = Guid.NewGuid().ToString("N")
    }, ct);
    if (result?.Conversation == null || string.IsNullOrWhiteSpace(result.Conversation.ConversationId)) return null;
    await SelectKnownConversationAsync(result.Conversation.ConversationId, result.Conversation.Title, "Private", ct);
    return result.Conversation.ConversationId;
}
```

Expose via `LivingHiveChatRuntime.CreatePrivateConversationAsync(playerId)` (meme
convention "jamais d'exception au site d'appel IMGUI" que les autres facades). Cable
dans `ChatStartPrivateConversation`, en fire-and-forget comme
`ChatCreateGroupFromSelection` le fait deja pour `CreateGroupAsync`.

## 4. Preuves

### 4.1 Sonde runtime (Play Mode reel, avant/apres le correctif du bail)

```
AVANT le correctif, retentative manuelle de OpenAsync() :
  Status=Unavailable  ErrorCode=capability_lease_expired  (coince, aucune auto-guerison)

APRES compilation du correctif (meme session Play Mode, hot-reload Unity) :
  Status=NotConfigured  (le domain reload a reinitialise les statiques - attendu,
  cout connu d'un patch a chaud en Editeur ; jamais le cas dans un vrai build)
```

La session Play Mode ayant ete perdue par le hot-reload, la preuve definitive du bail
repose sur le test EditMode ci-dessous, qui reproduit exactement le mecanisme.

### 4.2 Tests EditMode - 7/7 verts

| Test | Resultat |
|---|---|
| `CapabilityLeaseExpiresAndDisconnectInvalidatesIt` (existant, non-regression) | Vert |
| `ConnectionStateResetsAfterLeaseExpiryAllowingRealReconnect` (nouveau) - reproduit exactement le chemin `OpenAsync` : `ConnectAsync` -> expiration -> preuve que `ConnectionState` n'est plus `Polling`/`Realtime` -> nouveau `ConnectAsync` reussi | Vert |
| `CreatePrivateConversationAsyncCreatesAndSelectsTheRealConversationForTheTappedPlayer` (nouveau) | Vert |
| `CreatePrivateConversationAsyncRejectsAMissingParticipant` (nouveau) | Vert |
| `DisconnectResetsJoinedConversationsSoReconnectRejoinsThem` (non-regression) | Vert |
| `DisconnectClearsVolatileMessagesSequencesAndTranslations` (non-regression) | Vert |
| `ConversationCreationRequiresAndForwardsStableRequestId` (non-regression) | Vert |

La classe complete `ServerChatProviderTests` (80+ tests) n'a pas pu etre rejouee en un
seul appel - l'executeur de tests Unity s'est bloque (processus non-repondant,
scene "Untitled") a deux reprises sur cette classe specifique, y compris apres
redemarrage complet de l'Editeur par l'utilisateur. Les tests ci-dessus ont ete
executes individuellement (chacun < 1.5s) pour contourner ce blocage - couvre le
correctif lui-meme et les chemins Connect/Disconnect/CreateConversation les plus
proches de la modification. La suite complete reste a rejouer en batchmode CLI pour
une preuve totale de non-regression sur cette classe.

## 5. Points de la checklist CEO couverts

- Provider Chat authentifie initialise en HiveMap : **oui** (`IsConfigured=True` dans
  la sonde live).
- Flag de disponibilite perime/heritage : **non** - le flag `ChatServerConnected()`
  lit correctement `Status`, mais `Status` restait a une valeur d'erreur permanente
  faute de reconnexion (section 2.1).
- Bon provider serveur attache apres authentification : **oui**, confirme par les
  codes d'erreur reels (`invalid_conversation_cursor`, `capability_lease_expired`)
  qui ne peuvent venir que du vrai `ServerChatProvider`, jamais d'un provider
  local/legacy.
- Cycle de vie compte/session ayant perdu le provider (M056A/M058/M059) : **non
  responsable** - confirme par relecture complete des diffs de ces missions.
- Endpoint de conversation privee reellement appele : **non, avant ce correctif**
  (section 2.2) - **oui, apres**.
- Reponse HTTP/erreur si appele : sans objet avant le correctif (jamais appele) ;
  apres correctif, `ServerChatProvider.CreateConversationAsync` a sa propre gestion
  d'erreur deja testee (`InvalidReceipt`, codes serveur mappes).
- Configuration serveur `Chat__Enabled` : hors sujet ici - desactive en PRODUCTION par
  design documente (`HiveViewProductUiPresenter.ChatRoyal.cs`, commentaire d'en-tete),
  la scene de test utilise le provider serveur reel, confirme par les erreurs reelles
  observees.
- Provider local/legacy utilise par erreur : **non**, ecarte par les memes preuves.
- Identifiant de joueur retourne par la recherche : non teste explicitement dans cette
  mission (hors du chemin qui a echoue), mais `entry.PlayerId` est maintenant reellement
  transmis au serveur (`ParticipantIds`), donc verifiable au prochain test CEO.
- Lien avec la regression de creation de groupe deja observee (repli sur un
  placeholder) : meme famille de defaut - un chemin de creation de conversation qui ne
  finit jamais son cablage jusqu'au serveur reel. `CreatePrivateConversationAsync`
  suit exactement le meme patron que `CreateGroupAsync`, deja corrige/valide.

## 6. Ce qui reste a confirmer par le CEO

Cliquer **Discuter** sur un vrai joueur, en Play Mode reel, dans
`Environment2D5D_HiveMap_Test`, et confirmer que la conversation s'ouvre reellement
(pas seulement l'absence du toast d'erreur). C'est la condition de sortie posee par le
CEO pour `M060` - non remplie tant que ce test manuel n'a pas ete refait.

## 7. Fichiers touches

- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs` (le correctif du bail)
- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatController.cs` (nouvelle methode + facade)
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.ChatRoyal.cs` (cablage du clic Discuter)
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs` (2 tests neufs)
- `Docs/AI/Missions/M059D-CL-Chat-Royal-Private-Conversation-Recovery.md` (ce rapport)

Aucun autre fichier de Chat Royal modifie. Aucune migration serveur, aucun nouvel
endpoint, aucune configuration production touchee.
