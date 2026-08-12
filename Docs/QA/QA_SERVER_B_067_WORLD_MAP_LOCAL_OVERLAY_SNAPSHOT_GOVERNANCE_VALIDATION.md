# QA SERVER-B-067 - World Map Local Overlay Snapshot Governance Validation

Date QA : 2026-07-14  
Role : QA-A  
Mode : validation locale, hors ligne et lecture seule hors present rapport  
Rapport producteur : `Server/ops/world-map-chunk-contract/SERVER-B-067 - World Map Local Overlay Snapshot Revision Governance Wave 4 Report.md`  
Destination : fallback autorise `Docs/QA` ; aucun ecrit n'a ete effectue dans le depot externe `C:/projets/beekingdom/QA`.

## Verdict executif

**PASS**

SERVER-B-067 fournit une gouvernance locale en memoire coherente pour les snapshots d'overlays World Map. Les scopes `(WorldId, GameServerId)` sont isoles, les revisions sont monotones par scope, la publication est atomique, les lectures ne voient pas de contenu mixte et l'historique est borne. Les protections SERVER-B-065/066 restent appliquees par la composition canonique existante.

Aucun endpoint, enregistrement hote, HTTP, SQL, staging, acces distant, configuration production, donnee reelle ou changement Unity n'est introduit. Ce verdict ne valide ni persistence apres redemarrage, ni provider distribue, ni service officiel/live.

## Perimetre et methode

Controles effectues :

- lecture du rapport producteur, du nouveau provider, des tests et de la documentation ops ;
- inspection des chemins publication, lecture, historique, CAS, finalisation et erreurs ;
- verification statique des references sous `Server/src` et de la configuration Production ;
- replay local des tests cibles, des SharedContracts et de la suite Release ;
- replay du verificateur ops avec `NuGet.Config` a sources vides ;
- verification finale des hashes et des residus.

Les tests .NET ont ete executes avec `--no-build --no-restore`. Les binaires Release controles sont posterieurs aux sources de cette wave. Le verificateur ops a utilise ses repertoires locaux temporaires puis les a supprimes.

## Resultats par critere

| Critere | Resultat | Preuve QA |
|---|---|---|
| Snapshots immuables et scopes isoles | PASS | Dictionnaire de scopes exacts, etat et verrou par scope, collections copiees puis exposees en lecture seule. Scope croise refuse et mutation du tableau appelant sans effet. |
| Revisions monotones independantes | PASS | Allocation `latest + 1` sous verrou propre au scope. Tests : A `1,2`, B `1,2`; concurrence finale A `13`, B `6`. Format `overlay-snapshot-` + revision D20. |
| CAS revision et hash | PASS | Revision et hash attendus sont compares a la derniere version visible sous le meme verrou. Un seul writer CAS concurrent gagne; le writer stale est refuse sans consommer de revision. |
| NoChange et retries idempotents | PASS | Le hash semantique canonique est compare avant le CAS : un retry identique, meme stale, retourne `NoChange` avec revision/hash/ETag/bytes inchanges. L'ordre equivalent des listes est canonicalise. |
| Publication atomique et lecteurs coherents | PASS | Le snapshot complet et son historique sont construits avant un unique `Volatile.Write`; les lecteurs utilisent un unique `Volatile.Read`. Le test concurrent partage une seule instance/provider et refuse tout melange hive/resource/flight. |
| Historique borne | PASS | Capacite validee entre `2` et `128`; purge deterministe des plus anciennes entrees. Capacite `2` apres quatre publications : revisions conservees `3,4`. |
| Composition canonique 065/066 | PASS | Le provider appelle `WorldMapChunkReadinessContract.FinalizeReadinessOverlays` pour le candidat semantique et pour la revision finale. Aucune formule parallele de payload/hash/ETag n'est presente. |
| Guardrails non-live/air-only | PASS | Enveloppe ou entite `Live`/`ServerAuthoritative`, overlay peint, `AirOnly=false` et `RoadGraphUsed=true` sont refuses. Le depassement budget reste distinct de `OverlayContractViolation`. |
| Annulation et exception | PASS | Annulation et enumeration fautive avant commit laissent revision et hash precedents intacts. |
| Absence d'activation | PASS | Le nouveau type n'est reference sous `Server/src` que dans son fichier de declaration. Aucun DI, endpoint, controller, hosted service, HTTP, SQL, socket ou Unity dans le produit de cette wave. |

## Replay des tests

### Tests cibles SERVER-B-067

```text
Discovered: 10
Passed:     10
Failed:      0
Skipped:     0
ExitCode:    0
```

Les dix scenarios annonces ont ete rejoues : monotonicite, NoChange canonique, invalidation ETag, conflit CAS, isolation des scopes, concurrence atomique, historique borne, annulation/exception, guardrails/budget/doublons et detachement des collections.

