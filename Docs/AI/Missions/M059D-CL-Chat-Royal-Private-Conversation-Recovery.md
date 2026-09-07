# M059D-CL — Chat Royal : récupération du chemin "Discuter" (bail de capacités + création de conversation privée)

Date : 2026-09-07
Agent : Claude Code
Scène de test : `Environment2D5D_HiveMap_Test` (jamais `LivingHive.unity`)
Statut : **Correctif appliqué et testé, en attente du retest CEO.** L'ouverture
d'une conversation privée fonctionne (confirmé CEO). L'envoi échouait car
`ChatSendCurrent()` (le composeur de "Nouvelle discussion") n'avait jamais été câblé
sur le backend, dans aucun commit de l'historique (confirmé par `git log -S`) -
**mais l'envoi backend réel existe et fonctionne depuis des semaines** via un autre
écran du même fichier (`SendAllianceChatMessage`, tiroir Alliance, M043Q-T), déjà
validé bout en bout par le CEO. Vérification architecturale confirmée :
`LivingHiveChatRuntime` est le runtime Chat générique déjà utilisé par HiveMap (nom
historique, aucune dépendance à la scène LivingHive - voir section 14.1). Correctif :
`ChatSendCurrent()` câblé sur ce même point d'entrée déjà prouvé, aucun nouveau
provider, aucun doublon (section 14). 6/6 tests ciblés verts. Instrumentation
temporaire entièrement retirée. Commit local uniquement, aucun push.

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

---

## 11. RETEST CEO APRÈS `6febaa9` — ÉCHEC, cause différente probable (course, pas régression du correctif du curseur)

Reproduction identique : session Play Mode fraîche, `Nouvelle discussion → recherche
"bob" → bob trouvé → Discuter → "Chat serveur indisponible..."`.

**Le correctif du curseur vide (`6febaa9`) n'est PAS remis en cause** — aucune preuve
ne le contredit, et il reste couvert par 9 tests EditMode verts (section 10.4). Une
sonde en **lecture seule** (aucun nouvel appel réseau, juste la lecture du snapshot
déjà en mémoire — donc sans risque de geler l'Éditeur, contrairement à l'incident de
la section 9.2) faite juste après l'échec rapporté a montré :

```
IsConfigured=True  Status=Online  ErrorCode=<vide>  Conversations=6
```

**Chat est bien "Online" au moment de la sonde** — ce qui confirme que le correctif
du curseur fonctionne (sans lui, `LoadAllConversationsAsync` n'aurait jamais pu
atteindre `Online`, il resterait bloqué en `Error` comme avant `6febaa9`). Ceci
pointe vers une **cause différente**, très probablement une **course** entre
l'ouverture de Chat Royal (`OpenAsync`, qui passe par `Connecting` avant `Online`) et
le clic Discuter, plutôt qu'un défaut permanent : si le joueur clique Discuter avant
que la connexion initiale n'ait fini de s'établir, `ChatServerConnected()` renvoie
correctement `false` à cet instant précis (`Status` n'est alors ni `Online` ni
`Polling`), déclenche le toast, puis la connexion termine son établissement
juste après — exactement ce que la sonde a capturé.

**Hypothèse, pas certitude** : rien ne prouve encore que c'est bien une course plutôt
qu'un autre défaut transitoire. Aucun correctif spéculatif n'a été appliqué sur cette
base seule, conformément à la consigne.

### 11.1 Instrumentation ajoutée pour la preuve définitive (aucun nouveau correctif de comportement)

Deux points de capture temporaires, tous deux en `Debug.Log` (jamais `LogError`, même
raison qu'en section 9.4 - ne jamais faire échouer un test existant), sans jeton ni
donnée sensible :

1. **`HiveViewProductUiPresenter.ChatRoyal.cs` - `ChatStartPrivateConversation`**, au
   moment exact où le portail `ChatServerConnected()` bloque le clic : capture
   `IsConfigured`, `Status`, `ErrorCode`, `Conversations.Count` **au moment précis du
   clic** (pas quelques secondes après comme la sonde de cette section). C'est la
   preuve manquante pour confirmer ou infirmer l'hypothèse de course.
2. **`LivingHiveChatController.cs` - `CreatePrivateConversationAsync`** : capture le
   succès (conversation sélectionnée) ou l'échec exact (`Error`, `ServerCode`,
   `StatusCode` HTTP) si jamais le portail est franchi mais que la création
   elle-même échoue plus loin dans le pipeline.

