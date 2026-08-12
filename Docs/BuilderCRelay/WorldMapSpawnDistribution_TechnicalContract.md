# WorldMap Spawn Distribution - Technical Contract P7

Date locale: 2026-07-15

## Portee

Mission Builder-C Relay P7: definir en read-only le contrat technique de distribution locale deterministe des entites WorldMap 25x25/50x50.

Contraintes:

- Aucun fichier Unity, scene, PNG, APK, terrain, BearDen ou asset ne doit etre modifie par ce contrat.
- La distribution locale est une preview deterministe et inspectable.
- La distribution locale ne cree aucun etat officiel.
- Le futur serveur pourra reprendre, valider, remplacer ou refuser les propositions locales.

Sources utilisees:

- `Docs/BuilderCRelay/WorldMapScenarioDataLayer_TechnicalContract.md`
- `Docs/BuilderCRelay/WorldMap50x50_RuntimePerformanceContract.md`

## Objectif P7

Le contrat P7 definit:

- Les entrees de seed/version/world/chunk.
- La generation stable d'IDs preview.
- Les tables de poids ressources R1-R3 et bestiaire T1-T7.
- Les caps par chunk et par fenetre active.
- Les volumes d'exclusion BearDen/eau/falaise/evenement.
- Les distances minimales entre entites.
- Les regles de streaming/pooling sans allocation excessive.
- La reprojection 25x25 -> 50x50.
- Une interface d'inspection locale sans autorite officielle.

## Entrees deterministes

Toute generation locale doit recevoir un contexte explicite:

```text
SpawnDistributionContext
- schema_version
- world_id
- world_grid_version
- server_id_preview
- season_id
- spawn_seed_version
- distribution_table_version
- exclusion_volume_version
- chunk_id_logical
- chunk_x
- chunk_y
- grid_width_chunks
- grid_height_chunks
- chunk_size_world_units
- source_kind = seed_preview
- official = false
```

Regles:

- `spawn_seed_version` change seulement si l'algorithme de generation change.
- `distribution_table_version` change si les poids R/T changent.
- `exclusion_volume_version` change si BearDen/eau/falaise/evenement change.
- `world_grid_version` change si la grille 25x25/50x50 ou le rectangle jouable change.
- La meme entree doit produire la meme sortie bit-a-bit au niveau logique.

## Hash et random stable

Pseudo-code recommande:

```csharp
public static uint StableHash(
    string worldId,
    string seasonId,
    string spawnSeedVersion,
    string chunkIdLogical,
    string family,
    int salt)
{
    // FNV-1a 32-bit ou xxHash32 equivalent, implementation locale stable.
    // Interdit: GetHashCode(), Random sans seed, temps systeme.
    return Hash32(worldId + "|" + seasonId + "|" + spawnSeedVersion + "|" + chunkIdLogical + "|" + family + "|" + salt);
}

public static float Unit01(uint hash)
{
    return (hash & 0x00FFFFFFu) / 16777215f;
}

public static int Range(uint hash, int minInclusive, int maxExclusive)
{
    return minInclusive + (int)(hash % (uint)(maxExclusive - minInclusive));
}
```

Interdits:

- `string.GetHashCode()` pour determiner le spawn.
- `UnityEngine.Random` global.
- `System.Random` sans seed explicite.
- Heure locale, frame count ou ordre de chargement comme entree de distribution.

## Generation stable d'IDs

Format local preview:

```text
preview:{world_id}:{world_grid_version}:{family}:{chunk_id_logical}:{slot}:{spawn_seed_version}
```

Exemples:

- `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_25x25_v1:resource:C32_32:r0:spawn_v1`
- `preview:BK-DEMO-WORLD-WAVE5-LOCAL:grid_50x50_v1:bestiary:C25_25:b0:spawn_v1`

Regles:

