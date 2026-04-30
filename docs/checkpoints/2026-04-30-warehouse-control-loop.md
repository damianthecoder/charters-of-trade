# 2026-04-30 Warehouse Control Loop Checkpoint

## Summary

Started `agent/warehouse-control-loop` from synced `main` after the graphic polish merge. This phase turns warehouse policy from a read-only signal into the first player-controllable gameplay loop: select a city, choose a resource, set safety stock and reorder point, apply the policy, and watch save hash, market signals, export availability, and route contract priority react deterministically.

## Changed Systems

- `GodotBridge`: added `PrototypeSession.SetWarehousePolicy(cityId, resourceId, safetyStock, reorderPoint)`.
- `GodotBridge`: warehouse policy overrides now clamp to deterministic bounds, reject invalid city/resource targets, and only accept resources with tracked market needs.
- `GodotBridge`: `PrototypeMarketSignal` now exposes `IsPolicyOverridden`; market signals, route contract priority, and exportable warehouse stock use effective policy values.
- `Persistence.Core`: bumped save format to version 2 and added `WarehousePolicySaveState` plus `SaveGame.WarehousePolicies`.
- `Persistence.Core`: save validation now requires the current save version and validates policy ids, duplicates, negative values, and reorder/safety ordering.
- `ChartersOfTrade.Godot`: added Warehouse Policy controls in the sidebar for selected cities: resource dropdown, safety stock input, reorder input, and Apply button.
- `ChartersOfTrade.Godot`: policy readouts now explain reserved stock, exportable stock, manual/default policy source, and the save-hash effect after applying a policy.
- `Tests`: added deterministic policy override, save hash, clamp/reject, route contract priority/availability, and save-load hash coverage.
- `Tests`: extended interaction smoke to scroll the sidebar, prove warehouse controls are visible, apply a policy, assert save hash changed, and continue through route contract/tick flow.
- `Docs`: added `ADR-0006-warehouse-policy-save-state.md`.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 30/30 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; produced `artifacts/godot-smoke/visual-smoke-20260430-04293200000002.png` at 1920x1080.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.

## Review Notes

- Simulation/save/logistics review found no blockers and flagged two P2 issues. Fixed both by rejecting non-need resources as policy targets and requiring save version 2 explicitly.
- Godot/integration review found no blockers and flagged one P2 plus one P3. Fixed the P2 by scrolling to the Warehouse Policy controls and asserting viewport intersection in smoke; fixed the P3 by using an explicit contract-candidate helper with clearer failure output.

## Risks

- Save version 2 currently has no legacy v1 migration path; old saves should not be treated as supported until migration logic exists.
- Warehouse controls are numeric and test-harness oriented; the later player UI should probably offer presets or clearer affordances.
- The first policy loop protects stock but does not yet expose route-level unit reservations.

## Next Step

Extend the control loop into route-level reservation controls: reserve units for a selected route contract, expose remaining exportable stock, and show the cashflow/demand effect after ticks.
