# Resource Spawn Economy Config Report

Date locale: 2026-07-15

## Portee

Transformation de `Docs/WorldMapRuntimeEntitiesWave2/ResourceSpawnEconomySpec.md`
en donnees consommables localement.

Fichiers produits:

- `artifacts/WorldMapRuntimeEntitiesWave2/config/resource_spawn_economy_preview_v1.json`
- `artifacts/WorldMapRuntimeEntitiesWave2/config/resource_spawn_economy_preview_v1.schema.json`

Contraintes respectees:

- Aucun fichier Unity modifie.
- Aucun PNG modifie.
- Aucun APK modifie.
- Aucun terrain, master terrain, BearDen source, serveur, remote ou donnee
  officielle modifie.
- Configuration locale uniquement: `server=false`, `official=false`,
  `official_gain=false`.

## Checkpoint de reprise Wave2

Identifiant: `W2-RESOURCE-ECONOMY-QA-FINAL-2026-07-15`

Etat: `QA_COUNTER_REVIEWED_LOCAL_CONFIG`

Le checkpoint publie couvre la specification economie ressources et sa
traduction en donnees locales consommables. Le perimetre est ferme pour le
prototype local: les tables, la validation structuree et les garde-fous
d'autorite sont presents dans les fichiers listes ci-dessus.

La contre-revue QA R3 a relu le JSON et le schema associe avec le parseur
structure. Le perimetre reste local, preview et non officiel.

## Contre-revue QA R3

Findings et corrections:

- `QA-001` ferme: les gates du JSON etaient encore `PENDING` malgre la
  validation effective. Ils sont maintenant publies en `PASS`/`YES`.
- `QA-002` ferme: le schema acceptait un objet `gates` sans cles ni statuts
  imposes. Les six cles et leurs valeurs finales sont maintenant requises.
- `QA-003` ferme: le schema limitait le tableau a sept elements mais ne
  garantissait pas les sept `kind`. Il impose maintenant les sept kinds et
  l'unicite des elements.

Controles QA executes:

```text
JSON_PARSE=PASS
SCHEMA_JSON_PARSE=PASS
SCHEMA_GATES_STRICT=PASS
SCHEMA_RESOURCE_SET_STRICT=PASS
CONFIG_PARSE=PASS
RESOURCE_7_R1_R3=PASS
SPAWN_BUDGETS=PASS
EXCLUSIONS=PASS
NEGATIVE_CASES=PASS
READY_FOR_RESOURCE_CONFIG_INTEGRATION=YES
```

Limite de l'environnement QA: aucun moteur externe `jsonschema` ou `ajv`
n'est installe. Le verdict repose donc sur le parseur JSON et les assertions
structurees locales, incluant les contraintes QA durcies du schema.

Verdict final local: `QA_FINAL_LOCAL=PASS`

Preuve de validation au checkpoint:

```text
JSON_PARSE=PASS
SCHEMA_JSON_PARSE=PASS
CONFIG_PARSE=PASS
RESOURCE_7_R1_R3=PASS
SPAWN_BUDGETS=PASS
EXCLUSIONS=PASS
NEGATIVE_CASES=PASS
READY_FOR_RESOURCE_CONFIG_INTEGRATION=YES
```

Le prochain consommateur peut charger le JSON et controler sa forme avec le
schema associe. Toute integration ulterieure doit conserver
`server=false`, `official=false` et `official_gain=false` tant qu'une
autorite serveur et une economie officielle ne sont pas explicitement
specifiees.

## Contenu de la configuration

La configuration `resource_spawn_economy_preview_v1.json` contient:

- 7 ressources: `nectar`, `pollen`, `water`, `wax`, `honey`,
  `royal_jelly`, `propolis`.
- Tiers R1/R2/R3 avec tokens `poor`, `medium`, `rich`.
- Poids globaux par ressource.
- Poids R1/R2/R3 par ressource.
- Capacites preview R1/R2/R3 par ressource.
- Multiplicateurs de biome pour `flower_meadow`, `grove`, `wet_edge`,
  `wild_hive`, `neutral_clearing`, `dry_scrub`, `event_reserved`.
- Classes de chunk `sparse`, `standard`, `rich`, `proof_anchor`, `blocked`.
- Caps de fenetre active 5x5.
- Respawn demo avec jitter deterministe.
- Distances minimales.
- Exclusions BearDen, Water, Cliff, Event, Reserved.
- Contentions preview et locks locaux.
- Anti-farm preview.
- Migration logique 25x25 -> 50x50.
- 15 cas negatifs Wave2.
- Champs de seed/version et format d'ID preview.

