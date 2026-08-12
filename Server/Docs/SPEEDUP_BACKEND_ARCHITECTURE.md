# Server SpeedUp Backend Architecture

## Scope

The SpeedUp backend is disabled by default until its catalog and deployment readiness are explicitly enabled. The server is the only authority for inventory quantity, timer end time, completion state and idempotency.

## State

`PlayerHiveState.SpeedUps` stores the persistent item quantity map. A stack is identified by the server catalog item id; category and duration are validated from `SpeedUpOptions`, never trusted from the client.

The same state also contains the authoritative operation models for building upgrades, production, research, training and brood vitality. `SpeedUpInventoryService` applies a reduction inside `IHiveStateRepository.ExecuteAtomicallyAsync`.

## API

- `GET /game/v1/hives/{hiveId}/speedups`
- `POST /game/v1/hives/{hiveId}/speedups/apply`
- `POST /game/v1/hives/{hiveId}/speedups/{category}/apply`

The category route is a typed façade over the same mutation service. The request contains `itemId`, `category`, `targetId`, `durationSeconds`, `expectedRevision` and `idempotencyKey`.

## Mutation sequence

```text
Authenticate
  -> validate contract and catalog item
  -> atomic repository transaction
  -> replay idempotency receipt when applicable
  -> validate revision and inventory quantity
  -> resolve target handler
  -> reduce timer, clamped at server now
  -> consume one inventory item
  -> increment revision and persist receipt
  -> return inventory and timer snapshot
```

The operation is rejected if the target is absent, the quantity is insufficient, the revision is stale, or the idempotency payload conflicts. A repeated identical idempotency key does not consume again.

## Extensibility

Target handlers are registered by category and share the same service. Current handlers cover construction, manufacturing, research, training, healing and universal routing. Adding a future queue requires a handler and state model, not a new client mutation pipeline.

## Rewards and deployment

The response `Rewards` and `Events` collections are now backed by a persistent reward ledger (`RewardLedgerState` on `PlayerHiveState`, see `RewardLedgerService`). Each grant writes a claimable reward (`Rewards`) and a ledger entry in the same atomic mutation; the existing claim path syncs the entry and appends a `reward_claimed` event. Queue completions are recorded exactly once as `queue_completed` events (per `OperationId`).

Ledger API:

- `GET /game/v1/hives/{hiveId}/rewards` — settlement + snapshot (pending rewards, events). Gated by `RewardLedger.Enabled`.
- `POST /admin/v1/players/{playerId}/hives/{hiveId}/rewards/grant` — idempotent grant. Gated by `AdminSupport`.

The ledger (`RewardLedger.Enabled`) and the SpeedUp backend (`SpeedUps.Enabled`) stay `false` by default. Live deployment to `chat.dravii.com` remains gated until the client wiring and deployment access are in place.

## Errors

Normalized codes include `game.invalid_request`, `game.invalid_speedup`, `game.inventory_insufficient`, `game.timer_not_found`, `game.revision_conflict`, `game.idempotency_conflict`, `game.category_unsupported` and `game.unavailable`.
