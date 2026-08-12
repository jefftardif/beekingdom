# Simulation Service Architecture

## Scope

`BeeKingdom.Simulation` is the authoritative simulation core for the Bee Kingdom server. It owns tick execution, deterministic phase ordering, loaded-colony orchestration, pause/resume, fast-forward for tests, autosave checks, and simulation diagnostics.

Unity remains a display client. Gameplay simulation should run through this service rather than inside the client, gateway, colony service, or persistence layer.

## Components

| Component | Responsibility |
| --- | --- |
| `SimulationManager` | Public facade for server endpoints and future service integrations. |
| `SimulationEngine` | Runtime state machine, loaded-colony registry, tick batching, pause/resume/stop/start, fast-forward. |
| `SimulationScheduler` | Deterministic registration and ordering of simulation systems by stage, order, and name. |
| `TickProcessor` | Executes one colony tick through the strict stage order and performs save checks. |
| `SimulationContext` | Immutable tick context containing tick id, world id, colony id, timestamp, season, weather, active events, and mode. |
| `SimulationDiagnostics` | Tracks ticks, average tick duration, colonies simulated/loaded, memory, CPU placeholder, and save timing. |
| `ISimulationEventSink` | Publishes simulation lifecycle events without binding the engine to a transport. |

## Tick Modes

Supported modes:

* `Fixed`: official gameplay tick mode.
* `VariableAdministration`: administrative/manual tick using current server time.
* `FastForward`: deterministic fixed-time execution for tests and recovery scenarios.

Fixed and fast-forward contexts derive timestamps from `SimulationEpochUtc + TickId * FixedTickInterval`, so tick timestamps are deterministic.

## Strict Execution Order

Every tick executes the following stages exactly in numeric order:

1. `GameplayEvents`
2. `GameplayEffects`
3. `GameplayAttributes`
4. `Construction`
5. `Population`
6. `BeeLifecycle`
7. `BeeNeeds`
8. `BeeHealth`
9. `Fatigue`
10. `Experience`
11. `AI`
12. `Economy`
13. `World`
14. `SaveCheck`
15. `Diagnostics`

Within a stage, systems are sorted by `Order` and then by `Name` using ordinal comparison. This gives future gameplay modules deterministic extension points.

## Colony Coordination

The engine loads colony records through `ColonyManager`, keeps a simulation-side loaded-colony registry, and can unload colonies explicitly or when inactive. Save checks call the Colony Service to produce incremental snapshots.

Colony profile ownership remains in `BeeKingdom.Colony`; simulation execution remains in `BeeKingdom.Simulation`.

## Configuration

The `Simulation` configuration section controls:

* `FixedTickInterval`
* `AutoSaveEveryTicks`
* `InactiveUnloadAfter`
* `MaxFastForwardTicks`
* `MaxColoniesPerTickBatch`
* `SimulationEpochUtc`

## Events

Published simulation events:

* `SimulationStarted`
* `SimulationStopped`
* `TickExecuted`
* `SimulationColonyLoaded`
* `SimulationColonyUnloaded`
* `SimulationPaused`
* `SimulationResumed`

## Server API

Initial HTTP endpoints:

* `POST /simulation/start`
* `POST /simulation/stop`
* `POST /simulation/pause`
* `POST /simulation/resume`
* `POST /simulation/tick`
* `POST /simulation/fast-forward`
* `POST /simulation/colonies/{colonyId}/load`
* `POST /simulation/colonies/{colonyId}/unload`
* `GET /simulation/diagnostics`

## Scalability Direction

The current implementation is single-process and deterministic. It prepares future horizontal scaling by keeping colony execution isolated by `ColonyId`, using deterministic batches, separating scheduling from processing, and keeping persistence behind the Colony Service and repository boundaries.

Future multi-server distribution can introduce shard assignment, migration, and load balancing above `SimulationEngine` without changing the tick phase contract.
