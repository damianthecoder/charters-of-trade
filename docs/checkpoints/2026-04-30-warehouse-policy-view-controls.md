# 2026-04-30 Warehouse Policy View Controls Checkpoint

## Summary

Added a documented three-agent workflow plus GPT-5.5 review gate for the next development phase, then implemented the first small accepted step: Godot-only warehouse policy view controls. The controls focus and sort existing `PrototypeMarketSignal` data by priority dispatch, safety stock, or reorder queue. They do not mutate simulation state, save state, content hashes, balance, or benchmark semantics.

## Changed Systems

- `docs/agent-plans`: added `warehouse-policy-view-controls-team.md` to define the three-agent team, GPT-5.5 lead reviewer, communication files, review gates, stop conditions, and current accepted step.
- `docs/agent-plans`: added `warehouse-policy-view-controls-review-gpt55.md` with the delegated review findings and fixes.
- `ChartersOfTrade.Godot`: added `PrototypePolicyViewMode`, policy mode buttons, policy focus dropdown, and per-mode sorting/rendering in `BootstrapPanel.cs`.
- `ChartersOfTrade.Godot`: selecting a city now focuses the Warehouse Policy panel on that city; route selection and clear selection return the panel to auto-focus.
- `ChartersOfTrade.Godot`: updated `InteractionSmokeRunner.cs` to exercise Priority/Safety/Reorder policy views, the policy focus dropdown, and stable named dropdown lookup so policy and contract controls are not confused.
- `ChartersOfTrade.Godot`: added `VisualQa.tscn` and `VisualQaRunner.cs` for non-headless multi-seed visual captures.
- `tools`: added `visual-qa.ps1`, which runs the visual QA scene and verifies 12 PNG captures.

## Tests

- `git diff --check`: passed.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed, 0 warnings, 0 errors.
- `dotnet run --project tests/ChartersOfTrade.Tests/ChartersOfTrade.Tests.csproj --no-build --no-restore`: passed with 25/25 tests.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: console build/tests passed, then failed before Godot interaction smoke because this sandboxed Godot run could not open `user://logs/godot2026-04-30T04.51.37.log`.
- Direct headless Godot smoke attempts with workspace user-data environment/path also failed before scene completion with the same `user://logs` open error followed by signal 11.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` outside the sandbox: passed with 25/25 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`; produced `artifacts/godot-smoke/visual-smoke-20260430-05241000000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1` outside the sandbox: passed with 12 captures in `artifacts/godot-visual-qa/visual-qa-20260430-052337`.
- Representative captures reviewed: `seed-424242-routes.png` and `seed-424242-sidebar-bottom.png` showed a readable primary map, usable sidebar scrolling, and clear Warehouse Policy controls/content.

## Review Notes

- GPT-5.5 lead review found no P0/P1/P2 blockers and confirmed the change stays inside MVP scope.
- GPT-5.5 confirmed no Godot dependency leaked into core projects and the controls remain presentation-only.
- Fixed the P3 misleading auto-focus label by relabeling the dropdown per active policy view.
- Fixed the P3 smoke coverage gap by exercising the policy focus `OptionButton` directly.
- Follow-up GPT-5.5 review of the visual QA runner found no blockers, no architecture violations, and no untracked generated artifacts outside ignored output folders.

## Risks

- Full Godot interaction and visual smoke still need to be run outside the current sandbox because Godot cannot write its user log here.
- The sidebar is denser after adding policy controls, so manual 1920x1080 QA should check text fit and visual scroll comfort.
- Warehouse policy view controls must remain inspection aids until a separate gameplay/save-format decision is recorded.
- The new visual QA runner is intentionally non-headless and should be treated as local Windows verification, not a required sandbox check.

## Next Step

Push local branch `agent/warehouse-policy-view-controls` after explicit user approval, then open review or start the next narrow pass on player-facing warehouse policy decisions.
