# Project Memory

## Current Goal

Turn the visible Godot 4.x .NET prototype into an interactive P0 vertical slice: the core systems now tick together, the Godot screen exposes flow-map modes and clearer inspection, and the next step is verifying the visual UX branch in the normal Godot/.NET environment before integrating explicit route contract choice.

## Latest Decisions

- Engine target is Godot 4.x .NET, not Unity.
- The first implementation milestone is a deterministic simulation core outside Godot.
- Core modules are split into world generation, economy, logistics, city simulation, AI, persistence, Godot bridge, tests, and benchmarks.
- Tests avoid external NuGet packages for now so the project can build in a restricted network environment.
- Research escalation starts after 15 minutes of blocked work.
- Local verification should use `tools/build.ps1`, `tools/test.ps1`, and `tools/benchmark.ps1`.
- Godot .NET 4.6.1 is installed at `C:\Users\damia\OneDrive\Pulpit\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64\Godot_v4.6.1-stable_mono_win64_console.exe`; use `tools/godot.ps1` for CLI calls.
- Typed content loading lives in `Content.Core`; P0 resources/recipes are validated before the scenario starts and produce a deterministic `sha256:` content hash.
- Godot C# uses `Godot.NET.Sdk/4.6.1` from the local GodotSharp package folder through root `NuGet.Config`.
- Godot C# scenes need `[dotnet] project/assembly_name` in `project.godot`; hand-authored C# scene scripts are marked `[GlobalClass]` so the editor cache can map them reliably.
- NuGet audit is disabled in `Directory.Build.props` because local offline builds should not depend on nuget.org vulnerability metadata.
- Larger changes require automatic delegated code review before the final session summary. Changes touching worldgen, economy, logistics, save/load, AI, balance, CI, or Godot runtime use at least one review agent; cross-subsystem changes touching 3+ systems use two focused reviewers.
- The always-on documentation agent is implemented as a recurring memory keeper ritual, not a real background process. Small changes are tracked by the main agent; larger/cross-cutting sessions can delegate a dedicated memory keeper agent to prepare `PROJECT_MEMORY.md` and checkpoint updates before the final summary.
- The current P0 prototype loop is intentionally hosted in `GodotBridge` as a vertical-slice coordinator; clean core modules remain Godot-free.
- Review pass on the interactive prototype fixed P1/P2 risks: world hashes now include terrain, market consumption uses declared needs, production cash only follows produced recipes, saves reject invalid negative state, and local test/benchmark scripts rebuild before running.
- Visual direction starts with Ledger Cartography: historical map and merchant ledger materiality combined with modern flow-map readability. Territory remains quiet; routes, markets, margins, capacity, and supply pressure are the main visual language.
- Visual selection, hover, animation, map modes, colors, contract UI placeholders, and inspector state belong in the Godot presentation layer. They must not leak into the deterministic core or save format.
- Git is the project coordination baseline. Work should branch from `main`, keep generated Godot/.NET caches ignored, and use commits/checkpoints to make parallel agent work reviewable. The GitHub remote is `origin` at `https://github.com/damianthecoder/charters-of-trade.git`.

## System State

- `WorldGen.Core`: deterministic seedable world generation, raster summary, settlement nodes, route candidates, world hash, solvency kernel.
- `Economy.Core`: resource and recipe definitions, inventory, market pricing, basic production tick.
- `Logistics.Core`: routes, capacities, lead time, route profitability.
- `CitySim.Core`: cohort population, city stock, workforce, simple growth signals.
- `AI.Company`: utility scorer for expansion/trade opportunities.
- `Persistence.Core`: save game DTOs, JSON serialization, save validation, stable state hash.
- `Content.Core`: JSON content loader, validation, and canonical content hash for P0 resources/recipes.
- `GodotBridge`: dependency-free bridge facade plus `PrototypeSession`, which runs a deterministic P0 loop across content, world, economy, logistics, city growth, AI, and persistence hashing.
- `ChartersOfTrade.Godot`: Godot .NET project with a `Main.tscn` prototype shell driven by `BootstrapPanel.cs`; it renders terrain, settlement nodes, route lines, KPI metrics, city summary, ledger, tick controls, city/route selection, hover states, route cash labels, animated route pulses, supply rings, route/city warning marks, city type stamps, Routes/Profit/Demand map modes, a route contract control placeholder, priority signals, and a contextual inspector.
- `Tests`: custom console test runner for determinism, terrain-sensitive world hashes, content validation, prototype ticks, declared consumption, save validation, save/load, economy, and AI; latest run passed 14/14.
- `Benchmarks`: console runner reporting seed-level playability metrics plus time-to-profit, bankruptcy frequency, post-run cash, AI move, and unmet demand.

