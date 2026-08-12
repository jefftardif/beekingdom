# SERVER-064 - World Map Chunk Query Service Wave 2 Report

Date locale: 2026-07-14
Perimetre: local / transport-neutral only
Verdict: PASS local, sans endpoint HTTP, sans SQL, sans staging, sans acces distant, sans Unity.

## Resume

SERVER-064 ajoute une couche locale de service de requete pour les chunks de carte mondiale. Le service accepte `WorldMapChunkRequest` et retourne un resultat type autour de `WorldMapChunkWindowResponse`.

La couche reste transport-neutral: aucun endpoint, aucun controller, aucune registration ASP.NET, aucun HTTP 304 reel, aucune persistance officielle et aucun claim live.

## Sources relues

- `C:\projets\beekingdom\QA\QA_SERVER_B_063_WORLD_MAP_CHUNK_JSON_CONTRACT_VALIDATION.md`
- `C:\projets\beekingdom\prompt_server\rapports\SERVER-B-063 - World Map Chunk JSON Contract Correction Report.md`
- `C:\projets\beekingdomgame-master\Server\src\BeeKingdom.Shared\WorldMap\WorldMapChunkContracts.cs`
- `C:\projets\beekingdomgame-master\Server\ops\world-map-chunk-contract`

## Architecture realisee

Ajout dans `BeeKingdom.Shared.WorldMap`:

- `IWorldMapChunkQueryService`
- `WorldMapChunkQueryService`
- `WorldMapChunkQueryResult`
- `WorldMapChunkQueryResultState`
- `IWorldMapChunkIdentityProvider`
- `IWorldMapChunkOverlayProvider`
- `WorldMapChunkWorldState`
- `WorldMapChunkOverlayQuery`
- `DeterministicLocalWorldMapChunkIdentityProvider`
- `DeterministicLocalWorldMapChunkOverlayProvider`

Etats de resultat:

- `Success`: reponse canonique `WorldMapChunkWindowResponse`.
- `NotModified`: cache metadata seulement, sans response body.
- `Rejected`: erreurs typees existantes `WorldMapChunkContractError`.

## Decisions techniques

- `WorldMapChunkReadinessContract.CreateReadinessWindow` reste la source unique pour fenetre, clipping, ordre Y/X, hashes, ETags, guardrails et non-claims.
- Les providers sont injectables et read-only; les implementations deterministes sont reservees aux tests/local readiness.
- La verification `IfNoneMatch` reste un etat type local, pas un comportement HTTP.
- Les overlays restent separes du fond, et les vols restent air-only sans graphe routier.
- Le verificateur ops utilise des repertoires CLI/NuGet locaux au workspace afin d'eviter le profil utilisateur et tout feed reseau.

## Fichiers crees

- `C:\projets\beekingdomgame-master\Server\src\BeeKingdom.Shared\WorldMap\WorldMapChunkQueryService.cs`
- `C:\projets\beekingdom\prompt_server\rapports\SERVER-064 - World Map Chunk Query Service Wave 2 Report.md`
- `C:\projets\beekingdomgame-master\Server\ops\world-map-chunk-contract\SERVER-064 - World Map Chunk Query Service Wave 2 Report.md`

## Fichiers modifies

- `C:\projets\beekingdomgame-master\Server\tests\BeeKingdom.Tests\SharedContractsTests.cs`
- `C:\projets\beekingdomgame-master\Server\ops\world-map-chunk-contract\README.md`
- `C:\projets\beekingdomgame-master\Server\ops\world-map-chunk-contract\world-map-chunk-contract-spec.md`
- `C:\projets\beekingdomgame-master\Server\ops\world-map-chunk-contract\Test-WorldMapChunkContract.ps1`

## Tests realises

Tests cibles shared contracts:

- Commande: `dotnet test .\Server\tests\BeeKingdom.Tests\BeeKingdom.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~SharedContractsTests" --logger "console;verbosity=minimal"`
- Resultat: 89 reussis, 0 echec, 0 ignore.

Suite serveur complete:

- Commande: `dotnet test .\Server\BeeKingdom.Server.slnx --configuration Release --no-restore --logger "console;verbosity=minimal"`
- Resultat: 180 reussis, 0 echec, 6 ignores, total 186.

Skips exacts:

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

Verificateur ops:

- Commande: `powershell -ExecutionPolicy Bypass -File C:\projets\beekingdomgame-master\Server\ops\world-map-chunk-contract\Test-WorldMapChunkContract.ps1`
- Resultat: `WORLD_MAP_CHUNK_JSON_CONTRACT_VERIFICATION = PASS`
- Full window: 25 chunks, manifest hash `a6e46a84bc24cb94111c09a1a3ea44aced10323575f6a2dad47497b477b55fa1`, ETag `W/"ac2b6a99deb6456e95fed31fd40e0417c87bd88eed336644419abc4fbef92d72"`.
- Edge window: 9 chunks, manifest hash `d9038abfee2eb1150d4e04986fa3a8e8cf879811398c04d102ba80c5f85c754a`, ETag `W/"06948970e15cf1d8bdb8246318ded665b7c20b0375770b4ea442a08dfe689aa1"`.

## Couverture

- `Success` avec reponse canonique: prouve.
- `NotModified` avec `IfNoneMatch` exact: prouve, sans body.
- `Rejected` avec erreurs typees: prouve.
- Fenetre 5x5/25 et bord 3x3/9: prouve.
- Meme requete/etat: memes bytes/hash/ETag.
- Changement seed ou artistic revision: nouveau manifest hash/ETag/invalidation.
- World/server mismatch: rejet type.
- Cancellation: prouve.
- Lectures concurrentes: prouve.
- Absence de fuite cross-world: prouve.
- Overlays separes et vols air-only: prouve.
- Aucun graphe routier: prouve.

## Scans et residus

- Aucun `WorldMapChunkQueryService` ou `IWorldMapChunkQueryService` reference dans `BeeKingdom.Server`.
- Aucun endpoint HTTP WorldMapChunk cree.
- Aucun SQL, staging, remote access, secret ou donnee joueur reelle ajoute.
- Aucun residu local du verificateur dans `Server\ops\world-map-chunk-contract` (`bin`, `obj`, `.dotnet-home`, `.nuget-packages`, `.nuget-http-cache`, `.appdata` absents apres execution).

## Dette technique

- Un futur adapter HTTP pourra mapper `NotModified` vers HTTP 304, mais cette wave ne le fait pas.
- Les providers deterministes ne sont pas une autorite live multi-noeud.
- Persistence, auth, rate limiting, cache distribue et observabilite live restent hors scope.

## Conformite

- Local / transport-neutral only: conforme.
- Aucun endpoint live: conforme.
- Aucun SQL/staging/remote: conforme.
- Aucun changement Unity: conforme.
- Aucun claim serveur live: conforme.

SERVER_064_WORLD_MAP_CHUNK_QUERY_SERVICE = PASS

TRANSPORT_NEUTRAL_QUERY_SERVICE_READY = YES

HTTP_OR_LIVE_ENDPOINT_CREATED = NO

READY_FOR_QA_WORLD_MAP_CHUNK_QUERY_SERVICE = YES
