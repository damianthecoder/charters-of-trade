# Checkpoint: Flow Throughput Metrics

Date: 2026-05-07

## Summary

Implemented Slice 1 from `docs/agent-plans/next-feature-company-operations-plan.md`. Route-operation throughput is now exposed as derived prototype telemetry: total dispatches, total arrivals, units dispatched, units arrived, and unmet demand served.

The metrics intentionally remain outside save state and state hashes. No save version change or ADR was needed.

## Changed Systems

- `GodotBridge`: added `PrototypeRouteThroughputView` and `PrototypeSnapshot.RouteThroughput`.
- `PrototypeSession`: increments dispatch totals when a route-operation transit is created and arrival/unmet-demand totals when a transit is delivered.
- `Benchmarks`: added CSV columns and summary averages for route dispatches, route arrivals, dispatched units, arrived units, and unmet demand served.
- `Tests`: added deterministic same-seed coverage for route throughput metrics on a known dispatchable route operation.
- Documentation: updated `PROJECT_MEMORY.md` with the new telemetry, benchmark results, risk, and next step.

## Tests

- `git diff --check`: passed.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 60/60 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; visual smoke output was `artifacts/godot-smoke/visual-smoke-20260507-05480600000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7110, median time to profit 1.0, bankruptcy frequency 0/25, average scenario score 67.2, scenario wins/timeouts/bankruptcies 0/25/0, average active route operations 2.3, average in-transit shipments 0.0, average route dispatches 1.7, average route arrivals 1.7, average units dispatched 4.0, average units arrived 4.0, and average unmet demand served 2.8.

## Review Notes

- Local review checked that throughput counters are updated only at actual dispatch and actual delivery points, and that paused/congested/no-stock operation rows do not count as dispatches.
- Local review checked that `RouteThroughput` is assigned after `SaveCodec.ComputeStateHash(save)` is prepared and is never included in save DTOs.
- No delegated review agent was spawned in this run because the active tool policy only allows delegation after an explicit user request.

## Risks

- The benchmark now measures actual throughput, but the naive route activation still creates zero throughput on some seeds and all benchmark scenario rows time out.
- Throughput is cumulative for the current `PrototypeSession` only. That is intentional for telemetry, but historical graphs or save/load continuity would need a separate save-format decision.

## Next Step

Implement Slice 2: persisted production focus save state, likely with save version 5 and ADR-0009.
