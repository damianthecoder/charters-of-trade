# Checkpoint: Visual UX Map Modes

## Summary

Executed the Visual UX Map Modes agent plan against the Godot presentation layer. The prototype now has Routes, Profit, and Demand map modes, clearer city type stamps, route/city warning marks, stronger inspector structure, and a visible route contract control area that degrades to a disabled placeholder because the bridge contract surface is not present in this checkout.

## Changed Systems

- `ChartersOfTrade.Godot`: updated `BootstrapPanel.cs` with map-mode buttons, mode-specific route/city drawing, city stamps for charter towns, ports, and market towns, warning marks for unmet demand, losing routes, and route capacity pressure, richer city/route inspector text, and a route contract control block.
- Documentation: updated `PROJECT_MEMORY.md` and added this checkpoint.

## Tests

- `git diff --check`: passed.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: not run in this Codex macOS session because `powershell` is not installed.
- `pwsh -ExecutionPolicy Bypass -File .\tools\build.ps1`: not run because `pwsh` is not installed.
- Fallback `dotnet --info`: not run successfully because `dotnet` is not installed.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: not run for the same missing-tooling reason.

## Review Notes

- Delegated static review found no blocking P0/P1 issues and no core dependency leakage.
- Fixed P3 review finding: pending route contract option choice is preserved across view refreshes when no bridge-selected contract is present.
- Deferred P2 review finding: interaction smoke coverage still only starts the scene and does not click map modes, city/route hit targets, `Advance Tick`, or `Run 5`.
- Deferred P3 review finding: per-frame redraw still repeats route pressure and relatedness scans; acceptable for P0 map scale, but cache map lookups before larger maps.

## Risks

- C# and Godot API compatibility still need confirmation on the normal Windows Godot/.NET workstation.
- Manual QA for scene startup, map nonblank state, hover, city/route selection, map-mode switching, `Advance Tick`, `Run 5`, and the contract placeholder is still pending.
- The route contract control is intentionally disabled until `PrototypeSnapshot.AvailableContracts`, `SelectedContractId`, and `PrototypeSession.SelectRouteContract(string)` are available.

## Next Step

Run full build/test/Godot smoke on the normal Windows Godot/.NET workstation for this visual UX branch.
