using System;
using System.Threading.Tasks;
using BeeKingdom.WorldMap;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldStreamerTests
    {
        private static WorldStreamerHarness NewHarness(int loadRadius = 2, int unloadRadius = 4, Func<ChunkCoordinate, long, WorldChunkContent> contentFactory = null)
        {
            WorldConfiguration configuration = WorldMapTestConfiguration.Create(loadRadius: loadRadius, unloadRadius: unloadRadius);
            var grid = new WorldGrid(configuration);
            var source = new ScriptedContentSource(contentFactory);
            var focus = new ManualFocusProvider(new WorldPosition(0L, 0L));
            var loader = new WorldChunkLoader(grid, source, configuration.Streaming);
            var streamer = new WorldStreamer(grid, loader, configuration.Streaming, focus);
            return new WorldStreamerHarness(grid, source, focus, streamer);
        }

        [Test]
        public void TickRequestsChunksAroundFocus()
        {
            WorldStreamerHarness harness = NewHarness(loadRadius: 1, unloadRadius: 3);

            harness.Streamer.Tick();

            Assert.That(harness.Source.LoadCalls.Count, Is.EqualTo(9));
            Assert.That(harness.Source.LoadCalls, Contains.Item(new ChunkCoordinate(-1L, -1L)));
            Assert.That(harness.Source.LoadCalls, Contains.Item(new ChunkCoordinate(1L, 1L)));
        }

        [Test]
        public async Task CompletedLoadsBecomeLoadedChunks()
        {
            WorldStreamerHarness harness = NewHarness(loadRadius: 1, unloadRadius: 3);

            harness.Streamer.Tick();
            harness.Source.CompleteAll();
            await harness.Streamer.DrainAsync();

            Assert.That(harness.Streamer.LoadedChunkCount, Is.EqualTo(9));
            Assert.That(harness.Streamer.PendingLoadCount, Is.Zero);
            Assert.That(harness.Grid.ChunkCount, Is.EqualTo(9));
        }

        [Test]
        public async Task UnloadsChunksBeyondUnloadRadiusFarthestFirst()
        {
            WorldStreamerHarness harness = NewHarness(loadRadius: 0, unloadRadius: 2);

            harness.Streamer.Tick();
            harness.Source.CompleteAll();
            await harness.Streamer.DrainAsync();
            Assert.That(harness.Streamer.LoadedChunkCount, Is.EqualTo(1));

            harness.Focus.FocusPosition = new WorldPosition(64L * 4L, 0L);
            harness.Streamer.Tick();
            harness.Source.CompleteAll();
            await harness.Streamer.DrainAsync();

            Assert.That(harness.Streamer.LoadedChunkCount, Is.EqualTo(1));
            Assert.That(harness.Grid.TryGetChunk(new ChunkCoordinate(0L, 0L), out _), Is.False);
            Assert.That(harness.Grid.TryGetChunk(new ChunkCoordinate(4L, 0L), out _), Is.True);
            Assert.That(harness.Source.UnloadCallCount, Is.EqualTo(1));
        }

        [Test]
        public void FocusIsLastChunkAligned()
        {
            WorldStreamerHarness harness = NewHarness(loadRadius: 1, unloadRadius: 3);
            harness.Focus.FocusPosition = new WorldPosition(500L, 500L);

            harness.Streamer.Tick();

            Assert.That(harness.Streamer.LastFocusChunk, Is.EqualTo(new ChunkCoordinate(7L, 7L)));
        }

        [Test]
        public async Task ChunkEventsFireOnLoadCompletion()
        {
            WorldStreamerHarness harness = NewHarness(loadRadius: 0, unloadRadius: 2);
            int loadEvents = 0;
            harness.Streamer.ChunkLoadRequested += chunk => loadEvents++;

            harness.Streamer.Tick();
            harness.Source.CompleteAll();
            await harness.Streamer.DrainAsync();

            Assert.That(loadEvents, Is.EqualTo(1));
            Assert.That(harness.Streamer.LoadedChunkCount, Is.EqualTo(1));
        }

        [Test]
        public async Task UnloadAllUnloadsEverything()
        {
            WorldStreamerHarness harness = NewHarness(loadRadius: 1, unloadRadius: 3);

            harness.Streamer.Tick();
            harness.Source.CompleteAll();
            await harness.Streamer.DrainAsync();
            harness.Streamer.UnloadAll();
            await harness.Streamer.DrainAsync();

            Assert.That(harness.Streamer.LoadedChunkCount, Is.Zero);
            Assert.That(harness.Grid.ChunkCount, Is.Zero);
            Assert.That(harness.Source.UnloadCallCount, Is.EqualTo(9));
        }

        [Test]
        public async Task FailedLoadIsRetriedOnNextTick()
        {
            WorldStreamerHarness harness = NewHarness(loadRadius: 0, unloadRadius: 2);
            ChunkCoordinate chunk = new ChunkCoordinate(0L, 0L);

            harness.Streamer.Tick();
            harness.Source.FailNext(chunk);
            await DrainQuietlyAsync(harness);

            Assert.That(harness.Streamer.LoadedChunkCount, Is.Zero);
            Assert.That(harness.Streamer.PendingLoadCount, Is.Zero);

            harness.Streamer.Tick();
            harness.Source.CompleteNext(chunk);
            await harness.Streamer.DrainAsync();

            Assert.That(harness.Streamer.LoadedChunkCount, Is.EqualTo(1));
        }

        [Test]
        public void SecondTickDoesNotRequestChunksAlreadyInFlight()
        {
            WorldStreamerHarness harness = NewHarness(loadRadius: 1, unloadRadius: 3);

            harness.Streamer.Tick();
            harness.Streamer.Tick();

            Assert.That(harness.Source.LoadCalls.Count, Is.EqualTo(9));
        }

        [Test]
        public async Task MovedObjectIsUnregisteredWhenItsChunkUnloads()
        {
            WorldStreamerHarness harness = NewHarness(loadRadius: 1, unloadRadius: 3);
            var worldObject = new WorldObject(WorldObjectId.Fixed(42L), WorldObjectKind.None, new WorldPosition(10L, 10L));

            harness.Grid.RegisterObject(worldObject);
            harness.Streamer.Tick();
            harness.Source.CompleteAll();
            await harness.Streamer.DrainAsync();

            harness.Streamer.UnloadAll();
            await harness.Streamer.DrainAsync();

            Assert.That(worldObject.Chunk, Is.Null);
            Assert.That(harness.Grid.ObjectCount, Is.Zero);
        }

        [Test]
        public async Task ReloadingSameChunkProducesNewContent()
        {
            ChunkCoordinate chunk = new ChunkCoordinate(0L, 0L);
            WorldStreamerHarness harness = NewHarness(
                loadRadius: 0,
                unloadRadius: 2,
                contentFactory: (c, size) =>
                {
                    WorldPosition origin = WorldCoordinateSystem.ChunkOrigin(c, size);
                    var worldObject = new WorldObject(WorldObjectId.Fixed(1000L + c.X * 100L + c.Y), WorldObjectKind.None, origin);
                    return new WorldChunkContent(new[] { worldObject });
                });

            harness.Streamer.Tick();
            harness.Source.CompleteNext(chunk);
            await harness.Streamer.DrainAsync();
            int firstObjects = harness.Grid.GetOrCreateChunk(chunk).ObjectCount;
            Assert.That(firstObjects, Is.EqualTo(1));

            harness.Streamer.UnloadAll();
            await harness.Streamer.DrainAsync();
            Assert.That(harness.Streamer.LoadedChunkCount, Is.Zero);

            harness.Streamer.Tick();
            harness.Source.CompleteNext(chunk);
            await harness.Streamer.DrainAsync();

            Assert.That(harness.Streamer.LoadedChunkCount, Is.EqualTo(1));
            Assert.That(harness.Grid.GetOrCreateChunk(chunk).ObjectCount, Is.EqualTo(firstObjects));
        }

        private static async Task DrainQuietlyAsync(WorldStreamerHarness harness)
        {
            try
            {
                await harness.Streamer.DrainAsync();
            }
            catch (Exception)
            {
                // La charge a echoue : DrainAsync propage l'exception de la tache
                // fautive ; c'est le comportement attendu du test.
            }
        }

        private sealed class WorldStreamerHarness
        {
            public WorldGrid Grid { get; }
            public ScriptedContentSource Source { get; }
            public ManualFocusProvider Focus { get; }
            public WorldStreamer Streamer { get; }

            public WorldStreamerHarness(WorldGrid grid, ScriptedContentSource source, ManualFocusProvider focus, WorldStreamer streamer)
            {
                Grid = grid;
                Source = source;
                Focus = focus;
                Streamer = streamer;
            }
        }
    }
}
