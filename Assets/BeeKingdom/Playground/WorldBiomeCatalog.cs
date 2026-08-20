using UnityEngine;

namespace BeeKingdom.Playground
{
    // Biome data model for the World Map, sourced from the design bible at
    // C:\projets\beekingdom\BIBLE\09_World\WORLD_BIBLE_FOUNDATION.md. Pure data, no
    // behavior - mirrors the tuple-array style of PointOfInterestCatalog inside
    // WorldMapMmoFullscreenFoundationBootstrap.cs rather than the heavier BiomeRegistry
    // machinery in BiomeFramework.cs (that file's WorldBiomeType enum - Prairie/Forest/
    // Mountain/River/Marsh/FlowerFields/Wetland/Meadow/Orchard/Farmland/Urban - is a
    // different game's taxonomy, has no bible-named biomes, and is otherwise dead code
    // kept alive only by its own tests; do not reuse it here).
    //
    // The 10x10 grid below (each cell = 5x5 tiles of the streamed 50x50 art) was authored
    // by sampling ~40 tiles spread across
    // Resources/WorldMapWave6Runtime/UIB_ImmenseContinuousMaster50x50_wave5method_12288_preview/
    // and classifying the actual painted terrain against the bible's 6 biome identities.
    // Note: a meaningful share of the painted continent is rocky/snow-capped mountain
    // terrain that has no equivalent in the bible (which describes a "bee-scale" world -
    // prairie as continent, puddle as sea - with no mountain biome at all). Those areas
    // were mapped to Terres Seches as the closest mood fit (harsh, rare, tense), not a
    // literal match. Worth a second look once new art can be commissioned to match the
    // bible's bee-scale vision more closely; until then this is the best-effort mapping.
    public enum WorldBiome
    {
        PrairieFleurie,
        ForetClaire,
        BergesEtMares,
        RonciersEtHaies,
        TerresSeches,
        VergerAncien
    }

    public readonly struct WorldBiomeProfile
    {
        public readonly WorldBiome Biome;
        public readonly string DisplayLabel;
        public readonly string IdentityText;
        public readonly string PlayerPromise;
        public readonly Color EmotionalColor;

        public WorldBiomeProfile(WorldBiome biome, string displayLabel, string identityText, string playerPromise, Color emotionalColor)
        {
            Biome = biome;
            DisplayLabel = displayLabel;
            IdentityText = identityText;
            PlayerPromise = playerPromise;
            EmotionalColor = emotionalColor;
        }
    }

    public readonly struct WorldRegionProfile
    {
        public readonly string RegionId;
        public readonly string Label;
        public readonly Rect NormalizedBounds;
        public readonly WorldBiome DominantBiome;

        public WorldRegionProfile(string regionId, string label, Rect normalizedBounds, WorldBiome dominantBiome)
        {
            RegionId = regionId;
            Label = label;
            NormalizedBounds = normalizedBounds;
            DominantBiome = dominantBiome;
        }
    }

    public static class WorldBiomeCatalog
    {
        // Mirrors WorldMapWave6StreamingTileProvider's live grid (the wave5method_12288
        // preview package used by the actual production scene).
        private const int OriginChunkX = WorldMapWave6StreamingTileProvider.OriginChunkX;
        private const int OriginChunkY = WorldMapWave6StreamingTileProvider.OriginChunkY;
        private const int GridTiles = WorldMapWave6StreamingTileProvider.Rows; // 50, square grid
        private const int GridCells = 10;
        private const int TilesPerCell = GridTiles / GridCells; // 5

        public static readonly WorldBiomeProfile[] Profiles =
        {
            new WorldBiomeProfile(
                WorldBiome.PrairieFleurie, "Prairie Fleurie",
                "Lumineuse, ouverte, riche en nectar, lisible pour les nouveaux joueurs.",
                "Exploration accessible, premiers souvenirs, premiere impression de monde vivant.",
                new Color(1f, 0.85f, 0.35f)),
            new WorldBiomeProfile(
                WorldBiome.ForetClaire, "Foret Claire",
                "Verticale, protegee, riche en ombres douces et racines.",
                "Profondeur, mystere, anciens territoires.",
                new Color(0.16f, 0.35f, 0.20f)),
            new WorldBiomeProfile(
                WorldBiome.BergesEtMares, "Berges et Mares",
                "Humidite, reflets, sons calmes, danger d'embuscade.",
                "Richesse visible mais risque clair.",
                new Color(0.20f, 0.55f, 0.60f)),
            new WorldBiomeProfile(
                WorldBiome.RonciersEtHaies, "Ronciers et Haies",
                "Dense, defensif, labyrinthique, riche en cachettes.",
                "Territoire dangereux, cachettes, conflits.",
                new Color(0.30f, 0.20f, 0.35f)),
            new WorldBiomeProfile(
                WorldBiome.TerresSeches, "Terres Seches",
                "Pierres chaudes, herbes courtes, fleurs rares mais precieuses.",
                "Rarete, tension, valeur elevee des ressources.",
                new Color(0.65f, 0.45f, 0.25f)),
            new WorldBiomeProfile(
                WorldBiome.VergerAncien, "Verger Ancien",
                "Abondance ordonnee, traces humaines lointaines, floraisons spectaculaires.",
                "Evenements rares et memorables, competition sociale.",
                new Color(0.80f, 0.55f, 0.20f)),
        };

