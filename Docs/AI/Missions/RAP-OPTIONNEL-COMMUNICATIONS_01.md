# RAP-OPTIONNEL-COMMUNICATIONS_01 — Module Communication (Chat Royal) branché au backend réel

Date : 2026-09-07
Agent : Claude Code (reprise d'une mission interrompue par limite d'usage API)
Décision d'autorisation : le CEO (Jeff Tardif) a levé le gel du module Communication et
l'interdiction de toucher `Assets/BeeKingdom/Playground/` pour ce chantier.

---

## 1. Le problème de départ

L'écran « CHAT ROYAL » n'a **jamais** été connecté au serveur. Le diagnostic M056A l'avait
établi : les bugs 2, 3, 4 et 5 rapportés par le testeur externe n'étaient pas des régressions
mais quatre symptômes d'une seule cause — une fonctionnalité absente.

Concrètement, avant cette mission :

- l'écran affichait des conversations codées en dur (`pv-alex`, `pv-marie`…), des messages
  générés par graine fixe, et un simulateur injectait de faux messages toutes les ~16 secondes ;
- un vrai backend de chat existait et fonctionnait (`Server/src/BeeKingdom.Chat/`), avec un
  client Unity complet (`ServerChatProvider`, `LivingHiveChatController`), mais branché
  uniquement sur le tiroir de chat du QG d'alliance — jamais sur l'écran plein écran ;
- la notion de **groupe de joueurs n'existait nulle part**, ni serveur ni client ;
- « Nouvelle discussion » ouvrait un champ de recherche avec un toast « placeholder »,
  « Nouveau groupe » et « Paramètres » affichaient « arrive à un prochain sprint ».

---

## 2. Ce qui est livré

### 2.1 Serveur — groupes, invitations, préférences (travail de l'agent précédent, vérifié ici)

Cette couche était déjà écrite quand j'ai repris la mission. Je l'ai relue intégralement,
recompilée et retestée avant de bâtir dessus. Description basée sur la lecture du code :

- **Nouveau type de canal `ChatChannelType.Group`**, volontairement hors du
  `IChatAudienceResolver` : l'appartenance à un groupe n'est déductible de rien (elle est
  choisie par le créateur, un joueur à la fois, et change ensuite), donc elle est portée par une
  liste d'invitations explicite et mutable appartenant à un Leader.
- **`ChatGroupOperations.cs`** : création (idempotente via reçu `ClientRequestId`), invitation,
  réponse accepter/refuser, retrait de membre, transfert de leadership, départ du groupe,
  préférences. Deux garde-fous notables : un groupe ne peut jamais devenir sans chef tant qu'il
  reste quelqu'un dedans (`group_leader_must_transfer`), et l'`audienceKey` d'un groupe est
  aléatoire — deux groupes aux mêmes membres sont deux groupes différents et ne doivent pas
  entrer en collision comme le font délibérément les salons privés/alliance.
- **Boucle de refus côté expéditeur** : un refus reste une ligne auditée non acquittée que
  l'expéditeur récupère à son prochain sondage puis acquitte — pas un événement temps réel, car
  le temps réel est désactivé en production.
- **Préférence d'auto-réponse côté serveur** (`Ask`/`Accept`/`Decline`), appliquée au moment de
  l'émission de l'invitation : elle vaut donc même joueur hors ligne ou connecté depuis un futur
  client web. Les préférences purement cosmétiques restent locales.
- **Migration `094_chat_groups.sql`** (+ rollback), enregistrée dans `DatabaseCatalog.cs`.

Toutes les réponses résolvent les noms d'affichage côté serveur : aucun client n'a besoin d'un
second aller-retour vers l'annuaire pour afficher un groupe. C'est ce qui rend l'API réutilisable
telle quelle par le futur site web.

### 2.2 Serveur — un correctif réellement bloquant que j'ai dû ajouter