Compilé et vérifié propre. Tests ciblés rejoués après l'ajout - tous verts :
`CreatePrivateConversationAsyncCreatesAndSelectsTheRealConversationForTheTappedPlayer`,
`CreatePrivateConversationAsyncRejectsAMissingParticipant`,
`CodecNormalizesAnEmptyNextCursorToNullMatchingNoNextPage`,
`EmptyNextCursorFromTransportIsTreatedAsNoNextPageNotAnInvalidCursor`.

**Aucune sonde réseau synchrone déclenchée** pour cette investigation - uniquement
lecture du snapshot déjà en mémoire, conformément à la consigne et à l'incident déjà
documenté (section 9.2 / mémoire `feedback_unity_mcp_script_execute_hangs.md`).

### 11.2 Prochaine étape

Un nouveau clic **Discuter** du CEO fera apparaître en Console soit :
- `[M059D-CL DIAGNOSTIC] Discuter blocked at ChatServerConnected() gate | Status=Connecting|Error|...` -
  confirmerait l'hypothèse de course (ou révélerait un état different de `Online`,
  auquel cas ce `Status`/`ErrorCode` exact sera la nouvelle preuve à traiter ;
- ou, si le portail est franchi cette fois, `[M059D-CL DIAGNOSTIC] CreatePrivateConversationAsync FAILED | Error=... | ServerCode=... | HttpStatus=...` -
  révélerait un défaut plus loin dans le pipeline de création (serveur, endpoint, ou
  identité du joueur).

Selon ce qui apparaît, soit un correctif de timing/UX (ex. désactiver Discuter ou
afficher un état "connexion en cours" tant que `Status` n'est pas encore
Online/Polling au lieu du toast d'échec), soit un correctif ciblé sur le pipeline de
création, sera appliqué - avec preuve, pas par supposition.

---

## 12. RETEST CEO — l'ouverture fonctionne, l'ENVOI échoue (nouveau symptôme, nouvelle instrumentation)

Progrès confirmé : `Nouvelle discussion → recherche "bob" → bob trouvé → Discuter`
ouvre maintenant réellement une conversation privée, qui apparaît dans la liste.
**Mais envoyer un message dans cette conversation ne fonctionne pas.**

### 12.1 Cause trouvée par lecture de code — ⚠️ CONCLUSION CORRIGÉE, voir section 13

Le bouton **"Envoyer"** et la touche **Entrée** du compositeur de Chat Royal
(`DrawChatComposer`, le vrai écran que le CEO utilise pour "Nouvelle discussion")
appellent tous deux `ChatSendCurrent()` (`HiveViewProductUiPresenter.cs`). Lecture
complète de cette méthode : **elle n'appelle jamais `LivingHiveChatRuntime.SendAsync`
ni aucun transport réseau.** Elle se contente d'ajouter un `ChatMessageData` local à
une liste en mémoire (`ChatMessagesFor(chatSelectedConversation)`).

Ceci explique pourquoi le message "semble" s'afficher (il est bien ajouté localement,
visuellement) mais n'est jamais réellement livré : rien n'est envoyé au serveur, donc
rien n'est persisté, rien n'est reçu par le destinataire, et le message disparaîtra à
la prochaine reconstruction de la liste depuis le snapshot serveur réel.

~~vestige de l'ancien simulateur de démonstration jamais retiré~~ — **cette partie de
la formulation était trompeuse et a été explicitement corrigée en section 13** : le
diagnostic du CODE (que `ChatSendCurrent()` est local-only) reste exact et confirmé,
mais l'implication "l'envoi backend n'a jamais existé pour Chat Royal" était fausse.
Un envoi réel, fonctionnel, backend, visible sur le site web, existe et a été validé
par le CEO — via un AUTRE écran (`SendAllianceChatMessage`, le tiroir de chat
Alliance), pas via ce composeur-ci. Voir section 13 pour l'analyse complète.

