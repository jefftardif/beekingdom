# WorldMap Spawn Inspector - Performance and Determinism Telemetry Spec P8

Date locale: 2026-07-15

## Portee

Cette specification definit la telemetrie locale read-only necessaire pour fermer
l'audit P7 du Spawn Inspector. Elle mesure seed, hashes, chunks, caches,
allocations, temps CPU, exclusions, distances, budgets et autorite.

Contraintes:

- aucune mutation de terrain, scene, PNG, APK, BearDen source ou donnee officielle;
- aucun serveur reel et aucun gain officiel;
- le 50x50 reste un catalogue logique de 2500 coordonnees;
- la fenetre active reste bornee a 25 chunks;
- la telemetrie n'influence jamais le generateur;
- la serialisation et l'affichage des mesures se font hors des zones chronometrees.

READY_FOR_P8_TELEMETRY=YES signifie que le schema et ses seuils sont prets a etre
implementes. Il ne signifie pas que les mesures P8 ont deja passe.

## Principes

1. Un run de preuve doit etre reconstructible sans connaitre l'etat de l'UI.
2. Valeur de seed, version de seed et versions de donnees sont des champs distincts.
3. Le hash runtime historique est conserve, mais le gate repose sur un digest
   d'audit canonique plus complet.
4. Chaque candidat a exactement une disposition finale: accepte ou rejete.
5. Chaque rejet a une phase primaire structuree: exclusion, distance ou budget.
6. Les compteurs de performance sont observes; ils ne font jamais partie du
   resultat logique ni de son digest.
7. Un seuil depasse produit FAIL; une mesure absente produit NOT_MEASURED et bloque
   le gate concerne.

## Enveloppe de telemetrie

Un enregistrement SpawnTelemetryRun contient au minimum:

| Groupe | Champs obligatoires |
| --- | --- |
| Identite | telemetry_schema_version, run_id, sample_kind, build_fingerprint, timestamp_utc |
| Mesure | measurement_scope, warmup_count, sample_count, reference_environment_id |
| Contexte | world_id, season_id, server_id_preview, source_kind, official |
| Seed | spawn_seed_value, spawn_seed_encoding, spawn_seed_version |
| Versions | distribution_table_version, exclusion_volume_version, world_grid_version |
| Grille | grid_width_chunks, grid_height_chunks, chunk_size_world_units |
| Requete | center_chunk_x, center_chunk_y, radius_chunks, request_order_policy |
| Hashes | generator_hash_algorithm, generator_hash, context_digest, candidate_digest, accepted_digest, rejected_digest, inspection_audit_digest |
| Chunks | requested, active, unique, activated, deactivated, active_set_digest |
| Familles | candidate, accepted et rejected par hive/resource/bestiary/event |
| Cache | entity cache before/after/peak, terrain cache before/after/peak, sprite loads, pool occupancy |
| Allocations | bytes par scope, bytes/frame, bytes/switch, bytes/stress, instantiate_count, destroy_count |
| Temps | elapsed CPU par scope, p50, p95, max et stress median |
| Exclusions | volumes et tested/hits/rejected/accepted_invalid par kind et famille |
| Distances | checks, rejects, accepted_violation_count, minimum observe par paire |
| Autorite | official_true_count, official_gain_true_count, official_action_count, server_request_count |
| Resultat | gate_seed_hash, gate_chunks, gate_cache, gate_allocations, gate_time, gate_exclusions, gate_authority |

run_id et timestamp_utc servent uniquement a distinguer les mesures. Ils sont
interdits dans tout calcul de spawn, d'ID preview ou de digest logique.

## Seed et contexte canonique

### Champs requis

| Champ | Regle |
| --- | --- |
| spawn_seed_value | Valeur exacte consommee par le generateur, jamais un libelle Seed A/Seed B. |
| spawn_seed_encoding | decimal_u64_v1 ou utf8_exact_v1. |
| spawn_seed_version | Version de l'algorithme de seed, par exemple spawn_v1. |
| distribution_table_version | Version exacte des poids R1-R3/T1-T7. |
| exclusion_volume_version | Version exacte des volumes appliques. |
| world_grid_version | Version exacte de la grille et du rectangle jouable. |

