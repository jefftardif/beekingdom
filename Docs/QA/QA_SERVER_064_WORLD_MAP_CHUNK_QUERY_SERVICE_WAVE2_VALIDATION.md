# QA SERVER-064 - World Map Chunk Query Service Wave 2

Date QA : 2026-07-14  
Role : QA-A / Lead Quality Engineer  
Perimetre : service applicatif local transport-neutral, sans endpoint, SQL, staging, acces distant ou Unity

## Publication

Le chemin contractuel demande `C:\projets\beekingdom\QA\QA_SERVER_064_WORLD_MAP_CHUNK_QUERY_SERVICE_WAVE2_VALIDATION.md` n'est pas inscriptible dans la session QA actuelle. Le present rapport est publie dans le workspace a `C:\projets\beekingdomgame-master\Docs\QA\QA_SERVER_064_WORLD_MAP_CHUNK_QUERY_SERVICE_WAVE2_VALIDATION.md`.

## Verdict

**BLOCKED**

Le service est bien transport-neutral, les trois resultats types sont coherents, le contrat spatial/cache canonique est reutilise, et toutes les executions annoncees passent. Le gate reste toutefois bloque par la composition finale des overlays fournis par le provider.

Le budget et les guardrails sont calcules sur les overlays canoniques avant l'appel du provider. Le service remplace ensuite les overlays sans recalculer le budget. Il ne refuse pas non plus les drapeaux `Live` ou `ServerAuthoritative`. Un provider conforme aux interfaces peut donc produire un resultat `Success` dont le corps depasse le budget annonce ou contredit les non-claims.

## Sources controlees