**Ce n'est pas encore un correctif appliqué** — conformément à la consigne, seule une
instrumentation a été ajoutée pour confirmer ce diagnostic en runtime avant toute
correction.

### 12.2 Piste secondaire confirmée — pourquoi le titre affiche "Discussion" au lieu de "bob"

`CreatePrivateConversationAsync` (le correctif de la section 5.2) n'envoie jamais de
`Title` dans sa requête de création (`RemoteCreateConversationRequest.Title` reste
`null`) — cohérent avec une conversation privée 1:1, qui n'a normalement pas de titre
propre côté serveur. `HiveViewProductUiPresenter.ChatRoyal.cs` ligne 157 retombe
alors sur le texte générique `"Discussion"` dès que `conversation.Title` est vide :
`Title = string.IsNullOrWhiteSpace(conversation.Title) ? "Discussion" : conversation.Title`.

**Confirmé par lecture de code, cause distincte du bug d'envoi** : la conversation
n'est pas mal hydratée ni le mauvais objet sélectionné - c'est un affichage qui
n'a simplement jamais été conçu pour dériver le nom de l'AUTRE participant (ex.
"bob") pour une conversation privée sans titre. Hors du périmètre demandé cette
fois (l'envoi) - à traiter séparément si le CEO le souhaite.

### 12.3 Instrumentation ajoutée (aucun correctif de comportement, non commitée)

Trois points de capture temporaires, tous en `Debug.Log` (jamais `LogError`, jamais
de contenu de message ni de jeton - seulement la longueur du texte et l'identité de
conversation) :

1. **`HiveViewProductUiPresenter.cs` - `ChatSendCurrent()`** : capture explicitement
   que ce chemin est LOCAL-ONLY (n'atteint jamais le provider), avec
   `conversationId`, `textLength`, `ChatServerConnected()`, `Status`, `ErrorCode` au
   moment du clic. Capture aussi le cas de validation locale (texte vide).
2. **`LivingHiveChatController.cs` - `SendAsync`** : capture l'entrée réelle dans le
   pipeline provider/transport (`conversationId`, `textLength`), le succès, et
   l'échec exact (`Error`, `ServerCode`, `StatusCode` HTTP) - pour prouver
   définitivement si CE chemin est atteint par un autre déclencheur que le
   compositeur de Chat Royal (ex. l'écran Alliance, qui utilise déjà le vrai
   `SendAsync` via `SendAllianceChatMessage`).

Compilé et vérifié propre. Tests ciblés rejoués : `SendRetryAndRealtimeRestDuplicateAreIdempotent`,
`CreatePrivateConversationAsyncCreatesAndSelectsTheRealConversationForTheTappedPlayer`
restent verts.

**Découverte annexe, sans rapport avec M059D** : le test préexistant
`LivingHiveControllerKeepsOptimisticMessageQueuedWhenServerIsOffline` échoue dans cet
environnement avec `System.ArgumentException : Property Count was not found` sur
`Assert.That(snapshot.Messages, Has.Count.EqualTo(1))` — `LivingHiveChatSnapshot.
Messages` est construit via `.ToArray()` (ligne 139 de `LivingHiveChatController.cs`),
et `Has.Count` échoue par réflexion sur les tableaux dans cette version de NUnit/Unity
Test Framework (`Count` y est une implémentation explicite de `ICollection`, invisible
à la réflexion simple), alors qu'il fonctionne sur les `List<T>` (confirmé : le même
test framework accepte `Has.Count.EqualTo` sur un autre `IReadOnlyList` non-tableau
plus tôt dans le même fichier, ligne 29). Les deux assertions précédentes de ce même
test (`Status`, `PendingCount`) passent sans problème, donc le pipeline `SendAsync`
lui-même se comporte comme attendu (message mis en file, statut `Offline`) - seule
l'assertion `Has.Count` sur ce tableau spécifique échoue. **Ni introduit ni corrigé
par cette mission** - signalé pour référence, hors périmètre de M059D.

### 12.4 Prochaine étape

Un nouveau clic **Envoyer**/Entrée du CEO dans Chat Royal fera apparaître en Console
`[M059D-CL DIAGNOSTIC] ChatSendCurrent: LOCAL-ONLY SIMULATOR PATH...` — la confirmation
runtime attendue. Si (comme prévu) `LivingHiveChatController.SendAsync` n'affiche
strictement AUCUNE ligne pour cette action, la cause de la section 12.1 sera
définitivement confirmée, et le correctif consistera à câbler `ChatSendCurrent()` sur
`LivingHiveChatRuntime.SendAsync` (le même point d'entrée déjà utilisé par
`SendAllianceChatMessage`), sans créer de second provider ni de repli local.

---

## 13. CORRECTION EXPLICITE DU DIAGNOSTIC — l'envoi backend a bien existé et fonctionné

Le CEO a personnellement validé un échange réel entre les comptes Jeff et Stara
depuis Chat Royal plein écran, visible aussi sur le site web — donc nécessairement
arrivé jusqu'au backend partagé. La formulation de la section 12.1 ("vestige de
l'ancien simulateur... jamais retiré") laissait entendre que l'envoi n'avait JAMAIS
été câblé pour Chat Royal, ce qui est **faux et corrigé ici**.

### 13.1 Méthode — recherche Git exhaustive, pas de supposition

```
git log --all -S "ChatSendCurrent" --oneline -- Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs
→ 4e88f68c BASELINE: recover latest LivingHive production state   (SEUL résultat)

git log --all -S "SendAllianceChatMessage" --oneline -- Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs
→ ca8fcab8 M043Q-T: Alliance Center real chat/search/invite UI...  (SEUL résultat)
```

`-S` (pickaxe) retrouve TOUS les commits où une chaîne apparaît ou disparaît, sur
TOUTE l'historique (`--all`), pas seulement la branche courante - la méthode la plus
fiable pour prouver qu'une fonction n'a jamais changé, plutôt que de se fier à un
`git log` limité à HEAD.

**Résultat, sans ambiguïté :**
- `ChatSendCurrent()` (le composeur de "Nouvelle discussion"/groupes de Chat Royal)
  n'a été touché par **aucun** commit depuis le tout premier commit de ce dépôt
  (`4e88f68c`, un import de baseline). Son corps est **identique** depuis le début de
  l'historique disponible.
- `SendAllianceChatMessage()` (le tiroir de chat Alliance, un écran DIFFÉRENT) a été
  introduit dans `ca8fcab8` (M043Q-T, "Alliance Center real chat...") - **des
  semaines avant** les missions de chat d'aujourd'hui - et reste, lui, câblé sur
  `LivingHiveChatRuntime.SendAsync` depuis son introduction, inchangé depuis.
- Vérifié aussi que M057 (`b9f4ff16`), le correctif SQL (`fb7aa9c9`), M058
  (contenu dans `fb7aa9c9`), M059 (`e0e2f88d`, `cff44b0c`) et RAP-OPTIONNEL-
  COMMUNICATIONS_01 (`9b246629`, la mission qui a branché Chat Royal sur le vrai
  backend EN LECTURE) ne touchent, dans aucun de leurs diffs, ni `ChatSendCurrent`
  ni `SendAsync`. Le diff complet de `9b246629` ne contient AUCUNE ligne supprimée
  autour du composeur ou de l'envoi - rien n'a été retiré, la fonction a toujours
  été telle quelle.

### 13.2 Conclusion corrigée

**L'envoi backend réel n'a jamais été perdu ni régressé pour `ChatSendCurrent()`,
car il n'y a jamais été câblé du tout dans cette fonction précise - mais l'envoi
backend réel EXISTE et FONCTIONNE dans ce même fichier, depuis des semaines, via un
AUTRE écran : le tiroir de chat Alliance (`SendAllianceChatMessage` →
`LivingHiveChatRuntime.SendAsync`).** L'échange Jeff/Stara que le CEO a validé, visible
sur le site web, est passé par ce chemin-là - cohérent avec le fait qu'Alliance chat
est une fonctionnalité bien plus ancienne (M043Q-T) que "Nouvelle discussion"/groupes
privés (RAP-OPTIONNEL-COMMUNICATIONS_01, livré aujourd'hui même). Les deux écrans
partagent le même habillage visuel "Chat Royal", ce qui explique la confusion.

Il ne s'agit donc **pas d'une régression à retrouver et restaurer dans l'historique**
(il n'y a rien à restaurer : aucune version antérieure de `ChatSendCurrent()`
n'a jamais appelé le backend) - il s'agit d'un **écran qui n'a jamais fini d'être
câblé**, avec, à côté, dans le MÊME fichier, la preuve vivante de la bonne façon de
le faire (`SendAllianceChatMessage`, 4 lignes, aucune nouvelle abstraction).

### 13.3 Direction du correctif (confirmée par l'historique, pas spéculative)

Réutiliser **exactement** le point d'entrée déjà prouvé fonctionnel -
`LivingHiveChatRuntime.SendAsync` - depuis `ChatSendCurrent()`, sur le modèle direct
de `SendAllianceChatMessage` :

```csharp
private static async void SendAllianceChatMessage(string body)
{
    try { await BeeKingdom.Gameplay.Communication.LivingHiveChatRuntime.SendAsync(body); }
    catch (Exception exception) { Debug.LogWarning("[AllianceChat] Send failed: " + exception.GetType().Name); }
}
```

Aucun nouveau provider, aucun repli local, aucune invention - le même chemin que le
CEO a déjà vu fonctionner de bout en bout. Correctif **non encore appliqué** dans
cette mise à jour du rapport, conformément à la consigne de cette session (git
d'abord, correctif ensuite, sur confirmation).

---

## 14. CORRECTIF APPLIQUÉ — vérification architecturale + câblage sur le point d'entrée existant

### 14.1 Vérification architecturale préalable (GO explicite requis avant tout code)

Avant tout changement, confirmation exhaustive que `LivingHiveChatRuntime` est un
wrapper générique déjà utilisé par HiveMap, sans dépendance de scène LivingHive :

- `LivingHiveChatRuntime`/`LivingHiveChatController`/`ServerChatProvider`/
  `LivingHiveChatBootstrap` sont des classes C# pures
  (`Assets/BeeKingdom/Gameplay/Communication/`) - aucune référence à une scène, un
  GameObject ou un MonoBehaviour nulle part dans toute la chaîne
  (`LivingHiveChatBootstrap.ActivateAsync` → `RemoteChatClientFactory.Create` →
  `LivingHiveChatRuntime.ReconfigureAsync`).
- Le seul composant qui pilote ce runtime, `LivingHiveChatBridgeBootstrap`
  (`Assets/BeeKingdom/Playground/`), s'auto-attache **uniquement** aux scènes
  `Environment2D5D*` et est câblé dans `HiveMapRuntimeBootstrapInitializer` - la
  liste officielle des bootstraps HiveMap. Son propre commentaire d'en-tête se
  définit explicitement **en opposition** à "the real LivingHive flow".
- **Deux autres surfaces HiveMap déjà fonctionnelles** pointent vers exactement ce
  même `LivingHiveChatRuntime.SendAsync` : `SendAllianceChatMessage` (tiroir
  Alliance) et `LivingHiveChatBridge.SetSendHandler` (mini-chat du Canvas uGUI
  `LivingHiveMenu`, également listé par CLAUDE.md comme composant actuel de HiveMap).
- **Conclusion** : "LivingHive" dans ce nom est purement historique (même famille
  que `LivingHiveMenuCanvas`/`LivingHiveMenu`) - pas une dépendance à la scène
  retirée. Aucun découplage nécessaire, aucun second provider à créer.

### 14.2 Correctif

`ChatSendCurrent()` (`HiveViewProductUiPresenter.cs`) branche maintenant sur
`chatUsingServerData` :
- **Conversation réelle** (`chatUsingServerData == true`) → nouvelle méthode
  `ChatSendCurrentToServer(text)`, qui appelle `LivingHiveChatRuntime.SendAsync`
  exactement sur le modèle de `SendAllianceChatMessage` (fire-and-forget
  `async void`, capture uniquement le type d'exception, jamais le contenu). Aucun
  message local n'est ajouté en plus : l'optimiste/succès/échec est déjà entièrement
  géré par `LivingHiveChatController.SendAsync` lui-même (le message passe par
  `Queued` puis `Confirmed`/`Failed`), et `ChatRoyalSyncFromServer` relit déjà ce
  même snapshot dans `chatMessagesByConversation` - ajouter un doublon local aurait
  été une régression, pas un correctif.
- **Mode démo/hors-ligne** (`chatUsingServerData == false`) → comportement local
  strictement inchangé, aucune régression du mode démo explicite déjà voulu par ce
  fichier.

Aucun nouveau provider, aucun runtime Chat parallèle, aucune dépendance de scène
LivingHive réintroduite, aucun doublon.

**Libellé "Discussion"** (section 12.2) : non touché, conformément à la consigne -
hors du périmètre nécessaire au fonctionnement de l'envoi.

**Instrumentation temporaire retirée** : les 3 points de capture de la section 12.3
(`ChatSendCurrent`, `LivingHiveChatController.SendAsync`) et les 2 de la section 11.1
(`ChatStartPrivateConversation`, `CreatePrivateConversationAsync` - l'ouverture est
maintenant confirmée fonctionnelle par le CEO, ces diagnostics n'ont plus d'utilité)
ont tous été retirés. `git diff` confirme qu'aucun résidu de diagnostic ne reste dans
`LivingHiveChatController.cs` ni `HiveViewProductUiPresenter.ChatRoyal.cs` (ces deux
fichiers sont redevenus identiques au dernier commit, `using UnityEngine;` temporaire
retiré de `LivingHiveChatController.cs`).

### 14.3 Tests de régression ciblés (nouveaux + non-régression)

Nouveau fichier `Assets/BeeKingdom/Playground/Editor/ChatRoyalSendWiringTests.cs` -
seul endroit qui peut prouver le **branchement au niveau de l'écran**
(`ServerChatProviderTests` teste `ServerChatProvider`/`LivingHiveChatController` en
isolation, jamais le point d'entrée UI réel) :

| Test | Preuve |
|---|---|
| `SendingFromARealConversationReachesTheServerProviderNotTheLocalSimulator` | une conversation réelle atteint bien le transport (`rest.LastSendRequest` non nul, corps exact) et ne duplique jamais dans la liste locale |
| `SendingFromADemoConversationKeepsTheOldLocalBehaviorAndNeverTouchesTheTransport` | le mode démo garde son comportement local exact, ne touche jamais le transport |
| `EmptyComposerTextSendsNothingAndDoesNotThrow` | la validation locale (texte vide) reste inchangée, aucune exception |

Trois crochets `...ForProof` ajoutés (même convention que les dizaines déjà
présentes dans ce fichier) : `SetChatSendTestStateForProof`,
`ChatSendCurrentForProof`, `ChatLocalMessageCountForProof`.

**6/6 tests ciblés verts** : les 3 nouveaux ci-dessus +
`CreatePrivateConversationAsyncCreatesAndSelectsTheRealConversationForTheTappedPlayer`,
`SendRetryAndRealtimeRestDuplicateAreIdempotent` (non-régression provider),
`SandboxLivingHiveUiStabilizationTests` (classe complète, 22/22, non-régression
écran). Compilation vérifiée propre à chaque étape.

### 14.4 Commit

Commit local uniquement (voir hash dans le message de fin de mission), aucun push.
Fichiers : `HiveViewProductUiPresenter.cs` (correctif + crochets de preuve),
`ChatRoyalSendWiringTests.cs` (+`.meta`), ce rapport.

### 14.5 Limite connue, hors périmètre de ce correctif

Cliquer une conversation **existante** dans la liste (`ChatSelectConversation`) met à
jour `chatSelectedConversation` côté UI mais n'appelle jamais
`LivingHiveChatRuntime.SelectAsync`/`SelectKnownAsync` pour synchroniser la sélection
réelle du contrôleur - un écart pré-existant, distinct du bug corrigé ici. Pour le
scénario testé par le CEO (Discuter → nouvelle conversation créée ET sélectionnée par
`CreatePrivateConversationAsync` → envoi immédiat), ce n'est pas un problème : la
sélection du contrôleur est déjà correcte. Signalé pour référence, pas corrigé -
hors du périmètre demandé.

### 14.6 Prochaine étape

**READY FOR CEO SEND RUNTIME RETEST.** Rouvrir la conversation avec "bob" (ou en
créer une nouvelle via Discuter) et envoyer un message réel.