Pour decimal_u64_v1, la forme canonique est en base 10, sans signe et sans zero
initial sauf la valeur 0. Pour utf8_exact_v1, les octets UTF-8 sont conserves
exactement; aucune trim, casse ou normalisation implicite n'est autorisee.

Le seed reel doit participer au context_digest et au calcul de distribution.
spawn_seed_version ne peut jamais servir de substitut a spawn_seed_value.

### Seuils seed/contexte

| Assertion | Seuil PASS |
| --- | ---: |
| Champs de contexte obligatoires absents | 0 |
| Encodages de seed inconnus | 0 |
| Runs de repetition avec contexte different involontairement | 0 |
| Valeur de seed injectee depuis heure/frame/ordre de chargement | 0 |
| Valeur officielle ou secret serveur dans la telemetrie locale | 0 |

## Hashes et serialisation canonique

### Deux niveaux de hash

generator_hash conserve le hash runtime expose par P7. Il doit etre accompagne de
generator_hash_algorithm, par exemple fnv1a32_utf8_v1 ou xxhash32_v1. Une valeur
32-bit est encodee par 8 caracteres hexadecimaux minuscules.

inspection_audit_digest est le digest de preuve obligatoire:

- algorithme: SHA-256;
- encodage: UTF-8;
- sortie: 64 caracteres hexadecimaux minuscules;
- payload: spawn_inspection_payload_v1 canonique.

Les hashes historiques 01b78336 et fef6f1b4 restent des observations P7. Ils ne
sont pas des golden values tant que leurs seeds et payloads ne sont pas recus.

### Payload canonique

Le payload est un objet structure, jamais une concatenation ambigue. Sa
serialisation respecte:

- ordre des champs fixe selon le schema;
- culture invariante;
- booleens true/false en minuscules;
- entiers en base 10;
- chaines JSON echappees en UTF-8;
- fin de ligne LF;
- aucun champ optionnel omis sans presence explicite null;
- aucune coordonnee flottante brute.

Les coordonnees normalisees sont encodees en entiers:

- normalized_x_q1e6 = round_half_away_from_zero(clamp(x, 0, 1) * 1000000);
- normalized_y_q1e6 = round_half_away_from_zero(clamp(y, 0, 1) * 1000000).

Ordres canoniques:

| Collection | Ordre |
| --- | --- |
| Chunks | chunk_y numerique, puis chunk_x numerique |
| Volumes | politique d'exclusion versionnee, puis volume_id ordinal |
| Candidats | rang de priorite deterministe, famille, slot, candidate_id ordinal |
| Acceptes | meme ordre canonique que les candidats |
| Rejetes | meme ordre canonique que les candidats, puis phase et detail |

Le payload de inspection_audit_digest inclut le contexte complet, les chunks
actifs, tous les candidats, acceptes, rejetes et leurs raisons. Les digests
context/candidate/accepted/rejected isolent les sous-ensembles pour diagnostiquer
un mismatch.

### Champs exclus des digests logiques

- run_id, timestamp_utc et sample_kind;
- machine, OS, build path et reference_environment_id;
- frame index, temps CPU, allocations et statistiques cache;
- etat visuel de camera, zoom, selection et overlay, sauf center_chunk derive;
- ordre d'arrivee non canonique des requetes;
- texte de log, UI, labels et format d'affichage;
- compteurs de telemetrie et resultat des gates.

Les versions, le seed reel, les exclusions, les coordonnees, les tiers, les
capacites, les IDs, les dispositions et les raisons de rejet ne sont jamais
exclus.

### Seuils hash/determinisme

