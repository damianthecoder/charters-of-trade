# 2026-04-30 Full HD Test UI Checkpoint

## Summary

Continued from the synced `agent/logistics-warehouse-policies` branch onto `agent/full-hd-test-ui`. This phase makes the Godot prototype more useful as a manual systems test harness and sets Full HD as the project viewport baseline.

## Changed Systems

- `ChartersOfTrade.Godot`: set the project viewport to 1920x1080 with canvas-item stretch and expanded aspect.
- `BootstrapPanel.cs`: added responsive layout profiles for compact, mid, and Full HD windows.
- `BootstrapPanel.cs`: wrapped the sidebar in a scroll container so long diagnostic sections remain reachable without crushing the map.
- `BootstrapPanel.cs`: added a System Test Bench with seed editing, `Reset Seed`, `Run 12`, and a live probe for cashflow, event count, AI move, top pressure, best contract, and save hash.
- `InteractionSmokeRunner.cs`: now runs against a Full HD window, verifies the test bench controls, runs to tick 18, and asserts the map area is large enough for a Full HD layout.
- `InteractionSmokeRunner.cs`: review fix now changes the seed, exercises `Reset Seed`, checks tick/hash/seed reset behavior, and verifies the sidebar has an actual vertical scroll path.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: passed with 22/22 tests and `INTERACTION_SMOKE PASS`.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: passed with 25/25 playable seeds, average unmet demand ratio 0.7055, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.
- Godot movie writer produced a confirmed 1920x1080 control frame at `artifacts/godot-smoke/full-hd-test-ui00000035.png`.
- Current Codex macOS follow-up: `git diff --check` passed after review fixes; full PowerShell/Godot verification was not rerun here because this environment lacks the Windows Godot/.NET toolchain.

## Notes

- The branch is stacked on `agent/logistics-warehouse-policies`, because the test probe reads market and warehouse policy signals added there.
- The generated visual artifact is ignored by git under `artifacts/`.
- The UI is still a prototype/test bench, not final player-facing art direction.
- Review found P2: the smoke runner found the Full HD test bench but did not prove reset or scroll behavior. Fixed by changing the seed, pressing `Reset Seed`, checking tick/hash/seed output, and exercising the sidebar scroll range.

## Next Step

Use the Full HD harness to manually test the current systems, then add explicit warehouse automation controls: reorder toggles, reserve sliders, and route-level reservation policies.
