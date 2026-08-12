# Builder-B — World Map Wave 3 — Ingest réel du master UI-B et bundle runtime

Date: 2026-07-14  
Version de lot: `uib-wave3-continuous-v1`  
Périmètre: préparation locale hors Unity

## Verdict

Le master UI-B autoritatif a passé le gate d'entrée, puis le pipeline a produit deux bundles complets depuis zéro. Les deux bundles ont été vérifiés, comparés aux 25 tuiles UI-B, soumis aux injections négatives, restaurés, revérifiés et comparés fichier par fichier.

Le bundle local est prêt pour la validation indépendante Builder-C. Il n'est pas intégré à Unity et ne constitue pas un monde immense/live livré.

## Source autoritative

Master lu en lecture seule:

`C:\projets\beekingdom\worldmap_art_wave5\UIB_ContinuousMaster5x5\master_5x5_2560.png`

Contrôles réels:

| Contrôle | Résultat |
|---|---:|
| Format | PNG, PASS |
| Mode | RGB, PASS |
| Dimensions | 2560 x 2560, PASS |
| PIL verify | PASS |
| SHA-256 attendu | `D3CDC2DDE9D56CAC58BE6833790B6FD8FC38AC157F72A01DCEBD8117583A95B4` |
| SHA-256 observé | `D3CDC2DDE9D56CAC58BE6833790B6FD8FC38AC157F72A01DCEBD8117583A95B4` |
| Match | exact |

Hash pixel RGB du master:

`4d6daadc128c16912b8ed222f966d26c93aa300462edd4d2c38e8a97c98c7181`

## Cohérence du lot UI-B

Le préflight a contrôlé en lecture seule:

- schéma `bee-kingdom.world-map.continuous-master-wave3.v1`;
- contrat 5 x 5, tuiles 512 x 512 et limites 512/1024/1536/2048;
- ordre strict `R0C0..R4C4`;
- 25 fichiers présents, RGB 512 x 512;
- 25 hashes conformes au manifeste et tous uniques;
- coordonnées et voisins cardinaux cohérents;
- 25 tuiles pixel-identiques aux crops correspondants du master;
- reconstruction UI-B pixel-identique et hash-identique au master;
- 41 entrées de `hashes_sha256.json`, 0 manque et 0 mismatch;
- `mechanical_validation.json`: PASS, 40 coutures;
- `perceptual_review_uib.json`: verdict signé PASS;
- rapport UI-B présent, hash source et verdict continu PASS présents.

Résultats pixel UI-B:

- différences cumulées entre les 25 tuiles et le master: `0`;
- différences reconstruction mémoire: `0`;
- différences `reconstruction_5x5_from_tiles.png`: `0`.

## Générations depuis zéro

Dossiers de sortie:

- `C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging\run1`
- `C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging\run2`

Commande d'orchestration exécutée:

```powershell
python real_ingest.py `
  --ui-dir "C:\projets\beekingdom\worldmap_art_wave5\UIB_ContinuousMaster5x5" `
  --staging-root "C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging" `
  --expected-hash "D3CDC2DDE9D56CAC58BE6833790B6FD8FC38AC157F72A01DCEBD8117583A95B4" `
  --version "uib-wave3-continuous-v1"
```

Chaque run contient:

- 25 PNG canoniques 512 x 512;
- 1 reconstruction canonique;
- 1 manifeste canonique;
- 25 PNG runtime 516 x 516;
- 1 manifeste runtime;
- 1 validation JSON;
- total: 54 fichiers, 35 164 229 octets.

## Vérification canonique réelle

Résultats run1 et run2:

| Contrôle | run1 | run2 |
|---|---:|---:|
| Tuiles canoniques | 25 | 25 |
| Dimensions | 512 x 512 | 512 x 512 |
| Altérations de crop | 0 pixel | 0 pixel |
| Reconstruction assemblée | 0 pixel différent | 0 pixel différent |
| Reconstruction enregistrée | 0 pixel différent | 0 pixel différent |
| Hash pixel reconstruction | match master | match master |

Hash PNG de la reconstruction générée:

`20e027ecb09c1041091eabc8f1673b48fb25a588e88bdb5fcff96404239784f5`

Hash pixel de la reconstruction générée:

`4d6daadc128c16912b8ed222f966d26c93aa300462edd4d2c38e8a97c98c7181`

### Comparaison aux tuiles UI-B

- tuiles générées comparées: 25/25 par run;
- différences pixel cumulées: `0` pour run1, `0` pour run2;
- les 6 553 600 pixels sont donc identiques à la composition UI-B autoritative;
- hashes PNG fichier identiques: 0/25.

Le dernier point n'est pas un échec: UI-B et Builder-B utilisent des paramètres de compression PNG différents. Le contenu lossless décodé est strictement identique. Les manifests Builder-B documentent les hashes de leurs propres PNG ainsi que les hashes pixels, sans réutiliser artificiellement les hashes fichier UI-B.

## Vérification runtime réelle

Résultats run1 et run2:

| Contrôle | run1 | run2 |
|---|---:|---:|
| Tuiles runtime | 25 | 25 |
| Dimensions | 516 x 516 | 516 x 516 |
| Intérieur 512 x 512 différent de UI-B | 0 pixel | 0 pixel |
| Gouttières différentes du master attendu | 0 pixel | 0 pixel |
| Frontières internes contrôlées | 40 | 40 |
| Frontières internes PASS | 40 | 40 |
| UV exactes `2/516..514/516` | 25/25 | 25/25 |
| Côtés internes vrais voisins | 80/80 | 80/80 |
| Clamp interne invalide | 0 | 0 |
| Côtés externes clampés | 20/20 | 20/20 |

