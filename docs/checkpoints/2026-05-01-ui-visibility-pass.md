# 2026-05-01 UI Visibility Pass

## Summary

Completed the focused Phase 0 UI visibility pass on `codex/ui-visibility-pass`. Stage 3-6 systems are now visible in Full HD through first-screen First Charter Season progress bars, Stage Systems status cards, tick-change feedback, and stronger map overlays for production, route operations, NPC pressure, and warehouse pressure.

## Changed Systems

- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`: added season progress bars, Stage 3 Production/Stage 4 Routes/Stage 5 NPC/Warehouse Guard cards, tick-change feedback, and presentation-only system overlays.
- `src/ChartersOfTrade.Godot/Scripts/InteractionSmokeRunner.cs`: asserts first-screen progress/status visibility and tick feedback refresh.
- `src/ChartersOfTrade.Godot/Scripts/VisualQaRunner.cs`: asserts first-screen progress/status visibility across visual QA seeds.
- No simulation core, bridge DTO, save format, economy, logistics, AI, world generation, or benchmark logic changed.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed 54/54 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; visual smoke frame `artifacts/godot-smoke/visual-smoke-20260501-06211200000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, bankruptcy frequency 0/25, average scenario score 65.0, scenario wins/timeouts/bankruptcies 0/25/0.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed 21 captures in `artifacts/godot-visual-qa/visual-qa-20260501-062124`.
- `git diff --check`: passed.

## Review Notes

- Delegated review found no blocking Godot runtime or deterministic-state issue and confirmed the diff stayed in Godot script files.
- Fixed P2 review feedback by moving the First Charter Season progress panel above the System Test Bench so progress is visible on the first Full HD screen.
- Fixed P3 review feedback by adding viewport-intersection assertions for season progress bars and Stage 3/4 status card bodies.
- The first rerun of `tools/test.ps1` after moving the objective panel failed because an old smoke assertion still required the `Run 12` test button to be first-screen visible; that assertion was removed because UI priority shifted to gameplay-system visibility, and the interaction still exercises `Run 12`.
- One parallel `tools/visual-qa.ps1` run failed because it raced with a concurrent build and used stale runner code. A sequential rerun passed.

## Risks

- The map overlays are intentionally presentation-only and animated; they do not affect deterministic state, but they can become crowded as map density grows.
- The System Test Bench is lower in the initial sidebar viewport than before because First Charter Season progress and Stage Systems now take first-screen priority.
- Godot continues to log `Failed to read the root certificate store` during smoke/QA on this Windows machine, but the Godot scenes and captures pass.

## Next Step

Manually inspect the latest Full HD visual QA captures and tune overlay label density before adding more gameplay surface area.
