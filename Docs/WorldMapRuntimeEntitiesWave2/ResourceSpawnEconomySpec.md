# WorldMap Resource Spawn Economy - Local Preview Spec

Date locale: 2026-07-15

Statut: specification documentaire Wave2, non implementee, locale et non officielle.

## 1. Objet et frontiere d'autorite

Cette specification definit une economie de spawn et d'interaction locale preview pour les ressources WorldMap suivantes:

- Nectar: `nectar`
- Pollen: `pollen`
- Eau: `water`
- Cire: `wax`
- Miel: `honey`
- Gelée royale: `royal_jelly`
- Propolis: `propolis`

Elle couvre les poids par biome et par chunk, R1-R3, les quantites, le respawn demo, les caps, les exclusions, les distances minimales, les contentions de spawn et de collecte, l'anti-farm preview et la migration logique 25x25/50x50.

Ce document ne valide aucune implementation. Il ne definit aucune economie de production et ne cree aucun gain officiel.

Invariants d'autorite non negociables:

```text
source_kind=seed_preview
server=false
official=false
official_gain=false
inventory_delta={}
reward_grants=[]
progression_delta={}
persistence_scope=session_preview
```

- Une quantite collectee est un decrement simule du noeud, pas un objet ajoute a un inventaire.
- Un compteur local peut afficher `simulated_collected`, mais il est jetable et ne peut alimenter ni compte, ni progression, ni craft, ni classement.
- Aucun receipt preview ne peut etre reutilise comme commande ou preuve de gain production.
- Le futur serveur reste autoritaire pour spawn, quantites, respawn, verrou de collecteur, recompenses et persistence.
- Aucun terrain, pixel, PNG, scene Unity ou APK n'est une source de verite pour cette economie.

## 2. Sources lues et baseline

Sources documentaires consommees:

- `Docs/WorldMapRuntimeEntitiesWave1/ResourceInteractionStage_Report.md`
- `Docs/BuilderCRelay/WorldMapSpawnDistribution_TechnicalContract.md`
- `Docs/WorldMapRuntimeEntitiesWave1/ProductionIntegrationContract.md`
- `Docs/WorldMapRuntimeEntitiesWave1/RuntimeScenarioDataLayer_Report.md` (P6)
- `Docs/WorldMapRuntimeEntitiesWave1/SpawnInspectorIntegration_Report.md` (P7)

Baseline observee, sans extrapolation:

- P6 expose un provider `local_demo`, des IDs stables, des coordonnees normalisees, une reprojection logique 25x25 vers 50x50, `server=false` et `official_gain=false`.
- P7 couvre les sept familles de ressources, R1-R3, les caps de 3 ressources/chunk et 75 ressources/fenetre 5x5, ainsi que les exclusions et distances generales.
- Le run P7 rapporte 25 chunks, 11 ressources et des hashes distincts pour deux seeds. Ce resultat est une observation, pas une cible minimale de densite.
- Le Resource Interaction Stage rapporte quantite restante, epuisement et respawn demo deterministe, mais ne publie ni delais par ressource, ni contention, ni anti-farm.
- Le contrat production reserve au serveur tout spawn, quantite, respawn, verrou et gain officiels.

Les tables et delais Wave2 ci-dessous sont donc des propositions locales versionnees. Ils ne modifient pas retroactivement les preuves P6/P7.

## 3. Vocabulaire et etats

| Terme | Definition locale preview |
| --- | --- |
| R1 `poor` | Noeud pauvre, quantite basse dans l'enveloppe P7. |
| R2 `medium` | Noeud moyen. |
| R3 `rich` | Noeud riche et rare, prioritaire pour la lisibilite et soumis a des caps renforces. |
| `capacity` | Quantite preview maximale du noeud. Ce n'est pas une valeur d'inventaire. |
| `remaining` | Quantite preview restant a depleter localement. |
| `available` | Selection et collecte preview permises. |
| `locked` | Un acteur preview detient le lease local court. |
| `depleted` | `remaining=0`; aucune collecte acceptee. |
| `cooldown` | Respawn planifie par l'horloge logique demo. |
| `suppressed` | Respawn differe par une regle anti-farm preview. |

Transitions autorisees:

```text
available -> locked -> available
available|locked -> depleted -> cooldown -> available
cooldown -> suppressed -> cooldown|available
```

Toute autre transition est un rejet inspectable. Un noeud depleted reste le meme noeud logique et compte dans les caps actifs; aucun remplacement opportuniste n'est genere.

## 4. Contexte deterministe, seeds et IDs

