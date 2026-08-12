using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Core.Save
{
    public sealed class SaveEngine : ISaveService
    {
        private const int DefaultSaveVersion = 1;
        private const string DefaultGameVersion = "0.1.0";

        private readonly SaveRepository repository;
        private readonly SaveSerializer serializer;
        private readonly SaveDeserializer deserializer;
        private readonly SaveMigrationManager migrationManager;
        private readonly int currentSaveVersion;
        private readonly string gameVersion;
        private ServiceState state = ServiceState.Registered;
        private Exception failure;

        public string ServiceName => nameof(SaveEngine);
        public int Priority => 50;
        public IReadOnlyList<Type> Dependencies => Array.Empty<Type>();
        public ServiceState State => state;
        public bool IsInitialized => state != ServiceState.Registered;
        public Exception Failure => failure;
        public SaveDiagnostics Diagnostics { get; } = new SaveDiagnostics();

        public SaveEngine(SaveRepository repository)
            : this(repository, new SaveSerializer(), new SaveDeserializer(), new SaveMigrationManager(), DefaultSaveVersion, DefaultGameVersion)
        {
        }

        public SaveEngine(
            SaveRepository repository,
            SaveSerializer serializer,
            SaveDeserializer deserializer,
            SaveMigrationManager migrationManager,
            int currentSaveVersion = DefaultSaveVersion,
            string gameVersion = DefaultGameVersion)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
            this.migrationManager = migrationManager ?? throw new ArgumentNullException(nameof(migrationManager));
            this.currentSaveVersion = currentSaveVersion;
            this.gameVersion = gameVersion ?? DefaultGameVersion;
        }

        public void Initialize(IServiceRegistry services) { state = ServiceState.Initialized; }
        public void Start() { state = ServiceState.Running; }
        public void Tick(float deltaTime) { }
        public void FixedTick(float deltaTime) { }
        public void LateTick(float deltaTime) { }
        public void Pause() { state = ServiceState.Paused; }
        public void Resume() { state = ServiceState.Running; }
        public void Shutdown() { state = ServiceState.Disposed; }
        public void Dispose() { Shutdown(); }
        public void Fail(Exception exception)
        {
            failure = exception;
            state = ServiceState.Failed;
        }

        public SaveSnapshot CreateSnapshot(string payload = "")
        {
            DateTime now = DateTime.UtcNow;
            SaveSnapshot snapshot = new SaveSnapshot(currentSaveVersion, gameVersion, now, now, string.Empty, payload);
            return serializer.EnsureChecksum(snapshot);
        }

        public void Save(string slot, SaveSnapshot snapshot)
        {
            if (repository.TryRead(slot, out string existingData) &&
                deserializer.TryDeserialize(existingData, out SaveSnapshot existing) &&
                existing.SaveVersion == snapshot.SaveVersion &&
                existing.GameVersion == snapshot.GameVersion &&
                existing.Payload == snapshot.Payload)
            {
                Diagnostics.RecordIncrementalSkip();
                return;
            }

            SaveSnapshot normalized = serializer.EnsureChecksum(snapshot.Touch());
            repository.Write(slot, serializer.Serialize(normalized));
            Diagnostics.RecordSave(slot);
        }

        public bool TryLoad(string slot, out SaveSnapshot snapshot)
        {
            snapshot = null;
            if (!repository.TryRead(slot, out string data) || !deserializer.TryDeserialize(data, out SaveSnapshot loaded))
            {
                return false;
            }

            if (!Validate(loaded))
            {
                return false;
            }

            snapshot = Migrate(loaded);
            Diagnostics.RecordLoad(slot);
            return true;
        }

        public bool Validate(SaveSnapshot snapshot)
        {
            if (snapshot == null)
            {
                Diagnostics.RecordValidationFailure();
                return false;
            }

            string expected = SaveChecksum.Calculate(new SaveSnapshot(
                snapshot.SaveVersion,
                snapshot.GameVersion,
                snapshot.CreatedAtUtc,
                snapshot.LastModifiedUtc,
                string.Empty,
                snapshot.Payload));

            bool isValid = expected == snapshot.Checksum;
            if (!isValid)
            {
                Diagnostics.RecordValidationFailure();
            }

            return isValid;
        }

        public SaveSnapshot Migrate(SaveSnapshot snapshot)
        {
            if (snapshot.SaveVersion >= currentSaveVersion)
            {
                return snapshot;
            }

            SaveSnapshot migrated = migrationManager.Migrate(snapshot, currentSaveVersion, Diagnostics);
            return serializer.EnsureChecksum(migrated);
        }

        public void AutoSave(string slot, SaveSnapshot snapshot)
        {
            Save(slot, snapshot);
            Diagnostics.RecordAutoSave(slot);
        }

        public bool HasSave(string key)
        {
            return repository.Exists(key);
        }

        public void Save(string key, string payload)
        {
            Save(key, CreateSnapshot(payload));
        }

        public bool TryLoad(string key, out string payload)
        {
            if (TryLoad(key, out SaveSnapshot snapshot))
            {
                payload = snapshot.Payload;
                return true;
            }

            payload = null;
            return false;
        }

        public void Delete(string key)
        {
            repository.Delete(key);
            Diagnostics.RecordDelete(key);
        }
    }
}
