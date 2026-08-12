# WorldMap Scenario Data Layer - Technical Contract P6

Date locale: 2026-07-15

## Portee

Mission Builder-C Relay P6: definir un contrat minimal data/autorite pour les scenarios WorldMap sans modifier Unity, PNG, APK, terrain, scene, serveur ou donnees reelles.

Sources autorisees lues:

- `Docs/WorldMapRuntimeEntitiesWave1/ProductionIntegrationContract.md`
- `Docs/BuilderCRelay/WorldMap50x50_RuntimePerformanceContract.md`

Principe central:

- Le client Unity affiche, previsualise, simule en local-demo et mesure les budgets.
- Le client Unity ne calcule jamais un etat officiel.
- Le futur serveur, ou un provider d'autorite equivalent, est seul responsable de spawn officiel, quantites, respawn, combat, recompenses, persistence et audit.

## Schema versionne minimal

Tous les enregistrements persistables doivent porter:

- `schema_version`
- `world_id`
- `world_grid_version`
- `authority_version`
- `official`
- `source_kind`: `local_demo`, `seed_preview`, `server_snapshot`, `migration_snapshot`
- `audit_hash_optional`

Familles supportees:

- `hive`
- `resource`
- `bestiary`
- `event`

Entite commune:

```text
WorldEntityRecord
- schema_version
- entity_id
- entity_family
- entity_type
- world_id
- chunk_id_logical
- logical_coord
- world_coord_normalized
- tier_or_level
- variant
- spawn_state
- spawn_seed_version
- authority_version
- official
```

Extension ruche:

```text
HiveEntityData
- hive_level
- hive_class
- visual_tier
- sprite_family
- faction_overlay
- skin_override_optional
- owner_ref_preview_optional
```

Extension ressource:

```text
ResourceEntityData
- resource_kind
- richness_tier
- capacity
- remaining
- depleted_at_optional
- respawn_at_optional
- respawn_rule_id
- collector_lock_until_optional
```

Extension bestiaire:

```text
BestiaryEntityData
- bestiary_tier
- variant
- combat_access: solo, raid, blocked
- combat_rule_version
- role_hint
```

Extension evenement:

```text
EventEntityData
- event_type
- event_rule_version
- starts_at_optional
- ends_at_optional
- exclusion_volume_ids
- visibility_state
```

## Identifiants stables

`entity_id`:

- Stable entre 25x25 et 50x50.
- Jamais derive d'une position ecran.
- Peut etre derive d'un seed uniquement pour preview locale, mais le serveur futur doit pouvoir confirmer ou remplacer l'id officiel.
- Format recommande: `{world_id}:{family}:{stable_key}:{spawn_seed_version}`.
- Les ids de demo doivent porter un prefixe explicite: `demo:` ou `proof:`.

`chunk_id_logical`:

- Format: `Cxx_yy` pour compatibilite avec les patterns actuels.
- Versionne par `world_grid_version`.
- Ne suffit pas seul a repositionner une entite: il doit etre accompagne de `local_x01/local_y01` et `world_coord_normalized`.

`stable_key`:

- Pour seed preview: hash deterministe de `world_id/server_id/season_id/chunk_id_logical/entity_family/spawn_seed_version/local_slot`.
- Pour serveur: cle officielle fournie dans le snapshot.
- Pour migration: cle source conservee + `migration_from_world_version`.

## Coordonnees et reprojection

Coordonnee logique:

```text
WorldLogicalCoord
- chunk_x
- chunk_y
- local_x01
- local_y01
- chunk_id_logical
- sector_id_optional
```

Coordonnee normalisee:

```text
WorldCoordNormalized
- x01
- y01
- playable_rect_version
```

Regles:

- `x01` et `y01` sont clamps dans `[0, 1]`.
- La source persistable pour migration est `world_coord_normalized`, pas `world_coord` brut.
- `world_coord` peut etre recalcule a partir de la grille cible.
- Les exclusions BearDen, eau, falaises, evenements et zones reservees sont revalidees apres reprojection.

Reprojection 25x25 -> 50x50:

```text
target_x = normalized.x01 * target_grid_width_chunks
target_y = normalized.y01 * target_grid_height_chunks
chunk_x = floor(target_x)
chunk_y = floor(target_y)
local_x01 = frac(target_x)
local_y01 = frac(target_y)
chunk_id_logical = C{chunk_x:00}_{chunk_y:00}
```

Garde-fou bord droit/bas:

- Si `normalized.x01 == 1`, utiliser `chunk_x = width - 1` et `local_x01 = 1`.
- Si `normalized.y01 == 1`, utiliser `chunk_y = height - 1` et `local_y01 = 1`.

## Interface autorite/provider

Le provider local-demo et le provider serveur doivent partager une interface de lecture, mais pas les memes garanties.

```csharp
namespace BeeKingdom.WorldMap.Contracts
{
    public enum WorldAuthorityMode
    {
        LocalDemo,
        SeedPreview,
        ServerAuthoritative
    }

    public readonly struct WorldAuthorityStamp
    {
        public readonly WorldAuthorityMode Mode;
        public readonly string AuthorityVersion;
        public readonly bool Official;
        public readonly string AuditHash;
    }

    public interface IWorldScenarioDataProvider
    {
        bool TryGetWindow(WorldWindowRequest request, out WorldWindowSnapshot snapshot, out WorldDataError error);
        bool TryGetEntity(string entityId, out WorldEntitySnapshot entity, out WorldDataError error);
    }

    public interface IWorldScenarioCommandProvider
    {
        bool TryPreview(WorldScenarioCommand command, out WorldScenarioPreview preview, out WorldDataError error);
        bool TrySubmitOfficial(WorldScenarioCommand command, out WorldScenarioReceipt receipt, out WorldDataError error);
    }
}
```

Regles d'autorite:

- `LocalDemo` peut retourner `official=false` uniquement.
- `SeedPreview` peut proposer des entites ou resultats UX, toujours `official=false`.
- `ServerAuthoritative` seul peut retourner `official=true`.
- Si l'autorite est absente, les boutons officiels doivent etre desactives ou marques preview.
- Les recompenses locales doivent porter `official_gain=false`.

## Pseudo-interfaces C# data

```csharp
public enum WorldEntityFamily
{
    Hive,
    Resource,
    Bestiary,
    Event
}

public readonly struct WorldWindowRequest
{
    public readonly string WorldId;
    public readonly string ServerId;
    public readonly string SeasonId;
    public readonly string WorldGridVersion;
    public readonly string CenterChunkIdLogical;
    public readonly int RadiusChunks;
    public readonly WorldEntityFamily[] Families;
    public readonly int ClientPreviewSeed;
}

public readonly struct WorldWindowSnapshot
{
    public readonly string WorldId;
    public readonly string WorldGridVersion;
    public readonly string BoundsMinChunkId;
    public readonly string BoundsMaxChunkId;
    public readonly WorldEntitySnapshot[] Entities;
    public readonly WorldExclusionVolume[] Exclusions;
    public readonly WorldAuthorityStamp Authority;
}

public readonly struct WorldEntitySnapshot
{
    public readonly string SchemaVersion;
    public readonly string EntityId;
    public readonly WorldEntityFamily Family;
    public readonly string EntityType;
    public readonly string ChunkIdLogical;
    public readonly WorldLogicalCoord LogicalCoord;
    public readonly WorldCoordNormalized NormalizedCoord;
    public readonly int TierOrLevel;
    public readonly int Variant;
    public readonly string SpawnState;
    public readonly string SpawnSeedVersion;
    public readonly WorldAuthorityStamp Authority;
}
```

Scenario commands:

```csharp
public enum WorldScenarioKind
{
    Collecte,
    Duel,
    Raid
}

public readonly struct WorldScenarioCommand
{
    public readonly string CommandId;
    public readonly WorldScenarioKind Kind;
    public readonly string ActorHiveEntityId;
    public readonly string TargetEntityId;
    public readonly string CompositionSnapshot;
    public readonly string ClientIntentId;
    public readonly long ClientObservedUnixMs;
    public readonly WorldAuthorityStamp RequestedAuthority;
}

public readonly struct WorldScenarioPreview
{
    public readonly string PreviewId;
    public readonly bool OfficialGain;
    public readonly string DeterministicHash;
    public readonly string[] UiFeedbackTokens;
    public readonly WorldEntityDelta[] PreviewDeltas;
}

public readonly struct WorldScenarioReceipt
{
    public readonly string ReceiptId;
    public readonly bool Official;
    public readonly string Result;
    public readonly string[] RewardGrants;
    public readonly WorldEntityDelta[] ConfirmedDeltas;
    public readonly string AuditHash;
}
```

