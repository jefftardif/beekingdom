# QA SERVER-B-066 - World Map Overlay Revision Cache Validator

Date QA : 2026-07-14  
Role : QA-A / Lead Quality Engineer  
Perimetre : validation locale, transport-neutral et non-live du contrat de revision/hash overlay

## Publication

Le chemin principal `C:\projets\beekingdom\QA\QA_SERVER_B_066_WORLD_MAP_OVERLAY_REVISION_CACHE_VALIDATION.md` n'est pas inscriptible dans la session QA. Le present rapport utilise le fallback autorise :

`C:\projets\beekingdomgame-master\Docs\QA\QA_SERVER_B_066_WORLD_MAP_OVERLAY_REVISION_CACHE_VALIDATION.md`

## Verdict

**PASS**

SERVER-B-066 ferme le gate futur laisse par SERVER-B-065 pour le provider local deterministe. La reponse finale porte une revision overlay obligatoire et un hash canonique recalcule, puis l'ETag combine identite monde/serveur, fenetre, manifeste de fond, revision et contenu overlay.

Les invalidations dynamiques locales, la collision de revision, la canonicalisation des listes et `NotModified` exact sont confirmees. Les guardrails SERVER-B-065 restent appliques avant tout `Success` ou `NotModified`.

Ce PASS valide uniquement un contrat applicatif local preparatoire. Il ne valide ni endpoint, ni HTTP/304, ni provider officiel, ni SQL, ni staging, ni acces distant, ni cache live, ni donnee reelle, ni integration Unity.

## Sources controlees

- `C:\projets\beekingdom\prompt_server\rapports\SERVER-B-066 - World Map Overlay Revision Cache Validator Wave 3 Report.md`
- `Server/ops/world-map-chunk-contract/SERVER-B-066 - World Map Overlay Revision Cache Validator Wave 3 Report.md`
- `Docs/QA/QA_SERVER_B_065_WORLD_MAP_OVERLAY_GUARDRAILS_VALIDATION.md`
- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkContracts.cs`
- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkQueryService.cs`
- `Server/src/BeeKingdom.Shared/WorldMap/WorldMapChunkJson.cs`
- `Server/tests/BeeKingdom.Tests/SharedContractsTests.cs`
- `Server/ops/world-map-chunk-contract/`
- `Server/src/BeeKingdom.Server/appsettings.Production.json`

Les deux copies du rapport producteur SERVER-B-066 sont byte-identiques : `16991` octets, SHA-256 `27ee2ace45edcc72ee6090daadbf631eaedd4fab88ec059220cf38d9689a0204`.

## Matrice de decision

| Critere | Resultat | Constat QA |
|---|---|---|
| Revision obligatoire | PASS | `[JsonRequired]` est applique a `overlayRevision`; l'omission echoue a la deserialisation. |
| Revision non vide | PASS | `IsNullOrWhiteSpace` refuse null, vide ou whitespace avec `OverlayContractViolation`. |
| Revision deterministe locale | PASS | Le provider local et les lectures repetees conservent la meme revision, les memes bytes, le meme hash et le meme ETag. |
| Hash canonique final | PASS | Le provider hash est efface, l'enveloppe canonique wire est hashee en SHA-256, puis le hash calcule est ecrit. |
| Pas d'auto-inclusion | PASS | Le wire de hash contient explicitement `overlayHash:""`. |
| Ordre semantique equivalent | PASS | Tris ordinaux et tie-breakers complets; permutations de listes donnent bytes/hash/ETag identiques. |
| ETag combine | PASS | Monde, serveur, centre X/Y, rayon, manifest, revision et overlay hash sont tous inclus. |
| NotModified exact | PASS | Comparaison ordinale apres finalisation; reponse nulle et metadonnees exactes. |
| Invalidation dynamique locale | PASS | Vol, ressource, ruche et revision changent le validateur avec fond inchange. |
| Collision de revision | PASS | Meme revision et contenu different produisent hash et ETag differents. |
| Guardrails SERVER-B-065 | PASS | Budget final, non-live, non-authority, separation, air-only, no-road et isolation sont preserves. |
| Exemples 25/9 | PASS | JSON reels, hashes overlay, ETags, delta tokens et SHA fichiers recalcules. |
| Tests et verifier | PASS | `20/20`, `79/79`, `170/0/6`, verifier offline code `0`. |
| Perimetre local uniquement | PASS | Aucun endpoint, HTTP, SQL, staging, remote, secret, donnee reelle, Unity ou changement production. |

