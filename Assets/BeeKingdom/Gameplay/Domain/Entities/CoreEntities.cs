using System;
using BeeKingdom.Gameplay.Domain.Collections;
using BeeKingdom.Gameplay.Domain.Enums;
using BeeKingdom.Gameplay.Domain.Identifiers;
using BeeKingdom.Gameplay.Domain.Interfaces;
using BeeKingdom.Gameplay.Domain.ValueObjects;

namespace BeeKingdom.Gameplay.Domain.Entities
{
    /// <summary>
    /// Aggregate root. Invariant: a player owns zero or more hives.
    /// </summary>
    [Serializable]
    public sealed class Player : DomainEntity<PlayerId>, IAggregateRoot
    {
        public HiveCollection Hives { get; }

        public Player(PlayerId id, DateTime createdAt, HiveCollection hives = null) : base(id, createdAt)
        {
            Hives = hives ?? new HiveCollection();
        }
    }

    /// <summary>
    /// Aggregate root. Invariant: a hive has exactly one queen and owns its bees/buildings.
    /// </summary>
    [Serializable]
    public sealed class Hive : DomainEntity<HiveId>, IAggregateRoot
    {
        public PlayerId OwnerPlayerId { get; }
        public Queen Queen { get; }
        public BeeCollection Bees { get; }
        public BuildingCollection Buildings { get; }
        public Inventory Inventory { get; }

        public Hive(HiveId id, PlayerId ownerPlayerId, Queen queen, DateTime createdAt, BeeCollection bees = null, BuildingCollection buildings = null, Inventory inventory = null) : base(id, createdAt)
        {
            Queen = queen ?? throw new ArgumentNullException(nameof(queen), "A hive must always have exactly one queen.");
            OwnerPlayerId = ownerPlayerId;
            Bees = bees ?? new BeeCollection();
            Buildings = buildings ?? new BuildingCollection();
            Inventory = inventory;
        }
    }

    [Serializable]
    public sealed class Queen : DomainEntity<BeeId>
    {
        public HiveId HiveId { get; }
        public Health Health { get; }
        public Energy Energy { get; }

        public Queen(BeeId id, HiveId hiveId, Health health, Energy energy, DateTime createdAt) : base(id, createdAt)
        {
            HiveId = hiveId;
            Health = health;
            Energy = energy;
        }
    }

    /// <summary>
    /// Invariant: a bee always belongs to a hive.
    /// </summary>
    [Serializable]
    public sealed class Bee : DomainEntity<BeeId>
    {
        public HiveId HiveId { get; }
        public BeeRole Role { get; }
        public BeeState State { get; }
        public Health Health { get; }
        public Energy Energy { get; }

        public Bee(BeeId id, HiveId hiveId, BeeRole role, BeeState state, Health health, Energy energy, DateTime createdAt) : base(id, createdAt)
        {
            HiveId = hiveId;
            Role = role;
            State = state;
            Health = health;
            Energy = energy;
        }
    }

    /// <summary>
    /// Invariant: a building always belongs to a hive.
    /// </summary>
    [Serializable]
    public sealed class Building : DomainEntity<BuildingId>
    {
        public HiveId HiveId { get; }
        public BuildingType Type { get; }
        public Position2D Position { get; }

        public Building(BuildingId id, HiveId hiveId, BuildingType type, Position2D position, DateTime createdAt) : base(id, createdAt)
        {
            HiveId = hiveId;
            Type = type;
            Position = position;
        }
    }

    /// <summary>
    /// Invariant: an inventory belongs to exactly one owner entity.
    /// OwnerId remains a typed string because owner aggregate type can vary.
    /// </summary>
    [Serializable]
    public sealed class Inventory : DomainEntity<InventoryId>
    {
        public string OwnerId { get; }
        public InventoryCollection Resources { get; }

        public Inventory(InventoryId id, string ownerId, DateTime createdAt, InventoryCollection resources = null) : base(id, createdAt)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Inventory owner is required.", nameof(ownerId));
            OwnerId = ownerId;
            Resources = resources ?? new InventoryCollection();
        }
    }

    [Serializable]
    public sealed class ResourceStack : DomainEntity<ResourceStackId>
    {
        public ResourceAmount Amount { get; }

        public ResourceStack(ResourceStackId id, ResourceAmount amount, DateTime createdAt) : base(id, createdAt)
        {
            Amount = amount;
        }
    }

    [Serializable]
    public sealed class ResearchTree : DomainEntity<ResearchId>
    {
        public HiveId HiveId { get; }

        public ResearchTree(ResearchId id, HiveId hiveId, DateTime createdAt) : base(id, createdAt)
        {
            HiveId = hiveId;
        }
    }

    [Serializable]
    public sealed class Region : DomainEntity<RegionId>, IAggregateRoot
    {
        public RegionType Type { get; }
        public WeatherType Weather { get; }
        public Season Season { get; }

        public Region(RegionId id, RegionType type, WeatherType weather, Season season, DateTime createdAt) : base(id, createdAt)
        {
            Type = type;
            Weather = weather;
            Season = season;
        }
    }

    [Serializable]
    public sealed class FlowerNode : DomainEntity<FlowerNodeId>
    {
        public RegionId RegionId { get; }
        public WorldCoordinate Coordinate { get; }

        public FlowerNode(FlowerNodeId id, RegionId regionId, WorldCoordinate coordinate, DateTime createdAt) : base(id, createdAt)
        {
            RegionId = regionId;
            Coordinate = coordinate;
        }
    }

    [Serializable]
    public sealed class Army : DomainEntity<ArmyId>
    {
        public HiveId HiveId { get; }
        public BeeCollection Bees { get; }

        public Army(ArmyId id, HiveId hiveId, BeeCollection bees, DateTime createdAt) : base(id, createdAt)
        {
            HiveId = hiveId;
            Bees = bees ?? new BeeCollection();
        }
    }

    [Serializable]
    public sealed class Alliance : DomainEntity<AllianceId>, IAggregateRoot
    {
        public string Name { get; }

        public Alliance(AllianceId id, string name, DateTime createdAt) : base(id, createdAt)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Alliance name is required.", nameof(name));
            Name = name;
        }
    }
}
