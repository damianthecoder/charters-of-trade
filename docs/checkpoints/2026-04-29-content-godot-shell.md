# 2026-04-29 Content And Godot Shell Checkpoint

## Summary

Implemented typed P0 content loading, validation, deterministic content hashing, and the first runnable Godot .NET shell. The Godot scene now starts from `Main.tscn`, instantiates a C# `BootstrapPanel`, calls `GodotBridge`, and displays the starter simulation hashes and counts.

## Changed Systems

- Content: added `Content.Core` with JSON loading, validation, and canonical SHA-256 content hash.
- Economy scenario: starter market and market needs now derive from P0 resource tags and tiers.
- Persistence bridge: starter save now stores the real content hash and content-driven initial market/prices.
- Godot bridge: `NewGameSnapshot` includes `ContentHash`; content path resolution now walks upward from runtime directories so Godot and tests can both locate root `content`.
- Godot layer: added `ChartersOfTrade.Godot.csproj`, `scenes/Main.tscn`, and `Scripts/BootstrapPanel.cs`.
- Tooling: root `NuGet.Config` points at local GodotSharp packages; `NuGetAudit` disabled for clean offline builds.
- Content data: removed unknown `linen` substitute from `wool` so validation passes.

## Tests Run

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed, 0 warnings.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: 9/9 passed.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: 25/25 playable seeds, average unmet demand ratio 0.6733.
- `powershell -ExecutionPolicy Bypass -File .\tools\godot.ps1 --headless --path .\src\ChartersOfTrade.Godot --import`: passed.
- `powershell -ExecutionPolicy Bypass -File .\tools\godot.ps1 --headless --path .\src\ChartersOfTrade.Godot --scene res://scenes/Main.tscn --quit-after 2`: passed.

## Risks

- Godot shell is diagnostic only; it does not render the world map yet.
- Content validation is code-based; no JSON schema or authoring/export pipeline exists yet.
- Benchmark metrics are still proxy metrics and should not be treated as final balance.
- Godot CLI may need elevated filesystem access because the editor writes settings under `%APPDATA%`.

## Next Step

Render generated settlement nodes and route lines in Godot, then add a simple deterministic tick action with a ledger-style diagnostic panel.
