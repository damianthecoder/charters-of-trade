# Agent Plan: Warehouse Policy View Controls

## Scope

Continue after `graphic-polish-pass` with a presentation-only Godot pass. The goal is to make the existing warehouse policy signals easier to inspect during manual systems testing, without adding a new saved gameplay system.

## Team

### Agent 1: Product And Gameplay Planner

- Keep the P0 scope centered on controlling flows, margins, shortages, and company value.
- Reject scope drift into city building, war, politics, or full logistics automation.
- Define whether each proposed control is a UI helper or gameplay state.
- Acceptance check: every new control answers how the tester understands or controls trade flows.

### Agent 2: Godot UI Implementer

- Own `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs` and Godot-only interaction smoke changes.
- Add compact `Warehouse Policy` view controls for focus and signal sorting.
- Keep selection, filters, hover state, and visual mode state inside Godot UI.
- Do not add Godot concepts to `GodotBridge`, core projects, or save DTOs.

### Agent 3: QA And Integration Planner

- Own verification guidance for `tools/build.ps1`, `tools/test.ps1`, `tools/benchmark.ps1`, interaction smoke, and visual smoke.
- Track baseline metrics: 25/25 playable seeds, bankruptcy 0/25, median time to profit 1.0, unmet demand around 0.7115 after 12 ticks.
- Call out CI/sandbox risks, especially non-headless Godot visual smoke and Godot user log writes.
- Require save/hash tests if any warehouse control becomes gameplay state.

### GPT-5.5 Lead Reviewer

- Review the direction at each gate before the final summary.
- Confirm the change remains within MVP scope and project thesis.
- Check that the simulation core remains Godot-free and that UI helper state does not enter save/load.
- Organize communication through markdown records rather than relying on chat history.

## Communication Files

- `PROJECT_MEMORY.md`: current project state, latest decisions, test results, risks, and the next step.
- `docs/checkpoints`: session checkpoints with summary, changed systems, tests, review notes, risks, and exactly one next step.
- `docs/agent-plans`: branch or phase plans and reviewer reports.
- `docs/adr`: durable architecture, determinism, save format, data, or tooling decisions only.
- `docs/research`: blocking implementation notes after 15 minutes of blocked work.

## Review Gates

1. Intake: read `PROJECT_MEMORY.md`, `AGENTS.md`, and relevant ADRs before editing.
2. Scope: confirm the work stays inside graph-first mercantile flow control.
3. Architecture: keep Godot presentation state separate from deterministic simulation and save format.
4. Tests: run the project scripts appropriate to touched systems.
5. Review: use at least one delegated review for Godot runtime changes.
6. Memory: update project memory and add a checkpoint before the final session summary.

## Stop Conditions

- A Godot dependency appears in a simulation core project.
- Determinism, content hash, worldgen, or save format changes without an ADR or targeted tests.
- The work expands MVP scope without a recorded decision.
- `tools/test.ps1` or `tools/benchmark.ps1` fails without a recorded reason.
- A blocker lasts more than 15 minutes without a research note.

## Current Accepted Step

Add Godot-only warehouse policy view controls that sort and focus existing `PrototypeMarketSignal` data. These controls are inspection aids only and do not change logistics behavior, save state, or benchmark semantics.

