# Checkpoint: Route Contract System

## Summary

Implemented the first Godot-free route contract system for the P0 prototype. `PrototypeSnapshot` now exposes available route contracts and the selected contract id, and `PrototypeSession.SelectRouteContract` lets the presentation layer select a contract that drives the next logistics tick.

## Changed Systems

- `GodotBridge`: added `PrototypeRouteContractView`, available contract generation, deterministic contract selection, selected-contract logistics execution, and production reservation so the contracted cargo is not consumed before delivery.
- `Persistence.Core`: added `pendingRouteContractId` to `SaveGame` so pending player logistics choices are part of the state hash.
- `Tests`: added deterministic route contract coverage and a hash assertion for pending selections.
- `Tools`: removed the redundant Godot `--build-solutions` step from `tools/test.ps1`; the script already builds the full .NET solution and still runs Godot scene smoke.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed, 0 warnings.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed, 18/18 tests plus Godot headless scene smoke.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed, 25/25 playable seeds, average unmet demand ratio 0.6967, median time to profit 1.0, bankruptcy frequency 0/25.

## Review Notes

- Initial review found P1: selected contracts affected next tick behavior but were absent from save/hash state. Fixed by adding `pendingRouteContractId` to `SaveGame` and asserting selection changes the current state hash.
- Initial review found P1: selected contracts briefly executed before production. Fixed by keeping production-before-logistics order and reserving contracted cargo during production.
- Initial review found P2: adding contract data as positional `PrototypeSnapshot` members could break bridge consumers. Fixed by exposing `AvailableContracts` and `SelectedContractId` as additive init properties.
- Follow-up review reported no blockers.

## Risks

- `pendingRouteContractId` extends the save v1 shape while the save pipeline is still prototype-only.
- Route contract execution is intentionally simple: one selected contract replaces automatic route choice for that logistics tick while selected.
- Godot `--build-solutions --quit` hung and produced a crash dialog in this workspace. The test script now relies on the normal solution build plus scene smoke instead.

## Next Step

Integrate route contract controls in the Godot visual UX branch using `AvailableContracts`, `SelectedContractId`, and `SelectRouteContract`.
