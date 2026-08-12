using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeeKingdom.WorldMap
{
    // Contenu produit par une source de contenu pour un chunk : objets du monde
    // (positions et ids explicites) + tuiles libres pour les futurs systemes.
    public sealed class WorldChunkContent
    {
        private readonly List<WorldObject> objects;
        private readonly Dictionary<TileCoordinate, WorldTile> tiles;

        public IReadOnlyList<WorldObject> Objects => objects;
        public IReadOnlyDictionary<TileCoordinate, WorldTile> Tiles => tiles;

        public WorldChunkContent()
            : this(Array.Empty<WorldObject>(), Array.Empty<WorldTile>())
        {
        }

        public WorldChunkContent(IEnumerable<WorldObject> objects, IEnumerable<WorldTile> tiles = null)
        {
            this.objects = new List<WorldObject>(objects ?? Array.Empty<WorldObject>());
            this.tiles = new Dictionary<TileCoordinate, WorldTile>();
            if (tiles != null)
            {
                foreach (WorldTile tile in tiles)
                {
                    this.tiles[tile.Coordinate] = tile;
                }
            }
        }
    }

    // Source de contenu des chunks. Le streaming ne connait que cette interface :
    // terrain, objets statiques, donnees de jeu arriveront par implementation.
    public interface IWorldChunkContentSource
    {
        Task<WorldChunkContent> LoadAsync(ChunkCoordinate chunk, long chunkSize, CancellationToken cancellationToken);
        void Unload(ChunkCoordinate chunk, WorldChunkContent content);
    }

    // Source vide : par defaut sur, utilisee quand aucune source n'est fournie.
    public sealed class EmptyWorldChunkContentSource : IWorldChunkContentSource
    {
        public static readonly EmptyWorldChunkContentSource Instance = new EmptyWorldChunkContentSource();

        public Task<WorldChunkContent> LoadAsync(ChunkCoordinate chunk, long chunkSize, CancellationToken cancellationToken)
        {
            return Task.FromResult(new WorldChunkContent());
        }

        public void Unload(ChunkCoordinate chunk, WorldChunkContent content)
        {
        }
    }
}