## Revision overlay obligatoire

`WorldMapChunkOverlayEnvelope` expose :

```text
[JsonRequired] string OverlayRevision
[JsonRequired] string OverlayHash
```

Preuves :

- la revision locale canonique est `overlay-readiness-001`;
- `WorldMapChunkOverlayEnvelope.Empty` porte `overlay-empty-readiness-001` et un hash canonique de 64 caracteres;
- le hash QA independant de l'enveloppe vide vaut `91dc39da9345848e64335d7ba500ca92a38e415e2f7e469b13b0ea993ec0577e`, identique a la constante source;
- le scenario service avec revision whitespace retourne `Rejected / OverlayContractViolation` et aucune metadonnee de cache;
- un paquet JSON temporaire sans `overlayRevision` retourne le diagnostic requis et un code `1` :

```text
JSON deserialization ... was missing required properties ... overlayRevision
MISSING_REVISION_REJECTED = True
```

Le paquet negatif et ses artefacts de build ont ete supprimes apres le controle.

La deterministicite validee ici est celle du provider local et des snapshots explicites. Aucun allocateur officiel ou stockage de revision n'est cree par cette wave.

## Hash overlay canonique

La frontiere unique `FinalizeReadinessOverlays` applique :

1. canonicalisation des trois listes;
2. conservation de `OverlayRevision`;
3. remplacement de toute valeur provider par `OverlayHash = ""`;
4. serialisation de l'enveloppe reelle avec `WorldMapChunkJson.CreateOptions()`;
5. SHA-256 des bytes UTF-8;
6. ecriture du digest lowercase dans l'enveloppe finale.

Le hash couvre les drapeaux enveloppe, toutes les ruches, ressources et vols, ainsi que la revision. Il ne se couvre pas lui-meme.

Le test de collision fournit volontairement `provider-stale-hash`, conserve la meme revision, change seulement `Hive.PowerBand`, puis confirme :

- hash provider ignore;
- nouveau hash canonique;
- nouvel ETag;
- `Success` face a l'ancien ETag.

## Canonicalisation des listes

Tous les tris de chaines utilisent `StringComparer.Ordinal`. Les tie-breakers couvrent tous les champs wire :

- ruche : ID, position X/Y, power band, authority, live;
- ressource : ID, position X/Y, kind, richness, authority, live;
- vol : ID, origine X/Y, destination X/Y, kind, state, air-only, road graph, authority, live.

Deux enveloppes avec les memes elements en ordre ascendant/descendant et deux faux hashes provider differents produisent :

- le meme ordre final;
- le meme `OverlayHash`;
- le meme ETag;
- les memes bytes de reponse.

Si tous les tie-breakers sont identiques, les elements sont wire-identiques; leur permutation ne change donc pas les bytes.

## ETag combine et NotModified

L'entree exacte est :

```text
worldId|gameServerId|centerChunkX|centerChunkY|radius|manifestHash|overlayRevision|overlayHash
```

Le digest est expose au format local faible `W/"<64 hex lowercase>"`. Le `DeltaToken` est regenere depuis cet ETag.

Ordre de service confirme :

1. resolution monde/serveur;
2. construction du fond canonique;
3. resolution provider overlay;
4. finalisation/hash/budget/guardrails;
5. rejet des erreurs;
6. comparaison ordinale `IfNoneMatch`;
7. `NotModified` ou `Success`.

Sans changement, deux lectures produisent des bytes/hash/ETag identiques. Un `IfNoneMatch` exact retourne :

- `State = NotModified`;
- `Response = null`;
- ETag, manifest hash et invalidation key exacts;
- `Errors` vide.

Aucun code HTTP 304 ou header HTTP n'est implemente.

## Invalidations dynamiques locales

