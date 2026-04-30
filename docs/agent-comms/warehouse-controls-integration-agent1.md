# Agent 1 Notes: Simulation, Save, Logistics

## Status

Completed and integrated.

## Decisions

- Canonical base is `agent/warehouse-control-loop`.
- Save version 2 and `SaveGame.WarehousePolicies` remain canonical.
- Route policy state is additive save v2 state in `SaveGame.RoutePolicies`; it does not replace warehouse policies.
- Route priority resources must be part of that route's reserved resource set.

## Work Log

- Added `RoutePolicySaveState`, `PrototypeRoutePolicyView`, route reservation setters, and route priority setters.
- Wired route reserved resources into available contract filtering and automatic logistics candidate filtering.
- Added a route priority boost to contract ordering and automatic logistics ordering.
- Added console tests for deterministic route policy hashes, reservation filtering, priority ordering, invalid no-ops, and orphan-priority save validation.
- Addressed review findings by deriving default route-policy resources from declared market needs, requiring route policy coverage for every saved route, validating policy resources against route `reservedFor`, and tightening the logistics filtering test around a route/resource that would otherwise ship.
- Coordinated the automation handoff by adding Balanced/Conservative warehouse policy mode to the existing safety/reorder model instead of importing the stale `origin/agent/warehouse-automation-controls` branch. Balanced normalizes to no saved mode; Conservative raises policy thresholds and hashes as non-default state.
- Addressed delegated simulation review suggestions by adding explicit Balanced-vs-null hash normalization coverage and proving Conservative mode flows into route contract policy text.

## Test Notes

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed, 0 warnings, 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed, 36/36 plus `INTERACTION_SMOKE PASS` and `VISUAL_SMOKE PASS`, visual frame `artifacts/godot-smoke/visual-smoke-20260430-14505400000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed, 25/25 playable seeds, average unmet demand 0.7115, median time-to-profit 1.0, bankruptcy 0/25.

## Risks

- Route policy state must not create a second, conflicting warehouse automation save model.
- Route priority is intentionally a simple ordering boost, not final route scheduling.