### 4.1 Contexte requis

Chaque generation doit recevoir explicitement:

```text
ResourceSpawnEconomyContext
- schema_version
- world_id
- world_grid_version
- server_id_preview
- season_id
- spawn_seed_value
- spawn_seed_version
- preview_id_schema_version
- distribution_table_version
- biome_table_version
- quantity_table_version
- respawn_rule_version
- cap_rule_version
- exclusion_volume_version
- habitat_rule_version
- distance_rule_version
- contention_rule_version
- anti_farm_rule_version
- migration_rule_version
- hash_algorithm_version
- chunk_id_logical
- chunk_x
- chunk_y
- grid_width_chunks
- grid_height_chunks
- chunk_size_world_units
- active_window_id
- active_chunk_ids_sorted
- chunk_biome_tag
- resource_density_profile
- source_kind=seed_preview
- official=false
```

`spawn_seed_value` est obligatoire, non vide, limite a 128 octets UTF-8 et n'est ni un secret ni un token d'autorite. `active_chunk_ids_sorted` contient l'ensemble canonique de la fenetre clampée, avec 9 a 25 chunks selon bord/coin. Une version inconnue ou un champ requis absent invalide le chunk ou la fenetre; aucun fallback silencieux n'est permis.

### 4.2 Versions initiales du prototype

| Champ | Valeur initiale | Changement qui impose un bump |
| --- | --- | --- |
| `schema_version` | `world_map_resource_economy_preview_v1` | Schema ou semantique d'un champ. |
| `preview_id_schema_version` | `preview_resource_id_v2` | Format ou canonisation des IDs. |
| `spawn_seed_version` | `resource_spawn_v2` | Nombre/ordre des draws, slots, positions ou algorithme seed. |
| `distribution_table_version` | `resource_distribution_preview_v2` | Poids famille/tier, profils ou modificateurs. |
| `biome_table_version` | `resource_biome_preview_v1` | Tags, poids biome ou heritage de biome. |
| `quantity_table_version` | `resource_quantity_preview_v1` | Bornes de capacity ou decrement maximal. |
| `respawn_rule_version` | `resource_respawn_demo_v1` | Delais, jitter ou horloge. |
| `cap_rule_version` | `resource_caps_preview_v1` | Cap chunk, fenetre ou rarete. |
| `habitat_rule_version` | `resource_habitat_preview_v1` | Eligibilite de bord d'eau ou autre habitat positif. |
| `distance_rule_version` | `resource_distance_preview_v1` | Toute distance ou priorite spatiale. |
| `contention_rule_version` | `resource_contention_preview_v1` | Tri, lease ou arbitrage de requetes. |
| `anti_farm_rule_version` | `resource_antifarm_preview_v1` | Chaleur, decay, rate limit ou cap de cycles. |
| `migration_rule_version` | `resource_grid_migration_preview_v1` | Reprojection, mapping ou preservation d'etat. |
| `hash_algorithm_version` | `sha256_canonical_utf8_v1` | Encodage, canonisation ou extraction des draws. |

`resource_spawn_v2` est volontairement distinct du `spawn_v1` P7: l'ajout du biome, du profil de densite et d'un seed canonique explicite change l'algorithme de distribution. Les versions sont immuables; une table modifiee sous le meme identifiant est un FAIL.

### 4.3 Canonisation et draws

- Chaines en UTF-8 NFC; enums en tokens ASCII minuscules; entiers en decimal invariant; separateur de champ `\n`.
- Les champs du contexte sont serialises dans l'ordre de la section 4.1.
- Chaque draw est domain-separe: `slot_presence`, `kind`, `tier`, `pos_x`, `pos_y`, `capacity`, `respawn_jitter`, `contention_rank`.
- `digest = SHA-256(canonical_context + "\ndomain=" + domain + "\nslot=" + slot + "\ncycle=" + cycle)`.
- Le draw entier est forme par les 8 premiers octets du digest, en unsigned 64-bit big-endian.
- Un choix pondere utilise `draw % somme_des_poids`; aucune arithmetique flottante n'est necessaire.
- Sont interdits: `GetHashCode()`, random global, heure murale, frame count, ordre de chargement, ordre de fichiers et position camera.

La position locale conserve l'enveloppe P7 `0.14..0.86` sur chaque axe. Une position rejetee n'est ni deplacee ni relancee.

### 4.4 IDs et hashes

Format Wave2:

```text
preview:{world_id}:{world_grid_version}:resource:{chunk_id_logical}:r{slot}:{spawn_seed_version}:{seed_digest16}:{distribution_table_version}
```

