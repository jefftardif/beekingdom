using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Shared.DTO;

public sealed record AllianceDto(AllianceId AllianceId, string Name, PlayerId LeaderId, int MemberCount);