Créer un groupe exige un scope `(gameServerId, worldId)`. **Aucun client de chat ne pouvait
l'obtenir** : toute la surface `/chat/v1` n'adressait jusqu'ici que des identifiants de
conversation qu'on lui donnait, et le scope n'était exposé nulle part sous `/chat/v1`. Le client
Unity n'a d'ailleurs jamais eu de WorldId (l'écran WorldMap affiche encore « not assigned »).

Sans correctif, la seule issue aurait été de fabriquer des GUID côté client — ce qui aurait créé
des salons dans le mauvais monde, de façon invisible et irrattrapable depuis le client.

Correctif minimal et additif : `/chat/v1/capabilities` publie désormais `gameServerId` et
`defaultWorldId`. Ce sont deux propriétés `init` optionnelles ajoutées au record existant, donc
aucun site de construction ni test existant n'a changé, et les consommateurs actuels ignorent
simplement les deux champs supplémentaires. Cela rend l'API de chat auto-suffisante pour Unity
**et** pour le futur client web, qui aurait buté exactement sur le même mur.

### 2.3 Client — transport

- `ValidChannels` accepte `"Group"`. **C'était une régression latente sérieuse** : le serveur
  annonce désormais `Group` dans ses capacités, et le client rejette toute la négociation sur un
  canal inconnu — un client non mis à jour n'aurait pas « perdu les groupes », il aurait mis
  **tout le chat hors ligne**. C'est le test le plus important de la suite.
- `ServerChatProvider.Groups.cs` : `CreateGroupAsync`, `GetGroupDetailAsync`,
  `InviteToGroupAsync`, `RemoveGroupMemberAsync`, `TransferGroupLeadershipAsync`,
  `LeaveGroupAsync`, `ListInvitationsAsync`, `ListSentInvitationResponsesAsync`,
  `AcknowledgeInvitationResponsesAsync`, `RespondToInvitationAsync`, `GetChatPreferencesAsync`,
  `UpdateChatPreferencesAsync`.
- Choix explicite : ces appels **ne passent pas** par les files de rejeu hors ligne utilisées par
  l'envoi de messages. Un message en attente mérite d'être rejoué une heure plus tard ; un
  « exclure ce membre » ou un « accepter cette invitation » non — l'état du groupe a pu changer
  entre-temps et le rejeu ressusciterait une décision que le joueur a déjà vue échouer. Seule la
  *création* porte un `ClientRequestId`, parce que le serveur la dédoublonne et qu'un double
  appui ne doit pas créer deux salons.
- Les identifiants sont validés avant d'être insérés dans l'URL (un id porteur d'un séparateur
  de chemin est refusé, pas silencieusement encodé).
- `UnityChatJsonCodec.Groups.cs` : les endpoints d'invitations répondent un tableau JSON nu, que
  `JsonUtility` ne sait pas lire. Le codec l'emballe côté client plutôt que de forcer le serveur
  à une enveloppe non idiomatique que le client web devrait déballer pour rien.

### 2.4 Client — contrôleur

`LivingHiveChatController.Groups.cs` étend le snapshot avec `PendingInvitations`,
`SentInvitationResponses`, `SelectedGroup`, `AutoInviteResponse`, `GroupScopeConfigured`, et
ajoute la façade statique correspondante sur `LivingHiveChatRuntime`.

Le sondage des invitations est branché sur **le tick de sondage existant**, pas sur un second
minuteur : le backend de chat est limité en débit par joueur, un timer séparé aurait doublé le
taux de requêtes.

Le scope de groupe provient des capacités négociées, jamais d'une valeur inventée. S'il manque
(serveur plus ancien), la création est refusée avec le code `chat_group_scope_not_configured` et
l'interface le dit — c'est testé.

### 2.5 Client — interface

- **Bug de mise en page corrigé** (demande explicite du CEO). Cause : `searchH` était calculé
  *avant* `DrawChatActionBar`, qui bascule lui-même `chatSearchActive` dans le même appel. Le
  `mainTop` de la frame restait donc basé sur l'ancien état, et le champ de recherche
  apparaissait par-dessus la liste au lieu de la pousser vers le bas. La hauteur est désormais
  relue *après* la barre d'action. **Ce correctif n'avait pas été fait par M056A** — vérifié.
- **Bascule vers les données réelles** : `ChatRoyalSyncFromServer()` recopie le snapshot serveur
  dans les structures que tous les panneaux consomment déjà. Aucun panneau de dessin n'a été
  réécrit : seule la *source* des données change, le rendu reste identique.
- **Le simulateur de faux messages se tait** dès que de vraies conversations sont affichées.
- **Nouvel onglet « Groupes »** dans la barre de canaux, alimenté par les conversations de type
  `Group`.
- **« Nouvelle discussion »** ouvre un vrai sélecteur de joueur (annuaire serveur, recherche
  anti-rebond à 350 ms).
- **« Nouveau groupe »** : sélection multi-joueurs + titre + création.
- **Écran de groupe** : liste des membres avec icône couronne pour le créateur, transfert de
  leadership, exclusion, annulation d'invitation en attente, ajout de membres après coup
  (réservé au leader), départ du groupe.
- **Alerte d'invitation globale** accepter/refuser, dessinée dans le `OnGUI` du pont de chat,
  **hors** de la condition d'ouverture de l'écran Communication — donc visible même quand le
  joueur n'y est pas, comme exigé. Accepter ouvre Communication → onglet Groupes → le groupe.
  Refuser notifie l'expéditeur à son prochain sondage (toast « X a refusé de rejoindre Y »),
  annoncé une seule fois puis acquitté.
- **« Paramètres »** : couleur d'accent (5 choix, PlayerPrefs, par appareil) et règle
  d'invitation (demander / accepter / refuser automatiquement, enregistrée côté serveur).
- **Règle CLAUDE.md du 2026-09-03 respectée** : l'écran dessine ses propres sous-modaux
  par-dessus son propre contenu, donc tout le contenu principal passe par
  `DrawUnderOwnOverlayGate(...)`. Sans ça, IMGUI aurait résolu les clics des modaux sur les
  contrôles invisibles de la barre d'action et de la liste en dessous.

### 2.6 Amélioration Quality of Life (règle CLAUDE.md du 2026-08-04)

Un bandeau permanent indique si l'écran affiche des **données SERVEUR** ou la **maquette DEMO**,
avec l'état de connexion. Sans lui, un testeur ne peut pas distinguer « le chat marche » de « je
regarde des données inventées » — c'est exactement le piège qui a coûté une mission de
diagnostic entière (M056A). Quand un groupe est ouvert, le bandeau sert aussi de bouton
« Membres ».

---

## 3. Décision de conception importante : la maquette n'est pas supprimée

Les données locales deviennent un **mode démo** au lieu d'être effacées. Raison : le chat est
désactivé en PRODUCTION (`Chat__Enabled=false`, hors sujet ici, non touché). Supprimer la
maquette aurait vidé l'écran du CEO sans rien livrer en échange — c'est précisément l'argument
retenu par la mission M056A. Le mode réel prend le dessus dès qu'il y a une vraie connexion, et
le bandeau dit toujours lequel des deux est affiché : on ne fait jamais passer des données de
démo pour des données serveur.

---

## 4. Preuves

**Serveur** — `dotnet build Server/BeeKingdom.Server.slnx` : vert (avant et après mon ajout).

**Tests serveur** — `BeeKingdom.HiveOperations.Tests` : 181/181. `BeeKingdom.Tests` :
**646 réussis, 8 ignorés, 1 échec** au passage final (rejoué *après* ma modification du contrat
de capacités, donc cette modification est couverte). Premier passage : 2 échecs, dont un non
reproductible aux passages suivants — instable, non identifié.

> **Correction au rapport de l'agent précédent** : la couche serveur n'était **pas**
> entièrement verte, contrairement à ce qui a été annoncé. L'échec reproductible est
> `CatalogSqlMatchesCheckedInScriptFiles` sur `091_alliance_help.sql` (attendu 1717 caractères,
> trouvé 3092). **Il est préexistant et sans lien avec le chat** : le diff de l'agent précédent
> sur `DatabaseCatalog.cs` est purement additif (+36 lignes, 0 suppression, l'enregistrement de
> `094_chat_groups.sql`), et `091_alliance_help.sql` n'est pas modifié dans l'arbre de travail.
> Il vient de la mission M045 (Alliance Help), où le fichier `.sql` a été commenté sans mettre à
> jour la copie inline du catalogue. **C'est un vrai défaut** : le runner de migration exécute le
> SQL inline, donc ce qui tourne réellement contre la base n'est plus ce que dit le fichier
> versionné. Non corrigé ici (hors périmètre, appartient à une autre mission) mais signalé.
> Le second échec du premier passage ne s'est pas reproduit — instable, non identifié.

**Compilation Unity** — `assets-refresh` vert après chaque étape, 0 erreur console.

**Tests EditMode Unity** (nouveaux) :
- `Assets/BeeKingdom/Tests/Editor/ChatGroupTransportTests.cs` — **13/13 verts**
- `Assets/BeeKingdom/Tests/Editor/LivingHiveChatGroupControllerTests.cs` — **7/7 verts**

---

## 5. Limites connues — à lire avant de tester

1. **Aucun test en Play Mode n'a été effectué.** Le parcours complet (créer une discussion,
   créer un groupe, inviter/accepter/refuser, transférer le leadership, exclure) n'a **pas** été
   joué. Les preuves sont statiques : compilation + tests EditMode. À valider par le CEO.