Contrat du manifeste runtime:

- `pixels_each_side: 2`;
- `runtime_width/runtime_height: 516`;
- `source_for_internal_sides: true_adjacent_master_pixels`;
- `outer_edge_policy: clamp_master_edge_only`;
- `stretching: false`;
- hash source master exact sur chaque tuile;
- crop canonique, origine macro, fenêtre source, clamp, provenance, UV et hashes vérifiés.

## Verify explicite

Les commandes CLI ont été relancées indépendamment après la restauration des injections:

```powershell
python macro_slicer.py verify --input "...\master_5x5_2560.png" --bundle "...\run1"
python macro_slicer.py verify --input "...\master_5x5_2560.png" --bundle "...\run2"
```

Les deux commandes ont retourné code 0 avec:

- `WORLD_MAP_MACRO_SLICER_WAVE3 = PASS`;
- `CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL = YES`;
- `RUNTIME_GUTTERS_FROM_TRUE_NEIGHBORS = YES`;
- `READY_FOR_UIB_WAVE3_MASTER_INGEST = YES`.

Les validations explicites sont archivées dans:

- `artifacts/WorldMapWave3_RuntimeBundle_staging/verify_run1.json`;
- `artifacts/WorldMapWave3_RuntimeBundle_staging/verify_run2.json`.

Leur SHA-256 commun est:

`F922D70A5BDBA8C987DE8A71033E59AAEF8CEC7E8D3A4E1D659513B8E2C7B253`

## Injections négatives et restauration

Toutes les injections ont été effectuées uniquement dans run2. Les octets originaux ont été conservés en mémoire, restaurés immédiatement, puis le bundle a été revérifié.

| Injection | Rejet observé | Après restauration |
|---|---|---:|
| Suppression `R0C0.png` | `MISSING_CANONICAL_TILE` | PASS |
| Copie de `R0C0` sur `R0C1` | `DUPLICATE_CANONICAL_TILE`, `CANONICAL_PIXEL_ALTERATION` | PASS |
| Hash manifeste remplacé par zéro | `CANONICAL_HASH_MISMATCH` | PASS |
| Inversion des deux premières entrées | `CANONICAL_ORDER_MISMATCH` | PASS |
| `stretching` forcé à true | `RUNTIME_GUTTER_CONTRACT_MISMATCH` | PASS |
| Pixel de gouttière et pixel intérieur altérés | `RUNTIME_HASH_MISMATCH`, `RUNTIME_PIXEL_ALTERATION`, `INTERNAL_GUTTER_BOUNDARY_FAILURE` | PASS |

Verify final après toutes les restaurations: PASS sur run1 et run2.

## Déterminisme run1/run2

Comparaison de tous les fichiers relatifs après restauration:

- fichiers run1: 54;
- fichiers run2: 54;
- manquants: 0;
- extras: 0;
- hashes différents: 0;
- identité byte/hash: YES.

Digest d'arbre run1:

`2176c7c5b81108e006014a1310095c9570d414963539bc0766dd4c023456fd2f`

Digest d'arbre run2:

`2176c7c5b81108e006014a1310095c9570d414963539bc0766dd4c023456fd2f`

## Résumé machine-readable

Fichier:

`C:\projets\beekingdomgame-master\artifacts\WorldMapWave3_RuntimeBundle_staging\real_ingest_summary.json`

SHA-256:

`DE20D28007D002A25634D5D15165DC05692E5DBCDED00D3230D04B37DFEBBE18`

Il contient le hash source, les résultats UI-B, les comptes canoniques/runtime, les 40 frontières, les UV/clamps, les six injections, les digests d'arbre et les verdicts.

## Fichiers de pipeline

Ajout pour cet ingest:

- `C:\projets\beekingdomgame-master\tools\world-map-macro-slicer\real_ingest.py`;
- mise à jour de `README.md` avec la commande reproductible.

Le slicer synthétique validé précédemment reste inchangé dans son comportement de production.

## Périmètre et non-claims

- Le master, les 25 tuiles, manifests, preuves et rapport UI-B ont été lus uniquement.
- Aucun fichier UI-B n'a été modifié.
- Aucun fichier Unity, `Assets`, scène, serveur ou réglage projet n'a été lu pour intégration ni modifié.
- Le bundle est local et prêt pour validation Builder-C.
- L'intégration Unity n'est pas faite.
- Le monde immense/live n'est pas livré.
- Aucun gameplay, économie, serveur ou déplacement officiel n'est revendiqué.

## Placement du rapport

Le chemin cible externe n'est pas inscriptible dans l'environnement actuel. Le fallback explicitement autorisé est utilisé:

`C:\projets\beekingdomgame-master\tools\world-map-macro-slicer\BuilderB_WorldMapMacroMasterSlicerWave3_RealIngest_Report.md`

## Verdicts finaux

`WORLD_MAP_WAVE3_REAL_MASTER_INGEST = PASS`

`REAL_MASTER_HASH_MATCH = YES`

`REAL_CANONICAL_RECONSTRUCTION_PIXEL_IDENTICAL = YES`

`REAL_RUNTIME_GUTTERS_40_OF_40 = PASS`

`REAL_RUN1_RUN2_BYTE_IDENTICAL = YES`

`READY_FOR_BUILDERC_RUNTIME_GUTTER_VALIDATION = YES`
