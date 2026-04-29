# Agent Plan: Route Contract System

## Mission

Implement the first player-facing route contract choice so the selected route contract affects the deterministic P0 prototype loop.

Work on branch:

```powershell
git checkout main
git pull
git checkout -b agent/route-contract-system
```

## Ownership

Primary files:

- `src/GodotBridge/PrototypeSession.cs`
- `src/GodotBridge/SimulationBridge.cs`
- `tests/ChartersOfTrade.Tests/Program.cs`
- `benchmarks/ChartersOfTrade.Benchmarks/Program.cs`
- relevant docs/checkpoints

Avoid editing:

- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs` except for a tiny compatibility stub if absolutely required.
- Godot scenes/assets.
- Core projects unless a small model belongs there and remains Godot-free.

You are not alone in the codebase. Another agent may be changing the Godot UI at the same time. Do not revert their work. Keep the bridge contract stable and communicate any contract change through commit notes and docs.

## Required Context

Before editing, read:

- `PROJECT_MEMORY.md`
- `AGENTS.md`
- `docs/adr/ADR-0001-godot-dotnet-core.md`
- `docs/adr/ADR-0002-determinism-save-format.md`
- `docs/adr/ADR-0004-discrete-economy-tick.md`
- `docs/checkpoints/2026-04-29-visual-flow-map-slice.md`

## Shared Bridge Contract

Implement or preserve this surface for the visual agent:

```csharp
PrototypeSnapshot.AvailableContracts
PrototypeSnapshot.SelectedContractId
PrototypeSession.SelectRouteContract(string contractId)
```

Contract view data should include:

```csharp
Id
RouteId
FromNode
ToNode
ResourceId
ExpectedRevenue
TransportCost
ExpectedNet
CapacityPerDay
```

Keep this data Godot-free. Do not add `Vector2`, `Color`, `Node`, input state, or scene concepts to `GodotBridge` or core projects.

## Tasks

1. Add a Godot-free route contract view model in `GodotBridge`.
2. Expose available contracts in `PrototypeSnapshot`.
3. Track the selected contract id in `PrototypeSession`.
4. Add `SelectRouteContract(string contractId)` with deterministic validation.
5. Make `RunLogistics` prefer or obey the selected contract.
6. Ensure contract-driven ticks still produce stable save hashes.
7. Add deterministic tests for:
   - available contracts are stable for the same seed
   - selecting a contract affects the next logistics tick
   - invalid contract ids are rejected or ignored consistently
   - save hash remains deterministic across equivalent sessions
8. Update benchmark output only if contract behavior changes benchmark meaning.
9. Update `PROJECT_MEMORY.md` and add a checkpoint if behavior changes.

## Ready Criteria

This agent is done when:

- The selected route contract changes the next tick's logistics behavior.
- `PrototypeSnapshot` exposes contract data the UI can render.
- Tests prove deterministic contract behavior.
- The simulation core remains free of Godot dependencies.
- `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1` passes.
- `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1` passes.

Run `tools/benchmark.ps1` if route contract behavior changes benchmark interpretation.

## Commit Plan

Prefer small commits:

```text
Expose route contract choices in prototype session
Apply selected route contracts during logistics tick
Test deterministic route contract behavior
Record route contract checkpoint
```

Push the branch:

```powershell
git push -u origin agent/route-contract-system
```

## Review Requirements

This touches logistics orchestration and possibly benchmark behavior, so request at least one review focused on:

- determinism
- no negative stock
- route/cash invariants
- save hash stability
- no Godot dependency leak

If the change expands beyond `GodotBridge` into multiple core subsystems, request a second reviewer for integration and test coverage.

## Handoff Notes

The visual agent should be able to build UI against `PrototypeSnapshot.AvailableContracts`, `PrototypeSnapshot.SelectedContractId`, and `PrototypeSession.SelectRouteContract`. If this surface changes, update this file and mention the exact replacement in the PR/commit summary.
