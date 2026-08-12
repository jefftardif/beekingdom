using System.Collections.Generic;
using BeeKingdom.Economy;

namespace BeeKingdom.Gameplay
{
    public sealed class StarterResourceProfile
    {
        private readonly Dictionary<ResourceType, double> amounts;

        public IReadOnlyDictionary<ResourceType, double> Amounts => amounts;
        public double CellCapacity { get; }

        public StarterResourceProfile(IReadOnlyDictionary<ResourceType, double> amounts, double cellCapacity)
        {
            this.amounts = new Dictionary<ResourceType, double>(amounts ?? new Dictionary<ResourceType, double>());
            CellCapacity = cellCapacity <= 0d ? 100d : cellCapacity;
        }

        public static StarterResourceProfile CreateDefault()
        {
            return new StarterResourceProfile(
                new Dictionary<ResourceType, double>
                {
                    { ResourceType.Nectar, 80d },
                    { ResourceType.Pollen, 60d },
                    { ResourceType.Water, 40d },
                    { ResourceType.Wax, 80d },
                    { ResourceType.Honey, 40d }
                },
                100d);
        }
    }
}
