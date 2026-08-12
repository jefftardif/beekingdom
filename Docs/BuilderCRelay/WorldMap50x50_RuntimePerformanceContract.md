# WorldMap 50x50 - Runtime Performance Contract

Date locale: 2026-07-15

## Portee Builder-C-Relay

Mission: architecture performance et documentation uniquement.

Contraintes respectees:

- Aucun fichier Unity, scene, PNG, APK, master terrain, BearDen, serveur ou donnee reelle ne doit etre modifie par ce contrat.
- La carte visible actuelle reste Wave5 25x25.
- Le 50x50 est un catalogue logique/stress runtime, pas un terrain fabrique.
- Les entites runtime restent decouplees du terrain: elles ne sont pas peintes dans les tuiles.
- Le client Unity affiche, previsualise et stresse; il ne devient pas autoritaire.

Sources locales lues:

- `Docs/Recovery/BeeKingdom_Relay_Progress.md`
- `Docs/WorldMapRuntimeEntitiesWave1/ProductionIntegrationContract.md`
- Code runtime cible, par tranches courtes:
  - `Assets/BeeKingdom/Playground/WorldMapWave5StreamingTileProvider.cs`
  - `Assets/BeeKingdom/Playground/WorldMapMmoFullscreenFoundationBootstrap.cs`
  - `Assets/BeeKingdom/Playground/WorldMapLocalLabRuntime.cs`
  - `Assets/BeeKingdom/Playground/Editor/WorldMap50x50ReadinessProofHarness.cs`
- Rapports de preuve locaux:
  - `Docs/WorldMapRuntimeEntitiesWave1/WorldMap50x50Readiness_Report.md`
  - `Docs/BuilderA/WorldMapRuntimeEntitiesWave1/WorldMap50x50ReadinessProof/WorldMap50x50ReadinessProofReceipt.md`

## Etat runtime observe

Terrain Wave5 25x25:

- Grille artistique: 25 lignes x 25 colonnes = 625 tuiles.
- Origine artistique: chunk C20_20.
- Dernier chunk artistique: C44_44.
- Centre artistique: C32_32.
- Taille tuile monde: 512 unites.
- Taille PNG runtime avec gouttiere: 516 px.
- Gouttiere: 2 px.
- Cache textures terrain: capacite 96.
- Prefetch terrain: anneau 1.
- Chargement visible: synchrone pour le coeur visible, async limitee pour le prefetch.
- Eviction: LRU hors prefetch/desirs courants.

Monde logique actuel:

- Grille logique runtime: 64 x 64 chunks.
- Taille chunk: 512 unites monde.
- Fenetre active entites: rayon 2 chunks, soit jusqu'a 5x5 = 25 chunks.
- Frontiere active: recomputation seulement quand le centre sort du voisinage actif courant.
- Cache chunk entites: `Dictionary<Vector2Int, WorldChunkData>`.
- Listes actives: ruches, ressources, bestiaire reconstruites depuis les chunks actifs.

Stress 50x50 observe:

- Catalogue logique: 50 x 50 = 2500 coordonnees.
- Mode stress: desactive par defaut.
- Terrain 50x50: non genere.
- Chunk cache runtime avant/apres stress: stable, 25/25 dans la preuve locale.
- Allocations mesurees par la preuve: 0 byte.
- Resultat P1: PASS.

## Budgets 25x25 cibles

Ces budgets concernent la carte Wave5 visible et les entites runtime autour du joueur.

| Surface | Cible | Plafond dur | Notes |
| --- | ---: | ---: | --- |
| Tuiles terrain catalogue | 625 | 625 | Aucune tuile ajoutee en P1/P2 sans nouveau manifeste. |
| Tuiles terrain visibles coeur | viewport-dependent | <= 25 | Le coeur visible doit rester dans une fenetre bornee. |
| Tuiles prefetch | coeur + ring 1 | <= 96 cache | Le cache terrain reste la limite dure. |
| Chunks entites actifs | 9 a 25 | 25 | Rayon 2, bords NW/SE acceptes a 9. |
| Ruches actives | cible <= 16 | <= 25 | Inclut seeds demo et hives seedes. |
| Ressources actives | cible <= 50 | <= 75 | Densite actuelle 1 ou 2 par chunk, plus seeds core. |
| Menaces actives | cible <= 16 | <= 25 | Bestiaire local/demo seulement. |
| Cache sprites entites | familles utilisees | <= assets Wave1 utiles | Charger par chemin stable, pas par instanciation volatile. |
| Allocations steady-state/frame | 0 B apres warmup | <= 1 KB/frame temporaire | Toute allocation repetee doit etre traquee. |
| Spike pan/zoom | invisible UX | <= 4 ms CPU simulation | Hors cout rendu Unity/GUI legacy. |

