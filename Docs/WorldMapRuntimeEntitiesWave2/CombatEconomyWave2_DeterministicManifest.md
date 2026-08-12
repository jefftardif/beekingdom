# Combat/Economy Wave2 - Deterministic Manifest

Date locale: 2026-07-15
Statut: rapport manifeste local, non executable
Verdict: PASS_REPORT_ONLY

## Objet

Ce manifeste fige un scenario canonique Combat/Economy Wave2 pour reprise, tests locaux futurs et revue QA. Il couvre:

- ruche test;
- ruche ennemie locale;
- soldats, gardiennes, eclaireuses et ouvrieres;
- collecte de ressources;
- raid local;
- spawns deterministes.

Ce rapport ne lance pas Unity, ne cree pas de JSON runtime, ne modifie aucun serveur, ne produit aucun PNG et ne produit aucun APK.

## Sources

- `Docs/WorldMapRuntimeEntitiesWave2/CombatBalanceLocalLabSpec.md`
- `Docs/WorldMapRuntimeEntitiesWave2/ResourceSpawnEconomySpec.md`
- `Docs/WorldMapRuntimeEntitiesWave2/CombatEconomyWave2_DeliveryReport.md`

## Invariants

Toutes les lignes de ce manifeste portent les invariants suivants:

```text
server=false
official=false
official_gain=false
local_only=true
source_kind=seed_preview
reward_grants=[]
inventory_delta={}
xp_delta=0
progression_delta=0
persistence_scope=session_preview
```

Un consommateur futur doit rejeter le scenario si une valeur officielle ou live apparait.

## Scenario canonique

| Champ | Valeur |
|---|---|
| `scenario_id` | `wave2_combat_economy_manifest_001` |
| `world_id` | `BK-DEMO-WORLD-WAVE5-LOCAL` |
| `world_grid_version` | `grid_25x25_v1` |
| `active_window_id` | `WAVE2_C32_32_R2_5X5` |
| `active_chunk_ids_sorted` | `C30_30..C34_34` en ordre ordinal ligne puis colonne |
| `season_id` | `local_preview_summer_v1` |
| `spawn_seed_value` | `Wave2CombatEconomy:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:PLAYER_TEST_HIVE:2026-07-15` |
| `spawn_seed_version` | `resource_spawn_v2` |
| `seed_digest_sha256` | `75de53be553e17c5ddd99a152dfa952ca2b2720407bf45036c804a87c9c48296` |
| `seed_digest16` | `75de53be553e17c5` |
| `combat_rule_version` | `local_combat_balance_wave2_preview_v1` |
| `resource_schema_version` | `world_map_resource_economy_preview_v1` |
| `distribution_table_version` | `resource_distribution_preview_v2` |
| `quantity_table_version` | `resource_quantity_preview_v1` |
| `respawn_rule_version` | `resource_respawn_demo_v1` |
| `cap_rule_version` | `resource_caps_preview_v1` |
| `hash_algorithm_version` | `sha256_canonical_utf8_v1` |
| `scenario_manifest_digest` | `7a7cb135a3162e2ab26cc8bf1380d2f1f84b17f3b583d9782099570b0e1628d4` |

Le digest du scenario est un point de reprise documentaire. Il couvre le seed, la ruche test, la ruche ennemie, les sept noeuds ressources, l'exemple de collecte et l'exemple de raid ci-dessous.

## Ruche test

| Champ | Valeur |
|---|---|
| `hive_id` | `PLAYER_TEST_HIVE` |
| `target_family` | `test_hive` |
| `chunk_id_logical` | `C32_32` |
| `world_coord_q1e6` | `500000,500000` |
| `level` | `35` |
| `class_profile` | `raid_virtual_wings_t7` |
| `soldiers` | `140` |
| `guards` | `86` |
| `scouts` | `70` |
| `workers` | `180` |
| `health` | `1200` |
| `max_health` | `1200` |
| `server` | `false` |
| `official_gain` | `false` |

Score attendu:

```text
L = 35
S = 140
G = 86
E = 70
O = 180
available_score = 140 + 86 + 70 + floor(180 / 2) + 2 * 35
available_score = 456
```

## Ruche ennemie locale

| Champ | Valeur |
|---|---|
| `hive_id` | `ENEMY_TEST_HIVE` |
| `target_family` | `test_hive` |
| `chunk_id_logical` | `C34_34` |
| `world_coord_q1e6` | `596000,596000` |
| `tier` | `T7` |
| `combat_mode` | `raid_local` |
| `target_health` | `1800` |
| `target_max_health` | `1800` |
| `server` | `false` |
| `official_gain` | `false` |

Cette ruche ennemie est une cible locale de laboratoire. Elle ne represente pas un joueur reel, un matchmaking, un raid live ou une guerre officielle.

## Ailes virtuelles T7

Les ailes sont locales et virtuelles. Elles ne representent aucun joueur connecte.

