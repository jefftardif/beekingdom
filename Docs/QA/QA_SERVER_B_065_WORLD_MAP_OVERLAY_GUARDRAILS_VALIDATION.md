# QA SERVER-B-065 - World Map Overlay Guardrails Correction

Date QA : 2026-07-14  
Role : QA-A / Lead Quality Engineer  
Perimetre : correction locale du service WorldMap transport-neutral, sans endpoint, SQL, staging, acces distant ou Unity

## Publication

Le chemin principal `C:\projets\beekingdom\QA\QA_SERVER_B_065_WORLD_MAP_OVERLAY_GUARDRAILS_VALIDATION.md` n'est pas inscriptible dans la session QA. Le present rapport utilise le chemin de repli autorise :

`C:\projets\beekingdomgame-master\Docs\QA\QA_SERVER_B_065_WORLD_MAP_OVERLAY_GUARDRAILS_VALIDATION.md`

## Verdict

**PASS**

Les quatre blocages ouverts par QA sur SERVER-064 sont fermes dans le perimetre local, deterministe et non-live. La composition finale des overlays passe maintenant par une frontiere canonique unique, le budget est recalcule sur les comptes reels du provider, les violations d'autorite/live/vol sont refusees avec une taxonomie dediee, et l'isolation multi-world est prouvee avec une meme instance de service et des providers partages.

Aucun endpoint HTTP/live, enregistrement runtime, SQL, staging, acces distant, secret, donnee reelle ou changement Unity n'est valide par ce PASS.

## Sources controlees

- `Docs/QA/QA_SERVER_064_WORLD_MAP_CHUNK_QUERY_SERVICE_WAVE2_VALIDATION.md`
- `Server/ops/world-map-chunk-contract/SERVER-B-065 - World Map Overlay Guardrails Correction Report.md`
- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkContracts.cs`
- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkQueryService.cs`
- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkJson.cs`
- `Server/tests/BeeKingdom.Tests/SharedContractsTests.cs`
- `Server/ops/world-map-chunk-contract/`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`

## Fermeture des blocages SERVER-064

| Blocage QA-064 | Resultat | Preuve QA independante |
|---|---|---|
| B1 - budget calcule avant les overlays provider | FERME | `FinalizeReadinessOverlays` recalcule `EstimatedPayloadBytes` avec les comptes provider et remplace le guardrail final. Un payload a 1000 ressources est refuse. |
| B2 - claims `Live` / `ServerAuthoritative` non imposes | FERME | Refus au niveau enveloppe, ruche, ressource et vol; `PaintedIntoBackground`, `AirOnly=false` et `RoadGraphUsed=true` sont aussi refuses. |
| R1 - preuve multi-world sur deux services distincts | FERME | Le test utilise une seule instance de service, un identity provider partage a deux mondes et un overlay provider partage. |
| R2 - mauvaise taxonomie `PayloadBudgetExceeded` | FERME | `OverlayContractViolation = 8` est dedie aux violations overlay; `PayloadBudgetExceeded = 2` reste reserve au budget. |

La reserve R3 de QA-064 sur l'ETag des overlays dynamiques reste un gate futur, hors du perimetre du provider local deterministe. Elle ne bloque pas SERVER-B-065.

## Audit de la frontiere canonique

Controle statique :

```text
FinalizeReadinessOverlays definitions        = 1
FinalizeReadinessOverlays calls in service   = 1
EstimatePayloadBytes definitions             = 1
Payload formula terms duplicated in service  = 0
```

`WorldMapChunkReadinessContract.FinalizeReadinessOverlays` effectue dans l'ordre :

1. calcul de l'estimation sur `response.Chunks.Count` et les listes provider reelles;
2. remplacement de `Guardrails.EstimatedPayloadBytes`;
3. nettoyage des erreurs derivees d'une finalisation precedente;
4. validation du contrat overlay;
5. ajout de `PayloadBudgetExceeded` si le budget final est depasse;
6. retour de la reponse finale avec overlays, guardrails et erreurs coherents.

Le service appelle cette frontiere apres le provider, puis traite `response.Errors` avant `NotModified` et avant `Success`. Il ne contient aucune copie de la formule `2048 + chunks*512 + hives*256 + resources*256 + flights*384`.

