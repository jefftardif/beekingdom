using BeeKingdom.Shared.Enums;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Shared.DTO;

public sealed record BuildingDto(BuildingId BuildingId, ColonyId ColonyId, BuildingKind Kind, string DefinitionId, int Level, double Health);