Validation errors:

```csharp
public enum WorldDataErrorCode
{
    None,
    UnsupportedSchema,
    InvalidAuthorityMode,
    OfficialClientComputationRejected,
    InvalidEntityId,
    InvalidChunkId,
    InvalidNormalizedCoord,
    ExclusionVolumeHit,
    DensityBudgetExceeded,
    AllocationBudgetExceeded,
    MigrationAuditMissing,
    ServerAuthorityUnavailable
}
```

## Seeds deterministes

Entree seed minimale:

- `world_id`
- `server_id`
- `season_id`
- `chunk_id_logical`
- `entity_family`
- `spawn_seed_version`
- `exclusion_version`

Sortie seed preview:

- `proposed_entities`
- `rejected_candidates`
- `exclusion_hits`
- `deterministic_hash`
- `official=false`

Regles:

- Le seed doit etre stable pour une meme entree.
- Le seed ne valide pas l'etat officiel.
- Le seed ne peut pas accorder loot, progression, perte officielle, cooldown officiel ou persistence.
- Les ids seedes doivent rester remplacables par snapshot serveur.

## Scenarios Collecte, Duel, Raid

Collecte:

- Cible: `resource`.
- Local-demo: peut animer vol, depletion preview, respawn preview.
- Officiel: serveur confirme `remaining`, `collector_lock_until`, recompenses et `respawn_at`.
- Interdit client: calculer un gain officiel depuis `remaining`.

Duel:

- Cible: `bestiary` T1..T4 ou cible explicitement solo.
- Local-demo: peut produire un resultat deterministe UX avec `official=false`.
- Officiel: serveur retourne `combat_id`, `result`, `damage_report`, `loss_report`, `reward_grants`, `cooldowns`, `audit_hash`.
- Interdit client: transformer une simulation locale en progression.

Raid:

- Cible: `bestiary` T5..T7 ou evenement raid.
- Local-demo: peut afficher besoin de composition, trajectoire, score preview.
- Officiel: serveur exige `attacker_party_id`, `composition_snapshot`, `server_time`, `combat_rule_version`.
- Interdit client: accepter un raid officiel sans autorite serveur ou sans composition.

## Budgets data/performance

Budgets repris pour P6:

- Catalogue 50x50: 2500 coordonnees logiques maximum.
- Fenetre active: <= 25 chunks.
- Coin de carte: 9 chunks actifs attendus.
- Ruches actives: <= 25.
- Ressources actives: <= 75.
- Bestiaire actif: <= 25.
- Cache terrain Wave5: <= 96 textures.
- Allocations stress: <= 2,000,000 B/run.
- Allocations pan/zoom steady-state apres warmup: cible 0 B/frame, plafond temporaire <= 1 KB/frame.
- Switch de fenetre active: cible <= 32 KB.

Densite:

- Une fenetre dense ne doit pas depasser les pools actifs.
- Si total entites actives > 80, labels non prioritaires en LOD1.
- Si total entites actives > 110, ressources pauvres non proches en symbole compact.
- Aucun scenario ne doit charger le catalogue complet en scene.

Cache:

- Cache terrain, cache chunk data et cache sprites restent separes.
- Le stress synthetique ne mute pas `chunkCache`.
- Les sprites sont caches par famille/variant/tier, pas par `entity_id`.

## Validation des entrees

Avant toute preview:

- Verifier `schema_version` supportee.
- Verifier `world_id` non vide.
- Verifier `world_grid_version` connue.
- Verifier `chunk_id_logical` parseable et dans la grille.
- Verifier coordonnees normalisees dans `[0, 1]`.
- Verifier famille/type compatible.
- Verifier budget densite de la fenetre.
- Verifier volumes d'exclusion.
- Forcer `official=false` si provider local-demo ou seed-preview.

Avant toute action officielle:

- Refuser si `RequestedAuthority.Mode != ServerAuthoritative`.
- Refuser si serveur/authority unavailable.
- Refuser si snapshot entite obsolete.
- Refuser si combat/resource rule version absente.
- Refuser si `official` est calcule cote client.
- Exiger audit hash dans le receipt.

## Validation migrations

Snapshot migration:

```text
WorldMigrationSnapshot
- migration_id
- migration_from_world_version
- migration_to_world_version
- source_grid
- target_grid
- entity_count
- migrated_entities
- rejected_entities
- exclusion_revalidation_report
- migration_audit_hash
```

Gates:

- `entity_id` conserve ou mappe explicitement.
- `chunk_id_logical` source conserve comme metadata.
- `world_coord_normalized` present pour chaque entite.
- Reprojection deterministic hash stable.
- Exclusions revalidees.
- Rejets listes, jamais silencieux.
- Aucun etat officiel reconstruit depuis pixels ou scene.

## Interdits stricts

- Calculer un etat officiel cote client.
- Deriver l'etat officiel depuis pixels terrain.
- Sauver seulement une coordonnee ecran.
- Sauver seulement un `Vector2 world` sans version de grille.
- Peindre ressources, ruches ou bestiaire dans le terrain.
- Charger 2500 chunks ou textures pour prouver le 50x50.
- Faire passer `local_demo` ou `seed_preview` pour `server_authoritative`.
- Accorder loot, progression, cooldown, perte ou respawn officiel sans receipt d'autorite.

## Risques

- Les ids demo actuels peuvent etre confondus avec ids officiels.
- Les coordonnees C32_32/proof peuvent devenir des hypotheses cachees.
- Les scenarios locaux peuvent donner une impression de gain officiel.
- IMGUI et strings HUD peuvent masquer des allocations en production.
- Une migration sans `world_coord_normalized` perd la stabilite 25x25 -> 50x50.
- Les exclusions BearDen/evenements peuvent etre oubliees au moment de reprojeter.
- Un provider serveur futur pourrait reutiliser l'interface preview sans imposer `official=true` + audit hash.

Garde-fous:

- Prefixer toute demo/preuve.
- Afficher/propager `official=false` dans tous les previews.
- Tester les erreurs d'autorite comme cas de premiere classe.
- Exiger audit hash sur tout receipt officiel.
- Garder les budgets dans le contrat data, pas seulement dans le rendu.

## Criteres de handoff

Builder-A peut consommer ce contrat si:

- [ ] Les types minimum sont transposables sans modifier le terrain.
- [ ] `IWorldScenarioDataProvider` peut etre implemente en local-demo avec `official=false`.
- [ ] `IWorldScenarioCommandProvider.TrySubmitOfficial` refuse sans autorite serveur.
- [ ] Les snapshots incluent `world_grid_version`, `chunk_id_logical`, `world_coord_normalized`.
- [ ] Les scenarios Collecte/Duel/Raid exposent preview et receipt separes.
- [ ] Les budgets actifs restent 25 chunks / 25 ruches / 75 ressources / 25 menaces.
- [ ] Les migrations 25x25 -> 50x50 produisent audit hash et rejets explicites.
- [ ] Aucun test local ne declare de gain officiel.

## Audit P6 independant

Recherche locale effectuee dans `Docs` pour marqueurs P6/data/autorite:

- Aucun rapport P6 externe existant trouve au moment de cette redaction.
- Aucune preuve P6 separee a auditer n'a ete trouvee.
- Les preuves disponibles et autorisees confirment seulement les bases P1/50x50: catalogue 2500, fenetre active bornee, cache terrain preserve, stress sans terrain, allocations stress observees a 0 B.

Verdict independant:

- `P6_EXTERNAL_PROOF_AUDIT=NOT_AVAILABLE`
- `P6_CONTRACT_SCOPE_COMPLETE=YES`
- `OFFICIAL_CLIENT_STATE_COMPUTATION_FORBIDDEN=YES`
- `READY_FOR_BUILDER_A_CONSUMPTION=YES`

Ce YES signifie: Builder-A peut consommer le contrat technique pour implementation locale/proof-first. Il ne signifie pas que l'autorite serveur, la persistence officielle, l'economie officielle ou le combat officiel sont disponibles.