- `seed_digest16` correspond aux 16 premiers caracteres hex du SHA-256 du seed canonique; le seed brut n'est pas place dans l'ID.
- Un ID P7 importe est conserve dans `legacy_preview_id`.
- `lineage_id` prend l'ID source lors de la premiere creation et reste stable pendant une reprojection de snapshot.
- Un ID preview n'est jamais un `entity_id` officiel.

Deux hashes sont obligatoires:

- `base_distribution_hash`: contexte de generation, candidats acceptes et rejetes tries par ID, sans etat temporel.
- `runtime_availability_hash`: `remaining`, etat, lease, cycle, respawn, chaleur et `demo_clock_tick`.

Une collecte, un cooldown ou l'anti-farm ne doit jamais changer `base_distribution_hash`.

## 5. Tables locales preview R1-R3

### 5.1 Poids famille et tier

Les poids tier reprennent la baseline P7. Le poids famille neutral est le comparateur P7; en generation Wave2, il est remplace par le poids du biome de la section 6.

| Ressource | Token | Poids famille neutral | R1 | R2 | R3 |
| --- | --- | ---: | ---: | ---: | ---: |
| Nectar | `nectar` | 22 | 45 | 40 | 15 |
| Pollen | `pollen` | 22 | 50 | 35 | 15 |
| Eau | `water` | 14 | 55 | 35 | 10 |
| Cire | `wax` | 12 | 60 | 32 | 8 |
| Miel | `honey` | 12 | 55 | 35 | 10 |
| Gelée royale | `royal_jelly` | 8 | 75 | 20 | 5 |
| Propolis | `propolis` | 10 | 65 | 28 | 7 |

Chaque ligne R1+R2+R3 vaut 100. Le tier est tire apres la famille et ne depend pas de l'ordre d'affichage.

### 5.2 Quantites preview

Les sous-plages restent dans les enveloppes P7: R1 `8..49`, R2 `50..95`, R3 `96..129`. Les unites sont propres a chaque ressource et ne sont pas comparables a un prix, un loot ou un rendement production.

| Ressource | Capacity R1 | Capacity R2 | Capacity R3 | Decrement max/action |
| --- | ---: | ---: | ---: | ---: |
| Nectar | 24-42 | 58-82 | 104-124 | 12 |
| Pollen | 28-48 | 62-90 | 108-129 | 12 |
| Eau | 30-49 | 64-92 | 110-129 | 16 |
| Cire | 14-30 | 52-68 | 96-108 | 8 |
| Miel | 12-26 | 50-66 | 96-104 | 8 |
| Gelée royale | 8-12 | 50-54 | 96-98 | 2 |
| Propolis | 10-22 | 50-62 | 96-102 | 4 |

Regles:

- `capacity = min + draw_capacity % (max - min + 1)`.
- `remaining=capacity` a la creation et a chaque respawn autorise.
- La capacity reste identique pour le meme noeud, seed et versions; le respawn ne relance pas la quantity.
- Une action demande un entier `1..decrement_max`; le decrement reel vaut `min(requested, remaining)`.
- Le decrement produit seulement un receipt de simulation avec `official_gain=false` et aucun delta d'inventaire.

## 6. Poids par biome et par chunk

### 6.1 Table biome

Le biome est un tag logique versionne fourni par les donnees locales. Il n'est jamais deduit des couleurs ou pixels du terrain.

| Biome tag | Libelle | Profil par defaut | Nectar | Pollen | Eau | Cire | Miel | Gelée royale | Propolis | Total |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `flower_meadow` | Prairie fleurie | `abundant` | 30 | 32 | 10 | 8 | 9 | 4 | 7 | 100 |
| `forest_edge` | Lisiere forestiere | `standard` | 18 | 18 | 10 | 10 | 10 | 8 | 26 | 100 |
| `wetland_edge` | Bord de zone humide | `standard` | 18 | 16 | 32 | 7 | 8 | 4 | 15 | 100 |
| `apiary_orchard` | Rucher/verger | `abundant` | 24 | 22 | 10 | 15 | 17 | 7 | 5 | 100 |
| `rocky_highland` | Hauteur rocheuse | `sparse` | 12 | 14 | 8 | 20 | 8 | 5 | 33 | 100 |
| `neutral` | Neutre explicite | `standard` | 22 | 22 | 14 | 12 | 12 | 8 | 10 | 100 |

Un biome absent ou inconnu donne `MissingBiomeTag`; il n'est pas remplace implicitement par `neutral`.

### 6.2 Profil de densite chunk

