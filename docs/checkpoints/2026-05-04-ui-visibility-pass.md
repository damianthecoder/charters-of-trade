# 2026-05-04 UI Visibility Pass

## Summary

Completed the Autoprojektowanie #2 UI visibility pass for the current Stage 3-6 MVP slice. The work makes existing systems obvious in the first Full HD screen without changing deterministic core state, GodotBridge data contracts, scenario rules, or save format.

## Changed Systems

- Godot presentation: added a first-screen `Stage 3-6 Status` panel summarizing Production Chains, Route Operation, NPC Pressure, First Charter Season progress, and tick feedback.
- Godot presentation: changed the First Charter Season panel from plain progress text to compact progress bars for cash, deliveries, and stable needs.
- Godot map: added HUD pills for season/operation/chain status, active route-operation labels, and combined `CHAIN`, `RIVAL`, or `CHAIN/RIVAL` badges for top production/NPC signals.
- Godot QA: interaction smoke now asserts the new status panel, objective progress bars, route-operation activation status, and tick feedback.
- Visual QA: added one Stage 3-6 status capture per seed and raised `tools/visual-qa.ps1` expected capture count from 21 to 24.
- Team coordination: added `docs/agent-plans/automation-ui-visibility-team.md` for the three-agent team plus GPT-5.5 supervisor workflow.

## Tests

- `git diff --check`: passed with only the expected CRLF warning for `tools/visual-qa.ps1`.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed earlier in the run with 0 warnings and 0 errors before the final badge follow-up.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed after final edits with 54/54 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`, producing `artifacts/godot-smoke/visual-smoke-20260504-03405000000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, bankruptcy frequency 0/25, average scenario score 65.0, and scenario wins/timeouts/bankruptcies 0/25/0.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with 24 captures in `artifacts/godot-visual-qa/visual-qa-20260504-034102`.

## Review Notes

- GPT-5.5 supervisor gate approved the presentation-only direction before implementation.
- Delegated final review found no blocking issues and confirmed no deterministic core, save DTO, content, bridge, csproj, or ADR files were changed.
- Review residuals: map badges/labels are still simple overlays and visual QA does not pixel-assert their exact placement.
- The badge crowding residual was partially addressed by combining same-city production/NPC markers into one `CHAIN/RIVAL` badge.

## Risks

- Map overlay labels are still manually placed; larger maps or denser signals may need a reserved/collision-aware system badge layer.
- Visual QA verifies text presence, capture count, and nonblank frames, but not exact pixel placement of every overlay.
- Benchmark scenario win rate remains 0/25 timeouts because unattended benchmark runs still do not select route contracts.

## Next Step

Add a scripted First Charter Season benchmark or smoke path that selects route contracts so scenario win rate becomes an actionable balance KPI.