## Budget final et rejets

| Cas | Chunks | Ruches | Ressources | Vols | Estimation | Budget | Resultat |
|---|---:|---:|---:|---:|---:|---:|---|
| Provider conforme etendu | 25 | 2 | 3 | 2 | 16896 | 98304 | `Success` |
| Provider surdimensionne | 25 | 1 | 1000 | 1 | 271488 | 98304 | `Rejected / PayloadBudgetExceeded` |

La seconde finalisation du meme payload surdimensionne conserve une estimation de `271488` et une seule erreur derivee. Pour le rejet service, `Response`, `ETag`, `ManifestHash` et `InvalidationKey` sont tous nuls.

## Contrat overlay et taxonomie

Les scenarios negatifs couvrent et refusent individuellement :

- enveloppe `PaintedIntoBackground=true`;
- enveloppe `Live=true`;
- enveloppe `ServerAuthoritative=true`;
- ruche `Live=true` ou `ServerAuthoritative=true`;
- ressource `Live=true` ou `ServerAuthoritative=true`;
- vol `Live=true` ou `ServerAuthoritative=true`;
- vol `AirOnly=false`;
- vol `RoadGraphUsed=true`.

Ces cas retournent `OverlayContractViolation`. L'enum conserve exactement les valeurs historiques `0..7`; le nouveau code est ajoute en valeur `8` :

```text
0 RadiusOutOfRange
1 InvalidWorldBounds
2 PayloadBudgetExceeded
3 UnknownWorld
4 UnknownChunk
5 ManifestRevisionMismatch
6 DeltaTokenInvalid
7 AuthRequiredFuture
8 OverlayContractViolation
```

Les deux exemples JSON portent ce catalogue dans le meme ordre.

## Resultats types et metadonnees

### Success

- `State = Success`;
- `Response` non nulle;
- ETag, manifest hash et invalidation key recopies exactement depuis `Response.Cache`;
- `Errors` vide;
- estimation finale du provider visible dans les guardrails.

### NotModified

- `State = NotModified`;
- `Response = null`;
- ETag, manifest hash et invalidation key conserves;
- `Errors` vide;
- comparaison `IfNoneMatch` ordinale;
- aucun claim HTTP 304.

### Rejected

- `State = Rejected`;
- `Response = null`;
- ETag, manifest hash et invalidation key nuls;
- erreur typee stable correspondant au rejet.

## Concurrence et isolation multi-world

La preuve corrigee construit exactement :

- une instance `WorldMapChunkQueryService`;
- un `SharedWorldMapChunkIdentityProvider` contenant deux couples `(WorldId, GameServerId)`;
- un `DeterministicLocalWorldMapChunkOverlayProvider` partage;
- quatre lectures concurrentes, deux par monde, via ce meme graphe d'objets.

Les quatre lectures reussissent avec les bons WorldId/GameServerId, des cles d'invalidation scopees et des manifests distincts. Le croisement `(worldA, serverB)` est refuse avec `UnknownWorld` et toutes les metadonnees de cache nulles.

## Executions QA independantes

### Tests cibles

```text
Discovered: 15
Passed:     15
Failed:      0
Skipped:     0
```

Les quinze noms correspondent a la matrice SERVER-B-065, dont budget final, toutes les violations overlay, metadonnees, annulation et concurrence partagee.

### SharedContractsTests

```text
Passed:  74
Failed:   0
Skipped:  0
```

### Suite Release complete

```text
Discovered: 171
Passed:     165
Failed:       0
Skipped:      6
```

Les six skips sont exclusivement les scenarios SQL opt-in historiques :

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

Aucun test WorldMap, JSON ou query-service n'est ignore.

## Verificateur ops hors ligne

Le replay decisif a utilise simultanement :

- le `NuGet.Config` du paquet avec zero source;
- un profil NuGet temporaire QA preconfigure lui aussi avec zero source;
- `powershell.exe -NoProfile -ExecutionPolicy Bypass` limite au processus;
- aucun feed ou profil utilisateur reel.

