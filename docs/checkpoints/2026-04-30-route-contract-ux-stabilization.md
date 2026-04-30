# 2026-04-30 Route Contract UX Stabilization Checkpoint

## Summary

Stabilized the integrated `agent/route-contract-system` branch for PR readiness. The Godot prototype now has a test-only interaction smoke scene that loads the real `Main.tscn`, clicks through map modes, city and route selection, route contract selection, `Advance Tick`, and `Run 5`, then checks for post-interaction visual content. Route contract controls and inspectors now communicate selected, best, preview, empty, and stale contract states more clearly.

## Changed Systems

- `ChartersOfTrade.Godot`: added `InteractionSmokeRunner.cs` and `InteractionSmoke.tscn` for headless UI interaction coverage against the real prototype scene.
- `ChartersOfTrade.Godot`: polished `BootstrapPanel.cs` route contract dropdown labels, action button state, contract summaries, and city/route inspector wording.
- `Tools`: updated `tools/test.ps1` to run the interaction smoke scene instead of only starting `Main.tscn`.
- `Project memory`: recorded the stabilized UX, new smoke path, and remaining Windows verification step.

## Tests

- `git diff --check`: passed in the current macOS Codex session.
- Untracked smoke scene/script whitespace checks: passed via `git diff --no-index --check` with no whitespace errors.
- `tools/test.ps1`: not run in this macOS Codex session because `dotnet`, `pwsh`, and `powershell` are not installed.
- `tools/benchmark.ps1`: not run in this macOS Codex session for the same runtime/tooling reason.

## Review Notes

- The two-agent implementation split kept smoke tooling and UX polish separate: Agent 1 owned interaction smoke files and `tools/test.ps1`; Agent 2 owned `BootstrapPanel.cs`.
- The deterministic core and route contract bridge APIs were not changed in this stabilization pass.
- Delegated review found no P0/P1 blockers.
- Delegated review found P2: `tools/test.ps1` trusted Godot's exit code without requiring the `INTERACTION_SMOKE PASS` marker. Fixed by capturing Godot output and failing if the marker is absent.

## Risks

- The new interaction smoke path still needs a real Windows Godot/.NET run to confirm Godot C# API compatibility and headless viewport capture behavior.
- Godot CLI smoke may still need to run outside sandboxed sessions because Godot writes runtime logs under `user://`.
- The visual map still redraws every frame for route pulse animation; acceptable for P0 scale, but cache static map layers before larger maps.

## Next Step

Run `tools/test.ps1` and `tools/benchmark.ps1` on the normal Windows Godot/.NET workstation, then push `agent/route-contract-system` and open the PR to `main`.
