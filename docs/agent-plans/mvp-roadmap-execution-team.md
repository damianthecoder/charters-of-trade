# Agent Plan: MVP Roadmap Execution

## Status

Accepted for the 2026-04-30 multi-agent roadmap pass on `agent/mvp-roadmap-execution`.

## Mission

Turn the verified warehouse/route policy test harness into the next playable MVP layer without losing determinism, Godot-free simulation boundaries, or the current Windows verification baseline.

## Coordination Rules

- Branch baseline: `agent/mvp-roadmap-execution`, created from synced `agent/warehouse-controls-integration` at `68b3c2f`.
- Keep the simulation core free of Godot dependencies.
- Use Team communication files before and after each stage.
- Do not expand MVP scope beyond the six stages without adding a decision note.
- Prefer small vertical slices that compile and test over broad unverified rewrites.
- Any implementation touching world generation, economy, logistics, save/load, AI, balance, CI, or Godot runtime needs review before final summary.
- If two stages need the same file, the stage owner must record the dependency and avoid editing until coordination assigns order.
- Verification target: `tools/build.ps1`, `tools/test.ps1`, `tools/benchmark.ps1`, plus `tools/visual-qa.ps1` for Godot UI changes.

## Stage Agents

### Stage 1: Stabilize And Merge Readiness

Communication file: `docs/agent-comms/mvp-stage1-stabilize.md`

Owns:

- PR readiness checklist.
- Manual QA checklist for Balanced/Conservative, route blocking, route priority, map modes, and contract selection.
- Merge-risk notes for `agent/warehouse-controls-integration -> main`.

Output:

- A concise readiness plan and any blockers. No gameplay implementation.

### Stage 2: City Specialization

Communication file: `docs/agent-comms/mvp-stage2-city-specialization.md`

Owns:

- City role model proposal and bounded implementation slice.
- City/district/role data surfaces in core or bridge.
- Tests proving deterministic city identities.

Output:

- The first playable city-specialization slice or a patch plan if blocked.

### Stage 3: Production Chain Gameplay

Communication file: `docs/agent-comms/mvp-stage3-production-chains.md`

Owns:

- Visible production-chain opportunity model.
- Recipe/input/output/margin explanations.
- Tests for deterministic chain opportunity ordering.

Output:

- A chain-opportunity implementation plan or bounded patch coordinated with Stage 2.

### Stage 4: Route Operations

Communication file: `docs/agent-comms/mvp-stage4-route-operations.md`

Owns:

- Route operation loop: capacity, recurring order/charter shape, priority cargo, blocked cargo, expected profit, unmet demand served.
- Tests for deterministic recurring route operations.

Output:

- A route-operation implementation plan or bounded patch coordinated with route policy save state.

### Stage 5: NPC Company AI

Communication file: `docs/agent-comms/mvp-stage5-npc-ai.md`

Owns:

- Visible NPC company moves and explainable AI pressure.
- Deterministic AI action summaries.
- Tests for best-move selection and non-random pressure.

Output:

- A deterministic NPC move slice or implementation plan coordinated with Stage 3/4 surfaces.

### Stage 6: MVP Player Goal Loop

Communication file: `docs/agent-comms/mvp-stage6-goal-loop.md`

Owns:

- Scenario objective shape: cash target, supply stability, charter contracts, bankruptcy guard, score/end summary.
- Save/hash implications for scenario progress.
- Tests for deterministic objective state.

Output:

- A goal-loop implementation plan or bounded patch coordinated with Stage 2-5 outputs.

## Reviewer And Coordinator

Communication file: `docs/agent-comms/mvp-review-coordination.md`

Owns:

- Compliance with `AGENTS.md`, `PROJECT_MEMORY.md`, and ADRs.
- Cross-stage conflict detection.
- Review of stage outputs, test gaps, architecture risks, and scope creep.
- Final go/no-go recommendation for integration order.

Output:

- A coordination report with blockers, dependencies, and recommended integration sequence.

## Ready Criteria

- Stage communication files are updated.
- Implementation work is limited to coordinated, testable slices.
- `PROJECT_MEMORY.md` and a checkpoint are updated before final summary.
- Local and GitHub branch heads are synchronized after verified commits.
