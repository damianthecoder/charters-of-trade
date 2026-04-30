# 2026-04-30 Visual Readability Pass Checkpoint

## Summary

Continued from the synced `agent/full-hd-test-ui` branch onto `agent/visual-readability-pass`. This phase improves the prototype as a player-facing systems test harness: the sidebar now explains metrics through the systems that produce them, and the map is easier to use for visual verification of terrain, routes, settlements, contracts, and pressure.

## Changed Systems

- `WorldGen.Core`: moved default `WorldGenVersion` to `0.2.0` and replaced independent tile noise with deterministic coherent value-noise terrain fields.
- `WorldGen.Core`: added border-water/coastline readability, explicit settlement spacing, coast-adjacent port placement, and coastal route mode only between two ports.
- `ChartersOfTrade.Godot`: adjusted the Full HD shell so the map remains the primary surface and the sidebar has a fixed, scrollable diagnostics width.
- `ChartersOfTrade.Godot`: renamed and rewrote UI explanations around `Company Ledger`, `Market Pressure`, `Warehouse Policy`, and `Route Contract` so each explanation points at the working system behind it.
- `ChartersOfTrade.Godot`: improved map rendering with coastline strokes, terrain bands, subtler labels, a mode banner, and cached terrain lookup for coast drawing.
- `Tests`: added coherent terrain/world-readability coverage across a 25-seed corpus plus key manual seeds.
- `Tools`: added `tools/visual-smoke.ps1` for non-headless 1920x1080 render verification.
- `Docs`: added `ADR-0005-coherent-worldgen-fields.md`.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 23/23 tests and `INTERACTION_SMOKE PASS`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-smoke.ps1`: passed and produced `artifacts/godot-smoke/visual-smoke-20260430-03434400000002.png` at 1920x1080.

## Review Notes

- Simulation/worldgen review found no P0/P1 blockers. Fixed its P2 notes by tying port placement to coast-adjacent terrain, limiting coastal routes to port-to-port edges, replacing settlement fallback with a clear bounded failure, and broadening terrain invariants.
- Godot/integration review found no P0/P1 blockers. Fixed the per-frame terrain dictionary allocation by caching the terrain lookup when the snapshot world changes.
- The headless smoke render-pixel gap is addressed by adding `tools/visual-smoke.ps1` rather than forcing non-headless Godot into `tools/test.ps1`.

## Risks

- The world generator is now more coherent but still P0 scale; route paths can still be straight-line overlays and are not yet pathfound across terrain.
- `tools/test.ps1` remains headless and does not prove renderer pixels. Use `tools/visual-smoke.ps1` after visual changes.
- The UI is a systems test harness, not final player-facing art direction.

## Next Step

Add explicit warehouse automation controls: reorder toggles, reserve sliders, and route-level reservation policies in the Godot prototype.