Regles 25x25:

- Le terrain Wave5 doit rester fail-closed: manifeste invalide, hash invalide ou tuile manquante = pas de fallback trompeur.
- Les entites doivent s'activer par chunk logique, pas par scan de pixels.
- Les ressources epuisees/respawn restent locales en demo; aucun gain officiel.
- Les coordonnees ecran ne doivent jamais etre sauvegardees comme source de verite.

## Budgets 50x50 cibles

Ces budgets concernent le catalogue logique 50x50 futur et son stress synthetique.

| Surface | Cible | Plafond dur | Statut actuel |
| --- | ---: | ---: | --- |
| Coordonnees catalogue | 2500 | 2500 | PASS observe. |
| Fenetre active | 5x5 | 25 chunks | PASS observe. |
| Fenetre bord NW/SE | 3x3 | 9 chunks min valides | PASS observe. |
| Densest active chunks | 25 | 25 | PASS observe. |
| Densest ruches | 14 observe | <= 25 | PASS observe. |
| Densest ressources | 40 observe | <= 75 | PASS observe. |
| Densest bestiaire | 14 observe | <= 25 | PASS observe. |
| Catalogue ruches | 725 observe | non toutes actives | PASS observe. |
| Catalogue ressources | 3740 observe | non toutes actives | PASS observe. |
| Catalogue bestiaire | 699 observe | non toutes actives | PASS observe. |
| Texture cache Wave5 | 15 observe | <= 96 | PASS observe. |
| Allocations stress | 0 observe | <= 2,000,000 B | PASS observe. |

Regles 50x50:

- Le 50x50 ne charge pas 2500 chunks en scene.
- Le 50x50 ne cree pas de terrain, PNG ou atlas temporaire.
- Le stress parcourt des coordonnees et compte les densites par fonction seedee; il ne doit pas remplir `chunkCache`.
- Le stress doit valider centre, coin nord-ouest, coin sud-est et fenetre la plus dense.
- Toute future extension doit conserver la fenetre active <= 25 tant que les budgets mobiles ne sont pas revises.

## Catalogue et fenetre active

Modele catalogue:

- `catalog_coord`: coordonnee logique discrete `(x, y)` dans une grille versionnee.
- `chunk_id`: `Cxx_yy`, derive de la coordonnee logique.
- `sector_id`: `Sxx_yy`, groupe de 4x4 chunks observe dans le runtime.
- `world_coord`: position continue en unites monde, derivee de `chunk + local_0_1`.
- `world_coord_normalized`: position 0..1 dans la zone jouable versionnee.

Fenetre active:

- Centre: chunk courant derive de `world_coord / chunk_size`.
- Rayon: 2 chunks.
- Activation: tous les chunks dans `[center - 2, center + 2]`, clamps aux bords.
- Deactivation: toute coordonnee hors rayon 2.
- Minimum attendu aux coins: 9 chunks.
- Maximum attendu hors coins: 25 chunks.

Contrat cache:

- `chunkCache` peut memoriser les chunks effectivement visites en runtime demo.
- Le stress 50x50 ne doit pas muter `chunkCache`.
- Le cache terrain Wave5 reste separe du cache entites.
- Le cache sprites runtime doit etre indexe par chemin logique d'asset, pas par coordonnee.

## Pooling ressources, menaces et ruches

Contrat de pooling cible:

