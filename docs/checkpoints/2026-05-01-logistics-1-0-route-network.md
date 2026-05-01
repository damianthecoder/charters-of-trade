# 2026-05-01 Logistics 1.0 Route Network

## Summary

Implemented Phase 3 / Logistics 1.0 on `codex/phase3-logistics-1-0` from synced `origin/main`. The prototype now supports a persisted active route-operation network instead of one global recurring charter: multiple active operations, per-route capacity allocation, cargo priorities, route maintenance costs, port/coastal vs road differences, congestion pauses, unprofitable-route pauses, and in-transit shipments with delayed arrival.

## Changed Systems

- `Persistence.Core`: save version 4 adds `RouteOperationSaveState` and `RouteTransitSaveState`, canonical sorting, and validation for operation/transit identity, endpoints, operation linkage, units, timing, and non-negative settlement values.
- `GodotBridge`: `PrototypeSession` now tracks active route operations and in-transit shipments, dispatches by route capacity and priority, charges route maintenance, delays arrivals by route mode/congestion, and exposes active network/transit snapshot data.
- `Godot UI`: the Route Contract/Operation panel can add operations into the active network, stop a route-scoped operation, and report network operation count plus in-transit shipment status.
- `Benchmarks`: unattended benchmark now selects up to three profitable route operations across distinct routes and reports active route operation and in-transit shipment counts.
- `Tests`: added deterministic coverage for multiple active operations, transit queues, route-operation save/load hashing, and invalid transit validation.
- `Docs`: added ADR-0008 and updated project memory/checkpoint state.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 59/59 console tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; latest visual smoke frame `artifacts/godot-smoke/visual-smoke-20260501-14175100000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7110, median time to profit 1.0, bankruptcy frequency 0/25, average scenario score 67.2, scenario wins/timeouts/bankruptcies 0/25/0, average active route operations 2.3, average in-transit shipments 0.0 at tick 12.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with 21 captures in `artifacts/godot-visual-qa/visual-qa-20260501-141801`.

## Review Notes

- Delegated simulation review found four issues and all were fixed: partial dispatch no longer mutates the saved unit cap, scenario objective credit only counts the explicitly selected operation, route maintenance no longer double-counts inside per-operation expected net, and production reservations only protect dispatchable operations.
- Delegated integration review found three issues and all were fixed: route transits must reference and match saved operations, Godot Stop Operation targets the visible active operation, and operation priority was removed from save state because it is derived from current route policy and market pressure.

## Risks

- `PrototypeSession` is now carrying more orchestration responsibility. A later refactor should move route-operation allocation/dispatch into a Godot-free logistics service before fleet/unit depth grows.
- The benchmark records final in-transit count, not total dispatched/arrived throughput, so it can miss busy networks that fully drain by tick 12.
- `PendingRouteContractId` is retained for UI focus compatibility only; future save cleanup should migrate selection state away from gameplay naming.
- Stopping a route operation currently cancels that operation's in-transit shipments to keep save v4 transit references strict. A later logistics pass can model independent shipment ownership if desired.

## Next Step

Review route-operation extraction into a Godot-free logistics service before adding fleet units or multi-leg scheduling.
