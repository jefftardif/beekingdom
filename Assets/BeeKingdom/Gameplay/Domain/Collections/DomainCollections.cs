using System;
using System.Collections;
using System.Collections.Generic;
using BeeKingdom.Gameplay.Domain.Entities;
using BeeKingdom.Gameplay.Domain.Identifiers;
using GameplayHive = BeeKingdom.Gameplay.Domain.Entities.Hive;

namespace BeeKingdom.Gameplay.Domain.Collections
{
    [Serializable]
    public sealed class BeeCollection : IReadOnlyCollection<Bee>
    {
        private readonly List<Bee> bees = new List<Bee>();
        public int Count => bees.Count;
        public void Add(Bee bee) { if (bee == null) throw new ArgumentNullException(nameof(bee)); bees.Add(bee); }
        public bool TryGet(BeeId id, out Bee bee) { bee = bees.Find(item => item.Id.Equals(id)); return bee != null; }
        public IEnumerator<Bee> GetEnumerator() => bees.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Serializable]
    public sealed class BuildingCollection : IReadOnlyCollection<Building>
    {
        private readonly List<Building> buildings = new List<Building>();
        public int Count => buildings.Count;
        public void Add(Building building) { if (building == null) throw new ArgumentNullException(nameof(building)); buildings.Add(building); }
        public bool TryGet(BuildingId id, out Building building) { building = buildings.Find(item => item.Id.Equals(id)); return building != null; }
        public IEnumerator<Building> GetEnumerator() => buildings.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Serializable]
    public sealed class HiveCollection : IReadOnlyCollection<GameplayHive>
    {
        private readonly List<GameplayHive> hives = new List<GameplayHive>();
        public int Count => hives.Count;
        public void Add(GameplayHive hive) { if (hive == null) throw new ArgumentNullException(nameof(hive)); hives.Add(hive); }
        public bool TryGet(HiveId id, out GameplayHive hive) { hive = hives.Find(item => item.Id.Equals(id)); return hive != null; }
        public IEnumerator<GameplayHive> GetEnumerator() => hives.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Serializable]
    public sealed class InventoryCollection : IReadOnlyCollection<ResourceStack>
    {
        private readonly List<ResourceStack> resources = new List<ResourceStack>();
        public int Count => resources.Count;
        public void Add(ResourceStack stack) { if (stack == null) throw new ArgumentNullException(nameof(stack)); resources.Add(stack); }
        public IEnumerator<ResourceStack> GetEnumerator() => resources.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
