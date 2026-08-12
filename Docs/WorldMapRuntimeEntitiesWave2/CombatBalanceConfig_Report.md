# WorldMap Runtime Entities Wave2 - Combat Balance Config Report

Date locale: 2026-07-15

## Objet

Transformation de `CombatBalanceLocalLabSpec.md` en configuration locale versionnee et consommable.

Aucun fichier Unity, PNG ou APK n'a ete modifie. Aucun serveur, gain officiel, recompense officielle ou persistence officielle n'est active.

## Artefacts produits

- `artifacts/WorldMapRuntimeEntitiesWave2/config/combat_balance_preview_v1.json`
- `artifacts/WorldMapRuntimeEntitiesWave2/config/combat_balance_preview_v1.schema.json`
- `Docs/WorldMapRuntimeEntitiesWave2/CombatBalanceConfig_Report.md`

## Contenu config

Le fichier `combat_balance_preview_v1.json` contient:

- versions: `schema_version=combat_balance_preview.schema.v1`, `config_version=combat_balance_preview.v1`, `rule_version=1`, `data_version=1`;
- autorite locale: `local_only=true`, `server=false`, `official_gain=false`, `remote=false`, `real_data=false`;
- interdits officiels: `official_rewards_allowed=false`, `official_persistence_allowed=false`;
- bornes: tiers 1..7, niveaux ruche 1..50, unites 0..999999, PV 0..999999, duel local damage 12..180, retour 0..80;
- classes: `Neutral`, `RoyalGuard`, `Striker`, `Nurturer`, `Scout`, `Alchemist`;
- formules preview: `solo_power`, `raid_power`, bonus de classe, compatibilite PV/requis/disponible;
- matrice T1-T7: niveaux, classes recommandees, mode `solo_local` ou `raid_local`, variants, compositions, PV, requis, disponible proof;
- pertes locales: ranges win/hold soldats/gardiennes et PV joueur minimum;
- cooldowns preview: action, blessure, respawn;
- politiques negatives: target missing, tier out of bounds, raid required, zero units, target/player already defeated, invalid row;
- cas positifs et negatifs publies.

## Couverture T1-T7

| Tier | Mode | Niveaux | Classes recommandees | PV | Required | Proof available | Cooldown action |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: |
| T1 | solo_local | 1-6 | Neutral, Scout | 135 | 28 | 168 | 20s |
| T2 | solo_local | 7-11 | Scout, Nurturer | 190 | 56 | 216 | 35s |
| T3 | solo_local | 12-17 | Nurturer, Striker | 245 | 84 | 264 | 60s |
| T4 | solo_local | 18-24 | RoyalGuard, Striker | 300 | 112 | 312 | 90s |
| T5 | raid_local | 25-31 | RoyalGuard | 355 | 200 | 360 | 180s |
| T6 | raid_local | 32-39 | Alchemist, RoyalGuard | 410 | 228 | 408 | 300s |
| T7 | raid_local | 40-50 | Alchemist, Striker | 465 | 336 | 456 | 480s |

## Validation structuree

Validation realisee avec parseur JSON structure PowerShell `ConvertFrom-Json`, puis controles semantiques locaux:

- parse du JSON config;
- parse du JSON schema;
- verification versions;
- verification autorite local-only;
- verification des 6 classes attendues;
- verification des 7 tiers attendus;
- verification T1-T4 `solo_local` / `SOLO`;
- verification T5-T7 `raid_local` / `RAID`;
- verification formules `virtual_hp = 80 + tier * 55`;
- verification formules `required = tier * 28 + bonus T5 + bonus T7`;
- verification formules `proof_available = 120 + tier * 48`;
- verification compositions non negatives;
- verification ranges pertes min <= max;
- verification cooldowns preview non negatifs;
- verification cas positifs;
- verification cas negatifs;
- verification `server=false` et `official_gain=false` sur les cas.

## Cas positifs publies

| Case | Attendu | Resultat valide |
| --- | --- | --- |
| `positive_t1_solo_win` | T1 solo win, no server, no gain | result=win server=false official_gain=false |
| `positive_t4_solo_edge` | T4 solo win, no server, no gain | result=win server=false official_gain=false |
| `positive_t7_raid_proof` | T7 raid required 336, available 456, no server, no gain | result=win server=false official_gain=false |

## Cas negatifs publies

| Case | Attendu | Resultat valide |
| --- | --- | --- |
| `negative_t5_solo_rejected` | T5 solo refuse par raid required | result=hold reason=raid_required server=false official_gain=false |
| `negative_zero_units` | aucune unite de combat | result=hold reason=no_combat_units server=false official_gain=false |
| `negative_tier_out_of_bounds` | tier 8 refuse/clamp preview | result=hold reason=tier_out_of_bounds server=false official_gain=false |

## Sortie de validation

```text
CONFIG_PARSE=PASS
COVERAGE_LEVEL_CLASS_TIER=PASS
NEGATIVE_CASES=PASS
LOCAL_ONLY=PASS
POSITIVE_CASES:
positive_t1_solo_win: result=win server=False official_gain=False
positive_t4_solo_edge: result=win server=False official_gain=False
positive_t7_raid_proof: result=win server=False official_gain=False
NEGATIVE_CASES:
negative_t5_solo_rejected: result=hold reason=raid_required server=False official_gain=False
negative_zero_units: result=hold reason=no_combat_units server=False official_gain=False
negative_tier_out_of_bounds: result=hold reason=tier_out_of_bounds server=False official_gain=False
READY_FOR_COMBAT_CONFIG_INTEGRATION=YES
```

## Non-regression locale

- `server=false` est force dans `authority`, les test cases et la telemetrie template.
- `official_gain=false` est force dans `authority`, les policies, les test cases et la telemetrie template.
- `official_rewards_allowed=false` interdit toute recompense officielle.
- `official_persistence_allowed=false` interdit toute persistence officielle.
- T5-T7 restent raid local.
- T1-T4 restent solo local.

## Gates

CONFIG_PARSE=PASS

COVERAGE_LEVEL_CLASS_TIER=PASS

NEGATIVE_CASES=PASS

LOCAL_ONLY=PASS

READY_FOR_COMBAT_CONFIG_INTEGRATION=YES