| Aile | Classe | Level | Soldiers | Guards | Scouts | Workers | Weight |
|---|---|---:|---:|---:|---:|---:|---:|
| `wing_0` | `RoyalGuard` | 35 | 35 | 22 | 18 | 45 | 97 |
| `wing_1` | `Nurturer` | 35 | 35 | 22 | 18 | 45 | 97 |
| `wing_2` | `Scout` | 35 | 35 | 21 | 17 | 45 | 95 |
| `wing_3` | `Alchemist` | 35 | 35 | 21 | 17 | 45 | 95 |

Modificateurs ponderes attendus:

| Modificateur | Valeur bp |
|---|---:|
| `raid_damage_bp` | 10443 |
| `received_hp_modifier_bp` | 9095 |
| `casualty_modifier_bp` | 8817 |
| `cooldown_modifier_bp` | 9744 |

## Spawns ressources acceptes

Les IDs suivent le format Wave2:

```text
preview:{world_id}:{world_grid_version}:resource:{chunk_id_logical}:r{slot}:{spawn_seed_version}:{seed_digest16}:{distribution_table_version}
```

| Node | ID | Chunk | Biome | Profil | Kind | Tier | Capacity | Remaining | Etat | Position q1e6 |
|---|---|---|---|---|---|---|---:|---:|---|---|
| `RES-001` | `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:resource:C32_32:r0:resource_spawn_v2:75de53be553e17c5:resource_distribution_preview_v2` | C32_32 | `flower_meadow` | `standard` | `pollen` | R2 | 84 | 84 | `available` | `510000,492000` |
| `RES-002` | `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:resource:C32_33:r0:resource_spawn_v2:75de53be553e17c5:resource_distribution_preview_v2` | C32_33 | `flower_meadow` | `standard` | `nectar` | R2 | 76 | 76 | `available` | `522000,548000` |
| `RES-003` | `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:resource:C31_32:r0:resource_spawn_v2:75de53be553e17c5:resource_distribution_preview_v2` | C31_32 | `apiary_orchard` | `standard` | `wax` | R1 | 24 | 24 | `available` | `474000,509000` |
| `RES-004` | `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:resource:C33_32:r0:resource_spawn_v2:75de53be553e17c5:resource_distribution_preview_v2` | C33_32 | `wetland_edge` | `standard` | `water` | R2 | 72 | 72 | `available` | `556000,501000` |
| `RES-005` | `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:resource:C32_31:r0:resource_spawn_v2:75de53be553e17c5:resource_distribution_preview_v2` | C32_31 | `apiary_orchard` | `standard` | `honey` | R1 | 22 | 22 | `available` | `506000,462000` |
| `RES-006` | `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:resource:C31_33:r0:resource_spawn_v2:75de53be553e17c5:resource_distribution_preview_v2` | C31_33 | `forest_edge` | `standard` | `propolis` | R3 | 100 | 100 | `available` | `478000,552000` |
| `RES-007` | `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:resource:C33_33:r0:resource_spawn_v2:75de53be553e17c5:resource_distribution_preview_v2` | C33_33 | `apiary_orchard` | `standard` | `royal_jelly` | R1 | 10 | 0 | `cooldown` | `566000,566000` |

Cap check attendu:

| Gate | Resultat |
|---|---|
| Ressources par chunk <= 3 | PASS |
| Meme famille par chunk <= 1 | PASS |
| R3 par chunk <= 1 | PASS |
| Gelee royale par chunk <= 1 | PASS |
| Ressources fenetre <= 75 | PASS, 7 |
| R3 fenetre <= 8 | PASS, 1 |
| Gelee royale fenetre <= 3 | PASS, 1 |
| Propolis R3 fenetre <= 2 | PASS, 1 |

## Respawn local

`RES-007` est l'exemple cooldown:

| Champ | Valeur |
|---|---|
| `depleted_tick` | 920 |
| `respawn_due_tick` | 1400 |
| `respawn_cycle_index` | 1 |
| `farm_heat_cell` | 3 |
| `anti_farm_state` | `none` |
| `base_distribution_hash_changes` | `false` |
| `runtime_availability_hash_changes` | `true` |

La disponibilite change le hash runtime seulement. Elle ne doit jamais changer `base_distribution_hash`.

## Collecte acceptee

| Champ | Valeur |
|---|---|
| `action_id` | `COL-001` |
| `node` | `RES-001` |
| `actor_preview_id` | `actor:local:scout_team_alpha` |
| `request_tick` | 100 |
| `request_nonce` | `col-001-a` |
| `requested_amount` | 12 |
| `simulated_decrement` | 12 |
| `remaining_before` | 84 |
| `remaining_after` | 72 |
| `collector_lock_until_tick` | 120 |
| `state_after` | `available` |
| `official` | `false` |
| `official_gain` | `false` |
| `inventory_delta` | `{}` |
| `reward_grants` | `[]` |

