# Checkpoint: Route Contract + Visual UX Integration

## Summary

Merged `origin/agent/visual-ux-map-modes` into `agent/route-contract-system` and integrated the visual map modes with the route contract API. The Godot prototype now shows Routes/Profit/Demand modes, city stamps, warnings, richer inspectors, and active route contract controls backed by typed `GodotBridge` APIs.

## Changed Systems

- `ChartersOfTrade.Godot`: integrated map mode UI, visual route/city warnings, city type stamps, route contract dropdown/action, and contract-aware inspector text.
- `ChartersOfTrade.Godot`: replaced temporary reflection-based contract bridge access with typed `PrototypeSnapshot.AvailableContracts`, `PrototypeSnapshot.SelectedContractId`, and `PrototypeSession.SelectRouteContract`.
- Documentation: resolved `PROJECT_MEMORY.md`, added the visual UX checkpoint from the parallel branch, and recorded this integration checkpoint.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed outside the sandbox with 18/18 tests plus Godot headless scene smoke.
- The same test command first failed inside the sandbox because Godot could not write `user://logs` and crashed with signal 11; rerunning outside the sandbox fixed it.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.6967, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.
- Godot visual smoke capture passed and produced `artifacts/godot-smoke/visual-smoke00000002.png`, showing a nonblank integrated UI with map modes and an active route contract dropdown.

## Review Notes

- Delegated review found no P0/P1 blockers.
- Fixed P2: route inspector now distinguishes zero matching contracts from missing bridge data.
- Fixed P2: `PROJECT_MEMORY.md` now records integrated verification results.
- Fixed P3: contract summary refreshes when the dropdown selection changes.
- Fixed P3: route contract UI now uses typed APIs instead of reflection.
- Deferred: automated smoke coverage still does not click map modes, city/route hit targets, tick controls, or contract selection.

## Risks

- Godot CLI smoke may require outside-sandbox execution because Godot writes runtime logs under `user://`.
- Current visual smoke proves startup and nonblank rendering, not end-to-end UI interaction.

## Next Step

Add a lightweight interaction smoke path for map-mode clicks, city/route selection, tick controls, and contract selection.