        public static readonly WorldRegionProfile[] Regions =
        {
            new WorldRegionProfile("coeur_de_prairie", "Coeur de Prairie", new Rect(0.0f, 0.3f, 0.7f, 0.2f), WorldBiome.PrairieFleurie),
            new WorldRegionProfile("lisiere_des_chenes", "Lisiere des Chenes", new Rect(0.0f, 0.0f, 0.2f, 0.3f), WorldBiome.ForetClaire),
            new WorldRegionProfile("ronciers_noirs", "Ronciers Noirs", new Rect(0.7f, 0.3f, 0.3f, 0.2f), WorldBiome.RonciersEtHaies),
            new WorldRegionProfile("berges_de_rosee", "Berges de Rosee", new Rect(0.3f, 0.5f, 0.4f, 0.2f), WorldBiome.BergesEtMares),
            new WorldRegionProfile("pierres_du_midi", "Pierres du Midi", new Rect(0.2f, 0.0f, 0.6f, 0.3f), WorldBiome.TerresSeches),
        };

        // Row index = cellY (0 = south edge of the painted area, 9 = north edge).
        // Column index = cellX (0 = west edge, 9 = east edge). Each cell = 5x5 tiles.
        private static readonly WorldBiome[,] BiomeGrid =
        {
            // cellY 0 (rows 0-4): mostly rocky/mountainous per sampling, forest at the west edge, one water pocket east.
            { WorldBiome.ForetClaire, WorldBiome.ForetClaire, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares },
            { WorldBiome.ForetClaire, WorldBiome.ForetClaire, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares },
            { WorldBiome.ForetClaire, WorldBiome.ForetClaire, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares },
            // cellY 3-4: bright wildflower prairie band, ronciers pocket at the far east edge.
            { WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies },
            { WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.PrairieFleurie, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies },
            // cellY 5-6: golden-orchard west, water-rich center, rocky east.
            { WorldBiome.VergerAncien, WorldBiome.VergerAncien, WorldBiome.VergerAncien, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches },
            { WorldBiome.VergerAncien, WorldBiome.VergerAncien, WorldBiome.VergerAncien, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches },
            // cellY 7-9: orchard/roncier south band, water pocket at the south-east corner.
            { WorldBiome.VergerAncien, WorldBiome.VergerAncien, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.BergesEtMares },
            { WorldBiome.VergerAncien, WorldBiome.VergerAncien, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares },
            { WorldBiome.VergerAncien, WorldBiome.VergerAncien, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies, WorldBiome.RonciersEtHaies, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.TerresSeches, WorldBiome.BergesEtMares, WorldBiome.BergesEtMares },
        };

        public static WorldBiomeProfile ProfileFor(WorldBiome biome)
        {
            for (int i = 0; i < Profiles.Length; i++)
            {
                if (Profiles[i].Biome == biome) return Profiles[i];
            }
            return Profiles[0];
        }

        // chunkX/chunkY are absolute world-chunk coordinates (same space as
        // WorldChunkData.Chunk / WorldMapWave6StreamingTileProvider tile coordinates).
        // Out-of-range coordinates clamp to the nearest edge cell rather than throwing,
        // since callers iterate active chunks that can extend slightly past the painted
        // 50x50 area near the world bounds.
        public static WorldBiome BiomeForChunk(int chunkX, int chunkY)
        {
            int localX = chunkX - OriginChunkX;
            int localY = chunkY - OriginChunkY;
            int cellX = Mathf.Clamp(localX / TilesPerCell, 0, GridCells - 1);
            int cellY = Mathf.Clamp(localY / TilesPerCell, 0, GridCells - 1);
            return BiomeGrid[cellY, cellX];
        }
    }
}
