using System;

namespace BeeKingdom.Buildings.Interaction
{
    public enum BuildingState
    {
        Preview = 0,
        Active = 1,
        Future = 2
    }

    public enum BuildingResource
    {
        None = 0,
        Honey = 1,
        Wax = 2,
        Pollen = 3
    }

    [Flags]
    public enum BuildingCapabilities
    {
        None = 0,
        Production = 1 << 0,
        Upgrade = 1 << 1,
        Research = 1 << 2
    }
}