| Profil | Score diagnostic | Slots candidats | Presence | Cap accepte du profil |
| --- | ---: | --- | --- | ---: |
| `blocked` | 0 | aucun | 0% | 0 |
| `sparse` | 60 | `r0` | `r0` si draw 0..99 < 60 | 1 |
| `standard` | 100 | `r0`, `r1` | `r0` toujours; `r1` si draw < 55 | 2 |
| `abundant` | 140 | `r0`, `r1`, `r2` | `r0/r1` toujours; `r2` si draw < 40 | 3 |

Le profil controle le nombre de candidats avant exclusions et distances. Il n'impose aucun minimum accepte.

Chaque chunk peut fournir un `resource_kind_multiplier` entier `0..200` par famille, avec 100 par defaut. Le poids effectif est:

```text
effective_weight(chunk, resource) = biome_weight * resource_kind_multiplier
```

- Les poids effectifs positifs sont renormalises par leur somme au moment du choix.
- `water` est force a 0 si le chunk n'a pas `water_edge_eligible=true`.
- Si tous les poids sont 0, le slot est rejete `NoEligibleResourceKind`; aucune famille de secours n'est tiree.
- Tout changement de profil ou modificateur exige un bump de `distribution_table_version` ou une version propre des donnees chunk incluse dans le contexte.
- Un candidat rejete n'est pas remplace. Cette regle evite les rerolls par exclusions, caps ou streaming.

## 7. Respawn demo local

L'horloge est `demo_clock_tick`, monotone, injectable en test, a 10 ticks/seconde. L'heure murale et le frame count ne sont jamais utilises. La pause demo suspend cette horloge.

Delais avant anti-farm, jitter deterministe inclus:

| Ressource | R1 secondes | R2 secondes | R3 secondes |
| --- | ---: | ---: | ---: |
| Nectar | 30-36 | 45-54 | 75-90 |
| Pollen | 25-30 | 40-48 | 70-84 |
| Eau | 35-42 | 55-66 | 90-108 |
| Cire | 45-54 | 75-90 | 120-144 |
| Miel | 50-60 | 80-96 | 135-162 |
| Gelée royale | 90-108 | 150-180 | 240-288 |
| Propolis | 70-84 | 110-132 | 180-216 |

La borne basse est le delai de base. Le jitter vaut `draw_respawn_jitter % (floor(base * 0.20) + 1)` secondes, avec `cycle` dans le domaine du hash. Le delai final est ensuite multiplie par le facteur anti-farm et arrondi au tick superieur.

Regles de cycle:

1. Au passage a `remaining=0`, enregistrer `depleted_tick`, incrementer la chaleur et calculer `respawn_due_tick`.
2. Le noeud reste `depleted/cooldown`; son ID, son tier, sa position et sa capacity ne changent pas.
3. A l'echeance, revalider exclusion, habitat, distance et caps dans les versions courantes du snapshot.
4. Si valide et non supprime, passer a `available`, remettre `remaining=capacity` et incrementer `respawn_cycle_index`.
5. Si invalide, rester depleted avec une raison inspectable; ne pas relocaliser et ne pas tirer un remplacement.
6. Le dechargement d'un chunk ne reinitialise ni timer, ni cycle, ni anti-farm.

Ces temps sont volontairement compresses pour une demo. Ils ne sont pas des temps de production.

## 8. Caps de spawn et d'activation

Un noeud available, locked, depleted, cooldown ou suppressed compte comme un noeud actif tant que son record appartient a la fenetre.

| Portee | Cap dur |
| --- | ---: |
| Ressources totales par chunk | 3 |
| Meme famille de ressource par chunk | 1 |
| R3 par chunk | 1 |
| Gelée royale par chunk | 1 |
| Chunks actifs par fenetre | 25 |
| Ressources totales par fenetre 5x5 | 75 |
| R3 par fenetre 5x5 | 8 |
| Gelée royale tous tiers par fenetre | 3 |
| Gelée royale R3 par fenetre | 1 |
| Propolis R3 par fenetre | 2 |
| Respawns effectivement reactives par seconde/fenetre | 3 |

- Les caps chunk de profil `sparse/standard/abundant` peuvent etre plus bas que le cap dur de 3.
- Tous les noeuds, y compris anchors demo eventuels, comptent dans les caps. Il n'existe pas d'exception economique cachee.
- Les respawns excedentaires dus a la meme seconde sont ordonnes par `contention_rank` puis decales au tick disponible suivant.
- Aucun nombre minimal de ressources n'est garanti. Le resultat P7 de 11 ressources sur 25 chunks reste donc compatible.
- Une grille logique 50x50 ne charge jamais 2500 chunks: les memes caps de fenetre 5x5 s'appliquent.

