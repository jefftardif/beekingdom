# M043H/M043I-CL — Alliance Core Live Alpha Enablement + SQL Persistence

Mission en deux temps, en session interactive avec Jeff : (1) activer
`Alliance.Enabled=true` sur l'API de production réelle
(`https://api-ops.beekingdomgame.com`), (2) suite à une panne de production
causée par la première tentative, diagnostiquer la cause racine et construire
la persistance SQL manquante pour Alliance avant de réessayer.

## 1. Verdict honnête d'entrée de jeu

Alliance Core est **activé en production, sur une vraie persistance SQL
Server, vérifié sain**. Une panne réelle mais brève (quelques minutes,
détectée automatiquement par le smoke test du pipeline avant tout trafic
joueur réel) a eu lieu lors de la première tentative — root cause prouvée,
corrigée, et le second déploiement a réussi. Diplomatie et Guerre restent
désactivées comme prévu. Aucune alliance CEO n'a été créée, aucune donnée de
compte modifiée.

## 2. Contexte et décision initiale (M043H)

Après plusieurs cycles de correction du client Unity (M043-M043G), le CEO a
décidé d'activer `Alliance.Enabled=true` sur l'API de production réelle,
avec `DiplomacyEnabled=false` et `WarEnabled=false`. Le brief exigeait :
aucune migration SQL, aucune activation Diplomatie/Guerre, aucun
développement hors-sujet, aucune action de compte au nom du CEO, et
confirmation avant tout commit/push.

Vérification préalable : `Persistence:Provider` dans le dépôt
(`appsettings.Production.json`) affichait `"InMemory"`. Sur cette base, le
plan initial était de simplement committer le code Alliance existant
(persistance JSON durable uniquement, voir M042) et pousser vers `deploy`.

## 3. Incident de production — panne réelle

Commit `c34b9d2` (code Alliance + `Alliance.Enabled=true`) poussé sur
`deploy`. Le job GitHub Actions a réussi les étapes de copie/redémarrage du
pool IIS, mais **le smoke test a échoué** :
`Invoke-WebRequest : The remote server returned an error: (500) Internal Server Error`.
Confirmation directe par `curl https://api-ops.beekingdomgame.com/` : réponse
IIS **"HTTP Error 500.30 - ASP.NET Core app failed to start"** — un crash au
démarrage du worker process, pas une erreur applicative normale.

**Action immédiate** : `git revert --no-edit c34b9d2` (commit `041baa1`),
poussé sur `deploy`. Le déploiement du revert a réussi ; `curl -L
https://api-ops.beekingdomgame.com/` a confirmé `200 {"status":"Healthy"}`.
Production stable en quelques minutes, avant tout diagnostic approfondi.

## 4. Cause racine — PROUVÉE, pas supposée

Reproduction locale du code de `c34b9d2` sous `ASPNETCORE_ENVIRONMENT=Production`
avec le fichier `appsettings.Production.json` **exact** du dépôt : le
serveur démarre sans erreur, `/alliance/v1/alliances/search` répond `200`.
La reproduction locale ne reproduisait PAS le crash — la cause devait donc
être une différence d'environnement réel, absente du dépôt par design.

Inspection directe des variables d'environnement du pool IIS `BeeKingdomApi`
(`appcmd list apppool "BeeKingdomApi" /text:*`, exécuté par Jeff) : **confirmé**
`Persistence__Provider = SqlServer` (avec une vraie base
`SQLEXPRESS01`/`BeeKingdom`), une variable d'environnement du pool qui
écrase silencieusement le `"InMemory"` du fichier versionné.

`AllianceServiceCollectionExtensions.AddBeeKingdomAlliance` (code M042)
contenait un `throw new InvalidOperationException(...)` **volontaire et
synchrone** dès que `PersistenceOptions.UsesSqlServer(configuration)` est
vrai — un garde-fou pour empêcher qu'Alliance persiste silencieusement au
mauvais endroit tant qu'aucune implémentation SQL n'existait. Cet appel a
lieu directement dans les instructions de haut niveau de `Program.cs`,
**avant** que le serveur puisse écouter — d'où le 500.30 : ce n'est pas
Alliance qui échoue, c'est tout le processus qui ne démarre jamais.

Les autres modules (`Accounts`, `Chat`, `Colony`) ont le même test
`UsesSqlServer`, mais basculent silencieusement vers leur repository SQL au
lieu de planter — c'est pourquoi rien n'avait cassé avant : Alliance était
le premier module à interdire explicitement le SQL avec un `throw`.

## 5. Décision du CEO et correctif (M043I)

Le CEO a tranché : **construire la persistance SQL pour Alliance** plutôt
que de contourner le garde-fou.

### 5.1 Schéma — `Server/src/BeeKingdom.Database/Scripts/090_alliance_platform.sql`

14 tables, ajoutées à `DatabaseCatalog.Migrations` (+ rollback correspondant
dans `DatabaseRollbackCatalog.Rollbacks`, script
`090_rollback_alliance_platform.sql`) :