2. **Aucune capture d'écran des 4 onglets** n'a été produite.
3. **La suite EditMode complète (1573 tests) n'a pas pu aller au bout** : dépassement du délai
   MCP de 300 s, exactement comme lors de M056A. La non-régression globale n'est pas prouvée.
   À rejouer en batchmode CLI.
4. **L'éditeur Unity s'est bloqué** en lançant la classe préexistante `ServerChatProviderTests`
   (ne répond plus, CPU quasi stable, même symptôme que M056A). Il n'a **pas** été tué : le titre
   de fenêtre indiquait une scène « Untitled » potentiellement non sauvegardée. Conséquences
   honnêtes :
   - `ServerChatProviderTests` et `LivingHiveChatLayoutTests` **n'ont pas pu être rejoués** avec
     mes modifications. Leur non-régression n'est pas prouvée.
   - Le blocage a orienté un soupçon vers mon propre code : j'avais ajouté deux appels réseau
     (`RefreshInvitationsAsync`, `RefreshPreferencesAsync`) dans `OpenAsync`, c'est-à-dire sur le
     chemin critique d'ouverture que cette longue suite couvre avec de faux transports comptant
     les appels. **Je les ai retirés par prudence** : les invitations arrivent de toute façon au
     premier tick de sondage et les préférences sont relues à l'ouverture de l'écran Paramètres,
     donc aucun comportement n'est perdu et `OpenAsync` retrouve exactement sa séquence d'appels
     d'origine. **Cette dernière modification n'a pas pu être recompilée** (éditeur bloqué) — à
     valider en premier à la reprise.