| Assertion | Seuil PASS |
| --- | ---: |
| Mismatch sur deux runs identiques dans la meme session | 0 |
| Mismatch apres sortie puis reentree de fenetre | 0 |
| Mismatch apres nouvelle session avec meme contexte | 0 |
| Mismatch apres permutation de l'ordre chunks/volumes | 0 |
| IDs dupliques dans un run | 0 |
| Candidats sans disposition finale | 0 |
| Candidats avec plusieurs dispositions finales | 0 |
| accepted_count + rejected_count - candidate_count | 0 |
| Variation seed avec context_digest inchange | 0 cas toleres |
| Variation table/exclusion/grille avec inspection_audit_digest inchange | 0 cas toleres |

Pour la fixture de variation seed, candidate_digest doit aussi differer. Un
simple changement de metadata sans changement des candidats ne satisfait pas ce
cas.

## Telemetrie chunks et densite

### Compteurs obligatoires

- catalog_coordinate_count;
- requested_chunk_count, active_chunk_count et unique_active_chunk_count;
- active_chunk_set_digest;
- active_chunk_min_x/max_x/min_y/max_y;
- activated_chunk_count et deactivated_chunk_count par transition;
- candidate/accepted/rejected par famille et par chunk;
- max_hives_per_chunk, max_resources_per_chunk, max_bestiary_per_chunk,
  max_events_per_chunk;
- active_hives, active_resources, active_bestiary, active_events;
- proof_anchor_count et liste des chunks avec exception explicite;
- scene_chunk_object_count_peak pour detecter une activation 2500 involontaire.

### Seuils chunks

| Scenario/mesure | Seuil PASS |
| --- | ---: |
| Catalogue 50x50 | catalog_coordinate_count == 2500 |
| Centre 50x50 C25_25 | active_chunk_count == 25 |
| Coin NW C00_00 | active_chunk_count == 9 |
| Coin SE C49_49 | active_chunk_count == 9 |
| Toute fenetre valide | 9 <= active_chunk_count <= 25 |
| Chunks actifs uniques | unique_active_chunk_count == active_chunk_count |
| Hive par chunk | <= 1, sauf proof_anchor explicitement recu |
| Resource par chunk | <= 3 |
| Bestiary par chunk | <= 1 |
| Event par chunk | <= 1 |
| Hives par fenetre | <= 25 |
| Resources par fenetre | <= 75 |
| Bestiary par fenetre | <= 25 |
| Events par fenetre | <= 8 |
| Chunks scene simultanes en stress | <= 25 |
| Chargement simultane des 2500 chunks | 0 occurrence |

Les exceptions proof_anchor ne sont jamais implicites. Chaque exception contient
anchor_id, family, chunk, proof_only=true et raison. Elle reste incluse dans le
digest.

## Telemetrie cache et pools

### Compteurs obligatoires

| Surface | Compteurs |
| --- | --- |
| Cache chunks entites | count_before, count_after, count_peak, hit_count, miss_count, write_count, eviction_count |
| Cache terrain Wave5 | texture_count_before, texture_count_after, texture_count_peak, load_count_delta, eviction_count |
| Cache sprites | key_count, load_count_before/after, duplicate_load_count |
| Pools | capacity, active_before/after/peak, checkout_count, return_count, overflow_count |

Les compteurs terrain et entites restent separes. Une cle sprite est un chemin
logique stable; une coordonnee ou un entity_id n'est jamais une cle de sprite.

### Seuils cache/pools

| Assertion | Seuil PASS |
| --- | ---: |
| Stress 50x50: entity_chunk_cache_count_after - before | 0 |
| Stress 50x50: entity_chunk_cache_write_count | 0 |
| Cache textures terrain peak | <= 96 |
| Stress Spawn Inspector: terrain texture load delta | 0 |
| Charges sprite dupliquees apres warmup | 0 |
| Pool hives active peak | <= 25 |
| Pool resources active peak | <= 75 |
| Pool bestiary active peak | <= 25 |
| Pool events active peak | <= 8 |
| Pool overflow | 0 |
| Instantiate apres warmup dans pan/zoom | 0 |
| Destroy apres warmup dans pan/zoom | 0 |

Le taux hit/miss est rapporte mais n'a pas de seuil absolu P8: il depend du
parcours. Toute regression future utilise le meme parcours et declenche WARNING
si le hit rate baisse de plus de 10 points de pourcentage.

