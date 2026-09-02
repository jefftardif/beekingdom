using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

// M042-CL: one JSON file per war ({root}/{warId:N}.json), plus a small shared receipts file -
// same pattern as the other three Durable Alliance repositories.
public sealed class DurableJsonAllianceWarRepository : IAllianceWarRepository
{
    private readonly InMemoryAllianceWarRepository inner = new();
    private readonly string root;
    private readonly string receiptsPath;
    private readonly object writeLock = new();

    public DurableJsonAllianceWarRepository(string rootDirectory)
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
            AllianceWar? war = DurableJsonFileIo.ReadIfExists<AllianceWar>(file);
            if (war != null) inner.RestoreWar(war);
        }
        Dictionary<string, Guid>? receipts = DurableJsonFileIo.ReadIfExists<Dictionary<string, Guid>>(receiptsPath);
        if (receipts != null)
            foreach (var kv in receipts) inner.RestoreReceiptRaw(kv.Key, kv.Value);
    }

    public AllianceWar Save(AllianceWar war)
    {
        AllianceWar result = inner.Save(war);
        lock (writeLock) DurableJsonFileIo.WriteAtomic(Path.Combine(root, war.WarId.ToString("N") + ".json"), result);
        return result;
    }

    public AllianceWar? Get(Guid warId) => inner.Get(warId);
    public IReadOnlyList<AllianceWar> ListActiveForAlliance(AllianceId allianceId) => inner.ListActiveForAlliance(allianceId);
    public bool HasActiveWarBetween(AllianceId allianceA, AllianceId allianceB) => inner.HasActiveWarBetween(allianceA, allianceB);
    public Guid? GetDeclareReceipt(PlayerId actorPlayerId, string clientRequestId) => inner.GetDeclareReceipt(actorPlayerId, clientRequestId);

    public void SaveDeclareReceipt(PlayerId actorPlayerId, string clientRequestId, Guid warId)
    {
        inner.SaveDeclareReceipt(actorPlayerId, clientRequestId, warId);
        lock (writeLock) DurableJsonFileIo.WriteAtomic(receiptsPath, inner.DumpReceipts().ToDictionary(kv => kv.Key, kv => kv.Value));
    }
}
