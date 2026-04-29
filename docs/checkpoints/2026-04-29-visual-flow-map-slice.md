# Checkpoint: Visual Flow Map Slice

## Summary

Started the visual layer for the P0 prototype. The Godot screen now presents the game as a flow map: cities and routes can be selected, routes show cashflow signals, route pulses imply movement, city rings show supply state, and the sidebar includes a contextual inspector plus priority signals.

## Changed Systems

- `ChartersOfTrade.Godot`: upgraded the prototype shell from static map plus logs to an interactive visual slice with city/route hit testing, hover/selection states, route cash labels, supply rings, animated route pulses, priority signals, and an inspector.
- Documentation: added visual-layer research notes and recorded the Ledger Cartography direction.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed, 0 warnings.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: 14/14 tests passed plus Godot headless build and scene smoke.

## Review Notes

- Review found no blocking issues.
- Deferred P3: the map redraws every frame for route pulse animation and does per-frame lookup/LINQ work. Acceptable for P0 scale, but cache static map data before larger maps.
- Deferred P3: Godot smoke test starts the scene but does not click UI or verify selection/inspector behavior. Add interaction smoke coverage before relying on this UI for gameplay QA.

## Risks

- The Godot UI is still a prototype/debug surface, not final art direction.
- Selection and visual state must stay in the Godot presentation layer; core systems remain Godot-free.
- Flow-map readability can degrade quickly as nodes and routes increase.

## Next Step

Add explicit route contract choice backed by a lightweight interaction smoke test for map selection and tick controls.