## Telemetrie allocations

### Scopes de mesure

| Scope | Protocole |
| --- | --- |
| steady_same_window | 3 warmups, puis 300 frames sans changement de chunk central |
| active_window_switch | 3 warmups, puis 20 transitions de centre connues |
| inspect_repeat | 3 warmups, puis 100 inspections du meme contexte |
| stress50_cold | 1 run complet de 2500 coordonnees |
| stress50_warm | 1 warmup, puis 5 runs mesures |

Chaque scope recoit allocation_bytes_total, allocation_bytes_max_sample,
allocation_bytes_per_frame ou per_switch, ainsi que instantiate_count et
destroy_count. La collecte utilise des buffers prealloues.

### Exclusions de mesure allocations

Sont hors de la zone mesuree:

- compilation, domain reload et initialisation de la preuve;
- les trois warmups declares;
- construction du run_id et lecture de l'horloge;
- serialisation, ecriture, affichage et tri final de telemetrie;
- rendu, IMGUI, labels et captures de preuve;
- chargement initial des outils de mesure.

Ne sont pas exclus:

- generation des candidats;
- tri deterministe necessaire au generateur;
- tests d'exclusion, distance et budget;
- construction/reutilisation des listes actives;
- activation, retour et overflow des pools;
- changement de fenetre et maintenance de cache induite par ce changement.

### Seuils allocations

| Scope | Cible | Seuil PASS |
| --- | ---: | ---: |
| steady_same_window spawn scope | 0 B/frame | 0 B total sur 300 frames |
| Budget global temporaire observe | 0 B/frame | <= 1,024 B sur toute frame, avec WARNING si >0 |
| active_window_switch | amorti | <= 32,768 B par switch |
| inspect_repeat apres warmup | 0 B/call | 0 B total sur 100 calls |
| stress50_cold | <= 2,000,000 B/run | <= 2,000,000 B |
| stress50_warm | 0 B/run cible | <= 2,000,000 B/run et aucune hausse run-over-run |
| Instantiate/Destroy pan/zoom apres warmup | 0 | 0 |

Un scope avec collecte intrusive ou allocation non attribuable est
MEASUREMENT_INVALID, jamais PASS par soustraction estimee.

## Telemetrie temps CPU

### Scopes

- candidate_generation_cpu_ms;
- exclusion_cpu_ms;
- distance_cpu_ms;
- budget_cpu_ms;
- active_list_rebuild_cpu_ms;
- spawn_simulation_total_cpu_ms;
- inspector_ui_cpu_ms, mesure separee et non incluse dans simulation;
- stress50_total_cpu_ms;
- telemetry_finalize_cpu_ms, mesure separee.

Utiliser une horloge monotone haute resolution. Rapporter p50, p95 et max apres
warmup. Les scopes enfants ne doivent pas etre additionnes si leurs intervalles se
chevauchent.

### Protocoles et seuils temps

| Scope | Echantillons | Cible | Seuil PASS |
| --- | ---: | ---: | ---: |
| steady_same_window simulation | 300 frames | p95 <= 1.0 ms | max <= 4.0 ms |
| active_window_switch simulation | 20 switches | p95 <= 4.0 ms | max <= 4.0 ms |
| inspect_repeat | 100 calls | baseline stable | p95 <= 4.0 ms |
| stress50_warm | 5 runs | baseline stable | median <= baseline verrouillee x 1.20 |
| stress50_warm max | 5 runs | baseline stable | max <= baseline verrouillee x 1.50 |

Le contrat source ne fixe pas de plafond absolu portable pour le scan synthetique
2500. Le premier recu P8 verrouille donc stress50_baseline_median_ms sur un
reference_environment_id stable. Ce premier baseline doit etre rapporte mais ne
peut pas auto-valider une regression. Toute comparaison ulterieure exige le meme
environnement et le meme build de mesure.

Un max simulation > 4.0 ms est FAIL meme si le p95 passe. UI, rendu et
telemetry_finalize sont diagnostiques separes et ne masquent pas le cout de
simulation.

