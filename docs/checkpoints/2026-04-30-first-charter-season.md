# 2026-04-30 First Charter Season Checkpoint

## Summary

Implemented Stage 6 first charter season goal loop on `codex-stage6-first-charter-season`. The prototype now tracks a deterministic 12-tick scenario objective with cash, selected route-operation deliveries, resource variety, stable city needs, timeout, win, and bankruptcy result state.

## Changed Systems

- Persistence: bumped save format to version 3 and added validated `ScenarioObjectiveSaveState`.
- GodotBridge: added `FirstCharterSeason` rules, live objective evaluation, selected-delivery counting, stable-need streaks, scoring, ledger result entries, save/hash integration, and scenario snapshot view data.
- Godot UI: added a visible `First Charter Season` sidebar panel and Company Ledger season score metric; smoke/visual QA now verifies the panel body.
- Benchmarks: added scenario result, score, delivery count, and stable-need columns plus aggregate scenario summary.
- Tooling: `tools/visual-qa.ps1` now expects 21 captures because each seed includes a scenario objective capture.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed, 0 warnings, 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed 54/54 tests plus `INTERACTION_SMOKE PASS` and `VISUAL_SMOKE PASS`; visual smoke frame `artifacts/godot-smoke/visual-smoke-20260430-20543900000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed 25/25 playable seeds, average unmet demand 0.7115, median time to profit 1.0, bankruptcy 0/25, average scenario score 65.0, scenario wins/timeouts/bankruptcies 0/25/0.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed 21 captures in `artifacts/godot-visual-qa/visual-qa-20260430-205453`.

## Review Notes

- Delegated review found no blockers.
- Fixed review findings by making scenario stable-need thresholds independent from manual warehouse-policy lowering, counting route-operation deliveries from a structured dispatch result instead of ledger text, adding a first-charter win/timeout/bankruptcy rules test, asserting objective panel body text in smoke/visual QA, tightening scenario save validation around whitespace/duplicates, and replacing mojibake in this checkpoint.

## Risks

- The benchmark does not auto-select contracts, so scenario benchmark rows time out by design while still reporting score/stability. Player-driven success needs either manual play or a future scripted playable season benchmark.
- `PrototypeSession` continues to accumulate vertical-slice orchestration responsibility and should be split if the next phase adds persistent NPC state or multi-objective campaign flow.
- Save v3 has no legacy migration path yet.

## Next Step

Add a scripted season-play benchmark that selects and maintains route operations so the first-charter win path is measured across the seed corpus.
