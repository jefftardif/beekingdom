# LivingHive Phase 4 — composition et réservation d’escouade (serveur)

## Contrat livré

Le serveur ajoute un brouillon/réservation unique par ruche, cloisonné par `PlayerId` et `HiveId` :

- contrat `phase4-combat-squad-reservation-v1` et catalogue `phase4-combat-v1` ;
- familles strictes `guardians`, `wingrunners`, `darters` ;
- capacité technique initiale `12` (borne de contrat, non une règle de puissance ni un seed de production), avec plafond de désérialisation `1000` ;
- quantités positives au total, non négatives, exactement sur les trois clés canoniques, et plafonnées par capacité puis par roster disponible ;
- commit atomique : les unités sont réservées mais non consommées ; release atomique et idempotent ;
- même clé et même charge rejoue le reçu, charge différente => `game.idempotency_conflict` ; révision concurrente => `game.revision_conflict` ;
- migration `v7 -> v8` sans seed. Un état absent reste absent ; un état présent est borné et validé avant d’être autoritaire.

Routes fermées par défaut (`CombatSquadReservation:Enabled=false`, y compris Production) :

`GET /game/v1/hives/{hiveId}/combat/squad-reservation`, `POST .../commit`, `POST .../release`.

Le flag false répond `503 game.unavailable` avant authentification ou lecture. Les routes actives dérivent l’identité exclusivement du Bearer et ne font confiance à aucune partition, quantité ou révision client sans validation transactionnelle.

## Preuves locales

- `dotnet test ...BeeKingdom.HiveOperations.Tests.csproj --no-restore --filter CombatSquadReservationTests` avec `DOTNET_ROLL_FORWARD=Major` : **2/2**.
- Suite `BeeKingdom.HiveOperations.Tests` avec le même roll-forward : **46/46**, 0 échec.
- Build Release `BeeKingdom.Server.csproj --no-restore` : **0 erreur**, 1 avertissement préexistant de conflit `Microsoft.Data.SqlClient` (5.x/6.x).
- Runtime natif .NET 8 x64 absent dans la VM ; l’exécution a utilisé le runtime disponible 10.0.10 via roll-forward majeur. Aucune preuve SQL externe, TLS/IIS, Android staging ou HTTP WebApplicationFactory n’est revendiquée dans ce lot.

## Fichiers modifiés/créés

- `Server/src/BeeKingdom.HiveOperations/HiveOperationModels.cs`
- `Server/src/BeeKingdom.HiveOperations/HiveStateMigrator.cs`
- `Server/src/BeeKingdom.HiveOperations/CombatSquadReservationService.cs` (nouveau)
- `Server/src/BeeKingdom.HiveOperations/CombatSquadReservationOptions.cs` (nouveau)
- `Server/src/BeeKingdom.Server/Program.cs`
- `Server/src/BeeKingdom.Server/appsettings.json`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`
- `Server/tests/BeeKingdom.HiveOperations.Tests/CombatRecruitmentTests.cs`
- `Server/tests/BeeKingdom.HiveOperations.Tests/CombatSquadReservationTests.cs` (nouveau)
- `Docs/ProductionIntegration/LivingHive_Phase4_CombatSquadReservation_Server_2026-07-22.md` (nouveau)

## Correctif d’audit

La release persiste désormais les trois clés canoniques à zéro et a été relue après reconstruction du repository. Les hash invalides sont calculés sans déréférencer une charge nulle; chaque quantité est bornée à 1 000 000, la somme est protégée contre overflow, `ExpectedRevision` négative est refusée, et les reçus sont plafonnés à 4096 avec `game.receipts_full`.

Le migrateur refuse les dictionnaires incomplets/non canoniques, les réservations nulles avec quantités non nulles, les identifiants vides, les totaux nuls identifiés, les dépassements de capacité/roster et les reçus invalides. Les routes contrôlent les clés d’idempotence de longueur maximale 256.

Preuves après correctif : sous-suite réservation/recrutement **7/7**, suite HiveOperations **47/47**, build Release **0 erreur** (même avertissement SqlClient préexistant), `dotnet/testhost=0`. Aucune exécution HTTP n’est revendiquée.

## Preuve finale de reconstruction

Le test `Commit_and_release_are_idempotent_and_do_not_consume_roster` couvre désormais : commit, reconstruction du repository/service, lecture de la réservation, conflit d’un second commit actif, release, reconstruction, lecture des trois zéros et du roster intact, rejeu release idempotent, et absence de visibilité pour un autre couple joueur/ruche. Le test de bornes couvre une charge totale réellement supérieure à 12 en plus du dépassement du roster.

`DeploymentAuthorized=false`; aucun candidat, transfert, activation ou déploiement n’a été effectué. Les portes restantes sont le runtime .NET 8 natif, SQL jetable/reconstruction, HTTP réellement découvert, TLS/IIS et Android staging.
## Validation HTTP et contrat public — 2026-07-23

Les mutations HTTP renvoient désormais `{ receipt, snapshot }`. Le reçu public
contient `playerId`, `hiveId`, `idempotencyKey`, `action`, `reservationId`, les
quantités canoniques, les révisions de réservation avant/après, `acceptedAtUtc`
et `code`; aucune clé interne ni `payloadHash` n'est exposée. L'horloge est injectée
via `IServerClock`. Les reçus sont bornés à 128, avec éviction par date puis clé,
en conservant le reçu courant. Les révisions négatives/`long.MaxValue`, les
dépassements et les sur-réservations sont refusés avant mutation.

Preuves : `CombatSquadReservationEndpointTests` 3/3 ; build Release 0 erreur,
2 avertissements existants ; flags `CombatSquadReservation:Enabled=false` par
défaut et Production. La suite serveur complète reste à rejouer après cette
passe avant toute promotion ; aucun candidat ni déploiement.
