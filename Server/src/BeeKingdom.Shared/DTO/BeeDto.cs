using BeeKingdom.Shared.Enums;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Shared.DTO;

public sealed record BeeDto(BeeId BeeId, ColonyId ColonyId, BeeRole Role, double Health, double Energy);
