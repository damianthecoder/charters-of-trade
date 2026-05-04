# 2026-05-04 Integrated Testing Ready

## Summary

Created `codex/integrated-testing-ready` and merged the active UI visibility work, Logistics 1.0 route-operation network, legacy `codex/ui-visibility-pass`, and legacy `agent/warehouse-policy-view-controls`. The older UI branches were preserved for history and documentation, while the newer 2026-05-04 Godot UI/QA implementation remained the active code path where conflicts overlapped.

## Changed Systems

- `PROJECT_MEMORY.md`: records the branch integration, superseded legacy UI branches, and full post-merge verification.
- `docs/checkpoints/2026-05-04-integrated-testing-ready.md`: records this integration checkpoint.
- `docs/adr/ADR-0008-route-operation-network-save-state.md`: merged Logistics 1.0 save-format decision for route operations and transits.
- `src/GodotBridge/PrototypeSession.cs`: merged Logistics 1.0 multi-operation route network, delayed route transits, route-scoped stopping, and route capacity allocation.
- `src/Persistence.Core/SaveGame.cs`: merged save version 4 route-operation and transit state.
- `tests/ChartersOfTrade.Tests/Program.cs`: merged route-operation network, transit validation, and save/load coverage.
- `benchmarks/ChartersOfTrade.Benchmarks/Program.cs`: merged scripted route-operation network activation and benchmark metrics.
- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`, `InteractionSmokeRunner.cs`, `VisualQaRunner.cs`, and `tools/visual-qa.ps1`: kept the latest 2026-05-04 UI visibility/QA code after resolving conflicts with older UI branches.

## Tests

- `git diff --check`: passed.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed with 0 warnings and 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 59/59 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; visual smoke frame `artifacts/godot-smoke/visual-smoke-20260504-04082600000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7110, median time to profit 1.0, bankruptcy frequency 0/25, average scenario score 67.2, scenario wins/timeouts/bankruptcies 0/25/0, average active route operations 2.3, and average in-transit shipments 0.0.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with 24 captures in `artifacts/godot-visual-qa/visual-qa-20260504-040845`.

## Review Notes

- Manual integration review kept current implementations for conflicted Godot UI and QA files because the legacy branches had older presentation-only variants already superseded by later warehouse controls, UI visibility, and Logistics 1.0 compatibility work.
- Legacy branch planning/review/checkpoint docs were preserved so the coordination history remains visible.
- No unresolved merge conflicts remain and all local branches listed for integration are merged into `codex/integrated-testing-ready`.

## Risks

- Godot still logs `Failed to read the root certificate store` on this Windows machine, but smoke and visual QA scenes complete successfully.
- `PrototypeSession` now carries more route-operation orchestration; split dispatch/allocation out before adding fleet units or multi-leg scheduling.
- Visual overlays are stronger but still use simple placement, so larger or denser maps may need label reservation/collision improvements.

## Next Step

Manually test `codex/integrated-testing-ready`, focusing route-operation network UI, save/load continuity, and Stage 3-6 overlay readability before the next logistics refactor.
