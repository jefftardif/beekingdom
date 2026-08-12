# Passation de session — État du projet Bee Kingdom (2026-07-27)

**Auteur:** Claude Code (session terminee le 2026-07-27, avant `/clear`)
**But de ce fichier:** permettre a une session Claude Code completement neuve de reprendre
sans perdre de contexte, ni sur la session UI qui vient de se terminer, ni sur le
chantier chat/messagerie qui devient la priorite suivante.

## 0. Comment utiliser ce document

1. Lis ce fichier en entier d'abord.
2. Puis suis la liste de lecture obligatoire de `CLAUDE.md` (racine du projet),
   en particulier `Docs/Claude/Claude_Continuation.md` — deja a jour, ne pas le
   dupliquer ici.
3. **Correction importante (2026-07-27, apres redaction initiale de ce
   fichier):** le chat/messagerie N'EST PAS le chantier prioritaire. Jeff a
   explicitement dit qu'il ne veut pas reprendre le chat en priorite. La
   section 2 ci-dessous reste comme investigation factuelle archivee (utile
   si le chat redevient un jour prioritaire) mais **ne doit pas etre traitee
   comme une tache a commencer**.
4. **Mode de travail reel actuel:** Jeff teste en direct les differentes
   fenetres et boutons du jeu en Play Mode et rapporte au fur et a mesure ce
   qu'il decouvre a ameliorer, sur n'importe quel systeme du jeu (pas un seul
   chantier fixe). Voir section 3 pour t'orienter rapidement dans
   l'ensemble du code et des fonctionnalites du jeu, pour pouvoir repondre
   efficacement a n'importe quel retour, ou qu'il porte.

## 1. Ce qui vient d'etre termine (session du 2026-07-26/27, Hive UI)

Deja documente en detail dans `Docs/Claude/Claude_Continuation.md` (2 entrees en
tete). Resume pour memoire rapide, ne pas retravailler sans besoin explicite:

- Chapitre 2 du tutoriel etendu ("Consolidation des cellules").
- Raccourci developpeur "Sauter Acte I" (menu splash + bouton bas de la ruche).
- Nettoyage des panneaux de debug WorldMap, nouvelle barre de navigation stylee
  (localiser/retour a la ruche), avec deux vrais bugs corriges (ordre de
  generation de chunk, superposition de boutons).
- Corrections de troncature de texte (side rail Construction/Entrainement/
  Recherche, panneau de detail de batiment).
- Retraits UI demandes: notice "production a ton retour", texte de
  divulgation "cache appareil non protege", icones d'abeilles animees.
- Icone de zone cliquable ouvrant une popup d'info batiment, avec textes
  complets et honnetes (FR/EN) pour les 14 batiments/zones.
- Construction complete de la fonctionnalite "Vue d'ensemble de la colonie"
  (plein ecran depuis le Coeur royal): bandeau, liste scrollable
  troupes/batiments/ressources/progression, pages de detail par batiment.