- L'id preview est stable tant que les entrees versionnees restent identiques.
- L'id preview n'est pas un id officiel.
- Une migration doit conserver l'id source comme metadata et produire un mapping explicite.
- Le serveur futur peut retourner un `entity_id` officiel different.

Pseudo-code:

```csharp
public static string PreviewEntityId(SpawnDistributionContext ctx, string family, int slot)
{
    return "preview:" + ctx.WorldId + ":" + ctx.WorldGridVersion + ":" + family + ":" + ctx.ChunkIdLogical + ":" + family[0] + slot + ":" + ctx.SpawnSeedVersion;
}
```

## Tables de poids ressources R1-R3

Familles ressources:

- `pollen`
- `nectar`
- `water`
- `wax`
- `honey`
- `royal_jelly`
- `propolis`

Tiers:

- R1 pauvre: quantite faible, taille/symbole compact.
- R2 moyen: quantite moyenne, lisibilite prioritaire.
- R3 riche: quantite haute, plus rare, interaction prioritaire.

Table locale preview v1:

| Resource kind | Poids apparition | R1 | R2 | R3 | Notes |
| --- | ---: | ---: | ---: | ---: | --- |
| pollen | 22 | 50 | 35 | 15 | Ressource commune. |
| nectar | 22 | 45 | 40 | 15 | Ressource commune. |
| water | 14 | 55 | 35 | 10 | Soumise aux exclusions eau si zones reservees. |
| wax | 12 | 60 | 32 | 8 | Moins frequent. |
| honey | 12 | 55 | 35 | 10 | Quantite plafonnee. |
| propolis | 10 | 65 | 28 | 7 | Defensive/rare. |
| royal_jelly | 8 | 75 | 20 | 5 | Rare, faibles quantites absolues. |

Quantites preview:

| Tier | Capacity range | Richness token |
| --- | ---: | --- |
| R1 | 8-49 | `poor` |
| R2 | 50-95 | `medium` |
| R3 | 96-129 | `rich` |

Regles:

- Les quantites officielles futures viennent d'une table serveur versionnee.
- En preview, `remaining = capacity` au spawn.
- Collecte locale peut simuler une baisse avec `official_gain=false`.

## Tables de poids bestiaire T1-T7

Tiers:

- T1-T2: nuisance locale, duel preview autorise.
- T3-T4: elite locale, duel preview autorise.
- T5-T7: menace raid, raid preview uniquement.

Table locale preview v1:

| Tier | Poids | Variants | Combat access | Role |
| ---: | ---: | ---: | --- | --- |
| T1 | 24 | 2 | duel | nuisance |
| T2 | 20 | 2 | duel | nuisance forte |
| T3 | 17 | 2 | duel | elite locale |
| T4 | 14 | 2 | duel | elite dangereuse |
| T5 | 10 | 2 | raid | raid mineur |
| T6 | 8 | 2 | raid | raid majeur |
| T7 | 7 | 2 | raid | raid boss |

Regles:

- T1-T4 peuvent exposer une preview Duel.
- T5-T7 doivent exposer une preview Raid et une exigence de composition.
- Aucun tier ne donne loot officiel en local.
- Les variants sont deterministes via hash du slot.

## Caps par chunk et fenetre

Caps par chunk preview:

| Famille | Cible/chunk | Plafond/chunk | Notes |
| --- | ---: | ---: | --- |
| hive | 0-1 | 1 | Sauf anchors demo/proof explicites. |
| resource | 1-2 | 3 | 3 seulement si densite locale autorisee. |
| bestiary | 0-1 | 1 | T5-T7 rares. |
| event | 0-1 | 1 | Mutuellement exclusif avec certains spawns. |

Caps fenetre active:

| Famille | Plafond fenetre 5x5 |
| --- | ---: |
| chunks actifs | 25 |
| hives | 25 |
| resources | 75 |
| bestiary | 25 |
| events | 8 |

Regles:

- Une fenetre coin 3x3 doit rester valide avec 9 chunks.
- La distribution ne doit jamais exiger le catalogue complet en scene.
- Si un cap fenetre est atteint, les candidats excedentaires sont rejetes de maniere deterministe et listes dans l'inspection.

## Volumes d'exclusion

Types:

```text
ExclusionVolume
- volume_id
- volume_kind: BearDen, Water, Cliff, Event, Reserved
- world_grid_version
- shape_kind: circle, rect, polygon
- normalized_bounds
- priority
- blocks_families
- exclusion_version
```

Regles:

- BearDen bloque au minimum `hive`, `resource`, `bestiary` dans son volume.
- Eau bloque les entites terrestres; peut autoriser `water` seulement si la table le permet.
- Falaise bloque les ressources interactives et ruches.
- Evenement peut bloquer ou reserver selon `event_rule_version`.
- Les volumes sont evalues avant les distances minimales.
- Les rejets sont inspectables: `candidate_id`, `volume_id`, `volume_kind`.

Pseudo-code:

```csharp
bool IsExcluded(CandidateSpawn candidate, IReadOnlyList<ExclusionVolume> volumes, out string reason)
{
    for (int i = 0; i < volumes.Count; i++)
    {
        if (!volumes[i].Blocks(candidate.Family)) continue;
        if (!volumes[i].Contains(candidate.NormalizedCoord)) continue;
        reason = volumes[i].VolumeId;
        return true;
    }

    reason = string.Empty;
    return false;
}
```

## Distances minimales

Distances en unites monde, a adapter si `chunk_size_world_units` change:

| Pair | Distance min |
| --- | ---: |
| hive-hive | 300 |
| hive-resource | 105 |
| hive-bestiary | 160 |
| resource-resource | 90 |
| resource-bestiary | 80 |
| bestiary-bestiary | 180 |
| event-any | selon volume |

Regles:

- Les distances sont verifiees dans le chunk courant et les chunks voisins actifs.
- La verification doit etre deterministe: ordre candidats stable par famille puis slot.
- En cas de conflit, priorite: anchors proof/demo > event > hive > R3 resource > T5-T7 bestiary > autres resources > T1-T4 bestiary.
- Les candidats rejetes restent visibles dans l'interface d'inspection locale.

## Algorithme de distribution locale

Pseudo-code global:

```csharp
WorldSpawnInspection GenerateChunkSpawns(SpawnDistributionContext ctx, ExclusionVolume[] exclusions, SpawnBudget budget)
{
    inspection.ClearFor(ctx);
    candidates.Clear();

    AddHiveCandidates(ctx, candidates);
    AddResourceCandidates(ctx, candidates);
    AddBestiaryCandidates(ctx, candidates);
    AddEventCandidates(ctx, candidates);

    SortCandidatesByDeterministicPriority(candidates);

    for (int i = 0; i < candidates.Count; i++)
    {
        CandidateSpawn candidate = candidates[i];

        if (IsExcluded(candidate, exclusions, out string exclusionId))
        {
            inspection.Reject(candidate, "exclusion:" + exclusionId);
            continue;
        }

        if (!PassesDistance(candidate, accepted))
        {
            inspection.Reject(candidate, "distance");
            continue;
        }

        if (!budget.TryAccept(candidate))
        {
            inspection.Reject(candidate, "budget");
            continue;
        }

        accepted.Add(candidate.ToEntitySnapshot(official: false));
        inspection.Accept(candidate);
    }

    return inspection.Freeze();
}
```

Resource candidate:

```csharp
CandidateSpawn BuildResourceCandidate(SpawnDistributionContext ctx, int slot)
{
    uint h = StableHash(ctx.WorldId, ctx.SeasonId, ctx.SpawnSeedVersion, ctx.ChunkIdLogical, "resource", slot);
    ResourceKind kind = WeightedPick(ResourceKindWeightsV1, h);
    ResourceTier tier = WeightedPick(ResourceTierWeightsV1[kind], Rotate(h, 9));
    WorldLogicalCoord coord = PointInChunk(ctx, Rotate(h, 17), 0.14f, 0.86f);
    int capacity = CapacityForTier(tier, Rotate(h, 23));
    return CandidateSpawn.Resource(PreviewEntityId(ctx, "resource", slot), kind, tier, coord, capacity);
}
```

