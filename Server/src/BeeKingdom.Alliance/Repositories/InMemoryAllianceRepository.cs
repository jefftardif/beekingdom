using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public sealed class InMemoryAllianceRepository : IAllianceRepository
{
    private readonly Dictionary<Guid, AllianceEntity> alliances = new();
    private readonly Dictionary<string, Guid> allianceIdBySlug = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Guid> createReceipts = new(StringComparer.Ordinal);

    // Membership keyed by (allianceId, playerId); a second index gives O(1) "does this player
    // already have an active alliance" lookups, which is the invariant we enforce most often.
    private readonly Dictionary<(Guid Alliance, Guid Player), AllianceMembership> memberships = new();
    private readonly Dictionary<Guid, Guid> activeAllianceByPlayer = new();

    private readonly Dictionary<Guid, AllianceApplication> applications = new();
    private readonly Dictionary<string, Guid> applicationReceipts = new(StringComparer.Ordinal);

    private readonly Dictionary<Guid, AllianceInvitation> invitations = new();
    private readonly Dictionary<string, Guid> invitationReceipts = new(StringComparer.Ordinal);

    private readonly object sync = new();

    public AllianceEntity Save(AllianceEntity alliance)
    {
        lock (sync)
        {
            alliances[alliance.AllianceId.Value] = alliance;
            if (!string.IsNullOrWhiteSpace(alliance.PublicSlug))
            {
                allianceIdBySlug[alliance.PublicSlug] = alliance.AllianceId.Value;
            }
            return alliance;
        }
    }

    public AllianceEntity? Get(AllianceId allianceId)
    {
        lock (sync) return alliances.GetValueOrDefault(allianceId.Value);
    }

    public AllianceEntity? GetBySlug(string slug)
    {
        lock (sync)
        {
            return allianceIdBySlug.TryGetValue(slug, out Guid id) ? alliances.GetValueOrDefault(id) : null;
        }
    }

    public IReadOnlyList<AllianceEntity> Search(AllianceSearchQuery query, out int totalCount)
    {
        lock (sync)
        {
            IEnumerable<AllianceEntity> filtered = alliances.Values.Where(a => a.Status == AllianceStatus.Active);
            if (!string.IsNullOrWhiteSpace(query.NameOrTag))
            {
                string needle = query.NameOrTag.Trim();
                filtered = filtered.Where(a =>
                    a.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    a.Tag.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(query.Language))
            {
                filtered = filtered.Where(a => string.Equals(a.Language, query.Language, StringComparison.OrdinalIgnoreCase));
            }
            if (query.JoinMode.HasValue)
            {
                filtered = filtered.Where(a => a.JoinMode == query.JoinMode.Value);
            }

            AllianceEntity[] all = filtered.OrderByDescending(a => a.MemberCount).ThenBy(a => a.AllianceId.Value).ToArray();
            totalCount = all.Length;
            return all.Skip(Math.Max(0, query.Offset)).Take(Math.Clamp(query.Limit, 1, 200)).ToArray();
        }
    }

    public AllianceId? GetCreateReceipt(PlayerId playerId, string clientRequestId)
    {
        lock (sync) return createReceipts.TryGetValue(ReceiptKey(playerId, clientRequestId), out Guid id) ? new AllianceId(id) : null;
    }

    public void SaveCreateReceipt(PlayerId playerId, string clientRequestId, AllianceId allianceId)
    {
        lock (sync) createReceipts[ReceiptKey(playerId, clientRequestId)] = allianceId.Value;
    }

    public AllianceMembership SaveMembership(AllianceMembership membership)
    {
        lock (sync)
        {
            var key = (membership.AllianceId.Value, membership.PlayerId.Value);
            memberships[key] = membership;
            if (membership.RemovedAtUtc == null)
            {
                activeAllianceByPlayer[membership.PlayerId.Value] = membership.AllianceId.Value;
            }
            else if (activeAllianceByPlayer.TryGetValue(membership.PlayerId.Value, out Guid current) && current == membership.AllianceId.Value)
            {
                activeAllianceByPlayer.Remove(membership.PlayerId.Value);
            }
            return membership;
        }
    }

    public AllianceMembership? GetActiveMembership(AllianceId allianceId, PlayerId playerId)
    {
        lock (sync)
        {
            AllianceMembership? membership = memberships.GetValueOrDefault((allianceId.Value, playerId.Value));
            return membership is { RemovedAtUtc: null } ? membership : null;
        }
    }

    public AllianceMembership? GetActiveMembershipForPlayer(PlayerId playerId)
    {
        lock (sync)
        {
            if (!activeAllianceByPlayer.TryGetValue(playerId.Value, out Guid allianceId)) return null;
            return memberships.GetValueOrDefault((allianceId, playerId.Value));
        }
    }

    public IReadOnlyList<AllianceMembership> ListActiveMembers(AllianceId allianceId)
    {
        lock (sync)
        {
            return memberships.Values
                .Where(m => m.AllianceId == allianceId && m.RemovedAtUtc == null)
                .OrderByDescending(m => m.Role)
                .ThenBy(m => m.JoinedAtUtc)
                .ToArray();
        }
    }

    public AllianceApplication SaveApplication(AllianceApplication application)
    {
        lock (sync) { applications[application.ApplicationId] = application; return application; }
    }

    public AllianceApplication? GetApplication(Guid applicationId)
    {
        lock (sync) return applications.GetValueOrDefault(applicationId);
    }

    public AllianceApplication? GetPendingApplication(AllianceId allianceId, PlayerId playerId)
    {
        lock (sync)
        {
            return applications.Values.FirstOrDefault(a =>
                a.AllianceId == allianceId && a.PlayerId == playerId && a.Status == AllianceApplicationStatus.Pending);
        }
    }

    public IReadOnlyList<AllianceApplication> ListPendingApplications(AllianceId allianceId)
    {
        lock (sync)
        {
            return applications.Values.Where(a => a.AllianceId == allianceId && a.Status == AllianceApplicationStatus.Pending)
                .OrderBy(a => a.SubmittedAtUtc).ToArray();
        }
    }

    public Guid? GetApplicationReceipt(PlayerId playerId, string clientRequestId)
    {
        lock (sync) return applicationReceipts.TryGetValue(ReceiptKey(playerId, clientRequestId), out Guid id) ? id : null;
    }

    public void SaveApplicationReceipt(PlayerId playerId, string clientRequestId, Guid applicationId)
    {
        lock (sync) applicationReceipts[ReceiptKey(playerId, clientRequestId)] = applicationId;
    }

    public AllianceInvitation SaveInvitation(AllianceInvitation invitation)
    {
        lock (sync) { invitations[invitation.InvitationId] = invitation; return invitation; }
    }

    public AllianceInvitation? GetInvitation(Guid invitationId)
    {
        lock (sync) return invitations.GetValueOrDefault(invitationId);
    }

    public AllianceInvitation? GetPendingInvitation(AllianceId allianceId, PlayerId invitedPlayerId)
    {
        lock (sync)
        {
            return invitations.Values.FirstOrDefault(i =>
                i.AllianceId == allianceId && i.InvitedPlayerId == invitedPlayerId && i.Status == AllianceInvitationStatus.Pending);
        }
    }

    public IReadOnlyList<AllianceInvitation> ListPendingInvitationsForPlayer(PlayerId playerId)
    {
        lock (sync)
        {
            return invitations.Values.Where(i => i.InvitedPlayerId == playerId && i.Status == AllianceInvitationStatus.Pending)
                .OrderByDescending(i => i.CreatedAtUtc).ToArray();
        }
    }

    public Guid? GetInvitationReceipt(PlayerId playerId, string clientRequestId)
    {
        lock (sync) return invitationReceipts.TryGetValue(ReceiptKey(playerId, clientRequestId), out Guid id) ? id : null;
    }

    public void SaveInvitationReceipt(PlayerId playerId, string clientRequestId, Guid invitationId)
    {
        lock (sync) invitationReceipts[ReceiptKey(playerId, clientRequestId)] = invitationId;
    }

    private static string ReceiptKey(PlayerId playerId, string clientRequestId) => $"{playerId.Value:N}:{clientRequestId}";

    // M042-CL: internal-only dump/restore surface used exclusively by
    // DurableJsonAllianceRepository (same assembly) to persist/rehydrate full state across a
    // server restart - not part of IAllianceRepository, never used by AllianceService. Dumps
    // return EVERY row (including removed memberships / non-pending applications/invitations)
    // so history isn't lost on the round trip; restores bypass business rules (this repository's
    // own Save*/receipt methods are already pure storage with no validation, so they're reused
    // directly for restore rather than duplicating them).
    internal IReadOnlyList<AllianceEntity> DumpAllAlliances() { lock (sync) return alliances.Values.ToArray(); }
    internal IReadOnlyList<AllianceMembership> DumpMemberships(Guid allianceId) { lock (sync) return memberships.Values.Where(m => m.AllianceId.Value == allianceId).ToArray(); }
    internal IReadOnlyList<AllianceApplication> DumpApplications(Guid allianceId) { lock (sync) return applications.Values.Where(a => a.AllianceId.Value == allianceId).ToArray(); }
    internal IReadOnlyList<AllianceInvitation> DumpInvitations(Guid allianceId) { lock (sync) return invitations.Values.Where(i => i.AllianceId.Value == allianceId).ToArray(); }
    internal IReadOnlyDictionary<string, Guid> DumpCreateReceipts() { lock (sync) return new Dictionary<string, Guid>(createReceipts, StringComparer.Ordinal); }
    internal IReadOnlyDictionary<string, Guid> DumpApplicationReceipts() { lock (sync) return new Dictionary<string, Guid>(applicationReceipts, StringComparer.Ordinal); }
    internal IReadOnlyDictionary<string, Guid> DumpInvitationReceipts() { lock (sync) return new Dictionary<string, Guid>(invitationReceipts, StringComparer.Ordinal); }

    internal void RestoreCreateReceiptRaw(string key, Guid allianceId) { lock (sync) createReceipts[key] = allianceId; }
    internal void RestoreApplicationReceiptRaw(string key, Guid applicationId) { lock (sync) applicationReceipts[key] = applicationId; }
    internal void RestoreInvitationReceiptRaw(string key, Guid invitationId) { lock (sync) invitationReceipts[key] = invitationId; }
}