- **Dernier ajout (2026-07-27):** integration des 14 images HD reelles par
  batiment (fournies par Jeff dans `C:\projets\beekingdom\imagesBuildings\`,
  copiees dans
  `Assets/BeeKingdom/Playground/Resources/PremiumBeeReference/BuildingBanners/`)
  dans les bandeaux des pages de detail de la Vue d'ensemble, remplacant le
  fallback generique teinte. Compilation verifiee sans erreur.
- Question encore ouverte posee a Jeff (pas de reponse recue avant `/clear`):
  faut-il aussi utiliser ces images dans la popup d'info batiment ou le
  bandeau de la vue liste generale?

Aucune suite de tests automatisee n'a ete relancee cette session (choix
deliberé pour eviter de repeter un incident anterieur ou `tests-run` a
plante en executant pendant que Jeff etait en Play Mode — verification
manuelle en Play Mode privilegiee pour les changements UI-only).

## 2. Mode de travail actuel — tests live et ameliorations au fur et a mesure

Jeff est en train de tester en Play Mode les differentes fenetres et boutons
du jeu (ruche, carte du monde, panneaux de detail, tutoriel, etc.) et rapporte
au fur et a mesure ce qu'il decouvre a corriger ou ameliorer. **Il n'y a pas
un chantier fixe unique en ce moment** — chaque retour peut porter sur
n'importe quel systeme du jeu. La session suivante doit donc:

- avoir une bonne vue d'ensemble de l'architecture ET des fonctionnalites du
  jeu (section 3 ci-dessous) pour pouvoir reagir vite a n'importe quel retour;
- traiter chaque retour de Jeff comme une petite tache independante (corriger,
  verifier la compilation, documenter dans `Claude_Continuation.md` en fin de
  session), sans supposer un fil conducteur impose entre deux retours;
- continuer a mettre a jour `Docs/Claude/Claude_Continuation.md` a la fin de
  chaque tache significative, comme mandate par `CLAUDE.md`.

## 3. Orientation generale — comment retrouver vite l'architecture et les fonctionnalites du jeu

Ce jeu est trop vaste pour etre resume ici; utilise plutot ces points d'entree
pour te reperer rapidement selon ce sur quoi porte le retour de Jeff:

**Lecture produit/fonctionnelle (obligatoire selon `CLAUDE.md`):**
- `Docs/Product/BeeKingdom_LivingHive_ExecutionPlan.md` — plan produit
  d'ensemble de la ruche vivante.
- `Docs/Benchmarks/AntLegion/AntLegion_BeeKingdom_FunctionalReference.md` —
  reference fonctionnelle (jeu concurrent) a depasser, pas a copier.
- `Docs/Demos/LivingHive.md` — demo/etat fonctionnel de la ruche.
- `Docs/Product/BeeKingdom_Localization.md` — regles de localisation
  (tous les textes doivent passer par `BeeLocalization`).

**Code — les deux monolithes UI qui portent l'essentiel du jeu jouable:**
- `Assets/BeeKingdom/Playground/HiveViewProductUiPresenter.cs` (~33 000+
  lignes) — quasi toute l'UI et la logique de la vue Ruche: hotspots de
  batiments (`ReferenceHotspots`), tutoriel guide (machine a etats
  `GuidedCollectionTutorialStep`), menus (`HiveMenuMode`), panneaux de
  detail, Vue d'ensemble de la colonie, etc.
- `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs`
  — UI et logique de la carte du monde (scene canonique
  `WorldMapWave6Wave5Method12288Preview.unity`): chunks, hotspots de ruches
  adverses, ressources, bestiaire, barre de navigation.

**Serveur (si un retour touche a la persistance/au reseau, hors chat):**
- `Server/src/BeeKingdom.Server/` — projet serveur principal (.NET 8).
- `Server/src/BeeKingdom.Database/` — migrations SQL.
- Voir aussi les rapports `Docs/Architecture/ARCH-*.md` et
  `Docs/Architecture/LeadArchitect_TakeoverReport_2026-07-23.md` pour l'etat
  d'ensemble de l'architecture serveur si besoin de contexte plus profond.

**Fondations protegees — a ne jamais toucher sans demande explicite (rappel
`CLAUDE.md`):** carte terrain 50x50 (`WorldMapWave6Wave5Method12288Preview.unity`
et son package `UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview`),
image de base de la ruche `LivingHive`.

Reflexe recommande avant toute correction: utiliser Grep/Explore pour
localiser precisement la fonction/le panneau concerne dans l'un de ces deux
fichiers plutot que de supposer son emplacement, puis verifier en Play Mode
apres correction (compilation via `console-get-logs`, verification manuelle
— voir section 1 pour la raison de ne pas relancer `tests-run` pendant que
Jeff est en Play Mode).

## 4. [ARCHIVE — PAS LA PRIORITE ACTUELLE] Chat et messagerie

Jeff a confirme ne pas vouloir reprendre le chat en priorite. Ce qui suit est
une investigation factuelle du depot faite le 2026-07-27 (lecture reelle des
fichiers, pas de suppositions), conservee pour reference si ce chantier
redevient prioritaire un jour. **Ne pas commencer ce travail sans demande
explicite de Jeff.**

### 4.1 Chronologie reconstruite

- **15-16 juillet 2026**: `Docs/WorldMapCommunication/ChatMessaging_LocalArchitecture_Spec.md`
  definit le contrat chat local (4 canaux, idempotence, `IChatProvider`).
  Phases serveur 1/2/3 (`ChatMessaging_ServerPhase{1,2,3}_Report.md`)
  construisent reellement `Server/src/BeeKingdom.Chat` (endpoints REST,
  repository SQL, hub SignalR), chaque rapport insistant: `Chat:Enabled=false`,
  aucune action sur `104.129.128.136`.
- **16-17 juillet**: plusieurs checkpoints (`ChatMessaging_ServerAccessBinaryCheckpoint.md`,
  `ChatMessaging_ServerIIS_Checkpoint.md`, `ChatMessaging_ProductionReadiness_*`,
  `ChatMessaging_PreLiveSwitch_Checkpoint.md` puis `ChatMessaging_PostLiveSwitch_Checkpoint.md`)
  affirment noir sur blanc un **deploiement IIS reel** sur `srvesdt`
  (`104.129.128.136`): site `BeeKingdom.ChatApi`, certificat Let's Encrypt
  `chat.dravii.com`, SQL `.\SQLEXPRESS01`, migrations appliquees, et
  `CHAT_ENABLED=YES` / `CHAT_REALTIME_ENABLED=YES` / `Chat__Enabled=true` dans
  le `web.config` reel du serveur. Une page de test publique existait
  (`https://chat.dravii.com/test-chat/`).
- **Doute officiel**: le mandat qui suit (`Communication_Agent_ParallelProduction_Goal.md`)
  instruit explicitement de ne **jamais** se fier aux checkpoints du 16-17
  juillet sur la seule foi du texte — preuve que la coordination du projet ne
  faisait deja plus confiance a ce qui precede.
- **21-22 juillet (reprise)**: `Docs/AgentCoordination/Communication_VM_Assignment.md`
  (identique a `Docs/Communication_VM_Assignment.md`) mandate un agent
  `Communication` pour construire un pont Unity -> serveur **existant**, sans
  jamais reactiver la production. Une quarantaine de jalons
  `ChatMessaging_*Milestone_2026-07-21.md` construisent ensuite cote Unity un
  provider distant complet (`ServerChatProvider`, transport
  `UnityWebRequestChatRestTransport`, outbox durable, reconciliation de
  sequence, traduction UI) — en **REST polling**, chaque rapport repetant que
  "SignalR reste optionnel". Cote serveur, des "candidats production locaux"
  sont prepares sous `Server/artifacts/candidates/`, tous marques
  `DeploymentAuthorized=false`, jamais transferes.
- **Etat le plus recent confirme**: aucun deploiement effectif signale apres
  le 21-22 juillet. Toutes les portes (SQL jetable, .NET natif, TLS, Android
  staging) restent ouvertes.

### 4.2 DECOUVERTE CRITIQUE — a traiter si/quand ce chantier reprend

`Server/src/BeeKingdom.Server/appsettings.Production.json`, **actuellement
committe dans le depot**, contient:

```json
"Chat": { "Enabled": true, "RealtimeEnabled": true, ... }
```

alors que:
- le defaut cote code (`ChatOptions.cs`) est `false`/`false`;
- **tous** les rapports du 21-22 juillet affirment explicitement que ces
  valeurs n'ont pas ete activees;
- le mandat existant dit noir sur blanc: *"Ne change pas `Chat:Enabled` ou
  `Chat:RealtimeEnabled` en production."*
- le fichier historique fige `Server/artifacts/chat-prod-prep/BeeKingdom.Server/appsettings.Production.json`
  (16 juillet) a bien `false`/`false` — donc la valeur `true`/`true` dans le
  fichier **actif** du depot est une regression ou un oubli, pas un etat
  voulu.

**Si ce fichier etait deploye tel quel, le chat et le temps reel
s'activeraient par defaut en production.** Ceci doit etre verifie et corrige
(remettre `false`/`false`, ou confirmer avec Jeff que c'est un choix
deliberé) avant toute autre action sur ce chantier. Ne PAS deployer ce
fichier sans avoir clarifie ce point avec Jeff.

### 4.3 Canaux de chat exacts (source faisant autorite)

Definis dans `Docs/WorldMapCommunication/ChatMessaging_LocalArchitecture_Spec.md`
§2, confirmes par `ChatMessaging_UnityClientServerContract.md` et par la
reponse reelle de `GET /chat/v1/capabilities`. **Quatre canaux exactement**:

- `Alliance` — membres actifs de l'alliance
- `Server` — tous les joueurs authentifies du serveur/monde (affiche
  **"Global"** dans l'UI IMGUI Phase 1)
- `Private` — participants explicites, max 20 destinataires
- `Leaders` — role `officer`/`leader` uniquement

Libelles francais confirmes dans l'UI de test: **Alliance, Global, Privé,
Dirigeants**.

### 4.4 Etat reel cote serveur (code, verifie directement — pas la doc)

- `Server/src/BeeKingdom.Chat/Realtime/ChatRealtimeHub.cs`: hub SignalR
  **fonctionnel** — valide le bearer token, abort la connexion si
  `!Enabled || !RealtimeEnabled`, verifie reellement l'appartenance a la
  conversation (`chat.EnsureCanRead`) pour `JoinConversation`/`LeaveConversation`.
- `SignalRChatRealtimeDispatcher.cs`: publie sur le groupe
  `conversation:{id}` via `chat.event`, no-op si les flags sont faux.
- `ChatTranslationService.cs`: contrat complet et teste (validation BCP-47,
  cache `(MessageId,TargetLocale,ModelVersion)`, rate-limit, 500/1000
  caracteres) **mais aucun fournisseur reel n'est branche**:
  `ChatServiceCollectionExtensions.cs` n'enregistre `DeepLChatTranslationProvider`
  que si une cle DeepL est configuree, sinon **`UnavailableChatTranslationProvider`**
  (503 systematique). Le bouton "Traduire" demande par Jeff n'a donc
  actuellement **aucun moteur de traduction operationnel** derriere lui.

### 4.5 Etat reel cote client Unity (code, verifie directement)

- `Assets/BeeKingdom/Gameplay/Communication/SignalRChatRealtimeTransport.cs`
  est un **vrai** transport SignalR (pas un placeholder): `HubConnectionBuilder`,
  bearer token, reconnexion automatique, join/leave conversation. Coexiste
  avec `ServerChatProvider`/`UnityWebRequestChatRestTransport` (REST +
  polling de secours).
- Composants UI/logique existants: `LivingHiveChatController.cs` (etats
  `NotConfigured/Connecting/Online/Polling/Offline/...`),
  `LivingHiveChatBootstrap.cs` + `LivingHiveChatSessionCoordinator` (cycle de
  vie lie a la session), `ChatIngamePanel.cs` (panneau IMGUI de test,
  raccourci `F9`, **`LocalChatProvider` uniquement**, pas branche sur le
  serveur reel).
- Preuve directe de l'etat actuel:
  `Docs/WorldMapCommunication/Evidence/LivingHiveChat/LivingHiveChat_CaptureManifest.md`
  dit explicitement: *"Etat fournisseur: `NotConfigured` (honnete, aucun shell
  auth de production branche)"*. **Aucune fenetre de chat n'est actuellement
  montee/visible en jeu contre un vrai compte.** L'integration dans les
  fenetres ruche (`HiveViewProductUiPresenter.cs`) et carte
  (`WorldMapMmoFullscreenFoundationBootstrap.cs`) — l'objectif meme donne par
  Jeff — **reste entierement a faire**.

### 4.6 Ecarts documentation vs code reel (a garder en tete, ne pas se fier aveuglement aux .md)

1. **Le plus grave**: config de production committee incoherente avec la doc
   (section 2.2).
2. Les checkpoints du 16-17 juillet affirment un chat **deja mis en ligne
   reellement** (IIS/SQL reels); aucune preuve trouvee d'un rollback explicite
   depuis. Ne pas supposer que la prod est "vierge" sans verification directe
   du serveur reel.
3. Chaque jalon du 21 juillet repete "SignalR reste optionnel/non integre"
   alors qu'un vrai transport SignalR fonctionnel existe deja dans le depot.
4. Contrat de traduction complet documente sur des mois, mais aucun
   fournisseur reel cable (voir 2.4).

### 4.7 Artefacts de deploiement deja prepares

- `Server/artifacts/chat-prod-prep/` (16 juillet, config figee avec
  `Chat.Enabled=false`).
- `Server/artifacts/candidates/` (21 juillet, 3+ candidats horodates, tous
  `DeploymentAuthorized=false` dans `CANDIDATE-STATUS.json`).
- `tools/vm-sync/vm-sync-last-report.txt` (22 juillet): signale l'existence
  d'encore plus de contenu chat non couvert par cette investigation (axe
  "cloisonnement mobile par joueur", stockage protege Android-Keystore) dans
  `ChatMessaging_ServerConsolidation_2026-07-21.md` — a lire avant de
  commencer l'implementation si le temps le permet.