| Variation avec meme fond | Revision | Hash overlay | Ancien ETag | Resultat |
|---|---|---|---|---|
| Mouvement destination vol | changee | change | invalide | `Success` |
| Respawn / nouvel ID ressource | changee | change | invalide | `Success` |
| Evolution `PowerBand` ruche | changee | change | invalide | `Success` |
| Revision seule, contenu stable | changee | change | invalide | formule recalculee conforme |
| Collision : contenu change, revision identique | identique | change | invalide | `Success` |

Recalcul QA revision seule sur l'exemple full :

```text
Revision baseline = overlay-readiness-001
OverlayHash        = 3b959d7e6403e3a8d0b9e4815224419805b99e241188ed49baa5f01fddf9ae67
ETag               = W/"ac2b6a99deb6456e95fed31fd40e0417c87bd88eed336644419abc4fbef92d72"

Revision nouvelle  = overlay-readiness-002
OverlayHash        = 8774180a5c669130db6633a5f8a589b73b292362399b658c5c7258eaeaa9429e
ETag               = W/"173097b68a2bff7cb176ebbc2abd1a3197a6223029314c82640e261da0cd23e6"
```

Le recalcul QA confirme aussi qu'un changement de `WorldId` modifie l'ETag avec tous les autres champs conserves.

## Guardrails SERVER-B-065 preserves

La composition/hash overlay reste dans la meme finalisation que le budget et les violations de contrat. Le service traite les erreurs avant la comparaison ETag.

| Provider final | Estimation | Budget | Resultat |
|---|---:|---:|---|
| 25 chunks, 2 ruches, 3 ressources, 2 vols | 16896 | 98304 | `Success` |
| 25 chunks, 1 ruche, 1000 ressources, 1 vol | 271488 | 98304 | `Rejected / PayloadBudgetExceeded` |

`OverlayContractViolation = 8` reste distinct, sans renumeroter les valeurs `0..7`. Sont toujours refuses :

- revision vide/whitespace;
- overlay peint dans le fond;
- enveloppe, ruche, ressource ou vol live/authoritatif;
- vol `AirOnly=false`;
- vol `RoadGraphUsed=true`.

Le test concurrent conserve une seule instance de service, un identity provider a deux scopes et un overlay provider partage. Quatre lectures concurrentes restent isolees; un couple world/server croise est refuse avec corps et metadonnees nuls.

## Exemples JSON reels

| Preuve | Full | Edge |
|---|---|---|
| Chunks | 25 | 9 |
| Manifest fond | `a6e46a84bc24cb94111c09a1a3ea44aced10323575f6a2dad47497b477b55fa1` | `d9038abfee2eb1150d4e04986fa3a8e8cf879811398c04d102ba80c5f85c754a` |
| Revision overlay | `overlay-readiness-001` | `overlay-readiness-001` |
| Hash overlay | `3b959d7e6403e3a8d0b9e4815224419805b99e241188ed49baa5f01fddf9ae67` | `4b37971dfde47f8ba1130dd0dadb4eca7cd8709cc89b6c924720b546e84d80f3` |
| ETag combine | `W/"ac2b6a99deb6456e95fed31fd40e0417c87bd88eed336644419abc4fbef92d72"` | `W/"06948970e15cf1d8bdb8246318ded665b7c20b0375770b4ea442a08dfe689aa1"` |
| Delta token | suit exactement l'ETag | suit exactement l'ETag |
| Estimation | 15744 | 7552 |
| SHA-256 fichier | `30ee42c3c87c97583656e31c74b66bfd5ccd7f195a9be9a4bd11c99f653abf1d` | `a72d36663fa9ad2fe70be7b1359f19f3d16c1d0fda6c67e86d564ba925ca5002` |
| Taille | 11699 octets | 6431 octets |

QA a recalcule les hashes overlay depuis les objets wire avec `overlayHash=""`, puis les ETags depuis la formule combinee. Les quatre valeurs correspondent exactement aux JSON et au verificateur C#.

## Executions QA

### Matrice query service

```text
Discovered: 20
Passed:     20
Failed:      0
Skipped:     0
```

### SharedContractsTests

```text
Discovered: 79
Passed:     79
Failed:      0
Skipped:     0
```