Resultat :

```text
WORLD_MAP_CHUNK_JSON_CONTRACT_VERIFICATION = PASS
ExitCode                                      = 0
Repository NuGet source count                 = 0
QA profile NuGet source count                 = 0
FullWindowChunks                              = 25
EdgeWindowChunks                              = 9
Verifier bin exists after                     = false
Verifier obj exists after                     = false
Isolated QA profile exists after              = false
```

Un premier replay avait laisse NuGet creer sa configuration par defaut dans un profil temporaire, bien que `--configfile` imposat deja le fichier local. QA a supprime exactement ce profil et a rejoue avec les deux configurations verrouillees a zero source. Seul le second replay sert au verdict.

## Invariants spatiaux, cache et exemples

| Preuve | Fenetre complete | Edge |
|---|---|---|
| Chunks | 25 | 9 |
| Premier / dernier | `(8,-6)` / `(12,-2)` | `(0,0)` / `(2,2)` |
| Ordre | Y puis X | Y puis X |
| Manifest hash | `a6e46a84bc24cb94111c09a1a3ea44aced10323575f6a2dad47497b477b55fa1` | `d9038abfee2eb1150d4e04986fa3a8e8cf879811398c04d102ba80c5f85c754a` |
| ETag | `W/"9e17913cd519f0a06f111055b78c5aae3e7fc3119ac89d5f82490fed7d6c8151"` | `W/"3073255be6df2ae53ea1dd68da925c78a8bf705b7cf26e5bec51e330722e9a24"` |

Empreintes SHA-256 des exemples :

- `example-window-5x5.json` : `4c876e095572072b213eac81a4a2e49773a4ae1fc43b88fada093e5477363421`;
- `example-edge-window.json` : `8057cd362dc24fda1122b137f1877d2590983dcd01a0e56c32943b81e6f65dd2`.

Les overlays restent separes du fond. Tous les vols des exemples sont `AirOnly=true`, `RoadGraphUsed=false`, non-live et non-authoritatifs.

## Perimetre, non-claims et residus

- `BeeKingdom.Shared` reste une bibliotheque `net8.0` sans package, framework ou projet externe ajoute.
- Les symboles du query service ne sont trouves dans `Server/src` que dans leur fichier de declaration; aucun enregistrement DI, controller, route ou adapter HTTP n'est present pour ce service.
- Le scan des fichiers corriges ne trouve aucun secret, URL, client SQL, binding HTTP ou reference Unity.
- Les deux variables SQL opt-in sont absentes pendant les executions.
- `appsettings.Production.json` conserve `Persistence.Provider = InMemory`.
- Les references de preparation distante deja presentes dans la configuration restent `PreparationOnly` / `NotRouted`; QA n'a effectue ni valide aucune connexion ou action distante.
- Aucun `.trx`, `.mdf`, `.ldf`, `.bak`, `.db`, `.sqlite` ou `.tmp` n'a ete produit dans `Server`.
- Aucun `bin/obj` ne reste dans le paquet du verificateur.
- Aucun code, rapport producteur, fichier Unity ou configuration produit n'a ete modifie par QA.

## Limite future non bloquante

L'ETag actuel derive du manifeste de fond. Il n'inclut pas une revision d'overlays dynamiques. Avant tout provider dynamique ou toute semantique live de `NotModified`, une revision/validation overlay separee devra etre definie et testee.

Le provider valide ici est local, deterministe et non-live. Cette limite ne contredit donc pas ses bytes repetables et ne bloque ni la fermeture de SERVER-064 ni la prochaine wave locale.

## Decision de gate

- Les quatre blocages SERVER-064 sont fermes.
- La prochaine wave serveur locale peut utiliser ce service comme fondation transport-neutral.
- Toute integration HTTP, endpoint, SQL, staging, donnees reelles ou live exige un gate distinct.

QA_SERVER_B_065_WORLD_MAP_OVERLAY_GUARDRAILS = PASS
SERVER_064_BLOCKERS_CLOSED = YES
READY_FOR_NEXT_SERVER_WAVE = YES
HTTP_OR_LIVE_ENDPOINT_VALIDATED = NO
