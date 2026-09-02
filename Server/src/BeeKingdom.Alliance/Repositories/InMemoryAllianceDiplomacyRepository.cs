using BeeKingdom.Alliance.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Alliance.Repositories;

public sealed class InMemoryAllianceDiplomacyRepository : IAllianceDiplomacyRepository
{
    private readonly Dictionary<(Guid, Guid), AllianceDiplomaticRelation> relations = new();
    private readonly Dictionary<string, Guid> proposalReceipts = new(StringComparer.Ordinal);
    private readonly object sync = new();

    public AllianceDiplomaticRelation Save(AllianceDiplomaticRelation relation)
    {
        lock (sync)
        {
            relations[CanonicalKey(relation.AllianceIdA, relation.AllianceIdB)] = relation;
            return relation;
        }
    }

    public AllianceDiplomaticRelation? GetRelation(AllianceId allianceA, AllianceId allianceB)
    {
        lock (sync) return relations.GetValueOrDefault(CanonicalKey(allianceA, allianceB));
    }

    public IReadOnlyList<AllianceDiplomaticRelation> ListForAlliance(AllianceId allianceId)
    {
        lock (sync)
        {
            return relations.Values
                .Where(r => r.AllianceIdA == allianceId || r.AllianceIdB == allianceId)
                .OrderByDescending(r => r.UpdatedAtUtc)
                .ToArray();
        }
    }

    public Guid? GetProposalReceipt(PlayerId actorPlayerId, string clientRequestId)
    {
        lock (sync) return proposalReceipts.TryGetValue(ReceiptKey(actorPlayerId, clientRequestId), out Guid id) ? id : null;
    }

    public void SaveProposalReceipt(PlayerId actorPlayerId, string clientRequestId, Guid relationId)
    {
        lock (sync) proposalReceipts[ReceiptKey(actorPlayerId, clientRequestId)] = relationId;
    }

    // Canonical, order-independent key so "A proposes to B" and "B proposes to A" hit the same row.
    internal static (Guid, Guid) CanonicalKey(AllianceId a, AllianceId b)
        => a.Value.CompareTo(b.Value) <= 0 ? (a.Value, b.Value) : (b.Value, a.Value);

    private static string ReceiptKey(PlayerId playerId, string clientRequestId) => $"{playerId.Value:N}:{clientRequestId}";

    // M042-CL: see the identical note in InMemoryAllianceRepository.
    internal IReadOnlyList<AllianceDiplomaticRelation> DumpAll() { lock (sync) return relations.Values.ToArray(); }
    internal IReadOnlyDictionary<string, Guid> DumpReceipts() { lock (sync) return new Dictionary<string, Guid>(proposalReceipts, StringComparer.Ordinal); }
    internal void RestoreRelation(AllianceDiplomaticRelation relation) { lock (sync) relations[CanonicalKey(relation.AllianceIdA, relation.AllianceIdB)] = relation; }
    internal void RestoreReceiptRaw(string key, Guid relationId) { lock (sync) proposalReceipts[key] = relationId; }
}
