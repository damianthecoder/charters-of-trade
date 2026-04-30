# Agent 2 Notes: Godot UI, Smoke, Visual QA

## Status

Completed and integrated.

## Decisions

- Preserve map-first Full HD layout and sidebar gutter.
- UI-only selection/sort/focus state stays in Godot.
- Route policy controls live inside the existing Route Contract panel to avoid another sidebar section.
- Visual QA is local Windows verification and is not required for constrained automation.

## Work Log

- Added warehouse policy focus/sort controls for Priority, Safety, and Reorder views.
- Added compact route resource controls for allow/block and route priority.
- Added stable smoke names for contract, policy focus, warehouse resource, and route policy resource controls.
- Added `VisualQaRunner.cs`, `VisualQa.tscn`, and `tools/visual-qa.ps1` for 3 seeds x 3 map modes plus sidebar-bottom captures.
- Moved Godot `APPDATA`/`LOCALAPPDATA` into workspace-local `.godot_user` through `tools/godot.ps1`.
- Addressed review findings by keeping `tools/godot.ps1` from terminating caller scripts, making policy focus update editable warehouse controls, including blocked route resources in the control choices, and adding viewport visibility checks to visual QA.

## Test Notes

- `git diff --check`: passed with only the expected CRLF normalization warning for `tools/godot.ps1`.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed, 0 warnings, 0 errors.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed, 35/35 plus `INTERACTION_SMOKE PASS` and `VISUAL_SMOKE PASS`, visual frame `artifacts/godot-smoke/visual-smoke-20260430-14290800000002.png`.
- `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1`: passed with 12 captures in `artifacts/godot-visual-qa/visual-qa-20260430-142930`.

## Risks

- Sidebar density increased, but visual QA confirmed scrolling and bottom-panel access at 1920x1080.
- The route controls are compact test-harness controls, not final player UX.
