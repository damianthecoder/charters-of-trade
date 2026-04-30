# Project Memory

## Current Goal

Build the next P0 vertical-slice layer as a player-facing test harness: `agent/graphic-polish-pass` continues from the synced Full HD/visual readability work and makes the Godot prototype more appealing and readable for manual systems testing. The current pass follows the GPT Image mockup direction in code-native Godot UI: map-first composition, top map HUD, left map-mode rail, clearer labels, route direction markers, textured terrain detail, and sectioned sidebar panels.

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
- Graphic polish is a Godot presentation concern only. Map HUD chrome, label placement, terrain detail strokes, route arrows, button styles, and sidebar section panels are testing/readability affordances, not simulation state.
- Git is the project coordination baseline. Work should branch from `main`, keep generated Godot/.NET caches ignored, and use commits/checkpoints to make parallel agent work reviewable. The GitHub remote is `origin` at `https://github.com/damianthecoder/charters-of-trade.git`.
- Each development phase starts by syncing local `main` from `origin/main`, creating a focused feature branch, and ends by running verification, committing, pushing to GitHub, and confirming the remote branch head.
- Route contract selection is gameplay state, not transient UI state. Pending selected contracts are included in `SaveGame.PendingRouteContractId` and therefore in the state hash.
- `tools/test.ps1` uses the normal solution build plus the Godot interaction smoke scene. The separate Godot `--build-solutions --quit` step was removed because it hung and produced a Godot crash dialog in this workspace.
- Cross-agent branch status: `origin/agent/visual-ux-map-modes` commit `ac44fb6` has been merged into the route-contract work. The integrated Godot UI now uses typed `AvailableContracts`, `SelectedContractId`, and `SelectRouteContract` instead of the visual branch's temporary reflection/placeholder bridge path.
- World generation version `0.2.0` uses deterministic coherent terrain fields with border water, readable landmass/coastlines, explicit settlement spacing, coastal ports placed on coast-adjacent land, and coastal route mode only between two ports.
- `tools/test.ps1` runs the normal build, console tests, headless Godot interaction smoke, and non-headless `tools/visual-smoke.ps1` Full HD render verification by default; set `COT_SKIP_VISUAL_SMOKE=1` only for constrained automation.

## System State

