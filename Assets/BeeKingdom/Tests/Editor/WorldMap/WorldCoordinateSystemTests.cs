using System.Collections.Generic;
using System.Linq;
using BeeKingdom.WorldMap;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldCoordinateSystemTests
    {
        [TestCase(0L, 0L, 0L, 0L)]
        [TestCase(63L, 63L, 0L, 0L)]
        [TestCase(64L, 64L, 1L, 1L)]
        [TestCase(-1L, -1L, -1L, -1L)]
        [TestCase(-64L, -65L, -1L, -2L)]
        [TestCase(14582L, -3811L, 227L, -60L)]
        public void ChunkOfUsesFloorDivision(long x, long y, long expectedChunkX, long expectedChunkY)
        {
            ChunkCoordinate chunk = WorldCoordinateSystem.ChunkOf(new WorldPosition(x, y), 64L);

            Assert.That(chunk.X, Is.EqualTo(expectedChunkX));
            Assert.That(chunk.Y, Is.EqualTo(expectedChunkY));
        }

        [Test]
        public void ChunkOriginIsTheMinimumCorner()
        {
            ChunkCoordinate chunk = new ChunkCoordinate(-2L, 3L);

            WorldPosition origin = WorldCoordinateSystem.ChunkOrigin(chunk, 64L);

            Assert.That(origin.X, Is.EqualTo(-128L));
            Assert.That(origin.Y, Is.EqualTo(192L));
        }

        [Test]
        public void LocalWithinChunkIsAlwaysPositive()
        {
            WorldPosition position = new WorldPosition(-1L, -1L);
            ChunkCoordinate chunk = new ChunkCoordinate(-1L, -1L);

            WorldPosition local = WorldCoordinateSystem.LocalWithinChunk(position, chunk, 64L);

            Assert.That(local.X, Is.EqualTo(63L));
            Assert.That(local.Y, Is.EqualTo(63L));
        }

        [TestCase(0L, 0L, true)]
        [TestCase(63L, 63L, true)]
        [TestCase(64L, 0L, false)]
        [TestCase(-1L, 0L, false)]
        public void ChunkContainsOnlyAcceptsItsOwnDomain(long x, long y, bool expected)
        {
            Assert.That(WorldCoordinateSystem.ChunkContains(new WorldPosition(x, y), new ChunkCoordinate(0L, 0L), 64L), Is.EqualTo(expected));
        }

        [Test]
        public void TileMappingRoundTrips()
        {
            WorldPosition position = new WorldPosition(14582L, -3811L);

            TileCoordinate tile = WorldCoordinateSystem.TileOf(position, 1L);
            WorldPosition origin = WorldCoordinateSystem.TileOrigin(tile, 1L);

            Assert.That(origin, Is.EqualTo(position));
            Assert.That(WorldCoordinateSystem.ChunkOfTile(tile, 64L), Is.EqualTo(WorldCoordinateSystem.ChunkOf(position, 64L)));
        }

        [Test]
        public void TileOriginFloorsNegativeTiles()
        {
            TileCoordinate tile = WorldCoordinateSystem.TileOf(new WorldPosition(-1L, -1L), 2L);

            Assert.That(tile.X, Is.EqualTo(-1L));
            Assert.That(WorldCoordinateSystem.TileOrigin(tile, 2L).X, Is.EqualTo(-2L));
        }

        [Test]
        public void PackedTileIndexRoundTripsWithinEncodableRange()
        {
            TileCoordinate[] samples =
            {
                new TileCoordinate(0L, 0L),
                new TileCoordinate(63L, -63L),
                new TileCoordinate(-1L, 1L),
                new TileCoordinate(-123456L, 789012L),
                new TileCoordinate(-2147483648L, 2147483647L)
            };

            foreach (TileCoordinate sample in samples)
            {
                long packed = WorldCoordinateSystem.PackTileIndex(sample.X, sample.Y);
                TileCoordinate unpacked = WorldCoordinateSystem.UnpackTileIndex(packed);
                Assert.That(unpacked, Is.EqualTo(sample), "round trip failed for " + sample);
            }
        }

        [TestCase(0L, 0L, 0L)]
        [TestCase(0L, 0L, 4L)]
        [TestCase(2L, 3L, 1L)]
        [TestCase(-2L, -3L, 3L)]
        [TestCase(100L, -50L, 100L)]
        public void ChebyshevDistanceIsSymmetricMax(long x, long y, long other)
        {
            ChunkCoordinate left = new ChunkCoordinate(x, y);
            ChunkCoordinate right = new ChunkCoordinate(other, other);

            Assert.That(WorldCoordinateSystem.ChebyshevDistance(left, right), Is.EqualTo(WorldCoordinateSystem.ChebyshevDistance(right, left)));
            Assert.That(WorldCoordinateSystem.ChebyshevDistance(left, right), Is.EqualTo(Mathx.Max(Mathx.Abs(x - other), Mathx.Abs(y - other))));
        }

        [Test]
        public void ChunksInRadiusByDistanceCoversTheSquareOnce()
        {
            ChunkCoordinate center = new ChunkCoordinate(5L, -3L);
            long radius = 3L;

            List<ChunkCoordinate> chunks = WorldCoordinateSystem.ChunksInRadiusByDistance(center, radius).ToList();

            Assert.That(chunks.Count, Is.EqualTo(49));
            Assert.That(chunks.Distinct().Count(), Is.EqualTo(49));
        }

        [Test]
        public void ChunksInRadiusByDistanceOrdersCenterFirstThenRings()
        {
            ChunkCoordinate center = new ChunkCoordinate(0L, 0L);

            List<ChunkCoordinate> chunks = WorldCoordinateSystem.ChunksInRadiusByDistance(center, 2L).ToList();

            Assert.That(chunks[0], Is.EqualTo(center));
            ChunkCoordinate last = chunks[chunks.Count - 1];
            Assert.That(WorldCoordinateSystem.ChebyshevDistance(center, last), Is.EqualTo(2L));
            for (int i = 1; i < chunks.Count; i++)
            {
                long previous = WorldCoordinateSystem.ChebyshevDistance(center, chunks[i - 1]);
                long current = WorldCoordinateSystem.ChebyshevDistance(center, chunks[i]);
                Assert.That(current, Is.GreaterThanOrEqualTo(previous), "distance must be non-decreasing");
            }
        }

        [Test]
        public void FloorDivAndModAreConsistentForNegatives()
        {
            Assert.That(WorldCoordinateSystem.FloorDiv(-1L, 64L), Is.EqualTo(-1L));
            Assert.That(WorldCoordinateSystem.FloorMod(-1L, 64L), Is.EqualTo(63L));
            Assert.That(WorldCoordinateSystem.FloorDiv(-64L, 64L), Is.EqualTo(-1L));
            Assert.That(WorldCoordinateSystem.FloorMod(-64L, 64L), Is.EqualTo(0L));
        }

        [Test]
        public void InvalidSizesAreRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => WorldCoordinateSystem.ChunkOf(new WorldPosition(0L, 0L), 0L));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => WorldCoordinateSystem.TileOf(new WorldPosition(0L, 0L), -1L));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => WorldCoordinateSystem.ChunksInRadiusByDistance(new ChunkCoordinate(0L, 0L), -1L));
        }

        private static class Mathx
        {
            public static long Abs(long value) => value < 0 ? -value : value;
            public static long Max(long a, long b) => a > b ? a : b;
        }
    }
}