Bestiary candidate:

```csharp
CandidateSpawn BuildBestiaryCandidate(SpawnDistributionContext ctx, int slot)
{
    uint h = StableHash(ctx.WorldId, ctx.SeasonId, ctx.SpawnSeedVersion, ctx.ChunkIdLogical, "bestiary", slot);
    int tier = WeightedPick(BestiaryTierWeightsV1, h);
    int variant = 1 + Range(Rotate(h, 11), 0, 2);
    WorldLogicalCoord coord = PointInChunk(ctx, Rotate(h, 19), 0.16f, 0.84f);
    string access = tier <= 4 ? "duel" : "raid";
    return CandidateSpawn.Bestiary(PreviewEntityId(ctx, "bestiary", slot), tier, variant, access, coord);
}
```

## Streaming et pooling

Contrat streaming:

- Generer seulement la fenetre active et, optionnellement, un anneau de prefetch logique.
- Reutiliser les listes scratch.
- Ne pas allouer par frame pendant pan/zoom.
- Ne pas instancier/detruire des objets Unity dans la boucle chaude.
- Les entites hors fenetre sont recyclees ou desactivees.

Pools cibles:

- Hives: 25 actifs.
- Resources: 75 actifs.
- Bestiary: 25 actifs.
- Events: 8 actifs.
- Labels haute priorite: 28 total.
- Arcs/feedback: 8 actifs, 16 reserves.

Budgets allocations:

- Steady-state pan/zoom apres warmup: cible 0 B/frame.
- Switch fenetre active: cible <= 32 KB.
- Stress 50x50 complet: <= 2,000,000 B/run.
- Cache terrain Wave5: <= 96 textures.

## Reprojection 25x25 -> 50x50

Source de verite:

- `world_coord_normalized`
- `entity_id` source ou mapping explicite
- `spawn_seed_version`
- `distribution_table_version`
- `world_grid_version` source

Pseudo-code:

```csharp
WorldLogicalCoord Reproject(WorldCoordNormalized normalized, WorldGridDefinition target)
{
    float gx = normalized.X01 * target.WidthChunks;
    float gy = normalized.Y01 * target.HeightChunks;

    int chunkX = normalized.X01 >= 1f ? target.WidthChunks - 1 : Mathf.FloorToInt(gx);
    int chunkY = normalized.Y01 >= 1f ? target.HeightChunks - 1 : Mathf.FloorToInt(gy);

    float localX = normalized.X01 >= 1f ? 1f : gx - chunkX;
    float localY = normalized.Y01 >= 1f ? 1f : gy - chunkY;

    return new WorldLogicalCoord(chunkX, chunkY, localX, localY, ChunkId(chunkX, chunkY));
}
```

Apres reprojection:

- Revalider volumes d'exclusion cible.
- Revalider distances minimales dans la nouvelle fenetre.
- Conserver l'id source comme metadata.
- Produire `migration_audit_hash`.
- Lister les entites rejetees; aucun rejet silencieux.

## Interface d'inspection locale

Cette interface est read-only et sans autorite officielle.

```csharp
public interface IWorldSpawnDistributionInspector
{
    bool TryInspectChunk(SpawnDistributionContext context, out WorldSpawnInspection inspection);
    bool TryInspectWindow(WorldWindowRequest request, out WorldWindowSpawnInspection inspection);
}

public readonly struct WorldSpawnInspection
{
    public readonly SpawnDistributionContext Context;
    public readonly WorldEntitySnapshot[] Accepted;
    public readonly SpawnRejectedCandidate[] Rejected;
    public readonly string DeterministicHash;
    public readonly bool Official; // toujours false en local
}

public readonly struct SpawnRejectedCandidate
{
    public readonly string CandidateId;
    public readonly string Family;
    public readonly string Reason;
    public readonly string Detail;
}
```

