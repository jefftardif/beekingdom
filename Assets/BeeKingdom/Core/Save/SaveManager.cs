using System;

namespace BeeKingdom.Core.Save
{
    public sealed class SaveManager
    {
        private readonly SaveEngine engine;
        private readonly string defaultSlot;

        public SaveManager(SaveEngine engine, string defaultSlot = "default")
        {
            this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
            this.defaultSlot = string.IsNullOrWhiteSpace(defaultSlot) ? "default" : defaultSlot;
        }

        public SaveSnapshot NewGame(string payload = "")
        {
            SaveSnapshot snapshot = engine.CreateSnapshot(payload);
            engine.Save(defaultSlot, snapshot);
            return snapshot;
        }

        public bool TryLoad(out SaveSnapshot snapshot)
        {
            return engine.TryLoad(defaultSlot, out snapshot);
        }

        public void Save(SaveSnapshot snapshot)
        {
            engine.Save(defaultSlot, snapshot);
        }
    }
}
