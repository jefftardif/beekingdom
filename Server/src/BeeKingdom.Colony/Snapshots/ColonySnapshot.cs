using BeeKingdom.Colony.Models;
using BeeKingdom.Shared.ValueObjects;

namespace BeeKingdom.Colony.Snapshots;

public sealed record ColonySnapshot(
    Guid SnapshotId,
    ColonyId ColonyId,
    ColonySnapshotKind Kind,
    long BaseRevision,
    long Revision,
    DateTimeOffset CreatedAtUtc,
    string Version,
    byte[] Payload,
    IReadOnlyDictionary<string, string> Metadata);