### Suite Release complete

```text
Discovered: 176
Passed:     170
Failed:       0
Skipped:      6
```

Les six skips sont uniquement les scenarios SQL opt-in historiques :

1. `SqlServerBackupCanBeVerifiedAndRestoredToDisposableDatabase`
2. `SqlServerCreatesDisposableDatabaseAndAppliesMigrationsIdempotently`
3. `SqlServerRepositoryRoundTripsSyntheticAccountProgression`
4. `SqlServerSerializesMigrationsAndRejectsConcurrentDuplicateAccount`
5. `SqlServerStoresSyntheticCredentialSessionAndWorldScopedColonies`
6. `WorldSchemaReadinessDraftExecutesAndRollsBackLocally`

Aucun test WorldMap, JSON, overlay ou query-service n'est ignore.

### Verificateur ops hors ligne

Le replay decisif a utilise le `NuGet.Config` du paquet et un profil QA temporaire, tous deux avec zero source.

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

## Integrite et perimetre

Empreintes principales controlees avant les executions :

- `WorldMapChunkContracts.cs` : `46d664cd3cc234d4bb9729098288f4c769acb1b1974f7cab17eba8b4df44134b`;
- `WorldMapChunkQueryService.cs` : `ee2966a6648809c3059e0000f7664b5d618c14941c988b3648b5e368bf2d57e9`;
- `SharedContractsTests.cs` : `48f5dc33f91ca3ffe72cc19a32d84c7ac3b4aa1636c6f32464b22ea830d3aa97`;
- `Program.cs` : `6d6b6df5d1f42a9b0ac4d2dc951e54cde9a7223052922f594ff54502b119a735`.

Le query service est inchange depuis SERVER-B-065 et finalise deja avant `IfNoneMatch`. La recherche dans `Server/src` ne trouve le service ou ses providers que dans leur fichier de declaration : aucun enregistrement DI, controller, route, adapter HTTP ou provider officiel.

Controles finaux :

- scan produit/test/ops : aucun secret, URL, client HTTP, client SQL, socket ou dependency Unity; seul match Unity = assertion negative `Does.Not.Contain("UnityEngine")`;
- variables SQL opt-in absentes;
- `appsettings.Production.json` date du 2026-07-11, avant cette wave, SHA-256 `1a4d10ddb163b9f78b7f5e957a054fe46c0b58116535ac4d8b0233eeb5b4d098`;
- Production conserve `Persistence.Provider = InMemory`;
- aucun `.trx`, `.mdf`, `.ldf`, `.bak`, `.db`, `.sqlite` ou `.tmp` sous tests/ops;
- aucun `bin/obj` du verificateur;
- aucun dossier temporaire QA-066;
- aucun fichier produit, rapport producteur, configuration production ou fichier Unity modifie par QA.

## Limites futures non bloquantes

SERVER-B-066 ne fournit pas :

- allocation ou gouvernance officielle des revisions;
- stockage, transaction ou concurrence persistante du provider;
- endpoint, header ETag, cache HTTP ou statut 304;
- polling, push ou synchronisation temps reel;
- overlays officiels autoritatifs ou donnees joueur;
- SQL, staging, deploiement ou migration production.

Avant un provider officiel, une wave distincte devra definir la revision par scope/snapshot, ses garanties de concurrence et ses limites d'entree. Le hash canonique protege ici contre une reutilisation accidentelle de revision, mais ne remplace pas cette gouvernance.

## Decision de gate

- Le gate d'invalidation overlay dynamique local est ferme.
- Les guardrails SERVER-B-065 restent fermes.
- La prochaine wave serveur locale peut s'appuyer sur ce contrat.
- Aucune activation live ou transport HTTP n'est autorisee par ce verdict.

QA_SERVER_B_066_WORLD_MAP_OVERLAY_REVISION_CACHE = PASS
DYNAMIC_OVERLAY_ETAG_INVALIDATION = PASS
SERVER_B_065_GUARDRAILS_PRESERVED = YES
LOCAL_CONTRACT_ONLY_NO_LIVE_ACTIVATION = YES
READY_FOR_NEXT_SERVER_WAVE = YES