- Ruches visibles: pool par `visual_tier + hive_class + faction_overlay`.
- Ressources visibles: pool par `resource_kind + richness_tier`.
- Menaces visibles: pool par `bestiary_tier + variant`.
- Labels/badges: pool UI separe, activation seulement pour entites interactives ou proches.
- Flights/trajectoires: pool de lignes/arcs, jamais allocation par frame pendant un vol.

Plafonds de pool recommandes pour mobile:

- Hives: 25 objets actifs, 8 labels haute priorite.
- Resources: 75 objets actifs, 12 labels haute priorite.
- Bestiary: 25 objets actifs, 8 labels haute priorite.
- Flight arcs: 8 actifs, 16 reserves.
- Feedback flottant: 12 actifs, 24 reserves.

Regles:

- Aucun `Instantiate`/`Destroy` dans la boucle de pan/zoom apres warmup.
- Les sprites doivent etre charges une fois par famille visible et reutilises.
- Les entites hors fenetre active doivent etre desactivees ou recyclees, pas detruites.
- Les donnees officielles futures remplacent les seeds; les objets affiches restent pools.

## LOD interaction

LOD0 - interactif:

- Entite dans fenetre active et a portee de selection.
- Hitbox active.
- Label court si selectionnee, proche ou prioritaire.
- Quantite/etat visible pour ressource selectionnee ou proche.

LOD1 - lisible:

- Entite dans fenetre active mais non prioritaire.
- Sprite ou symbole visible.
- Pas de label permanent si densite elevee.
- Hitbox simplifiee.

LOD2 - resume:

- Fenetre active dense ou zoom faible.
- Agregat par chunk: compte ruches/ressources/menaces.
- Pas de hitbox individuelle sauf selection verrouillee.

LOD3 - hors fenetre:

- Aucune entite instanciee.
- Donnees accessibles seulement par catalogue/index si necessaire.

Seuils cibles:

- Si entites actives totales > 80: basculer labels non selectionnes en LOD1.
- Si entites actives totales > 110: basculer ressources pauvres non proches en symbole compact.
- Si zoom < 0.75: preferer LOD2 chunk summary pour entites non selectionnees.
- Si une entite est selectionnee: maintenir LOD0 meme pendant pan/zoom, jusqu'a sortie de fenetre active.

## Allocations et frame budget

Objectif steady-state:

- Apres warmup, pan/zoom sans changement de fenetre: 0 B/frame.
- Changement de fenetre: allocations amorties ou reutilisees; cible <= 32 KB au moment du switch.
- Stress synthetique: <= 2,000,000 B par run; objectif courant 0 B conserve.
- Pas de LINQ, closures, `new List`, `new Dictionary`, string formatting massif ou allocations GUI additionnelles dans les boucles chaudes futures.

Points a surveiller:

- Le runtime actuel utilise IMGUI (`OnGUI`), acceptable en labo mais couteux en production.
- Les libelles `CoordLabel`, `ChunkId`, `SectorId` creent des strings; production doit memoiser ou limiter aux surfaces HUD.
- Les listes `activeChunks`, `hives`, `resources`, `bestiary` sont reutilisees, ce qui est correct; conserver ce modele.
- Les sets/listes scratch du provider terrain existent deja; conserver les buffers.

## Coordonnees codees en dur relevees

Les constantes et positions suivantes sont acceptables en demo, mais doivent etre normalisees avant migration production.

Constantes terrain/logique:

- `OfficialMapPath = C:/projets/beekingdom/carte.png`: chemin local interdit en production runtime.
- `WorldId = BK-DEMO-WORLD-WAVE5-LOCAL`: identifiant demo.
- `GameServerId = GS-DEMO-WAVE5-READINESS`: identifiant demo.
- `LocalDemoSeed = 738921`: seed demo local.
- `ChunkSize = 512`.
- `SectorSizeChunks = 4`.
- `WorldChunkWidth/Height = 64`.
- `ActiveChunkRadius = 2`.
- `StressWorldMapChunks = 50`.
- Wave5: origine C20_20, grille 25x25, cache 96.

Positions demo hard-codees:

- Centre de preuve: `new Vector2(16640f, 16640f)`, soit centre C32_32.
- Core chunk: `(WorldChunkWidth / 2, WorldChunkHeight / 2)`, soit C32_32.
- Core seed positions locales:
  - player hive: C32_32 local `(0.48, 0.54)`.
  - nectar: C32_32 local `(0.68, 0.38)`.
  - pollen: C32_32 local `(0.29, 0.35)`.
  - water: C32_32 local `(0.18, 0.64)`.
  - bestiary T3: C32_32 local `(0.78, 0.66)`.
  - ally/capital resources: C33_32 local offsets.
  - neutral/propolis/royal jelly: C31_33 local offsets.
- Proof chunk references observed in harnesses/reports: C32_32, C35_32, C20_20, C44_44.

Contrat de migration:

- Toute position demo doit devenir un `SpawnAnchor` versionne.
- Toute coordonnee monde doit pouvoir etre derivee depuis `(world_grid_version, chunk_id, local_0_1)`.
- Toute coordonnee sauvegardee doit inclure `world_coord_normalized`.
- Les chemins locaux et ids demo doivent etre remplaces par configuration/runtime snapshot.
- Les preuves peuvent garder ces constantes, mais doivent les nommer `demo_anchor` ou `proof_anchor`.

## Contrat normalise de coordonnees

Types minimum:

```text
WorldGridDefinition
- world_id
- world_grid_version
- grid_width_chunks
- grid_height_chunks
- chunk_size_world_units
- playable_rect_normalized
- origin_policy

WorldLogicalCoord
- chunk_x
- chunk_y
- local_x01
- local_y01
- world_coord
- world_coord_normalized
- chunk_id
- sector_id

SpawnAnchor
- anchor_id
- anchor_kind
- world_id
- world_grid_version
- chunk_id
- local_x01
- local_y01
- migration_source_optional
- proof_only
```

Conversion 25x25 -> 50x50:

1. Lire `world_coord_normalized` depuis le snapshot 25x25.
2. Appliquer la definition cible 50x50 versionnee.
3. Calculer `chunk_x = floor(normalized_x * target_width_chunks)`.
4. Calculer `chunk_y = floor(normalized_y * target_height_chunks)`.
5. Recalculer `local_x01/local_y01` depuis la fraction restante.
6. Revalider exclusions BearDen/eau/falaises/evenements.
7. Ecrire `migration_from_world_version` et `migration_audit_hash`.

Interdits:

- Sauver uniquement `Vector2 screen`.
- Sauver uniquement `Vector2 world` sans version de grille.
- Deriver une entite officielle depuis un pixel de terrain.
- Utiliser C32_32 comme centre implicite en production.

## Harness stress synthetique

Le harness doit rester logique:

- Ne pas creer de terrain.
- Ne pas charger 2500 textures.
- Ne pas instancier 2500 chunks.
- Ne pas ecrire dans `chunkCache`.
- Ne pas alterer les ressources/respawns du labo.

Cas obligatoires:

- Fenetre centre: C25_25 en grille 50x50.
- Coin nord-ouest: C00_00.
- Coin sud-est: C49_49.
- Fenetre la plus dense: recherche exhaustive 2500 centres.
- Catalogue complet: compte ruches/ressources/bestiary.
- Terrain Wave5 preserve: manifeste pret, cache <= 96.
- Stress desactive par defaut.

Assertions minimales:

- `catalog_coordinates == 2500`.
- `active_chunks <= 25`.
- `corner_active_chunks == 9`.
- `densest_hives <= 25`.
- `densest_resources <= 75`.
- `densest_bestiary <= 25`.
- `chunk_cache_before == chunk_cache_after`.
- `allocated_bytes <= 2_000_000`.
- `NO_50X50_ART_GENERATED=true`.

Extension recommandee:

- Ajouter une variante "allocation warm" apres un premier run pour prouver 0 B steady-state.
- Ajouter une variante "camera sweep" qui traverse 10 centres sans depasser les pools.
- Ajouter un compteur de labels actifs par LOD.

## Interfaces data et spawn seede futures

Ces interfaces ne supposent aucun serveur reel. Elles preparent seulement la frontiere.

Requete catalogue:

```text
IWorldCatalogProvider.GetWindow(WorldWindowRequest request) -> WorldWindowSnapshot

WorldWindowRequest
- world_id
- server_id
- season_id
- world_grid_version
- center_chunk_id
- radius_chunks
- filters
- client_preview_seed
- authority_version_optional

WorldWindowSnapshot
- world_id
- world_grid_version
- window_bounds_chunks
- entities
- exclusions
- generated_from_seed_version
- authority_version
- official
```

Entite:

```text
WorldRuntimeEntity
- entity_id
- entity_family: hive/resource/bestiary/event
- entity_type
- logical_coord
- tier_or_level
- variant
- faction_overlay_optional
- spawn_state
- interaction_lod_hint
- authority_version
- official
```

Spawn seede preview:

```text
ISpawnSeedPreview.Generate(SpawnSeedRequest request) -> SpawnSeedPreview

SpawnSeedRequest
- world_id
- server_id
- season_id
- chunk_id
- entity_family
- spawn_seed_version
- exclusion_version

SpawnSeedPreview
- proposed_entities
- rejected_candidates
- exclusion_hits
- deterministic_hash
- official=false
```

Production future:

- Le seed propose.
- Le serveur valide.
- Le client affiche le snapshot autorise.
- Les gains, combats, quantites et respawns officiels viennent du serveur.

## Risques

Risques principaux:

- Confondre stress 50x50 avec terrain 50x50 reel.
- Laisser les constantes demo C32_32 devenir des regles de production.
- Remplir progressivement `chunkCache` pendant un stress exhaustif.
- Charger les sprites par entite au lieu de les partager par famille.
- Multiplier les labels IMGUI et allocations strings pendant pan/zoom.
- Melanger coordonnees ecran, monde, chunk et normalisees.
- Permettre au client de calculer un gain ou resultat de combat officiel.
- Oublier les volumes d'exclusion BearDen/evenements lors de la migration.

Garde-fous:

- Tout mode stress doit exposer `NO_50X50_ART_GENERATED=true`.
- Tout snapshot demo doit porter `official=false`.
- Tout document de preuve doit reporter cache avant/apres.
- Toute entite persistable doit porter `world_grid_version`.
- Toute coordonnee hard-codee doit etre nommee `demo` ou `proof`.
- Toute nouvelle densite doit passer par budgets hives/resources/bestiary.
- Toute migration doit produire un audit hash.

## Gates de handoff

Avant de passer a une implementation production, verifier:

- [ ] Le 25x25 Wave5 reste intact: 625 tuiles, origine C20_20, cache <= 96.
- [ ] Le 50x50 reste logique uniquement tant que l'art terrain 50x50 n'est pas commande.
- [ ] Le stress 50x50 reste desactive par defaut.
- [ ] Le stress ne modifie pas `chunkCache`.
- [ ] Les fenetres centre/NW/SE/densest passent les budgets.
- [ ] Les pools entites couvrent 25 ruches, 75 ressources, 25 menaces.
- [ ] Les labels et hitboxes respectent le LOD interaction.
- [ ] Le steady-state pan/zoom est mesure a 0 B/frame apres warmup.
- [ ] Les chemins locaux et ids demo sont encapsules en config/proof.
- [ ] Les coordonnees demo sont convertibles en `SpawnAnchor`.
- [ ] Les snapshots incluent `world_grid_version` et `world_coord_normalized`.
- [ ] Les exclusions BearDen/evenements sont appliquees avant spawn.
- [ ] Les gains/combats/respawns officiels restent hors client.
- [ ] Les preuves ecrivent un recu lisible avec valeurs observees.

## Verdict

WORLD_MAP_50X50_RUNTIME_PERFORMANCE_CONTRACT=READY_FOR_HANDOFF

Le runtime actuel peut supporter un contrat 50x50 logique avec catalogue 2500 coordonnees, fenetre active bornee, cache terrain preserve et stress synthetique sans terrain. La prochaine etape technique ne doit pas agrandir l'art: elle doit normaliser les coordonnees, isoler les anchors demo/proof, ajouter les pools/LOD mesurables et preparer les interfaces de snapshot/spawn seedee sans serveur reel.
