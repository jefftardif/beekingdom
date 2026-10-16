# Tests ciblés des groupes Chat Royal (.NET 8)

Depuis la racine du dépôt :

```powershell
dotnet test tools/tests/chat-groups/ChatGroups.Tests.csproj
```

Compile les sources Communication et exécute uniquement `ChatGroupTransportTests` et `LivingHiveChatGroupControllerTests`, avec transports simulés par ces suites. Les sources ne sont pas copiées.

Le shim remplace `Debug` par la console, `JsonUtility` par `System.Text.Json` avec `IncludeFields`, et reprend le contrat `IChatJsonCodec`. Les mappings du codec sont testés, mais pas la sémantique native de `UnityEngine.JsonUtility`.

Exclus : interface Unity, transport UnityWebRequest, transport SignalR, fabrique et bootstrap. Ce harness ne certifie ni les assemblages Unity jeu/éditeur, ni EditMode/PlayMode, le rendu, les échanges réseau réels ou la persistance serveur. Cette dernière relève des tests serveur ciblés et du test LocalDB dédié.