### SharedContracts

```text
Total:   89
Passed:  89
Failed:   0
Skipped:  0
ExitCode: 0
```

### Suite Release complete

```text
Discovered: 186
Passed:     180
Failed:       0
Skipped:      6
ExitCode:     0
```

Les six skips sont exclusivement les scenarios SQL opt-in historiques :

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

Aucun test WorldMap, overlay, query-service ou snapshot-governance n'est ignore.

## Verificateur ops hors ligne

Le `NuGet.Config` du paquet contient uniquement `<clear />`. Le replay termine avec :

```text
WORLD_MAP_CHUNK_JSON_CONTRACT_VERIFICATION = PASS
ExitCode = 0
FullWindowChunks = 25
FullWindowOverlayHash = 3b959d7e6403e3a8d0b9e4815224419805b99e241188ed49baa5f01fddf9ae67
FullWindowETag = W/"ac2b6a99deb6456e95fed31fd40e0417c87bd88eed336644419abc4fbef92d72"
EdgeWindowChunks = 9
EdgeWindowOverlayHash = 4b37971dfde47f8ba1130dd0dadb4eca7cd8709cc89b6c924720b546e84d80f3
EdgeWindowETag = W/"06948970e15cf1d8bdb8246318ded665b7c20b0375770b4ea442a08dfe689aa1"
```

Les exemples restent conformes au gate 066 et n'ont pas ete regeneres par cette wave.

## Integrite et proprete

Empreintes recalculees :

| Fichier | Octets | SHA-256 |
|---|---:|---|
| `LocalWorldMapOverlaySnapshotProvider.cs` | 21525 | `68AF89BBD00AD539D717964C674DE85BC9ECBF42B78A196AC062D33F07985A01` |
| `SharedContractsTests.cs` | 162011 | `73972471ECDCBA8B2278765D9F061447DC3F8564FE64F94AC46D09B7539B1B9D` |
| `README.md` | 9726 | `0B363C903FD43F5E39929F6890196CB9F2CDF0EF54EFF3589BFC31C021E9C1C0` |
| `world-map-chunk-contract-spec.md` | 14551 | `E5A52CF3BF0B408D789DB5E87E8CBDAB880643315F8D82244E922A05359B9337` |

Baselines 065/066 confirmees inchangees :

- `WorldMapChunkContracts.cs` : `46D664CD3CC234D4BB9729098288F4C769ACB1B1974F7CAB17EBA8B4DF44134B` ;
- `WorldMapChunkQueryService.cs` : `EE2966A6648809C3059E0000F7664B5D618C14941C988B3648B5E368BF2D57E9` ;
- verifier `Program.cs` : `6D6B6DF5D1F42A9B0AC4D2DC951E54CDE9A7223052922F594FF54502B119A735` ;
- exemple 25 chunks : `30EE42C3C87C97583656E31C74B66BFD5CCD7F195A9BE9A4BD11C99F653ABF1D` ;
- exemple edge 9 chunks : `A72D36663FA9AD2FE70BE7B1359F19F3D16C1D0FDA6C67E86D564BA925CA5002` ;
- `appsettings.Production.json` : `1A4D10DDB163B9F78B7F5E957A054FE46C0B58116535AC4D8B0233EEB5B4D098`, avec `Persistence.Provider = InMemory`.

Controle final :

```text
Verifier bin/obj et profils temporaires = 0
.trx/.mdf/.ldf/.bak/.db/.sqlite/.tmp sous Server/tests et Server/ops = 0
Variables SQL opt-in actives = 0
Fichier produit modifie par QA = 0
Acces distant ou staging = 0
```

Le workspace ne fournit pas de metadata Git exploitable (`not a git repository`). L'integrite a donc ete controlee par inventaire explicite, references, timestamps et hashes.

## Limites non validees

Ce PASS ne couvre pas et n'autorise pas :

- persistence des snapshots apres redemarrage ;
- coordination multi-processus ou provider distribue ;
- provider officiel ou donnees joueur reelles ;
- endpoint, HTTP/304, cache reseau ou enregistrement DI ;
- SQL, staging, deploiement, service live ou modification Unity.

Ces limites sont hors scope de SERVER-B-067 et ne remettent pas en cause le fournisseur local deterministe valide ici.

## Decision finale

Les exigences de la Wave 4 locale sont satisfaites. Aucun blocker SERVER-B-067 n'est ouvert. Les gates SERVER-B-065 et SERVER-B-066 restent fermes, et aucune activation hote/live n'a eu lieu.

QA_SERVER_B_067_LOCAL_OVERLAY_SNAPSHOT_GOVERNANCE = PASS
SERVER_B_065_066_GUARDRAILS_PRESERVED = YES
NO_HOST_OR_LIVE_ACTIVATION = YES
