# LivingHive Chat — reprise du cache récent côté serveur

Date : 2026-07-21  
Périmètre : `Server/` et tests serveur uniquement. Aucun fichier Unity/Assets, aucune activation ni déploiement.

La checklist de promotion de référence est `Docs/WorldMapCommunication/ChatMessaging_MandateEvidenceMatrix_2026-07-21.md`; ce jalon ne ferme aucune des portes .NET 8 natif, SQL jetable, cycle A→logout→B, TLS/IIS/Android ou shell authentifié.

## Garanties implémentées

- `ChatService.MarkRead` ne fait plus confiance au curseur proposé par le client : il est borné à la dernière séquence réellement persistée dans la conversation et fusionné monotoniquement avec le curseur durable.
- Les compteurs `UnreadCount` et `MentionCount` de la réponse sont recalculés à partir des messages autorisés du serveur, par pages de 100, et non à partir d’un corps ou de compteurs client.
- Le recalcul conserve la règle d’inbox existante : les messages envoyés par le joueur authentifié ne sont pas comptés comme non lus ni comme mentions à traiter.
- `GetMessages` vérifie l’appartenance `PlayerId` avant toute lecture de messages. Le dépôt renvoie les messages strictement par `Sequence` croissante et filtre `Sequence > afterSequence`, ce qui permet la reprise sans saut ni doublon.
- Les curseurs de conversations sont opaques, liés au joueur et réutilisables pour sélectionner une conversation hors de la première page.
- Les reçus d’envoi/création existants restent indexés par joueur et `ClientRequestId`; un rejeu identique retourne le résultat durable, tandis qu’une charge différente reste un conflit.
- Aucune partition, époque de session, identité, compteur ou corps déclaré par l’appareil n’est une autorité serveur : le `PlayerId` est exclusivement dérivé du bearer à chaque requête. Une réponse tardive d’une ancienne session ne peut donc pas lire ou acquitter les ressources du nouveau joueur; la reprise utilise le même reçu idempotent dans la partition authentifiée.

## Fichiers modifiés

- `Server/src/BeeKingdom.Chat/ChatService.cs`
- `Server/tests/BeeKingdom.Tests/ChatTransportContractTests.cs`

## Fichier ajouté

- `Docs/ProductionIntegration/LivingHiveChat_ProtectedRecentCache_ServerAuthoritativeReplay_2026-07-21.md`

## Preuves

Deux tests de contrat ont été ajoutés : reprise paginée ordonnée sans doublon et curseur de lecture borné par la dernière séquence serveur; sélection d’une conversation située hors de la première page avec contrôle d’appartenance d’un autre joueur.

La compilation et l’exécution ont été tentées avec `DOTNET_ROLL_FORWARD=Major` (seul runtime installé : .NET 10; les projets ciblent net8.0). La compilation du projet de tests et de ses dépendances aboutit. Le testhost .NET 10 ne découvre toutefois aucun test NUnit dans cet environnement (`Aucun test n'est disponible`), donc aucun nombre de tests réussis ne peut être revendiqué pour cette passe. La porte SQL n’a pas été contournée et aucun SQL externe n’a été exécuté.

Une build Release de `BeeKingdom.Server.csproj` a ensuite abouti avec 0 erreur et 1 avertissement existant de conflit de versions `Microsoft.Data.SqlClient`. Cette build ne remplace pas l’exécution requise sous runtime .NET 8 natif.

Après le recalcul des compteurs, `BeeKingdom.Chat.csproj` a été recompilé en Release avec 0 avertissement et 0 erreur.

Une tentative directe `dotnet vstest` avec `NUnit3.TestAdapter.dll` depuis le cache NuGet produit la même absence de découverte; le blocage est donc l’environnement/runtime, pas un résultat de test vert masqué.

## Porte de validation à reprendre

Sur une machine disposant de `Microsoft.NETCore.App 8.x` et du SDK .NET 8, exécuter depuis la racine :

```powershell
dotnet test Server/tests/BeeKingdom.Tests/BeeKingdom.Tests.csproj --framework net8.0 --logger "console;verbosity=minimal"
dotnet build Server/src/BeeKingdom.Server/BeeKingdom.Server.csproj -c Release --no-restore
```

La promotion reste interdite tant que cette exécution ne rapporte pas zéro test non découvert et zéro échec, puis que les portes SQL, TLS/IIS et Android staging ne sont pas levées séparément.

Le candidat local existant reste inchangé et `DeploymentAuthorized=false`; `Chat/Realtime` restent désactivés. Les portes .NET 8 natif, SQL, TLS/IIS et Android staging restent ouvertes.