## 9. Exclusions et habitat Eau

Les volumes sont evalues avant les distances et caps. Pour plusieurs volumes, trier par priorite decroissante puis `volume_id` croissant et reporter toutes les collisions, avec une raison primaire stable.

| Volume/regle | Effet | Clearance preview | Raison primaire |
| --- | --- | ---: | --- |
| Hors rectangle jouable | Bloque toutes les ressources | 0 | `OutOfBounds` |
| `BearDen` | Bloque toutes les ressources | 60 unites monde | `Exclusion:BearDen` |
| `Water` core | Bloque toute ressource interactive, y compris `water` | 20 pour terrestre, 0 pour `water` | `Exclusion:WaterCore` |
| `Cliff` | Bloque toutes les ressources | 35 | `Exclusion:Cliff` |
| `Event`/`Reserved` | Bloque les ressources normales | max(90, clearance du volume) | `Exclusion:EventReserved` |
| `NoResource` | Bloque toutes les ressources | clearance du volume, 0 par defaut | `Exclusion:NoResource` |

`water` represente un point de collecte sur rive, pas un spawn dans l'eau navigable. Il est valide seulement si:

- le chunk porte `water_edge_eligible=true`;
- la position appartient a un volume positif `WaterHarvestEdge` de `habitat_rule_version`;
- la position est hors du volume `Water` core;
- toutes les autres exclusions et distances passent.

Un candidat Eau non eligible est rejete `WaterEdgeRequired`, sans reroll vers une autre ressource.

## 10. Distances minimales

Les distances sont en unites monde et reprennent les minima P7, avec des surcharges de rarete Wave2.

| Paire ou contrainte | Distance minimale |
| --- | ---: |
| Ressource - ressource, base | 90 |
| Meme famille de ressource | 110 |
| Au moins une ressource R3 | 120 |
| Au moins une Gelée royale | 150 |
| Propolis R3 - toute ressource | 140 |
| Ruche - ressource | 105 |
| Bestiaire - ressource | 80 |
| Evenement/reserve - ressource | max(90, clearance du volume) |
| BearDen - ressource | 60 au-dela du bord |
| Falaise - ressource | 35 au-dela du bord |

La distance effective est le maximum de toutes les regles applicables. Les controles couvrent le chunk courant et tout chunk dont les bounds croisent le rayon, meme si cela depasse les huit voisins lorsque `chunk_size_world_units` est petit.

L'ordre de validation est stable. En conflit, le perdant est rejete `MinDistanceConflict:{winner_id}`; il n'est ni pousse, ni deplace, ni regenere.

## 11. Contention deterministe

### 11.1 Contention de spawn

Cette spec conserve la priorite globale P7:

```text
anchor proof/demo > event > hive > ressource R3 > bestiaire T5-T7
> ressource R2 > ressource R1 > bestiaire T1-T4
```

Dans une meme classe de priorite, trier par `contention_rank`, puis par ID ordinal. Pour chaque candidat:

1. Valider bounds, biome et habitat.
2. Valider les volumes d'exclusion.
3. Valider les distances avec les candidats deja acceptes.
4. Valider les caps chunk, tier, famille et fenetre.
5. Accepter ou enregistrer exactement une raison primaire et les raisons secondaires.

Les caps de fenetre sont resolus en batch: reunir d'abord les candidats de tous les `active_chunk_ids_sorted`, puis appliquer le tri global. Il est interdit d'accepter definitivement chunk par chunk selon l'ordre de streaming. Une inspection limitee a un seul chunk peut exposer des candidats pre-budget, mais doit les marquer `provisional_until_window_budget=true`.

Tous les candidats acceptes et rejetes entrent dans `base_distribution_hash`. L'ordre des chunks demandes ne change pas le resultat d'une meme fenetre.

### 11.2 Contention de collecte

- `actor_preview_id` est un identifiant de session locale, pas une identite joueur officielle.
- Les demandes d'un meme `demo_clock_tick` sont batchées et triees par SHA-256 de `node_id|actor_preview_id|request_nonce`.
- Le gagnant acquiert un lease de 20 ticks, soit 2 secondes.
- Une demande concurrente recoit `ResourceBusyPreview` et ne modifie rien; il n'y a pas de file implicite.
- Le detenteur peut renouveler le lease par une action acceptee. Un unload ne libere pas le lease avant son tick d'echeance.
- Le decrement est atomique. Deux demandes ne peuvent jamais consommer deux fois la meme unite restante.
- Une demande sur depleted/cooldown/suppressed est rejetee avec l'etat courant et `respawn_due_tick` si applicable.

