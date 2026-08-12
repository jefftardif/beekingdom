# Bee Kingdom Gateway Architecture

## Scope

`BeeKingdom.Gateway` is the single logical client entry point. It accepts connections, authenticates sessions, validates protocol messages, applies rate limits, routes messages to internal service targets, and disconnects invalid clients.

It contains no gameplay logic and makes no simulation decisions.

## Components

| Component | Responsibility |
| --- | --- |
| `GatewayManager` | Public API for accept, authenticate, route, disconnect, query, statistics. |
| `GatewayHost` | Host identity and lifecycle placeholder for future multi-gateway deployments. |
| `ConnectionManager` | In-memory connection registry and connection state transitions. |
| `SessionRouter` | Validates sessions through `AuthenticationManager`. |
| `RequestRouter` | Configurable message-type to service-target routing. |
| `GatewayRateLimiter` | Fixed-window limits by player, session, IP, and message type. |
| `GatewayDiagnostics` | Active connections, new connections, disconnections, latency, bandwidth, messages, routing errors. |

## Connection Model

Each `GatewayConnection` contains:

* `ConnectionId`
* `SessionId`
* `PlayerId`
* `ClientVersion`
* `ProtocolVersion`
* `Region`
* `LatencyMilliseconds`
* `IpAddress`
* `ConnectionState`

Supported states:

* `Connecting`
* `Authenticating`
* `Connected`
* `Idle`
* `Disconnecting`
* `Disconnected`

## Routing

Routes target these service categories:

* Authentication
* Account
* Colony
* World
* Simulation
* Chat
* Alliance
* Notification
* LiveOps
* Administration

The initial router maps message types to targets and can be extended at runtime with `RegisterRoute`.

## Validation

Before routing, Gateway validates:

* connection exists and is not disconnected;
* message size is below configured maximum;
* protocol validation passes;
* message session/player matches the authenticated connection;
* rate limits are respected.

## Scalability Notes

The current implementation is in-memory and transport-independent. The shape prepares future horizontal scaling by isolating connection state, session validation, routing, diagnostics, and rate limiting behind focused components.
