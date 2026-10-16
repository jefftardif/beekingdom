# Startup data synchronization - 2026-10-16

## User request
Use the startup loading screen to synchronize gameplay data before revealing the hive,
even when this makes startup longer. Preserve the existing premium artwork.

## Confirmed cause
The old loading screen was a fixed 2.4-second cosmetic sequence. It did not await gameplay
reads. TryConfigureGameplayForActiveSession also unconditionally called
CloseGameplayForSignedOutSession before every configuration attempt. Opening Bank/Alliance
or changing scenes consequently discarded already-loaded controllers and repeated reads.

## Changes
- Bind gameplay once per account/hive/server/source/authority identity. Token refresh on the
  same session source does not dispose controllers. An actual identity change still clears
  all old controllers, snapshots and in-flight startup work.
- Keep the existing idempotent hive initialization before data reads. Its completion and
  bank-prefetch callbacks are cancellation-aware and generation-checked.
- Start existing independent reads after initialization and follow 14 actual check results.
- Required: hive state, building levels, production/resource balances and population.
- Optional: Research, Army, Alliance, Bank, Nursery care, Quests, Mail, Inventory,
  Daily objectives and the detailed stock ledger. An unavailable optional service is
  reported rather than silently reported as synchronized or blocking the entire game.
- A 45-second data deadline produces Retry/Sign-in when essential data is unavailable;
  unavailable optional reads are reported without blocking known-good essentials.
- Entry into the authenticated hive is deferred before EnteredHive is set. Header values
  are published before revealing the hive. The approved splash artwork is retained and
  its progress follows completed checks, not elapsed time.
- Bank consumes its preloaded validated snapshot once and no longer forces a building
  refresh when authoritative building levels are already available.
- Protected offline consultation and the explicit local demo remain separate; no server
  hive-initialization write is attempted without an official session.

## Safety
No server source/configuration/deployment modified. No feature gates enabled. No harvest,
reward claim, care, recruitment, deposit or withdrawal added to startup. The existing
idempotent EnsureHive operation remains the sole initialization action. No tokens logged.

## Validation
126 targeted EditMode tests PASS, 0 failed (startup, session UI, Bank/Nursery, FTUE,
premium tutorial, Alliance and overlays). Test cases cover delayed readiness, required
failure/timeout, optional unavailability/timeout, probe exception, stable ready state,
identity boundaries, entry ordering, preloaded Bank and absence of economic actions.
Evidence: C:/BeeKingdom/DEV/BK-Studio/startup-sync-20261016/startup-tests-v1.json.

Actual canonical scene starts and the explicit local-demo route still enters correctly.
The first observations were signed out. A subsequent authenticated session was available
and verified without injecting an account or server snapshot:
- startupReady=true, 14/14 checks complete;
- unavailable optional services: nursery, inventory, stock (not reported as synchronized);
- 15 building levels and 3 resource production lines loaded; Bank level 2;
- eight sampled controllers/clients keep the same references across five configuration calls;
- session generation remains unchanged through hive entry and Bank opening;
- Bank opens with the real level, Ready building/production state and an available Reserve button.
Evidence: authenticated-reuse-proof.txt, authenticated-bank-proof.txt, bank-preloaded.png.
The one-use Bank-prefetch snapshot had already been consumed before the final repeated
opening; that final capture is not claimed as proof of its first consumption.
No cold-start performance number or economic transaction is claimed.

Local-only delivery. No build, push or deployment. Unrelated working-tree changes retained.
Unity is left running in the canonical scene, Bank open on the authenticated session.