5. Le chat étant désactivé en production, tout ce qui précède ne se vérifie qu'avec un serveur
   de dev où `Chat__Enabled=true`.
6. Le sélecteur de joueurs exige une session de compte officielle ; sans elle il affiche
   « Annuaire des joueurs indisponible » au lieu de résultats.
7. L'ouverture d'une discussion privée depuis le sélecteur signale l'intention et rafraîchit
   l'état, mais ne crée pas encore la conversation privée côté serveur en un seul geste — le
   chemin `CreateConversationAsync` existe et est prêt, il reste à le relier à ce bouton.

---

## 6. Fichiers touchés

**Serveur (modifiés par moi)**
- `Server/src/BeeKingdom.Chat/Models/ChatContracts.cs` (scope optionnel sur les capacités)
- `Server/src/BeeKingdom.Server/Program.cs` (publication du scope sur `/chat/v1/capabilities`)

**Serveur (agent précédent, vérifiés et non modifiés)**
- `Server/src/BeeKingdom.Chat/` : `ChatManager.cs`, `ChatService.cs`, `ChatGroupOperations.cs`,
  `Models/ChatChannelType.cs`, `Models/ChatRecords.cs`,
  `Repositories/{IChatRepository,InMemoryChatRepository,SqlChatRepository}.cs`
