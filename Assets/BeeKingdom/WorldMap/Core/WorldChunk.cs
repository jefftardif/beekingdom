using System;
using System.Collections.Generic;

namespace BeeKingdom.WorldMap
{
    public enum WorldChunkState
    {
        Unloaded = 0,
        Loading = 1,
        Loaded = 2,
        Unloading = 3
    }

    // Chunk de la carte : conteneur d'objets du monde et de tuiles, pilote par le
    // WorldChunkLoader (transitions d'etat). Un chunk charge n'existe que s'il est
    // en cours de chargement ou charge ; le dechargement le vide et l'unregister.
    public sealed class WorldChunk
    {
        private readonly Dictionary<WorldObjectId, WorldObject> objects = new Dictionary<WorldObjectId, WorldObject>();
        private readonly Dictionary<TileCoordinate, WorldTile> tiles = new Dictionary<TileCoordinate, WorldTile>();

        public ChunkCoordinate Coordinate { get; }
        public long Size { get; }
        public WorldChunkState State { get; private set; }
        public WorldChunkContent Content { get; internal set; }
        public int ObjectCount => objects.Count;
        public int TileCount => tiles.Count;

        public event Action<WorldChunk> StateChanged;

        public WorldChunk(ChunkCoordinate coordinate, long size)
        {
            Coordinate = coordinate;
            Size = size;
        }

        internal void SetState(WorldChunkState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(this);
        }

        public bool AddObject(WorldObject worldObject)
        {
            if (worldObject == null)
            {
                throw new ArgumentNullException(nameof(worldObject));
            }

            if (objects.ContainsKey(worldObject.Id))
            {
                return false;
            }

            objects.Add(worldObject.Id, worldObject);
            worldObject.Chunk = this;
            return true;
        }

        public bool RemoveObject(WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            if (!objects.Remove(worldObject.Id))
            {
                return false;
            }

            if (worldObject.Chunk == this)
            {
                worldObject.Chunk = null;
            }

            return true;
        }

        public bool TryGetObject(WorldObjectId id, out WorldObject worldObject)
        {
            return objects.TryGetValue(id, out worldObject);
        }

        public IReadOnlyCollection<WorldObject> Objects => objects.Values;

        public void SetTile(TileCoordinate coordinate, WorldTile value)
        {
            tiles[coordinate] = value;
        }

        public bool TryGetTile(TileCoordinate coordinate, out WorldTile value)
        {
            return tiles.TryGetValue(coordinate, out value);
        }

        public bool RemoveTile(TileCoordinate coordinate)
        {
            return tiles.Remove(coordinate);
        }

        internal void ResetContent()
        {
            objects.Clear();
            tiles.Clear();
            Content = null;
        }
    }

    // Tuile de la carte : donnee fine sous le chunk. Base volontairement minimale :
    // les futurs systemes (terrain, occupation, ressources) l'etendront.
    public readonly struct WorldTile
    {
        public TileCoordinate Coordinate { get; }
        public byte Flags { get; }

        public WorldTile(TileCoordinate coordinate, byte flags = 0)
        {
            Coordinate = coordinate;
            Flags = flags;
        }

        public bool IsEmpty => Flags == 0;
    }
}
