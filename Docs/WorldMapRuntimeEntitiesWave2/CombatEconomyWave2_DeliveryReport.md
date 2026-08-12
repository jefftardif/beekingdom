# Combat/Economy Wave2 - Delivery Report

Date locale: 2026-07-15
Role: Combat/Economy Wave2
Perimetre: rapport local uniquement
Verdict: PASS_WITH_REPORT_ONLY_RESERVES

## Objet

Ce rapport consolide les specifications Combat/Economy Wave2 attendues pour:

- ruche test;
- ruche ennemie;
- soldats;
- collecte;
- raids;
- spawns deterministes.

Il consomme les deux sources locales Wave2 disponibles:

- `Docs/WorldMapRuntimeEntitiesWave2/CombatBalanceLocalLabSpec.md`
- `Docs/WorldMapRuntimeEntitiesWave2/ResourceSpawnEconomySpec.md`

Ce rapport ne lance pas Unity, ne modifie pas le runtime, ne cree aucun serveur, ne produit aucun PNG, ne produit aucun APK et ne valide aucun comportement officiel/live.

## Frontiere d'autorite

Toute sortie Combat/Economy Wave2 reste locale et preview.

Valeurs obligatoires:

```text
server=false
official=false
official_gain=false
local_only=true
reward_grants=[]
inventory_delta={}
xp_delta=0
progression_delta=0
persistence_scope=session_preview
```

Tout chemin qui introduit un gain officiel, une progression, une persistence serveur, une recompense, un matchmaking ou un adversaire live est hors scope et doit etre refuse.

## Sources consommees

### Combat

`CombatBalanceLocalLabSpec.md` donne:

- regle cible `local_combat_balance_wave2_preview_v1`;
- verdict source `READY_FOR_LOCAL_COMBAT_BALANCE_PROTOTYPE=YES`;
- coexistence avec `legacy_hive_duel_v1`;
- deux ruches test stables: `PLAYER_TEST_HIVE` et `ENEMY_TEST_HIVE`;
- niveaux bornes 1..50;
- classes `Neutral`, `RoyalGuard`, `Striker`, `Nurturer`, `Scout`, `Alchemist`;
- unites `soldiers`, `guards`, `scouts`, `workers`;
- T1..T4 en `solo_local`;
- T5..T7 en `raid_local`;
- score, PV, degats, pertes et cooldown preview deterministes;
- matrice de tests LCB-001 a LCB-032.

### Economy / resources

`ResourceSpawnEconomySpec.md` donne:

- schema `world_map_resource_economy_preview_v1`;
- seed version `resource_spawn_v2`;
- familles `nectar`, `pollen`, `water`, `wax`, `honey`, `royal_jelly`, `propolis`;
- tiers R1/R2/R3;
- biomes, profils de densite, quantites, caps, exclusions et distances;
- contention de spawn et de collecte;
- respawn demo local avec horloge injectable;
- anti-farm preview;
- migration logique 25x25/50x50;
- matrice negative RSE-NEG-001 a RSE-NEG-022;
- verdict source `READY_FOR_LOCAL_RESOURCE_ECONOMY_PROTOTYPE=YES`.

## Specification consolidee

### 1. Ruche test

La ruche test est l'origine locale des previews de collecte et de combat.

Champs requis:

| Champ | Regle |
|---|---|
| `hive_id` | `PLAYER_TEST_HIVE` ou ID stable local equivalent. |
| `level` | Borne 1..50. |
| `class` | Une des six classes locales, avec `Neutral` effectif sous niveau 10. |
| `soldiers` | Entier 0..999999. |
| `guards` | Entier 0..999999. |
| `scouts` | Entier 0..999999. |
| `workers` | Entier 0..999999. |
| `health` | Borne 0..`max_health`. |
| `max_health` | Borne 1..999999. |
| `server` | Toujours `false`. |
| `official_gain` | Toujours `false`. |

La ruche test peut produire:

- preview combat sans mutation;
- application combat locale explicite;
- collecte preview de ressource;
- inspection locale.

Elle ne peut pas produire:

- gain officiel;
- inventaire officiel;
- XP;
- progression;
- sauvegarde officielle;
- raid live.

### 2. Ruche ennemie

La ruche ennemie Wave2 est une cible locale de test ou une cible bestiaire assimilable a une ruche cible. Elle n'est jamais un joueur reel.

Champs requis:

| Champ | Regle |
|---|---|
| `hive_id` | `ENEMY_TEST_HIVE`, `target_family=test_hive`, ou cible bestiaire locale stable. |
| `target_family` | `bestiary` ou `test_hive`. |
| `tier` | 1..7. |
| `target_health` | Borne 0..`target_max_health`. |
| `target_max_health` | Selon matrice T1..T7 ou preset local. |
| `combat_mode` | T1..T4 `solo_local`; T5..T7 `raid_local`. |
| `server` | Toujours `false`. |
| `official_gain` | Toujours `false`. |