- `C:\projets\beekingdomgame-master\Server\ops\world-map-chunk-contract\SERVER-064 - World Map Chunk Query Service Wave 2 Report.md`
- `C:\projets\beekingdomgame-master\Server\src\BeeKingdom.Shared\WorldMap\WorldMapChunkQueryService.cs`
- `C:\projets\beekingdomgame-master\Server\src\BeeKingdom.Shared\WorldMap\WorldMapChunkContracts.cs`
- `C:\projets\beekingdomgame-master\Server\src\BeeKingdom.Shared\WorldMap\WorldMapChunkJson.cs`
- `C:\projets\beekingdomgame-master\Server\tests\BeeKingdom.Tests\SharedContractsTests.cs`
- `C:\projets\beekingdomgame-master\Server\ops\world-map-chunk-contract\`

## Constats bloquants

### B1 - Guardrails de payload invalides apres remplacement des overlays

Dans `WorldMapChunkQueryService.cs` :

- ligne 66 : le budget est controle sur `canonical.Guardrails`;
- ligne 71 : les overlays du provider sont obtenus ensuite;
- ligne 91 : `canonical with { Overlays = overlays }` remplace le corps sans remplacer les guardrails.

`EstimatedPayloadBytes` reste donc calcule sur un overlay ruche, une ressource et un vol, meme si le provider retourne davantage d'elements.

Exemple deterministe derive de la formule canonique :

```text
25 chunks + 1 ruche + 1000 ressources + 1 vol
= 2048 + (25 * 512) + 256 + (1000 * 256) + 384
= 271488 octets
Budget = 98304 octets
Guardrail conserve par le service = 15744 octets
```

Avec `PaintedIntoBackground=false` et des vols aeriens, ce resultat n'est pas rejete par le service actuel. Le corps et ses metadonnees se contredisent.

Impact : le provider public peut contourner le seul garde-fou de taille du contrat. Ce defaut doit etre ferme avant de construire un adapter ou une nouvelle wave serveur.

### B2 - Non-claims overlays non imposes par le service

La condition de validation ligne 86 refuse uniquement :

- un overlay peint dans le fond;
- un vol avec `AirOnly=false`;
- un vol avec `RoadGraphUsed=true`.

Elle ne refuse pas :

- `overlays.ServerAuthoritative=true`;
- `overlays.Live=true`;
- une ruche, ressource ou un vol avec `ServerAuthoritative=true` ou `Live=true`.

Un provider peut donc retourner des overlays declares live/authoritatifs tandis que `WorldMapChunkNonClaims.ServerAuthorityActive=false` et les autres non-claims restent inchanges. Le service retournerait `Success` avec des claims internes contradictoires.

Impact : risque de faux claim serveur/live au niveau du contrat local lui-meme.

## Reserves de preuve

### R1 - Isolation multi-world insuffisamment demontree

Le test annonce comme isolation multi-world cree `serviceA` et `serviceB` aux lignes 1925-1926. Chaque service possede son propre provider mono-world. Il prouve la concurrence entre deux graphes d'objets distincts, pas l'isolation de deux mondes servis par une meme instance et un meme provider multi-world.

Le service est stateless et le risque immediat est limite, mais le claim du rapport producteur est plus large que la preuve.

### R2 - Taxonomie d'erreur overlay imprecise

Une violation de separation ou de vol aerien retourne `PayloadBudgetExceeded`. Ce code ne decrit pas la cause. Une erreur dediee de contrat overlay serait plus fiable pour un futur adapter et pour le diagnostic joueur/ops.

### R3 - Cache futur des overlays dynamiques

L'ETag canonique porte le manifeste de fond, pas une revision du provider d'overlays. Le provider deterministe actuel est stable, donc les tests locaux passent. Tout futur provider dynamique devra garantir l'immuabilite sous cet ETag ou fournir une revision incluse dans un validateur futur avant d'utiliser `NotModified`.

## Matrice de validation

| Critere | Resultat | Constat QA |
|---|---|---|
| Interfaces et providers transport-neutral | PASS | Bibliotheque `net8.0` sans ASP.NET, HTTP, SQL, socket ou Unity. |
| Resultats `Success`, `NotModified`, `Rejected` | PASS | Etats et corps/metadonnees exacts confirmes dans la source. |
| Reutilisation du contrat canonique | PASS_WITH_BLOCKER | Construction/hash/ETag uniques, mais guardrails obsoletes apres remplacement des overlays. |
| Fenetre 25 / edge 9 | PASS | Ordre, clipping, origines, dimensions et pagination preserves. |
| Overlays separes et vols aeriens | PASS_WITH_BLOCKER | Provider local conforme; drapeaux live/authority et budget final non imposes. |
| Annulation | PASS | Controle avant providers, apres identite, dans providers locaux et apres overlays. |
| Concurrence et determinisme | PASS | Quatre lectures et bytes/hash/ETag repetables. |
| Isolation multi-world | RESERVE | Deux instances distinctes, pas une instance/provider partage. |
| 8 tests cibles | PASS | 8 reussis, 0 echec, 0 ignore. |
| 67 SharedContractsTests | PASS | 67 reussis, 0 echec, 0 ignore. |
| Suite Release | PASS | 158 reussis, 0 echec, 6 SQL opt-in ignores. |
| Verificateur ops hors ligne | PASS | Sortie 0 avec profils/sources NuGet controles vides; nettoyage confirme. |
| Non-claims d'activation | PASS | Aucun endpoint, binding live, SQL, staging, distant ou Unity. |
| Coherence rapport/source | FAIL | Enforcement final du payload et isolation multi-world surevalues dans le rapport. |

## Neutralite transport

`BeeKingdom.Shared.csproj` ne contient aucune reference de package, projet ou framework externe. Les trois interfaces manipulent uniquement des DTO domaine, `ValueTask` et `CancellationToken`.

La recherche dans `Server/src` ne trouve aucune utilisation du service en dehors de son fichier de declaration. Il n'existe donc :

- aucun endpoint ou controller WorldMap chunk;
- aucune route Minimal API;
- aucun enregistrement DI live;
- aucun code HTTP/304;
- aucun client reseau ou socket;
- aucun acces SQL;
- aucune reference Unity.

## Resultats types

### Success

- `State = Success`;
- `Response` non null;
- ETag, manifest hash et invalidation key recopies depuis le cache de la reponse;
- `Errors` vide.

### NotModified

- `State = NotModified`;
- `Response = null`;
- ETag, manifest hash et invalidation key presents;
- `Errors` vide;
- comparaison ETag exacte et ordinale;
- aucun claim HTTP 304.

### Rejected

- `State = Rejected`;
- `Response = null`;
- ETag, manifest hash et invalidation key nuls;
- erreurs typees presentes dans tous les chemins utilises par le service.

Les rejets world/server utilisent `UnknownWorld`; une seed ou revision incoherente utilise `ManifestRevisionMismatch`; les erreurs canoniques de rayon/bornes sont propagees.

## Contrat spatial et cache

La recherche statique confirme une seule implementation de `BuildChunkWindow`, `ComputeManifestHash`, `ComputeEtag` et `EstimatePayloadBytes`, toutes dans `WorldMapChunkReadinessContract`.

Les tests et le verificateur confirment :

- fenetre complete : 25 chunks;
- premier chunk `(8,-6)`, dernier `(12,-2)`;
- ordre strict Y puis X;
- origines `chunk * 256`;
- dimensions `256x256`;
- edge `0..2` : 9 chunks;
- manifest full : `a6e46a84bc24cb94111c09a1a3ea44aced10323575f6a2dad47497b477b55fa1`;
- ETag full : `W/"9e17913cd519f0a06f111055b78c5aae3e7fc3119ac89d5f82490fed7d6c8151"`;
- manifest edge : `d9038abfee2eb1150d4e04986fa3a8e8cf879811398c04d102ba80c5f85c754a`;
- ETag edge : `W/"3073255be6df2ae53ea1dd68da925c78a8bf705b7cf26e5bec51e330722e9a24"`;
- seed change : manifest et ETag changent, cle artistique stable;
- revision artistique : manifest, ETag et cle d'invalidation changent.

## Executions QA

### Tests cibles

```text
Discovered: 8
Passed: 8
Failed: 0
Skipped: 0
```

Les huit noms correspondent exactement au rapport SERVER-064.

### SharedContractsTests

```text
Passed: 67
Failed: 0
Skipped: 0
```

### Suite Release complete

```text
Discovered: 164
Passed: 158
Failed: 0
Skipped: 6
```

Les six skips sont exactement :

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

Aucun test WorldMap, JSON ou query-service n'est ignore.

Le lancement autorisant un rebuild a compile `BeeKingdom.Shared`, puis a ete arrete par un refus d'ecriture sandbox de Coverlet dans le dossier de test. Les suites ont ensuite ete rejouees avec `--no-build --no-restore` sur les binaires Release livres. Le DLL Shared est posterieur a la source du service et le DLL Tests est posterieur a la source des tests; les huit tests SERVER-064 ont ete decouverts explicitement.

### Verificateur ops

Le premier appel direct a ete bloque par la politique d'execution PowerShell. Le replay final a utilise :

- `powershell.exe -NoProfile -ExecutionPolicy Bypass` limite au processus;
- le `NuGet.Config` du depot avec `<packageSources><clear /></packageSources>`;
- un profil NuGet QA isole contenant lui aussi uniquement `<clear />`;
- aucun feed reseau;
- aucun profil utilisateur reel.

Resultat final :

```text
ExitCode = 0
WORLD_MAP_CHUNK_JSON_CONTRACT_VERIFICATION = PASS
Verifier bin exists after run = false
Verifier obj exists after run = false
QA offline profile residue = false
```

## Integrite et non-claims

Empreintes controlees avant/apres les executions :

- `WorldMapChunkContracts.cs` : `8f04a93af88bbc1cc9c6eef29f9b19dea9769e16a013cd7ddc4bdf848dea8856`;
- `WorldMapChunkJson.cs` : `f8e3d7c5fda18276220e38df5e09bff600d83ce7875853bb900d9bbdf7009b78`;
- `WorldMapChunkQueryService.cs` : `89e86f5e64ae9f388f565d74a9f0928844e458d1f9adbc256922d4e505932863`;
- `SharedContractsTests.cs` : `dd97cf37f59a03347ecab33d736b1f4e8ec0126bc9ff641120cc97818b8c4415`;
- exemples JSON SERVER-063 inchanges.

Les sources et rapports producteurs n'ont pas ete modifies par QA. Aucun secret n'a ete detecte. Les deux variables SQL opt-in sont absentes. Les configurations base et Production restent `Persistence.Provider = InMemory`. Aucun `.trx`, fichier de base, backup ou temporaire QA-064 ne reste dans le paquet ops ou le profil de preuve.

Le fichier normal de mapping Coverlet existe dans le dossier `bin` des tests et n'a pas ete traite comme source produit ni supprime. Le verificateur ops, lui, ne laisse aucun `bin/obj`.

## Corrections requises

Role correctif : **Server-A**, avec revalidation Server-B puis QA-A.

1. Composer les overlays via une API canonique de `WorldMapChunkReadinessContract` qui recalcule `EstimatedPayloadBytes` sur les overlays reels, sans dupliquer la formule dans le service.
2. Rejeter apres composition tout payload superieur a `PayloadBudgetBytes`.
3. Rejeter les drapeaux `Live` et `ServerAuthoritative` au niveau envelope, ruche, ressource et vol pour cette wave locale.
4. Utiliser un code d'erreur dedie pour les violations overlay, distinct de `PayloadBudgetExceeded`.
5. Ajouter des tests provider negatifs : payload trop grand, overlay live/authoritatif, painted overlay, vol non aerien et route terrestre.
6. Verifier les metadonnees exactes des trois resultats dans les tests.
7. Ajouter une preuve concurrente multi-world avec une meme instance de service et un provider partage capable de resoudre au moins deux mondes.
8. Rejouer 8 tests cibles etendus, SharedContracts, suite Release et verificateur hors ligne.

## Decision de gate

Le contrat 063 et le provider deterministe restent valides. Le service Wave 2 ne doit pas devenir la base d'un adapter ou d'un provider extensible tant que ses guardrails et non-claims ne portent pas sur la reponse finale effectivement retournee.

QA_SERVER_064_WORLD_MAP_CHUNK_QUERY_SERVICE_WAVE2 = BLOCKED
READY_FOR_NEXT_SERVER_WAVE = NO
HTTP_OR_LIVE_ENDPOINT_VALIDATED = NO
