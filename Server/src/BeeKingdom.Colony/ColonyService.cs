using System.Diagnostics;
using System.Text.Json;
using BeeKingdom.Colony.Configuration;
using BeeKingdom.Colony.Diagnostics;
using BeeKingdom.Colony.Events;
using BeeKingdom.Colony.Models;
using BeeKingdom.Colony.Registry;
using BeeKingdom.Colony.Repositories;
using BeeKingdom.Colony.Snapshots;
using BeeKingdom.Infrastructure.Time;
using BeeKingdom.Shared.Serialization;
using BeeKingdom.Shared.ValueObjects;
using Microsoft.Extensions.Options;

namespace BeeKingdom.Colony;

public interface IColonyService
{
    ColonyDiagnostics Diagnostics { get; }
    ColonyRecord CreateColony(CreateColonyRequest request);
    ColonyRecord LoadColony(ColonyId colonyId);
    ColonySnapshot SaveColony(ColonyId colonyId, ColonySnapshotKind kind);
    ColonyRecord DeleteColony(ColonyId colonyId);
    IReadOnlyList<ColonyRecord> QueryColony(ColonyQuery query);
    ColonyRecord RenameColony(ColonyId colonyId, string hiveName);
    ColonyRecord SetColonyStatus(ColonyId colonyId, ColonyStatus status);
    ColonyStatistics GetColonyStatistics(ColonyId colonyId);
}

public sealed class ColonyService : IColonyService
{
    private readonly IColonyRepository repository;
    private readonly ColonyRegistry registry;
    private readonly IColonyEventSink events;
    private readonly IServerClock clock;
    private readonly ColonyOptions options;

    public ColonyService(IColonyRepository repository, ColonyRegistry registry, IColonyEventSink events, IServerClock clock, IOptions<ColonyOptions> options)
    {
        this.repository = repository;
        this.registry = registry;
        this.events = events;
        this.clock = clock;
        this.options = options.Value;
    }

    public ColonyDiagnostics Diagnostics { get; } = new();

    public ColonyRecord CreateColony(CreateColonyRequest request)
    {
        ColonyProfile profile = new(ColonyId.New(), request.PlayerId, request.WorldId, request.HiveName, clock.UtcNow, "Spring", 1, request.QueenId, 1, 0, ColonyStatus.Creating);
        ColonyRecord colony = new(
            profile,
            new ColonyStatistics(1, 0, 0, 0, clock.UtcNow),
            new ColonySettings("Auto", options.CompressionPolicy, options.VersioningStrategy),
            [new ColonyHistoryEntry(clock.UtcNow, "Created", "Colony created.")],
            0);

        repository.Create(colony);
        registry.Register(colony);
        RefreshActiveCount();
        events.Publish(new ColonyCreated(clock.UtcNow, profile.ColonyId, profile.PlayerId));
        return colony;
    }

    public ColonyRecord LoadColony(ColonyId colonyId)
    {
        long start = Stopwatch.GetTimestamp();
        ColonyRecord colony = repository.Get(colonyId) ?? throw new KeyNotFoundException($"Colony {colonyId} was not found.");
        registry.Register(colony);
        Diagnostics.RecordLoaded(Stopwatch.GetTimestamp() - start);
        events.Publish(new ColonyLoaded(clock.UtcNow, colony.Profile.ColonyId, colony.Profile.PlayerId));
        return colony;
    }

    public ColonySnapshot SaveColony(ColonyId colonyId, ColonySnapshotKind kind)
    {
        long start = Stopwatch.GetTimestamp();
        ColonyRecord colony = registry.TryGet(colonyId, out ColonyRecord loaded) ? loaded : LoadColony(colonyId);
        long nextRevision = colony.Revision + 1;
        ColonyRecord updated = colony with
        {
            Revision = nextRevision,
            Statistics = colony.Statistics with { Revision = nextRevision, UpdatedAtUtc = clock.UtcNow },
            History = colony.History.Concat([new ColonyHistoryEntry(clock.UtcNow, "Saved", $"Saved revision {nextRevision}.")]).ToArray()
        };
        repository.Save(updated);
        registry.Register(updated);

        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(updated, BeeJson.CreateDefaultOptions());
        if (payload.Length > options.MaxSnapshotBytes)
        {
            Diagnostics.RecordPersistenceError();
            throw new InvalidOperationException("Colony snapshot exceeds maximum configured size.");
        }

        ColonySnapshot snapshot = new(Guid.NewGuid(), colonyId, kind, kind == ColonySnapshotKind.Incremental ? colony.Revision : 0, nextRevision, clock.UtcNow, "1.0.0", payload, new Dictionary<string, string> { ["compression"] = options.CompressionPolicy });
        repository.SaveSnapshot(snapshot);
        Diagnostics.RecordSaved(Stopwatch.GetTimestamp() - start, payload.Length);
        events.Publish(new ColonySaved(clock.UtcNow, colonyId, nextRevision));
        return snapshot;
    }

