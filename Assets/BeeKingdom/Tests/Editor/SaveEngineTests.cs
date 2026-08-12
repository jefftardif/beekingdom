using BeeKingdom.Core.Save;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SaveEngineTests
    {
        [Test]
        public void CreateSnapshotProducesVersionedValidSave()
        {
            SaveEngine engine = CreateEngine();

            SaveSnapshot snapshot = engine.CreateSnapshot("{\"honey\":10}");

            Assert.That(snapshot.SaveVersion, Is.EqualTo(1));
            Assert.That(snapshot.GameVersion, Is.Not.Empty);
            Assert.That(snapshot.Checksum, Is.Not.Empty);
            Assert.That(engine.Validate(snapshot), Is.True);
        }

        [Test]
        public void SaveAndLoadRoundTripsPayload()
        {
            SaveEngine engine = CreateEngine();
            SaveSnapshot snapshot = engine.CreateSnapshot("payload");

            engine.Save("slot-a", snapshot);

            Assert.That(engine.TryLoad("slot-a", out SaveSnapshot loaded), Is.True);
            Assert.That(loaded.Payload, Is.EqualTo("payload"));
            Assert.That(engine.Diagnostics.SaveCount, Is.EqualTo(1));
            Assert.That(engine.Diagnostics.LoadCount, Is.EqualTo(1));
        }

        [Test]
        public void CorruptedSaveFailsValidation()
        {
            InMemorySaveRepository repository = new InMemorySaveRepository();
            SaveEngine engine = CreateEngine(repository);
            SaveSnapshot snapshot = engine.CreateSnapshot("payload");
            SaveSerializer serializer = new SaveSerializer();
            repository.Write("slot-a", serializer.Serialize(snapshot).Replace(snapshot.Checksum, "0000"));

            Assert.That(engine.TryLoad("slot-a", out SaveSnapshot _), Is.False);
        }

        [Test]
        public void MigrationUpdatesOlderSnapshot()
        {
            InMemorySaveRepository repository = new InMemorySaveRepository();
            SaveMigrationManager migrations = new SaveMigrationManager();
            migrations.RegisterMigration(1, snapshot => snapshot.WithVersion(2));
            SaveEngine versionOne = new SaveEngine(repository);
            SaveEngine versionTwo = new SaveEngine(repository, new SaveSerializer(), new SaveDeserializer(), migrations, 2, "0.2.0");

            versionOne.Save("slot-a", versionOne.CreateSnapshot("payload"));

            Assert.That(versionTwo.TryLoad("slot-a", out SaveSnapshot migrated), Is.True);
            Assert.That(migrated.SaveVersion, Is.EqualTo(2));
            Assert.That(versionTwo.Diagnostics.MigrationCount, Is.EqualTo(1));
        }

        [Test]
        public void AutoSaveRecordsDiagnostics()
        {
            SaveEngine engine = CreateEngine();

            engine.AutoSave("slot-a", engine.CreateSnapshot("payload"));

            Assert.That(engine.HasSave("slot-a"), Is.True);
            Assert.That(engine.Diagnostics.AutoSaveCount, Is.EqualTo(1));
        }

        [Test]
        public void IncrementalSaveSkipsUnchangedPayload()
        {
            SaveEngine engine = CreateEngine();
            SaveSnapshot snapshot = engine.CreateSnapshot("payload");

            engine.Save("slot-a", snapshot);
            engine.Save("slot-a", snapshot);

            Assert.That(engine.Diagnostics.SaveCount, Is.EqualTo(1));
            Assert.That(engine.Diagnostics.IncrementalSkipCount, Is.EqualTo(1));
        }

        [Test]
        public void DeleteRemovesSave()
        {
            SaveEngine engine = CreateEngine();
            engine.Save("slot-a", engine.CreateSnapshot("payload"));

            engine.Delete("slot-a");

            Assert.That(engine.HasSave("slot-a"), Is.False);
            Assert.That(engine.Diagnostics.DeleteCount, Is.EqualTo(1));
        }

        private static SaveEngine CreateEngine()
        {
            return CreateEngine(new InMemorySaveRepository());
        }

        private static SaveEngine CreateEngine(InMemorySaveRepository repository)
        {
            return new SaveEngine(repository);
        }
    }
}