Receipt minimal:

```text
LocalResourceCollectionReceipt
- node_id
- lineage_id
- actor_preview_id
- request_tick
- requested_amount
- simulated_decrement
- remaining_after
- state_after
- collector_lock_until_tick
- respawn_due_tick_optional
- official=false
- official_gain=false
- inventory_delta={}
- reward_grants=[]
```

## 12. Anti-farm preview

L'anti-farm est un outil d'UX et de test local. Il ne pretend pas etre un dispositif anti-cheat et ne persiste aucun etat officiel.

Ledger local:

- cle session: `world_id|server_id_preview|season_id|preview_session_id`;
- cle noeud: `lineage_id`;
- cle zone: cellule canonique 25x25 derivee de `world_coord_normalized`, afin de survivre a une reprojection 50x50;
- le ledger n'est pas efface par changement de seed, pan, zoom, unload/reload ou passage 25x25/50x50;
- seul `Reset preview` explicite le remet a zero et incremente `anti_farm_reset_count` dans l'inspection.

Regles:

| Controle | Valeur preview | Rejet/effet |
| --- | --- | --- |
| Debounce acteur/noeud | 8 ticks (0,8 s) | `ActorNodeDebounced` |
| Debit acteur | 8 actions acceptees / 100 ticks (10 s) | `ActorPreviewRateLimited` |
| Chaleur ajoutee a depletion | R1=1, R2=2, R3=3 | Appliquee une fois par cycle. |
| Decroissance chaleur | -1 / 600 ticks (60 s) sans depletion dans la cellule | Minimum 0. |
| Chaleur 0-3 | multiplicateur respawn 1,00 | Aucun delai additionnel. |
| Chaleur 4-6 | multiplicateur 1,25 | Arrondi au tick superieur. |
| Chaleur 7-9 | multiplicateur 1,50 | Arrondi au tick superieur. |
| Chaleur >=10 | suppression au moins 1800 ticks (180 s) et jusqu'a chaleur <10 | `FarmHeatSuppressed` |
| Respawns/noeud | 3 dans toute fenetre glissante de 6000 ticks (10 min) | Le suivant attend la sortie du plus ancien cycle. |

Contraintes:

- L'anti-farm ne change ni famille, ni tier, ni capacity, ni position, ni `base_distribution_hash`.
- Il ne fait que refuser une action ou retarder une disponibilite; `runtime_availability_hash` porte cet etat.
- Changer de seed ne remet pas la chaleur a zero et ne cree pas de gain. Un nouvel ensemble de noeuds reste sous la meme cle de zone.
- Aucun reroll ou backfill n'est lance apres suppression.
- Un restart peut perdre le ledger car il est `session_preview`; cette limite doit etre affichee dans l'inspection et n'est pas une garantie de securite.

## 13. Migration logique 25x25/50x50

### 13.1 Source de verite

La migration utilise uniquement:

- `world_coord_normalized` valide dans `[0,1]`;
- `lineage_id` et `source_preview_id`;
- toutes les versions de generation/economie;
- l'etat `capacity`, `remaining`, tier, cycle, cooldown, lease et anti-farm;
- les definitions logiques de grille, biome, habitat et exclusions.

Elle n'utilise ni pixels, ni coordonnees ecran, ni terrain 25x25/50x50.

Pour stabiliser les frontieres, Wave2 ajoute `world_coord_q1e6` dans `0..1_000_000`. Une coordonnee P6 est validee contre NaN, infini et `[0,1]`, puis convertie une seule fois par `round_half_up(value * 1_000_000)` et auditee.

Pour un axe et une grille de taille `N`:

```text
si q == 1_000_000:
  chunk = N - 1
  local_q = 1_000_000
sinon:
  scaled = q * N
  chunk = scaled div 1_000_000
  local_q = scaled mod 1_000_000
```

La meme formule couvre 25 vers 50 et 50 vers 25, y compris les bords exacts 0 et 1.

### 13.2 Migration de snapshot, pas regeneration

Une migration de snapshot:

