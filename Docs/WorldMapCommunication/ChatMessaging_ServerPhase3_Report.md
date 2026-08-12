# Bee Kingdom - ChatMessaging Server Phase 3 Report

Date: 2026-07-16
Scope: serveur chat/messagerie uniquement. Aucun changement Unity, PNG, Wave5, BearDen, APK ou carte 50x50. Aucun deploiement live sur `104.129.128.136`.

## Statut

Phase 3 locale terminee et testee.

- `BUILD_SERVER=PASS`
- `SERVER_TEST_SUITE=PASS`
- `LOCAL_PACKAGE_PREPARED=PASS`
- `LIVE_DEPLOY=NOT_STARTED`
- `READY_FOR_PROD_DEPLOY=BLOCKED_EXTERNAL_INPUT`

Le blocage production restant est externe: DNS/nom de domaine, certificat TLS, acces SQL production, regles firewall, compte de service/IIS et fenetre de maintenance doivent etre confirmes avant toute modification du serveur live.

## Changements Phase 3

### Resolution audience cote serveur

Ajout de l'abstraction `IChatAudienceResolver` pour sortir la decision d'autorisation du payload client et la centraliser cote serveur.

Fichiers ajoutes:

- `Server/src/BeeKingdom.Chat/Audience/IChatAudienceResolver.cs`
- `Server/src/BeeKingdom.Chat/Audience/ChatAudienceDecision.cs`
- `Server/src/BeeKingdom.Chat/Audience/LocalChatAudienceResolver.cs`

Fichiers modifies:

- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/src/BeeKingdom.Chat/DependencyInjection/ChatServiceCollectionExtensions.cs`
- `Server/tests/BeeKingdom.Tests/ChatAudienceResolverTests.cs`
- `Server/tests/BeeKingdom.Tests/ChatMessagingEndpointTests.cs`
- `Server/tests/BeeKingdom.Tests/PersistenceProviderSelectionTests.cs`

Comportement obtenu:

- `server/global`: joueur authentifie autorise dans le contexte serveur/monde demande.
- `private`: participants explicites + createur, avec limite `Chat:MaxPrivateRecipients`.
- `alliance`: exige une decision resolver de type membre/officier/leader.
- `leaders`: exige une decision resolver officier/leader.
- annonces alliance: exige officier/leader et construit le fanout membres + emetteur.

Implementation locale/staging:

- `LocalChatAudienceResolver` est deterministe pour les tests.
- Tant qu'il n'existe pas de source officielle live alliances/roles, le champ `requesterAllianceRole` reste accepte comme source de simulation locale/staging.
- Le chemin privilegie est maintenant le resolver: `ChatService` ne verifie plus directement `requesterAllianceRole`.
- Absence de role ne donne aucun acces alliance/leaders/annonces. Elle reste acceptable pour `server` et `private`.

Limite documentee:

- La source officielle de verite pour alliances/roles/monde reste a brancher plus tard dans une implementation production du resolver.

### Temps reel SignalR

Ajout du dispatcher SignalR effectif derriere le flag `Chat:RealtimeEnabled`.

Fichiers ajoutes:

- `Server/src/BeeKingdom.Chat/Realtime/ChatRealtimeGroups.cs`
- `Server/src/BeeKingdom.Chat/Realtime/SignalRChatRealtimeDispatcher.cs`
- `Server/tests/BeeKingdom.Tests/SignalRChatRealtimeDispatcherTests.cs`

Fichiers modifies:

- `Server/src/BeeKingdom.Chat/Realtime/ChatRealtimeHub.cs`
- `Server/src/BeeKingdom.Chat/DependencyInjection/ChatServiceCollectionExtensions.cs`

Comportement obtenu:

- `IChatRealtimeDispatcher` est branche sur `SignalRChatRealtimeDispatcher`.
- Publication vers le groupe SignalR `conversation:{conversationId:N}` via la methode client `chat.event`.
- Le dispatcher ne publie rien tant que `Chat:Enabled=false` ou `Chat:RealtimeEnabled=false`.
- Le hub refuse/erreur si temps reel desactive.
- Le hub expose `JoinConversation(string conversationId)` et `LeaveConversation(string conversationId)`.

Limite documentee:

- `JoinConversation` ne valide pas encore l'appartenance conversation via repository/auth cote hub. La securite de lecture/ecriture reste couverte par les endpoints REST et le repository, mais une verification hub avant groupe SignalR est requise avant ouverture large en production.

## SQL et migrations

Statut SQL:

- `SqlChatRepository` est pret depuis Phase 2.
- Le schema `060_chat_messaging.sql` est present et enregistre par la suite de migrations.
- Les tests confirment que `060_chat_messaging.sql` est detecte par le runner.
- Les tests SQL reels restent opt-in et ignores sans chaine de connexion locale `BEE_SQL_INTEGRATION_CONNECTION_STRING`.

Fichier migration:

- `Server/src/BeeKingdom.Database/Scripts/060_chat_messaging.sql`

Tables chat prevues:

- `dbo.ChatConversations`
- `dbo.ChatConversationParticipants`
- `dbo.ChatConversationSequences`
- `dbo.ChatMessages`
- `dbo.ChatInbox`
- `dbo.ChatOutboxReceipts`
- `dbo.ChatModerationReports`

## Gates locales executees

Build serveur:

```powershell
dotnet build Server/BeeKingdom.Server.slnx --no-restore
```

Resultat: PASS, 0 warning, 0 erreur.

Suite tests serveur:

```powershell
dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj --no-build
```

Resultat: PASS, 190 reussis, 7 ignores, 0 echec.

Tests ajoutes/couverts:

- `ChatAudienceResolverTests`
- `SignalRChatRealtimeDispatcherTests`
- `ChatMessagingEndpointTests`
- `PersistenceProviderSelectionTests`

Package local serveur prepare:

```powershell
dotnet publish Server/src/BeeKingdom.Server/BeeKingdom.Server.csproj -c Release -r win-x64 --self-contained false -o Server/artifacts/chat-prod-prep/BeeKingdom.Server
```

Resultat: PASS.

Package local outils migrations prepare:

```powershell
dotnet publish Server/src/BeeKingdom.Tools/BeeKingdom.Tools.csproj -c Release -r win-x64 --self-contained false -o Server/artifacts/chat-prod-prep/BeeKingdom.Tools
```

Resultat: PASS.

Packages locaux:

- `Server/artifacts/chat-prod-prep/BeeKingdom.Server`
- `Server/artifacts/chat-prod-prep/BeeKingdom.Tools`

## Gates production

Gates atteints localement:

- Build serveur PASS.
- Suite tests serveur PASS.
- SQL repository chat pret.
- Migration `060_chat_messaging.sql` prete et enregistree.
- `Chat:Enabled=false` et `Chat:RealtimeEnabled=false` dans `appsettings.Production.json`.
- Packages Windows/IIS et outils migrations prepares localement.
- Aucun secret reel ajoute au depot.

Gates non atteints car externes:

- DNS/nom de domaine non confirme.
- Certificat TLS non fourni.
- Acces SQL production non fourni.
- Regles firewall/IIS non confirmees.
- Compte de service non confirme.
- Methode d'acces admin au serveur live non confirmee dans ce thread.
- Fenetre de maintenance et backup evidence non confirmees.

Conclusion:

`READY_FOR_PROD_DEPLOY=BLOCKED_EXTERNAL_INPUT`

## Prochaines actions minimales

1. Valider les entrees externes listees dans le runbook production.
2. Brancher une implementation production de `IChatAudienceResolver` sur la source officielle alliances/roles quand elle existe.
3. Ajouter une verification d'appartenance dans `ChatRealtimeHub.JoinConversation` avant activation temps reel large.
4. Executer le dry-run SQL/ops contre l'environnement cible avec secrets hors depot.
5. Garder `Chat:Enabled=false` jusqu'au basculement controle.
