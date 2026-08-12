using System;
using System.Collections.Generic;

namespace BeeKingdom.WorldMap
{
    // Registre spatial des chunks charges et des objets du monde enregistres.
    // Facade du systeme de coordonnees + conteneur ; n'effectue aucun chargement.
    public sealed class WorldGrid
    {
        private readonly WorldConfiguration configuration;
        private readonly Dictionary<ChunkCoordinate, WorldChunk> chunks = new Dictionary<ChunkCoordinate, WorldChunk>();
        private readonly Dictionary<WorldObjectId, WorldObject> objects = new Dictionary<WorldObjectId, WorldObject>();

        public WorldConfiguration Configuration { get; }
        public long ChunkSize => configuration.ChunkSize;
        public int ChunkCount => chunks.Count;
        public int ObjectCount => objects.Count;
        public IReadOnlyCollection<WorldChunk> Chunks => chunks.Values;
        public IReadOnlyCollection<WorldObject> Objects => objects.Values;

        public WorldGrid(WorldConfiguration configuration)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            configuration.Validate();
            Configuration = configuration;
        }

        public ChunkCoordinate ChunkOf(WorldPosition position)
        {
            return WorldCoordinateSystem.ChunkOf(position, configuration.ChunkSize);
        }

        public WorldPosition ChunkOrigin(ChunkCoordinate chunk)
        {
            return WorldCoordinateSystem.ChunkOrigin(chunk, configuration.ChunkSize);
        }

        public WorldChunk GetOrCreateChunk(ChunkCoordinate coordinate)
        {
            if (!chunks.TryGetValue(coordinate, out WorldChunk chunk))
            {
                chunk = new WorldChunk(coordinate, configuration.ChunkSize);
                chunks.Add(coordinate, chunk);
            }

            return chunk;
        }

        public bool TryGetChunk(ChunkCoordinate coordinate, out WorldChunk chunk)
        {
            return chunks.TryGetValue(coordinate, out chunk);
        }

        public bool IsChunkLoaded(ChunkCoordinate coordinate)
        {
            return chunks.TryGetValue(coordinate, out WorldChunk chunk) && chunk.State == WorldChunkState.Loaded;
        }

        // Retire le chunk du registre s'il est vide (aucun objet ni tuile). Ne decharge
        // pas : le WorldChunkLoader gere le contenu avant de demander le retrait.
        public bool RemoveChunk(ChunkCoordinate coordinate)
        {
            if (!chunks.TryGetValue(coordinate, out WorldChunk chunk))
            {
                return false;
            }

            if (chunk.ObjectCount != 0 || chunk.TileCount != 0)
            {
                return false;
            }

            return chunks.Remove(coordinate);
        }

        public bool TryGetObject(WorldObjectId id, out WorldObject worldObject)
        {
            return objects.TryGetValue(id, out worldObject);
        }

        public WorldObject RegisterObject(WorldObject worldObject)
        {
            if (worldObject == null)
            {
                throw new ArgumentNullException(nameof(worldObject));
            }

            if (objects.ContainsKey(worldObject.Id))
            {
                throw new InvalidOperationException("A world object with the same id is already registered.");
            }

            objects.Add(worldObject.Id, worldObject);
            WorldChunk chunk = GetOrCreateChunk(ChunkOf(worldObject.Position));
            chunk.AddObject(worldObject);
            return worldObject;
        }

        public bool UnregisterObject(WorldObject worldObject)
        {
            if (worldObject == null || !objects.Remove(worldObject.Id))
            {
                return false;
            }

            if (worldObject.Chunk != null)
            {
                worldObject.Chunk.RemoveObject(worldObject);
            }

            return true;
        }

        // Deplace un objet enregistre : met a jour la position et l'appartenance au
        // chunk. Leve PositionChanged via WorldObject.MoveTo.
        public void MoveObject(WorldObject worldObject, WorldPosition newPosition)
        {
            if (worldObject == null)
            {
                throw new ArgumentNullException(nameof(worldObject));
            }

            if (!objects.ContainsKey(worldObject.Id))
            {
                throw new InvalidOperationException("The world object is not registered on this grid.");
            }

            ChunkCoordinate from = ChunkOf(worldObject.Position);
            ChunkCoordinate to = ChunkOf(newPosition);
            worldObject.MoveTo(newPosition);
            if (!from.Equals(to))
            {
                worldObject.Chunk?.RemoveObject(worldObject);
                GetOrCreateChunk(to).AddObject(worldObject);
            }
        }
    }
}
