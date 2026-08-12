# Chat serveur — session stricte, échéance et reprise idempotente

Date: 2026-07-21  
État: validation locale uniquement; aucune promotion

## Contrats appliqués

Les routes chat REST et le hub temps réel rejettent avant tout effet métier un bearer absent ou syntaxiquement invalide. La syntaxe est bornée à 1..8192 caractères, ASCII b64token (`A-Z`, `a-z`, `0-9`, `-._~+/`) avec `=` uniquement en suffixe. Aucun trim silencieux n'est effectué. Toutes les erreurs donnent `401 chat.session_required`; aucune valeur de jeton, extrait, hash réversible ou `playerId` brut n'est journalisé.

Le hub valide également `access_token` (transport SignalR) avant connexion et vérifie l'autorisation de lecture avant l'abonnement au groupe. Le profil désactivé continue d'interrompre la connexion.

Les travaux asynchrones reçoivent `RequestAborted`. Une annulation observée avant le commit arrête l'opération sans quota, reçu ou message. L'envoi persiste le reçu et le message avant la publication temps réel; une coupure après ce commit peut donc rendre la réponse inconnue, mais le rejeu avec le même `ClientRequestId` renvoie le même message sans doublon. Une indisponibilité de traduction répond 503 avec `Retry-After: 30`.

Les tests HTTP désactivent les redirections automatiques et vérifient `capabilities` en 200, puis conversations, pages de messages, envoi, lecture, signalement et traduction directement en 401 sans `Location` ni 3xx. Le préflight staging répète ces sondes sans bearer et exige le préfixe canonique HTTPS `/chat/v1`.

## Preuves

- build Release et cible HTTP: 0 erreur, 0 avertissement;
- suite chat isolée: 21/21;
- tests ciblés bearer/redirect/annulation: verts;
- suite HTTP complète complémentaire net10: 235 réussis, 7 SQL opt-in ignorés, 0 échec, total 242;
- smoke du DLL publié: `Healthy`, `chat-v1`, rétention 30, `server=false`, `realtime=false`, `PreparationOnly`;
- candidat courant: `Server/artifacts/candidates/BeeKingdom.Server.20260721T175116Z`, 54 fichiers avant manifeste, `DeploymentAuthorized=false`;
- `170156Z`, `170435Z`, `170747Z`, `173749Z` et `174204Z` sont révoqués par `CANDIDATE-STATUS.json` lorsqu'ils précèdent ce candidat.

Les portes SQL jetable, runtime natif .NET 8, TLS/SNI/Full strict, IIS/proxy et Unity Android restent ouvertes. Aucun accès distant ou déploiement n'a été tenté.

## Fichiers du lot

Créés:

- `Server/src/BeeKingdom.Authentication/Security/BearerTokenSyntax.cs`
- `Server/tests/BeeKingdom.Tests/BearerTokenSyntaxTests.cs`
- `Docs/ProductionIntegration/ChatMessaging_ServerAuthTimeoutHardening_2026-07-21.md`

Modifiés:

- `Server/src/BeeKingdom.Authentication/Tokens/AuthenticationTokenManager.cs`
- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/Realtime/ChatRealtimeHub.cs`
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/tests/BeeKingdom.Tests/ChatMessagingEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`
- `Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj`
- `Server/tools/New-ProductionCandidateLocal.ps1`
- `Server/tools/Test-ChatStagingPreflight.ps1`
- `Server/tools/Test-ProductionConfiguration.ps1`
- `Server/tools/Test-ProductionLocal.ps1`
- `Server/artifacts/candidates/CANDIDATE-STATUS.json`

Artefact local créé:

- `Server/artifacts/candidates/BeeKingdom.Server.20260721T175116Z/` avec manifeste.

Aucune modification sous `Assets/`, LivingHive, carte ou images; aucune synchronisation finale effectuée.
