using System;
using System.Collections.Generic;

namespace BeeKingdom.Core.Save
{
    public sealed class SaveMigrationManager
    {
        private readonly Dictionary<int, Func<SaveSnapshot, SaveSnapshot>> migrations = new Dictionary<int, Func<SaveSnapshot, SaveSnapshot>>();

        public void RegisterMigration(int fromVersion, Func<SaveSnapshot, SaveSnapshot> migration)
        {
            migrations[fromVersion] = migration ?? throw new ArgumentNullException(nameof(migration));
        }

        public SaveSnapshot Migrate(SaveSnapshot snapshot, int targetVersion, SaveDiagnostics diagnostics)
        {
            SaveSnapshot current = snapshot;
            while (current.SaveVersion < targetVersion)
            {
                if (!migrations.TryGetValue(current.SaveVersion, out Func<SaveSnapshot, SaveSnapshot> migration))
                {
                    throw new InvalidOperationException($"Missing save migration from version {current.SaveVersion}.");
                }

                current = migration(current);
                diagnostics?.RecordMigration();
            }

            return current;
        }
    }
}
