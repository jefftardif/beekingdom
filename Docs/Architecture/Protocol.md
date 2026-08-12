# Bee Kingdom Communication Protocol

## Purpose

`BeeKingdom.Protocol` defines the official Unity/server message protocol. It is independent from REST, WebSocket, gRPC, sockets, or any future gateway transport. Transports move serialized protocol messages; they do not define message shape.

## Message Envelope

Every protocol message uses `ProtocolMessage<TPayload>` and contains:

* `ProtocolVersion`
* `MessageId`
* `MessageType`
* `CorrelationId`
* `TraceId`
* `TimestampUtc`
* `SessionId`
* `PlayerId`
* `ColonyId`
* `Payload`

Supported message types:

* `Request`
* `Response`
* `Command`
* `Event`
* `Notification`
* `Heartbeat`
* `Acknowledgement`
* `Error`

## Components

| Component | Responsibility |
| --- | --- |
| `ProtocolManager` | Public API for serialize, deserialize, validate, register messages, get version, negotiate version. |
| `MessageSerializer` | JSON UTF-8 serialization implementation. |
| `MessageDeserializer` | JSON UTF-8 deserialization implementation. |
| `ProtocolVersionManager` | Version support checks and negotiation. |
| `ProtocolValidator` | Version, structure, size, session, authentication placeholder, and integrity placeholder validation. |
| `ProtocolDiagnostics` | Counts messages, bytes, errors, processing ticks, and per-type statistics. |
| `MessageRegistry` | Registers known message names and protocol categories. |

## Versioning

`ProtocolVersion.Current` is `1.0`. Compatibility currently requires the same major version and a requested minor version less than or equal to the server-supported minor version. Unsupported negotiations return the default version and record an `UnsupportedVersion` diagnostic.

## Serialization

The protocol serializer is abstracted through `IMessageSerializer` and `IMessageDeserializer`. The initial implementation uses `System.Text.Json` with UTF-8 bytes. MessagePack or Protobuf can be introduced by adding new serializer implementations without changing shared contracts.

## Validation

Before processing, protocol messages are validated for:

* supported version;
* payload size;
* non-empty message, correlation, and trace ids;
* non-empty session id;
* non-empty player id;
* non-empty colony id;
* non-null payload.

Future authentication and integrity checks should extend `ProtocolValidator` or wrap it in gateway-specific validation.

## Errors

Protocol errors use `ProtocolErrorCode`:

* `InvalidMessage`
* `UnsupportedVersion`
* `Unauthorized`
* `ValidationError`
* `ServerError`
* `Timeout`
* `RateLimited`

Error responses can use `ProtocolMessage<ErrorPayload>` with message type `Error`.

## Future Playable Hive Loop Readiness Contract

`PlayableHiveLoopReadinessResponse` prepares the future server shape for the playable Hive loop without creating an official endpoint or live authority.

Covered future read models:

* player resources;
* buildings;
* building levels;
* building upgrades;
* construction queue;
* troops;
* training queue;
* player army.

The contract is readiness-only. It must remain `ReadOnly=true`, `NonLive=true`, `OfficialEndpoint=false`, `MutationAllowed=false`, `PersistenceClaimAllowed=false` and `RealTimeSynchronizationEnabled=false` until a later SERVER explicitly activates backend authority.

Nullable fields such as resource amounts, capacities, building levels, upgrade durations, construction completion times, troop counts and army capacity are intentional. They prevent the client or documentation from claiming official progression, persistence, rewards, training, construction or synchronization before the authoritative server source exists.