## Validation structuree

Commande de validation executee avec le Python embarque Codex:

```text
JSON_PARSE=PASS
JSON_SCHEMA=SKIPPED_JSONSCHEMA_NOT_INSTALLED
CONFIG_PARSE=PASS
RESOURCE_7_R1_R3=PASS
SPAWN_BUDGETS=PASS
EXCLUSIONS=PASS
NEGATIVE_CASES=PASS
READY_FOR_RESOURCE_CONFIG_INTEGRATION=YES
```

Note:

- Le module Python `jsonschema` et le module Node `ajv` ne sont pas installes
  dans l'environnement local.
- Le fichier schema a ete parse comme JSON valide.
- Les gates ci-dessous proviennent donc d'un parseur structure maison qui charge
  le JSON, charge le schema JSON, puis verifie les invariants metier requis.

## Gates verifies

| Gate | Resultat | Preuve |
| --- | --- | --- |
| `CONFIG_PARSE` | PASS | JSON config parse, JSON schema parse, champs obligatoires controles |
| `RESOURCE_7_R1_R3` | PASS | 7 kinds exacts et chaque ressource contient R1/R2/R3 en poids et capacites |
| `SPAWN_BUDGETS` | PASS | Caps fenetre 25 chunks / 75 ressources, classes chunk et R3 caps presents |
| `EXCLUSIONS` | PASS | Volumes BearDen/Water/Cliff/Event/Reserved presents avec raisons de rejet |
| `NEGATIVE_CASES` | PASS | `W2-NEG-001` a `W2-NEG-015` presents, uniques et ordonnes |
| `READY_FOR_RESOURCE_CONFIG_INTEGRATION` | YES | Config locale non officielle consommable pour prototype |

## Autorite locale

Champs top-level verifies:

```text
server=false
official=false
official_gain=false
```

Champs d'autorite verifies:

```text
client_official_state_forbidden=true
no_remote=true
no_unity_scene_mutation=true
no_png_mutation=true
no_apk_mutation=true
no_terrain_mutation=true
```

Cette configuration n'autorise ni economie officielle, ni inventaire officiel,
ni respawn officiel, ni persistence serveur, ni anti-cheat production.

## Details des tables

Ressources:

| Kind | Poids global | Caps specifiques |
| --- | ---: | --- |
| `nectar` | 22 | Aucun cap specifique |
| `pollen` | 22 | Aucun cap specifique |
| `water` | 14 | 16 par fenetre, point eau valide requis |
| `wax` | 12 | Aucun cap specifique |
| `honey` | 12 | 12 par fenetre |
| `royal_jelly` | 8 | 6 par fenetre, collecte 1-4 |
| `propolis` | 10 | Aucun cap specifique |

Caps fenetre:

| Cap | Valeur |
| --- | ---: |
| `active_chunks_5x5` | 25 |
| `accepted_resources` | 75 |
| `accepted_r3_resources` | 18 |
| `accepted_royal_jelly` | 6 |
| `accepted_honey` | 12 |
| `accepted_water` | 16 |

Distances minimales couvertes:

- `hive-resource`
- `resource-resource`
- `resource-bestiary`
- `resource-event`
- `resource-BearDen-boundary`
- `resource-world-edge`
- `R3-R3`
- `R3-royal_jelly`
- `royal_jelly-royal_jelly`
- `water-water`

## Cas negatifs inclus

La configuration inclut:

- seed manquant;
- contexte officiel interdit;
- ressource dans BearDen;
- ressource terrestre dans eau;
- ressource sur falaise;
- ressource dans evenement reserve;
- deux R3 sous distance minimale;
- quantite negative;
- collecte sur noeud epuise;
- collecte pendant lock;
- regeneration excessive meme seed/fenetre;
- reprojection hors 50x50;
- variation de table sans changement digest;
- overlay diagnostic actif;
- tentative de gain officiel.

## Verdict

- `CONFIG_PARSE=PASS`
- `RESOURCE_7_R1_R3=PASS`
- `SPAWN_BUDGETS=PASS`
- `EXCLUSIONS=PASS`
- `NEGATIVE_CASES=PASS`
- `SERVER_FALSE=PASS`
- `OFFICIAL_GAIN_FALSE=PASS`
- `UNITY_PNG_APK_MODIFIED=NO`
- `READY_FOR_RESOURCE_CONFIG_INTEGRATION=YES`

La configuration est prete pour une integration locale/proof-first. Elle ne
valide pas une economie officielle ou une autorite serveur.
