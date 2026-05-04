# Automation UI Visibility Team Plan

## Purpose

Coordinate Autoprojektowanie #2 for the focused UI visibility pass identified in `PROJECT_MEMORY.md`: Stage 3-6 gameplay systems exist in the Godot sidebar, but testers cannot easily see what changed between development stages. This plan is the team communication hub for making the current MVP systems visibly distinct in Full HD without changing deterministic simulation state.

## Source Context

- `AGENTS.md`: keep the simulation core free of Godot dependencies; use project scripts for verification; larger Godot runtime changes need delegated review.
- `ADR-0001`: Godot is the presentation layer; pure .NET simulation core remains Godot-free.
- `ADR-0002`: deterministic save data and hashes must remain stable; UI-only visibility changes should not add save state.
- `ADR-0003`: the map is a hybrid raster and node graph; economy/logistics visibility should emphasize graph flows over tile simulation.
- `ADR-0007`: First Charter Season objective state is already persisted in save v3; this pass should present it more clearly, not revise objective rules.

## Team Roles

### Agent 1: Godot Visibility Implementation

- Owns Godot presentation changes that make Stage 3-6 systems readable at a glance.
- Primary focus: `src/ChartersOfTrade.Godot/Scripts/BootstrapPanel.cs`, relevant Godot scenes, visual smoke expectations, and screenshot-producing QA flows.
- Must keep all new visibility state transient and presentation-only unless explicitly coordinated with the supervisor.

### Agent 2: Bridge/Test Verification

- Owns bridge-facing assertions and automated checks that prove the UI is showing existing data correctly.
- Primary focus: `src/GodotBridge`, Godot smoke/visual QA tests, and `tools/test.ps1` / `tools/visual-qa.ps1` expectations.
- Must avoid expanding gameplay rules, save format, or deterministic scoring unless a new ADR is approved.

### Agent 3: Documentation And Communication

- Owns this communication plan only: `docs/agent-plans/automation-ui-visibility-team.md`.
- Tracks agreed roles, staging, acceptance criteria, handoff rules, file ownership, risks, and decisions for the team.
- Does not edit `PROJECT_MEMORY.md`, checkpoints, code, scenes, tests, or ADRs during this task.

### Supervisor: GPT-5.5

- Coordinates branch discipline, scope control, and cross-agent conflict resolution.
- Confirms when the work has become large enough to require delegated review under `AGENTS.md`.
- Decides whether any proposed change crosses from UI visibility into gameplay, persistence, determinism, tooling, or architecture and therefore needs a memory/checkpoint/ADR update by the appropriate owner.

## File Ownership

- Agent 1 owns Godot presentation files for the pass.
- Agent 2 owns bridge-facing checks, smoke assertions, visual QA expectations, and verification scripts when needed.
- Agent 3 owns only this file.
- `PROJECT_MEMORY.md`, `docs/checkpoints`, and ADR files are reserved for the supervisor or a designated memory keeper if the implementation phase changes a key system, test result, benchmark, tooling rule, review finding, or project risk.

## Staging

### Stage 0: Sync And Baseline

- Start from synced `main` or the supervisor-approved integration branch.
- Confirm no uncommitted changes belong to another agent before editing.
- Capture the current visual baseline from the latest passing `tools/test.ps1` / `tools/visual-qa.ps1` output when available.

### Stage 1: Visibility Design Contract

- Define the exact player-visible deltas before code edits:
  - First Charter Season progress should read as the active goal loop.
  - Production chains should show readiness, bottleneck, and destination value without looking like passive debug text.
  - Route operations should make active/paused/capacity status visible on both map and sidebar.
  - NPC pressure should surface as rival intent or pressure, not only a list entry.
  - Tick changes should provide visible feedback that the simulation advanced.
- Keep the design contract presentation-only.

### Stage 2: Implementation

- Agent 1 implements hierarchy, cards, badges, progress indicators, change feedback, and map overlays in Godot.
- Agent 2 adjusts smoke/visual QA assertions only where they verify actual user-visible outcomes.
- Do not add core dependencies on Godot or persist transient UI state.

### Stage 3: Review And Integration

- Run `git diff --check`.
- Run `powershell -ExecutionPolicy Bypass -File .\tools\build.ps1`.
- Run `powershell -ExecutionPolicy Bypass -File .\tools\test.ps1`.
- Run `powershell -ExecutionPolicy Bypass -File .\tools\visual-qa.ps1` when visual changes affect capture expectations.
- Use delegated review before the final implementation summary because this pass touches Godot runtime presentation.

### Stage 4: Handoff

- Supervisor or memory keeper records final system/test/checkpoint updates if required by `AGENTS.md`.
- Agent 3 updates this plan only if team decisions, ownership, staging, or risks change.
- Final handoff must list changed files and verification results.

## Acceptance Criteria

- Full HD first screen clearly communicates that the current build includes First Charter Season, production chains, route operations, NPC pressure, and warehouse/route policies.
- At least three Stage 3-6 systems have visible map or status-card treatment beyond plain sidebar paragraphs.
- Tick advancement creates readable change feedback without changing deterministic state or save hashes.
- Interaction smoke still verifies core user actions and expected panel visibility.
- Visual QA captures demonstrate distinct states for objective progress, NPC pressure, route operations, and production opportunities.
- Core projects remain free of Godot dependencies.
- No save format, scenario rule, benchmark KPI, or AI scoring change is introduced by this visibility pass unless separately approved and documented.

## Status Handoff Rules

- Each agent posts status using this shape:
  - `Branch`: current branch and latest commit if pushed.
  - `Files touched`: exact owned files changed.
  - `Behavior changed`: visible behavior only, or state clearly if no behavior changed.
  - `Verification`: commands run and concrete pass/fail result.
  - `Risks/blockers`: one or more actionable items, or `None`.
  - `Next step`: exactly one next action.
- Status should distinguish implemented behavior from proposed behavior.
- Agents must not report another agent's file as complete without reading the current diff.
- When a conflict appears, pause overlapping edits and ask the supervisor to assign the integration owner.

## Risks

- UI visibility work can accidentally become gameplay design if it changes scenario rules, route policy effects, NPC scoring, or save fields.
- `BootstrapPanel.cs` is already a large vertical-slice coordinator; presentation additions should avoid burying important state transitions in unrelated layout code.
- Visual smoke and Godot CLI may be sensitive to local Godot processes or log paths; stale windows can show older compiled UI.
- Sidebar-only improvements may fail the goal; the map and first viewport need to carry more of the new-system signal.
- Adding too many badges or panels can reduce legibility at 1920x1080 and make mobile/lower-resolution adaptation harder later.

## Decisions

- This pass is scoped as presentation and verification, not new gameplay.
- First Charter Season save v3 state is the source of objective truth; do not add alternate UI-only objective progress calculations.
- NPC pressure remains derived and non-persistent for this pass.
- Production chain opportunities remain read-only unless a later supervisor-approved task records a gameplay/save decision.
- Route operation visibility should explain existing active/paused/capacity data before adding new controls.
- Agent 3's documentation scope is intentionally limited to this communication hub.
