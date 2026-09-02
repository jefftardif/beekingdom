using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

// M042-CL: Alpha durable persistence - same tier as DurableJsonHiveStateRepository (one JSON
// file per aggregate, atomic write, no SQL). Delegates ALL query/business logic to an inner
// InMemoryAllianceRepository (fully reused, already tested by AllianceServiceTests) and adds a
// disk-backed write-through + startup replay on top, via that class's internal Dump*/Restore*
// surface (same assembly). See ALLIANCE_PLATFORM_ARCHITECTURE.md section 17 for the rationale
// (why not SQL yet) and Docs/AI/Missions/M042-CL-Alliance-Platform-Integration.md for the
// restart-survival test that validates this class end-to-end.
public sealed class DurableJsonAllianceRepository : IAllianceRepository
{
    private sealed class AllianceFileBundle
    {
        public AllianceEntity? Alliance { get; set; }
        public List<AllianceMembership> Memberships { get; set; } = new();
        public List<AllianceApplication> Applications { get; set; } = new();
        public List<AllianceInvitation> Invitations { get; set; } = new();
    }

    private sealed class ReceiptsFile
    {
        public Dictionary<string, Guid> Create { get; set; } = new();
        public Dictionary<string, Guid> Application { get; set; } = new();
        public Dictionary<string, Guid> Invitation { get; set; } = new();
    }

    private readonly InMemoryAllianceRepository inner = new();
    private readonly string root;
    private readonly string receiptsPath;
    private readonly object writeLock = new();

    public DurableJsonAllianceRepository(string rootDirectory)
    {
        root = rootDirectory;
        receiptsPath = Path.Combine(root, "_receipts.json");
        LoadAll();
    }

    private void LoadAll()
    {
        foreach (string file in DurableJsonFileIo.EnumerateJsonFiles(root))
        {
            if (Path.GetFileName(file).StartsWith("_", StringComparison.Ordinal)) continue;
            AllianceFileBundle? bundle = DurableJsonFileIo.ReadIfExists<AllianceFileBundle>(file);
            if (bundle?.Alliance == null) continue;
            inner.Save(bundle.Alliance);
            foreach (AllianceMembership membership in bundle.Memberships) inner.SaveMembership(membership);
            foreach (AllianceApplication application in bundle.Applications) inner.SaveApplication(application);
            foreach (AllianceInvitation invitation in bundle.Invitations) inner.SaveInvitation(invitation);
        }

        ReceiptsFile? receipts = DurableJsonFileIo.ReadIfExists<ReceiptsFile>(receiptsPath);
        if (receipts == null) return;
        foreach (var kv in receipts.Create) inner.RestoreCreateReceiptRaw(kv.Key, kv.Value);
        foreach (var kv in receipts.Application) inner.RestoreApplicationReceiptRaw(kv.Key, kv.Value);
        foreach (var kv in receipts.Invitation) inner.RestoreInvitationReceiptRaw(kv.Key, kv.Value);
    }

    private void PersistAlliance(Guid allianceId)
    {
        lock (writeLock)
        {
            AllianceEntity? alliance = inner.Get(new AllianceId(allianceId));
            if (alliance == null) return; // nothing to persist yet (shouldn't happen for a known id)
            var bundle = new AllianceFileBundle
            {
                Alliance = alliance,
                Memberships = inner.DumpMemberships(allianceId).ToList(),
                Applications = inner.DumpApplications(allianceId).ToList(),
                Invitations = inner.DumpInvitations(allianceId).ToList()
            };
            DurableJsonFileIo.WriteAtomic(Path.Combine(root, allianceId.ToString("N") + ".json"), bundle);
        }
    }