Regles inspection:

- L'inspection peut afficher ids preview, coordonnees, poids, tiers et raisons de rejet.
- L'inspection ne peut pas confirmer recompense, progression, combat ou persistence.
- L'inspection doit exposer `official=false`.
- L'inspection doit reporter les caps et compteurs actifs.

## Invariants P7

Invariants de determinisme:

- Meme contexte versionne => memes candidats, memes ids, memes rejets.
- Changement de seed/table/exclusion/grid => hash d'inspection different.
- L'ordre de generation ne depend pas de l'ordre des fichiers, frames ou chargements.

Invariants d'autorite:

- Toute sortie P7 locale porte `official=false`.
- Aucun gain officiel n'est calcule.
- Aucun combat officiel n'est resolu.
- Aucun respawn officiel n'est programme.

Invariants de performance:

- Fenetre active <= 25 chunks.
- Resources actives <= 75.
- Hives actives <= 25.
- Bestiary actif <= 25.
- Stress 50x50 ne charge pas 2500 chunks en scene.
- Stress 50x50 ne cree pas de terrain.

Invariants de migration:

- Chaque entite migrable a `world_coord_normalized`.
- Chaque reprojection ecrit un audit hash.
- Chaque exclusion post-migration est explicite.

## Audit P6 en attente

Recherche locale P6 effectuee avant creation P7:

- `RuntimeScenarioDataLayer_Report.md`: absent.
- Recu RuntimeScenarioDataLayer associe: absent.
- Fichiers ScenarioDataLayer trouves: contrat Builder-C P6 et matrice QA seulement.

Verdict audit P6 actuel:

- `P6_RUNTIME_SCENARIO_DATA_LAYER_REPORT_FOUND=NO`
- `P6_RUNTIME_SCENARIO_DATA_LAYER_RECEIPT_FOUND=NO`
- `P6_INDEPENDENT_AUDIT_STATUS=PENDING_EVIDENCE`

Si le rapport P6 et son recu apparaissent ensuite, l'audit independant doit verifier:

- `official=false` pour local-demo/seed-preview.
- refus des actions officielles sans autorite serveur.
- presence de `world_grid_version`, `chunk_id_logical`, `world_coord_normalized`.
- separation preview/receipt pour Collecte/Duel/Raid.
- aucune mutation de terrain, PNG, scene ou APK.
- preuves de budget fenetre/cache/allocations.

## Criteres de handoff

Builder-A peut consommer P7 si:

- [ ] Les entrees seed/version/world/chunk sont explicites.
- [ ] Les IDs preview sont stables et non officiels.
- [ ] Les tables R1-R3/T1-T7 sont versionnees.
- [ ] Les caps chunk/fenetre sont appliques avant affichage.
- [ ] Les volumes BearDen/eau/falaise/evenement rejettent les candidats.
- [ ] Les distances minimales sont deterministes.
- [ ] Le streaming utilise pools/scratch buffers sans allocations excessives.
- [ ] La reprojection 25x25 -> 50x50 part de `world_coord_normalized`.
- [ ] L'interface d'inspection locale expose acceptes/rejetes/hash et `official=false`.
- [ ] Aucun etat officiel n'est calcule par le client.

## Verdict

- `WORLD_MAP_SPAWN_DISTRIBUTION_CONTRACT=P7_READY`
- `OFFICIAL_CLIENT_STATE_COMPUTATION_FORBIDDEN=YES`
- `READY_FOR_P7_CONSUMPTION=YES`

Ce YES autorise une implementation locale/proof-first de la distribution deterministe. Il ne valide pas une economie officielle, un spawn officiel, un combat officiel, un raid officiel ou une persistence serveur.
