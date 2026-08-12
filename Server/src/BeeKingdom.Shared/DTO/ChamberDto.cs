using BeeKingdom.Shared.Enums;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Shared.DTO;

public sealed record ChamberDto(ChamberId ChamberId, ColonyId ColonyId, ChamberKind Kind, int Capacity, HexCoordinate Position);
