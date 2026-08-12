using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Shared.DTO;

public sealed record InventoryDto(ColonyId ColonyId, IReadOnlyList<ResourceDto> Resources);
