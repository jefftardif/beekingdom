using BeeKingdom.WorldMap;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldChunkTests
    {
        [Test]
        public void AddObjectRegistersChunkOwnershipAndRejectsDuplicates()
        {
            var chunk = new WorldChunk(new ChunkCoordinate(0L, 0L), 64L);
            var worldObject = new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(10L, 10L));

            Assert.That(chunk.AddObject(worldObject), Is.True);
            Assert.That(chunk.ObjectCount, Is.EqualTo(1));
            Assert.That(worldObject.Chunk, Is.SameAs(chunk));
            Assert.That(chunk.AddObject(worldObject), Is.False);
            Assert.That(chunk.ObjectCount, Is.EqualTo(1));
        }

        [Test]
        public void RemoveObjectClearsOwnership()
        {
            var chunk = new WorldChunk(new ChunkCoordinate(0L, 0L), 64L);
            var worldObject = new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(10L, 10L));
            chunk.AddObject(worldObject);

            Assert.That(chunk.RemoveObject(worldObject), Is.True);
            Assert.That(chunk.ObjectCount, Is.Zero);
            Assert.That(worldObject.Chunk, Is.Null);
            Assert.That(chunk.RemoveObject(worldObject), Is.False);
        }

        [Test]
        public void TilesAreStoredAndRemovedByCoordinate()
        {
            var chunk = new WorldChunk(new ChunkCoordinate(1L, 1L), 64L);
            var tile = new WorldTile(new TileCoordinate(-1L, 2L), 7);

            chunk.SetTile(tile.Coordinate, tile);

            Assert.That(chunk.TileCount, Is.EqualTo(1));
            Assert.That(chunk.TryGetTile(new TileCoordinate(-1L, 2L), out WorldTile read), Is.True);
            Assert.That(read.Coordinate, Is.EqualTo(tile.Coordinate));
            Assert.That(read.Flags, Is.EqualTo(7));
            Assert.That(chunk.RemoveTile(new TileCoordinate(-1L, 2L)), Is.True);
            Assert.That(chunk.TryGetTile(new TileCoordinate(-1L, 2L), out _), Is.False);
        }

        [Test]
        public void ResetContentClearsObjectsAndTiles()
        {
            var chunk = new WorldChunk(new ChunkCoordinate(0L, 0L), 64L);
            chunk.AddObject(new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(0L, 0L)));
            chunk.SetTile(new TileCoordinate(0L, 0L), new WorldTile(new TileCoordinate(0L, 0L)));

            chunk.ResetContent();

            Assert.That(chunk.ObjectCount, Is.Zero);
            Assert.That(chunk.TileCount, Is.Zero);
        }

        [Test]
        public void StateChangedFiresOncePerTransition()
        {
            var chunk = new WorldChunk(new ChunkCoordinate(0L, 0L), 64L);
            int transitions = 0;
            WorldChunkState last = WorldChunkState.Unloaded;
            chunk.StateChanged += changed => { transitions++; last = changed.State; };

            chunk.SetState(WorldChunkState.Loading);
            chunk.SetState(WorldChunkState.Loaded);
            chunk.SetState(WorldChunkState.Loaded);

            Assert.That(transitions, Is.EqualTo(2));
            Assert.That(last, Is.EqualTo(WorldChunkState.Loaded));
        }
    }

    public sealed class WorldGridTests
    {
        private static WorldGrid NewGrid(long chunkSize = 64L)
        {
            return new WorldGrid(WorldMapTestConfiguration.Create(chunkSize));
        }

        [Test]
        public void GetOrCreateChunkReturnsTheSameInstance()
        {
            WorldGrid grid = NewGrid();
            ChunkCoordinate coordinate = new ChunkCoordinate(-3L, 5L);

            WorldChunk first = grid.GetOrCreateChunk(coordinate);
            WorldChunk second = grid.GetOrCreateChunk(coordinate);

            Assert.That(second, Is.SameAs(first));
            Assert.That(grid.ChunkCount, Is.EqualTo(1));
            Assert.That(first.Size, Is.EqualTo(64L));
        }

        [Test]
        public void ChunkOfWorldPositionFlowsThroughCoordinateSystem()
        {
            WorldGrid grid = NewGrid();

            Assert.That(grid.ChunkOf(new WorldPosition(-1L, 64L)), Is.EqualTo(new ChunkCoordinate(-1L, 1L)));
        }

        [Test]
        public void RegisterObjectFindsItsChunkAndRejectsDuplicates()
        {
            WorldGrid grid = NewGrid();
            var worldObject = new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(64L, -1L));

            grid.RegisterObject(worldObject);

            Assert.That(worldObject.Chunk.Coordinate, Is.EqualTo(new ChunkCoordinate(1L, -1L)));
            Assert.That(grid.ObjectCount, Is.EqualTo(1));
            Assert.Throws<System.InvalidOperationException>(() => grid.RegisterObject(new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(0L, 0L))));
        }

        [Test]
        public void MoveObjectAcrossChunksUpdatesMembershipAndRaisesEvent()
        {
            WorldGrid grid = NewGrid();
            var worldObject = new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(0L, 0L));
            grid.RegisterObject(worldObject);
            WorldChunk from = worldObject.Chunk;
            WorldPosition? eventPosition = null;
            worldObject.PositionChanged += (obj, previous, current) => eventPosition = current;

            grid.MoveObject(worldObject, new WorldPosition(64L, 64L));

            Assert.That(worldObject.Position, Is.EqualTo(new WorldPosition(64L, 64L)));
            Assert.That(worldObject.Chunk.Coordinate, Is.EqualTo(new ChunkCoordinate(1L, 1L)));
            Assert.That(worldObject.Chunk, Is.Not.SameAs(from));
            Assert.That(from.ObjectCount, Is.Zero);
            Assert.That(eventPosition, Is.EqualTo(new WorldPosition(64L, 64L)));
        }

        [Test]
        public void MoveObjectWithinChunkKeepsMembership()
        {
            WorldGrid grid = NewGrid();
            var worldObject = new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(10L, 10L));
            grid.RegisterObject(worldObject);
            WorldChunk chunk = worldObject.Chunk;

            grid.MoveObject(worldObject, new WorldPosition(20L, 20L));

            Assert.That(worldObject.Chunk, Is.SameAs(chunk));
            Assert.That(chunk.ObjectCount, Is.EqualTo(1));
        }

        [Test]
        public void RemoveChunkOnlyWhenEmpty()
        {
            WorldGrid grid = NewGrid();
            ChunkCoordinate coordinate = new ChunkCoordinate(0L, 0L);
            WorldChunk chunk = grid.GetOrCreateChunk(coordinate);
            chunk.AddObject(new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(0L, 0L)));

            Assert.That(grid.RemoveChunk(coordinate), Is.False);
            chunk.ResetContent();
            Assert.That(grid.RemoveChunk(coordinate), Is.True);
            Assert.That(grid.ChunkCount, Is.Zero);
        }

        [Test]
        public void UnregisterObjectRemovesFromGridAndChunk()
        {
            WorldGrid grid = NewGrid();
            var worldObject = new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(0L, 0L));
            grid.RegisterObject(worldObject);

            Assert.That(grid.UnregisterObject(worldObject), Is.True);
            Assert.That(grid.ObjectCount, Is.Zero);
            Assert.That(worldObject.Chunk, Is.Null);
            Assert.That(grid.UnregisterObject(worldObject), Is.False);
        }

        [Test]
        public void InvalidConfigurationIsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new WorldGrid(
                new WorldConfiguration(2L, 1L, new StreamingSettings(), new CameraSettings(), new LodSettings(), new PoolSettings())));
        }
    }
}