### 4.8 Decisions d'architecture — RIEN n'est encore decide

Aucune decision n'a ete prise cette session sur:
- ou monter le composant UI chat dans les fenetres ruche/carte;
- comment il communiquera avec le hub existant (reutiliser
  `SignalRChatRealtimeTransport`/`ServerChatProvider` tels quels, ou les
  adapter);
- l'emplacement exact et la logique du bouton "Traduire" (detection de
  langue differente du francais).

Ce sera la premiere vraie tache de la prochaine session, apres la
verification de securite 2.2.

### 4.9 [Archive chat] Travail fait vs restant

**Fait (par des sessions anterieures, code reel verifie):** contrat serveur
chat + traduction, hub SignalR, transport Unity SignalR et REST/polling,
outbox/journal durable, panneau de test IMGUI local.

**Restant (rien de ceci n'a ete commence):**
- Corriger/valider `appsettings.Production.json` (2.2).
- Concevoir et implementer le montage du composant chat dans
  `HiveViewProductUiPresenter.cs` et `WorldMapMmoFullscreenFoundationBootstrap.cs`.
- Brancher un vrai fournisseur de traduction (ou confirmer avec Jeff qu'on
  attend une cle DeepL) pour que le bouton "Traduire" fonctionne reellement.
- Implementer le bouton "Traduire" + detection de langue differente du
  francais dans l'UI.
- Verifier/clarifier l'etat reel du serveur 104.129.128.136 (voir 4.2)
  avant toute preparation de mise en production.
- Tests: aucun test d'integration UI ruche/carte <-> chat n'existe encore
  (a ecrire une fois le montage fait).

### 4.10 [Archive chat] Etat production — rien d'irreversible n'a ete touche

- Aucune action serveur n'a ete effectuee sur `104.129.128.136` durant cette
  session ni durant l'investigation (lecture de fichiers locaux uniquement).
- L'accord de prudence existant, tel qu'ecrit dans le mandat du projet
  (`Docs/AgentCoordination/Communication_VM_Assignment.md` et
  `Docs/WorldMapCommunication/Communication_Agent_ParallelProduction_Goal.md`):
  *"Ne deploie rien sur `chat.dravii.com` ou `104.129.128.136` sans
  autorisation explicite, preuve de sauvegarde et plan de rollback."*
  A respecter integralement pour la suite.
- Question ouverte non resolue: un chat a reellement ete mis en ligne sur ce
  serveur le 16-17 juillet 2026 selon la documentation d'epoque — aucune
  preuve d'un rollback n'a ete trouvee. **Ne pas supposer l'etat du serveur
  reel sans le verifier directement** (SSH/RDP ou equivalent, avec
  confirmation de Jeff avant toute inspection live si necessaire).

## 5. Blocages / questions ouvertes

**Question encore en attente de reponse de Jeff (session precedente, non
bloquante):** les 14 images HD de batiments (voir section 1) doivent-elles
aussi remplacer le bandeau de la popup d'info batiment ou de la vue liste
generale de la Vue d'ensemble de la colonie? A poser si l'occasion se
presente, sans bloquer sur autre chose en attendant.

Le chantier chat (section 4) contient ses propres questions ouvertes
archivees (4.2, 4.10) — non pertinentes tant que ce chantier n'est pas
relance explicitement par Jeff.

## 6. Prochaine etape concrete pour la session suivante

Il n'y a pas de tache unique planifiee. Le mode de travail est reactif
(section 2): attendre le prochain retour de Jeff sur une fenetre/un bouton
qu'il teste en Play Mode, puis:

1. Localiser precisement le code concerne (Grep/Explore dans les deux
   monolithes UI de la section 3, ou ailleurs selon le retour).
2. Corriger, verifier la compilation (`console-get-logs`), et laisser Jeff
   verifier visuellement en Play Mode (voir section 1 pour la raison de ne
   pas relancer `tests-run` pendant qu'il est en Play Mode).
3. Documenter chaque correction significative dans
   `Docs/Claude/Claude_Continuation.md` en fin de tache, comme mandate par
   `CLAUDE.md`.

## 7. Prompt a donner apres `/clear`

Copier-coller ce qui suit dans la nouvelle session:

```
Lis Docs/AgentCoordination/ProjectStatus_Handoff_2026-07-27.md en entier,
puis CLAUDE.md et Docs/Claude/Claude_Continuation.md. Je suis en train de
tester en Play Mode les differentes fenetres et boutons du jeu et je vais te
donner au fur et a mesure ce que je decouvre a ameliorer ou corriger, sur
n'importe quel systeme du jeu (pas un chantier fixe). Le chat/messagerie
N'EST PAS la priorite actuelle (section 4 du handoff = archive, ne pas y
toucher sans demande explicite de ma part). Utilise la section 3 du handoff
pour t'orienter vite dans l'architecture et les fonctionnalites du jeu selon
ce que je te rapporte.
```
