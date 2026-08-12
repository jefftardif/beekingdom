using System.Collections.Generic;

namespace BeeKingdom.Core.Attributes
{
    public sealed class GameplayAttributeSnapshot
    {
        public int Version { get; }
        public string OwnerId { get; }
        public string SetId { get; }
        public IReadOnlyList<GameplayAttributeSnapshotEntry> Entries { get; }

        public GameplayAttributeSnapshot(int version, string ownerId, string setId, IReadOnlyList<GameplayAttributeSnapshotEntry> entries)
        {
            Version = version;
            OwnerId = ownerId;
            SetId = setId;
            Entries = entries;
        }
    }

    public readonly struct GameplayAttributeSnapshotEntry
    {
        public string AttributeId { get; }
        public double BaseValue { get; }
        public double FinalValue { get; }

        public GameplayAttributeSnapshotEntry(string attributeId, double baseValue, double finalValue)
        {
            AttributeId = attributeId;
            BaseValue = baseValue;
            FinalValue = finalValue;
        }
    }
}
