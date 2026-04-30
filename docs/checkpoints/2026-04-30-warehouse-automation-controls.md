# 2026-04-30 Warehouse Automation Controls Checkpoint

## Summary

Continued from pushed `main` after integrating `origin/agent/graphic-polish-pass` onto `agent/warehouse-automation-controls`. This phase turns warehouse policy from read-only diagnostic text into deterministic gameplay state: city/resource reorder automation, reserve stock, and route goods priority now flow through typed bridge methods, save/hash state, Godot controls, and smoke coverage.

## Changed Systems

- `Persistence.Core`: bumped prototype saves to version 2 and added warehouse policy plus route policy save records with validation and stable hash normalization.
- `GodotBridge`: added `PrototypeWarehousePolicyView`, `PrototypeRoutePolicyView`, policy setters, reserve-aware exportability, route reserved-resource filtering, and route priority boosts for contracts/logistics.
- `ChartersOfTrade.Godot`: added warehouse reorder, reserve slider, route reserved-good, and route priority controls to the sectioned sidebar; inspectors and system probe now summarize saved policy state.
- `Tests`: added console coverage for reorder toggles, reserve-stock contract removal, route reservation filtering/priority, invalid policy no-ops, orphan priority save validation, and policy snapshot exposure.
- `Docs`: added `ADR-0006-warehouse-policy-gameplay-state.md` and updated `PROJECT_MEMORY.md` through the memory completion gate.

## Tests

- `dotnet --info`: could not run in this macOS Codex workspace because `dotnet` is not installed.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File ./tools/test.ps1`: could not run in this macOS Codex workspace because `pwsh` is not installed.
- Required Windows follow-up: run `tools/test.ps1` and require 30/30 console tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`.
- Required Windows follow-up: run `tools/benchmark.ps1` and require 25/25 playable seeds; compare unmet demand to the prior graphic-polish baseline of 0.7115.

## Review Notes

- Delegated Godot/runtime review found a P1 route-policy selection drift and P2 brittle smoke control lookup. Both were fixed by preserving the selected route resource before control refresh and naming the smoke-targeted controls.
- Delegated simulation/persistence review found a P1 disabled-reorder auto-shipping gap and P2 orphan route-priority save validation gap. Both were fixed by excluding disabled reorder resources from automatic logistics and validating priority resources against reserved resources, with added console coverage.
- Follow-up delegated re-checks reported no remaining findings.

## Risks

- Save version 2 has no migration path yet; durable external saves still need explicit migration handling.
- Route priority is a P0 control surface and uses a simple priority boost, not final route scheduling.
- Godot slider updates are immediate bridge calls; larger policy panels may need debounced edits later.
- Full verification remains Windows/Godot/.NET-bound because this macOS workspace lacks `dotnet`, `pwsh`, and the configured Godot runtime.

## Next Step

Run Windows verification for `agent/warehouse-automation-controls`, then continue with guide-aligned city specialization through district-slot city roles.
