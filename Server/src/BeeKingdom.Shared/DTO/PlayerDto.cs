using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Shared.DTO;

public sealed record PlayerDto(PlayerId PlayerId, string DisplayName, int Level, DateTimeOffset CreatedAtUtc);