    public ColonyRecord DeleteColony(ColonyId colonyId)
    {
        ColonyRecord colony = repository.Get(colonyId) ?? throw new KeyNotFoundException($"Colony {colonyId} was not found.");
        if (colony.Profile.Status == ColonyStatus.Deleted)
        {
            return colony;
        }

        ValidateTransition(colony.Profile.Status, ColonyStatus.Deleted);
        ColonyRecord updated = repository.Save(colony with
        {
            Profile = colony.Profile with { Status = ColonyStatus.Deleted },
            Revision = colony.Revision + 1,
            History = colony.History.Concat([new ColonyHistoryEntry(clock.UtcNow, "Deleted", "Colony deleted.")]).ToArray()
        });
        registry.Register(updated);
        RefreshActiveCount();
        events.Publish(new ColonyDeleted(clock.UtcNow, colonyId));
        return updated;
    }

    public IReadOnlyList<ColonyRecord> QueryColony(ColonyQuery query) => repository.Query(query);

    public ColonyRecord RenameColony(ColonyId colonyId, string hiveName)
    {
        ColonyRecord colony = repository.Get(colonyId) ?? throw new KeyNotFoundException($"Colony {colonyId} was not found.");
        if (colony.Profile.Status == ColonyStatus.Deleted)
        {
            throw new InvalidOperationException("Deleted colonies cannot be renamed.");
        }

        ColonyRecord updated = repository.Save(colony with
        {
            Profile = colony.Profile with { HiveName = hiveName },
            Revision = colony.Revision + 1,
            History = colony.History.Concat([new ColonyHistoryEntry(clock.UtcNow, "Renamed", $"Colony renamed to {hiveName}.")]).ToArray()
        });
        registry.Register(updated);
        events.Publish(new ColonyRenamed(clock.UtcNow, colonyId, hiveName));
        return updated;
    }

    public ColonyRecord SetColonyStatus(ColonyId colonyId, ColonyStatus status)
    {
        ColonyRecord colony = repository.Get(colonyId) ?? throw new KeyNotFoundException($"Colony {colonyId} was not found.");
        if (colony.Profile.Status == status)
        {
            return colony;
        }

        ValidateTransition(colony.Profile.Status, status);
        long nextRevision = colony.Revision + 1;
        ColonyRecord updated = repository.Save(colony with
        {
            Profile = colony.Profile with { Status = status },
            Statistics = colony.Statistics with { Revision = nextRevision, UpdatedAtUtc = clock.UtcNow },
            Revision = nextRevision,
            History = colony.History.Concat([new ColonyHistoryEntry(clock.UtcNow, "StatusChanged", $"{colony.Profile.Status} -> {status}.")]).ToArray()
        });
        registry.Register(updated);
        RefreshActiveCount();
        return updated;
    }

    public ColonyStatistics GetColonyStatistics(ColonyId colonyId)
    {
        ColonyRecord colony = repository.Get(colonyId) ?? throw new KeyNotFoundException($"Colony {colonyId} was not found.");
        return colony.Statistics;
    }

    private void RefreshActiveCount()
    {
        Diagnostics.SetActiveColonies(repository.Query(new ColonyQuery(Status: ColonyStatus.Active)).Count);
    }

    private static void ValidateTransition(ColonyStatus current, ColonyStatus next)
    {
        bool allowed = current switch
        {
            ColonyStatus.Creating => next is ColonyStatus.Active or ColonyStatus.Locked or ColonyStatus.Deleted,
            ColonyStatus.Active => next is ColonyStatus.Sleeping or ColonyStatus.Migrating or ColonyStatus.Locked or ColonyStatus.Deleted,
            ColonyStatus.Sleeping => next is ColonyStatus.Active or ColonyStatus.Locked or ColonyStatus.Deleted,
            ColonyStatus.Migrating => next is ColonyStatus.Active or ColonyStatus.Locked or ColonyStatus.Deleted,
            ColonyStatus.Locked => next is ColonyStatus.Active or ColonyStatus.Deleted,
            ColonyStatus.Deleted => false,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException($"Invalid colony status transition: {current} -> {next}.");
        }
    }
}
