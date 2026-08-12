using BeeKingdom.WorldMap;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class WorldObjectPoolTests
    {
        private static WorldObjectPool NewPool(int maxPerKey = 0, int warmupPerKey = 0)
        {
            return new WorldObjectPool(new PoolSettings(maxPerKey, warmupPerKey));
        }

        [Test]
        public void RentCreatesAndReturnReuses()
        {
            WorldObjectPool pool = NewPool();
            pool.RegisterFactory("view", () => new FakeWorldObjectView());
            var owner = new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(0L, 0L));

            IWorldObjectView first = pool.Rent("view");
            pool.Return("view", first);
            IWorldObjectView second = pool.Rent("view");

            Assert.That(first, Is.SameAs(second));
            Assert.That(pool.Created, Is.EqualTo(1));
            Assert.That(pool.Available("view"), Is.Zero);
            Assert.That(pool.Outstanding("view"), Is.EqualTo(1));
        }

        [Test]
        public void RentAttachesOwnerAndReturnDetaches()
        {
            WorldObjectPool pool = NewPool();
            pool.RegisterFactory("view", () => new FakeWorldObjectView());
            var owner = new WorldObject(WorldObjectId.Fixed(1L), WorldObjectKind.None, new WorldPosition(0L, 0L));
            FakeWorldObjectView view = (FakeWorldObjectView)pool.Rent("view");

            view.Attach(owner);
            pool.Return("view", view);

            Assert.That(view.AttachCount, Is.EqualTo(1));
            Assert.That(view.DetachCount, Is.EqualTo(0));
        }

        [Test]
        public void MaxPerKeyCapsOutstandingInstances()
        {
            WorldObjectPool pool = NewPool(maxPerKey: 2);
            pool.RegisterFactory("view", () => new FakeWorldObjectView());

            Assert.That(pool.Rent("view"), Is.Not.Null);
            Assert.That(pool.Rent("view"), Is.Not.Null);
            Assert.That(pool.Rent("view"), Is.Null);
        }

        [Test]
        public void PrewarmCreatesWarmupInstances()
        {
            WorldObjectPool pool = NewPool(warmupPerKey: 3);
            pool.RegisterFactory("view", () => new FakeWorldObjectView());

            pool.Prewarm("view", 3);

            Assert.That(pool.Available("view"), Is.EqualTo(3));
            Assert.That(pool.Created, Is.EqualTo(3));
        }

        [Test]
        public void PrewarmRespectsMaxPerKey()
        {
            WorldObjectPool pool = NewPool(maxPerKey: 2, warmupPerKey: 5);
            pool.RegisterFactory("view", () => new FakeWorldObjectView());

            pool.Prewarm("view", 5);

            Assert.That(pool.Available("view"), Is.EqualTo(2));
        }

        [Test]
        public void ReturnWithoutRentThrows()
        {
            WorldObjectPool pool = NewPool();
            pool.RegisterFactory("view", () => new FakeWorldObjectView());

            Assert.Throws<System.InvalidOperationException>(() => pool.Return("view", new FakeWorldObjectView()));
        }

        [Test]
        public void RentWithoutFactoryThrows()
        {
            WorldObjectPool pool = NewPool();

            Assert.Throws<System.InvalidOperationException>(() => pool.Rent("missing"));
        }

        [Test]
        public void OutstandingCountsOnlyRented()
        {
            WorldObjectPool pool = NewPool();
            pool.RegisterFactory("view", () => new FakeWorldObjectView());

            IWorldObjectView rented = pool.Rent("view");
            pool.Rent("view");
            pool.Return("view", rented);

            Assert.That(pool.Outstanding("view"), Is.EqualTo(1));
            Assert.That(pool.RentedOutstanding, Is.EqualTo(1));
        }
    }

    public sealed class WorldSelectionTests
    {
        [Test]
        public void SelectReplacesInSingleMode()
        {
            var selection = new WorldSelection(multiSelectEnabled: false);
            WorldObjectId first = WorldObjectId.Fixed(1L);
            WorldObjectId second = WorldObjectId.Fixed(2L);

            selection.Select(first);
            selection.Select(second);

            Assert.That(selection.Count, Is.EqualTo(1));
            Assert.That(selection.Contains(second), Is.True);
            Assert.That(selection.Contains(first), Is.False);
        }

        [Test]
        public void MultiSelectAccumulatesAndClears()
        {
            var selection = new WorldSelection(multiSelectEnabled: true);
            WorldObjectId first = WorldObjectId.Fixed(1L);
            WorldObjectId second = WorldObjectId.Fixed(2L);

            selection.Select(first);
            selection.Select(second);
            selection.Deselect(first);

            Assert.That(selection.Count, Is.EqualTo(1));
            Assert.That(selection.Contains(second), Is.True);
            selection.Clear();
            Assert.That(selection.IsEmpty, Is.True);
        }

        [Test]
        public void NoneIdIsIgnored()
        {
            var selection = new WorldSelection();

            selection.Select(WorldObjectId.Fixed(0L));
            selection.Toggle(WorldObjectId.Fixed(0L));

            Assert.That(selection.IsEmpty, Is.True);
        }

        [Test]
        public void SelectionChangedReportsAddsAndRemoves()
        {
            var selection = new WorldSelection();
            WorldObjectId first = WorldObjectId.Fixed(1L);
            WorldObjectId second = WorldObjectId.Fixed(2L);
            WorldSelectionChanged last = null;
            selection.SelectionChanged += changed => last = changed;

            selection.Select(first);
            selection.Select(second);

            Assert.That(last.Added, Does.Contain(second));
            Assert.That(last.Removed, Does.Contain(first));
            Assert.That(last.Removed.Count, Is.EqualTo(1));
        }
    }

    public sealed class WorldLodTests
    {
        private static WorldLOD NewLod()
        {
            return new WorldLOD(new LodSettings(nearDistance: 100f, midDistance: 200f, farDistance: 300f));
        }

        [Test]
        public void DistanceBandsMatchThresholds()
        {
            WorldLOD lod = NewLod();
            var focus = new WorldVector2(0d, 0d);

            Assert.That(lod.Evaluate(focus, new WorldVector2(50d, 0d)), Is.EqualTo(WorldLodLevel.Lod0));
            Assert.That(lod.Evaluate(focus, new WorldVector2(150d, 0d)), Is.EqualTo(WorldLodLevel.Lod1));
            Assert.That(lod.Evaluate(focus, new WorldVector2(250d, 0d)), Is.EqualTo(WorldLodLevel.Lod2));
            Assert.That(lod.Evaluate(focus, new WorldVector2(350d, 0d)), Is.EqualTo(WorldLodLevel.Culled));
        }

        [Test]
        public void WorldPositionEvaluateUsesLongIntegers()
        {
            WorldLOD lod = NewLod();

            Assert.That(lod.Evaluate(new WorldPosition(0L, 0L), new WorldPosition(99L, 0L)), Is.EqualTo(WorldLodLevel.Lod0));
            Assert.That(lod.Evaluate(new WorldPosition(0L, 0L), new WorldPosition(301L, 0L)), Is.EqualTo(WorldLodLevel.Culled));
        }

        [Test]
        public void EvaluateChunkUsesRectangleDistance()
        {
            WorldLOD lod = NewLod();
            var focus = new WorldPosition(32L, 32L);

            Assert.That(lod.EvaluateChunk(focus, new ChunkCoordinate(0L, 0L), 64L), Is.EqualTo(WorldLodLevel.Lod0));
            Assert.That(lod.EvaluateChunk(focus, new ChunkCoordinate(1L, 0L), 64L), Is.EqualTo(WorldLodLevel.Lod0));
        }

        [Test]
        public void FarChunkIsCulled()
        {
            WorldLOD lod = NewLod();
            var focus = new WorldPosition(0L, 0L);

            Assert.That(lod.EvaluateChunk(focus, new ChunkCoordinate(10L, 0L), 64L), Is.EqualTo(WorldLodLevel.Culled));
        }
    }

    public sealed class WorldSaveTests
    {
        [Test]
        public void SaveAndLoadRoundTrip()
        {
            var store = new MemoryWorldMapSaveStore();
            var save = new WorldSave(store);
            var data = new WorldMapSaveData();
            data.SetCamera(new WorldVector2(14582d, -3811d), 128f);
            data.DebugOverlayVisible = true;

            save.Save(data);

            Assert.That(save.TryLoad(out WorldMapSaveData loaded), Is.True);
            Assert.That(loaded.CameraPosition, Is.EqualTo(new WorldVector2(14582d, -3811d)));
            Assert.That(loaded.CameraZoom, Is.EqualTo(128f));
            Assert.That(loaded.DebugOverlayVisible, Is.True);
        }

        [Test]
        public void TryLoadOnEmptyStoreReturnsFalse()
        {
            var store = new MemoryWorldMapSaveStore();
            var save = new WorldSave(store);

            Assert.That(save.TryLoad(out _), Is.False);
        }

        [Test]
        public void CorruptedOrNonFiniteDataIsRejected()
        {
            var store = new MemoryWorldMapSaveStore();
            var save = new WorldSave(store);
            store.Write(WorldSave.DefaultKey, "not json");
            Assert.That(save.TryLoad(out _), Is.False);

            var data = new WorldMapSaveData();
            data.CameraZoom = float.PositiveInfinity;
            save.Save(data);
            Assert.That(save.TryLoad(out _), Is.False);
        }

        [Test]
        public void ResetDeletesTheEntry()
        {
            var store = new MemoryWorldMapSaveStore();
            var save = new WorldSave(store);
            save.Save(new WorldMapSaveData());

            save.Reset();

            Assert.That(save.TryLoad(out _), Is.False);
        }
    }
}
