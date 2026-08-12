using System.Collections.Generic;

namespace BeeKingdom.Economy
{
    public sealed class ResourceFlowGraph
    {
        private readonly Dictionary<string, ResourceStorage> storages = new Dictionary<string, ResourceStorage>();

        public ResourceStorage GetOrCreateStorage(string storageId)
        {
            if (!storages.TryGetValue(storageId, out ResourceStorage storage))
            {
                storage = new ResourceStorage(storageId);
                storages[storageId] = storage;
            }

            return storage;
        }

        public bool TryGetStorage(string storageId, out ResourceStorage storage)
        {
            return storages.TryGetValue(storageId, out storage);
        }
    }
}
