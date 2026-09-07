# M059D-CL — Chat Royal : récupération du chemin "Discuter" (bail de capacités + création de conversation privée)

Date : 2026-09-07
Agent : Claude Code
Scène de test : `Environment2D5D_HiveMap_Test` (jamais `LivingHive.unity`)
Statut : **trois régressions distinctes diagnostiquées et corrigées** (bail de
capacités, création de conversation privée jamais câblée, et — la cause réelle
rencontrée par le CEO au retest, voir section 10 — normalisation `null`/chaîne vide
du curseur de pagination côté client). Preuve directe capturée en Console pendant un
Play Mode réel du CEO. 9 tests EditMode ciblés verts. Rien n'a été poussé.

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

---

## 9. ÉCHEC DU RETEST CEO — la vraie cause n'était pas celle corrigée

Le CEO a refait le test exact (`Nouvelle discussion → recherche "bob" → bob trouvé →
Discuter`) après le commit `f64380d2`. **Échec identique** : `Chat serveur
indisponible : impossible d'ouvrir une discussion.`

### 9.1 Preuve runtime directe (sonde en lecture seule sur le snapshot déjà en mémoire)

```
IsConfigured=True  Status=Error  ErrorCode=invalid_conversation_cursor
Conversations=6  SelectedConversationId=c2b28689-...  PendingCount=0
```