## Telemetrie exclusions

### Enregistrement par volume

Chaque volume rapporte:

- volume_id, volume_kind et exclusion_version;
- shape_kind et bounds_digest, sans dupliquer une geometrie lourde;
- priority et exclusion_order_policy;
- blocks_families;
- candidates_tested, hit_count, rejection_count et allowed_count par famille;
- selected_as_primary_reason_count;
- input_order_index uniquement pour le test de permutation.

Chaque rejet d'exclusion rapporte candidate_id, family, volume_id, volume_kind,
phase=exclusion et reason_code. La phase exclusion est evaluee avant distance,
puis budget.

### Fixtures positives obligatoires

| Fixture | Couverture minimale |
| --- | --- |
| EXC_BEARDEN | >=1 hive, >=1 resource et >=1 bestiary bloques |
| EXC_WATER_LAND | >=1 entite terrestre bloquee |
| EXC_WATER_RESOURCE | Comportement water conforme a la table versionnee, allowed ou blocked explicitement |
| EXC_CLIFF | >=1 hive et >=1 resource bloques |
| EXC_EVENT | >=1 candidat bloque ou reserve selon event_rule_version |
| EXC_OVERLAP | >=1 candidat dans deux volumes, raison primaire stable |
| EXC_ORDER | Meme fixture avec liste volumes permutee, meme disposition et meme digest |

### Seuils exclusions

| Assertion | Seuil PASS |
| --- | ---: |
| Candidats acceptes dans un volume qui bloque leur famille | 0 |
| Rejets d'exclusion sans candidate_id | 0 |
| Rejets d'exclusion sans volume_id/kind/version | 0 |
| Rejets silencieux | 0 |
| Violation ordre exclusion -> distance -> budget | 0 |
| Mismatch apres permutation des volumes | 0 |
| Fixtures obligatoires sans hit/disposition attendue | 0 |
| BearDen hit par hive/resource/bestiary fixture | >= 1 pour chaque famille |
| Water land hit | >= 1 |
| Cliff hit par hive/resource fixture | >= 1 pour chaque famille |
| Event disposition versionnee | >= 1 |

La politique d'ordre doit avoir un identifiant versionne et un tie-break ordinal
sur volume_id. Sa direction de priorite ne peut pas rester implicite.

## Telemetrie distances et budgets

Compteurs requis:

- distance_check_count et distance_rejection_count par paire;
- configured_min_distance et accepted_min_observed_distance par paire;
- accepted_distance_violation_count;
- budget_rejection_count par famille et par chunk/fenetre;
- priority_rank du candidat accepte/rejete;
- candidate_accounting_error_count.

Seuils:

| Assertion | Seuil PASS |
| --- | ---: |
| accepted_distance_violation_count | 0 |
| candidate_accounting_error_count | 0 |
| Rejet distance sans paire ni distance mesuree | 0 |
| Rejet budget sans cap, scope et compte avant/apres | 0 |
| Ordre different apres permutation candidats | 0 mismatch |

Distances minimales a telemetrer:

| Paire | Minimum |
| --- | ---: |
| hive-hive | 300 |
| hive-resource | 105 |
| hive-bestiary | 160 |
| resource-resource | 90 |
| resource-bestiary | 80 |
| bestiary-bestiary | 180 |

Chaque paire recoit une fixture sous la limite et une fixture a la limite. Le
resultat a la limite doit suivre une convention inclusive/exclusive versionnee et
rester identique entre sessions.

## Telemetrie d'autorite

Compteurs obligatoires:

- inspection_official_true_count;
- entity_official_true_count;
- official_gain_true_count;
- official_combat_resolution_count;
- official_raid_resolution_count;
- official_respawn_schedule_count;
- official_persistence_write_count;
- server_request_count;
- local_preview_receipt_count.

Seuils PASS:

| Assertion | Seuil |
| --- | ---: |
| Tout compteur official=true ou action officielle | 0 |
| server_request_count | 0 |
| Inspections sans official=false explicite | 0 |
| Entites locales sans official=false explicite | 0 |
| local_preview_receipt_count | >= 1 pour les actions preview testees |

