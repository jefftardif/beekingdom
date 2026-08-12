# Consolidation serveur chat — persistance, erreurs et reprise

Date: 2026-07-21  
Portee: `Server/`, tests serveur et `Docs/ProductionIntegration/` uniquement.

## Resultat

- Solution `BeeKingdom.Server.slnx` compilee en Release: 0 erreur, 0 avertissement.
- Suite ciblee chat: 16/16 tests reussis.
- Aucun deploiement, activation publique, secret ou synchronisation.
- `Chat:Enabled` et `Chat:RealtimeEnabled` n'ont pas ete actives.

## Contrats stabilises

Le JSON reste camelCase. Les erreurs 400/401/403/404/409/429/503 exposent uniquement `code`, `message` (cle localisable) et `retryAfterSeconds` facultatif. Catalogue v1: `chat.invalid_request`, `chat.session_required`, `chat.forbidden`, `chat.not_found`, `chat.idempotency_conflict`, `chat.rate_limited`, `chat.unavailable`, `chat.translation_unavailable`. Les 429 portent `Retry-After: 60`; les 503 bornes portent `Retry-After: 30`.

L'authentification precede tout appel metier. Un 401 ne peut donc consommer quota, creer recu/conversation/Inbox/message/rapport, ni publier SignalR. `/chat/v1/capabilities` reste public et ne retourne que protocole, fonctions, limites et etat d'activation, sans secret ni connexion.

Une absence de reponse reseau (`status 0` cote Unity) reste distincte d'une reponse HTTP 4xx/5xx; le serveur ne produit jamais de statut 0.

## Traduction

Le cache reste idempotent sur `(MessageId, TargetLocale, ModelVersion)`. Un echec fournisseur n'ecrit aucune ligne incomplete et un retry peut reussir. La traduction ne modifie ni original, sequence, Inbox, moderation ou lecture. Les metriques `chat.translation.requests` et `chat.translation.latency.ms` distinguent `success`, `cache`, `rate_limited` et `provider_unavailable`; les journaux ne contiennent aucun corps, traduction, jeton ou secret.

## Idempotence et temps reel

- Creation: participants GUID non vides, distincts, tries; titre/audience trims, blancs transformes en null; casse conservee; serveur/monde restent des GUID. Un ordre different ou un doublon est semantiquement identique; un payload reellement different produit 409.
- Envoi: le meme `ClientRequestId` retrouve le meme message apres reconstruction du service; un payload different produit 409.
- Temps reel: publication seulement apres lisibilite repository. SignalR et REST portent les memes `messageId`, `conversationId`, `sequence`, `clientRequestId`, `body` et `acceptedAtUtc`.
- Messages: `Sequence > afterSequence`, ordre ascendant, pages repetees identiques; `nextAfterSequence` est la derniere sequence retournee lorsqu'une page pleine indique une suite.

## Moderation durable

`ReportChatMessageRequest` exige maintenant `clientRequestId` et `category`. Le recu unique `(ReporterPlayerId, ClientRequestId)` contient seulement SHA-256 de `(MessageId, Category)`, `ReportId`, dates et expiration; jamais le corps du message. Retry identique retourne le meme rapport; collision de payload produit 409. Migration reversible preparee: `063_chat_moderation_idempotency.sql`, retention par defaut 30 jours.

Limite de staging: l'insertion du rapport et du recu doit encore etre enveloppee dans une transaction SQL unique avec gestion explicite de la course de deux premiers retries. Aucune activation avant cette preuve.

## Curseur de lecture

Les repositories memoire et SQL fusionnent atomiquement `ReadCursorSequence=max(current,requested)`. Une ecriture ancienne conserve le minimum de non-lus/mentions deja atteint. La reponse retourne l'entree effective relue. Le test 10 puis 4 confirme l'absence de regression.

## Pagination — ecart ouvert

La pagination messages est couverte. La liste serveur de conversations renvoie encore `nextCursor=null` et n'accepte pas de curseur opaque. Les exigences de curseur conversation stable, lie a l'audience, alteration/cycle et absence de fuite inter-joueur restent donc une porte de staging explicite.

## Environnement de test

