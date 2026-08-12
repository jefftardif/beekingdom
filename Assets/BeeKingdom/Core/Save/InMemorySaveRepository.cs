using System.Collections.Generic;

namespace BeeKingdom.Core.Save
{
    public sealed class InMemorySaveRepository : SaveRepository
    {
        private readonly Dictionary<string, string> saves = new Dictionary<string, string>();

        public override bool Exists(string slot)
        {
            return saves.ContainsKey(slot);
        }

        public override void Write(string slot, string data)
        {
            saves[slot] = data;
        }

        public override bool TryRead(string slot, out string data)
        {
            return saves.TryGetValue(slot, out data);
        }

        public override void Delete(string slot)
        {
            saves.Remove(slot);
        }
    }
}
