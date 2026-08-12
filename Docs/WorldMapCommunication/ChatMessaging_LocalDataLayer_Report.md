# Bee Kingdom - rapport data layer Chat/Messagerie local

**Date :** 2026-07-15  
**Périmètre :** prototype local en mémoire, sans MonoBehaviour et sans transport réseau.

## Livré

- `ChannelType` et identifiants déterministes pour les conversations, messages et requêtes client.
- Modèles persistants locaux pour conversations, messages, inbox, curseurs de lecture, outbox et modération.
- `IChatProvider` et `LocalChatProvider` avec les quatre canaux `Alliance`, `Server`, `Private` et `Leadership`.
- Permissions de lecture/écriture selon audience, alliance, serveur et rôles `member`, `officer`, `leader`, `moderator`.
- Non-lus calculés par curseur monotone, mentions, archivage, mute, suppression en tombstone et rapports de modération locaux.
- File offline déterministe avec reconnexion/rejeu et déduplication par `clientRequestId`.
- Fixtures locales déterministes : un exemple par canal, annonce dirigeants, mention, état vide, état masqué et message offline rejoué.
- Capacités explicites : `provider=local`, `server=false`, `official_gain=false`, `networkTransport=none`.

## Tests ciblés

`Assets/BeeKingdom/Tests/Editor/ChatMessagingLocalDataLayerTests.cs` couvre :

- les quatre canaux et leurs IDs stables ;
- l’annonce réservée aux dirigeants ;
- le message privé offline, la reconnexion et l’idempotence ;
- les non-lus par curseur, mentions et exclusion des messages de l’émetteur.

## Vérification

- `dotnet build BeeKingdom.Gameplay.csproj --no-restore` : réussi, 0 erreur.
- `dotnet build BeeKingdom.Tests.csproj --no-restore` : réussi, 0 erreur ; avertissements préexistants dans d’autres assemblies.
- Unity EditMode ciblé `BeeKingdom.Tests.Editor.ChatMessagingLocalDataLayerTests` : **4/4 passés**.

Aucun backend, endpoint, connexion réseau, scène Unity, PNG, APK, DNS, TLS, SQL ou donnée réelle n’a été créé ou modifié.