## Changed Areas

- Repository initialized.
- .NET solution and module projects created.
- Root project memory and agent instructions added.
- Initial ADRs added.
- P0 content definitions added as JSON.
- Workspace-local .NET build/test/benchmark scripts added.
- Godot .NET CLI wrapper added at `tools/godot.ps1`.
- Bootstrap checkpoint recorded in `docs/checkpoints/2026-04-29-bootstrap.md`.
- `Content.Core` project added and wired into tests, benchmarks, and `GodotBridge`.
- Starter scenario now consumes `content/resources.p0.json` and `content/recipes.p0.json` instead of hardcoded starter content.
- Fixed P0 content validation issue by removing the unknown `linen` substitute from `wool`.
- Godot shell added: `ChartersOfTrade.Godot.csproj`, `scenes/Main.tscn`, and `Scripts/BootstrapPanel.cs`.
- Root `NuGet.Config` points at local GodotSharp packages.
- Content/Godot checkpoint recorded in `docs/checkpoints/2026-04-29-content-godot-shell.md`.
- Interactive P0 prototype loop added in `src/GodotBridge/PrototypeSession.cs`.
- Godot shell upgraded from static diagnostic card to interactive map/KPI/ledger/tick view.
- Benchmarks now run 12 prototype ticks per seed and report time-to-profit plus bankruptcy frequency.
- Prototype checkpoint recorded in `docs/checkpoints/2026-04-29-interactive-prototype-loop.md`.
- Review fixes added after delegated review: terrain is part of world hash, save validation rejects negative state, Godot runtime output receives P0 content JSON, and tool scripts now rebuild before running tests/benchmarks.
- Added the memory keeper ritual to `AGENTS.md` and recorded it as the project process for preserving context across compaction.
- Visual flow-map slice added to `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`: selectable cities/routes, highlighted connected flows, city supply rings, route cash labels, animated route pulses, contextual inspector, priority signals, and warmer ledger-cartography styling.
- Visual UX map modes added to `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`: Routes/Profit/Demand mode controls, city type stamps, demand and loss warning marks, clearer route/city inspectors, and a route contract control area that stays disabled until bridge contract data is available.
- Visual research note recorded in `docs/research/2026-04-29-visual-layer.md`.
- Visual checkpoint recorded in `docs/checkpoints/2026-04-29-visual-flow-map-slice.md`.
- Git baseline setup added `.gitattributes`, checkpointed the repository process in `docs/checkpoints/2026-04-29-git-baseline.md`, and connected `origin` to `https://github.com/damianthecoder/charters-of-trade.git`.
- Parallel day-plan instructions added in `docs/agent-plans/route-contract-system-agent.md` and `docs/agent-plans/visual-ux-map-modes-agent.md`.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed, 0 warnings.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: 14/14 passed plus Godot headless build/scene smoke.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: 25/25 playable seeds, average unmet demand ratio 0.6967, median time to profit 1.0, bankruptcy frequency 0/25 after 12 ticks.
- Current Codex macOS session could not rerun build/test scripts because `powershell`, `pwsh`, and `dotnet` are not installed. `git diff --check` passed for the visual UX map modes branch.

## Risks

- The Godot layer is interactive but still a prototype/debug shell, not final gameplay UI.
- The first economy model is intentionally simple and exists to prove determinism and test scaffolding, not final balance.
- Content validation exists in code, but there is no standalone JSON schema or authoring/export pipeline yet.
- Restore graph parallelism can collide in this Windows workspace; use the provided scripts or `-p:RestoreBuildInParallel=false -m:1`.
- Deterministic hashes must never use current-culture number formatting.
- Benchmark KPIs are still proxy metrics; `time-to-profit`, `unmet-demand-ratio`, and `bankruptcy frequency` now exist but are not final design targets.
- Godot CLI calls that touch editor settings may need to run outside the sandbox because Godot writes to `%APPDATA%`.
- `PrototypeSession` is a vertical-slice coordinator; if it grows much more, split stable logic into a proper simulation orchestration project.
- `tools/test.ps1` now includes Godot smoke and may need the same elevated filesystem access in sandboxed Codex sessions.
- The visual map currently redraws every frame for route pulse animation; acceptable for P0 scale, but cache static terrain/routes before larger maps.
- Current Godot smoke verifies scene startup, not interactive clicks or nonblank visual assertions.
- Parallel external collaboration now has a GitHub remote, but agents still need branch discipline to avoid overlapping edits.
- Visual UX map modes are currently statically checked only in this Codex environment; full Godot scene smoke and interaction QA still need the normal Windows Godot/.NET workstation.

## Next Step

Run full build/test/Godot smoke on the normal Windows Godot/.NET workstation for the visual UX map modes branch.