- `Server/src/BeeKingdom.Database/DatabaseCatalog.cs`,
  `Server/src/BeeKingdom.Database/Scripts/094_chat_groups.sql` (+ rollback)
- `Server/tests/BeeKingdom.Tests/` : `ChatMessagingEndpointTests.cs`,
  `ChatTransportContractTests.cs`, `ChatGroupServiceTests.cs`

**Client — nouveaux**
- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatGroupContracts.cs`
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.Groups.cs`
- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.Groups.cs`
- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatController.Groups.cs`
- `Assets/BeeKingdom/Playground/ChatPlayerPickerController.cs`
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.ChatRoyal.cs`
- `Assets/BeeKingdom/Tests/Editor/ChatGroupTransportTests.cs`
- `Assets/BeeKingdom/Tests/Editor/LivingHiveChatGroupControllerTests.cs`

**Client — modifiés**
- `Assets/BeeKingdom/Gameplay/Communication/ServerChatProvider.cs` (partial + canal `Group`)
- `Assets/BeeKingdom/Gameplay/Communication/UnityChatJsonCodec.cs` (partial + scope)
- `Assets/BeeKingdom/Gameplay/Communication/RemoteChatContracts.cs` (scope)
- `Assets/BeeKingdom/Gameplay/Communication/LivingHiveChatController.cs` (snapshot, façade, sondage)
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (mise en page, sync, modaux, boutons)
- `Assets/BeeKingdom/Playground/LivingHiveChatBridgeBootstrap.cs` (alerte globale)
- `Assets/BeeKingdom/Playground/MobileAccountSessionRuntimeBootstrap.cs` (câblage du sélecteur)

Rien n'a été commité, poussé ni déployé. La carte terrain 50x50 et `LivingHive.unity` n'ont pas
été touchées.

---

## 7. Prochain test utilisateur

Dans `Environment2D5D_HiveMap_Test`, avec un serveur de dev où `Chat__Enabled=true` et un compte
connecté :

1. Ouvrir Communication → CHAT ROYAL. Vérifier le bandeau en haut à droite : il doit dire
   **« ● SERVEUR »** et non « ○ DEMO ». S'il dit DEMO, le reste ne prouve rien.
2. Cliquer « Recherche », puis « Nouvelle discussion » : la liste doit être **poussée vers le
   bas**, jamais recouverte (c'est le bug visuel corrigé).
3. « Nouveau groupe » → taper un titre, chercher un joueur (≥ 2 caractères), en sélectionner
   plusieurs, créer. Le groupe doit apparaître dans l'onglet Groupes.
4. Sur un second compte : l'alerte accepter/refuser doit apparaître **même en dehors de l'écran
   Communication**. Accepter doit ouvrir directement le groupe ; refuser doit faire apparaître un
   toast sur le compte de l'expéditeur au bout de quelques secondes.
5. Dans le groupe, bouton « Membres » : vérifier la couronne du créateur, le transfert de
   leadership, l'exclusion.
6. « Paramètres » : changer la couleur d'accent (doit persister au redémarrage) et passer en
   « Accepter automatiquement », puis se faire réinviter — l'invitation doit être acceptée sans
   alerte.
7. Revérifier au passage les fonctions existantes sur données réelles : favoris, recherche,
   badge non-lus, scroll.