Un compteur absent ne vaut jamais zero.

## Matrice minimale de runs P8

| sample_kind | Objet |
| --- | --- |
| DET_REPEAT_A / DET_REPEAT_B | Deux runs consecutifs du meme contexte |
| DET_REENTRY | Quitter puis revisiter la meme fenetre |
| DET_FRESH_SESSION | Rejouer le contexte apres nouvelle session |
| DET_ORDER | Permuter chunks, candidats et volumes d'entree |
| VAR_SEED | Changer seulement spawn_seed_value |
| VAR_TABLE | Changer seulement distribution_table_version |
| VAR_EXCLUSION | Changer seulement exclusion_volume_version |
| VAR_GRID | Changer seulement world_grid_version |
| WINDOW_CENTER | C25_25 sur grille 50x50 |
| WINDOW_NW | C00_00 |
| WINDOW_SE | C49_49 |
| WINDOW_DENSEST | Recherche exhaustive des 2500 centres |
| EXC_BEARDEN/WATER/CLIFF/EVENT/OVERLAP | Couverture positive des volumes |
| PERF_STEADY | 300 frames apres warmup |
| PERF_SWITCH | 20 changements de fenetre |
| PERF_STRESS_COLD/WARM | 1 cold, puis 5 warm |

Chaque sample_kind contient le contexte complet et ses propres gates. Il est
interdit de reutiliser un PASS d'un autre harness sans relier build_fingerprint,
context_digest et measurement_scope.

## Evaluation des gates

Chaque sous-gate vaut PASS, FAIL, NOT_MEASURED ou MEASUREMENT_INVALID:

| Gate | Condition PASS |
| --- | --- |
| P8_SEED_HASH | Tous les tests seed/contexte/hash passent sans mismatch. |
| P8_CHUNKS | Centre, coins, densest, caps chunk/fenetre et catalogue passent. |
| P8_CACHE | Cache terrain <=96, stress sans mutation chunkCache, pools sans overflow. |
| P8_ALLOCATIONS | Tous les scopes respectent leurs plafonds et les scopes 0 B sont a zero. |
| P8_TIME | Max simulation <=4 ms et baseline stress sans regression. |
| P8_EXCLUSIONS | Toutes les fixtures positives passent sans accepte interdit ni rejet silencieux. |
| P8_AUTHORITY | Tous les compteurs officiels et serveur sont a zero. |

Regles:

- NOT_MEASURED ou MEASUREMENT_INVALID bloque le gate parent;
- aucune moyenne ne peut masquer un maximum au-dessus d'un plafond dur;
- WARNING ne remplace pas PASS ou FAIL;
- un recu final liste valeurs observees, seuils et digests, pas seulement PASS;
- BUILDER_C_P7_AUDIT ne devient PASS que si les sept sous-gates sont PASS.

## Format de recu attendu

Le recu P8 doit fournir au minimum:

1. build_fingerprint et reference_environment_id;
2. contexte complet, seed reel et toutes les versions;
3. generator_hash avec algorithme et les cinq digests SHA-256;
4. comptes candidats/acceptes/rejetes et conservation;
5. chunks centre/NW/SE/densest et caps par famille;
6. caches before/after/peak et pools;
7. allocations par scope;
8. p50/p95/max temps CPU;
9. hits d'exclusion par kind/famille et rejets structures;
10. violations distance/budget;
11. compteurs d'autorite;
12. statut de chaque sous-gate.

## Gates de handoff

WORLD_MAP_SPAWN_INSPECTOR_TELEMETRY_SPEC=P8_READY
READY_FOR_P8_TELEMETRY=YES
BUILDER_C_P7_AUDIT=CONDITIONAL_PASS

Ce handoff autorise uniquement l'ajout de mesures locales read-only et leur
preuve. Il n'autorise aucune mutation de terrain, generation d'art 50x50,
connexion serveur ou validation d'etat officiel.