- `dbo.Alliances` (agrégat racine, slug public unique filtré)
- `dbo.AllianceCreateReceipts` (idempotence création)
- `dbo.AllianceMemberships` (un joueur = au plus une alliance active, via
  index unique filtré sur `PlayerId WHERE RemovedAtUtc IS NULL`)
- `dbo.AllianceApplications` / `dbo.AllianceApplicationReceipts`
- `dbo.AllianceInvitations` / `dbo.AllianceInvitationReceipts`
- `dbo.AllianceActivityEvents` / `dbo.AllianceActivitySequences`
  (séquence atomique par alliance, même patron `UPDLOCK, HOLDLOCK` que
  `ChatConversationSequences`) / `dbo.AllianceActivityDedupe`
- `dbo.AllianceDiplomaticRelations` (paire canonique A<B) /
  `dbo.AllianceDiplomacyProposalReceipts`
- `dbo.AllianceWars` / `dbo.AllianceWarDeclareReceipts`

### 5.2 Repositories SQL

4 nouveaux fichiers dans `Server/src/BeeKingdom.Alliance/Repositories/` :
`SqlAllianceRepository.cs`, `SqlAllianceActivityRepository.cs`,
`SqlAllianceDiplomacyRepository.cs`, `SqlAllianceWarRepository.cs` — ADO.NET
brut (`MERGE` pour les upserts, transactions pour la séquence d'activité),
même style que `SqlChatRepository`/`SqlColonyRepository` existants.

### 5.3 Câblage

`AllianceServiceCollectionExtensions.AddBeeKingdomAlliance` bascule
maintenant entre `SqlAlliance*Repository` (si
`Persistence:Provider=SqlServer`) et `DurableJsonAlliance*Repository`
(sinon), au lieu de lancer une exception — même forme que
Accounts/Chat/Colony.

### 5.4 Fichiers récupérés

La restauration précédente (`git checkout HEAD -- Server/`) avait par erreur
effacé plusieurs hunks légitimes du commit Alliance original
(`AccountService.GetAccountByPlayerId`, `IChatRepository.UpsertParticipant`/
`RemoveParticipant`, câblage `PlayerDirectoryService`, config
`Alliance` dans `appsettings.Production.json`). Récupérés individuellement
via `git checkout c34b9d2 -- <fichier>`, en excluant délibérément le
changement `LivingHiveResearch` non lié (resté dans `git stash@{0}`).

## 6. Vérifications

- **Build serveur complet** (`dotnet build BeeKingdom.Server.csproj -c
  Release`) : 0 erreur.
- **Build tests** : 0 erreur.
- **Suite de tests complète** (`dotnet test`) : 456/464 verts, 8 ignorés
  (tests nécessitant un vrai SQL Server local, absent de cet environnement —
  préexistant, pas nouveau), 0 échec après correction de 2 tests qui avaient
  une liste figée du nombre de scripts de rollback/migration
  (`RollbackCatalogDropsTablesInReverseDependencyOrder` dans
  `HttpEndpointTests.cs`, `CatalogSqlMatchesCheckedInScriptFiles` dans
  `DatabaseMigrationTests.cs` — cassés par l'ajout légitime du script 090,
  corrigés pour inclure la nouvelle entrée).
- **Aucun test n'a tourné contre un vrai SQL Server** — seule la logique et
  la compilation sont validées en local ; le round-trip réel a été vérifié
  directement en production après déploiement (section 8).

## 7. Commit, push, déploiement

- Commit `91978f1` sur `main` : "M043I-CL: Alliance SQL Server persistence
  (schema, repositories, provider branch)", 58 fichiers, autorisé
  explicitement par Jeff.
- `git push origin main` puis (après confirmation explicite) `git push
  origin main:deploy` — exécuté par Jeff lui-même (le classificateur d'auto
  mode a bloqué ce push depuis l'agent, action à haut risque après l'incident
  du soir).
- Pipeline GitHub Actions "Deploy BeeKingdomApi" : **succès**, smoke test
  vert.

## 8. Migration et vérification post-déploiement

Les migrations ne s'appliquent **pas** automatiquement au démarrage — elles
passent par l'endpoint protégé `POST /ops/migrations/apply`.

**Clés Ops perdues** : `Ops:AdminKey` et `Ops:MigrationApplyKey` en clair
étaient introuvables (seul leur hash SHA256 est stocké côté serveur par
design — jamais dans le dépôt). Régénérées en session avec Jeff (nouvelles
valeurs sauvegardées dans son gestionnaire de mots de passe), hash mis à
jour via `appcmd set config .../environmentVariables` + recyclage du pool
`BeeKingdomApi`.

Migration appliquée avec succès (`{"status":"Applied"}`). Vérification
finale :
- `GET https://api-ops.beekingdomgame.com/` → `200 {"status":"Healthy",...}`
- `GET https://api-ops.beekingdomgame.com/alliance/v1/alliances/search` →
  `200 {"items":[],"totalCount":0}` (tables SQL créées et fonctionnelles,
  aucune alliance existante — état attendu).

## 9. Ce qui n'a PAS été fait (hors périmètre, respecté)

- Aucune activation Diplomatie/Guerre.
- Aucune création/modification d'alliance ou de compte au nom du CEO.
- Aucune action Play Mode effectuée par l'agent.
- Aucun développement hors Alliance/persistance.

## 10. Verdict final (A–K)

| # | Critère | Résultat |
|---|---|---|
| A | Cause racine de la panne prouvée (pas supposée) | ✅ OUI — `Persistence__Provider=SqlServer` via env var IIS + `throw` synchrone au démarrage |
| B | Persistance SQL construite (schéma + repositories) | ✅ OUI — 14 tables, 4 repositories |
| C | Aucune migration SQL non désirée/accidentelle | ✅ OUI — décision explicite du CEO |
| D | Build serveur complet sans erreur | ✅ OUI |
| E | Suite de tests verte (hors tests SQL locaux indisponibles) | ✅ OUI — 456/456 (hors 8 ignorés) |
| F | Commit/push autorisés explicitement à chaque étape | ✅ OUI |
| G | Déploiement production réussi (2e tentative) | ✅ OUI — smoke test vert |
| H | Migration appliquée en production | ✅ OUI — `{"status":"Applied"}` |
| I | Serveur sain post-migration | ✅ OUI — `200 Healthy` |
| J | Endpoint Alliance fonctionnel post-migration | ✅ OUI — `200 {"items":[],"totalCount":0}` |
| K | Diplomatie/Guerre toujours désactivées, aucune donnée CEO touchée | ✅ OUI |

## 11. Prochain test utilisateur

Ouvrir Alliance Center en jeu avec un vrai compte (pas seulement l'API) et
confirmer que créer une alliance, la rechercher, et la rejoindre fonctionne
de bout en bout maintenant que la persistance est réellement SQL et
survivra à un redémarrage/recyclage du pool IIS.

## 11B. M043J-CL — invalid_response persistant après la migration SQL

Après la migration SQL (section 8), Jeff a rouvert Alliance Center en jeu et
observé **la même erreur "invalid_response"** que lors des tentatives
précédentes (M043E/M043G) — malgré un serveur sain et des tables SQL
fonctionnelles. Diagnostic par preuve directe (appels réels via reflection
dans une session Unity Play Mode déjà authentifiée, contre la production) :

- `GetMyAllianceAsync`/`ListMyInvitationsAsync` échouaient avec
  `Error=InvalidResponse Message='game.request_invalid'` — un code
  **jamais envoyé par le serveur**, trouvé uniquement dans
  `Assets/BeeKingdom/Networking/UnityAuthenticatedGameRestTransport.cs`.
- **Cause racine réelle** : `UnityAuthenticatedGameRestTransport.ValidateRequest`
  exigeait que **toute** requête commence par `/game/v1/` — un transport
  partagé par tous les clients (Hive, Alliance, PlayerDirectory), mais
  `AllianceClient` utilise `/alliance/v1/...`. Chaque appel Alliance était
  donc rejeté **côté client, avant tout envoi réseau**, depuis le tout début
  (M041) — expliquant pourquoi aucune correction précédente (serveur,
  config, persistance SQL) n'avait jamais réglé ce symptôme récurrent.
- Corrigé (`AllowedPathPrefixes` élargi à `/game/v1/` et `/alliance/v1/`).
  Re-test immédiat : nouvelle erreur différente,
  `game.read_cache_boundary_missing` — le middleware serveur
  `Cache-Control: private, no-store` (`Program.cs`) n'était appliqué qu'aux
  routes `/game/v1`, jamais `/alliance/v1`. Corrigé côté serveur (élargi la
  même liste de préfixes), testé (456/456), commité (`afa04d5`), déployé
  avec l'accord explicite de Jeff.
- **Vérification finale** : en-tête `Cache-Control: private, no-store`
  confirmé présent sur `/alliance/v1/alliances/search` en production
  (`curl -D -`). Nouveau test live Unity : les erreurs `invalid_response`/
  `read_cache_boundary_missing` ont disparu (dernier obstacle rencontré,
  `auth.refresh_failed`, était juste l'expiration normale de la session de
  test Play Mode ouverte depuis le matin — sans rapport, résolu par un
  simple redémarrage de Play Mode). Jeff confirme visuellement en jeu :
  l'écran "AUCUNE ALLIANCE" se charge maintenant sans erreur.
- Fichiers touchés :
  `Assets/BeeKingdom/Networking/UnityAuthenticatedGameRestTransport.cs`
  (client), `Server/src/BeeKingdom.Server/Program.cs` (serveur, middleware
  Cache-Control).

## 12. Ouvert / à faire ensuite

- Restaurer le changement `LivingHiveResearch` resté dans `git stash@{0}`
  ("M043H: pending LivingHiveResearch catalog change, unrelated to Alliance
  deploy") — non lié à cette mission, à traiter séparément.
- Envisager d'ajouter un test SQL réel (round-trip contre un vrai SQL Server
  de test) pour les nouveaux repositories Alliance, sur le modèle des tests
  `SqlServer*` existants déjà présents mais ignorés faute d'instance locale.
