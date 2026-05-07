# Checkpoint: Production Focus Save State

Date: 2026-05-07

## Summary

Implemented Slice 2 from `docs/agent-plans/next-feature-company-operations-plan.md`. City production can now be set to `auto`, focused on one recipe, or paused. Non-default production policies are persisted gameplay state and participate in stable save hashes.

The save format moved to version 5 and `ADR-0009-production-focus-save-state.md` records the decision.

## Changed Systems

- `Persistence.Core`: added `ProductionPolicySaveState`, `SaveGame.ProductionPolicies`, save version 5, canonical sorting/default normalization, and validation for city references and production modes.
- `GodotBridge`: added `PrototypeProductionPolicyView`, production focus/clear/pause methods, save serialization of non-default production policies, focused recipe ordering, and input reservation for focused recipes that cannot run yet.
- `ChartersOfTrade.Godot`: added Production Chains controls for Set Focus, Auto, and Pause; Stage 3 status, city inspector, system probe, and map badges now expose production focus state.
- `Tests`: added production focus hash/no-op coverage plus production policy save-load and validation tests.
- `InteractionSmokeRunner`: now sets production focus from the UI and verifies that the save hash and Stage 3 status respond.
- Documentation: added ADR-0009 and updated `PROJECT_MEMORY.md`.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 64/64 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; visual smoke output was `artifacts/godot-smoke/visual-smoke-20260507-06080100000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7110, median time to profit 1.0, bankruptcy frequency 0/25, average scenario score 67.2, scenario wins/timeouts/bankruptcies 0/25/0, average active route operations 2.3, average in-transit shipments 0.0, average route dispatches 1.7, average route arrivals 1.7, average units dispatched 4.0, average units arrived 4.0, and average unmet demand served 2.8.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with 24 captures in `artifacts/godot-visual-qa/visual-qa-20260507-060813`.

## Review Notes

- Local review checked that default `auto` production policies normalize out of hashes while `focus` and `paused` policies change hashes.
- Local review checked that focus methods reject unknown cities/recipes without tick or hash changes.
- Local review checked that production focus is implemented in `GodotBridge`/persistence DTOs and does not introduce Godot dependencies into core simulation projects.
- No delegated review agent was spawned because the active tool policy only allows delegation after an explicit user request.

## Risks

- Save v5 has no migration path from v4 yet.
- Focused production is city-level and recipe-level only. It does not yet include production slots, build costs, cooldowns, or city development commitments.
- Benchmarks do not yet use production focus, so the scenario win rate remains the naive route-operation baseline.

## Next Step

Implement Slice 3: a scripted First Charter Season win path that uses production focus plus route operations, and report it separately from naive benchmark play.
