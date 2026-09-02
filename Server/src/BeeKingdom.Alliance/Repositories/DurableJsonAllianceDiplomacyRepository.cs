using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

// M042-CL: one JSON file per relation ({root}/{relationId:N}.json), plus a small shared
// receipts file - same pattern as the other three Durable Alliance repositories.
public sealed class DurableJsonAllianceDiplomacyRepository : IAllianceDiplomacyRepository
{
    private readonly InMemoryAllianceDiplomacyRepository inner = new();
    private readonly string root;
    private readonly string receiptsPath;
    private readonly object writeLock = new();

    public DurableJsonAllianceDiplomacyRepository(string rootDirectory)
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
            AllianceDiplomaticRelation? relation = DurableJsonFileIo.ReadIfExists<AllianceDiplomaticRelation>(file);
            if (relation != null) inner.RestoreRelation(relation);
        }
        Dictionary<string, Guid>? receipts = DurableJsonFileIo.ReadIfExists<Dictionary<string, Guid>>(receiptsPath);
        if (receipts != null)
            foreach (var kv in receipts) inner.RestoreReceiptRaw(kv.Key, kv.Value);
    }

    public AllianceDiplomaticRelation Save(AllianceDiplomaticRelation relation)
    {
        AllianceDiplomaticRelation result = inner.Save(relation);
        lock (writeLock) DurableJsonFileIo.WriteAtomic(Path.Combine(root, relation.RelationId.ToString("N") + ".json"), result);
        return result;
    }

    public AllianceDiplomaticRelation? GetRelation(AllianceId allianceA, AllianceId allianceB) => inner.GetRelation(allianceA, allianceB);
    public IReadOnlyList<AllianceDiplomaticRelation> ListForAlliance(AllianceId allianceId) => inner.ListForAlliance(allianceId);
    public Guid? GetProposalReceipt(PlayerId actorPlayerId, string clientRequestId) => inner.GetProposalReceipt(actorPlayerId, clientRequestId);

    public void SaveProposalReceipt(PlayerId actorPlayerId, string clientRequestId, Guid relationId)
    {
        inner.SaveProposalReceipt(actorPlayerId, clientRequestId, relationId);
        lock (writeLock) DurableJsonFileIo.WriteAtomic(receiptsPath, inner.DumpReceipts().ToDictionary(kv => kv.Key, kv => kv.Value));
    }
}