La VM ne contient que le runtime .NET 10 alors que la suite HTTP cible .NET 8. Avec roll-forward majeur, 170 tests passent, 7 SQL restent ignores et 30 tests HTTP echouent dans le `PipeWriter` du banc ASP.NET avant la validation metier. La solution et la suite isolee compilent/executent correctement. Un rerun HTTP et SQL jetable sous runtime .NET 8 est obligatoire en staging.

## Fichiers exacts de cette consolidation

Crees:

- `Server/src/BeeKingdom.Chat/Translations/ChatTranslationDiagnostics.cs`
- `Server/src/BeeKingdom.Database/Scripts/063_chat_moderation_idempotency.sql`
- `Server/src/BeeKingdom.Database/Scripts/063_chat_moderation_idempotency.rollback.sql`
- `Docs/ProductionIntegration/ChatMessaging_ServerConsolidation_2026-07-21.md`

Modifies:

- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/ChatManager.cs`
- `Server/src/BeeKingdom.Chat/DependencyInjection/ChatServiceCollectionExtensions.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatContracts.cs`
- `Server/src/BeeKingdom.Chat/Models/ChatRecords.cs`
- `Server/src/BeeKingdom.Chat/Repositories/IChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Repositories/InMemoryChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Repositories/SqlChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Translations/ChatTranslationContracts.cs`
- `Server/src/BeeKingdom.Chat/Translations/ChatTranslationService.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Database/DatabaseCatalog.cs`
- `Server/src/BeeKingdom.Database/DatabaseRollbackCatalog.cs`
- `Server/tests/BeeKingdom.ChatTranslation.Tests/BeeKingdom.ChatTranslation.Tests.csproj`
- `Server/tests/BeeKingdom.Tests/ChatTranslationServiceTests.cs`
- `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`

Le correctif client Inbox signale par Communication est confirme clos. Aucun fichier `Assets/`, LivingHive, scene, carte ou image n'a ete modifie.

## Lot de fermeture staging local

Verification finale du lot: solution Release 0 erreur/0 avertissement; suite isolee 20/20.

### Pagination conversations

`GET /chat/v1/conversations` accepte maintenant `cursor`. Le jeton URL-safe contient une version, une portee joueur non reversible, un offset strictement positif et une somme de controle; un jeton altere, inconnu ou provenant d'un autre joueur retourne 400 sans journaliser sa valeur. Les repositories appliquent un ordre total stable (activite decroissante, puis `ConversationId`), lisent `limit+1`, et n'emettent `nextCursor` que lorsqu'une suite existe. Les limites restent bornees a 1..100. Une page repetee est identique tant que droits et donnees ne changent pas.

### Atomicite moderation et purge

`SaveModerationReportIdempotent` execute lecture verrouillee, creation du rapport et creation du recu dans une transaction SQL `Serializable`. Deux retries identiques convergent vers le meme `ReportId`; un hash different retourne 409 avant toute seconde ecriture. Le repository memoire applique la meme operation sous verrou.

Les recus d'envoi acceptes, de creation et de moderation sont purges avec une retention configurable `Chat:IdempotencyReceiptRetentionDays`, valeur par defaut 30 jours et minimum 7 jours. La purge est transactionnelle en SQL, bornee a une execution par heure et ne journalise aucun payload. Les recus d'envoi encore sans acceptation ne sont jamais purges par cette politique.

### Capacites

Les capacites et les validateurs utilisent la meme instance effective de `ChatOptions`. Les tests figent `chat-v1`, `BodyMaxCharacters`, `MaxPrivateRecipients`, debits, canaux, `ReadCursors` et `ModerationReports`, puis prouvent que le serveur refuse encore un corps ou un groupe prive hors limites. `server=false` continue de bloquer les mutations; `realtime=false` n'empeche pas le polling REST lorsque `server=true`.

### Diagnostics

L'audit du code chat ne trouve qu'un journal structure de traduction: resultat agrege et latence. Un test de capture couvre succes et cache et prouve l'absence du corps original, du texte traduit, du joueur et du message. Aucun bearer, URL, curseur ou identifiant brut n'est journalise. Aucune correlation persistante n'est utilisee.

### Smoke Production local

Le binaire Release a ete lance avec `ASPNETCORE_ENVIRONMENT=Production`, `Persistence:Provider=InMemory`, `Chat:Enabled=false`, `Chat:RealtimeEnabled=false` et une ecoute locale `127.0.0.1:5088`. Resultats:

- `/health`: `Healthy`;
- `/chat/v1/capabilities`: `server=false`, `realtime=false`, `protocolVersion=chat-v1`;
- `/runtime/chat-readiness`: `PreparationOnly`, drapeaux desactives.

Le processus a ete arrete apres le smoke. Aucun port public, serveur distant, SQL distant ou drapeau de production n'a ete modifie.

Fichiers modifies dans ce lot additionnel:

- `Server/src/BeeKingdom.Chat/Configuration/ChatOptions.cs`
- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/ChatManager.cs`
- `Server/src/BeeKingdom.Chat/DependencyInjection/ChatServiceCollectionExtensions.cs`
- `Server/src/BeeKingdom.Chat/Repositories/IChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Repositories/InMemoryChatRepository.cs`
- `Server/src/BeeKingdom.Chat/Repositories/SqlChatRepository.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.Tests/ChatTranslationServiceTests.cs`
- `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`
- `Docs/ProductionIntegration/ChatMessaging_ServerConsolidation_2026-07-21.md`

