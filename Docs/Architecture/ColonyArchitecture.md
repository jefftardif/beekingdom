# Colony Service Architecture

## Scope

`BeeKingdom.Colony` is the authoritative data service for player colonies. It owns colony identity, profile state, high-level statistics, settings, history, loading, saving, snapshots, and validated status transitions.

The service contains no gameplay simulation logic. Bee behaviour, production, construction, combat, and resource evolution remain responsibilities of future simulation services.

## Components

| Component | Responsibility |
| --- | --- |
| `ColonyManager` | Public facade used by the server composition root and future internal services. |
| `IColonyService` / `ColonyService` | Implements creation, loading, saving, deletion, query, rename, statistics, and status transitions. |
| `IColonyRepository` / `InMemoryColonyRepository` | Persistence boundary for colony records and snapshots. Current storage is in-memory and replaceable by SQL persistence. |
| `ColonyRegistry` | Tracks loaded colonies in memory for fast access during service coordination. |
| `ColonySnapshot` | Versioned full or incremental serialized colony record. |
| `ColonyDiagnostics` | Tracks active colonies, loaded colonies, save/load timings, snapshot size, and persistence errors. |
| `IColonyEventSink` | Publishes colony lifecycle events while keeping transport and messaging choices outside the domain service. |

## Data Model

`ColonyProfile` contains the official profile fields requested by the specification: `ColonyId`, `PlayerId`, `WorldId`, `HiveName`, `CreationDate`, `CurrentSeason`, `CurrentPopulation`, `QueenId`, `ColonyLevel`, `PrestigeLevel`, and `Status`.

`ColonyRecord` also stores high-level statistics, configurable settings, history entries, and a revision. Simulation internals are intentionally absent.

## Status Transitions

Supported statuses are `Creating`, `Active`, `Sleeping`, `Migrating`, `Locked`, and `Deleted`.

Transitions are validated by `ColonyService`. Deleted colonies are terminal, and deleted records cannot be renamed or reactivated.

## Snapshot Strategy

Snapshots use `System.Text.Json` with the shared `BeeJson` options to keep serialization deterministic across the server stack.

Supported snapshot concepts:

* full snapshot;
* incremental snapshot with a base revision;
* autosave-ready metadata through configured save policy;
* restore-ready payload by deserializing `ColonyRecord`;
* semantic version marker.

The current repository stores snapshots in memory. SQL-backed snapshot persistence should implement `IColonyRepository` without changing `ColonyService`.

## Configuration

The `Colony` configuration section controls `MaxSnapshotBytes`, `AutoSaveInterval`, `CompressionPolicy`, `RetentionDays`, and `VersioningStrategy`.

## Server API

Initial HTTP endpoints:

* `POST /colonies`
* `GET /colonies/{colonyId}`
* `POST /colonies/{colonyId}/load`
* `POST /colonies/{colonyId}/save`
* `POST /colonies/{colonyId}/rename`
* `POST /colonies/{colonyId}/status`
* `DELETE /colonies/{colonyId}`
* `GET /colonies/{colonyId}/statistics`

These endpoints are thin adapters over `ColonyManager`; business rules remain in `BeeKingdom.Colony`.

## Integration Boundaries

`BeeKingdom.Colony` references `BeeKingdom.Shared` for identifiers and serialization conventions, `BeeKingdom.Infrastructure` for server time and dependency injection conventions, and `BeeKingdom.Persistence` as the persistence boundary direction.

It is designed to integrate later with Authentication, Account, Simulation, World, and SQL-backed Persistence through service composition and repository replacement.
