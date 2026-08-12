using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Shared.DTO;

public sealed record ColonyDto(ColonyId ColonyId, PlayerId OwnerId, string Name, int Population, IReadOnlyList<ChamberDto> Chambers);