Porte restante: executer les migrations et les tests de reconstruction/concurrence sur une base SQL jetable avec le runtime .NET 8. Cette operation n'a pas ete lancee sur la production ni sur la base distante.

## Configuration externe Unity attendue pour staging

La fabrique Unity doit recevoir une valeur non secrete, propre a l'environnement, sous la forme `https://<hote-staging>/chat/v1`. Cette valeur appartient au manifeste/configuration de deploiement et ne doit pas etre compilee comme constante de domaine. Elle ne contient ni bearer, compte, mot de passe, chaine SQL ou parametre sensible. Le bearer reste fourni separement par la source de session renouvelable.

Le serveur conserve `ProtocolVersion=chat-v1` et toutes les routes publiques du contrat sous `/chat/v1`. Avant branchement d'un build Unity staging, les preuves suivantes sont obligatoires sur l'hote non public retenu:

- certificat non expire correspondant exactement au nom SNI;
- chaine complete reconnue par Android/Unity, sans autorite locale;
- TLS moderne et absence de redirection vers HTTP;
- mode Cloudflare Full (strict) avec certificat origine valide, si Cloudflare est dans le chemin;
- test reel Unity Android de `/chat/v1/capabilities`, 401 structure, polling et negotiate SignalR;
- aucune exception HTTP hors loopback.

Aucun hote staging n'a encore ete fourni/autorise dans cette tranche; ces controles n'ont donc pas ete simules sur le domaine public.

## Smoke Production reproductible

Le script `Server/tools/Test-ProductionLocal.ps1` rend le controle local reproductible. Il compile en Release sauf avec `-NoBuild`, lance un processus enfant sur loopback seulement, force `Production`, `InMemory`, workers desactives et les deux drapeaux chat a `false`, valide les trois endpoints puis arrete toujours le processus. Il ne lit aucune configuration distante et ne contient aucun secret.

Commande Windows PowerShell 5.1:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Server/tools/Test-ProductionLocal.ps1
```

Execution verifiee: succes, `Healthy`, `chat-v1`, `server=false`, `realtime=false`, `PreparationOnly`. Aucun listener ne restait sur le port 5088 apres la fin.

Fichier cree:

- `Server/tools/Test-ProductionLocal.ps1`

## Stockage protege mobile — contrat staging

Les quatre journaux Unity (messages, conversations, signalements, lectures) doivent utiliser un protecteur authentifie injecte; aucune cle ou matiere secrete n'appartient au depot ni a la configuration serveur. Politique attendue:

- Android: cle non exportable creee dans Android Keystore, chiffrement authentifie lie a l'application et a la cle logique du journal;
- iOS: cle non exportable/protegee par Keychain avec accessibilite choisie selon la politique de session;
- creation: generation au premier besoin, jamais derivee d'un mot de passe ou bearer;
- rotation: nouvelle version d'enveloppe, dechiffrement ancien puis reecriture atomique; conservation de l'ancien materiel uniquement pendant la fenetre de migration;
- deconnexion: effacement des journaux et, selon la politique de compte, destruction de la cle locale;
- reinstallation/restauration: une enveloppe sans sa cle devient illisible, reste preservee pour diagnostic/reprise explicite et n'est jamais traitee comme vide;
- alteration ou mauvaise cle: erreur sure, aucun effacement silencieux et aucun envoi reseau du contenu;
- aucune sauvegarde cloud d'une cle non explicitement approuvee par la politique produit/securite.

Le serveur demeure `PreparationOnly`; ce stockage client ne transforme aucune donnee locale en autorite officielle.

## Préflight HTTPS staging reproductible

`Server/tools/Test-ChatStagingPreflight.ps1` prépare la validation en lecture seule d'un hôte autorisé. Il refuse HTTP, loopback, credentials dans l'URL, query/fragment et tout préfixe autre que `/chat/v1`. Il ouvre TLS avec le nom DNS comme SNI et laisse le système valider chaîne et nom, contrôle la marge d'expiration, un motif d'émetteur facultatif, l'absence de redirection, puis lit uniquement `capabilities` sans bearer.

Le script a été analysé sans erreur de syntaxe. Les refus HTTP et loopback ont été exécutés et confirmés avant toute connexion distante. Il n'a pas été lancé sur un hôte public ou staging, aucun hôte n'étant autorisé/fourni.

Commande future:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Server/tools/Test-ChatStagingPreflight.ps1 -BaseUrl https://<hote-staging>/chat/v1
```