1. Gele et trie les records source par `lineage_id`.
2. Reprojette chaque coordonnee normalisee vers la grille cible.
3. Conserve famille, tier, capacity, remaining, cycle et duree restante du cooldown.
4. Produit un nouvel ID de grille cible et un mapping `source_preview_id -> target_preview_id`; `lineage_id` reste inchange.
5. Reassocie la chaleur a la cellule canonique 25x25, sans reset.
6. Herite le biome et les modificateurs du chunk parent 25x25 vers ses quatre enfants 50x50 seulement si aucune table cible versionnee n'existe; cet heritage porte `biome_inherited_from_grid`.
7. Revalide habitat, exclusions, distances et caps cibles dans l'ordre deterministe.
8. Liste chaque record accepte, rejete ou retarde. Aucun drop silencieux et aucune duplication ne sont permis.
9. Ecrit `migration_audit_hash` en SHA-256 sur le contexte, le mapping et les decisions tries.

Une generation fraiche sur `grid_50x50` avec `resource_spawn_v2` n'est pas une migration: elle peut produire d'autres noeuds et doit porter un nouveau `base_distribution_hash`.

Si `quantity_table_version` change pendant une migration explicite:

- recalculer la nouvelle capacity depuis `lineage_id` et la nouvelle table;
- conserver depleted a 0;
- sinon calculer `new_remaining = floor((old_remaining * new_capacity + old_capacity / 2) / old_capacity)`, puis clamp `1..new_capacity`;
- enregistrer ancien/nouveau couple dans l'audit.

Les timers conservent leur duree logique restante. Une migration ne rend jamais immediatement disponible un noeud en cooldown ou suppressed.

### 13.3 Caps et collisions de grille

- La fenetre active reste limitee a 5x5 chunks et 25 chunks sur les deux grilles.
- Un merge 50x50 vers 25x25 peut rapprocher plusieurs noeuds. Le gagnant est choisi par priorite puis `contention_rank`; les autres sont rejetes `MigrationDistanceConflict` ou `MigrationCapExceeded`.
- Un split 25x25 vers 50x50 ne duplique jamais un noeud dans les quatre chunks enfants.
- Toute exclusion apparue dans la grille cible produit `MigrationExclusionHit` avec `volume_id`.
- La migration ne genere aucun terrain 50x50 et n'instancie aucun catalogue global.

## 14. Inspection locale requise

L'inspection read-only doit exposer, au minimum:

- contexte, seed digest et toutes les versions;
- biome, profil, modificateurs et poids effectifs du chunk;
- candidats acceptes/rejetes avec raison primaire et raisons secondaires;
- famille, R1-R3, capacity, remaining, etat, cycle et respawn due;
- caps chunk/fenetre et compteurs rares;
- lease, debit acteur, chaleur, suppression et compte de resets;
- `base_distribution_hash`, `runtime_availability_hash` et `migration_audit_hash` si applicable;
- `server=false`, `official=false`, `official_gain=false`.

L'inspection ne peut confirmer recompense, inventaire, craft, progression, prix ou persistence.

## 15. Matrice de tests negatifs