Receipt attendu:

```text
LocalResourceCollectionReceipt COL-001 node=RES-001 request_tick=100 requested_amount=12 simulated_decrement=12 remaining_after=72 state_after=available collector_lock_until_tick=120 official=false official_gain=false inventory_delta={} reward_grants=[]
```

## Collecte refusee

| Champ | Valeur |
|---|---|
| `action_id` | `COL-NEG-001` |
| `node` | `RES-007` |
| `actor_preview_id` | `actor:local:scout_team_beta` |
| `request_tick` | 200 |
| `requested_amount` | 2 |
| `state_before` | `cooldown` |
| `respawn_due_tick` | 1400 |
| `result` | `rejected` |
| `reason` | `ResourceUnavailableCooldown` |
| `simulated_decrement` | 0 |
| `remaining_after` | 0 |
| `official_gain` | `false` |

## Raid accepte

| Champ | Valeur |
|---|---|
| `action_id` | `RAID-001` |
| `rule_version` | `local_combat_balance_wave2_preview_v1` |
| `attacker` | `PLAYER_TEST_HIVE` |
| `target` | `ENEMY_TEST_HIVE` |
| `tier` | `T7` |
| `combat_mode` | `raid_local` |
| `required_score` | 336 |
| `available_score` | 456 |
| `readiness_bp` | 13571 |
| `pressure_bp` | 12142 |
| `projected_target_damage` | 547 |
| `projected_attacker_hp_loss` | 104 |
| `projected_soldier_losses` | 7 |
| `projected_guard_losses` | 2 |
| `projected_scout_losses` | 1 |
| `projected_target_health_after` | 1253 |
| `projected_attacker_health_after` | 1096 |
| `projected_cooldown_seconds` | 59 |
| `gate` | `ready` |
| `outcome` | `engaged` |
| `server` | `false` |
| `official_gain` | `false` |

Telemetry attendue:

```text
combat_preview rule=local_combat_balance_wave2_preview_v1 tier=T7 mode=raid_local required=336 available=456 gate=ready outcome=engaged target_hp_delta=-547 attacker_hp_delta=-104 soldier_loss=7 guard_loss=2 scout_loss=1 cooldown_preview_s=59 official_gain=false server=false
```

## Raid refuse

| Champ | Valeur |
|---|---|
| `action_id` | `RAID-NEG-001` |
| `tier` | `T7` |
| `combat_mode` | `solo_local` |
| `required_score` | 336 |
| `available_score` | 456 |
| `gate` | `hold` |
| `outcome` | `blocked` |
| `reason` | `mode_not_allowed` |
| `projected_target_damage` | 0 |
| `projected_attacker_hp_loss` | 0 |
| `projected_losses` | 0 |
| `projected_cooldown_seconds` | 0 |
| `official_gain` | `false` |
| `server` | `false` |

Telemetry attendue:

```text
combat_preview rule=local_combat_balance_wave2_preview_v1 tier=T7 mode=solo_local required=336 available=456 gate=hold outcome=blocked reason=mode_not_allowed official_gain=false server=false
```

## Oracle source conserve

Le manifeste conserve aussi l'oracle source LCB-015 pour le futur test unitaire de calcul T3:

| Champ | Valeur attendue |
|---|---:|
| Tier | T3 |
| Classe | RoyalGuard |
| Score | 84 |
| Degat cible | 108 |
| Perte PV attaquant | 37 |
| Pertes totales | 2 |
| Pertes soldats/gardiennes/eclaireuses | 2 / 0 / 0 |
| Cooldown | 12 s |
| Outcome | `engaged` |

## Matrice replay

| Replay | Entree | Sortie attendue |
|---|---|---|
| `REPLAY-SEED-001` | Meme seed, versions et fenetre | Memes sept noeuds, memes IDs, memes capacities, meme `scenario_manifest_digest`. |
| `REPLAY-COL-001` | Etat initial + `COL-001` | `RES-001.remaining_after=72`, aucun inventaire officiel. |
| `REPLAY-COL-NEG-001` | Etat initial + `COL-NEG-001` | Rejet cooldown, decrement 0, due tick 1400. |
| `REPLAY-RAID-001` | Etat initial + `RAID-001` | Score 456/336, damage 547, HP loss 104, pertes 7/2/1, cooldown 59. |
| `REPLAY-RAID-NEG-001` | Etat initial + `RAID-NEG-001` | `mode_not_allowed`, zero mutation. |
| `REPLAY-AUTH-001` | Toute ligne avec `official_gain=true` | Hard fail, scenario invalide. |

## Verdict

`PASS_REPORT_ONLY`

Le manifeste deterministe demande est publie sous forme de rapport. Il est assez precis pour une implementation locale future ou une matrice de tests hors runtime officiel. Il ne valide aucune implementation et ne cree aucun artefact de jeu.
