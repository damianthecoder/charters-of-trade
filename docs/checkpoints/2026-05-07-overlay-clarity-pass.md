# 2026-05-07 Overlay Clarity Pass

## Summary

Improved the Godot presentation overlay for the current Stage 3-6 MVP slice. The pass keeps the deterministic core, bridge DTOs, save format, content, tests, and benchmark logic unchanged; it only makes existing map and sidebar signals easier to read.

## Changed Systems

- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`: map system badges now use plain-language labels such as `Produce: Forge Tools` and `Rival: Cloth` instead of terse `CHAIN/RIVAL` labels.
- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`: production/rival badges are placed through the existing collision-aware label reservation path so they avoid the top HUD, map legend, and city labels more consistently.
- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`: the map legend now explains overlay colors for production, rival pressure, and active route operations.
- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`: route-operation labels now use `Route active` or `Route paused` phrasing and can be drawn for each active route operation route.
- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`: Stage 3-6 status begins with a `Priority:` line using the existing scenario next-step/result text.
- `PROJECT_MEMORY.md`: records the overlay clarity decision, changed presentation surface, verification, residual risk, and next step.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 59/59 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; visual smoke frame `artifacts/godot-smoke/visual-smoke-20260507-05012700000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with 24 captures in `artifacts/godot-visual-qa/visual-qa-20260507-050143`.

## Review Notes

- Self-review confirmed the diff stayed in Godot presentation code plus memory/checkpoint docs.
- No simulation core, save DTO, bridge contract, content, benchmark, or ADR file changed.
- The new badges show more information and are clearer, but the map is also visually busier than the previous terse overlay.

## Risks

- Dense future maps may still need a dedicated reserved overlay layer, filtering controls, or stronger prioritization so production and rival badges do not crowd city labels.
- The pass improves text clarity but does not redesign the prototype sidebar or final gameplay flow.

## Next Step

Manually compare the latest visual QA captures and decide whether overlay filtering or a deeper HUD/sidebar redesign should be the next UI pass.