- `WorldGen.Core`: deterministic seedable world generation, coherent terrain raster, readable coastline/landmass constraints, settlement nodes, route candidates, world hash, solvency kernel.
- `Economy.Core`: resource and recipe definitions, inventory, stock-pressure-aware market pricing, basic production tick.
- `Logistics.Core`: routes, capacities, lead time, route profitability.
- `CitySim.Core`: cohort population, city stock, workforce, simple growth signals.
- `AI.Company`: utility scorer for expansion/trade opportunities.
- `Persistence.Core`: save game DTOs, JSON serialization, save validation, stable state hash, and pending route contract id support.
- `Content.Core`: JSON content loader, validation, and canonical content hash for P0 resources/recipes.
- `GodotBridge`: dependency-free bridge facade plus `PrototypeSession`, which runs a deterministic P0 loop across content, world, economy, logistics, route contracts, city growth, AI, persistence hashing, per-city market pressure signals, and lightweight warehouse policy signals.
- `ChartersOfTrade.Godot`: Godot .NET project with a `Main.tscn` prototype shell driven by `BootstrapPanel.cs`; it renders coherent terrain, coastline highlights, terrain texture marks, settlement nodes, route lines/arrows, top map HUD, left map-mode rail, KPI metrics, city summary, ledger, tick controls, city/route selection, hover states, collision-aware map labels, route cash labels, animated route pulses, supply rings, route/city warning marks, city type stamps, Routes/Profit/Demand map modes, polished route contract controls, market pressure, warehouse policy signals, sectioned sidebar panels, and a contextual inspector. `InteractionSmoke.tscn` loads the real scene and exercises expected user actions headlessly.
- `Tests`: custom console test runner for determinism, terrain-sensitive world hashes, coherent world readability invariants, dense settlement configs, content validation, prototype ticks, route contracts, declared consumption, save validation, save/load, economy, AI, and Godot interaction smoke; latest Windows run passed 25/25 plus `INTERACTION_SMOKE PASS` and `VISUAL_SMOKE PASS`.
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
- Visual UX map modes checkpoint recorded in `docs/checkpoints/2026-04-29-visual-ux-map-modes.md`.
- Git baseline setup added `.gitattributes`, checkpointed the repository process in `docs/checkpoints/2026-04-29-git-baseline.md`, and connected `origin` to `https://github.com/damianthecoder/charters-of-trade.git`.
- Parallel day-plan instructions added in `docs/agent-plans/route-contract-system-agent.md` and `docs/agent-plans/visual-ux-map-modes-agent.md`.
- Route contract system added on branch `agent/route-contract-system`: `PrototypeRouteContractView`, `PrototypeSnapshot.AvailableContracts`, `PrototypeSnapshot.SelectedContractId`, `PrototypeSession.SelectRouteContract`, production reservation for contracted cargo, `SaveGame.PendingRouteContractId`, and deterministic tests.
- `tools/test.ps1` now skips the redundant Godot `--build-solutions --quit` step and keeps Godot scene smoke.
- Route contract checkpoint recorded in `docs/checkpoints/2026-04-29-route-contract-system.md`.
- Cross-agent repo sync rechecked `origin/agent/visual-ux-map-modes`; it now contains `ac44fb6 Add visual UX map modes`, changing `PROJECT_MEMORY.md`, adding `docs/checkpoints/2026-04-29-visual-ux-map-modes.md`, and heavily updating `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`.
- Integrated visual UX map modes into `agent/route-contract-system`, resolved `PROJECT_MEMORY.md`, converted Godot route contract controls from reflection to typed `GodotBridge` API calls, fixed route inspector "no contracts" messaging, and refreshed contract summary text when the dropdown selection changes.
- Stabilized the integrated route contract UX: contract dropdown labels now show rank, city names, resource labels, and signed net values; summaries distinguish selected, best, preview, empty, and stale contract states; city/route inspectors explain contract context more directly.
- Added Godot interaction smoke tooling in `InteractionSmokeRunner.cs` and `InteractionSmoke.tscn`; `tools/test.ps1` now runs this smoke path instead of only starting `Main.tscn`.
- Stabilized Windows interaction smoke after verification: increased the Godot frame budget, read `RichTextLabel.GetParsedText()` for appended inspector text, and used a headless-safe post-interaction UI assertion instead of sampling a dummy viewport texture.
- Economy depth pass started on `agent/economy-depth-pass` from synced `origin/main`: local prices now react to stockout, near-term coverage, surplus, and perishability; `PrototypeCityView` exposes `PrototypeMarketSignal`; Godot city/route inspectors and priority signals show local shortage reasons instead of only charter-town pressure.
- Logistics/warehouse policy pass started on `agent/logistics-warehouse-policies` from synced `agent/economy-depth-pass`: `PrototypeMarketSignal` now includes safety stock, reorder point, shipment priority, and policy action; automatic logistics and route contracts prioritize urgent destination needs and avoid exporting source warehouse stock below safety reserve.
- Full HD test UI pass started on `agent/full-hd-test-ui` from synced `agent/logistics-warehouse-policies`: Godot project viewport is now 1920x1080 with canvas-item stretch, the prototype shell uses responsive layout profiles and a scrollable sidebar, and the sidebar includes a System Test Bench with seed reset, Run 12, and a live system probe.
- Visual readability pass started on `agent/visual-readability-pass` from synced `agent/full-hd-test-ui`: GPT Image produced a UI direction mockup; `BootstrapPanel.cs` now gives the map a fixed primary share of Full HD, labels explanations by working systems (`Company Ledger`, `Market Pressure`, `Warehouse Policy`, `Route Contract`), caches terrain lookup for coast rendering, and uses cleaner route/city labels for visual testing.
- World generation now uses coherent deterministic value-noise terrain fields, records `WorldGenVersion` `0.2.0`, enforces readable settlement spacing for P0 configs, places ports only on coast-adjacent land, and limits coastal route mode to port-to-port edges.
- Added `ADR-0005-coherent-worldgen-fields.md`; `tools/visual-smoke.ps1` now provides non-headless Full HD render verification and is called by `tools/test.ps1` unless `COT_SKIP_VISUAL_SMOKE=1`.
- Graphic polish pass started on `agent/graphic-polish-pass` from synced Full HD work: `BootstrapPanel.cs` now uses sectioned sidebar cards, styled buttons, larger log text, a map title/HUD strip, left map-mode rail, richer deterministic terrain detail, route outlines/arrows, and collision-aware city labels. Review feedback fixed Event Ledger panel sizing and duplicate hover labels.

## Tests

- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`: passed, 0 warnings.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`: 18/18 passed plus Godot headless scene smoke.
- `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1`: 25/25 playable seeds, average unmet demand ratio 0.6967, median time to profit 1.0, bankruptcy frequency 0/25 after 12 ticks.
- Integrated branch verification after review fixes: `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` passed outside the sandbox with 18/18 tests plus Godot headless scene smoke. The same command first failed inside the sandbox because Godot could not write `user://logs` and crashed with signal 11.
- Integrated branch benchmark after review fixes: `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1` passed with 25/25 playable seeds, average unmet demand ratio 0.6967, median time to profit 1.0, bankruptcy frequency 0/25 after 12 ticks.
- Visual smoke capture passed with Godot movie maker at `artifacts/godot-smoke/visual-smoke00000002.png`; the rendered frame is nonblank, shows Routes/Profit/Demand buttons, city stamps, routes, KPIs, and an active route contract dropdown.
- Windows verification after smoke stabilization: `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` passed with 18/18 tests and `INTERACTION_SMOKE PASS`; `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1` passed with 25/25 playable seeds, average unmet demand ratio 0.6967, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.
- Economy depth branch verification: `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` passed with 20/20 tests and `INTERACTION_SMOKE PASS`; `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1` passed with 25/25 playable seeds, average unmet demand ratio 0.6967, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.
- Logistics/warehouse policy branch verification: `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` passed with 22/22 tests and `INTERACTION_SMOKE PASS`; `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1` passed with 25/25 playable seeds, average unmet demand ratio 0.7055, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.
- Full HD test UI verification: `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` passed with 22/22 tests and `INTERACTION_SMOKE PASS` at tick 18; `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1` passed with 25/25 playable seeds, average unmet demand ratio 0.7055, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks. Godot movie writer produced a confirmed 1920x1080 control frame at `artifacts/godot-smoke/full-hd-test-ui00000035.png`.
- Visual readability verification after review fixes: `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` passed with 25/25 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`, producing `artifacts/godot-smoke/visual-smoke-20260430-03483900000002.png`; `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1` passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.
- Graphic polish verification after review fixes: `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` passed with 25/25 tests, `INTERACTION_SMOKE PASS`, and `VISUAL_SMOKE PASS`, producing `artifacts/godot-smoke/visual-smoke-20260430-04040700000002.png`; `powershell -ExecutionPolicy Bypass -File .\tools\benchmark.ps1` passed with 25/25 playable seeds, average unmet demand ratio 0.7115, median time to profit 1.0, and bankruptcy frequency 0/25 after 12 ticks.

## Risks

- The Godot layer is interactive but still a prototype/debug shell, not final gameplay UI.
- The first economy model is intentionally simple and exists to prove determinism and test scaffolding, not final balance.
- Content validation exists in code, but there is no standalone JSON schema or authoring/export pipeline yet.
- Restore graph parallelism can collide in this Windows workspace; use the provided scripts or `-p:RestoreBuildInParallel=false -m:1`.
- Deterministic hashes must never use current-culture number formatting.
- Benchmark KPIs are still proxy metrics; `time-to-profit`, `unmet-demand-ratio`, and `bankruptcy frequency` now exist but are not final design targets.
- Godot CLI calls that touch editor settings may need to run outside the sandbox because Godot writes to `%APPDATA%`.
- `PrototypeSession` is a vertical-slice coordinator; if it grows much more, split stable logic into a proper simulation orchestration project.
- `tools/test.ps1` now includes Godot interaction smoke and may need the same elevated filesystem access in sandboxed Codex sessions.
- The visual map still redraws every frame for route pulse animation; terrain lookup is cached, but larger maps should cache static render layers before scale increases.
- `tools/test.ps1` now includes non-headless visual smoke by default, so sandboxed Codex sessions may need approval for Godot log and renderer artifact writes.
- Parallel external collaboration now has a GitHub remote, but agents still need branch discipline to avoid overlapping edits.
- `SaveGame.PendingRouteContractId` is a prototype save v1 extension; future save migrations should formalize command/contract state.
- Godot `--build-solutions --quit` can hang/crash in this workspace; do not re-add it to the test script unless the underlying Godot CLI issue is understood.
- Godot CLI smoke may need to run outside sandboxed Codex sessions because Godot writes editor/runtime logs under `user://`.
- Map-label placement is still a simple local collision pass; future larger maps may need a dedicated label layout/cache layer.

## Next Step

Run manual visual QA across Routes/Profit/Demand modes and multiple seeds at 1920x1080 before adding the next warehouse automation controls.
