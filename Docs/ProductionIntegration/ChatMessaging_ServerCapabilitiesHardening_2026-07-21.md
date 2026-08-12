# Chat serveur — capabilities-first, cache et enveloppe bornée

Date: 2026-07-21  
Responsable: Intégrateur Production  
État: validation locale uniquement

## Résultat

`GET /chat/v1/capabilities` expose maintenant `idempotencyReceiptRetentionDays` en camelCase depuis la même instance `ChatOptions` que la purge des reçus. La valeur Production est 30 jours. Le domaine protocolaire serveur est 2..3650 jours, tandis que le contrôle Production refuse une valeur inférieure à 30 afin de couvrir la fenêtre maximale client de 29 jours.

La réponse capabilities est l'unique route chat publique sans bearer et émet:

- `Cache-Control: no-store, no-cache, max-age=0, must-revalidate`;
- `Pragma: no-cache`;
- `Expires: 0`;
- `Vary: Accept-Encoding`;
- aucun `Age` positif.

Le préflight staging exige exactement une URL `https://<hôte>/chat/v1`, sans identifiants, query, fragment, autre chemin ou redirection. Il ajoute uniquement `/capabilities`, ce qui évite tout double préfixe. Il refuse également une politique cache incomplète, un `Age` positif, un provider autre que `server`, un protocole autre que `chat-v1`, des canaux vides/inconnus/dupliqués et toute limite hors enveloppe.

Les bornes `ChatOptions` validées avec `ValidateOnStart` sont:

- corps: 1..4000 caractères;
- messages/joueur/minute: 1..600;
- messages/conversation/10 secondes: 1..100;
- créations privées/heure: 1..1000;
- destinataires privés: 1..100;
- rétention: 2..3650 jours;
- protocole: exactement `chat-v1` jusqu'à l'introduction d'une compatibilité versionnée.

Les segments conversation/message sont traités une fois par ASP.NET Core, bornés à 1..256 caractères, refusés s'ils ont des espaces de bord, puis validés comme GUID sans second décodage. `%2F`, `%3F`, `%23`, `%252F`, Unicode, 256/257 caractères et espaces encodés produisent `400 chat.invalid_request`, sans redirection ni résolution vers une autre route.

Le hub temps réel valide désormais le bearer ou le `access_token` SignalR avant la connexion, puis l'autorisation de lecture avant l'abonnement à un groupe conversation. Le chat désactivé continue d'interrompre toute connexion.

Ordre contractuel staging/Unity: partition protégée → capabilities sans Authorization → validation protocole/provider/portes/fonctions/limites/rétention/cache → session → drainage → synchronisation. Aucun drainage ne doit partir au simple retour réseau avant renégociation.

Le jalon client d'horloge partagée ne demande aucun changement d'endpoint serveur. Le candidat courant a été construit après les changements serveur de composition canonique, rétention, cache, bornes, segments et authentification temps réel.

## Preuves

- build Release: 0 erreur, 0 avertissement;
- tests chat isolés: 20/20;
- tests ciblés options/cache/endpoints: 8/8;
- tests segments opaques: 9/9;
- suite HTTP complète complémentaire net10: 222 réussis, 7 SQL opt-in ignorés, 0 échec, total 229;
- smoke du DLL publié: `Healthy`, `chat-v1`, rétention 30, `server=false`, `realtime=false`, `PreparationOnly`;
- candidat: `Server/artifacts/candidates/BeeKingdom.Server.20260721T173749Z`;
- 54 fichiers avant manifeste;
- aucun PDB, aucune configuration Development et aucun motif secret détecté;
- `DeploymentAuthorized=false`.

`Server/artifacts/candidates/CANDIDATE-STATUS.json` est l'autorité. Les candidats `170156Z`, `170435Z` et `170747Z` sont révoqués et ne doivent jamais être transférés ou promus.

Les preuves SQL jetables, runtime natif .NET 8, TLS/SNI/Full strict, préservation IIS/proxy et Unity Android restent des portes staging ouvertes.

## Inventaire exact

Fichiers créés:

- `Server/tests/BeeKingdom.Tests/ChatOptionsValidationTests.cs`
- `Docs/ProductionIntegration/ChatMessaging_ServerCapabilitiesHardening_2026-07-21.md`

Fichiers modifiés:

- `Server/src/BeeKingdom.Chat/ChatManager.cs`
- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/DependencyInjection/ChatServiceCollectionExtensions.cs`
- `Server/src/BeeKingdom.Chat/Realtime/ChatRealtimeHub.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.ChatTranslation.Tests/BeeKingdom.ChatTranslation.Tests.csproj`
- `Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj`
- `Server/tests/BeeKingdom.Tests/ChatMessagingEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`
- `Server/tools/New-ProductionCandidateLocal.ps1`
- `Server/tools/Test-ChatStagingPreflight.ps1`
- `Server/tools/Test-ProductionConfiguration.ps1`
- `Server/tools/Test-ProductionLocal.ps1`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/artifacts/candidates/CANDIDATE-STATUS.json`

Répertoire d'artefact créé:

- `Server/artifacts/candidates/BeeKingdom.Server.20260721T173749Z/` — 54 fichiers publiés plus `candidate.manifest.json`.

Aucune modification n'a été faite sous `Assets/`, LivingHive, carte ou images. Aucun accès distant, transfert, déploiement, activation publique ou synchronisation finale n'a été exécuté.
