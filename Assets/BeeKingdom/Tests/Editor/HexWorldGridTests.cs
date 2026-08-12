using BeeKingdom.World;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class HexWorldGridTests
    {
        [Test]
        public void CoordinatesExposeCubeInvariant()
        {
            HexCoordinates coordinates = new HexCoordinates(3, -5);

            Assert.That(coordinates.Q + coordinates.R + coordinates.S, Is.EqualTo(0));
            Assert.That(coordinates.DistanceTo(new HexCoordinates(0, 0)), Is.EqualTo(5));
        }

        [Test]
        public void NeighborsAreSixUniqueCells()
        {
            HexGrid grid = new HexGrid();

            var neighbors = grid.GetNeighbors(new HexCoordinates(0, 0));

            Assert.That(neighbors.Count, Is.EqualTo(6));
            Assert.That(neighbors[0].Coordinates, Is.EqualTo(new HexCoordinates(1, 0)));
            Assert.That(grid.Cells.Count, Is.EqualTo(6));
        }

        [Test]
        public void ChunkMappingHandlesNegativeCoordinates()
        {
            HexCoordinates coordinates = new HexCoordinates(-1, -17);

            WorldChunkCoordinate chunk = coordinates.ToChunkCoordinate(16);

            Assert.That(chunk.X, Is.EqualTo(-1));
            Assert.That(chunk.Y, Is.EqualTo(-2));
        }

        [Test]
        public void WorldRegionsMapToHexCells()
        {
            WorldManager manager = new WorldManager();
            WorldState world = manager.CreateWorld(new WorldSeed("hex-world"), WorldGenerationProfile.CreateDefault(WorldGenerationProfileType.Tutorial));

            HexGrid grid = HexGrid.FromWorld(world, 16);

            Assert.That(grid.Cells.Count, Is.EqualTo(world.Regions.Count * 7));
            Assert.That(grid.RegionIndex.GetCells("region-0-0").Count, Is.EqualTo(7));
        }

        [Test]
        public void StreamingMarksCellsLoadedAndUnloaded()
        {
            HexGrid grid = new HexGrid(16);
            HexCell cell = grid.CreateCell(new HexCoordinates(1, 1), "region");
            WorldChunkCoordinate chunk = cell.ChunkCoordinate;

            grid.LoadChunk(chunk);
            Assert.That(cell.IsLoaded, Is.True);
            Assert.That(grid.IsChunkLoaded(chunk), Is.True);

            grid.UnloadChunk(chunk);
            Assert.That(cell.IsLoaded, Is.False);
            Assert.That(grid.IsChunkLoaded(chunk), Is.False);
        }

        [Test]
        public void SnapshotPreservesCellsForSerialization()
        {
            HexGrid grid = new HexGrid(8);
            grid.CreateCell(new HexCoordinates(2, 3), "region-a");

            HexGridSnapshot snapshot = grid.CreateSnapshot();

            Assert.That(snapshot.ChunkSize, Is.EqualTo(8));
            Assert.That(snapshot.Cells.Count, Is.EqualTo(1));
            Assert.That(snapshot.Cells[0].RegionId, Is.EqualTo("region-a"));
        }

        [Test]
        public void LargeGridCreationRemainsStable()
        {
            HexGrid grid = new HexGrid(32);
            for (int q = -50; q <= 50; q++)
            {
                for (int r = -50; r <= 50; r++)
                {
                    grid.CreateCell(new HexCoordinates(q, r));
                }
            }

            Assert.That(grid.Cells.Count, Is.EqualTo(10201));
            Assert.That(grid.GetNeighbors(new HexCoordinates(0, 0)).Count, Is.EqualTo(6));
        }
    }
}
