# 2026-04-30 Graphic Polish Pass Checkpoint

## Summary

Continued from synced Full HD/visual readability work onto `agent/graphic-polish-pass`. This phase keeps the implementation code-native in Godot while moving the prototype closer to the GPT Image UI direction: map-first composition, stronger ledger-cartography styling, clearer map labels, better route readability, and a sidebar that reads as connected system panels instead of one long debug column.

## Changed Systems

- `ChartersOfTrade.Godot`: wrapped sidebar systems in section panels for Company Ledger, System Test Bench, Map Mode, Route Contract, Inspector, Market Pressure, Warehouse Policy, City Network, and Event Ledger.
- `ChartersOfTrade.Godot`: adjusted Full HD layout widths so the map remains primary while the right panel has enough room for readable explanations and controls.
- `ChartersOfTrade.Godot`: added styled buttons, larger log text, section accents, and a minimum/expand behavior for the Event Ledger panel.
- `ChartersOfTrade.Godot`: replaced the old map banner/legend with a top map HUD and left map-mode rail.
- `ChartersOfTrade.Godot`: enriched terrain visuals with deterministic water strokes, ridge marks, grove marks, and subtler grid lines.
- `ChartersOfTrade.Godot`: improved route readability with dark outlines, directional arrows, and existing animated pulses.
- `ChartersOfTrade.Godot`: added collision-aware city label placement and suppressed duplicate hover labels when the city is already labeled by the active map mode.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 25/25 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; produced `artifacts/godot-smoke/visual-smoke-20260430-04040700000002.png` at 1920x1080.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.

## Review Notes

- Delegated Godot UI review found no blockers and no architecture violation; the diff stayed confined to the Godot presentation file.
- Fixed the P2 Event Ledger sizing note by letting the Event Ledger section expand vertically and giving it a minimum height.
- Fixed the P3 hover-label readability note by suppressing the extra hover label when the active mode already labels the hovered city.

## Risks

- The UI is still a prototype systems test harness, not final art direction.
- Map labels use a simple local collision pass; larger maps may need a cached label layout pass.
- The map still redraws every frame for route pulse animation.

## Next Step

Run manual visual QA across Routes/Profit/Demand modes and multiple seeds at 1920x1080 before adding the next warehouse automation controls.
