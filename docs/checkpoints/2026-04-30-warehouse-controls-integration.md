# Checkpoint: Warehouse Controls Integration

## Summary

Integrated warehouse policy controls, route resource reservation/priority controls, save v2 route policy state, Godot UI controls, interaction smoke coverage, and multi-seed visual QA on `agent/warehouse-controls-integration`.

## Changed Systems

- `Persistence.Core`: added `RoutePolicySaveState`, normalized route policy hashing, and validation requiring complete per-route policy coverage.
- `GodotBridge`: added route policy views/setters, route resource filtering, route priority boosts, route policy save output, and content-need-derived default route resources.
- `ChartersOfTrade.Godot`: added warehouse policy focus/sort controls, route resource allow/block and priority controls, smoke-friendly control names, and visual QA runner scene.
- `tools`: added `tools/visual-qa.ps1` and adjusted `tools/godot.ps1` so Godot CLI calls use workspace-local user data without terminating caller scripts.
- `docs`: updated `PROJECT_MEMORY.md`, ADR-0006, and Team communication files.

## Tests

- `git diff --check`: passed with only the expected CRLF normalization warning for `tools/godot.ps1`.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 35/35 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; visual smoke frame at `artifacts/godot-smoke/visual-smoke-20260430-14290800000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, bankruptcy frequency 0/25.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with 12 captures in `artifacts/godot-visual-qa/visual-qa-20260430-142930`.

## Review Notes

- Fixed simulation review finding: default route policy resources now come from declared market needs rather than starter market stock.
- Fixed simulation review finding: save validation now rejects missing route policies and policy resources outside each route's saved `reservedFor` list.
- Fixed simulation review finding: route logistics filtering test now blocks a route/resource that the baseline run proves would otherwise ship.
- Fixed UI review finding: Godot helper no longer exits caller scripts from inside `tools/godot.ps1`.
- Fixed UI review finding: policy focus dropdown updates editable warehouse policy controls.
- Fixed UI review finding: blocked route resources remain visible in the route policy dropdown.
- Fixed UI review finding: visual QA asserts sidebar control visibility before capture.

## Risks

- Route priority is still a simple deterministic ordering boost, not final scheduling.
- Route controls are compact testing controls, not the final player-facing workflow.
- Visual QA verifies visibility and captures frames, but still needs human inspection for art-direction quality.

## Next Step

Run one manual gameplay QA pass on route blocking and priority across several seeds.