Fichier créé:

- `Server/tools/Test-ChatStagingPreflight.ps1`

## Cloisonnement mobile par joueur

`StoragePartitionId` doit être dérivé exclusivement de l'identité joueur stable attestée par la session, jamais du bearer, d'un nom affiché ou d'une partition anonyme commune. Un renouvellement de jeton conserve la partition et l'instance de journaux; un changement de compte détruit l'instance du client et la reconstruit avec la partition du nouveau joueur.

Scénario Android staging obligatoire:

1. joueur A hors ligne crée une opération en attente;
2. A se déconnecte; le client et ses références mémoire sont détruits;
3. joueur B se connecte avec une autre partition et ne voit/rejoue aucune opération A;
4. B se déconnecte;
5. A revient avec la même identité attestée et retrouve uniquement son opération;
6. une rotation de jeton de A ne change ni empreinte de partition ni clé de journal;
7. le stockage brut ne contient ni identifiant A/B ni corps en clair.

La partition est une clé de cloisonnement, pas une preuve d'authentification. L'enveloppe Keystore/Keychain authentifiée reste nécessaire.

## Lanceur SQL jetable

`Server/tools/Test-SqlDisposable.ps1` lance uniquement `SqlServerOptInIntegrationTests`. Il exige `BEE_SQL_INTEGRATION_CONNECTION_STRING` dans l'environnement du processus, refuse toute cible autre que `(localdb)`, refuse `User ID`/mot de passe, exige Integrated Security et vérifie la présence de `SqlLocalDB.exe`. La chaîne n'est jamais affichée ou écrite.

Les garde-fous absence de configuration et cible distante ont été exécutés avec succès: le script s'arrête avant toute connexion. L'exécution positive reste impossible sur cette VM car LocalDB n'est pas installé.

Usage futur sur poste staging local autorisé:

```powershell
$env:BEE_SQL_INTEGRATION_CONNECTION_STRING = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;'
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Server/tools/Test-SqlDisposable.ps1
```

Fichier créé:

- `Server/tools/Test-SqlDisposable.ps1`

## Journaux hors ligne bornés — scénario d'intégration

La saturation locale `ChatPendingJournalFullException` n'est pas un statut HTTP et ne doit jamais être traduite en 400/409/429 serveur. Le scénario Android/staging doit remplir chacun des quatre journaux à sa capacité négociée, puis:

1. confirmer qu'une nouvelle identité est refusée sans éviction ni réécriture;
2. confirmer qu'un retry de la même identité et l'avancement d'un curseur existant restent possibles;
3. rétablir le réseau et drainer toutes les entrées avec les mêmes `ClientRequestId`;
4. vérifier côté serveur un seul résultat/reçu par identité et aucun quota recompte pour les retries connus;
5. confirmer qu'un acquittement libère exactement une place;
6. accepter ensuite une seule nouvelle opération;
7. vérifier qu'aucun corps, identifiant brut ou secret n'apparaît dans diagnostics et stockage brut.

La rétention serveur de 30 jours reste très supérieure à une reprise de journal borné; aucune purge ne doit toucher un reçu encore dans cette fenêtre.

## Durcissement de configuration Production

