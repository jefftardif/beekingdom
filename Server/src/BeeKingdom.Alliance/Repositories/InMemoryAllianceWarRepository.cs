using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public sealed class InMemoryAllianceWarRepository : IAllianceWarRepository
{
    private readonly Dictionary<Guid, AllianceWar> wars = new();
    private readonly Dictionary<string, Guid> declareReceipts = new(StringComparer.Ordinal);
    private readonly object sync = new();

    public AllianceWar Save(AllianceWar war)
    {
        lock (sync) { wars[war.WarId] = war; return war; }
    }

    public AllianceWar? Get(Guid warId)
    {
        lock (sync) return wars.GetValueOrDefault(warId);
    }

    public IReadOnlyList<AllianceWar> ListActiveForAlliance(AllianceId allianceId)
    {
        lock (sync)
        {
            return wars.Values.Where(w =>
                (w.AttackerAllianceId == allianceId || w.DefenderAllianceId == allianceId) &&
                w.Status is AllianceWarStatus.Declared or AllianceWarStatus.Active)
                .OrderByDescending(w => w.DeclaredAtUtc)
                .ToArray();
        }
    }

    public bool HasActiveWarBetween(AllianceId allianceA, AllianceId allianceB)
    {
        lock (sync)
        {
            return wars.Values.Any(w =>
                w.Status is AllianceWarStatus.Declared or AllianceWarStatus.Active &&
                ((w.AttackerAllianceId == allianceA && w.DefenderAllianceId == allianceB) ||
                 (w.AttackerAllianceId == allianceB && w.DefenderAllianceId == allianceA)));
        }
    }

    public Guid? GetDeclareReceipt(PlayerId actorPlayerId, string clientRequestId)
    {
        lock (sync) return declareReceipts.TryGetValue(ReceiptKey(actorPlayerId, clientRequestId), out Guid id) ? id : null;
    }

    public void SaveDeclareReceipt(PlayerId actorPlayerId, string clientRequestId, Guid warId)
    {
        lock (sync) declareReceipts[ReceiptKey(actorPlayerId, clientRequestId)] = warId;
    }

    private static string ReceiptKey(PlayerId playerId, string clientRequestId) => $"{playerId.Value:N}:{clientRequestId}";

    // M042-CL: see the identical note in InMemoryAllianceRepository.
    internal IReadOnlyList<AllianceWar> DumpAll() { lock (sync) return wars.Values.ToArray(); }
    internal IReadOnlyDictionary<string, Guid> DumpReceipts() { lock (sync) return new Dictionary<string, Guid>(declareReceipts, StringComparer.Ordinal); }
    internal void RestoreWar(AllianceWar war) { lock (sync) wars[war.WarId] = war; }
    internal void RestoreReceiptRaw(string key, Guid warId) { lock (sync) declareReceipts[key] = warId; }
}