**`invalid_conversation_cursor`, pas `capability_lease_expired`.** C'est l'erreur que
la toute première session de diagnostic avait déjà vue (avant le correctif du bail),
et elle réapparaît sur une session Play Mode **fraîche** (juste après un redémarrage
complet de l'Éditeur) — donc ce n'est PAS un bail qui a expiré après 5 minutes,
c'est un défaut qui se produit **dès le premier chargement de la liste de
conversations**. Le correctif du bail (section 3.1) reste réel et nécessaire, mais
**ne corrige pas ce que le CEO rencontre réellement**.

### 9.2 Tentative de capture directe de la cause — a provoqué un gel de l'Éditeur

Une tentative d'appeler directement `provider.NegotiateCapabilitiesAsync(...)` puis
`provider.ListConversationsAsync(...)` via un script de diagnostic a rendu l'Éditeur
non-répondant plusieurs minutes, nécessitant un redémarrage manuel par l'utilisateur.
**Cause identifiée et retenue pour ne plus la reproduire** (voir la mémoire
`feedback_unity_mcp_script_execute_hangs.md`) : l'outil MCP `script-execute` exécute
le script de façon SYNCHRONE sur le thread principal Unity (bloque sur `Task.Result`).
`UnityWebRequestAsyncOperation` a besoin que ce même thread principal continue de
tourner (sa boucle `Update`) pour déclencher son évènement `completed` — en le
bloquant depuis un script MCP, on crée un interblocage réel, pas un simple appel
lent. **Cette technique de diagnostic ne sera plus utilisée.**

### 9.3 Hypothèses écartées par lecture statique du code (sans nouvel appel réseau)

- **Encodage du curseur serveur** (`ChatService.EncodeConversationCursor`) : base64
  URL-safe sans padding, sans espace, sans caractère de contrôle possible — devrait
  toujours passer la validation cliente pour n'importe quel entier `offset`.
- **Politique de nommage JSON** : serveur configuré en `camelCase`
  (`ConfigureHttpJsonOptions` dans `Program.cs`), cohérent avec les champs `camelCase`
  attendus côté client (`WireConversationPage.nextCursor`).
- **Mauvaise interprétation d'une erreur HTTP comme succès** :
  `UnityWebRequestChatRestTransport` ne désérialise le corps que si le code de statut
  est 2xx (`ServerChatProvider.cs` ligne ~199) ; `Send<T>` vérifie `!response.IsSuccess`
  avant d'utiliser `response.Body`. Une erreur HTTP ne peut pas atterrir dans
  `NextCursor`.
- **Troncature de réponse** (`BoundedChatDownloadHandler`) produirait un
  `TransportError` différent (`RemoteChatError.Transport`), pas
  `invalid_conversation_cursor`.
- **`ValidateConversationPage` est bien le point d'origine réel** de l'exception dans
  TOUS les cas (elle est appelée à l'intérieur même de `ListConversationsAsync`,
  donc avant même que `LoadAllConversationsAsync` n'ait la main) — confirmé par
  lecture du flot d'appel, pas supposé.

**Reste non écarté, faute de pouvoir observer le curseur réel en toute sécurité** :
un défaut serveur authentique dans `EncodeConversationCursor`/`DecodeConversationCursor`
pour CE compte spécifique (utilisé intensivement pendant des mois de QA — beaucoup de
conversations Alliance/Groupe/Privé accumulées, donc une vraie pagination au-delà de
la 1ʳᵉ page est plausible), ou un cas limite non couvert par la lecture statique.

### 9.4 Diagnostic temporaire ajouté (à retirer après confirmation)

`ServerChatProvider.cs` : nouvelle méthode privée `LogInvalidCursorDiagnostic`,
appelée aux deux points d'origine de `invalid_conversation_cursor`
(`ValidateConversationPage` et le contrôle redondant de `LoadAllConversationsAsync`).
Utilise `Debug.Log` (pas `LogError`/`LogWarning`, pour ne jamais faire échouer un test
existant qui déclenche ce chemin sans s'y attendre — vérifié :
`ConversationCursorIsBoundedEscapedAndInvalidServerCursorIsRejected` reste vert).

Affiche, sans jamais logger de jeton ni de secret : l'origine de l'appel, le nombre
d'éléments de la page, si le curseur est null, sa longueur, s'il contient des
caractères de contrôle, s'il est égal à sa propre version `Trim()`, et un aperçu
tronqué (12 premiers + 12 derniers caractères seulement).

**Compilé et vérifié** (`assets-refresh` propre, 0 erreur) ; test existant
`ConversationCursorIsBoundedEscapedAndInvalidServerCursorIsRejected` toujours vert ;
les 4 tests M059D neufs/existants restés verts après l'ajout.

**Non commité intentionnellement** : c'est un diagnostic temporaire, pas le correctif
final. Il vivra dans l'arbre de travail jusqu'au prochain retest CEO, dont le message
`[M059D-CL DIAGNOSTIC]` en Console donnera la preuve exacte nécessaire pour le
correctif définitif (puis sera retiré et remplacé par ce correctif dans le commit
final).

### 9.5 Prochaine étape

Un nouveau clic **Discuter** du CEO en Play Mode fera apparaître dans la Console
Unity une ligne `[M059D-CL DIAGNOSTIC] invalid_conversation_cursor at ...` avec les
caractéristiques exactes du curseur fautif. C'est la preuve manquante pour choisir le
correctif final avec certitude plutôt que par supposition.

---

## 10. Preuve capturée — cause racine confirmée et corrigée

### 10.1 Preuve Console exacte (Play Mode réel, CEO, session fraîche)

```
[M059D-CL DIAGNOSTIC] invalid_conversation_cursor at ValidateConversationPage
| itemCount=6 | cursorIsNull=False | cursorLength=0 | cursorHasControlChars=False
| cursorTrimEqualsSelf=True | cursorPreview=
```

**`cursorIsNull=False` et `cursorLength=0`** : le curseur reçu par le client n'est pas
`null`, c'est une **chaîne vide `""`**. C'est la preuve directe et suffisante.

### 10.2 Cause racine exacte

`ChatService.ListConversations` (serveur) ne renvoie jamais que soit un curseur
encodé non-vide, soit un littéral C#/JSON `null` — jamais une chaîne vide comme
concept propre (`ChatService.cs` ligne ~174 :
`hasMore ? EncodeConversationCursor(...) : null`). Le serveur envoie donc
correctement `"nextCursor":null` dans le JSON quand il n'y a pas de page suivante
(le cas normal, quasi systématique).

Côté client, `UnityEngine.JsonUtility` — le backend JSON réellement utilisé en
production (`UnityJsonBackend`, jamais exercé par les tests EditMode existants, qui
substituent tous `SystemTextJsonBackend`) — **déserialise ce `null` JSON en chaîne C#
vide `""`, jamais en `null`**. C'est une limite documentée de `JsonUtility`, pas un
défaut serveur : `WireConversationPage.nextCursor` (un `string` simple, sans
enrobage `Nullable`) ne peut pas représenter fidèlement l'absence de valeur avec ce
parseur.

`UnityChatJsonCodec.Map` recopiait `wire.nextCursor` tel quel dans
`RemoteConversationPage.NextCursor`, donc `""` arrivait jusqu'à
`ValidateConversationPage`, dont le garde-fou ne testait que `!= null` — `""` passait
ce test et atterrissait dans `ValidateCursor("")`, qui la rejette à raison (un
curseur RÉEL ne peut pas être une chaîne vide) avec le message
`invalid_conversation_cursor`. Le contrat "`null` et chaîne vide/blanche signifient
tous deux 'pas de page suivante'" existait déjà et était correctement appliqué par
`LoadAllConversationsAsync` (`string.IsNullOrWhiteSpace`) — mais ce code n'est jamais
atteint dans ce cas précis, car `ValidateConversationPage` (appelé plus tôt, à
l'intérieur même de `ListConversationsAsync`) explose avant.

**Aucun défaut serveur. Aucun changement de contrat serveur nécessaire** — confirmé
par lecture de `ChatService.ListConversations`/`EncodeConversationCursor` : `""` n'a
jamais de signification distincte de `null` dans ce contrat.

### 10.3 Correctif appliqué (minimal, purement client)

1. **`UnityChatJsonCodec.Map(WireConversationPage wire)`** — normalise `null` ET
   chaîne vide/blanche en `null` au point unique où le défaut de `JsonUtility` est
   introduit, pour que tout consommateur en aval reçoive un contrat propre sans
   avoir à re-découvrir ce piège.
2. **`ServerChatProvider.ValidateConversationPage`** — le garde-fou
   `NextCursor != null` devient `!string.IsNullOrWhiteSpace(NextCursor)`, symétrique
   avec `LoadAllConversationsAsync`, en défense en profondeur (au cas où une future
   source de `RemoteConversationPage` ne passerait pas par le codec).
3. **Diagnostic temporaire retiré** (section 9.4) — plus nécessaire, la cause est
   prouvée et corrigée à la source.

Aucun autre chemin de pagination du client chat n'utilise un curseur `string`
nullable de ce type (vérifié par recherche exhaustive) — c'est le seul endroit
concerné.

### 10.4 Tests de régression (2 nouveaux, reproduisant la cause exacte)

- `CodecNormalizesAnEmptyNextCursorToNullMatchingNoNextPage` — déserialise
  `{"items":[],"nextCursor":""}` via le codec réel et vérifie
  `RemoteConversationPage.NextCursor == null`.
- `EmptyNextCursorFromTransportIsTreatedAsNoNextPageNotAnInvalidCursor` — fait
  transiter `NextCursor = ""` par le pipeline complet
  (`ServerChatProvider.LoadAllConversationsAsync`) et vérifie `IsComplete == true`,
  aucune exception.

**9/9 tests EditMode ciblés verts** : les 2 nouveaux ci-dessus, les 4 tests M059D
précédents (bail de capacités ×2, création de conversation privée ×2), et 3
non-régressions voisines (`ConversationCursorIsBoundedEscapedAndInvalidServerCursorIsRejected`
— le test existant qui exerce exactement ce chemin, `ConversationPaginationDeduplicatesAcrossPages`,
`ConversationCursorCycleIsRejected` — confirme qu'une vraie pagination multi-pages et
la détection de cycle de curseur continuent de fonctionner normalement).

La classe complète `ServerChatProviderTests` n'a toujours pas pu tourner en un seul
appel (limite connue de cette session, voir mémoire `feedback_unity_mcp_script_execute_hangs.md`) ;
les tests ci-dessus ont été exécutés individuellement.

### 10.5 Fichiers touchés (ce correctif)

- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.cs` (le correctif racine)
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs` (garde-fou symétrique + retrait du diagnostic)
- `Assets/BeeKingdom/Tests/Editor/ServerChatProviderTests.cs` (2 tests neufs)

### 10.6 Prochaine étape

Un nouveau clic **Discuter** du CEO en Play Mode reste la seule preuve d'acceptation
valable — voir section 6. L'attente cette fois est que la conversation s'ouvre
réellement, sans toast d'erreur.
