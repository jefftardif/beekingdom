using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public interface IAllianceActivityRepository
{
    AllianceActivityEvent Append(AllianceActivityEvent activity);

    // Idempotent append: if an activity with the same (AllianceId, Type, dedupeKey) was already
    // recorded, returns the existing one instead of duplicating - used by retry-safe callers
    // (e.g. a retried MemberJoined publish) without needing a separate receipt table per event.
    AllianceActivityEvent AppendIdempotent(AllianceActivityEvent activity, string dedupeKey);

    // Cursor pagination ordered by Sequence descending (most recent first) - stable even as new
    // events are appended concurrently, since Sequence never changes for an existing row.
    AllianceActivityPage ListForAlliance(AllianceId allianceId, long? beforeSequence, int limit, AllianceActivityVisibility maxVisibility);

    // Public-Web-safe variant: never returns anything above Public visibility, regardless of what
    // the caller asks for - the safety check lives here, not just in the caller.
    AllianceActivityPage ListPublicForAlliance(AllianceId allianceId, long? beforeSequence, int limit);
}