Regle de refus:

- si la cible porte une autorite live, serveur ou officielle, l'action echoue avant tout calcul avec violation d'autorite;
- si `target_health=0`, l'application retourne `target_already_defeated` sans cooldown ni perte.

### 3. Soldats et score local

Wave2 utilise quatre compteurs locaux:

| Unite | Role |
|---|---|
| `soldiers` | Corps d'attaque principal. |
| `guards` | Defense, mitigation et roles de raid. |
| `scouts` | Reconnaissance, roles de raid et selection locale. |
| `workers` | Logistique, demi-score, pas de pertes directes Wave2. |

Score de composition:

```text
L = floor(sum(wing.level) / n)
S = sum(wing.soldiers)
G = sum(wing.guards)
E = sum(wing.scouts)
O = sum(wing.workers)

available_score = S + G + E + floor(O / 2) + 2 * L
```

Les classes ne changent pas le score. Elles modifient degats, PV recus, pertes et cooldown selon la table source.

### 4. Matrice combat et raids

Combat local:

| Tier | Niveau | Mode | Ailes | Score requis | PV cible | CD base |
|---|---:|---|---:|---:|---:|---:|
| T1 | 1..4 | solo_local | 1 | 28 | 120 | 5s |
| T2 | 5..9 | solo_local | 1 | 56 | 220 | 8s |
| T3 | 10..14 | solo_local | 1 | 84 | 360 | 12s |
| T4 | 15..19 | solo_local | 1 | 112 | 560 | 18s |
| T5 | 20..29 | raid_local | 2 | 200 | 900 | 30s |
| T6 | 30..34 | raid_local | 3 | 264 | 1300 | 45s |
| T7 | 35..50 | raid_local | 4 | 336 | 1800 | 60s |

Raids:

- T5..T7 exigent `raid_local`;
- les ailes virtuelles sont locales et ne representent aucun joueur connecte;
- T7 doit conserver l'ancre `required=336`, `available=456`, `readiness_bp=13571`;
- un raid produit preview, pertes et cooldown local seulement;
- aucune recompense officielle n'est generee.

Ordre de portes:

1. regle et tier valides;
2. invariants autorite forces;
3. attaquant et cible presents;
4. PV attaquant et cible valides;
5. pas d'action en vol;
6. cooldown local termine;
7. mode conforme;
8. nombre d'ailes et niveaux conformes;
9. classes conformes;
10. minima d'unites atteints;
11. score disponible >= score requis.

Si une porte echoue:

```text
gate=hold
outcome=blocked
projected_target_damage=0
projected_losses=0
projected_cooldown_seconds=0
official_gain=false
server=false
```

### 5. Collecte

La collecte concerne les noeuds de ressources locaux issus de `ResourceSpawnEconomySpec.md`.

Ressources couvertes:

- nectar;
- pollen;
- water;
- wax;
- honey;
- royal_jelly;
- propolis.

Etats autorises:

```text
available -> locked -> available
available|locked -> depleted -> cooldown -> available
cooldown -> suppressed -> cooldown|available
```

Receipt minimal:

```text
node_id
lineage_id
actor_preview_id
request_tick
requested_amount
simulated_decrement
remaining_after
state_after
collector_lock_until_tick
respawn_due_tick_optional
official=false
official_gain=false
inventory_delta={}
reward_grants=[]
```

La collecte:

- diminue `remaining` localement;
- peut afficher `simulated_collected`;
- ne credite aucun inventaire officiel;
- ne cree aucun craft, classement, progression ou gain.

### 6. Spawns deterministes

Le spawn Wave2 est seed-preview, pas officiel.

Contexte requis:

- `world_id`;
- `world_grid_version`;
- `season_id`;
- `spawn_seed_value`;
- `spawn_seed_version=resource_spawn_v2`;
- versions de tables;
- chunk logique;
- biome;
- profil de densite;
- fenetre active canonique;
- `source_kind=seed_preview`;
- `official=false`.

Regles deterministes:

- seed obligatoire, non vide, max 128 octets UTF-8;
- draws SHA-256 domaines;
- aucun `GetHashCode()`;
- aucun random global;
- aucune heure murale;
- aucun frame count;
- aucun ordre de fichier ou camera.

IDs:

```text
preview:{world_id}:{world_grid_version}:resource:{chunk_id_logical}:r{slot}:{spawn_seed_version}:{seed_digest16}:{distribution_table_version}
```

Hashes:

- `base_distribution_hash` pour la distribution stable;
- `runtime_availability_hash` pour remaining, etat, lease, respawn, chaleur et tick demo.

Une collecte ou un cooldown ne peut jamais changer `base_distribution_hash`.

### 7. Respawn et anti-farm

Le respawn demo:

- utilise `demo_clock_tick` a 10 ticks/seconde;
- ne depend pas de l'heure murale;
- conserve ID, tier, position et capacity;
- remet `remaining=capacity` seulement a echeance valide;
- ne relocalise jamais un noeud invalide;
- ne tire aucun remplacement opportuniste.

L'anti-farm preview:

- limite le debit acteur;
- ajoute de la chaleur par depletion;
- retarde ou supprime temporairement le respawn local;
- ne modifie ni famille, ni tier, ni capacity, ni position;
- ne change jamais `base_distribution_hash`;
- n'est pas un anti-cheat production.

### 8. Caps, distances et exclusions

Caps principaux:

| Portee | Cap |
|---|---:|
| Ressources par chunk | 3 |
| Meme famille par chunk | 1 |
| R3 par chunk | 1 |
| Gelée royale par chunk | 1 |
| Chunks actifs fenetre | 25 |
| Ressources par fenetre 5x5 | 75 |
| R3 par fenetre | 8 |
| Gelée royale fenetre | 3 |
| Gelée royale R3 fenetre | 1 |
| Propolis R3 fenetre | 2 |

Exclusions:

- hors rectangle jouable;
- BearDen;
- Water core;
- Cliff;
- Event/Reserved;
- NoResource.

Distances minimales notables:

- ressource-ressource base: 90;
- meme famille: 110;
- au moins une R3: 120;
- gelée royale: 150;
- ruche-ressource: 105;
- bestiaire-ressource: 80.

Un candidat rejete n'est pas deplace, pousse ou regenere.

## Tests attendus

### Combat

La conformite combat exige LCB-001 a LCB-032, notamment:

- invariants autorite forces;
- presets seuil T1..T7;
- modes T1..T4 solo et T5..T7 raid;
- ancre T7 336/456;
- preview sans mutation;
- application unique;
- `stale_preview`;
- cooldown local;
- absence de mutation stock, ressource, XP, loot ou progression;
- telemetrie finissant par `official_gain=false server=false`.

### Resource economy

La conformite ressource exige RSE-NEG-001 a RSE-NEG-022, notamment:

- seed/version obligatoires;
- determinisme hash stable;
- version bump obligatoire sur table modifiee;
- biome absent refuse;
- eau sans rive refusee;
- exclusions et distances stables;
- caps respectes;
- contention atomique;
- horloge monotone;
- anti-farm non reset par pan/unload/seed swap;
- aucune violation d'autorite officielle.

## Checkpoint PASS/FAIL

| Gate | Resultat | Justification |
|---|---|---|
| Sources Wave2 trouvees | PASS | Les deux specs sources sont presentes dans `Docs/WorldMapRuntimeEntitiesWave2`. |
| Sources consommees | PASS | Les invariants, matrices, tests et verdicts source sont repris dans ce rapport. |
| Ruche test | PASS | Champs, autorite, PV, unites et usages locaux specifies. |
| Ruche ennemie | PASS | Cible test/bestiary, PV, tier, mode et refus live specifies. |
| Soldats | PASS | Soldats, gardiennes, eclaireuses, ouvrieres et score consolides. |
| Collecte | PASS | Etats, receipt, decrement local et absence de gain officiel specifies. |
| Raids | PASS | T5..T7 raid local, ailes virtuelles, ancre T7 et portes specifies. |
| Spawns deterministes | PASS | Seed, versions, IDs, hashes et interdictions de hasard non deterministe specifies. |
| Rapports seulement | PASS | Aucun Unity, serveur, PNG, APK ou runtime produit. |
| Prototype implemente | FAIL_EXPECTED | Non demande; sources disent prototype futur/local seulement. |
| Validation production | FAIL_EXPECTED | Explicitement interdite par les specs sources. |

## Verdict

`PASS_WITH_REPORT_ONLY_RESERVES`

Le livrable rapport attendu est publie et couvre Combat/Economy Wave2 pour ruche test, ruche ennemie, soldats, collecte, raids et spawns deterministes. Les deux sources locales ont ete consommees.

Reserves maintenues:

- aucun prototype implemente;
- aucun test runtime execute;
- aucune validation Unity;
- aucune validation serveur/live;
- aucune validation economie officielle;
- aucun PNG ou APK produit.

Le prochain travail autorise est un prototype local opt-in ou une matrice de tests locale contre ces specifications, toujours avec `server=false` et `official_gain=false`.
