# Agent Plan: Warehouse Controls Integration

## Status

Accepted for the 2026-04-30 integration pass.

## Mission

Reconcile the verified `agent/warehouse-control-loop` branch with later warehouse UI and automation experiments into one deterministic, Godot-free gameplay-control slice. Save version 2 and `SaveGame.WarehousePolicies` remain canonical.

## Team

### Agent 1: Simulation, Save, Logistics

Owns:

- `src/GodotBridge/PrototypeSession.cs`
- `src/GodotBridge/SimulationBridge.cs`
- `src/Persistence.Core/SaveGame.cs`
- `tests/ChartersOfTrade.Tests/Program.cs`
- `docs/agent-comms/warehouse-controls-integration-agent1.md`

Responsibilities:

- Keep simulation and save state free of Godot dependencies.
- Extend the current warehouse policy loop into route-level resource reservation and priority controls where useful.
- Preserve save version 2 and deterministic state hashing.
- Add focused tests for route policy validation, reservation filtering, priority ordering, save/load, and hash behavior.

### Agent 2: Godot UI, Smoke, Visual QA

Owns:

- `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`
- `src/ChartersOfTrade.Godot/Scripts/InteractionSmokeRunner.cs`
- `src/ChartersOfTrade.Godot/Scripts/VisualQaRunner.cs`
- `src/ChartersOfTrade.Godot/scenes/VisualQa.tscn`
- `tools/visual-qa.ps1`
- `tools/godot.ps1`
- `.gitignore`
- `docs/agent-comms/warehouse-controls-integration-agent2.md`

Responsibilities:

- Preserve the map-first Full HD layout and sidebar gutter.
- Expose policy and route controls compactly without storing presentation state in the bridge.
- Bring over useful visual QA tooling from the UI-only branch.
- Expand interaction smoke only after the simulation API is stable.

## Shared Communication Files

- Team plan: `docs/agent-plans/warehouse-controls-integration-team.md`
- Agent 1 notes: `docs/agent-comms/warehouse-controls-integration-agent1.md`
- Agent 2 notes: `docs/agent-comms/warehouse-controls-integration-agent2.md`
- Final checkpoint: `docs/checkpoints/2026-04-30-warehouse-controls-integration.md`

## Integration Rules

- Do not replace save version 2 with any save-v1 automation model.
- Do not duplicate ADR-0006; update it only if the save decision materially changes.
- Do not merge unverified branch diffs wholesale. Port small, reviewed pieces.
- Keep generated artifacts out of commits.
- Run `tools/test.ps1` and `tools/benchmark.ps1` before final summary.

## Ready Criteria

- One canonical warehouse/route policy model.
- Tests cover deterministic behavior and invalid policy no-ops.
- Godot smoke proves the controls are visible and usable.
- Visual QA exists for manual multi-seed inspection.
- `PROJECT_MEMORY.md` and a checkpoint record decisions, tests, review notes, risks, and exactly one next step.