| ID | Injection negative | Resultat obligatoire |
| --- | --- | --- |
| RSE-NEG-001 | `spawn_seed_value` vide ou version absente | Rejet contexte `MissingSeedOrVersion`; aucun spawn. |
| RSE-NEG-002 | Meme contexte/version produit deux listes ou hashes differents | Gate determinisme FAIL; prototype refuse. |
| RSE-NEG-003 | Poids/quantite/delai modifie sans bump de version | `VersionContentMismatch`; aucun fallback. |
| RSE-NEG-004 | Biome absent/inconnu | `MissingBiomeTag`; pas de fallback neutral. |
| RSE-NEG-005 | Poids negatif, multiplicateur >200 ou somme effective nulle | Table refusee ou slot `NoEligibleResourceKind`; aucune division par zero. |
| RSE-NEG-006 | Candidat `water` sans `WaterHarvestEdge` ou dans Water core | Rejet `WaterEdgeRequired`/`Exclusion:WaterCore`; aucun reroll. |
| RSE-NEG-007 | Candidat dans BearDen, Cliff, Event/Reserved ou hors bounds | Rejet avec volume et raison exacte; aucune relocalisation. |
| RSE-NEG-008 | Candidat sous distance minimale | Un gagnant stable, perdant `MinDistanceConflict:{id}`. |
| RSE-NEG-009 | 4e ressource chunk, 76e fenetre, 9e R3 ou rare au-dessus du cap | Surplus rejete `CapExceeded:{scope}`. |
| RSE-NEG-010 | `capacity<0`, `remaining<0`, `remaining>capacity` ou tier hors R1-R3 | Snapshot refuse `InvalidResourceQuantity`. |
| RSE-NEG-011 | Deux collectes meme tick sur le meme noeud | Une seule mutation atomique; autre `ResourceBusyPreview`. |
| RSE-NEG-012 | Collecte sur depleted/cooldown/suppressed | Rejet sans decrement ni receipt de gain. |
| RSE-NEG-013 | Horloge recule ou respawn avant `respawn_due_tick` | `NonMonotonicDemoClock` ou maintien cooldown. |
| RSE-NEG-014 | Pan, unload, seed swap ou grille swap remet timer/chaleur a zero | Gate anti-farm FAIL. |
| RSE-NEG-015 | 4e respawn d'un noeud dans 10 minutes ou chaleur >=10 | Respawn retarde selon ledger, sans nouveau noeud. |
| RSE-NEG-016 | `official=true`, `official_gain=true`, inventory/progression delta ou reward non vide | Hard FAIL `OfficialAuthorityViolation`; action annulee. |
| RSE-NEG-017 | Coordonnee NaN, infinie ou hors `[0,1]` | Migration refusee `InvalidNormalizedCoordinate`; aucun clamp silencieux. |
| RSE-NEG-018 | Reprojection duplique un lineage ou omet un rejet | Gate migration FAIL; audit incomplet refuse. |
| RSE-NEG-019 | Migration 50x50 charge plus de 25 chunks ou cree 2500 objets actifs | Gate streaming/cap FAIL. |
| RSE-NEG-020 | Changement de grille regenere au lieu de migrer un snapshot | Rejet de la migration; operation reclassifiee explicitement `fresh_generation`. |
| RSE-NEG-021 | Etat cooldown/chaleur change `base_distribution_hash` | Gate separation hashes FAIL. |
| RSE-NEG-022 | Receipt preview est accepte comme grant/inventaire | Hard FAIL d'autorite et aucune mutation durable. |

Tous ces negatifs doivent etre automatisables avec horloge et contexte injectes. Aucun test ne requiert serveur, remote, donnee reelle, Unity, PNG ou APK.

## 16. Gates de handoff documentaire

Le gate final indique uniquement que la specification est assez complete pour construire un prototype local. Il ne dit pas que ce prototype existe ou qu'il est valide.

| Gate | Valeur documentaire | Condition |
| --- | --- | --- |
| `RESOURCE_PREVIEW_AUTHORITY_ISOLATED` | YES | Tous les outputs restent `official=false`, sans grant. |
| `RESOURCE_R1_R3_TABLES_COMPLETE` | YES | Sept ressources, poids et quantites definis. |
| `RESOURCE_BIOME_CHUNK_WEIGHTS_DEFINED` | YES | Biomes, profils et modificateurs versionnes. |
| `RESOURCE_DEMO_RESPAWN_DEFINED` | YES | Horloge, delais, jitter et cycle definis. |
| `RESOURCE_CAPS_EXCLUSIONS_DISTANCES_DEFINED` | YES | Caps chunk/fenetre/rares et contraintes spatiales explicites. |
| `RESOURCE_CONTENTION_DEFINED` | YES | Arbitrage spawn et collecte atomique explicites. |
| `RESOURCE_ANTI_FARM_PREVIEW_DEFINED` | YES | Ledger, chaleur, rate limit et limites documentes. |
| `RESOURCE_MIGRATION_25X25_50X50_DEFINED` | YES | Mapping, preservation, revalidation et audit explicites. |
| `RESOURCE_NEGATIVE_TESTS_DEFINED` | YES | Matrice RSE-NEG-001..022 presente. |
| `SERVER_OR_REMOTE_REQUIRED` | NO | Prototype entierement local. |
| `OFFICIAL_GAIN_OR_PERSISTENCE_ALLOWED` | NO | Interdit par contrat et gates negatifs. |
| `UNITY_PNG_APK_CHANGE_REQUIRED` | NO | Specification documentaire seulement. |

Le gate doit devenir `NO` si une table n'est pas versionnee, si le seed reel n'est pas explicite, si un rejet est silencieux, si l'anti-farm modifie la distribution de base, si une migration duplique/perd un lineage, ou si une voie de gain officiel existe.

```text
READY_FOR_LOCAL_RESOURCE_ECONOMY_PROTOTYPE=YES
LOCAL_RESOURCE_ECONOMY_IMPLEMENTATION_VALIDATED=NO
OFFICIAL_RESOURCE_ECONOMY_APPROVED=NO
```

Le `YES` autorise uniquement l'implementation future d'un prototype local inspectable contre cette spec. Il n'autorise aucun spawn, gain, respawn, inventaire ou etat officiel cote client.