    private void PersistReceipts()
    {
        lock (writeLock)
        {
            var file = new ReceiptsFile
            {
                Create = inner.DumpCreateReceipts().ToDictionary(kv => kv.Key, kv => kv.Value),
                Application = inner.DumpApplicationReceipts().ToDictionary(kv => kv.Key, kv => kv.Value),
                Invitation = inner.DumpInvitationReceipts().ToDictionary(kv => kv.Key, kv => kv.Value)
            };
            DurableJsonFileIo.WriteAtomic(receiptsPath, file);
        }
    }

    public AllianceEntity Save(AllianceEntity alliance)
    {
        AllianceEntity result = inner.Save(alliance);
        PersistAlliance(alliance.AllianceId.Value);
        return result;
    }

    public AllianceEntity? Get(AllianceId allianceId) => inner.Get(allianceId);
    public AllianceEntity? GetBySlug(string slug) => inner.GetBySlug(slug);
    public IReadOnlyList<AllianceEntity> Search(AllianceSearchQuery query, out int totalCount) => inner.Search(query, out totalCount);

    public AllianceId? GetCreateReceipt(PlayerId playerId, string clientRequestId) => inner.GetCreateReceipt(playerId, clientRequestId);

    public void SaveCreateReceipt(PlayerId playerId, string clientRequestId, AllianceId allianceId)
    {
        inner.SaveCreateReceipt(playerId, clientRequestId, allianceId);
        PersistReceipts();
    }

    public AllianceMembership SaveMembership(AllianceMembership membership)
    {
        AllianceMembership result = inner.SaveMembership(membership);
        PersistAlliance(membership.AllianceId.Value);
        return result;
    }

    public AllianceMembership? GetActiveMembership(AllianceId allianceId, PlayerId playerId) => inner.GetActiveMembership(allianceId, playerId);
    public AllianceMembership? GetActiveMembershipForPlayer(PlayerId playerId) => inner.GetActiveMembershipForPlayer(playerId);
    public IReadOnlyList<AllianceMembership> ListActiveMembers(AllianceId allianceId) => inner.ListActiveMembers(allianceId);

    public AllianceApplication SaveApplication(AllianceApplication application)
    {
        AllianceApplication result = inner.SaveApplication(application);
        PersistAlliance(application.AllianceId.Value);
        return result;
    }

    public AllianceApplication? GetApplication(Guid applicationId) => inner.GetApplication(applicationId);
    public AllianceApplication? GetPendingApplication(AllianceId allianceId, PlayerId playerId) => inner.GetPendingApplication(allianceId, playerId);
    public IReadOnlyList<AllianceApplication> ListPendingApplications(AllianceId allianceId) => inner.ListPendingApplications(allianceId);
    public Guid? GetApplicationReceipt(PlayerId playerId, string clientRequestId) => inner.GetApplicationReceipt(playerId, clientRequestId);

    public void SaveApplicationReceipt(PlayerId playerId, string clientRequestId, Guid applicationId)
    {
        inner.SaveApplicationReceipt(playerId, clientRequestId, applicationId);
        PersistReceipts();
    }

    public AllianceInvitation SaveInvitation(AllianceInvitation invitation)
    {
        AllianceInvitation result = inner.SaveInvitation(invitation);
        PersistAlliance(invitation.AllianceId.Value);
        return result;
    }

    public AllianceInvitation? GetInvitation(Guid invitationId) => inner.GetInvitation(invitationId);
    public AllianceInvitation? GetPendingInvitation(AllianceId allianceId, PlayerId invitedPlayerId) => inner.GetPendingInvitation(allianceId, invitedPlayerId);
    public IReadOnlyList<AllianceInvitation> ListPendingInvitationsForPlayer(PlayerId playerId) => inner.ListPendingInvitationsForPlayer(playerId);
    public Guid? GetInvitationReceipt(PlayerId playerId, string clientRequestId) => inner.GetInvitationReceipt(playerId, clientRequestId);

    public void SaveInvitationReceipt(PlayerId playerId, string clientRequestId, Guid invitationId)
    {
        inner.SaveInvitationReceipt(playerId, clientRequestId, invitationId);
        PersistReceipts();
    }
}
