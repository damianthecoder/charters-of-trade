# Checkpoint: Warehouse Mode Coordination

## Summary

Coordinated the reported warehouse automation handoff with the synced `agent/warehouse-controls-integration` branch. The fetched `origin/agent/warehouse-automation-controls` branch contained an older reorder/reserve implementation, while the reported detached Balanced/Conservative diff was not present locally or on GitHub. The missing mode behavior was implemented directly on the canonical integration branch.

## Changed Systems

- `Persistence.Core`: added optional warehouse policy `Mode` save field, validation for `balanced`/`conservative`, and canonical normalization that omits default Balanced mode from hashes.
- `GodotBridge`: added `PrototypeSession.SetWarehousePolicyMode`, policy mode constants, Conservative threshold presets, mode-aware policy action text, and `PrototypeMarketSignal.PolicyMode`.
- `ChartersOfTrade.Godot`: added `WarehouseModeOptions` to the warehouse policy controls and visible Balanced/Conservative labels in summaries/policy lines.
- `Tests`: added console coverage for mode threshold changes, hash behavior, reset to default Balanced mode, invalid mode rejection, and save validation.
- `Tools`: increased `tools/visual-qa.ps1` frame budget so the stricter mode-control visibility checks complete all 12 captures.
- `Docs`: updated `PROJECT_MEMORY.md`, ADR-0006, Team plan/communication files, and this checkpoint.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 36/36 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; visual smoke frame at `artifacts/godot-smoke/visual-smoke-20260430-14505400000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, bankruptcy frequency 0/25.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: first stopped at 9/12 captures under the old frame budget, then passed after raising `--quit-after` to 180 and adding mode-selector captures with 15 captures in `artifacts/godot-visual-qa/visual-qa-20260430-145117`.

## Review Notes

- Fixed delegated simulation review suggestion: added coverage proving explicit Balanced mode hashes the same as null/default mode.
- Fixed delegated simulation review suggestion: added contract-layer coverage proving Conservative mode reaches route contract policy text.
- Fixed delegated UI review suggestion: interaction smoke now selects Conservative and then Balanced to cover the special reset path.
- Fixed delegated UI review suggestion: visual QA now captures `WarehouseModeOptions` visibly and expects 15 PNGs.
- Both delegated reviews reported no blockers.

## Risks

- Conservative mode is currently a preset over safety/reorder thresholds, not a full automation strategy layer.
- The stale `origin/agent/warehouse-automation-controls` branch should not be merged over this branch without a fresh diff review.
- Visual QA is still capture/visibility based and needs human inspection for final art quality.

## Next Step

Run one manual gameplay QA pass on warehouse Balanced/Conservative modes plus route blocking/priority across several seeds.
