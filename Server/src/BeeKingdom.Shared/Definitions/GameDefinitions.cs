using BeeKingdom.Shared.Enums;

namespace BeeKingdom.Shared.Definitions;

public interface IDefinition
{
    string DefinitionId { get; }
    string DisplayName { get; }
}

public sealed record BuildingDefinition(string DefinitionId, string DisplayName, BuildingKind Kind, int MaxLevel) : IDefinition;

public sealed record ResourceDefinition(string DefinitionId, string DisplayName, ResourceKind Kind, double DefaultCapacity) : IDefinition;

public sealed record BeeDefinition(string DefinitionId, string DisplayName, BeeRole Role, double BaseHealth, double BaseEnergy) : IDefinition;

public sealed record FlowerDefinition(string DefinitionId, string DisplayName, string BiomeId, double NectarYield, double PollenYield) : IDefinition;
