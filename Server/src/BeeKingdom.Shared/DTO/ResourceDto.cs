using BeeKingdom.Shared.Enums;

namespace BeeKingdom.Shared.DTO;

public sealed record ResourceDto(ResourceKind Kind, double Amount, double Capacity);
