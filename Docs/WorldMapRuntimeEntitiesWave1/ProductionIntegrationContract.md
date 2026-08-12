# WorldMap Runtime Entities Wave1 - Production Integration Contract

Date locale: 2026-07-15

## Objet

Contrat de transition entre le laboratoire local/demo WorldMap et une future integration production autoritaire. Ce document ne publie rien et n'active aucun serveur.

## Principes obligatoires

- Le client Unity affiche et previsualise; il ne decide jamais l'etat officiel.
- Le serveur futur sera autoritaire pour spawn, quantites, respawn, combat, recompenses et persistence.
- Les sprites restent decouples des factions: faction = overlay runtime separe.
- Le terrain Wave5/50x50 reste un support visuel; les entites runtime ne sont jamais peintes dans le terrain.
- Toute migration 25x25 -> 50x50 doit conserver des identifiants logiques stables, pas des coordonnees d'image.

## Spawn seedé futur

Entree minimale:

- `world_id`
- `server_id`
- `season_id`
- `chunk_id`
- `entity_family`: hive/resource/bestiary/event
- `spawn_seed_version`

Sortie minimale:

- `entity_id`
- `entity_type`
- `world_coord`
- `tier_or_level`
- `variant`
- `spawn_state`
- `authority_version`

Regle:

- Le seed peut proposer, mais la validation finale appartient au serveur.
- Les zones reservees comme BearDen ou futurs evenements ultimes exposent des volumes d'exclusion.

## Respawn et quantites

Ressource officielle:

- `capacity`
- `remaining`
- `depleted_at`
- `respawn_at`
- `respawn_rule_id`
- `collector_lock_until`

Regles:

- Le client peut animer `remaining` localement uniquement apres confirmation serveur.
- Un mode preview/local peut simuler, mais doit porter `official_gain=false`.
- Les quantites pauvre/moyen/riche doivent etre derivees d'une table versionnee.

## Combat solo/raid

Entree officielle:

- `attacker_party_id`
- `target_entity_id`
- `combat_mode`: solo/raid
- `composition_snapshot`
- `server_time`
- `combat_rule_version`

Sortie officielle:

- `combat_id`
- `result`
- `damage_report`
- `loss_report`
- `reward_grants`
- `cooldowns`
- `audit_hash`

Regles:

- Le client ne calcule pas le resultat officiel.
- La simulation locale/demo peut rester deterministe pour UX, mais elle ne cree ni loot ni progression.
- T1..T4 peuvent etre solo; T5..T7 doivent demander une composition raid ou cooperative selon table serveur.

## Mapping niveau/classe ruche

Table visuelle actuelle:

- Niveau 1/4/7/9: H1 neutre.
- Niveau 10: H2 par classe.
- Niveau 20/35/50: H3 par classe.
- Niveaux intermediaires: palier inferieur deterministe.

Production:

- `hive_level`
- `hive_class`
- `visual_tier`
- `sprite_family`
- `faction_overlay`
- `skin_override_optional`

Regles:

- La classe officielle vient du serveur.
- Le client resout seulement le sprite a partir du snapshot autorise.
- Les overlays faction/alliance/guerre restent des couches separees.

## Sauvegarde et migration 25x25 -> 50x50

Identifiants a conserver:

- `world_id`
- `entity_id`
- `chunk_id_logical`
- `world_coord_normalized`
- `spawn_seed_version`

Migration conseillee:

1. Geler les entites 25x25 dans un snapshot versionne.
2. Convertir `world_coord` vers une coordonnee normalisee 0..1 sur la zone jouable.
3. Reprojeter sur 50x50 via tables d'origine/taille versionnees.
4. Revalider exclusions evenements, eau, falaises et zones speciales.
5. Ecrire `migration_from_world_version` et `migration_audit_hash`.

Interdits:

- Deriver l'etat officiel depuis les pixels terrain.
- Sauver des positions uniquement en coordonnees ecran.
- Peindre des ressources ou bestiaires dans le master terrain.

## Handoff technique

Etat local actuel pret pour demo:

- Mission 1 LAB LOCAL: PASS.
- HIVE_RUNTIME_PROGRESSION: PASS.
- RESOURCE_INTERACTION_STAGE: PASS.
- BESTIARY_INTERACTION_STAGE: PASS.
- FINAL_VISUAL_SMOKE_QA: PASS_WITH_NOTES.

Reserve avant production:

- Definir tables serveur officielles.
- Definir schema persistence.
- Definir anti-cheat/audit.
- Definir migration world-size.
- Faire QA device/player-facing hors batch avant release.