L'audit a détecté que le fallback SQL localhost défini pour le développement pouvait rester hérité lorsque le binaire était lancé depuis son répertoire de publication. Corrections:

- `SqlServerOptions.ConnectionString` n'a plus de valeur par défaut;
- `appsettings.Production.json` neutralise explicitement les trois chaînes SQL;
- si `Persistence:Provider=SqlServer`, les connexions runtime et migration externes sont validées au démarrage;
- `PersistenceOptions` et `SqlServerOptions` utilisent `ValidateOnStart`, avant l'ouverture du listener;
- le smoke Production utilise désormais le répertoire du binaire comme content root réel.

Preuves:

- bascule Production vers SqlServer sans variables externes: échec `OptionsValidationException` pour runtime et migration, aucun listener 5091;
- mode sûr InMemory: smoke toujours `Healthy`, `chat-v1`, `server=false`, `realtime=false`, `PreparationOnly`;
- build serveur Release: 0 erreur, 0 avertissement;
- `Test-ProductionConfiguration.ps1`: succès sur tous les invariants suivis.

Le validateur statique refuse notamment chat/realtime actifs, protocole divergent, clés ops dans le dépôt, fallback SQL Production, autorité officielle active, absence de preuves backup/maintenance et rollback pré-acquitté.

Fichier créé:

- `Server/tools/Test-ProductionConfiguration.ps1`

Fichiers modifiés:

- `Server/src/BeeKingdom.Persistence/Configuration/SqlServerOptions.cs`
- `Server/src/BeeKingdom.Persistence/DependencyInjection/PersistenceServiceCollectionExtensions.cs`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tools/Test-ProductionLocal.ps1`

## Reprise coordonnée et erreurs locales — scénarios staging

Les codes `local_queue_full` et `local_storage_unavailable` ont HTTP 0 côté Unity et ne représentent aucune requête reçue. Ils ne doivent apparaître ni dans les compteurs HTTP, ni comme 429/503 serveur.

Drainage partiel obligatoire:

1. préparer conversation, message, lecture et signalement;
2. vérifier l'ordre conversation → message → lecture → signalement;
3. injecter un 503 après la première réussite;
4. confirmer que le reçu de la réussite existe et que les trois autres intentions restent locales;
5. relancer et vérifier que seule la partie restante est envoyée, sans doublon ni quota recompte;
6. comparer les compteurs avant/terminé/restant avec le résultat client.

Panne/quarantaine Android obligatoire:

1. altérer une enveloppe protégée puis simuler la clé indisponible;
2. confirmer `LocalStorageUnavailable`, zéro HTTP et aucune suppression;
3. lancer uniquement sur action utilisateur localisée `QuarantineAndReset`;
4. vérifier copie chiffrée et suppression des sources seulement après vérification;
5. redémarrer avec file active vide, restaurer avant toute nouvelle écriture, puis drainer idempotemment;
6. répéter, créer une nouvelle donnée active après reset et confirmer que `Restore` refuse l'écrasement;
7. ne jamais téléverser enveloppe ou quarantaine au serveur, diagnostics ou support distant.

Une panne de suppression locale après succès serveur laisse volontairement le reçu rejouable; l'idempotence serveur doit retourner le même résultat sans second effet.

## Candidat Production local publié

`Server/tools/New-ProductionCandidateLocal.ps1` assemble un candidat horodaté sans autoriser son déploiement. La chaîne exécute:

1. validation statique Production;
2. build Release sans restauration réseau;
3. suite chat ciblée;
4. `dotnet publish` framework-dependent;
5. contrôle de la configuration réellement embarquée;
6. smoke depuis le DLL publié et son propre content root;
7. manifeste SHA-256 de tous les fichiers publiés.

Exécution vérifiée du 2026-07-21:

- build: 0 erreur, 0 avertissement;
- tests chat: 20/20;
- smoke publié: Healthy, chat-v1, server=false, realtime=false, PreparationOnly;
- candidat: `Server/artifacts/candidates/BeeKingdom.Server.20260721T170156Z`;
- 67 fichiers inventoriés avant le manifeste;
- `DeploymentAuthorized=false` inscrit dans le manifeste;
- aucune copie ou activation distante.

Fichier créé:

- `Server/tools/New-ProductionCandidateLocal.ps1`

Fichier modifié:

- `Server/tools/Test-ProductionLocal.ps1` (support d'un DLL publié explicitement sous `Server/`).

Ce candidat est une preuve locale, pas un paquet approuvé. Il ne doit pas être transféré tant que SQL jetable, HTTP .NET 8, TLS/Full strict et Android staging ne sont pas verts.

### Candidat durci suivant

L'inspection du premier artefact a identifié `appsettings.Development.json`, des PDB et des valeurs de base développement permissives. Le pipeline a été durci:

- configuration de base fail-closed: chaînes SQL vides, clés Ops obligatoires;
- exceptions localhost/ops permissives déplacées uniquement dans `appsettings.Development.json` source;
- profil Development supprimé du candidat publié;
- symboles PDB désactivés et interdits par le pipeline;
- recherche dans JSON/config publiée: aucun `Password=`, `User Id=` ou bearer.

Second candidat vérifié:

- `Server/artifacts/candidates/BeeKingdom.Server.20260721T170435Z`;
- 54 fichiers avant manifeste;
- aucun PDB et aucun `appsettings.Development.json`;
- build 0/0, tests 20/20, smoke publié vert sur 5090;
- `DeploymentAuthorized=false`.

Le premier candidat reste un artefact local historique et ne doit pas être utilisé; seul le second représente l'état durci actuel, sans pour autant être approuvé pour transfert.

### Inventaire courant après corrections de migration

Les tests de migration ont ensuite détecté deux attentes obsolètes: propriétés SQL Production présentes mais vides au lieu d'être absentes, et ordre rollback incomplet. Les propriétés ont été retirées, puis le rollback a été remis dans l'ordre inverse `070, 063, 062, 061, 060, 050, 040, 030, 020`. Build complet: 0/0; tests configuration/persistence/migrations: 21/21.

Un troisième candidat remplace donc tous les précédents:

- courant local: `BeeKingdom.Server.20260721T170747Z`;
- 54 fichiers avant manifeste;
- build 0/0, chat 20/20, smoke publié vert sur 5092;
- sans Development/PDB/motif secret;
- `DeploymentAuthorized=false`.

Révocations obligatoires:

- `20260721T170156Z`: révoqué, inutilisable;
- `20260721T170435Z`: révoqué car antérieur aux corrections de contrat migration/configuration;
- aucun des trois candidats n'est autorisé au transfert ou déploiement.

L'autorité d'inventaire locale est `Server/artifacts/candidates/CANDIDATE-STATUS.json`.

Le générateur met désormais automatiquement à jour cet inventaire: tout ancien candidat `local-validation-only` devient `revoked` avant d'inscrire le nouveau courant. Cette règle empêche qu'un artefact antérieur reste implicitement promouvable.

## Intégrité des journaux restaurés — matrice Android

Pour chacun des quatre journaux, injecter séparément:

- un journal au-delà de la capacité;
- une version d'entrée inconnue;
- une identité idempotente dupliquée;
- un compteur ou une séquence négative;
- une date message invalide ou un champ obligatoire absent.

Pour chaque cas, la preuve attendue est identique: `LocalStorageUnavailable`, zéro HTTP, octets persistés inchangés, aucune normalisation/réécriture, et possibilité de quarantaine explicite. Le serveur ne propose aucune réparation d'un reçu ambigu. Les tests SQL futurs doivent en parallèle prouver l'unicité des reçus complets sous reconstruction et concurrence.

### Taille stricte et récupération exclusive

Deux limites restent indépendantes:

- `bodyMaxCharacters` vient des capabilities et borne un message métier;
- `MaxPendingSerializedCharactersPerJournal` borne l'image locale complète et ne peut jamais élargir la première limite.

Android staging doit prouver qu'une nouvelle image trop grande produit `LocalQueueFull`, zéro HTTP et conserve l'ancienne valeur; une valeur restaurée déjà surdimensionnée produit `LocalStorageUnavailable` avant parsing, conserve ses octets et reste quarantainable.

Le scénario de course de récupération doit bloquer le stockage sécurisé pendant une copie de quarantaine, lancer simultanément une nouvelle sauvegarde et vérifier qu'elle attend derrière la porte de partition sans HTTP. Après libération: ancien journal uniquement en quarantaine, nouvelle opération uniquement active, puis chaque reçu exactement une fois au drainage. Toute récupération synchrone doit être exécutée hors du thread d'affichage Unity.
